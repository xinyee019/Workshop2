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

    private float currentSpeed = 0f;
    private float moveInput = 0f;
    private float turnInput = 0f;

    void Update()
    {
        moveInput = Input.GetAxisRaw("Vertical");   // W/S
        turnInput = Input.GetAxisRaw("Horizontal"); // A/D

        HandleMovement();
        HandleTurning();
    }

    void HandleMovement()
    {
        // Accelerate or decelerate smoothly
        if (moveInput != 0)
        {
            float targetSpeed = moveInput > 0 ? maxSpeed : -reverseSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            // Natural slow down
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        // Apply movement
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    void HandleTurning()
    {
        // Turn only when pressing A/D
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float rotationAmount = turnInput * turnSpeed * speedFactor * turnLimit * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
        }
    }
}
