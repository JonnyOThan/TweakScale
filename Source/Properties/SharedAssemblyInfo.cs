using System.Reflection;

[assembly: AssemblyVersion(TweakScale.VersionInfo.STRING)]
[assembly: AssemblyFileVersion(TweakScale.VersionInfo.STRING)]
[assembly: KSPAssembly(TweakScale.VersionInfo.ASSEMBLY, TweakScale.VersionInfo.MAJOR, TweakScale.VersionInfo.MINOR, TweakScale.VersionInfo.REVISION)]

namespace TweakScale
{
	static partial class VersionInfo
	{
		public const int MAJOR = 3;
		public const int MINOR = 0;
		public const int REVISION = 5;
		public const string STRING = "3.0.5";
	}
}
