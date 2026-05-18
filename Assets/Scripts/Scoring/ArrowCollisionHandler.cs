using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ArrowCollisionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExperimentManager experimentManager;

    [Header("Collision Behaviour")]
    [SerializeField] private bool stickOnCollision = true;
    [SerializeField] private bool registerAnyNonTargetCollisionAsMiss = true;

    private Rigidbody arrowRigidbody;
    private bool hasCollided;

    private void Awake()
    {
        arrowRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        hasCollided = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided)
        {
            return;
        }

        hasCollided = true;

        Vector3 hitPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        TargetScoring targetScoring = collision.collider.GetComponentInParent<TargetScoring>();

        if (experimentManager != null)
        {
            if (targetScoring != null)
            {
                experimentManager.RegisterTargetHit(hitPoint);
            }
            else if (registerAnyNonTargetCollisionAsMiss)
            {
                experimentManager.RegisterMiss(hitPoint, $"Collision with {collision.collider.name}.");
            }
        }
        else
        {
            Debug.LogWarning("[ArrowCollisionHandler] ExperimentManager is not assigned.");
        }

        if (stickOnCollision)
        {
            StickTo(collision.transform);
        }
    }

    private void StickTo(Transform parent)
    {
        if (arrowRigidbody != null)
        {
            arrowRigidbody.isKinematic = true;
            arrowRigidbody.useGravity = false;
        }

        transform.SetParent(parent, true);
    }

    public void AssignExperimentManager(ExperimentManager manager)
    {
        experimentManager = manager;
    }

    public void ResetCollisionState()
    {
        hasCollided = false;

        if (arrowRigidbody != null)
        {
            arrowRigidbody.isKinematic = false;
            arrowRigidbody.useGravity = true;
        }
    }
}
