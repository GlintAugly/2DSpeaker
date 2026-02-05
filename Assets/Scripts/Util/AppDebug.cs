using UnityEngine;

public class AppDebug
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [HideInCallstack]
    public static void Log(string message, params object[] args)
    {
        Debug.LogFormat(message, args);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [HideInCallstack]
    public static void LogWarning(string message, params object[] args)
    {
        Debug.LogWarningFormat(message, args);
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [HideInCallstack]
    public static void LogError(string message, params object[] args)
    {
        Debug.LogErrorFormat(message, args);
    }
}
