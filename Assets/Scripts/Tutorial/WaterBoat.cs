using Xinyee;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WaterFloat))]
public class WaterBoat : MonoBehaviour
{
    [Header("Movement")]
    public Transform Motor;
    public float Power = 5f;
    public float MaxSpeed = 10f;
    public float Acceleration = 2f; // How quickly boat reaches max speed
    public float Deceleration = 3f; // How quickly boat slows down

    [Header("Steering")]
    public float SteerPower = 500f;
    public float MaxTurnSpeed = 2f; // Maximum turning speed in radians per second
    public float MinTurnSpeed = 0.5f; // Minimum turning speed when slow
    public float TurnAcceleration = 3f; // How quickly turning responds
    public float TurnStopPower = 5f; // How quickly boat stops turning

    [Header("Water Resistance")]
    public float WaterDrag = 0.1f; // Forward/backward drag
    public float LateralDrag = 0.85f; // Sideways resistance (higher = less drift, 0.5-0.95)
    public float AngularDrag = 2f; // Rotation resistance

    [Header("Advanced Physics")]
    public float WakeEffectMultiplier = 1.2f; // Amplifies movement feeling
    public float SpeedTurnInfluence = 0.7f; // How much speed affects turning
    public bool EnableInertia = true; // More realistic movement
    public float InertiaDamping = 0.95f; // How much to preserve momentum

    [Header("Effects")]
    public ParticleSystem WakeParticles;
    public float MotorTiltAngle = 30f;

    // Components
    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;
    protected Camera Camera;

    // Input smoothing
    protected float currentThrottle = 0f;
    protected float currentTurnInput = 0f;
    protected Vector3 CamVel;

    // Enhanced physics
    private Vector3 lastForward;
    private float currentSpeed;

    public void Awake()
    {
        if (WakeParticles == null)
            WakeParticles = GetComponentInChildren<ParticleSystem>();

        Rigidbody = GetComponent<Rigidbody>();
        StartRotation = Motor.localRotation;
        Camera = Camera.main;

        lastForward = transform.forward;

        // Set up rigidbody for boat physics
        Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void FixedUpdate()
    {
        HandleInput();
        ApplyMovement();
        ApplyTurning();
        ApplyWaterResistance();
        UpdateMotorAndEffects();
    }

    private void HandleInput()
    {
        // Get input
        float throttleInput = 0f;
        float steerInput = 0f;

        if (Input.GetKey(KeyCode.W)) throttleInput = 1f;
        if (Input.GetKey(KeyCode.S)) throttleInput = -1f;
        if (Input.GetKey(KeyCode.D)) steerInput = 1f;
        if (Input.GetKey(KeyCode.A)) steerInput = -1f;

        // Smooth throttle for natural acceleration/deceleration
        float throttleSpeed = throttleInput != 0 ? Acceleration : Deceleration;
        currentThrottle = Mathf.MoveTowards(
            currentThrottle,
            throttleInput,
            throttleSpeed * Time.fixedDeltaTime
        );

        // Smooth steering with faster response
        currentTurnInput = Mathf.MoveTowards(
            currentTurnInput,
            steerInput,
            TurnAcceleration * Time.fixedDeltaTime
        );

        // Calculate current speed
        currentSpeed = Vector3.Dot(Rigidbody.velocity, transform.forward);
    }

    private void ApplyMovement()
    {
        if (Mathf.Abs(currentThrottle) > 0.01f)
        {
            // Calculate target velocity
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 targetVelocity = forward * MaxSpeed * currentThrottle;

            // Apply force to reach target velocity
            PhysicsHelper.ApplyForceToReachVelocity(
                Rigidbody,
                targetVelocity,
                Power * WakeEffectMultiplier
            );

            // Add slight upward force when moving for wave riding effect
            if (currentSpeed > 1f)
            {
                float upwardBoost = Mathf.Abs(currentSpeed) * 0.1f;
                Rigidbody.AddForce(Vector3.up * upwardBoost, ForceMode.Acceleration);
            }
        }
        else if (EnableInertia)
        {
            // Gradual slowdown with inertia
            Vector3 horizontalVelocity = Rigidbody.velocity;
            horizontalVelocity.y = 0;
            Rigidbody.velocity = new Vector3(
                horizontalVelocity.x * InertiaDamping,
                Rigidbody.velocity.y,
                horizontalVelocity.z * InertiaDamping
            );
        }
    }

    private void ApplyTurning()
    {
        if (Mathf.Abs(currentTurnInput) > 0.01f)
        {
            // Speed-based turning (slower at low speeds, responsive at high speeds)
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / MaxSpeed);
            speedFactor = Mathf.Lerp(MinTurnSpeed, 1f, speedFactor * SpeedTurnInfluence);

            // Apply turning torque
            float turnTorque = currentTurnInput * SteerPower * speedFactor;
            Rigidbody.AddTorque(Vector3.up * turnTorque, ForceMode.Acceleration);

            // Limit maximum turn speed
            Vector3 angularVel = Rigidbody.angularVelocity;
            float maxTurnAtSpeed = MaxTurnSpeed * (0.5f + speedFactor * 0.5f);
            angularVel.y = Mathf.Clamp(angularVel.y, -maxTurnAtSpeed, maxTurnAtSpeed);
            Rigidbody.angularVelocity = angularVel;
        }
        else
        {
            // Actively stop turning when no input
            Vector3 counterTorque = -Rigidbody.angularVelocity * TurnStopPower;
            Rigidbody.AddTorque(counterTorque, ForceMode.Acceleration);
        }
    }

