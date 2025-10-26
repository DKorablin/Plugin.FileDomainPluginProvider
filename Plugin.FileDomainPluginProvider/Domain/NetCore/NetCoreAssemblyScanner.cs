using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Runtime.Loader;
#endif
using SAL.Flatbed;

namespace Plugin.FileDomainPluginProvider.Domain.NetCore
{
	/// <summary>net5+ implementation that scans assemblies for plugin types using AssemblyLoadContext instead of AppDomain.</summary>
	internal class NetCoreAssemblyScanner : IDisposable
	{
#if NET5_0_OR_GREATER
		private readonly IsolatedLoadContext _loadContext;

		public NetCoreAssemblyScanner(String directoryName)
			=> _loadContext = new IsolatedLoadContext(directoryName, isCollectible: true);

		public AssemblyTypesInfo ScanAssemblyImpl(String file)
		{
			try
			{
				Assembly assembly = this._loadContext.LoadFromPluginPath2(Path.GetFullPath(file));
				List<String> types = new List<String>();
				foreach(Type t in assembly.GetTypes())
					if(PluginUtils.IsPluginType(t))
						types.Add(t.FullName);
				return types.Count > 0 ? new AssemblyTypesInfo(file, types.ToArray()) : null;
			} catch(BadImageFormatException)
			{
				return null;
			} catch(ReflectionTypeLoadException exc)
			{
				String errors = exc.LoaderExceptions?.Length > 0
					? String.Join(Environment.NewLine, new HashSet<String>(Array.ConvertAll(exc.LoaderExceptions, e => e.Message)).ToArray())
					: exc.Message;
				return new AssemblyTypesInfo(file, errors);
			} catch(FileLoadException exc)
			{
				// Handle scenario when assembly with same identity is already loaded into this context.
				// We search for existing loaded assembly with matching full name and provide its location as referencedAssemblyPath.
				AssemblyName targetName = AssemblyName.GetAssemblyName(file);
				Assembly existing = _loadContext.Assemblies.FirstOrDefault(a => a.GetName().FullName == targetName.FullName);
				if(existing != null && !String.Equals(existing.Location, file, StringComparison.OrdinalIgnoreCase))
					return new AssemblyTypesInfo(file, $"Assembly \"{existing.FullName}\" will be skipped because it’s already loaded from \"{existing.Location}\".", existing.Location);
				else
					return new AssemblyTypesInfo(file, (exc.InnerException ?? exc).Message, existing?.Location);
			} catch(Exception exc)
			{
				return new AssemblyTypesInfo(file, (exc.InnerException ?? exc).Message);
			}
		}

		public void Dispose()
		{
			this._loadContext.Unload();
		}

		public static AssemblyTypesInfo[] ScanFolder(String path)
		{
			List<AssemblyTypesInfo> result = new List<AssemblyTypesInfo>();
			if(!Directory.Exists(path))
				throw new DirectoryNotFoundException($"The directory \"{path}\" does not exist.");

			using(NetCoreAssemblyScanner scanner = new NetCoreAssemblyScanner(path))
				foreach(String file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
					if(FilePluginProvider.FilePluginArgs.CheckFileExtension(file))
					{
						AssemblyTypesInfo info = scanner.ScanAssemblyImpl(file);
						if(info != null)
							result.Add(info);
					}
			return result.ToArray();
		}

		public static AssemblyTypesInfo ScanAssembly(String file)
		{
			var scanner = new NetCoreAssemblyScanner(Path.GetDirectoryName(file));
			return scanner.ScanAssemblyImpl(file);
		}
#else
		public static AssemblyTypesInfo[] ScanFolder(String path) => throw new NotSupportedException();
		public static AssemblyTypesInfo ScanAssembly(String file) => throw new NotSupportedException();
		public void Dispose() {}
#endif
	}
}