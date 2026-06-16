using UnityEngine;

public static class VolumeInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;
        Debug.Log($"[VolumeInitializer] Global volume initialized to {savedVolume}");
    }
}
