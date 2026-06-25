#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TrajectorySharedCanvasSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private const string PointableCanvasTypeName =
        "Oculus.Interaction.PointableCanvas";

    private const string PlaneSurfaceTypeName =
        "Oculus.Interaction.Surfaces.PlaneSurface";

    private const string RayInteractableTypeName =
        "Oculus.Interaction.RayInteractable";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10f4 - Move Trajectory To Shared HUD Canvas")]
    public static void Apply()
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

            Text statusText =
                GetReference<Text>(
                    serializedController,
                    "statusText");

            Text hintText =
                GetReference<Text>(
                    serializedController,
                    "hintText");

            if (trajectoryButton == null)
            {
                throw new InvalidOperationException(
                    "Trajectory button reference is missing.");
            }

            Canvas oldTrajectoryCanvas =
                FindCanvasByName(
                    "TrajectoryToggleHUD");

            Canvas timedGameCanvas =
                FindCanvasByName(
                    "TimedGameCanvas");

            if (oldTrajectoryCanvas == null)
            {
                throw new InvalidOperationException(
                    "TrajectoryToggleHUD Canvas was not found.");
            }

            if (timedGameCanvas == null)
            {
                throw new InvalidOperationException(
                    "TimedGameCanvas was not found.");
            }

            RectTransform roundHud =
                FindDescendantRect(
                    timedGameCanvas.transform,
                    "RoundHUD");

            RectTransform pauseButtonRect =
                FindDescendantRect(
                    timedGameCanvas.transform,
                    "PauseButton");

            if (roundHud == null)
            {
                throw new InvalidOperationException(
                    "RoundHUD was not found.");
            }

            if (pauseButtonRect == null)
            {
                throw new InvalidOperationException(
                    "PauseButton was not found.");
            }

            RectTransform trajectoryRect =
                trajectoryButton.transform as RectTransform;

            if (trajectoryRect == null)
            {
                throw new InvalidOperationException(
                    "Trajectory button has no RectTransform.");
            }

            Vector3[] worldCorners =
                new Vector3[4];

            trajectoryRect.GetWorldCorners(
                worldCorners);

            Undo.SetTransformParent(
                trajectoryRect,
                roundHud,
                "Move Trajectory Button Into Round HUD");

            ApplyWorldRectToParent(
                trajectoryRect,
                roundHud,
                worldCorners);

            trajectoryRect.SetAsLastSibling();

            trajectoryButton.interactable =
                true;

            Image trajectoryImage =
                trajectoryButton.GetComponent<Image>();

            if (trajectoryImage == null)
            {
                throw new InvalidOperationException(
                    "Trajectory button Image is missing.");
            }

            trajectoryImage.raycastTarget =
                true;

            if (statusText != null)
            {
                statusText.raycastTarget =
                    false;

                statusText.alignment =
                    TextAnchor.MiddleCenter;

                EditorUtility.SetDirty(
                    statusText);
            }

            if (hintText != null)
            {
                hintText.text =
                    string.Empty;

                hintText.raycastTarget =
                    false;

                hintText.gameObject.SetActive(
                    false);

                EditorUtility.SetDirty(
                    hintText);

                EditorUtility.SetDirty(
                    hintText.gameObject);
            }

            DisableOldTrajectoryCanvasInteraction(
                oldTrajectoryCanvas);

            Canvas resultingCanvas =
                trajectoryButton
                    .GetComponentInParent<Canvas>(
                        true);

            if (resultingCanvas != timedGameCanvas)
            {
                throw new InvalidOperationException(
                    "Trajectory button was not moved under TimedGameCanvas.");
            }

            EditorUtility.SetDirty(
                trajectoryRect);

            EditorUtility.SetDirty(
                trajectoryButton);

            EditorUtility.SetDirty(
                trajectoryImage);

            EditorUtility.SetDirty(
                controller);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[TRAJECTORY HUD] Stage 10f4 applied.\n" +
                "Trajectory button path: " +
                GetHierarchyPath(
                    trajectoryRect) +
                "\nShared Canvas: " +
                GetHierarchyPath(
                    timedGameCanvas.transform) +
                "\nOld trajectory ray surface disabled.");

            EditorUtility.DisplayDialog(
                "Trajectory Moved To Shared HUD",
                "Trajectory and Pause/Quit now use the same TimedGameCanvas.\n\n" +
                "This removes the separate trajectory ray surface that could intercept clicks intended for Quit.\n\n" +
                "- Trajectory remains click/pinch controlled.\n" +
                "- Quit remains a normal button.\n" +
                "- The old TrajectoryToggleHUD Canvas stays only as the runtime controller host.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 10f4 Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10f4 - Validate Shared HUD Canvas")]
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

        if (trajectoryButton == null)
        {
            throw new InvalidOperationException(
                "Trajectory button reference is missing.");
        }

        Canvas timedGameCanvas =
            FindCanvasByName(
                "TimedGameCanvas");

        Canvas oldTrajectoryCanvas =
            FindCanvasByName(
                "TrajectoryToggleHUD");

        if (timedGameCanvas == null)
        {
            throw new InvalidOperationException(
                "TimedGameCanvas was not found.");
        }

        RectTransform roundHud =
            FindDescendantRect(
                timedGameCanvas.transform,
                "RoundHUD");

        RectTransform pauseButtonRect =
            FindDescendantRect(
                timedGameCanvas.transform,
                "PauseButton");

        if (roundHud == null)
        {
            throw new InvalidOperationException(
                "RoundHUD was not found.");
        }

        if (pauseButtonRect == null)
        {
            throw new InvalidOperationException(
                "PauseButton was not found.");
        }

        Button pauseButton =
            pauseButtonRect.GetComponent<Button>();

        if (
            pauseButton == null ||
            !pauseButton.interactable
        )
        {
            throw new InvalidOperationException(
                "Pause/Quit button is missing or not interactable.");
        }

        Image pauseImage =
            pauseButton.GetComponent<Image>();

        if (
            pauseImage == null ||
            !pauseImage.raycastTarget
        )
        {
            throw new InvalidOperationException(
                "Pause/Quit button does not receive raycasts.");
        }

        Canvas trajectoryCanvas =
            trajectoryButton.GetComponentInParent<Canvas>(
                true);

        if (trajectoryCanvas != timedGameCanvas)
        {
            throw new InvalidOperationException(
                "Trajectory button is not using TimedGameCanvas.");
        }

        if (!trajectoryButton.transform.IsChildOf(roundHud))
        {
            throw new InvalidOperationException(
                "Trajectory button is not inside RoundHUD.");
        }

        if (!trajectoryButton.interactable)
        {
            throw new InvalidOperationException(
                "Trajectory button is not interactable.");
        }

        Image trajectoryImage =
            trajectoryButton.GetComponent<Image>();

        if (
            trajectoryImage == null ||
            !trajectoryImage.raycastTarget
        )
        {
            throw new InvalidOperationException(
                "Trajectory button does not receive raycasts.");
        }

        if (oldTrajectoryCanvas != null)
        {
            if (oldTrajectoryCanvas.enabled)
            {
                throw new InvalidOperationException(
                    "Old TrajectoryToggleHUD Canvas is still enabled.");
            }

            GraphicRaycaster oldRaycaster =
                oldTrajectoryCanvas
                    .GetComponent<GraphicRaycaster>();

            if (
                oldRaycaster != null &&
                oldRaycaster.enabled
            )
            {
                throw new InvalidOperationException(
                    "Old TrajectoryToggleHUD GraphicRaycaster is still enabled.");
            }

            Component oldRayInteractable =
                FindComponentByTypeName(
                    oldTrajectoryCanvas.gameObject,
                    RayInteractableTypeName);

            Behaviour oldRayBehaviour =
                oldRayInteractable as Behaviour;

            if (
                oldRayBehaviour != null &&
                oldRayBehaviour.enabled
            )
            {
                throw new InvalidOperationException(
                    "Old TrajectoryToggleHUD RayInteractable is still enabled.");
            }
        }

        RectTransform trajectoryRect =
            trajectoryButton.transform as RectTransform;

        if (
            trajectoryRect != null &&
            WorldRectsOverlap(
                trajectoryRect,
                pauseButtonRect)
        )
        {
            throw new InvalidOperationException(
                "Trajectory and Pause/Quit buttons overlap.");
        }

        Debug.Log(
            "[TRAJECTORY HUD] Stage 10f4 validation passed.\n" +
            "Trajectory and Pause/Quit share TimedGameCanvas.");

        EditorUtility.DisplayDialog(
            "Shared HUD Canvas Validated",
            "Trajectory and Pause/Quit now share the same pointable Canvas.\n\n" +
            "The old separate trajectory ray surface is disabled and cannot block Quit.\n\n" +
            "Final behaviour must be tested on Meta Quest.",
            "OK");
    }

    private static void DisableOldTrajectoryCanvasInteraction(
        Canvas oldCanvas)
    {
        if (oldCanvas == null)
        {
            return;
        }

        oldCanvas.enabled =
            false;

        EditorUtility.SetDirty(
            oldCanvas);

        GraphicRaycaster raycaster =
            oldCanvas.GetComponent<GraphicRaycaster>();

        if (raycaster != null)
        {
            raycaster.enabled =
                false;

            EditorUtility.SetDirty(
                raycaster);
        }

        CanvasScaler scaler =
            oldCanvas.GetComponent<CanvasScaler>();

        if (scaler != null)
        {
            scaler.enabled =
                false;

            EditorUtility.SetDirty(
                scaler);
        }

        SetBehaviourEnabledByTypeName(
            oldCanvas.gameObject,
            PointableCanvasTypeName,
            false);

        SetBehaviourEnabledByTypeName(
            oldCanvas.gameObject,
            PlaneSurfaceTypeName,
            false);

        SetBehaviourEnabledByTypeName(
            oldCanvas.gameObject,
            RayInteractableTypeName,
            false);
    }

    private static void ApplyWorldRectToParent(
        RectTransform target,
        RectTransform parent,
        Vector3[] worldCorners)
    {
        Vector3 first =
            parent.InverseTransformPoint(
                worldCorners[0]);

        float minX =
            first.x;

        float maxX =
            first.x;

        float minY =
            first.y;

        float maxY =
            first.y;

        for (
            int index = 1;
            index < worldCorners.Length;
            index++
        )
        {
            Vector3 local =
                parent.InverseTransformPoint(
                    worldCorners[index]);

            minX =
                Mathf.Min(
                    minX,
                    local.x);

            maxX =
                Mathf.Max(
                    maxX,
                    local.x);

            minY =
                Mathf.Min(
                    minY,
                    local.y);

            maxY =
                Mathf.Max(
                    maxY,
                    local.y);
        }

        target.anchorMin =
            new Vector2(
                0.5f,
                0.5f);

        target.anchorMax =
            new Vector2(
                0.5f,
                0.5f);

        target.pivot =
            new Vector2(
                0.5f,
                0.5f);

        target.localRotation =
            Quaternion.identity;

        target.localScale =
            Vector3.one;

        target.sizeDelta =
            new Vector2(
                Mathf.Max(
                    1f,
                    maxX - minX),
                Mathf.Max(
                    1f,
                    maxY - minY));

        target.anchoredPosition =
            new Vector2(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f);

        target.localPosition =
            new Vector3(
                target.localPosition.x,
                target.localPosition.y,
                0f);
    }

    private static bool WorldRectsOverlap(
        RectTransform first,
        RectTransform second)
    {
        Rect firstRect =
            GetWorldAxisAlignedRect(
                first);

        Rect secondRect =
            GetWorldAxisAlignedRect(
                second);

        return firstRect.Overlaps(
            secondRect);
    }

    private static Rect GetWorldAxisAlignedRect(
        RectTransform target)
    {
        Vector3[] corners =
            new Vector3[4];

        target.GetWorldCorners(
            corners);

        float minX =
            corners[0].x;

        float maxX =
            corners[0].x;

        float minY =
            corners[0].y;

        float maxY =
            corners[0].y;

        for (
            int index = 1;
            index < corners.Length;
            index++
        )
        {
            minX =
                Mathf.Min(
                    minX,
                    corners[index].x);

            maxX =
                Mathf.Max(
                    maxX,
                    corners[index].x);

            minY =
                Mathf.Min(
                    minY,
                    corners[index].y);

            maxY =
                Mathf.Max(
                    maxY,
                    corners[index].y);
        }

        return Rect.MinMaxRect(
            minX,
            minY,
            maxX,
            maxY);
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

    private static RectTransform FindDescendantRect(
        Transform root,
        string objectName)
    {
        if (root == null)
        {
            return null;
        }

        RectTransform[] rects =
            root.GetComponentsInChildren<RectTransform>(
                true);

        foreach (RectTransform rect in rects)
        {
            if (
                rect != null &&
                rect.gameObject.name ==
                    objectName
            )
            {
                return rect;
            }
        }

        return null;
    }

    private static Component FindComponentByTypeName(
        GameObject gameObject,
        string typeName)
    {
        if (gameObject == null)
        {
            return null;
        }

        Component[] components =
            gameObject.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (
                component != null &&
                component.GetType().FullName ==
                    typeName
            )
            {
                return component;
            }
        }

        return null;
    }

    private static void SetBehaviourEnabledByTypeName(
        GameObject gameObject,
        string typeName,
        bool enabled)
    {
        Component component =
            FindComponentByTypeName(
                gameObject,
                typeName);

        Behaviour behaviour =
            component as Behaviour;

        if (behaviour == null)
        {
            return;
        }

        behaviour.enabled =
            enabled;

        EditorUtility.SetDirty(
            behaviour);
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