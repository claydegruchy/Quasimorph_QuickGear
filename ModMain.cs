using System;
using System.Collections.Generic;
using System.IO;
using MGSC;
using Newtonsoft.Json;
using UnityEngine;

namespace QuasimorphHelloWorld
{
    public class ModConfig
    {
        public class ItemEntry
        {
            public string ItemId { get; set; } = "";
            public int Count { get; set; } = 1;
        }

        public List<ItemEntry> Items { get; set; } = new List<ItemEntry> { };

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
                HotkeyCode = "G"
            };

        private static ModConfig _config = new ModConfig();
        private static KeyCode _hotkey = KeyCode.G;

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
            Debug.Log(
                "[QuickGear] Loaded, built: "
                    + File.GetLastWriteTime(typeof(ModMain).Assembly.Location)
            );
            EnsureDefaultConfig();
        }

        [Hook(ModHookType.AfterSaveLoaded)]
        public static void OnAfterSaveLoaded(IModContext context)
        {
            SavedGameMetadata meta = context.State.Get<SavedGameMetadata>();
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

        [Hook(ModHookType.SpaceUpdateAfterGameLoop)]
        public static void OnSpaceUpdate(IModContext context)
        {
            if (!Input.GetKeyDown(_hotkey))
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

            Debug.Log(
                "[QuickGear] Running quick gear. Config contents: "
                    + JsonConvert.SerializeObject(_config, Formatting.Indented)
            );

            Mercenary selectedMerc = GetSelectedMerc();

            foreach (ModConfig.ItemEntry entry in _config.Items)
            {
                if (selectedMerc != null)
                {
                    PullFromCargo(
                        cargo,
                        new List<Mercenary> { selectedMerc },
                        entry.ItemId,
                        entry.Count
                    );
                }
                else
                {
                    PullFromCargo(cargo, mercenaries.Values, entry.ItemId, entry.Count);
                }
            }
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

                BasePickupItem newItem =
                    SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(itemId);
                newItem.StackCount = (short)toPull;

                sourceItem.StackCount -= (short)toPull;
                if (sourceItem.StackCount <= 0)
                {
                    sourceTab.Remove(sourceItem);
                }

                if (
                    merc.CreatureData.Inventory.BackpackStore.TryPutItem(newItem, CellPosition.Zero)
                )
                {
                    Debug.Log($"[QuickGear] Moved {toPull}x {itemId} to {merc.ProfileId}");
                }
                else
                {
                    Debug.Log(
                        $"[QuickGear] No space in {merc.ProfileId} backpack, returning to cargo."
                    );
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
