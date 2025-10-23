using UnityEngine;

public class BoatCube : MonoBehaviour
{
    [Header("Boat References")]
    public Transform waterPlane;

    [Header("Buoyancy Settings")]
    public Transform[] floatPoints;
    public float floatPower = 8f; // Increased
    public float waterDrag = 1.5f; // Reduced
    public float waterAngularDrag = 2f; // Reduced
    public float bounceDamping = 0.3f; // Reduced

    [Header("Movement Settings")]
    public float enginePower = 200f; // DRAMATICALLY INCREASED
    public float turnPower = 80f;
    public float maxSpeed = 15f;
    public float reversePower = 100f;

    [Header("Auto-Righting Settings")]
    public bool enableAutoRighting = true;
    public float rightingForce = 10f;
    public float rightingTorque = 5f;
    public float maxTiltAngle = 45f;
    public float emergencyRightingThreshold = 80f;

    [Header("Stability Settings")]
    public float stabilityForce = 5f; // Reduced
    public float antiRollForce = 3f; // Reduced

    [Header("Input Settings")]
    public string verticalInput = "Vertical";
    public string horizontalInput = "Horizontal";

    // Private variables
    private Rigidbody boatRigidbody;
    private float thrustInput;
    private float turnInput;
    private bool isInWater = false;

    void Start()
    {
        boatRigidbody = GetComponent<Rigidbody>();

        // PROPER Rigidbody setup
        if (boatRigidbody != null)
        {
            boatRigidbody.mass = 700f; // REDUCED mass - boats are buoyant!
            boatRigidbody.drag = 0.05f; // Reduced
            boatRigidbody.angularDrag = 0.1f; // Reduced for better turning
            boatRigidbody.centerOfMass = new Vector3(0, -0.5f, 0);

            // Make sure these are correct
            boatRigidbody.useGravity = true;
            boatRigidbody.isKinematic = false;
        }

        // If float points aren't assigned, create some default ones
        if (floatPoints == null || floatPoints.Length == 0)
        {
            CreateDefaultFloatPoints();
        }
    }

    void Update()
    {
        // Get player input
        thrustInput = Input.GetAxis(verticalInput);
        turnInput = Input.GetAxis(horizontalInput);

        // Emergency recovery key (press R to flip boat upright)
        if (Input.GetKeyDown(KeyCode.R))
        {
            EmergencyRecover();
        }
    }

    void FixedUpdate()
    {
        // Apply buoyancy forces
        ApplyBuoyancy();

        // Apply movement and turning FIRST
        ApplyMovement();
        ApplyTurning();

        // Then apply stability
        ApplyStability();

        if (enableAutoRighting)
        {
            ApplyAutoRighting();
        }
    }

    void ApplyBuoyancy()
    {
        if (floatPoints == null || boatRigidbody == null) return;

        int pointsUnderwater = 0;
        float totalBuoyancy = 0f;

        foreach (Transform floatPoint in floatPoints)
        {
            if (floatPoint == null) continue;

            Vector3 worldFloatPoint = floatPoint.position;
            float waterHeight = GetWaterHeightAtPosition(worldFloatPoint);
            float submersion = Mathf.Clamp01((waterHeight - worldFloatPoint.y) / 1f);

            if (submersion > 0)
            {
                pointsUnderwater++;

                // Stronger buoyancy calculation
                float floatForce = Mathf.Abs(Physics.gravity.y) * submersion * floatPower * 2f;
                boatRigidbody.AddForceAtPosition(Vector3.up * floatForce, worldFloatPoint, ForceMode.Force);
                totalBuoyancy += floatForce;

                // Reduced bounce damping
                float bounceForce = (waterHeight - worldFloatPoint.y) * bounceDamping;
                boatRigidbody.AddForceAtPosition(Vector3.up * bounceForce, worldFloatPoint, ForceMode.Force);
            }
        }

        if (pointsUnderwater > 0)
        {
            isInWater = true;
            // Reduced drag
            boatRigidbody.AddForce(-boatRigidbody.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.Force);
            boatRigidbody.AddTorque(-boatRigidbody.angularVelocity * waterAngularDrag * Time.fixedDeltaTime, ForceMode.Force);

            // Debug movement
            Debug.Log($"In water! Velocity: {boatRigidbody.velocity.magnitude:F2}, Buoyancy: {totalBuoyancy:F2}");
        }
        else
        {
            isInWater = false;
        }
    }

