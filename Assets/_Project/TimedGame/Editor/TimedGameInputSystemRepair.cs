using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class TimedGameInputSystemRepair
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08d.1 - Repair Input System UI")]
    public static void Repair()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 08d.1",
                "Exit Play Mode before repairing the UI input module.",
                "OK");

            return;
        }

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
                "Stage 08d.1",
                "Open the required scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return;
        }

        GameObject eventSystemObject =
            GameObject.Find(
                "TimedGameEventSystem");

        if (eventSystemObject == null)
        {
            EventSystem existingEventSystem =
                UnityEngine.Object.FindFirstObjectByType<EventSystem>();

            if (existingEventSystem != null)
            {
                eventSystemObject =
                    existingEventSystem.gameObject;
            }
        }

        if (eventSystemObject == null)
        {
            eventSystemObject =
                new GameObject(
                    "TimedGameEventSystem");

            Undo.RegisterCreatedObjectUndo(
                eventSystemObject,
                "Create Timed Game Event System");

            Undo.AddComponent<EventSystem>(
                eventSystemObject);
        }

        EventSystem eventSystem =
            eventSystemObject.GetComponent<EventSystem>();

        if (eventSystem == null)
        {
            eventSystem =
                Undo.AddComponent<EventSystem>(
                    eventSystemObject);
        }

        StandaloneInputModule legacyModule =
            eventSystemObject.GetComponent<StandaloneInputModule>();

        if (legacyModule != null)
        {
            Undo.DestroyObjectImmediate(
                legacyModule);
        }

        InputSystemUIInputModule inputSystemModule =
            eventSystemObject.GetComponent<InputSystemUIInputModule>();

        if (inputSystemModule == null)
        {
            inputSystemModule =
                Undo.AddComponent<InputSystemUIInputModule>(
                    eventSystemObject);
        }

        EditorUtility.SetDirty(
            eventSystemObject);

        EditorSceneManager.MarkSceneDirty(
            scene);

        EditorSceneManager.SaveScene(
            scene);

        Selection.activeGameObject =
            eventSystemObject;

        Debug.Log(
            "[TIMED GAME UI] Stage 08d.1 Input System repair completed." +
            " | EventSystem=" +
            eventSystemObject.name +
            " | module=" +
            inputSystemModule.GetType().Name);

        EditorUtility.DisplayDialog(
            "Stage 08d.1 Repair Complete",
            "The legacy StandaloneInputModule was removed.\n\n" +
            "InputSystemUIInputModule is now installed.\n\n" +
            "The scene was saved.",
            "OK");
    }
}
