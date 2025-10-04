using UnityEngine;

public class EnvironmentDetector : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject playerObject;

    void Start()
    {
        if (playerObject == null)
        {
            playerObject = PlayerManager.Instance?.gameObject;
        }
    }

    [Header("Detection Settings")]
    [SerializeField] private float maxDetectionDistance = 1000f;
    [SerializeField] private float minDetectionDistance = 10f;
    [SerializeField] private float fieldOfViewAngle = 90f; // ADD THIS - NPC's field of view
    [SerializeField] private AnimationCurve detectionDifficultyCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Line of Sight Settings")]
    [SerializeField] private LayerMask obstacleLayers = 1;
    [SerializeField] private int scanRays = 5; // ADD THIS - multiple rays for better detection
    [SerializeField] private float scanSpread = 0.5f; // ADD THIS - cone spread for rays

    [Header("Environment Visibility Modifiers")]
    [SerializeField] private float clearingVisibility = 1.0f;
    [SerializeField] private float forestVisibility = 0.3f;
    [SerializeField] private float thickBrushVisibility = 0.1f;
    [SerializeField] private float waterVisibility = 0.7f;
    [SerializeField] private float rockyVisibility = 0.8f;
    [SerializeField] private float villageVisibility = 0.9f;

    public EnvironmentType GetCurrentEnvironment()
    {
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 10f))
        {
            return GetEnvironmentFromTag(hit.collider.tag);
        }
        return EnvironmentType.Forest;
    }

    public EnvironmentType GetEnvironmentAtPosition(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 10f))
        {
            return GetEnvironmentFromTag(hit.collider.tag);
        }
        return EnvironmentType.Forest;
    }

    // FIXED METHOD - Better line of sight with multiple rays
    public bool HasLineOfSightToPlayer()
    {
        if (playerObject == null)
        {
            Debug.LogWarning("Player object not assigned in EnvironmentDetector!");
            return false;
        }

        // First, check if player is in field of view
        if (!IsInFieldOfView(playerObject.transform))
            return false;

        Vector3 toPlayer = playerObject.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        // Single center ray (most accurate)
        if (CheckSingleRay(transform.position, toPlayer.normalized, distance))
            return true;

        // Multiple ray scan for better coverage
        if (scanRays > 1)
        {
            for (int i = 0; i < scanRays; i++)
            {
                Vector3 rayDirection = GetRayDirectionWithSpread(toPlayer.normalized, i);
                if (CheckSingleRay(transform.position, rayDirection, distance))
                    return true;
            }
        }

        return false;
    }

    // NEW METHOD - Check if target is within field of view
    private bool IsInFieldOfView(Transform target)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= fieldOfViewAngle / 2f;
    }

    // NEW METHOD - Check a single ray for line of sight
    private bool CheckSingleRay(Vector3 origin, Vector3 direction, float maxDistance)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, maxDistance, obstacleLayers))
        {
            // Check if we hit the player (or a child of the player)
            return hit.collider.gameObject == playerObject ||
                   hit.collider.transform.IsChildOf(playerObject.transform);
        }
        return false; // No hit means we didn't reach the player
    }

    // NEW METHOD - Calculate ray direction with spread
    private Vector3 GetRayDirectionWithSpread(Vector3 baseDirection, int rayIndex)
    {
        if (scanRays <= 1) return baseDirection;

        float spreadFactor = (float)rayIndex / (scanRays - 1) - 0.5f; // -0.5 to 0.5
        Vector3 spread = transform.right * (spreadFactor * scanSpread);

        return (baseDirection + spread).normalized;
    }

    public bool CanDetectPlayerAtDistance(Vector3 playerPosition, EnvironmentType playerEnvironment)
    {
        if (playerObject == null) return false;

        float distance = Vector3.Distance(transform.position, playerPosition);

        // Cannot detect beyond maximum range
        if (distance > maxDetectionDistance)
            return false;

        // Always detectable at close range (<=50m)
        if (distance <= 50f)
        {
            Debug.Log($"Player detected deterministically at distance {distance}");
            return true;
        }

        // Check line of sight
        if (!HasLineOfSightToPlayer())
            return false;

        // Calculate detection probability
        float normalizedDistance = Mathf.InverseLerp(minDetectionDistance, maxDetectionDistance, distance);
        float distanceDetectionChance = detectionDifficultyCurve.Evaluate(normalizedDistance);
        float environmentModifier = GetEnvironmentVisibility(playerEnvironment);
        float finalDetectionChance = distanceDetectionChance * environmentModifier;

        // Roll for detection
        bool detected = Random.Range(0f, 1f) <= finalDetectionChance;
        Debug.Log($"Player detection roll: distance {distance}, chance {finalDetectionChance}, detected {detected}");
        return detected;
    }

    public float GetPlayerDetectionProbability(Vector3 playerPosition, EnvironmentType playerEnvironment)
    {
        if (playerObject == null) return 0f;

        float distance = Vector3.Distance(transform.position, playerPosition);

        if (distance > maxDetectionDistance)
            return 0f;

        if (distance <= minDetectionDistance)
            return 1f;

        // Check line of sight
        if (!HasLineOfSightToPlayer())
            return 0f;

        float normalizedDistance = Mathf.InverseLerp(minDetectionDistance, maxDetectionDistance, distance);
        float distanceDetectionChance = detectionDifficultyCurve.Evaluate(normalizedDistance);
        float environmentModifier = GetEnvironmentVisibility(playerEnvironment);

        return distanceDetectionChance * environmentModifier;
    }

    // IMPROVED METHOD - Simple player visibility check
    public bool IsPlayerVisible()
    {
        if (playerObject == null)
        {
            Debug.LogWarning("Player object not assigned in EnvironmentDetector!");
            return false;
        }

        // Close range always detect
        if (Vector3.Distance(transform.position, playerObject.transform.position) < 10f) return true;

        // Get player's current environment
        EnvironmentType playerEnv = GetEnvironmentAtPosition(playerObject.transform.position);

        return CanDetectPlayerAtDistance(playerObject.transform.position, playerEnv);
    }

    // NEW METHOD - For debugging: Visualize detection area
    private void OnDrawGizmosSelected()
    {
        if (playerObject == null) return;

        // Draw field of view
        Gizmos.color = Color.yellow;
        Vector3 leftBound = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * maxDetectionDistance;
        Vector3 rightBound = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * maxDetectionDistance;
        Gizmos.DrawRay(transform.position, leftBound);
        Gizmos.DrawRay(transform.position, rightBound);

        // Draw line to player
        Gizmos.color = HasLineOfSightToPlayer() ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, playerObject.transform.position);
    }

    private float GetEnvironmentVisibility(EnvironmentType environment)
    {
        switch (environment)
        {
            case EnvironmentType.Clearing: return clearingVisibility;
            case EnvironmentType.Forest: return forestVisibility;
            case EnvironmentType.ThickBrush: return thickBrushVisibility;
            case EnvironmentType.Water: return waterVisibility;
            case EnvironmentType.Rocky: return rockyVisibility;
            case EnvironmentType.Village: return villageVisibility;
            default: return clearingVisibility;
        }
    }

    private EnvironmentType GetEnvironmentFromTag(string tag)
    {
        switch (tag)
        {
            case "Water": return EnvironmentType.Water;
            case "Rocky": return EnvironmentType.Rocky;
            case "ThickBrush": return EnvironmentType.ThickBrush;
            case "Village": return EnvironmentType.Village;
            case "Clearing": return EnvironmentType.Clearing;
            default: return EnvironmentType.Forest;
        }
    }
}