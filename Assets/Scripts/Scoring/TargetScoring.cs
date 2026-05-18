using UnityEngine;

public class TargetScoring : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Transform targetCenter;

    [Header("Scoring Settings")]
    [SerializeField] private float maxScoringRadius = 0.5f;
    [SerializeField] private int maxScore = 10;

    public Transform TargetCenter => targetCenter;
    public float MaxScoringRadius => maxScoringRadius;

    public int CalculateScore(Vector3 hitPoint)
    {
        if (targetCenter == null)
        {
            Debug.LogWarning("[TargetScoring] TargetCenter is not assigned.");
            return 0;
        }

        float distance = GetDistanceFromCenter(hitPoint);

        if (distance > maxScoringRadius)
        {
            return 0;
        }

        float normalizedDistance = Mathf.Clamp01(distance / maxScoringRadius);
        int score = Mathf.RoundToInt(maxScore * (1f - normalizedDistance));

        return Mathf.Clamp(score, 0, maxScore);
    }

    public float GetDistanceFromCenter(Vector3 hitPoint)
    {
        if (targetCenter == null)
        {
            return -1f;
        }

        return Vector3.Distance(hitPoint, targetCenter.position);
    }
}
