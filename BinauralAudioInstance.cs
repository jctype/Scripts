using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[RequireComponent(typeof(AudioSource))]
public class BinauralAudioInstance : MonoBehaviour
{
    [System.Serializable]
    public class SpatialSettings
    {
        [Header("Position")]
        [Range(-180, 180)] 
        public float azimuth = 0f; // -180 to 180 degrees (-90 = left, 0 = center, 90 = right)
        
        [Range(-90, 90)]
        public float elevation = 0f; // -90 to 90 degrees (-90 = below, 0 = ear level, 90 = above)
        
        [Header("Distance")]
        [Range(0.1f, 100f)]
        public float distance = 1f; // meters (near=1-5, mid=5-10, far=10+, default=<1)
        
        [Header("Environment")]
        public string environment = "open";
        
        [Header("Occlusion")]
        [Range(0f, 1f)]
        public float occlusion = 0f; // 0 = clear, 1 = fully occluded
        
        // Calculated parameters
        [HideInInspector] public float panStereo = 0f;
        [HideInInspector] public float volumeAttenuation = 1f;
        [HideInInspector] public float lowPassCutoff = 22000f;
        [HideInInspector] public float reverbMix = 0f;
    }
    
    [Header("Audio Source")]
    public AudioSource audioSource;
    
    [Header("Spatial Settings")]
    public SpatialSettings settings = new SpatialSettings();
    
    [Header("Auto-parse from filename?")]
    public bool parseFromFilename = true;
    
    [Header("Angle System")]
    public float angleUnderscoreBefore = 22.5f;    // _left pattern (e.g., nearleft)
    public float angleUnderscoreBoth = 45f;        // _left_ pattern (e.g., near_left)
    public float angleUnderscoreAfter = 90f;       // left_ pattern (e.g., left_trees)
    
    // Audio filters
    private AudioLowPassFilter lowPassFilter;
    private AudioReverbFilter reverbFilter;
    private AudioEchoFilter echoFilter;
    
    // Static environment presets
    private static Dictionary<string, AudioReverbPreset> environmentPresets = new Dictionary<string, AudioReverbPreset>
    {
        {"open", AudioReverbPreset.Off},
        {"field", AudioReverbPreset.Off},
        {"plain", AudioReverbPreset.Off},
        {"trees", AudioReverbPreset.Forest},
        {"forest", AudioReverbPreset.Forest},
        {"wood", AudioReverbPreset.Forest},
        {"cave", AudioReverbPreset.Cave},
        {"cavern", AudioReverbPreset.Cave},
        {"tunnel", AudioReverbPreset.Cave},
        {"water", AudioReverbPreset.Underwater},
        {"river", AudioReverbPreset.Underwater},
        {"lake", AudioReverbPreset.Underwater},
        {"sea", AudioReverbPreset.Underwater},
        {"stone", AudioReverbPreset.Stoneroom},
        {"rock", AudioReverbPreset.Stoneroom},
        {"mountain", AudioReverbPreset.Stoneroom},
        {"dwarf", AudioReverbPreset.Stoneroom}
    };
    
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Add filters if needed
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        if (lowPassFilter == null)
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        
        reverbFilter = GetComponent<AudioReverbFilter>();
        if (reverbFilter == null)
            reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
        
