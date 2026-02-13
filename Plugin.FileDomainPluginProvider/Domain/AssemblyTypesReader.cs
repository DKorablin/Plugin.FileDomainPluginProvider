using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SAL.Flatbed;

namespace Plugin.FileDomainPluginProvider.Domain
{
	internal class AssemblyTypesReader
	{
		private String[] FilePath { get; }

		private ManualResetEvent OnDone { get; set; }

		public AssemblyTypesInfo[] Info { get; private set; }

		public AssemblyTypesReader(String[] filePath, ManualResetEvent onDone)
		{
			this.FilePath = filePath;
			this.Info = new AssemblyTypesInfo[this.FilePath.Length];
			this.OnDone = onDone;
		}

		public void Read(Object threadContext)
		{
			for(Int32 loop = 0; loop < this.FilePath.Length; loop++)
				this.Info[loop] = GetAssemblyTypes(this.FilePath[loop]);
			this.OnDone.Set();
		}

		public static AssemblyTypesInfo GetAssemblyTypes(String filePath)
		{
			try
			{
				Assembly assembly = Assembly.LoadFrom(filePath);

				if(!String.Equals(assembly.Location, filePath, StringComparison.OrdinalIgnoreCase))
					return new AssemblyTypesInfo(filePath, $"Assembly \"{assembly.FullName}\" will be skipped because it’s already loaded from \"{assembly.Location}\".", assembly.Location);

				List<String> types = new List<String>();
				foreach(Type assemblyType in assembly.GetTypes())
					if(PluginUtils.IsPluginType(assemblyType))
						types.Add(assemblyType.FullName);

				if(types.Count > 0)
					return new AssemblyTypesInfo(filePath, types.ToArray());
			} catch(BadImageFormatException)
			{
				// Unsupported binary format (not .NET assembly)
			} catch(FileLoadException exc)
			{
				Int32 hResult = Marshal.GetHRForException(exc);
				switch((UInt32)hResult)
				{
				case 0x80131515://loadFromRemoteSources
					Exception exc1 = exc.InnerException ?? exc;
					return new AssemblyTypesInfo(filePath, exc1.Message);
				}
			} catch(ReflectionTypeLoadException exc)
			{
				String errors = exc.LoaderExceptions?.Length > 0
					? String.Join(Environment.NewLine, new HashSet<String>(Array.ConvertAll(exc.LoaderExceptions, e => e.Message)).ToArray())
					: exc.Message;
				return new AssemblyTypesInfo(filePath, errors);
			} catch(Exception exc)
			{
				Exception exc1 = exc.InnerException ?? exc;
				return new AssemblyTypesInfo(filePath, exc1.Message);
			}

			return null;
		}
	}
}