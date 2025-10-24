using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BoatCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    public float collectionRange = 3f;
    public KeyCode collectKey = KeyCode.E;

    [Header("UI References")]
    public GameObject collectPromptUI;
    public Text collectPromptText;
    public AudioClip collectSound;

    public CollectableItem nearbyCollectable;
    private AudioSource audioSource;

    void Start()
    {
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Hide prompt initially
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        // Check for collection input
        if (Input.GetKeyDown(collectKey) && nearbyCollectable != null)
        {
            CollectItem();
        }

        // Update UI prompt
        UpdateCollectPrompt();
    }

    public void SetNearbyCollectable(CollectableItem collectable)
    {
        // Only set if not already collected and not already the current collectable
        if (collectable != null && !collectable.IsCollected && nearbyCollectable != collectable)
        {
            nearbyCollectable = collectable;
            Debug.Log($"Nearby collectable set: {collectable.itemName}");
        }
    }

    public void ClearNearbyCollectable(CollectableItem collectable)
    {
        if (nearbyCollectable == collectable)
        {
            nearbyCollectable = null;
            Debug.Log($"Nearby collectable cleared: {collectable.itemName}");
        }
    }

    private void CollectItem()
    {
        if (nearbyCollectable != null && !nearbyCollectable.IsCollected)
        {
            // Play sound
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // Collect the item
            nearbyCollectable.Collect();
            nearbyCollectable = null;

            // Hide prompt immediately after collection
            UpdateCollectPrompt();
        }
    }

    private void UpdateCollectPrompt()
    {
        if (collectPromptUI != null)
        {
            bool showPrompt = nearbyCollectable != null && !nearbyCollectable.IsCollected;
            collectPromptUI.SetActive(showPrompt);

            if (showPrompt && collectPromptText != null)
            {
                collectPromptText.text = $"Press {collectKey} to collect {nearbyCollectable.itemName}";
            }
        }
    }

    // Optional: Raycast-based detection as backup
    void FixedUpdate()
    {
        // This backup system should check if we lost our current collectable
        CheckForCollectablesWithOverlap();
    }

    private void CheckForCollectablesWithOverlap()
    {
        // If we have a current collectable, verify it's still in range
        if (nearbyCollectable != null)
        {
            float distance = Vector3.Distance(transform.position, nearbyCollectable.transform.position);
            if (distance > collectionRange || nearbyCollectable.IsCollected)
            {
                ClearNearbyCollectable(nearbyCollectable);
            }
        }

        // If no current collectable, look for new ones
        if (nearbyCollectable == null)
        {
            FindNewCollectables();
        }
    }

    private void FindNewCollectables()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, collectionRange);
        CollectableItem closestCollectable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            CollectableItem collectable = hitCollider.GetComponent<CollectableItem>();
            if (collectable != null && !collectable.IsCollected)
            {
                float distance = Vector3.Distance(transform.position, collectable.transform.position);
                if (distance < closestDistance)
                {
                    closestCollectable = collectable;
                    closestDistance = distance;
                }
            }
        }

        if (closestCollectable != null)
        {
            SetNearbyCollectable(closestCollectable);
        }
    }

    // Visualize collection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}