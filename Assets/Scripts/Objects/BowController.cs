using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
<<<<<<< Updated upstream
using UnityEngine.XR.Interaction.Toolkit.Interactors;
=======
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Debug = UnityEngine.Debug;
>>>>>>> Stashed changes

public class BowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stringAnchor;
    [SerializeField] private Transform nockPoint;
    [SerializeField] private GameObject arrowPrefab;

    [Header("Settings")]
    [SerializeField] private float maxDrawDistance = 0.5f;
    [SerializeField] private float maxLaunchForce = 30f;
    [SerializeField] private bool autoSpawnArrowOnDraw = true;

    private Transform drawTransform;
    private GameObject currentArrow;
    private ArrowController currentArrowController;
    private bool isDrawing;
    private float drawAmount;

    private XRGrabInteractable bowGrabInteractable;

<<<<<<< Updated upstream
    void Awake()
    {
        bowGrab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        bowGrab.selectEntered.AddListener(OnBowGrabbed);
        bowGrab.selectExited.AddListener(OnBowReleased);
    }

    void Update()
=======
    private void Awake()
>>>>>>> Stashed changes
    {
        bowGrabInteractable = GetComponent<XRGrabInteractable>();

        if (bowGrabInteractable == null)
        {
            Debug.LogError("[BowController] Missing XRGrabInteractable on Bow.");
            return;
        }

        bowGrabInteractable.selectEntered.AddListener(OnBowGrabbed);
        bowGrabInteractable.selectExited.AddListener(OnBowReleased);
    }

    private void OnDestroy()
    {
        if (bowGrabInteractable == null)
        {
            return;
        }

        bowGrabInteractable.selectEntered.RemoveListener(OnBowGrabbed);
        bowGrabInteractable.selectExited.RemoveListener(OnBowReleased);
    }

    private void Update()
    {
        if (!isDrawing || drawTransform == null || stringAnchor == null || nockPoint == null)
        {
            return;
        }

        UpdateDraw();
    }

    public void StartDraw(Transform pullingTransform)
    {
        Debug.Log("[BowController] StartDraw called.");

        if (pullingTransform == null)
        {
            Debug.LogWarning("[BowController] StartDraw failed: pullingTransform is null.");
            return;
        }

        if (stringAnchor == null)
        {
            Debug.LogWarning("[BowController] StartDraw failed: stringAnchor is not assigned.");
            return;
        }

        if (nockPoint == null)
        {
            Debug.LogWarning("[BowController] StartDraw failed: nockPoint is not assigned.");
            return;
        }

        if (arrowPrefab == null)
        {
            Debug.LogWarning("[BowController] StartDraw failed: arrowPrefab is not assigned.");
            return;
        }

        if (isDrawing)
        {
            Debug.Log("[BowController] Already drawing.");
            return;
        }

        drawTransform = pullingTransform;
        isDrawing = true;
        drawAmount = 0f;

        if (autoSpawnArrowOnDraw)
        {
            SpawnArrowAtNockPoint();
        }
    }

    public void Release()
    {
        Debug.Log("[BowController] Release called.");

        if (!isDrawing)
        {
            Debug.Log("[BowController] Release ignored: not currently drawing.");
            return;
        }

        isDrawing = false;

        if (currentArrow == null || currentArrowController == null)
        {
            Debug.LogWarning("[BowController] Release failed: no current arrow.");
            ResetDrawState();
            return;
        }

        float launchForce = drawAmount * maxLaunchForce;

        if (launchForce <= 0.05f)
        {
            Debug.LogWarning("[BowController] Release ignored: draw amount is too small.");
            Destroy(currentArrow);
            ResetDrawState();
            return;
        }

        Vector3 launchDirection = nockPoint.forward.normalized;

        currentArrow.transform.SetParent(null, true);
        currentArrowController.Launch(launchDirection, launchForce);

        Debug.Log($"[BowController] Arrow launched. DrawAmount={drawAmount:F2}, Force={launchForce:F2}, Direction={launchDirection}");

        currentArrow = null;
        currentArrowController = null;
        ResetDrawState();
    }

    private void UpdateDraw()
    {
        Vector3 drawVector = drawTransform.position - stringAnchor.position;
        float drawDistance = Mathf.Clamp(drawVector.magnitude, 0f, maxDrawDistance);

        drawAmount = Mathf.Clamp01(drawDistance / maxDrawDistance);

        Vector3 drawDirection = drawVector.sqrMagnitude > 0.0001f
            ? drawVector.normalized
            : -nockPoint.forward;

        Vector3 newNockPosition = stringAnchor.position + drawDirection * drawDistance;

        nockPoint.position = newNockPosition;

        if (currentArrow != null)
        {
            currentArrow.transform.position = nockPoint.position;
            currentArrow.transform.rotation = nockPoint.rotation;
        }
    }

    private void SpawnArrowAtNockPoint()
    {
        currentArrow = Instantiate(arrowPrefab, nockPoint.position, nockPoint.rotation);
        currentArrow.name = "Arrow_Runtime";

        currentArrowController = currentArrow.GetComponent<ArrowController>();

        if (currentArrowController == null)
        {
            Debug.LogWarning("[BowController] Spawned arrow has no ArrowController.");
            return;
        }

        currentArrowController.SetNocked(nockPoint);

        Debug.Log("[BowController] Arrow spawned and nocked.");
    }

    private void ResetDrawState()
    {
        drawTransform = null;
        drawAmount = 0f;
    }

    private void OnBowGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("[BowController] Bow grabbed.");
    }

    private void OnBowReleased(SelectExitEventArgs args)
    {
        Debug.Log("[BowController] Bow released. Arrow will NOT be launched from bow release.");

        // Important:
        // Do not call Release() here.
        // The arrow should only be launched when the string/draw handle is released.
    }
}