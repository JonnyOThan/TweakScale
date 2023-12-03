using System;
using System.Linq;
using UnityEngine;

namespace TweakScale
{
    public class TweakScale : PartModule, IPartCostModifier, IPartMassModifier
    {
        /// <summary>
        /// The selected scale. Different from currentScale only for destination single update, where currentScale is set to match this.
        /// </summary>
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Scale", guiFormat = "0.000", guiUnits = "m")]
        [UI_ScaleEdit(scene = UI_Scene.Editor)]
        public float guiScaleValue = -1;

        /// <summary>
        /// Index into scale values array.
        /// </summary>
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Scale")]
        [UI_ChooseOption(scene = UI_Scene.Editor)]
        public int guiScaleNameIndex = -1;

        // the gui shows currentScaleFactor * guiDefaultScale as the actual scale value.
        // e.g. a 1.25m part at 1x scale will have guiDefaultScale of 1.25, and a free-scale part will have guiDefaultScale at 100 so that it shows a percent
        // this used to be called "defaultScale" as a persistent KSPField but it's been renamed to make it clear that it's only for getting data to/from the gui
        public float guiDefaultScale = -1;

        // the actual scale factor in use.  1.0 means no scaling, 2.0 is twice as big, etc
        [KSPField(isPersistant = true)]
        public float currentScaleFactor = 1;

        /// <summary>
        /// Whether the part should be freely scalable or limited to destination list of allowed values.
        /// </summary>
        [KSPField]
        public bool isFreeScale = false;

        /// <summary>
        /// The scale factor array. If isFreeScale is false, the part may only be one of these scales.
        /// </summary>
        protected float[] ScaleFactors;

        /// <summary>
        /// The node scale array. If node scales are defined the nodes will be resized to these values.
        ///</summary>
        protected int[] ScaleNodes;

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
        public Vector3 defaultTransformScale = new Vector3(0f, 0f, 0f);

        // TODO: these are not being used anymore, need to check the mods that they are related to and see if they're still necessary
        public bool ignoreResourcesForCost = false;
        public bool scaleMass = true;

        /// <summary>
        /// Updaters for different PartModules.
        /// </summary>
        private IRescalable[] _updaters;

        /// <summary>
        /// the amount of extra funds caused by scaling (could be negative)
        /// </summary>
        [KSPField(isPersistant = true)]
        public float extraCost;

        /// <summary>
        /// the amount of extra mass added by scaling (could be negative)
        /// </summary>
        [KSPField(isPersistant = true)]
        public float extraMass;

