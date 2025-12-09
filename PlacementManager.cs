using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    [Header("Placement Settings")]
    public LayerMask socketLayer;
    public float rotationSpeed = 90f;
    public Color ghostColor = new Color(1, 1, 1, 0.5f);

    [Header("Prefabs - Assign in Inspector")]
    public GameObject defaultRebounderPrefab;
    public Dictionary<RebounderType, GameObject> rebounderPrefabMap = new Dictionary<RebounderType, GameObject>();

    [Header("Heatmap Preview")]
    public float simVelocity = 10f;
    public int simRayCount = 64;
    public float simAngleSpread = 15f;
    private List<SocketData> allSockets = new List<SocketData>();
    private Dictionary<RebounderType, Vector3> trampLaunchVectors = new Dictionary<RebounderType, Vector3>()
    {
        {RebounderType.Basic, Vector3.up * 0.8f + Vector3.forward * 0.6f},
        {RebounderType.Angled, Vector3.up * 0.7f + Vector3.forward * 0.7f},
        {RebounderType.Sticky, Vector3.up * 0.6f + Vector3.forward * 0.8f},
        {RebounderType.Boost, Vector3.up * 1.0f + Vector3.forward * 0.5f},
    };

    [Header("Audio")]
    public AudioClip trampPlaceSound;

    private RebounderType selectedType = RebounderType.Basic;
    private GameObject previewGhost;
    private GameObject socketUnderMouse;
    private float lastSimTime = 0f;
    private const float SIM_INTERVAL = 0.5f;

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
    }

    private void Start()
    {
        // Initialize prefab map if empty
        if (rebounderPrefabMap.Count == 0 && defaultRebounderPrefab != null)
        {
            rebounderPrefabMap[RebounderType.Basic] = defaultRebounderPrefab;
        }
    }

    public void OnSocketsGenerated(List<SocketData> sockets)
    {
        allSockets = sockets;
        foreach (var socket in allSockets)
            socket.UpdateVisual(0f);
    }

    public void SelectRebounder(RebounderType type)
    {
        selectedType = type;
        Debug.Log($"Selected rebounder: {type}");

        if (previewGhost != null)
        {
            Destroy(previewGhost);
            previewGhost = null;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Build)
        {
            if (previewGhost != null)
                previewGhost.SetActive(false);
            return;
        }

        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, socketLayer))
        {
            if (socketUnderMouse != hit.collider.gameObject)
            {
                socketUnderMouse = hit.collider.gameObject;

                if (previewGhost == null)
                    CreatePreviewGhost();

                if (previewGhost != null)
                {
                    previewGhost.transform.position = hit.point + Vector3.up * 0.1f;
                    previewGhost.SetActive(true);
                }
            }

            if (previewGhost != null)
            {
                if (Time.time - lastSimTime > SIM_INTERVAL)
                {
                    SimulateHeatmapFromAllTramps();
                    lastSimTime = Time.time;
                }

                if (Input.GetKey(KeyCode.Q))
                    previewGhost.transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
                if (Input.GetKey(KeyCode.E))
                    previewGhost.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

                if (Input.GetMouseButtonDown(0))
                {
                    PlaceRebounder();
                }
            }
        }
        else if (previewGhost != null)
        {
            previewGhost.SetActive(false);
            socketUnderMouse = null;
        }

        if (Input.GetMouseButtonDown(1) && previewGhost != null)
        {
            Destroy(previewGhost);
            previewGhost = null;
        }
    }

    private GameObject GetRebounderPrefab(RebounderType type)
    {
        // 1. Check prefab map
        if (rebounderPrefabMap.TryGetValue(type, out GameObject prefab) && prefab != null)
            return prefab;

        // 2. Try Resources fallback
        string resourcePath = $"Prefabs/Rebounders/{type}";
        prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null)
        {
            rebounderPrefabMap[type] = prefab;
            return prefab;
        }

        // 3. Use default
        if (defaultRebounderPrefab != null)
        {
            Debug.LogWarning($"Rebounder prefab for {type} not found. Using default.");
            return defaultRebounderPrefab;
        }

        // 4. Create emergency fallback
        Debug.LogError($"No rebounder prefab available for {type}! Creating fallback.");
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.name = $"Fallback_{type}";
        fallback.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);

        var controller = fallback.AddComponent<RebounderController>();
        controller.type = type;

        return fallback;
    }

    private void CreatePreviewGhost()
    {
        GameObject prefab = GetRebounderPrefab(selectedType);
        if (prefab == null)
        {
            Debug.LogError($"Cannot create ghost: No prefab for {selectedType}");
            return;
        }

        previewGhost = Instantiate(prefab);
        previewGhost.name = $"Ghost_{selectedType}";

        Renderer[] renderers = previewGhost.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material[] materials = rend.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material ghostMat = new Material(materials[i]);
                Color color = ghostMat.color;
                color.a = ghostColor.a;
                ghostMat.color = color;

                ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetInt("_ZWrite", 0);
                ghostMat.DisableKeyword("_ALPHATEST_ON");
                ghostMat.EnableKeyword("_ALPHABLEND_ON");
                ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ghostMat.renderQueue = 3000;

                materials[i] = ghostMat;
            }
            rend.materials = materials;
        }

        Collider[] colliders = previewGhost.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = false;

        // Disable any scripts on ghost
        MonoBehaviour[] scripts = previewGhost.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
            script.enabled = false;
    }

    private void PlaceRebounder()
    {
        if (previewGhost == null) return;

        GameObject prefab = GetRebounderPrefab(selectedType);
        if (prefab == null)
        {
            Debug.LogError($"Cannot place: No prefab for {selectedType}");
            return;
        }

        GameObject realRebounder = Instantiate(
            prefab,
            previewGhost.transform.position,
            previewGhost.transform.rotation,
            previewGhost.transform.parent
        );

        realRebounder.name = $"{selectedType}_Placed";

        // Play sound
        if (AudioManager.Instance != null && trampPlaceSound != null)
            AudioManager.Instance.PlaySound(trampPlaceSound);

        // Register placement
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterTrampPlaced(selectedType);

        Debug.Log($"Placed {selectedType} at {realRebounder.transform.position}");

        // Keep ghost for next placement
        previewGhost.SetActive(false);
        previewGhost.transform.position = Vector3.zero;
    }

    private void SimulateHeatmapFromAllTramps()
    {
        if (allSockets.Count == 0) return;

        foreach (var socket in allSockets)
            socket.hitProbability = 0f;

        RebounderController[] placedTramps = FindObjectsOfType<RebounderController>();
        foreach (var tramp in placedTramps)
        {
            Vector3 launchDir = trampLaunchVectors.GetValueOrDefault(tramp.type, Vector3.up);
            SimulateRaysFromPoint(tramp.transform.position + Vector3.up * 0.5f, launchDir, simVelocity);
        }

        SocketData[] goalSockets = allSockets.Where(s =>
            s.transform.position.y > -10 && // Sanity check
            Vector3.Distance(s.transform.position, GameManager.Instance.anvilGoalTransform.position) < 5f
        ).ToArray();

        foreach (var gs in goalSockets)
            gs.hitProbability = Mathf.Max(gs.hitProbability, 0.3f);

        foreach (var socket in allSockets)
            socket.UpdateVisual(socket.hitProbability);
    }

    private void SimulateRaysFromPoint(Vector3 startPos, Vector3 baseDir, float velocity)
    {
        float gravity = Physics.gravity.magnitude;
        float timeToGround = (velocity * baseDir.y) / gravity * 2f;

        for (int i = 0; i < simRayCount; i++)
        {
            float angle = (i / (float)simRayCount) * 2f * simAngleSpread - simAngleSpread;
            Vector3 rayDir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir.normalized;
            rayDir = rayDir.normalized * velocity;

            Vector3 currentPos = startPos;
            Vector3 currentVel = rayDir;

            for (float t = 0; t < timeToGround && t < 5f; t += 0.1f)
            {
                currentPos += currentVel * 0.1f;
                currentVel += Physics.gravity * 0.1f;

                if (Physics.Raycast(currentPos, Vector3.down, out RaycastHit hit, 2f,
                    LayerMask.GetMask("Default", "PlacementSocket")))
                {
                    SocketData socket = hit.collider.GetComponent<SocketData>();
                    if (socket != null)
                    {
                        socket.hitProbability += 1f / simRayCount;
                    }
                    break;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (previewGhost != null)
            Destroy(previewGhost);
    }
}