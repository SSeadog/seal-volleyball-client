using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string PrefKeyBgmVolume = "volume_bgm";
    private const string PrefKeySfxVolume = "volume_sfx";

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    [SerializeField] private SoundEntry[] bgmClips;

    [Header("SFX Clips")]
    [SerializeField] private SoundEntry[] sfxClips;

    private Dictionary<string, AudioClip> bgmMap;
    private Dictionary<string, AudioClip> sfxMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource != null)
        {
            bgmSource.loop = true;
        }

        LoadVolumes();
        ApplyVolumes();

        BuildDictionaries();
    }

    private void LoadVolumes()
    {
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeyBgmVolume, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeySfxVolume, 1f));
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public float GetBgmVolume() => bgmVolume;
    public float GetSfxVolume() => sfxVolume;

    public void SetBgmVolume(float value, bool save = true)
    {
        bgmVolume = Mathf.Clamp01(value);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (save)
        {
            PlayerPrefs.SetFloat(PrefKeyBgmVolume, bgmVolume);
            PlayerPrefs.Save();
        }
    }

    public void SetSfxVolume(float value, bool save = true)
    {
        sfxVolume = Mathf.Clamp01(value);
        if (save)
        {
            PlayerPrefs.SetFloat(PrefKeySfxVolume, sfxVolume);
            PlayerPrefs.Save();
        }
    }

    private void BuildDictionaries()
    {
        bgmMap = new Dictionary<string, AudioClip>();
        sfxMap = new Dictionary<string, AudioClip>();

        if (bgmClips != null)
        {
            foreach (var entry in bgmClips)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || entry.clip == null)
                    continue;

                string lowerKey = entry.key.ToLowerInvariant();
                if (!bgmMap.ContainsKey(lowerKey))
                {
                    bgmMap.Add(lowerKey, entry.clip);
                }
            }
        }

        if (sfxClips != null)
        {
            foreach (var entry in sfxClips)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || entry.clip == null)
                    continue;

                string lowerKey = entry.key.ToLowerInvariant();
                if (!sfxMap.ContainsKey(lowerKey))
                {
                    sfxMap.Add(lowerKey, entry.clip);
                }
            }
        }
    }

    /// <summary>
    /// BGM을 재생합니다. null이면 아무 것도 하지 않습니다.
    /// </summary>
    public void PlayBgm(AudioClip clip, float volume = 1f)
    {
        if (bgmSource == null || clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.volume = Mathf.Clamp01(volume) * bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// 키(string)로 등록된 BGM을 재생합니다.
    /// </summary>
    public void PlayBgm(string key, float volume = 1f)
    {
        if (bgmMap == null) return;
        string lowerKey = key?.ToLowerInvariant();
        if (string.IsNullOrEmpty(lowerKey) || !bgmMap.TryGetValue(lowerKey, out var clip) || clip == null)
        {
            Debug.LogWarning($"SoundManager: BGM key '{key}' not found.");
            return;
        }

        PlayBgm(clip, volume);
    }

    public void StopBgm()
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    /// <summary>
    /// 효과음을 재생합니다. null이면 아무 것도 하지 않습니다.
    /// </summary>
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume);
    }

    /// <summary>
    /// 키(string)로 등록된 효과음을 재생합니다.
    /// </summary>
    public void PlaySfx(string key, float volume = 1f)
    {
        if (sfxMap == null) return;
        string lowerKey = key?.ToLowerInvariant();
        if (string.IsNullOrEmpty(lowerKey) || !sfxMap.TryGetValue(lowerKey, out var clip) || clip == null)
        {
            Debug.LogWarning($"SoundManager: SFX key '{key}' not found.");
            return;
        }

        PlaySfx(clip, volume);
    }
}