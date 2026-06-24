using System;
using UnityEngine;

namespace ForestArchery.Wildlife
{
    public sealed class WildlifeScoreManager : MonoBehaviour
    {
        public static WildlifeScoreManager Instance { get; private set; }

        public int TotalScore { get; private set; }
        public int TotalHits { get; private set; }
        public int CurrentStreak { get; private set; }
        public int BestStreak { get; private set; }

        public bool ScoringEnabled { get; private set; } = true;

        public event Action<int, int> ScoreChanged;
        public event Action<string, int, string> HitRegistered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    "[WILDLIFE] Duplicate WildlifeScoreManager removed.");
                Destroy(this);
                return;
            }

            Instance = this;
            RaiseScoreChanged();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterHit(
            WildlifeSpeciesDefinition definition,
            int awardedScore,
            string hitLabel)
        {
            if (!ScoringEnabled)
            {
                Debug.Log(
                    "[WILDLIFE] Hit ignored because scoring is disabled.");

                return;
            }

            awardedScore = Mathf.Max(0, awardedScore);

            TotalScore += awardedScore;
            TotalHits++;
            CurrentStreak++;
            BestStreak = Mathf.Max(BestStreak, CurrentStreak);

            string displayName =
                definition != null &&
                !string.IsNullOrWhiteSpace(definition.displayName)
                    ? definition.displayName
                    : "Animal";

            RaiseScoreChanged();
            HitRegistered?.Invoke(
                displayName,
                awardedScore,
                hitLabel);

            Debug.Log(
                "[WILDLIFE] Hit" +
                " | species=" + displayName +
                " | hitbox=" + hitLabel +
                " | awarded=" + awardedScore +
                " | total=" + TotalScore);
        }

        public void RegisterMiss()
        {
            if (!ScoringEnabled)
            {
                return;
            }

            CurrentStreak = 0;
            RaiseScoreChanged();
        }

        public void SetScoringEnabled(
            bool enabled)
        {
            if (ScoringEnabled == enabled)
            {
                return;
            }

            ScoringEnabled = enabled;

            Debug.Log(
                "[WILDLIFE] Scoring enabled=" +
                ScoringEnabled);
        }

        public void ResetScore()
        {
            TotalScore = 0;
            TotalHits = 0;
            CurrentStreak = 0;
            BestStreak = 0;
            RaiseScoreChanged();
        }

        private void RaiseScoreChanged()
        {
            ScoreChanged?.Invoke(TotalScore, TotalHits);
        }
    }
}