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

public static class FinalPlayerFacingUICleanupSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private static readonly string[] ForbiddenRowFragments =
    {
        "TEST BUILD",
        "RESET TEST",
        "60 SECONDS",
        "60 SECOND",
        "TEST MODE",
        "RESET"
    };

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10b2 - Remove Entire Test Rows")]
    public static void InstallAndApply()
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

            FinalPlayerFacingUICleanup cleanup =
                menu.GetComponent<FinalPlayerFacingUICleanup>();

            if (cleanup == null)
            {
                cleanup =
                    Undo.AddComponent<FinalPlayerFacingUICleanup>(
                        menu.gameObject);
            }

            int removedRows =
                RemoveEntireRowsFromOpenScene();

            EditorUtility.SetDirty(cleanup);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[FINAL UI] Entire test/reset rows removed: " +
                removedRows);

            EditorUtility.DisplayDialog(
                "Entire Test Rows Removed",
                "Removed complete UI rows containing:\n\n" +
                "- TEST BUILD\n" +
                "- RESET TEST / RESET\n" +
                "- 60 SECONDS\n\n" +
                "Rows were disabled as complete containers, not edited word by word.\n" +
                "Runtime protection remains active.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Test Row Removal Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10b2 - Validate Entire Test Rows Removed")]
    public static void Validate()
    {
        TimedGameMenuController menu =
            UnityEngine.Object
                .FindFirstObjectByType<TimedGameMenuController>();

        if (menu == null)
        {
            throw new InvalidOperationException(
                "TimedGameMenuController was not found.");
        }

        FinalPlayerFacingUICleanup cleanup =
            menu.GetComponent<FinalPlayerFacingUICleanup>();

        if (cleanup == null)
        {
            throw new InvalidOperationException(
                "FinalPlayerFacingUICleanup is not installed.");
        }

        List<string> visibleProblems =
            FindVisibleForbiddenTexts();

        if (visibleProblems.Count > 0)
        {
            throw new InvalidOperationException(
                "Visible test/reset text still exists:\n" +
                string.Join(
                    "\n",
                    visibleProblems));
        }

        Debug.Log(
            "[FINAL UI] Stage 10b2 validation passed.");

        EditorUtility.DisplayDialog(
            "Entire Test Rows Validated",
            "No visible TEST BUILD, RESET or 60 SECONDS row remains.\n" +
            "The complete row containers are removed from the player-facing UI.",
            "OK");
    }

    private static int RemoveEntireRowsFromOpenScene()
    {
        HashSet<GameObject> rows =
            new HashSet<GameObject>();

        Text[] legacyTexts =
            UnityEngine.Object
                .FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (Text textComponent in legacyTexts)
        {
            if (
                textComponent == null ||
                string.IsNullOrWhiteSpace(
                    textComponent.text) ||
                !ContainsForbidden(
                    textComponent.text)
            )
            {
                continue;
            }

            rows.Add(
                FindSafeRowRoot(
                    textComponent.transform));
        }

        foreach (GameObject row in rows)
        {
            if (row == null)
            {
                continue;
            }

            Undo.RecordObject(
                row,
                "Remove entire test UI row");

            row.SetActive(false);
            EditorUtility.SetDirty(row);
        }

        return rows.Count;
    }

    private static GameObject FindSafeRowRoot(
        Transform textTransform)
    {
        Transform current =
            textTransform;

        GameObject lastSafe =
            textTransform.gameObject;

        for (
            int depth = 0;
            depth < 5 &&
            current != null;
            depth++
        )
        {
            if (
                current.GetComponent<TimedGameMenuController>() != null
            )
            {
                break;
            }

            if (ContainsOnlyForbiddenText(current))
            {
                lastSafe =
                    current.gameObject;

                current =
                    current.parent;

                continue;
            }

            break;
        }

        Button button =
            textTransform.GetComponentInParent<Button>(
                true);

        if (
            button != null &&
            ContainsOnlyForbiddenText(
                button.transform)
        )
        {
            lastSafe =
                button.gameObject;
        }

        return lastSafe;
    }

    private static bool ContainsOnlyForbiddenText(
        Transform root)
    {
        bool foundAnyText =
            false;

        Text[] texts =
            root.GetComponentsInChildren<Text>(
                true);

        foreach (Text textComponent in texts)
        {
            if (
                textComponent == null ||
                string.IsNullOrWhiteSpace(
                    textComponent.text)
            )
            {
                continue;
            }

            foundAnyText =
                true;

            if (
                !ContainsForbidden(
                    textComponent.text)
            )
            {
                return false;
            }
        }

        return foundAnyText;
    }

    private static List<string> FindVisibleForbiddenTexts()
    {
        List<string> problems =
            new List<string>();

        Text[] texts =
            UnityEngine.Object
                .FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (Text textComponent in texts)
        {
            if (
                textComponent == null ||
                !textComponent.gameObject.activeInHierarchy ||
                string.IsNullOrWhiteSpace(
                    textComponent.text)
            )
            {
                continue;
            }

            if (
                ContainsForbidden(
                    textComponent.text)
            )
            {
                problems.Add(
                    textComponent.gameObject.name +
                    ": " +
                    textComponent.text);
            }
        }

        return problems;
    }

    private static bool ContainsForbidden(
        string value)
    {
        foreach (string forbidden in ForbiddenRowFragments)
        {
            if (
                value.IndexOf(
                    forbidden,
                    StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                return true;
            }
        }

        return false;
    }
}
#endif