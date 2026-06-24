using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

[DefaultExecutionOrder(33000)]
[DisallowMultipleComponent]
public sealed class BowExplicitSourceOrientation :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform orientationFrame;

    [SerializeField]
    private HandGrabInteractable handGrab;

    [SerializeField]
    private GrabInteractable controllerGrab;

    [SerializeField]
    private Transform head;

    [Header("Real Hand Profiles")]
    [SerializeField]
    private float leftHandYawDegrees =
        90f;

    [SerializeField]
    private float rightHandYawDegrees =
        -90f;

    [Header("Controller Base Profiles")]
    [SerializeField]
    private float leftControllerBaseYawDegrees =
        90f;

    [SerializeField]
    private float rightControllerBaseYawDegrees =
        90f;

    [Header("Controller Correction")]
    [SerializeField]
    private Vector3 controllerCorrectionEuler =
        new Vector3(
            0f,
            90f,
            90f);

    [Header("Release")]
    [SerializeField]
    private Vector3 releasedFrameEuler =
        Vector3.zero;

    [Header("Debug")]
    [SerializeField]
    private bool logProfileChanges =
        true;

    private GripSource lastSource =
        GripSource.None;

    private PlayerSide lastSide =
        PlayerSide.Unknown;

    private enum GripSource
    {
        None,
        RealHand,
        Controller
    }

    private enum PlayerSide
    {
        Unknown,
        Left,
        Right
    }

    public void Configure(
        Transform frame,
        HandGrabInteractable handInteractable,
        GrabInteractable controllerInteractable,
        Transform headTransform)
    {
        orientationFrame =
            frame;

        handGrab =
            handInteractable;

        controllerGrab =
            controllerInteractable;

        head =
            headTransform;

        // STANGA ramane corecta.
        leftHandYawDegrees =
            90f;

        leftControllerBaseYawDegrees =
            90f;

        // DREAPTA se intoarce cu 180 grade fata de varianta precedenta.
        rightHandYawDegrees =
            -90f;

        rightControllerBaseYawDegrees =
            90f;

        controllerCorrectionEuler =
            new Vector3(
                0f,
                90f,
                90f);

        releasedFrameEuler =
            Vector3.zero;

        ApplyReleasedRotation();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        lastSource =
            GripSource.None;

        lastSide =
            PlayerSide.Unknown;

        ApplyReleasedRotation();
    }

    private void OnDisable()
    {
        lastSource =
            GripSource.None;

        lastSide =
            PlayerSide.Unknown;
    }

    private void LateUpdate()
    {
        ResolveReferences();

        GripSource source;
        PlayerSide side;

        if (
            TryResolveCurrentGrip(
                out source,
                out side)
        )
        {
            ApplyProfile(
                source,
                side);

            if (
                logProfileChanges &&
                (
                    source != lastSource ||
                    side != lastSide
                )
            )
            {
                Quaternion rotation =
                    CalculateProfileRotation(
                        source,
                        side);

                Debug.Log(
                    "[BOW EXPLICIT ORIENTATION] " +
                    "Source = " +
                    source +
                    " | Side = " +
                    side +
                    " | Frame Euler = " +
                    ToSignedEuler(
                        rotation));
            }

            lastSource =
                source;

            lastSide =
                side;

            return;
        }

        if (
            lastSource != GripSource.None ||
            lastSide != PlayerSide.Unknown
        )
        {
            if (logProfileChanges)
            {
                Debug.Log(
                    "[BOW EXPLICIT ORIENTATION] " +
                    "Bow released. Frame reset to rest orientation.");
            }
        }

        lastSource =
            GripSource.None;

        lastSide =
            PlayerSide.Unknown;

        ApplyReleasedRotation();
    }

    private bool TryResolveCurrentGrip(
        out GripSource source,
        out PlayerSide side)
    {
        source =
            GripSource.None;

        side =
            PlayerSide.Unknown;

        if (controllerGrab != null)
        {
            foreach (
                GrabInteractor interactor
                in controllerGrab.SelectingInteractors
            )
            {
                if (interactor == null)
                {
                    continue;
                }

                source =
                    GripSource.Controller;

                side =
                    ResolveSideFromHierarchy(
                        interactor.transform);

                if (
                    side ==
                    PlayerSide.Unknown
                )
                {
                    side =
                        ResolveSideFromPosition(
                            interactor.transform);
                }

                return true;
            }
        }

        if (handGrab != null)
        {
            foreach (
                HandGrabInteractor interactor
                in handGrab.SelectingInteractors
            )
            {
                if (interactor == null)
                {
                    continue;
                }

                bool controllerDriven =
                    IsControllerDrivenHand(
                        interactor);

                source =
                    controllerDriven
                        ? GripSource.Controller
                        : GripSource.RealHand;

                side =
                    ResolveHandSide(
                        interactor);

                return true;
            }
        }

        return false;
    }

    private Quaternion CalculateProfileRotation(
        GripSource source,
        PlayerSide side)
    {
        if (source == GripSource.RealHand)
        {
            float handYaw =
                side == PlayerSide.Left
                    ? leftHandYawDegrees
                    : rightHandYawDegrees;

            return Quaternion.Euler(
                0f,
                handYaw,
                0f);
        }

        if (source == GripSource.Controller)
        {
            float controllerBaseYaw =
                side == PlayerSide.Left
                    ? leftControllerBaseYawDegrees
                    : rightControllerBaseYawDegrees;

            Quaternion baseRotation =
                Quaternion.Euler(
                    0f,
                    controllerBaseYaw,
                    0f);

            Quaternion controllerCorrection =
                Quaternion.Euler(
                    controllerCorrectionEuler);

            return
                controllerCorrection *
                baseRotation;
        }

        return Quaternion.Euler(
            releasedFrameEuler);
    }

    private void ApplyProfile(
        GripSource source,
        PlayerSide side)
    {
        if (orientationFrame == null)
        {
            return;
        }

        Quaternion profileRotation =
            CalculateProfileRotation(
                source,
                side);

        orientationFrame.localPosition =
            Vector3.zero;

        orientationFrame.localRotation =
            profileRotation;

        orientationFrame.localScale =
            Vector3.one;
    }

    private void ApplyReleasedRotation()
    {
        if (orientationFrame == null)
        {
            return;
        }

        orientationFrame.localPosition =
            Vector3.zero;

        orientationFrame.localRotation =
            Quaternion.Euler(
                releasedFrameEuler);

        orientationFrame.localScale =
            Vector3.one;
    }

    private PlayerSide ResolveHandSide(
        HandGrabInteractor interactor)
    {
        if (
            interactor != null &&
            interactor.Hand != null
        )
        {
            Handedness handedness =
                interactor.Hand.Handedness;

            if (handedness == Handedness.Left)
            {
                return PlayerSide.Left;
            }

            if (handedness == Handedness.Right)
            {
                return PlayerSide.Right;
            }
        }

        if (interactor != null)
        {
            PlayerSide hierarchySide =
                ResolveSideFromHierarchy(
                    interactor.transform);

            if (
                hierarchySide !=
                PlayerSide.Unknown
            )
            {
                return hierarchySide;
            }

            return ResolveSideFromPosition(
                interactor.transform);
        }

        return PlayerSide.Unknown;
    }

    private bool IsControllerDrivenHand(
        HandGrabInteractor interactor)
    {
        if (interactor == null)
        {
            return false;
        }

        string typeName =
            interactor
                .GetType()
                .Name
                .ToLowerInvariant();

        if (
            typeName.Contains("touchcontroller") ||
            typeName.Contains("controllerhand") ||
            typeName.Contains("synthetichand")
        )
        {
            return true;
        }

        Transform current =
            interactor.transform;

        while (current != null)
        {
            string lowerName =
                current.name
                    .ToLowerInvariant();

            if (
                lowerName.Contains("touchcontroller") ||
                lowerName.Contains("controllerhand") ||
                lowerName.Contains("controllerdriven") ||
                lowerName.Contains("synthetichand")
            )
            {
                return true;
            }

            current =
                current.parent;
        }

        return false;
    }

    private PlayerSide ResolveSideFromHierarchy(
        Transform start)
    {
        Transform current =
            start;

        while (current != null)
        {
            string lowerName =
                current.name
                    .ToLowerInvariant();

            if (lowerName.Contains("left"))
            {
                return PlayerSide.Left;
            }

            if (lowerName.Contains("right"))
            {
                return PlayerSide.Right;
            }

            current =
                current.parent;
        }

        return PlayerSide.Unknown;
    }

    private PlayerSide ResolveSideFromPosition(
        Transform source)
    {
        ResolveHead();

        if (
            source == null ||
            head == null
        )
        {
            return PlayerSide.Unknown;
        }

        Vector3 localPosition =
            head.InverseTransformPoint(
                source.position);

        return localPosition.x < 0f
            ? PlayerSide.Left
            : PlayerSide.Right;
    }

    private void ResolveReferences()
    {
        if (orientationFrame == null)
        {
            orientationFrame =
                transform.Find(
                    "BowOrientationFrame");
        }

        if (handGrab == null)
        {
            handGrab =
                GetComponent<
                    HandGrabInteractable>();
        }

        if (controllerGrab == null)
        {
            controllerGrab =
                GetComponent<
                    GrabInteractable>();
        }

        ResolveHead();
    }

    private void ResolveHead()
    {
        if (head != null)
        {
            return;
        }

        Camera mainCamera =
            Camera.main;

        if (mainCamera != null)
        {
            head =
                mainCamera.transform;

            return;
        }

        GameObject centerEyeAnchor =
            GameObject.Find(
                "CenterEyeAnchor");

        if (centerEyeAnchor != null)
        {
            head =
                centerEyeAnchor.transform;
        }
    }

    private string ToSignedEuler(
        Quaternion rotation)
    {
        Vector3 euler =
            rotation.eulerAngles;

        euler.x =
            NormalizeAngle(
                euler.x);

        euler.y =
            NormalizeAngle(
                euler.y);

        euler.z =
            NormalizeAngle(
                euler.z);

        return string.Format(
            "({0:0}, {1:0}, {2:0})",
            euler.x,
            euler.y,
            euler.z);
    }

    private float NormalizeAngle(
        float angle)
    {
        return angle > 180f
            ? angle - 360f
            : angle;
    }
}
