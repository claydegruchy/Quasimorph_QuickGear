using System;
using System.IO;
using MGSC;
using UnityEngine;
using QuasimorphHelloWorld.Framework;

namespace QuasimorphHelloWorld.Triggers
{
    /// <summary>
    /// Trigger that manages config lifecycle:
    /// - Loads default config on bootstrap
    /// - Loads slot-specific config when save is loaded
    /// </summary>
    public class ConfigurationTrigger
    {
        private readonly GenericConfigManager<ModConfig> _configManager;
        private readonly string _defaultConfigPath;
        private readonly Func<int, string> _slotConfigPathFunc;

        public ConfigurationTrigger(
            GenericConfigManager<ModConfig> configManager,
            string defaultConfigPath,
            Func<int, string> slotConfigPathFunc
        )
        {
            _configManager = configManager;
            _defaultConfigPath = defaultConfigPath;
            _slotConfigPathFunc = slotConfigPathFunc;
        }

        public void OnBootstrap()
        {
            _configManager.EnsureDefaultConfig(new ModConfig
            {
                Items = new System.Collections.Generic.List<ModConfig.ItemEntry>
                {
                    new ModConfig.ItemEntry { ItemId = "medical_kit_2", Count = 2 },
                    new ModConfig.ItemEntry { ItemId = "water_bottle_1", Count = 1 }
                },
                HotkeyCode = "G"
            });
        }

        public void OnSaveLoaded()
        {
            SavedGameMetadata meta = GlobalModContext.Context.State.Get<SavedGameMetadata>();
            if (meta == null)
            {
                Debug.Log("[QuickGear] No save metadata, using default config.");
                _configManager.LoadFromAlternatePath(_defaultConfigPath);
                GlobalModContext.SetCurrentSlot(-1);
                return;
            }

            GlobalModContext.SetCurrentSlot(meta.Slot);
            string slotPath = _slotConfigPathFunc(meta.Slot);

            if (!File.Exists(slotPath))
            {
                string defaultJson = File.ReadAllText(_defaultConfigPath);
                File.WriteAllText(slotPath, defaultJson);
                Debug.Log($"[QuickGear] Created slot {meta.Slot} config from default.");
            }

            _configManager.LoadFromAlternatePath(slotPath);
            Debug.Log($"[QuickGear] Loaded slot {meta.Slot} config.");
        }
    }
}
