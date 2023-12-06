// TODO: audit ckan's suggest and support list for stuff that might be missing here

Need to figure out how existing mods are using tweakscale - what passes are their patches in?

# Early patches

## placed directly in part

https://github.com/KerbalFoundries/KerbalFoundries/blob/962f0e54799cc55211108cddb27539029b450fa1/Parts/Screw.cfg#L139
https://github.com/confusingbits/KSPInterstellar/blob/25aaf0a16e2f3c719b41a2a17ead219139ed12d1/GameData/Interstellar/Parts/Utility/InlineRefinery/InlineRefinery.cfg#L135
https://github.com/SernisD/KSP-Interstellar-Extended/blob/c719959811d3d8ed0882c3fcd04fa891a4d83438/GameData/InterstellarFuelSwitch/Parts/TankRevamp/EC2503.cfg#L87
https://github.com/Felbourn/Project-Alexandria/blob/20ae486d5bdd14ef8ed6d0006e16e139808d68ec/GameData/Felbourn/X-15/engine.cfg#L128
https://github.com/BT-Industries/Maritime-Pack-2.0/blob/45ac96bba7f20763dbef23c204c5ed8b64c9beda/GameData/MaritimePack/Parts/Accessories/Bitts/part.cfg#L31
https://github.com/Drag0nD3str0yer/Moderately-Plane-Related/blob/eeca090f5bb92f95e3d4ff41e84ffbcb82e5dda4/GameData/ModeratelyPlaneRelated/Parts/Aero/HighPerformanceCtrl/TCA_HP_Control02.cfg#L140
https://github.com/Felbourn/Project-Genesis/blob/cac039ebca33a7676f376651f88f5c75bc326f07/GameData/AerojetKerbodyne/Parts/OrionThrusters/AJ-10.cfg#L173
https://github.com/Olympic1/L-Tech/blob/66426bd234da9163b9a525af6493e1f0c767cf96/GameData/LTech/Parts/Utility/Tanks/Tank_Snacks.cfg#L46
https://github.com/zer0Kerbal/GoodspeedAerospace/blob/6b1ff85b1ccf744aec58523c218faa6a58f9dd04/GameData/GoodspeedAerospace/Parts/Structural/hexframe1.cfg#L29
https://github.com/MagicSmokeIndustries/InfernalRobotics/blob/68a0d6e7454dced6b680cc71d3aa3bc3613cc087/Resources/IR-LegacyParts/GameData/MagicSmokeIndustries/Parts/Legacy/dockingwasher_free/part.cfg#L45
https://github.com/zer0Kerbal/BurgerMod/blob/361ec4fdf39f6aba084f0c53078d85cdebd8cdf5/partTomato.cfg#L64
https://github.com/KerboNerd/KNI-HP-CYREX/blob/f6c43db3716ecf5d5dfc25562c49fd81efd0a404/GameData/KerboNerdLaboratories/Parts/Structural/scifiAdapter375/adapteeer.cfg#L33
https://github.com/Felbourn/RSS25/blob/da7ed27573ce91943de3dbcff20dc7e381e7d61d/GameData/History/Restone/Merc_LFE_Redstone.cfg#L45
https://github.com/tylerraiz/EDBMods/blob/1f53b3159e00b2a1f4567880136a6403d4fbca72/GameData/EDBMods/Parts/LauncherOne/LOUpper.cfg#L75
https://github.com/zer0Kerbal/CargoBaysOld/blob/d3eb1d7536af3c7d68fa5e76b0c6e37cf4db1779/GameData/DaMichel/CargoBays/Parts/375/dm-round-cargobay375-4.cfg#L112
https://github.com/zer0Kerbal/SphericalTanksOld/blob/6651f48d7e111d6f41cb858a9967a0b3d0476353/GameData/DaMichel/SphericalTanks/Parts/c1875-r0825-SphericalTankR.cfg#L48
https://github.com/gomker/HullBreach/blob/ef530b50fef4b15fbaffd83673e0b443e05e7c52/Distribution/GameData/HullBreach/Parts/LargeSeaWaterPump/LargeSeaWaterPump.cfg#L101
https://github.com/JadeOfMaar/Measurizer/blob/e6cac5bc250f780647869f0b80ec3e33ce740f5d/GameData/Measurizer/MultiA.cfg#L116

## legacy pass

