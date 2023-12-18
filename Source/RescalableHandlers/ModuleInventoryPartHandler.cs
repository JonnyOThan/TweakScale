using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale.RescalableHandlers
{
	internal class ModuleInventoryPartHandler : IRescalable<ModuleInventoryPart>
	{
		ModuleInventoryPart _module;

		public ModuleInventoryPartHandler(ModuleInventoryPart module)
		{
			_module = module;
		}

		public void OnRescale(ScalingFactor factor)
		{
			// remove item slots if there's no longer room
			bool removedAny = false;
			for (int itemIndex = _module.storedParts.Count - 1; itemIndex >= 0; itemIndex--)
			{
				// NOTE: the events here seem to only be used to update the UI, which we're about to rebuild anyway
				int slotIndex = _module.storedParts.KeyAt(itemIndex);
				if (slotIndex >= _module.InventorySlots)
				{
					//GameEvents.onModuleInventorySlotChanged.Fire(_module, slotIndex);
					_module.storedParts.Remove(slotIndex);
					removedAny = true;
				}
			}

			if (removedAny)
			{
				//GameEvents.onModuleInventoryChanged.Fire(_module);
				_module.ResetInventoryPartsByName();
			}

			_module.UpdateCapacityValues();

			var partActionInventory = (_module.fields[nameof(ModuleInventoryPart.InventorySlots)].uiControlEditor as UI_Grid).partActionItem as UIPartActionInventory;

			if (partActionInventory != null && _module.InventorySlots != partActionInventory.slotButton.Count)
			{
				foreach (var slot in partActionInventory.slotButton)
				{
					GameObject.Destroy(slot.gameObject);
				}
				partActionInventory.slotButton.Clear();
				partActionInventory.slotPartIcon.Clear();

				partActionInventory.fieldValue = _module.InventorySlots;
				partActionInventory.InitializeSlots();
			}
		}
	}
}
