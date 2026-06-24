using System;
using System.Globalization;
using System.IO;
using System.Text;
using ForestArchery.Wildlife;
using UnityEngine;

namespace ForestArchery.TimedGame
{
    [DefaultExecutionOrder(7000)]
    public sealed class TimedGameResearchLogger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private TimedRoundController roundController;

        [SerializeField]
        private TimedGameMenuController menuController;

        [SerializeField]
        private WildlifeScoreManager scoreManager;

        [SerializeField]
        private global::TrajectoryToggleController trajectoryToggle;

        [Header("Logging")]
        [SerializeField]
        private string folderName = "ForestArcheryResearch";

        [SerializeField]
        private bool verboseLogging = true;

        private string directoryPath;
        private string roundsPath;
        private string shotsPath;
        private string eventsPath;

        private string sessionId = string.Empty;
        private string participantId = "P_UNKNOWN";
        private string profileId = string.Empty;
        private string roundType = "Recorded";
        private string interactionMode = "Controller";

        private float roundStartedAt;
        private int lastObservedArrowCount;
        private int conditionIndex;

        private bool roundActive;
        private bool roundLogged;
        private bool trajectoryAtStart;

        private OpenShot openShot;

        private sealed class OpenShot
        {
            public int shotNumber;
            public float launchedAt;
            public bool trajectoryEnabled;
            public bool hit;
            public string targetSpecies = string.Empty;
            public string hitZone = string.Empty;
            public int awardedScore;
        }

        public string DirectoryPath => directoryPath;
        public string RoundsPath => roundsPath;
        public string ShotsPath => shotsPath;
        public string EventsPath => eventsPath;

