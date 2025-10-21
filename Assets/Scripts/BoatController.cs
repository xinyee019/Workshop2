using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;

    [Header("Movement Settings")]
    public float enginePower = 50f;
    public float turnPower = 5f;
    public float maxSpeed = 10f;

    [Header("Input Settings")]
    public string throttleInput = "Vertical";  // W/S or Up/Down arrows
    public string turnInput = "Horizontal";   // A/D or Left/Right arrows

    [Header("Stabilization")]
    public Transform orientation; // Optional: empty GameObject for forward direction
    public float uprightTorque = 10f; // helps keep boat upright

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.centerOfMass = new Vector3(0, -0.5f, 0); // lower center for stability
    }

    private void FixedUpdate()
    {
        HandleMovement();
        StabilizeBoat();
    }

    void HandleMovement()
    {
        float throttle = Input.GetAxis(throttleInput); // forward/back
        float turn = Input.GetAxis(turnInput);         // left/right

        // Limit speed
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (horizontalVel.magnitude < maxSpeed)
        {
            // Apply forward thrust
            Vector3 forceDir = (orientation ? orientation.forward : transform.forward);
            rb.AddForce(forceDir * throttle * enginePower, ForceMode.Force);
        }

        // Apply turning torque
        rb.AddTorque(Vector3.up * turn * turnPower, ForceMode.Force);
    }

    void StabilizeBoat()
    {
        // Optional upright torque (keeps the boat from flipping)
        Vector3 currentUp = transform.up;
        Quaternion targetRotation = Quaternion.FromToRotation(currentUp, Vector3.up) * rb.rotation;
        rb.AddTorque(new Vector3(targetRotation.x, 0f, targetRotation.z) * uprightTorque);
    }
}
