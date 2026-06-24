using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TimedGameMetaRayCanvasSetup
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private const string CanvasObjectName =
        "TimedGameCanvas";

    private const string EventSystemObjectName =
        "TimedGameEventSystem";

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08d.2 - Install Meta Ray Canvas")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 08d.2",
                "Exit Play Mode before installing Meta ray interaction.",
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
                "Stage 08d.2",
                "Open the required scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return;
        }

        GameObject canvasObject =
            FindSceneObjectByName(
                scene,
                CanvasObjectName);

        if (canvasObject == null)
        {
            EditorUtility.DisplayDialog(
                "Stage 08d.2",
                "TimedGameCanvas was not found in the active scene.",
                "OK");

            return;
        }

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        RectTransform rectTransform =
            canvasObject.GetComponent<RectTransform>();

        GraphicRaycaster graphicRaycaster =
            canvasObject.GetComponent<GraphicRaycaster>();

        if (
            canvas == null ||
            rectTransform == null ||
            graphicRaycaster == null
        )
        {
            EditorUtility.DisplayDialog(
                "Stage 08d.2",
                "TimedGameCanvas is missing Canvas, RectTransform, or GraphicRaycaster.",
                "OK");

            return;
        }

        try
        {
            GameObject eventSystemObject =
                EnsureEventSystem(
                    scene);

            PointableCanvasModule pointableModule =
                GetOrAddComponent<PointableCanvasModule>(
                    eventSystemObject);

            pointableModule.ExclusiveMode =
                true;

            RemoveInputSystemUiModule(
                eventSystemObject,
                pointableModule);

            PointableCanvas pointableCanvas =
                GetOrAddComponent<PointableCanvas>(
                    canvasObject);

            PlaneSurface planeSurface =
                GetOrAddComponent<PlaneSurface>(
                    canvasObject);

            RayInteractable rayInteractable =
                GetOrAddComponent<RayInteractable>(
                    canvasObject);

            pointableCanvas.InjectAllPointableCanvas(
                canvas);

            planeSurface.InjectAllPlaneSurface(
                PlaneSurface.NormalFacing.Backward,
                true);

            rayInteractable.InjectAllRayInteractable(
                planeSurface);

            rayInteractable.InjectOptionalPointableElement(
                pointableCanvas);

            EditorUtility.SetDirty(
                canvasObject);

            EditorUtility.SetDirty(
                eventSystemObject);

            EditorUtility.SetDirty(
                pointableCanvas);

            EditorUtility.SetDirty(
                planeSurface);

            EditorUtility.SetDirty(
                rayInteractable);

            EditorUtility.SetDirty(
                pointableModule);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            ValidateInstallation(
                canvasObject,
                eventSystemObject);

            Selection.activeGameObject =
                canvasObject;

            Debug.Log(
                "[TIMED GAME UI] Stage 08d.2 Meta ray canvas installed." +
                "\nCanvas: " +
                GetHierarchyPath(
                    canvasObject.transform) +
                "\nComponents: PointableCanvas, PlaneSurface, RayInteractable" +
                "\nEventSystem: PointableCanvasModule" +
                "\nExclusiveMode: true");

            EditorUtility.DisplayDialog(
                "Stage 08d.2 Complete",
                "Meta ray interaction was installed automatically.\n\n" +
                "TimedGameCanvas now contains:\n" +
                "- PointableCanvas\n" +
                "- PlaneSurface\n" +
                "- RayInteractable\n\n" +
                "TimedGameEventSystem now contains:\n" +
                "- EventSystem\n" +
                "- PointableCanvasModule\n\n" +
                "The scene was saved.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08d.2 Failed",
                exception.Message,
                "OK");
        }
    }

    private static GameObject EnsureEventSystem(
        Scene scene)
    {
        GameObject eventSystemObject =
            FindSceneObjectByName(
                scene,
                EventSystemObjectName);

        if (eventSystemObject == null)
        {
            EventSystem existing =
                UnityEngine.Object
                    .FindFirstObjectByType<EventSystem>();

            if (existing != null)
            {
                eventSystemObject =
                    existing.gameObject;
            }
        }

        if (eventSystemObject == null)
        {
            eventSystemObject =
                new GameObject(
                    EventSystemObjectName);

            Undo.RegisterCreatedObjectUndo(
                eventSystemObject,
                "Create Timed Game Event System");
        }

        GetOrAddComponent<EventSystem>(
            eventSystemObject);

        return eventSystemObject;
    }

    private static void RemoveInputSystemUiModule(
        GameObject eventSystemObject,
        PointableCanvasModule pointableModule)
    {
        BaseInputModule[] modules =
            eventSystemObject
                .GetComponents<BaseInputModule>();

        foreach (BaseInputModule module in modules)
        {
            if (
                module == null ||
                module == pointableModule
            )
            {
                continue;
            }

            string fullTypeName =
                module.GetType().FullName ??
                module.GetType().Name;

            if (
                string.Equals(
                    fullTypeName,
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule",
                    StringComparison.Ordinal) ||
                string.Equals(
                    fullTypeName,
                    "UnityEngine.EventSystems.StandaloneInputModule",
                    StringComparison.Ordinal)
            )
            {
                Undo.DestroyObjectImmediate(
                    module);
            }
        }
    }

    private static T GetOrAddComponent<T>(
        GameObject target)
        where T : Component
    {
        T existing =
            target.GetComponent<T>();

        if (existing != null)
        {
            return existing;
        }

        return
            Undo.AddComponent<T>(
                target);
    }

    private static void ValidateInstallation(
        GameObject canvasObject,
        GameObject eventSystemObject)
    {
        string[] missing =
        {
            canvasObject.GetComponent<PointableCanvas>() == null
                ? "PointableCanvas"
                : null,

            canvasObject.GetComponent<PlaneSurface>() == null
                ? "PlaneSurface"
                : null,

            canvasObject.GetComponent<RayInteractable>() == null
                ? "RayInteractable"
                : null,

            eventSystemObject.GetComponent<EventSystem>() == null
                ? "EventSystem"
                : null,

            eventSystemObject.GetComponent<PointableCanvasModule>() == null
                ? "PointableCanvasModule"
                : null
        };

        string[] actualMissing =
            missing
                .Where(
                    item =>
                        !string.IsNullOrEmpty(
                            item))
                .ToArray();

        if (actualMissing.Length > 0)
        {
            throw new InvalidOperationException(
                "Installation validation failed. Missing: " +
                string.Join(
                    ", ",
                    actualMissing));
        }

        PointableCanvasModule module =
            eventSystemObject
                .GetComponent<PointableCanvasModule>();

        if (!module.ExclusiveMode)
        {
            throw new InvalidOperationException(
                "PointableCanvasModule ExclusiveMode was not enabled.");
        }
    }

    private static GameObject FindSceneObjectByName(
        Scene scene,
        string objectName)
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
                        objectName,
                        StringComparison.Ordinal)
                )
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private static string GetHierarchyPath(
        Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        string path =
            transform.name;

        while (transform.parent != null)
        {
            transform =
                transform.parent;

            path =
                transform.name +
                "/" +
                path;
        }

        return path;
    }
}
