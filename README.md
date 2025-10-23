# File Domain Plugin Provider
[![Auto build](https://github.com/DKorablin/Plugin.FileDomainPluginProvider/actions/workflows/release.yml/badge.svg)](https://github.com/DKorablin/Plugin.FileDomainPluginProvider/releases/latest)

Plugin provider for loading runtime plugins from the file system with an inspection (sandbox) AppDomain to reduce primary AppDomain pollution and mitigate DLL Hell.

## Features
* Scans one or many folders for plugin assemblies.
* Uses a secondary AppDomain to reflect and detect valid plugin types (implements `IPlugin`). Only needed assemblies are then loaded into the primary AppDomain.
* Supports multiple library extensions (see `FilePluginArgs.LibraryExtensions`).
* Watches folders (FileSystemWatcher) and loads newly changed assemblies on the fly.
* Custom dependency resolution inside each scanned folder (local probing before escalating to parent provider).

## How It Works (Pipeline)
1. Resolve plugin search paths: read `SAL_Path` command line argument (semicolon separated). If absent, use the current process directory.
2. For every existing path, enumerate files (recursive) that match allowed extensions.
3. For each candidate file, queue an `AssemblyTypesReader` in the sandbox AppDomain (via `AssemblyAnalyzer`) to:
   - Load the assembly (LoadFrom).
   - Enumerate types and filter with `PluginUtils.IsPluginType` (implements required interfaces / constraints).
   - Capture errors (BadImageFormat, ReflectionTypeLoad issues) into `AssemblyTypesInfo`.
4. Return the collected `AssemblyTypesInfo[]` to the primary AppDomain via a `MarshalByRefObject` proxy.
5. In the primary domain, for each successful result load the assembly (LoadFile) and register every discovered plugin type with `Host.Plugins.LoadPlugin`.
6. Set up `FileSystemWatcher` per folder to monitor future changes; on change, repeat the check for that single file.

## Dependency Resolution
During analysis the sandbox attaches handlers:
* `ReflectionOnlyAssemblyResolve` and `AssemblyResolve` search sibling files in the same directory using the same allowed extensions list.
* This limits loading of unrelated assemblies and prevents the primary domain from loading assemblies that ultimately provide no plugin types.

## Assembly Identity / Duplicate Handling
Two assemblies with the same identity (Name, Version, Culture, PublicKeyToken) cannot both be loaded side‑by‑side in one AppDomain. Target framework is NOT part of identity. The reader detects if a different file path resolves to an already loaded assembly and skips it. Ensure you version or rename multi-target builds to avoid silent reuse of a previously loaded variant.

## Configuration
Command line: `SAL_Path=DirA;DirB;DirC` (no quotes, semicolon separated). Missing or empty uses the process base directory.
Extensions: controlled by `FilePluginArgs.LibraryExtensions` (default includes `.dll`). Add more there if you need (e.g. native adapters) before enumeration.

## Implementing a Plugin
1. Reference the SAL core (interfaces providing `IPlugin`, `IPluginProvider`, etc.).
2. Implement your plugin class deriving / implementing the required interfaces. Exported public, non-abstract.
3. Build targeting a framework compatible with the host (.NET Framework 3.5 for this provider). Multi-target packages must avoid identity clashes.
4. Copy the resulting assembly (and its private dependencies) into one of the watched plugin folders.
5. Optional: include a strong name and increment version numbers to allow upgrading side by side (after process restart if needed).

Minimal pattern:
```
public sealed class MySamplePlugin : IPlugin {
    public bool OnConnection(ConnectMode mode) { /* init */ return true; }
    public bool OnDisconnection(DisconnectMode mode) { return true; }
}
```

## Hot Reload Behavior
* Only Changed events are handled; modifying (overwrite) an existing file triggers a rescan and load (types added become available). Existing loaded assembly cannot be unloaded (CLR limitation in .NET Framework). A full process restart is required to replace an already loaded assembly with a newer build (identity must change or file locked).
* New files are loaded automatically after detection.

## Limitations / Notes
* Target framework: .NET Framework 3.5 – modern APIs (AssemblyLoadContext, unloadable contexts) are not available.
* No unloading of individual plugins or assemblies (AppDomain unload would require isolating each assembly in its own domain, not implemented).
* Avoid copying multiple versions of the same assembly (same identity) across scanned folders.
* Reflection-only load path kept minimal to avoid StackOverflowException (see guarded return of null in resolve handler).
* Errors during type load are captured and traced; assemblies with zero valid plugin types are ignored.

## Troubleshooting
Problem: Assembly skipped with message about already loaded path.
Cause: Identity clash (same Name/Version/PKT). Rename or bump version. Check that you don't have multiple copies (included based on different .NET versions) of the same assembly in different plugin folders.

Problem: BadImageFormatException.
Cause: Not a valid .NET assembly or built for incompatible architecture.

Problem: Types empty.
Cause: No public types implementing expected plugin interface; ensure correct reference to core interfaces.

Problem: Dependencies not found.
Cause: Missing dependent DLL in same directory. Copy dependency next to plugin or ensure it is already loaded by host.