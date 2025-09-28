using UnityEngine;
using System.Collections;

[System.Serializable]
public class CloudLayerConfig
{
    [Header("Cloud Appearance")]
    public string layerName = "Cloud Layer";
    public Material cloudMaterial;
    public int maxClouds = 50;
    public float spawnRadius = 100f;

    [Header("Noise Parameters")]
    [Range(0f, 1f)] public float coverage = 0.5f;
    [Range(0f, 1f)] public float density = 0.5f;
    [Range(0.1f, 10f)] public float noiseScale = 2f;
    [Range(0f, 1f)] public float detailIntensity = 0.3f;

    [Header("Geometry")]
    public float altitude = 100f;
    public float thickness = 20f;
    public int layers = 8;
    public float cloudScale = 10f;

    [Header("Movement")]
    public float windSpeed = 1f;
    public float altitudeWindMultiplier = 1f;

    [Header("Transition Speeds")]
    public float coverageTransitionSpeed = 0.5f;
    public float densityTransitionSpeed = 0.5f;
    public float scaleTransitionSpeed = 0.3f;
}

public class CloudLayer : MonoBehaviour
{
    public CloudLayerConfig config;

    private Transform cameraTransform;
    private ArrayList activeClouds;
    private Queue cloudPool;
    private Coroutine spawnCoroutine;

    // Current values for smooth transitions
    private float currentCoverage;
    private float currentDensity;
    private float currentNoiseScale;
    private float currentDetailIntensity;

    // Target values from weather system
    private float targetCoverage;
    private float targetDensity;
    private float targetNoiseScale;
    private float targetDetailIntensity;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        activeClouds = new ArrayList();
        cloudPool = new Queue();

        // Initialize current values
        currentCoverage = config.coverage;
        currentDensity = config.density;
        currentNoiseScale = config.noiseScale;
        currentDetailIntensity = config.detailIntensity;

        targetCoverage = config.coverage;
        targetDensity = config.density;
        targetNoiseScale = config.noiseScale;
        targetDetailIntensity = config.detailIntensity;

        // Pre-populate pool
        for (int i = 0; i < config.maxClouds / 2; i++)
        {
            CreateCloudForPool();
        }

        spawnCoroutine = StartCoroutine(SpawnCloudsRoutine());
        StartCoroutine(UpdateCloudsRoutine());
    }

    void Update()
    {
        UpdateParameters();
        UpdateShaderProperties();
    }

    private void UpdateParameters()
    {
        // Smoothly interpolate towards target values
        currentCoverage = Mathf.Lerp(currentCoverage, targetCoverage, config.coverageTransitionSpeed * Time.deltaTime);
        currentDensity = Mathf.Lerp(currentDensity, targetDensity, config.densityTransitionSpeed * Time.deltaTime);
        currentNoiseScale = Mathf.Lerp(currentNoiseScale, targetNoiseScale, config.scaleTransitionSpeed * Time.deltaTime);
        currentDetailIntensity = Mathf.Lerp(currentDetailIntensity, targetDetailIntensity, config.densityTransitionSpeed * Time.deltaTime);
    }

    private void UpdateShaderProperties()
    {
        if (config.cloudMaterial != null)
        {
            config.cloudMaterial.SetFloat("_Coverage", currentCoverage);
            config.cloudMaterial.SetFloat("_Density", currentDensity);
            config.cloudMaterial.SetFloat("_NoiseScale", currentNoiseScale);
            config.cloudMaterial.SetFloat("_DetailIntensity", currentDetailIntensity);
            config.cloudMaterial.SetFloat("_WindSpeed", config.windSpeed * config.altitudeWindMultiplier);
        }
    }

    private IEnumerator SpawnCloudsRoutine()
    {
        while (true)
        {
            // Calculate desired cloud count based on coverage
            int desiredCloudCount = Mathf.RoundToInt(config.maxClouds * currentCoverage);

            // Spawn or despawn clouds to match desired count
            while (activeClouds.Count < desiredCloudCount && cloudPool.Count > 0)
            {
                SpawnCloud();
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            }

            while (activeClouds.Count > desiredCloudCount && activeClouds.Count > 0)
            {
                DespawnOldestCloud();
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(2f); // Check every 2 seconds
        }
    }

    private IEnumerator UpdateCloudsRoutine()
    {
        while (true)
        {
            // Update cloud positions relative to camera
            for (int i = activeClouds.Count - 1; i >= 0; i--)
            {
                CloudQuadStack cloud = (CloudQuadStack)activeClouds[i];
                if (cloud != null)
                {
                    cloud.UpdatePosition(cameraTransform.position, config.spawnRadius);
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private void SpawnCloud()
    {
        if (cloudPool.Count == 0) return;

        CloudQuadStack cloud = (CloudQuadStack)cloudPool.Dequeue();
        Vector3 spawnPos = CalculateSpawnPosition();

        cloud.Activate(spawnPos, config.altitude, config.thickness, config.layers, config.cloudScale);
        activeClouds.Add(cloud);
    }

    private void DespawnOldestCloud()
    {
        if (activeClouds.Count > 0)
        {
            CloudQuadStack cloud = (CloudQuadStack)activeClouds[0];
            activeClouds.RemoveAt(0);
            cloud.Deactivate();
            cloudPool.Enqueue(cloud);
        }
    }

    private Vector3 CalculateSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * config.spawnRadius;
        Vector3 cameraPos = cameraTransform.position;
        return new Vector3(cameraPos.x + randomCircle.x, config.altitude, cameraPos.z + randomCircle.y);
    }

    private void CreateCloudForPool()
    {
        GameObject cloudGO = new GameObject("CloudQuadStack");
        CloudQuadStack cloudStack = cloudGO.AddComponent<CloudQuadStack>();
        cloudStack.Initialize(config.cloudMaterial);
        cloudGO.SetActive(false);
        cloudPool.Enqueue(cloudStack);
    }

    // Public API for weather system
    public void SetTargetParameters(float coverage, float density, float noiseScale, float detailIntensity)
    {
        targetCoverage = Mathf.Clamp01(coverage);
        targetDensity = Mathf.Clamp01(density);
        targetNoiseScale = Mathf.Clamp(noiseScale, 0.1f, 10f);
        targetDetailIntensity = Mathf.Clamp01(detailIntensity);
    }

    public float GetCurrentCoverage() { return currentCoverage; }
    public float GetCurrentDensity() { return currentDensity; }

    void OnDestroy()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }
}