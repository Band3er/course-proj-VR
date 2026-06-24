using UnityEngine;

namespace ForestArchery.Wildlife
{
    public sealed class LivingBirdColliderRelay : MonoBehaviour
    {
        [SerializeField] private LivingBirdArrowTarget target;

        public void Configure(
            LivingBirdArrowTarget configuredTarget)
        {
            target = configuredTarget;
        }

        private void Awake()
        {
            if (target == null)
            {
                target =
                    GetComponentInParent
                        <LivingBirdArrowTarget>();
            }
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (
                target == null ||
                other == null
            )
            {
                return;
            }

            Rigidbody body =
                other.attachedRigidbody;

            Vector3 velocity =
                body != null
                    ? body.linearVelocity
                    : Vector3.zero;

            target.TryHandleArrow(
                other,
                other.ClosestPoint(
                    transform.position),
                velocity);
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            if (
                target == null ||
                collision == null ||
                collision.collider == null
            )
            {
                return;
            }

            Vector3 point =
                collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : transform.position;

            target.TryHandleArrow(
                collision.collider,
                point,
                collision.relativeVelocity);
        }
    }
}
