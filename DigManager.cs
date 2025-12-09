using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DigManager : MonoBehaviour
{
    [Header("Dig Settings")]
    public LayerMask terrainLayer; // Set to the terrain's layer in inspector
    [Range(2f, 20f)] public float initialDistance = 8f;
    [Range(1f, 5f)] public float minDistance = 2f;
    [Range(10f, 50f)] public float maxDistance = 20f;
    [Range(1f, 10f)] public float distanceAdjustSpeed = 5f;
    [Range(0.005f, 0.05f)] public float digAmount = 0.01f; // Normalized height change (0-1 scale)
    [Range(1, 10)] public int brushSize = 3; // Odd numbers for centered brush

    [Header("Preview")]
    public Color previewColor = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent red for dig marker

    [Header("Audio")]
    public AudioClip digSound;
    public AudioClip addSound;

    private float currentDistance;
    private GameObject previewMarker;
    private Terrain terrain;
    private AudioSource audioSource;

    private void Awake()
    {
        currentDistance = initialDistance;
        terrain = ProcGenManager.Instance.terrain;
        if (terrain == null)
        {
            Debug.LogError("No terrain found in ProcGenManager.");
        }
        audioSource = GetComponent<AudioSource>();
        CreatePreviewMarker();
    }

    private void CreatePreviewMarker()
    {
        previewMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        previewMarker.transform.localScale = Vector3.one * (brushSize * 0.2f); // Scale based on brush for visual cue
        Destroy(previewMarker.GetComponent<Collider>()); // No physics needed
        Renderer rend = previewMarker.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Standard"));
        rend.material.color = previewColor;
        rend.material.SetFloat("_Mode", 2); // Transparent render mode
        rend.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        rend.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        rend.material.SetInt("_ZWrite", 0);
        rend.material.EnableKeyword("_ALPHABLEND_ON");
        rend.material.renderQueue = 3000;
        previewMarker.SetActive(false);
    }

    private void Update()
    {
        // Only process if in Dig mode (respects mode toggle, no conflict with other inputs)
        if (ModeController.Instance == null || ModeController.Instance.currentMode != PlayerMode.Dig)
        {
            previewMarker.SetActive(false);
            return;
        }

        previewMarker.SetActive(true);

        // Adjust distance with mouse Y (up/down moves marker away/back, as requested)
        float mouseY = Input.GetAxis("Mouse Y");
        currentDistance -= mouseY * distanceAdjustSpeed * Time.deltaTime;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // Position marker in front of character, clamped to terrain hit if closer
        Vector3 direction = transform.forward;
        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, direction); // Start from approximate eye height
        Vector3 targetPos = transform.position + direction * currentDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, terrainLayer))
        {
            if (hit.distance < currentDistance)
            {
                targetPos = hit.point;
            }
        }

        previewMarker.transform.position = targetPos;

        // Dig on left mouse, add on right mouse (as requested)
        if (Input.GetMouseButtonDown(0))
        {
            ModifyTerrain(targetPos, -digAmount);
        }
        if (Input.GetMouseButtonDown(1))
        {
            ModifyTerrain(targetPos, digAmount);
        }
    }

    private void ModifyTerrain(Vector3 worldPos, float amount)
    {
        // Convert world pos to heightmap coordinates
        Vector3 terrainLocal = worldPos - terrain.transform.position;
        int mapRes = terrain.terrainData.heightmapResolution;
        int hx = Mathf.RoundToInt((terrainLocal.x / terrain.terrainData.size.x) * (mapRes - 1));
        int hz = Mathf.RoundToInt((terrainLocal.z / terrain.terrainData.size.z) * (mapRes - 1));

        // Calculate brush area, clamped to bounds
        int half = brushSize / 2;
        int startX = Mathf.Clamp(hx - half, 0, mapRes - 1);
        int startZ = Mathf.Clamp(hz - half, 0, mapRes - 1);
        int width = Mathf.Min(brushSize, mapRes - startX);
        int height = Mathf.Min(brushSize, mapRes - startZ);

        // Get current heights, modify, and set back
        float[,] heights = terrain.terrainData.GetHeights(startX, startZ, width, height);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                heights[i, j] = Mathf.Clamp01(heights[i, j] + amount);
            }
        }
        terrain.terrainData.SetHeightsDelayLOD(startX, startZ, heights);

        // Play appropriate sound
        AudioClip sound = (amount < 0) ? digSound : addSound;
        if (audioSource && sound != null)
        {
            audioSource.PlayOneShot(sound);
        }
    }
}