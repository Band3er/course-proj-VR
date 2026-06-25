using System.Collections;
using UnityEngine;

namespace ForestArchery.Finalization
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class BowEyeLevelStartPlacement :
        MonoBehaviour
    {
        [SerializeField]
        private Transform eyeTransform;

        [SerializeField, Min(0.1f)]
        private float forwardDistance = 0.5f;

        [SerializeField, Min(0f)]
        private float initialDelaySeconds = 0.75f;

        [SerializeField, Min(0.1f)]
        private float maximumTrackingWaitSeconds = 3f;

        [SerializeField, Min(0.001f)]
        private float stabilityThreshold = 0.02f;

        [SerializeField, Min(1)]
        private int stableFramesRequired = 8;

        [SerializeField]
        private bool placementCompleted;

        private Rigidbody cachedRigidbody;

        public Transform EyeTransform =>
            eyeTransform;

        public float ForwardDistance =>
            forwardDistance;

        public bool PlacementCompleted =>
            placementCompleted;

        public void Configure(
            Transform configuredEye,
            float configuredForwardDistance)
        {
            eyeTransform =
                configuredEye;

            forwardDistance =
                Mathf.Max(
                    0.1f,
                    configuredForwardDistance);

            placementCompleted =
                false;
        }

        private void Awake()
        {
            cachedRigidbody =
                GetComponent<Rigidbody>();
        }

        private IEnumerator Start()
        {
            if (initialDelaySeconds > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        initialDelaySeconds);
            }

            ResolveEyeTransform();

            if (eyeTransform == null)
            {
                Debug.LogWarning(
                    "[BOW START PLACEMENT] Center eye was not found.");

                yield break;
            }

            Vector3 previousPosition =
                eyeTransform.position;

            int stableFrames =
                0;

            float deadline =
                Time.realtimeSinceStartup +
                maximumTrackingWaitSeconds;

            while (
                stableFrames <
                    stableFramesRequired &&
                Time.realtimeSinceStartup <
                    deadline
            )
            {
                yield return null;

                Vector3 currentPosition =
                    eyeTransform.position;

                float movement =
                    Vector3.Distance(
                        previousPosition,
                        currentPosition);

                stableFrames =
                    movement <= stabilityThreshold
                        ? stableFrames + 1
                        : 0;

                previousPosition =
                    currentPosition;
            }

            PlaceAtEyeLevel();
        }

        private void ResolveEyeTransform()
        {
            if (eyeTransform != null)
            {
                return;
            }

            Camera mainCamera =
                Camera.main;

            if (mainCamera != null)
            {
                eyeTransform =
                    mainCamera.transform;

                return;
            }

            Camera fallback =
                FindFirstObjectByType<Camera>();

            if (fallback != null)
            {
                eyeTransform =
                    fallback.transform;
            }
        }

        private void PlaceAtEyeLevel()
        {
            if (eyeTransform == null)
            {
                return;
            }

            Vector3 horizontalForward =
                Vector3.ProjectOnPlane(
                    eyeTransform.forward,
                    Vector3.up);

            if (
                horizontalForward.sqrMagnitude <
                0.0001f
            )
            {
                horizontalForward =
                    Vector3.forward;
            }

            horizontalForward.Normalize();

            Vector3 targetPosition =
                eyeTransform.position +
                horizontalForward *
                forwardDistance;

            if (cachedRigidbody != null)
            {
                cachedRigidbody.position =
                    targetPosition;

                cachedRigidbody.linearVelocity =
                    Vector3.zero;

                cachedRigidbody.angularVelocity =
                    Vector3.zero;
            }
            else
            {
                transform.position =
                    targetPosition;
            }

            placementCompleted =
                true;

            Debug.Log(
                "[BOW START PLACEMENT] Bow positioned at eye level, " +
                forwardDistance.ToString("F2") +
                " m in front of the player.");
        }
    }
}