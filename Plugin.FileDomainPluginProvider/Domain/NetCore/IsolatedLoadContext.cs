// net5+ only implementation. For net35 this file is empty.
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
		private readonly String _baseDirectory;

		public IsolatedLoadContext(String baseDirectory, Boolean isCollectible)
			: base($"PluginContext_{Guid.NewGuid():N}", isCollectible)
		{
			_baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			String candidatePath = Path.Combine(_baseDirectory, assemblyName.Name + ".dll");
			if(File.Exists(candidatePath))
			{
				try { return LoadFromAssemblyPath(candidatePath); }
				catch(BadImageFormatException) { return null; }
			}
			return null; // delegate to default context
		}
	}
}
#endif