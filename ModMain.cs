using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MGSC;
using Newtonsoft.Json;
using UnityEngine;
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
                HotkeyCode = "G"
            };

        private static ModConfig _config = new ModConfig();
        private static KeyCode _hotkey = KeyCode.G;
        private static readonly Harmony _harmony = new Harmony("QuickGear");
        private static int _currentSlot = -1;
        public static IModContext _modContext;

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
        }

        [Hook(ModHookType.MainMenuStarted)]
        public static void OnMainMenuStarted(IModContext context)
        {
            Debug.Log("[QuickGear] Main menu started. Auto-load is disabled to avoid runtime crashes.");
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
                        Debug.Log("[QuickGear] Single hotkey press confirmed. Running quick equip.");
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

            bool isDoublePress = _pendingQuickEquip
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
                    Debug.Log("[QuickGear] Previous single press timeout expired. Running quick equip.");
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
            string profileId = NormalizeProfileId(merc.ProfileId);
            var savedEquip = new ModConfig.SavedEquipment();
            var inventory = merc.CreatureData.Inventory;

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

        public static void LoadSavedEquipment(Mercenary merc)
        {
            string profileId = merc.ProfileId;
            if (!TryGetSavedEquipment(profileId, out var savedEquip))
            {
                Debug.Log($"[QuickGear] No saved equipment for {profileId}");
                return;
            }

            var inventory = merc.CreatureData.Inventory;
            UnequipAllEquipment(merc);
            var allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
            var magnumCargo = _modContext.State.Get<MagnumCargo>();
            if (magnumCargo == null)
            {
                Debug.Log("[QuickGear] No magnum cargo available for saved equipment load.");
                return;
            }

            var shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
            var perkFactory = _modContext.State.Get<PerkFactory>();

            if (magnumCargo.ShipCargo.Count > 0)
            {
                AugmentationSystem.RemoveAllAugmentationsAndImplants(
                    merc,
                    magnumCargo.ShipCargo[0]
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

                var limbItem = allItems.FirstOrDefault(i => i.Id == augId);
                if (limbItem == null)
                {
                    limbItem = shipCargoItems.FirstOrDefault(i => i.Id == augId);
                    if (limbItem != null)
                    {
                        PullFromCargo(magnumCargo, new List<Mercenary> { merc }, augId, 1);
                        allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                        shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
                        limbItem = allItems.FirstOrDefault(i => i.Id == augId);
                    }
                }

                if (limbItem == null)
                {
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
                    failedLimbs.Add($"{woundSlotId}:{augId}");
                }
                else
                {
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
                    var implantItem = allItems.FirstOrDefault(i => i.Id == implantId);
                    if (implantItem == null)
                    {
                        implantItem = shipCargoItems.FirstOrDefault(i => i.Id == implantId);
                        if (implantItem != null)
                        {
                            PullFromCargo(magnumCargo, new List<Mercenary> { merc }, implantId, 1);
                            allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                            shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
                            implantItem = allItems.FirstOrDefault(i => i.Id == implantId);
                        }
                    }

                    if (implantItem == null)
                    {
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
                        failedImplants.Add($"{woundSlotId}:{implantId}");
                    }
                    else
                    {
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

                var item = allItems.FirstOrDefault(i => i.Id == itemId);
                if (item == null)
                {
                    item = shipCargoItems.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        PullFromCargo(magnumCargo, new List<Mercenary> { merc }, itemId, 1);
                        allItems = inventory.AllContainers.SelectMany(c => c.Items).ToList();
                        shipCargoItems = magnumCargo.ShipCargo.SelectMany(c => c.Items).ToList();
                        item = allItems.FirstOrDefault(i => i.Id == itemId);
                    }
                }

                if (item == null)
                {
                    missingItems.Add(itemId);
                    continue;
                }

                ItemStorage slot = GetSlotByName(inventory, slotName);
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
            return TryGetSavedEquipment(merc.ProfileId, out _);
        }

        private static bool TryGetSavedEquipment(string profileId, out ModConfig.SavedEquipment savedEquip)
        {
            if (_config.SavedEquipmentHistory.TryGetValue(profileId, out savedEquip))
            {
                Debug.Log($"[QuickGear] Found saved equipment for exact key: {profileId}");
                return true;
            }

            string normalized = NormalizeProfileId(profileId);
            Debug.Log($"[QuickGear] Normalized {profileId} to {normalized}");
            Debug.Log($"[QuickGear] Available keys: {string.Join(", ", _config.SavedEquipmentHistory.Keys)}");
            
            if (normalized != profileId && _config.SavedEquipmentHistory.TryGetValue(normalized, out savedEquip))
            {
                Debug.Log($"[QuickGear] Found saved equipment for normalized key: {normalized}");
                return true;
            }

            Debug.Log($"[QuickGear] No saved equipment found for {profileId} (normalized: {normalized})");
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

        private static void UnequipAllEquipment(Mercenary merc)
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
                    if (!inventory.Unequip(item))
                    {
                        Debug.Log($"[QuickGear] Failed to unequip {item.Id} from {slot.Source}.");
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

                    Transform parent = (inventoryWindow != null)
                        ? inventoryWindow.transform
                        : __instance.transform;

                    var existingButton = parent.Find("QuickRestockButton");
                    if (existingButton != null)
                        return;

                    Debug.Log(
                        "[QuickGear] Creating QuickRestockButtons in ArsenalScreen under "
                            + parent.name
                    );

                    // Create three smaller buttons (25% of previous size) in top-right of inventory window
                    // Place buttons at the parent-local coordinates you provided (no snapping)
                    // Base parent-local point reported: (196.3, 122.8)
                    Vector2 baseLocal = new Vector2(196.3f, 122.8f);

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
                        }
                    );

                    CreateQuickGearButton(
                        parent,
                        "LoadSavedEquipmentButton",
                        "Load Saved Equipment",
                        baseLocal + new Vector2(46f, 0f),
                        40f,
                        16f,
                        () =>
                        {
                            try
                            {
                                Debug.Log("[QuickGear] Load Saved Equipment clicked: loading saved equipment.");
                                ModMain.LoadSavedEquipment(mercenary);
                            }
                            catch (Exception e)
                            {
                                Debug.Log("[QuickGear] Error loading saved equipment: " + e.Message);
                            }
                        }
                    );

                    CreateQuickGearButton(
                        parent,
                        "SaveEquipmentButton",
                        "Save Equipment",
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
                        }
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
                string label,
                Vector2 anchoredPosition,
                float width,
                float height,
                Action onClick
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

                var img = buttonObj.GetComponent<Image>();
                img.color = new Color(0.12f, 0.56f, 0.9f, 0.95f);
                img.type = Image.Type.Sliced;
                img.raycastTarget = true;

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
                captionRect.offsetMin = new Vector2(4f, 4f);
                captionRect.offsetMax = new Vector2(-4f, -4f);

                var txt = captionObj.GetComponent<Text>();
                txt.text = label;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.color = Color.white;
                // Keep text simple and small per user preference
                txt.resizeTextForBestFit = false;
                txt.fontSize = 3;
                txt.raycastTarget = false;

                // No persistent mouse logging — debug removed to reduce log spam
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