        [KSPField(guiActiveEditor = true, guiName = "Scale Children")]
        [UI_Toggle(enabledText = "On", disabledText = "Off", affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
        public bool scaleChildren = false;
        // this is shared between all modules, but it's a KSPField so that it shows up in the PAW.  There might be a better way to do that.
        public static bool x_scaleChildren = false;

        /// <summary>
        /// The ScaleType for this part.
        /// </summary>
        public ScaleType ScaleType { get; private set; }

        public bool IsRescaled => Math.Abs(currentScaleFactor - 1f) > 1e-5f;

        internal void CalculateCostAndMass()
        {
            extraCost = 0;
            // TODO: if some modules change their cost or mass based on scale, we could have a problem (feedback loop)
            // getting the module costs from the prefab isn't great either, because it would ignore any modifications that were done to the part (simple example: changing the length of the structural tube)
            // and how do we know which modified module costs should be scaled and which shouldn't?  Probably need to do an audit of all IPartCostModifier and IPartMassModifier
            // it might work to have a registry of which modifiers should be applied here and which shouldn't ?
            // is this what the PrefabDryCostWriter was trying to solve?
            // we might need to store off some info when the part is created
            float dryCost = part.partInfo.cost + part.GetModuleCosts(part.partInfo.cost); 
            foreach (var partResource in _prefabPart.Resources)
            {
                dryCost -= (float)partResource.maxAmount * partResource.info.unitCost;
            }

            float costExponent = ScaleExponents.getDryCostExponent(ScaleType.Exponents);
            float costScale = Mathf.Pow(currentScaleFactor, costExponent);
            float newCost = costScale * dryCost;

            extraCost = newCost - dryCost;

            // TODO: do we need to consider the mass of kerbals here?
            // TODO: we no longer consider the scaleMass flag - need to figure out if we need to.  it's related to ModuleFuelTanks
            extraMass = 0;
            part.UpdateMass();
            float dryMass = part.mass - part.inventoryMass;
            float massExponent = ScaleExponents.getDryMassExponent(ScaleType.Exponents);
            float massScale = Mathf.Pow(currentScaleFactor, massExponent);
            float newMass = massScale * dryMass;
            extraMass = newMass - dryMass;
            part.UpdateMass();
        }

        /// <summary>
        /// Loads settings from <paramref name="scaleType"/>.
        /// </summary>
        /// <param name="scaleType">The settings to use.</param>
        private void SetupFromConfig(ScaleType scaleType)
        {
            ScaleType = scaleType;

            isFreeScale = scaleType.IsFreeScale;
            guiDefaultScale = scaleType.DefaultScale;
            guiScaleValue = currentScaleFactor * guiDefaultScale;
            Fields["guiScaleValue"].guiActiveEditor = false;
            Fields["guiScaleNameIndex"].guiActiveEditor = false;
            ScaleFactors = scaleType.ScaleFactors;
            if (ScaleFactors.Length <= 0)
                return;

            if (isFreeScale)
            {
                Fields["guiScaleValue"].guiActiveEditor = true;
                var range = (UI_ScaleEdit)Fields["guiScaleValue"].uiControlEditor;
                range.intervals = ScaleFactors;
                range.incrementSlide = scaleType.IncrementSlide;
                range.unit = scaleType.Suffix;
                range.sigFigs = 3;
                Fields["guiScaleValue"].guiUnits = scaleType.Suffix;
            }
            else
            {
                Fields["guiScaleNameIndex"].guiActiveEditor = ScaleFactors.Length > 1;
                var options = (UI_ChooseOption)Fields["guiScaleNameIndex"].uiControlEditor;
                ScaleNodes = scaleType.ScaleNodes;
                options.options = scaleType.ScaleNames;
                guiScaleNameIndex = Tools.ClosestIndex(guiScaleValue, ScaleFactors);
                guiScaleValue = ScaleFactors[guiScaleNameIndex];
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // try to load old persisted data
            {
                float currentScaleFromCfgNode = -1;
                node.TryGetValue("defaultScale", ref guiDefaultScale);
                if (node.TryGetValue("currentScale", ref currentScaleFromCfgNode) && currentScaleFromCfgNode > 0 && guiDefaultScale > 0)
                {
                    currentScaleFactor = currentScaleFromCfgNode / guiDefaultScale;
                }
            }

            if (part.partInfo == null)
            {
                // Loading of the prefab from the part config
                _prefabPart = part;

                SetupFromConfig(new ScaleType(node));
            }

            guiScaleValue = currentScaleFactor * guiDefaultScale;
            isEnabled = true;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            _prefabPart = part.partInfo.partPrefab;

            // TODO: this isn't the correct way to get the prefab module.  we should look it up by index.
            SetupFromConfig(_prefabPart.FindModuleImplementing<TweakScale>().ScaleType);

            if (!CheckIntegrity())
            {
                enabled = false;
                return;
            }

            _updaters = TweakScaleHandlerDatabase.CreateUpdaters(part);

            if (IsRescaled)
            {
                // Note that if we fall in here, this part was LOADED from a craft file or vessel in flight.  newly created parts in the editor aren't rescaled.
                ScalePartTransform();
                CallUpdaters(1.0f); // TODO: is 1.0 correct here?  most likely...because everything else in the part should have already been scaled
                // TODO: do we need to worry about drag cubes or anything else?
                foreach (var attachNode in part.attachNodes)
                {
                    ScaleAttachNodeSize(attachNode);
                }
                ScaleAttachNodeSize(part.srfAttachNode); // does the size of the srfAttachNode even matter?
            }
            else
            {
                if (part.Modules.Contains("FSfuelSwitch"))
                    ignoreResourcesForCost = true;
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (_prefabPart.CrewCapacity > 0)
                {
                    GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                }

                scaleChildren = x_scaleChildren;
                Fields["scaleChildren"].OnValueModified += OnScaleChildrenModified;
            }
            else if (!IsRescaled)
            {
                enabled = false;
                isEnabled = false;
            }

            // scale IVA overlay
            if (HighLogic.LoadedSceneIsFlight && enabled && (part.internalModel != null))
            {
                _savedIvaScale = part.internalModel.transform.localScale * currentScaleFactor;
                part.internalModel.transform.localScale = _savedIvaScale;
                part.internalModel.transform.hasChanged = true;
            }
        }

        private void OnScaleChildrenModified(object arg1)
        {
            x_scaleChildren = scaleChildren;
        }

        void OnDestroy()
        {
            Fields["scaleChildren"].OnValueModified -= OnScaleChildrenModified;
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
            _updaters = null; // probably not necessary, but we can help the garbage collector along maybe
        }

        /// <summary>
        /// Scale has changed!
        /// </summary>
        private void OnTweakScaleChanged(float newScaleFactor)
        {
            // TODO: I really hate the concept of the relative scale factor.  It will introduce floating point errors when used repeatedly
            // everything should be computed from the absolute scale and the prefab
            float relativeScaleFactor = newScaleFactor / currentScaleFactor;
            currentScaleFactor = newScaleFactor;

            if (scaleChildren)
            {
                ChainScale(relativeScaleFactor);
            }

            ScalePart(true, relativeScaleFactor);
            CallUpdaters(relativeScaleFactor);

            // TODO: this is going to get called multiple times when chain scaling, should move this...
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        void OnEditorShipModified(ShipConstruct ship)
        {
            if (part.CrewCapacity < _prefabPart.CrewCapacity)
            {
                CrewManifestUpdater.UpdateCrewManifest(part);
            }
        }

        float GetScaleFactorFromGUI()
        {
            float guiScale = isFreeScale ? guiScaleValue : ScaleFactors[guiScaleNameIndex];
            return guiScale / guiDefaultScale;
        }

        void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                scaleChildren = x_scaleChildren;

                // TODO: perhaps this could be done with a callback?
                float newScaleFactor = GetScaleFactorFromGUI();
                if (newScaleFactor != currentScaleFactor)
                {
                    OnTweakScaleChanged(newScaleFactor);
                }
            }
            else
            {
                enabled = false; // stop calling this in flight
            }
        }

        public override void OnUpdate()
        {
            // note: OnUpdate is only called in flight, not the editor
            // isEnabled controls whether the Part calls this function, so only keep it awake if we need it
            isEnabled = false;

            // flight scene frequently nukes our OnStart resize some time later (probably portraits or crew transfers)
            // TODO: we could intercept that with harmony and get rid of this update method
            if (part.internalModel != null)
            {
                if (part.internalModel.transform.localScale != _savedIvaScale)
                {
                    part.internalModel.transform.localScale = _savedIvaScale;
                    part.internalModel.transform.hasChanged = true;
                }

                isEnabled = true;
            }
        }

        void CallUpdaters(float relativeScaleFactor)
        {
            ScalingFactor notificationPayload = new ScalingFactor(currentScaleFactor, relativeScaleFactor, isFreeScale ? -1 : guiScaleNameIndex);
         
            // Recording the ordering here for posterity:
            // TSGenericUpdater is first (applies exponents to everything)
            // then UpdateCrewManifest
            // then UpdateAntennaPowerDisplay
            // then UpdateMftModule
            // then TestFlightCore
            // then part events
            // then all other updaters except TSGenericUpdater
            // it's not exactly clear which of these care about ordering other than the TSGenericUpdater goes first

            // First apply the exponents
            float oldMass = part.mass;
            ScaleExponents.UpdateObject(part, _prefabPart, ScaleType.Exponents, notificationPayload);
            part.mass = oldMass; // since the exponent configs are set up to modify the part mass directly, reset it here

            // send scaling part message (should this be its own partUpdater type?)  I guess not, because then we can keep the updater list empty for many parts
            var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
            data.Set<float>("factorAbsolute", notificationPayload.absolute.linear);
            data.Set<float>("factorRelative", notificationPayload.relative.linear);
            part.SendEvent("OnPartScaleChanged", data, 0);

            if (_updaters != null)
            {
                foreach (var updater in _updaters)
                {
                    try
                    {
                        updater.OnRescale(notificationPayload);
                    }
                    catch (Exception ex)
                    {
                        Tools.LogException(ex, "Updater {0} {1} on part [{2}] threw an exception:", updater.GetType(), updater, part.partInfo.name);
                    }
                }
            }
        }

        int GetUnscaledAttachNodeSize(string attachNodeId)
        {
            // TODO: this is incorrect when PartModuleVariants is involved, or anything that changes the nodes at runtime.
            // but we're only using this for the node size, so it's not a *huge* deal right now.
            var prefabNode = attachNodeId == _prefabPart.srfAttachNode.id
                ? _prefabPart.srfAttachNode // does the size of the srfAttachNode even matter? probably not
                : _prefabPart.FindAttachNode(attachNodeId);

            return prefabNode == null ? 1 : prefabNode.size;
        }

        private void ScalePart(bool moveParts, float relativeScaleFactor)
        {
            ScalePartTransform();

            foreach (var node in part.attachNodes)
            {
                MoveNode(node, moveParts, relativeScaleFactor);
            }
            if (part.srfAttachNode != null)
            {
                MoveNode(part.srfAttachNode, moveParts, relativeScaleFactor);
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
                    var targetPosition = attachedPosition * relativeScaleFactor;
                    child.transform.Translate(targetPosition - attachedPosition, part.transform);
                }
            }

            CalculateCostAndMass();
            ScaleDragCubes(relativeScaleFactor);
        }

        private void ScalePartTransform()
        {
            part.rescaleFactor = _prefabPart.rescaleFactor * currentScaleFactor;

            var trafo = part.partTransform.Find("model");
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

                trafo.localScale = currentScaleFactor * defaultTransformScale;
                trafo.hasChanged = true;
                part.partTransform.hasChanged = true;
            }
        }

