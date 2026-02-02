namespace Script.CommonLib
{
    public static class LogHelper
    {
        public static void Log(string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(msg);
#endif
        }
        
        public static void Warning(string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarning(msg);
#endif
        }
        
        public static void Error(string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogError(msg);
#endif
        }
    }
}
