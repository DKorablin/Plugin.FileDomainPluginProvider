using System;
using System.IO;
using System.Reflection;

namespace Plugin.FileDomainPluginProvider.Domain
{
	[Serializable]
	internal class AssemblyAnalyzerBase : MarshalByRefObject
	{
		private ResolveEventHandler _reflectionOnlyResolve;
		private ResolveEventHandler _resolve;

		protected void AttachResolveEvents(String assemblyPath)
		{
			DirectoryInfo directory = new DirectoryInfo(assemblyPath);

			this._reflectionOnlyResolve = delegate(Object s, ResolveEventArgs e) { return OnReflectionOnlyResolve(e, directory); };
			this._resolve = delegate(Object s, ResolveEventArgs e) { return OnResolve(e, directory); };

			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += this._reflectionOnlyResolve;
			AppDomain.CurrentDomain.AssemblyResolve += this._resolve;//TODO: Попытка загрузить нехватающие сборки
		}

		protected void DetachResolveEvents()
		{
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= this._reflectionOnlyResolve;
			AppDomain.CurrentDomain.AssemblyResolve -= this._resolve;//TODO: Попытка загрузить нехватающие сборки
		}

		/// <summary>Attempts ReflectionOnlyLoad of current Assemblies dependants</summary>
		/// <param name="args">ReflectionOnlyAssemblyResolve event args</param>
		/// <param name="directory">The current Assemblies Directory</param>
		/// <returns>ReflectionOnlyLoadFrom loaded dependant Assembly</returns>
		private Assembly OnReflectionOnlyResolve(ResolveEventArgs args, DirectoryInfo directory)
		{

			Assembly loadedAssembly = Array.Find(AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies(), delegate(Assembly asm) { return String.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase); });

			if(loadedAssembly != null)
				return loadedAssembly;

			AssemblyName assemblyName = new AssemblyName(args.Name);
			String dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + Constant.LibraryExtension);

			return File.Exists(dependentAssemblyFilename)
				? Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename)
				: Assembly.ReflectionOnlyLoad(args.Name);
		}

		private Assembly OnResolve(ResolveEventArgs args, DirectoryInfo directory)
		{
			Assembly loadedAssembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), delegate(Assembly asm) { return String.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase); });

			if(loadedAssembly != null)
				return loadedAssembly;

			AssemblyName assemblyName = new AssemblyName(args.Name);
			String dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + Constant.LibraryExtension);

			return File.Exists(dependentAssemblyFilename)
				? Assembly.LoadFrom(dependentAssemblyFilename)
				: null;//return Assembly.Load(args.Name); - StackOverflowException
		}
	}
}