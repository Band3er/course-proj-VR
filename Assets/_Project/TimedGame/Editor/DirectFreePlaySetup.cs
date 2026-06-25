#if UNITY_EDITOR
using System;
using System.Reflection;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DirectFreePlaySetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10e - Apply Direct Mixed Free Play")]
    public static void Apply()
    {
        try
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            TimedGameMenuController menu =
                UnityEngine.Object
                    .FindFirstObjectByType<TimedGameMenuController>();

            TimedGameShootingModeGate shootingGate =
                UnityEngine.Object
                    .FindFirstObjectByType<TimedGameShootingModeGate>();

            TimedGamePauseController pauseController =
                UnityEngine.Object
                    .FindFirstObjectByType<TimedGamePauseController>();

            if (
                menu == null ||
                shootingGate == null ||
                pauseController == null
            )
            {
                throw new InvalidOperationException(
                    "One or more required Stage 10e components were not found.");
            }

            SerializedObject serializedMenu =
                new SerializedObject(menu);

            Button freePlayButton =
                GetReference<Button>(
                    serializedMenu,
                    "mainPracticeButton");

            SetButtonLabel(
                freePlayButton,
                "FREE PLAY\nUNLIMITED");

            SerializedObject serializedPause =
                new SerializedObject(pauseController);

            Button roundHudButton =
                GetReference<Button>(
                    serializedPause,
                    "pauseButton");

            SetButtonLabel(
                roundHudButton,
                "PAUSE");

            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(shootingGate);
            EditorUtility.SetDirty(pauseController);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[FREE PLAY] Stage 10e applied.");

            EditorUtility.DisplayDialog(
                "Direct Mixed Free Play Applied",
                "Free Play now:\n\n" +
                "- starts immediately from the main menu;\n" +
                "- skips interaction-mode selection;\n" +
                "- has no countdown;\n" +
                "- allows natural switching between controllers and hands;\n" +
                "- uses a direct QUIT button instead of Pause.\n\n" +
                "The 5-minute Hunting Challenge remains unchanged and mode-locked.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Stage 10e Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10e - Validate Direct Mixed Free Play")]
    public static void Validate()
    {
        TimedGameMenuController menu =
            UnityEngine.Object
                .FindFirstObjectByType<TimedGameMenuController>();

        TimedGameShootingModeGate shootingGate =
            UnityEngine.Object
                .FindFirstObjectByType<TimedGameShootingModeGate>();

        TimedGamePauseController pauseController =
            UnityEngine.Object
                .FindFirstObjectByType<TimedGamePauseController>();

        if (
            menu == null ||
            shootingGate == null ||
            pauseController == null
        )
        {
            throw new InvalidOperationException(
                "Required Stage 10e components are missing.");
        }

        SerializedObject serializedMenu =
            new SerializedObject(menu);

        Button freePlayButton =
            GetReference<Button>(
                serializedMenu,
                "mainPracticeButton");

        string freePlayLabel =
            GetButtonLabel(
                freePlayButton);

        if (
            freePlayLabel !=
            "FREE PLAY\nUNLIMITED"
        )
        {
            throw new InvalidOperationException(
                "Free Play button has the wrong label: " +
                freePlayLabel);
        }

        MethodInfo directMethod =
            typeof(TimedGameMenuController)
                .GetMethod(
                    "ShowPracticeModeSelection",
                    BindingFlags.Instance |
                    BindingFlags.Public);

        if (directMethod == null)
        {
            throw new InvalidOperationException(
                "ShowPracticeModeSelection is missing.");
        }

        PropertyInfo currentPracticeProperty =
            typeof(TimedGameMenuController)
                .GetProperty(
                    "CurrentRoundIsPractice",
                    BindingFlags.Instance |
                    BindingFlags.Public);

        if (currentPracticeProperty == null)
        {
            throw new InvalidOperationException(
                "CurrentRoundIsPractice is missing.");
        }

        Debug.Log(
            "[FREE PLAY] Stage 10e validation passed.");

        EditorUtility.DisplayDialog(
            "Direct Mixed Free Play Validated",
            "Stage 10e is installed.\n\n" +
            "Validate final behaviour on Meta Quest:\n" +
            "- direct entry;\n" +
            "- hands/controllers can switch naturally;\n" +
            "- direct Quit;\n" +
            "- Hunt remains mode-locked.",
            "OK");
    }

    private static T GetReference<T>(
        SerializedObject serializedObject,
        string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Serialized property not found: " +
                propertyName);
        }

        return property.objectReferenceValue as T;
    }

    private static void SetButtonLabel(
        Button button,
        string label)
    {
        if (button == null)
        {
            throw new InvalidOperationException(
                "Required button is missing.");
        }

        Text labelText =
            button.GetComponentInChildren<Text>(
                true);

        if (labelText == null)
        {
            throw new InvalidOperationException(
                "Button label is missing on: " +
                button.gameObject.name);
        }

        labelText.text =
            label;

        EditorUtility.SetDirty(labelText);
        EditorUtility.SetDirty(button);
    }

    private static string GetButtonLabel(
        Button button)
    {
        if (button == null)
        {
            return "<missing>";
        }

        Text labelText =
            button.GetComponentInChildren<Text>(
                true);

        return labelText != null
            ? labelText.text
            : "<missing label>";
    }
}
#endif