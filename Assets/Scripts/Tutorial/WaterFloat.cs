using Xinyee;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaterFloat : MonoBehaviour
{
    [Header("Drag Settings")]
    public float AirDrag = 1;
    public float WaterDrag = 10;
    public bool AffectDirection = true;
    public Transform[] FloatPoints;

    [Header("Buoyancy Settings")]
    public float BuoyancyForce = 15f;
    public float BuoyancyDamping = 0.05f;
    public float DepthBeforeSubmerged = 1f;

    [Header("Wave Following")]
    public bool FollowWaveSlope = true; // NEW: Makes boat tilt with waves
    public float WaveSlopeInfluence = 1f; // How much boat follows wave angle
    public float RotationSpeed = 3f; // How fast boat rotates to match waves

    [Header("Physics Mode")]
    public bool UseRealWaves = true; // FALSE = flat water with shader only
    public float FlatWaterHeight = 0f; // Water Y position when UseRealWaves = false

    [Header("Smoothing")]
    public float RotationSmoothTime = 0.3f;
    public float MaxRotationSpeed = 50f;

    // Components
    protected Rigidbody Rigidbody;
    protected Waves Waves;

    // Water line data
    protected float[] FloatPointHeights; // Individual height for each float point
    protected Vector3[] WaterLinePoints;
    protected Vector3 TargetUp;
    protected Vector3 smoothVectorRotation;
    protected Vector3 centerOffset;

    public Vector3 Center { get { return transform.position + centerOffset; } }

    void Awake()
    {
        if (UseRealWaves)
        {
            Waves = FindObjectOfType<Waves>();
        }

        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.useGravity = false;
        Rigidbody.centerOfMass = new Vector3(0, -0.5f, 0);

        // Initialize arrays
        WaterLinePoints = new Vector3[FloatPoints.Length];
        FloatPointHeights = new float[FloatPoints.Length];

        for (int i = 0; i < FloatPoints.Length; i++)
            WaterLinePoints[i] = FloatPoints[i].position;

        centerOffset = PhysicsHelper.GetCenter(WaterLinePoints) - transform.position;
    }

    void FixedUpdate()
    {
        if (FloatPoints == null || FloatPoints.Length == 0)
            return;

        if (UseRealWaves && Waves == null)
        {
            Waves = FindObjectOfType<Waves>();
            if (Waves == null) return;
        }

        // Calculate water heights at each float point
        CalculateFloatPointHeights();

        // Apply forces at each float point independently
        ApplyBuoyancyForces();

        // Rotate boat to match wave slope
        if (FollowWaveSlope)
        {
            ApplyWaveRotation();
        }
    }

    private void CalculateFloatPointHeights()
    {
        for (int i = 0; i < FloatPoints.Length; i++)
        {
            Vector3 floatPointPos = FloatPoints[i].position;

            if (UseRealWaves && Waves != null)
            {
                // Get actual wave height at this point
                FloatPointHeights[i] = Waves.GetHeight(floatPointPos);
            }
            else
            {
                // Use flat water
                FloatPointHeights[i] = FlatWaterHeight;
            }

            WaterLinePoints[i] = new Vector3(
                floatPointPos.x,
                FloatPointHeights[i],
                floatPointPos.z
            );
        }
    }

    private void ApplyBuoyancyForces()
    {
        Rigidbody.drag = AirDrag;
        bool anyPointUnderwater = false;

        // Apply buoyancy force at EACH float point individually
        for (int i = 0; i < FloatPoints.Length; i++)
        {
            Vector3 floatPointPos = FloatPoints[i].position;
            float waterHeight = FloatPointHeights[i];
            float depth = waterHeight - floatPointPos.y;

            if (depth > 0) // Float point is underwater
            {
                anyPointUnderwater = true;

                // Calculate buoyancy force for this specific point
                float forceMagnitude = BuoyancyForce * Mathf.Clamp01(depth / DepthBeforeSubmerged);

                // Apply force at this specific float point position
                Vector3 buoyancyForce = Vector3.up * forceMagnitude;
                Rigidbody.AddForceAtPosition(buoyancyForce, floatPointPos, ForceMode.Acceleration);

                // Apply local damping to reduce oscillation
                Vector3 pointVelocity = Rigidbody.GetPointVelocity(floatPointPos);
                Vector3 dampingForce = -pointVelocity * BuoyancyDamping;
                Rigidbody.AddForceAtPosition(dampingForce, floatPointPos, ForceMode.Acceleration);
            }
        }

        if (anyPointUnderwater)
        {
            Rigidbody.drag = WaterDrag;
        }
        else
        {
            // Apply gravity when above water
            Rigidbody.AddForce(Physics.gravity, ForceMode.Acceleration);
        }
    }

    private void ApplyWaveRotation()
    {
        // Calculate the normal vector from float points
        TargetUp = PhysicsHelper.GetNormal(WaterLinePoints);

        // Apply rotation influence
        TargetUp = Vector3.Slerp(Vector3.up, TargetUp, WaveSlopeInfluence);

        // Smooth the rotation
        Vector3 smoothUp = Vector3.SmoothDamp(
            transform.up,
            TargetUp,
            ref smoothVectorRotation,
            RotationSmoothTime,
            MaxRotationSpeed
        );

        // Apply rotation
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, smoothUp) * Rigidbody.rotation;
        Rigidbody.MoveRotation(Quaternion.Slerp(
            Rigidbody.rotation,
            targetRotation,
            RotationSpeed * Time.fixedDeltaTime
        ));

        // Dampen angular velocity
        Rigidbody.angularVelocity *= 0.95f;
    }

    private void OnDrawGizmos()
    {
        if (FloatPoints == null || FloatPoints.Length == 0)
            return;

        // Initialize Waves reference if needed
        if (UseRealWaves && Waves == null && Application.isPlaying)
        {
            Waves = FindObjectOfType<Waves>();
        }

        for (int i = 0; i < FloatPoints.Length; i++)
        {
            if (FloatPoints[i] == null)
                continue;

            Vector3 floatPos = FloatPoints[i].position;

            // Draw float point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(floatPos, 0.1f);

            // Draw water level
            float waterHeight = FlatWaterHeight;
            if (UseRealWaves && Waves != null)
            {
                try
                {
                    waterHeight = Waves.GetHeight(floatPos);
                }
                catch { }
            }

            Vector3 waterPoint = new Vector3(floatPos.x, waterHeight, floatPos.z);

            // Color based on submersion
            bool isUnderwater = floatPos.y < waterHeight;
            Gizmos.color = isUnderwater ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(floatPos, waterPoint);

            Gizmos.color = Color.blue;
            Gizmos.DrawCube(waterPoint, Vector3.one * 0.15f);
        }

        // Draw center of mass
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(Center, 0.3f);

            // Draw up vector
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, TargetUp * 2f);
        }

        // Draw boat orientation plane
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 center = transform.position;
        Vector3 right = transform.right * 2f;
        Vector3 forward = transform.forward * 3f;

        Gizmos.DrawLine(center - right - forward, center + right - forward);
        Gizmos.DrawLine(center + right - forward, center + right + forward);
        Gizmos.DrawLine(center + right + forward, center - right + forward);
        Gizmos.DrawLine(center - right + forward, center - right - forward);
    }
}