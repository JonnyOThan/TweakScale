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

		void Start()
		{
			ScaleChildren = HotkeyManager.Instance.AddHotkey("Scale chaining", new[] { KeyCode.LeftShift }, new[] { KeyCode.LeftControl, KeyCode.K }, false);
			MatchNodeSize = HotkeyManager.Instance.AddHotkey("Match node size", new[] { KeyCode.LeftShift }, new[] { KeyCode.LeftControl, KeyCode.M }, false);

			GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
		}

		void OnDestroy()
		{
			GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
		}

		private void OnEditorPartEvent(ConstructionEventType eventType, Part selectedPart)
		{
			if (eventType != ConstructionEventType.PartDragging) return;

			if (selectedPart.potentialParent != null && MatchNodeSize)
			{
				var selectedTweakScaleModule = selectedPart.FindModuleImplementing<TweakScale>();
				var parentTweakScaleModule = selectedPart.potentialParent.FindModuleImplementing<TweakScale>();

				if (selectedTweakScaleModule == null || parentTweakScaleModule == null) return;

				// TODO: can we analyze WHICH node is being attached to figure out the right scale?  might not be possible in all cases, but would be pretty cool for stuff like adapters which clearly have at least 2 different sizes
				float necessaryScale = parentTweakScaleModule.guiScaleValue / selectedTweakScaleModule.guiDefaultScale;

				// TODO: not totally convinced that this is the right way to set the scale on the potential part (mimicking the UI)
				var field = selectedTweakScaleModule.Fields[nameof(TweakScale.guiScaleValue)];
				field.SetValue(necessaryScale * selectedTweakScaleModule.guiDefaultScale, selectedTweakScaleModule);
			}
		}
	}
}
