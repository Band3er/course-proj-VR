using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestArchery.TimedGame
{
    public sealed class TimedRoundSession
    {
        private static readonly int[] WarningThresholds =
        {
            60,
            30,
            10,
            5,
            4,
            3,
            2,
            1
        };

        private readonly HashSet<int> emittedWarnings =
            new HashSet<int>();

        private TimedRoundState state =
            TimedRoundState.Idle;

        private string profileId =
            string.Empty;

        private TimedGameInteractionMode interactionMode =
            TimedGameInteractionMode.Controller;

        private float configuredDurationSeconds;
        private float remainingSeconds;
        private float countdownRemainingSeconds;

        private int currentScore;
        private int currentHits;
        private int arrowsLaunched;

        public TimedRoundState State => state;
        public string ProfileId => profileId;
        public TimedGameInteractionMode InteractionMode => interactionMode;
        public float RemainingSeconds => remainingSeconds;
        public int RemainingWholeSeconds =>
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    remainingSeconds));

        public int CurrentScore => currentScore;
        public int CurrentHits => currentHits;
        public int ArrowsLaunched => arrowsLaunched;

        public bool IsScoringAllowed =>
            state == TimedRoundState.Playing;

        public bool IsGameplayAllowed =>
            state == TimedRoundState.Playing;

        public TimedRoundResult LastResult { get; private set; }

        public event Action<TimedRoundState> StateChanged;
        public event Action<int> CountdownChanged;
        public event Action<int> TimeChanged;
        public event Action<string> MessageRequested;
        public event Action RoundStarted;
        public event Action<TimedRoundResult> RoundEnded;

        public void BeginRound(
            string requestedProfileId,
            TimedGameInteractionMode requestedMode,
            float durationSeconds = 300f,
            float countdownSeconds = 3f)
        {
            if (
                string.IsNullOrWhiteSpace(
                    requestedProfileId)
            )
            {
                throw new ArgumentException(
                    "A valid profile ID is required.",
                    nameof(requestedProfileId));
            }

            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds),
                    "Round duration must be greater than zero.");
            }

            if (countdownSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(countdownSeconds),
                    "Countdown duration cannot be negative.");
            }

            profileId =
                requestedProfileId;

            interactionMode =
                requestedMode;

            configuredDurationSeconds =
                durationSeconds;

            remainingSeconds =
                durationSeconds;

            countdownRemainingSeconds =
                countdownSeconds;

            currentScore = 0;
            currentHits = 0;
            arrowsLaunched = 0;
            LastResult = null;

            emittedWarnings.Clear();

            if (countdownSeconds > 0f)
            {
                SetState(
                    TimedRoundState.Countdown);

                CountdownChanged?.Invoke(
                    Mathf.Max(
                        1,
                        Mathf.CeilToInt(
                            countdownRemainingSeconds)));
            }
            else
            {
                StartPlaying();
            }
        }

        public void Tick(
            float unscaledDeltaTime)
        {
            float safeDeltaTime =
                Mathf.Max(
                    0f,
                    unscaledDeltaTime);

            if (
                state ==
                TimedRoundState.Countdown
            )
            {
                TickCountdown(
                    safeDeltaTime);

                return;
            }

            if (
                state ==
                TimedRoundState.Playing
            )
            {
                TickPlaying(
                    safeDeltaTime);
            }
        }

        public void RegisterArrowLaunched()
        {
            if (!IsGameplayAllowed)
            {
                return;
            }

            arrowsLaunched++;
        }

        public void UpdateScoreSnapshot(
            int score,
            int hits)
        {
            if (!IsScoringAllowed)
            {
                return;
            }

            currentScore =
                Mathf.Max(
                    0,
                    score);

            currentHits =
                Mathf.Max(
                    0,
                    hits);
        }

        public void PauseRound()
        {
            if (
                state !=
                TimedRoundState.Playing
            )
            {
                return;
            }

            SetState(
                TimedRoundState.Paused);

            MessageRequested?.Invoke(
                "PAUSED");
        }

        public void ResumeRound()
        {
            if (
                state !=
                TimedRoundState.Paused
            )
            {
                return;
            }

            SetState(
                TimedRoundState.Playing);

            TimeChanged?.Invoke(
                RemainingWholeSeconds);

            MessageRequested?.Invoke(
                "RESUMED");
        }
        public void CancelRound()
        {
            if (
                state ==
                    TimedRoundState.Idle ||
                state ==
                    TimedRoundState.Results ||
                state ==
                    TimedRoundState.Cancelled
            )
            {
                return;
            }

            remainingSeconds = 0f;
            countdownRemainingSeconds = 0f;

            SetState(
                TimedRoundState.Cancelled);

            MessageRequested?.Invoke(
                "ROUND CANCELLED");
        }

        public void AcknowledgeResults()
        {
            if (
                state !=
                TimedRoundState.Results &&
                state !=
                TimedRoundState.Cancelled
            )
            {
                return;
            }

            SetState(
                TimedRoundState.Idle);
        }

        private void TickCountdown(
            float deltaTime)
        {
            int previousWhole =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        countdownRemainingSeconds));

            countdownRemainingSeconds =
                Mathf.Max(
                    0f,
                    countdownRemainingSeconds -
                    deltaTime);

            if (
                countdownRemainingSeconds <= 0f
            )
            {
                CountdownChanged?.Invoke(
                    0);

                StartPlaying();
                return;
            }

            int currentWhole =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        countdownRemainingSeconds));

            if (
                currentWhole !=
                previousWhole
            )
            {
                CountdownChanged?.Invoke(
                    currentWhole);
            }
        }

        private void StartPlaying()
        {
            remainingSeconds =
                configuredDurationSeconds;

            SetState(
                TimedRoundState.Playing);

            MessageRequested?.Invoke(
                "GO!");

            TimeChanged?.Invoke(
                RemainingWholeSeconds);

            RoundStarted?.Invoke();
        }

        private void TickPlaying(
            float deltaTime)
        {
            float previousRemaining =
                remainingSeconds;

            int previousWhole =
                RemainingWholeSeconds;

            remainingSeconds =
                Mathf.Max(
                    0f,
                    remainingSeconds -
                    deltaTime);

            int currentWhole =
                RemainingWholeSeconds;

            EmitCrossedWarnings(
                previousRemaining,
                remainingSeconds);

            if (
                currentWhole !=
                previousWhole
            )
            {
                TimeChanged?.Invoke(
                    currentWhole);
            }

            if (
                remainingSeconds <= 0f
            )
            {
                CompleteRound();
            }
        }

        private void EmitCrossedWarnings(
            float previousRemaining,
            float currentRemaining)
        {
            foreach (int threshold in WarningThresholds)
            {
                if (
                    emittedWarnings.Contains(
                        threshold)
                )
                {
                    continue;
                }

                bool crossedThreshold =
                    previousRemaining >
                        threshold &&
                    currentRemaining <=
                        threshold;

                if (!crossedThreshold)
                {
                    continue;
                }

                emittedWarnings.Add(
                    threshold);

                MessageRequested?.Invoke(
                    GetWarningMessage(
                        threshold));
            }
        }

        private static string GetWarningMessage(
            int threshold)
        {
            if (threshold == 60)
            {
                return "1 MINUTE LEFT";
            }

            if (threshold == 30)
            {
                return "30 SECONDS LEFT";
            }

            if (threshold == 10)
            {
                return "10 SECONDS!";
            }

            return
                threshold.ToString();
        }

        private void CompleteRound()
        {
            remainingSeconds = 0f;

            LastResult =
                BuildResult();

            SetState(
                TimedRoundState.Results);

            MessageRequested?.Invoke(
                "TIME'S UP!");

            RoundEnded?.Invoke(
                LastResult);
        }

        private TimedRoundResult BuildResult()
        {
            int misses =
                Mathf.Max(
                    0,
                    arrowsLaunched -
                    currentHits);

            float accuracy =
                arrowsLaunched > 0
                    ? currentHits * 100f /
                        arrowsLaunched
                    : 0f;

            float averageScore =
                arrowsLaunched > 0
                    ? currentScore /
                        (float)arrowsLaunched
                    : 0f;

            return
                new TimedRoundResult
                {
                    profileId =
                        profileId,
                    interactionMode =
                        interactionMode,
                    score =
                        currentScore,
                    hits =
                        currentHits,
                    arrowsLaunched =
                        arrowsLaunched,
                    misses =
                        misses,
                    accuracy =
                        accuracy,
                    averageScorePerArrow =
                        averageScore,
                    durationSeconds =
                        Mathf.RoundToInt(
                            configuredDurationSeconds)
                };
        }

        private void SetState(
            TimedRoundState newState)
        {
            if (state == newState)
            {
                return;
            }

            state =
                newState;

            StateChanged?.Invoke(
                state);
        }
    }
}
