using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatFloatStable : MonoBehaviour
{
    [Header("Water Reference")]
    public Transform waterPlane;

    [Header("Float Settings")]
    public Transform[] floatPoints;
    public float floatPower = 1f;         // multiplier for buoyant force
    public float waterDrag = 2f;
    public float waterAngularDrag = 1.5f;
    public float damping = 0.3f;
    public bool autoBalanceLift = true;

    private Rigidbody rb;
    private float smoothVelocityY;
    private float baseLiftPerPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (autoBalanceLift && floatPoints.Length > 0)
        {
            // Base lift ensures it can counter gravity at equilibrium
            baseLiftPerPoint = (rb.mass * Physics.gravity.magnitude) / floatPoints.Length;
        }
    }

    private void FixedUpdate()
    {
        ApplyBuoyancy();
    }

    private void ApplyBuoyancy()
    {
        if (waterPlane == null || floatPoints.Length == 0)
            return;

        float totalLift = 0f;
        int submergedCount = 0;

        foreach (Transform p in floatPoints)
        {
            float waterHeight = waterPlane.position.y;
            float depth = waterHeight - p.position.y;

            if (depth > 0f)
            {
                submergedCount++;

                // smoother lift curve (weaker near surface)
                float lift = baseLiftPerPoint * floatPower * Mathf.Clamp01(depth);
                totalLift += lift;
                rb.AddForceAtPosition(Vector3.up * lift, p.position, ForceMode.Force);
            }
        }

        // Apply water resistance if touching water
        if (submergedCount > 0)
        {
            float subRatio = (float)submergedCount / floatPoints.Length;
            rb.drag = Mathf.Lerp(rb.drag, waterDrag * subRatio, Time.fixedDeltaTime * 2f);
            rb.angularDrag = Mathf.Lerp(rb.angularDrag, waterAngularDrag * subRatio, Time.fixedDeltaTime * 2f);

            // Smooth vertical velocity to avoid small oscillations
            rb.velocity = new Vector3(
                rb.velocity.x,
                Mathf.SmoothDamp(rb.velocity.y, 0, ref smoothVelocityY, 0.3f),
                rb.velocity.z
            );
        }
        else
        {
            rb.drag = Mathf.Lerp(rb.drag, 0.05f, Time.fixedDeltaTime);
            rb.angularDrag = Mathf.Lerp(rb.angularDrag, 0.05f, Time.fixedDeltaTime);
        }

        Debug.Log($"[Boat Debug] Mass={rb.mass:F1} | LiftPerPoint={baseLiftPerPoint:F2} | TotalLift={totalLift:F2} | SubPoints={submergedCount} | VelY={rb.velocity.y:F2}");
    }
}
