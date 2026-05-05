using System;
using UnityEngine;
using MGSC;
using QuasimorphHelloWorld.Framework;

namespace QuasimorphHelloWorld.Triggers
{
    /// <summary>
    /// Trigger that handles hotkey input and dispatches to gear management.
    /// </summary>
    public class HotkeyTrigger
    {
        private readonly GenericConfigManager<ModConfig> _configManager;
        private readonly Action<Mercenary> _onSinglePress;
        private readonly Action<Mercenary> _onDoublePress;
        private float _lastHotkeyPressTime = -1f;
        private KeyCode _hotkey = KeyCode.G;

        public HotkeyTrigger(
            GenericConfigManager<ModConfig> configManager,
            Action<Mercenary> onSinglePress,
            Action<Mercenary> onDoublePress
        )
        {
            _configManager = configManager;
            _onSinglePress = onSinglePress;
            _onDoublePress = onDoublePress;
        }

        public void OnSpaceUpdate()
        {
            ParseHotkeyFromConfig();

            if (!Input.GetKeyDown(_hotkey))
                return;

            float now = Time.time;
            bool isDoublePress = (_lastHotkeyPressTime > 0f) && (now - _lastHotkeyPressTime < 0.5f);

            Mercenary selectedMerc = ModUiHelper.GetSelectedMerc();
            if (selectedMerc == null)
            {
                Debug.Log("[QuickGear] No merc selected.");
                return;
            }

            Debug.Log("[QuickGear] Selected merc: " + selectedMerc.ProfileId);

            if (isDoublePress)
            {
                Debug.Log("[QuickGear] Hotkey double-pressed.");
                _lastHotkeyPressTime = -1f;
                _onDoublePress?.Invoke(selectedMerc);
                return;
            }

            _lastHotkeyPressTime = now;
            Debug.Log("[QuickGear] Hotkey single-pressed.");
            _onSinglePress?.Invoke(selectedMerc);
        }

        private void ParseHotkeyFromConfig()
        {
            if (!Enum.TryParse<KeyCode>(_configManager.Config.HotkeyCode, out _hotkey))
            {
                Debug.Log("[QuickGear] Invalid hotkey, defaulting to G.");
                _hotkey = KeyCode.G;
            }
        }
    }
}
