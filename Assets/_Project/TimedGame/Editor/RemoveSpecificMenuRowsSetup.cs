#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RemoveSpecificMenuRowsSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private static readonly string[] TargetPhrases =
    {
        "60-SECOND ROUND",
        "60 SECOND ROUND",
        "PAUSE / QUIT / ROUND",
        "PAUSE/QUIT/ROUND"
    };

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10b3 - Remove Specific Menu Rows")]
    public static void InstallAndApply()
    {
        try
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            TimedGameMenuController menu =
                UnityEngine.Object.FindFirstObjectByType<TimedGameMenuController>();

            if (menu == null) {
                throw new InvalidOperationException(
                    "TimedGameMenuController was not found.");
            }

            RemoveSpecificMenuRows cleanup =
                menu.GetComponent<RemoveSpecificMenuRows>();

            if (cleanup == null)
            {
                cleanup =
                    Undo.AddComponent<RemoveSpecificMenuRows>(
                        menu.gameObject);
            }

            cleanup.ApplyNow();

            int removedCount =
                RemoveRowsImmediatelyInScene();

            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(cleanup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[FINAL UI] Specific menu rows removed: " +
                removedCount);

            EditorUtility.DisplayDialog(
                "Specific Menu Rows Removed",
                "Removed rows containing:\n\n" +
                "- 60-SECOND ROUND\n" +
                "- PAUSE / QUIT / ROUND\n\n" +
                "Scene saved successfully.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Specific Menu Row Removal Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10b3 - Validate Specific Menu Rows Removed")]
    public static void Validate()
    {
        List<string> visibleMatches =
            FindVisibleMatches();

        if (visibleMatches.Count > 0)
        {
            throw new InvalidOperationException(
                "Still visible:\n" +
                string.Join("\n", visibleMatches));
        }

        Debug.Log(
            "[FINAL UI] Stage 10b3 validation passed.");

        EditorUtility.DisplayDialog(
            "Specific Menu Rows Validated",
            "No visible row containing:\n" +
            "- 60-SECOND ROUND\n" +
            "- PAUSE / QUIT / ROUND",
            "OK");
    }

    private static int RemoveRowsImmediatelyInScene()
    {
        int removed = 0;

        Text[] texts =
            UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Text textComponent in texts)
        {
            if (
                textComponent == null ||
                !MatchesTarget(textComponent.text)
            ) {
                continue;
            }

            GameObject target =
                FindBestRemovalTarget(textComponent.transform);

            if (target != null && target.activeSelf)
            {
                Undo.RecordObject(target, "Remove specific menu row");
                target.SetActive(false);
                EditorUtility.SetDirty(target);
                removed++;
            }
        }

        return removed;
    }

    private static GameObject FindBestRemovalTarget(
        Transform source)
    {
        if (source == null) {
            return null;
        }

        Transform parent = source.parent;

        if (
            parent != null &&
            IsTextOnlyContainer(parent)
        ) {
            return parent.gameObject;
        }

        if (
            parent != null &&
            parent.parent != null &&
            IsTextOnlyContainer(parent.parent)
        ) {
            return parent.parent.gameObject;
        }

        return source.gameObject;
    }

    private static bool IsTextOnlyContainer(
        Transform root)
    {
        if (root == null) {
            return false;
        }

        if (root.GetComponent<Button>() != null) {
            return false;
        }

        if (root.GetComponentInChildren<Button>(true) != null) {
            return false;
        }

        bool foundText = false;

        Text[] texts =
            root.GetComponentsInChildren<Text>(true);

        foreach (Text textComponent in texts)
        {
            if (
                textComponent == null ||
                string.IsNullOrWhiteSpace(textComponent.text)
            ) {
                continue;
            }

            foundText = true;
        }

        return foundText;
    }

    private static List<string> FindVisibleMatches()
    {
        List<string> problems =
            new List<string>();

        Text[] texts =
            UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Text textComponent in texts)
        {
            if (
                textComponent == null ||
                !textComponent.gameObject.activeInHierarchy ||
                string.IsNullOrWhiteSpace(textComponent.text)
            ) {
                continue;
            }

            if (MatchesTarget(textComponent.text))
            {
                problems.Add(
                    textComponent.gameObject.name +
                    ": " +
                    textComponent.text);
            }
        }

        return problems;
    }

    private static bool MatchesTarget(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        foreach (string phrase in TargetPhrases)
        {
            if (
                value.IndexOf(
                    phrase,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            ) {
                return true;
            }
        }

        return false;
    }
}
#endif