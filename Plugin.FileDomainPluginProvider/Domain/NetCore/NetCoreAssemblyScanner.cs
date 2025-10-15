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
	/// <summary>
	/// net5+ implementation that scans assemblies for plugin types using AssemblyLoadContext instead of AppDomain.
	/// </summary>
	internal static class NetCoreAssemblyScanner
	{
#if NET5_0_OR_GREATER
		public static AssemblyTypesInfo[] ScanFolder(String path)
		{
			List<AssemblyTypesInfo> result = new List<AssemblyTypesInfo>();
			if(!Directory.Exists(path)) return result.ToArray();

			foreach(String file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
				if(FilePluginProvider.FilePluginArgs.CheckFileExtension(file))
				{
					AssemblyTypesInfo info = ScanAssembly(file);
					if(info != null)
						result.Add(info);
				}
			return result.ToArray();
		}

		public static AssemblyTypesInfo ScanAssembly(String file)
		{
			try
			{
				var alc = new IsolatedLoadContext(Path.GetDirectoryName(file), isCollectible: true);
				Assembly assembly = alc.LoadFromAssemblyPath(Path.GetFullPath(file));
				List<String> types = new List<String>();
				foreach(Type t in assembly.GetTypes())
					if(PluginUtils.IsPluginType(t))
						types.Add(t.FullName);
				return types.Count > 0 ? new AssemblyTypesInfo(file, types.ToArray()) : null;
			} catch(BadImageFormatException) { return null; }
			catch(ReflectionTypeLoadException exc)
			{
				String errors = exc.LoaderExceptions != null && exc.LoaderExceptions.Length > 0
					? String.Join(Environment.NewLine, new HashSet<String>(Array.ConvertAll(exc.LoaderExceptions, e => e.Message)).ToArray())
					: exc.Message;
				return new AssemblyTypesInfo(file, errors);
			}
			catch(Exception exc) { return new AssemblyTypesInfo(file, (exc.InnerException ?? exc).Message); }
		}
#else
		public static AssemblyTypesInfo[] ScanFolder(String path) => throw new NotSupportedException();
		public static AssemblyTypesInfo ScanAssembly(String file) => throw new NotSupportedException();
#endif
	}
}
