#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TrajectoryVrPointableRepairSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private const string PointableCanvasTypeName =
        "Oculus.Interaction.PointableCanvas";

    private const string PlaneSurfaceTypeName =
        "Oculus.Interaction.Surfaces.PlaneSurface";

    private const string RayInteractableTypeName =
        "Oculus.Interaction.RayInteractable";

    private const string PointableCanvasModuleTypeName =
        "Oculus.Interaction.PointableCanvasModule";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10f3 - Repair Trajectory VR Pointable")]
    public static void Repair()
    {
        try
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            TrajectoryToggleController controller =
                UnityEngine.Object
                    .FindFirstObjectByType<TrajectoryToggleController>();

            if (controller == null)
            {
                throw new InvalidOperationException(
                    "TrajectoryToggleController was not found.");
            }

            SerializedObject serializedController =
                new SerializedObject(controller);

            Button trajectoryButton =
                GetReference<Button>(
                    serializedController,
                    "uiButton");

            Text hintText =
                GetReference<Text>(
                    serializedController,
                    "hintText");

            if (trajectoryButton == null)
            {
                throw new InvalidOperationException(
                    "Trajectory button reference is missing.");
            }

            Canvas trajectoryCanvas =
                trajectoryButton.GetComponentInParent<Canvas>(
                    true);

            if (trajectoryCanvas == null)
            {
                throw new InvalidOperationException(
                    "Trajectory button is not inside a Canvas.");
            }

            Canvas sourceCanvas =
                FindCanvasByName(
                    "TimedGameCanvas");

            if (sourceCanvas == null)
            {
                throw new InvalidOperationException(
                    "TimedGameCanvas was not found. It is required as the known-good VR UI source.");
            }

            Type pointableCanvasType =
                FindComponentType(
                    PointableCanvasTypeName);

            Type planeSurfaceType =
                FindComponentType(
                    PlaneSurfaceTypeName);

            Type rayInteractableType =
                FindComponentType(
                    RayInteractableTypeName);

            Type pointableCanvasModuleType =
                FindComponentType(
                    PointableCanvasModuleTypeName);

            Component sourcePointable =
                sourceCanvas.GetComponent(
                    pointableCanvasType);

            Component sourcePlane =
                sourceCanvas.GetComponent(
                    planeSurfaceType);

            Component sourceRay =
                sourceCanvas.GetComponent(
                    rayInteractableType);

            if (
                sourcePointable == null ||
                sourcePlane == null ||
                sourceRay == null
            )
            {
                throw new InvalidOperationException(
                    "TimedGameCanvas is missing one or more known-good Meta VR UI components.");
            }

            Component targetPointable =
                GetOrCopyComponent(
                    sourcePointable,
                    trajectoryCanvas.gameObject,
                    pointableCanvasType);

            Component targetPlane =
                GetOrCopyComponent(
                    sourcePlane,
                    trajectoryCanvas.gameObject,
                    planeSurfaceType);

            Component targetRay =
                GetOrCopyComponent(
                    sourceRay,
                    trajectoryCanvas.gameObject,
                    rayInteractableType);

            SetObjectReference(
                targetPointable,
                "_canvas",
                trajectoryCanvas);

            SetObjectReference(
                targetRay,
                "_pointableElement",
                targetPointable);

            SetObjectReference(
                targetRay,
                "_surface",
                targetPlane);

            SetObjectReferenceIfPresent(
                targetRay,
                "_selectSurface",
                null);

            EnableBehaviour(
                targetPointable);

            EnableBehaviour(
                targetPlane);

            EnableBehaviour(
                targetRay);

            GraphicRaycaster graphicRaycaster =
                trajectoryCanvas
                    .GetComponent<GraphicRaycaster>();

            if (graphicRaycaster == null)
            {
                graphicRaycaster =
                    Undo.AddComponent<GraphicRaycaster>(
                        trajectoryCanvas.gameObject);
            }

            graphicRaycaster.enabled =
                true;

            trajectoryButton.interactable =
                true;

            Image buttonImage =
                trajectoryButton
                    .GetComponent<Image>();

            if (buttonImage != null)
            {
                buttonImage.raycastTarget =
                    true;

                EditorUtility.SetDirty(
                    buttonImage);
            }

            if (hintText != null)
            {
                hintText.text =
                    string.Empty;

                hintText.gameObject.SetActive(
                    false);

                EditorUtility.SetDirty(
                    hintText);

                EditorUtility.SetDirty(
                    hintText.gameObject);
            }

            EnsurePointableCanvasModule(
                pointableCanvasModuleType);

            EditorUtility.SetDirty(
                trajectoryCanvas.gameObject);

            EditorUtility.SetDirty(
                trajectoryCanvas);

            EditorUtility.SetDirty(
                graphicRaycaster);

            EditorUtility.SetDirty(
                trajectoryButton);

            EditorUtility.SetDirty(
                controller);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[TRAJECTORY VR UI] Stage 10f3 repaired.\n" +
                "Canvas: " +
                GetHierarchyPath(
                    trajectoryCanvas.transform) +
                "\nPointableCanvas: " +
                targetPointable.GetType().FullName +
                "\nPlaneSurface: " +
                targetPlane.GetType().FullName +
                "\nRayInteractable: " +
                targetRay.GetType().FullName);

            EditorUtility.DisplayDialog(
                "Trajectory VR Pointable Repaired",
                "The missing Meta VR UI components were restored on TrajectoryToggleHUD.\n\n" +
                "- PointableCanvas restored\n" +
                "- PlaneSurface restored\n" +
                "- RayInteractable restored\n" +
                "- References repaired\n" +
                "- GraphicRaycaster enabled\n" +
                "- Button raycast enabled\n" +
                "- Hint text remains hidden\n\n" +
                "The components were copied from the known-good TimedGameCanvas.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 10f3 Repair Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10f3 - Validate Trajectory VR Pointable")]
    public static void Validate()
    {
        TrajectoryToggleController controller =
            UnityEngine.Object
                .FindFirstObjectByType<TrajectoryToggleController>();

        if (controller == null)
        {
            throw new InvalidOperationException(
                "TrajectoryToggleController was not found.");
        }

        SerializedObject serializedController =
            new SerializedObject(controller);

        Button trajectoryButton =
            GetReference<Button>(
                serializedController,
                "uiButton");

        Text hintText =
            GetReference<Text>(
                serializedController,
                "hintText");

        if (trajectoryButton == null)
        {
            throw new InvalidOperationException(
                "Trajectory button reference is missing.");
        }

        Canvas trajectoryCanvas =
            trajectoryButton.GetComponentInParent<Canvas>(
                true);

        if (trajectoryCanvas == null)
        {
            throw new InvalidOperationException(
                "Trajectory button is not inside a Canvas.");
        }

        Type pointableCanvasType =
            FindComponentType(
                PointableCanvasTypeName);

        Type planeSurfaceType =
            FindComponentType(
                PlaneSurfaceTypeName);

        Type rayInteractableType =
            FindComponentType(
                RayInteractableTypeName);

        Type pointableCanvasModuleType =
            FindComponentType(
                PointableCanvasModuleTypeName);

        Component pointable =
            trajectoryCanvas.GetComponent(
                pointableCanvasType);

        Component plane =
            trajectoryCanvas.GetComponent(
                planeSurfaceType);

        Component ray =
            trajectoryCanvas.GetComponent(
                rayInteractableType);

        GraphicRaycaster graphicRaycaster =
            trajectoryCanvas
                .GetComponent<GraphicRaycaster>();

        if (pointable == null)
        {
            throw new InvalidOperationException(
                "PointableCanvas is still missing from TrajectoryToggleHUD.");
        }

        if (plane == null)
        {
            throw new InvalidOperationException(
                "PlaneSurface is still missing from TrajectoryToggleHUD.");
        }

        if (ray == null)
        {
            throw new InvalidOperationException(
                "RayInteractable is still missing from TrajectoryToggleHUD.");
        }

        if (
            graphicRaycaster == null ||
            !graphicRaycaster.enabled
        )
        {
            throw new InvalidOperationException(
                "GraphicRaycaster is missing or disabled.");
        }

        if (!trajectoryButton.interactable)
        {
            throw new InvalidOperationException(
                "Trajectory button is not interactable.");
        }

        Image buttonImage =
            trajectoryButton.GetComponent<Image>();

        if (
            buttonImage == null ||
            !buttonImage.raycastTarget
        )
        {
            throw new InvalidOperationException(
                "Trajectory button Image is missing or does not receive raycasts.");
        }

        if (
            hintText != null &&
            (
                hintText.gameObject.activeSelf ||
                !string.IsNullOrWhiteSpace(
                    hintText.text)
            )
        )
        {
            throw new InvalidOperationException(
                "Trajectory hint text is still visible.");
        }

        ValidateReference(
            pointable,
            "_canvas",
            trajectoryCanvas);

        ValidateReference(
            ray,
            "_pointableElement",
            pointable);

        ValidateReference(
            ray,
            "_surface",
            plane);

        EventSystem eventSystem =
            UnityEngine.Object
                .FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            throw new InvalidOperationException(
                "EventSystem is missing.");
        }

        Component pointableCanvasModule =
            eventSystem.GetComponent(
                pointableCanvasModuleType);

        if (pointableCanvasModule == null)
        {
            throw new InvalidOperationException(
                "PointableCanvasModule is missing from EventSystem.");
        }

        string runtimePath =
            "Assets/_Project/Wildlife/Runtime/TrajectoryToggleController.cs";

        string source =
            System.IO.File.ReadAllText(
                runtimePath);

        string[] forbiddenInputFragments =
        {
            "RawButton.LThumbstick",
            "RawButton.RThumbstick",
            "Keyboard.current",
            "Input.GetKeyDown",
            "PRESS EITHER THUMBSTICK"
        };

        foreach (string fragment in forbiddenInputFragments)
        {
            if (
                source.IndexOf(
                    fragment,
                    StringComparison.Ordinal) >= 0
            )
            {
                throw new InvalidOperationException(
                    "Old trajectory input remains in source: " +
                    fragment);
            }
        }

        Debug.Log(
            "[TRAJECTORY VR UI] Stage 10f3 validation passed.\n" +
            "Canvas: " +
            GetHierarchyPath(
                trajectoryCanvas.transform));

        EditorUtility.DisplayDialog(
            "Trajectory VR Pointable Validated",
            "TrajectoryToggleHUD now has all required Meta VR UI components and references.\n\n" +
            "Final click/pinch behaviour must be tested on Meta Quest.",
            "OK");
    }

    private static Canvas FindCanvasByName(
        string objectName)
    {
        Canvas[] canvases =
            UnityEngine.Object
                .FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (
                canvas != null &&
                canvas.gameObject.name ==
                    objectName
            )
            {
                return canvas;
            }
        }

        return null;
    }

    private static Type FindComponentType(
        string fullName)
    {
        foreach (Type type in TypeCache.GetTypesDerivedFrom<Component>())
        {
            if (type.FullName == fullName)
            {
                return type;
            }
        }

        throw new InvalidOperationException(
            "Component type was not found: " +
            fullName);
    }

    private static Component GetOrCopyComponent(
        Component source,
        GameObject target,
        Type componentType)
    {
        Component existing =
            target.GetComponent(
                componentType);

        if (existing != null)
        {
            return existing;
        }

        if (!ComponentUtility.CopyComponent(source))
        {
            throw new InvalidOperationException(
                "Could not copy component: " +
                componentType.FullName);
        }

        if (!ComponentUtility.PasteComponentAsNew(target))
        {
            throw new InvalidOperationException(
                "Could not paste component: " +
                componentType.FullName);
        }

        Component created =
            target.GetComponent(
                componentType);

        if (created == null)
        {
            throw new InvalidOperationException(
                "Copied component was not created: " +
                componentType.FullName);
        }

        return created;
    }

    private static void EnsurePointableCanvasModule(
        Type pointableCanvasModuleType)
    {
        EventSystem eventSystem =
            UnityEngine.Object
                .FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            throw new InvalidOperationException(
                "EventSystem was not found.");
        }

        Component module =
            eventSystem.GetComponent(
                pointableCanvasModuleType);

        if (module == null)
        {
            module =
                Undo.AddComponent(
                    eventSystem.gameObject,
                    pointableCanvasModuleType);
        }

        EnableBehaviour(
            module);

        EditorUtility.SetDirty(
            module);

        EditorUtility.SetDirty(
            eventSystem.gameObject);
    }

    private static void SetObjectReference(
        Component component,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedObject serialized =
            new SerializedObject(
                component);

        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                component.GetType().FullName +
                " is missing serialized property: " +
                propertyName);
        }

        property.objectReferenceValue =
            value;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            component);
    }

    private static void SetObjectReferenceIfPresent(
        Component component,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedObject serialized =
            new SerializedObject(
                component);

        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            return;
        }

        property.objectReferenceValue =
            value;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            component);
    }

    private static void ValidateReference(
        Component component,
        string propertyName,
        UnityEngine.Object expected)
    {
        SerializedObject serialized =
            new SerializedObject(
                component);

        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                component.GetType().FullName +
                " is missing serialized property: " +
                propertyName);
        }

        if (
            property.objectReferenceValue !=
            expected
        )
        {
            throw new InvalidOperationException(
                component.GetType().FullName +
                "." +
                propertyName +
                " has the wrong reference.");
        }
    }

    private static void EnableBehaviour(
        Component component)
    {
        Behaviour behaviour =
            component as Behaviour;

        if (behaviour != null)
        {
            behaviour.enabled =
                true;
        }
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

    private static string GetHierarchyPath(
        Transform item)
    {
        if (item == null)
        {
            return "<null>";
        }

        string path =
            item.name;

        Transform current =
            item.parent;

        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }

        return path;
    }
}
#endif