        private void ScaleAttachNodeSize(AttachNode node)
        {
            int originalNodeSize = GetUnscaledAttachNodeSize(node.id);

            if (isFreeScale || ScaleNodes == null || ScaleNodes.Length == 0)
            {
                float tmpNodeSize = Mathf.Max(originalNodeSize, 0.5f);
                node.size = (int)(tmpNodeSize * guiScaleValue / guiDefaultScale + 0.49);
            }
            else
            {
                node.size = originalNodeSize + (1 * ScaleNodes[guiScaleNameIndex]);
            }

            node.size = Math.Max(0, node.size);
        }

        // TODO: this is probably wrong; and will accumulate error if you continually scale something up and down
        // need to figure out how to compute this from the prefab and the current state of the part
        private void ScaleDragCubes(float relativeScaleFactor)
        {
            float quadratic = relativeScaleFactor * relativeScaleFactor;
            int len = part.DragCubes.Cubes.Count;
            for (int ic = 0; ic < len; ic++)
            {
                DragCube dragCube = part.DragCubes.Cubes[ic];
                dragCube.Size *= relativeScaleFactor;
                for (int i = 0; i < dragCube.Area.Length; i++)
                    dragCube.Area[i] *= quadratic;

                for (int i = 0; i < dragCube.Depth.Length; i++)
                    dragCube.Depth[i] *= relativeScaleFactor;
            }
            part.DragCubes.ForceUpdate(true, true);
        }

