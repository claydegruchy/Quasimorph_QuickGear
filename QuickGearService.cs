using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MGSC;
using Newtonsoft.Json;
using UnityEngine;

namespace QuasimorphHelloWorld
{
    public static class QuickGearService
    {
        public static void OnSpaceUpdate(IModContext context)
        {
            // QuickGear is triggered from the Arsenal UI buttons instead of a keybind.
        }

        public static void SaveEquipment(Mercenary merc)
        {
            if (merc == null)
                return;

            string profileId = merc.ProfileId;
            var savedEquip = new ModConfig.SavedEquipment();
            var inventory = merc.CreatureData.Inventory;

            Debug.Log($"[QuickGear] Saving equipment for profileId={profileId} (raw profile key).\n[QuickGear] Current save slot: {ModConfigStore.CurrentSlot}");

            if (inventory.BackpackSlot.First != null)
                savedEquip.Equipment["Backpack"] = inventory.BackpackSlot.First.Id;
            if (inventory.PrimarySlot.First != null)
                savedEquip.Equipment["Primary"] = inventory.PrimarySlot.First.Id;
            if (inventory.SecondarySlot.First != null)
                savedEquip.Equipment["Secondary"] = inventory.SecondarySlot.First.Id;
            if (inventory.ServoArmSlot.First != null)
                savedEquip.Equipment["ServoArm"] = inventory.ServoArmSlot.First.Id;
            if (inventory.AdditionalSlot.First != null)
                savedEquip.Equipment["Additional"] = inventory.AdditionalSlot.First.Id;
            if (inventory.ArmorSlot.First != null)
                savedEquip.Equipment["Armor"] = inventory.ArmorSlot.First.Id;
            if (inventory.HelmetSlot.First != null)
                savedEquip.Equipment["Helmet"] = inventory.HelmetSlot.First.Id;
            if (inventory.LeggingsSlot.First != null)
                savedEquip.Equipment["Leggings"] = inventory.LeggingsSlot.First.Id;
            if (inventory.BootsSlot.First != null)
                savedEquip.Equipment["Boots"] = inventory.BootsSlot.First.Id;
            if (inventory.VestSlot.First != null)
                savedEquip.Equipment["Vest"] = inventory.VestSlot.First.Id;

            foreach (var kvp in merc.CreatureData.AugmentationMap)
            {
                savedEquip.Limbs[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in merc.CreatureData.WoundSlotMap)
            {
                var implantIds = kvp.Value.InstalledImplantsData.Select(i => i.ImplantId).ToList();
                if (implantIds.Any())
                {
                    savedEquip.Implants[kvp.Key] = implantIds;
                }
            }

            ModConfigStore.Config.SavedEquipmentHistory[profileId] = savedEquip;
            ModConfigStore.SaveConfig();
            Debug.Log($"[QuickGear] Saved equipment for {profileId}");
        }

        public static void EquipQuickGear(Mercenary merc)
        {
            if (ModMain._modContext == null)
            {
                Debug.Log("[QuickGear] No mod context available.");
                return;
            }

            MagnumCargo cargo = ModMain._modContext.State.Get<MagnumCargo>();
            Mercenaries mercenaries = ModMain._modContext.State.Get<Mercenaries>();

            if (cargo == null || mercenaries == null || mercenaries.Values.Count == 0)
            {
                Debug.Log("[QuickGear] State not ready.");
                return;
            }

            Debug.Log(
                "[QuickGear] Running quick gear. Config contents: "
                    + JsonConvert.SerializeObject(ModConfigStore.Config, Formatting.Indented)
            );

            foreach (ModConfig.ItemEntry entry in ModConfigStore.Config.Items)
            {
                PullFromCargo(cargo, new List<Mercenary> { merc }, entry.ItemId, entry.Count);
            }

            RefreshArsenalScreen();
        }

        public static void SaveInventoryQuickGear(Mercenary merc)
        {
            if (merc == null)
            {
                Debug.Log("[QuickGear] No merc provided for inventory save.");
                return;
            }

            var inventory = merc.CreatureData.Inventory;
            if (inventory == null)
            {
                Debug.Log("[QuickGear] Merc inventory is null.");
                return;
            }

            Dictionary<string, int> counts = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (ItemStorage storage in inventory.Storages)
            {
                if (storage == null)
                    continue;

                foreach (BasePickupItem item in storage.Items)
                {
                    if (item == null)
                        continue;

                    int itemCount = (item.IsStackable ? item.StackCount : 1);
                    if (counts.ContainsKey(item.Id))
                    {
                        counts[item.Id] += itemCount;
                    }
                    else
                    {
                        counts[item.Id] = itemCount;
                    }
                }
            }

            ModConfigStore.Config.Items = counts
                .Select(kvp => new ModConfig.ItemEntry { ItemId = kvp.Key, Count = kvp.Value })
                .ToList();

            ModConfigStore.SaveConfig();
            Debug.Log(
                $"[QuickGear] Saved {counts.Count} inventory item types to quick equip config."
            );
        }

        public static void LoadSavedEquipment(Mercenary merc)
        {
            string profileId = merc.ProfileId;
            if (!TryGetSavedEquipment(profileId, out var savedEquip))
            {
                Debug.Log($"[QuickGear] No saved equipment for {profileId}");
                return;
            }

            Debug.Log(
                $"[QuickGear] LoadSavedEquipment for {profileId}: equipment={savedEquip.Equipment.Count}, limbs={savedEquip.Limbs.Count}, implants={savedEquip.Implants.Values.Sum(list => list?.Count ?? 0)}"
            );

            var inventory = merc.CreatureData.Inventory;
            var magnumCargo = ModMain._modContext.State.Get<MagnumCargo>();
            if (magnumCargo == null)
            {
                Debug.Log("[QuickGear] No magnum cargo available for saved equipment load.");
                return;
            }

            ItemStorage cargoFallback = magnumCargo.ShipCargo.FirstOrDefault();
            if (cargoFallback != null)
            {
                StashInventoryIntoCargo(inventory, cargoFallback);
            }

            UnequipAllEquipment(merc, cargoFallback);

            var allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
            Debug.Log($"[QuickGear] After unequip, inventory contains {allItems.Count} items.");
            var shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
            Debug.Log($"[QuickGear] Ship cargo has {shipCargoItems.Count} items available.");
            var perkFactory = ModMain._modContext.State.Get<PerkFactory>();

            if (cargoFallback != null)
            {
                Debug.Log("[QuickGear] Clearing implants before augmentations from first ship cargo container.");
                ClearExistingAugmentationsAndImplants(merc, cargoFallback);
                shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
            }
            else
            {
                Debug.Log("[QuickGear] No magnum cargo available to clear implants and limbs.");
            }

            List<string> missingItems = new List<string>();
            List<string> failedLimbs = new List<string>();
            List<string> failedImplants = new List<string>();

            foreach (var kvp in savedEquip.Limbs)
            {
                string woundSlotId = kvp.Key;
                string augId = kvp.Value;
                Debug.Log($"[QuickGear] Loading limb {augId} into slot {woundSlotId}.");

                var limbItem = allItems.FirstOrDefault(i => i.Id == augId);
                if (limbItem == null)
                {
                    limbItem = shipCargoItems.FirstOrDefault(i => i.Id == augId);
                    if (limbItem != null)
                    {
                        Debug.Log($"[QuickGear] Limb {augId} found in ship cargo, pulling.");
                        PullFromCargo(magnumCargo, new List<Mercenary> { merc }, augId, 1);
                        allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                        shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
                        limbItem = allItems.FirstOrDefault(i => i.Id == augId);
                    }
                }

                if (limbItem == null)
                {
                    Debug.Log($"[QuickGear] Limb {augId} not found in inventory or cargo.");
                    missingItems.Add(augId);
                    continue;
                }

                if (
                    merc.CreatureData.AugmentationMap.TryGetValue(woundSlotId, out var existingAug)
                    && existingAug != augId
                )
                {
                    AugmentationSystem.RemoveAugmentation(
                        merc,
                        woundSlotId,
                        null,
                        isItemSpawn: false
                    );
                }

                if (!AugmentationSystem.TryApplyGeneratedAugmentation(merc.CreatureData, augId))
                {
                    Debug.Log($"[QuickGear] Failed to apply limb augmentation {augId} to slot {woundSlotId}.");
                    failedLimbs.Add($"{woundSlotId}:{augId}");
                }
                else
                {
                    Debug.Log($"[QuickGear] Applied limb augmentation {augId} to slot {woundSlotId}.");
                    RemoveItemFromInventory(inventory, limbItem);
                    allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                }
            }

            AugmentationSystem.ConfigureImplicitEffects(merc.CreatureData, false);

            foreach (var kvp in savedEquip.Implants)
            {
                string woundSlotId = kvp.Key;
                foreach (string implantId in kvp.Value)
                {
                    Debug.Log($"[QuickGear] Loading implant {implantId} into slot {woundSlotId}.");
                    var implantItem = allItems.FirstOrDefault(i => i.Id == implantId);
                    if (implantItem == null)
                    {
                        implantItem = shipCargoItems.FirstOrDefault(i => i.Id == implantId);
                        if (implantItem != null)
                        {
                            Debug.Log($"[QuickGear] Implant {implantId} found in ship cargo, pulling.");
                            PullFromCargo(magnumCargo, new List<Mercenary> { merc }, implantId, 1);
                            allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                            shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
                            implantItem = allItems.FirstOrDefault(i => i.Id == implantId);
                        }
                    }

                    if (implantItem == null)
                    {
                        Debug.Log($"[QuickGear] Implant {implantId} not found in inventory or cargo.");
                        missingItems.Add(implantId);
                        continue;
                    }

                    if (
                        !AugmentationSystem.TryApplyGeneratedImplant(
                            perkFactory,
                            merc.CreatureData,
                            implantId
                        )
                    )
                    {
                        Debug.Log($"[QuickGear] Failed to apply implant {implantId} to slot {woundSlotId}.");
                        failedImplants.Add($"{woundSlotId}:{implantId}");
                    }
                    else
                    {
                        Debug.Log($"[QuickGear] Applied implant {implantId} to slot {woundSlotId}.");
                        RemoveItemFromInventory(inventory, implantItem);
                        allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                    }
                }
            }

            AugmentationSystem.ConfigureImplicitEffects(merc.CreatureData, false);

            foreach (var kvp in savedEquip.Equipment)
            {
                string slotName = kvp.Key;
                string itemId = kvp.Value;
                Debug.Log($"[QuickGear] Loading equipment {itemId} into slot {slotName}.");

                var item = allItems.FirstOrDefault(i => i.Id == itemId);
                if (item == null)
                {
                    item = shipCargoItems.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        Debug.Log($"[QuickGear] Equipment {itemId} found in ship cargo, pulling.");
                        PullFromCargo(magnumCargo, new List<Mercenary> { merc }, itemId, 1);
                        allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                        shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
                        item = allItems.FirstOrDefault(i => i.Id == itemId);
                    }
                }

                if (item == null)
                {
                    Debug.Log($"[QuickGear] Equipment {itemId} not found in inventory or cargo.");
                    missingItems.Add(itemId);
                    continue;
                }

                ItemStorage slot = GetSlotByName(inventory, slotName);
                if (slot == null)
                {
                    Debug.Log($"[QuickGear] Unknown equipment slot {slotName} for item {itemId}.");
                    missingItems.Add(itemId);
                    continue;
                }

                bool equipped = TryEquipIntoSavedSlot(inventory, slotName, slot, item, out string equipFailReason);
                Debug.Log($"[QuickGear] Equip attempt for {itemId} into {slotName}: {equipped}. {(equipped ? string.Empty : "Reason: " + equipFailReason)}");
                if (!equipped)
                {
                    Debug.Log($"[QuickGear] Failed to equip {itemId} into slot {slotName}. {equipFailReason}");
                }

                if (magnumCargo != null)
                {
                    foreach (ItemStorage tab in magnumCargo.ShipCargo)
                    {
                        if (tab.Items.Contains(item))
                        {
                            Debug.Log($"[QuickGear] Removing duplicate {itemId} from ship cargo.");
                            tab.Remove(item);
                        }
                    }
                }
            }

            AugmentationSystem.ConfigureImplicitEffects(merc.CreatureData);

            if (failedLimbs.Any() || failedImplants.Any() || missingItems.Any())
            {
                string message = "Warning: Some items could not be equipped:\n";
                if (failedLimbs.Any())
                    message += $"Failed limbs: {string.Join(", ", failedLimbs)}\n";
                if (failedImplants.Any())
                    message += $"Failed implants: {string.Join(", ", failedImplants)}\n";
                if (missingItems.Any())
                    message += $"Missing items: {string.Join(", ", missingItems)}";
                Debug.Log($"[QuickGear] {message}");
            }

            RefreshArsenalScreen();
            Debug.Log($"[QuickGear] Loaded saved equipment for {profileId}");
        }

