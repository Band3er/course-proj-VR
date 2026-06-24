using System;
using ForestArchery.TimedGame;
using ForestArchery.Wildlife;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TimedGameResearchFinalizationSetup
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 09b - Install Practice Recorded Research Finalization")]
    public static void Install()
    {
        if (!ValidateScene())
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 09b",
                "Exit Play Mode before installing.",
                "OK");

            return;
        }

        try
        {
            Scene scene =
                SceneManager.GetActiveScene();

            GameObject systemObject =
                FindSceneObject(
                    scene,
                    "TimedGameSystem");

            if (systemObject == null)
            {
                throw new InvalidOperationException(
                    "TimedGameSystem was not found.");
            }

            TimedRoundController roundController =
                systemObject
                    .GetComponent<TimedRoundController>();

            TimedGameMenuController menuController =
                systemObject
                    .GetComponent<TimedGameMenuController>();

            if (
                roundController == null ||
                menuController == null
            )
            {
                throw new InvalidOperationException(
                    "TimedRoundController or TimedGameMenuController is missing.");
            }

            TimedGameResearchLogger logger =
                systemObject
                    .GetComponent<TimedGameResearchLogger>();

            if (logger == null)
            {
                logger =
                    Undo.AddComponent
                        <TimedGameResearchLogger>(
                            systemObject);
            }

            WildlifeScoreManager scoreManager =
                UnityEngine.Object
                    .FindFirstObjectByType
                        <WildlifeScoreManager>();

            global::TrajectoryToggleController trajectory =
                UnityEngine.Object
                    .FindFirstObjectByType
                        <global::TrajectoryToggleController>();

            SerializedObject loggerSerialized =
                new SerializedObject(
                    logger);

            SetReference(
                loggerSerialized,
                "roundController",
                roundController);

            SetReference(
                loggerSerialized,
                "menuController",
                menuController);

            SetReference(
                loggerSerialized,
                "scoreManager",
                scoreManager);

            SetReference(
                loggerSerialized,
                "trajectoryToggle",
                trajectory);

            loggerSerialized
                .ApplyModifiedPropertiesWithoutUndo();

            SerializedObject menuSerialized =
                new SerializedObject(
                    menuController);

            Button recordedButton =
                GetReference<Button>(
                    menuSerialized,
                    "mainTimedRoundButton");

            Button practiceButton =
                GetReference<Button>(
                    menuSerialized,
                    "mainPracticeButton");

            Text mainStatus =
                GetReference<Text>(
                    menuSerialized,
                    "mainStatusText");

            SetButtonLabel(
                recordedButton,
                "RECORDED ROUND\n5 MIN");

            SetButtonLabel(
                practiceButton,
                "PRACTICE\n1 MIN");

            if (mainStatus != null)
            {
                mainStatus.text =
                    "PRACTICE: 1 MIN, NOT SAVED\n" +
                    "RECORDED: 5 MIN, SAVED TO RESEARCH CSV";
            }

            EditorUtility.SetDirty(
                systemObject);

            EditorUtility.SetDirty(
                logger);

            EditorUtility.SetDirty(
                menuController);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();

            ValidateInstalledState();

            Selection.activeGameObject =
                systemObject;

            Debug.Log(
                "[STAGE 09B] Practice/Recorded research finalization installed." +
                "\nPractice duration: 60 seconds" +
                "\nRecorded duration: 300 seconds" +
                "\nPractice excluded from leaderboard and personal best" +
                "\nResearch CSV logger installed");

            EditorUtility.DisplayDialog(
                "Stage 09b Installed",
                "Practice and Recorded Round are now separate.\n\n" +
                "Practice: 1 minute, not saved to leaderboard.\n" +
                "Recorded Round: 5 minutes, saved normally.\n\n" +
                "Research CSV files are written under:\n" +
                "Application.persistentDataPath/ForestArcheryResearch",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 09b Failed",
                exception.Message,
                "OK");
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 09b - Validate Practice Recorded Research Finalization")]
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
                "[STAGE 09B] Validation passed.");

            EditorUtility.DisplayDialog(
                "Stage 09b Validation Passed",
                "Practice, Recorded Round and the research logger are configured.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 09b Validation Failed",
                exception.Message,
                "OK");
        }
    }

    private static void ValidateInstalledState()
    {
        GameObject systemObject =
            GameObject.Find(
                "TimedGameSystem");

        if (systemObject == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem is missing.");
        }

        TimedGameMenuController menu =
            systemObject
                .GetComponent<TimedGameMenuController>();

        TimedGameResearchLogger logger =
            systemObject
                .GetComponent<TimedGameResearchLogger>();

        if (
            menu == null ||
            logger == null
        )
        {
            throw new InvalidOperationException(
                "Menu controller or research logger is missing.");
        }

        SerializedObject serialized =
            new SerializedObject(
                logger);

        ValidateReference(
            serialized,
            "roundController");

        ValidateReference(
            serialized,
            "menuController");

        ValidateReference(
            serialized,
            "scoreManager");

        ValidateReference(
            serialized,
            "trajectoryToggle");

        SerializedObject menuSerialized =
            new SerializedObject(
                menu);

        Button recordedButton =
            GetReference<Button>(
                menuSerialized,
                "mainTimedRoundButton");

        Button practiceButton =
            GetReference<Button>(
                menuSerialized,
                "mainPracticeButton");

        if (
            recordedButton == null ||
            practiceButton == null
        )
        {
            throw new InvalidOperationException(
                "Main menu Practice/Recorded buttons are missing.");
        }

        string recordedLabel =
            GetButtonLabel(
                recordedButton);

        string practiceLabel =
            GetButtonLabel(
                practiceButton);

        if (
            recordedLabel.IndexOf(
                "5 MIN",
                StringComparison.OrdinalIgnoreCase) <
                0
        )
        {
            throw new InvalidOperationException(
                "Recorded button label is not configured.");
        }

        if (
            practiceLabel.IndexOf(
                "1 MIN",
                StringComparison.OrdinalIgnoreCase) <
                0
        )
        {
            throw new InvalidOperationException(
                "Practice button label is not configured.");
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
                "Stage 09b",
                "Open this scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return false;
        }

        return true;
    }

    private static GameObject FindSceneObject(
        Scene scene,
        string exactName)
    {
        foreach (
            GameObject root in
            scene.GetRootGameObjects()
        )
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(
                    true);

            foreach (Transform transform in transforms)
            {
                if (
                    transform != null &&
                    string.Equals(
                        transform.name,
                        exactName,
                        StringComparison.Ordinal)
                )
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private static void SetReference(
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

    private static void ValidateReference(
        SerializedObject serialized,
        string propertyName)
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
                "Missing reference: " +
                propertyName);
        }
    }

    private static T GetReference<T>(
        SerializedObject serialized,
        string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        return
            property != null
                ? property.objectReferenceValue
                    as T
                : null;
    }

    private static void SetButtonLabel(
        Button button,
        string label)
    {
        if (button == null)
        {
            return;
        }

        Text text =
            button
                .GetComponentInChildren<Text>(
                    true);

        if (text != null)
        {
            text.text =
                label;

            EditorUtility.SetDirty(
                text);
        }
    }

    private static string GetButtonLabel(
        Button button)
    {
        if (button == null)
        {
            return string.Empty;
        }

        Text text =
            button
                .GetComponentInChildren<Text>(
                    true);

        return
            text != null
                ? text.text
                : string.Empty;
    }
}
