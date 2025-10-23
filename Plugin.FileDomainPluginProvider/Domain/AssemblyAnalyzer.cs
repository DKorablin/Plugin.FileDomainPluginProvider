using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Plugin.FilePluginProvider;

namespace Plugin.FileDomainPluginProvider.Domain
{
	[Serializable]
	internal class AssemblyAnalyzer : AssemblyAnalyzerBase
	{
		public AssemblyTypesInfo[] CheckAssemblies(String path)
		{
			base.AttachResolveEvents(path);

			List<AssemblyTypesInfo> assemblies = new List<AssemblyTypesInfo>();
			try
			{
				List<ManualResetEvent> onDone = new List<ManualResetEvent>();
				List<AssemblyTypesReader> readers = new List<AssemblyTypesReader>();
				foreach(String filePath in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
					if(FilePluginArgs.CheckFileExtension(filePath))
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
				if(FilePluginArgs.CheckFileExtension(assemblyPath))
					result = AssemblyTypesReader.GetAssemblyTypes(assemblyPath);
			} finally
			{
				base.DetachResolveEvents();
			}

			return result;
		}
	}
}