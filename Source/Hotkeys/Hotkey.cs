using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakScale
{
	public class Hotkey
	{
		private readonly Dictionary<KeyCode, bool> _modifiers = new Dictionary<KeyCode, bool>();
		private KeyCode _trigger = KeyCode.None;
		private readonly string _name;
		private readonly PluginConfiguration _config;

		// Creates a hotkey - the last key in the collection is the trigger key, previous keys are modifiers
		public Hotkey(string name, ICollection<KeyCode> keys)
		{
			_config = HotkeyManager.Instance.Config;
			_name = name;
			if (keys.Count == 0)
			{
				Tools.LogWarning("No keys for hotkey {0}. Need at least 1 key in defaultKey parameter, got none.", _name);
			}
			else
			{
				SetKeys(keys);
			}
			Load();
		}

		public Hotkey(string name, string defaultKey)
		{
			_config = HotkeyManager.Instance.Config;
			_name = name;
			SetKeys(ParseString(defaultKey));
			Load();
		}

		public void Load()
		{
			_config.load();
			var rawNames = _config.GetValue(_name, "");
			if (!string.IsNullOrEmpty(rawNames))
			{
				SetKeys(ParseString(rawNames));
			}
			Save();
		}

		static ICollection<KeyCode> ParseString(string s)
		{
			var names = s.Split('+');
			return names.Select(keyName => (KeyCode)Enum.Parse(typeof(KeyCode), keyName, true)).ToList();
		}

		void SetKeys(ICollection<KeyCode> keys)
		{
			foreach (var modifierKey in x_modifierKeys)
			{
				_modifiers[modifierKey] = keys.Contains(modifierKey);
			}
			_trigger = keys.Last();
		}

		private void Save()
		{
			var result = "";
			foreach (var kv in _modifiers)
				if (kv.Value)
					result += kv.Key + "+";

			_config.SetValue(_name, result + _trigger);
			_config.save();
		}

		static HashSet<KeyCode> x_modifierKeys = new HashSet<KeyCode>()
		{
			KeyCode.RightShift,
			KeyCode.LeftShift,
			KeyCode.RightControl,
			KeyCode.LeftControl,
			KeyCode.RightAlt,
			KeyCode.LeftAlt,
			KeyCode.RightApple,
			KeyCode.RightCommand,
			KeyCode.LeftApple,
			KeyCode.LeftCommand, 
			KeyCode.LeftWindows, 
			KeyCode.RightWindows,
		};

		public bool IsTriggered
		{
			get
			{
				return _modifiers.All(a => Input.GetKey(a.Key) == a.Value) && Input.GetKeyDown(_trigger);
			}
		}

		public bool IsHeld()
		{
			return _modifiers.All(a => Input.GetKey(a.Key) == a.Value) && Input.GetKey(_trigger);
		}
	}
}
