using System;
using System.Collections.Generic;
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

        // A few different systems can alter attach nodes in the editor (ModulePartVariants, ModuleB9PartSwitch, maybe more)
        // The lifecycle between all of these is pretty complex and tough to manage, but one thing that would make it all easier is
        // if we could always ask "what would the state of this attachnode be in an unscaled part?"  This attemps to track that.
        struct AttachNodeInfo
        {
            public Vector3 position;
            public int size;
        }
        Dictionary<string, AttachNodeInfo> unscaledAttachNodes = new Dictionary<string, AttachNodeInfo>();
        
        public void SetUnscaledAttachNode(AttachNode attachNode)
        {
            unscaledAttachNodes[attachNode.id] = new AttachNodeInfo { position = attachNode.position, size = attachNode.size };
        }

        public void SetUnscaledAttachNodePosition(string attachNodeId, Vector3 position)
        {
            var nodeInfo = unscaledAttachNodes[attachNodeId];
            nodeInfo.position = position;
            unscaledAttachNodes[attachNodeId] = nodeInfo;
        }

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
            isEnabled = true; // isEnabled gets persisted in the cfg, and we want to always start enabled and then go to sleep
        }

        // This is a hacky way to do some "final" loading - the part compiler will deactivate the part when it's nearly finished
        // and that will end up here.  Note there are still a few fields that haven't been set yet
        void OnDisable()
        {
            // part is null for the version of this that gets called when rendering the part icon.  Just skip.
            if (HighLogic.LoadedScene == GameScenes.LOADING && part != null)
            {
                foreach (var attachNode in part.attachNodes)
                {
                    SetUnscaledAttachNode(attachNode);
                }
                if (part.srfAttachNode != null)
                {
                    SetUnscaledAttachNode(part.srfAttachNode);
                }
            }
        }

        new void Awake()
        {
            base.Awake();

            if (HighLogic.LoadedSceneIsEditor)
            {
                // This has to be really early because other modules might try to shove data in here as they're initializing
                var prefabModule = part.partInfo.partPrefab.FindModuleImplementing<TweakScale>();
                unscaledAttachNodes = new Dictionary<string, AttachNodeInfo>(prefabModule.unscaledAttachNodes);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            _prefabPart = part.partInfo.partPrefab;
            // TODO: this isn't the correct way to get the prefab module.  we should look it up by index.
            var prefabModule = _prefabPart.FindModuleImplementing<TweakScale>();

            SetupFromConfig(prefabModule.ScaleType);

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
                Fields["guiScaleValue"].OnValueModified += OnGuiScaleModified;
                Fields["guiScaleNameIndex"].OnValueModified += OnGuiScaleModified;
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
            }
        }

        void OnDestroy()
        {
            Fields["scaleChildren"].OnValueModified -= OnScaleChildrenModified;
            Fields["guiScaleValue"].OnValueModified -= OnGuiScaleModified;
            Fields["guiScaleNameIndex"].OnValueModified -= OnGuiScaleModified;
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
            _updaters = null; // probably not necessary, but we can help the garbage collector along maybe
        }
        private void OnGuiScaleModified(object arg1)
        {
            float newScaleFactor = GetScaleFactorFromGUI();
            if (newScaleFactor != currentScaleFactor)
            {
                OnTweakScaleChanged(newScaleFactor);

                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }
        }

        private void OnScaleChildrenModified(object arg1)
        {
            x_scaleChildren = scaleChildren;
        }

        /// <summary>
        /// Scale has changed!
        /// </summary>
        private void OnTweakScaleChanged(float newScaleFactor)
        {
            // TODO: I really hate the concept of the relative scale factor.  It will introduce floating point errors when used repeatedly
            // everything should be computed from the absolute scale and the prefab
            // this might not be possible in cases where we can't access the original value from the prefab
            // (attach nodes are a good example of this, because WHICH attachnode should be considered the "default" can change, and I had to build a whole system to solve it)
            float relativeScaleFactor = newScaleFactor / currentScaleFactor;
            currentScaleFactor = newScaleFactor;

            // TODO: would it make more sense to scale ourselves first and then the children?  Currently we might be moving each children twice
            if (scaleChildren)
            {
                ChainScale(relativeScaleFactor);
            }

            ScalePart(relativeScaleFactor);
            CallUpdaters(relativeScaleFactor);
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
                // copy from the global setting into our KSPField (might want to do this to all TweakScale modules when changing the setting, so we can get rid of the update function?)
                scaleChildren = x_scaleChildren;
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
            if (unscaledAttachNodes.TryGetValue(attachNodeId, out var nodeInfo))
            {
                return nodeInfo.size;
            }
            else
            {
                Tools.LogError("Couldn't find a stored unscaled attach node with ID {0} on part {1}", attachNodeId, part.partInfo.name);
                return 1;
            }
        }

        private void ScalePart(float relativeScaleFactor)
        {
            ScalePartTransform();

            // handle nodes and node-attached parts
            foreach (var node in part.attachNodes)
            {
                MoveNode(node);
            }
            if (part.srfAttachNode != null)
            {
                MoveNode(part.srfAttachNode);
            }

            // handle parts that are surface-attached to this one
            foreach (var child in part.children)
            {
                if (child.srfAttachNode == null || child.srfAttachNode.attachedPart != part)
                    continue;

                var attachedPosition = child.transform.localPosition + child.transform.localRotation * child.srfAttachNode.position;
                var targetPosition = attachedPosition * relativeScaleFactor;
                child.transform.Translate(targetPosition - attachedPosition, part.transform);
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

        internal void MoveNode(AttachNode node)
        {
            var oldPosition = node.position;

            AttachNodeInfo unscaledNodeInfo = unscaledAttachNodes[node.id];

            node.originalPosition = node.position = unscaledNodeInfo.position * currentScaleFactor;

            if (node.attachedPart != null)
            {
                var deltaPos = node.position - oldPosition;

                // If this node connects to our parent part, then *we* need to move
                if (node.attachedPart == part.parent)
                {
                    part.transform.Translate(-deltaPos, part.transform);
                }
                // otherwise the child object needs to move
                else
                {
                    node.attachedPart.transform.Translate(deltaPos, part.transform);
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