        private static void ClearExistingAugmentationsAndImplants(
            Mercenary merc,
            ItemStorage cargoFallback
        )
        {
            var creatureData = merc.CreatureData;

            foreach (string woundSlotId in creatureData.WoundSlotMap.Keys.ToList())
            {
                if (AugmentationSystem.HasImplantsInstalled(creatureData, woundSlotId))
                {
                    AugmentationSystem.RemoveAllImplants(
                        creatureData,
                        woundSlotId,
                        cargoFallback,
                        false
                    );
                }
            }

            foreach (string woundSlotId in creatureData.AugmentationMap.Keys.ToList())
            {
                AugmentationSystem.RemoveAugmentation(merc, woundSlotId, cargoFallback, true);
            }

            AugmentationSystem.RestoreDefaultWoundSlots(merc);
            AugmentationSystem.ConfigureImplicitEffects(creatureData, false);
            CreatureSystem.SetBareHandSlot(creatureData);
        }

        private static void StashInventoryIntoCargo(Inventory inventory, ItemStorage cargoFallback)
        {
            foreach (ItemStorage storage in inventory.AllContainers)
            {
                if (
                    storage == null
                    || storage == inventory.BareHandsSlot
                    || storage == inventory.ArmStumpSlot
                    || storage.Items.Count == 0
                )
                {
                    continue;
                }

                foreach (BasePickupItem item in storage.Items.ToList())
                {
                    storage.Remove(item);
                    if (!cargoFallback.TryPutItem(item, CellPosition.Zero))
                    {
                        cargoFallback.AddItemAndReshuffleOptional(item);
                    }
                }
            }
        }

