using UnityEngine;

public class SocketData : MonoBehaviour
{
    [HideInInspector] public int pathIndex = -1;      // Which path segment it's on
    [HideInInspector] public int clusterId = -1;      // Cluster group ID
    [HideInInspector] public string debugName = "";   // Editor label
    [HideInInspector] public float predictedHeight;   // For sim matching

    [Header("Runtime Heatmap")]
    [Range(0f, 1f)] public float hitProbability = 0f;  // 0-1 from sims
    public ParticleSystem highlightRing;  // Assigned in ProcGen or prefab

    private void Awake()
    {
        // Auto-find ring if child
        if (highlightRing == null)
            highlightRing = GetComponentInChildren<ParticleSystem>();

        // Default cold blue
        UpdateVisual(0f);
    }

    /// <summary>
    /// Update glow based on sim hit density. Called by PlacementManager.
    /// Hot = fiery orange/red, Cold = dim blue.
    /// </summary>
    public void UpdateVisual(float probability)
    {
        hitProbability = probability;
        if (highlightRing != null)
        {
            var main = highlightRing.main;
            main.startColor = Color.Lerp(
                new Color(0.2f, 0.2f, 1f, 0.3f),  // Cold blue
                new Color(1f, 0.5f, 0f, 0.8f),    // Hot orange
                probability
            );
            main.startSize = 0.5f + probability * 1.5f;  // Grow with heat
        }
    }

    private void OnDrawGizmos()
    {
        // Editor: Label + probability sphere
        if (!string.IsNullOrEmpty(debugName))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, debugName);
        }
        Gizmos.color = Color.Lerp(Color.blue, Color.red, hitProbability);
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}