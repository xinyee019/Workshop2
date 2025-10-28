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
    [SerializeField] private LayerMask spawnCheckMask = 1;
    [SerializeField] private float spawnCheckRadius = 0.5f;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;

    private List<GameObject> activeTrashItems = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int totalSpawnWeight;
    private int spawnAttempts = 0;

    public int CurrentTrashCount => activeTrashItems.Count;
    public int MaxTrashCount => maxConcurrentItems;
    public int SpawnAttempts => spawnAttempts;

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

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            if (debugMode) Debug.Log("Stopping spawn coroutine");
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        if (debugMode) Debug.Log("Spawn routine started");

        // Initial spawn
        if (activeTrashItems.Count < maxConcurrentItems)
        {
            TrySpawnTrash();
        }

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (activeTrashItems.Count < maxConcurrentItems)
            {
                TrySpawnTrash();
            }
            else if (debugMode)
            {
                Debug.Log($"Max items reached ({activeTrashItems.Count}/{maxConcurrentItems}), waiting...");
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

        Vector3 spawnPosition = GetRandomSpawnPosition();

        if (IsValidSpawnPosition(spawnPosition))
        {
            TrashItem trashToSpawn = GetRandomTrashItem();
            if (trashToSpawn != null && trashToSpawn.prefab != null)
            {
                GameObject newTrash = Instantiate(trashToSpawn.prefab, spawnPosition, GetRandomRotation());
                activeTrashItems.Add(newTrash);

                if (debugMode) Debug.Log($"Spawned {trashToSpawn.prefab.name} at {spawnPosition}");

                // Auto-destroy after lifetime
                StartCoroutine(DestroyAfterTime(newTrash, itemLifetime));
            }
            else
            {
                if (debugMode) Debug.LogError("Invalid trash item or prefab reference!");
            }
        }
        else
        {
            if (debugMode) Debug.LogWarning($"Invalid spawn position at {spawnPosition}, attempt #{spawnAttempts}");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPoint = new Vector3(
            Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            Random.Range(-spawnArea.y / 2, spawnArea.y / 2),
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
            Debug.Log($"Position {position} is occupied, checking with radius {spawnCheckRadius}");
        }

        return !isOccupied;
    }

    private Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(0, Random.Range(0, 360f), 0);
    }

    private TrashItem GetRandomTrashItem()
    {
        if (trashPrefabs.Length == 0)
        {
            if (debugMode) Debug.LogError("No trash prefabs available!");
            return null;
        }

        // If total weight is 0, just return random item
        if (totalSpawnWeight <= 0)
        {
            if (debugMode) Debug.LogWarning("Total spawn weight is 0, using random selection");
            return trashPrefabs[Random.Range(0, trashPrefabs.Length)];
        }

        int randomValue = Random.Range(0, totalSpawnWeight);
        int currentWeight = 0;

        foreach (var trash in trashPrefabs)
        {
            currentWeight += trash.spawnWeight;
            if (randomValue < currentWeight)
            {
                if (debugMode) Debug.Log($"Selected {trash.prefab.name} with weight {trash.spawnWeight}");
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
            if (debugMode) Debug.Log($"Destroying {trashObject.name} after {delay} seconds");
            activeTrashItems.Remove(trashObject);
            Destroy(trashObject);
        }
    }

    // Public methods for external control
    public void ForceSpawn()
    {
        if (activeTrashItems.Count < maxConcurrentItems)
        {
            if (debugMode) Debug.Log("Force spawning item");
            TrySpawnTrash();
        }
    }

    public void ClearAllTrash()
    {
        if (debugMode) Debug.Log("Clearing all trash items");

        StopAllCoroutines();

        foreach (var trash in activeTrashItems)
        {
            if (trash != null)
                Destroy(trash);
        }

        activeTrashItems.Clear();

        // Restart spawning if it was running
        if (spawnCoroutine != null)
        {
            StartSpawning();
        }
    }

    public void SetSpawnRate(float newInterval)
    {
        spawnInterval = Mathf.Max(0.1f, newInterval);

        if (debugMode) Debug.Log($"Spawn rate set to {newInterval}");

        // Restart coroutine with new interval
        if (spawnCoroutine != null)
        {
            StopSpawning();
            StartSpawning();
        }
    }

    public void SetMaxItems(int newMax)
    {
        maxConcurrentItems = Mathf.Max(1, newMax);

        if (debugMode) Debug.Log($"Max items set to {newMax}");

        // Remove excess items if necessary
        while (activeTrashItems.Count > maxConcurrentItems)
        {
            if (activeTrashItems[0] != null)
                Destroy(activeTrashItems[0]);
            activeTrashItems.RemoveAt(0);
        }
    }

    // Debug method to check spawner status
    public void PrintStatus()
    {
        Debug.Log($"ItemSpawner Status:");
        Debug.Log($"- Active Items: {activeTrashItems.Count}/{maxConcurrentItems}");
        Debug.Log($"- Spawn Attempts: {spawnAttempts}");
        Debug.Log($"- Prefabs Available: {trashPrefabs?.Length ?? 0}");
        Debug.Log($"- Spawn Area: {spawnArea}");
        Debug.Log($"- Spawn Position: {transform.position}");
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, spawnArea);

        // Draw spawn check radius at a sample position
        Gizmos.color = Color.red;
        Vector3 samplePos = transform.position + new Vector3(spawnArea.x / 2, 0, 0);
        Gizmos.DrawWireSphere(samplePos, spawnCheckRadius);
    }
}