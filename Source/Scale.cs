using System;
using System.Linq;
using System.Reflection;
using TweakScale.Annotations;
using UnityEngine;
//using ModuleWheels;

namespace TweakScale
{
    public class TweakScale : PartModule, IPartCostModifier, IPartMassModifier
    {
        /// <summary>
        /// The selected scale. Different from currentScale only for destination single update, where currentScale is set to match this.
        /// </summary>
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Scale", guiFormat = "0.000", guiUnits = "m")]
        [UI_ScaleEdit(scene = UI_Scene.Editor)]
// ReSharper disable once InconsistentNaming
        public float tweakScale = -1;

        /// <summary>
        /// Index into scale values array.
        /// </summary>
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Scale")]
        [UI_ChooseOption(scene = UI_Scene.Editor)]
// ReSharper disable once InconsistentNaming
        public int tweakName = 0;

        /// <summary>
        /// The scale to which the part currently is scaled.
        /// </summary>
        [KSPField(isPersistant = true)]
// ReSharper disable once InconsistentNaming
        public float currentScale = -1;

        /// <summary>
        /// The default scale, i.e. the number by which to divide tweakScale and currentScale to get the relative size difference from when the part is used without TweakScale.
        /// </summary>
        [KSPField(isPersistant = true)]
// ReSharper disable once InconsistentNaming
        public float defaultScale = -1;

        /// <summary>
        /// Whether the part should be freely scalable or limited to destination list of allowed values.
        /// </summary>
        [KSPField(isPersistant = false)]
// ReSharper disable once InconsistentNaming
        public bool isFreeScale = false;

        /// <summary>
        /// The scale exponentValue array. If isFreeScale is false, the part may only be one of these scales.
        /// </summary>
        protected float[] ScaleFactors = { 0.625f, 1.25f, 2.5f, 3.75f, 5f };
        
        /// <summary>
        /// The node scale array. If node scales are defined the nodes will be resized to these values.
        ///</summary>
        protected int[] ScaleNodes = { };

        /// <summary>
        /// The unmodified prefab part. From this, default values are found.
        /// </summary>
        private Part _prefabPart;

        /// <summary>
        /// Cached scale vector, we need this because the game regularly reverts the scaling of the IVA overlay
        /// </summary>
        private Vector3 _savedIvaScale;

        /// <summary>
        /// The exponentValue by which the part is scaled by default. When destination part uses MODEL { scale = ... }, this will be different from (1,1,1).
        /// </summary>
        [KSPField(isPersistant = true)]
// ReSharper disable once InconsistentNaming
        public Vector3 defaultTransformScale = new Vector3(0f, 0f, 0f);

        private bool _firstUpdateWithParent = true;
        private bool _setupRun;
        private bool _firstUpdate = true;
        public bool ignoreResourcesForCost = false;
        public bool scaleMass = true;

        /// <summary>
        /// Updaters for different PartModules.
        /// </summary>
        private IRescalable[] _updaters = new IRescalable[0];

        /// <summary>
        /// Cost of unscaled, empty part.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float DryCost;

        /// <summary>
        /// scaled mass
        /// </summary>
        [KSPField(isPersistant = false)]
        public float MassScale = 1;

        private Hotkeyable _chainingEnabled;

        /// <summary>
        /// The ScaleType for this part.
        /// </summary>
        public ScaleType ScaleType { get; private set; }

        public bool IsRescaled
        {
            get
            {
                return (Math.Abs(currentScale / defaultScale - 1f) > 1e-5f);
            }
        }

        /// <summary>
        /// The current scaling factor.
        /// </summary>
        public ScalingFactor ScalingFactor
        {
            get
            {
                return new ScalingFactor(tweakScale / defaultScale, tweakScale / currentScale, isFreeScale ? -1 : tweakName);
            }
        }


