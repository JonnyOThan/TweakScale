using System.Collections.Generic;
using UnityEngine;
using KSP.IO;
using System;

namespace TweakScale
{
	class Hotkeyable
	{
		private readonly string _name;
		private readonly KeyCode _tempToggle;
		private readonly Hotkey _toggle;
		private bool _state;
		private readonly PluginConfiguration _config;

		public bool State
		{
			get
			{
				return _state ^ Input.GetKey(_tempToggle);
			}
			set
			{
				if (_state != value)
				{
					_state = value;
					_config.SetValue(_name, _state);
					try
					{
						_config.save();
					}
					catch (Exception ex)
					{
						Tools.LogException(ex);
					}
				}
			}
		}

		public Hotkeyable(string name, KeyCode tempDisableDefault, ICollection<KeyCode> toggleDefault, bool state, PluginConfiguration config)
		{
			_config = config;
			_name = name;
			_tempToggle = config.GetValue("ToggleTemp " + name, tempDisableDefault);
			_toggle = new Hotkey("Toggle " + name, toggleDefault, config);
			_state = config.GetValue(name, state);
		}

		public void Update()
		{
			if (!_toggle.IsTriggered)
				return;
			State = !_state;
			ScreenMessages.PostScreenMessage(_name + (_state ? " enabled." : " disabled."), EditorLogic.fetch.modeMsg);
		}

		public string GetToggleKey()
		{
			return _toggle.GetKeysAsString();
		}

		public string GetKeybindPrompt()
		{
			return $"[{GetToggleKey()}/{_tempToggle}] {(State ? "Disable" : "Enable")} {_name}";
		}

		public static implicit operator bool(Hotkeyable a)
		{
			return a.State;
		}
	}
}
