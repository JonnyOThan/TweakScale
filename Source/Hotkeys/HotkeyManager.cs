using System.Collections.Generic;
using KSP.IO;
using UnityEngine;

namespace TweakScale
{
	internal class HotkeyManager
	{
		private readonly Dictionary<string, Hotkeyable> _hotkeys = new Dictionary<string, Hotkeyable>();
		private readonly PluginConfiguration _config;

		public HotkeyManager(PluginConfiguration config)
		{
			_config = config;
		}

		public bool Update()
		{
			bool anyChanged = false;
			foreach (var key in _hotkeys.Values)
			{
				anyChanged = key.Update() || anyChanged;
			}

			return anyChanged;
		}

		public Hotkeyable AddHotkey(string hotkeyName, KeyCode tempDisableDefault, ICollection<KeyCode> toggleDefault, bool state)
		{
			if (_hotkeys.ContainsKey(hotkeyName))
				return _hotkeys[hotkeyName];
			return _hotkeys[hotkeyName] = new Hotkeyable(hotkeyName, tempDisableDefault, toggleDefault, state, _config);
		}
	}
}
