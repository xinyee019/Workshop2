using UnityEngine;

/// <summary>
/// Dynamically resizes, repositions, and centers the water plane to fit the MapGenerator terrain.
/// Automatically updates whenever the map regenerates (during play mode or editor preview).
/// Ensures smooth ripple alignment without visible tiling seams.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class WaterPlaneFitter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to your MapGenerator (assign in Inspector).")]
    public MapGenerator mapGenerator;

    [Tooltip("Optional: Assign your WaterTrigger object (child under ObjectTriggeredRippleWaterPlane).")]
    public Transform waterTrigger;

    [Header("Settings")]
    [Tooltip("Vertical offset for fine-tuning water height.")]
    public float heightOffset = 0f;

    [Tooltip("How often to auto-check for terrain size changes (in seconds).")]
    public float autoUpdateInterval = 1f;

    private float _lastUpdateTime;
    private int _lastMapSize = -1;
    private float _lastWaterLevel = -999f;

    void Start()
    {
        UpdateWaterPlane();
    }

    void Update()
    {
        // Auto-update during runtime or editor if map changes
        if (!mapGenerator) return;

        if (Application.isPlaying)
        {
            if (Time.time - _lastUpdateTime > autoUpdateInterval)
            {
                AutoDetectAndUpdate();
                _lastUpdateTime = Time.time;
            }
        }
        else
        {
            // Also refresh in Editor when values change
            AutoDetectAndUpdate();
        }
    }

    private void AutoDetectAndUpdate()
    {
        int mapSize = MapGenerator.mapChunkSize;
        float waterLevel = GetWaterLevel();

        if (mapSize != _lastMapSize || Mathf.Abs(waterLevel - _lastWaterLevel) > 0.001f)
        {
            UpdateWaterPlane();
            _lastMapSize = mapSize;
            _lastWaterLevel = waterLevel;
        }
    }

    public void UpdateWaterPlane()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("❌ WaterPlaneFitter: MapGenerator not assigned!");
            return;
        }

        int mapSize = MapGenerator.mapChunkSize; // usually 241, or from your terrain settings
        float half = (mapSize - 1) / 2f;

        // Compute water height based on terrain region (usually first = water)
        float waterLevel = GetWaterLevel();

        // 🔹 Keep it perfectly centered around origin
        transform.localScale = new Vector3(mapSize, 1f, mapSize);
        transform.position = new Vector3(0f, waterLevel + heightOffset, 0f);
        transform.rotation = Quaternion.identity;

        // 🔹 Adjust WaterTrigger to match
        if (waterTrigger != null)
        {
            waterTrigger.position = new Vector3(0, waterLevel + heightOffset + 0.2f, 0);
            waterTrigger.localScale = Vector3.one;
        }

        Debug.Log($"🌊 WaterPlaneFitter: Updated! Size={mapSize} | WaterY={waterLevel + heightOffset:F2}");
    }

    private float GetWaterLevel()
    {
        if (mapGenerator.regions != null && mapGenerator.regions.Length > 0)
            return mapGenerator.regions[0].height * mapGenerator.meshHeightMultiplier;
        return 0f;
    }
}
