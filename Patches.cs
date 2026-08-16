using MGSC;
using HarmonyLib;

namespace QuasimorphHelloWorld
{
    [HarmonyPatch(typeof(SpaceGameMode), "StartMission")]
    public static class SpaceGameMode_StartMission_Patch
    {
        public static void Prefix(SpaceModeFinishedData data, Mission mission, bool saveGame)
        {
            if (ModMain._modContext == null)
            {
                return;
            }

            if (data.mercProfileId != null)
            {
                Mercenaries mercenaries = ModMain._modContext.State.Get<Mercenaries>();
                Mercenary merc = mercenaries.Get(data.mercProfileId);
                if (merc != null)
                {
                    QuickGearService.SaveEquipment(merc);
                }
            }
        }
    }
}
