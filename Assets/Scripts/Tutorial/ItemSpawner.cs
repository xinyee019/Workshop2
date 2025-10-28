using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class TrashItem
    {
        public GameObject prefab;
        [Range(0, 100)]
        public int spawnWeight = 50;
    }

    [Header("Spawn Settings")]
    [SerializeField] private TrashItem[] trashPrefabs;
    [SerializeField] private int maxConcurrentItems = 10;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float itemLifetime = 30f;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnArea = new Vector3(10f, 0f, 10f);
    [SerializeField] private float spawnHeight = 0.5f; // Height above ground
    [SerializeField] private LayerMask spawnCheckMask = 1;
    [SerializeField] private float spawnCheckRadius = 0.1f; // Reduced radius

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;

    private List<GameObject> activeTrashItems = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int totalSpawnWeight;
    private int spawnAttempts = 0;

    void Start()
    {
        if (debugMode) Debug.Log("ItemSpawner Started");

        CalculateTotalWeight();

        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    void CalculateTotalWeight()
    {
        totalSpawnWeight = 0;
        foreach (var trash in trashPrefabs)
        {
            totalSpawnWeight += trash.spawnWeight;
        }

        if (debugMode) Debug.Log($"Total spawn weight calculated: {totalSpawnWeight}");
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            if (debugMode) Debug.Log("Starting spawn coroutine");
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        if (debugMode) Debug.Log("Spawn routine started");

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (activeTrashItems.Count < maxConcurrentItems)
            {
                TrySpawnTrash();
            }
        }
    }

    public void TrySpawnTrash()
    {
        spawnAttempts++;

        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            if (debugMode) Debug.LogError("No trash prefabs assigned!");
            return;
        }

        // Try multiple positions before giving up
        for (int i = 0; i < 5; i++) // Try 5 different positions
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();

            if (IsValidSpawnPosition(spawnPosition))
            {
                TrashItem trashToSpawn = GetRandomTrashItem();
                if (trashToSpawn != null && trashToSpawn.prefab != null)
                {
                    GameObject newTrash = Instantiate(trashToSpawn.prefab, spawnPosition, GetRandomRotation());
                    activeTrashItems.Add(newTrash);

                    if (debugMode) Debug.Log($"Spawned {trashToSpawn.prefab.name} at {spawnPosition}");

                    StartCoroutine(DestroyAfterTime(newTrash, itemLifetime));
                    return; // Successfully spawned, exit the method
                }
            }
        }

        if (debugMode) Debug.LogWarning($"Failed to find valid spawn position after 5 attempts");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPoint = new Vector3(
            Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            spawnHeight, // Use fixed height above ground
            Random.Range(-spawnArea.z / 2, spawnArea.z / 2)
        );

        return transform.position + randomPoint;
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if position is clear using sphere cast
        bool isOccupied = Physics.CheckSphere(position, spawnCheckRadius, spawnCheckMask);

        if (debugMode && isOccupied)
        {
            // Find out what we're hitting
            Collider[] hitColliders = Physics.OverlapSphere(position, spawnCheckRadius, spawnCheckMask);
            if (hitColliders.Length > 0)
            {
                Debug.Log($"Position {position} is occupied by: {hitColliders[0].name}");
            }
        }

        return !isOccupied;
    }

    private Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(0, Random.Range(0, 360f), 0);
    }

    private TrashItem GetRandomTrashItem()
    {
        if (trashPrefabs.Length == 0) return null;
        if (totalSpawnWeight <= 0) return trashPrefabs[Random.Range(0, trashPrefabs.Length)];

        int randomValue = Random.Range(0, totalSpawnWeight);
        int currentWeight = 0;

        foreach (var trash in trashPrefabs)
        {
            currentWeight += trash.spawnWeight;
            if (randomValue < currentWeight)
            {
                return trash;
            }
        }

        return trashPrefabs[0];
    }

    private IEnumerator DestroyAfterTime(GameObject trashObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (trashObject != null)
        {
            activeTrashItems.Remove(trashObject);
            Destroy(trashObject);
        }
    }

    // Quick test method
    [ContextMenu("Test Spawn Immediately")]
    public void TestSpawnImmediately()
    {
        if (debugMode) Debug.Log("Testing immediate spawn...");
        TrySpawnTrash();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position + Vector3.up * spawnHeight, new Vector3(spawnArea.x, 0.1f, spawnArea.z));
    }
}