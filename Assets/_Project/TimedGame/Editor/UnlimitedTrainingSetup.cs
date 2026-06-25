#if UNITY_EDITOR
using System;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UnlimitedTrainingSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10d - Apply Unlimited Training")]
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

            TimedRoundController roundController =
                UnityEngine.Object
                    .FindFirstObjectByType<TimedRoundController>();

            if (menu == null)
            {
                throw new InvalidOperationException(
                    "TimedGameMenuController was not found.");
            }

            if (roundController == null)
            {
                throw new InvalidOperationException(
                    "TimedRoundController was not found.");
            }

            SerializedObject serializedMenu =
                new SerializedObject(menu);

            Button trainingButton =
                GetReference<Button>(
                    serializedMenu,
                    "mainPracticeButton");

            SetButtonLabel(
                trainingButton,
                "FREE PLAY\nUNLIMITED");

            ReplaceExactText(
                "TRAINING\n1 MIN",
                "FREE PLAY\nUNLIMITED");

            ReplaceExactText(
                "TRAINING - 01:00",
                "TRAINING - FREE PLAY");

            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(roundController);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[UNLIMITED TRAINING] Stage 10d applied.");

            EditorUtility.DisplayDialog(
                "Unlimited Training Applied",
                "Training is now unlimited.\n\n" +
                "- The timer displays FREE PLAY.\n" +
                "- Training does not end automatically.\n" +
                "- Use Pause > Quit to return to the main menu.\n" +
                "- The 5-minute Hunting Challenge is unchanged.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Unlimited Training Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10d - Validate Unlimited Training")]
    public static void Validate()
    {
        TimedGameMenuController menu =
            UnityEngine.Object
                .FindFirstObjectByType<TimedGameMenuController>();

        TimedRoundController roundController =
            UnityEngine.Object
                .FindFirstObjectByType<TimedRoundController>();

        if (menu == null || roundController == null)
        {
            throw new InvalidOperationException(
                "Required timed-game components were not found.");
        }

        SerializedObject serializedMenu =
            new SerializedObject(menu);

        Button trainingButton =
            GetReference<Button>(
                serializedMenu,
                "mainPracticeButton");

        string trainingLabel =
            GetButtonLabel(
                trainingButton);

        if (trainingLabel != "FREE PLAY\nUNLIMITED")
        {
            throw new InvalidOperationException(
                "Training button label is not final: " +
                trainingLabel);
        }

        if (
            roundController.GetType().GetMethod(
                "ConfigureUnlimitedRound") == null
        )
        {
            throw new InvalidOperationException(
                "ConfigureUnlimitedRound is missing.");
        }

        if (
            roundController.GetType().GetProperty(
                "IsUnlimitedRound") == null
        )
        {
            throw new InvalidOperationException(
                "IsUnlimitedRound is missing.");
        }

        Debug.Log(
            "[UNLIMITED TRAINING] Stage 10d validation passed.");

        EditorUtility.DisplayDialog(
            "Unlimited Training Validated",
            "Free Play is unlimited and starts directly.\n" +
            "The Hunting Challenge remains timed at 5 minutes.",
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
                "Training button is missing.");
        }

        Text labelText =
            button.GetComponentInChildren<Text>(
                true);

        if (labelText == null)
        {
            throw new InvalidOperationException(
                "Training button label is missing.");
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

    private static void ReplaceExactText(
        string oldText,
        string newText)
    {
        Text[] texts =
            UnityEngine.Object
                .FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (Text textComponent in texts)
        {
            if (
                textComponent != null &&
                textComponent.text == oldText
            )
            {
                textComponent.text =
                    newText;

                EditorUtility.SetDirty(
                    textComponent);
            }
        }
    }
}
#endif