using Expansions.Serenity;
using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	internal class TweakScaleEditorLogic : SingletonBehavior<TweakScaleEditorLogic>
	{
		PluginConfiguration _config;
		HotkeyManager _hotkeyManager;

		public KeyCode IncreaseScaleKey { get; private set; } = KeyCode.W;
		public KeyCode DecreaseScaleKey { get; private set; } = KeyCode.S;
		public KeyCode NextScaleIntervalKey { get; private set; } = KeyCode.D;
		public KeyCode PrevScaleIntervalKey { get; private set; } = KeyCode.A;
		public KeyCode ScaleModeKey { get; private set; } = KeyCode.Alpha5;

		public Hotkeyable ScaleChildren { get; private set; }
		public Hotkeyable MatchNodeSize { get; private set; }

		bool _showStats = false;
		public bool ShowStats
		{
			get { return _showStats; }
			set
			{
				if (_showStats != value)
				{
					_showStats = value;
					_config.SetValue("Show Stats", _showStats);
					SaveConfig();
				}
			}
		}

		void Start()
		{
			_config = PluginConfiguration.CreateForType<TweakScale>();
			try
			{
				_config.load();
			}
			catch (Exception ex)
			{
				Tools.LogException(ex);
			}

			_hotkeyManager = new HotkeyManager(_config);

			IncreaseScaleKey = _config.GetValue(nameof(IncreaseScaleKey), IncreaseScaleKey);
			DecreaseScaleKey = _config.GetValue(nameof(DecreaseScaleKey), DecreaseScaleKey);
			NextScaleIntervalKey = _config.GetValue(nameof(NextScaleIntervalKey), NextScaleIntervalKey);
			PrevScaleIntervalKey = _config.GetValue(nameof(PrevScaleIntervalKey), PrevScaleIntervalKey);
			ScaleModeKey = _config.GetValue(nameof(ScaleModeKey), ScaleModeKey);

			ScaleChildren = _hotkeyManager.AddHotkey("Scale Children", KeyCode.LeftControl, new[] { KeyCode.K }, true);
			MatchNodeSize = _hotkeyManager.AddHotkey("Match Node Size", KeyCode.LeftControl, new[] { KeyCode.M }, true);

			_showStats = _config.GetValue("Show Stats", _showStats);

			SaveConfig();

			GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);

			EditorLogic.fetch.toolsUI.gameObject.AddComponent<ConstructionModeScale>();
		}

		void OnDestroy()
		{
			GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
		}

		void Update()
		{
			if (EditorLogic.fetch.AnyTextFieldHasFocus() || DeltaVApp.AnyTextFieldHasFocus() || RoboticControllerManager.AnyWindowTextFieldHasFocus())
			{
				return;
			}

			if (_hotkeyManager.Update())
			{
				SaveConfig();
			}
		}

		void SaveConfig()
		{
			try
			{
				_config.save();
			}
			catch (Exception ex)
			{
				Tools.LogException(ex);
			}
		}

		float partPreviousScale = 1.0f;
		Vector3 selGrabOffset = Vector3.zero;
		bool doneAttach = false;

		private void OnEditorPartEvent(ConstructionEventType eventType, Part selectedPart)
		{
			var selectedTweakScaleModule = selectedPart.FindModuleImplementing<TweakScale>();
			if (selectedTweakScaleModule == null) return;

			switch (eventType)
			{
				case ConstructionEventType.PartCreated:
				case ConstructionEventType.PartPicked:
				case ConstructionEventType.PartCopied:
				case ConstructionEventType.PartDetached:
					partPreviousScale = selectedTweakScaleModule.currentScaleFactor;
					doneAttach = false;
					break;
				case ConstructionEventType.PartDragging:
					HandleMatchNodeSize(selectedTweakScaleModule);
					break;
			}
		}

		float GetAttachNodeDiameter(Part part, string attachNodeId)
		{
			var tweakScaleModule = part.FindModuleImplementing<TweakScale>();

			if (tweakScaleModule != null &&
				tweakScaleModule.TryGetUnscaledAttachNode(attachNodeId, out var attachNodeInfo))
			{
				return attachNodeInfo.diameter * tweakScaleModule.currentScaleFactor;
			}
			else
			{
				var attachNode = part.FindAttachNode(attachNodeId);
				return Tools.AttachNodeSizeDiameter(attachNode.size);
			}
		}

		void HandleMatchNodeSize(TweakScale selectedTweakScaleModule)
		{
			Part selectedPart = selectedTweakScaleModule.part;
			Attachment attachment = EditorLogic.fetch.attachment;

			if (MatchNodeSize && selectedPart.potentialParent != null && attachment.mode == AttachModes.STACK)
			{
				float parentAttachNodeDiameter = GetAttachNodeDiameter(selectedPart.potentialParent, attachment.otherPartNode.id);

				if (selectedTweakScaleModule.TryGetUnscaledAttachNode(attachment.callerPartNode.id, out var selectedNode))
				{
					if (!doneAttach)
					{
						float necessaryScale = parentAttachNodeDiameter / selectedNode.diameter;

						Vector3 oldNodeWorldPosition = selectedPart.transform.rotation * attachment.callerPartNode.position;
						selectedTweakScaleModule.SetScaleFactor(necessaryScale);
						Vector3 newNodeWorldPosition = selectedPart.transform.rotation * attachment.callerPartNode.position;

						selGrabOffset = newNodeWorldPosition - oldNodeWorldPosition;
						EditorLogic.fetch.selPartGrabOffset += selGrabOffset;
						doneAttach = true;
					}

					var message = $"Node Size: {parentAttachNodeDiameter.ToString("0.0##")}m\n" +
						MatchNodeSize.GetKeybindPrompt();

					ScreenMessages.PostScreenMessage(message, 0f, ScreenMessageStyle.LOWER_CENTER);
				}
			}
			else if (doneAttach)
			{
				selectedTweakScaleModule.SetScaleFactor(partPreviousScale);
				EditorLogic.fetch.selPartGrabOffset -= selGrabOffset;
				selGrabOffset = Vector3.zero;
				doneAttach = false;
			}
		}
	}
}
