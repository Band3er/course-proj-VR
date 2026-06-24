using System;
using System.Reflection;
using ForestArchery.TimedGame;
using ForestArchery.Wildlife;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TimedGameGameplayIntegrationSetup
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08e - Install Gameplay Integration")]
    public static void Install()
    {
        if (!ValidateScene())
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 08e",
                "Exit Play Mode before installing gameplay integration.",
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

            if (systemObject == null)
            {
                throw new InvalidOperationException(
                    "TimedGameSystem was not found.");
            }

            TimedRoundController roundController =
                systemObject.GetComponent<TimedRoundController>();

            TimedGameMenuController menuController =
                systemObject.GetComponent<TimedGameMenuController>();

            if (roundController == null)
            {
                throw new InvalidOperationException(
                    "TimedRoundController is missing from TimedGameSystem.");
            }

            if (menuController == null)
            {
                throw new InvalidOperationException(
                    "TimedGameMenuController is missing from TimedGameSystem.");
            }

            WildlifeScoreManager scoreManager =
                UnityEngine.Object
                    .FindFirstObjectByType<WildlifeScoreManager>();

            global::BowDrawController bowController =
                UnityEngine.Object
                    .FindFirstObjectByType<global::BowDrawController>();

            global::ArrowTrajectoryPreview trajectoryPreview =
                UnityEngine.Object
                    .FindFirstObjectByType<global::ArrowTrajectoryPreview>();

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

            TimedGameGameplayBridge bridge =
                systemObject
                    .GetComponent<TimedGameGameplayBridge>();

            if (bridge == null)
            {
                bridge =
                    Undo.AddComponent<TimedGameGameplayBridge>(
                        systemObject);
            }

            SerializedObject bridgeSerialized =
                new SerializedObject(
                    bridge);

            SetObjectReference(
                bridgeSerialized,
                "roundController",
                roundController);

            SetObjectReference(
                bridgeSerialized,
                "scoreManager",
                scoreManager);

            SetObjectReference(
                bridgeSerialized,
                "bowController",
                bowController);

            SetObjectReference(
                bridgeSerialized,
                "trajectoryPreview",
                trajectoryPreview);

            SetBoolean(
                bridgeSerialized,
                "clearExistingArrowsAtRoundStart",
                true);

            SetBoolean(
                bridgeSerialized,
                "verboseLogging",
                true);

            bridgeSerialized
                .ApplyModifiedPropertiesWithoutUndo();

            SerializedObject menuSerialized =
                new SerializedObject(
                    menuController);

            SetBoolean(
                menuSerialized,
                "useShortDeviceIntegrationRound",
                true);

            SetFloat(
                menuSerialized,
                "deviceIntegrationRoundDurationSeconds",
                60f);

            menuSerialized
                .ApplyModifiedPropertiesWithoutUndo();

            UpdateModeInformationText();

            EditorUtility.SetDirty(
                systemObject);

            EditorUtility.SetDirty(
                bridge);

            EditorUtility.SetDirty(
                menuController);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            Selection.activeGameObject =
                systemObject;

            ValidateInstalledState(
                systemObject);

            Debug.Log(
                "[TIMED GAME INTEGRATION] Stage 08e installed." +
                "\nScore reset/start/stop: connected" +
                "\nArrow launch counting: connected" +
                "\nBow gate: connected" +
                "\nDevice integration test duration: 60 seconds");

            EditorUtility.DisplayDialog(
                "Stage 08e Installed",
                "Timed gameplay integration was installed.\n\n" +
                "Connected:\n" +
                "- dynamic wildlife score\n" +
                "- hit counter\n" +
                "- arrow launch counter\n" +
                "- bow enable/disable gate\n" +
                "- score stop at TIME'S UP\n" +
                "- result and local leaderboard data\n\n" +
                "Device integration-test rounds are temporarily 60 seconds.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08e Failed",
                exception.Message,
                "OK");
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08e - Validate Gameplay Integration")]
    public static void Validate()
    {
        if (!ValidateScene())
        {
            return;
        }

        try
        {
            GameObject systemObject =
                GameObject.Find(
                    "TimedGameSystem");

            ValidateInstalledState(
                systemObject);

            TimedGameMenuController menuController =
                systemObject
                    .GetComponent<TimedGameMenuController>();

            SerializedObject menuSerialized =
                new SerializedObject(
                    menuController);

            bool shortDeviceTest =
                menuSerialized
                    .FindProperty(
                        "useShortDeviceIntegrationRound")
                    .boolValue;

            float shortDeviceDuration =
                menuSerialized
                    .FindProperty(
                        "deviceIntegrationRoundDurationSeconds")
                    .floatValue;

            if (!shortDeviceTest)
            {
                throw new InvalidOperationException(
                    "The temporary device integration-test mode is not enabled.");
            }

            if (
                Mathf.Abs(
                    shortDeviceDuration -
                    60f) >
                0.01f
            )
            {
                throw new InvalidOperationException(
                    "The temporary device integration-test duration is not 60 seconds.");
            }

            EventInfo arrowEvent =
                typeof(global::ArrowController)
                    .GetEvent(
                        "AnyArrowLaunched",
                        BindingFlags.Public |
                        BindingFlags.Static);

            if (arrowEvent == null)
            {
                throw new InvalidOperationException(
                    "ArrowController.AnyArrowLaunched was not found.");
            }

            PropertyInfo scoringProperty =
                typeof(WildlifeScoreManager)
                    .GetProperty(
                        "ScoringEnabled",
                        BindingFlags.Public |
                        BindingFlags.Instance);

            MethodInfo scoringMethod =
                typeof(WildlifeScoreManager)
                    .GetMethod(
                        "SetScoringEnabled",
                        BindingFlags.Public |
                        BindingFlags.Instance);

            if (
                scoringProperty == null ||
                scoringMethod == null
            )
            {
                throw new InvalidOperationException(
                    "Wildlife scoring gate API is incomplete.");
            }

            Debug.Log(
                "[TIMED GAME INTEGRATION] Stage 08e validation passed.");

            EditorUtility.DisplayDialog(
                "Stage 08e Validation Passed",
                "Scene references, scoring gate, arrow launch event, bow gate, " +
                "and 60-second device test mode are configured.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08e Validation Failed",
                exception.Message,
                "OK");
        }
    }

    private static void ValidateInstalledState(
        GameObject systemObject)
    {
        if (systemObject == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem was not found.");
        }

        TimedGameGameplayBridge bridge =
            systemObject
                .GetComponent<TimedGameGameplayBridge>();

        if (bridge == null)
        {
            throw new InvalidOperationException(
                "TimedGameGameplayBridge is missing.");
        }

        SerializedObject serialized =
            new SerializedObject(
                bridge);

        string[] requiredReferences =
        {
            "roundController",
            "scoreManager",
            "bowController"
        };

        foreach (string propertyName in requiredReferences)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (
                property == null ||
                property.objectReferenceValue == null
            )
            {
                throw new InvalidOperationException(
                    "Bridge reference is missing: " +
                    propertyName);
            }
        }
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
                "Timed Game Integration",
                "Open the required scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return false;
        }

        return true;
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
            "INTEGRATION TEST BUILD: 60-SECOND ROUND\n" +
            "FINAL PRODUCTION ROUND: 5 MINUTES";

        EditorUtility.SetDirty(
            infoText);
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
