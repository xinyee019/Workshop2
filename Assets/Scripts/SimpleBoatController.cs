using UnityEngine;
using Photon.Pun;

public class SimpleBoatController : MonoBehaviourPunCallbacks
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
    public float collisionBuffer = 0.5f;

    private float currentSpeed = 0f;
    private float moveInput = 0f;
    private float turnInput = 0f;
    private float bobOffset;
    private Quaternion baseRotation;
    private Vector3 lastValidPosition;

    private new PhotonView photonView;

    void Start()
    {
        // Use base.photonView instead of declaring a new one
        if (!base.photonView.IsMine)
        {
            // Disable cameras and audio listeners
            Camera[] cameras = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                cam.enabled = false;
            }

            AudioListener[] listeners = GetComponentsInChildren<AudioListener>();
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = false;
            }

            enabled = false;
        }

        baseRotation = transform.rotation;
        bobOffset = Random.Range(0f, 100f);
        lastValidPosition = transform.position;
    }

    void Update()
    {
        if (!base.photonView.IsMine) // Use base.photonView here
            return;

        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");

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

        Vector3 potentialPosition = transform.position + transform.forward * currentSpeed * Time.deltaTime;

        float terrainY = GetTerrainHeightAt(potentialPosition);
        float waterY = waterHeight;

        if (terrainY > waterY + collisionBuffer)
        {
            currentSpeed = 0f;
            transform.position = lastValidPosition;
        }
        else
        {
            transform.position = potentialPosition;
            lastValidPosition = transform.position;
        }

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
        float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;

        float pitch = -Mathf.Lerp(0, tiltAmount, Mathf.Abs(currentSpeed) / maxSpeed) * Mathf.Sign(currentSpeed);

        float roll = -turnInput * rollAmount;

        Quaternion targetRotation = Quaternion.Euler(baseRotation.eulerAngles.x + pitch, transform.eulerAngles.y, baseRotation.eulerAngles.z + roll);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * buoyancySmooth);

        Vector3 pos = transform.position;
        pos.y = waterHeight + bob;
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * buoyancySmooth);
    }

    float GetTerrainHeightAt(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * terrainCheckHeight, Vector3.down, out hit, terrainCheckHeight * 2f, terrainLayer))
        {
            return hit.point.y;
        }
        return Mathf.NegativeInfinity;
    }
}