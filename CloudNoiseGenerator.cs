using UnityEngine;
using System.Collections;

public class CloudNoiseGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureSize = 128;
    public string savePath = "Assets/Textures/CloudNoise3D.asset";

    [Header("Noise Settings")]
    public float frequency = 1.0f;
    public int octaves = 3;
    public float lacunarity = 2.0f;
    public float persistence = 0.5f;

    [Header("Generate Button")]
    [SerializeField] private bool generateNoise = false;

    void OnValidate()
    {
        if (generateNoise)
        {
            generateNoise = false; // Reset the button
            Generate3DNoiseTexture();
        }
    }

    [ContextMenu("Generate 3D Noise Texture")]
    public void Generate3DNoiseTexture()
    {
        if (textureSize <= 0)
        {
            Debug.LogError("Texture size must be greater than 0");
            return;
        }

        Debug.Log("Starting 3D noise texture generation...");

        Texture3D texture = new Texture3D(textureSize, textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[textureSize * textureSize * textureSize];

        float startTime = Time.realtimeSinceStartup;

        for (int z = 0; z < textureSize; z++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float noiseValue = GenerateFractalNoise(x, y, z);
                    colors[x + y * textureSize + z * textureSize * textureSize] = new Color(noiseValue, noiseValue, noiseValue, 1.0f);
                }
            }

            // Progress update for large textures
            if (z % 16 == 0)
            {
                float progress = (float)z / textureSize * 100f;
                Debug.Log($"Generation progress: {progress:F1}%");
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

#if UNITY_EDITOR
        try
        {
            // Create the directory if it doesn't exist
            string directory = System.IO.Path.GetDirectoryName(savePath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            UnityEditor.AssetDatabase.CreateAsset(texture, savePath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"3D Noise Texture generated successfully at: {savePath}");
            Debug.Log($"Generation time: {endTime - startTime:F2} seconds");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save texture: {e.Message}");
        }
#else
        Debug.LogWarning("3D texture generation only works in Unity Editor");
#endif
    }

    private float GenerateFractalNoise(float x, float y, float z)
    {
        float value = 0.0f;
        float amplitude = 1.0f;
        float maxValue = 0.0f;
        float currentFrequency = frequency;

        for (int i = 0; i < octaves; i++)
        {
            float noiseValue = GenerateSimpleNoise(x * currentFrequency, y * currentFrequency, z * currentFrequency);
            value += noiseValue * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            currentFrequency *= lacunarity;
        }

        return Mathf.Clamp01(value / maxValue);
    }

    private float GenerateSimpleNoise(float x, float y, float z)
    {
        // Use Unity's built-in Perlin noise for simplicity
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6f;
    }

    // Alternative: Generate a simple cloud-like texture without Perlin noise
    [ContextMenu("Generate Simple Cloud Texture")]
    public void GenerateSimpleCloudTexture()
    {
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[256 * 256];

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                // Simple cloud-like pattern
                float dx = (x - 128f) / 128f;
                float dy = (y - 128f) / 128f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float cloud = Mathf.Clamp01(1f - distance * 1.5f);

                // Add some turbulence
                cloud += Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.3f;
                cloud = Mathf.Clamp01(cloud);

                colors[x + y * 256] = new Color(cloud, cloud, cloud, 1f);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

#if UNITY_EDITOR
        string cloudSavePath = "Assets/Textures/SimpleCloudNoise.png";
        System.IO.File.WriteAllBytes(cloudSavePath, texture.EncodeToPNG());
        UnityEditor.AssetDatabase.ImportAsset(cloudSavePath);
        Debug.Log($"Simple cloud texture saved at: {cloudSavePath}");
#endif
    }

    // Editor-only utility to check if texture exists
    [ContextMenu("Check Texture Status")]
    public void CheckTextureStatus()
    {
#if UNITY_EDITOR
        UnityEngine.Object existingTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture3D>(savePath);
        if (existingTexture != null)
        {
            Debug.Log($"Texture already exists at: {savePath}");
        }
        else
        {
            Debug.Log($"No texture found at: {savePath}");
        }
#endif
    }
}