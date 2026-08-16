using System;
using System.IO;
using MGSC;
using UnityEngine;
using HarmonyLib;

namespace QuasimorphHelloWorld
{
    public static class ModMain
    {
        public static ModConfig _default_config => ModConfigStore.DefaultConfig;
        public static readonly Harmony _harmony = new Harmony("QuickGear");
        public static IModContext _modContext;

        private static string DefaultConfigPath => ModConfigStore.DefaultConfigPath;

        private static string SlotConfigPath(int slot) => ModConfigStore.SlotConfigPath(slot);

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
            Debug.Log(
                "[QuickGear] Main menu started. Auto-load is disabled to avoid runtime crashes."
            );
        }

        [Hook(ModHookType.AfterSaveLoaded)]
        public static void OnAfterSaveLoaded(IModContext context)
        {
            ModConfigStore.LoadGlobalSettings();
            SavedGameMetadata meta = context.State.Get<SavedGameMetadata>();
            ModConfigStore.CurrentSlot = (meta != null) ? meta.Slot : -1;
            if (meta == null)
            {
                Debug.Log("[QuickGear] No save metadata, using default config.");
                LoadConfig(DefaultConfigPath);
                return;
            }

            string slotPath = SlotConfigPath(meta.Slot);
            if (!File.Exists(slotPath))
            {
                string defaultJson = File.ReadAllText(DefaultConfigPath);
                File.WriteAllText(slotPath, defaultJson);
                Debug.Log($"[QuickGear] Created slot {meta.Slot} config from default.");
            }

            LoadConfig(slotPath);
            Debug.Log($"[QuickGear] Loaded slot {meta.Slot} config.");
        }

        [Hook(ModHookType.SpaceUpdateAfterGameLoop)]
        public static void OnSpaceUpdate(IModContext context)
        {
            // QuickGear is run from the Arsenal UI buttons; no keybind trigger is required.
        }

        public static void EnsureDefaultConfig() => ModConfigStore.EnsureDefaultConfig();

        public static void LoadConfig(string path)
        {
            ModConfigStore.LoadConfig(path);
        }

        public static void SaveConfig() => ModConfigStore.SaveConfig();

        public static void SaveEquipment(Mercenary merc) => QuickGearService.SaveEquipment(merc);

        public static void EquipQuickGear(Mercenary merc) => QuickGearService.EquipQuickGear(merc);

        public static void SaveInventoryQuickGear(Mercenary merc) =>
            QuickGearService.SaveInventoryQuickGear(merc);

        public static void LoadSavedEquipment(Mercenary merc) =>
            QuickGearService.LoadSavedEquipment(merc);

        public static bool HasSavedEquipment(Mercenary merc) => QuickGearService.HasSavedEquipment(merc);

        public static Mercenary GetSelectedMerc() => QuickGearService.GetSelectedMerc();
    }
}
