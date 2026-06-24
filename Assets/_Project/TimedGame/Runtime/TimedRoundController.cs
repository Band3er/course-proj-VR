using System;
using UnityEngine;

namespace ForestArchery.TimedGame
{
    public sealed class TimedRoundController : MonoBehaviour
    {
        [Header("Round Configuration")]
        [SerializeField, Min(1f)]
        private float roundDurationSeconds = 300f;

        [SerializeField, Min(0f)]
        private float countdownSeconds = 3f;

        [Header("Development")]
        [SerializeField]
        private bool verboseLogging = true;

        private TimedRoundSession session;

        public TimedRoundSession Session
        {
            get
            {
                EnsureSession();
                return session;
            }
        }

        public float RoundDurationSeconds =>
            roundDurationSeconds;

        public float CountdownSeconds =>
            countdownSeconds;

        public TimedRoundState State =>
            Session.State;

        public bool IsGameplayAllowed =>
            Session.IsGameplayAllowed;

        public bool IsScoringAllowed =>
            Session.IsScoringAllowed;

        public event Action<TimedRoundState> StateChanged;
        public event Action<int> CountdownChanged;
        public event Action<int> TimeChanged;
        public event Action<string> MessageRequested;
        public event Action RoundStarted;
        public event Action<TimedRoundResult> RoundEnded;

        private void Awake()
        {
            EnsureSession();
        }

        private void Update()
        {
            Session.Tick(
                Time.unscaledDeltaTime);
        }

        public void ConfigureDurations(
            float configuredRoundDurationSeconds,
            float configuredCountdownSeconds)
        {
            roundDurationSeconds =
                Mathf.Max(
                    1f,
                    configuredRoundDurationSeconds);

            countdownSeconds =
                Mathf.Max(
                    0f,
                    configuredCountdownSeconds);
        }

        public void StartRound(
            string profileId,
            TimedGameInteractionMode mode)
        {
            Session.BeginRound(
                profileId,
                mode,
                roundDurationSeconds,
                countdownSeconds);
        }

        public void RegisterArrowLaunched()
        {
            Session.RegisterArrowLaunched();
        }

        public void UpdateScoreSnapshot(
            int score,
            int hits)
        {
            Session.UpdateScoreSnapshot(
                score,
                hits);
        }

        public void PauseRound()
        {
            Session.PauseRound();
        }

        public void ResumeRound()
        {
            Session.ResumeRound();
        }
        public void CancelRound()
        {
            Session.CancelRound();
        }

        public void AcknowledgeResults()
        {
            Session.AcknowledgeResults();
        }

        private void EnsureSession()
        {
            if (session != null)
            {
                return;
            }

            session =
                new TimedRoundSession();

            session.StateChanged +=
                OnSessionStateChanged;

            session.CountdownChanged +=
                OnSessionCountdownChanged;

            session.TimeChanged +=
                OnSessionTimeChanged;

            session.MessageRequested +=
                OnSessionMessageRequested;

            session.RoundStarted +=
                OnSessionRoundStarted;

            session.RoundEnded +=
                OnSessionRoundEnded;
        }

        private void OnDestroy()
        {
            if (session == null)
            {
                return;
            }

            session.StateChanged -=
                OnSessionStateChanged;

            session.CountdownChanged -=
                OnSessionCountdownChanged;

            session.TimeChanged -=
                OnSessionTimeChanged;

            session.MessageRequested -=
                OnSessionMessageRequested;

            session.RoundStarted -=
                OnSessionRoundStarted;

            session.RoundEnded -=
                OnSessionRoundEnded;
        }

        private void OnSessionStateChanged(
            TimedRoundState newState)
        {
            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED ROUND] State=" +
                    newState);
            }

            StateChanged?.Invoke(
                newState);
        }

        private void OnSessionCountdownChanged(
            int value)
        {
            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED ROUND] Countdown=" +
                    value);
            }

            CountdownChanged?.Invoke(
                value);
        }

        private void OnSessionTimeChanged(
            int wholeSeconds)
        {
            TimeChanged?.Invoke(
                wholeSeconds);
        }

        private void OnSessionMessageRequested(
            string message)
        {
            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED ROUND] Message=" +
                    message);
            }

            MessageRequested?.Invoke(
                message);
        }

        private void OnSessionRoundStarted()
        {
            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED ROUND] Round started.");
            }

            RoundStarted?.Invoke();
        }

        private void OnSessionRoundEnded(
            TimedRoundResult result)
        {
            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED ROUND] Round ended" +
                    " | score=" +
                    result.score +
                    " | hits=" +
                    result.hits +
                    " | arrows=" +
                    result.arrowsLaunched +
                    " | accuracy=" +
                    result.accuracy.ToString("F1") +
                    "%");
            }

            RoundEnded?.Invoke(
                result);
        }
    }
}
