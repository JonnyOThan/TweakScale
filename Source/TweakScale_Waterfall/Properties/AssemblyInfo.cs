﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("TweakScale_Waterfall")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("TweakScale_Waterfall")]
[assembly: AssemblyCopyright("Copyright ©  2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b3f3175a-c048-4fb7-ac5e-44d5935d3348")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: KSPAssemblyDependencyEqualMajor("Scale_Redist", TweakScale.VersionInfo.MAJOR, TweakScale.VersionInfo.MINOR, TweakScale.VersionInfo.REVISION)]
[assembly: KSPAssemblyDependency("Waterfall", 0, 0)]
[assembly: KSPAssemblyDependency("HarmonyKSP", 1, 0)]

namespace TweakScale
{
	static partial class VersionInfo
	{
		public const string ASSEMBLY = "TweakScale_Waterfall";
	}
}