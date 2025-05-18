using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Voice")]
    public AudioSource VoiceSource;
    public bool CurrentlyPlayingVoiceClip => VoiceSource != null && VoiceSource.isPlaying;

    [Header("SFX")]
    public Transform SfxParent;

    [Header("Background Music")]
    public AudioSource BackgroundMusic;
    public bool CurrentlyPlayingBackgroundMusic => BackgroundMusic != null && BackgroundMusic.isPlaying;

    [Header("Settings")]
    [SerializeField]
    [Expandable]
    private AudioSettings audioSettings;

    private AudioSource tempSfxAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
        BackgroundMusic.loop = true;
    }

    public void PlayVoiceClip(AudioClip clip)
    {
        VoiceSource.clip = clip;
        VoiceSource.volume = audioSettings.VoiceVolume;
        VoiceSource.Play();
    }

    public void StopVoiceClip()
    {
        VoiceSource.Stop();
    }

    public void PlayBackgroundMusic(AudioClip clip)
    {
        BackgroundMusic.clip = clip;
        BackgroundMusic.volume = audioSettings.BackgroundMusicVolume;
        BackgroundMusic.Play();
    }

    public void StopBackgroundMusic()
    {
        BackgroundMusic.Stop();
    }

    public void StopBackgroundMusic(float fadeOutTime = 1f, bool destroy = true)
    {
        CrossfadeAudioSourceDown(BackgroundMusic, fadeOutTime, destroy);
    }

    public void PauseBackgroundMusic()
    {
        BackgroundMusic.Pause();
    }

    public void CrossfadeBackgroundMusic(AudioClip clip, float crossfadeTime = 1f)
    {
        AudioSource newBGSource = Instantiate(BackgroundMusic.gameObject, transform).GetComponent<AudioSource>();
        newBGSource.gameObject.name = BackgroundMusic.gameObject.name;
        BackgroundMusic.gameObject.name = BackgroundMusic.gameObject.name + " [OLD]";

        newBGSource.clip = clip;
        newBGSource.volume = 0f;

        AudioSource oldBGSource = BackgroundMusic;

        BackgroundMusic = newBGSource;
        BackgroundMusic.Play();

        StartCoroutine(CrossfadeAudioSourceDown(oldBGSource, crossfadeTime));
        StartCoroutine(CrossfadeAudioSourceUp(BackgroundMusic, crossfadeTime));
    }

    public void PlaySound(AudioClip clip)
    {
        StartCoroutine(PlaySoundCoroutine(clip));
    }

    public void PlaySound(AudioClip clip, float extraPitch)
    {
        StartCoroutine(PlaySoundCoroutine(clip, extraPitch));
    }

    public IEnumerator PlaySoundCoroutine(AudioClip clip, float extraPitch = 0f)
    {
        GameObject tempSoundPlayer = new GameObject(clip.name);
        tempSoundPlayer.transform.SetParent(SfxParent);

        AudioSource source = tempSoundPlayer.AddComponent<AudioSource>();
        tempSfxAudioSource = source;
        source.clip = clip;
        source.pitch += extraPitch;
        source.volume = audioSettings.SfxVolume;
        source.Play();

        while (source.isPlaying) yield return null;

        Destroy(tempSoundPlayer);
    }

    private IEnumerator CrossfadeAudioSourceUp(AudioSource source, float crossfadeTime = 1f)
    {
        while (source.volume != 1f) { source.volume += Time.deltaTime / crossfadeTime; yield return null; }
    }

    private IEnumerator CrossfadeAudioSourceDown(AudioSource source, float crossfadeTime = 1f, bool deleteOnDone = true)
    {
        while (source.volume != 0f) { source.volume -= Time.deltaTime / crossfadeTime; yield return null; }
        if (deleteOnDone) Destroy(source.gameObject);
    }

    private void UpdateVolumeSettings()
    {
        BackgroundMusic.volume = audioSettings.BackgroundMusicVolume;
        VoiceSource.volume = audioSettings.VoiceVolume;

        if (tempSfxAudioSource != null)
        {
            tempSfxAudioSource.volume = audioSettings.SfxVolume;
        }
    }

    private void PauseAllAudio()
    {
        BackgroundMusic.Pause();
        VoiceSource.Pause();
        tempSfxAudioSource?.Pause();
    }

    private void UnpauseAllAudio()
    {
        BackgroundMusic.UnPause();
        VoiceSource.UnPause();
        tempSfxAudioSource?.UnPause();
    }
}
