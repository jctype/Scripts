using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    // Singleton
    public static AudioManager Instance;

    [Header("Global Volumes (0-1)")]
    public float masterVolume = 1f;
    public float sfxVolume = 0.8f;
    public float musicVolume = 0.6f;
    public float voVolume = 1f;

    [Header("Audio Sources – assign in inspector or auto-create")]
    public AudioSource musicSource;
    public AudioSource voSource;                // For dwarf voice lines
    private List<AudioSource> sfxSources = new List<AudioSource>(); // Pooled for overlapping clinks

    [Header("Clink Sounds – 7 variations for velocity-based pitch")]
    public AudioClip[] clinkClips; // assign low to high pitch in inspector

    [Header("Other SFX")]
    public AudioClip launchWhoosh;
    public AudioClip goalChime;
    public AudioClip voidSuck;
    public AudioClip trampPlaceSound;

    [Header("Binaural Audio Settings")]
    public bool enableBinauralProcessing = true;
    public GameObject binauralAudioPrefab; // Prefab with BinauralAudioInstance component
    
    // Pool for binaural audio objects
    private Queue<GameObject> binauralPool = new Queue<GameObject>();
    private const int BINAURAL_POOL_SIZE = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-create sources if not assigned
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (voSource == null) voSource = gameObject.AddComponent<AudioSource>();

        // Create 10 reusable SFX sources
        for (int i = 0; i < 10; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = sfxVolume;
            sfxSources.Add(src);
        }

        // Initialize binaural pool if enabled
        if (enableBinauralProcessing && binauralAudioPrefab != null)
        {
            InitializeBinauralPool();
        }
    }

    // ========== EXISTING FUNCTIONALITY (UNCHANGED) ==========

    // Play a random clink with pitch based on impact speed (5-25 m/s typical)
    public void PlayClink(float velocityMagnitude)
    {
        if (clinkClips.Length == 0) return;

        AudioSource src = GetAvailableSFXSource();
        if (src == null) return;

        // Choose clip roughly by velocity – higher speed = higher pitch
        int index = Mathf.Clamp(Mathf.FloorToInt(velocityMagnitude / 5f), 0, clinkClips.Length - 1);
        src.clip = clinkClips[index];

        // Pitch variation for juicy feel
        src.pitch = 0.9f + Random.Range(-0.15f, 0.25f) + (velocityMagnitude * 0.02f);
        src.volume = sfxVolume * masterVolume * Mathf.Clamp01(velocityMagnitude / 10f);

        src.PlayOneShot(src.clip);
    }

    // Generic SFX play (existing - for non-binaural sounds)
    public void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        AudioSource src = GetAvailableSFXSource();
        if (src == null) return;

        src.volume = sfxVolume * masterVolume * volumeScale;
        src.pitch = 1f + Random.Range(-0.1f, 0.1f);
        src.PlayOneShot(clip);
    }

    // Voice over for intro / success / fail
    public void PlayVO(AudioClip clip)
    {
        if (clip == null) return;
        voSource.Stop();
        voSource.clip = clip;
        voSource.volume = voVolume * masterVolume;
        voSource.Play();
    }

    // Simple music control
    public void PlayMusic(AudioClip musicClip, bool loop = true)
    {
        musicSource.clip = musicClip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.Play();
    }

    private AudioSource GetAvailableSFXSource()
    {
        // Find stopped source
        foreach (var src in sfxSources)
            if (!src.isPlaying) return src;

        // If all busy, use the oldest one (it will overlap – fine for clinks)
        return sfxSources[0];
    }

    // Call these from UI sliders if you add options menu
    public void SetMasterVolume(float vol) => masterVolume = vol;
    public void SetSFXVolume(float vol) => sfxVolume = vol;
    public void SetMusicVolume(float vol) => musicVolume = vol;
    public void SetVOVolume(float vol) => voVolume = vol;

    // ========== NEW BINAURAL FUNCTIONALITY ==========

    /// <summary>
    /// Play a sound with binaural spatialization based on filename parsing
    /// </summary>
    public GameObject PlayBinauralSound(string filename, float volumeScale = 1f, Vector3? position = null)
    {
        if (!enableBinauralProcessing)
        {
            // Fall back to regular sound if binaural is disabled
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{filename}");
            if (clip != null) PlaySound(clip, volumeScale);
            return null;
        }

        AudioClip audioClip = Resources.Load<AudioClip>($"Audio/{filename}");
        if (audioClip == null)
        {
            Debug.LogWarning($"Audio clip not found: {filename}");
            return null;
        }

        // Get or create a binaural audio object
        GameObject audioObj = GetBinauralObject();
        if (audioObj == null) return null;

        // Set position if specified
        if (position.HasValue)
        {
            audioObj.transform.position = position.Value;
        }

        // Get components
        AudioSource source = audioObj.GetComponent<AudioSource>();
        BinauralAudioInstance binaural = audioObj.GetComponent<BinauralAudioInstance>();

        // Configure audio source
        source.clip = audioClip;
        source.volume = sfxVolume * masterVolume * volumeScale;
        
        // Parse filename for spatial settings
        binaural.ParseSettingsFromFilename(filename);
        binaural.ApplySpatialSettings();
        
        // Play
        source.Play();

        // Schedule return to pool
        StartCoroutine(ReturnToPoolAfterPlay(audioObj, audioClip.length));

        return audioObj;
    }

    /// <summary>
    /// Play a sound with custom binaural settings
    /// </summary>
    public GameObject PlayBinauralSound(AudioClip clip, BinauralAudioInstance.SpatialSettings settings, 
                                        float volumeScale = 1f, Vector3? position = null)
    {
        if (!enableBinauralProcessing || clip == null)
        {
            PlaySound(clip, volumeScale);
            return null;
        }

        GameObject audioObj = GetBinauralObject();
        if (audioObj == null) return null;

        if (position.HasValue)
        {
            audioObj.transform.position = position.Value;
        }

        AudioSource source = audioObj.GetComponent<AudioSource>();
        BinauralAudioInstance binaural = audioObj.GetComponent<BinauralAudioInstance>();

        source.clip = clip;
        source.volume = sfxVolume * masterVolume * volumeScale;
        
        // Apply custom settings
        binaural.settings = settings;
        binaural.parseFromFilename = false; // We're using custom settings
        binaural.ApplySpatialSettings();
        
        source.Play();

        StartCoroutine(ReturnToPoolAfterPlay(audioObj, clip.length));

        return audioObj;
    }

    /// <summary>
    /// Play a binaural one-shot (doesn't use pooling, good for rare sounds)
    /// </summary>
    public GameObject PlayBinauralOneShot(string filename, float volumeScale = 1f, Vector3? position = null)
    {
        if (!enableBinauralProcessing)
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{filename}");
            if (clip != null) PlaySound(clip, volumeScale);
            return null;
        }

        AudioClip audioClip = Resources.Load<AudioClip>($"Audio/{filename}");
        if (audioClip == null)
        {
            Debug.LogWarning($"Audio clip not found: {filename}");
            return null;
        }

        // Create temporary object (not pooled)
        GameObject audioObj = Instantiate(binauralAudioPrefab);
        if (position.HasValue)
        {
            audioObj.transform.position = position.Value;
        }

        AudioSource source = audioObj.GetComponent<AudioSource>();
        BinauralAudioInstance binaural = audioObj.GetComponent<BinauralAudioInstance>();

        source.clip = audioClip;
        source.volume = sfxVolume * masterVolume * volumeScale;
        
        binaural.ParseSettingsFromFilename(filename);
        binaural.ApplySpatialSettings();
        
        source.Play();

        // Destroy after playback
        Destroy(audioObj, audioClip.length + 0.1f);

        return audioObj;
    }

    // ========== POOL MANAGEMENT ==========

    private void InitializeBinauralPool()
    {
        for (int i = 0; i < BINAURAL_POOL_SIZE; i++)
        {
            GameObject obj = Instantiate(binauralAudioPrefab, transform);
            obj.SetActive(false);
            binauralPool.Enqueue(obj);
        }
    }

    private GameObject GetBinauralObject()
    {
        if (binauralPool.Count > 0)
        {
            GameObject obj = binauralPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        
        // Pool empty - create a new one
        Debug.LogWarning("Binaural pool empty, creating new object. Consider increasing pool size.");
        GameObject newObj = Instantiate(binauralAudioPrefab, transform);
        return newObj;
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(GameObject audioObj, float duration)
    {
        yield return new WaitForSeconds(duration + 0.1f); // Small buffer
        
        if (audioObj != null)
        {
            AudioSource source = audioObj.GetComponent<AudioSource>();
            source.Stop();
            source.clip = null;
            audioObj.SetActive(false);
            
            // Only return to pool if we have room (prevents infinite growth)
            if (binauralPool.Count < BINAURAL_POOL_SIZE * 2)
            {
                binauralPool.Enqueue(audioObj);
            }
            else
            {
                Destroy(audioObj);
            }
        }
    }

    /// <summary>
    /// Get a binaural audio instance for manual control
    /// </summary>
    public BinauralAudioInstance GetBinauralInstance()
    {
        GameObject obj = GetBinauralObject();
        return obj.GetComponent<BinauralAudioInstance>();
    }

    /// <summary>
    /// Return a binaural instance to the pool
    /// </summary>
    public void ReturnBinauralInstance(GameObject audioObj)
    {
        if (audioObj == null) return;
        
        AudioSource source = audioObj.GetComponent<AudioSource>();
        if (source != null)
        {
            source.Stop();
            source.clip = null;
        }
        
        audioObj.SetActive(false);
        
        if (binauralPool.Count < BINAURAL_POOL_SIZE * 2)
        {
            binauralPool.Enqueue(audioObj);
        }
        else
        {
            Destroy(audioObj);
        }
    }
}