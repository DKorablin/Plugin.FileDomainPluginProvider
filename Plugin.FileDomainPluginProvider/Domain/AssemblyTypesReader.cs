using System;
using System.Collections.Generic;
using System.IO;
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
			this.Info = new AssemblyTypesInfo[FilePath.Length];
			this.OnDone = onDone;
		}

		public void Read(Object threadContext)
		{
			for(Int32 loop = 0; loop < this.FilePath.Length; loop++)
				this.Info[loop] = GetAssemblyTypes(FilePath[loop]);
			this.OnDone.Set();
		}

		public static AssemblyTypesInfo GetAssemblyTypes(String filePath)
		{
			List<String> types = new List<String>();
			try
			{
				Assembly assembly = Assembly.LoadFrom(filePath);
				//Assembly assembly = context.LoadFromAssemblyPath(filePath);

				foreach(Type assemblyType in assembly.GetTypes())
					if(PluginUtils.IsPluginType(assemblyType))
						types.Add(assemblyType.FullName);
			} catch(BadImageFormatException)
			{
				return null;
			} catch(FileLoadException exc)
			{
				Int32 hResult = Marshal.GetHRForException(exc);
				switch((UInt32)hResult)
				{
				case 0x80131515://loadFromRemoteSources
					Exception exc1 = exc.InnerException ?? exc;
					return new AssemblyTypesInfo(filePath, exc1.Message);
				}
				return null;
			} catch(ReflectionTypeLoadException exc)
			{
				String errors = exc.LoaderExceptions != null && exc.LoaderExceptions.Length > 0
					? String.Join(Environment.NewLine, Array.ConvertAll(exc.LoaderExceptions, (Exception e) => { return e.Message; }))
					: exc.Message;
				return new AssemblyTypesInfo(filePath, errors);
			} catch(Exception exc)
			{
				Exception exc1 = exc.InnerException ?? exc;
				return new AssemblyTypesInfo(filePath, exc1.Message);
			}

			return types.Count > 0
				? new AssemblyTypesInfo(filePath, types.ToArray())
				: null;
		}
	}
}
