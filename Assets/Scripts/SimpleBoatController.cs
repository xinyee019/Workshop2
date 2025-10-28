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
    public float bobFrequency = 1.5f;    // How fast the boat bobs
    public float bobAmplitude = 0.15f;   // How high/low it moves
    public float tiltAmount = 5f;        // Max forward/back tilt
    public float rollAmount = 4f;        // Max sideways roll
    public float buoyancySmooth = 2f;    // How smoothly rotation transitions

    private float currentSpeed = 0f;
    private float moveInput = 0f;
    private float turnInput = 0f;
    private float bobOffset;
    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
        bobOffset = Random.Range(0f, 100f); // Desync waves between boats
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Vertical");   // W/S
        turnInput = Input.GetAxisRaw("Horizontal"); // A/D

        HandleMovement();
        HandleTurning();
        ApplyFakeBuoyancy();
    }

    void HandleMovement()
    {
        if (moveInput != 0)
        {
            float targetSpeed = moveInput > 0 ? maxSpeed : -reverseSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        transform.position += transform.forward * currentSpeed * Time.deltaTime;
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

        // Apply bobbing vertically
        Vector3 pos = transform.position;
        pos.y = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude + pos.y;
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * buoyancySmooth);
    }
}
