using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };
    public DrawMode drawMode;

    public const int mapChunkSize = 121;

    [Range(0, 6)] public int levelOfDetail;
    public float noiseScale = 50f;

    public int octaves = 4;
    [Range(0, 1)] public float persistance = 0.5f;
    public float lacunarity = 2f;
    public int seed = 1234;
    public Vector2 offset;

    public bool useFalloffMap = true;
    public float meshHeightMultiplier = 20f;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate = true;
    public TerrainType[] regions;

    private float[,] falloffMap;
    [HideInInspector] public float[,] currentHeightMap;
    [HideInInspector] public Color[] currentColourMap;
    [HideInInspector] public Mesh terrainMesh;

    public delegate void MapGenerated();
    public event MapGenerated OnMapGenerated;

    public bool IsMapReady => currentHeightMap != null;

    void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    void Start()
    {
        GenerateMap();
    }

    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed,
            noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        currentHeightMap = noiseMap;

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFalloffMap)
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);

                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        currentColourMap = colourMap;

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
            terrainMesh = meshData.CreateMesh();
            display.DrawMesh(meshData, TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FalloffMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));

        OnMapGenerated?.Invoke();
        Debug.Log("Map generation completed and event triggered.");
    }

    public bool IsWater(Vector3 worldPosition, float tolerance = 0.02f)
    {
        if (currentHeightMap == null) return false;

        // Convert world position to heightmap coordinates
        // World coordinates: center is (0,0), range is approximately [-120, 120]
        // Heightmap coordinates: range is [0, mapChunkSize-1]
        float half = (mapChunkSize - 1) / 2f;
        int x = Mathf.RoundToInt(worldPosition.x + half);
        int y = Mathf.RoundToInt((mapChunkSize - 1) - (worldPosition.z + half));

        if (x < 0 || x >= mapChunkSize || y < 0 || y >= mapChunkSize) return false;

        float height = currentHeightMap[x, y];
        return height <= regions[0].height + tolerance;
    }

    public Vector3 GetRandomWaterPosition()
    {
        if (currentHeightMap == null) return Vector3.zero;

        float half = (mapChunkSize - 1) / 2f;
        
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            float randomX = Random.Range(-half, half);
            float randomZ = Random.Range(-half, half);
            Vector3 worldPos = new Vector3(randomX, 0f, randomZ);

            if (IsWater(worldPos))
            {
                // Get the height at this position
                int ix = Mathf.Clamp(Mathf.RoundToInt(worldPos.x + half), 0, mapChunkSize - 1);
                int iz = Mathf.Clamp(Mathf.RoundToInt((mapChunkSize - 1) - (worldPos.z + half)), 0, mapChunkSize - 1);
                float heightValue = currentHeightMap[ix, iz];
                float worldY = heightValue * meshHeightMultiplier + meshHeightCurve.Evaluate(heightValue);
                
                return new Vector3(worldPos.x, worldY, worldPos.z);
            }
        }
        
        // Fallback to center if no water found
        return Vector3.zero;
    }

    void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}
