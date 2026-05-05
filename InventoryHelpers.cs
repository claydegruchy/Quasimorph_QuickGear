using System.Collections.Generic;
using System.Linq;
using MGSC;
using UnityEngine;

namespace QuasimorphHelloWorld
{
    public static class InventoryHelpers
    {
        public static ItemStorage GetSlotByName(Inventory inventory, string slotName)
        {
            return slotName switch
            {
                "Primary" => inventory.PrimarySlot,
                "Secondary" => inventory.SecondarySlot,
                "ServoArm" => inventory.ServoArmSlot,
                "Additional" => inventory.AdditionalSlot,
                "Armor" => inventory.ArmorSlot,
                "Helmet" => inventory.HelmetSlot,
                "Leggings" => inventory.LeggingsSlot,
                "Boots" => inventory.BootsSlot,
                "Backpack" => inventory.BackpackSlot,
                "Vest" => inventory.VestSlot,
                _ => null
            };
        }

        public static int CountItemsInInventory(Mercenary merc, string itemId)
        {
            int count = 0;
            foreach (ItemStorage storage in merc.CreatureData.Inventory.AllContainers)
            {
                count += storage.CountItems(itemId);
            }
            return count;
        }

        public static int CountItemsInCargo(MagnumCargo cargo, string itemId)
        {
            int count = 0;
            foreach (ItemStorage tab in cargo.ShipCargo)
            {
                count += tab.CountItems(itemId);
            }
            return count;
        }

        public static void PullFromCargo(
            MagnumCargo cargo,
            List<Mercenary> mercs,
            string itemId,
            int countPerMerc
        )
        {
            foreach (Mercenary merc in mercs)
            {
                int current = CountItemsInInventory(merc, itemId);
                int needed = countPerMerc - current;

                if (needed <= 0)
                {
                    Debug.Log($"[QuickGear] {merc.ProfileId} already has enough {itemId}");
                    continue;
                }

                int availableInCargo = CountItemsInCargo(cargo, itemId);
                int toPull = Mathf.Min(needed, availableInCargo);

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

                BasePickupItem newItem =
                    SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(itemId);
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
    }
}
