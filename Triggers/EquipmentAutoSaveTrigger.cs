using System;
using MGSC;
using UnityEngine;
using QuasimorphHelloWorld.Framework;

namespace QuasimorphHelloWorld.Triggers
{
    /// <summary>
    /// Trigger that saves merc equipment when a mission starts.
    /// </summary>
    public class EquipmentAutoSaveTrigger
    {
        private readonly Action<Mercenary> _onSaveEquipment;

        public EquipmentAutoSaveTrigger(Action<Mercenary> onSaveEquipment)
        {
            _onSaveEquipment = onSaveEquipment;
        }

        public void OnMissionStart(Mercenary merc)
        {
            if (merc != null)
            {
                Debug.Log($"[QuickGear] Auto-saving equipment for {merc.ProfileId}");
                _onSaveEquipment?.Invoke(merc);
            }
        }
    }
}
