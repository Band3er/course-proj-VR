using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ForestArchery.TimedGame
{
    public sealed class TimedGameMenuController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField]
        private TimedRoundController roundController;

        [Header("Development Safety")]
        [SerializeField]
        private bool useTemporaryEditorStorage = true;

        [SerializeField]
        private bool useShortEditorRound = true;

        [SerializeField, Min(5f)]
        private float editorRoundDurationSeconds = 15f;

        [Header("Device Integration Test")]
        [SerializeField]
        private bool useShortDeviceIntegrationRound = true;

        [SerializeField, Min(10f)]
        private float deviceIntegrationRoundDurationSeconds = 60f;

        [Header("Panels")]
        [SerializeField]
        private CanvasGroup mainMenuPanel;

        [SerializeField]
        private CanvasGroup playerSelectionPanel;

        [SerializeField]
        private CanvasGroup createPlayerPanel;

        [SerializeField]
        private CanvasGroup modeSelectionPanel;

        [SerializeField]
        private CanvasGroup leaderboardPanel;

        [SerializeField]
        private CanvasGroup resultsPanel;

        [SerializeField]
        private CanvasGroup resetConfirmationPanel;

        [SerializeField]
        private CanvasGroup roundHud;

        [Header("Main Menu")]
        [SerializeField]
        private Text mainCurrentPlayerText;

        [SerializeField]
        private Text mainStatusText;

        [SerializeField]
        private Button mainTimedRoundButton;

        [SerializeField]
        private Button mainPracticeButton;

        [SerializeField]
        private Button mainPlayersButton;

        [SerializeField]
        private Button mainLeaderboardButton;

        [SerializeField]
        private Button mainResetButton;

        [Header("Player Selection")]
        [SerializeField]
        private Text playerListText;

        [SerializeField]
        private Text playerSelectionStatusText;

        [SerializeField]
        private Button playerPreviousButton;

        [SerializeField]
        private Button playerNextButton;

        [SerializeField]
        private Button playerConfirmButton;

        [SerializeField]
        private Button playerCreateButton;

        [SerializeField]
        private Button playerBackButton;

        [Header("Create Player")]
        [SerializeField]
        private Text createPlayerNameText;

        [SerializeField]
        private Text createPlayerStatusText;

        [SerializeField]
        private Transform virtualKeyboardRoot;

        [SerializeField]
        private Button createPlayerBackButton;

        [Header("Mode Selection")]
        [SerializeField]
        private Text modePlayerText;

        [SerializeField]
        private Text controllerBestText;

        [SerializeField]
        private Text handTrackingBestText;

        [SerializeField]
        private Button controllerModeButton;

        [SerializeField]
        private Button handTrackingModeButton;

        [SerializeField]
        private Button modeBackButton;

        [Header("Leaderboard")]
        [SerializeField]
        private Text leaderboardTitleText;

        [SerializeField]
        private Text leaderboardEntriesText;

        [SerializeField]
        private Button controllerLeaderboardButton;

        [SerializeField]
        private Button handTrackingLeaderboardButton;

        [SerializeField]
        private Button leaderboardBackButton;

        [Header("Results")]
        [SerializeField]
        private Text resultsSummaryText;

        [SerializeField]
        private Button resultsPlayAgainButton;

        [SerializeField]
        private Button resultsLeaderboardButton;

        [SerializeField]
        private Button resultsMainMenuButton;

        [Header("Reset")]
        [SerializeField]
        private Text resetWarningText;

        [SerializeField]
        private Button resetCancelButton;

        [SerializeField]
        private Button resetDeleteButton;

        [Header("Round HUD")]
        [SerializeField]
        private Text timerText;

        [SerializeField]
        private Text roundMessageText;

        private LocalProfileRepository repository;
        private List<PlayerProfileData> profiles =
            new List<PlayerProfileData>();

        private int highlightedProfileIndex;
        private string pendingPlayerName =
            string.Empty;

        private TimedGameInteractionMode selectedMode =
            TimedGameInteractionMode.Controller;

        private TimedRoundResult lastRoundResult;
        private bool lastRoundWasPersonalBest;
        private int previousPersonalBest;

        private bool pendingPracticeRound;
        private bool lastRoundWasPractice;

        public bool CurrentRoundIsPractice =>
            pendingPracticeRound;

        public string CurrentRoundTypeLabel =>
            pendingPracticeRound
                ? "Practice"
                : "Recorded";

        private float hideRoundMessageAt =
            -1f;

        private void Awake()
        {
            if (roundController == null)
            {
                roundController =
                    GetComponent<TimedRoundController>();
            }

            if (roundController == null)
            {
                Debug.LogError(
                    "[TIMED GAME UI] TimedRoundController is missing.");

                enabled =
                    false;

                return;
            }

            InitializeRepository();
            BindButtons();
            BindRoundEvents();
            RefreshProfiles();

            PlayerProfileData selectedProfile =
                repository.GetSelectedProfile();

            if (selectedProfile == null)
            {
                ShowCreatePlayer();
            }
            else
            {
                ShowMainMenu();
            }
        }

        private void Update()
        {
            if (
                hideRoundMessageAt > 0f &&
                Time.unscaledTime >=
                    hideRoundMessageAt &&
                roundController.State ==
                    TimedRoundState.Playing
            )
            {
                roundMessageText.text =
                    string.Empty;

                hideRoundMessageAt =
                    -1f;
            }

            bool backPressed =
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame;

            if (backPressed)
            {
                HandleBackRequest();
            }
        }

        private void OnDestroy()
        {
            UnbindRoundEvents();
        }

        public void ShowMainMenu()
        {
            HideAllPanels();

            SetPanelVisible(
                mainMenuPanel,
                true);

            PlayerProfileData selectedProfile =
                repository.GetSelectedProfile();

            mainCurrentPlayerText.text =
                selectedProfile != null
                    ? "CURRENT PLAYER\n" +
                        selectedProfile.displayName
                    : "CURRENT PLAYER\nNONE";

            mainStatusText.text =
                Application.isEditor &&
                useTemporaryEditorStorage
                    ? "EDITOR TEST STORAGE\n" +
                        repository.DirectoryPath
                    : "LOCAL DATA\n" +
                        repository.DirectoryPath;
        }

        public void ShowPlayerSelection()
        {
            RefreshProfiles();
            HideAllPanels();

            SetPanelVisible(
                playerSelectionPanel,
                true);

            RefreshPlayerSelectionView();
        }

        public void ShowCreatePlayer()
        {
            pendingPlayerName =
                string.Empty;

            HideAllPanels();

            SetPanelVisible(
                createPlayerPanel,
                true);

            RefreshCreatePlayerView(
                "ENTER A PLAYER NAME");

            BindVirtualKeyboard();
        }

        public void ShowPracticeModeSelection()
        {
            pendingPracticeRound =
                true;

            ShowModeSelection();
        }

        public void ShowRecordedModeSelection()
        {
            pendingPracticeRound =
                false;

            ShowModeSelection();
        }

        public void ShowModeSelection()
        {
            PlayerProfileData selectedProfile =
                repository.GetSelectedProfile();

            if (selectedProfile == null)
            {
                ShowCreatePlayer();
                return;
            }

            HideAllPanels();

            SetPanelVisible(
                modeSelectionPanel,
                true);

            modePlayerText.text =
                (
                    pendingPracticeRound
                        ? "PRACTICE - 01:00"
                        : "RECORDED ROUND - 05:00"
                ) +
                "\nPLAYER: " +
                selectedProfile.displayName;

            controllerBestText.text =
                "CONTROLLER BEST\n" +
                selectedProfile
                    .controllerRecord
                    .personalBestScore;

            handTrackingBestText.text =
                "HAND TRACKING BEST\n" +
                selectedProfile
                    .handTrackingRecord
                    .personalBestScore;
        }

        public void ShowControllerLeaderboard()
        {
            ShowLeaderboard(
                TimedGameInteractionMode.Controller);
        }

        public void ShowHandTrackingLeaderboard()
        {
            ShowLeaderboard(
                TimedGameInteractionMode.HandTracking);
        }

        public void ShowResetConfirmation()
        {
            HideAllPanels();

            SetPanelVisible(
                resetConfirmationPanel,
                true);

            resetWarningText.text =
                "DELETE ALL LOCAL DATA?\n\n" +
                "This permanently removes all profiles,\n" +
                "Controller records, Hand Tracking records,\n" +
                "leaderboards and saved round history.\n\n" +
                "THIS ACTION CANNOT BE UNDONE.";
        }

        public void PreviousPlayer()
        {
            if (profiles.Count == 0)
            {
                return;
            }

            highlightedProfileIndex =
                (
                    highlightedProfileIndex -
                    1 +
                    profiles.Count
                ) %
                profiles.Count;

            RefreshPlayerSelectionView();
        }

        public void NextPlayer()
        {
            if (profiles.Count == 0)
            {
                return;
            }

            highlightedProfileIndex =
                (
                    highlightedProfileIndex +
                    1
                ) %
                profiles.Count;

            RefreshPlayerSelectionView();
        }

        public void ConfirmHighlightedPlayer()
        {
            if (
                profiles.Count == 0 ||
                highlightedProfileIndex < 0 ||
                highlightedProfileIndex >=
                    profiles.Count
            )
            {
                playerSelectionStatusText.text =
                    "NO PLAYER AVAILABLE";

                return;
            }

            PlayerProfileData profile =
                repository.SelectProfile(
                    profiles[
                        highlightedProfileIndex]
                        .profileId);

            playerSelectionStatusText.text =
                "SELECTED: " +
                profile.displayName;

            ShowMainMenu();
        }

        public void StartControllerRound()
        {
            StartRound(
                TimedGameInteractionMode.Controller);
        }

        public void StartHandTrackingRound()
        {
            StartRound(
                TimedGameInteractionMode.HandTracking);
        }

        public void PlayAgain()
        {
            StartRound(
                selectedMode);
        }

        public void DeleteAllLocalData()
        {
            repository.DeleteAllData();

            profiles.Clear();
            highlightedProfileIndex = 0;

            pendingPlayerName =
                string.Empty;

            ShowCreatePlayer();

            createPlayerStatusText.text =
                "LOCAL DATA DELETED";
        }

        public void ShowPracticePlaceholder()
        {
            mainStatusText.text =
                "PRACTICE MODE WILL BE CONNECTED\n" +
                "AFTER TIMED GAME INTEGRATION.";
        }

        private void InitializeRepository()
        {
            if (
                Application.isEditor &&
                useTemporaryEditorStorage
            )
            {
                string temporaryDirectory =
                    Path.Combine(
                        Application.temporaryCachePath,
                        "ForestArcheryTimedGame_UIStage08d");

                repository =
                    new LocalProfileRepository(
                        temporaryDirectory);
            }
            else
            {
                repository =
                    new LocalProfileRepository();
            }

            repository.LoadOrCreate();
        }

        private void BindButtons()
        {
            Bind(
                mainTimedRoundButton,
                ShowRecordedModeSelection);

            Bind(
                mainPracticeButton,
                ShowPracticeModeSelection);

            Bind(
                mainPlayersButton,
                ShowPlayerSelection);

            Bind(
                mainLeaderboardButton,
                ShowControllerLeaderboard);

            Bind(
                mainResetButton,
                ShowResetConfirmation);

            Bind(
                playerPreviousButton,
                PreviousPlayer);

            Bind(
                playerNextButton,
                NextPlayer);

            Bind(
                playerConfirmButton,
                ConfirmHighlightedPlayer);

            Bind(
                playerCreateButton,
                ShowCreatePlayer);

            Bind(
                playerBackButton,
                ShowMainMenu);

            Bind(
                createPlayerBackButton,
                HandleCreatePlayerBack);

            Bind(
                controllerModeButton,
                StartControllerRound);

            Bind(
                handTrackingModeButton,
                StartHandTrackingRound);

            Bind(
                modeBackButton,
                ShowMainMenu);

            Bind(
                controllerLeaderboardButton,
                ShowControllerLeaderboard);

            Bind(
                handTrackingLeaderboardButton,
                ShowHandTrackingLeaderboard);

            Bind(
                leaderboardBackButton,
                ShowMainMenu);

            Bind(
                resultsPlayAgainButton,
                PlayAgain);

            Bind(
                resultsLeaderboardButton,
                ShowResultsModeLeaderboard);

            Bind(
                resultsMainMenuButton,
                AcknowledgeResultsAndShowMainMenu);

            Bind(
                resetCancelButton,
                ShowMainMenu);

            Bind(
                resetDeleteButton,
                DeleteAllLocalData);
        }

        private static void Bind(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (
                button == null ||
                action == null
            )
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                action);
        }

        private void BindVirtualKeyboard()
        {
            if (virtualKeyboardRoot == null)
            {
                return;
            }

            Button[] keyButtons =
                virtualKeyboardRoot
                    .GetComponentsInChildren<Button>(
                        true);

            foreach (Button button in keyButtons)
            {
                button.onClick.RemoveAllListeners();

                string keyName =
                    button.gameObject.name;

                if (
                    keyName.StartsWith(
                        "Key_",
                        StringComparison.Ordinal)
                )
                {
                    string token =
                        keyName.Substring(
                            "Key_".Length);

                    button.onClick.AddListener(
                        () =>
                            HandleKeyboardToken(
                                token));
                }
            }
        }

        private void HandleKeyboardToken(
            string token)
        {
            if (
                string.Equals(
                    token,
                    "BACKSPACE",
                    StringComparison.Ordinal)
            )
            {
                if (
                    pendingPlayerName.Length > 0
                )
                {
                    pendingPlayerName =
                        pendingPlayerName.Substring(
                            0,
                            pendingPlayerName.Length - 1);
                }

                RefreshCreatePlayerView(
                    "EDITING");

                return;
            }

            if (
                string.Equals(
                    token,
                    "CLEAR",
                    StringComparison.Ordinal)
            )
            {
                pendingPlayerName =
                    string.Empty;

                RefreshCreatePlayerView(
                    "CLEARED");

                return;
            }

            if (
                string.Equals(
                    token,
                    "SPACE",
                    StringComparison.Ordinal)
            )
            {
                AppendCharacter(
                    " ");

                return;
            }

            if (
                string.Equals(
                    token,
                    "CONFIRM",
                    StringComparison.Ordinal)
            )
            {
                ConfirmCreatePlayer();

                return;
            }

            AppendCharacter(
                token);
        }

        private void AppendCharacter(
            string value)
        {
            if (
                string.IsNullOrEmpty(
                    value)
            )
            {
                return;
            }

            if (
                pendingPlayerName.Length >=
                LocalProfileRepository.MaximumNameLength
            )
            {
                RefreshCreatePlayerView(
                    "MAXIMUM 12 CHARACTERS");

                return;
            }

            pendingPlayerName +=
                value;

            if (
                pendingPlayerName.Length >
                LocalProfileRepository.MaximumNameLength
            )
            {
                pendingPlayerName =
                    pendingPlayerName.Substring(
                        0,
                        LocalProfileRepository.MaximumNameLength);
            }

            RefreshCreatePlayerView(
                "EDITING");
        }

        private void ConfirmCreatePlayer()
        {
            try
            {
                PlayerProfileData profile =
                    repository.CreateProfile(
                        pendingPlayerName);

                RefreshProfiles();

                createPlayerStatusText.text =
                    "CREATED: " +
                    profile.displayName;

                ShowMainMenu();
            }
            catch (Exception exception)
            {
                RefreshCreatePlayerView(
                    exception.Message.ToUpperInvariant());
            }
        }

        private void HandleCreatePlayerBack()
        {
            if (
                repository.GetSelectedProfile() ==
                null
            )
            {
                RefreshCreatePlayerView(
                    "CREATE OR SELECT A PLAYER");

                return;
            }

            ShowMainMenu();
        }

        private void StartRound(
            TimedGameInteractionMode mode)
        {
            PlayerProfileData profile =
                repository.GetSelectedProfile();

            if (profile == null)
            {
                ShowCreatePlayer();
                return;
            }

            selectedMode =
                mode;

            if (
                Application.isEditor &&
                useShortEditorRound
            )
            {
                roundController.ConfigureDurations(
                    editorRoundDurationSeconds,
                    3f);
            }
            else if (
                !Application.isEditor &&
                useShortDeviceIntegrationRound
            )
            {
                roundController.ConfigureDurations(
                    deviceIntegrationRoundDurationSeconds,
                    3f);
            }
            else
            {
                roundController.ConfigureDurations(
                    300f,
                    3f);
            }

            HideAllPanels();

            SetPanelVisible(
                roundHud,
                true);

            timerText.text =
                FormatTime(
                    Mathf.CeilToInt(
                        roundController
                            .RoundDurationSeconds));

            roundMessageText.text =
                "GET READY";

            hideRoundMessageAt =
                -1f;

            float configuredDurationSeconds =
                pendingPracticeRound
                    ? 60f
                    : 300f;

            roundController.ConfigureDurations(
                configuredDurationSeconds,
                3f);

            roundController.StartRound(
                profile.profileId,
                mode);
        }

        private void BindRoundEvents()
        {
            roundController.StateChanged +=
                HandleRoundStateChanged;

            roundController.CountdownChanged +=
                HandleCountdownChanged;

            roundController.TimeChanged +=
                HandleTimeChanged;

            roundController.MessageRequested +=
                HandleRoundMessage;

            roundController.RoundEnded +=
                HandleRoundEnded;
        }

        private void UnbindRoundEvents()
        {
            if (roundController == null)
            {
                return;
            }

            roundController.StateChanged -=
                HandleRoundStateChanged;

            roundController.CountdownChanged -=
                HandleCountdownChanged;

            roundController.TimeChanged -=
                HandleTimeChanged;

            roundController.MessageRequested -=
                HandleRoundMessage;

            roundController.RoundEnded -=
                HandleRoundEnded;
        }

        private void HandleRoundStateChanged(
            TimedRoundState newState)
        {
            if (
                newState ==
                TimedRoundState.Cancelled
            )
            {
                ShowMainMenu();
            }
        }

        private void HandleCountdownChanged(
            int value)
        {
            if (value > 0)
            {
                roundMessageText.text =
                    value.ToString();

                hideRoundMessageAt =
                    -1f;
            }
        }

        private void HandleTimeChanged(
            int wholeSeconds)
        {
            timerText.text =
                FormatTime(
                    wholeSeconds);
        }

        private void HandleRoundMessage(
            string message)
        {
            roundMessageText.text =
                message;

            if (
                message == "TIME'S UP!"
            )
            {
                hideRoundMessageAt =
                    -1f;
            }
            else if (
                message == "GO!"
            )
            {
                hideRoundMessageAt =
                    Time.unscaledTime +
                    1.0f;
            }
            else if (
                roundController.State ==
                TimedRoundState.Playing
            )
            {
                hideRoundMessageAt =
                    Time.unscaledTime +
                    1.1f;
            }
        }

        private void HandleRoundEnded(
            TimedRoundResult result)
        {
            lastRoundResult =
                result;

            lastRoundWasPractice =
                pendingPracticeRound;

            lastRoundWasPersonalBest =
                false;

            previousPersonalBest =
                0;

            PlayerProfileData profile =
                repository.FindProfile(
                    result.profileId);

            previousPersonalBest =
                0;

            if (
                profile != null &&
                !lastRoundWasPractice
            )
            {
                ModeRecord previousRecord =
                    LeaderboardService.GetModeRecord(
                        profile,
                        result.interactionMode);

                previousPersonalBest =
                    previousRecord != null
                        ? previousRecord.personalBestScore
                        : 0;

                repository.RecordRound(
                    profile.profileId,
                    result.interactionMode,
                    result.score,
                    result.hits,
                    result.arrowsLaunched,
                    result.durationSeconds);

                ModeRecord updatedRecord =
                    LeaderboardService.GetModeRecord(
                        profile,
                        result.interactionMode);

                lastRoundWasPersonalBest =
                    updatedRecord != null &&
                    updatedRecord.personalBestScore ==
                        result.score &&
                    result.score >=
                        previousPersonalBest;
            }

            ShowResults();
        }

        private void ShowResults()
        {
            HideAllPanels();

            SetPanelVisible(
                resultsPanel,
                true);

            PlayerProfileData profile =
                lastRoundResult != null
                    ? repository.FindProfile(
                        lastRoundResult.profileId)
                    : null;

            string playerName =
                profile != null
                    ? profile.displayName
                    : "UNKNOWN";

            string bestLine =
                lastRoundWasPractice
                    ? "PRACTICE RESULT\n" +
                        "NOT SAVED TO PERSONAL BEST OR LEADERBOARD"
                    : (
                        lastRoundWasPersonalBest
                            ? "NEW PERSONAL BEST!\n" +
                                "PREVIOUS BEST: " +
                                previousPersonalBest
                            : "PERSONAL BEST: " +
                                GetPersonalBest(
                                    profile,
                                    selectedMode)
                    );

            if (resultsLeaderboardButton != null)
            {
                resultsLeaderboardButton.interactable =
                    !lastRoundWasPractice;
            }

            resultsSummaryText.text =
                (
                    lastRoundWasPractice
                        ? "PRACTICE COMPLETE\n\n"
                        : "RECORDED ROUND COMPLETE\n\n"
                ) +
                "PLAYER: " +
                playerName +
                "\n" +
                "MODE: " +
                GetModeLabel(
                    selectedMode) +
                "\n\n" +
                "FINAL SCORE: " +
                lastRoundResult.score +
                "\n" +
                "HITS: " +
                lastRoundResult.hits +
                "\n" +
                "ARROWS: " +
                lastRoundResult.arrowsLaunched +
                "\n" +
                "MISSES: " +
                lastRoundResult.misses +
                "\n" +
                "ACCURACY: " +
                lastRoundResult.accuracy.ToString("F1") +
                "%\n" +
                "AVG SCORE / ARROW: " +
                lastRoundResult
                    .averageScorePerArrow
                    .ToString("F1") +
                "\n\n" +
                bestLine;
        }

        private void ShowLeaderboard(
            TimedGameInteractionMode mode)
        {
            selectedMode =
                mode;

            HideAllPanels();

            SetPanelVisible(
                leaderboardPanel,
                true);

            leaderboardTitleText.text =
                GetModeLabel(
                    mode) +
                " LEADERBOARD";

            List<LeaderboardEntry> entries =
                LeaderboardService.Build(
                    repository.Data,
                    mode,
                    10);

            if (entries.Count == 0)
            {
                leaderboardEntriesText.text =
                    "NO COMPLETED ROUNDS YET";

                return;
            }

            PlayerProfileData selectedProfile =
                repository.GetSelectedProfile();

            leaderboardEntriesText.text =
                string.Join(
                    "\n",
                    entries.Select(
                        entry =>
                        {
                            bool isCurrent =
                                selectedProfile != null &&
                                selectedProfile.profileId ==
                                    entry.profileId;

                            return
                                (
                                    isCurrent
                                        ? "> "
                                        : "  "
                                ) +
                                entry.rank
                                    .ToString()
                                    .PadLeft(2) +
                                "   " +
                                entry.playerName
                                    .PadRight(12) +
                                "   " +
                                entry.bestScore;
                        }));
        }

        private void ShowResultsModeLeaderboard()
        {
            ShowLeaderboard(
                selectedMode);
        }

        private void AcknowledgeResultsAndShowMainMenu()
        {
            roundController.AcknowledgeResults();
            ShowMainMenu();
        }

        private void RefreshProfiles()
        {
            profiles =
                repository.Data.profiles
                    .Where(
                        profile =>
                            profile != null)
                    .OrderBy(
                        profile =>
                            profile.displayName,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            PlayerProfileData selectedProfile =
                repository.GetSelectedProfile();

            int selectedIndex =
                selectedProfile == null
                    ? -1
                    : profiles.FindIndex(
                        profile =>
                            profile.profileId ==
                            selectedProfile.profileId);

            highlightedProfileIndex =
                selectedIndex >= 0
                    ? selectedIndex
                    : Mathf.Clamp(
                        highlightedProfileIndex,
                        0,
                        Mathf.Max(
                            0,
                            profiles.Count - 1));
        }

        private void RefreshPlayerSelectionView()
        {
            if (profiles.Count == 0)
            {
                playerListText.text =
                    "NO LOCAL PLAYERS";

                playerSelectionStatusText.text =
                    "CREATE A NEW PLAYER";

                return;
            }

            playerListText.text =
                string.Join(
                    "\n",
                    profiles.Select(
                        (profile, index) =>
                            (
                                index ==
                                highlightedProfileIndex
                                    ? "> "
                                    : "  "
                            ) +
                            profile.displayName));

            PlayerProfileData highlighted =
                profiles[
                    highlightedProfileIndex];

            playerSelectionStatusText.text =
                "HIGHLIGHTED: " +
                highlighted.displayName;
        }

        private void RefreshCreatePlayerView(
            string status)
        {
            createPlayerNameText.text =
                string.IsNullOrEmpty(
                    pendingPlayerName)
                    ? "_"
                    : pendingPlayerName;

            createPlayerStatusText.text =
                status;
        }

        private void HandleBackRequest()
        {
            if (
                roundController.State ==
                TimedRoundState.Playing
            )
            {
                roundController.PauseRound();
                return;
            }

            if (
                roundController.State ==
                TimedRoundState.Paused
            )
            {
                roundController.ResumeRound();
                return;
            }

            if (
                roundController.State ==
                TimedRoundState.Countdown
            )
            {
                roundController.CancelRound();
                return;
            }

            ShowMainMenu();
        }

        private void HideAllPanels()
        {
            SetPanelVisible(
                mainMenuPanel,
                false);

            SetPanelVisible(
                playerSelectionPanel,
                false);

            SetPanelVisible(
                createPlayerPanel,
                false);

            SetPanelVisible(
                modeSelectionPanel,
                false);

            SetPanelVisible(
                leaderboardPanel,
                false);

            SetPanelVisible(
                resultsPanel,
                false);

            SetPanelVisible(
                resetConfirmationPanel,
                false);

            SetPanelVisible(
                roundHud,
                false);
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

            int minutes =
                safeSeconds / 60;

            int seconds =
                safeSeconds % 60;

            return
                minutes.ToString("00") +
                ":" +
                seconds.ToString("00");
        }

        private static string GetModeLabel(
            TimedGameInteractionMode mode)
        {
            return
                mode ==
                TimedGameInteractionMode.HandTracking
                    ? "HAND TRACKING"
                    : "CONTROLLER";
        }

        private static int GetPersonalBest(
            PlayerProfileData profile,
            TimedGameInteractionMode mode)
        {
            ModeRecord record =
                LeaderboardService.GetModeRecord(
                    profile,
                    mode);

            return
                record != null
                    ? record.personalBestScore
                    : 0;
        }
    }
}
