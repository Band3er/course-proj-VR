#if UNITY_EDITOR
using System;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FinalGamePresentationSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10a - Apply Final Game Presentation")]
    public static void ApplyFinalPresentation()
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

            if (menu == null)
            {
                throw new InvalidOperationException(
                    "TimedGameMenuController was not found.");
            }

            SerializedObject menuSerialized =
                new SerializedObject(menu);

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
                "START HUNT\n5 MIN");

            SetButtonLabel(
                practiceButton,
                "FREE PLAY\nUNLIMITED");

            if (mainStatus != null)
            {
                mainStatus.text =
                    "Choose your interaction style and begin the hunt.";
                EditorUtility.SetDirty(mainStatus);
            }

            ReplaceExactSceneText(
                "PRACTICE\n1 MIN",
                "FREE PLAY\nUNLIMITED");

            ReplaceExactSceneText(
                "RECORDED ROUND\n5 MIN",
                "START HUNT\n5 MIN");

            ReplaceExactSceneText(
                "PRACTICE: 1 MIN, NOT SAVED\nRECORDED: 5 MIN, SAVED TO RESEARCH CSV",
                "Choose your interaction style and begin the hunt.");

            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[FINAL PRESENTATION] Player-facing test and research wording removed.");

            EditorUtility.DisplayDialog(
                "Final Game Presentation",
                "Final player-facing presentation applied.\n\n" +
                "Training: unlimited free play\n" +
                "Start Hunt: 5 minutes\n" +
                "Research logging remains active internally.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Final Game Presentation Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10a - Validate Final Game Presentation")]
    public static void ValidateFinalPresentation()
    {
        TimedGameMenuController menu =
            UnityEngine.Object
                .FindFirstObjectByType<TimedGameMenuController>();

        if (menu == null)
        {
            throw new InvalidOperationException(
                "TimedGameMenuController was not found.");
        }

        SerializedObject menuSerialized =
            new SerializedObject(menu);

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

        string recordedText =
            GetButtonLabel(recordedButton);

        string practiceText =
            GetButtonLabel(practiceButton);

        if (recordedText != "START HUNT\n5 MIN")
        {
            throw new InvalidOperationException(
                "Recorded button still has non-final text: " +
                recordedText);
        }

        if (practiceText != "FREE PLAY\nUNLIMITED")
        {
            throw new InvalidOperationException(
                "Practice button still has non-final text: " +
                practiceText);
        }

        if (
            mainStatus == null ||
            mainStatus.text !=
                "Choose your interaction style and begin the hunt."
        )
        {
            throw new InvalidOperationException(
                "Main status text is not finalized.");
        }

        Text[] allTexts =
            UnityEngine.Object
                .FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        string[] forbiddenPlayerFacingFragments =
        {
            "NOT SAVED",
            "SAVED TO RESEARCH CSV",
            "RECORDED ROUND",
            "PRACTICE COMPLETE",
            "PRACTICE RESULT"
        };

        foreach (Text textComponent in allTexts)
        {
            if (textComponent == null)
            {
                continue;
            }

            foreach (string forbidden in forbiddenPlayerFacingFragments)
            {
                if (
                    !string.IsNullOrEmpty(textComponent.text) &&
                    textComponent.text.IndexOf(
                        forbidden,
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    throw new InvalidOperationException(
                        "Non-final player-facing text remains on object '" +
                        textComponent.gameObject.name +
                        "': " +
                        textComponent.text);
                }
            }
        }

        Debug.Log(
            "[FINAL PRESENTATION] Validation passed.");

        EditorUtility.DisplayDialog(
            "Final Game Presentation Validated",
            "No player-facing test/research wording remains in the scene.",
            "OK");
    }

    private static T GetReference<T>(
        SerializedObject serializedObject,
        string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty property =
            serializedObject.FindProperty(propertyName);

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
                "A required menu button is missing.");
        }

        Text labelText =
            button.GetComponentInChildren<Text>(true);

        if (labelText == null)
        {
            throw new InvalidOperationException(
                "Button label Text component is missing on: " +
                button.gameObject.name);
        }

        labelText.text = label;
        EditorUtility.SetDirty(labelText);
        EditorUtility.SetDirty(button);
    }

    private static string GetButtonLabel(
        Button button)
    {
        if (button == null)
        {
            return "<missing button>";
        }

        Text labelText =
            button.GetComponentInChildren<Text>(true);

        return labelText != null
            ? labelText.text
            : "<missing label>";
    }

    private static void ReplaceExactSceneText(
        string oldText,
        string newText)
    {
        Text[] allTexts =
            UnityEngine.Object
                .FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (Text textComponent in allTexts)
        {
            if (
                textComponent != null &&
                textComponent.text == oldText
            )
            {
                textComponent.text = newText;
                EditorUtility.SetDirty(textComponent);
            }
        }
    }
}
#endif