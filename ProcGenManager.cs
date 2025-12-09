using UnityEngine;
using System.Collections.Generic;

public class ProcGenManager : MonoBehaviour
{
    public static ProcGenManager Instance;

    [Header("Grid Settings")]
    public int gridWidth = 60;
    public int gridHeight = 60;
    public float cellSize = 1f;

    [Header("Terrain")]
    public Terrain terrain;

    [Header("Obstacle Prefabs (Multiple allowed for variety)")]
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
    public GameObject socketPrefab;

    [Header("Game Object Prefabs")]
    public GameObject windliftPrefab;
    public GameObject goalPrefab; // Optional - will create cube if null

    [Header("Smart Socket Placement")]
    [Range(3, 8)] public int socketsPerCluster = 5;
    public float bounceArcHeight = 3f;
    public AnimationCurve clusterSpreadByTier = AnimationCurve.Linear(0, 2.5f, 5, 0.6f);
    public bool useSmartSockets = true;

    [Header("Path Generation")]
    [Range(0f, 90f)] public float maxPathTurnAngle = 45f;
    [Range(1, 10)] public int minStepSize = 3;
    [Range(1, 10)] public int maxStepSize = 7;

    [Header("Debug")]
    public bool showDebugPath = true;

    private List<Vector2Int> guaranteedPath = new List<Vector2Int>();
    private GameObject levelParent;
    [SerializeField, HideInInspector] private List<SocketData> placedSockets = new List<SocketData>();

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;

