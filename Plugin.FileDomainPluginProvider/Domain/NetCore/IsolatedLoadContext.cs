#if NET5_0_OR_GREATER
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Plugin.FileDomainPluginProvider.Domain.NetCore
{
	/// <summary>Custom AssemblyLoadContext that isolates plugin assemblies for .NET 5+ targets.</summary>
	internal sealed class IsolatedLoadContext : AssemblyLoadContext
	{
		private static Boolean DependencyContextFailed = false;

		private readonly String _baseDirectory;
		private String _pluginPath;
		private AssemblyDependencyResolver _resolver;
		private NuGetResolver _extendedResolver;

		private NuGetResolver ExtendedResolver
		{
			get => this._extendedResolver ??= new NuGetResolver(this._pluginPath);
		}

		public IsolatedLoadContext(String baseDirectory, Boolean isCollectible)
			: base($"Plugin.FileDomainPluginProvider.ProbingContext.{Guid.NewGuid():N}", isCollectible)
		{
			_baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
		}

		public Assembly LoadFromPluginPath2(String pluginPath)
		{
			this._pluginPath = pluginPath ?? throw new ArgumentNullException(nameof(pluginPath));
			this._extendedResolver = null;

			this._resolver = new AssemblyDependencyResolver(pluginPath);
			return this.LoadFromAssemblyPath(pluginPath);
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			String candidatePath = _resolver.ResolveAssemblyToPath(assemblyName);
			if(candidatePath != null)
				return TryToLoadAssembly(candidatePath);

			if(!DependencyContextFailed)
				try
				{
					candidatePath = this.ExtendedResolver?.ResolveAssemblyToPath(assemblyName);
				} catch(FileNotFoundException exc)
				{// ignore, referenced assemblies are not loaded from NuGet packages
					DependencyContextFailed = true;
				}

			if(candidatePath != null)
				return TryToLoadAssembly(candidatePath);

			return null; // delegate to default context

			Assembly TryToLoadAssembly(String path)
			{
				try
				{
					return LoadFromAssemblyPath(path);
				}
				catch(BadImageFormatException)
				{
					return null;
				}
			}
		}
	}
}
#endif