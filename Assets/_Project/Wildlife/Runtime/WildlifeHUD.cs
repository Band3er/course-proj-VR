using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestArchery.Wildlife
{
    public sealed class WildlifeHUD : MonoBehaviour
    {
        [SerializeField] private WildlifeScoreManager scoreManager;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text hitText;
        [SerializeField] private CanvasGroup hitCanvasGroup;

        private Coroutine hitRoutine;

        public void Configure(
            WildlifeScoreManager manager,
            Text scoreLabel,
            Text hitLabel,
            CanvasGroup popupGroup)
        {
            scoreManager = manager;
            scoreText = scoreLabel;
            hitText = hitLabel;
            hitCanvasGroup = popupGroup;
        }

        private void OnEnable()
        {
            if (scoreManager == null)
            {
                scoreManager = WildlifeScoreManager.Instance;
            }

            Subscribe();
            RefreshScore();
            HidePopupImmediately();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (scoreManager == null)
            {
                return;
            }

            scoreManager.ScoreChanged -= OnScoreChanged;
            scoreManager.HitRegistered -= OnHitRegistered;

            scoreManager.ScoreChanged += OnScoreChanged;
            scoreManager.HitRegistered += OnHitRegistered;
        }

        private void Unsubscribe()
        {
            if (scoreManager == null)
            {
                return;
            }

            scoreManager.ScoreChanged -= OnScoreChanged;
            scoreManager.HitRegistered -= OnHitRegistered;
        }

        private void RefreshScore()
        {
            int score =
                scoreManager != null
                    ? scoreManager.TotalScore
                    : 0;

            int hits =
                scoreManager != null
                    ? scoreManager.TotalHits
                    : 0;

            OnScoreChanged(score, hits);
        }

        private void OnScoreChanged(int totalScore, int totalHits)
        {
            if (scoreText == null)
            {
                return;
            }

            scoreText.text =
                "SCORE  " +
                totalScore.ToString("N0") +
                "\nHITS  " +
                totalHits;
        }

        private void OnHitRegistered(
            string animalName,
            int awardedScore,
            string hitLabel)
        {
            if (hitText == null || hitCanvasGroup == null)
            {
                return;
            }

            if (hitRoutine != null)
            {
                StopCoroutine(hitRoutine);
            }

            hitRoutine = StartCoroutine(
                ShowHitRoutine(
                    animalName,
                    awardedScore,
                    hitLabel));
        }

        private IEnumerator ShowHitRoutine(
            string animalName,
            int awardedScore,
            string hitLabel)
        {
            string suffix =
                string.Equals(
                    hitLabel,
                    "Head",
                    System.StringComparison.OrdinalIgnoreCase)
                    ? " HEADSHOT"
                    : string.Empty;

            hitText.text =
                animalName.ToUpperInvariant() +
                suffix +
                "  +" +
                awardedScore;

            hitCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(0.85f);

            float fadeTime = 0.45f;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                hitCanvasGroup.alpha =
                    1f - Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }

            HidePopupImmediately();
            hitRoutine = null;
        }

        private void HidePopupImmediately()
        {
            if (hitCanvasGroup != null)
            {
                hitCanvasGroup.alpha = 0f;
            }

            if (hitText != null)
            {
                hitText.text = string.Empty;
            }
        }
    }
}