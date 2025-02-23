using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("9b495690-d55f-4aa0-9e44-93787550a2aa")]
[assembly: System.CLSCompliant(true)]

#if NETSTANDARD
[assembly: AssemblyMetadata("ProjectUrl", "https://dkorablin.ru/project/Default.aspx?File=106")]
#else

[assembly: AssemblyDescription("Plugin loader assemblty with sandbox check in new domain")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2016-2025")]
#endif