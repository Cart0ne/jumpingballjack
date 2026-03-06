using UnityEngine;
using System.Runtime.InteropServices;

public class iOSAudioFix : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _SetAudioSessionPlayback();
#endif

    void Awake()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _SetAudioSessionPlayback();
#endif
    }
}