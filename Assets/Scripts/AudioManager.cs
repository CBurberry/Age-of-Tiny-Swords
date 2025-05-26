using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using RuntimeStatics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Files")]
    public List<SerializedKeyValuePair<AudioClip, float>> PeacefulBGMClips;
    public SerializedKeyValuePair<AudioClip, float> BattleBGMClip;

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

    [SerializeField]
    private float timeBetweenBGM;

    private AudioSource tempSfxAudioSource;
    private AudioClip lastPlayedBgm;
    private float elapsedIdleTime;

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
        BackgroundMusic.loop = false;
    }

    private void Start()
    {
        PlayRandomPeacefulBGM();
    }

    private void Update()
    {
        if (!CurrentlyPlayingBackgroundMusic && Time.timeScale > 0f) 
        {
            elapsedIdleTime += Time.deltaTime;
            if (elapsedIdleTime > timeBetweenBGM) 
            {
                PlayRandomPeacefulBGM();
                elapsedIdleTime = 0f;
            }
        }

        if (!BackgroundMusic.loop && CurrentlyPlayingBackgroundMusic) 
        {
            if (BackgroundMusic.time > BackgroundMusic.clip.length * 0.95f) 
            {
                //Fade down slowly over 5s
                BackgroundMusic.volume -= Time.deltaTime * 0.2f;
            }
        }
    }

    [Button("PlayRandomPeacefulBGM")]
    public void PlayRandomPeacefulBGM()
    {
        if (lastPlayedBgm == null)
        {
            var random = PeacefulBGMClips.PickRandom();
            PlayBackgroundMusic(random.Key, random.Value);
        }
        else 
        {
            var random = PeacefulBGMClips.Where(x => x.Key != lastPlayedBgm).PickRandom();
            PlayBackgroundMusic(random.Key, random.Value);
        }
        BackgroundMusic.loop = false;
    }

    [Button("PlayBattleThemeLooping")]
    public void PlayBattleThemeLooping()
    {
        if (CurrentlyPlayingBackgroundMusic && Application.isPlaying)
        {
            CrossfadeBackgroundMusic(BattleBGMClip.Key, BattleBGMClip.Value);
        }
        else
        {
            PlayBackgroundMusic(BattleBGMClip.Key, BattleBGMClip.Value);
        }
        BackgroundMusic.loop = true;
    }

    public void PlayVoiceClip(AudioClip clip, float volume)
    {
        VoiceSource.clip = clip;
        VoiceSource.volume = volume * audioSettings.VoiceVolume;
        VoiceSource.Play();
    }

    public void StopVoiceClip()
    {
        VoiceSource.Stop();
    }

    public void PlayBackgroundMusic(AudioClip clip, float volume = 1f)
    {
        BackgroundMusic.clip = clip;
        BackgroundMusic.volume = volume * audioSettings.BackgroundMusicVolume;
        BackgroundMusic.Play();
        lastPlayedBgm = clip;
    }

    [Button("StopBackgroundMusic")]
    public void StopBackgroundMusic()
    {
        BackgroundMusic.Stop();
    }

    [Button("StopBackgroundMusic (Crossfade)")]
    public void StopBackgroundMusic(float fadeOutTime = 1f, bool destroy = true)
    {
        CrossfadeAudioSourceDown(BackgroundMusic, fadeOutTime, destroy);
    }

    public void PauseBackgroundMusic()
    {
        BackgroundMusic.Pause();
    }

    public void CrossfadeBackgroundMusic(AudioClip clip, float volume, float crossfadeTime = 1f)
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
        StartCoroutine(CrossfadeAudioSourceUp(BackgroundMusic, volume * audioSettings.BackgroundMusicVolume, crossfadeTime));
    }

    public void PlaySound(AudioClip clip, float volume)
    {
        StartCoroutine(PlaySoundCoroutine(clip, volume));
    }

    public void PlaySound(AudioClip clip, float volume, float extraPitch)
    {
        StartCoroutine(PlaySoundCoroutine(clip, volume, extraPitch));
    }

    public IEnumerator PlaySoundCoroutine(AudioClip clip, float volume, float extraPitch = 0f)
    {
        GameObject tempSoundPlayer = new GameObject(clip.name);
        tempSoundPlayer.transform.SetParent(SfxParent);

        AudioSource source = tempSoundPlayer.AddComponent<AudioSource>();
        tempSfxAudioSource = source;
        source.clip = clip;
        source.pitch += extraPitch;
        source.volume = volume * audioSettings.SfxVolume;
        source.Play();

        while (source.isPlaying) yield return null;

        Destroy(tempSoundPlayer);
    }

    private IEnumerator CrossfadeAudioSourceUp(AudioSource source, float volume, float crossfadeTime = 1f)
    {
        while (source.volume != volume) { source.volume += Time.deltaTime / crossfadeTime; yield return null; }
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

    [Button("PauseAllAudio")]
    private void PauseAllAudio()
    {
        BackgroundMusic.Pause();
        VoiceSource.Pause();

        if (tempSfxAudioSource != null) 
        {
            tempSfxAudioSource.Pause();
        }
    }

    [Button("UnpauseAllAudio")]
    private void UnpauseAllAudio()
    {
        BackgroundMusic.UnPause();
        VoiceSource.UnPause();

        if (tempSfxAudioSource != null)
        {
            tempSfxAudioSource.UnPause();
        }
    }
}
