using UnityEngine;

public class ArrowController : MonoBehaviour
{
    private Rigidbody rb;
    private bool isNocked = false;
    private bool isLaunched = false;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Sageata se roteste in directia de zbor (realista)
        if (isLaunched && !hasHit && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    public void SetNocked(bool nocked)
    {
        isNocked = nocked;
        rb.isKinematic = true;
    }

    public void Launch(Vector3 direction)
    {
        isLaunched = true;
        isNocked = false;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;
        isLaunched = false;

        // Ingheata sageata unde a lovit
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Verifica daca a lovit tinta
        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log("HIT TARGET!");
            // Poti notifica ExperimentManager aici
        }
    }
}