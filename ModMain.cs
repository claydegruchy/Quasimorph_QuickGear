using System;
using System.IO;
using MGSC;
using UnityEngine;
using HarmonyLib;
using QuasimorphHelloWorld.Framework;
using QuasimorphHelloWorld.Triggers;

namespace QuasimorphHelloWorld
{
    /// <summary>
    /// Main mod entry point. Register triggers and config here.
    /// </summary>
    public static class ModMain
    {
        private static readonly Harmony _harmony = new Harmony("QuickGear");
        private static GenericConfigManager<ModConfig> _configManager;
        private static ConfigurationTrigger _configTrigger;
        private static HotkeyTrigger _hotkeyTrigger;
        private static EquipmentAutoSaveTrigger _equipmentAutoSaveTrigger;

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
            GlobalModContext.SetContext(context);
            Debug.Log(
                "[QuickGear] Loaded, built: "
                    + File.GetLastWriteTime(typeof(ModMain).Assembly.Location)
            );

            // Initialize config manager
            _configManager = new GenericConfigManager<ModConfig>(DefaultConfigPath);
            QuickGearService.Initialize(_configManager);

            // Set up triggers
            _configTrigger = new ConfigurationTrigger(
                _configManager,
                DefaultConfigPath,
                SlotConfigPath
            );
            _hotkeyTrigger = new HotkeyTrigger(
                _configManager,
                OnSinglePressHotkey,
                OnDoublePressHotkey
            );
            _equipmentAutoSaveTrigger = new EquipmentAutoSaveTrigger(
                QuickGearService.SaveEquipment
            );

            // Register hooks
            ModHookRegistry.RegisterBootstrap(ctx =>
            {
                _configTrigger.OnBootstrap();
            });

            ModHookRegistry.RegisterSaveLoaded(ctx =>
            {
                _configTrigger.OnSaveLoaded();
            });

            ModHookRegistry.RegisterSpaceUpdate(ctx =>
            {
                _hotkeyTrigger.OnSpaceUpdate();
            });

            // Apply harmony patches
            _harmony.PatchAll();
            ModPatches.SetEquipmentAutoSaveTrigger(_equipmentAutoSaveTrigger);

            // Execute bootstrap hooks
            ModHookRegistry.ExecuteBootstrap(context);

            Debug.Log("[QuickGear] Initialization complete.");
        }

        [Hook(ModHookType.AfterSaveLoaded)]
        public static void OnAfterSaveLoaded(IModContext context)
        {
            GlobalModContext.SetContext(context);
            ModHookRegistry.ExecuteSaveLoaded(context);
        }

        [Hook(ModHookType.SpaceUpdateAfterGameLoop)]
        public static void OnSpaceUpdate(IModContext context)
        {
            ModHookRegistry.ExecuteSpaceUpdate(context);
        }

        private static void OnSinglePressHotkey(Mercenary merc)
        {
            Debug.Log("[QuickGear] Single press: equipping quick gear.");
            QuickGearService.EquipQuickGear(merc);
        }

        private static void OnDoublePressHotkey(Mercenary merc)
        {
            Debug.Log("[QuickGear] Double press: loading saved equipment.");
            if (QuickGearService.HasSavedEquipment(merc))
            {
                QuickGearService.LoadSavedEquipment(merc);
            }
            else
            {
                Debug.Log("[QuickGear] No saved equipment for this merc.");
            }
        }
    }
}