        private void MoveNode(AttachNode node, bool movePart, float relativeScaleFactor)
        {
            var oldPosition = node.position;

            // I hate that this is the simplest fix for scaling parts that dynamically change their nodes
            // TODO: what if we stored off the last known position of each node, and then if it's different here, we assume the new value is unscaled?
            // at least that way you can fix any problems by changing the part scale
            node.originalPosition = node.position = node.position * relativeScaleFactor;

            var deltaPos = node.position - oldPosition;

            if (movePart && node.attachedPart != null)
            {
                if (node.attachedPart == part.parent)
                {
                    part.transform.Translate(-deltaPos, part.transform);
                }
                else
                {
                    var offset = node.attachedPart.attPos * (relativeScaleFactor - 1);
                    node.attachedPart.transform.Translate(deltaPos + offset, part.transform);
                    node.attachedPart.attPos *= relativeScaleFactor;
                }
            }
            ScaleAttachNodeSize(node);
        }

        /// <summary>
        /// Propagate relative scaling factor to children.
        /// </summary>
        private void ChainScale(float relativeScaleFactor)
        {
            int len = part.children.Count;
            for (int i=0; i< len; i++)
            {
                var child = part.children[i];
                var b = child.FindModuleImplementing<TweakScale>();
                if (b == null)
                    continue; // TODO: should we continue down the chain?  could be weird.

                if (Math.Abs(relativeScaleFactor - 1) <= 1e-4f)
                    continue;

                b.guiScaleValue *= relativeScaleFactor;
                if (!b.isFreeScale && (b.ScaleFactors.Length > 0))
                {
                    b.guiScaleNameIndex = Tools.ClosestIndex(b.guiScaleValue, b.ScaleFactors);
                }
                b.MarkWindowDirty();
                b.OnTweakScaleChanged(b.GetScaleFactorFromGUI());
            }
        }

