using UnityEngine;

public class ArcheryEventBridge : MonoBehaviour
{
    [SerializeField] private ExperimentManager experimentManager;

    public void OnShotStarted()
    {
        experimentManager?.StartShot();
    }

    public void OnArrowReleased()
    {
        experimentManager?.RegisterArrowReleased();
    }

    public void OnArrowHitTarget(Vector3 hitPoint)
    {
        experimentManager?.RegisterTargetHit(hitPoint);
    }

    public void OnArrowMissed(Vector3 hitPoint)
    {
        experimentManager?.RegisterMiss(hitPoint);
    }

    public void OnFailedGrab()
    {
        experimentManager?.RegisterFailedGrab();
    }

    public void OnAccidentalRelease()
    {
        experimentManager?.RegisterAccidentalRelease();
    }

    public void OnTrackingLost()
    {
        experimentManager?.RegisterTrackingLoss();
    }

    public void StartControllerCondition()
    {
        experimentManager?.StartControllerCondition();
    }

    public void StartHandTrackingCondition()
    {
        experimentManager?.StartHandTrackingCondition();
    }
}