        private void Awake()
        {
            ResolveReferences();
            InitializeFiles();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (
                !roundActive ||
                roundController == null ||
                roundController.State != TimedRoundState.Playing
            )
            {
                return;
            }

            int currentArrowCount =
                roundController.Session.ArrowsLaunched;

            while (
                lastObservedArrowCount <
                currentArrowCount
            )
            {
                FinalizeOpenShotAsMiss();

                lastObservedArrowCount++;

                openShot =
                    new OpenShot
                    {
                        shotNumber =
                            lastObservedArrowCount,
                        launchedAt =
                            Time.unscaledTime,
                        trajectoryEnabled =
                            GetTrajectoryState()
                    };

                AppendEvent(
                    "arrow_launched",
                    "shot=" +
                    lastObservedArrowCount);
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

            if (scoreManager == null)
            {
                scoreManager =
                    WildlifeScoreManager.Instance;

                if (scoreManager == null)
                {
                    scoreManager =
                        FindFirstObjectByType
                            <WildlifeScoreManager>();
                }
            }

            if (trajectoryToggle == null)
            {
                trajectoryToggle =
                    FindFirstObjectByType
                        <global::TrajectoryToggleController>();
            }
        }

        private void InitializeFiles()
        {
            directoryPath =
                Path.Combine(
                    Application.persistentDataPath,
                    folderName);

            Directory.CreateDirectory(
                directoryPath);

            roundsPath =
                Path.Combine(
                    directoryPath,
                    "research_rounds.csv");

            shotsPath =
                Path.Combine(
                    directoryPath,
                    "research_shots.csv");

            eventsPath =
                Path.Combine(
                    directoryPath,
                    "research_events.csv");

            EnsureHeader(
                roundsPath,
                "timestamp,session_id,participant_id,profile_id,round_type,interaction_mode,condition_index,planned_duration_seconds,actual_elapsed_seconds,completed,cancelled,final_score,arrows_launched,successful_hits,misses,accuracy_percent,average_score_per_arrow,best_streak,trajectory_at_start,trajectory_at_end");

            EnsureHeader(
                shotsPath,
                "timestamp,session_id,participant_id,profile_id,round_type,interaction_mode,shot_number,time_from_round_start_seconds,hit,target_species,hit_zone,awarded_score,cumulative_score,cumulative_hits,trajectory_enabled");

            EnsureHeader(
                eventsPath,
                "timestamp,session_id,participant_id,profile_id,round_type,interaction_mode,event_type,time_from_round_start_seconds,details");

            if (verboseLogging)
            {
                Debug.Log(
                    "[RESEARCH LOGGER] CSV directory:\n" +
                    directoryPath);
            }
        }

        private void Subscribe()
        {
            if (roundController != null)
            {
                roundController.RoundStarted -=
                    HandleRoundStarted;

                roundController.RoundStarted +=
                    HandleRoundStarted;

                roundController.RoundEnded -=
                    HandleRoundEnded;

                roundController.RoundEnded +=
                    HandleRoundEnded;

                roundController.StateChanged -=
                    HandleStateChanged;

                roundController.StateChanged +=
                    HandleStateChanged;
            }

            if (scoreManager != null)
            {
                scoreManager.HitRegistered -=
                    HandleHitRegistered;

                scoreManager.HitRegistered +=
                    HandleHitRegistered;
            }
        }

        private void Unsubscribe()
        {
            if (roundController != null)
            {
                roundController.RoundStarted -=
                    HandleRoundStarted;

                roundController.RoundEnded -=
                    HandleRoundEnded;

                roundController.StateChanged -=
                    HandleStateChanged;
            }

            if (scoreManager != null)
            {
                scoreManager.HitRegistered -=
                    HandleHitRegistered;
            }
        }

        private void HandleRoundStarted()
        {
            ResolveReferences();
            InitializeFiles();

            if (scoreManager != null)
            {
                scoreManager.HitRegistered -=
                    HandleHitRegistered;

                scoreManager.HitRegistered +=
                    HandleHitRegistered;
            }

            profileId =
                roundController.Session.ProfileId;

            participantId =
                ResolveParticipantName(
                    profileId);

            roundType =
                menuController != null &&
                menuController.CurrentRoundIsPractice
                    ? "Practice"
                    : "Recorded";

            interactionMode =
                roundController
                    .Session
                    .InteractionMode
                    .ToString();

            conditionIndex++;

            sessionId =
                DateTime.UtcNow.ToString(
                    "yyyyMMdd_HHmmss_fff",
                    CultureInfo.InvariantCulture) +
                "_" +
                SanitizeToken(
                    participantId) +
                "_" +
                roundType +
                "_" +
                interactionMode;

            roundStartedAt =
                Time.unscaledTime;

            lastObservedArrowCount =
                0;

            trajectoryAtStart =
                GetTrajectoryState();

            openShot =
                null;

            roundActive =
                true;

            roundLogged =
                false;

            AppendEvent(
                "round_started",
                "planned_duration=" +
                Mathf.RoundToInt(
                    roundController.RoundDurationSeconds));
        }

        private void HandleStateChanged(
            TimedRoundState state)
        {
            if (!roundActive)
            {
                return;
            }

            if (state == TimedRoundState.Paused)
            {
                AppendEvent(
                    "round_paused",
                    string.Empty);
            }
            else if (state == TimedRoundState.Playing)
            {
                AppendEvent(
                    "round_playing",
                    string.Empty);
            }
            else if (state == TimedRoundState.Cancelled)
            {
                AppendEvent(
                    "round_cancelled",
                    string.Empty);

                FinalizeOpenShotAsMiss();
                LogCancelledRound();
            }
        }

        private void HandleRoundEnded(
            TimedRoundResult result)
        {
            if (
                result == null ||
                roundLogged
            )
            {
                return;
            }

            FinalizeOpenShotAsMiss();

            AppendRound(
                result.durationSeconds,
                Mathf.Max(
                    0f,
                    Time.unscaledTime -
                    roundStartedAt),
                true,
                false,
                result.score,
                result.arrowsLaunched,
                result.hits,
                result.misses,
                result.accuracy,
                result.averageScorePerArrow);

            AppendEvent(
                "round_completed",
                "score=" +
                result.score);

            roundLogged =
                true;

            roundActive =
                false;
        }

        private void LogCancelledRound()
        {
            if (
                roundController == null ||
                roundLogged
            )
            {
                return;
            }

            TimedRoundSession session =
                roundController.Session;

            int arrows =
                session.ArrowsLaunched;

            int hits =
                session.CurrentHits;

            int misses =
                Mathf.Max(
                    0,
                    arrows - hits);

            float accuracy =
                arrows > 0
                    ? hits * 100f / arrows
                    : 0f;

            float average =
                arrows > 0
                    ? session.CurrentScore /
                        (float)arrows
                    : 0f;

            AppendRound(
                Mathf.RoundToInt(
                    roundController.RoundDurationSeconds),
                Mathf.Max(
                    0f,
                    Time.unscaledTime -
                    roundStartedAt),
                false,
                true,
                session.CurrentScore,
                arrows,
                hits,
                misses,
                accuracy,
                average);

            roundLogged =
                true;

            roundActive =
                false;
        }

        private void HandleHitRegistered(
            string species,
            int awardedScore,
            string hitLabel)
        {
            if (
                !roundActive ||
                openShot == null
            )
            {
                return;
            }

            openShot.hit =
                true;

            openShot.targetSpecies =
                species ?? string.Empty;

            openShot.hitZone =
                hitLabel ?? string.Empty;

            openShot.awardedScore =
                Mathf.Max(
                    0,
                    awardedScore);

            AppendShot(
                openShot);

            AppendEvent(
                "wildlife_hit",
                "shot=" +
                openShot.shotNumber +
                ";species=" +
                openShot.targetSpecies +
                ";zone=" +
                openShot.hitZone +
                ";score=" +
                openShot.awardedScore);

            openShot =
                null;
        }

        private void FinalizeOpenShotAsMiss()
        {
            if (openShot == null)
            {
                return;
            }

            AppendShot(
                openShot);

            if (!openShot.hit)
            {
                AppendEvent(
                    "shot_miss_or_environment",
                    "shot=" +
                    openShot.shotNumber);
            }

            openShot =
                null;
        }

        private void AppendRound(
            int plannedDuration,
            float actualElapsed,
            bool completed,
            bool cancelled,
            int score,
            int arrows,
            int hits,
            int misses,
            float accuracy,
            float average)
        {
            int bestStreak =
                scoreManager != null
                    ? scoreManager.BestStreak
                    : 0;

            string line =
                JoinCsv(
                    UtcTimestamp(),
                    sessionId,
                    participantId,
                    profileId,
                    roundType,
                    interactionMode,
                    conditionIndex.ToString(
                        CultureInfo.InvariantCulture),
                    plannedDuration.ToString(
                        CultureInfo.InvariantCulture),
                    actualElapsed.ToString(
                        "F3",
                        CultureInfo.InvariantCulture),
                    completed.ToString(),
                    cancelled.ToString(),
                    score.ToString(
                        CultureInfo.InvariantCulture),
                    arrows.ToString(
                        CultureInfo.InvariantCulture),
                    hits.ToString(
                        CultureInfo.InvariantCulture),
                    misses.ToString(
                        CultureInfo.InvariantCulture),
                    accuracy.ToString(
                        "F2",
                        CultureInfo.InvariantCulture),
                    average.ToString(
                        "F2",
                        CultureInfo.InvariantCulture),
                    bestStreak.ToString(
                        CultureInfo.InvariantCulture),
                    trajectoryAtStart.ToString(),
                    GetTrajectoryState().ToString());

            File.AppendAllText(
                roundsPath,
                line +
                Environment.NewLine,
                Encoding.UTF8);
        }

        private void AppendShot(
            OpenShot shot)
        {
            if (shot == null)
            {
                return;
            }

            int cumulativeScore =
                scoreManager != null
                    ? scoreManager.TotalScore
                    : roundController.Session.CurrentScore;

            int cumulativeHits =
                scoreManager != null
                    ? scoreManager.TotalHits
                    : roundController.Session.CurrentHits;

            string line =
                JoinCsv(
                    UtcTimestamp(),
                    sessionId,
                    participantId,
                    profileId,
                    roundType,
                    interactionMode,
                    shot.shotNumber.ToString(
                        CultureInfo.InvariantCulture),
                    Mathf.Max(
                        0f,
                        shot.launchedAt -
                        roundStartedAt)
                        .ToString(
                            "F3",
                            CultureInfo.InvariantCulture),
                    shot.hit.ToString(),
                    shot.targetSpecies,
                    shot.hitZone,
                    shot.awardedScore.ToString(
                        CultureInfo.InvariantCulture),
                    cumulativeScore.ToString(
                        CultureInfo.InvariantCulture),
                    cumulativeHits.ToString(
                        CultureInfo.InvariantCulture),
                    shot.trajectoryEnabled.ToString());

            File.AppendAllText(
                shotsPath,
                line +
                Environment.NewLine,
                Encoding.UTF8);
        }

        private void AppendEvent(
            string eventType,
            string details)
        {
            if (
                string.IsNullOrWhiteSpace(
                    eventsPath)
            )
            {
                return;
            }

            float timeFromStart =
                roundActive
                    ? Mathf.Max(
                        0f,
                        Time.unscaledTime -
                        roundStartedAt)
                    : 0f;

            string line =
                JoinCsv(
                    UtcTimestamp(),
                    sessionId,
                    participantId,
                    profileId,
                    roundType,
                    interactionMode,
                    eventType,
                    timeFromStart.ToString(
                        "F3",
                        CultureInfo.InvariantCulture),
                    details ?? string.Empty);

            File.AppendAllText(
                eventsPath,
                line +
                Environment.NewLine,
                Encoding.UTF8);
        }

        private string ResolveParticipantName(
            string requestedProfileId)
        {
            try
            {
                LocalProfileRepository repository =
                    new LocalProfileRepository();

                repository.LoadOrCreate();

                PlayerProfileData profile =
                    repository.FindProfile(
                        requestedProfileId);

                if (
                    profile != null &&
                    !string.IsNullOrWhiteSpace(
                        profile.displayName)
                )
                {
                    return profile.displayName;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[RESEARCH LOGGER] Could not resolve participant name: " +
                    exception.Message);
            }

            return
                string.IsNullOrWhiteSpace(
                    requestedProfileId)
                    ? "P_UNKNOWN"
                    : requestedProfileId;
        }

        private bool GetTrajectoryState()
        {
            return
                trajectoryToggle != null &&
                trajectoryToggle.TrajectoryEnabled;
        }

        private static void EnsureHeader(
            string path,
            string header)
        {
            if (File.Exists(path))
            {
                return;
            }

            File.WriteAllText(
                path,
                header +
                Environment.NewLine,
                Encoding.UTF8);
        }

        private static string UtcTimestamp()
        {
            return
                DateTime.UtcNow.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture);
        }

        private static string SanitizeToken(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "UNKNOWN";
            }

            StringBuilder builder =
                new StringBuilder();

            foreach (char character in value)
            {
                if (
                    char.IsLetterOrDigit(
                        character) ||
                    character == '-' ||
                    character == '_'
                )
                {
                    builder.Append(
                        character);
                }
            }

            return
                builder.Length > 0
                    ? builder.ToString()
                    : "UNKNOWN";
        }

        private static string JoinCsv(
            params string[] values)
        {
            StringBuilder builder =
                new StringBuilder();

            for (
                int index = 0;
                index < values.Length;
                index++
            )
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(
                    EscapeCsv(
                        values[index]));
            }

            return builder.ToString();
        }

        private static string EscapeCsv(
            string value)
        {
            string safe =
                value ?? string.Empty;

            bool quote =
                safe.Contains(",") ||
                safe.Contains("\"") ||
                safe.Contains("\n") ||
                safe.Contains("\r");

            safe =
                safe.Replace(
                    "\"",
                    "\"\"");

            return
                quote
                    ? "\"" +
                        safe +
                        "\""
                    : safe;
        }
    }
}
