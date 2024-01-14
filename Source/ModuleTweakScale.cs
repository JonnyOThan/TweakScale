using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TweakScale
{
	public class TweakScale : PartModule, IPartCostModifier, IPartMassModifier, IModuleInfo
	{
		const string guiGroupName = "TweakScale";
		const string guiGroupDisplayName = "TweakScale";

		/// <summary>
		/// The selected scale. Different from currentScale only for destination single update, where currentScale is set to match this.
		/// </summary>
		[KSPField(guiActiveEditor = true, guiName = "Scale", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName, guiFormat = "0.###", guiUnits = "m")]
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
		[KSPField(guiActiveEditor = true, guiName = "Show KeyBindings", groupName = guiGroupName, groupDisplayName = guiGroupDisplayName)]
		[UI_Toggle(enabledText = "On", disabledText = "Off", affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
		public bool showKeyBindings = false;
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
		string[] scaleNames;

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

		public HandlerType FindHandlerOfType<HandlerType>() where HandlerType : class, IRescalable
		{
			if (_handlers == null) return null;
			return (HandlerType)_handlers.FirstOrDefault(h => h.GetType() == typeof(HandlerType));
		}

		/// <summary>
		/// the amount of extra funds caused by scaling (could be negative)
		/// </summary>
		[KSPField(isPersistant = true)]
		public float extraCost;
		float guiAddedCost = 0;

		/// <summary>
		/// the amount of extra mass added by scaling (could be negative)
		/// </summary>
		[KSPField(isPersistant = true)]
		public float extraMass;

		[SerializeField] double unscaledDryCost;
		[SerializeField] double unscaledResourceCapacityCost;

		/// <summary>
		/// The ScaleType for this part.
		/// </summary>
		public ScaleType ScaleType { get; private set; }

		public bool IsRescaled => Math.Abs(currentScaleFactor - 1f) > 1e-5f;

		// These are not used, but only included here to silence warnings when applying scaling exponents to this module
		// TODO: someday should find a way to avoid this hackery.
		// These are set to 0 so that they don't show up in the PAW stats.  but it might be useful to set them to 1 and then use these
		// instead of ScaleExponent.getDryMassExponent and getDryCostExponent
#pragma warning disable 0414
		private float DryCost = 0;
		private float MassScale = 0;
#pragma warning restore 0414

#region Attach Node Stuff

		// A few different systems can alter attach nodes in the editor (ModulePartVariants, ModuleB9PartSwitch, maybe more)
		// The lifecycle between all of these is pretty complex and tough to manage, but one thing that would make it all easier is
		// if we could always ask "what would the state of this attachnode be in an unscaled part?"  This attemps to track that.
		internal struct AttachNodeInfo
		{
			public Vector3 position;
			public float size; // usually an integer, 0 = 0.625m, 1 = 1.25m, 2 = 2.5m, etc.  Can be a float though
			public float diameter; // the actual diameter, e.g. 1.25m, 2.5m, etc.
		}

		Dictionary<string, AttachNodeInfo> unscaledAttachNodes = new Dictionary<string, AttachNodeInfo>();
		ConfigNode attachNodeDiameters; // only valid in loading
		[SerializeField] float attachNodeDiameter = 0f; // if nonzero and the ATTACHNODEDIAMETER node is not used, this sets all attach nodes to this diameter

		public void SetUnscaledAttachNode(AttachNode attachNode)
		{
			float diameter = Tools.AttachNodeSizeDiameter(attachNode.size);

			// see if there is an override in the cfg node (which is only valid during loading) or an already existing default (size must match though)
			if (attachNodeDiameters != null)
			{
				attachNodeDiameters.TryGetValue(attachNode.id, ref diameter);
			}
			else if (attachNodeDiameter != 0)
			{
				diameter = attachNodeDiameter;
			}
			else if (unscaledAttachNodes.TryGetValue(attachNode.id, out var existingInfo) && existingInfo.size == attachNode.size)
			{
				diameter = existingInfo.diameter;
			}

			SetUnscaledAttachNode(attachNode, diameter);
		}

		public void SetUnscaledAttachNode(AttachNode attachNode, float diameter)
		{
			unscaledAttachNodes[attachNode.id] = new AttachNodeInfo
			{ 
				position = attachNode.position,
				size = attachNode.size,
				diameter = diameter
			};
		}

		public void SetUnscaledAttachNodePosition(string attachNodeId, Vector3 position)
		{
			var nodeInfo = unscaledAttachNodes[attachNodeId];
			nodeInfo.position = position;
			unscaledAttachNodes[attachNodeId] = nodeInfo;
		}

		internal bool TryGetUnscaledAttachNode(string attachNodeId, out AttachNodeInfo attachNodeInfo)
		{
			return unscaledAttachNodes.TryGetValue(attachNodeId, out attachNodeInfo);
		}

		float GetUnscaledAttachNodeSize(string attachNodeId)
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

#endregion

		new void Awake()
		{
			base.Awake();

			// This has to be really early because other modules might try to shove data in here as they're initializing
			if (part?.partInfo?.partPrefab != null)
			{
				var prefabModule = part.partInfo.partPrefab.FindModuleImplementing<TweakScale>();
				unscaledAttachNodes = new Dictionary<string, AttachNodeInfo>(prefabModule.unscaledAttachNodes);

				// if the prefab did not have a srfAttachNode defined, then we need to add an entry for it
				if (part.partInfo.partPrefab.srfAttachNode == null && part.srfAttachNode != null)
				{
					SetUnscaledAttachNode(part.srfAttachNode);
				}
			}
		}

		void OnDestroy()
		{
			Fields[nameof(scaleChildren)].OnValueModified -= OnScaleChildrenModified;
			Fields[nameof(matchNodeSize)].OnValueModified -= OnMatchNodeSizeModified;
			Fields[nameof(showStats)].OnValueModified -= OnShowStatsModified;
			Fields[nameof(showKeyBindings)].OnValueModified -= OnShowKeyBindingsModified;
			Fields[nameof(guiScaleValue)].OnValueModified -= OnGuiScaleModified;
			Fields[nameof(guiScaleNameIndex)].OnValueModified -= OnGuiScaleModified;
			GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
			_handlers = null; // probably not necessary, but we can help the garbage collector along maybe
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
			scaleNames = scaleType.GetUnlockedScaleNames();
			if (ScaleFactors.Length <= 0)
				return;

			if (isFreeScale)
			{
				Fields["guiScaleValue"].guiActiveEditor = true;
				var range = (UI_ScaleEdit)Fields["guiScaleValue"].uiControlEditor;
				range.intervals = ScaleFactors;
				range.incrementSlide = scaleType.IncrementSlide; // TODO: does this need to be filtered by unlock?
				range.unit = scaleType.Suffix;
				range.sigFigs = scaleType.Suffix == "%" ? 0 : 3;
				Fields["guiScaleValue"].guiUnits = scaleType.Suffix;
			}
			else
			{
				Fields["guiScaleNameIndex"].guiActiveEditor = ScaleFactors.Length > 1;
				var options = (UI_ChooseOption)Fields["guiScaleNameIndex"].uiControlEditor;
				options.options = scaleNames;
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

#if DEBUG
				var scaleTypes = node.GetValuesList("type");
				if (scaleTypes.Count > 1)
				{
					Tools.LogWarning("PART {0} config has multiple scale type settings: {1}", part.name, string.Join(", ", scaleTypes));
				}
#endif

				attachNodeDiameters = node.GetNode("ATTACHNODEDIAMETER");
				node.TryGetValue(nameof(attachNodeDiameter), ref attachNodeDiameter);
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

					// TS/L saves the unscaled node position in the originalPosition, and *sometimes* scales the current position
					foreach (var attachNode in part.attachNodes)
					{
						attachNode.position = (attachNode.originalPosition *= currentScaleFactor);
					}
					if (part.srfAttachNode != null && part.srfAttachNode.originalPosition != part.srfAttachNode.position)
					{
						part.srfAttachNode.position = (part.srfAttachNode.originalPosition *= currentScaleFactor);
					}
				}

				guiScaleValue = currentScaleFactor * guiDefaultScale;
				isEnabled = true; // isEnabled gets persisted in the cfg, and we want to always start enabled and then go to sleep
			}
		}

		void OnLoadFinalize()
		{
			// grab the unscaled state of attach nodes
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

			InitializeUnscaledCosts();
		}

		// Determine what the unscaled dry cost is.  The cost from the prefab includes the price of resources, but other mods can mess with this....
		internal void InitializeUnscaledCosts()
		{
			GetPartResourceCosts(part, out double prefabResourceCost, out unscaledResourceCapacityCost);
			unscaledDryCost = part.partInfo.cost - unscaledResourceCapacityCost;
		}

		// ADVANCED USE ONLY - directly set the unscaled costs
		internal void SetUnscaledCosts(double dryCost, double resourceCapacityCost)
		{
			unscaledDryCost = dryCost;
			unscaledResourceCapacityCost = resourceCapacityCost;
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

			if (HighLogic.LoadedSceneIsEditor)
			{
				if (_prefabPart.CrewCapacity > 0)
				{
					GameEvents.onEditorShipModified.Add(OnEditorShipModified);
				}

				scaleChildren = TweakScaleEditorLogic.Instance.ScaleChildren;
				Fields[nameof(scaleChildren)].OnValueModified += OnScaleChildrenModified;
				Fields[nameof(scaleChildren)].guiName = $"[{TweakScaleEditorLogic.Instance.ScaleChildren.GetToggleKey()}] Scale Children";

				matchNodeSize = TweakScaleEditorLogic.Instance.MatchNodeSize;
				Fields[nameof(matchNodeSize)].OnValueModified += OnMatchNodeSizeModified;
				Fields[nameof(matchNodeSize)].guiName = $"[{TweakScaleEditorLogic.Instance.MatchNodeSize.GetToggleKey()}] Match Node Size";

				showStats = TweakScaleEditorLogic.Instance.ShowStats;
				Fields[nameof(showStats)].OnValueModified += OnShowStatsModified;

				showKeyBindings = TweakScaleEditorLogic.Instance.ShowKeyBinds;
				Fields[nameof(showKeyBindings)].OnValueModified += OnShowKeyBindingsModified;

				Fields[nameof(guiScaleValue)].OnValueModified += OnGuiScaleModified;
				Fields[nameof(guiScaleNameIndex)].OnValueModified += OnGuiScaleModified;

				// TODO: there might be other IPartCostModifier and IPartMassModifier modules out there that we need to handle
				foreach (var variantModule in part.modules.GetModules<ModulePartVariants>())
				{
					UI_VariantSelector uI_VariantSelector = variantModule.Fields["variantIndex"].uiControlEditor as UI_VariantSelector;
					uI_VariantSelector.onFieldChanged += OnVariantChanged;
				}
			}
			else if (!IsRescaled)
			{
				enabled = false;
				isEnabled = false;
			}

			if (IsRescaled)
			{
				// Note that if we fall in here, this part was LOADED from a craft file or vessel in flight.  newly created parts in the editor aren't rescaled.
				// this whole block is basically ScalePart except it doesn't move children - should we unify?
				StringBuilder infoBuilder = GetInfoBuilder();
				ScalePartTransform();
				foreach (var attachNode in part.attachNodes)
				{
					ScaleAttachNodeSize(attachNode);
				}
				ScaleAttachNodeSize(part.srfAttachNode); // does the size of the srfAttachNode even matter?
				ScaleDragCubes(currentScaleFactor);
				CallHandlers(1.0f, infoBuilder); // TODO: is 1.0 correct here?  most likely...because everything else in the part should have already been scaled
				// TODO: this may not be the right place to do this, because other modules might not be fully set up yet...should this move to OnStartFinished?
				CalculateCostAndMass(false);
				FinalizeStats(infoBuilder);
			}
			else
			{
				extraCost = 0;
				extraMass = 0;
				SetStatsLabel("");
			}

			// scale IVA overlay
			if (HighLogic.LoadedSceneIsFlight && enabled && (part.internalModel != null))
			{
				_savedIvaScale = part.internalModel.transform.localScale * currentScaleFactor;
				part.internalModel.transform.localScale = _savedIvaScale;
			}
		}

		public override void OnStartFinished(StartState state)
		{
			base.OnStartFinished(state);

			if (!HighLogic.LoadedSceneIsFlight) return;

			float scale = currentScaleFactor;

			foreach (var fxGroup in part.fxGroups)
			{
				for (int i = 0; i < fxGroup.fxEmittersNewSystem.Count; i++)
				{
					var particleSystem = fxGroup.fxEmittersNewSystem[i];

					if (particleSystem.main.scalingMode == ParticleSystemScalingMode.Local)
					{
						particleSystem.transform.localScale *= scale;
					}
				}
				// TODO: lights?
			}

			foreach (var effectList in part.effects.effectList.Values)
			{
				foreach (var effect in effectList)
				{
					if (effect is PrefabParticleFX particleEffect)
					{
						if (particleEffect.emitter.main.scalingMode == ParticleSystemScalingMode.Local)
						{
							particleEffect.emitter.transform.localScale = particleEffect.localScale * scale;
						}
					}
					else if (effect is ModelMultiParticleFX modelMultiParticleFX)
					{
						foreach (var emitter in modelMultiParticleFX.emitters)
						{
							if (emitter.ps.main.scalingMode == ParticleSystemScalingMode.Local)
							{
								emitter.ps.transform.localScale = modelMultiParticleFX.localScale * scale;
							}
						}
					}
				}
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

#region editor event handlers

		public override void OnWillBeCopied(bool asSymCounterpart)
		{
			if (!asSymCounterpart) return;

			if (EditorLogic.fetch.selectedPart == part)
			{
				TweakScaleEditorLogic.Instance.HandleMatchNodeSize(this);
			}
		}

		void OnEditorShipModified(ShipConstruct ship)
		{
			if (part.CrewCapacity < _prefabPart.CrewCapacity)
			{
				CrewManifestHandler.UpdateCrewManifest(part);
			}
		}

		private void OnVariantChanged(BaseField field, object arg2)
		{
			bool anyChanged = CalculateCostAndMass();
			foreach (var symmetryPart in part.symmetryCounterparts)
			{
				anyChanged = symmetryPart.FindModuleImplementing<TweakScale>().CalculateCostAndMass() || anyChanged;
			}

			if (anyChanged)
			{
				GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
			}
		}
#endregion

#region Stats handling

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

		private void SetStatsLabel(string text)
		{
			if (text == null || text.Length == 0)
			{
				guiStatsText = "";
			}
			else
			{
				guiStatsText = text;
			}

			UpdateStatsVisibility();
		}

		void UpdateStatsVisibility()
		{
			var statsField = Fields[nameof(guiStatsText)];
			Fields[nameof(guiStatsText)].guiActiveEditor = showStats && guiStatsText.Length > 0;

			statsField.uiControlEditor?.partActionItem?.UpdateItem();

			if (part.PartActionWindow != null && !part.PartActionWindow.isActiveAndEnabled)
			{
				part.PartActionWindow.displayDirty = true;
			}
			else
			{
				// make sure the group updates size as well
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

		void FinalizeStats(StringBuilder infoBuilder)
		{
			if (IsRescaled && infoBuilder != null)
			{
				infoBuilder.AppendFormat("\nCost: {0:+0;-#}", guiAddedCost);
				infoBuilder.AppendFormat("\nMass: {0:+0.0##;-0.0##}", extraMass);

				SetStatsLabel(infoBuilder.ToString());
			}
			else
			{
				SetStatsLabel("");
			}
		}
		#endregion

#region editor options
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

		private void OnShowKeyBindingsModified(object arg1)
		{
			TweakScaleEditorLogic.Instance.ShowKeyBinds = showKeyBindings;
		}

		static bool UpdateLocalSetting(ref bool localSetting, bool globalSetting, BaseField field)
		{
			if (localSetting != globalSetting)
			{
				localSetting = globalSetting;
				field?.uiControlEditor?.partActionItem?.UpdateItem();
				return true;
			}

			return false;
		}

		void Update()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				// copy from the global setting into our KSPField (might want to do this to all TweakScale modules when changing the setting, so we can get rid of the update function?)
				// TODO: have the TweakScaleEditorLogic have events we can register for, that just call UpdateItem as appropriate
				// or is there a way to only do this when the PAW is opened, to avoid a performance hit on high part count ships when changing the setting
				UpdateLocalSetting(ref scaleChildren, TweakScaleEditorLogic.Instance.ScaleChildren, Fields[nameof(scaleChildren)]);
				UpdateLocalSetting(ref matchNodeSize, TweakScaleEditorLogic.Instance.MatchNodeSize, Fields[nameof(matchNodeSize)]);
				UpdateLocalSetting(ref showKeyBindings, TweakScaleEditorLogic.Instance.ShowKeyBinds, Fields[nameof(showKeyBindings)]);
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
#endregion editor options

#region scaling logic
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

		float GetScaleFactorFromGUI()
		{
			float guiScale = isFreeScale ? guiScaleValue : ScaleFactors[guiScaleNameIndex];
			return guiScale / guiDefaultScale;
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
			// resources seems like another case - other mods can change what resources are in the part, so you'd need to find some way to get the baseline amounts
			float relativeScaleFactor = newScaleFactor / currentScaleFactor;
			currentScaleFactor = newScaleFactor;

			if (scaleChildren && relativeScaleFactor != 1)
			{
				ChainScale(relativeScaleFactor);
			}

			StringBuilder infoBuilder = GetInfoBuilder();

			ScalePart(relativeScaleFactor);
			CallHandlers(relativeScaleFactor, infoBuilder);
			CalculateCostAndMass(false);
			FinalizeStats(infoBuilder);
		}

		void CallHandlers(float relativeScaleFactor, StringBuilder infoBuilder)
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
		}

		// modules that understand scaling themselves (or more generally: apply modifiers that shouldn't be scaled) should be excluded from cost/mass adjustments
		// TODO: can we turn these into Type objects in order to better support inheritance?
		static HashSet<string> x_modulesToExcludeForDryStats = new string[]
		{
			"ModuleB9PartSwitch",
			"TweakScale",
			"ModuleInventoryPart",
			"ModuleFuelTanks",
			"InterstellarFuelSwitch", // implements IRescalable
			"ModuleSwitchableTank", // ConfigurableContainers, understands scaling
			// "ModuleTankManager", // ConfigurableContainers: the tank manager's cost modifier actually IS an inherent cost - it figures out what the true dry cost of the part should be assuming the tanks are CC ones
		}.ToHashSet();

		static internal void GetPartResourceCosts(Part part, out double amountCost, out double capacityCost)
		{
			amountCost = 0;
			capacityCost = 0;

			foreach (var partResource in part.Resources)
			{
				amountCost += partResource.amount * partResource.info.unitCost;
				capacityCost += partResource.maxAmount * partResource.info.unitCost;
			}
		}

		internal bool CalculateCostAndMass(bool refreshStats = true)
		{
			// we never recalculate this in flight, because everything should be stored in the persistent file
			if (HighLogic.LoadedSceneIsFlight) return false;


			float oldExtraCost = extraCost;
			float oldExtraMass = extraMass;

			// The whole point of this function is to calculate extraCost and extraMass so that the final cost and mass of the part come out correctly.
			// extraMass and extraCost are the values returned from GetModuleMass and GetModuleCost, and the stock game adds these values to the value for the part.
			// Cost is tricky because of how resources are treated; the prefab cost *includes* the cost of the resources in the part
			// and there are at least 5 different fuel switch mods (B9PartSwitch, ModularFuelTanks, FSFuelSwitch, InterstellarFuelSwitch, and SimpleFuelSwitch)
			// Further, some modules can apply modifiers that may or may not be relevant for scaling.  For example ModulePartVariants should be treated as if
			// it's changing the prefab's dry mass and cost, but ModuleInventoryPart should not (because items in inventory dont' get more expensive when you scale the part up)
			// And then some modded modules take scaling into account on their own in their cost/mass modifier functions.

			// the basic approach here is:
			// 1. we need to calculate the final desired cost of the part, applying cost modifiers from mods and resource costs as appropriate
			// 2. then, knowing the equation that the stock code uses to calculate the final cost of the part, apply algebra to figure out what extraCost must be

			// The stock equation is:
			//   final_cost = partInfo.cost + sum_of_cost_modifiers - resource_capacity_cost + resource_amount_cost
			// note that resource capacity and amount costs use the *current* values, not the ones from the prefab - i.e. they are affected by fuel switching and scaling
			// separating sum_of_cost_modifiers into 3 groups: extraCost (tweakScale), inherent (things that logically modify the "prefab" cost, e.g. ModulePartVariants), additional (stuff like inventory):
			//   final_cost = partInfo.cost + extraCost + inherent_cost_modifiers + additional_cost_modifiers - resource_capacity_cost + resource_amount_cost
			// And we usually calculate the final DRY cost, ignoring resources and those "additional" cost modifiers:
			//   final_dry_cost = partInfo.cost + extraCost + inherent_cost_modifiers - resource_capacity_cost
			// and rearranging:
			//   extraCost = final_dry_cost - (partInfo.cost + inherent_cost_modifiers - resource_capacity_cost)
			//   extraCost = final_dry_cost - partInfo.cost - inherent_cost_modifiers + resource_capacity_cost
			// and then we also have:
			//   final_dry_cost = (unscaledDryCost + inherent_cost_modifiers) * cost_scale
			// note that unscaledDryCost is usually the partInfo.cost minus the price of resources stored in the prefab, but could be different based on mods

			if (IsRescaled)
			{
				double inherentCostModifiers = 0;
				float mass = part.partInfo.partPrefab.mass;

				// stuff like ModulePartvariants can effectively change the dry cost and mass
				foreach (var module in part.modules.modules)
				{
					if (x_modulesToExcludeForDryStats.Contains(module.ClassName)) continue;

					if (module is IPartCostModifier costModifier)
					{
						// should this use unscaledCost or the prefrab cost (i.e. including resources)?  I don't think I've come across a module that uses it, so it might not matter
						inherentCostModifiers += costModifier.GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT);
					}

					if (module is IPartMassModifier massModifier)
					{
						mass += massModifier.GetModuleMass(part.partInfo.partPrefab.mass, ModifierStagingSituation.CURRENT);
					}
				}

				float costExponent = ScaleExponents.getDryCostExponent(ScaleType.Exponents);
				float costScale = Mathf.Pow(currentScaleFactor, costExponent);
				double finalDryCost = costScale * (unscaledDryCost + inherentCostModifiers);

				extraCost = (float)(finalDryCost - part.partInfo.cost - inherentCostModifiers + unscaledResourceCapacityCost * costScale);
				guiAddedCost = (float)(extraCost - unscaledResourceCapacityCost * (costScale - 1));

				if (scaleMass)
				{
					float dryMassScale = GetDryMassScale();
					float newMass = dryMassScale * mass;
					extraMass = newMass - mass;
					part.UpdateMass();
				}
				else
				{
					extraMass = 0;
				}
			}
			else
			{
				extraCost = 0;
				extraMass = 0;
			}

			if (oldExtraCost != extraCost || oldExtraMass != extraMass)
			{
				// horrible hack to refresh the cost and mass fields in the stats...would anything else in here ever need to change maybe?  Should we just rebuild the whole thing?
				if (refreshStats && IsRescaled && HighLogic.LoadedSceneIsEditor)
				{
					StringBuilder infoBuilder = GetInfoBuilder();
					int cutoffIndex = guiStatsText.LastIndexOf("\nCost:");
					if (cutoffIndex >= 0)
					{
						infoBuilder.Append(guiStatsText);
						infoBuilder.Length = cutoffIndex;
					}
				}

				return true;
			}

			return false;
		}

		// ADVANCED USE ONLY!
		// this applies the scaling exponents ONLY to the part resources.  It can be used after some fuelswitch mod that is not tweakscale-aware changes the resources in the part (to unscaled values)
		internal void ScalePartResources()
		{
			if (!IsRescaled) return;

			ScaleExponents resourceExponents = ScaleType.Exponents["Part"]?.GetChild("Resources");

			if (resourceExponents == null) return;

			// I had initially wanted to construct a full ScaleExponents containing just the resource exponents and use ScaleExponents.UpdateObject
			// but ScaleExponents is difficult to reconstruct and poke at, so we'll just do this manually...

			var scalingMode = resourceExponents._exponents["maxAmount"];
			double resourcesScale = Math.Pow(currentScaleFactor, scalingMode.ExponentValue);

			foreach (var partResource in part.Resources)
			{
				partResource.maxAmount *= resourcesScale;
				partResource.amount *= resourcesScale;
			}

			// TODO: somehow update the stats text in the PAW?

			CalculateCostAndMass();
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
			float originalNodeSize = GetUnscaledAttachNodeSize(node.id);

			float tmpNodeSize = Mathf.Max(originalNodeSize, 0.5f);
			node.size = (int)(tmpNodeSize * guiScaleValue / guiDefaultScale + 0.49);
			node.size = Math.Max(0, node.size);

			if (node.icon != null)
			{
				node.icon.transform.localScale = Vector3.one * node.radius * ((node.size == 0) ? node.size + 0.5f : node.size);
			}
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
			part.DragCubes.SetDragWeights();
		}

		Part GetAttachedPart(AttachNode node)
		{
			if (node.attachedPart != null) return node.attachedPart;
			var editorAttachment = EditorLogic.fetch?.attachment;
			if (editorAttachment != null && editorAttachment.callerPartNode == node)
			{
				return editorAttachment.potentialParent;
			}
			return null;
		}

		internal void MoveNode(AttachNode node)
		{
			var oldPosition = node.position;

			AttachNodeInfo unscaledNodeInfo = unscaledAttachNodes[node.id];

			node.originalPosition = node.position = unscaledNodeInfo.position * currentScaleFactor;

			Part attachedPart = GetAttachedPart(node);

			if (attachedPart != null)
			{
				var deltaPos = node.position - oldPosition;

				// If this node connects to our parent part, then *we* need to move (note that potentialParent == parent if the part is *actually* attached)
				if (attachedPart == part.potentialParent)
				{
					part.transform.Translate(-deltaPos, part.transform);
				}
				// otherwise the child object needs to move
				else
				{
					attachedPart.transform.Translate(deltaPos, part.transform);
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

				b.SetScaleFactor(b.currentScaleFactor * relativeScaleFactor);
			}
		}

#endregion scaling logic

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

			string groupTitle = guiGroupDisplayName + " " + GetScaleString();
			if (part.PartActionWindow.parameterGroups.TryGetValue(guiGroupName, out var group))
			{
				group.groupHeader.text = groupTitle;
			}

			foreach (var item in part.PartActionWindow.listItems)
			{
				if (includeScaleControls || (item != scaleValueItem && item != scaleNameIndexItem))
				{
					item.UpdateItem();
				}
			}
		}

		public float GetDryMassScale()
		{
			float massExponent = ScaleExponents.getDryMassExponent(ScaleType.Exponents);
			return Mathf.Pow(currentScaleFactor, massExponent);
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

		string IModuleInfo.GetModuleTitle()
		{
			// HACK: compiling module infos is one of the last things that happens when compiling the prefab.
			// So we hook in here to do final loading after all the part modules, attach nodes, etc have been set up
			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				OnLoadFinalize();
			}

			return moduleName;
		}

		string IModuleInfo.GetInfo()
		{
			return "Scale Type: " + ScaleType.Name;

			// TODO: the rest of this is kinda useful for debugging, but not very well polished for general users
#if false
			x_infoBuilder.Clear();
			x_infoBuilder.AppendFormat("Scale Type: {0}\n", ScaleType.Name);

			foreach (var exponentSet in ScaleType.Exponents)
			{
				// TODO: this won't handle module inheritance correctly
				if (exponentSet.Key == "Part" || part.modules.Contains(exponentSet.Key))
				{
					x_infoBuilder.AppendFormat("{0}:\n", exponentSet.Key);

					foreach (var child in exponentSet.Value._exponents)
					{
						x_infoBuilder.AppendFormat("{0}: {1}\n", child.Key, child.Value.ExponentValue);
					}

					// TODO: nested exponents?
				}
			}

			return x_infoBuilder.ToString();
#endif
		}

		Callback<Rect> IModuleInfo.GetDrawModulePanelCallback()
		{
			return null;
		}

		string IModuleInfo.GetPrimaryField()
		{
			return null;
		}

		internal string GetScaleString()
		{
			if (isFreeScale)
			{
				// TODO: cache this to reduce garbage?
				return string.Format("{0:0.###}{1}", guiScaleValue, ScaleType.Suffix);
			}
			else
			{
				return scaleNames[guiScaleNameIndex];
			}
		}

		void GetIntervalInfo(ScaleFactorSnapMode snapMode, float scaleValue, bool preferLower, out float min, out float max, out float step)
		{
			int intervalIndex = Tools.FindIntervalIndex(scaleValue, ScaleFactors, preferLower);

			min = ScaleFactors[intervalIndex];
			max = ScaleFactors[intervalIndex + 1];
			step = ScaleType.IncrementSlide[intervalIndex];
			if (step > 0f)
			{
				if (snapMode == ScaleFactorSnapMode.CoarseSteps)
				{
					int numSteps = (int)Mathf.Round((max - min) / step);

					if (numSteps % 10 == 0)
					{
						step = (max - min) / 10;
					}
					else if (numSteps % 5 == 0)
					{
						step = (max - min) / 5;
					}
					else if (numSteps % 4 == 0)
					{
						step = (max - min) / 4;
					}
					else if (numSteps % 2 == 0)
					{
						step = (max - min) / 2;
					}
				}
			}
		}

		internal void SetScaleFactor(float scaleFactor, ScaleFactorSnapMode snapMode = ScaleFactorSnapMode.None)
		{
			float newGuiScaleValue = scaleFactor * guiDefaultScale;

			if (ScaleFactors.Length > 0)
			{
				if (isFreeScale)
				{
					newGuiScaleValue = Mathf.Clamp(newGuiScaleValue, ScaleFactors.First(), ScaleFactors.Last());

					if (snapMode != ScaleFactorSnapMode.None && ScaleFactors.Length >= 2)
					{
						bool decreasing = scaleFactor < currentScaleFactor;
						GetIntervalInfo(snapMode, newGuiScaleValue, decreasing, out float min, out float max, out float step);

						newGuiScaleValue = min + (float)Math.Round((newGuiScaleValue - min) / step) * step;

						if (Mathf.Approximately(newGuiScaleValue, max))
						{
							newGuiScaleValue = max;
						}
					}
				}
				else
				{
					guiScaleNameIndex = Tools.ClosestIndex(newGuiScaleValue, ScaleFactors);
					newGuiScaleValue = ScaleFactors[guiScaleNameIndex];
				}
			}

			if (newGuiScaleValue == guiScaleValue) return;

			guiScaleValue = newGuiScaleValue;
			OnTweakScaleChanged(GetScaleFactorFromGUI());
			UpdatePartActionWindow(true);
		}

		internal void IncrementScaleFactor(int incrementDirection, ScaleFactorSnapMode snapMode)
		{
			if (isFreeScale)
			{
				float incrementAmount = 0.01f * incrementDirection;

				if (snapMode != ScaleFactorSnapMode.None && ScaleFactors.Length >= 2)
				{
					bool decreasing = incrementDirection < 0;
					GetIntervalInfo(snapMode, guiScaleValue, decreasing, out float min, out float max, out float step);
					incrementAmount = step * incrementDirection / guiDefaultScale;
				}

				SetScaleFactor(currentScaleFactor + incrementAmount, snapMode);
			}
			else
			{
				JumpScaleFactor(incrementDirection);
			}
		}

		internal void JumpScaleFactor(int jumpDirection)
		{
			// if we're in the middle of an interval and jumpDirection is negative, we want to just go to the current interval
			// but if it's right on the boundary, go to the previous one (which will be returned from FindIntervalIndex by passing true as preferLower)
			int intervalIndex = Tools.FindIntervalIndex(guiScaleValue, ScaleFactors, jumpDirection < 0);
			if (jumpDirection > 0)
			{
				intervalIndex++;
			}

			intervalIndex = Mathf.Clamp(intervalIndex, 0, ScaleFactors.Length - 1);

			SetScaleFactor(ScaleFactors[intervalIndex] / guiDefaultScale);
		}
	}

	internal enum ScaleFactorSnapMode
	{
		None,
		FineSteps,
		CoarseSteps,
	}
}
