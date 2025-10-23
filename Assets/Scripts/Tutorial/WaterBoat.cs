using Xinyee;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WaterFloat))]
public class WaterBoat : MonoBehaviour
{
    //visible Properties
    public Transform Motor;
    public float SteerPower = 500f;
    public float Power = 5f;
    public float MaxSpeed = 10f;
    public float Drag = 0.1f;
    public float TurnStopPower = 5f;

    // New turning speed control parameters
    public float MaxTurnSpeed = 2f; // Maximum turning speed in radians per second
    public float MinTurnSpeed = 0.5f; // Minimum turning speed factor
    public float TurnAcceleration = 1f; // How quickly turning ramps up

    //used Components
    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;
    protected ParticleSystem ParticleSystem;
    protected Camera Camera;

    //internal Properties
    protected Vector3 CamVel;
    protected float currentTurnInput; // Smooth turning input

    public void Awake()
    {
        ParticleSystem = GetComponentInChildren<ParticleSystem>();
        Rigidbody = GetComponent<Rigidbody>();
        StartRotation = Motor.localRotation;
        Camera = Camera.main;
    }

    public void FixedUpdate()
    {
        //default direction
        var forceDirection = transform.forward;
        float targetSteer = 0f;

        //steer direction [-1,0,1]
        if (Input.GetKey(KeyCode.D))
            targetSteer = 1;
        if (Input.GetKey(KeyCode.A))
            targetSteer = -1;

        // Smooth turning input
        currentTurnInput = Mathf.MoveTowards(currentTurnInput, targetSteer, TurnAcceleration * Time.fixedDeltaTime);

        // --- Turning Behavior ---
        if (targetSteer != 0)
        {
            // Apply turning torque when keys are pressed
            if (Rigidbody.velocity.magnitude < 1f)
                currentTurnInput *= Rigidbody.velocity.magnitude; // less steering when almost stationary

            float turnTorque = currentTurnInput * SteerPower * Mathf.Clamp01(Rigidbody.velocity.magnitude / MaxSpeed);
            Rigidbody.AddTorque(Vector3.up * turnTorque, ForceMode.Acceleration);

            // Limit maximum turning speed
            Vector3 angularVel = Rigidbody.angularVelocity;
            angularVel.y = Mathf.Clamp(angularVel.y, -MaxTurnSpeed, MaxTurnSpeed);
            Rigidbody.angularVelocity = angularVel;
        }
        else
        {
            // Reset turning input when no keys pressed
            currentTurnInput = 0f;

            // When no keys pressed, gradually stop turning
            Vector3 counterTorque = -Rigidbody.angularVelocity * TurnStopPower;
            Rigidbody.AddTorque(counterTorque, ForceMode.Acceleration);
        }

        //compute vectors
        var forward = Vector3.Scale(new Vector3(1, 0, 1), transform.forward);
        var targetVel = Vector3.zero;

        //forward/backward power
        if (Input.GetKey(KeyCode.W))
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed, Power);
        if (Input.GetKey(KeyCode.S))
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * -MaxSpeed, Power);

        // --- Drift Simulation ---
        Vector3 localVel = transform.InverseTransformDirection(Rigidbody.velocity);

        // reduce sideways velocity gradually (simulates water resistance)
        localVel.x *= 0.9f; // smaller = more drift; 0.5f = very slippery, 0.95f = tight control

        // apply damping
        Rigidbody.velocity = transform.TransformDirection(localVel);

        //Motor Animation // Particle system
        Motor.SetPositionAndRotation(Motor.position, transform.rotation * StartRotation * Quaternion.Euler(0, 30f * currentTurnInput, 0));
        if (ParticleSystem != null)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
                ParticleSystem.Play();
            else
                ParticleSystem.Pause();
        }

        //moving forward
        var movingForward = Vector3.Cross(transform.forward, Rigidbody.velocity).y < 0;

        //move in direction
        Rigidbody.velocity = Quaternion.AngleAxis(Vector3.SignedAngle(Rigidbody.velocity, (movingForward ? 1f : 0f) * transform.forward, Vector3.up) * Drag, Vector3.up) * Rigidbody.velocity;

        //camera position
        //Camera.transform.LookAt(transform.position + transform.forward * 6f + transform.up * 2f);
        //Camera.transform.position = Vector3.SmoothDamp(Camera.transform.position, transform.position + transform.forward * -8f + transform.up * 2f, ref CamVel, 0.05f);
    }
}