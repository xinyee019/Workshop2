using UnityEngine;

public class BoatCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;          // Assign the boat here
    public Vector3 offset = new Vector3(0f, 15f, -10f);

    [Header("Follow Settings")]
    public float followSmoothness = 5f;
    public float rotationSpeed = 3f;
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 25f;

    private float currentZoom = 15f;
    private float currentRotation = 0f;

    void Start()
    {
        currentZoom = offset.magnitude;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Mouse scroll for zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scroll * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        // Right mouse button drag to rotate
        if (Input.GetMouseButton(1))
        {
            currentRotation += Input.GetAxis("Mouse X") * rotationSpeed;
        }

        // Calculate position and rotation
        Quaternion rot = Quaternion.Euler(45f, currentRotation, 0f);
        Vector3 desiredPos = target.position - rot * Vector3.forward * currentZoom;

        transform.position = Vector3.Lerp(transform.position, desiredPos + Vector3.up * 3f, followSmoothness * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
