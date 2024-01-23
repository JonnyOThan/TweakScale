using Smooth.Compare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AttachNode;

namespace TweakScale.RescalableHandlers
{
	internal class ModuleDockingNodeHandler : IRescalable<ModuleDockingNode>
	{
		public ModuleDockingNodeHandler(ModuleDockingNode module)
		{
			m_module = module;
			
			int moduleIndex = module.part.modules.IndexOf(module);
			var prefabModule = module.part.partInfo.partPrefab.modules[moduleIndex] as ModuleDockingNode;

			// TODO: can this ever change at runtime?  Like with B9PS or something?
			m_prefabNodeTypes = prefabModule.nodeTypes;
		}

		public void OnRescale(ScalingFactor factor)
		{
			HashSet<string> newNodeTypes = new HashSet<string>();

			foreach (var nodeType in m_prefabNodeTypes)
			{
				newNodeTypes.Add(GetScaledNodeType(nodeType, factor.absolute.linear));
			}

			m_module.nodeTypes = newNodeTypes;
			m_module.nodeType = string.Join(", ", newNodeTypes);
		}

		static string GetScaledNodeType(string prefabNodeType, float scaleFactor)
		{
			if (x_nodeTypeToDiameter.TryGetValue(prefabNodeType, out var prefabNodeDiameter))
			{
				float scaledDiameter = prefabNodeDiameter * scaleFactor;

				foreach (var pair in x_nodeTypeToDiameter)
				{
					if (Mathfx.Approx(pair.Value, scaledDiameter, 1e-4f))
					{
						return pair.Key;
					}
				}
			}

			return prefabNodeType + "x" + scaleFactor;
		}

		static readonly Dictionary<string, float> x_nodeTypeToDiameter = new Dictionary<string, float>() {
			{"size0",   0.625f },
			{"size1",   1.25f },
			{"size1p5", 1.875f },
			{"size2",   2.5f },
			{"size3",   3.75f },
			{"size4",   5f },
			{"size5",   7.5f },
			{"size6",  10f },
		};

		readonly ModuleDockingNode m_module;
		readonly HashSet<string> m_prefabNodeTypes;
	}
}
