using UnityEngine;
using System.Collections.Generic;

public static class MarchingCubes
{
    // Edge table (1D array)
    private static int[] edgeTable = MarchingCubesTable.edgeTable;  
    private static int[,] triTable = MarchingCubesTable.triTable;

    // Edge vertices offsets for interpolation
    private static readonly Vector3[] edgeOffsets = new Vector3[12]
    {
        new Vector3(0.5f, 0, 0),    // 0: between vertices 0 and 1
        new Vector3(1, 0, 0.5f),    // 1: between vertices 1 and 2
        new Vector3(0.5f, 0, 1),    // 2: between vertices 2 and 3
        new Vector3(0, 0, 0.5f),    // 3: between vertices 3 and 0
        new Vector3(0.5f, 1, 0),    // 4: between vertices 4 and 5
        new Vector3(1, 1, 0.5f),    // 5: between vertices 5 and 6
        new Vector3(0.5f, 1, 1),    // 6: between vertices 6 and 7
        new Vector3(0, 1, 0.5f),    // 7: between vertices 7 and 4
        new Vector3(0, 0.5f, 0),    // 8: between vertices 0 and 4
        new Vector3(1, 0.5f, 0),    // 9: between vertices 1 and 5
        new Vector3(1, 0.5f, 1),    // 10: between vertices 2 and 6
        new Vector3(0, 0.5f, 1)     // 11: between vertices 3 and 7
    };

    // Which vertices each edge connects
    private static readonly int[,] edgeVertices = new int[12, 2]
    {
        {0, 1}, {1, 2}, {2, 3}, {3, 0}, // Bottom edges
        {4, 5}, {5, 6}, {6, 7}, {7, 4}, // Top edges
        {0, 4}, {1, 5}, {2, 6}, {3, 7}  // Vertical edges
    };

    public static void GenerateMesh(
        float[,,] densityGrid,
        float voxelSize,
        float surfaceLevel,
        out Vector3[] vertices,
        out int[] triangles)
    {
        List<Vector3> vertList = new List<Vector3>();
        List<int> triList = new List<int>();

        int width = densityGrid.GetLength(0) - 1;
        int height = densityGrid.GetLength(1) - 1;
        int depth = densityGrid.GetLength(2) - 1;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    // Get densities at all 8 corners of the cube
                    float[] cubeDensities = new float[8];
                    cubeDensities[0] = densityGrid[x, y, z];
                    cubeDensities[1] = densityGrid[x + 1, y, z];
                    cubeDensities[2] = densityGrid[x + 1, y, z + 1];
                    cubeDensities[3] = densityGrid[x, y, z + 1];
                    cubeDensities[4] = densityGrid[x, y + 1, z];
                    cubeDensities[5] = densityGrid[x + 1, y + 1, z];
                    cubeDensities[6] = densityGrid[x + 1, y + 1, z + 1];
                    cubeDensities[7] = densityGrid[x, y + 1, z + 1];

                    // Determine cube configuration index (0-255)
                    int configIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cubeDensities[i] > surfaceLevel)
                            configIndex |= 1 << i;
                    }

                    // If completely inside or outside, skip
                    if (configIndex == 0 || configIndex == 255)
                        continue;

                    // Get edge mask from edge table
                    int edgeMask = edgeTable[configIndex];

                    // Calculate vertex positions on each edge
                    Vector3[] edgePositions = new Vector3[12];

                    for (int edge = 0; edge < 12; edge++)
                    {
                        if ((edgeMask & (1 << edge)) != 0)
                        {
                            int v1 = edgeVertices[edge, 0];
                            int v2 = edgeVertices[edge, 1];

                            float d1 = cubeDensities[v1];
                            float d2 = cubeDensities[v2];

                            // Linear interpolation
                            float t = (surfaceLevel - d1) / (d2 - d1);
                            t = Mathf.Clamp01(t);

                            // Calculate position in local grid coordinates
                            Vector3 p1 = GetVertexPosition(v1);
                            Vector3 p2 = GetVertexPosition(v2);

                            Vector3 localPos = Vector3.Lerp(p1, p2, t);

                            // Convert to world position
                            edgePositions[edge] = new Vector3(
                                (x + localPos.x) * voxelSize,
                                (y + localPos.y) * voxelSize,
                                (z + localPos.z) * voxelSize
                            );
                        }
                    }

                    // Add triangles for this configuration
                    for (int i = 0; i < 16; i += 3)
                    {
                        int edge1 = triTable[configIndex, i];
                        int edge2 = triTable[configIndex, i + 1];
                        int edge3 = triTable[configIndex, i + 2];

                        if (edge1 == -1 || edge2 == -1 || edge3 == -1)
                            break;

                        int v1Index = vertList.Count;
                        vertList.Add(edgePositions[edge1]);
                        vertList.Add(edgePositions[edge2]);
                        vertList.Add(edgePositions[edge3]);

                        triList.Add(v1Index);
                        triList.Add(v1Index + 1);
                        triList.Add(v1Index + 2);
                    }
                }
            }
        }

        vertices = vertList.ToArray();
        triangles = triList.ToArray();
    }

    private static Vector3 GetVertexPosition(int vertexIndex)
    {
        switch (vertexIndex)
        {
            case 0: return new Vector3(0, 0, 0);
            case 1: return new Vector3(1, 0, 0);
            case 2: return new Vector3(1, 0, 1);
            case 3: return new Vector3(0, 0, 1);
            case 4: return new Vector3(0, 1, 0);
            case 5: return new Vector3(1, 1, 0);
            case 6: return new Vector3(1, 1, 1);
            case 7: return new Vector3(0, 1, 1);
            default: return Vector3.zero;
        }
    }

    // Simplified version for debugging
    public static Mesh GenerateTestMesh(float[,,] densityGrid, float voxelSize, float surfaceLevel)
    {
        GenerateMesh(densityGrid, voxelSize, surfaceLevel, out Vector3[] vertices, out int[] triangles);

        if (vertices.Length == 0)
        {
            // Return empty mesh if no vertices
            Mesh emptyMesh = new Mesh();  // Changed from 'mesh' to 'emptyMesh'
            emptyMesh.vertices = new Vector3[0];
            emptyMesh.triangles = new int[0];
            return emptyMesh;
        }

        Mesh resultMesh = new Mesh();  // Changed from 'mesh' to 'resultMesh'
        resultMesh.vertices = vertices;
        resultMesh.triangles = triangles;
        resultMesh.RecalculateNormals();
        resultMesh.RecalculateBounds();

        return resultMesh;
    }
}