        /// <summary>
        /// Disable TweakScale module if something is wrong.
        /// </summary>
        /// <returns>True if something is wrong, false otherwise.</returns>
        private bool CheckIntegrity()
        {
            if (ScaleFactors == null || ScaleFactors.Length == 0)
            {
                isEnabled = false; // disable TweakScale module
                Tools.LogError("PART [{0}] '{1}' has no valid scale factors. ScaleType: {1}.  This is probably caused by an invalid TweakScale configuration for the part.", part.partInfo.name, part.partInfo.title, ScaleType);
                return false;
            }
            if (this != part.FindModuleImplementing<TweakScale>())
            {
                isEnabled = false; // disable TweakScale module
                Tools.LogError("Duplicate TweakScale module on PART [{0}] {1}", part.partInfo.name, part.partInfo.title);
                Fields["guiScaleValue"].guiActiveEditor = false;
                Fields["guiScaleNameIndex"].guiActiveEditor = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Marks the right-click window as dirty (i.e. tells it to update).
        /// </summary>
        private void MarkWindowDirty() // redraw the right-click window with the updated stats
        {
            if (!part.PartActionWindow) return;

            part.PartActionWindow.displayDirty = true;
        }

        float IPartCostModifier.GetModuleCost(float defaultCost, ModifierStagingSituation situation)
        {
            return extraCost;
        }

        ModifierChangeWhen IPartCostModifier.GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        float IPartMassModifier.GetModuleMass(float defaultMass, ModifierStagingSituation situation)
        {
            return extraMass;
        }

        ModifierChangeWhen IPartMassModifier.GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }
    }
}
