using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace QuasimorphHelloWorld
{
	public static class ModMain
	{
		private static readonly Dictionary<string, int> _desiredItems = new Dictionary<string, int>
		{
			{ "water_bottle_1", 3 },
		};

		[Hook(ModHookType.AfterBootstrap)]
		public static void OnAfterBootstrap(IModContext context)
		{
			Debug.Log("[QuickGear] Loaded, built: " + System.IO.File.GetLastWriteTime(typeof(ModMain).Assembly.Location));
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

			foreach (KeyValuePair<string, int> desired in _desiredItems)
			{
				string itemId = desired.Key;
				int targetCount = desired.Value;

				int current = CountItemsInAllInventories(mercenaries, itemId);
				int needed = targetCount - current;

				Debug.Log($"[QuickGear] {itemId}: have {current}, want {targetCount}, need to pull {needed}");

				if (needed <= 0)
				{
					Debug.Log($"[QuickGear] Already have enough {itemId}, skipping.");
					continue;
				}

				int availableInCargo = CountItemsInCargo(cargo, itemId);
				int toPull = System.Math.Min(needed, availableInCargo);

				Debug.Log($"[QuickGear] {itemId}: cargo has {availableInCargo}, pulling {toPull}");

				if (toPull <= 0)
				{
					Debug.Log($"[QuickGear] None in cargo: {itemId}");
					continue;
				}

				PullFromCargo(cargo, mercenaries, itemId, toPull);
			}
		}

		private static int CountItemsInAllInventories(Mercenaries mercenaries, string itemId)
		{
			int count = 0;
			foreach (Mercenary merc in mercenaries.Values)
			{
				foreach (ItemStorage storage in merc.CreatureData.Inventory.AllContainers)
				{
					count += storage.CountItems(itemId);
				}
			}
			return count;
		}

		private static int CountItemsInCargo(MagnumCargo cargo, string itemId)
		{
			int count = 0;
			foreach (ItemStorage tab in cargo.ShipCargo)
			{
				count += tab.CountItems(itemId);
			}
			return count;
		}

		private static void PullFromCargo(MagnumCargo cargo, Mercenaries mercenaries, string itemId, int count)
		{
			// Find the source stack in cargo
			BasePickupItem sourceItem = null;
			ItemStorage sourceTab = null;

			foreach (ItemStorage tab in cargo.ShipCargo)
			{
				for (int i = tab.Items.Count - 1; i >= 0; i--)
				{
					if (tab.Items[i].Id.Equals(itemId))
					{
						sourceItem = tab.Items[i];
						sourceTab = tab;
						break;
					}
				}
				if (sourceItem != null)
				{
					break;
				}
			}

			if (sourceItem == null)
			{
				Debug.Log("[QuickGear] Source item disappeared: " + itemId);
				return;
			}

			// Create new item with exact count needed
			BasePickupItem newItem = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(itemId);
			newItem.StackCount = (short)count;

			// Deduct from cargo first before attempting to place
			sourceItem.StackCount -= (short)count;
			if (sourceItem.StackCount <= 0)
			{
				sourceTab.Remove(sourceItem);
			}

			// Try to place into a merc backpack
			foreach (Mercenary merc in mercenaries.Values)
			{
				if (merc.CreatureData.Inventory.BackpackStore.TryPutItem(newItem, CellPosition.Zero))
				{
					Debug.Log($"[QuickGear] Moved {count}x {itemId} to {merc.ProfileId}");
					return;
				}
			}

			// No merc had space — put the count back into cargo
			Debug.Log("[QuickGear] No merc had space, returning items to cargo.");
			sourceItem.StackCount += (short)count;
			if (!sourceTab.Items.Contains(sourceItem))
			{
				sourceTab.AddItemAndReshuffleOptional(sourceItem);
			}
		}
	}
}