        levelParent = new GameObject("GeneratedLevel");
        levelParent.tag = "Generated";
    }

    public void Generate(int tier, int level)
    {
        ClearScene();

        if (terrain == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }

        if (terrain.terrainData.size.x != gridWidth * cellSize ||
            terrain.terrainData.size.z != gridHeight * cellSize)
        {
            Debug.LogWarning($"Terrain size mismatch – adjust terrain or grid settings.");
        }

        int pathLength = 8 + tier * 3 + (level == 2 ? 5 : 0);
        Vector2Int start = new Vector2Int(4, gridHeight - 5);
        Vector2Int goal = new Vector2Int(gridWidth - 5, 4);
        guaranteedPath = GeneratePlayablePath(start, goal, pathLength, tier);

        GenerateTerrainHeights(tier);
        PlaceWindlift(start);
        PlaceGoalWithClearance(goal, tier);
        PlaceObstacles(tier);

        if (useSmartSockets)
            PlaceSmartSockets(tier);
        else
            PlaceSockets();

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.OnSocketsGenerated(placedSockets);

        Debug.Log($"Level generated – Tier {tier}, Level {level} | Path: {guaranteedPath.Count} segments | Sockets: {placedSockets.Count}");
    }

    // ====================================================================
    // ENHANCED PATH GENERATION
    // ====================================================================
    private List<Vector2Int> GeneratePlayablePath(Vector2Int start, Vector2Int goal, int desiredLength, int tier)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = start;
        path.Add(current);

        float tierDifficulty = Mathf.Clamp01(tier / 5f);
        float currentMaxTurnAngle = Mathf.Lerp(30f, maxPathTurnAngle, tierDifficulty);

        while (path.Count < desiredLength && Vector2Int.Distance(current, goal) > 5)
        {
            // FIX: Convert Vector2Int to Vector2 for normalized
            Vector2 toGoal = ((Vector2)(goal - current)).normalized;

            // Add some randomness but bias toward goal
            float turnAngle = Random.Range(-currentMaxTurnAngle, currentMaxTurnAngle);
            Vector2 direction = Quaternion.Euler(0, 0, turnAngle) * toGoal;
            direction.Normalize();

            // Step size based on bounce physics
            int stepSize = Random.Range(minStepSize, maxStepSize + 1);
            Vector2Int next = current + new Vector2Int(
                Mathf.RoundToInt(direction.x * stepSize),
                Mathf.RoundToInt(direction.y * stepSize)
            );

            // Keep within bounds with margin for sockets
            next.x = Mathf.Clamp(next.x, 5, gridWidth - 6);
            next.y = Mathf.Clamp(next.y, 5, gridHeight - 6);

            // Avoid getting stuck
            if (Vector2Int.Distance(next, current) < 2)
                continue;

            path.Add(next);
            current = next;

            // Gradually tighten turns as we approach goal
            float distanceToGoal = Vector2Int.Distance(current, goal);
            currentMaxTurnAngle = Mathf.Lerp(15f, maxPathTurnAngle, distanceToGoal / 100f);
        }

        // Ensure we end near goal
        if (Vector2Int.Distance(current, goal) > 8)
        {
            path.Add(goal);
        }

        return path;
    }

    // ====================================================================
    // WINDLIFT PLACEMENT
    // ====================================================================
    private void PlaceWindlift(Vector2Int startGridPos)
    {
        Vector3 worldPos = GridToWorld(startGridPos);
        worldPos.y = terrain.SampleHeight(worldPos) + 0.5f;

        // Orient toward first path segment if possible
        Quaternion rotation = Quaternion.identity;
        if (guaranteedPath.Count > 1)
        {
            Vector3 lookTarget = GridToWorld(guaranteedPath[1]);
            lookTarget.y = worldPos.y;
            Vector3 direction = (lookTarget - worldPos).normalized;
            if (direction.magnitude > 0.1f)
            {
                rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        GameObject windliftObj; // Declare here

        if (windliftPrefab == null)
        {
            Debug.LogWarning("No windlift prefab assigned. Creating placeholder.");
            windliftObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            windliftObj.name = "Windlift";
            windliftObj.transform.position = worldPos; // Use the calculated position
            windliftObj.transform.localScale = new Vector3(2f, 1f, 2f);
            windliftObj.AddComponent<WindliftController>();
        }
        else
        {
            windliftObj = Instantiate(windliftPrefab, worldPos, rotation, levelParent.transform);
        }

        windliftObj.tag = "Generated";
        windliftObj.transform.parent = levelParent.transform;

        // Clear area around windlift
        ClearArea(worldPos, 3f);
    }

    // ====================================================================
    // GOAL PLACEMENT WITH CLEARANCE
    // ====================================================================
    private void PlaceGoalWithClearance(Vector2Int goalGridPos, int tier)
    {
        Vector3 goalPos = GridToWorld(goalGridPos);
        goalPos.y = terrain.SampleHeight(goalPos);

        // Search for best position near goal (flattest area)
        Vector3 bestPos = goalPos;
        float bestSlope = 90f;

        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                Vector3 testPos = goalPos + new Vector3(x * 2f, 0, z * 2f);
                testPos.y = terrain.SampleHeight(testPos);

                float slope = terrain.terrainData.GetSteepness(
                    testPos.x / terrain.terrainData.size.x,
                    testPos.z / terrain.terrainData.size.z
                );

                if (slope < bestSlope && slope < 30f)
                {
                    bestSlope = slope;
                    bestPos = testPos;
                }
            }
        }

        // Create or instantiate goal
        GameObject goalObj;
        if (goalPrefab != null)
        {
            goalObj = Instantiate(goalPrefab, bestPos + Vector3.up * 0.5f, Quaternion.identity, levelParent.transform);
        }
        else
        {
            goalObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goalObj.transform.position = bestPos + Vector3.up * 0.5f;
            goalObj.transform.localScale = new Vector3(3f, 1f, 3f);
            goalObj.GetComponent<Renderer>().material.color = Color.red;
            goalObj.AddComponent<BoxCollider>().isTrigger = true;
        }

        goalObj.tag = "Goal";
        goalObj.name = "Goal";
        goalObj.layer = LayerMask.NameToLayer("Goal");

        // Clear area around goal
        ClearArea(bestPos, 4f);
    }

    // ====================================================================
    // CLEAR AREA UTILITY
    // ====================================================================
    private void ClearArea(Vector3 center, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(center, radius);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Tree") || collider.CompareTag("Rock") || collider.CompareTag("Generated"))
            {
                Destroy(collider.gameObject);
            }
        }
    }

    // ====================================================================
    // OBSTACLE PLACEMENT (UPDATED TO AVOID KEY AREAS)
    // ====================================================================
    private void PlaceObstacles(int tier)
    {
        int treeCount = 60 + tier * 15;
        int rockCount = 30 + tier * 12;

        // Trees
        if (treePrefabs != null && treePrefabs.Length > 0)
        {
            for (int i = 0; i < treeCount; i++)
            {
                Vector2Int gridPos = new Vector2Int(Random.Range(5, gridWidth - 5), Random.Range(5, gridHeight - 5));

                // Don't place in path OR near start/goal
                if (IsInPathOrBuffer(gridPos) ||
                    Vector2Int.Distance(gridPos, guaranteedPath[0]) < 8 ||
                    Vector2Int.Distance(gridPos, guaranteedPath[guaranteedPath.Count - 1]) < 8)
                    continue;

                Vector3 worldPos = GridToWorld(gridPos);
                worldPos.y = terrain.SampleHeight(worldPos);

                // Check slope
                float slope = terrain.terrainData.GetSteepness(
                    worldPos.x / terrain.terrainData.size.x,
                    worldPos.z / terrain.terrainData.size.z
                );
                if (slope > 40f) continue;

                GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.85f, 1.25f);

                GameObject tree = Instantiate(prefab, worldPos, rotation, levelParent.transform);
                tree.transform.localScale = Vector3.one * scale;
                tree.tag = "Tree";
            }
        }

        // Rocks
        if (rockPrefabs != null && rockPrefabs.Length > 0)
        {
            for (int i = 0; i < rockCount; i++)
            {
                Vector2Int gridPos = new Vector2Int(Random.Range(3, gridWidth - 3), Random.Range(3, gridHeight - 3));

                if (IsInPathOrBuffer(gridPos) ||
                    Vector2Int.Distance(gridPos, guaranteedPath[0]) < 6 ||
                    Vector2Int.Distance(gridPos, guaranteedPath[guaranteedPath.Count - 1]) < 6)
                    continue;

                Vector3 worldPos = GridToWorld(gridPos);
                worldPos.y = terrain.SampleHeight(worldPos);

                float slope = terrain.terrainData.GetSteepness(
                    worldPos.x / terrain.terrainData.size.x,
                    worldPos.z / terrain.terrainData.size.z
                );
                if (slope > 45f) continue;

                GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.8f, 1.3f);

                GameObject rock = Instantiate(prefab, worldPos, rotation, levelParent.transform);
                rock.transform.localScale = Vector3.one * scale;
                rock.tag = "Rock";
            }
        }
    }

    // ====================================================================
    // SMART SOCKET PLACEMENT WITH VALIDATION
    // ====================================================================
    private void PlaceSmartSockets(int tier)
    {
        placedSockets.Clear();

        int pathSegments = guaranteedPath.Count - 1;
        for (int i = 0; i < pathSegments; i++)
        {
            Vector3 from = GridToWorld(guaranteedPath[i]);
            Vector3 to = GridToWorld(guaranteedPath[i + 1]);
            from.y = terrain.SampleHeight(from) + 0.1f;
            to.y = terrain.SampleHeight(to) + 0.1f;

            Vector3 apex = Vector3.Lerp(from, to, 0.5f) + Vector3.up * bounceArcHeight;
            PlaceSocketCluster(apex, tier, i * 2, "Apex");

            Vector3 landing = to - (to - from).normalized * cellSize * 0.7f;
            landing.y = terrain.SampleHeight(landing) + 0.1f;
            PlaceSocketCluster(landing, tier, i * 2 + 1, "Land");
        }

        Vector3 finalPos = GridToWorld(guaranteedPath[guaranteedPath.Count - 1]);
        finalPos.y = terrain.SampleHeight(finalPos) + 0.1f;
        PlaceSocketCluster(finalPos, tier, 999, "GoalZone");
    }

    private void PlaceSocketCluster(Vector3 center, int tier, int clusterId, string debugName)
    {
        float spreadRadius = clusterSpreadByTier.Evaluate(tier);
        int placed = 0;
        int attempts = 0;
        int maxAttempts = socketsPerCluster * 15;

        while (placed < socketsPerCluster && attempts < maxAttempts)
        {
            attempts++;

            Vector2 offset = Random.insideUnitCircle * spreadRadius;
            Vector3 pos = center + new Vector3(offset.x, 0, offset.y);
            pos.y = terrain.SampleHeight(pos) + 0.1f;

            Vector2Int gridPos = WorldToGrid(pos);
            if (gridPos.x < 3 || gridPos.x >= gridWidth - 3 ||
                gridPos.y < 3 || gridPos.y >= gridHeight - 3)
                continue;

            // Validate position
            if (!IsValidSocketPosition(pos))
                continue;

            // Check overlap with existing sockets
            bool overlap = false;
            foreach (var s in placedSockets)
            {
                if (Vector3.Distance(s.transform.position, pos) < cellSize * 0.8f)
                {
                    overlap = true;
                    break;
                }
            }
            if (overlap) continue;

            GameObject socketObj = Instantiate(socketPrefab, pos, Quaternion.identity, levelParent.transform);
            socketObj.tag = "Socket";
            socketObj.layer = LayerMask.NameToLayer("PlacementSocket");

            // Align to terrain normal
            Vector3 normal = terrain.terrainData.GetInterpolatedNormal(
                pos.x / terrain.terrainData.size.x,
                pos.z / terrain.terrainData.size.z
            );
            socketObj.transform.up = normal;

            SocketData data = socketObj.GetComponent<SocketData>() ?? socketObj.AddComponent<SocketData>();
            data.pathIndex = clusterId / 2;
            data.clusterId = clusterId;
            data.debugName = debugName;

            placedSockets.Add(data);
            placed++;
        }

        if (placed < socketsPerCluster)
        {
            Debug.LogWarning($"Socket cluster {debugName}: placed {placed}/{socketsPerCluster} sockets");
        }
    }

    private bool IsValidSocketPosition(Vector3 pos)
    {
        // Check slope
        float slope = terrain.terrainData.GetSteepness(
            pos.x / terrain.terrainData.size.x,
            pos.z / terrain.terrainData.size.z
        );
        if (slope > 25f) return false;

        // Check if in path buffer
        Vector2Int gridPos = WorldToGrid(pos);
        if (IsInPathOrBuffer(gridPos)) return false;

        // Check clearance
        Collider[] colliders = Physics.OverlapSphere(pos, 1.5f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Tree") || collider.CompareTag("Rock") ||
                collider.CompareTag("Goal") || collider.CompareTag("Windlift"))
                return false;
        }

        return true;
    }

    // ====================================================================
    // UNIFORM SOCKET PLACEMENT (FALLBACK)
    // ====================================================================
    private void PlaceSockets()
    {
        placedSockets.Clear();

        for (int x = 0; x < gridWidth; x += 2)
        {
            for (int z = 0; z < gridHeight; z += 2)
            {
                Vector3 worldPos = GridToWorld(new Vector2Int(x, z));
                worldPos.y = terrain.SampleHeight(worldPos) + 0.1f;

                float slope = terrain.terrainData.GetSteepness(
                    worldPos.x / terrain.terrainData.size.x,
                    worldPos.z / terrain.terrainData.size.z
                );

                Vector2Int gridPos = new Vector2Int(x, z);
                if (slope < 25f && !IsInPathOrBuffer(gridPos))
                {
                    GameObject socket = Instantiate(socketPrefab, worldPos, Quaternion.identity, levelParent.transform);
                    socket.tag = "Socket";
                    socket.layer = LayerMask.NameToLayer("PlacementSocket");

                    SocketData data = socket.GetComponent<SocketData>() ?? socket.AddComponent<SocketData>();
                    placedSockets.Add(data);
                }
            }
        }
    }

    // ====================================================================
    // UTILITY METHODS (UNCHANGED)
    // ====================================================================
    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x / cellSize), Mathf.FloorToInt(worldPos.z / cellSize));
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize);
    }

    private bool IsInPathOrBuffer(Vector2Int cell)
    {
        foreach (var p in guaranteedPath)
            if (Vector2Int.Distance(cell, p) <= 2.5f) return true; // Increased buffer
        return false;
    }

    private void GenerateTerrainHeights(int tier)
    {
        int resolution = terrain.terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        float terrainWidth = terrain.terrainData.size.x;
        float terrainLength = terrain.terrainData.size.z;

        // ADJUSTED FOR SMOOTHNESS: Larger scale = broader, smoother hills
        float baseScale = 0.01f;          // Large, smooth hills (was 0.05f)
        float detailScale = 0.03f;        // Medium details
        float fineScale = 0.1f;           // Small rock-like details

        // Tier-based terrain: higher tiers = slightly rougher
        float heightMultiplier = 0.08f + (tier * 0.01f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = (x / (float)resolution) * terrainWidth;
                float worldZ = (y / (float)resolution) * terrainLength;

                // 1. Large smooth hills (base terrain)
                float sampleX = worldX * baseScale;
                float sampleZ = worldZ * baseScale;
                float noise = Mathf.PerlinNoise(sampleX, sampleZ);

                // 2. Add medium details (hillsides)
                noise += Mathf.PerlinNoise(worldX * detailScale, worldZ * detailScale) * 0.25f;

                // 3. Add small details (rocks, bumps) - LESS weight for smoothness
                noise += Mathf.PerlinNoise(worldX * fineScale, worldZ * fineScale) * 0.1f;

                // Normalize (Perlin returns ~0-1, but sum might be >1)
                noise = noise / 1.35f;

                // Apply height multiplier
                heights[y, x] = noise * heightMultiplier;

                Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(worldX / cellSize), Mathf.FloorToInt(worldZ / cellSize));
                if (IsInPathOrBuffer(gridPos))
                {
                    // SMOOTH PATH FLATTENING: Gradual transition instead of hard cutoff
                    // Find distance to nearest path point
                    float minDistance = float.MaxValue;
                    foreach (var p in guaranteedPath)
                    {
                        Vector3 pathPos = GridToWorld(p);
                        float dist = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(pathPos.x, pathPos.z));
                        if (dist < minDistance) minDistance = dist;
                    }

                    // Within 1.5 units: completely flat (path)
                    // 1.5 to 4 units: smooth transition
                    // Beyond 4 units: normal terrain
                    if (minDistance < 1.5f)
                    {
                        heights[y, x] = 0.01f; // Flat path
                    }
                    else if (minDistance < 4f)
                    {
                        float blend = (minDistance - 1.5f) / 2.5f; // 0 to 1
                        heights[y, x] = Mathf.Lerp(0.01f, heights[y, x], blend);
                    }
                }
            }
        }

        // Apply one smoothing pass to remove sharp peaks/holes
        heights = SmoothTerrain(heights, 1);

        terrain.terrainData.SetHeights(0, 0, heights);
    }

    // NEW: Smoothing function to remove sharp features
    private float[,] SmoothTerrain(float[,] heights, int passes)
    {
        int width = heights.GetLength(1);
        int height = heights.GetLength(0);

        for (int p = 0; p < passes; p++)
        {
            float[,] smoothed = (float[,])heights.Clone();

            // Skip edges to avoid index issues
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    // 3x3 box blur
                    float sum = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            sum += heights[y + dy, x + dx];
                        }
                    }
                    smoothed[y, x] = sum / 9f;
                }
            }

            heights = smoothed;
        }

        return heights;
    }

    private void ClearScene()
    {
        if (levelParent != null)
        {
            foreach (Transform child in levelParent.transform)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        GameObject[] generated = GameObject.FindGameObjectsWithTag("Generated");
        foreach (GameObject obj in generated)
        {
            if (obj != levelParent && obj != terrain.gameObject && obj != gameObject)
                Destroy(obj);
        }

        GameObject[] sockets = GameObject.FindGameObjectsWithTag("Socket");
        foreach (GameObject obj in sockets)
        {
            Destroy(obj);
        }

        placedSockets.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPath || guaranteedPath == null || guaranteedPath.Count < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < guaranteedPath.Count - 1; i++)
        {
            Vector3 start = GridToWorld(guaranteedPath[i]);
            Vector3 end = GridToWorld(guaranteedPath[i + 1]);
            start.y = terrain.SampleHeight(start) + 1f;
            end.y = terrain.SampleHeight(end) + 1f;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(start, 0.5f);
        }

        // Draw start and goal areas
        if (guaranteedPath.Count > 0)
        {
            Gizmos.color = Color.blue;
            Vector3 startPos = GridToWorld(guaranteedPath[0]);
            startPos.y = terrain.SampleHeight(startPos);
            Gizmos.DrawWireSphere(startPos, 3f);

            Gizmos.color = Color.red;
            Vector3 goalPos = GridToWorld(guaranteedPath[guaranteedPath.Count - 1]);
            goalPos.y = terrain.SampleHeight(goalPos);
            Gizmos.DrawWireSphere(goalPos, 4f);
        }
    }
}