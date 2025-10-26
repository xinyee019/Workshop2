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
    private bool isInitialized = false;

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

        isInitialized = true;
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

            // Immediately hide prompt when clearing
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }
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

            // Clear reference and hide prompt
            CollectableItem collectedItem = nearbyCollectable;
            nearbyCollectable = null;

            // Hide prompt immediately after collection
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }

            Debug.Log($"Collected: {collectedItem.itemName}");
        }
    }

    private void UpdateCollectPrompt()
    {
        if (collectPromptUI != null && isInitialized)
        {
            bool showPrompt = nearbyCollectable != null &&
                            !nearbyCollectable.IsCollected &&
                            IsCollectableInRange(nearbyCollectable);

            collectPromptUI.SetActive(showPrompt);

            if (showPrompt && collectPromptText != null)
            {
                collectPromptText.text = $"Press {collectKey} to collect {nearbyCollectable.itemName}";
            }
        }
    }

    private bool IsCollectableInRange(CollectableItem collectable)
    {
        if (collectable == null) return false;

        float distance = Vector3.Distance(transform.position, collectable.transform.position);
        return distance <= collectionRange;
    }

    void FixedUpdate()
    {
        // This backup system should check if we lost our current collectable
        CheckForCollectablesWithOverlap();
    }

    private void CheckForCollectablesWithOverlap()
    {
        // If we have a current collectable, verify it's still in range and valid
        if (nearbyCollectable != null)
        {
            // Check if collectable was destroyed, collected, or out of range
            if (nearbyCollectable == null ||
                nearbyCollectable.IsCollected ||
                !IsCollectableInRange(nearbyCollectable))
            {
                ClearNearbyCollectable(nearbyCollectable);
                return; // Don't look for new ones immediately
            }
        }

        // Only look for new collectables if we don't have a current valid one
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
                if (distance < closestDistance && distance <= collectionRange)
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

    // Handle cases where collectable is destroyed without triggering collision
    void OnDestroy()
    {
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }
    }

    // Visualize collection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}