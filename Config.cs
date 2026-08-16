using System;
using System.Collections.Generic;
using System.IO;
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

        public class SavedEquipment
        {
            public Dictionary<string, string> Equipment { get; set; } =
                new Dictionary<string, string>();
            public Dictionary<string, string> Limbs { get; set; } =
                new Dictionary<string, string>();
            public Dictionary<string, List<string>> Implants { get; set; } =
                new Dictionary<string, List<string>>();
        }

        public List<ItemEntry> Items { get; set; } = new List<ItemEntry>();
        public Dictionary<string, SavedEquipment> SavedEquipmentHistory { get; set; } =
            new Dictionary<string, SavedEquipment>();
        public string HotkeyCode { get; set; } = "G";
    }

    public class ModGlobalSettings
    {
        public string Language { get; set; } = "en";
        public bool EnableLogging { get; set; } = false;
    }

    public static class ModConfigStore
    {
        public static ModConfig Config { get; set; } = new ModConfig();
        public static ModGlobalSettings GlobalSettings { get; set; } = new ModGlobalSettings();
        public static int CurrentSlot { get; set; } = -1;

        public static ModConfig DefaultConfig =>
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

        public static ModGlobalSettings DefaultGlobalSettings =>
            new ModGlobalSettings { Language = "en", EnableLogging = false };

        public static string DefaultConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..",
                "LocalLow",
                "Magnum Scriptum Ltd",
                "Quasimorph_ModConfigs",
                "QuickGear",
                "config.json"
            );

        public static string GlobalConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..",
                "LocalLow",
                "Magnum Scriptum Ltd",
                "Quasimorph_ModConfigs",
                "QuickGear",
                "global_config.json"
            );

        public static string SlotConfigPath(int slot) =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..",
                "LocalLow",
                "Magnum Scriptum Ltd",
                "Quasimorph_ModConfigs",
                "QuickGear",
                $"slot_{slot}_config.json"
            );

        public static void EnsureDefaultConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(DefaultConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(DefaultConfigPath))
                {
                    LoadConfig(DefaultConfigPath);
                }
                else
                {
                    Config = DefaultConfig;
                    ApplyGlobalSettings();
                }

                EnsureGlobalSettings();
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to ensure default config. Error: " + e.Message);
                Config = DefaultConfig;
            }
        }

        public static void EnsureGlobalSettings()
        {
            try
            {
                if (File.Exists(GlobalConfigPath))
                {
                    LoadGlobalSettings();
                    return;
                }

                GlobalSettings = DefaultGlobalSettings;
                ApplyGlobalSettings();
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to ensure global settings. Error: " + e.Message);
                GlobalSettings = DefaultGlobalSettings;
                ApplyGlobalSettings();
            }
        }

        public static void LoadConfig(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                Config = JsonConvert.DeserializeObject<ModConfig>(json) ?? new ModConfig();
                Debug.Log("[QuickGear] Loaded config from: " + path);
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to load config, using defaults. Error: " + e.Message);
                Config = DefaultConfig;
            }
        }

        public static void LoadGlobalSettings()
        {
            try
            {
                string json = File.ReadAllText(GlobalConfigPath);
                GlobalSettings =
                    JsonConvert.DeserializeObject<ModGlobalSettings>(json)
                    ?? new ModGlobalSettings();
                if (string.IsNullOrWhiteSpace(GlobalSettings.Language))
                {
                    GlobalSettings.Language = "en";
                }
                ApplyGlobalSettings();
                Debug.Log("[QuickGear] Loaded global settings from: " + GlobalConfigPath);
            }
            catch (Exception e)
            {
                Debug.Log(
                    "[QuickGear] Failed to load global settings, using defaults. Error: "
                        + e.Message
                );
                GlobalSettings = DefaultGlobalSettings;
                ApplyGlobalSettings();
            }
        }

        public static void ApplyGlobalSettings()
        {
            Debug.unityLogger.logEnabled = GlobalSettings.EnableLogging;
        }

        public static void SaveConfig()
        {
            try
            {
                string path = (CurrentSlot >= 0) ? SlotConfigPath(CurrentSlot) : DefaultConfigPath;
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(path, json);
                Debug.Log("[QuickGear] Saved config to: " + path);
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to save config. Error: " + e.Message);
            }
        }

        public static void SaveGlobalSettings()
        {
            try
            {
                ApplyGlobalSettings();
                string json = JsonConvert.SerializeObject(GlobalSettings, Formatting.Indented);
                File.WriteAllText(GlobalConfigPath, json);
                Debug.Log("[QuickGear] Saved global settings to: " + GlobalConfigPath);
            }
            catch (Exception e)
            {
                Debug.Log("[QuickGear] Failed to save global settings. Error: " + e.Message);
            }
        }
    }
}
