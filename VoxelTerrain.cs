using System.Collections.Generic;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int worldSize = 4; // Number of chunks in each direction
    public int chunkSize = 16; // Voxels per chunk
    public float voxelSize = 0.5f;
    public float surfaceLevel = 0.5f;

    [Header("Material")]
    public Material chunkMaterial;

    [Header("Generation")]
    public float noiseScale = 0.1f;
    public float terrainHeight = 10f;

    private Dictionary<Vector3Int, TerrainChunk> chunks = new Dictionary<Vector3Int, TerrainChunk>();
    private Queue<Vector3Int> chunksToUpdate = new Queue<Vector3Int>();

    void Start()
    {
        GenerateWorld();
    }

    void Update()
    {
        // Process chunk updates
        if (chunksToUpdate.Count > 0)
        {
            Vector3Int chunkCoord = chunksToUpdate.Dequeue();
            if (chunks.ContainsKey(chunkCoord))
            {
                chunks[chunkCoord].GenerateMesh();
            }
        }
    }

    void GenerateWorld()
    {
        for (int x = -worldSize; x <= worldSize; x++)
        {
            for (int y = -worldSize; y <= worldSize; y++)
            {
                for (int z = -worldSize; z <= worldSize; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    CreateChunk(coord);
                }
            }
        }
    }

    void CreateChunk(Vector3Int coord)
    {
        GameObject chunkGO = new GameObject($"Chunk_{coord.x}_{coord.y}_{coord.z}");
        chunkGO.transform.SetParent(transform);
        chunkGO.transform.localPosition = new Vector3(
            coord.x * chunkSize * voxelSize,
            coord.y * chunkSize * voxelSize,
            coord.z * chunkSize * voxelSize
        );

        TerrainChunk chunk = chunkGO.AddComponent<TerrainChunk>();
        chunk.Initialize(this, coord, surfaceLevel);

        // Apply material if assigned
        if (chunkMaterial != null)
        {
            chunkGO.GetComponent<MeshRenderer>().material = chunkMaterial;
        }

        chunks.Add(coord, chunk);
    }

    public float GetDensity(Vector3 worldPos)
    {
        Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
        Vector3Int localCoord = WorldToLocalVoxelCoord(worldPos);

        if (chunks.ContainsKey(chunkCoord))
        {
            return chunks[chunkCoord].GetDensity(localCoord);
        }

        // Generate density for unloaded chunks
        return GenerateDensity(worldPos);
    }

    public void SetDensity(Vector3 worldPos, float density)
    {
        Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
        Vector3Int localCoord = WorldToLocalVoxelCoord(worldPos);

        if (chunks.ContainsKey(chunkCoord))
        {
            chunks[chunkCoord].SetDensity(localCoord, density);
            MarkChunkForUpdate(chunkCoord);

            // Also mark neighboring chunks if we're at the edge
            if (localCoord.x == 0) MarkChunkForUpdate(chunkCoord + Vector3Int.left);
            if (localCoord.x == chunkSize - 1) MarkChunkForUpdate(chunkCoord + Vector3Int.right);
            if (localCoord.y == 0) MarkChunkForUpdate(chunkCoord + Vector3Int.down);
            if (localCoord.y == chunkSize - 1) MarkChunkForUpdate(chunkCoord + Vector3Int.up);
            if (localCoord.z == 0) MarkChunkForUpdate(chunkCoord + Vector3Int.back);
            if (localCoord.z == chunkSize - 1) MarkChunkForUpdate(chunkCoord + Vector3Int.forward);
        }
    }

    float GenerateDensity(Vector3 worldPos)
    {
        // Simple terrain generation - sphere world
        float radius = (worldSize * chunkSize * voxelSize) * 0.5f;
        Vector3 center = Vector3.zero;
        float distanceFromCenter = Vector3.Distance(worldPos, center);

        // Add some noise for variation
        float noise = Mathf.PerlinNoise(
            worldPos.x * noiseScale,
            worldPos.z * noiseScale
        ) * terrainHeight;

        return (radius + noise) - distanceFromCenter;
    }

    // FIX: Changed from private to public
    public Vector3Int WorldToChunkCoord(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / (chunkSize * voxelSize)),
            Mathf.FloorToInt(worldPos.y / (chunkSize * voxelSize)),
            Mathf.FloorToInt(worldPos.z / (chunkSize * voxelSize))
        );
    }

    // FIX: Changed from private to public
    public Vector3Int WorldToLocalVoxelCoord(Vector3 worldPos)
    {
        Vector3 chunkWorldPos = new Vector3(
            WorldToChunkCoord(worldPos).x * chunkSize * voxelSize,
            WorldToChunkCoord(worldPos).y * chunkSize * voxelSize,
            WorldToChunkCoord(worldPos).z * chunkSize * voxelSize
        );

        Vector3 localPos = worldPos - chunkWorldPos;
        return new Vector3Int(
            Mathf.FloorToInt(localPos.x / voxelSize),
            Mathf.FloorToInt(localPos.y / voxelSize),
            Mathf.FloorToInt(localPos.z / voxelSize)
        );
    }

    void MarkChunkForUpdate(Vector3Int coord)
    {
        if (!chunksToUpdate.Contains(coord))
        {
            chunksToUpdate.Enqueue(coord);
        }
    }

    // FIX: Added this method for converting grid to world
    public Vector3 GridToWorld(Vector3Int gridCoord)
    {
        return new Vector3(
            gridCoord.x * voxelSize,
            gridCoord.y * voxelSize,
            gridCoord.z * voxelSize
        );
    }

    // FIX: Added helper method to get chunk world position
    public Vector3 ChunkCoordToWorldPosition(Vector3Int chunkCoord)
    {
        return new Vector3(
            chunkCoord.x * chunkSize * voxelSize,
            chunkCoord.y * chunkSize * voxelSize,
            chunkCoord.z * chunkSize * voxelSize
        );
    }
}