using UnityEngine;
using UnityEngine.UI;

namespace ForestArchery.TimedGame
{
    [DefaultExecutionOrder(32000)]
    [DisallowMultipleComponent]
    public sealed class HandArcheryRayProximityGate : MonoBehaviour
    {
        [SerializeField] private BowDrawController bow;
        [SerializeField] private Transform leftHandPoint;
        [SerializeField] private Transform rightHandPoint;
        [SerializeField] private GameObject leftHandRayRoot;
        [SerializeField] private GameObject rightHandRayRoot;
        [SerializeField] private GameObject leftDistanceGrabRoot;
        [SerializeField] private GameObject rightDistanceGrabRoot;
        [SerializeField] private CanvasGroup pausePanel;
        [SerializeField, Min(0.1f)] private float suspendDistance = 0.55f;
        [SerializeField, Min(0.15f)] private float restoreDistance = 0.72f;
        [SerializeField] private bool leftSuppressed;
        [SerializeField] private bool rightSuppressed;

        private bool cached;
        private bool leftRayInitial;
        private bool rightRayInitial;
        private bool leftDistanceInitial;
        private bool rightDistanceInitial;

        public float SuspendDistance => suspendDistance;
        public float RestoreDistance => restoreDistance;

        public void Configure(
            BowDrawController configuredBow,
            Transform configuredLeftHandPoint,
            Transform configuredRightHandPoint,
            GameObject configuredLeftRayRoot,
            GameObject configuredRightRayRoot,
            GameObject configuredLeftDistanceRoot,
            GameObject configuredRightDistanceRoot,
            CanvasGroup configuredPausePanel,
            float configuredSuspendDistance,
            float configuredRestoreDistance)
        {
            bow = configuredBow;
            leftHandPoint = configuredLeftHandPoint;
            rightHandPoint = configuredRightHandPoint;
            leftHandRayRoot = configuredLeftRayRoot;
            rightHandRayRoot = configuredRightRayRoot;
            leftDistanceGrabRoot = configuredLeftDistanceRoot;
            rightDistanceGrabRoot = configuredRightDistanceRoot;
            pausePanel = configuredPausePanel;
            suspendDistance = Mathf.Max(0.1f, configuredSuspendDistance);
            restoreDistance = Mathf.Max(suspendDistance + 0.05f, configuredRestoreDistance);
            CacheInitialStates();
            RestoreBoth();
        }

        private void Awake()
        {
            CacheInitialStates();
        }

        private void OnEnable()
        {
            CacheInitialStates();
            RestoreBoth();
        }

        private void OnDisable()
        {
            RestoreBoth();
        }

        private void OnDestroy()
        {
            RestoreBoth();
        }

        private void Update()
        {
            if (!ReferencesValid())
            {
                RestoreBoth();
                return;
            }

            bool bowUnavailable = !bow.gameObject.activeInHierarchy;
            bool uiNeedsRays = IsPauseVisible() || Time.timeScale <= 0.001f;

            if (bowUnavailable || uiNeedsRays)
            {
                RestoreBoth();
                return;
            }

            UpdateHand(
                leftHandPoint,
                leftHandRayRoot,
                leftDistanceGrabRoot,
                ref leftSuppressed,
                leftRayInitial,
                leftDistanceInitial);

            UpdateHand(
                rightHandPoint,
                rightHandRayRoot,
                rightDistanceGrabRoot,
                ref rightSuppressed,
                rightRayInitial,
                rightDistanceInitial);
        }

        private void UpdateHand(
            Transform handPoint,
            GameObject rayRoot,
            GameObject distanceRoot,
            ref bool suppressed,
            bool rayInitial,
            bool distanceInitial)
        {
            if (handPoint == null || !handPoint.gameObject.activeInHierarchy)
            {
                RestoreHand(rayRoot, distanceRoot, ref suppressed, rayInitial, distanceInitial);
                return;
            }

            float distance = DistanceToBowOrString(handPoint.position);

            if (!suppressed && distance <= suspendDistance)
            {
                SetActive(rayRoot, false);
                SetActive(distanceRoot, false);
                suppressed = true;
            }
            else if (suppressed && distance >= restoreDistance)
            {
                RestoreHand(rayRoot, distanceRoot, ref suppressed, rayInitial, distanceInitial);
            }
        }

        private float DistanceToBowOrString(Vector3 handPosition)
        {
            float result = Vector3.Distance(handPosition, bow.transform.position);

            if (bow.bowHoldPoint != null)
            {
                result = Mathf.Min(result, Vector3.Distance(handPosition, bow.bowHoldPoint.position));
            }

            if (bow.stringGrabPoint != null)
            {
                result = Mathf.Min(result, Vector3.Distance(handPosition, bow.stringGrabPoint.position));
            }

            return result;
        }

        private bool IsPauseVisible()
        {
            return pausePanel != null &&
                   pausePanel.gameObject.activeInHierarchy &&
                   (pausePanel.alpha > 0.5f || pausePanel.interactable || pausePanel.blocksRaycasts);
        }

        private bool ReferencesValid()
        {
            return bow != null &&
                   leftHandPoint != null &&
                   rightHandPoint != null &&
                   leftHandRayRoot != null &&
                   rightHandRayRoot != null &&
                   leftDistanceGrabRoot != null &&
                   rightDistanceGrabRoot != null;
        }

        private void CacheInitialStates()
        {
            if (cached || !ReferencesValid())
            {
                return;
            }

            leftRayInitial = leftHandRayRoot.activeSelf;
            rightRayInitial = rightHandRayRoot.activeSelf;
            leftDistanceInitial = leftDistanceGrabRoot.activeSelf;
            rightDistanceInitial = rightDistanceGrabRoot.activeSelf;
            cached = true;
        }

        private void RestoreBoth()
        {
            if (!cached)
            {
                return;
            }

            RestoreHand(
                leftHandRayRoot,
                leftDistanceGrabRoot,
                ref leftSuppressed,
                leftRayInitial,
                leftDistanceInitial);

            RestoreHand(
                rightHandRayRoot,
                rightDistanceGrabRoot,
                ref rightSuppressed,
                rightRayInitial,
                rightDistanceInitial);
        }

        private static void RestoreHand(
            GameObject rayRoot,
            GameObject distanceRoot,
            ref bool suppressed,
            bool rayInitial,
            bool distanceInitial)
        {
            if (!suppressed)
            {
                return;
            }

            SetActive(rayRoot, rayInitial);
            SetActive(distanceRoot, distanceInitial);
            suppressed = false;
        }

        private static void SetActive(GameObject target, bool state)
        {
            if (target != null && target.activeSelf != state)
            {
                target.SetActive(state);
            }
        }
    }
}