        protected virtual void SetupPrefab()
        {
            var PartNode = GameDatabase.Instance.GetConfigs("PART").FirstOrDefault(c => c.name.Replace('_', '.') == part.name).config;
            var ModuleNode = PartNode.GetNodes("MODULE").FirstOrDefault(n => n.GetValue("name") == moduleName);

            ScaleType = new ScaleType(ModuleNode);
            SetupFromConfig(ScaleType);
            tweakScale = currentScale = defaultScale;

            tfInterface = Type.GetType("TestFlightCore.TestFlightInterface, TestFlightCore", false);
        }

        /// <summary>
        /// Sets up values from ScaleType, creates updaters, and sets up initial values.
        /// </summary>
        protected virtual void Setup()
        {
            if (_setupRun)
            {
                return;
            }
            _prefabPart = part.partInfo.partPrefab;
            _updaters = TweakScaleUpdater.CreateUpdaters(part).ToArray();

            ScaleType = (_prefabPart.Modules["TweakScale"] as TweakScale).ScaleType;
            SetupFromConfig(ScaleType);

            if (!isFreeScale && ScaleFactors.Length != 0)
            {
                tweakName = Tools.ClosestIndex(tweakScale, ScaleFactors);
                tweakScale = ScaleFactors[tweakName];
            }

            if (IsRescaled)
            {
                ScalePart(false, true);
                try
                {
                    CallUpdaters();
                }
                catch (Exception exception)
                {
                    Tools.LogWf("Exception on Rescale: {0}", exception);
                }
            }
            else
            {
                DryCost = (float)(part.partInfo.cost - _prefabPart.Resources.Cast<PartResource>().Aggregate(0.0, (a, b) => a + b.maxAmount * b.info.unitCost));
                if (part.Modules.Contains("FSfuelSwitch"))
                  ignoreResourcesForCost = true;
                
                if (DryCost < 0)
                {
                    Debug.LogError("TweakScale: part=" + part.name + ", DryCost=" + DryCost.ToString());
                    DryCost = 0;
                }
            }
            _setupRun = true;
        }

