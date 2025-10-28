using UnityEngine;

public class SimpleBoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 5f;
    public float maxSpeed = 10f;
    public float reverseSpeed = 4f;
    public float deceleration = 3f;

    [Header("Turning Settings")]
    public float turnSpeed = 45f;
    public float turnLimit = 1f;

    [Header("Fake Buoyancy Settings")]
    public float bobFrequency = 1.5f;
    public float bobAmplitude = 0.15f;
    public float tiltAmount = 5f;
    public float rollAmount = 4f;
    public float buoyancySmooth = 2f;

    [Header("Environment Settings")]
    public float waterHeight = 0f;
    public LayerMask terrainLayer;
    public float terrainCheckHeight = 100f;
    public float collisionBuffer = 0.5f; // Distance to stop before terrain

    private float currentSpeed = 0f;
    private float moveInput = 0f;
    private float turnInput = 0f;
    private float bobOffset;
    private Quaternion baseRotation;
    private Vector3 lastValidPosition;

    void Start()
    {
        baseRotation = transform.rotation;
        bobOffset = Random.Range(0f, 100f);
        lastValidPosition = transform.position;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");

        HandleMovement();
        HandleTurning();
        ApplyFakeBuoyancy();
    }

    void HandleMovement()
    {
        // Forward/Reverse Acceleration
        if (moveInput != 0)
        {
            float targetSpeed = moveInput > 0 ? maxSpeed : -reverseSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        // Calculate potential new position
        Vector3 potentialPosition = transform.position + transform.forward * currentSpeed * Time.deltaTime;

        // Check terrain height at potential position
        float terrainY = GetTerrainHeightAt(potentialPosition);
        float waterY = waterHeight;

        // If terrain is significantly higher than water, bounce back
        if (terrainY > waterY + collisionBuffer)
        {
            // Stop movement and push back
            currentSpeed = 0f;
            transform.position = lastValidPosition;
        }
        else
        {
            // Valid position, move forward
            transform.position = potentialPosition;
            lastValidPosition = transform.position;
        }

        // Keep boat at water level
        transform.position = new Vector3(transform.position.x, waterY, transform.position.z);
    }

    void HandleTurning()
    {
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float rotationAmount = turnInput * turnSpeed * speedFactor * turnLimit * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
        }
    }

    void ApplyFakeBuoyancy()
    {
        // Bobbing effect (sin wave)
        float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;

        // Pitch (forward/back tilt) based on speed
        float pitch = -Mathf.Lerp(0, tiltAmount, Mathf.Abs(currentSpeed) / maxSpeed) * Mathf.Sign(currentSpeed);

        // Roll (side tilt) based on turning input
        float roll = -turnInput * rollAmount;

        // Combine into final rotation
        Quaternion targetRotation = Quaternion.Euler(baseRotation.eulerAngles.x + pitch, transform.eulerAngles.y, baseRotation.eulerAngles.z + roll);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * buoyancySmooth);

        // Apply bobbing (relative to water height)
        Vector3 pos = transform.position;
        pos.y = waterHeight + bob;
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * buoyancySmooth);
    }

    float GetTerrainHeightAt(Vector3 position)
    {
        // Cast downward from above to detect terrain
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * terrainCheckHeight, Vector3.down, out hit, terrainCheckHeight * 2f, terrainLayer))
        {
            return hit.point.y;
        }
        return Mathf.NegativeInfinity; // No terrain found, assume water
    }
}