    private void ApplyWaterResistance()
    {
        // Forward/backward drag
        Vector3 localVelocity = transform.InverseTransformDirection(Rigidbody.velocity);

        // Apply lateral (sideways) drag - simulates water resistance
        // Higher value = tighter control, lower value = more drift
        localVelocity.x *= LateralDrag;

        // Apply directional drag based on movement direction
        if (currentThrottle < 0.1f) // Not actively accelerating
        {
            localVelocity.z *= (1f - WaterDrag);
        }

        Rigidbody.velocity = transform.TransformDirection(localVelocity);

        // Angular drag for smoother rotation
        Rigidbody.angularVelocity *= (1f - AngularDrag * Time.fixedDeltaTime);

        // Align velocity with forward direction when moving fast
        if (currentSpeed > MaxSpeed * 0.3f)
        {
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 velocity = Rigidbody.velocity;
            float verticalVel = velocity.y;
            velocity.y = 0;

            // Gradually align velocity with forward direction
            float alignmentStrength = WaterDrag * Mathf.Clamp01(currentSpeed / MaxSpeed);
            Vector3 targetDirection = Vector3.Slerp(
                velocity.normalized,
                forward * Mathf.Sign(currentSpeed),
                alignmentStrength
            );

            velocity = targetDirection * velocity.magnitude;
            velocity.y = verticalVel;
            Rigidbody.velocity = velocity;
        }
    }

    private void UpdateMotorAndEffects()
    {
        // Animate motor
        float motorAngle = MotorTiltAngle * currentTurnInput;
        Motor.SetPositionAndRotation(
            Motor.position,
            transform.rotation * StartRotation * Quaternion.Euler(0, motorAngle, 0)
        );

        // Particle effects
        if (WakeParticles != null)
        {
            // Emit based on speed
            var emission = WakeParticles.emission;
            float speedRatio = Mathf.Abs(currentSpeed) / MaxSpeed;

            if (Mathf.Abs(currentThrottle) > 0.1f && speedRatio > 0.1f)
            {
                if (!WakeParticles.isPlaying)
                    WakeParticles.Play();

                // Scale particle emission with speed
                emission.rateOverTime = 10f + speedRatio * 40f;
            }
            else
            {
                if (WakeParticles.isPlaying)
                    WakeParticles.Stop();
            }
        }
    }

    // Optional: Visual debug info
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || Rigidbody == null)
            return;

        // Draw velocity
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Rigidbody.velocity);

        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);

        // Draw turn direction
        if (Mathf.Abs(currentTurnInput) > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Vector3 turnDir = Quaternion.Euler(0, currentTurnInput * 45f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position + Vector3.up, turnDir * 2f);
        }
    }
}