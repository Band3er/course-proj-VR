using UnityEngine;

namespace ForestArchery.Wildlife
{
    public sealed class WildlifeHitbox : MonoBehaviour
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

        private void OnCollisionEnter(Collision collision)
        {
            if (animal == null || collision == null)
            {
                return;
            }

            ArrowController arrow =
                collision.collider.GetComponentInParent<ArrowController>();

            if (arrow == null)
            {
                arrow =
                    collision.gameObject
                        .GetComponentInParent<ArrowController>();
            }

            if (arrow == null)
            {
                return;
            }

            Vector3 hitPoint =
                collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : transform.position;

            animal.TryReceiveArrowHit(
                scoreMultiplier,
                hitLabel,
                hitPoint,
                collision.relativeVelocity);
        }
    }
}