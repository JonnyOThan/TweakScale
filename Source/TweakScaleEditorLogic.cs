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
		public Hotkeyable ScaleChildren { get; private set; }
		public Hotkeyable MatchNodeSize { get; private set; }

		bool _showStats = false;
		public bool ShowStats
		{
			get { return _showStats; }
			set
			{
				_showStats = value;
				// TODO: this should save the config, but it's currently owned by the HotkeyManager.  seems like maybe that class should go away and get merged with this one.
			}
		}
		void Start()
		{
			ScaleChildren = HotkeyManager.Instance.AddHotkey("Scale Children", new[] { KeyCode.LeftShift }, new[] { KeyCode.LeftControl, KeyCode.K }, false);
			MatchNodeSize = HotkeyManager.Instance.AddHotkey("Match Node Size", new[] { KeyCode.LeftShift }, new[] { KeyCode.LeftControl, KeyCode.M }, false);

			GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
		}

		void OnDestroy()
		{
			GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
		}

		float partPreviousScale = 1.0f;

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
				return Tools.AttachNodeSizeDiameter(attachNodeInfo.size) * tweakScaleModule.currentScaleFactor;
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
					float childNodeDiameter = Tools.AttachNodeSizeDiameter(selectedNode.size);
					float necessaryScale = parentAttachNodeDiameter / childNodeDiameter;

					selectedTweakScaleModule.SetScaleFactor(necessaryScale);
				}
			}
			else
			{
				selectedTweakScaleModule.SetScaleFactor(partPreviousScale);
			}
		}
	}
}
