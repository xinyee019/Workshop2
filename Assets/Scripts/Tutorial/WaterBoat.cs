using Ditzelgames;
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

    //used Components
    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;
    protected ParticleSystem ParticleSystem;
    protected Camera Camera;

    //internal Properties
    protected Vector3 CamVel;


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
        float steer = 0f;

        //steer direction [-1,0,1]
        if (Input.GetKey(KeyCode.D))
            steer = 1;
        if (Input.GetKey(KeyCode.A))
            steer = -1;


        //Rotational Force
        //Rigidbody.AddForceAtPosition(steer * transform.right * SteerPower / 100f, Motor.position);

        if (Rigidbody.velocity.magnitude < 1f)
            steer *= Rigidbody.velocity.magnitude; // less steering when almost stationary


        // --- Smooth Turning via Torque ---
        float turnTorque = steer * SteerPower * Mathf.Clamp01(Rigidbody.velocity.magnitude / MaxSpeed);
        Rigidbody.AddTorque(Vector3.up * turnTorque, ForceMode.Acceleration);

        // Prevent endless spin-up
        Rigidbody.angularVelocity = Vector3.ClampMagnitude(Rigidbody.angularVelocity, 1.5f);



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
        Motor.SetPositionAndRotation(Motor.position, transform.rotation * StartRotation * Quaternion.Euler(0, 30f * steer, 0));
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