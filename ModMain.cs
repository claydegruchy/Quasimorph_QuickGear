using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MGSC;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HarmonyLib;

namespace QuasimorphHelloWorld
{
    public class ModConfig
    {
        public class ItemEntry
        {
            public string ItemId { get; set; } = "";
            public int Count { get; set; } = 1;
        }

        public class ModLocalization
        {
            public Dictionary<string, Dictionary<string, string>> Languages { get; set; } =
                new Dictionary<string, Dictionary<string, string>>();
        }

        public class SavedEquipment
        {
            public Dictionary<string, string> Equipment { get; set; } =
                new Dictionary<string, string>();
            public Dictionary<string, string> Limbs { get; set; } =
                new Dictionary<string, string>();
            public Dictionary<string, List<string>> Implants { get; set; } =
                new Dictionary<string, List<string>>();
        }

        public List<ItemEntry> Items { get; set; } = new List<ItemEntry> { };
        public Dictionary<string, SavedEquipment> SavedEquipmentHistory { get; set; } =
            new Dictionary<string, SavedEquipment>();
        public string HotkeyCode { get; set; } = "G";
        public bool SaveImplants { get; set; } = true;
    }

    public static class ModMain
    {
        public static ModConfig _default_config =>
            new ModConfig
            {
                Items = new List<ModConfig.ItemEntry>
                {
                    new ModConfig.ItemEntry { ItemId = "medical_kit_2", Count = 2 },
                    new ModConfig.ItemEntry { ItemId = "water_bottle_1", Count = 1 }
                },
                SavedEquipmentHistory = new Dictionary<string, ModConfig.SavedEquipment>(),
                HotkeyCode = "G",
                SaveImplants = true
            };

        public static IModContext _modContext;
        private static ModConfig _config = new ModConfig();
        private static KeyCode _hotkey = KeyCode.G;
        private static readonly Harmony _harmony = new Harmony("QuickGear");
        private static int _currentSlot = -1;
        private static ModConfig.ModLocalization _localization = new ModConfig.ModLocalization();

        public static bool SaveImplants { get; set; } = true;
        private static int _fontSize = 4;

        private static string DefaultConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..",
                "LocalLow",
                "Magnum Scriptum Ltd",
                "Quasimorph_ModConfigs",
                "QuickGear",
                "config.json"
            );

