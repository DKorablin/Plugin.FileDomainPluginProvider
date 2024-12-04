using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Plugin.FileDomainPluginProvider.Domain
{
    [Serializable]
	internal class AssemblyAnalyzer : AssemblyAnalyzerBase
	{
		public AssemblyTypesInfo[] CheckAssemblies(String path)
		{
			base.AttachResolveEvents(path);
			//System.Diagnostics.Debugger.Launch();
			List<AssemblyTypesInfo> assemblies = new List<AssemblyTypesInfo>();
			try
			{
				List<ManualResetEvent> onDone = new List<ManualResetEvent>();
				List<AssemblyTypesReader> readers = new List<AssemblyTypesReader>();
				foreach(String filePath in Directory.GetFiles(path, Constant.LibrarySearchExtension, SearchOption.AllDirectories))
				{
					ManualResetEvent evt = new ManualResetEvent(false);
					AssemblyTypesReader reader = new AssemblyTypesReader(new String[] { filePath }, evt);
					onDone.Add(evt);
					readers.Add(reader);

					ThreadPool.QueueUserWorkItem(reader.Read, this);
				}

				foreach(ManualResetEvent evt in onDone)
					evt.WaitOne();

				foreach(AssemblyTypesReader reader in readers)
					foreach(AssemblyTypesInfo info in reader.Info)
					{
						//reader.OnDone.WaitOne();
						if(info != null)
							assemblies.Add(info);
					}
			} finally
			{
				base.DetachResolveEvents();
			}

			return assemblies.ToArray();
		}

		public AssemblyTypesInfo CheckAssembly(String assemblyPath)
		{
			base.AttachResolveEvents(Path.GetDirectoryName(assemblyPath));

			AssemblyTypesInfo result = null;
			try
			{
				if(Path.GetExtension(assemblyPath).Equals(Constant.LibraryExtension, StringComparison.OrdinalIgnoreCase))
					result = AssemblyTypesReader.GetAssemblyTypes(assemblyPath);
			} finally
			{
				base.DetachResolveEvents();
			}

			return result;
		}
	}
}