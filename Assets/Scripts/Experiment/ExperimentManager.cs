using UnityEngine;

public class ExperimentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExperimentLogger experimentLogger;
    [SerializeField] private TargetScoring targetScoring;

    [Header("Experiment Settings")]
    [SerializeField] private string participantId = "P01";
    [SerializeField] private InputMode currentInputMode = InputMode.Controller;
    [SerializeField] private int maxShotsPerCondition = 5;
    [SerializeField] private bool autoStartNextShot = true;

    [Header("Runtime Info")]
    [SerializeField] private int currentShotNumber = 0;
    [SerializeField] private bool shotInProgress = false;

    private float shotStartTime;
    private int failedGrabs;
    private int accidentalReleases;
    private int trackingLosses;

    public InputMode CurrentInputMode => currentInputMode;
    public int CurrentShotNumber => currentShotNumber;
    public bool ShotInProgress => shotInProgress;

    private void Awake()
    {
        if (experimentLogger == null)
        {
            experimentLogger = GetComponent<ExperimentLogger>();
        }
    }

    public void SetParticipantId(string newParticipantId)
    {
        participantId = string.IsNullOrWhiteSpace(newParticipantId)
            ? "P_UNKNOWN"
            : newParticipantId.Trim();
    }

    public void SetTargetScoring(TargetScoring newTargetScoring)
    {
        targetScoring = newTargetScoring;
    }

    public void StartControllerCondition()
    {
        StartCondition(InputMode.Controller);
    }

    public void StartHandTrackingCondition()
    {
        StartCondition(InputMode.HandTracking);
    }

    public void StartCondition(InputMode mode)
    {
        currentInputMode = mode;
        currentShotNumber = 0;
        shotInProgress = false;
        ResetErrorCounters();

        Debug.Log($"[ExperimentManager] Started condition: {currentInputMode}");

        StartShot();
    }

    public void StartShot()
    {
        if (currentShotNumber >= maxShotsPerCondition)
        {
            Debug.Log($"[ExperimentManager] Condition already completed: {currentInputMode}");
            return;
        }

        currentShotNumber++;
        shotStartTime = Time.time;
        shotInProgress = true;
        ResetErrorCounters();

        Debug.Log($"[ExperimentManager] Shot started: {currentShotNumber}/{maxShotsPerCondition}, mode={currentInputMode}");
    }

    public void RegisterArrowReleased()
    {
        if (!shotInProgress)
        {
            Debug.LogWarning("[ExperimentManager] Arrow released, but no shot is currently in progress.");
            return;
        }

        Debug.Log($"[ExperimentManager] Arrow released for shot {currentShotNumber}.");
    }

    public void RegisterTargetHit(Vector3 hitPoint)
    {
        int score = 0;
        float distance = -1f;

        if (targetScoring != null)
        {
            score = targetScoring.CalculateScore(hitPoint);
            distance = targetScoring.GetDistanceFromCenter(hitPoint);
        }
        else
        {
            Debug.LogWarning("[ExperimentManager] TargetScoring is missing. Hit will be logged with score 0 and distance -1.");
        }

        FinishShot(true, score, distance, "Target hit.");
    }

    public void RegisterMiss(Vector3 hitPoint, string reason = "Miss or non-target collision.")
    {
        float distance = -1f;

        if (targetScoring != null)
        {
            distance = targetScoring.GetDistanceFromCenter(hitPoint);
        }

        FinishShot(false, 0, distance, reason);
    }

    public void RegisterFailedGrab()
    {
        failedGrabs++;
    }

    public void RegisterAccidentalRelease()
    {
        accidentalReleases++;
    }

    public void RegisterTrackingLoss()
    {
        trackingLosses++;
    }

    private void FinishShot(bool hit, int score, float distanceFromCenter, string notes)
    {
        if (!shotInProgress)
        {
            Debug.LogWarning("[ExperimentManager] Tried to finish a shot, but no shot is currently in progress.");
            return;
        }

        float timeToShoot = Time.time - shotStartTime;

        ShotData data = new ShotData
        {
            participantId = participantId,
            inputMode = currentInputMode,
            shotNumber = currentShotNumber,
            hit = hit,
            score = score,
            timeToShoot = timeToShoot,
            distanceFromCenter = distanceFromCenter,
            failedGrabs = failedGrabs,
            accidentalReleases = accidentalReleases,
            trackingLosses = trackingLosses,
            notes = notes
        };

        if (experimentLogger != null)
        {
            experimentLogger.LogShot(data);
        }
        else
        {
            Debug.LogWarning("[ExperimentManager] ExperimentLogger is missing. Data was not saved.");
        }

        shotInProgress = false;

        if (autoStartNextShot && currentShotNumber < maxShotsPerCondition)
        {
            StartShot();
        }
        else if (currentShotNumber >= maxShotsPerCondition)
        {
            Debug.Log($"[ExperimentManager] Finished all shots for condition: {currentInputMode}");
        }
    }

    private void ResetErrorCounters()
    {
        failedGrabs = 0;
        accidentalReleases = 0;
        trackingLosses = 0;
    }
}
