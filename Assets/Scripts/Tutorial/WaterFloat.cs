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
    public bool AttachToSurface = false;
    public Transform[] FloatPoints;

    [Header("Buoyancy Settings")]
    public float BuoyancyForce = 15f; // Increased for more responsive floating
    public float BuoyancyDamping = 0.05f; // Smoothing factor
    public float SubmergedVolumeMultiplier = 1.5f; // How much force increases with depth

    [Header("Rotation Smoothing")]
    public float RotationSmoothTime = 0.3f; // Faster response
    public float MaxRotationSpeed = 50f; // Prevent extreme tilting

    [Header("Performance")]
    public bool useFastPhysics = true; // Use optimized calculations

    // Components
    protected Rigidbody Rigidbody;
    protected Waves Waves;

    // Water line
    protected float WaterLine;
    protected Vector3[] WaterLinePoints;

    // Smooth rotation
    protected Vector3 smoothVectorRotation;
    protected Vector3 TargetUp;
    protected Vector3 centerOffset;

    // Velocity smoothing for natural feel
    private Vector3 lastVelocity;
    private float lastWaterLine;
    private Vector3 smoothForce;

    public Vector3 Center { get { return transform.position + centerOffset; } }

    void Awake()
    {
        Waves = FindObjectOfType<Waves>();
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.useGravity = false;

        // Improved mass distribution
        Rigidbody.centerOfMass = new Vector3(0, -0.5f, 0); // Lower center of mass for stability

        WaterLinePoints = new Vector3[FloatPoints.Length];
        for (int i = 0; i < FloatPoints.Length; i++)
            WaterLinePoints[i] = FloatPoints[i].position;
        centerOffset = PhysicsHelper.GetCenter(WaterLinePoints) - transform.position;

        lastVelocity = Vector3.zero;
        lastWaterLine = 0f;
        smoothForce = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (Waves == null || FloatPoints == null || FloatPoints.Length == 0)
            return;

        float newWaterLine = 0f;
        bool pointUnderWater = false;
        int underwaterPoints = 0;

        // Calculate water line for each float point
        for (int i = 0; i < FloatPoints.Length; i++)
        {
            WaterLinePoints[i] = FloatPoints[i].position;
            WaterLinePoints[i].y = Waves.GetHeight(FloatPoints[i].position);
            newWaterLine += WaterLinePoints[i].y / FloatPoints.Length;

            if (WaterLinePoints[i].y > FloatPoints[i].position.y)
            {
                pointUnderWater = true;
                underwaterPoints++;
            }
        }

        // Smooth water line changes for stability
        float waterLineDelta = newWaterLine - lastWaterLine;
        lastWaterLine = Mathf.Lerp(lastWaterLine, newWaterLine, BuoyancyDamping);
        WaterLine = lastWaterLine;

        // Calculate up vector from water surface
        TargetUp = PhysicsHelper.GetNormal(WaterLinePoints);

        // Apply physics based on submersion
        ApplyBuoyancy(pointUnderWater, underwaterPoints, waterLineDelta);
        ApplyRotation(pointUnderWater);

        // Smooth velocity changes for natural movement
        lastVelocity = Rigidbody.velocity;
    }

    private void ApplyBuoyancy(bool pointUnderWater, int underwaterPoints, float waterLineDelta)
    {
        Vector3 gravity = Physics.gravity;
        float submersionRatio = (float)underwaterPoints / FloatPoints.Length;

        Rigidbody.drag = AirDrag;

        if (WaterLine > Center.y)
        {
            Rigidbody.drag = WaterDrag;

            if (AttachToSurface)
            {
                // Attach to water surface (simpler mode)
                Rigidbody.position = new Vector3(
                    Rigidbody.position.x,
                    WaterLine - centerOffset.y,
                    Rigidbody.position.z
                );
            }
            else
            {
                // Enhanced buoyancy system
                float depth = WaterLine - Center.y;
                float depthFactor = Mathf.Clamp01(depth * SubmergedVolumeMultiplier);

                // Calculate buoyancy force
                Vector3 buoyancyDirection = AffectDirection ? TargetUp : Vector3.up;
                float buoyancyMagnitude = BuoyancyForce * depthFactor * submersionRatio;

                // Smooth force application
                Vector3 targetForce = buoyancyDirection * buoyancyMagnitude;
                smoothForce = Vector3.Lerp(smoothForce, targetForce, BuoyancyDamping * 10f);

                // Apply forces
                Rigidbody.AddForce(smoothForce, ForceMode.Acceleration);

                // Counter-gravity
                float gravityCounter = Mathf.Clamp(Mathf.Abs(depth), 0, 1);
                Rigidbody.AddForce(-gravity * gravityCounter, ForceMode.Acceleration);

                // Dampen vertical velocity when near equilibrium
                if (Mathf.Abs(depth) < 0.3f)
                {
                    Vector3 vel = Rigidbody.velocity;
                    vel.y *= 0.9f; // Dampen vertical bobbing
                    Rigidbody.velocity = vel;
                }
            }
        }
        else
        {
            // Above water - apply normal gravity
            Rigidbody.AddForce(gravity, ForceMode.Acceleration);
            smoothForce = Vector3.Lerp(smoothForce, Vector3.zero, 0.1f);
        }
    }

    private void ApplyRotation(bool pointUnderWater)
    {
        if (pointUnderWater)
        {
            // Smoother rotation matching water surface
            TargetUp = Vector3.SmoothDamp(
                transform.up,
                TargetUp,
                ref smoothVectorRotation,
                RotationSmoothTime
            );

            // Calculate target rotation
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, TargetUp) * Rigidbody.rotation;

            // Apply with max speed limit
            float maxRadiansDelta = MaxRotationSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Rigidbody.rotation = Quaternion.RotateTowards(
                Rigidbody.rotation,
                targetRotation,
                maxRadiansDelta * Mathf.Rad2Deg
            );

            // Dampen angular velocity for stability
            Rigidbody.angularVelocity *= 0.95f;
        }
    }

    private void OnDrawGizmos()
    {
        if (FloatPoints == null || FloatPoints.Length == 0)
            return;

        Gizmos.color = Color.green;

        if (Waves == null)
        {
            Waves = FindObjectOfType<Waves>();
        }

        float averageWaterLine = 0f;
        int validPoints = 0;

        for (int i = 0; i < FloatPoints.Length; i++)
        {
            if (FloatPoints[i] == null)
                continue;

            // Draw float points
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(FloatPoints[i].position, 0.1f);

            if (Waves != null)
            {
                try
                {
                    float waterHeight = Waves.GetHeight(FloatPoints[i].position);
                    Vector3 waterPoint = new Vector3(
                        FloatPoints[i].position.x,
                        waterHeight,
                        FloatPoints[i].position.z
                    );

                    averageWaterLine += waterHeight;
                    validPoints++;

                    // Draw water level
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(waterPoint, Vector3.one * 0.2f);

                    // Draw connection line
                    Gizmos.color = FloatPoints[i].position.y < waterHeight ? Color.cyan : Color.yellow;
                    Gizmos.DrawLine(FloatPoints[i].position, waterPoint);
                }
                catch (System.Exception)
                {
                    continue;
                }
            }
        }

        // Draw center and water line
        if (validPoints > 0 && Waves != null)
        {
            averageWaterLine /= validPoints;

            Vector3 center = Vector3.zero;
            int centerPoints = 0;
            for (int i = 0; i < FloatPoints.Length; i++)
            {
                if (FloatPoints[i] != null)
                {
                    center += FloatPoints[i].position;
                    centerPoints++;
                }
            }
            if (centerPoints > 0) center /= centerPoints;

            // Draw average water line
            Gizmos.color = Color.red;
            Vector3 waterLineCenter = new Vector3(center.x, averageWaterLine, center.z);
            Gizmos.DrawCube(waterLineCenter, Vector3.one * 0.5f);

            // Draw up direction
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(waterLineCenter, TargetUp * 2f);
            }

            // Draw water plane
            DrawWaterPlaneGizmo(center, averageWaterLine, 3f);

            // Draw center of mass
            if (Application.isPlaying)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(Center, 0.3f);
            }
        }
    }

    private void DrawWaterPlaneGizmo(Vector3 center, float waterHeight, float size)
    {
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);

        Vector3 corner1 = new Vector3(center.x - size, waterHeight, center.z - size);
        Vector3 corner2 = new Vector3(center.x + size, waterHeight, center.z - size);
        Vector3 corner3 = new Vector3(center.x + size, waterHeight, center.z + size);
        Vector3 corner4 = new Vector3(center.x - size, waterHeight, center.z + size);

        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);

        Gizmos.DrawLine(corner1, corner3);
        Gizmos.DrawLine(corner2, corner4);
    }
}