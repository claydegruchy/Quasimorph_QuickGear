using System;
using System.Collections.Generic;
using System.Linq;
using MGSC;
using Newtonsoft.Json;
using UnityEngine;
using QuasimorphHelloWorld.Framework;

namespace QuasimorphHelloWorld
{
    /// <summary>
    /// Core equipment management service. Can be reused in other mods.
    /// </summary>
    public static class QuickGearService
    {
        private static GenericConfigManager<ModConfig> _configManager;

        public static void Initialize(GenericConfigManager<ModConfig> configManager)
        {
            _configManager = configManager;
        }

        public static void SaveEquipment(Mercenary merc)
        {
            if (_configManager == null)
            {
                Debug.Log("[QuickGear] Config manager not initialized.");
                return;
            }

            string profileId = merc.ProfileId;
            var savedEquip = new ModConfig.SavedEquipment();
            var inventory = merc.CreatureData.Inventory;

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
            if (inventory.BackpackSlot.First != null)
                savedEquip.Equipment["Backpack"] = inventory.BackpackSlot.First.Id;
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

            _configManager.Config.SavedEquipmentHistory[profileId] = savedEquip;
            _configManager.SaveConfig();
            Debug.Log($"[QuickGear] Saved equipment for {profileId}");
        }

        public static void EquipQuickGear(Mercenary merc)
        {
            if (_configManager == null || GlobalModContext.Context == null)
            {
                Debug.Log("[QuickGear] Not initialized.");
                return;
            }

            MagnumCargo cargo = GlobalModContext.Context.State.Get<MagnumCargo>();
            Mercenaries mercenaries = GlobalModContext.Context.State.Get<Mercenaries>();

            if (cargo == null || mercenaries == null || mercenaries.Values.Count == 0)
            {
                Debug.Log("[QuickGear] State not ready.");
                return;
            }

            Debug.Log(
                "[QuickGear] Running quick gear. Config contents: "
                    + JsonConvert.SerializeObject(_configManager.Config, Formatting.Indented)
            );

            foreach (ModConfig.ItemEntry entry in _configManager.Config.Items)
            {
                InventoryHelpers.PullFromCargo(cargo, new List<Mercenary> { merc }, entry.ItemId, entry.Count);
            }
        }

        public static void LoadSavedEquipment(Mercenary merc)
        {
            if (_configManager == null || GlobalModContext.Context == null)
            {
                Debug.Log("[QuickGear] Not initialized.");
                return;
            }

            string profileId = merc.ProfileId;
            if (!_configManager.Config.SavedEquipmentHistory.TryGetValue(profileId, out var savedEquip))
            {
                Debug.Log($"[QuickGear] No saved equipment for {profileId}");
                return;
            }

            var inventory = merc.CreatureData.Inventory;
            var allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
            var magnumCargo = GlobalModContext.Context.State.Get<MagnumCargo>();
            var shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
            var perkFactory = GlobalModContext.Context.State.Get<PerkFactory>();

            List<string> missingItems = new List<string>();
            List<string> failedLimbs = new List<string>();
            List<string> failedImplants = new List<string>();

            foreach (var kvp in savedEquip.Limbs)
            {
                string woundSlotId = kvp.Key;
                string augId = kvp.Value;

                if (
                    merc.CreatureData.AugmentationMap.TryGetValue(woundSlotId, out var existingAug)
                    && existingAug != augId
                )
                {
                    AugmentationSystem.RemoveAugmentation(merc, woundSlotId, null, isItemSpawn: false);
                }

                if (!AugmentationSystem.TryApplyGeneratedAugmentation(merc.CreatureData, augId))
                {
                    failedLimbs.Add($"{woundSlotId}:{augId}");
                }
            }

            foreach (var kvp in savedEquip.Implants)
            {
                string woundSlotId = kvp.Key;
                foreach (string implantId in kvp.Value)
                {
                    if (!AugmentationSystem.TryApplyGeneratedImplant(perkFactory, merc.CreatureData, implantId))
                    {
                        failedImplants.Add($"{woundSlotId}:{implantId}");
                    }
                }
            }

            foreach (var kvp in savedEquip.Equipment)
            {
                string slotName = kvp.Key;
                string itemId = kvp.Value;

                var item = allItems.FirstOrDefault(i => i.Id == itemId);
                if (item == null)
                {
                    item = shipCargoItems.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        InventoryHelpers.PullFromCargo(magnumCargo, new List<Mercenary> { merc }, itemId, 1);
                        allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                        item = allItems.FirstOrDefault(i => i.Id == itemId);
                    }
                }

                if (item == null)
                {
                    missingItems.Add(itemId);
                    continue;
                }

                ItemStorage slot = InventoryHelpers.GetSlotByName(inventory, slotName);
                if (slot != null)
                {
                    inventory.TakeOrEquip(item, putIfSlotBusy: true);
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

            Debug.Log($"[QuickGear] Loaded saved equipment for {profileId}");
        }

        public static bool HasSavedEquipment(Mercenary merc)
        {
            if (_configManager == null)
                return false;

            return _configManager.Config.SavedEquipmentHistory.ContainsKey(merc.ProfileId);
        }
    }
}
