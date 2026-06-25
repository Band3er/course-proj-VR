using UnityEngine;

namespace ForestArchery.Finalization
{
    [DefaultExecutionOrder(32500)]
    [DisallowMultipleComponent]
    public sealed class ControllerLocomotionRuntimeGuard :
        MonoBehaviour
    {
        [Header("Controller interaction parents")]
        [SerializeField]
        private GameObject leftControllerRoot;

        [SerializeField]
        private GameObject rightControllerRoot;

        [Header("Locomotion hierarchy")]
        [SerializeField]
        private GameObject leftLocomotionGroup;

        [SerializeField]
        private GameObject rightLocomotionGroup;

        [SerializeField]
        private GameObject leftLocomotionActions;

        [SerializeField]
        private GameObject rightLocomotionActions;

        [SerializeField]
        private GameObject leftSlideInteractor;

        [SerializeField]
        private GameObject rightSlideInteractor;

        [SerializeField]
        private GameObject leftTurnInteractor;

        [SerializeField]
        private GameObject rightTurnInteractor;

        [Header("Locomotion outputs")]
        [SerializeField]
        private GameObject leftLocomotionOutput;

        [SerializeField]
        private GameObject rightLocomotionOutput;

        [SerializeField]
        private GameObject locomotorRoot;

        [SerializeField]
        private GameObject playerControllerRoot;

        [Header("Runtime")]
        [SerializeField]
        private bool enforceEveryFrame = true;

        public bool ReferencesAssigned =>
            leftControllerRoot != null &&
            rightControllerRoot != null &&
            leftLocomotionGroup != null &&
            rightLocomotionGroup != null &&
            leftLocomotionActions != null &&
            rightLocomotionActions != null &&
            leftSlideInteractor != null &&
            rightSlideInteractor != null &&
            leftTurnInteractor != null &&
            rightTurnInteractor != null &&
            leftLocomotionOutput != null &&
            rightLocomotionOutput != null &&
            locomotorRoot != null &&
            playerControllerRoot != null;

        public void Configure(
            GameObject configuredLeftControllerRoot,
            GameObject configuredRightControllerRoot,
            GameObject configuredLeftLocomotionGroup,
            GameObject configuredRightLocomotionGroup,
            GameObject configuredLeftLocomotionActions,
            GameObject configuredRightLocomotionActions,
            GameObject configuredLeftSlideInteractor,
            GameObject configuredRightSlideInteractor,
            GameObject configuredLeftTurnInteractor,
            GameObject configuredRightTurnInteractor,
            GameObject configuredLeftLocomotionOutput,
            GameObject configuredRightLocomotionOutput,
            GameObject configuredLocomotorRoot,
            GameObject configuredPlayerControllerRoot)
        {
            leftControllerRoot =
                configuredLeftControllerRoot;

            rightControllerRoot =
                configuredRightControllerRoot;

            leftLocomotionGroup =
                configuredLeftLocomotionGroup;

            rightLocomotionGroup =
                configuredRightLocomotionGroup;

            leftLocomotionActions =
                configuredLeftLocomotionActions;

            rightLocomotionActions =
                configuredRightLocomotionActions;

            leftSlideInteractor =
                configuredLeftSlideInteractor;

            rightSlideInteractor =
                configuredRightSlideInteractor;

            leftTurnInteractor =
                configuredLeftTurnInteractor;

            rightTurnInteractor =
                configuredRightTurnInteractor;

            leftLocomotionOutput =
                configuredLeftLocomotionOutput;

            rightLocomotionOutput =
                configuredRightLocomotionOutput;

            locomotorRoot =
                configuredLocomotorRoot;

            playerControllerRoot =
                configuredPlayerControllerRoot;

            ApplyRequiredStates();
        }

        private void Awake()
        {
            ApplyRequiredStates();
        }

        private void OnEnable()
        {
            ApplyRequiredStates();
        }

        private void LateUpdate()
        {
            if (enforceEveryFrame)
            {
                ApplyRequiredStates();
            }
        }

        public void ApplyRequiredStates()
        {
            SetActive(
                leftControllerRoot,
                true);

            SetActive(
                rightControllerRoot,
                true);

            SetActive(
                leftLocomotionGroup,
                true);

            SetActive(
                rightLocomotionGroup,
                true);

            SetActive(
                leftLocomotionActions,
                true);

            SetActive(
                rightLocomotionActions,
                true);

            SetActive(
                leftSlideInteractor,
                true);

            SetActive(
                rightSlideInteractor,
                false);

            SetActive(
                leftTurnInteractor,
                false);

            SetActive(
                rightTurnInteractor,
                true);

            SetActive(
                leftLocomotionOutput,
                true);

            SetActive(
                rightLocomotionOutput,
                true);

            SetActive(
                locomotorRoot,
                true);

            SetActive(
                playerControllerRoot,
                true);
        }

        private static void SetActive(
            GameObject target,
            bool state)
        {
            if (
                target != null &&
                target.activeSelf != state
            )
            {
                target.SetActive(
                    state);
            }
        }
    }
}