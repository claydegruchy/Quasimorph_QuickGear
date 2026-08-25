namespace QuasimorphHelloWorld
{
    public static class Debug
    {
        private static bool Enabled => ModConfigStore.GlobalSettings.EnableLogging;

        public static void Log(object message)
        {
            if (!Enabled)
                return;

            global::UnityEngine.Debug.Log(message);
        }

        public static void LogWarning(object message)
        {
            if (!Enabled)
                return;

            global::UnityEngine.Debug.LogWarning(message);
        }

        public static void LogError(object message)
        {
            if (!Enabled)
                return;

            global::UnityEngine.Debug.LogError(message);
        }
    }
}