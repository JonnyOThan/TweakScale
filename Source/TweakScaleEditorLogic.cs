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

		void Start()
		{
			ScaleChildren = HotkeyManager.Instance.AddHotkey("Scale chaining", new[] { KeyCode.LeftShift }, new[] { KeyCode.LeftControl, KeyCode.K }, false);
		}
	}
}