    void ApplyStability()
    {
        if (!isInWater || boatRigidbody == null) return;

        float uprightness = Vector3.Dot(transform.up, Vector3.up);

        // Reduced stability forces to not fight movement
        if (uprightness > 0.5f)
        {
            float depth = GetWaterHeightAtPosition(transform.position) - transform.position.y;
            if (depth > 0)
            {
                float antiSinkForce = Mathf.Abs(Physics.gravity.y) * depth * stabilityForce;
                boatRigidbody.AddForce(Vector3.up * antiSinkForce, ForceMode.Force);
            }
        }

        // Reduced anti-roll
        Vector3 rollAxis = Vector3.Cross(transform.up, Vector3.up);
        float rollMagnitude = rollAxis.magnitude;
        if (rollMagnitude > 0.05f) // Increased threshold
        {
            boatRigidbody.AddTorque(rollAxis * antiRollForce * rollMagnitude, ForceMode.Force);
        }
    }

    void ApplyAutoRighting()
    {
        if (!isInWater || boatRigidbody == null) return;

        float uprightness = Vector3.Dot(transform.up, Vector3.up);
        float tiltAngle = Mathf.Acos(uprightness) * Mathf.Rad2Deg;

        // Only apply strong righting when really needed
        if (tiltAngle > emergencyRightingThreshold)
        {
            Vector3 rightingDirection = Vector3.Cross(transform.right, Vector3.up);
            boatRigidbody.AddTorque(rightingDirection * rightingTorque * 2f, ForceMode.Force);
        }
        else if (tiltAngle > maxTiltAngle + 20f) // Higher threshold
        {
            Vector3 rightingDirection = Vector3.Cross(transform.right, Vector3.up);
            boatRigidbody.AddTorque(rightingDirection * rightingTorque, ForceMode.Force);
        }
    }

    void ApplyMovement()
    {
        if (thrustInput != 0 && isInWater)
        {
            // REMOVED speed limit check - let physics handle it
            float power = thrustInput > 0 ? enginePower : reversePower;
            Vector3 forwardForce = transform.forward * thrustInput * power;
            boatRigidbody.AddForce(forwardForce, ForceMode.Force);

            Debug.Log($"Applying movement! Force: {forwardForce.magnitude:F2}, Input: {thrustInput}");
        }
    }

    void ApplyTurning()
    {
        if (turnInput != 0 && isInWater)
        {
            // Strong, direct turning
            float turnStrength = turnInput * turnPower;
            boatRigidbody.AddTorque(transform.up * turnStrength, ForceMode.Force);
        }
    }

    float GetWaterHeightAtPosition(Vector3 position)
    {
        float baseHeight = waterPlane.position.y;
        float waveHeight = CalculateWaveHeight(position);
        return baseHeight + waveHeight;
    }

    float CalculateWaveHeight(Vector3 worldPosition)
    {
        Vector3 localPos = waterPlane.InverseTransformPoint(worldPosition);
        float time = Time.time;

        float bigWave = Mathf.Sin(localPos.x * 0.1f + time * 0.66f) *
                       Mathf.Sin(localPos.z * 0.1f + time * 0.33f) * 0.12f;

        float ripple = Mathf.PerlinNoise(localPos.x * 7.06f + time * 1.48f,
                                        localPos.z * 7.06f + time * 1.48f) * 0.05f;

        return bigWave + ripple;
    }

    void CreateDefaultFloatPoints()
    {
        floatPoints = new Transform[4];

        Vector3[] localPositions = new Vector3[]
        {
            new Vector3(0, -1, 2),
            new Vector3(0, -1, -2),
            new Vector3(-1.5f, -1, 0),
            new Vector3(1.5f, -1, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject floatPoint = new GameObject("FloatPoint_" + i);
            floatPoint.transform.SetParent(transform);
            floatPoint.transform.localPosition = localPositions[i];
            floatPoints[i] = floatPoint.transform;
        }

        Debug.Log("Created default float points for boat");
    }

    public void EmergencyRecover()
    {
        if (boatRigidbody != null)
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            boatRigidbody.velocity = Vector3.zero;
            boatRigidbody.angularVelocity = Vector3.zero;
            Debug.Log("Boat emergency recovery activated!");
        }
    }

    void OnDrawGizmos()
    {
        if (floatPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform point in floatPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.2f);
                }
            }
        }

        if (boatRigidbody != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(boatRigidbody.centerOfMass), 0.3f);
        }
    }
}

