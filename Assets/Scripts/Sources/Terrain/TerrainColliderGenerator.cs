using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainColliderGenerator : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public GameObject waterColliderPrefab;
    public GameObject landColliderPrefab;

    private List<GameObject> colliders = new List<GameObject>();

    void Start()
    {
        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator not found!");
            return;
        }

        StartCoroutine(GenerateCollidersAfterMap());
    }

    IEnumerator GenerateCollidersAfterMap()
    {
        Debug.Log("Waiting for map generation...");
        yield return new WaitUntil(() => mapGenerator.IsMapReady);
        Debug.Log("Map ready, generating colliders...");
        GenerateColliders();
    }

    void GenerateColliders()
    {
        ClearExistingColliders();

        int gridSize = 10;

        // FIX: Use MapGenerator.mapChunkSize (class name) instead of mapGenerator.mapChunkSize (instance)
        int cellSize = Mathf.CeilToInt(MapGenerator.mapChunkSize / gridSize);

        for (int gridX = 0; gridX < gridSize; gridX++)
        {
            for (int gridZ = 0; gridZ < gridSize; gridZ++)
            {
                Vector3 worldPos = GetWorldPosition(gridX * cellSize, gridZ * cellSize);
                bool isWater = mapGenerator.IsWater(worldPos);

                if (isWater ? waterColliderPrefab != null : landColliderPrefab != null)
                {
                    GameObject colliderObj = Instantiate(
                        isWater ? waterColliderPrefab : landColliderPrefab,
                        worldPos,
                        Quaternion.identity,
                        transform
                    );

                    colliderObj.name = (isWater ? "Water" : "Land") + $"Collider_{gridX}_{gridZ}";
                    colliders.Add(colliderObj);
                }
            }
        }

        Debug.Log($"Generated {colliders.Count} terrain colliders");
    }

    Vector3 GetWorldPosition(int x, int z)
    {
        // FIX: Use MapGenerator.mapChunkSize (class name) instead of mapGenerator.mapChunkSize (instance)
        float halfSize = (MapGenerator.mapChunkSize - 1) / 2f;
        return new Vector3(x - halfSize, 0, z - halfSize);
    }

    void ClearExistingColliders()
    {
        foreach (GameObject collider in colliders)
        {
            if (collider != null)
                Destroy(collider);
        }
        colliders.Clear();
    }
}