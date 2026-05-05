using System.Reflection;
using MGSC;
using UnityEngine;

namespace QuasimorphHelloWorld
{
    public static class ModUiHelper
    {
        public static Mercenary GetSelectedMerc()
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

            FieldInfo field = typeof(ArsenalScreen).GetField(
                "_merc",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field == null)
            {
                return null;
            }

            return field.GetValue(screen) as Mercenary;
        }
    }
}
