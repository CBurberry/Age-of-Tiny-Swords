using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioSettings", menuName = "TinyWorld/Settings/Audio")]
public class AudioSettings : ScriptableObject, IGameSettings
{
    public event Action OnSettingsSaved;
    public event Action OnSettingsLoaded;

    [Range(0f, 1f)]
    public float BackgroundMusicVolume;

    [Range(0f, 1f)]
    public float VoiceVolume;

    [Range(0f, 1f)]
    public float SfxVolume;

    //Save the current values to persistent data
    public virtual void SaveSettings()
    {
        PlayerPrefs.SetFloat(nameof(BackgroundMusicVolume), BackgroundMusicVolume);
        PlayerPrefs.SetFloat(nameof(VoiceVolume), VoiceVolume);
        PlayerPrefs.SetFloat(nameof(SfxVolume), SfxVolume);
        PlayerPrefs.Save();
        OnSettingsSaved?.Invoke();
    }

    //Override current instance values with persistent data store, do nothing if none exists
    public virtual void LoadSettings()
    {
        BackgroundMusicVolume = PlayerPrefs.GetFloat(nameof(BackgroundMusicVolume));
        VoiceVolume = PlayerPrefs.GetFloat(nameof(VoiceVolume));
        SfxVolume = PlayerPrefs.GetFloat(nameof(SfxVolume));
        OnSettingsLoaded?.Invoke();
    }
}
