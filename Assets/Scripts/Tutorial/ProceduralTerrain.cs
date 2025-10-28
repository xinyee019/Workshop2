using UnityEngine;

[System.Serializable]
public class HeightColor
{
    public float minHeight;
    public Color color;
}

public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int width = 100;
    public int height = 100;
    public int seed = 0;

    [Header("Noise Settings")]
    public float noiseScale = 50f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Height Settings")]
    public float heightMultiplier = 20f;

    [Header("Island Settings")]
    public bool useIslandFalloff = true;
    public float falloffStrength = 2f;

    [Header("Color Settings")]
    public HeightColor[] heightColors = new HeightColor[]
    {
        new HeightColor { minHeight = 0f, color = new Color(0.2f, 0.2f, 0.8f) },
        new HeightColor { minHeight = 0.3f, color = new Color(0.8f, 0.7f, 0.2f) },
        new HeightColor { minHeight = 0.4f, color = new Color(0.2f, 0.7f, 0.2f) },
        new HeightColor { minHeight = 0.6f, color = new Color(0.5f, 0.5f, 0.5f) },
        new HeightColor { minHeight = 0.8f, color = Color.white }
    };

    [Header("Components")]
    [SerializeField] private MeshFilter terrainMeshFilter;
    [SerializeField] private MeshRenderer terrainRenderer;
    [SerializeField] private MeshCollider terrainCollider;

    private float[] noiseMap;
    private float maxNoiseValue = 0f;

    void Reset()
    {
        terrainMeshFilter = GetComponent<MeshFilter>();
        terrainRenderer = GetComponent<MeshRenderer>();
        terrainCollider = GetComponent<MeshCollider>();
    }

    void Start()
    {
        GenerateTerrain();
    }

    void OnValidate()
    {
        if (terrainMeshFilter != null)
        {
            GenerateTerrain();
        }
    }

    public void GenerateTerrain()
    {
        if (terrainMeshFilter == null) return;

        Mesh mesh = CreateTerrainMesh();
        terrainMeshFilter.mesh = mesh;

        if (terrainCollider != null)
        {
            terrainCollider.sharedMesh = null;
            terrainCollider.sharedMesh = mesh;
        }
    }

    Mesh CreateTerrainMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        GenerateNoiseMap();

        Vector3[] vertices = new Vector3[width * height];
        Color[] colors = new Color[width * height];

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = z * width + x;
                float normalizedHeight = noiseMap[idx] / maxNoiseValue;

                if (useIslandFalloff)
                {
                    normalizedHeight *= GetFalloffValue(x, z);
                }

                float y = normalizedHeight * heightMultiplier;

                vertices[idx] = new Vector3(x, y, z);
                colors[idx] = GetColorForHeight(normalizedHeight);
            }
        }

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
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void GenerateNoiseMap()
    {
        noiseMap = new float[width * height];
        maxNoiseValue = 0f;

        Random.InitState(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = Random.Range(-10000f, 10000f);
            float offsetY = Random.Range(-10000f, 10000f);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseValue = 0f;
                float maxAmplitude = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = (x / noiseScale) * frequency + octaveOffsets[o].x;
                    float sampleY = (z / noiseScale) * frequency + octaveOffsets[o].y;

                    noiseValue += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;
                    maxAmplitude += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseValue /= maxAmplitude;
                noiseMap[z * width + x] = noiseValue;
                maxNoiseValue = Mathf.Max(maxNoiseValue, noiseValue);
            }
        }
    }

    Color GetColorForHeight(float height)
    {
        System.Array.Sort(heightColors, (a, b) => a.minHeight.CompareTo(b.minHeight));

        for (int i = heightColors.Length - 1; i >= 0; i--)
        {
            if (height >= heightColors[i].minHeight)
            {
                return heightColors[i].color;
            }
        }

        return heightColors[0].color;
    }

    float GetFalloffValue(int x, int z)
    {
        float normX = (x / (float)width) * 2f - 1f;
        float normZ = (z / (float)height) * 2f - 1f;

        float dist = Mathf.Max(Mathf.Abs(normX), Mathf.Abs(normZ));
        float falloff = 1f - Mathf.Pow(dist, falloffStrength);
        return Mathf.Clamp01(falloff);
    }
}