        private static string SlotConfigPath(int slot) =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..",
                "LocalLow",
                "Magnum Scriptum Ltd",
                "Quasimorph_ModConfigs",
                "QuickGear",
                $"slot_{slot}_config.json"
            );

        [Hook(ModHookType.AfterBootstrap)]
        public static void OnAfterBootstrap(IModContext context)
        {
            _modContext = context;
            Debug.Log(
                "[QuickGear] Loaded, built: "
                    + File.GetLastWriteTime(typeof(ModMain).Assembly.Location)
            );
            _harmony.PatchAll();
            EnsureDefaultConfig();
            EnsureLocalization();
        }

        [Hook(ModHookType.MainMenuStarted)]
        public static void OnMainMenuStarted(IModContext context)
        {
            Debug.Log(
                "[QuickGear] Main menu started. Auto-load is disabled to avoid runtime crashes."
            );
        }

        [Hook(ModHookType.AfterSaveLoaded)]
        public static void OnAfterSaveLoaded(IModContext context)
        {
            SavedGameMetadata meta = context.State.Get<SavedGameMetadata>();
            _currentSlot = (meta != null) ? meta.Slot : -1;
            if (meta == null)
            {
                Debug.Log("[QuickGear] No save metadata, using default config.");
                LoadConfig(DefaultConfigPath);
                return;
            }

            string slotPath = SlotConfigPath(meta.Slot);
            if (!File.Exists(slotPath))
            {
                // Copy default config to slot config
                string defaultJson = File.ReadAllText(DefaultConfigPath);
                File.WriteAllText(slotPath, defaultJson);
                Debug.Log($"[QuickGear] Created slot {meta.Slot} config from default.");
            }

            LoadConfig(slotPath);
            Debug.Log($"[QuickGear] Loaded slot {meta.Slot} config.");
        }

        private static void EnsureLocalization()
        {
            try
            {
                var assembly = typeof(ModMain).Assembly;

                string resourceName = assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(name =>
                        name.EndsWith(
                            "Assets.localization.json",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (string.IsNullOrEmpty(resourceName))
                {
                    Debug.LogError(
                        "[QuickGear] Embedded localization.json was not found in DLL."
                    );

                    _localization = new ModConfig.ModLocalization();
                    return;
                }

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();

                    _localization =
                        JsonConvert.DeserializeObject<ModConfig.ModLocalization>(json)
                        ?? new ModConfig.ModLocalization();
                }

                Debug.Log(
                    "[QuickGear] Embedded localization loaded: "
                    + resourceName
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[QuickGear] Failed to load embedded localization: "
                    + e.Message
                );

                _localization = new ModConfig.ModLocalization();
            }
        }

        private static void EnsureDefaultConfig()
        {
            try
            {
                string path = DefaultConfigPath;
                string dir = Path.GetDirectoryName(path);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(path))
                {
                    string defaultJson = JsonConvert.SerializeObject(
                        _default_config,
                        Formatting.Indented
                    );
                    File.WriteAllText(path, defaultJson);
                    Debug.Log("[QuickGear] Created default config at: " + path);
                }

                LoadConfig(path);
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to ensure default config. Error: " + e.Message);
            }
        }

        private static float _lastHotkeyPressTime = -1f;
        private static bool _pendingQuickEquip = false;
        private static Mercenary _pendingQuickEquipMerc;
        private const float DoublePressWindow = 0.5f;

        [Hook(ModHookType.SpaceUpdateAfterGameLoop)]
        public static void OnSpaceUpdate(IModContext context)
        {
            float now = Time.time;
            if (!Input.GetKeyDown(_hotkey))
            {
                if (_pendingQuickEquip && now - _lastHotkeyPressTime >= DoublePressWindow)
                {
                    _pendingQuickEquip = false;
                    if (_pendingQuickEquipMerc != null)
                    {
                        Debug.Log(
                            "[QuickGear] Single hotkey press confirmed. Running quick equip."
                        );
                        EquipQuickGear(_pendingQuickEquipMerc);
                        _pendingQuickEquipMerc = null;
                    }
                }
                return;
            }

            Mercenary selectedMerc = GetSelectedMerc();
            if (selectedMerc == null)
            {
                Debug.Log("[QuickGear] No merc selected.");
                return;
            }

            bool isDoublePress =
                _pendingQuickEquip
                && _pendingQuickEquipMerc == selectedMerc
                && (now - _lastHotkeyPressTime < DoublePressWindow);

            if (isDoublePress)
            {
                Debug.Log("[QuickGear] Hotkey double-pressed. Equipping saved gear.");
                _pendingQuickEquip = false;
                _pendingQuickEquipMerc = null;
                LoadSavedEquipment(selectedMerc);
                return;
            }

            if (_pendingQuickEquip && now - _lastHotkeyPressTime >= DoublePressWindow)
            {
                _pendingQuickEquip = false;
                if (_pendingQuickEquipMerc != null)
                {
                    Debug.Log(
                        "[QuickGear] Previous single press timeout expired. Running quick equip."
                    );
                    EquipQuickGear(_pendingQuickEquipMerc);
                    _pendingQuickEquipMerc = null;
                }
            }

            _pendingQuickEquip = true;
            _pendingQuickEquipMerc = selectedMerc;
            _lastHotkeyPressTime = now;
            Debug.Log("[QuickGear] Hotkey pressed. Waiting for second press to determine action.");
        }

        private static void LoadConfig(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                _config = JsonConvert.DeserializeObject<ModConfig>(json);
                Debug.Log("[QuickGear] Loaded config from: " + path);
                if (!Enum.TryParse<KeyCode>(_config.HotkeyCode, out _hotkey))
                {
                    Debug.Log(
                        "[QuickGear] Invalid hotkey '" + _config.HotkeyCode + "', defaulting to G."
                    );
                    _hotkey = KeyCode.G;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to load config, using defaults. Error: " + e.Message);
            }
        }

        private static void SaveConfig()
        {
            try
            {
                string path =
                    (_currentSlot >= 0) ? SlotConfigPath(_currentSlot) : DefaultConfigPath;
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(path, json);
                Debug.Log("[QuickGear] Saved config to: " + path);
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to save config. Error: " + e.Message);
            }
        }

        public static void SaveEquipment(Mercenary merc)
        {
            string profileId = merc.ProfileId;
            var savedEquip = new ModConfig.SavedEquipment();
            var inventory = merc.CreatureData.Inventory;

            Debug.Log($"[QuickGear] Saving equipment for profileId={profileId} (raw profile key).");
            Debug.Log($"[QuickGear] Current save slot: {_currentSlot}");

            // Save equipment slots
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

            if (_config.SaveImplants)
            {
                // Save limbs (augmentations)
                foreach (var kvp in merc.CreatureData.AugmentationMap)
                {
                    savedEquip.Limbs[kvp.Key] = kvp.Value;
                }

                // Save implants
                foreach (var kvp in merc.CreatureData.WoundSlotMap)
                {
                    var implantIds = kvp.Value.InstalledImplantsData.Select(i => i.ImplantId).ToList();
                    if (implantIds.Any())
                    {
                        savedEquip.Implants[kvp.Key] = implantIds;
                    }
                }
            }

            _config.SavedEquipmentHistory[profileId] = savedEquip;
            SaveConfig();
            Debug.Log($"[QuickGear] Saved equipment for {profileId}");
        }

        public static void EquipQuickGear(Mercenary merc)
        {
            if (_modContext == null)
            {
                Debug.Log("[QuickGear] No mod context available.");
                return;
            }

            MagnumCargo cargo = _modContext.State.Get<MagnumCargo>();
            Mercenaries mercenaries = _modContext.State.Get<Mercenaries>();

            if (cargo == null || mercenaries == null || mercenaries.Values.Count == 0)
            {
                Debug.Log("[QuickGear] State not ready.");
                return;
            }

            Debug.Log(
                "[QuickGear] Running quick gear. Config contents: "
                    + JsonConvert.SerializeObject(_config, Formatting.Indented)
            );

            foreach (ModConfig.ItemEntry entry in _config.Items)
            {
                PullFromCargo(cargo, new List<Mercenary> { merc }, entry.ItemId, entry.Count);
            }
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

            _config.Items = counts
                .Select(kvp => new ModConfig.ItemEntry { ItemId = kvp.Key, Count = kvp.Value })
                .ToList();

            SaveConfig();
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
            var magnumCargo = _modContext.State.Get<MagnumCargo>();
            if (magnumCargo == null)
            {
                Debug.Log("[QuickGear] No magnum cargo available for saved equipment load.");
                return;
            }

            ItemStorage cargoFallback = magnumCargo.ShipCargo.FirstOrDefault();
            UnequipAllEquipment(merc, cargoFallback);

            var allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
            Debug.Log($"[QuickGear] After unequip, inventory contains {allItems.Count} items.");
            var shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
            Debug.Log($"[QuickGear] Ship cargo has {shipCargoItems.Count} items available.");
            var perkFactory = _modContext.State.Get<PerkFactory>();

            if (cargoFallback != null)
            {
                Debug.Log("[QuickGear] Clearing augmentations/implants from first ship cargo container.");
                AugmentationSystem.RemoveAllAugmentationsAndImplants(
                    merc,
                    cargoFallback
                );
                shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
            }
            else
            {
                Debug.Log("[QuickGear] No magnum cargo available to clear implants and limbs.");
            }

            List<string> missingItems = new List<string>();
            List<string> failedLimbs = new List<string>();
            List<string> failedImplants = new List<string>();

            // 1. Load limbs first
            foreach (var kvp in savedEquip.Limbs)
            {
                string woundSlotId = kvp.Key;
                string augId = kvp.Value;
                Debug.Log($"[QuickGear] Loading limb {augId} into slot {woundSlotId}.");

                var limbItem = allItems.FirstOrDefault(i => i.Id == augId);
                bool foundInCargo = false;
                if (limbItem == null)
                {
                    limbItem = shipCargoItems.FirstOrDefault(i => i.Id == augId);
                    if (limbItem != null)
                    {
                        foundInCargo = true;
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

            // 2. Load implants second
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
                            shipCargoItems = magnumCargo.ShipCargo
                                .SelectMany(c => c.Items)
                                .ToList();
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

            // 3. Load equipment last
            foreach (var kvp in savedEquip.Equipment)
            {
                string slotName = kvp.Key;
                string itemId = kvp.Value;
                Debug.Log($"[QuickGear] Loading equipment {itemId} into slot {slotName}.");

                var item = allItems.FirstOrDefault(i => i.Id == itemId);
                bool foundInCargo = false;
                if (item == null)
                {
                    item = shipCargoItems.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        foundInCargo = true;
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

                bool equipped = inventory.TakeOrEquip(item, putIfSlotBusy: true);
                Debug.Log($"[QuickGear] Equip attempt for {itemId} into {slotName}: {equipped}.");
                if (!equipped)
                {
                    Debug.Log($"[QuickGear] Failed to equip {itemId} into slot {slotName}.");
                }

                // If the item still exists in ship cargo, remove it to avoid duplicates on repeated loads
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
            RefreshArsenalScreen(merc);

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
            return TryGetSavedEquipment(merc.ProfileId, out _);
        }

        private static void RefreshArsenalScreen(Mercenary merc)
        {
            if (
                merc == null
                || !UI.IsShowing<ArsenalScreen>()
            )
            {
                return;
            }

            ArsenalScreen screen = UI.Get<ArsenalScreen>();

            if (screen == null)
                return;

            screen.RefreshView();

            Debug.Log(
                "[QuickGear] Arsenal screen refreshed."
            );
        }

        private static bool TryGetSavedEquipment(
            string profileId,
            out ModConfig.SavedEquipment savedEquip
        )
        {
            if (_config.SavedEquipmentHistory.TryGetValue(profileId, out savedEquip))
            {
                Debug.Log($"[QuickGear] Found saved equipment for exact key: {profileId}");
                return true;
            }

            string normalized = NormalizeProfileId(profileId);
            Debug.Log($"[QuickGear] Normalized {profileId} to {normalized}");
            Debug.Log(
                $"[QuickGear] Available keys: {string.Join(", ", _config.SavedEquipmentHistory.Keys)}"
            );

            if (
                normalized != profileId
                && _config.SavedEquipmentHistory.TryGetValue(normalized, out savedEquip)
            )
            {
                Debug.Log($"[QuickGear] Found saved equipment for normalized key: {normalized}");
                return true;
            }

            Debug.Log(
                $"[QuickGear] No saved equipment found for {profileId} (normalized: {normalized})"
            );
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

        private static Mercenary GetSelectedMerc()
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

        [HarmonyPatch(typeof(SpaceGameMode), "StartMission")]
        public static class SpaceGameMode_StartMission_Patch
        {
            public static void Prefix(SpaceModeFinishedData data, Mission mission, bool saveGame)
            {
                if (_modContext == null)
                {
                    Debug.Log("[QuickGear] No mod context available.");
                    return;
                }

                if (data.mercProfileId != null)
                {
                    Mercenaries mercenaries = _modContext.State.Get<Mercenaries>();
                    Mercenary merc = mercenaries.Get(data.mercProfileId);
                    if (merc != null)
                    {
                        ModMain.SaveEquipment(merc);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ArsenalScreen), "Configure")]
        public static class ArsenalScreen_Configure_Patch
        {
            private class QuickGearLocalizedLabel : MonoBehaviour
            {
                public string LocalizationKey;

                private Text _label;
                private string _lastLanguage;

                private void Start()
                {
                    _label = GetComponent<Text>();
                    UpdateText();
                }

                private void Update()
                {
                    if (_label == null)
                        return;

                    string currentLanguage =
                        Singleton<Localization>.Instance.CurrentLang.ToString();

                    if (currentLanguage == _lastLanguage)
                        return;

                    UpdateText();
                }

                private void UpdateText()
                {
                    if (_label == null || string.IsNullOrEmpty(LocalizationKey))
                        return;

                    _lastLanguage =
                        Singleton<Localization>.Instance.CurrentLang.ToString();

                    _label.text = GetLocalizedButtonText(LocalizationKey);
                }
            }

            private static Vector2 TogglePoistion = new Vector2(353f, 122.8f);

            public static void Postfix(ArsenalScreen __instance, Mercenary mercenary)
            {
                try
                {
                    if (mercenary == null)
                        return;

                    Debug.Log(
                        "[QuickGear] ArsenalScreen.Configure called for: " + mercenary.ProfileId
                    );

                    GameObject inventoryWindow = Traverse
                        .Create(__instance)
                        .Field<GameObject>("_inventoryWindow")
                        .Value;

                    Transform parent =
                        (inventoryWindow != null)
                            ? inventoryWindow.transform
                            : __instance.transform;

                    var existingButton = parent.Find("QuickRestockButton");
                    if (existingButton != null)
                    {
                        Debug.Log("[QuickGear] QuickGear buttons already exist; updating for new merc.");
                        UpdateExistingQuickGearButtons(parent, mercenary);

                        CreateSaveImplantsToggle(
                            parent,
                            TogglePoistion
                            );

                        return;
                    }

                    Debug.Log(
                        "[QuickGear] Creating QuickRestockButtons in ArsenalScreen under "
                            + parent.name
                    );

                    // Create three smaller buttons (25% of previous size) in top-right of inventory window
                    // Place buttons at the parent-local coordinates you provided (no snapping)
                    // Base parent-local point reported: (196.3, 122.8)

                    // New baseLocal (185f, 122.8f) - for space on implant toggle

                    Vector2 baseLocal = new Vector2(185f, 122.8f);

                    string mercName = !string.IsNullOrEmpty(mercenary.AgentName)
                        ? mercenary.AgentName
                        : mercenary.ProfileId;

                    CreateQuickGearButton(
                        parent,
                        "QuickRestockButton",
                        "Quick Restock",
                        baseLocal + new Vector2(0f, 0f),
                        40f,
                        16f,
                        () =>
                        {
                            try
                            {
                                ModMain.EquipQuickGear(mercenary);
                            }
                            catch (Exception e)
                            {
                                Debug.Log("[QuickGear] Error running Quick Restock: " + e.Message);
                            }
                        },
                        "Pulls configured items from cargo to inventory, this equipment list is shared between all mercenary profiles.\n\nIdeal for items that are frequently used and need to be restocked quickly, such as medkits or consumables.",
                        QuickGearButton.UpdateMode.QuickRestock,
                        null
                    );

                    CreateQuickGearButton(
                        parent,
                        "LoadSavedEquipmentButton",
                        "Load equipment",
                        baseLocal + new Vector2(46f, 0f),
                        40f,
                        16f,
                        () =>
                        {
                            try
                            {
                                Debug.Log(
                                    "[QuickGear] Load Saved Equipment clicked: loading saved equipment."
                                );
                                ModMain.LoadSavedEquipment(mercenary);
                            }
                            catch (Exception e)
                            {
                                Debug.Log(
                                    "[QuickGear] Error loading saved equipment: " + e.Message
                                );
                            }
                        },
                        "Load saved equipment, limbs, and implants for this mercenary.",
                        QuickGearButton.UpdateMode.LoadSavedEquipment,
                        mercenary.ProfileId
                    );

                    CreateQuickGearButton(
                        parent,
                        "SaveEquipmentButton",
                        "Save equipment",
                        baseLocal + new Vector2(92f, 0f),
                        40f,
                        16f,
                        () =>
                        {
                            try
                            {
                                Debug.Log("[QuickGear] Save Equipment clicked: saving equipment.");
                                ModMain.SaveEquipment(mercenary);
                            }
                            catch (Exception e)
                            {
                                Debug.Log("[QuickGear] Error saving equipment: " + e.Message);
                            }
                        },
                        "Save current equipped items, limbs, and implants for this mercenary.",
                        QuickGearButton.UpdateMode.SaveEquipment,
                        mercenary.ProfileId
                    );

                    CreateQuickGearButton(
                        parent,
                        "SaveInventoryButton",
                        "Update Quick Restock",
                        baseLocal + new Vector2(138f, 0f),
                        40f,
                        16f,
                        () =>
                        {
                            try
                            {
                                Debug.Log(
                                    "[QuickGear] Save Inventory clicked: saving current inventory to quick equip config."
                                );
                                ModMain.SaveInventoryQuickGear(mercenary);
                            }
                            catch (Exception e)
                            {
                                Debug.Log(
                                    "[QuickGear] Error saving inventory quick equip: " + e.Message
                                );
                            }
                        },
                        "Save the current inventory items into the quick restock configuration. \n\nSaves to a shared configuration for all mercenary profiles.",
                        QuickGearButton.UpdateMode.SaveInventory,
                        mercenary.ProfileId
                    );

                    CreateSaveImplantsToggle(
                            parent,
                            TogglePoistion
                            );

                }
                catch (Exception e)
                {
                    Debug.Log("[QuickGear] Exception in ArsenalScreen patch: " + e.Message);
                    Debug.Log("[QuickGear] " + e.StackTrace);
                }
            }

            private static void CreateQuickGearButton(
                Transform parent,
                string objectName,
                string baseLabel,
                Vector2 anchoredPosition,
                float width,
                float height,
                Action onClick,
                string localizationKey = null,
                QuickGearButton.UpdateMode updateMode = QuickGearButton.UpdateMode.None,
                string mercProfileId = null
            )
            {
                if (parent.Find(objectName) != null)
                    return;

                var buttonObj = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );
                buttonObj.layer = LayerMask.NameToLayer("UI");
                buttonObj.transform.SetParent(parent, false);

                var rect = buttonObj.GetComponent<RectTransform>();
                // Use center anchoring so parent-local coordinates (from debug) map correctly
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = new Vector2(width, height);
                rect.localScale = Vector3.one;

                // Ensure layout systems (if parent has a LayoutGroup) respect our fixed size
                var layout = buttonObj.AddComponent<LayoutElement>();
                layout.preferredWidth = width;
                layout.preferredHeight = height;
                layout.minWidth = width;
                layout.minHeight = height;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;

                var img = buttonObj.GetComponent<Image>();
                img.color = new Color(25f / 255f, 32f / 255f, 33f / 255f, 0.95f);
                img.type = Image.Type.Sliced;
                img.raycastTarget = true;

                var outline = buttonObj.AddComponent<Outline>();
                outline.effectColor = new Color(79f / 255f, 114f / 255f, 102f / 255f, 0.95f);
                outline.effectDistance = new Vector2(1f, -1f);

                var button = buttonObj.GetComponent<Button>();
                button.targetGraphic = img;
                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick());
                }

                var captionObj = new GameObject(
                    "Caption",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text)
                );
                captionObj.transform.SetParent(buttonObj.transform, false);
                captionObj.layer = buttonObj.layer;
                var captionRect = captionObj.GetComponent<RectTransform>();
                captionRect.anchorMin = Vector2.zero;
                captionRect.anchorMax = Vector2.one;
                captionRect.offsetMin = new Vector2(2f, 2);
                captionRect.offsetMax = new Vector2(-2f, -2f);

                var txt = captionObj.GetComponent<Text>();
                // initial label; if updateMode provided we'll let the component update it
                txt.text = baseLabel;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.color = Color.white;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                // Keep text simple and small per user preference
                txt.resizeTextForBestFit = false;
                txt.fontSize = _fontSize;
                txt.raycastTarget = false;

                if (!string.IsNullOrEmpty(localizationKey))
                {
                    var tooltip =  buttonObj.AddComponent<QuickGearTooltip>();
                    tooltip.LocalizationKey = localizationKey;
                }

                if (updateMode != QuickGearButton.UpdateMode.None)
                {
                    var updater = buttonObj.AddComponent<QuickGearButton>();
                    updater.Mode = updateMode;
                    updater.BaseText = baseLabel;
                    updater.MercProfileId = mercProfileId;
                }

                // No persistent mouse logging — debug removed to reduce log spam
            }

            private static void CreateSaveImplantsToggle(
                Transform parent,
                Vector2 position
                )
            {
                if (parent == null)
                    return;

                if (_config == null)
                    _config = new ModConfig();

                GameObject toggleObject = parent
                    .Find("SaveImplantsToggle")
                    ?.gameObject;

                if (toggleObject == null)
                {
                    toggleObject = new GameObject(
                        "SaveImplantsToggle",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Toggle)
                    );

                    toggleObject.layer = LayerMask.NameToLayer("UI");
                    toggleObject.transform.SetParent(parent, false);
                    toggleObject.transform.SetAsLastSibling();

                    var toggleRect =
                        toggleObject.GetComponent<RectTransform>();

                    toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
                    toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
                    toggleRect.pivot = new Vector2(0.5f, 0.5f);
                    toggleRect.anchoredPosition = position;
                    toggleRect.sizeDelta = new Vector2(110f, 18f);
                    toggleRect.localScale = Vector3.one;

                    var backgroundObject = new GameObject(
                        "Background",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image)
                    );

                    backgroundObject.layer = toggleObject.layer;
                    backgroundObject.transform.SetParent(
                        toggleObject.transform,
                        false
                    );

                    var backgroundRect =
                        backgroundObject.GetComponent<RectTransform>();

                    backgroundRect.anchorMin = new Vector2(1f, 0.5f);
                    backgroundRect.anchorMax = new Vector2(1f, 0.5f);
                    backgroundRect.pivot = new Vector2(1f, 0.5f);
                    backgroundRect.sizeDelta = new Vector2(14f, 14f);

                    var background =
                        backgroundObject.GetComponent<Image>();

                    var checkboxSprite =
                        Resources.GetBuiltinResource<Sprite>(
                            "UI/Skin/UISprite.psd"
                        );

                    var panelObject = new GameObject(
                        "TogglePanel",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image)
                        );

                    panelObject.layer = toggleObject.layer;
                    panelObject.transform.SetParent(
                        toggleObject.transform,
                        false
                    );
                    var panelRect =
                        panelObject.GetComponent<RectTransform>();
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                    panelRect.sizeDelta = new Vector2(60f, 16f);

                    //Position background
                    panelRect.anchoredPosition = new Vector2(26f, 0f);

                    var panelImage =
                        panelObject.GetComponent<Image>();
                    panelImage.sprite = checkboxSprite;
                    panelImage.type = Image.Type.Sliced;
                    panelImage.color = new Color(
                        25f / 255f,
                        32f / 255f,
                        33f / 255f,
                        0.95f
                    );

                    panelImage.raycastTarget = false;
                    var panelOutline =
                        panelObject.AddComponent<Outline>();
                    panelOutline.effectColor = new Color(
                        79f / 255f,
                        114f / 255f,
                        102f / 255f,
                        0.95f
                    );
                    panelOutline.effectDistance = new Vector2(1f, -1f);
                    panelObject.transform.SetAsFirstSibling();

                    background.sprite = checkboxSprite;
                    background.type = Image.Type.Sliced;
                    background.color = new Color(
                        25f / 255f,
                        32f / 255f,
                        33f / 255f,
                        1f
                    );
                    background.raycastTarget = true;

                    var outline = backgroundObject.AddComponent<Outline>();
                    outline.effectColor = new Color(
                        132f / 255f,
                        190f / 255f,
                        155f / 255f,
                        1f
                    );
                    outline.effectDistance = new Vector2(1f, -1f);

                    var checkmarkObject = new GameObject(
                        "Checkmark",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image)
                    );

                    checkmarkObject.layer = toggleObject.layer;
                    checkmarkObject.transform.SetParent(
                        backgroundObject.transform,
                        false
                    );

                    var checkmarkRect =
                        checkmarkObject.GetComponent<RectTransform>();

                    checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
                    checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
                    checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
                    checkmarkRect.anchoredPosition = Vector2.zero;
                    checkmarkRect.sizeDelta = new Vector2(9f, 9f);

                    var checkmark =
                        checkmarkObject.GetComponent<Image>();

                    checkmark.sprite = checkboxSprite;
                    checkmark.color = new Color(
                        132f / 255f,
                        190f / 255f,
                        155f / 255f,
                        1f
                    );
                    checkmark.raycastTarget = false;

                    var labelObject = new GameObject(
                        "Caption",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Text)
                    );

                    labelObject.layer = toggleObject.layer;
                    labelObject.transform.SetParent(
                        toggleObject.transform,
                        false
                    );

                    var labelRect =
                        labelObject.GetComponent<RectTransform>();

                    labelRect.anchorMax = new Vector2(0f, 0.5f);
                    labelRect.pivot = new Vector2(0f, 0.5f);
                    labelRect.anchoredPosition = new Vector2(4.5f, 0f);
                    labelRect.sizeDelta = new Vector2(82f, 18f);
                    labelRect.localScale = Vector3.one;

                    var label =
                        labelObject.GetComponent<Text>();

                    label.alignment = TextAnchor.MiddleCenter;

                    label.text = GetLocalizedButtonText("Keep implants");
                    var localizedLabel = labelObject.AddComponent<QuickGearLocalizedLabel>();
                    localizedLabel.LocalizationKey = "Keep implants";

                    label.font = Resources.GetBuiltinResource<Font>(
                        "Arial.ttf"
                    );
                    label.fontSize = _fontSize;
                    label.color = Color.white;
                    label.horizontalOverflow =
                        HorizontalWrapMode.Overflow;
                    label.verticalOverflow =
                        VerticalWrapMode.Truncate;
                    label.raycastTarget = false;

                    panelImage.raycastTarget = true;
                    var tooltip =
                        panelObject.AddComponent<QuickGearTooltip>();
                    tooltip.LocalizationKey =
                        "When enabled, equipment sets include augments and implants.";

                    var toggle =
                        toggleObject.GetComponent<Toggle>();

                    toggle.targetGraphic = background;
                    toggle.graphic = checkmark;
                    toggle.toggleTransition =
                        Toggle.ToggleTransition.Fade;
                }

                var saveToggle =
                    toggleObject.GetComponent<Toggle>();

                if (saveToggle == null)
                    return;

                saveToggle.onValueChanged.RemoveAllListeners();

                saveToggle.isOn = _config.SaveImplants;

                saveToggle.onValueChanged.AddListener(value =>
                {
                    _config.SaveImplants = value;
                    ModMain.SaveConfig();

                    Debug.Log(
                        "[QuickGear] Save augments and implants: "
                            + value
                    );
                });

                toggleObject.SetActive(true);
                toggleObject.transform.SetAsLastSibling();

                Debug.Log(
                    "[QuickGear] SaveImplantsToggle created at "
                        + position
                );
            }

            private static int GetQuickRestockItemCount()
            {
                return _config.Items?.Sum(item => Math.Max(0, item.Count)) ?? 0;
            }

            private static int GetSavedEquipmentCount(Mercenary merc)
            {
                if (merc == null || !TryGetSavedEquipment(merc.ProfileId, out var savedEquip))
                {
                    return 0;
                }

                return CountSavedEquipmentItems(savedEquip);
            }

            private static int GetCurrentEquipmentCount(Mercenary merc)
            {
                if (merc == null)
                    return 0;

                var inventory = merc.CreatureData?.Inventory;
                if (inventory == null)
                    return 0;

                int count = 0;
                foreach (var slot in new[]
                    {
                        inventory.BackpackSlot,
                        inventory.PrimarySlot,
                        inventory.SecondarySlot,
                        inventory.ServoArmSlot,
                        inventory.AdditionalSlot,
                        inventory.ArmorSlot,
                        inventory.HelmetSlot,
                        inventory.LeggingsSlot,
                        inventory.BootsSlot,
                        inventory.VestSlot
                    })
                {
                    if (slot?.First != null)
                    {
                        count += 1;
                    }
                }

                return count;
            }

            private static int GetInventorySaveCount(Mercenary merc)
            {
                if (merc == null)
                    return 0;

                var inventory = merc.CreatureData?.Inventory;
                if (inventory == null)
                    return 0;

                int total = 0;
                foreach (ItemStorage storage in inventory.Storages)
                {
                    if (storage == null)
                        continue;

                    foreach (BasePickupItem item in storage.Items)
                    {
                        if (item == null)
                            continue;

                        total += item.IsStackable ? item.StackCount : 1;
                    }
                }

                return total;
            }

            private static int CountSavedEquipmentItems(ModConfig.SavedEquipment savedEquip)
            {
                if (savedEquip == null)
                    return 0;

                int count = savedEquip.Equipment?.Count ?? 0;
                count += savedEquip.Limbs?.Count ?? 0;
                count += savedEquip.Implants?.Values.Sum(list => list?.Count ?? 0) ?? 0;
                return count;
            }

            private static void UpdateExistingQuickGearButtons(Transform parent, Mercenary mercenary)
            {
                UpdateQuickGearButton(parent, "QuickRestockButton", () => ModMain.EquipQuickGear(mercenary), QuickGearButton.UpdateMode.QuickRestock, null);
                UpdateQuickGearButton(parent, "LoadSavedEquipmentButton", () =>
                {
                    Debug.Log("[QuickGear] Load Saved Equipment clicked: loading saved equipment.");
                    ModMain.LoadSavedEquipment(mercenary);
                }, QuickGearButton.UpdateMode.LoadSavedEquipment, mercenary.ProfileId);
                UpdateQuickGearButton(parent, "SaveEquipmentButton", () =>
                {
                    Debug.Log("[QuickGear] Save Equipment clicked: saving equipment.");
                    ModMain.SaveEquipment(mercenary);
                }, QuickGearButton.UpdateMode.SaveEquipment, mercenary.ProfileId);
                UpdateQuickGearButton(parent, "SaveInventoryButton", () =>
                {
                    Debug.Log("[QuickGear] Save Inventory clicked: saving current inventory to quick equip config.");
                    ModMain.SaveInventoryQuickGear(mercenary);
                }, QuickGearButton.UpdateMode.SaveInventory, mercenary.ProfileId);
            }

            private static void UpdateQuickGearButton(
                Transform parent,
                string objectName,
                Action onClick,
                QuickGearButton.UpdateMode mode,
                string mercProfileId
            )
            {
                var buttonObj = parent.Find(objectName);
                if (buttonObj == null)
                    return;

                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    if (onClick != null)
                        button.onClick.AddListener(() => onClick());
                }

                var updater = buttonObj.GetComponent<QuickGearButton>();
                if (updater != null)
                {
                    updater.Mode = mode;
                    updater.MercProfileId = mercProfileId;
                    updater.UpdateLabelImmediate();
                }
            }

            private static string FormatButtonLabel(string baseText, int count)
            {
                return count > 0 ? $"{baseText}: {count}" : baseText;
            }

            private class QuickGearTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
            {
                public string LocalizationKey;
                private bool _createdTooltip;

                public void OnPointerEnter(PointerEventData eventData)
                {
                    if (_createdTooltip || string.IsNullOrEmpty(LocalizationKey))
                        return;

                    string localizationKey =
                        GetLocalizedButtonText(LocalizationKey);
                    _createdTooltip = true;

                    if (SingletonMonoBehaviour<TooltipFactory>.Instance != null)
                    {
                        SingletonMonoBehaviour<TooltipFactory>.Instance
                            .ShowSimpleTextTooltip(localizationKey);
                    }
                }

                public void OnPointerExit(PointerEventData eventData)
                {
                    if (!_createdTooltip)
                        return;

                    _createdTooltip = false;
                    if (SingletonMonoBehaviour<TooltipFactory>.Instance != null)
                    {
                        SingletonMonoBehaviour<TooltipFactory>.Instance.HideSimpleTextTooltip();
                    }
                }
            }

            private class QuickGearButton : MonoBehaviour
            {
                public enum UpdateMode { None = 0, QuickRestock = 1, LoadSavedEquipment = 2, SaveEquipment = 3, SaveInventory = 4 }
                public UpdateMode Mode = UpdateMode.None;
                public string BaseText;
                public string MercProfileId;

                private Text _caption;
                private float _nextUpdate;

                private void Start()
                {
                    var t = transform.Find("Caption");
                    if (t != null)
                        _caption = t.GetComponent<Text>();
                    _nextUpdate = Time.time;
                    UpdateLabelImmediate();
                }

                private void Update()
                {
                    if (Time.time < _nextUpdate)
                        return;
                    _nextUpdate = Time.time + 0.5f;
                    UpdateLabelImmediate();
                }

                public void UpdateLabelImmediate()
                {
                    if (_caption == null || string.IsNullOrEmpty(BaseText))
                        return;

                    int count = 0;
                    try
                    {
                        switch (Mode)
                        {
                            case UpdateMode.QuickRestock:
                                count = GetQuickRestockItemCount();
                                break;
                            case UpdateMode.LoadSavedEquipment:
                                {
                                    var merc = ResolveMerc();
                                    count = GetSavedEquipmentCount(merc);
                                }
                                break;
                            case UpdateMode.SaveEquipment:
                                {
                                    var merc = ResolveMerc();
                                    count = GetCurrentEquipmentCount(merc);
                                }
                                break;
                            case UpdateMode.SaveInventory:
                                {
                                    var merc = ResolveMerc();
                                    count = GetInventorySaveCount(merc);
                                }
                                break;
                        }
                    }
                    catch { }

                    string buttonText = GetLocalizedButtonText(BaseText);
                    _caption.text = FormatButtonLabel(buttonText, count);
                }

                private Mercenary ResolveMerc()
                {
                    if (string.IsNullOrEmpty(MercProfileId) || ModMain._modContext == null)
                        return null;
                    var mercs = ModMain._modContext.State.Get<Mercenaries>();
                    if (mercs == null)
                        return null;
                    return mercs.Get(MercProfileId);
                }
            }

            private static void ClearHotkey(CommonButton button)
            {
                HotkeyButton hotkeyButton = button as HotkeyButton;
                if (hotkeyButton == null)
                {
                    Debug.Log("[QuickGear] Button is not a HotkeyButton, skipping hotkey clear.");
                    return;
                }

                Debug.Log("[QuickGear] Clearing hotkey for: " + button.gameObject.name);

                GameKeyPanel panel = Traverse
                    .Create(hotkeyButton)
                    .Field<GameKeyPanel>("_gameKeyPanel")
                    .Value;

                if (panel != null)
                {
                    Debug.Log("[QuickGear] Found GameKeyPanel, disabling it.");
                    panel.gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log("[QuickGear] GameKeyPanel is null.");
                }

                Traverse.Create(hotkeyButton).Field("_keyId").SetValue(string.Empty);
            }

            private static string GetLocalizedButtonText(string key)
            {
                string language =
                    Singleton<Localization>.Instance.CurrentLang.ToString();

                if (
                    _localization?.Languages != null
                    && _localization.Languages.TryGetValue(
                        language,
                        out var languageTexts
                    )
                    && languageTexts.TryGetValue(key, out var localizedText)
                    && !string.IsNullOrWhiteSpace(localizedText)
                )
                {
                    return localizedText;
                }

                return key;
            }

            // QuickGearDebug removed — no runtime mouse logging to avoid log spam

            private static void RepositionButton(
                CommonButton button,
                CommonButton referenceButton,
                float horizontalOffset
            )
            {
                RectTransform buttonRect = button.transform as RectTransform;
                RectTransform refRect = referenceButton.transform as RectTransform;

                if (buttonRect == null || refRect == null)
                {
                    Debug.Log("[QuickGear] RectTransform is null!");
                    return;
                }

                Debug.Log($"[QuickGear] Positioning {button.gameObject.name}");
                Debug.Log(
                    $"[QuickGear]   Reference button parent: {referenceButton.gameObject.name}"
                );
                Debug.Log($"[QuickGear]   Reference button position: {refRect.anchoredPosition}");
                Debug.Log($"[QuickGear]   Reference button size: {refRect.sizeDelta}");
                Debug.Log($"[QuickGear]   New position offset: {horizontalOffset}");

                // Set anchors to top-left corner (0, 1) relative to parent
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(0f, 1f);
                // Keep pivot the same as reference button for consistency if needed, or set to (0.5, 0.5)
                buttonRect.pivot = refRect.pivot;
                // Set position: horizontalOffset is X, and we assume Y=0 places it correctly relative to other top-left elements.
                buttonRect.anchoredPosition = new Vector2(horizontalOffset, 0f);
                buttonRect.sizeDelta = refRect.sizeDelta;
                buttonRect.localScale = refRect.localScale;

                Debug.Log($"[QuickGear]   New button position: {buttonRect.anchoredPosition}");
                Debug.Log($"[QuickGear]   New button size: {buttonRect.sizeDelta}");
            }
        }
    }
}
