using UnityEngine;
using UnityEngine.UI;

public sealed class TrajectoryToggleController :
    MonoBehaviour
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
        trajectoryPreview =
            preview;

        statusText =
            configuredStatusText;

        hintText =
            configuredHintText;

        backgroundImage =
            configuredBackground;

        uiButton =
            configuredButton;

        BindUiButton();
        HideInputHint();

        SetTrajectoryEnabled(
            trajectoryEnabledOnStart);
    }

    private void Awake()
    {
        BindUiButton();
        HideInputHint();
    }

    private void OnEnable()
    {
        BindUiButton();
        HideInputHint();
    }

    private void Start()
    {
        if (trajectoryPreview == null)
        {
            trajectoryPreview =
                FindFirstObjectByType
                    <ArrowTrajectoryPreview>();
        }

        BindUiButton();
        HideInputHint();

        SetTrajectoryEnabled(
            trajectoryEnabledOnStart);
    }

    private void OnDisable()
    {
        if (uiButton != null)
        {
            uiButton.onClick.RemoveListener(
                ToggleTrajectory);
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

        SetTrajectoryEnabled(
            nextState);

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
                .SetTrajectoryEnabled(
                    enabled);
        }

        UpdateVisualState(
            enabled);

        HideInputHint();

        Debug.Log(
            "[AIM ASSIST] Trajectory " +
            (
                enabled
                    ? "ENABLED"
                    : "DISABLED"
            ));
    }

    private void BindUiButton()
    {
        if (uiButton == null)
        {
            return;
        }

        uiButton.interactable =
            true;

        uiButton.onClick.RemoveListener(
            ToggleTrajectory);

        uiButton.onClick.AddListener(
            ToggleTrajectory);
    }

    private void HideInputHint()
    {
        if (hintText == null)
        {
            return;
        }

        hintText.text =
            string.Empty;

        if (hintText.gameObject.activeSelf)
        {
            hintText.gameObject.SetActive(
                false);
        }
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