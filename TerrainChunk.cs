using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    public Vector3Int chunkCoord;
    private float surfaceLevel;
    public float[,,] densityGrid;
    
    private VoxelTerrain terrain;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private bool isInitialized = false;
    
    public void Initialize(VoxelTerrain terrain, Vector3Int coord, float surfacelvl)
    {
        this.terrain = terrain;
        this.chunkCoord = coord;
        this.surfaceLevel = surfacelvl;

        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        
        int size = terrain.chunkSize + 1; // +1 for marching cubes
        densityGrid = new float[size, size, size];
        
        GenerateDensity();
        GenerateMesh();
        
        isInitialized = true;
    }
    
    void GenerateDensity()
    {
        for (int x = 0; x <= terrain.chunkSize; x++)
        {
            for (int y = 0; y <= terrain.chunkSize; y++)
            {
                for (int z = 0; z <= terrain.chunkSize; z++)
                {
                    Vector3 worldPos = transform.position + 
                        new Vector3(x, y, z) * terrain.voxelSize;
                    densityGrid[x, y, z] = terrain.GetDensity(worldPos);
                }
            }
        }
    }
    
    public void GenerateMesh()
    {
        if (!isInitialized) return;
        
        MarchingCubes.GenerateMesh(
            densityGrid,
            terrain.voxelSize,
           this.surfaceLevel,
            out Vector3[] vertices,
            out int[] triangles
        );
        
        if (vertices.Length == 0) return;
        
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
    
    public float GetDensity(Vector3Int localCoord)
    {
        if (IsValidCoordinate(localCoord))
        {
            return densityGrid[localCoord.x, localCoord.y, localCoord.z];
        }
        return 0;
    }
    
    public void SetDensity(Vector3Int localCoord, float value)
    {
        if (IsValidCoordinate(localCoord))
        {
            densityGrid[localCoord.x, localCoord.y, localCoord.z] = value;
        }
    }
    
    bool IsValidCoordinate(Vector3Int coord)
    {
        return coord.x >= 0 && coord.x <= terrain.chunkSize &&
               coord.y >= 0 && coord.y <= terrain.chunkSize &&
               coord.z >= 0 && coord.z <= terrain.chunkSize;
    }
}