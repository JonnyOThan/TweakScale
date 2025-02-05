using JetBrains.Annotations;
using Smooth.Algebraics;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TweakScale
{
	/// <summary>
	/// Various handy functions.
	/// </summary>
	public static class Tools
	{
		/// <summary>
		/// Gets the exponentValue in <paramref name="values"/> that's closest to <paramref name="x"/>.
		/// </summary>
		/// <param name="x">The exponentValue to find.</param>
		/// <param name="values">The values to look through.</param>
		/// <returns>The exponentValue in <paramref name="values"/> that's closest to <paramref name="x"/>.</returns>
		public static float Closest(float x, IEnumerable<float> values)
		{
			var minDistance = float.PositiveInfinity;
			var result = float.NaN;
			foreach (var value in values)
			{
				var tmpDistance = Math.Abs(value - x);
				if (tmpDistance < minDistance)
				{
					result = value;
					minDistance = tmpDistance;
				}
			}
			return result;
		}

		/// <summary>
		/// Finds the index of the exponentValue in <paramref name="values"/> that's closest to <paramref name="x"/>.
		/// </summary>
		/// <param name="x">The exponentValue to find.</param>
		/// <param name="values">The values to look through.</param>
		/// <returns>The index of the exponentValue in <paramref name="values"/> that's closest to <paramref name="x"/>.</returns>
		public static int ClosestIndex(float x, IEnumerable<float> values)
		{
			var minDistance = float.PositiveInfinity;
			int result = 0;
			int idx = 0;
			foreach (var value in values)
			{
				var tmpDistance = Math.Abs(value - x);
				if (tmpDistance < minDistance)
				{
					result = idx;
					minDistance = tmpDistance;
				}
				idx++;
			}
			return result;
		}

		public static int FindIntervalIndex(float value, float[] intervals, bool preferLower)
		{
			for (int index = 0; index < intervals.Length - 1; ++index)
			{
				if (value < intervals[index + 1]) return index;
				// if we're right on the boundary, the caller decides which interval
				if (Mathf.Approximately(value, intervals[index + 1]) && preferLower) return index;
			}

			return intervals.Length - 2;
		}

		private static string BuildLogString(string format, object[] args)
		{
			return "[TweakScale] " + string.Format(format, args.Select(a => a.PreFormat()).ToArray());
		}

		private static string BuildLogStringWithStack(string format, object[] args)
		{
			return BuildLogString(format, args) + Environment.NewLine + StackTraceUtility.ExtractStackTrace();
		}

		[System.Diagnostics.Conditional("DEBUG")]
		internal static void LogDebug(string format, params object[] args)
		{
			Debug.Log(BuildLogStringWithStack(format, args));
		}

		internal static void Log(string format, params object[] args)
		{
			Debug.Log(BuildLogString(format, args));
		}

		internal static void LogWarning(string format, params object[] args)
		{
#if DEBUG
			Debug.LogWarning(BuildLogStringWithStack(format, args));
#else
			Debug.LogWarning(BuildLogString(format, args));
#endif
		}

		internal static void LogError(string format, params object[] args)
		{
			// note: Logerror already includes the stack
			Debug.LogError(BuildLogString(format, args));
		}

		internal static void LogException(Exception ex, string format = "", params object[] args)
		{
			// note we don't use logerror so that the callstack doesn't get printed twice
			Debug.Log(BuildLogString(format, args));
			Debug.LogException(ex);
		}

		/// <summary>
		/// Formats certain types to make them more readable.
		/// </summary>
		/// <param name="obj">The object to format.</param>
		/// <returns>A more readable representation of <paramref name="obj"/>>.</returns>
		public static object PreFormat(this object obj)
		{
			if (obj == null)
			{
				return "null";
			}
			if (obj is IEnumerable)
			{
				if (obj.GetType().GetMethod("ToString", new Type[] { }).IsOverride())
				{
					var e = obj as IEnumerable;
					return string.Format("[{0}]", string.Join(", ", e.Cast<object>().Select(a => a.PreFormat().ToString()).ToArray()));
				}
			}
			return obj;
		}

		/// <summary>
		/// Reads destination exponentValue from the ConfigNode and magically converts it to the type you ask. Tested for float, boolean and double[]. Anything else is at your own risk.
		/// </summary>
		/// <typeparam name="T">The type to convert to. Usually inferred from <paramref name="defaultValue"/>.</typeparam>
		/// <param name="config">ScaleType node from which to read values.</param>
		/// <param name="name">Name of the ConfigNode's field.</param>
		/// <param name="defaultValue">The exponentValue to use when the ConfigNode doesn't contain what we want.</param>
		/// <returns>The exponentValue in the ConfigNode, or <paramref name="defaultValue"/> if no decent exponentValue is found there.</returns>
		public static T ConfigValue<T>(ConfigNode config, string name, T defaultValue)
		{
			if (!config.HasValue(name))
			{
				return defaultValue;
			}
			string cfgValue = config.GetValue(name);
			try
			{
				var result = ConvertEx.ChangeType<T>(cfgValue);
				return result;
			}
			catch (Exception ex)
			{
				if (ex is InvalidCastException || ex is FormatException || ex is OverflowException || ex is ArgumentNullException)
				{
					LogWarning("Failed to convert string value \"{0}\" to type {1}", cfgValue, typeof(T).Name);
					return defaultValue;
				}
				throw;
			}
		}

		/// <summary>
		/// Fetches the the comma-delimited string exponentValue by the name <paramref name="name"/> from the node <paramref name="config"/> and converts it into an array of <typeparamref name="T"/>s.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="config">The node to fetch values from.</param>
		/// <param name="name">The name of the exponentValue to fetch.</param>
		/// <param name="defaultValue">The exponentValue to return if the exponentValue does not exist, or cannot be converted to <typeparamref name="T"/>s.</param>
		/// <returns>An array containing the elements of the string exponentValue as <typeparamref name="T"/>s.</returns>
		public static T[] ConfigValue<T>(ConfigNode config, string name, T[] defaultValue)
		{
			if (!config.HasValue(name))
			{
				return defaultValue;
			}
			return ConvertString(config.GetValue(name), defaultValue);
		}

		/// <summary>
		/// Converts destination comma-delimited string into an array of <typeparamref name="T"/>s.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">A comma-delimited list of values.</param>
		/// <param name="defaultValue">The exponentValue to return if the list does not hold valid values.</param>
		/// <returns>An arra</returns>
		public static T[] ConvertString<T>(string value, T[] defaultValue)
		{
			try
			{
				return value.Split(',').Select(ConvertEx.ChangeType<T>).ToArray();
			}
			catch (Exception ex)
			{
				if (!(ex is InvalidCastException) && !(ex is FormatException) && !(ex is OverflowException) &&
					!(ex is ArgumentNullException))
					throw;
				LogWarning("Failed to convert string value \"{0}\" to type {1}", value, typeof(T).Name);
				return defaultValue;
			}
		}

		public static string ToString_rec(this object obj, int depth = 0)
		{
			if (obj == null)
				return "(null)";

			var result = new StringBuilder("(");
			var tt = obj.GetType();

			Func<object, string> fmt = a => a == null ? "(null)" : depth == 0 ? a.ToString() : a.ToString_rec();

			foreach (var field in tt.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				result.AppendFormat("{0}: {1}, ", field.Name, fmt(field.GetValue(obj)));
			}

			foreach (var field in tt.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					result.AppendFormat("{0}: {1}, ", field.Name, fmt(field.GetValue(obj, null)));
				}
				catch (Exception)
				{
				}
			}

			result.Append(")");

			return result.ToString();
		}

		static HashSet<Type> rawTypes = new HashSet<Type>()
		{
			typeof(string),
			typeof(Vector3),
			typeof(Quaternion)
		};

		public static void VisitRecursive(string name, object obj, Action<string, object, int> callback, int maxDepth = 0, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance, int currentDepth = 0)
		{
			if (obj == null)
			{
				callback(name, "null", currentDepth);
				return;
			}

			callback(name, obj, currentDepth);

			var tt = obj.GetType();

			if (maxDepth == currentDepth || tt.IsPrimitive || tt.IsEnum || rawTypes.Contains(tt))
			{
				return;
			}
			else if (obj is IList list)
			{
				for (int i = 0; i < list.Count; ++i)
				{
					VisitRecursive(i.ToString(), list[i], callback, maxDepth, flags, currentDepth + 1);
				}
			}
			else if (obj is IDictionary dict)
			{
				foreach (DictionaryEntry pair in dict)
				{
					VisitRecursive(pair.Key.ToString(), pair.Value, callback, maxDepth, flags, currentDepth + 1);
				}
			}
			// TODO: probably other collection types?
			// iterate fields and properties
			else
			{
				foreach (var field in tt.GetFields(flags))
				{
					VisitRecursive(field.Name, field.GetValue(obj), callback, maxDepth, flags, currentDepth + 1);
				}
				foreach (var property in tt.GetProperties(flags))
				{
					object value;
					try
					{
						value = property.GetValue(obj);
					}
					catch (Exception e)
					{
						Tools.LogException(e, "Error visiting property {0}.{1}", tt.Name, property.Name);
						continue;
					}

					VisitRecursive(property.Name, value, callback, maxDepth, flags, currentDepth + 1);
				}
			}
		}

		public static void CopyComponentData<FROM, TO>(FROM oldComponent, TO newComponent) where FROM : Component where TO : FROM
		{
			BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			Type sourceType = oldComponent.GetType();
			FieldInfo[] fields = sourceType.GetFields(flags);
			foreach (var field in fields)
			{
				object value = field.GetValue(oldComponent);
				field.SetValue(newComponent, value);
			}

			PropertyInfo[] properties = sourceType.GetProperties(flags);
			foreach (var property in properties)
			{
				if (property.CanWrite)
				{
					try
					{
						property.SetValue(newComponent, property.GetValue(oldComponent, null), null);
					}
					catch (Exception e)
					{
						Tools.LogException(e, "exception copying property {0}.{1}", sourceType.Name, property.Name);
					}
				}
			}
		}

		public static T CloneComponent<T>(T oldComponent, GameObject to) where T : Component
		{
			if (oldComponent != null)
			{
				var newComponent = to.AddComponent<T>();

				CopyComponentData(oldComponent, newComponent);

				return newComponent;
			}

			return null;
		}


		public static float AttachNodeSizeDiameter(float attachNodeSize)
		{
			// TODO: what about size00?
			if (attachNodeSize < 1)
			{
				return Mathf.Lerp(0.625f, 1.25f, attachNodeSize);
			}
			else
			{
				return 1.25f * attachNodeSize;
			}
		}

		public unsafe static int ToInt(this bool b)
		{
			return *(byte*)(&b);
		}
	}
}