        /// <summary>
        /// Loads settings from <paramref name="scaleType"/>.
        /// </summary>
        /// <param name="scaleType">The settings to use.</param>
        private void SetupFromConfig(ScaleType scaleType)
        {
            if (ScaleType == null) Debug.LogError("TweakScale: Scaletype==null! part=" + part.name);

            isFreeScale = scaleType.IsFreeScale;
            if (defaultScale == -1)
                defaultScale = scaleType.DefaultScale;

            if (currentScale == -1)
                currentScale = defaultScale;
            else if (defaultScale != scaleType.DefaultScale)
            {
                Tools.Logf("defaultScale has changed for part {0}: keeping relative scale.", part.name);
                currentScale *= scaleType.DefaultScale / defaultScale;
                defaultScale = scaleType.DefaultScale;
            }

            if (tweakScale == -1)
                tweakScale = currentScale;
            Fields["tweakScale"].guiActiveEditor = false;
            Fields["tweakName"].guiActiveEditor = false;
            ScaleFactors = scaleType.ScaleFactors;
            if (ScaleFactors.Length <= 0)
                return;

            if (isFreeScale)
            {
                Fields["tweakScale"].guiActiveEditor = true;
                var range = (UI_ScaleEdit)Fields["tweakScale"].uiControlEditor;
                range.intervals = scaleType.ScaleFactors;
                range.incrementSlide = scaleType.IncrementSlide;
                range.unit = scaleType.Suffix;
                range.sigFigs = 3;
                Fields["tweakScale"].guiUnits = scaleType.Suffix;
            }
            else
            {
                Fields["tweakName"].guiActiveEditor = scaleType.ScaleFactors.Length > 1;
                var options = (UI_ChooseOption)Fields["tweakName"].uiControlEditor;
                ScaleNodes = scaleType.ScaleNodes;
                options.options = scaleType.ScaleNames;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (part.partInfo == null)
            {
                // Loading of the prefab from the part config
                _prefabPart = part;
                SetupPrefab();

            }
            else
            {
                // Loading of the part from a saved craft
                tweakScale = currentScale;
                if (HighLogic.LoadedSceneIsEditor || IsRescaled)
                    Setup();
                else
                    enabled = false;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (part.parent != null)
                {
                    _firstUpdateWithParent = false;
                }
                Setup();

                if (_prefabPart.CrewCapacity > 0)
                {
                    GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                }

                _chainingEnabled = HotkeyManager.Instance.AddHotkey("Scale chaining", new[] {KeyCode.LeftShift},
                    new[] {KeyCode.LeftControl, KeyCode.K}, false);
            }

            // scale IVA overlay
            if (HighLogic.LoadedSceneIsFlight && enabled && (part.internalModel != null))
            {
                _savedIvaScale = part.internalModel.transform.localScale * ScalingFactor.absolute.linear;
                part.internalModel.transform.localScale = _savedIvaScale;
                part.internalModel.transform.hasChanged = true;
            }
        }

        /// <summary>
        /// Scale has changed!
        /// </summary>
        private void OnTweakScaleChanged()
        {
            if (!isFreeScale)
            {
                tweakScale = ScaleFactors[tweakName];
            }

            if ((_chainingEnabled != null) && _chainingEnabled.State)
            {
                ChainScale();
            }

            ScalePart(true, false);
            ScaleDragCubes(false);
            MarkWindowDirty();
            CallUpdaters();

            currentScale = tweakScale;
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        void OnEditorShipModified(ShipConstruct ship)
        {
            if (part.CrewCapacity >= _prefabPart.CrewCapacity) { return; }

            UpdateCrewManifest();
        }

        [UsedImplicitly]
        void Update()
        {
            if (_firstUpdate)
            {
                _firstUpdate = false;
                if (CheckIntegrity())
                    return;

                if (IsRescaled)
                {
                    ScaleDragCubes(true);
                    if (HighLogic.LoadedSceneIsEditor)
                        ScalePart(false, true);  // cloned parts and loaded crafts seem to need this (otherwise the node positions revert)
                }
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (currentScale >= 0f)
                {
                    var changed = currentScale != (isFreeScale ? tweakScale : ScaleFactors[tweakName]);
                    if (changed) // user has changed the scale tweakable
                    {
                        // If the user has changed the scale of the part before attaching it, we want to keep that scale.
                        _firstUpdateWithParent = false;
                        OnTweakScaleChanged();
                    }
                }
            }
            else
            {
                // flight scene frequently nukes our OnStart resize some time later
                if ((part.internalModel != null) && (part.internalModel.transform.localScale != _savedIvaScale))
                {
                    part.internalModel.transform.localScale = _savedIvaScale;
                    part.internalModel.transform.hasChanged = true;
                }
            }

            if (_firstUpdateWithParent && part.HasParent())
            {
                _firstUpdateWithParent = false;
            }

            int len = _updaters.Length;
            for (int i = 0; i < len; i++)
            {
                if (_updaters[i] is IUpdateable)
                    (_updaters[i] as IUpdateable).OnUpdate();
            }
        }

        void CallUpdaters()
        {
            // two passes, to depend less on the order of this list
            int len = _updaters.Length;
            for (int i = 0; i < len; i++)
            {
                // first apply the exponents
                var updater = _updaters[i];
                if (updater is TSGenericUpdater)
                {
                    try
                    {
                        float oldMass = part.mass;
                        updater.OnRescale(ScalingFactor);
                        part.mass = oldMass; // make sure we leave this in a clean state
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Exception on rescale: " + e.ToString());
                    }
                }
            }
            if (_prefabPart.CrewCapacity > 0)
                UpdateCrewManifest();

            if (part.Modules.Contains("ModuleDataTransmitter"))
                UpdateAntennaPowerDisplay();

            // MFT support
            UpdateMftModule();

            // TF support
            updateTestFlight();

            // send scaling part message
            var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
            data.Set<float>("factorAbsolute", ScalingFactor.absolute.linear);
            data.Set<float>("factorRelative", ScalingFactor.relative.linear);
            part.SendEvent("OnPartScaleChanged", data, 0);

            len = _updaters.Length;
            for (int i = 0; i < len; i++)
            {
                var updater = _updaters[i];
                // then call other updaters (emitters, other mods)
                if (updater is TSGenericUpdater)
                    continue;

                updater.OnRescale(ScalingFactor);
            }
        }

        private void UpdateCrewManifest()
        {
            if (!HighLogic.LoadedSceneIsEditor) { return; } //only run the following block in the editor; it updates the crew-assignment GUI

            VesselCrewManifest vcm = ShipConstruction.ShipManifest;
            if (vcm == null) { return; }
            PartCrewManifest pcm = vcm.GetPartCrewManifest(part.craftID);
            if (pcm == null) { return; }

            int len = pcm.partCrew.Length;
            int newLen = Math.Min(part.CrewCapacity, _prefabPart.CrewCapacity);
            if (len == newLen) { return; }

            if (EditorLogic.fetch.editorScreen == EditorScreen.Crew)
                EditorLogic.fetch.SelectPanelParts();

            for (int i = 0; i < len; i++)
                pcm.RemoveCrewFromSeat(i);

            pcm.partCrew = new string[newLen];
            for (int i = 0; i < newLen; i++)
                pcm.partCrew[i] = string.Empty;

            ShipConstruction.ShipManifest.SetPartManifest(part.craftID, pcm);
        }

        void UpdateMftModule()
        {
            try
            {
                if (_prefabPart.Modules.Contains("ModuleFuelTanks"))
                {
                    scaleMass = false;
                    var m = _prefabPart.Modules["ModuleFuelTanks"];
                    FieldInfo fieldInfo = m.GetType().GetField("totalVolume", BindingFlags.Public | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        double oldVol = (double)fieldInfo.GetValue(m) * 0.001d;
                        var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
                        data.Set<string>("volName", "Tankage");
                        data.Set<double>("newTotalVolume", oldVol * ScalingFactor.absolute.cubic);
                        part.SendEvent("OnPartVolumeChanged", data, 0);
                    }
                    else Tools.LogWf("MFT interaction failed (fieldinfo=null)");
                }
            }
            catch (Exception e)
            {
                Tools.LogWf("Exception during MFT interaction" + e.ToString());
            }
        }

        public static Type tfInterface = null;
        private void updateTestFlight()
        {
            if (null == tfInterface) return;
            BindingFlags tBindingFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;
            string name = "scale";
            string value = ScalingFactor.absolute.linear.ToString();
            string owner = "TweakScale";

            bool valueAdded = (bool)tfInterface.InvokeMember("AddInteropValue", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { part, name, value, owner });
            Debug.Log("[TweakScale] TF: valueAdded=" + valueAdded + ", value=" + value.ToString());
        }

        private void UpdateAntennaPowerDisplay()
        {
            var m = part.Modules["ModuleDataTransmitter"] as ModuleDataTransmitter;
            double p = m.antennaPower / 1000;
            Char suffix = 'k';
            if (p >= 1000)
            {
                p /= 1000f;
                suffix = 'M';
                if (p >= 1000)
                {
                    p /= 1000;
                    suffix = 'G';
                }
            }
            p = Math.Round(p, 2);
            string str = p.ToString() + suffix;
            if (m.antennaCombinable) { str += " (Combinable)"; }
            m.powerText = str;
        }

        /// <summary>
        /// Updates properties that change linearly with scale.
        /// </summary>
        /// <param name="moveParts">Whether or not to move attached parts.</param>
        /// <param name="absolute">Whether to use absolute or relative scaling.</param>
        private void ScalePart(bool moveParts, bool absolute)
        {
            ScalePartTransform();

            int len = part.attachNodes.Count;
            for (int i=0; i< len; i++)
            {
                var node = part.attachNodes[i];
                var nodesWithSameId = part.attachNodes
                    .Where(a => a.id == node.id)
                    .ToArray();
                var idIdx = Array.FindIndex(nodesWithSameId, a => a == node);
                var baseNodesWithSameId = _prefabPart.attachNodes
                    .Where(a => a.id == node.id)
                    .ToArray();
                if (idIdx < baseNodesWithSameId.Length)
                {
                    var baseNode = baseNodesWithSameId[idIdx];

                    MoveNode(node, baseNode, moveParts, absolute);
                }
                else
                {
                    Tools.LogWf("Error scaling part. Node {0} does not have counterpart in base part.", node.id);
                }
            }

            try
            {
                // support for ModulePartVariants (the stock texture switch module)
                if (_prefabPart.Modules.Contains("ModulePartVariants"))
                {
                    var pm = _prefabPart.Modules["ModulePartVariants"] as ModulePartVariants;
                    var m = part.Modules["ModulePartVariants"] as ModulePartVariants;

                    var n = pm.variantList.Count;
                    for (int i = 0; i < n; i++)
                    {
                        var v = m.variantList[i];
                        var pv = pm.variantList[i];
                        for (int j = 0; j < v.AttachNodes.Count; j++)
                        {
                            // the module contains attachNodes, so we need to scale those
                            MoveNode(v.AttachNodes[j], pv.AttachNodes[j], false, true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Tools.LogWf("Exception during stockTextureSwitch interaction" + e.ToString());
            }


            if (part.srfAttachNode != null)
            {
                MoveNode(part.srfAttachNode, _prefabPart.srfAttachNode, moveParts, absolute);
            }
            if (moveParts)
            {
                int numChilds = part.children.Count;
                for (int i=0; i<numChilds; i++)
                {
                    var child = part.children[i];
                    if (child.srfAttachNode == null || child.srfAttachNode.attachedPart != part)
                        continue;

                    var attachedPosition = child.transform.localPosition + child.transform.localRotation * child.srfAttachNode.position;
                    var targetPosition = attachedPosition * ScalingFactor.relative.linear;
                    child.transform.Translate(targetPosition - attachedPosition, part.transform);
                }
            }
        }

        private void ScalePartTransform()
        {
            part.rescaleFactor = _prefabPart.rescaleFactor * ScalingFactor.absolute.linear;

            var trafo = part.partTransform.FindChild("model");
            if (trafo != null)
            {
                if (defaultTransformScale.x == 0.0f)
                {
                    defaultTransformScale = trafo.localScale;
                }

                // check for flipped signs
                if (defaultTransformScale.x * trafo.localScale.x < 0)
                {
                    defaultTransformScale.x *= -1;
                }
                if (defaultTransformScale.y * trafo.localScale.y < 0)
                {
                    defaultTransformScale.y *= -1;
                }
                if (defaultTransformScale.z * trafo.localScale.z < 0)
                {
                    defaultTransformScale.z *= -1;
                }

                trafo.localScale = ScalingFactor.absolute.linear * defaultTransformScale;
                trafo.hasChanged = true;
                part.partTransform.hasChanged = true;
            }
        }

        /// <summary>
        /// Change the size of <paramref name="node"/> to reflect the new size of the part it's attached to.
        /// </summary>
        /// <param name="node">The node to resize.</param>
        /// <param name="baseNode">The same node, as found on the prefab part.</param>
        private void ScaleAttachNode(AttachNode node, AttachNode baseNode)
        {
            if (isFreeScale || ScaleNodes == null || ScaleNodes.Length == 0)
            {
                float tmpBaseNodeSize = baseNode.size;
                if (tmpBaseNodeSize == 0)
                {
                    tmpBaseNodeSize = 0.5f;
                }
                node.size = (int)(tmpBaseNodeSize * tweakScale / defaultScale + 0.49);
            }
            else
            {
                node.size = baseNode.size + (1 * ScaleNodes[tweakName]);
            }
            if (node.size < 0)
            {
                node.size = 0;
            }
        }

        private void ScaleDragCubes(bool absolute)
        {
            ScalingFactor.FactorSet factor;
            if (absolute)
                factor = ScalingFactor.absolute;
            else
                factor = ScalingFactor.relative;

            if (factor.linear == 1)
                return;

            int len = part.DragCubes.Cubes.Count;
            for (int ic = 0; ic < len; ic++)
            {
                DragCube dragCube = part.DragCubes.Cubes[ic];
                dragCube.Size *= factor.linear;
                for (int i = 0; i < dragCube.Area.Length; i++)
                    dragCube.Area[i] *= factor.quadratic;

                for (int i = 0; i < dragCube.Depth.Length; i++)
                    dragCube.Depth[i] *= factor.linear;
            }
            part.DragCubes.ForceUpdate(true, true);
        }

        /// <summary>
        /// Moves <paramref name="node"/> to reflect the new scale. If <paramref name="movePart"/> is true, also moves attached parts.
        /// </summary>
        /// <param name="node">The node to move.</param>
        /// <param name="baseNode">The same node, as found on the prefab part.</param>
        /// <param name="movePart">Whether or not to move attached parts.</param>
        /// <param name="absolute">Whether to use absolute or relative scaling.</param>
        private void MoveNode(AttachNode node, AttachNode baseNode, bool movePart, bool absolute)
        {
            if (baseNode == null)
            {
                baseNode = node;
                absolute = false;
            }

            var oldPosition = node.position;

            if (absolute)
                node.position = baseNode.position * ScalingFactor.absolute.linear;
            else
                node.position = node.position * ScalingFactor.relative.linear;

            var deltaPos = node.position - oldPosition;

            if (movePart && node.attachedPart != null)
            {
                if (node.attachedPart == part.parent)
                {
                    part.transform.Translate(-deltaPos, part.transform);
                }
                else
                {
                    var offset = node.attachedPart.attPos * (ScalingFactor.relative.linear - 1);
                    node.attachedPart.transform.Translate(deltaPos + offset, part.transform);
                    node.attachedPart.attPos *= ScalingFactor.relative.linear;
                }
            }
            ScaleAttachNode(node, baseNode);
        }

        /// <summary>
        /// Propagate relative scaling factor to children.
        /// </summary>
        private void ChainScale()
        {
            int len = part.children.Count;
            for (int i=0; i< len; i++)
            {
                var child = part.children[i];
                var b = child.GetComponent<TweakScale>();
                if (b == null)
                    continue;

                float factor = ScalingFactor.relative.linear;
                if (Math.Abs(factor - 1) <= 1e-4f)
                    continue;

                b.tweakScale *= factor;
                if (!b.isFreeScale && (b.ScaleFactors.Length > 0))
                {
                    b.tweakName = Tools.ClosestIndex(b.tweakScale, b.ScaleFactors);
                }
                b.OnTweakScaleChanged();
            }
        }

        /// <summary>
        /// Disable TweakScale module if something is wrong.
        /// </summary>
        /// <returns>True if something is wrong, false otherwise.</returns>
        private bool CheckIntegrity()
        {
            if (ScaleFactors.Length == 0)
            {
                enabled = false; // disable TweakScale module
                Tools.LogWf("{0}({1}) has no valid scale factors. This is probably caused by an invalid TweakScale configuration for the part.", part.name, part.partInfo.title);
                Debug.Log("[TweakScale]" + this.ToString());
                Debug.Log("[TweakScale]" + ScaleType.ToString());
                return true;
            }
            if (this != part.GetComponent<TweakScale>())
            {
                enabled = false; // disable TweakScale module
                Tools.LogWf("Duplicate TweakScale module on part [{0}] {1}", part.partInfo.name, part.partInfo.title);
                Fields["tweakScale"].guiActiveEditor = false;
                Fields["tweakName"].guiActiveEditor = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Marks the right-click window as dirty (i.e. tells it to update).
        /// </summary>
        private void MarkWindowDirty() // redraw the right-click window with the updated stats
        {
            foreach (var win in FindObjectsOfType<UIPartActionWindow>().Where(win => win.part == part))
            {
                // This causes the slider to be non-responsive - i.e. after you click once, you must click again, not drag the slider.
                win.displayDirty = true;
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation situation)
        {
            if (_setupRun && IsRescaled)
                if (ignoreResourcesForCost)
                  return (DryCost - part.partInfo.cost);
                else
                  return (float)(DryCost - part.partInfo.cost + part.Resources.Cast<PartResource>().Aggregate(0.0, (a, b) => a + b.maxAmount * b.info.unitCost));
            else
              return 0;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation situation)
        {
            if (_setupRun && IsRescaled && scaleMass)
              return _prefabPart.mass * (MassScale - 1f);
            else
              return 0;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }


        /// <summary>
        /// These are meant for use with an unloaded part (so you only have the persistent data
        /// but the part is not alive). In this case get currentScale/defaultScale and call
        /// this method on the prefab part.
        /// </summary>
        public double getMassFactor(double rescaleFactor)
        {
            var exponent = ScaleExponents.getMassExponent(ScaleType.Exponents);
            return Math.Pow(rescaleFactor, exponent);
        }
        public double getDryCostFactor(double rescaleFactor)
        {
            var exponent = ScaleExponents.getDryCostExponent(ScaleType.Exponents);
            return Math.Pow(rescaleFactor, exponent);
        }
        public double getVolumeFactor(double rescaleFactor)
        {
            return Math.Pow(rescaleFactor, 3);
        }


        public override string ToString()
        {
            var result = "TweakScale{\n";
            result += "\n _setupRun = " + _setupRun;
            result += "\n isFreeScale = " + isFreeScale;
            result += "\n " + ScaleFactors.Length  + " scaleFactors = ";
            foreach (var s in ScaleFactors)
                result += s + "  ";
            result += "\n tweakScale = "   + tweakScale;
            result += "\n currentScale = " + currentScale;
            result += "\n defaultScale = " + defaultScale;
            //result += " scaleNodes = " + ScaleNodes + "\n";
            //result += "   minValue = " + MinValue + "\n";
            //result += "   maxValue = " + MaxValue + "\n";
            return result + "\n}";
        }

        /*[KSPEvent(guiActive = false, active = true)]
        void OnPartScaleChanged(BaseEventData data)
        {
            float factorAbsolute = data.Get<float>("factorAbsolute");
            float factorRelative = data.Get<float>("factorRelative");
            Debug.Log("PartMessage: OnPartScaleChanged:"
                + "\npart=" + part.name
                + "\nfactorRelative=" + factorRelative.ToString()
                + "\nfactorAbsolute=" + factorAbsolute.ToString());

        }*/

        /*[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Debug")]
        public void debugOutput()
        {
            //var ap = part.partInfo;
            //Debug.Log("prefabCost=" + ap.cost + ", dryCost=" + DryCost +", prefabDryCost=" +(_prefabPart.Modules["TweakScale"] as TweakScale).DryCost);
            //Debug.Log("kisVolOvr=" +part.Modules["ModuleKISItem"].Fields["volumeOverride"].GetValue(part.Modules["ModuleKISItem"]));
            //Debug.Log("ResourceCost=" + (part.Resources.Cast<PartResource>().Aggregate(0.0, (a, b) => a + b.maxAmount * b.info.unitCost) ));

            //Debug.Log("massFactor=" + (part.partInfo.partPrefab.Modules["TweakScale"] as TweakScale).getMassFactor( (double)(currentScale / defaultScale)));
            //Debug.Log("costFactor=" + (part.partInfo.partPrefab.Modules["TweakScale"] as TweakScale).getDryCostFactor( (double)(currentScale / defaultScale)));
            //Debug.Log("volFactor =" + (part.partInfo.partPrefab.Modules["TweakScale"] as TweakScale).getVolumeFactor( (double)(currentScale / defaultScale)));

            //var x = part.collider;
            //Debug.Log("C: " +x.name +", enabled="+x.enabled);
            if (part.Modules.Contains("ModuleRCSFX")) {
                Debug.Log("RCS power=" +(part.Modules["ModuleRCSFX"] as ModuleRCSFX).thrusterPower);
            }
            if (part.Modules.Contains("ModuleEnginesFX"))
            {
                Debug.Log("Engine thrust=" +(part.Modules["ModuleEnginesFX"] as ModuleEnginesFX).maxThrust);
            }
        }*/
    }
}
