using System.Collections.Generic;
using KSP.IO;
using UnityEngine;

namespace TweakScale
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	internal class HotkeyManager : SingletonBehavior<HotkeyManager>
	{
		private readonly Dictionary<string, Hotkeyable> _hotkeys = new Dictionary<string, Hotkeyable>();
		private /*readonly*/ PluginConfiguration _config;

		new private void Awake()
		{
			base.Awake();

			_config = PluginConfiguration.CreateForType<TweakScale>();
		}

		public PluginConfiguration Config
		{
			get
			{
				return _config;
			}
		}

		private void Update()
		{
			foreach (var key in _hotkeys.Values)
			{
				key.Update();
			}
		}

		public Hotkeyable AddHotkey(string hotkeyName, ICollection<KeyCode> tempDisableDefault, ICollection<KeyCode> toggleDefault, bool state)
		{
			if (_hotkeys.ContainsKey(hotkeyName))
				return _hotkeys[hotkeyName];
			return _hotkeys[hotkeyName] = new Hotkeyable(hotkeyName, tempDisableDefault, toggleDefault, state);
		}
	}
}
