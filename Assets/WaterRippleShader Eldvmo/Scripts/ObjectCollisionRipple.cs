using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eldvmo.Ripples
{
    [RequireComponent(typeof(Rigidbody))]
    public class ObjectCollisionRipple : MonoBehaviour
    {
        [Header("Ripple Settings")]
        [SerializeField] private MeshRenderer ripplePlane;   // Water plane with ripple shader
        [SerializeField] private Collider waterTrigger;      // Trigger collider marking water area
        [SerializeField] private bool isFloatingWithWater = true;
        [SerializeField] private float moveUpHeight = 2f;

        private Collider ripplePlaneCollider;
        private Rigidbody rb;

        private bool isInWater = false;
        private Vector4[] ripplePoints = new Vector4[10];
        private int rippleIndex = 0;

        private Vector2 _oldInputCentre;
        private int waterLayerMask;

        void Start()
        {
            ripplePlaneCollider = ripplePlane.GetComponent<Collider>();
            waterLayerMask = LayerMask.GetMask("Water");
            rb = GetComponent<Rigidbody>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (ripplePlaneCollider != null && other == waterTrigger)
            {
                isInWater = true;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (ripplePlaneCollider != null && other == waterTrigger)
            {
                isInWater = false;
            }
        }

        void FixedUpdate()
        {
            if (!isInWater) return;

            // Raycast from slightly above the object downward to the water plane
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = Vector3.down;

            Ray ray = new Ray(origin, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 2f, waterLayerMask))
            {
                // === WORLD-SPACE VERSION ===
                // Use world XZ position of the hit point instead of UVs
                Vector2 worldXZ = new Vector2(hit.point.x, hit.point.z);

                // Prevent overly frequent ripples
                if (Vector2.Distance(_oldInputCentre, worldXZ) < 0.5f)
                    return;

                ripplePoints[rippleIndex] = new Vector4(worldXZ.x, worldXZ.y, Time.time, 0);
                rippleIndex = (rippleIndex + 1) % ripplePoints.Length;
                _oldInputCentre = worldXZ;

                // Send ripple data to material
                ripplePlane.material.SetVectorArray("_InputCentre", ripplePoints);

                // Optional floating effect
                if (isFloatingWithWater)
                {
                    SetObjectHeight(hit.point.y + moveUpHeight);
                    rb.useGravity = false;
                    StartCoroutine(EnableGravity());
                }
            }
        }

        private void SetObjectHeight(float targetHeight)
        {
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.Lerp(currentPos.y, targetHeight, Time.fixedDeltaTime * 0.5f);
            transform.position = currentPos;
        }

        // Re-enable gravity after a short delay to simulate bobbing
        private IEnumerator EnableGravity()
        {
            yield return new WaitForSeconds(0.5f);
            rb.useGravity = true;
        }
    }
}
