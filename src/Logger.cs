using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUI
{
    internal static class Logger
    {
        private const string Signature = "[Display Movement Speed UI]";
        
        public static void LogDebug(string msg)
        {
            if (Plugin.Config.DebugMode)
                Debug.Log($"{Signature} {msg}");
        }

        public static void LogInfo(string msg)
        {
            Debug.Log($"{Signature} {msg}");
        }

        public static void LogWarning(string msg)
        {
            Debug.LogWarning($"{Signature} {msg}");
        }

        public static void LogError(string msg)
        {
            Debug.LogError($"{Signature} {msg}");
        }
    }
}