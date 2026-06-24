using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>
/// Keeps the bow upright only after it has been released.
///
/// While the bow is held, this component does absolutely nothing.
/// Position and rotation are controlled entirely by Meta Interaction SDK.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(11000)]
public sealed class BowUprightAfterRelease : MonoBehaviour
{
    [SerializeField]
    private GrabInteractable controllerGrab;

    [SerializeField]
    private HandGrabInteractable handGrab;

    [SerializeField]
    private Rigidbody bowRigidbody;

    [SerializeField]
    private bool snapImmediatelyOnRelease = true;

    [SerializeField, Min(0f)]
    private float correctionSpeed = 20f;

    [SerializeField]
    private bool correctAtStartup = true;

    private bool wasHeld;
    private bool correctionActive;
    private bool initialized;

    private Quaternion releasedTargetRotation;

    public void Configure(
        GrabInteractable controllerInteractable,
        HandGrabInteractable handInteractable,
        Rigidbody rigidbodyReference,
        bool snapImmediately,
        float speed,
        bool correctInitially)
    {
        controllerGrab =
            controllerInteractable;

        handGrab =
            handInteractable;

        bowRigidbody =
            rigidbodyReference;

        snapImmediatelyOnRelease =
            snapImmediately;

        correctionSpeed =
            Mathf.Max(0f, speed);

        correctAtStartup =
            correctInitially;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        wasHeld = false;
        correctionActive = false;
        initialized = false;
    }

    private void LateUpdate()
    {
        bool isHeld =
            IsCurrentlyHeld();

        if (isHeld)
        {
            // Important:
            // Never modify the bow while a hand/controller holds it.
            wasHeld = true;
            correctionActive = false;
            initialized = true;
            return;
        }

        bool shouldBeginCorrection =
            wasHeld ||
            (
                !initialized &&
                correctAtStartup
            );

        if (shouldBeginCorrection)
        {
            BeginReleasedCorrection();

            wasHeld = false;
            initialized = true;
        }

        if (!correctionActive)
        {
            return;
        }

        if (snapImmediatelyOnRelease)
        {
            transform.rotation =
                releasedTargetRotation;

            correctionActive = false;
            return;
        }

        float interpolation =
            1f -
            Mathf.Exp(
                -correctionSpeed *
                Time.deltaTime);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                releasedTargetRotation,
                interpolation);

        if (
            Quaternion.Angle(
                transform.rotation,
                releasedTargetRotation) <
            0.1f
        )
        {
            transform.rotation =
                releasedTargetRotation;

            correctionActive = false;
        }
    }

    private bool IsCurrentlyHeld()
    {
        bool controllerHolding =
            controllerGrab != null &&
            controllerGrab
                .SelectingInteractors
                .Any();

        bool handHolding =
            handGrab != null &&
            handGrab
                .SelectingInteractors
                .Any();

        return
            controllerHolding ||
            handHolding;
    }

    private void BeginReleasedCorrection()
    {
        Vector3 horizontalStringDirection =
            Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up);

        if (
            horizontalStringDirection
                .sqrMagnitude <
            0.0001f
        )
        {
            horizontalStringDirection =
                Vector3.forward;
        }

        releasedTargetRotation =
            Quaternion.LookRotation(
                horizontalStringDirection.normalized,
                Vector3.up);

        if (bowRigidbody != null)
        {
            bowRigidbody.angularVelocity =
                Vector3.zero;
        }

        correctionActive = true;
    }

    private void ResolveReferences()
    {
        if (controllerGrab == null)
        {
            controllerGrab =
                GetComponent<GrabInteractable>();
        }

        if (handGrab == null)
        {
            handGrab =
                GetComponent<HandGrabInteractable>();
        }

        if (bowRigidbody == null)
        {
            bowRigidbody =
                GetComponent<Rigidbody>();
        }
    }
}
