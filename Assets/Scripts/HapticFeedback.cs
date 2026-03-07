using UnityEngine;
using System.Runtime.InteropServices;

public static class HapticFeedback
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _TriggerHapticLight();

    [DllImport("__Internal")]
    private static extern void _TriggerHapticMedium();

    [DllImport("__Internal")]
    private static extern void _TriggerHapticHeavy();
#endif

    public static void TriggerLight()
    {
        if (!SoundManager.soundEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
        _TriggerHapticLight();
#endif
    }

    public static void TriggerMedium()
    {
        if (!SoundManager.soundEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
        _TriggerHapticMedium();
#endif
    }

    public static void TriggerHeavy()
    {
        if (!SoundManager.soundEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
        _TriggerHapticHeavy();
#endif
    }
}
