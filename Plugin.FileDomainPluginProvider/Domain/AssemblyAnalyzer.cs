using System;
using System.Linq;
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
				String[] files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Where(FilePluginArgs.CheckFileExtension).ToArray();
				if(files.Length == 0)
					return assemblies.ToArray();

				// Process in batches to avoid ThreadPool starvation
				Int32 batchSize = Math.Max(1, files.Length / Environment.ProcessorCount);
				List<ManualResetEvent> onDone = new List<ManualResetEvent>();
				List<AssemblyTypesReader> readers = new List<AssemblyTypesReader>();

				for(Int32 i = 0; i < files.Length; i += batchSize)
				{
					Int32 count = Math.Min(batchSize, files.Length - i);
					String[] batch = new String[count];
					Array.Copy(files, i, batch, 0, count);

					ManualResetEvent evt = new ManualResetEvent(false);
					AssemblyTypesReader reader = new AssemblyTypesReader(batch, evt);
					onDone.Add(evt);
					readers.Add(reader);

					ThreadPool.QueueUserWorkItem(reader.Read, this);
				}

				foreach(ManualResetEvent evt in onDone)
					evt.WaitOne();

				foreach(AssemblyTypesReader reader in readers)
					foreach(AssemblyTypesInfo info in reader.Info)
						if(info != null)
							assemblies.Add(info);
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