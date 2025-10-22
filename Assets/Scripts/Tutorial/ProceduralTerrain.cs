using UnityEngine;

[ExecuteAlways]
public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int resolution = 100;
    public float size = 128f;
    public Material terrainMat;

    [Header("Noise Settings")]
    public bool useRandomSeed = true;
    public int seed = 0;
    public float scale = 20f;
    public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset = Vector2.zero;
    public float heightMultiplier = 10f;

    [Header("Falloff Settings")]
    public bool useFalloff = true;
    [Range(0.1f, 5f)] public float falloffScale = 1f;
    [Range(1f, 8f)] public float edgeSharpness = 3f;


    private MeshFilter filter;
    private MeshRenderer renderer;
    private MeshCollider collider;
    private Mesh mesh;

    void OnValidate()
    {
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        filter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        renderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        collider = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
        if (terrainMat != null)
            renderer.sharedMaterial = terrainMat;

        if (useRandomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);

        mesh = new Mesh();
        mesh.name = "ProceduralTerrain";

        int vertCount = (resolution + 1) * (resolution + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[resolution * resolution * 6];

        // Generate falloff map
        float[,] falloff = useFalloff ? FalloffGenerator.GenerateFalloffMap(resolution + 1, falloffScale, edgeSharpness) : null;

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        int iVert = 0;
        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float height = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = (x / (float)resolution) * size / scale * frequency + octaveOffsets[o].x;
                    float sampleY = (y / (float)resolution) * size / scale * frequency + octaveOffsets[o].y;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    height += perlin * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Apply falloff for island shape
                if (useFalloff)
                {
                    height *= Mathf.Clamp01(1 - falloff[x, y]);
                }

                verts[iVert] = new Vector3(
                    x / (float)resolution * size,
                    height * heightMultiplier,
                    y / (float)resolution * size
                );
                uvs[iVert] = new Vector2(x / (float)resolution, y / (float)resolution);
                iVert++;
            }
        }

        int triIndex = 0;
        int vert = 0;
        int vertsPerLine = resolution + 1;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                tris[triIndex + 0] = vert;
                tris[triIndex + 1] = vert + vertsPerLine;
                tris[triIndex + 2] = vert + 1;

                tris[triIndex + 3] = vert + 1;
                tris[triIndex + 4] = vert + vertsPerLine;
                tris[triIndex + 5] = vert + vertsPerLine + 1;

                vert++;
                triIndex += 6;
            }
            vert++;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
    }
}
