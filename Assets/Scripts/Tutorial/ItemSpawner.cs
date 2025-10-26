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
        public int spawnWeight = 50; // Higher weight = more likely to spawn
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

    [Header("Advanced Settings")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;

    private List<GameObject> activeTrashItems = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int totalSpawnWeight;

    public int CurrentTrashCount => activeTrashItems.Count;
    public int MaxTrashCount => maxConcurrentItems;

    void Start()
    {
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
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
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
        Vector3 spawnPosition = GetRandomSpawnPosition();

        if (IsValidSpawnPosition(spawnPosition))
        {
            TrashItem trashToSpawn = GetRandomTrashItem();
            if (trashToSpawn != null)
            {
                GameObject newTrash = Instantiate(trashToSpawn.prefab, spawnPosition, GetRandomRotation());
                activeTrashItems.Add(newTrash);

                // Auto-destroy after lifetime
                StartCoroutine(DestroyAfterTime(newTrash, itemLifetime));
            }
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
        return !Physics.CheckSphere(position, spawnCheckRadius, spawnCheckMask);
    }

    private Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(0, Random.Range(0, 360f), 0);
    }

    private TrashItem GetRandomTrashItem()
    {
        if (trashPrefabs.Length == 0) return null;

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

    // Public methods for external control
    public void ForceSpawn()
    {
        if (activeTrashItems.Count < maxConcurrentItems)
        {
            TrySpawnTrash();
        }
    }

    public void ClearAllTrash()
    {
        StopAllCoroutines();

        foreach (var trash in activeTrashItems)
        {
            if (trash != null)
                Destroy(trash);
        }

        activeTrashItems.Clear();
    }

    public void SetSpawnRate(float newInterval)
    {
        spawnInterval = Mathf.Max(0.1f, newInterval);

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

        // Remove excess items if necessary
        while (activeTrashItems.Count > maxConcurrentItems)
        {
            if (activeTrashItems[0] != null)
                Destroy(activeTrashItems[0]);
            activeTrashItems.RemoveAt(0);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, spawnArea);

        // Draw spawn check radius at a sample position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnCheckRadius);
    }
}