using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace QuasimorphHelloWorld
{
	public static class ModMain
	{
		private static IModContext _context;

		// Items to move from ship cargo to mercenary inventory
		private static readonly List<string> _itemIds = new List<string>
		{
			"water_bottle_1",
		};
[Hook(ModHookType.AfterBootstrap)]
public static void OnAfterBootstrap(IModContext context)
{
    UnityEngine.Debug.Log("[QuickGear] Loaded, built: " + System.IO.File.GetLastWriteTime(typeof(ModMain).Assembly.Location));
}

		[Hook(ModHookType.SpaceStarted)]
		public static void OnSpaceStarted(IModContext context)
		{
			_context = context;
		}

		[Hook(ModHookType.SpaceUpdateAfterGameLoop)]
		public static void OnSpaceUpdate(IModContext context)
		{
			if (!Input.GetKeyDown(KeyCode.G))
			{
				return;
			}

			MagnumCargo cargo = context.State.Get<MagnumCargo>();
			Mercenaries mercenaries = context.State.Get<Mercenaries>();

			if (cargo == null || mercenaries == null || mercenaries.Values.Count == 0)
			{
				Debug.Log("[QuickGear] State not ready.");
				return;
			}

			Mercenary merc = mercenaries.Values[0];
			Inventory inventory = merc.CreatureData.Inventory;

			foreach (string itemId in _itemIds)
			{
				MoveItemToInventory(cargo, inventory, itemId);
			}
		}

		private static void MoveItemToInventory(MagnumCargo cargo, Inventory inventory, string itemId)
		{
			foreach (ItemStorage tab in cargo.ShipCargo)
			{
				for (int i = tab.Items.Count - 1; i >= 0; i--)
				{
					BasePickupItem item = tab.Items[i];
					if (!item.Id.Equals(itemId))
					{
						continue;
					}

					tab.Remove(item);
					if (!inventory.BackpackStore.TryPutItem(item, CellPosition.Zero))
					{
						// No space, put it back
						tab.AddItemAndReshuffleOptional(item);
						Debug.Log("[QuickGear] No space in backpack for: " + itemId);
					}
					else
					{
						Debug.Log("[QuickGear] Moved: " + itemId);
					}
					return;
				}
			}

			Debug.Log("[QuickGear] Item not found in cargo: " + itemId);
		}
	}
}