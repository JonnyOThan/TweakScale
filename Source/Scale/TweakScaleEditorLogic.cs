﻿using Expansions.Serenity;
using KSP.IO;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TweakScale
{
	class ToggleOption
	{
		bool _value;
		PluginConfiguration _config;
		string _name;

		public ToggleOption(string name, bool defaultValue, PluginConfiguration config)
		{
			_name = name;
			_config = config;
			_value = config.GetValue(name, defaultValue);
		}

		public static implicit operator bool(ToggleOption opt) => opt._value;

		public bool Set(bool newValue)
		{
			if (_value != newValue)
			{
				_value = newValue;
				_config.SetValue(_name, _value);
				return true;
			}
			return false;
		}
	}

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

		ToggleOption _showStats;
		public bool ShowStats
		{
			get => _showStats;
			set => SetToggleOption(_showStats, value);
		}

		ToggleOption _showKeyBinds;
		public bool ShowKeyBinds
		{
			get => _showKeyBinds;
			set => SetToggleOption(_showKeyBinds, value);
		}

		static string GetFilePathFor(Type T, string file)
		{
			string assemblyDataPath = AssemblyLoader.loadedAssemblies.GetByAssembly(T.Assembly).dataPath;
			string text = Path.Combine(KSP.IO.IOUtils.PluginRootPath, assemblyDataPath);
			file = Path.GetFileName(file);
			return Path.Combine(text, file);
		}

		void Start()
		{
			try
			{
				// The stock CreateForType calls GetTypes on all loaded assemblies, which will break horribly if any of them throw exceptions
				//_config = PluginConfiguration.CreateForType<TweakScale>();
				_config = new PluginConfiguration(GetFilePathFor(typeof(TweakScale), "config.xml"));
				_config.load();
			}
			catch (Exception ex)
			{
				Tools.LogException(ex);
				_config = new PluginConfiguration(Path.Combine(Path.GetDirectoryName(typeof(TweakScale).Assembly.Location), "config.xml")); // strange, but at least it won't crash
			}

			_hotkeyManager = new HotkeyManager(_config);

			IncreaseScaleKey = _config.GetValue(nameof(IncreaseScaleKey), IncreaseScaleKey);
			DecreaseScaleKey = _config.GetValue(nameof(DecreaseScaleKey), DecreaseScaleKey);
			NextScaleIntervalKey = _config.GetValue(nameof(NextScaleIntervalKey), NextScaleIntervalKey);
			PrevScaleIntervalKey = _config.GetValue(nameof(PrevScaleIntervalKey), PrevScaleIntervalKey);
			ScaleModeKey = _config.GetValue(nameof(ScaleModeKey), ScaleModeKey);

			ScaleChildren = _hotkeyManager.AddHotkey("Scale Children", KeyCode.LeftControl, new[] { KeyCode.K }, true);
			MatchNodeSize = _hotkeyManager.AddHotkey("Match Node Size", KeyCode.LeftControl, new[] { KeyCode.M }, true);

			_showStats = new ToggleOption("Show Stats", false, _config);
			_showKeyBinds = new ToggleOption("Show KeyBinds", true, _config);

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

			_hotkeyManager.Update();
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

		void SetToggleOption(ToggleOption option, bool value)
		{
			if (option.Set(value))
			{
				SaveConfig();
			}
		}

		float partPreviousScale = 1.0f;
		Vector3 selGrabOffset = Vector3.zero;
		bool doneAttach = false;

		public bool MatchNodeSizeInProgress => doneAttach;

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
				case ConstructionEventType.PartAttached:
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

		internal void HandleMatchNodeSize(TweakScale selectedTweakScaleModule)
		{
			Part selectedPart = selectedTweakScaleModule.part;
			Attachment attachment = EditorLogic.fetch.attachment;

			if (MatchNodeSize && selectedPart.potentialParent != null && attachment.mode == AttachModes.STACK)
			{
				float parentAttachNodeDiameter = GetAttachNodeDiameter(selectedPart.potentialParent, attachment.otherPartNode.id);

				if (selectedTweakScaleModule.TryGetUnscaledAttachNode(attachment.callerPartNode.id, out var selectedNode))
				{
					float necessaryScale = parentAttachNodeDiameter / selectedNode.diameter;

					if (!doneAttach)
					{
						Vector3 oldNodeWorldPosition = selectedPart.transform.rotation * attachment.callerPartNode.position;
						SetSelectedPartScale(selectedTweakScaleModule, necessaryScale);
						Vector3 newNodeWorldPosition = selectedPart.transform.rotation * attachment.callerPartNode.position;

						selGrabOffset = newNodeWorldPosition - oldNodeWorldPosition;
						EditorLogic.fetch.selPartGrabOffset += selGrabOffset;
						doneAttach = true;
					}

					var message = $"Match Node Size: {parentAttachNodeDiameter.ToString("0.0##")}m";

					if (ShowKeyBinds && !Mathf.Approximately(necessaryScale, partPreviousScale))
					{
						message += "\n" + MatchNodeSize.GetKeybindPrompt();
					}

					ScreenMessages.PostScreenMessage(message, 0f, ScreenMessageStyle.LOWER_CENTER);
				}
			}
			else if (doneAttach)
			{
				SetSelectedPartScale(selectedTweakScaleModule, partPreviousScale);
				EditorLogic.fetch.selPartGrabOffset -= selGrabOffset;
				selGrabOffset = Vector3.zero;
				doneAttach = false;
			}

			if (ShowKeyBinds && !MatchNodeSize.BaseState && !MatchNodeSize.IsTempToggled && selectedPart.potentialParent != null && attachment.mode == AttachModes.STACK)
			{
				float parentAttachNodeDiameter = GetAttachNodeDiameter(selectedPart.potentialParent, attachment.otherPartNode.id);

				if (selectedTweakScaleModule.TryGetUnscaledAttachNode(attachment.callerPartNode.id, out var selectedNode))
				{
					if (!Mathf.Approximately(parentAttachNodeDiameter, selectedNode.diameter * selectedTweakScaleModule.currentScaleFactor))
					{
						ScreenMessages.PostScreenMessage(MatchNodeSize.GetKeybindPrompt(), 0f, ScreenMessageStyle.LOWER_CENTER);
					}
				}
			}
		}

		public void ResetSelectedPartScale(TweakScale tweakScaleModule)
		{
			SetSelectedPartScale(tweakScaleModule, 1.0f);
			partPreviousScale = 1.0f;
		}

		void SetSelectedPartScale(TweakScale tweakScaleModule, float scaleFactor)
		{
			tweakScaleModule.SetScaleFactorRecursively(scaleFactor);

			foreach (var symmetricPart in tweakScaleModule.part.symmetryCounterparts)
			{
				var otherModule = symmetricPart.FindModuleImplementing<TweakScale>();
				
				otherModule.SetScaleFactorRecursively(scaleFactor);
			}
		}
	}
}
