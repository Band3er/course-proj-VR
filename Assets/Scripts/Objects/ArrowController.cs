using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Rigidbody))]
public class ArrowController : MonoBehaviour
{
    private Rigidbody arrowRigidbody;
    private bool isNocked;
    private bool isLaunched;
    private bool hasHit;

    private void Awake()
    {
        arrowRigidbody = GetComponent<Rigidbody>();

        if (arrowRigidbody == null)
        {
            Debug.LogError("[ArrowController] Missing Rigidbody.");
        }
    }

    private void Update()
    {
        if (!isLaunched || hasHit || arrowRigidbody == null)
        {
            return;
        }

        Vector3 velocity = GetVelocity();

        if (velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }
    }

    public void SetNocked(Transform nockPoint)
    {
        isNocked = true;
        isLaunched = false;
        hasHit = false;

        if (arrowRigidbody != null)
        {
            arrowRigidbody.isKinematic = true;
            arrowRigidbody.useGravity = false;
            SetVelocity(Vector3.zero);
            arrowRigidbody.angularVelocity = Vector3.zero;
        }

        if (nockPoint != null)
        {
            transform.position = nockPoint.position;
            transform.rotation = nockPoint.rotation;
            transform.SetParent(nockPoint, true);
        }

        Debug.Log("[ArrowController] Arrow nocked.");
    }

    public void Launch(Vector3 direction, float force)
    {
        if (arrowRigidbody == null)
        {
            Debug.LogWarning("[ArrowController] Cannot launch: Rigidbody is missing.");
            return;
        }

        transform.SetParent(null, true);

        isNocked = false;
        isLaunched = true;
        hasHit = false;

        arrowRigidbody.isKinematic = false;
        arrowRigidbody.useGravity = true;
        arrowRigidbody.angularVelocity = Vector3.zero;

        Vector3 launchVelocity = direction.normalized * force;
        SetVelocity(launchVelocity);

        Debug.Log($"[ArrowController] Arrow launched. Force={force:F2}, Direction={direction}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || !isLaunched)
        {
            return;
        }

        hasHit = true;
        isLaunched = false;

        if (arrowRigidbody != null)
        {
            SetVelocity(Vector3.zero);
            arrowRigidbody.angularVelocity = Vector3.zero;
            arrowRigidbody.isKinematic = true;
            arrowRigidbody.useGravity = false;
        }

        transform.SetParent(collision.transform, true);

        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log("[ArrowController] HIT TARGET.");
        }
        else
        {
            Debug.Log("[ArrowController] Arrow hit: " + collision.gameObject.name);
        }
    }

    private Vector3 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return arrowRigidbody.linearVelocity;
#else
        return arrowRigidbody.velocity;
#endif
    }

    private void SetVelocity(Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        arrowRigidbody.linearVelocity = value;
#else
        arrowRigidbody.velocity = value;
#endif
    }
}