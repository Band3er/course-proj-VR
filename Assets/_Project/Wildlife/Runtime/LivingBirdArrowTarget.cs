using UnityEngine;

namespace ForestArchery.Wildlife
{
    public sealed class LivingBirdArrowTarget : MonoBehaviour
    {
        [SerializeField] private lb_Bird bird;
        [SerializeField] private WildlifeSpeciesDefinition scoreDefinition;
        [SerializeField] private float forceMultiplier = 8f;
        [SerializeField] private float minimumForce = 45f;

        private bool hitAccepted;
        private bool hitboxScaled;

        public bool HitAccepted => hitAccepted;

        public void Configure(
            lb_Bird configuredBird,
            WildlifeSpeciesDefinition definition,
            float configuredForceMultiplier,
            float configuredMinimumForce,
            float hitboxScale)
        {
            bird = configuredBird;
            scoreDefinition = definition;
            forceMultiplier = Mathf.Max(0.1f, configuredForceMultiplier);
            minimumForce = Mathf.Max(1f, configuredMinimumForce);

            if (!hitboxScaled)
            {
                BoxCollider primaryHitbox =
                    GetComponent<BoxCollider>();

                if (primaryHitbox != null)
                {
                    primaryHitbox.size *=
                        Mathf.Max(1f, hitboxScale);
                }

                hitboxScaled = true;
            }
        }

        private void Awake()
        {
            if (bird == null)
            {
                bird = GetComponent<lb_Bird>();
            }
        }

        private void OnEnable()
        {
            hitAccepted = false;
        }

        public void TryHandleArrow(
            Collider other,
            Vector3 hitPoint,
            Vector3 contactVelocity)
        {
            if (
                hitAccepted ||
                other == null ||
                bird == null ||
                !gameObject.activeInHierarchy
            )
            {
                return;
            }

            ArrowController arrow =
                other.GetComponentInParent<ArrowController>();

            if (
                arrow == null &&
                other.attachedRigidbody != null
            )
            {
                arrow =
                    other.attachedRigidbody
                        .GetComponentInParent<ArrowController>();
            }

            if (arrow == null)
            {
                return;
            }

            hitAccepted = true;

            Rigidbody arrowBody =
                other.attachedRigidbody != null
                    ? other.attachedRigidbody
                    : arrow.GetComponent<Rigidbody>();

            Vector3 velocity =
                arrowBody != null
                    ? arrowBody.linearVelocity
                    : contactVelocity;

            Vector3 forceDirection =
                velocity.sqrMagnitude > 0.0001f
                    ? velocity.normalized
                    : arrow.transform.forward;

            float forceMagnitude =
                Mathf.Max(
                    minimumForce,
                    velocity.magnitude * forceMultiplier);

            Vector3 appliedForce =
                forceDirection * forceMagnitude;

            bool targetWasAirborne;

            bool targetWasMoving =
                WildlifeDynamicScore.IsBirdMoving(
                    bird,
                    out targetWasAirborne);

            WildlifeScoreBreakdown dynamicScore =
                WildlifeDynamicScore.Calculate(
                    scoreDefinition,
                    1f,
                    hitPoint,
                    targetWasMoving,
                    targetWasAirborne);

            int awardedScore =
                dynamicScore.finalScore;

            WildlifeScoreManager.Instance?.RegisterHit(
                scoreDefinition,
                awardedScore,
                "Body");

            Debug.Log(
                WildlifeDynamicScore.BuildDebugText(
                    scoreDefinition,
                    dynamicScore));

            bird.KillBirdWithForce(
                appliedForce);

            Debug.Log(
                "[WILDLIFE BIRD] Hit" +
                " | bird=" + gameObject.name +
                " | awarded=" + awardedScore +
                " | point=" + hitPoint +
                " | force=" + appliedForce.magnitude.ToString("F1"));
        }
    }
}
