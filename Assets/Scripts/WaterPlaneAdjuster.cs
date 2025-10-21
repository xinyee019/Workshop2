using UnityEngine;

public class WaterPlaneAdjuster : MonoBehaviour
{
    [Header("Map Generator Reference")]
    public MapGenerator mapGenerator; // Reference to your map generator script

    [Header("Water Settings")]
    public float waterHeight = 10f; // Height of the water plane
    public float borderPadding = 10f; // Extra border around the map

    void Start()
    {
        AdjustWaterToMap();

        // Subscribe to map generation events
        if (mapGenerator != null)
        {
            mapGenerator.OnMapGenerated += AdjustWaterToMap;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (mapGenerator != null)
        {
            mapGenerator.OnMapGenerated -= AdjustWaterToMap;
        }
    }

    public void AdjustWaterToMap()
    {
        // Use the constant directly since it's public const
        float mapSize = MapGenerator.mapChunkSize; // Access via class name, not instance

        // Calculate water plane size
        float waterSize = mapSize + borderPadding;

        // For a plane: default size is 10x10, so scale = desiredSize / 10
        float scale = waterSize / 10f;

        transform.localScale = new Vector3(scale, 1, scale);
        transform.position = new Vector3(0, waterHeight, 0);

        Debug.Log($"Water plane adjusted to map size: {waterSize}x{waterSize} (Map: {mapSize})");

        // Optional: Adjust material tiling if needed
        AdjustMaterialTiling(waterSize);
    }

    void AdjustMaterialTiling(float waterSize)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            // Adjust tiling based on water size (optional)
            float tiling = waterSize / 50f; // Adjust divisor based on your desired texture density
            renderer.material.mainTextureScale = new Vector2(tiling, tiling);
        }
    }

    // Manual adjustment method (call this if needed)
    [ContextMenu("Adjust Water Now")]
    public void ManualAdjust()
    {
        AdjustWaterToMap();
    }
}