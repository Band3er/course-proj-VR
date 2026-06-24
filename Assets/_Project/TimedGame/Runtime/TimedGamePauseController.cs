using UnityEngine;
using UnityEngine.UI;

namespace ForestArchery.TimedGame
{
    public sealed class TimedGamePauseController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField]
        private TimedRoundController roundController;

        [SerializeField]
        private TimedGameMenuController menuController;

        [Header("UI")]
        [SerializeField]
        private CanvasGroup roundHud;

        [SerializeField]
        private CanvasGroup pausePanel;

        [SerializeField]
        private Text pauseInfoText;

        [SerializeField]
        private Button pauseButton;

        [SerializeField]
        private Button resumeButton;

        [SerializeField]
        private Button quitButton;

        private float timeScaleBeforePause =
            1f;

        private bool simulationFrozen;
        private bool subscribed;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
            SetPanelVisible(
                pausePanel,
                false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();

            ApplyState(
                roundController != null
                    ? roundController.State
                    : TimedRoundState.Idle);
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreSimulation();
        }

        private void OnDestroy()
        {
            RestoreSimulation();
        }

        public void PauseRound()
        {
            if (
                roundController == null ||
                roundController.State !=
                    TimedRoundState.Playing
            )
            {
                return;
            }

            roundController.PauseRound();
        }

        public void ResumeRound()
        {
            if (
                roundController == null ||
                roundController.State !=
                    TimedRoundState.Paused
            )
            {
                return;
            }

            roundController.ResumeRound();
        }

        public void QuitRound()
        {
            if (roundController == null)
            {
                return;
            }

            if (
                roundController.State ==
                    TimedRoundState.Playing ||
                roundController.State ==
                    TimedRoundState.Paused ||
                roundController.State ==
                    TimedRoundState.Countdown
            )
            {
                roundController.CancelRound();
            }

            RestoreSimulation();

            if (menuController != null)
            {
                menuController.ShowMainMenu();
            }
        }

        private void ResolveReferences()
        {
            if (roundController == null)
            {
                roundController =
                    GetComponent<TimedRoundController>();
            }

            if (menuController == null)
            {
                menuController =
                    GetComponent<TimedGameMenuController>();
            }
        }

        private void BindButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(
                    PauseRound);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(
                    ResumeRound);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(
                    QuitRound);
            }
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

            roundController.TimeChanged +=
                HandleTimeChanged;

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

            roundController.TimeChanged -=
                HandleTimeChanged;

            subscribed =
                false;
        }

        private void HandleStateChanged(
            TimedRoundState state)
        {
            ApplyState(
                state);
        }

        private void HandleTimeChanged(
            int wholeSeconds)
        {
            if (
                pauseInfoText != null &&
                roundController != null &&
                roundController.State ==
                    TimedRoundState.Paused
            )
            {
                pauseInfoText.text =
                    "TIME REMAINING\n" +
                    FormatTime(
                        wholeSeconds) +
                    "\n\nCURRENT ROUND SCORE WILL BE LOST IF YOU QUIT.";
            }
        }

        private void ApplyState(
            TimedRoundState state)
        {
            switch (state)
            {
                case TimedRoundState.Countdown:
                    RestoreSimulation();

                    SetPanelVisible(
                        pausePanel,
                        false);

                    SetPanelVisible(
                        roundHud,
                        true);

                    if (pauseButton != null)
                    {
                        pauseButton.interactable =
                            false;
                    }

                    break;

                case TimedRoundState.Playing:
                    RestoreSimulation();

                    SetPanelVisible(
                        pausePanel,
                        false);

                    SetPanelVisible(
                        roundHud,
                        true);

                    if (pauseButton != null)
                    {
                        pauseButton.interactable =
                            true;
                    }

                    break;

                case TimedRoundState.Paused:
                    FreezeSimulation();

                    SetPanelVisible(
                        roundHud,
                        false);

                    SetPanelVisible(
                        pausePanel,
                        true);

                    if (
                        pauseInfoText != null &&
                        roundController != null
                    )
                    {
                        pauseInfoText.text =
                            "TIME REMAINING\n" +
                            FormatTime(
                                roundController
                                    .Session
                                    .RemainingWholeSeconds) +
                            "\n\nCURRENT ROUND SCORE WILL BE LOST IF YOU QUIT.";
                    }

                    break;

                case TimedRoundState.Results:
                case TimedRoundState.Cancelled:
                case TimedRoundState.Idle:
                default:
                    RestoreSimulation();

                    SetPanelVisible(
                        pausePanel,
                        false);

                    SetPanelVisible(
                        roundHud,
                        false);

                    break;
            }
        }

        private void FreezeSimulation()
        {
            if (simulationFrozen)
            {
                return;
            }

            timeScaleBeforePause =
                Time.timeScale > 0f
                    ? Time.timeScale
                    : 1f;

            Time.timeScale =
                0f;

            AudioListener.pause =
                true;

            simulationFrozen =
                true;
        }

        private void RestoreSimulation()
        {
            if (!simulationFrozen)
            {
                return;
            }

            Time.timeScale =
                timeScaleBeforePause > 0f
                    ? timeScaleBeforePause
                    : 1f;

            AudioListener.pause =
                false;

            simulationFrozen =
                false;
        }

        private static void SetPanelVisible(
            CanvasGroup panel,
            bool visible)
        {
            if (panel == null)
            {
                return;
            }

            panel.alpha =
                visible
                    ? 1f
                    : 0f;

            panel.interactable =
                visible;

            panel.blocksRaycasts =
                visible;
        }

        private static string FormatTime(
            int wholeSeconds)
        {
            int safeSeconds =
                Mathf.Max(
                    0,
                    wholeSeconds);

            return
                (safeSeconds / 60)
                    .ToString("00") +
                ":" +
                (safeSeconds % 60)
                    .ToString("00");
        }
    }
}
