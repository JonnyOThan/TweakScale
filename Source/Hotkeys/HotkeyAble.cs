using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace TweakScale
{
	class Hotkeyable
	{
		private readonly string _name;
		private readonly Hotkey _tempDisable;
		private readonly Hotkey _toggle;
		private bool _state;
		private readonly PluginConfiguration _config;

		public bool State
		{
			get
			{
				return _state && !_tempDisable.IsHeld();
			}
			set
			{
				_state = value;
			}
		}

		public Hotkeyable(string name, ICollection<KeyCode> tempDisableDefault, ICollection<KeyCode> toggleDefault, bool state, PluginConfiguration config)
		{
			_config = config;
			_name = name;
			_tempDisable = new Hotkey("Disable " + name, tempDisableDefault, config);
			_toggle = new Hotkey("Toggle " + name, toggleDefault, config);
			_state = state;
			Load();
		}

		private void Load()
		{
			bool originalState = _state;
			_state = _config.GetValue(_name, _state);
			
			Tools.Log("Hotkey: {0} old: {1} New: {2}", _name, originalState, _state);

			_config.SetValue(_name, _state);
		}

		public bool Update()
		{
			if (!_toggle.IsTriggered)
				return false;
			_state = !_state;
			ScreenMessages.PostScreenMessage(_name + (_state ? " enabled." : " disabled."), EditorLogic.fetch.modeMsg);
			_config.SetValue(_name, _state);
			return true;
		}

		public static implicit operator bool(Hotkeyable a)
		{
			return a.State;
		}
	}
}
