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
        nearbyCollectable = collectable;
    }

    public void ClearNearbyCollectable(CollectableItem collectable)
    {
        if (nearbyCollectable == collectable)
        {
            nearbyCollectable = null;
        }
    }

    private void CollectItem()
    {
        if (nearbyCollectable != null)
        {
            // Play sound
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // Collect the item
            nearbyCollectable.Collect();
            nearbyCollectable = null;
        }
    }

    private void UpdateCollectPrompt()
    {
        if (collectPromptUI != null)
        {
            bool showPrompt = nearbyCollectable != null;
            collectPromptUI.SetActive(showPrompt);

            if (showPrompt && collectPromptText != null)
            {
                collectPromptText.text = $"Press E to collect {nearbyCollectable.itemName}";
            }
        }
    }

    // Optional: Raycast-based detection as backup
    void FixedUpdate()
    {
        // This is a backup detection system using raycasts
        if (nearbyCollectable == null)
        {
            DetectCollectablesWithRaycast();
        }
    }

    private void DetectCollectablesWithRaycast()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, collectionRange);
        foreach (var hitCollider in hitColliders)
        {
            CollectableItem collectable = hitCollider.GetComponent<CollectableItem>();
            if (collectable != null && !collectable.IsCollected) // Use the property instead
            {
                SetNearbyCollectable(collectable);
                break;
            }
        }
    }

    // Visualize collection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}