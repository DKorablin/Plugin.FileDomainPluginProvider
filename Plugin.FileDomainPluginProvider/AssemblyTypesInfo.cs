using System;
using System.Diagnostics;

namespace Plugin.FileDomainPluginProvider
{
	[Serializable]
	[DebuggerDisplay("AssemblyPath = {" + nameof(AssemblyPath) + "}, Types = {" + nameof(Types) + ".Length}, Error = {" + nameof(Error) + "}")]
	internal class AssemblyTypesInfo
	{
		public String AssemblyPath { get; }

		public String Error { get; }

		public String[] Types { get; }

		private AssemblyTypesInfo(String assemblyPath)
			=> this.AssemblyPath = assemblyPath;

		public AssemblyTypesInfo(String assemblyPath, String[] types)
			: this(assemblyPath)
			=> this.Types = types;

		public AssemblyTypesInfo(String assemblyPath, String error)
			: this(assemblyPath)
			=> this.Error = error;//We can't use Trace from different app domain directly
	}
}