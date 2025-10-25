using UnityEngine;
using System.Collections.Generic;

public class BoatSpawner : MonoBehaviour
{
    [Header("Boat Setup")]
    public GameObject[] boatPrefabs; // Multiple boat types

    [Header("Spawn Points")]
    public Transform[] spawnPoints;  // Assign 4+ spawn points in Inspector

    [Header("Spawn Settings")]
    public int boatsToSpawn = 4;     // How many boats to spawn (max = spawnPoints count)

    private void Start()
    {
        if (spawnPoints.Length == 0 || boatPrefabs.Length == 0)
        {
            Debug.LogError("Spawn points or boat prefabs not assigned!");
            return;
        }

        // Shuffle spawn points so they are used randomly without repetition
        List<Transform> shuffledPoints = new List<Transform>(spawnPoints);
        ShuffleList(shuffledPoints);

        // Spawn boats up to the number of available spawn points
        int spawnCount = Mathf.Min(boatsToSpawn, shuffledPoints.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            // Pick a random boat prefab
            GameObject chosenBoat = boatPrefabs[Random.Range(0, boatPrefabs.Length)];

            Transform spawnPoint = shuffledPoints[i];
            GameObject newBoat = Instantiate(chosenBoat, spawnPoint.position, spawnPoint.rotation);
            newBoat.SetActive(true); // ensure visible
        }
    }

    // Fisher–Yates shuffle
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
