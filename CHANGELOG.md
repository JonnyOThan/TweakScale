# Changelog

### Known Issues

* Scaling parts after re-rooting across a surface attachment does not move them correctly
* Scaling parts that alter their module data with B9PartSwitch (e.g. SimpleAdjustableFairings, HeatControl) does not work

## 3.2.4 - 2025-02-05

* Fix attachnode positions on parts where the B9PS subtype has a different position from the prefab
* Fix solar panels not scaling power output
* Gracefully handle other mods throwing exceptions from PluginConfiguration.CreateForType
* Fix default scale on Planetside PD 0625 docking port
* Fix ISRU and other ModuleResourceConverter not scaling inputs and outputs correctly
* SSPX cupolas (greenhouse, telescope) are no longer treated as science parts so that they can be scaled up
* Fix ModuleGeneratorExtended scaling behavior (used by KNES and some other mods)
* Fix FNEmitterController module scaling from KSPIE
* Gracefully handle certain bad scaling configs
* Fix engine particle scaling for certain cases (e.g. Restock Twin Boar)


## 3.2.3

* Fixed a bug where the part action window would become unusable after right-clicking certain parts
* Fixed "match node size" for Making History KV pod top nodes
* Fixed "match node size" for Making History Kodiak engine


## 3.2.2

* Moved .version file for ScaleRedist library
* Fixed NullReferenceException when trying to scale a part after re-rooting a surface attachment
* Fixed NearFutureLaunchVehicles node sizes