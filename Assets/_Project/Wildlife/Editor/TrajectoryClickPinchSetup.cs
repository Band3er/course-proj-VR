#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TrajectoryClickPinchSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10f v2 - Apply Trajectory Click Pinch")]
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
                new SerializedObject(
                    controller);

            Text statusText =
                GetReference<Text>(
                    serializedController,
                    "statusText");

            Text hintText =
                GetReference<Text>(
                    serializedController,
                    "hintText");

            Button uiButton =
                GetReference<Button>(
                    serializedController,
                    "uiButton");

            if (uiButton == null)
            {
                throw new InvalidOperationException(
                    "Trajectory UI Button is missing.");
            }

            Canvas trajectoryCanvas =
                uiButton.GetComponentInParent<Canvas>(
                    true);

            if (trajectoryCanvas == null)
            {
                throw new InvalidOperationException(
                    "Trajectory UI Button is not inside a Canvas.");
            }

            uiButton.interactable =
                true;

            EditorUtility.SetDirty(
                uiButton);

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

            if (statusText != null)
            {
                RectTransform statusRect =
                    statusText.rectTransform;

                statusRect.anchorMin =
                    Vector2.zero;

                statusRect.anchorMax =
                    Vector2.one;

                statusRect.offsetMin =
                    Vector2.zero;

                statusRect.offsetMax =
                    Vector2.zero;

                statusText.alignment =
                    TextAnchor.MiddleCenter;

                EditorUtility.SetDirty(
                    statusRect);

                EditorUtility.SetDirty(
                    statusText);
            }

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

            EditorUtility.SetDirty(
                graphicRaycaster);

            EditorUtility.SetDirty(
                controller);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[TRAJECTORY UI] Stage 10f v2 applied. " +
                "Canvas root: " +
                GetHierarchyPath(
                    trajectoryCanvas.transform));

            EditorUtility.DisplayDialog(
                "Trajectory Click / Pinch Applied",
                "Trajectory remains a normal VR UI button.\n\n" +
                "- Controller: point and click.\n" +
                "- Hand tracking: point and pinch.\n" +
                "- The instruction line is hidden.\n" +
                "- Validation now checks the complete Canvas hierarchy.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 10f v2 Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10f v2 - Validate Trajectory Click Pinch")]
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
            new SerializedObject(
                controller);

        Text hintText =
            GetReference<Text>(
                serializedController,
                "hintText");

        Button uiButton =
            GetReference<Button>(
                serializedController,
                "uiButton");

        if (uiButton == null)
        {
            throw new InvalidOperationException(
                "Trajectory UI Button is missing.");
        }

        if (!uiButton.interactable)
        {
            throw new InvalidOperationException(
                "Trajectory UI Button is not interactable.");
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
                "Trajectory input hint is still visible or contains text.");
        }

        Canvas trajectoryCanvas =
            uiButton.GetComponentInParent<Canvas>(
                true);

        if (trajectoryCanvas == null)
        {
            throw new InvalidOperationException(
                "Trajectory UI Button is not inside a Canvas.");
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

        Component pointableCanvas =
            FindComponentAroundCanvas(
                trajectoryCanvas,
                "Oculus.Interaction.PointableCanvas");

        Component rayInteractable =
            FindComponentAroundCanvas(
                trajectoryCanvas,
                "Oculus.Interaction.RayInteractable");

        GraphicRaycaster graphicRaycaster =
            trajectoryCanvas
                .GetComponent<GraphicRaycaster>();

        List<string> missing =
            new List<string>();

        if (pointableCanvas == null)
        {
            missing.Add(
                "Oculus.Interaction.PointableCanvas");
        }

        if (rayInteractable == null)
        {
            missing.Add(
                "Oculus.Interaction.RayInteractable");
        }

        if (graphicRaycaster == null)
        {
            missing.Add(
                "UnityEngine.UI.GraphicRaycaster");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Trajectory Canvas hierarchy is missing:\n" +
                string.Join(
                    "\n",
                    missing) +
                "\n\nCanvas path:\n" +
                GetHierarchyPath(
                    trajectoryCanvas.transform));
        }

        Debug.Log(
            "[TRAJECTORY UI] Stage 10f v2 validation passed.\n" +
            "Canvas: " +
            GetHierarchyPath(
                trajectoryCanvas.transform) +
            "\nPointableCanvas: " +
            GetHierarchyPath(
                pointableCanvas.transform) +
            "\nRayInteractable: " +
            GetHierarchyPath(
                rayInteractable.transform));

        EditorUtility.DisplayDialog(
            "Trajectory Click / Pinch Validated",
            "Trajectory HUD hierarchy is valid.\n\n" +
            "PointableCanvas, RayInteractable and GraphicRaycaster were found around the actual trajectory Canvas.\n\n" +
            "Final behaviour must still be tested on Meta Quest.",
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

    private static Component FindComponentAroundCanvas(
        Canvas canvas,
        string requiredTypeName)
    {
        if (canvas == null)
        {
            return null;
        }

        Component direct =
            FindComponentByTypeName(
                canvas.gameObject,
                requiredTypeName);

        if (direct != null)
        {
            return direct;
        }

        Transform current =
            canvas.transform.parent;

        while (current != null)
        {
            Component parentMatch =
                FindComponentByTypeName(
                    current.gameObject,
                    requiredTypeName);

            if (parentMatch != null)
            {
                return parentMatch;
            }

            current =
                current.parent;
        }

        Component[] childComponents =
            canvas.GetComponentsInChildren<Component>(
                true);

        foreach (Component component in childComponents)
        {
            if (
                component != null &&
                component.GetType().FullName ==
                    requiredTypeName
            )
            {
                return component;
            }
        }

        return null;
    }

    private static Component FindComponentByTypeName(
        GameObject gameObject,
        string requiredTypeName)
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
                    requiredTypeName
            )
            {
                return component;
            }
        }

        return null;
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