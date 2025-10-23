using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationCollider : MonoBehaviour
{
    private MapGenerator mapGenerator;
    private bool mapGenerated = false;

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator not found!");
            return;
        }

        StartCoroutine(WaitForMapGeneration());
    }

    IEnumerator WaitForMapGeneration()
    {
        int maxAttempts = 50;
        int attempts = 0;

        while (!mapGenerated && attempts < maxAttempts)
        {
            if (mapGenerator.terrainMesh != null)
            {
                AddTerrainCollision();
                mapGenerated = true;
                yield break;
            }

            attempts++;
            yield return new WaitForSeconds(0.1f);
        }

        if (!mapGenerated)
        {
            Debug.LogError("Failed to generate terrain mesh after " + maxAttempts + " attempts!");
        }
    }

    void AddTerrainCollision()
    {
        if (mapGenerator.terrainMesh != null)
        {
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mapGenerator.terrainMesh;
            meshCollider.convex = false;
            Debug.Log("Terrain collision added successfully!");
        }
        else
        {
            Debug.LogError("Terrain mesh is still null!");
        }
    }
}