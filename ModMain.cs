using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace QuasimorphHelloWorld
{
	public static class ModMain
	{
		private static readonly Dictionary<string, int> _desiredItems = new Dictionary<string, int>
		{
			{ "water_bottle_1", 1 },
			{ "powder", 1 },
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
				PullFromCargo(cargo, mercenaries, desired.Key, desired.Value);
			}
		}

		private static void PullFromCargo(MagnumCargo cargo, Mercenaries mercenaries, string itemId, int countPerMerc)
		{
			foreach (Mercenary merc in mercenaries.Values)
			{
				int current = CountItemsInInventory(merc, itemId);
				int needed = countPerMerc - current;

				if (needed <= 0)
				{
					Debug.Log($"[QuickGear] {merc.ProfileId} already has enough {itemId}");
					continue;
				}

				int availableInCargo = CountItemsInCargo(cargo, itemId);
				int toPull = System.Math.Min(needed, availableInCargo);

				if (toPull <= 0)
				{
					Debug.Log($"[QuickGear] No more {itemId} in cargo.");
					break;
				}

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
					break;
				}

				BasePickupItem newItem = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(itemId);
				newItem.StackCount = (short)toPull;

				sourceItem.StackCount -= (short)toPull;
				if (sourceItem.StackCount <= 0)
				{
					sourceTab.Remove(sourceItem);
				}

				if (merc.CreatureData.Inventory.BackpackStore.TryPutItem(newItem, CellPosition.Zero))
				{
					Debug.Log($"[QuickGear] Moved {toPull}x {itemId} to {merc.ProfileId}");
				}
				else
				{
					Debug.Log($"[QuickGear] No space in {merc.ProfileId} backpack, returning to cargo.");
					sourceItem.StackCount += (short)toPull;
					if (!sourceTab.Items.Contains(sourceItem))
					{
						sourceTab.AddItemAndReshuffleOptional(sourceItem);
					}
				}
			}
		}

		private static int CountItemsInInventory(Mercenary merc, string itemId)
		{
			int count = 0;
			foreach (ItemStorage storage in merc.CreatureData.Inventory.AllContainers)
			{
				count += storage.CountItems(itemId);
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
	}
}