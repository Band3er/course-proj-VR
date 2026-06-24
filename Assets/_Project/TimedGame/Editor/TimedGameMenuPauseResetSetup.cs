using System;
using System.Reflection;
using ForestArchery.TimedGame;
using ForestArchery.Wildlife;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TimedGameMenuPauseResetSetup
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private static Font uiFont;

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08f.1 - Install Menu Isolation Pause Reset")]
    public static void Install()
    {
        if (!ValidateScene())
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 08f.1",
                "Exit Play Mode before installing the fixes.",
                "OK");

            return;
        }

        Scene scene =
            SceneManager.GetActiveScene();

        try
        {
            GameObject systemObject =
                GameObject.Find(
                    "TimedGameSystem");

            GameObject canvasObject =
                GameObject.Find(
                    "TimedGameCanvas");

            GameObject roundHudObject =
                GameObject.Find(
                    "RoundHUD");

            if (systemObject == null)
            {
                throw new InvalidOperationException(
                    "TimedGameSystem was not found.");
            }

            if (canvasObject == null)
            {
                throw new InvalidOperationException(
                    "TimedGameCanvas was not found.");
            }

            if (roundHudObject == null)
            {
                throw new InvalidOperationException(
                    "RoundHUD was not found.");
            }

            TimedRoundController roundController =
                systemObject.GetComponent<TimedRoundController>();

            TimedGameMenuController menuController =
                systemObject.GetComponent<TimedGameMenuController>();

            TimedGameGameplayBridge gameplayBridge =
                systemObject.GetComponent<TimedGameGameplayBridge>();

            if (
                roundController == null ||
                menuController == null ||
                gameplayBridge == null
            )
            {
                throw new InvalidOperationException(
                    "Timed Game core components are incomplete.");
            }

            uiFont =
                FindExistingFont();

            CanvasGroup roundHud =
                roundHudObject.GetComponent<CanvasGroup>();

            if (roundHud == null)
            {
                roundHud =
                    Undo.AddComponent<CanvasGroup>(
                        roundHudObject);
            }

            Button pauseButton =
                FindOrCreateButton(
                    roundHudObject.transform,
                    "PauseButton",
                    "PAUSE",
                    new Vector2(
                        360f,
                        325f),
                    new Vector2(
                        190f,
                        58f),
                    23);

            GameObject pausePanelObject =
                GameObject.Find(
                    "PausePanel");

            if (pausePanelObject == null)
            {
                pausePanelObject =
                    new GameObject(
                        "PausePanel",
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(CanvasGroup));

                Undo.RegisterCreatedObjectUndo(
                    pausePanelObject,
                    "Create Timed Game Pause Panel");

                pausePanelObject.transform.SetParent(
                    canvasObject.transform,
                    false);

                RectTransform pauseRect =
                    pausePanelObject
                        .GetComponent<RectTransform>();

                pauseRect.anchorMin =
                    Vector2.zero;

                pauseRect.anchorMax =
                    Vector2.one;

                pauseRect.offsetMin =
                    Vector2.zero;

                pauseRect.offsetMax =
                    Vector2.zero;

                Image pauseBackground =
                    pausePanelObject
                        .GetComponent<Image>();

                pauseBackground.color =
                    new Color(
                        0.018f,
                        0.032f,
                        0.026f,
                        0.98f);
            }

            CanvasGroup pausePanel =
                pausePanelObject
                    .GetComponent<CanvasGroup>();

            Text pauseTitle =
                FindOrCreateText(
                    pausePanelObject.transform,
                    "Title",
                    "PAUSED",
                    62,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        250f),
                    new Vector2(
                        800f,
                        90f));

            Text pauseInfo =
                FindOrCreateText(
                    pausePanelObject.transform,
                    "Info",
                    "TIME REMAINING\n05:00\n\nCURRENT ROUND SCORE WILL BE LOST IF YOU QUIT.",
                    28,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        75f),
                    new Vector2(
                        780f,
                        260f));

            Button resumeButton =
                FindOrCreateButton(
                    pausePanelObject.transform,
                    "ResumeButton",
                    "RESUME",
                    new Vector2(
                        0f,
                        -120f),
                    new Vector2(
                        430f,
                        72f),
                    28);

            Button quitButton =
                FindOrCreateButton(
                    pausePanelObject.transform,
                    "QuitButton",
                    "QUIT ROUND",
                    new Vector2(
                        0f,
                        -215f),
                    new Vector2(
                        430f,
                        72f),
                    28);

            TimedGamePauseController pauseController =
                systemObject
                    .GetComponent<TimedGamePauseController>();

            if (pauseController == null)
            {
                pauseController =
                    Undo.AddComponent<TimedGamePauseController>(
                        systemObject);
            }

            SerializedObject pauseSerialized =
                new SerializedObject(
                    pauseController);

            SetObjectReference(
                pauseSerialized,
                "roundController",
                roundController);

            SetObjectReference(
                pauseSerialized,
                "menuController",
                menuController);

            SetObjectReference(
                pauseSerialized,
                "roundHud",
                roundHud);

            SetObjectReference(
                pauseSerialized,
                "pausePanel",
                pausePanel);

            SetObjectReference(
                pauseSerialized,
                "pauseInfoText",
                pauseInfo);

            SetObjectReference(
                pauseSerialized,
                "pauseButton",
                pauseButton);

            SetObjectReference(
                pauseSerialized,
                "resumeButton",
                resumeButton);

            SetObjectReference(
                pauseSerialized,
                "quitButton",
                quitButton);

            pauseSerialized
                .ApplyModifiedPropertiesWithoutUndo();

            ConfigureGameplayBridge(
                gameplayBridge,
                roundController);

            ConfigureTemporaryDeviceTest(
                menuController);

            UpdateModeInformationText();

            pausePanel.alpha = 0f;
            pausePanel.interactable = false;
            pausePanel.blocksRaycasts = false;

            EditorUtility.SetDirty(
                systemObject);

            EditorUtility.SetDirty(
                pausePanelObject);

            EditorUtility.SetDirty(
                roundHudObject);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();

            ValidateInstalledState();

            Selection.activeGameObject =
                systemObject;

            Debug.Log(
                "[TIMED GAME FIX] Stage 08f.1 installed." +
                "\nMenu isolation: configured" +
                "\nPause / Resume / Quit: configured" +
                "\nRound reset: configured" +
                "\nTemporary Quest test duration: 60 seconds");

            EditorUtility.DisplayDialog(
                "Stage 08f.1 Installed",
                "The requested fixes were installed:\n\n" +
                "- bow hidden and disabled outside gameplay\n" +
                "- trajectory and score HUD hidden outside gameplay\n" +
                "- Pause button during the round\n" +
                "- Resume and Quit Round menu\n" +
                "- Quit discards the current score\n" +
                "- arrows and wildlife reset between rounds\n\n" +
                "Quest test rounds are temporarily 60 seconds.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08f.1 Failed",
                exception.Message,
                "OK");
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08f.1 - Run Pause Core Self-Test")]
    public static void RunPauseCoreSelfTest()
    {
        try
        {
            TimedRoundSession session =
                new TimedRoundSession();

            session.BeginRound(
                "PAUSE_TEST",
                TimedGameInteractionMode.Controller,
                20f,
                0f);

            session.Tick(
                2f);

            float beforePause =
                session.RemainingSeconds;

            session.PauseRound();

            if (
                session.State !=
                TimedRoundState.Paused
            )
            {
                throw new InvalidOperationException(
                    "The session did not enter Paused.");
            }

            session.Tick(
                7f);

            if (
                Mathf.Abs(
                    session.RemainingSeconds -
                    beforePause) >
                0.001f
            )
            {
                throw new InvalidOperationException(
                    "The timer changed while paused.");
            }

            if (
                session.IsGameplayAllowed ||
                session.IsScoringAllowed
            )
            {
                throw new InvalidOperationException(
                    "Gameplay or scoring remained enabled while paused.");
            }

            session.ResumeRound();

            if (
                session.State !=
                TimedRoundState.Playing
            )
            {
                throw new InvalidOperationException(
                    "The session did not resume.");
            }

            session.Tick(
                1f);

            if (
                session.RemainingSeconds >=
                beforePause
            )
            {
                throw new InvalidOperationException(
                    "The timer did not continue after resume.");
            }

            session.CancelRound();

            if (
                session.State !=
                TimedRoundState.Cancelled
            )
            {
                throw new InvalidOperationException(
                    "Quit did not cancel the round.");
            }

            Debug.Log(
                "[TIMED GAME FIX] Stage 08f.1 pause core self-test passed." +
                "\nPause state: OK" +
                "\nTimer frozen while paused: OK" +
                "\nScoring/gameplay blocked while paused: OK" +
                "\nResume: OK" +
                "\nQuit/cancel: OK");

            EditorUtility.DisplayDialog(
                "Pause Core Self-Test Passed",
                "Pause, timer freeze, resume, and quit/cancel behavior are valid.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Pause Core Self-Test Failed",
                exception.Message,
                "OK");
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08f.1 - Validate Menu Isolation Pause Reset")]
    public static void Validate()
    {
        if (!ValidateScene())
        {
            return;
        }

        try
        {
            ValidateInstalledState();

            Debug.Log(
                "[TIMED GAME FIX] Stage 08f.1 validation passed.");

            EditorUtility.DisplayDialog(
                "Stage 08f.1 Validation Passed",
                "Menu isolation, pause UI, gameplay references, and round reset configuration are valid.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08f.1 Validation Failed",
                exception.Message,
                "OK");
        }
    }

    private static void ConfigureGameplayBridge(
        TimedGameGameplayBridge bridge,
        TimedRoundController roundController)
    {
        WildlifeScoreManager scoreManager =
            UnityEngine.Object
                .FindFirstObjectByType<WildlifeScoreManager>();

        global::BowDrawController bowController =
            UnityEngine.Object
                .FindFirstObjectByType<global::BowDrawController>();

        global::ArrowTrajectoryPreview trajectoryPreview =
            UnityEngine.Object
                .FindFirstObjectByType<global::ArrowTrajectoryPreview>();

        WildlifeSpawnManager groundSpawnManager =
            UnityEngine.Object
                .FindFirstObjectByType<WildlifeSpawnManager>();

        global::lb_BirdController birdController =
            UnityEngine.Object
                .FindFirstObjectByType<global::lb_BirdController>();

        GameObject trajectoryHud =
            GameObject.Find(
                "TrajectoryToggleHUD");

        GameObject scoreHud =
            GameObject.Find(
                "WildlifeHUD_RabbitPrototype");

        if (scoreManager == null)
        {
            throw new InvalidOperationException(
                "WildlifeScoreManager was not found.");
        }

        if (bowController == null)
        {
            throw new InvalidOperationException(
                "BowDrawController was not found.");
        }

        if (groundSpawnManager == null)
        {
            throw new InvalidOperationException(
                "WildlifeSpawnManager was not found.");
        }

        if (birdController == null)
        {
            throw new InvalidOperationException(
                "lb_BirdController was not found.");
        }

        if (trajectoryHud == null)
        {
            throw new InvalidOperationException(
                "TrajectoryToggleHUD was not found.");
        }

        if (scoreHud == null)
        {
            throw new InvalidOperationException(
                "WildlifeHUD_RabbitPrototype was not found.");
        }

        SerializedObject serialized =
            new SerializedObject(
                bridge);

        SetObjectReference(
            serialized,
            "roundController",
            roundController);

        SetObjectReference(
            serialized,
            "scoreManager",
            scoreManager);

        SetObjectReference(
            serialized,
            "bowController",
            bowController);

        SetObjectReference(
            serialized,
            "trajectoryPreview",
            trajectoryPreview);

        SetObjectReference(
            serialized,
            "groundSpawnManager",
            groundSpawnManager);

        SetObjectReference(
            serialized,
            "birdController",
            birdController);

        SetObjectReference(
            serialized,
            "trajectoryHudRoot",
            trajectoryHud);

        SetObjectReference(
            serialized,
            "scoreHudRoot",
            scoreHud);

        SetBoolean(
            serialized,
            "clearExistingArrowsAtRoundStart",
            true);

        SetBoolean(
            serialized,
            "resetSceneOnRoundExit",
            true);

        SetBoolean(
            serialized,
            "verboseLogging",
            false);

        serialized
            .ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureTemporaryDeviceTest(
        TimedGameMenuController menuController)
    {
        SerializedObject serialized =
            new SerializedObject(
                menuController);

        SetBoolean(
            serialized,
            "useShortDeviceIntegrationRound",
            true);

        SetFloat(
            serialized,
            "deviceIntegrationRoundDurationSeconds",
            60f);

        serialized
            .ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateInstalledState()
    {
        GameObject systemObject =
            GameObject.Find(
                "TimedGameSystem");

        if (systemObject == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem was not found.");
        }

        TimedRoundController roundController =
            systemObject.GetComponent<TimedRoundController>();

        TimedGameMenuController menuController =
            systemObject.GetComponent<TimedGameMenuController>();

        TimedGameGameplayBridge gameplayBridge =
            systemObject.GetComponent<TimedGameGameplayBridge>();

        TimedGamePauseController pauseController =
            systemObject.GetComponent<TimedGamePauseController>();

        if (
            roundController == null ||
            menuController == null ||
            gameplayBridge == null ||
            pauseController == null
        )
        {
            throw new InvalidOperationException(
                "One or more required Timed Game components are missing.");
        }

        if (
            !Enum.IsDefined(
                typeof(TimedRoundState),
                TimedRoundState.Paused)
        )
        {
            throw new InvalidOperationException(
                "TimedRoundState.Paused is missing.");
        }

        MethodInfo pauseMethod =
            typeof(TimedRoundController)
                .GetMethod(
                    "PauseRound",
                    BindingFlags.Public |
                    BindingFlags.Instance);

        MethodInfo resumeMethod =
            typeof(TimedRoundController)
                .GetMethod(
                    "ResumeRound",
                    BindingFlags.Public |
                    BindingFlags.Instance);

        if (
            pauseMethod == null ||
            resumeMethod == null
        )
        {
            throw new InvalidOperationException(
                "PauseRound or ResumeRound is missing.");
        }

        SerializedObject pauseSerialized =
            new SerializedObject(
                pauseController);

        string[] pauseReferences =
        {
            "roundController",
            "menuController",
            "roundHud",
            "pausePanel",
            "pauseInfoText",
            "pauseButton",
            "resumeButton",
            "quitButton"
        };

        foreach (string propertyName in pauseReferences)
        {
            SerializedProperty property =
                pauseSerialized.FindProperty(
                    propertyName);

            if (
                property == null ||
                property.objectReferenceValue == null
            )
            {
                throw new InvalidOperationException(
                    "Pause UI reference is missing: " +
                    propertyName);
            }
        }

        SerializedObject bridgeSerialized =
            new SerializedObject(
                gameplayBridge);

        string[] bridgeReferences =
        {
            "roundController",
            "scoreManager",
            "bowController",
            "groundSpawnManager",
            "birdController",
            "trajectoryHudRoot",
            "scoreHudRoot"
        };

        foreach (string propertyName in bridgeReferences)
        {
            SerializedProperty property =
                bridgeSerialized.FindProperty(
                    propertyName);

            if (
                property == null ||
                property.objectReferenceValue == null
            )
            {
                throw new InvalidOperationException(
                    "Gameplay bridge reference is missing: " +
                    propertyName);
            }
        }

        SerializedObject menuSerialized =
            new SerializedObject(
                menuController);

        bool shortTest =
            menuSerialized
                .FindProperty(
                    "useShortDeviceIntegrationRound")
                .boolValue;

        float testDuration =
            menuSerialized
                .FindProperty(
                    "deviceIntegrationRoundDurationSeconds")
                .floatValue;

        if (
            !shortTest ||
            Mathf.Abs(
                testDuration -
                60f) >
            0.01f
        )
        {
            throw new InvalidOperationException(
                "Temporary 60-second Quest test mode is not configured.");
        }
    }

    private static Font FindExistingFont()
    {
        Text existingText =
            UnityEngine.Object
                .FindFirstObjectByType<Text>();

        if (
            existingText != null &&
            existingText.font != null
        )
        {
            return existingText.font;
        }

        return
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
    }

    private static Text FindOrCreateText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        TextAnchor alignment,
        Vector2 position,
        Vector2 size)
    {
        Transform existing =
            parent.Find(
                name);

        GameObject textObject;

        if (existing != null)
        {
            textObject =
                existing.gameObject;
        }
        else
        {
            textObject =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            Undo.RegisterCreatedObjectUndo(
                textObject,
                "Create " + name);

            textObject.transform.SetParent(
                parent,
                false);
        }

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            position;

        Text text =
            textObject.GetComponent<Text>();

        text.font =
            uiFont;

        text.text =
            value;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.color =
            Color.white;

        text.horizontalOverflow =
            HorizontalWrapMode.Wrap;

        text.verticalOverflow =
            VerticalWrapMode.Truncate;

        return text;
    }

    private static Button FindOrCreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        Transform existing =
            parent.Find(
                name);

        GameObject buttonObject;

        if (existing != null)
        {
            buttonObject =
                existing.gameObject;
        }
        else
        {
            buttonObject =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));

            Undo.RegisterCreatedObjectUndo(
                buttonObject,
                "Create " + name);

            buttonObject.transform.SetParent(
                parent,
                false);
        }

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            position;

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            new Color(
                0.12f,
                0.28f,
                0.18f,
                0.98f);

        Button button =
            buttonObject.GetComponent<Button>();

        Transform labelTransform =
            buttonObject.transform.Find(
                "Label");

        Text labelText;

        if (labelTransform == null)
        {
            labelText =
                FindOrCreateText(
                    buttonObject.transform,
                    "Label",
                    label,
                    fontSize,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    size);
        }
        else
        {
            labelText =
                labelTransform.GetComponent<Text>();

            labelText.font =
                uiFont;

            labelText.text =
                label;

            labelText.fontSize =
                fontSize;

            labelText.alignment =
                TextAnchor.MiddleCenter;

            RectTransform labelRect =
                labelText
                    .GetComponent<RectTransform>();

            labelRect.sizeDelta =
                size;

            labelRect.anchoredPosition =
                Vector2.zero;
        }

        labelText.raycastTarget =
            false;

        return button;
    }

    private static void UpdateModeInformationText()
    {
        GameObject modePanel =
            GameObject.Find(
                "ModeSelectionPanel");

        if (modePanel == null)
        {
            return;
        }

        Transform infoTransform =
            modePanel.transform.Find(
                "Info");

        if (infoTransform == null)
        {
            return;
        }

        Text infoText =
            infoTransform.GetComponent<Text>();

        if (infoText == null)
        {
            return;
        }

        infoText.text =
            "TEST BUILD: 60-SECOND ROUND\n" +
            "PAUSE / QUIT / ROUND RESET TEST";

        EditorUtility.SetDirty(
            infoText);
    }

    private static bool ValidateScene()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        if (
            !scene.IsValid() ||
            !scene.isLoaded ||
            !string.Equals(
                scene.path,
                RequiredScenePath,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            EditorUtility.DisplayDialog(
                "Stage 08f.1",
                "Open the required scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return false;
        }

        return true;
    }

    private static void SetObjectReference(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing property: " +
                propertyName);
        }

        property.objectReferenceValue =
            value;
    }

    private static void SetBoolean(
        SerializedObject serialized,
        string propertyName,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing property: " +
                propertyName);
        }

        property.boolValue =
            value;
    }

    private static void SetFloat(
        SerializedObject serialized,
        string propertyName,
        float value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing property: " +
                propertyName);
        }

        property.floatValue =
            value;
    }
}