        public static bool HasSavedEquipment(Mercenary merc)
        {
            return TryGetSavedEquipment(merc.ProfileId, out _);
        }

        public static Mercenary GetSelectedMerc()
        {
            if (!UI.IsShowing<ArsenalScreen>())
            {
                return null;
            }

            ArsenalScreen screen = UI.Get<ArsenalScreen>();
            if (screen == null)
            {
                return null;
            }

            System.Reflection.FieldInfo field = typeof(ArsenalScreen).GetField(
                "_merc",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (field == null)
            {
                return null;
            }

            return field.GetValue(screen) as Mercenary;
        }

        private static bool TryGetSavedEquipment(
            string profileId,
            out ModConfig.SavedEquipment savedEquip
        )
        {
            if (ModConfigStore.Config.SavedEquipmentHistory.TryGetValue(profileId, out savedEquip))
            {
                Debug.Log($"[QuickGear] Found saved equipment for exact key: {profileId}");
                return true;
            }

            string normalized = NormalizeProfileId(profileId);
            if (
                normalized != profileId
                && ModConfigStore.Config.SavedEquipmentHistory.TryGetValue(normalized, out savedEquip)
            )
            {
                Debug.Log($"[QuickGear] Found saved equipment for normalized key: {normalized}");
                return true;
            }

            Debug.Log(
                $"[QuickGear] No saved equipment found for {profileId} (normalized: {normalized})"
            );
            savedEquip = null;
            return false;
        }

        private static string NormalizeProfileId(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
                return profileId;

            return profileId.EndsWith("_custom")
                ? profileId.Substring(0, profileId.Length - "_custom".Length)
                : profileId;
        }

        private static ItemStorage GetSlotByName(Inventory inventory, string slotName)
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

        private static bool TryEquipIntoSavedSlot(
            Inventory inventory,
            string slotName,
            ItemStorage slot,
            BasePickupItem item,
            out string reason
        )
        {
            reason = string.Empty;

            if (slot == null)
            {
                reason = "Target slot is null.";
                return false;
            }

            if (item == null)
            {
                reason = "Item is null.";
                return false;
            }

            if (!IsSlotAvailableForSavedName(inventory, slotName, slot))
            {
                reason = $"Slot {slotName} is currently unavailable for this mercenary state.";
                return false;
            }

            if (!slot.IsValidItem(item))
            {
                reason = $"Item {item.Id} is not valid for slot {slotName}.";
                return false;
            }

            if (!slot.Empty)
            {
                reason = $"Slot {slotName} is already occupied by {slot.First?.Id ?? "unknown item"}.";
                return false;
            }

            bool moved = ItemInteractionSystem.Move(item, slot, CellPosition.Zero, true, false);
            if (!moved)
            {
                reason = $"Move to slot {slotName} was rejected by ItemInteractionSystem.";
            }

            return moved;
        }

        private static bool IsSlotAvailableForSavedName(
            Inventory inventory,
            string slotName,
            ItemStorage slot
        )
        {
            if (slot == null)
            {
                return false;
            }

            return slotName switch
            {
                "Primary" => inventory.IsSlotAvailable(WeaponSlotType.Primary),
                "Secondary" => inventory.IsSlotAvailable(WeaponSlotType.Secondary),
                "Additional" => inventory.IsSlotAvailable(WeaponSlotType.Additional),
                "ServoArm" => inventory.IsSlotAvailable(WeaponSlotType.ServoArm),
                _ => !slot.IsBlocked
            };
        }

        public static void RefreshArsenalScreen()
        {
            try
            {
                if (UI.IsShowing<ArsenalScreen>())
                {
                    var screen = UI.Get<ArsenalScreen>();
                    screen?.RefreshView();
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[QuickGear] Failed to refresh Arsenal screen: " + ex.Message);
            }
        }

        private static void PullFromCargo(
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
                int toPull = Math.Min(needed, availableInCargo);

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
                        break;
                }

                if (sourceItem == null)
                    break;

                BasePickupItem itemToMove;
                bool usedSourceItem = false;
                bool sourceItemRemovedFromCargo = false;

                if (sourceItem.IsStackable && sourceItem.StackCount > toPull)
                {
                    itemToMove = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(
                        itemId
                    );
                    itemToMove.StackCount = (short)toPull;
                    if (sourceItem.IsUsable)
                    {
                        sourceItem
                            .Comp<UsableItemComponent>()
                            .SplitItem(itemToMove.Comp<UsableItemComponent>(), toPull);
                    }
                    sourceItem.StackCount -= (short)toPull;
                    if (sourceItem.StackCount <= 0)
                    {
                        sourceTab.Remove(sourceItem);
                        sourceItemRemovedFromCargo = true;
                    }
                }
                else
                {
                    itemToMove = sourceItem;
                    usedSourceItem = true;
                    sourceTab.Remove(sourceItem);
                    sourceItemRemovedFromCargo = true;
                }

                if (
                    merc.CreatureData.Inventory.BackpackStore.TryPutItem(
                        itemToMove,
                        CellPosition.Zero
                    )
                )
                {
                    Debug.Log($"[QuickGear] Moved {toPull}x {itemId} to {merc.ProfileId}");
                }
                else
                {
                    Debug.Log(
                        $"[QuickGear] No space in {merc.ProfileId} backpack, returning to cargo."
                    );
                    if (usedSourceItem)
                    {
                        sourceTab.AddItemAndReshuffleOptional(sourceItem);
                    }
                    else
                    {
                        sourceItem.StackCount += (short)toPull;
                        if (sourceItemRemovedFromCargo && !sourceTab.Items.Contains(sourceItem))
                        {
                            sourceTab.AddItemAndReshuffleOptional(sourceItem);
                        }
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

        private static void UnequipAllEquipment(Mercenary merc, ItemStorage fallbackStorage = null)
        {
            Inventory inventory = merc.CreatureData.Inventory;
            foreach (ItemStorage slot in inventory.Slots)
            {
                if (slot == inventory.BareHandsSlot || slot == inventory.ArmStumpSlot)
                {
                    continue;
                }

                List<BasePickupItem> slotItems = slot.Items.ToList();
                foreach (BasePickupItem item in slotItems)
                {
                    if (fallbackStorage != null)
                    {
                        item.Storage.Remove(item);
                        if (!fallbackStorage.TryPutItem(item, CellPosition.Zero))
                        {
                            fallbackStorage.AddItemAndReshuffleOptional(item);
                        }

                        if (!fallbackStorage.Items.Contains(item))
                        {
                            Debug.Log(
                                $"[QuickGear] WARNING: Failed to move {item.Id} into fallback storage {fallbackStorage.Source}."
                            );
                        }
                        else
                        {
                            Debug.Log(
                                $"[QuickGear] Unequipped {item.Id} from {slot.Source} to fallback storage {fallbackStorage.Source}."
                            );
                        }
                        continue;
                    }

                    bool unequipped = inventory.Unequip(item);
                    if (!unequipped)
                    {
                        Debug.Log(
                            $"[QuickGear] Failed to unequip {item.Id} from {slot.Source}. Item may be lost."
                        );
                    }
                }
            }
        }

        private static void RemoveItemFromInventory(Inventory inventory, BasePickupItem item)
        {
            foreach (ItemStorage container in inventory.AllContainers)
            {
                if (container.Items.Contains(item))
                {
                    container.Remove(item);
                    return;
                }
            }
        }
    }
}
