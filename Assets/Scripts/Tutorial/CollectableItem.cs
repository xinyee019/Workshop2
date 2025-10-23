using UnityEngine;
using System.Collections;

public class CollectableItem : MonoBehaviour
{
    [Header("Collectable Settings")]
    public string itemName = "Trash";
    public int scoreValue = 10;
    public float collectRange = 3f;

    [Header("Visual Feedback")]
    public Material highlightMaterial;
    public ParticleSystem collectParticles;

    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isPlayerInRange = false;

    // Private field with public property
    private bool isCollected = false;
    public bool IsCollected => isCollected; // Add this public read-only property

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        // Auto-add a trigger collider if none exists
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = collectRange;
        }
    }

    void Update()
    {
        // Rotate slowly for visual appeal
        if (!isCollected)
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isPlayerInRange = true;
            HighlightObject(true);

            // Notify the boat that this item is collectable
            BoatCollector collector = other.GetComponent<BoatCollector>();
            if (collector != null)
            {
                collector.SetNearbyCollectable(this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isPlayerInRange = false;
            HighlightObject(false);

            // Notify the boat that no item is nearby
            BoatCollector collector = other.GetComponent<BoatCollector>();
            if (collector != null)
            {
                collector.ClearNearbyCollectable(this);
            }
        }
    }

    public void Collect()
    {
        if (isCollected) return;

        isCollected = true;

        // Visual effects
        if (collectParticles != null)
        {
            ParticleSystem particles = Instantiate(collectParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }

        // Notify game manager or score system
        GameManager.Instance?.AddScore(scoreValue);
        GameManager.Instance?.AddCollectedItem(itemName);

        // Disable the object
        HighlightObject(false);
        gameObject.SetActive(false);

        Debug.Log($"Collected: {itemName} (+{scoreValue} points)");
    }

    private void HighlightObject(bool highlight)
    {
        if (objectRenderer != null && highlightMaterial != null)
        {
            objectRenderer.material = highlight ? highlightMaterial : originalMaterial;
        }

        // Optional: Add glow effect or outline
        if (highlight)
        {
            // You can add additional highlight effects here
        }
    }

    // Visualize the collect range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRange);
    }
}