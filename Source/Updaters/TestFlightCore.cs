using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TweakScale
{
	// TODO: go check out the TestFlightCore code and see what this is actually doing.  Ideally they'd implement this kind of thing over there
	internal class TestFlightCoreUpdater : IRescalablePart
	{
		#region Static reflection setup

		static Type tfInterface;
		static MethodInfo addInteropValue_MethodInfo;

		static TestFlightCoreUpdater()
		{
			var testFlightCoreAssemly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == "TestFlightCore");
			if (testFlightCoreAssemly == null) return;

			tfInterface = testFlightCoreAssemly.assembly.GetType("TestFlightCore.TestFlightInterface");

			if (tfInterface == null)
			{
				Tools.LogError("TestFlightCore assembly was loaded, but could not find the TestFlightCore.TestFlightInterface type");
				return;
			}

			addInteropValue_MethodInfo = tfInterface.GetMethod("AddInteropValue", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static);

			if (addInteropValue_MethodInfo == null)
			{
				Tools.LogError("TestFlightCore interface found, but could not find the AddInteropValue method");
			}
		}

		#endregion

		public TestFlightCoreUpdater(Part part)
		{
			_part = part;
		}

		public void OnRescale(ScalingFactor factor)
		{
			// TODO: implement a way to avoid creating this when TestFlightCore isn't installed
			if (addInteropValue_MethodInfo == null) return;

				string name = "scale";
			string value = factor.absolute.ToString();
			string owner = "TweakScale";

			// TODO: create a bound delegate so there's not so much reflection overhead here
			bool valueAdded = (bool)addInteropValue_MethodInfo.Invoke(null, new object[] { _part, name, value, owner });
			Tools.Log("TF: valueAdded=" + valueAdded + ", value=" + value.ToString());
		}

		Part _part;
	}
}
