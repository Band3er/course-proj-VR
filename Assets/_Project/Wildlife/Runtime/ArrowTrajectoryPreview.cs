using UnityEngine;
using UnityEngine.Rendering;

public sealed class ArrowTrajectoryPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BowDrawController bow;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform impactMarker;
    [SerializeField] private Transform ignoredPlayerRoot;

    [Header("Prediction")]
    [SerializeField, Range(16, 64)]
    private int sampleCount = 40;

    [SerializeField, Min(0.5f)]
    private float predictionDuration = 2.8f;

    [SerializeField, Range(0f, 0.25f)]
    private float minimumDraw01 = 0.025f;

    [SerializeField, Min(0f)]
    private float startForwardOffset = 0.04f;

    [SerializeField]
    private LayerMask collisionMask = ~0;

    [Header("Impact Marker")]
    [SerializeField, Min(0.001f)]
    private float impactMarkerScale = 0.035f;

    [SerializeField, Min(0f)]
    private float impactPulseSpeed = 5f;

    [SerializeField, Range(0f, 0.5f)]
    private float impactPulseAmount = 0.12f;

    private readonly RaycastHit[] raycastHits =
        new RaycastHit[24];

    private Vector3[] sampledPoints;
    private bool trajectoryEnabled = true;
    private bool hasImpact;

    public bool TrajectoryEnabled => trajectoryEnabled;

    public void Configure(
        BowDrawController bowController,
        LineRenderer configuredLineRenderer,
        Transform configuredImpactMarker,
        Transform playerRoot)
    {
        bow = bowController;
        lineRenderer = configuredLineRenderer;
        impactMarker = configuredImpactMarker;
        ignoredPlayerRoot = playerRoot;

        EnsurePointBuffer();
        ApplyRendererDefaults();
        HideTrajectory();
    }

    public void SetTrajectoryEnabled(bool enabled)
    {
        trajectoryEnabled = enabled;

        if (!trajectoryEnabled)
        {
            HideTrajectory();
        }
    }

    public void ToggleTrajectory()
    {
        SetTrajectoryEnabled(!trajectoryEnabled);
    }

    private void Awake()
    {
        if (bow == null)
        {
            bow = GetComponentInParent<BowDrawController>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        EnsurePointBuffer();
        ApplyRendererDefaults();
        HideTrajectory();
    }

    private void OnDisable()
    {
        HideTrajectory();
    }

    private void Update()
    {
        if (
            !trajectoryEnabled ||
            bow == null ||
            lineRenderer == null ||
            bow.arrowSpawnPoint == null ||
            !bow.IsDrawingNow ||
            bow.CurrentDraw01 < minimumDraw01
        )
        {
            HideTrajectory();
            return;
        }

        RenderTrajectory();

        if (hasImpact && impactMarker != null)
        {
            float pulse =
                1f +
                Mathf.Sin(Time.time * impactPulseSpeed) *
                impactPulseAmount;

            impactMarker.localScale =
                Vector3.one *
                impactMarkerScale *
                pulse;
        }
    }

    private void RenderTrajectory()
    {
        EnsurePointBuffer();

        Vector3 direction =
            bow.arrowSpawnPoint.forward.normalized;

        Vector3 startPosition =
            bow.stringGrabPoint != null
                ? bow.stringGrabPoint.position
                : bow.arrowSpawnPoint.position;

        startPosition +=
            direction * startForwardOffset;

        float launchSpeed =
            bow.CurrentDraw01 *
            bow.maxLaunchForce;

        Vector3 initialVelocity =
            direction * launchSpeed;

        float timeStep =
            predictionDuration /
            Mathf.Max(1, sampleCount - 1);

        int pointCount = 1;
        sampledPoints[0] = startPosition;

        hasImpact = false;
        Vector3 impactPoint = Vector3.zero;
        Vector3 impactNormal = Vector3.up;

        Vector3 previousPoint = startPosition;

        for (int index = 1; index < sampleCount; index++)
        {
            float time = timeStep * index;

            Vector3 nextPoint =
                startPosition +
                initialVelocity * time +
                0.5f *
                Physics.gravity *
                time *
                time;

            if (
                TryFindFirstValidHit(
                    previousPoint,
                    nextPoint,
                    out RaycastHit validHit)
            )
            {
                sampledPoints[pointCount] =
                    validHit.point;

                pointCount++;

                hasImpact = true;
                impactPoint = validHit.point;
                impactNormal = validHit.normal;
                break;
            }

            sampledPoints[pointCount] = nextPoint;
            pointCount++;
            previousPoint = nextPoint;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = pointCount;

        for (int index = 0; index < pointCount; index++)
        {
            lineRenderer.SetPosition(
                index,
                sampledPoints[index]);
        }

        if (impactMarker != null)
        {
            impactMarker.gameObject.SetActive(hasImpact);

            if (hasImpact)
            {
                impactMarker.position =
                    impactPoint +
                    impactNormal * 0.008f;

                impactMarker.rotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        impactNormal);
            }
        }
    }

    private bool TryFindFirstValidHit(
        Vector3 from,
        Vector3 to,
        out RaycastHit bestHit)
    {
        bestHit = default;

        Vector3 segment = to - from;
        float distance = segment.magnitude;

        if (distance <= 0.0001f)
        {
            return false;
        }

        Vector3 direction =
            segment / distance;

        int hitCount =
            Physics.RaycastNonAlloc(
                from,
                direction,
                raycastHits,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

        float nearestDistance =
            float.PositiveInfinity;

        bool found = false;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit =
                raycastHits[index];

            Collider collider =
                hit.collider;

            if (
                collider == null ||
                ShouldIgnoreCollider(collider)
            )
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private bool ShouldIgnoreCollider(
        Collider collider)
    {
        Transform colliderTransform =
            collider.transform;

        if (
            bow != null &&
            colliderTransform.IsChildOf(
                bow.transform)
        )
        {
            return true;
        }

        if (
            ignoredPlayerRoot != null &&
            colliderTransform.IsChildOf(
                ignoredPlayerRoot)
        )
        {
            return true;
        }

        if (
            collider.GetComponentInParent<ArrowController>() !=
            null
        )
        {
            return true;
        }

        if (
            collider.CompareTag("Player")
        )
        {
            return true;
        }

        int playerLayer =
            LayerMask.NameToLayer("Player");

        return
            playerLayer >= 0 &&
            collider.gameObject.layer == playerLayer;
    }

    private void EnsurePointBuffer()
    {
        sampleCount =
            Mathf.Clamp(sampleCount, 16, 64);

        if (
            sampledPoints == null ||
            sampledPoints.Length != sampleCount
        )
        {
            sampledPoints =
                new Vector3[sampleCount];
        }
    }

    private void ApplyRendererDefaults()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.alignment =
            LineAlignment.View;

        lineRenderer.textureMode =
            LineTextureMode.Tile;

        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
        lineRenderer.generateLightingData = false;

        lineRenderer.shadowCastingMode =
            ShadowCastingMode.Off;

        lineRenderer.receiveShadows = false;
        lineRenderer.lightProbeUsage =
            LightProbeUsage.Off;

        lineRenderer.reflectionProbeUsage =
            ReflectionProbeUsage.Off;

        lineRenderer.widthCurve =
            new AnimationCurve(
                new Keyframe(0f, 0.014f),
                new Keyframe(0.65f, 0.010f),
                new Keyframe(1f, 0.004f));

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(
                    new Color(1f, 0.92f, 0.28f),
                    0f),
                new GradientColorKey(
                    new Color(1f, 0.52f, 0.08f),
                    0.72f),
                new GradientColorKey(
                    new Color(1f, 0.18f, 0.04f),
                    1f)
            },
            new[]
            {
                new GradientAlphaKey(0.94f, 0f),
                new GradientAlphaKey(0.80f, 0.78f),
                new GradientAlphaKey(0.12f, 1f)
            });

        lineRenderer.colorGradient = gradient;
    }

    private void HideTrajectory()
    {
        hasImpact = false;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        if (impactMarker != null)
        {
            impactMarker.gameObject.SetActive(false);
        }
    }
}
