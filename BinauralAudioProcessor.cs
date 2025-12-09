using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[RequireComponent(typeof(AudioSource))]
public class BinauralAudioProcessor : MonoBehaviour
{
    [System.Serializable]
    public class AudioPositioningData
    {
        [Range(0f, 1f)] public float leftRightBalance = 0.5f; // 0 = full left, 1 = full right, 0.5 = center
        [Range(0f, 1f)] public float frontBackBalance = 0.5f; // 0 = front, 1 = back
        [Range(0f, 1f)] public float distance = 0.5f; // 0 = close, 1 = far
        [Range(0f, 1f)] public float occlusion = 0f; // 0 = clear, 1 = heavily obstructed
        [Range(0f, 1f)] public float elevation = 0.5f; // 0 = below, 1 = above, 0.5 = ear level
        public string environment = "open"; // "open", "trees", "cave", "water", "stone"
        
        // Calculated spatial parameters
        public float panStereo = 0f;
        public float volumeAttenuation = 1f;
        public float lowPassCutoff = 22000f;
        public float reverbLevel = 0f;
        public float delayTime = 0f;
    }

    [Header("Processing Settings")]
    public bool enableBinaural = true;
    [Range(0f, 0.5f)] public float maxDelayTime = 0.3f; // Max ITD (Interaural Time Difference)
    [Range(0f, 5000f)] public float maxLowPassCutoff = 5000f; // Min frequency when occluded
    public AnimationCurve distanceAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
    public AnimationCurve occlusionAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);

    [Header("Environment Presets")]
    public AudioReverbPreset[] environmentPresets = new AudioReverbPreset[]
    {
        AudioReverbPreset.Off,          // open
        AudioReverbPreset.Forest,       // trees
        AudioReverbPreset.Cave,         // cave
        AudioReverbPreset.Underwater,   // water
        AudioReverbPreset.Stoneroom     // stone
    };

    // Static dictionary for environment mappings
    private static Dictionary<string, int> environmentMap = new Dictionary<string, int>
    {
        {"open", 0}, {"field", 0}, {"plain", 0},
        {"tree", 1}, {"forest", 1}, {"wood", 1},
        {"cave", 2}, {"tunnel", 2}, {"cavern", 2},
        {"water", 3}, {"river", 3}, {"lake", 3}, {"sea", 3},
        {"stone", 4}, {"rock", 4}, {"mountain", 4}, {"dwarf", 4}
    };

    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;
    private AudioReverbFilter reverbFilter;
    private AudioEchoFilter echoFilter; // For delay simulation
    private Dictionary<string, AudioPositioningData> cachedPositionData = new Dictionary<string, AudioPositioningData>();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Add filters if not present
        if (!TryGetComponent(out lowPassFilter))
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        
        if (!TryGetComponent(out reverbFilter))
            reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
        
        if (!TryGetComponent(out echoFilter))
            echoFilter = gameObject.AddComponent<AudioEchoFilter>();
        
        // Initialize echo filter for ITD simulation
        echoFilter.decayRatio = 0.1f;
        echoFilter.dryMix = 1f;
        echoFilter.wetMix = 0f; // We'll adjust this per-channel
    }

    /// <summary>
    /// Parse filename for spatial positioning data
    /// Expected format: name_direction_distance_environment.extension
    /// Example: birdsong001_farleft_trees.wav
    /// </summary>
    public AudioPositioningData ParseFilename(string filename)
    {
        if (cachedPositionData.TryGetValue(filename, out var cachedData))
            return cachedData;

        AudioPositioningData data = new AudioPositioningData();
        
        // Remove extension and split by underscores
        string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filename);
        string[] parts = nameWithoutExt.Split('_');
        
        // Default values
        data.leftRightBalance = 0.5f;
        data.frontBackBalance = 0.5f;
        data.distance = 0.5f;
        data.occlusion = 0f;
        data.elevation = 0.5f;
        data.environment = "open";

        // Parse each part
        foreach (string part in parts)
        {
            string partLower = part.ToLower();
            
            // Direction parsing
            if (partLower.Contains("left"))
            {
                if (partLower.Contains("far")) data.leftRightBalance = 0.1f;
                else if (partLower.Contains("mid")) data.leftRightBalance = 0.25f;
                else data.leftRightBalance = 0.4f;
            }
            else if (partLower.Contains("right"))
            {
                if (partLower.Contains("far")) data.leftRightBalance = 0.9f;
                else if (partLower.Contains("mid")) data.leftRightBalance = 0.75f;
                else data.leftRightBalance = 0.6f;
            }
            else if (partLower.Contains("center") || partLower.Contains("middle"))
            {
                data.leftRightBalance = 0.5f;
            }
            
            // Front/back parsing
            if (partLower.Contains("front") || partLower.Contains("forward"))
            {
                data.frontBackBalance = 0.2f;
            }
            else if (partLower.Contains("back") || partLower.Contains("rear"))
            {
                data.frontBackBalance = 0.8f;
            }
            
            // Distance parsing
            if (partLower.Contains("far") || partLower.Contains("distant"))
            {
                data.distance = 0.8f;
            }
            else if (partLower.Contains("mid") || partLower.Contains("medium"))
            {
                data.distance = 0.5f;
            }
            else if (partLower.Contains("close") || partLower.Contains("near"))
            {
                data.distance = 0.2f;
            }
            
            // Occlusion parsing
            if (partLower.Contains("occlude") || partLower.Contains("behind") || partLower.Contains("through"))
            {
                data.occlusion = 0.6f;
            }
            
            // Elevation parsing
            if (partLower.Contains("high") || partLower.Contains("above") || partLower.Contains("top"))
            {
                data.elevation = 0.8f;
            }
            else if (partLower.Contains("low") || partLower.Contains("below") || partLower.Contains("bottom"))
            {
                data.elevation = 0.2f;
            }
            
            // Environment parsing
            foreach (var env in environmentMap.Keys)
            {
                if (partLower.Contains(env))
                {
                    data.environment = env;
                    break;
                }
            }
        }
        
        // Calculate derived parameters
        CalculateSpatialParameters(data);
        
        cachedPositionData[filename] = data;
        return data;
    }

    private void CalculateSpatialParameters(AudioPositioningData data)
    {
        // 1. Stereo pan based on left/right balance
        data.panStereo = (data.leftRightBalance - 0.5f) * 2f; // -1 to 1
        
        // 2. Volume attenuation based on distance and occlusion
        float distanceAtt = distanceAttenuation.Evaluate(data.distance);
        float occlusionAtt = occlusionAttenuation.Evaluate(data.occlusion);
        data.volumeAttenuation = Mathf.Min(distanceAtt, occlusionAtt);
        
        // 3. Low-pass filter for occlusion (high frequencies absorbed)
        data.lowPassCutoff = Mathf.Lerp(22000f, maxLowPassCutoff, data.occlusion);
        
        // 4. Reverb level based on environment and distance
        if (environmentMap.TryGetValue(data.environment, out int presetIndex))
        {
            data.reverbLevel = data.distance * 0.7f; // More reverb for distant sounds
        }
        
        // 5. Delay time for front/back positioning (HRTF simulation)
        // Sounds from front arrive slightly earlier than from back
        data.delayTime = data.frontBackBalance * maxDelayTime * 0.3f;
    }

    /// <summary>
    /// Apply binaural processing to the audio source
    /// </summary>
    public void ApplyBinauralProcessing(AudioClip clip = null)
    {
        if (!enableBinaural) return;
        
        string filename = clip != null ? clip.name : audioSource.clip.name;
        AudioPositioningData data = ParseFilename(filename);
        
        // Apply stereo panning (simple version - for full HRTF you'd need a spatializer plugin)
        audioSource.panStereo = data.panStereo;
        
        // Apply volume attenuation
        audioSource.volume = data.volumeAttenuation;
        
        // Apply low-pass filter for occlusion
        lowPassFilter.cutoffFrequency = data.lowPassCutoff;
        lowPassFilter.enabled = data.occlusion > 0.1f;
        
        // Apply reverb based on environment
        if (environmentMap.TryGetValue(data.environment, out int presetIndex))
        {
            reverbFilter.reverbPreset = environmentPresets[presetIndex];
            reverbFilter.dryLevel = 0f;
            reverbFilter.room = Mathf.Lerp(-10000f, 0f, data.reverbLevel);
            reverbFilter.enabled = data.reverbLevel > 0.1f;
        }
        
        // Apply delay for front/back positioning (simplified ITD)
        // Note: For true binaural audio, you'd need separate delay per ear
        echoFilter.delay = data.delayTime * 1000f; // Convert to milliseconds
        echoFilter.enabled = data.delayTime > 0.001f;
        
        // Apply elevation effect (pitch shift simulation)
        // Higher sounds get slightly brighter (less accurate but perceptually helpful)
        if (data.elevation > 0.6f) // Above ear level
        {
            audioSource.pitch = 1.05f;
        }
        else if (data.elevation < 0.4f) // Below ear level
        {
            audioSource.pitch = 0.95f;
        }
        else
        {
            audioSource.pitch = 1f;
        }
    }

    /// <summary>
    /// Play audio with binaural processing
    /// </summary>
    public void PlayBinaural(string filename)
    {
        // Load audio clip (you might want to use Addressables or Resources)
        AudioClip clip = Resources.Load<AudioClip>($"Audio/{filename}");
        if (clip == null)
        {
            Debug.LogWarning($"Audio clip not found: {filename}");
            return;
        }
        
        audioSource.clip = clip;
        ApplyBinauralProcessing(clip);
        audioSource.Play();
    }

    /// <summary>
    /// Play one-shot with binaural processing
    /// </summary>
    public void PlayBinauralOneShot(string filename, float volumeScale = 1f)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/{filename}");
        if (clip == null)
        {
            Debug.LogWarning($"Audio clip not found: {filename}");
            return;
        }
        
        ApplyBinauralProcessing(clip);
        audioSource.PlayOneShot(clip, volumeScale * audioSource.volume);
    }

    // Update for dynamic sources (if you want real-time updates)
    private void Update()
    {
        if (audioSource.isPlaying && enableBinaural)
        {
            // You could add dynamic updates here if the source moves
            // For static environmental sounds, this isn't needed
        }
    }

    /// <summary>
    /// Naming convention helper for your audio team
    /// </summary>
    public static string GetNamingConventionGuide()
    {
        return @"BINAURAL AUDIO NAMING CONVENTION:
        Format: name_direction_distance_environment.extension
        
        Direction modifiers:
        - farleft, left, midleft, center, midright, right, farright
        - front, forward, back, rear
        
        Distance modifiers:
        - close, near, mid, medium, far, distant
        
        Occlusion modifiers:
        - occluded, behind, through
        
        Elevation modifiers:
        - high, above, top, low, below, bottom
        
        Environment modifiers:
        - open, field, plain
        - tree, forest, wood
        - cave, tunnel, cavern
        - water, river, lake, sea
        - stone, rock, mountain, dwarf
        
        Examples:
        - birdsong001_farleft_trees.wav
        - hammerstrike_center_close_stone.wav
        - waterfall_right_far_water.wav
        - dwarflaugh_back_occluded_cave.wav";
    }
}