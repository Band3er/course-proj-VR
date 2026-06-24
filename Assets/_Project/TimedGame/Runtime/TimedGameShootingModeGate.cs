using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace ForestArchery.TimedGame
{
    [DefaultExecutionOrder(5000)]
    public sealed class TimedGameShootingModeGate : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField]
        private TimedRoundController roundController;

        [Header("Bow and String Controller Interactables")]
        [SerializeField]
        private GrabInteractable[] controllerInteractables;

        [Header("Bow and String Hand Interactables")]
        [SerializeField]
        private HandGrabInteractable[] handInteractables;

        [Header("Behaviour")]
        [SerializeField]
        private bool enableBothOutsideActiveRound = true;

        [SerializeField]
        private bool enforceWhileRoundIsActive = true;

        [SerializeField]
        private bool verboseLogging;

        private bool[] controllerInitialStates;
        private bool[] handInitialStates;

        private bool subscribed;
        private TimedRoundState lastState =
            (TimedRoundState)(-1);

        private TimedGameInteractionMode lastMode =
            (TimedGameInteractionMode)(-1);

        public bool IsRoundModeLocked { get; private set; }

        public TimedGameInteractionMode LockedMode
        {
            get;
            private set;
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureInitialStates();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (
                controllerInitialStates == null ||
                handInitialStates == null
            )
            {
                CaptureInitialStates();
            }

            Subscribe();
            ApplyCurrentState(
                true);
        }

        private void LateUpdate()
        {
            if (!enforceWhileRoundIsActive)
            {
                return;
            }

            if (
                roundController == null ||
                !IsActiveRoundState(
                    roundController.State)
            )
            {
                return;
            }

            ApplyCurrentState(
                false);
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreInitialStates();
        }

        public void ForceRefresh()
        {
            ApplyCurrentState(
                true);
        }

        private void ResolveReferences()
        {
            if (roundController == null)
            {
                roundController =
                    GetComponent<TimedRoundController>();
            }
        }

        private void CaptureInitialStates()
        {
            controllerInitialStates =
                CaptureStates(
                    controllerInteractables);

            handInitialStates =
                CaptureStates(
                    handInteractables);
        }

        private void Subscribe()
        {
            if (
                subscribed ||
                roundController == null
            )
            {
                return;
            }

            roundController.StateChanged +=
                HandleStateChanged;

            subscribed =
                true;
        }

        private void Unsubscribe()
        {
            if (
                !subscribed ||
                roundController == null
            )
            {
                return;
            }

            roundController.StateChanged -=
                HandleStateChanged;

            subscribed =
                false;
        }

        private void HandleStateChanged(
            TimedRoundState state)
        {
            ApplyCurrentState(
                true);
        }

        private void ApplyCurrentState(
            bool force)
        {
            if (roundController == null)
            {
                RestoreInitialStates();
                return;
            }

            TimedRoundState state =
                roundController.State;

            TimedGameInteractionMode mode =
                roundController
                    .Session
                    .InteractionMode;

            bool activeRound =
                IsActiveRoundState(
                    state);

            if (
                !force &&
                state == lastState &&
                mode == lastMode &&
                InteractableStatesMatch(
                    activeRound,
                    mode)
            )
            {
                return;
            }

            lastState =
                state;

            lastMode =
                mode;

            if (!activeRound)
            {
                IsRoundModeLocked =
                    false;

                if (enableBothOutsideActiveRound)
                {
                    RestoreInitialStates();
                }

                return;
            }

            LockedMode =
                mode;

            IsRoundModeLocked =
                true;

            bool controllerMode =
                mode ==
                TimedGameInteractionMode.Controller;

            ApplyStates(
                controllerInteractables,
                controllerInitialStates,
                controllerMode);

            ApplyStates(
                handInteractables,
                handInitialStates,
                !controllerMode);

            if (verboseLogging)
            {
                Debug.Log(
                    "[SHOOTING MODE GATE]" +
                    " state=" +
                    state +
                    " | mode=" +
                    mode +
                    " | controllerInteractables=" +
                    CountEnabled(
                        controllerInteractables) +
                    " | handInteractables=" +
                    CountEnabled(
                        handInteractables));
            }
        }

        private bool InteractableStatesMatch(
            bool activeRound,
            TimedGameInteractionMode mode)
        {
            if (!activeRound)
            {
                return
                    StatesMatchInitial(
                        controllerInteractables,
                        controllerInitialStates) &&
                    StatesMatchInitial(
                        handInteractables,
                        handInitialStates);
            }

            bool controllerMode =
                mode ==
                TimedGameInteractionMode.Controller;

            return
                StatesMatchDesired(
                    controllerInteractables,
                    controllerInitialStates,
                    controllerMode) &&
                StatesMatchDesired(
                    handInteractables,
                    handInitialStates,
                    !controllerMode);
        }

        private void RestoreInitialStates()
        {
            RestoreStates(
                controllerInteractables,
                controllerInitialStates);

            RestoreStates(
                handInteractables,
                handInitialStates);
        }

        private static bool IsActiveRoundState(
            TimedRoundState state)
        {
            return
                state ==
                    TimedRoundState.Countdown ||
                state ==
                    TimedRoundState.Playing ||
                state ==
                    TimedRoundState.Paused;
        }

        private static bool[] CaptureStates<T>(
            T[] behaviours)
            where T : Behaviour
        {
            if (behaviours == null)
            {
                return new bool[0];
            }

            bool[] states =
                new bool[
                    behaviours.Length];

            for (
                int index = 0;
                index < behaviours.Length;
                index++
            )
            {
                states[index] =
                    behaviours[index] != null &&
                    behaviours[index].enabled;
            }

            return states;
        }

        private static void ApplyStates<T>(
            T[] behaviours,
            bool[] initialStates,
            bool modalityEnabled)
            where T : Behaviour
        {
            if (
                behaviours == null ||
                initialStates == null
            )
            {
                return;
            }

            int count =
                Mathf.Min(
                    behaviours.Length,
                    initialStates.Length);

            for (
                int index = 0;
                index < count;
                index++
            )
            {
                T behaviour =
                    behaviours[index];

                if (behaviour == null)
                {
                    continue;
                }

                bool desired =
                    modalityEnabled &&
                    initialStates[index];

                if (
                    behaviour.enabled !=
                    desired
                )
                {
                    behaviour.enabled =
                        desired;
                }
            }
        }

        private static void RestoreStates<T>(
            T[] behaviours,
            bool[] initialStates)
            where T : Behaviour
        {
            if (
                behaviours == null ||
                initialStates == null
            )
            {
                return;
            }

            int count =
                Mathf.Min(
                    behaviours.Length,
                    initialStates.Length);

            for (
                int index = 0;
                index < count;
                index++
            )
            {
                T behaviour =
                    behaviours[index];

                if (behaviour == null)
                {
                    continue;
                }

                if (
                    behaviour.enabled !=
                    initialStates[index]
                )
                {
                    behaviour.enabled =
                        initialStates[index];
                }
            }
        }

        private static bool StatesMatchInitial<T>(
            T[] behaviours,
            bool[] initialStates)
            where T : Behaviour
        {
            if (
                behaviours == null ||
                initialStates == null ||
                behaviours.Length !=
                    initialStates.Length
            )
            {
                return false;
            }

            for (
                int index = 0;
                index < behaviours.Length;
                index++
            )
            {
                T behaviour =
                    behaviours[index];

                if (behaviour == null)
                {
                    continue;
                }

                if (
                    behaviour.enabled !=
                    initialStates[index]
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool StatesMatchDesired<T>(
            T[] behaviours,
            bool[] initialStates,
            bool modalityEnabled)
            where T : Behaviour
        {
            if (
                behaviours == null ||
                initialStates == null ||
                behaviours.Length !=
                    initialStates.Length
            )
            {
                return false;
            }

            for (
                int index = 0;
                index < behaviours.Length;
                index++
            )
            {
                T behaviour =
                    behaviours[index];

                if (behaviour == null)
                {
                    continue;
                }

                bool desired =
                    modalityEnabled &&
                    initialStates[index];

                if (
                    behaviour.enabled !=
                    desired
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountEnabled<T>(
            T[] behaviours)
            where T : Behaviour
        {
            if (behaviours == null)
            {
                return 0;
            }

            int count =
                0;

            foreach (T behaviour in behaviours)
            {
                if (
                    behaviour != null &&
                    behaviour.enabled
                )
                {
                    count++;
                }
            }

            return count;
        }
    }
}
