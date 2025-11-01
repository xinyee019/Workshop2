using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class FloatingItem : MonoBehaviour
{
    [Header("Float Settings")]
    public float waterHeight = 0f;
    public float bobFrequency = 1.5f;
    public float bobAmplitude = 0.15f;
    public float rotationSpeed = 45f;

    [Header("Collection")]
    public float collectionRadius = 1f;
    public string boatTag = "Boat"; // Set your boat tag in inspector

    private Vector3 startPosition;
    private float bobOffset;
    private bool isCollected = false;

    void Start()
    {
        startPosition = transform.position;
        bobOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (isCollected)
            return;

        // Apply bobbing motion
        float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;
        Vector3 pos = startPosition;
        pos.y = waterHeight + bob;
        transform.position = pos;

        // Gentle rotation
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(boatTag) && !isCollected)
        {
            CollectItem(other.gameObject);
        }
    }

    void CollectItem(GameObject boat)
    {
        isCollected = true;
        // Add your collection logic here
        // Examples:
        // - Add to inventory
        // - Play sound effect
        // - Play particle effect
        // - Increment score
        Destroy(gameObject);
    }
}