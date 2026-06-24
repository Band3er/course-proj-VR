using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class TrajectoryToggleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ArrowTrajectoryPreview trajectoryPreview;

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text hintText;

    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Button uiButton;

    [Header("State")]
    [SerializeField]
    private bool trajectoryEnabledOnStart = true;

    [SerializeField, Min(0.05f)]
    private float inputCooldown = 0.35f;

    private float nextAcceptedInputTime;

    public bool TrajectoryEnabled =>
        trajectoryPreview != null &&
        trajectoryPreview.TrajectoryEnabled;

    public void Configure(
        ArrowTrajectoryPreview preview,
        Text configuredStatusText,
        Text configuredHintText,
        Image configuredBackground,
        Button configuredButton)
    {
        trajectoryPreview = preview;
        statusText = configuredStatusText;
        hintText = configuredHintText;
        backgroundImage = configuredBackground;
        uiButton = configuredButton;

        if (uiButton != null)
        {
            uiButton.onClick.RemoveAllListeners();
            uiButton.onClick.AddListener(
                ToggleTrajectory);
        }

        SetTrajectoryEnabled(
            trajectoryEnabledOnStart);
    }

    private void Start()
    {
        if (trajectoryPreview == null)
        {
            trajectoryPreview =
                FindFirstObjectByType
                    <ArrowTrajectoryPreview>();
        }

        SetTrajectoryEnabled(
            trajectoryEnabledOnStart);
    }

    private void Update()
    {
        if (
            Time.unscaledTime <
            nextAcceptedInputTime
        )
        {
            return;
        }

        bool keyboardToggle = false;

#if ENABLE_INPUT_SYSTEM
        keyboardToggle =
            Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        keyboardToggle =
            Input.GetKeyDown(KeyCode.T);
#endif

        bool controllerToggle =
            OVRInput.GetDown(
                OVRInput.RawButton.LThumbstick) ||
            OVRInput.GetDown(
                OVRInput.RawButton.RThumbstick);

        if (
            keyboardToggle ||
            controllerToggle
        )
        {
            ToggleTrajectory();
        }
    }

    public void ToggleTrajectory()
    {
        if (
            Time.unscaledTime <
            nextAcceptedInputTime
        )
        {
            return;
        }

        bool nextState =
            trajectoryPreview == null ||
            !trajectoryPreview.TrajectoryEnabled;

        SetTrajectoryEnabled(nextState);

        nextAcceptedInputTime =
            Time.unscaledTime +
            inputCooldown;
    }

    public void SetTrajectoryEnabled(
        bool enabled)
    {
        if (trajectoryPreview != null)
        {
            trajectoryPreview
                .SetTrajectoryEnabled(enabled);
        }

        UpdateVisualState(enabled);

        Debug.Log(
            "[AIM ASSIST] Trajectory " +
            (enabled ? "ENABLED" : "DISABLED"));
    }

    private void UpdateVisualState(
        bool enabled)
    {
        if (statusText != null)
        {
            statusText.text =
                enabled
                    ? "TRAJECTORY  ON"
                    : "TRAJECTORY  OFF";

            statusText.color =
                enabled
                    ? new Color(
                        1f,
                        0.92f,
                        0.28f,
                        1f)
                    : new Color(
                        0.72f,
                        0.76f,
                        0.82f,
                        1f);
        }

        if (hintText != null)
        {
            hintText.text =
                "PRESS EITHER THUMBSTICK";
        }

        if (backgroundImage != null)
        {
            backgroundImage.color =
                enabled
                    ? new Color(
                        0.10f,
                        0.15f,
                        0.10f,
                        0.76f)
                    : new Color(
                        0.06f,
                        0.07f,
                        0.09f,
                        0.72f);
        }
    }
}
