# Changelog

### Known Issues

* Scaling parts after re-rooting across a surface attachment does not move them correctly

## Unreleased

* Add support for propellers from KerbalPowersNaval

## 3.3.1 - 2025-10-14

### Prevented Scaling Of Several Problematic Parts

* Some additional procedural parts from RO
* Stock fairing since they don't work correctly
* RealChutes

### Other Changes

* Fixed mass scale for RCS parts to maintain TWR
* Make sure drills are never treated as science so that they can be scaled up
* Fix NRE when a particle emitter transform is missing
* Fix exception when TestFlight is installed
* Gracefully handle harmony patching failure (usually caused by out of date mods)
* Support changing waterfall plumes with B9PartSwitch
* Wheel suspension spring strength exponent set to 2 in order to get consistent relative displacement at different scales
* Added bulkhead size heuristics up to size6 (7.5m)
* Better support for SystemHeat (thanks @arbsoup)
* Fix scaling for variable power engines from Near Future Propulsion
* Optimized application of scaling to PartModules; removed duplicate application of certain scale factors in derived types
* Specialized scale handlers can now opt to run before the exponent-based scaling
* Add support for scaling Near Future Exploration antenna reflectors
* Better support for scaling modules that are altered with B9PartSwitch
* Fixed scaling of FAR wings
* Fix incorrectly scaling control surface fraction for stock control surfaces


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