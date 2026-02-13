using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Plugin.FilePluginProvider;

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

			this._reflectionOnlyResolve = (s, e) => OnReflectionOnlyResolve(e, directory);
			this._resolve = (s, e) => OnResolve(e, directory);

			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += this._reflectionOnlyResolve;
			AppDomain.CurrentDomain.AssemblyResolve += this._resolve;
		}

		protected void DetachResolveEvents()
		{
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= this._reflectionOnlyResolve;
			AppDomain.CurrentDomain.AssemblyResolve -= this._resolve;
		}

		/// <summary>Attempts ReflectionOnlyLoad of current Assemblies dependents</summary>
		/// <param name="args">ReflectionOnlyAssemblyResolve event args</param>
		/// <param name="directory">The current Assemblies Directory</param>
		/// <returns>ReflectionOnlyLoadFrom loaded dependent Assembly</returns>
		private static Assembly OnReflectionOnlyResolve(ResolveEventArgs args, DirectoryInfo directory)
		{

			Assembly loadedAssembly = Array.Find(AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies(), (asm) => String.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase));

			if(loadedAssembly != null)
				return loadedAssembly;

			String assemblyPath = SearchForAssembly(args, directory);
			if(assemblyPath != null)
				return Assembly.ReflectionOnlyLoadFrom(assemblyPath);

			return Assembly.ReflectionOnlyLoad(args.Name);
		}

		private static Assembly OnResolve(ResolveEventArgs args, DirectoryInfo directory)
		{
			Assembly loadedAssembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), (asm) => String.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase));

			if(loadedAssembly != null)
				return loadedAssembly;

			String assemblyPath = SearchForAssembly(args, directory);
			if(assemblyPath != null)
				return Assembly.LoadFrom(assemblyPath);

			return null;//return Assembly.Load(args.Name); - StackOverflowException
		}

		private static String SearchForAssembly(ResolveEventArgs args, DirectoryInfo directory)
		{
			AssemblyName assemblyName = new AssemblyName(args.Name);
			foreach(String extension in FilePluginArgs.LibraryExtensions)
			{
				String dependentAssemblyFilePath = Path.Combine(directory.FullName, assemblyName.Name + extension);
				if(File.Exists(dependentAssemblyFilePath))
					return dependentAssemblyFilePath;
			}

			//It's possible that dependent assembly is located in subfolder of plugin directory. Need to check it as well.
			foreach(String foundAssemblyPath in Directory.EnumerateFiles(directory.FullName, assemblyName.Name + ".*", SearchOption.AllDirectories).Where(FilePluginArgs.CheckFileExtension))
				return foundAssemblyPath;

			return null;
		}
	}
}