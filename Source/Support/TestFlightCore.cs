using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TweakScale
{
	internal static class TestFlightCore
	{
		static Type tfInterface;

		static TestFlightCore()
		{
			var testFlightCoreAssemly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == "TestFlightCore");
			if (testFlightCoreAssemly == null) return;

			tfInterface = testFlightCoreAssemly.assembly.GetType("TestFlightCore.TestFlightInterface");
		}

		public static void UpdateTestFlight(TweakScale tweakScaleModule)
		{
			if (null == tfInterface) return;
			string name = "scale";
			string value = tweakScaleModule.currentScaleFactor.ToString();
			string owner = "TweakScale";

			// TODO: create a bound delegate so there's not so much reflection overhead here
			bool valueAdded = (bool)tfInterface.InvokeMember("AddInteropValue", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { tweakScaleModule.part, name, value, owner });
			Tools.Log("TF: valueAdded=" + valueAdded + ", value=" + value.ToString());
		}
	}
}
