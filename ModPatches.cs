using System;
using HarmonyLib;
using MGSC;
using UnityEngine;
using QuasimorphHelloWorld.Framework;
using QuasimorphHelloWorld.Triggers;

namespace QuasimorphHelloWorld
{
    public static class ModPatches
    {
        private static EquipmentAutoSaveTrigger _equipmentAutoSaveTrigger;

        public static void SetEquipmentAutoSaveTrigger(EquipmentAutoSaveTrigger trigger)
        {
            _equipmentAutoSaveTrigger = trigger;
        }

        [HarmonyPatch(typeof(SpaceGameMode), "StartMission")]
        public static class SpaceGameMode_StartMission_Patch
        {
            public static void Prefix(SpaceModeFinishedData data, Mission mission, bool saveGame)
            {
                if (GlobalModContext.Context == null)
                {
                    Debug.Log("[QuickGear] No mod context available.");
                    return;
                }

                if (data.mercProfileId != null)
                {
                    Mercenaries mercenaries = GlobalModContext.Context.State.Get<Mercenaries>();
                    Mercenary merc = mercenaries.Get(data.mercProfileId);
                    _equipmentAutoSaveTrigger?.OnMissionStart(merc);
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

                    Debug.Log("[QuickGear] ArsenalScreen.Configure called for: " + mercenary.ProfileId);
                }
                catch (Exception e)
                {
                    Debug.Log("[QuickGear] Exception in ArsenalScreen patch: " + e.Message);
                }
            }
        }
    }
}
