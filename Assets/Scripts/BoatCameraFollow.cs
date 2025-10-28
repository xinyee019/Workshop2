using UnityEngine;

public class BoatCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public float waterLevel = 0f; // Set this to your water surface Y position

    [Header("Camera Settings")]
    public float distance = 12f;
    public float height = 6f;
    public float sensitivity = 2f;
    public float smoothness = 0.1f;

    [Header("DREDGE-style Settings")]
    public float minHeightAboveWater = 1f; // Minimum height above water
    public float defaultPitchAngle = 25f; // Default camera pitch (looking slightly down)
    public float maxPitchAngle = 45f; // Maximum downward angle
    public float minPitchAngle = 10f; // Minimum downward angle
    public float orbitSpeedMultiplier = 1.5f; // How fast camera orbits around boat

    private float currentX = 0f;
    private float currentY = 0f;
    private Vector3 velocity = Vector3.zero;
    private float currentDistance;

    void Start()
    {
        if (target != null)
        {
            currentY = target.eulerAngles.y;
        }

        currentX = defaultPitchAngle; // Start with default pitch
        currentDistance = distance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        HandleInput();
        UpdateCamera();
    }

    void HandleInput()
    {
        // Mouse look with orbit multiplier
        currentY += Input.GetAxis("Mouse X") * sensitivity * orbitSpeedMultiplier;
        currentX -= Input.GetAxis("Mouse Y") * sensitivity; // Inverted for more natural control

        // Clamp vertical angle to prevent going under water
        currentX = Mathf.Clamp(currentX, minPitchAngle, maxPitchAngle);

        // Reset camera with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
    }

    void UpdateCamera()
    {
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentX, currentY, 0f);

        // Calculate desired position
        Vector3 desiredPosition = target.position + rotation * new Vector3(0f, height, -currentDistance);

        // Ensure camera doesn't go below water level
        float minY = waterLevel + minHeightAboveWater;
        if (desiredPosition.y < minY)
        {
            desiredPosition.y = minY;

            // Adjust pitch to maintain look direction when constrained by water
            Vector3 directionToTarget = target.position - desiredPosition;
            float constrainedPitch = Mathf.Atan2(directionToTarget.y, new Vector2(directionToTarget.x, directionToTarget.z).magnitude) * Mathf.Rad2Deg;
            currentX = Mathf.Clamp(constrainedPitch, minPitchAngle, maxPitchAngle);
        }

        // Apply smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothness);

        // Look at a point slightly in front of the boat for better navigation view
        Vector3 lookTarget = target.position + target.forward * 3f + Vector3.up * 1f;
        transform.LookAt(lookTarget);
    }

    void ResetCamera()
    {
        if (target != null)
        {
            currentY = target.eulerAngles.y;
            currentX = defaultPitchAngle;
        }
    }

    // Public methods for external control
    public void SetCameraDistance(float newDistance)
    {
        currentDistance = Mathf.Clamp(newDistance, 5f, 20f);
    }

    public void SetPitchAngle(float pitch)
    {
        currentX = Mathf.Clamp(pitch, minPitchAngle, maxPitchAngle);
    }

    // Draw debug gizmos
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            // Draw water level
            Gizmos.color = Color.blue;
            Vector3 waterStart = new Vector3(target.position.x - 10f, waterLevel, target.position.z - 10f);
            Vector3 waterEnd = new Vector3(target.position.x + 10f, waterLevel, target.position.z + 10f);
            Gizmos.DrawLine(waterStart, waterEnd);

            // Draw camera constraints
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position + Vector3.up * (waterLevel + minHeightAboveWater), 1f);

            // Draw look target
            Vector3 lookTarget = target.position + target.forward * 3f + Vector3.up * 1f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lookTarget, 0.5f);
            Gizmos.DrawLine(transform.position, lookTarget);
        }
    }
}