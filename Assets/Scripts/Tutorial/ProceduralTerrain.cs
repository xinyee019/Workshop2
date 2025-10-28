using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int width = 100;
    public int height = 100;
    public float scale = 10f;
    public float heightMultiplier = 10f;

    [Header("Components")]
    [SerializeField] private MeshFilter terrainMeshFilter;
    [SerializeField] private MeshRenderer terrainRenderer;
    [SerializeField] private MeshCollider terrainCollider;

    void Reset()
    {
        // Auto-setup components when added to GameObject
        terrainMeshFilter = GetComponent<MeshFilter>();
        terrainRenderer = GetComponent<MeshRenderer>();
        terrainCollider = GetComponent<MeshCollider>();
    }

    void Start()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        if (terrainMeshFilter == null) return;

        Mesh mesh = CreateTerrainMesh();
        terrainMeshFilter.mesh = mesh;

        if (terrainCollider != null)
            terrainCollider.sharedMesh = mesh;
    }

    Mesh CreateTerrainMesh()
    {
        Mesh mesh = new Mesh();

        // Create vertices
        Vector3[] vertices = new Vector3[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float y = Mathf.PerlinNoise(x / scale, z / scale) * heightMultiplier;
                vertices[z * width + x] = new Vector3(x, y, z);
            }
        }

        // Create triangles
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        int triIndex = 0;

        for (int z = 0; z < height - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int bottomLeft = z * width + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * width + x;
                int topRight = topLeft + 1;

                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;

                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}