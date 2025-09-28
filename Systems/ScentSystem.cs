using UnityEngine;
using System.Collections.Generic;

namespace ProjectHounded.AI.Core
{
    public class ScentSystem : MonoBehaviour
    {
        [Header("Scent Configuration")]
        public float globalDecayRate = 0.1f; // Scent freshness decay per second
        public float diffusionRate = 0.5f; // How quickly scent spreads to adjacent areas
        public float maxScentLifetime = 60f; // Maximum time scent persists (seconds)
        public float gridCellSize = 2.0f; // Meters per grid cell

        [Header("Debug Visualization")]
        public bool visualizeScentClouds = false;
        public Gradient scentVisualizationGradient;

        private Dictionary<Vector3Int, ScentCell> scentGrid = new Dictionary<Vector3Int, ScentCell>();

        void Update()
        {
            UpdateScentDecay();
            UpdateScentDiffusion();
            RemoveExpiredScent();
        }

        public ScentData GetMostRelevantScent()
        {
            ScentData mostRelevant = ScentData.Invalid;
            float highestPriority = 0f;

            foreach (var cell in scentGrid.Values)
            {
                if (cell.IsActive)
                {
                    // Priority formula: freshness * strength
                    float priority = cell.Freshness * cell.Strength;
                    if (priority > highestPriority)
                    {
                        highestPriority = priority;
                        mostRelevant = new ScentData
                        {
                            WorldPosition = cell.WorldPosition,
                            Freshness = cell.Freshness,
                            Strength = cell.Strength
                        };
                    }
                }
            }

            return mostRelevant;
        }

        public ScentData GetScentDataAt(Vector3 worldPosition)
        {
            Vector3Int gridKey = WorldToGridPosition(worldPosition);
            if (scentGrid.TryGetValue(gridKey, out ScentCell cell) && cell.IsActive)
            {
                return new ScentData
                {
                    WorldPosition = cell.WorldPosition,
                    Freshness = cell.Freshness,
                    Strength = cell.Strength
                };
            }
            return ScentData.Invalid;
        }

        public void EmitScent(Vector3 position, float strength, string sourceType)
        {
            Vector3Int gridKey = WorldToGridPosition(position);

            if (!scentGrid.ContainsKey(gridKey))
            {
                scentGrid[gridKey] = new ScentCell(gridKey, gridCellSize);
            }

            // Different sources might have different base strengths
            float baseStrength = strength;
            if (sourceType == "Player") baseStrength *= 1.5f;
            if (sourceType == "Decoy") baseStrength *= 0.7f;

            scentGrid[gridKey].AddScent(baseStrength);

            Debug.Log($"Scent emitted: {sourceType} at {position} (strength: {baseStrength})");
        }

        private void UpdateScentDecay()
        {
            foreach (var cell in scentGrid.Values)
            {
                cell.UpdateDecay(globalDecayRate * Time.deltaTime);
            }
        }

        private void UpdateScentDiffusion()
        {
            // Simple diffusion to adjacent cells
            var diffusionChanges = new Dictionary<Vector3Int, float>();

            foreach (var kvp in scentGrid)
            {
                if (kvp.Value.Strength > 0.1f) // Only diffuse from significant sources
                {
                    Vector3Int[] neighbors = GetNeighborCells(kvp.Key);
                    foreach (var neighbor in neighbors)
                    {
                        float diffusionAmount = kvp.Value.Strength * diffusionRate * Time.deltaTime;
                        if (!diffusionChanges.ContainsKey(neighbor))
                            diffusionChanges[neighbor] = 0f;
                        diffusionChanges[neighbor] += diffusionAmount;
                        kvp.Value.Strength -= diffusionAmount * 0.5f; // Conservation of scent mass
                    }
                }
            }

            // Apply diffusion changes
            foreach (var change in diffusionChanges)
            {
                if (!scentGrid.ContainsKey(change.Key))
                    scentGrid[change.Key] = new ScentCell(change.Key, gridCellSize);
                scentGrid[change.Key].Strength += change.Value;
            }
        }

        private void RemoveExpiredScent()
        {
            var expiredCells = new List<Vector3Int>();
            foreach (var kvp in scentGrid)
            {
                if (kvp.Value.Freshness <= 0.01f)
                    expiredCells.Add(kvp.Key);
            }

            foreach (var key in expiredCells)
            {
                scentGrid.Remove(key);
            }
        }

        private Vector3Int WorldToGridPosition(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x / gridCellSize),
                Mathf.RoundToInt(worldPos.y / gridCellSize),
                Mathf.RoundToInt(worldPos.z / gridCellSize)
            );
        }

        private Vector3Int[] GetNeighborCells(Vector3Int center)
        {
            return new Vector3Int[]
            {
                center + new Vector3Int(1, 0, 0),
                center + new Vector3Int(-1, 0, 0),
                center + new Vector3Int(0, 0, 1),
                center + new Vector3Int(0, 0, -1)
            }; 
        }

        // Debug visualization
        void OnDrawGizmos()
        {
            if (!visualizeScentClouds) return;

            foreach (var cell in scentGrid.Values)
            {
                if (cell.IsActive)
                {
                    float intensity = cell.Freshness * cell.Strength;
                    Gizmos.color = scentVisualizationGradient.Evaluate(intensity);
                    Gizmos.DrawCube(cell.WorldPosition, Vector3.one * gridCellSize * 0.8f);
                }
            }
        }

        [System.Serializable]
        public class ScentCell
        {
            public Vector3 WorldPosition { get; private set; }
            public float Freshness { get; set; } = 1.0f;
            public float Strength { get; set; } = 0.0f;
            public bool IsActive
            {
                get { return Freshness > 0.01f && Strength > 0.01f; }
            }

            public ScentCell(Vector3Int gridPos, float cellSize)
            {
                WorldPosition = new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, gridPos.z * cellSize);
            }

            public void AddScent(float additionalStrength)
            {
                Strength = Mathf.Clamp01(Strength + additionalStrength);
                Freshness = 1.0f; // Reset freshness when new scent is added
            }

            public void UpdateDecay(float decayAmount)
            {
                Freshness = Mathf.Clamp01(Freshness - decayAmount);
                // Strength decays along with freshness
                Strength *= Freshness;
            }
        }

        [System.Serializable]
        public struct ScentData
        {
            public Vector3 WorldPosition;
            public float Freshness;
            public float Strength;

            public static ScentData Invalid
            {
                get
                {
                    ScentData result;
                    result.WorldPosition = Vector3.zero;
                    result.Freshness = 0f;
                    result.Strength = 0f;
                    return result;
                }
            }

            public bool IsValid
            {
                get { return Freshness > 0.01f && Strength > 0.01f; }
            }
        }
    }
}