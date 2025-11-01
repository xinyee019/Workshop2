using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

public class BoatCollector : MonoBehaviourPunCallbacks
{
    [Header("Collection Settings")]
    public float collectionRange = 3f;
    public KeyCode collectKey = KeyCode.E;

    [Header("UI References")]
    public GameObject collectPromptUI;
    public Text collectPromptText;
    public AudioClip collectSound;

    private CollectableItem nearbyCollectable;
    private AudioSource audioSource;
    private bool isInitialized = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }

        isInitialized = true;
    }

    void Update()
    {
        // Only local player can collect items
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(collectKey) && nearbyCollectable != null && !nearbyCollectable.IsCollected)
        {
            CollectItem();
        }

        UpdateCollectPrompt();
    }

    private void CollectItem()
    {
        if (nearbyCollectable != null && !nearbyCollectable.IsCollected)
        {
            Debug.Log($"Attempting to collect: {nearbyCollectable.itemName}");

            // Play sound locally
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // This will handle network collection
            nearbyCollectable.Collect();

            // Hide prompt immediately
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }
        }
    }

    public void SetNearbyCollectable(CollectableItem collectable)
    {
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
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }
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
        if (!photonView.IsMine) return;

        // If we have a nearby collectable, check if it's still valid
        if (nearbyCollectable != null)
        {
            if (nearbyCollectable.IsCollected || !IsCollectableInRange(nearbyCollectable))
            {
                ClearNearbyCollectable(nearbyCollectable);
            }
        }
    }

    void OnDestroy()
    {
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}