        echoFilter = GetComponent<AudioEchoFilter>();
        if (echoFilter == null)
            echoFilter = gameObject.AddComponent<AudioEchoFilter>();
    }
    
    private void Start()
    {
        if (parseFromFilename && audioSource.clip != null)
        {
            ParseSettingsFromFilename(audioSource.clip.name);
        }
        
        ApplySpatialSettings();
    }
    
    private void Update()
    {
        // Update in real-time if needed (for moving sounds)
        ApplySpatialSettings();
    }
    
    public void ParseSettingsFromFilename(string filename)
    {
        string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filename).ToLower();
        
        // Reset to defaults
        settings.azimuth = 0f;
        settings.elevation = 0f;
        settings.distance = 0.8f; // Default (<1 meter)
        settings.environment = "open";
        settings.occlusion = 0f;
        
        // Check for left/right
        bool hasLeft = nameWithoutExt.Contains("left");
        bool hasRight = nameWithoutExt.Contains("right");
        
        if (hasLeft || hasRight)
        {
            string direction = hasLeft ? "left" : "right";
            
            // Use regex to find the direction word with context
            string pattern = @"([^a-z]|^)(" + direction + @")([^a-z]|$)";
            Match match = Regex.Match(nameWithoutExt, pattern);
            
            if (match.Success)
            {
                string beforeChar = match.Groups[1].Value;
                string afterChar = match.Groups[3].Value;
                
                bool underscoreBefore = beforeChar == "_" || (beforeChar == "" && !char.IsLetterOrDigit(nameWithoutExt[0]));
                bool underscoreAfter = afterChar == "_" || (afterChar == "" && match.Index + match.Length >= nameWithoutExt.Length);
                
                // Check for concatenated environment
                bool envConcatenated = false;
                if (!underscoreAfter && afterChar != "" && char.IsLetter(afterChar[0]))
                {
                    // Environment might be concatenated directly after direction
                    string remaining = nameWithoutExt.Substring(match.Index + match.Length);
                    foreach (var env in environmentPresets.Keys)
                    {
                        if (remaining.StartsWith(env))
                        {
                            envConcatenated = true;
                            settings.environment = env;
                            break;
                        }
                    }
                }
                
                // Apply angle based on underscore pattern
                if (hasLeft)
                {
                    if (underscoreBefore && !underscoreAfter && !envConcatenated)
                    {
                        // Pattern: _left (e.g., nearleft_trees)
                        settings.azimuth = -angleUnderscoreBefore;
                    }
                    else if (underscoreBefore && (underscoreAfter || envConcatenated))
                    {
                        // Pattern: _left_ or _left[env] (e.g., near_left_trees or near_lefttrees)
                        settings.azimuth = envConcatenated ? -angleUnderscoreAfter : -angleUnderscoreBoth;
                    }
                    else if (!underscoreBefore && (underscoreAfter || envConcatenated))
                    {
                        // Pattern: left_ or left[env] (e.g., left_trees or lefttrees)
                        settings.azimuth = -angleUnderscoreAfter;
                    }
                }
                else // hasRight
                {
                    if (underscoreBefore && !underscoreAfter && !envConcatenated)
                    {
                        settings.azimuth = angleUnderscoreBefore;
                    }
                    else if (underscoreBefore && (underscoreAfter || envConcatenated))
                    {
                        settings.azimuth = envConcatenated ? angleUnderscoreAfter : angleUnderscoreBoth;
                    }
                    else if (!underscoreBefore && (underscoreAfter || envConcatenated))
                    {
                        settings.azimuth = angleUnderscoreAfter;
                    }
                }
            }
        }
        
        // Parse distance
        if (nameWithoutExt.Contains("near"))
            settings.distance = Random.Range(1f, 5f); // 1-5 meters
        else if (nameWithoutExt.Contains("mid"))
            settings.distance = Random.Range(5f, 10f); // 5-10 meters
        else if (nameWithoutExt.Contains("far"))
            settings.distance = Random.Range(10f, 30f); // 10-30 meters
        else
            settings.distance = Random.Range(0.3f, 0.8f); // <1 meter (default)
        
        // Parse environment (if not already set from concatenated form)
        if (settings.environment == "open")
        {
            foreach (var env in environmentPresets.Keys)
            {
                if (nameWithoutExt.Contains(env))
                {
                    settings.environment = env;
                    break;
                }
            }
        }
        
        // Parse occlusion
        if (nameWithoutExt.Contains("occlude") || nameWithoutExt.Contains("through") || 
            nameWithoutExt.Contains("behind") || nameWithoutExt.Contains("filtered") ||
            nameWithoutExt.Contains("muffled"))
        {
            settings.occlusion = Random.Range(0.3f, 0.8f);
        }
    }
    
    public void ApplySpatialSettings()
    {
        // Calculate derived parameters
        CalculateSpatialParameters();
        
        // Apply to audio source and filters
        ApplyToAudioSource();
    }
    
    private void CalculateSpatialParameters()
    {
        // 1. Stereo pan based on azimuth
        // Front hemisphere: -90 to 90 degrees
        float effectiveAzimuth = settings.azimuth;
        if (effectiveAzimuth > 180f) effectiveAzimuth -= 360f;
        if (effectiveAzimuth < -180f) effectiveAzimuth += 360f;
        
        if (Mathf.Abs(effectiveAzimuth) <= 90f)
        {
            // Front: full panning
            settings.panStereo = Mathf.Sin(effectiveAzimuth * Mathf.Deg2Rad);
        }
        else
        {
            // Back: reduced panning
            float backAngle = effectiveAzimuth > 0 ? effectiveAzimuth - 90f : effectiveAzimuth + 90f;
            settings.panStereo = Mathf.Sin(backAngle * Mathf.Deg2Rad) * 0.5f;
        }
        
        // 2. Volume attenuation based on distance (inverse square law simplified)
        settings.volumeAttenuation = 1f / (1f + settings.distance * 0.15f);
        
        // 3. Apply occlusion (reduces high frequencies)
        settings.lowPassCutoff = Mathf.Lerp(22000f, 800f, settings.occlusion);
        
        // 4. Reverb mix based on environment and distance
        settings.reverbMix = Mathf.Clamp01(settings.distance * 0.08f);
    }
    
    private void ApplyToAudioSource()
    {
        if (audioSource == null) return;
        
        // Apply stereo pan
        audioSource.panStereo = Mathf.Clamp(settings.panStereo, -1f, 1f);
        
        // Apply volume (respects AudioManager's volume settings)
        audioSource.volume *= settings.volumeAttenuation;
        
        // Apply low-pass filter for occlusion
        lowPassFilter.cutoffFrequency = settings.lowPassCutoff;
        lowPassFilter.enabled = settings.occlusion > 0.05f;
        
        // Apply reverb based on environment
        if (environmentPresets.TryGetValue(settings.environment, out AudioReverbPreset preset))
        {
            reverbFilter.reverbPreset = preset;
            reverbFilter.dryLevel = 0f;
            reverbFilter.room = Mathf.Lerp(-10000f, -500f, settings.reverbMix);
            reverbFilter.enabled = settings.reverbMix > 0.1f && settings.environment != "open";
        }
        else
        {
            reverbFilter.enabled = false;
        }
        
        // Apply slight delay for back sounds (simplified ITD)
        if (Mathf.Abs(settings.azimuth) > 90f)
        {
            echoFilter.delay = 30f; // 30ms delay for back sounds
            echoFilter.wetMix = 0.1f;
            echoFilter.enabled = true;
        }
        else
        {
            echoFilter.enabled = false;
        }
        
        // Apply elevation (subtle pitch shift for height perception)
        if (settings.elevation > 30f)
            audioSource.pitch = 1.03f; // Above = slightly higher pitch
        else if (settings.elevation < -30f)
            audioSource.pitch = 0.97f; // Below = slightly lower pitch
        else
            audioSource.pitch = 1f;
    }
    
    // Public API for runtime adjustment
    public void SetPosition(float azimuth, float elevation, float distance)
    {
        settings.azimuth = azimuth;
        settings.elevation = elevation;
        settings.distance = distance;
        ApplySpatialSettings();
    }
    
    public void SetEnvironment(string environment)
    {
        settings.environment = environment;
        ApplySpatialSettings();
    }
    
    public void SetOcclusion(float occlusion)
    {
        settings.occlusion = occlusion;
        ApplySpatialSettings();
    }
    
    public void SetAllSettings(SpatialSettings newSettings)
    {
        settings = newSettings;
        ApplySpatialSettings();
    }
    
    // Static helper for naming convention
    public static string GetNamingConventionGuide()
    {
        return @"BINAURAL AUDIO NAMING CONVENTION:
        
        UNDERSCORE-BASED ANGLE SYSTEM:
        
        1. _left pattern (22.5° left):
           Format: name_nearleft_environment.wav
           Example: bird_nearleft_trees.wav
           
        2. _left_ pattern (45° left):
           Format: name_near_left_environment.wav  
           Example: bird_near_left_trees.wav
           
        3. left_ pattern (90° left):
           Format: name_left_environment.wav OR name_leftenvironment.wav
           Example: bird_left_trees.wav OR bird_near_lefttrees.wav
           
        Same for 'right': nearright=22.5°, near_right=45°, right=90°
        
        DISTANCE MODIFIERS:
        - near: 1-5 meters
        - mid: 5-10 meters  
        - far: 10-30 meters
        - (default): <1 meter
        
        ENVIRONMENT MODIFIERS:
        - open, field, plain
        - trees, forest, wood
        - cave, tunnel, cavern  
        - water, river, lake, sea
        - stone, rock, mountain, dwarf
        
        OCCLUSION MODIFIERS:
        - occluded, through, behind, filtered, muffled
        
        EXAMPLES:
        - bird_nearleft_trees.wav = 22.5° left, near, trees
        - hammer_mid_right_stone.wav = 45° right, medium, stone
        - waterfall_far_left_water.wav = 45° left, far, water
        - dwarflaugh_right_cave.wav = 90° right, default, cave
        - wind_near_lefttrees.wav = 90° left, near, trees (concatenated)
        ";
    }
}