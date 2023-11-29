- [ ] audit and implement all stock parts
- [ ] audit and fix mod support
- [ ] implement waterfall support
- [ ] implement whole-vessel (or subtree) scaling
- [ ] investigate part cost scaling on HECS2
		seems like this is being treated as "science" which becomes cheaper when it's bigger
		all of the probe cores seem to do this, which makes some sense, though the HECS2 also has a lot of battery space
- [x] add 1.875m scaling option for fuel tanks etc
- [ ] make sure save/load works
- [ ] make sure we can load crafts saved with TS/L
- [ ] make sure subassemblies/merging works
  [ ] fix TestFlightCore error:
  [ERR 14:30:27.365] ADDON BINDER: Cannot resolve assembly: TestFlightCore, Culture=neutral, PublicKeyToken=null
    UnityEngine.DebugLogHandler:LogFormat(LogType, Object, String, Object[])
    ModuleManager.UnityLogHandle.InterceptLogHandler:LogFormat(LogType, Object, String, Object[])
    UnityEngine.Debug:LogErrorFormat(String, Object[])
    AssemblyLoader:MyResolveEventHandler(Object, ResolveEventArgs) (at C:/Users/Jon/source/repos/ksp-assembly-csharp/AssemblyLoader.cs:833)
    System.Reflection.Assembly:Load(AssemblyName)
    AssemblyLoader:MyResolveEventHandler(Object, ResolveEventArgs) (at C:/Users/Jon/source/repos/ksp-assembly-csharp/AssemblyLoader.cs:820)
    System.Type:GetType(String, Boolean)
    TweakScale.TweakScale:SetupPrefab() (at C:/Users/Jon/source/repos/TweakScale/Source/Scale.cs:135)
    TweakScale.TweakScale:OnLoad(ConfigNode) (at C:/Users/Jon/source/repos/TweakScale/Source/Scale.cs:242)
    PartModule:Load(ConfigNode) (at C:/Users/Jon/source/repos/ksp-assembly-csharp/PartModule.cs:832)
    Part:AddModule(ConfigNode, Boolean) (at C:/Users/Jon/source/repos/ksp-assembly-csharp/Part.cs:5839)
    PartLoader:ParsePart(UrlConfig, ConfigNode) (at C:/Users/Jon/source/repos/ksp-assembly-csharp/PartLoader.cs:922)
    <CompileParts>d__63:MoveNext() (at C:/Users/Jon/source/repos/ksp-assembly-csharp/PartLoader.cs:577)
    UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr)

- [ ] Make it possible to change between free scale to stack scale (there's a lot of stuff set to free that should be stack)
- [x] handle part inventories
- [ ] handle stock exhaust particles
- [ ] check stock twin boar