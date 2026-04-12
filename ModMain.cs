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

		[Hook(ModHookType.SpaceStarted)]
		public static void OnSpaceStarted(IModContext context)
		{
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

				Debug.Log($"[QuickGear] {itemId}: have {current}, want {targetCount}, pulling {needed}");

				for (int i = 0; i < needed; i++)
				{
					if (!PullOneFromCargo(cargo, mercenaries, itemId))
					{
						Debug.Log("[QuickGear] Could not pull more of: " + itemId);
						break;
					}
				}
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

		private static bool PullOneFromCargo(MagnumCargo cargo, Mercenaries mercenaries, string itemId)
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

					foreach (Mercenary merc in mercenaries.Values)
					{
						if (merc.CreatureData.Inventory.BackpackStore.TryPutItem(item, CellPosition.Zero))
						{
							Debug.Log($"[QuickGear] Moved {itemId} to {merc.ProfileId}");
							return true;
						}
					}

					// No merc had space, put it back
					tab.AddItemAndReshuffleOptional(item);
					Debug.Log("[QuickGear] No merc had space for: " + itemId);
					return false;
				}
			}

			return false;
		}
	}
}