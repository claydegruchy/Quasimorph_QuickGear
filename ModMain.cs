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

        public List<ItemEntry> Items { get; set; } =
            new List<ItemEntry>
            {
                new ItemEntry { ItemId = "water_bottle_1", Count = 3 }
            };

        public string HotkeyCode { get; set; } = "G";
    }

    public static class ModMain
    {
        private static ModConfig _config = new ModConfig();
        private static KeyCode _hotkey = KeyCode.G;

        private static string ConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..",
                "LocalLow",
                "Magnum Scriptum Ltd",
                "Quasimorph_ModConfigs",
                "QuickGear",
                "config.json"
            );

        [Hook(ModHookType.AfterBootstrap)]
        public static void OnAfterBootstrap(IModContext context)
        {
            Debug.Log(
                "[QuickGear] Loaded, built: "
                    + File.GetLastWriteTime(typeof(ModMain).Assembly.Location)
            );
            LoadConfig();
        }

        private static void LoadConfig()
        {
            try
            {
                string path = ConfigPath;
                string dir = Path.GetDirectoryName(path);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(path))
                {
                    string defaultJson = JsonConvert.SerializeObject(_config, Formatting.Indented);
                    File.WriteAllText(path, defaultJson);
                    Debug.Log("[QuickGear] Created default config at: " + path);
                }
                else
                {
                    string json = File.ReadAllText(path);
                    _config = JsonConvert.DeserializeObject<ModConfig>(json);
                    Debug.Log("[QuickGear] Loaded config from: " + path);
                }

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

            Mercenary selectedMerc = GetSelectedMerc();
            List<Mercenary> targets =
                selectedMerc != null ? new List<Mercenary> { selectedMerc } : mercenaries.Values;

            foreach (ModConfig.ItemEntry entry in _config.Items)
            {
                PullFromCargo(cargo, targets, entry.ItemId, entry.Count);
            }
        }

        private static void PullFromCargo(
            MagnumCargo cargo,
            Mercenaries mercenaries,
            string itemId,
            int countPerMerc
        )
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
