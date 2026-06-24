using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class BowDrawController : MonoBehaviour
{
    [Header("References")]
    public Transform stringGrabPoint;
    public Transform stringRestPoint;
    public Transform bowHoldPoint;
    public Transform arrowSpawnPoint;
    public GameObject arrowPrefab;

    [Header("Draw Settings")]
    public float maxDrawDistance = 0.25f;
    public float maxLaunchForce = 40f;
    [Header("Controller Draw Tuning")]
    [Range(1f, 3f)]
    public float controllerDrawMultiplier = 1.8f;

    [Range(0f, 0.03f)]
    public float controllerDrawDeadZone = 0.0025f;

    [Range(5f, 60f)]
    public float drawSmoothingSpeed = 30f;

    public enum DrawAxis
    {
        NegZ,
        PosZ,
        NegX,
        PosX,
        NegY,
        PosY
    }

    public DrawAxis drawAxis = DrawAxis.NegZ;

    [Header("String Interaction")]
    public HandGrabInteractable stringInteractable;
    public GrabInteractable stringGrabInteractable;

    [Header("Bow Hold Interaction")]
    public HandGrabInteractable bowHandInteractable;
    public GrabInteractable bowGrabInteractable;

    [Header("Archery Rules")]
    public bool requireBowHeldBeforeDrawing = true;
    public bool requireOppositeHand = true;
    public bool disableLegacyHandednessFilters = true;

    [Header("Experiment (optional)")]
    public ArcheryEventBridge eventBridge;

    private enum PlayerSide
    {
        Unknown,
        Left,
        Right
    }

    private enum InputAuthority
    {
        None,
        Hand,
        Controller
    }

    private readonly HashSet<HandGrabInteractor>
        bowHandSelectors =
            new HashSet<HandGrabInteractor>();

    private readonly HashSet<GrabInteractor>
        bowControllerSelectors =
            new HashSet<GrabInteractor>();

    private readonly HashSet<HandGrabInteractor>
        stringHandSelectors =
            new HashSet<HandGrabInteractor>();

    private readonly HashSet<GrabInteractor>
        stringControllerSelectors =
            new HashSet<GrabInteractor>();

    private bool isBowHeld;
    private PlayerSide bowHolderSide =
        PlayerSide.Unknown;

    private InputAuthority bowAuthority =
        InputAuthority.None;

    private bool isDrawing;
    private bool isStringGrabbed;
    private bool isCancellingDraw;

    private PlayerSide stringUserSide =
        PlayerSide.Unknown;

    private InputAuthority stringAuthority =
        InputAuthority.None;

    private float currentDrawAmount;
    private float smoothDraw;
    private float targetDrawDistance;

    private Rigidbody stringRb;

    private GameObject currentArrow;
    private ArrowController currentArrowController;
    private Rigidbody arrowRb;
    private Vector3 controllerDrawStartWorldPosition;
    private bool hasControllerDrawStart;

    // Controller selector currently responsible for pulling the string.
    private GrabInteractor activeStringController;

    // Physical tracking transform used for controller displacement.
    private Transform activeControllerTrackingTransform;

    
    public bool IsDrawingNow =>
        isDrawing;

    public float CurrentDraw01 =>
        currentDrawAmount;

    public float CurrentDrawDistance =>
        smoothDraw;
private void Awake()
    {
        ResolveReferences();

        stringRb =
            stringGrabPoint != null
                ? stringGrabPoint
                    .GetComponent<Rigidbody>()
                : null;

        if (disableLegacyHandednessFilters)
        {
            DisableLegacyHandednessFilters();
        }
    }

    private void Start()
    {
        SetStringInteractionEnabled(
            !requireBowHeldBeforeDrawing);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopAllCoroutines();
        ClearRuntimeState();
    }

    private void SubscribeEvents()
    {
        if (bowHandInteractable != null)
        {
            bowHandInteractable
                .WhenSelectingInteractorAdded.Action +=
                OnBowHandGrabbed;

            bowHandInteractable
                .WhenSelectingInteractorRemoved.Action +=
                OnBowHandReleased;
        }

        if (bowGrabInteractable != null)
        {
            bowGrabInteractable
                .WhenSelectingInteractorAdded.Action +=
                OnBowControllerGrabbed;

            bowGrabInteractable
                .WhenSelectingInteractorRemoved.Action +=
                OnBowControllerReleased;
        }

        if (stringInteractable != null)
        {
            stringInteractable
                .WhenSelectingInteractorAdded.Action +=
                OnStringHandGrabbed;

            stringInteractable
                .WhenSelectingInteractorRemoved.Action +=
                OnStringHandReleased;
        }

        if (stringGrabInteractable != null)
        {
            stringGrabInteractable
                .WhenSelectingInteractorAdded.Action +=
                OnStringControllerGrabbed;

            stringGrabInteractable
                .WhenSelectingInteractorRemoved.Action +=
                OnStringControllerReleased;
        }
    }

    private void UnsubscribeEvents()
    {
        if (bowHandInteractable != null)
        {
            bowHandInteractable
                .WhenSelectingInteractorAdded.Action -=
                OnBowHandGrabbed;

            bowHandInteractable
                .WhenSelectingInteractorRemoved.Action -=
                OnBowHandReleased;
        }

        if (bowGrabInteractable != null)
        {
            bowGrabInteractable
                .WhenSelectingInteractorAdded.Action -=
                OnBowControllerGrabbed;

            bowGrabInteractable
                .WhenSelectingInteractorRemoved.Action -=
                OnBowControllerReleased;
        }

        if (stringInteractable != null)
        {
            stringInteractable
                .WhenSelectingInteractorAdded.Action -=
                OnStringHandGrabbed;

            stringInteractable
                .WhenSelectingInteractorRemoved.Action -=
                OnStringHandReleased;
        }

        if (stringGrabInteractable != null)
        {
            stringGrabInteractable
                .WhenSelectingInteractorAdded.Action -=
                OnStringControllerGrabbed;

            stringGrabInteractable
                .WhenSelectingInteractorRemoved.Action -=
                OnStringControllerReleased;
        }
    }

    // ============================================================
    // BOW HOLD
    // ============================================================

    private void OnBowHandGrabbed(
        HandGrabInteractor interactor)
    {
        RegisterBowSelector(
            interactor,
            InputAuthority.Hand);
    }

    private void OnBowControllerGrabbed(
        GrabInteractor interactor)
    {
        RegisterBowSelector(
            interactor,
            InputAuthority.Controller);
    }

    private void RegisterBowSelector(
        Component interactor,
        InputAuthority authority)
    {
        if (interactor == null)
        {
            return;
        }

        PlayerSide incomingSide =
            ResolvePlayerSide(interactor);

        if (
            isBowHeld &&
            bowHolderSide != PlayerSide.Unknown &&
            incomingSide != PlayerSide.Unknown &&
            incomingSide != bowHolderSide
        )
        {
            RejectInteractor(interactor);
            return;
        }

        if (!isBowHeld)
        {
            bowHolderSide =
                incomingSide;

            eventBridge?.OnShotStarted();
        }
        else if (
            bowHolderSide ==
                PlayerSide.Unknown &&
            incomingSide !=
                PlayerSide.Unknown
        )
        {
            bowHolderSide =
                incomingSide;
        }

        if (
            authority ==
            InputAuthority.Controller &&
            interactor is GrabInteractor
                controllerInteractor
        )
        {
            bowControllerSelectors.Add(
                controllerInteractor);

            // Un controller fizic poate produce și un callback
            // de tip controller-driven HandGrab. Controllerul
            // devine autoritatea de release.
            bowAuthority =
                InputAuthority.Controller;
        }
        else if (
            authority ==
            InputAuthority.Hand &&
            interactor is HandGrabInteractor
                handInteractor
        )
        {
            bowHandSelectors.Add(
                handInteractor);

            if (
                bowAuthority ==
                InputAuthority.None
            )
            {
                bowAuthority =
                    InputAuthority.Hand;
            }
        }

        isBowHeld = true;
        SetStringInteractionEnabled(true);
    }

    private void OnBowHandReleased(
        HandGrabInteractor interactor)
    {
        bowHandSelectors.Remove(
            interactor);

        TryFinalizeBowRelease();
    }

    private void OnBowControllerReleased(
        GrabInteractor interactor)
    {
        bowControllerSelectors.Remove(
            interactor);

        TryFinalizeBowRelease();
    }

    private void TryFinalizeBowRelease()
    {
        if (
            bowAuthority ==
                InputAuthority.Controller &&
            bowControllerSelectors.Count > 0
        )
        {
            return;
        }

        if (
            bowAuthority ==
                InputAuthority.Hand &&
            bowHandSelectors.Count > 0
        )
        {
            return;
        }

        if (
            bowAuthority ==
                InputAuthority.None &&
            (
                bowHandSelectors.Count > 0 ||
                bowControllerSelectors.Count > 0
            )
        )
        {
            return;
        }

        FinalizeBowRelease();
    }

    private void FinalizeBowRelease()
    {
        if (!isBowHeld)
        {
            return;
        }

        isBowHeld = false;
        bowHolderSide =
            PlayerSide.Unknown;

        bowAuthority =
            InputAuthority.None;

        foreach (
            HandGrabInteractor hand in
            bowHandSelectors.ToArray()
        )
        {
            if (hand != null)
            {
                StartCoroutine(
                    ForceReleaseHandNextFrame(
                        hand));
            }
        }

        foreach (
            GrabInteractor controller in
            bowControllerSelectors.ToArray()
        )
        {
            if (controller != null)
            {
                StartCoroutine(
                    ForceReleaseControllerNextFrame(
                        controller));
            }
        }

        bowHandSelectors.Clear();
        bowControllerSelectors.Clear();

        CancelCurrentDraw(
            forceInteractorRelease: true);

        if (requireBowHeldBeforeDrawing)
        {
            SetStringInteractionEnabled(false);
        }
    }

    // ============================================================
    // STRING SELECTORS
    // ============================================================

    private void OnStringHandGrabbed(
        HandGrabInteractor interactor)
    {
        RegisterStringSelector(
            interactor,
            InputAuthority.Hand);
    }

    private void OnStringControllerGrabbed(
        GrabInteractor interactor)
    {
        RegisterStringSelector(
            interactor,
            InputAuthority.Controller);
    }

    private void RegisterStringSelector(
        Component interactor,
        InputAuthority authority)
    {
        if (
            interactor == null ||
            !CanUseString(interactor)
        )
        {
            RejectInteractor(interactor);
            return;
        }

        bool wasAlreadyGrabbed =
            isStringGrabbed;

        PlayerSide incomingSide =
            ResolvePlayerSide(interactor);

        if (
            incomingSide !=
            PlayerSide.Unknown
        )
        {
            stringUserSide =
                incomingSide;
        }

        if (
            authority ==
            InputAuthority.Controller &&
            interactor is GrabInteractor
                controllerInteractor
        )
        {
            bool firstControllerSelector =
                stringControllerSelectors.Count == 0;

            stringControllerSelectors.Add(
                controllerInteractor);

            stringAuthority =
                InputAuthority.Controller;

            activeStringController =
                controllerInteractor;

            activeControllerTrackingTransform =
                GetControllerTrackingTransform(
                    controllerInteractor);

            if (
                firstControllerSelector ||
                !hasControllerDrawStart
            )
            {
                if (
                    activeControllerTrackingTransform !=
                    null
                )
                {
                    controllerDrawStartWorldPosition =
                        activeControllerTrackingTransform
                            .position;

                    hasControllerDrawStart =
                        true;

                    Debug.Log(
                        "[BOW][CONTROLLER] Grab accepted" +
                        " | interactor=" +
                        controllerInteractor.name +
                        " | trackingTransform=" +
                        activeControllerTrackingTransform.name +
                        " | source=" +
                        (
                            controllerInteractor.Rigidbody !=
                            null
                                ? "Rigidbody"
                                : "InteractorTransform"
                        ) +
                        " | start=" +
                        controllerDrawStartWorldPosition
                    );
                }
                else
                {
                    hasControllerDrawStart =
                        false;

                    Debug.LogError(
                        "[BOW][CONTROLLER] No tracking " +
                        "transform was found."
                    );
                }
            }
        }
        else if (
            authority ==
            InputAuthority.Hand &&
            interactor is HandGrabInteractor
                handInteractor
        )
        {
            stringHandSelectors.Add(
                handInteractor);

            if (
                stringAuthority ==
                InputAuthority.None
            )
            {
                stringAuthority =
                    InputAuthority.Hand;
            }
        }

        isStringGrabbed = true;

        if (stringRb != null)
        {
            stringRb.isKinematic = true;
            stringRb.useGravity = false;
        }

        if (!wasAlreadyGrabbed)
        {
            eventBridge?.OnShotStarted();
        }
    }

    private bool CanUseString(
        Component interactor)
    {
        if (
            requireBowHeldBeforeDrawing &&
            !isBowHeld
        )
        {
            return false;
        }

        PlayerSide incomingSide =
            ResolvePlayerSide(interactor);

        if (
            isStringGrabbed &&
            stringUserSide != PlayerSide.Unknown &&
            incomingSide != PlayerSide.Unknown
        )
        {
            // Permitem al doilea callback al aceleiași mâini
            // fizice, de exemplu Grab + ControllerDrivenHand.
            return
                incomingSide ==
                stringUserSide;
        }

        if (!requireOppositeHand)
        {
            return true;
        }

        if (
            bowHolderSide != PlayerSide.Unknown &&
            incomingSide != PlayerSide.Unknown
        )
        {
            return
                bowHolderSide !=
                incomingSide;
        }

        string bowToken =
            FindSideTokenFromCurrentBow();

        string stringToken =
            FindSideToken(
                interactor.transform);

        if (
            !string.IsNullOrEmpty(bowToken) &&
            !string.IsNullOrEmpty(stringToken)
        )
        {
            return
                !string.Equals(
                    bowToken,
                    stringToken,
                    StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private void OnStringHandReleased(
        HandGrabInteractor interactor)
    {
        stringHandSelectors.Remove(
            interactor);

        TryCompleteStringRelease();
    }

    private void OnStringControllerReleased(
        GrabInteractor interactor)
    {
        stringControllerSelectors.Remove(
            interactor);

        TryCompleteStringRelease();
    }

    private void TryCompleteStringRelease()
    {
        if (
            stringAuthority ==
                InputAuthority.Controller &&
            stringControllerSelectors.Count > 0
        )
        {
            return;
        }

        if (
            stringAuthority ==
                InputAuthority.Hand &&
            stringHandSelectors.Count > 0
        )
        {
            return;
        }

        if (
            stringAuthority ==
                InputAuthority.None &&
            (
                stringHandSelectors.Count > 0 ||
                stringControllerSelectors.Count > 0
            )
        )
        {
            return;
        }

        CompleteStringRelease();
    }

    private void CompleteStringRelease()
    {
        if (!isStringGrabbed)
        {
            return;
        }

        isStringGrabbed = false;
        stringAuthority =
            InputAuthority.None;

        stringUserSide =
            PlayerSide.Unknown;

        // Eliminăm eventualul selector duplicat de tip
        // controller-driven hand care a rămas activ.
        foreach (
            HandGrabInteractor hand in
            stringHandSelectors.ToArray()
        )
        {
            if (hand != null)
            {
                StartCoroutine(
                    ForceReleaseHandNextFrame(
                        hand));
            }
        }

        foreach (
            GrabInteractor controller in
            stringControllerSelectors.ToArray()
        )
        {
            if (controller != null)
            {
                StartCoroutine(
                    ForceReleaseControllerNextFrame(
                        controller));
            }
        }

        stringHandSelectors.Clear();
        stringControllerSelectors.Clear();

        activeStringController = null;
        activeControllerTrackingTransform = null;
        hasControllerDrawStart = false;
    }

    // ============================================================
    // FRAME UPDATE
    // ============================================================

    private void Update()
    {
        PruneSelectors();

        if (
            requireBowHeldBeforeDrawing &&
            !isBowHeld
        )
        {
            if (
                isDrawing ||
                isStringGrabbed
            )
            {
                CancelCurrentDraw(
                    forceInteractorRelease: true);
            }

            return;
        }

        if (isStringGrabbed)
        {
            if (!isDrawing)
            {
                StartDraw();
            }

            ComputeDraw();
        }
        else if (isDrawing)
        {
            ReleaseArrow();
        }
    }

    private void LateUpdate()
    {
        if (!isDrawing)
        {
            return;
        }

        if (
            stringRestPoint != null &&
            arrowSpawnPoint != null
        )
        {
            stringGrabPoint.position =
                stringRestPoint.position +
                (-arrowSpawnPoint.forward) *
                targetDrawDistance;
        }

        if (currentArrow != null)
        {
            currentArrow.transform.position =
                stringGrabPoint.position;

            currentArrow.transform.rotation =
                arrowSpawnPoint.rotation;
        }
    }

    private void PruneSelectors()
    {
        if (bowHandInteractable != null)
        {
            bowHandSelectors.RemoveWhere(
                item =>
                    item == null ||
                    !bowHandInteractable
                        .SelectingInteractors
                        .Any(
                            selected =>
                                selected == item));
        }

        if (bowGrabInteractable != null)
        {
            bowControllerSelectors.RemoveWhere(
                item =>
                    item == null ||
                    !bowGrabInteractable
                        .SelectingInteractors
                        .Any(
                            selected =>
                                selected == item));
        }

        if (stringInteractable != null)
        {
            stringHandSelectors.RemoveWhere(
                item =>
                    item == null ||
                    !stringInteractable
                        .SelectingInteractors
                        .Any(
                            selected =>
                                selected == item));
        }

        if (stringGrabInteractable != null)
        {
            stringControllerSelectors.RemoveWhere(
                item =>
                    item == null ||
                    !stringGrabInteractable
                        .SelectingInteractors
                        .Any(
                            selected =>
                                selected == item));
        }

        if (isBowHeld)
        {
            TryFinalizeBowRelease();
        }

        if (isStringGrabbed)
        {
            TryCompleteStringRelease();
        }
    }

    // ============================================================
    // DRAW AND ARROW
    // ============================================================

    private void StartDraw()
    {
        if (
            requireBowHeldBeforeDrawing &&
            !isBowHeld
        )
        {
            return;
        }

        isDrawing = true;

        smoothDraw = 0f;
        targetDrawDistance = 0f;
        currentDrawAmount = 0f;

        currentArrow =
            Instantiate(
                arrowPrefab,
                arrowSpawnPoint.position,
                arrowSpawnPoint.rotation);

        if (currentArrow == null)
        {
            isDrawing = false;
            return;
        }

        arrowRb =
            currentArrow
                .GetComponent<Rigidbody>();

        currentArrowController =
            currentArrow
                .GetComponent<ArrowController>();

        if (currentArrowController != null)
        {
            currentArrowController.SetNocked(
                arrowSpawnPoint);
        }
        else if (arrowRb != null)
        {
            arrowRb.isKinematic = true;
        }
    }

    private void ComputeDraw()
    {
        Transform interactionTransform =
            GetPreferredStringInteractorTransform();

        if (interactionTransform == null)
        {
            return;
        }

        Vector3 interactionWorldPosition =
            interactionTransform.position;

        HandGrabInteractor activeHand =
            stringHandSelectors
                .FirstOrDefault(
                    item =>
                        item != null &&
                        item.Hand != null);

        if (
            stringAuthority !=
                InputAuthority.Controller &&
            activeHand != null
        )
        {
            Pose rootPose;

            if (
                activeHand.Hand.GetRootPose(
                    out rootPose)
            )
            {
                interactionWorldPosition =
                    rootPose.position;
            }
        }

        Vector3 restWorldPosition =
            stringRestPoint != null
                ? stringRestPoint.position
                : stringGrabPoint.position;

        Vector3 drawDirection =
            -arrowSpawnPoint.forward;

        float rawDraw;

        if (
            stringAuthority ==
                InputAuthority.Controller &&
            hasControllerDrawStart
        )
        {
            Vector3 controllerMovement =
                interactionWorldPosition -
                controllerDrawStartWorldPosition;

            rawDraw =
                Vector3.Dot(
                    controllerMovement,
                    drawDirection);

            rawDraw *=
                controllerDrawMultiplier;

            if (rawDraw > 0f)
            {
                rawDraw =
                    Mathf.Max(
                        0f,
                        rawDraw -
                        controllerDrawDeadZone);
            }

            if (Time.frameCount % 10 == 0)
            {
                Debug.Log(
                    "[BOW][CONTROLLER] rawDraw=" +
                    rawDraw.ToString("F4") +
                    " | movement=" +
                    controllerMovement +
                    " | current=" +
                    interactionWorldPosition +
                    " | start=" +
                    controllerDrawStartWorldPosition +
                    " | tracking=" +
                    interactionTransform.name +
                    " | drawDirection=" +
                    drawDirection
                );
            }
        }
        else
        {
            rawDraw =
                Vector3.Dot(
                    interactionWorldPosition -
                    restWorldPosition,
                    drawDirection);
        }

        float drawDistance =
            Mathf.Clamp(
                rawDraw,
                0f,
                maxDrawDistance);

        smoothDraw =
            Mathf.Lerp(
                smoothDraw,
                drawDistance,
                Time.deltaTime *
                drawSmoothingSpeed);

        currentDrawAmount =
            maxDrawDistance > 0.0001f
                ? smoothDraw /
                    maxDrawDistance
                : 0f;

        targetDrawDistance =
            smoothDraw;
    }

    private Transform GetPreferredStringInteractorTransform()
    {
        // Pentru controller folosim transformul corpului fizic
        // urmarit de GrabInteractor, nu GameObject-ul container.
        if (
            stringAuthority ==
            InputAuthority.Controller
        )
        {
            GrabInteractor controller =
                activeStringController != null
                    ? activeStringController
                    : stringControllerSelectors
                        .FirstOrDefault(
                            item => item != null);

            Transform controllerTrackingTransform =
                GetControllerTrackingTransform(
                    controller);

            if (controllerTrackingTransform != null)
            {
                activeControllerTrackingTransform =
                    controllerTrackingTransform;

                return controllerTrackingTransform;
            }
        }

        HandGrabInteractor hand =
            stringHandSelectors
                .FirstOrDefault(
                    item => item != null);

        if (hand != null)
        {
            return hand.transform;
        }

        GrabInteractor fallbackController =
            stringControllerSelectors
                .FirstOrDefault(
                    item => item != null);

        return GetControllerTrackingTransform(
            fallbackController);
    }

    private Transform GetControllerTrackingTransform(
        GrabInteractor controller)
    {
        if (controller == null)
        {
            return null;
        }

        if (controller.Rigidbody != null)
        {
            return controller.Rigidbody.transform;
        }

        return controller.transform;
    }

    private void ReleaseArrow()
    {
        isDrawing = false;
        targetDrawDistance = 0f;

        if (currentArrow == null)
        {
            ResetString();
            return;
        }

        if (
            requireBowHeldBeforeDrawing &&
            !isBowHeld
        )
        {
            Destroy(currentArrow);
            ResetString();
            return;
        }

        eventBridge?.OnArrowReleased();

        float force =
            currentDrawAmount *
            maxLaunchForce;

        Debug.Log(
            "[BOW] Release" +
            " | draw01=" +
            currentDrawAmount.ToString("F4") +
            " | drawDistance=" +
            smoothDraw.ToString("F4") +
            " | force=" +
            force.ToString("F2")
        );

        if (force < 0.5f)
        {
            Destroy(currentArrow);
        }
        else if (currentArrowController != null)
        {
            currentArrowController.Launch(
                arrowSpawnPoint.forward,
                force);
        }
        else if (arrowRb != null)
        {
            arrowRb.isKinematic = false;
            arrowRb.useGravity = true;

            arrowRb.linearVelocity =
                arrowSpawnPoint.forward *
                force;
        }

        ResetString();
    }

    // ============================================================
    // CANCEL / RESET
    // ============================================================

    private void CancelCurrentDraw(
        bool forceInteractorRelease)
    {
        isCancellingDraw = true;

        HandGrabInteractor[] hands =
            stringHandSelectors.ToArray();

        GrabInteractor[] controllers =
            stringControllerSelectors.ToArray();

        isStringGrabbed = false;
        isDrawing = false;

        stringAuthority =
            InputAuthority.None;

        stringUserSide =
            PlayerSide.Unknown;

        stringHandSelectors.Clear();
        stringControllerSelectors.Clear();

        activeStringController = null;
        activeControllerTrackingTransform = null;
        hasControllerDrawStart = false;

        if (currentArrow != null)
        {
            Destroy(currentArrow);
        }

        ResetString();

        if (forceInteractorRelease)
        {
            foreach (
                HandGrabInteractor hand in
                hands
            )
            {
                if (hand != null)
                {
                    StartCoroutine(
                        ForceReleaseHandNextFrame(
                            hand));
                }
            }

            foreach (
                GrabInteractor controller in
                controllers
            )
            {
                if (controller != null)
                {
                    StartCoroutine(
                        ForceReleaseControllerNextFrame(
                            controller));
                }
            }
        }

        isCancellingDraw = false;
    }

    private void ResetString()
    {
        if (
            stringGrabPoint != null &&
            stringRestPoint != null
        )
        {
            stringGrabPoint.localPosition =
                stringRestPoint.localPosition;

            stringGrabPoint.localRotation =
                stringRestPoint.localRotation;
        }

        currentArrow = null;
        currentArrowController = null;
        arrowRb = null;

        currentDrawAmount = 0f;
        smoothDraw = 0f;
        targetDrawDistance = 0f;
    }

    private void ClearRuntimeState()
    {
        isBowHeld = false;
        bowHolderSide =
            PlayerSide.Unknown;

        bowAuthority =
            InputAuthority.None;

        bowHandSelectors.Clear();
        bowControllerSelectors.Clear();

        isDrawing = false;
        isStringGrabbed = false;
        isCancellingDraw = false;

        stringAuthority =
            InputAuthority.None;

        stringUserSide =
            PlayerSide.Unknown;

        stringHandSelectors.Clear();
        stringControllerSelectors.Clear();

        activeStringController = null;
        activeControllerTrackingTransform = null;
        hasControllerDrawStart = false;

        currentArrow = null;
        currentArrowController = null;
        arrowRb = null;
    }

    // ============================================================
    // INTERACTION UTILITIES
    // ============================================================

    private void SetStringInteractionEnabled(
        bool state)
    {
        if (stringInteractable != null)
        {
            stringInteractable.enabled =
                state;
        }

        if (stringGrabInteractable != null)
        {
            stringGrabInteractable.enabled =
                state;
        }
    }

    private IEnumerator ForceReleaseHandNextFrame(
        HandGrabInteractor interactor)
    {
        yield return null;

        if (interactor != null)
        {
            interactor.ForceRelease();
        }
    }

    private IEnumerator
        ForceReleaseControllerNextFrame(
            GrabInteractor interactor)
    {
        yield return null;

        if (interactor != null)
        {
            interactor.ForceRelease();
        }
    }

    private void RejectInteractor(
        Component interactor)
    {
        if (
            interactor is
            HandGrabInteractor hand
        )
        {
            StartCoroutine(
                ForceReleaseHandNextFrame(
                    hand));
        }
        else if (
            interactor is
            GrabInteractor controller
        )
        {
            StartCoroutine(
                ForceReleaseControllerNextFrame(
                    controller));
        }
    }

    // ============================================================
    // HANDEDNESS
    // ============================================================

    private PlayerSide ResolvePlayerSide(
        Component interactor)
    {
        if (interactor == null)
        {
            return PlayerSide.Unknown;
        }

        if (
            interactor is
                HandGrabInteractor handInteractor &&
            handInteractor.Hand != null
        )
        {
            PlayerSide directHandSide =
                ParseSide(
                    handInteractor
                        .Hand
                        .Handedness
                        .ToString());

            if (
                directHandSide !=
                PlayerSide.Unknown
            )
            {
                return directHandSide;
            }
        }

        Transform current =
            interactor.transform;

        while (current != null)
        {
            PlayerSide nameSide =
                ParseSide(current.name);

            if (
                nameSide !=
                PlayerSide.Unknown
            )
            {
                return nameSide;
            }

            foreach (
                Component component in
                current.GetComponents<Component>()
            )
            {
                PlayerSide reflectedSide =
                    TryReadHandedness(
                        component);

                if (
                    reflectedSide !=
                    PlayerSide.Unknown
                )
                {
                    return reflectedSide;
                }
            }

            current = current.parent;
        }

        Camera camera =
            Camera.main;

        if (camera != null)
        {
            float lateral =
                Vector3.Dot(
                    interactor.transform.position -
                    camera.transform.position,
                    camera.transform.right);

            if (Mathf.Abs(lateral) > 0.025f)
            {
                return lateral < 0f
                    ? PlayerSide.Left
                    : PlayerSide.Right;
            }
        }

        return PlayerSide.Unknown;
    }

    private PlayerSide TryReadHandedness(
        Component component)
    {
        if (component == null)
        {
            return PlayerSide.Unknown;
        }

        try
        {
            Type type =
                component.GetType();

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (
                string propertyName in
                new[]
                {
                    "Handedness",
                    "handedness"
                }
            )
            {
                PropertyInfo property =
                    type.GetProperty(
                        propertyName,
                        flags);

                if (
                    property != null &&
                    property
                        .GetIndexParameters()
                        .Length == 0
                )
                {
                    PlayerSide side =
                        ParseSide(
                            property
                                .GetValue(component)
                                ?.ToString());

                    if (
                        side !=
                        PlayerSide.Unknown
                    )
                    {
                        return side;
                    }
                }
            }

            foreach (
                string fieldName in
                new[]
                {
                    "Handedness",
                    "handedness",
                    "_handedness"
                }
            )
            {
                FieldInfo field =
                    type.GetField(
                        fieldName,
                        flags);

                if (field != null)
                {
                    PlayerSide side =
                        ParseSide(
                            field
                                .GetValue(component)
                                ?.ToString());

                    if (
                        side !=
                        PlayerSide.Unknown
                    )
                    {
                        return side;
                    }
                }
            }
        }
        catch
        {
            // Continuăm cu fallback-urile.
        }

        return PlayerSide.Unknown;
    }

    private PlayerSide ParseSide(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PlayerSide.Unknown;
        }

        string lower =
            value.ToLowerInvariant();

        if (lower.Contains("left"))
        {
            return PlayerSide.Left;
        }

        if (lower.Contains("right"))
        {
            return PlayerSide.Right;
        }

        return PlayerSide.Unknown;
    }

    private string FindSideTokenFromCurrentBow()
    {
        foreach (
            GrabInteractor controller in
            bowControllerSelectors
        )
        {
            string token =
                FindSideToken(
                    controller != null
                        ? controller.transform
                        : null);

            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }
        }

        foreach (
            HandGrabInteractor hand in
            bowHandSelectors
        )
        {
            string token =
                FindSideToken(
                    hand != null
                        ? hand.transform
                        : null);

            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }
        }

        return null;
    }

    private string FindSideToken(
        Transform source)
    {
        Transform current =
            source;

        while (current != null)
        {
            string lower =
                current.name.ToLowerInvariant();

            if (lower.Contains("left"))
            {
                return "left";
            }

            if (lower.Contains("right"))
            {
                return "right";
            }

            current = current.parent;
        }

        return null;
    }

    // ============================================================
    // REFERENCES
    // ============================================================

    private void ResolveReferences()
    {
        if (bowHandInteractable == null)
        {
            bowHandInteractable =
                GetComponent<HandGrabInteractable>();
        }

        if (bowGrabInteractable == null)
        {
            bowGrabInteractable =
                GetComponent<GrabInteractable>();
        }

        if (
            stringGrabPoint != null &&
            stringInteractable == null
        )
        {
            stringInteractable =
                stringGrabPoint
                    .GetComponent<HandGrabInteractable>();
        }

        if (
            stringGrabPoint != null &&
            stringGrabInteractable == null
        )
        {
            stringGrabInteractable =
                stringGrabPoint
                    .GetComponent<GrabInteractable>();
        }
    }

    private void DisableLegacyHandednessFilters()
    {
        foreach (
            Behaviour behaviour in
            GetComponentsInChildren<Behaviour>(true)
        )
        {
            if (
                behaviour != null &&
                behaviour
                    .GetType()
                    .Name ==
                "HandednessFilter"
            )
            {
                behaviour.enabled =
                    false;
            }
        }
    }
}

