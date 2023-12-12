using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TweakScale
{
	public class TweakScale : PartModule, IPartCostModifier, IPartMassModifier
	{
		const string guiGroupName = "TweakScale";
		const string guiGroupDisplayName = "TweakScale";

		/// <summary>
		/// The selected scale. Different from currentScale only for destination single update, where currentScale is set to match this.
		/// </summary>
		[KSPField(guiActiveEditor = true, guiName = "Scale", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName, guiFormat = "0.000", guiUnits = "m")]
		[UI_ScaleEditNumeric(scene = UI_Scene.Editor)]
		public float guiScaleValue = -1;

		/// <summary>
		/// Index into scale values array.
		/// </summary>
		[KSPField(guiActiveEditor = true, guiName = "Scale", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName)]
		[UI_ChooseOption(scene = UI_Scene.Editor)]
		public int guiScaleNameIndex = -1;

		// the gui shows currentScaleFactor * guiDefaultScale as the actual scale value.
		// e.g. a 1.25m part at 1x scale will have guiDefaultScale of 1.25, and a free-scale part will have guiDefaultScale at 100 so that it shows a percent
		// this used to be called "defaultScale" as a persistent KSPField but it's been renamed to make it clear that it's only for getting data to/from the gui
		public float guiDefaultScale = -1;

		// the actual scale factor in use.  1.0 means no scaling, 2.0 is twice as big, etc
		[KSPField(isPersistant = true)]
		public float currentScaleFactor = 1;

		// these are shared between all modules, but it's a KSPField so that it shows up in the PAW.  There might be a better way to do that.
		[KSPField(guiActiveEditor = true, guiName = "Scale Children", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName)]
		[UI_Toggle(enabledText = "On", disabledText = "Off", affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
		public bool scaleChildren = false;
		[KSPField(guiActiveEditor = true, guiName = "Match Node Size", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName)]
		[UI_Toggle(enabledText = "On", disabledText = "Off", affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
		public bool matchNodeSize = false;
		[KSPField(guiActiveEditor = true, guiName = "Show Stats", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName)]
		[UI_Toggle(enabledText = "On", disabledText = "Off", affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
		public bool showStats = false;

		[UI_Label]
		[KSPField(guiActiveEditor = false, guiName = "Stats", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName)]
		public string guiStatsText = "";

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

		// TODO: this is not being used anymore, need to check the mods (FSFuelSwitch) that they are related to and see if they're still necessary
		public bool ignoreResourcesForCost = false;
		
		// This is used by ModuleFuelTanks (RealFuels / ModularFuelTanks).  That module will subtract the prefab's mass in their GetModuleMass function:
		// https://github.com/KSP-RO/RealFuels/blob/920e4a6986534a51f06789d9bacc2556b23e8b6c/Source/Tanks/ModuleFuelTanks.cs#L828C119-L828C130
		// which means that the prefab mass we use as a baseline for mass scaling does not represent the actual default state of the part.
		// There would probably be better ways to accomplish this, e.g. setting the mass exponent to 0 in the cfg.
		// It might also make sense to run GetModuleMass on the part prefab to get the default mass state (but some mods might not expect that)
		public bool scaleMass = true;

		/// <summary>
		/// Handlers for different PartModules.
		/// </summary>
		private IRescalable[] _handlers;

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

		// modules that understand scaling themselves (or more generally: apply modifiers that shouldn't be scaled) should be excluded from cost/mass adjustments
		static HashSet<string> x_modulesToExcludeForDryStats = new string[]
		{
			"ModuleB9PartSwitch",
			"TweakScale",
			"ModuleInventoryPart",
			"ModuleFuelTanks",
		}.ToHashSet();

		internal void CalculateCostAndMass()
		{
			float cost = part.partInfo.cost;
			float mass = part.partInfo.partPrefab.mass;

			foreach (var module in part.modules.modules)
			{
				if (x_modulesToExcludeForDryStats.Contains(module.ClassName)) continue;

				if (module is IPartCostModifier costModifier)
				{
					cost += costModifier.GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT);
				}
				if (module is IPartMassModifier massModifier)
				{
					mass += massModifier.GetModuleMass(part.partInfo.partPrefab.mass, ModifierStagingSituation.CURRENT);
				}
			}

			// the cost from the prefab includes the price of resources
			foreach (var partResource in _prefabPart.Resources)
			{
				cost -= (float)partResource.maxAmount * partResource.info.unitCost;
			}

			float costExponent = ScaleExponents.getDryCostExponent(ScaleType.Exponents);
			float costScale = Mathf.Pow(currentScaleFactor, costExponent);
			float newCost = costScale * cost;

			extraCost = newCost - cost;

			// TODO: do we need to consider the mass of kerbals here?
			if (scaleMass)
			{
				float massExponent = ScaleExponents.getDryMassExponent(ScaleType.Exponents);
				float massScale = Mathf.Pow(currentScaleFactor, massExponent);
				float newMass = massScale * mass;
				extraMass = newMass - mass;
				part.UpdateMass();
			}
			else
			{
				extraMass = 0;
			}
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
			ScaleFactors = scaleType.GetUnlockedScaleFactors();
			if (ScaleFactors.Length <= 0)
				return;

			if (isFreeScale)
			{
				Fields["guiScaleValue"].guiActiveEditor = true;
				var range = (UI_ScaleEdit)Fields["guiScaleValue"].uiControlEditor;
				range.intervals = ScaleFactors;
				range.incrementSlide = scaleType.IncrementSlide; // TODO: does this need to be filtered by unlock?
				range.unit = scaleType.Suffix;
				range.sigFigs = 3;
				Fields["guiScaleValue"].guiUnits = scaleType.Suffix;
			}
			else
			{
				Fields["guiScaleNameIndex"].guiActiveEditor = ScaleFactors.Length > 1;
				var options = (UI_ChooseOption)Fields["guiScaleNameIndex"].uiControlEditor;
				ScaleNodes = scaleType.ScaleNodes;
				options.options = scaleType.GetUnlockedScaleNames();
				guiScaleNameIndex = Tools.ClosestIndex(guiScaleValue, ScaleFactors);
				guiScaleValue = ScaleFactors[guiScaleNameIndex];
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				// Loading of the prefab from the part config
				_prefabPart = part;
				SetupFromConfig(new ScaleType(node));
			}
			else
			{
				// try to load old persisted data
				if (!node.HasValue(nameof(currentScaleFactor)))
				{
					float currentScaleFromCfgNode = -1;
					node.TryGetValue("defaultScale", ref guiDefaultScale);
					if (node.TryGetValue("currentScale", ref currentScaleFromCfgNode) && currentScaleFromCfgNode > 0 && guiDefaultScale > 0)
					{
						currentScaleFactor = currentScaleFromCfgNode / guiDefaultScale;
					}
				}

				guiScaleValue = currentScaleFactor * guiDefaultScale;
				isEnabled = true; // isEnabled gets persisted in the cfg, and we want to always start enabled and then go to sleep
			}
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			// write some of the old keys to try to help interoperability with other versions of tweakscale
			if (ScaleType != null)
			{
				node.AddValue("type", ScaleType.Name);
			}
			node.AddValue("defaultScale", guiDefaultScale);
			node.AddValue("currentScale", guiScaleValue);
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

			// TODO: is anything in here needed for flight mode?  should this call be moved to OnLoad and restricted to LOADING and EDITOR scenes?
			// just conceptually, it does seem to be more closely tied to loading data from the cfg.
			SetupFromConfig(prefabModule.ScaleType);

			if (!CheckIntegrity())
			{
				enabled = false;
				return;
			}

			_handlers = TweakScaleHandlerDatabase.CreateHandlers(part);

			if (IsRescaled)
			{
				// Note that if we fall in here, this part was LOADED from a craft file or vessel in flight.  newly created parts in the editor aren't rescaled.
				ScalePartTransform();
				CallHandlers(1.0f); // TODO: is 1.0 correct here?  most likely...because everything else in the part should have already been scaled
									// TODO: do we need to worry about drag cubes or anything else?
				foreach (var attachNode in part.attachNodes)
				{
					ScaleAttachNodeSize(attachNode);
				}
				ScaleAttachNodeSize(part.srfAttachNode); // does the size of the srfAttachNode even matter?
			}
			else
			{
				SetStatsLabel("");
				if (part.Modules.Contains("FSfuelSwitch"))
					ignoreResourcesForCost = true;
			}

			if (HighLogic.LoadedSceneIsEditor)
			{
				if (_prefabPart.CrewCapacity > 0)
				{
					GameEvents.onEditorShipModified.Add(OnEditorShipModified);
				}

				scaleChildren = TweakScaleEditorLogic.Instance.ScaleChildren;
				Fields[nameof(scaleChildren)].OnValueModified += OnScaleChildrenModified;

				matchNodeSize = TweakScaleEditorLogic.Instance.MatchNodeSize;
				Fields[nameof(matchNodeSize)].OnValueModified += OnMatchNodeSizeModified;

				showStats = TweakScaleEditorLogic.Instance.ShowStats;
				Fields[nameof(showStats)].OnValueModified += OnShowStatsModified;

				Fields[nameof(guiScaleValue)].OnValueModified += OnGuiScaleModified;
				Fields[nameof(guiScaleNameIndex)].OnValueModified += OnGuiScaleModified;
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
			Fields[nameof(scaleChildren)].OnValueModified -= OnScaleChildrenModified;
			Fields[nameof(matchNodeSize)].OnValueModified -= OnMatchNodeSizeModified;
			Fields[nameof(showStats)].OnValueModified -= OnShowStatsModified;
			Fields[nameof(guiScaleValue)].OnValueModified -= OnGuiScaleModified;
			Fields[nameof(guiScaleNameIndex)].OnValueModified -= OnGuiScaleModified;
			GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
			_handlers = null; // probably not necessary, but we can help the garbage collector along maybe
		}
		private void OnGuiScaleModified(object arg1)
		{
			float newScaleFactor = GetScaleFactorFromGUI();
			if (newScaleFactor != currentScaleFactor)
			{
				OnTweakScaleChanged(newScaleFactor);

				GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);

				UpdatePartActionWindow(false);
			}
		}

		private void OnScaleChildrenModified(object arg1)
		{
			TweakScaleEditorLogic.Instance.ScaleChildren.State = scaleChildren;
		}

		private void OnMatchNodeSizeModified(object arg1)
		{
			TweakScaleEditorLogic.Instance.MatchNodeSize.State = matchNodeSize;
		}

		private void OnShowStatsModified(object arg1)
		{
			TweakScaleEditorLogic.Instance.ShowStats = showStats;
			UpdateStatsVisibility();
		}

		void UpdateStatsVisibility()
		{
			Fields[nameof(guiStatsText)].guiActiveEditor = showStats && guiStatsText.Length > 0;
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
			CallHandlers(relativeScaleFactor);
			CalculateCostAndMass();
		}

		void OnEditorShipModified(ShipConstruct ship)
		{
			if (part.CrewCapacity < _prefabPart.CrewCapacity)
			{
				CrewManifestHandler.UpdateCrewManifest(part);
			}
		}

		float GetScaleFactorFromGUI()
		{
			float guiScale = isFreeScale ? guiScaleValue : ScaleFactors[guiScaleNameIndex];
			return guiScale / guiDefaultScale;
		}

		bool UpdateLocalSetting(ref bool localSetting, bool globalSetting, BaseField field)
		{
			if (localSetting != globalSetting)
			{
				localSetting = globalSetting;
				field?.uiControlEditor?.partActionItem?.UpdateItem();
			}

			return false;
		}

		void Update()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				// copy from the global setting into our KSPField (might want to do this to all TweakScale modules when changing the setting, so we can get rid of the update function?)
				// maybe have the TweakScaleEditorLogic have events we can register for, that just call UpdateItem as appropriate
				UpdateLocalSetting(ref scaleChildren, TweakScaleEditorLogic.Instance.ScaleChildren, Fields[nameof(scaleChildren)]);
				UpdateLocalSetting(ref matchNodeSize, TweakScaleEditorLogic.Instance.MatchNodeSize, Fields[nameof(matchNodeSize)]);
				if (UpdateLocalSetting(ref showStats, TweakScaleEditorLogic.Instance.ShowStats, Fields[nameof(showStats)]))
				{
					UpdateStatsVisibility();
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
				}

				// TODO: the part's internal model might be spawned later (freeiva, crew transfers, moduleanimategeneric, etc) at which point we need to wake up.
				isEnabled = true;
			}
		}

		static StringBuilder x_infoBuilder = new StringBuilder();
		static StringBuilder GetInfoBuilder()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				x_infoBuilder.Clear();
				return x_infoBuilder;
			}

			return null;
		}

		void CallHandlers(float relativeScaleFactor)
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
			StringBuilder infoBuilder = GetInfoBuilder();
			ScaleExponents.UpdateObject(part, _prefabPart, ScaleType.Exponents, notificationPayload, infoBuilder);
			part.mass = oldMass; // since the exponent configs are set up to modify the part mass directly, reset it here

			// send scaling part message (should this be its own partUpdater type?)  I guess not, because then we can keep the handler list empty for many parts
			var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
			data.Set<float>("factorAbsolute", notificationPayload.absolute.linear);
			data.Set<float>("factorRelative", notificationPayload.relative.linear);
			part.SendEvent("OnPartScaleChanged", data, 0);

			if (_handlers != null)
			{
				foreach (var handler in _handlers)
				{
					try
					{
						// TODO: how to get string info out of this?
						handler.OnRescale(notificationPayload);
					}
					catch (Exception ex)
					{
						Tools.LogException(ex, "Handler {0} {1} on part [{2}] threw an exception:", handler.GetType(), handler, part.partInfo.name);
					}
				}
			}

			SetStatsLabel(infoBuilder?.ToString());
		}

		private void SetStatsLabel(string text)
		{
			var statsField = Fields[nameof(guiStatsText)];
			if (text == null || text.Length == 0)
			{
				guiStatsText = "";
				statsField.guiActiveEditor = false;
			}
			else
			{
				guiStatsText = text;
				statsField.uiControlEditor?.partActionItem?.UpdateItem();

				if (statsField.guiActiveEditor = showStats)
				{
					var tweakScaleGroup = part.PartActionWindow?.parameterGroups[guiGroupName];

					if (tweakScaleGroup != null)
					{
						// no idea why the commmented out stuff below doesn't work :/
						tweakScaleGroup.CollapseGroupToggle();
						tweakScaleGroup.CollapseGroupToggle();

						//tweakScaleGroup.SetUIState();
						//LayoutRebuilder.MarkLayoutForRebuild(tweakScaleGroup.contentLayout.transform as RectTransform);
						//Canvas.ForceUpdateCanvases();
						//tweakScaleGroup.window.UpdateWindow();
						//UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(tweakScaleGroup.transform as RectTransform);
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
			for (int i = 0; i < len; i++)
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
				b.UpdatePartActionWindow(true);
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


		private void UpdatePartActionWindow(bool includeScaleControls)
		{
			if (!part.PartActionWindow) return;

			var scaleValueItem = Fields["guiScaleValue"]._uiControlEditor.partActionItem;
			var scaleNameIndexItem = Fields["guiScaleNameIndex"].uiControlEditor.partActionItem;
			foreach (var item in part.PartActionWindow.listItems)
			{
				if (includeScaleControls || (item != scaleValueItem && item != scaleNameIndexItem))
				{
					item.UpdateItem();
				}
			}
		}

		public override string GetInfo()
		{
			return "Scale Type: " + ScaleType.Name;
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
