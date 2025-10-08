using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.RescalableHandlers
{
	// This class is necessary because ModuleSystemHeatFissionReactor's inputs and outputs lists are protected, and therefore do not get copied to the instance of the module in the normal way.
	// SystemHeat works around this by re-loading these lists in the Start method, but that is too late!  TweakScale does its thing in OnStart, which is called when the part is being created.
	// Start doesn't get called until later, directly by Unity.
	// https://github.com/post-kerbin-mining-corporation/SystemHeat/blob/6fedc9ae481374da2ebc6785a2331f583d103693/SystemHeat/SystemHeat/Modules/ModuleSystemHeatFissionReactor.cs#L403

	// also note that ModuleSystemHeatFissionEngine inherits from ModuleSystemHeatFissionReactor

	// does the harvester have the same problem...?

	[RescalablePartModuleHandler("ModuleSystemHeatFissionReactor")]
	internal class ModuleSystemHeatFissionReactorHandler : IRescalable, IRescalablePriority
	{
		#region static reflection stuff

		static Type x_moduleSystemHeatFissionReactorType;
		static FieldInfo x_inputsField;
		static MethodInfo x_GetModuleConfigNode;

		static ModuleSystemHeatFissionReactorHandler()
		{
			x_moduleSystemHeatFissionReactorType = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleSystemHeatFissionReactor");

			if (x_moduleSystemHeatFissionReactorType == null) return;

			x_inputsField = x_moduleSystemHeatFissionReactorType.GetField("inputs", BindingFlags.NonPublic | BindingFlags.Instance);

			if (x_inputsField == null)
			{
				Tools.LogError("ModuleSystemHeatFissionReactor was loaded but could not find the 'inputs' field!");
				return;
			}

			Type moduleUtils = x_moduleSystemHeatFissionReactorType.Assembly.GetType("SystemHeat.ModuleUtils");
			
			if (moduleUtils == null)
			{
				Tools.LogError("ModuleUtils type not found in SystemHeat assembly");
				return;
			}

			x_GetModuleConfigNode = moduleUtils.GetMethod("GetModuleConfigNode", BindingFlags.Public | BindingFlags.Static);

			if (x_GetModuleConfigNode == null)
			{
				Tools.LogError("SystemHeat: ModuleUtils was loaded but could not find the 'GetModuleConfigNode' method!");
			}
		}

		#endregion

		public ModuleSystemHeatFissionReactorHandler(PartModule partModule)
		{
			_module = partModule;
		}

		void IRescalable.OnRescale(ScalingFactor factor)
		{
			if (x_GetModuleConfigNode == null) return;

			var inputs = x_inputsField.GetValue(_module) as List<ResourceRatio>;

			// If MmoduleSystemHeatFissionReactor hasn't had its Start method called yet, the list will be null or empty.
			// Force it to reload now, which it would be doing later anyway.

			if (inputs == null || inputs.Count == 0)
			{
				ConfigNode configNode = x_GetModuleConfigNode.Invoke(null, new object[] { _module.part, _module.moduleName }) as ConfigNode;
				if (configNode != null)
				{
					_module.OnLoad(configNode);
				}
			}
		}

		int IRescalablePriority.Priority => (int)IRescalablePriority.PriorityThreshold.BeforeExponentHandlers;

		PartModule _module;
	}
}
