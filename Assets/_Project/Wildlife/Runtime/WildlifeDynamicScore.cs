using UnityEngine;

namespace ForestArchery.Wildlife
{
    public struct WildlifeScoreBreakdown
    {
        public int baseScore;
        public float hitZoneMultiplier;
        public float distanceMeters;
        public float distanceMultiplier;
        public bool targetMoving;
        public bool targetAirborne;
        public float movementMultiplier;
        public int finalScore;
        public string movementLabel;
    }

    public static class WildlifeDynamicScore
    {
        public static WildlifeScoreBreakdown Calculate(
            WildlifeSpeciesDefinition definition,
            float hitZoneMultiplier,
            Vector3 hitPoint,
            bool targetMoving,
            bool targetAirborne)
        {
            int resolvedBaseScore =
                definition != null
                    ? Mathf.Max(0, definition.baseScore)
                    : 0;

            float resolvedHitMultiplier =
                Mathf.Max(0f, hitZoneMultiplier);

            float distanceMeters =
                ResolveDistanceFromPlayer(hitPoint);

            float distanceMultiplier =
                GetDistanceMultiplier(distanceMeters);

            float movementMultiplier = 1f;
            string movementLabel = "STILL";

            if (definition != null)
            {
                if (targetAirborne)
                {
                    movementMultiplier =
                        Mathf.Max(
                            1f,
                            definition.airborneScoreMultiplier);

                    movementLabel = "FLYING";
                }
                else if (targetMoving)
                {
                    movementMultiplier =
                        Mathf.Max(
                            1f,
                            definition.movingScoreMultiplier);

                    movementLabel = "MOVING";
                }
            }

            int finalScore =
                Mathf.RoundToInt(
                    resolvedBaseScore *
                    resolvedHitMultiplier *
                    distanceMultiplier *
                    movementMultiplier);

            return new WildlifeScoreBreakdown
            {
                baseScore = resolvedBaseScore,
                hitZoneMultiplier = resolvedHitMultiplier,
                distanceMeters = distanceMeters,
                distanceMultiplier = distanceMultiplier,
                targetMoving = targetMoving,
                targetAirborne = targetAirborne,
                movementMultiplier = movementMultiplier,
                finalScore = Mathf.Max(0, finalScore),
                movementLabel = movementLabel
            };
        }

        public static float GetDistanceMultiplier(
            float distanceMeters)
        {
            if (distanceMeters < 5f)
            {
                return 1f;
            }

            if (distanceMeters < 10f)
            {
                return 1.10f;
            }

            if (distanceMeters < 15f)
            {
                return 1.25f;
            }

            if (distanceMeters < 20f)
            {
                return 1.40f;
            }

            return 1.55f;
        }

        public static float ResolveDistanceFromPlayer(
            Vector3 hitPoint)
        {
            Camera playerCamera = Camera.main;

            if (playerCamera == null)
            {
                playerCamera =
                    Object.FindFirstObjectByType<Camera>();
            }

            if (playerCamera == null)
            {
                return 0f;
            }

            return Vector3.Distance(
                playerCamera.transform.position,
                hitPoint);
        }

        public static bool IsBirdMoving(
            lb_Bird bird,
            out bool airborne)
        {
            airborne = false;

            if (bird == null)
            {
                return false;
            }

            Animator animator =
                bird.GetComponentInChildren<Animator>(true);

            bool flying =
                GetAnimatorBool(
                    animator,
                    "flying");

            bool landing =
                GetAnimatorBool(
                    animator,
                    "landing");

            airborne =
                flying ||
                landing;

            Rigidbody body =
                bird.GetComponent<Rigidbody>();

            bool velocityMoving =
                body != null &&
                body.linearVelocity.sqrMagnitude >
                    0.09f;

            return
                airborne ||
                velocityMoving;
        }

        public static string BuildDebugText(
            WildlifeSpeciesDefinition definition,
            WildlifeScoreBreakdown score)
        {
            string species =
                definition != null &&
                !string.IsNullOrWhiteSpace(
                    definition.displayName)
                    ? definition.displayName
                    : "Animal";

            return
                "[WILDLIFE SCORE]" +
                " | species=" + species +
                " | base=" + score.baseScore +
                " | hitMultiplier=x" +
                    score.hitZoneMultiplier.ToString("F2") +
                " | distance=" +
                    score.distanceMeters.ToString("F2") + "m" +
                " | distanceMultiplier=x" +
                    score.distanceMultiplier.ToString("F2") +
                " | movement=" +
                    score.movementLabel +
                " | movementMultiplier=x" +
                    score.movementMultiplier.ToString("F2") +
                " | final=" +
                    score.finalScore;
        }

        private static bool GetAnimatorBool(
            Animator animator,
            string parameterName)
        {
            if (
                animator == null ||
                string.IsNullOrWhiteSpace(
                    parameterName)
            )
            {
                return false;
            }

            AnimatorControllerParameter[] parameters =
                animator.parameters;

            for (
                int index = 0;
                index < parameters.Length;
                index++
            )
            {
                AnimatorControllerParameter parameter =
                    parameters[index];

                if (
                    parameter.type ==
                        AnimatorControllerParameterType.Bool &&
                    parameter.name ==
                        parameterName
                )
                {
                    return animator.GetBool(
                        parameterName);
                }
            }

            return false;
        }
    }
}
