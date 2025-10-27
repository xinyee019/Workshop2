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

    private Terrain terrain;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found!");
            return;
        }

        GenerateTerrain();
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

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                // Normalized coords for Perlin noise
                float xCoord = (float)x / resolution * scale + offsetX;
                float yCoord = (float)y / resolution * scale + offsetY;
                float noise = Mathf.PerlinNoise(xCoord, yCoord);

                // Distance from center for island shape
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float mask = Mathf.Clamp01(1f - Mathf.Pow(distance / maxDistance, edgeFalloff));

                // Combine noise + island mask
                heights[x, y] = noise * mask;
            }
        }
        return heights;
    }
}
