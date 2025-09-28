using UnityEngine;
using System.Collections;

public class CloudQuadStack : MonoBehaviour
{
    private Material cloudMaterial;
    private Mesh quadMesh;
    private Matrix4x4[] matrices;
    private Vector4[] layerOffsets;
    private MaterialPropertyBlock propertyBlock;
    
    private bool isActive = false;
    private float currentAltitude;
    private float currentThickness;
    
    public void Initialize(Material material)
    {
        cloudMaterial = new Material(material); // Create instance
        quadMesh = CreateQuadMesh();
        propertyBlock = new MaterialPropertyBlock();
    }
    
    public void Activate(Vector3 position, float altitude, float thickness, int layers, float scale)
    {
        transform.position = position;
        currentAltitude = altitude;
        currentThickness = thickness;
        
        SetupLayerData(layers, scale);
        isActive = true;
        gameObject.SetActive(true);
    }
    
    public void Deactivate()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
    
    private void SetupLayerData(int layers, float scale)
    {
        matrices = new Matrix4x4[layers];
        layerOffsets = new Vector4[layers];
        
        float layerSpacing = currentThickness / layers;
        float startHeight = currentAltitude - (currentThickness * 0.5f);
        
        for (int i = 0; i < layers; i++)
        {
            float height = startHeight + (i * layerSpacing);
            Vector3 position = new Vector3(transform.position.x, height, transform.position.z);
            
            // Unique offset for each layer to break up column pattern
            layerOffsets[i] = new Vector4(
                Random.Range(-1000f, 1000f),
                Random.Range(-1000f, 1000f),
                Random.Range(-1000f, 1000f),
                (float)i / layers // Normalized layer height for taper
            );
            
            matrices[i] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * scale);
        }
    }
    
    void Update()
    {
        if (!isActive || matrices == null) return;
        
        for (int i = 0; i < matrices.Length; i++)
        {
            propertyBlock.SetVector("_LayerOffset", layerOffsets[i]);
            Graphics.DrawMesh(quadMesh, matrices[i], cloudMaterial, 0, null, 0, propertyBlock);
        }
    }
    
    public void UpdatePosition(Vector3 cameraPosition, float spawnRadius)
    {
        Vector3 cameraPos2D = new Vector3(cameraPosition.x, 0, cameraPosition.z);
        Vector3 cloudPos2D = new Vector3(transform.position.x, 0, transform.position.z);
        
        float distance = Vector3.Distance(cameraPos2D, cloudPos2D);
        
        if (distance > spawnRadius * 1.5f)
        {
            // Reposition cloud to other side of camera
            Vector3 direction = (cloudPos2D - cameraPos2D).normalized;
            Vector3 newPos = cameraPos2D - direction * (spawnRadius * 0.9f);
            newPos.y = currentAltitude;
            transform.position = newPos;
            
            // Regenerate layer offsets for new position
            SetupLayerData(matrices.Length, transform.localScale.x);
        }
    }
    
    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
}