using UnityEngine;
[ExecuteInEditMode]
public class ProceduralTerrainGeneratorV2 : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 512;
    public int terrainLength = 512;
    public int terrainHeight = 50; // max vertical height

    [Header("Noise Settings")]
    public float scale = 80f;
    public float offsetX = 0f;
    public float offsetY = 0f;

    [Header("Island Settings")]
    [Range(0f, 1f)] public float islandSize = 0.5f; // smaller = more ocean
    [Range(0f, 1f)] public float edgeFalloff = 0.5f; // how fast it fades to sea level

    [Header("Seed Settings")]
    [Tooltip("Leave 0 to auto-randomize each play")]
    public int seed = 0;
    public bool randomizeOnPlay = true;

    [Header("Noise Variation")]
    public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    private Terrain terrain;
    private int currentSeed;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found!");
            return;
        }

        // Auto-randomize seed if enabled and seed is 0
        if (randomizeOnPlay && seed == 0)
        {
            currentSeed = System.DateTime.Now.GetHashCode();
        }
        else
        {
            currentSeed = seed;
        }

        GenerateTerrain();
        Debug.Log("Generated Terrain with Seed: " + currentSeed);
    }

    void OnValidate()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();

        if (terrain != null && Application.isPlaying == false)
        {
            // Use the seed directly in editor
            currentSeed = seed;
            GenerateTerrain();
        }
    }

    public void GenerateTerrain()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();

        // Apply dimensions
        TerrainData data = terrain.terrainData;
        data.heightmapResolution = 513;
        data.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        float[,] heights = GenerateHeights(data.heightmapResolution);
        data.SetHeights(0, 0, heights);
    }

    float[,] GenerateHeights(int resolution)
    {
        float[,] heights = new float[resolution, resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float maxDistance = resolution * islandSize;

        // Initialize random with seed
        Random.InitState(currentSeed);

        // Generate octave offsets
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetXOctave = Random.Range(-10000f, 10000f);
            float offsetYOctave = Random.Range(-10000f, 10000f);
            octaveOffsets[i] = new Vector2(offsetXOctave, offsetYOctave);
        }

        float maxNoiseValue = 0f;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseValue = 0f;
                float maxAmplitude = 0f;

                // Multi-octave Perlin noise
                for (int o = 0; o < octaves; o++)
                {
                    float xCoord = (float)x / resolution * scale * frequency + offsetX + octaveOffsets[o].x;
                    float yCoord = (float)y / resolution * scale * frequency + offsetY + octaveOffsets[o].y;

                    noiseValue += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
                    maxAmplitude += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseValue /= maxAmplitude;
                maxNoiseValue = Mathf.Max(maxNoiseValue, noiseValue);

                // Distance from center for island shape
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float mask = Mathf.Clamp01(1f - Mathf.Pow(distance / maxDistance, edgeFalloff));

                // Combine noise + island mask
                heights[x, y] = noiseValue * mask;
            }
        }

        return heights;
    }
}