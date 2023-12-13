using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Targeting;

namespace TweakScale
{
	public class Hotkey
	{
		private readonly Dictionary<KeyCode, bool> _modifiers = new Dictionary<KeyCode, bool>();
		private KeyCode _trigger = KeyCode.None;
		private readonly string _name;

		// Creates a hotkey - the last key in the collection is the trigger key, previous keys are modifiers
		public Hotkey(string name, ICollection<KeyCode> keys, PluginConfiguration config)
		{
			_name = name;
			if (keys.Count == 0)
			{
				Tools.LogWarning("No keys for hotkey {0}. Need at least 1 key in defaultKey parameter, got none.", _name);
			}
			else
			{
				SetKeys(keys);
			}
			Load(config);
		}

		public void Load(PluginConfiguration config)
		{
			try
			{
				var keysAsString = GetKeysAsString();
				keysAsString = config.GetValue(_name, keysAsString); // note that GetValue will store the value in the config if it doesn't exist
				if (!string.IsNullOrEmpty(keysAsString))
				{
					SetKeys(ParseString(keysAsString));
				}
			}
			catch (Exception ex)
			{
				Tools.LogException(ex);
			}
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

		string GetKeysAsString()
		{
			var activeModifiers = _modifiers.Where(kv => kv.Value).Select(kv => kv.Key);
			var result = string.Join("+", activeModifiers);
			return result == "" ? _trigger.ToString() : result + "+" + _trigger;
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
