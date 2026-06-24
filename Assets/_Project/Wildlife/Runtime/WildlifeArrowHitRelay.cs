using UnityEngine;

namespace ForestArchery.Wildlife
{
    public sealed class WildlifeArrowHitRelay : MonoBehaviour
    {
        [SerializeField] private WildlifeAnimal animal;
        [SerializeField] private float scoreMultiplier = 1f;
        [SerializeField] private string hitLabel = "Body";

        public void Configure(
            WildlifeAnimal owner,
            float multiplier,
            string label)
        {
            animal = owner;
            scoreMultiplier = Mathf.Max(0f, multiplier);
            hitLabel = string.IsNullOrWhiteSpace(label)
                ? "Body"
                : label;
        }

        private void Awake()
        {
            if (animal == null)
            {
                animal =
                    GetComponentInParent<WildlifeAnimal>();
            }
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            if (
                collision == null ||
                collision.collider == null
            )
            {
                return;
            }

            Vector3 hitPoint =
                collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : transform.position;

            TryHandleArrow(
                collision.collider,
                hitPoint,
                collision.relativeVelocity);
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (other == null)
            {
                return;
            }

            Rigidbody arrowBody =
                other.attachedRigidbody;

            Vector3 velocity =
                arrowBody != null
                    ? arrowBody.linearVelocity
                    : Vector3.zero;

            TryHandleArrow(
                other,
                other.ClosestPoint(transform.position),
                velocity);
        }

        private void TryHandleArrow(
            Collider other,
            Vector3 hitPoint,
            Vector3 velocity)
        {
            if (animal == null || other == null)
            {
                return;
            }

            ArrowController arrow =
                other.GetComponentInParent<ArrowController>();

            if (arrow == null)
            {
                return;
            }

            Debug.Log(
                "[WILDLIFE HIT RELAY] Arrow contact" +
                " | animal=" +
                animal.name +
                " | collider=" +
                name +
                " | hitbox=" +
                hitLabel);

            animal.TryReceiveArrowHit(
                scoreMultiplier,
                hitLabel,
                hitPoint,
                velocity);
        }
    }
}
