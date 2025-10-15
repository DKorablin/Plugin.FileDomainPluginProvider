using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
#if NET5_0_OR_GREATER
using Plugin.FileDomainPluginProvider.Domain.NetCore;
#endif
using Plugin.FilePluginProvider;
using SAL.Flatbed;

namespace Plugin.FileDomainPluginProvider
{
	/// <summary>
	/// Alternative loader logic for net5+ using AssemblyLoadContext.
	/// </summary>
	internal sealed class NetCorePluginLoader
	{
#if NET5_0_OR_GREATER
		private readonly TraceSource _trace;
		private readonly IHost _host;
		private readonly FilePluginArgs _args;

		public NetCorePluginLoader(TraceSource trace, IHost host, FilePluginArgs args)
		{
			_trace = trace; _host = host; _args = args;
		}

		public void LoadAll()
		{
			foreach(String pluginPath in _args.PluginPath)
				if(Directory.Exists(pluginPath))
				{
					AssemblyTypesInfo[] infos = NetCoreAssemblyScanner.ScanFolder(pluginPath);
					foreach(var info in infos)
						LoadAssembly(info, ConnectMode.Startup);
				}
		}

		public void LoadChanged(String path)
		{
			var info = NetCoreAssemblyScanner.ScanAssembly(path);
			if(info != null)
				this.LoadAssembly(info, ConnectMode.AfterStartup);
		}

		private void LoadAssembly(AssemblyTypesInfo info, ConnectMode mode)
		{
			if(info.Error != null)
			{
				_trace.TraceEvent(TraceEventType.Error, 1, "Path: {0} Error: {1}", info.AssemblyPath, info.Error);
				return;
			}
			try
			{
				if(info.Types.Length == 0)
					throw new InvalidOperationException("Types is empty");

				foreach(IPluginDescription plugin in _host.Plugins)
					if(info.AssemblyPath.Equals(plugin.Source, StringComparison.InvariantCultureIgnoreCase))
						return;

				Assembly assembly = Assembly.LoadFrom(info.AssemblyPath); // load into default context for activation
				foreach(string type in info.Types)
					_host.Plugins.LoadPlugin(assembly, type, info.AssemblyPath, mode);
			}
			catch(Exception exc)
			{
				exc.Data.Add("Library", info.AssemblyPath);
				_trace.TraceData(TraceEventType.Error, 1, exc);
			}
		}
#endif
	}
}