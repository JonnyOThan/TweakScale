﻿using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access destination type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("99eed0a4-ba83-42d9-80bd-0591cf3840be")]

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
[assembly: KSPAssemblyDependency("HarmonyKSP", 1, 0)]
[assembly: KSPAssemblyDependency("ModuleManager", 2, 5)]

namespace TweakScale
{
	static partial class VersionInfo
	{
		public const string ASSEMBLY = "Scale";
	}
}