https://github.com/linuxgurugamer/LLL-Continued/blob/b3cab5906e63ce2b08fd14cca90f787cde83c5e5/GameData/LLL/Patches/LLL_Scale.cfg#L464
https://github.com/Felbourn/Project-Alexandria/blob/20ae486d5bdd14ef8ed6d0006e16e139808d68ec/GameData/Felbourn/Vostok/ant.cfg#L34
https://github.com/WolfairCorp/AoA-Technologies/blob/3351b3bee8a9eb4ceca164fdbb63f329829ade2b/GameData/AoATech/Patches/AoA_FAR_config.cfg#L11
https://github.com/maanderson22/CivilianPopulationRevamp/blob/28395a73240d7fe39d9eda3d05085eec936be93e/Gamedata/CivilianPopulationRevamp/Configs/tweakscale.cfg#L95
https://github.com/pjwerneck/ksp_tweaks/blob/b42bb1a97dfabdfb61cb6dab635319670116921d/TweakScale/BahaSP.cfg#L6

# FOR passes

RealismOverhaul
	https://github.com/KSP-RO/RealismOverhaul/blob/32ab62ccbde3600b6c22c5bd78d1161ef1f5c08e/GameData/RealismOverhaul/REWORK/RO_NovaPunch_Misc.cfg#L24
	FOR[RealismOverhaul]

RetroFuture
	https://github.com/linuxgurugamer/RetroFuture/blob/a7ddee967e19e4195bc3198a24da52baf7bc1085/RetroFutureFromEpicCypertronian/Patches/RF_TweakScale.cfg#L5
	FOR[RetroFuture]

NodeDazzler
	https://github.com/zer0Kerbal/NodeDazzler/blob/0485e671e55e3998e96044e9cb8340a1bee2d8d9/GameData/_SGEx/Interkosmos/TweakScale.cfg#L8
	FOR[TweakScale] (wtf)

Genesis
	https://github.com/Felbourn/Project-Genesis/blob/cac039ebca33a7676f376651f88f5c75bc326f07/GameData/MEPS/TweakScale.cfg#L8
	FOR[MEPS]

JackOLantern
	https://github.com/zer0Kerbal/JackOLantern/blob/1856ae2595ee4d79d822071a80d50a821635506b/GameData/JackOLantern/Compatibility/TweakScale.cfg#L14
	FOR[JackOLantern]

DMagicOrbitalScience
	https://github.com/mpsenn/ksp/blob/86ac0b5778c25b7f74e162fcb46bd83e979841f4/GameData/DMagicOrbitalScience/Resources/DMagicTweakScale.cfg#L80
	FOR[DMagic]

SpaceX-RO-Falcons
	https://github.com/pmborg/SpaceX-RO-Falcons/blob/7089d83789d7401a9bf12016614b096fd507c047/GameData/Pmborg-RealFalcons/Pmborg_Real_Engines.cfg#L16
	https://github.com/pmborg/SpaceX-RO-Falcons/blob/7089d83789d7401a9bf12016614b096fd507c047/GameData/Pmborg-RealFalcons/Pmborg_Real_Engines.cfg#L199
	So this one is fun...
	these patches are marked :AFTER[RealPlume]:FINAL which is a bug.  you can't have more than one pass specifier.  The first one will be used and :FINAL is ignored.
	These two patches REMOVE the TweakScale module and don't replace it.
	The first patch *renames* an existing part (KK_SPX_Merlin1Ci -> PMB_Falcon1Merlin1A)
	The second patch copies that part and renames to PMB_Falcon1Merlin1C, (and also tries to remove the tweakscale module..but it was already removed...what?)
	So to handle this correctly, I think we need to mark PMB_Falcon1Merlin1A and PMB_Falcon1Merlin1C as tweakscale_ignore but it has to be some point after AFTER[RealPlume]



# Late passes

https://github.com/pmborg/SpaceX-RO-Falcons/blob/7089d83789d7401a9bf12016614b096fd507c047/GameData/Pmborg-RealFalcons/Grid_Fin.cfg#L10
	FINAL (but edits in-place)

https://github.com/Felbourn/Odyssey/blob/b0bd5fe00c4800b3e91d583fc90a2f77f3390732/GameData/Odyssey/Parts/Stations/structural.cfg#L137
https://github.com/adamlsd/Odyssey/blob/66a048284d20c3827083f4740b1a32049fab5d53/GameData/Odyssey/Parts/Comm/communications.cfg#L78
https://github.com/doktorjet/ChopShop/blob/94d15442595494a85b5500c88f75c2b78a33359a/GameData/ChopShop/ModuleManager/TweakScale.cfg#L21
	FINAL (tries to add)