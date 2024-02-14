using System.Reflection;

[assembly: AssemblyProduct(TweakScale.VersionInfo.ASSEMBLY)]
[assembly: AssemblyTitle(TweakScale.VersionInfo.ASSEMBLY)]
[assembly: AssemblyCopyright("Copyright © 2023")]

[assembly: AssemblyVersion(TweakScale.VersionInfo.STRING)]
[assembly: AssemblyFileVersion(TweakScale.VersionInfo.STRING)]
[assembly: KSPAssembly(TweakScale.VersionInfo.ASSEMBLY, TweakScale.VersionInfo.MAJOR, TweakScale.VersionInfo.MINOR, TweakScale.VersionInfo.REVISION)]

namespace TweakScale
{
	static partial class VersionInfo
	{
		public const int MAJOR = 3;
		public const int MINOR = 2;
		public const int REVISION = 2;
		public const string STRING = "3.2.2";
	}
}
