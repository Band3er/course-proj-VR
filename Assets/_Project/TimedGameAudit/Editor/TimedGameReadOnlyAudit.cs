using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TimedGameReadOnlyAudit
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private static readonly string[] RelevantTypeKeywords =
    {
        "WildlifeScore",
        "WildlifeHUD",
        "WildlifeAnimal",
        "WildlifeHitbox",
        "LivingBirdArrowTarget",
        "BowDrawController",
        "ArrowController",
        "ArrowCollisionHandler",
        "ArrowTrajectoryPreview",
        "ExperimentManager",
        "ExperimentLogger",
        "ArcheryEventBridge",
        "ShotData",
        "Canvas",
        "EventSystem",
        "Raycaster",
        "Button",
        "TMP",
        "TextMeshPro",
        "OVRHand",
        "HandGrab",
        "GrabInteractable",
        "Controller",
        "Locomotion",
        "Teleport",
        "Score",
        "HUD"
    };

    private static readonly string[] SourceMarkers =
    {
        "class WildlifeScoreManager",
        "class WildlifeHUD",
        "class WildlifeAnimal",
        "class WildlifeHitbox",
        "class LivingBirdArrowTarget",
        "class WildlifeDynamicScore",
        "class BowDrawController",
        "class ArrowController",
        "class ArrowCollisionHandler",
        "class ArrowTrajectoryPreview",
        "class ExperimentManager",
        "class ExperimentLogger",
        "class ArcheryEventBridge",
        "class ShotData",
        "Application.persistentDataPath",
        "enum InputMode",
        "RegisterHit(",
        "RegisterArrow",
        "LaunchArrow",
        "ReleaseArrow"
    };

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08a - Run Read-Only Audit")]
    public static void RunAudit()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (
            !activeScene.IsValid() ||
            !activeScene.isLoaded
        )
        {
            EditorUtility.DisplayDialog(
                "Timed Game Audit",
                "No valid loaded scene was found.",
                "OK");

            return;
        }

        if (
            !string.Equals(
                activeScene.path,
                RequiredScenePath,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            EditorUtility.DisplayDialog(
                "Timed Game Audit",
                "Open and save the required scene first:\n\n" +
                RequiredScenePath +
                "\n\nCurrent scene:\n" +
                activeScene.path,
                "OK");

            return;
        }

        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorUtility.DisplayDialog(
                "Timed Game Audit",
                "Save the scene before running the audit.",
                "OK");

            return;
        }

        try
        {
            string timestamp =
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss",
                    CultureInfo.InvariantCulture);

            string reportAssetFolder =
                "Assets/_Project/TimedGameAudit/Reports";

            string reportAbsoluteFolder =
                Path.GetFullPath(reportAssetFolder);

            Directory.CreateDirectory(
                reportAbsoluteFolder);

            string reportFileName =
                "TimedGame_ReadOnly_Audit_" +
                timestamp +
                ".txt";

            string reportAbsolutePath =
                Path.Combine(
                    reportAbsoluteFolder,
                    reportFileName);

            StringBuilder report =
                new StringBuilder(262144);

            AppendHeader(
                report,
                "FOREST ARCHERY - TIMED GAME READ-ONLY AUDIT");

            report.AppendLine(
                "Generated: " +
                DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture));

            report.AppendLine(
                "Unity version: " +
                Application.unityVersion);

            report.AppendLine(
                "Project path: " +
                Path.GetFullPath("."));

            report.AppendLine(
                "Active scene: " +
                activeScene.path);

            report.AppendLine(
                "Scene dirty: " +
                activeScene.isDirty);

            report.AppendLine(
                "Application persistentDataPath: " +
                Application.persistentDataPath);

            report.AppendLine(
                "Android application identifier: " +
                PlayerSettings.GetApplicationIdentifier(
                    BuildTargetGroup.Android));

            report.AppendLine();

            AppendBuildSettings(
                report);

            List<Component> sceneComponents =
                CollectSceneComponents(
                    activeScene);

            AppendSceneOverview(
                report,
                activeScene,
                sceneComponents);

            AppendRelevantComponents(
                report,
                sceneComponents);

            AppendCanvasAndUiOverview(
                report,
                sceneComponents);

            AppendCameraOverview(
                report,
                sceneComponents);

            AppendPotentialGameplayManagers(
                report,
                sceneComponents);

            AppendSourceFiles(
                report);

            AppendRecommendedIntegrationPoints(
                report);

            File.WriteAllText(
                reportAbsolutePath,
                report.ToString(),
                new UTF8Encoding(false));

            AssetDatabase.Refresh();

            string reportAssetPath =
                reportAssetFolder +
                "/" +
                reportFileName;

            TextAsset reportAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    reportAssetPath);

            Selection.activeObject =
                reportAsset;

            EditorGUIUtility.PingObject(
                reportAsset);

            Debug.Log(
                "[TIMED GAME AUDIT] Read-only audit completed\n" +
                reportAbsolutePath);

            EditorUtility.DisplayDialog(
                "Timed Game Audit Complete",
                "No scene or gameplay asset was modified.\n\n" +
                "Report:\n" +
                reportAbsolutePath,
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Timed Game Audit Failed",
                exception.Message,
                "OK");
        }
    }

    private static List<Component> CollectSceneComponents(
        Scene scene)
    {
        List<Component> components =
            new List<Component>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component[] found =
                root.GetComponentsInChildren<Component>(
                    true);

            foreach (Component component in found)
            {
                if (component != null)
                {
                    components.Add(
                        component);
                }
            }
        }

        return components;
    }

    private static void AppendBuildSettings(
        StringBuilder report)
    {
        AppendHeader(
            report,
            "BUILD SETTINGS");

        EditorBuildSettingsScene[] scenes =
            EditorBuildSettings.scenes;

        if (scenes == null || scenes.Length == 0)
        {
            report.AppendLine(
                "<no scenes in build settings>");

            report.AppendLine();
            return;
        }

        for (
            int index = 0;
            index < scenes.Length;
            index++
        )
        {
            EditorBuildSettingsScene scene =
                scenes[index];

            report.AppendLine(
                index.ToString(
                    CultureInfo.InvariantCulture) +
                " | enabled=" +
                scene.enabled +
                " | " +
                scene.path);
        }

        report.AppendLine();
    }

    private static void AppendSceneOverview(
        StringBuilder report,
        Scene scene,
        List<Component> sceneComponents)
    {
        AppendHeader(
            report,
            "SCENE OVERVIEW");

        GameObject[] roots =
            scene.GetRootGameObjects();

        report.AppendLine(
            "Root objects: " +
            roots.Length);

        report.AppendLine(
            "Total non-null components: " +
            sceneComponents.Count);

        report.AppendLine();

        foreach (
            GameObject root in roots
                .OrderBy(item => item.name)
        )
        {
            report.AppendLine(
                "- " +
                root.name +
                " | active=" +
                root.activeSelf +
                " | children=" +
                root.transform.childCount);
        }

        report.AppendLine();
    }

    private static void AppendRelevantComponents(
        StringBuilder report,
        List<Component> components)
    {
        AppendHeader(
            report,
            "RELEVANT SCENE COMPONENTS AND SERIALIZED REFERENCES");

        List<Component> relevant =
            components
                .Where(IsRelevantComponent)
                .OrderBy(
                    component =>
                        GetHierarchyPath(
                            component.transform))
                .ThenBy(
                    component =>
                        component.GetType().FullName)
                .ToList();

        report.AppendLine(
            "Relevant component count: " +
            relevant.Count);

        report.AppendLine();

        foreach (Component component in relevant)
        {
            Type type =
                component.GetType();

            report.AppendLine(
                "OBJECT: " +
                GetHierarchyPath(
                    component.transform));

            report.AppendLine(
                "ACTIVE: " +
                component.gameObject.activeInHierarchy);

            report.AppendLine(
                "COMPONENT: " +
                type.FullName);

            AppendSerializedProperties(
                report,
                component);

            report.AppendLine(
                new string('-', 72));
        }

        report.AppendLine();
    }

    private static bool IsRelevantComponent(
        Component component)
    {
        string typeName =
            component.GetType().FullName ??
            component.GetType().Name;

        foreach (string keyword in RelevantTypeKeywords)
        {
            if (
                typeName.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendSerializedProperties(
        StringBuilder report,
        Component component)
    {
        try
        {
            SerializedObject serializedObject =
                new SerializedObject(
                    component);

            SerializedProperty property =
                serializedObject.GetIterator();

            int appended = 0;
            bool enterChildren = true;

            while (
                property.NextVisible(
                    enterChildren)
            )
            {
                enterChildren = false;

                if (property.name == "m_Script")
                {
                    continue;
                }

                string value =
                    GetSerializedValue(
                        property);

                report.AppendLine(
                    "  " +
                    property.propertyPath +
                    " = " +
                    value);

                appended++;

                if (appended >= 100)
                {
                    report.AppendLine(
                        "  <property output truncated at 100 entries>");

                    break;
                }
            }
        }
        catch (Exception exception)
        {
            report.AppendLine(
                "  <serialized inspection failed: " +
                exception.Message +
                ">");
        }
    }

    private static string GetSerializedValue(
        SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.longValue.ToString(
                    CultureInfo.InvariantCulture);

            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();

            case SerializedPropertyType.Float:
                return property.doubleValue.ToString(
                    "F6",
                    CultureInfo.InvariantCulture);

            case SerializedPropertyType.String:
                return "\"" +
                    property.stringValue +
                    "\"";

            case SerializedPropertyType.Color:
                return property.colorValue.ToString();

            case SerializedPropertyType.ObjectReference:
                if (property.objectReferenceValue == null)
                {
                    return "<null>";
                }

                return
                    property.objectReferenceValue.name +
                    " [" +
                    property.objectReferenceValue.GetType().FullName +
                    "]";

            case SerializedPropertyType.LayerMask:
                return property.intValue.ToString(
                    CultureInfo.InvariantCulture);

            case SerializedPropertyType.Enum:
                if (
                    property.enumValueIndex >= 0 &&
                    property.enumValueIndex <
                        property.enumDisplayNames.Length
                )
                {
                    return
                        property.enumDisplayNames[
                            property.enumValueIndex] +
                        " (" +
                        property.enumValueIndex +
                        ")";
                }

                return property.enumValueIndex.ToString(
                    CultureInfo.InvariantCulture);

            case SerializedPropertyType.Vector2:
                return property.vector2Value.ToString();

            case SerializedPropertyType.Vector3:
                return property.vector3Value.ToString();

            case SerializedPropertyType.Vector4:
                return property.vector4Value.ToString();

            case SerializedPropertyType.Rect:
                return property.rectValue.ToString();

            case SerializedPropertyType.ArraySize:
                return property.intValue.ToString(
                    CultureInfo.InvariantCulture);

            case SerializedPropertyType.Character:
                return property.intValue.ToString(
                    CultureInfo.InvariantCulture);

            case SerializedPropertyType.AnimationCurve:
                return
                    "AnimationCurve(keys=" +
                    property.animationCurveValue.length +
                    ")";

            case SerializedPropertyType.Bounds:
                return property.boundsValue.ToString();

            case SerializedPropertyType.Quaternion:
                return property.quaternionValue.ToString();

            case SerializedPropertyType.ExposedReference:
                return
                    property.exposedReferenceValue != null
                        ? property.exposedReferenceValue.name
                        : "<null>";

            case SerializedPropertyType.Vector2Int:
                return property.vector2IntValue.ToString();

            case SerializedPropertyType.Vector3Int:
                return property.vector3IntValue.ToString();

            case SerializedPropertyType.RectInt:
                return property.rectIntValue.ToString();

            case SerializedPropertyType.BoundsInt:
                return property.boundsIntValue.ToString();

            case SerializedPropertyType.ManagedReference:
                return
                    property.managedReferenceFullTypename ??
                    "<null>";

            default:
                return
                    "<" +
                    property.propertyType +
                    ">";
        }
    }

    private static void AppendCanvasAndUiOverview(
        StringBuilder report,
        List<Component> components)
    {
        AppendHeader(
            report,
            "CANVAS AND UI OVERVIEW");

        foreach (
            Component component in components
                .Where(
                    item =>
                        ContainsAny(
                            item.GetType().FullName,
                            "Canvas",
                            "EventSystem",
                            "Raycaster",
                            "Button",
                            "TMP",
                            "TextMeshPro"))
                .OrderBy(
                    item =>
                        GetHierarchyPath(
                            item.transform))
        )
        {
            report.AppendLine(
                GetHierarchyPath(
                    component.transform) +
                " | " +
                component.GetType().FullName +
                " | active=" +
                component.gameObject.activeInHierarchy);

            string visibleText =
                TryReadTextValue(
                    component);

            if (!string.IsNullOrEmpty(visibleText))
            {
                report.AppendLine(
                    "  text=\"" +
                    visibleText.Replace(
                        "\n",
                        "\\n") +
                    "\"");
            }
        }

        report.AppendLine();
    }

    private static string TryReadTextValue(
        Component component)
    {
        try
        {
            PropertyInfo textProperty =
                component.GetType().GetProperty(
                    "text",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            if (
                textProperty != null &&
                textProperty.PropertyType ==
                    typeof(string)
            )
            {
                return
                    textProperty.GetValue(
                        component,
                        null) as string;
            }
        }
        catch
        {
        }

        return null;
    }

    private static void AppendCameraOverview(
        StringBuilder report,
        List<Component> components)
    {
        AppendHeader(
            report,
            "CAMERAS");

        List<Camera> cameras =
            components
                .OfType<Camera>()
                .OrderBy(
                    camera =>
                        GetHierarchyPath(
                            camera.transform))
                .ToList();

        foreach (Camera camera in cameras)
        {
            report.AppendLine(
                GetHierarchyPath(
                    camera.transform));

            report.AppendLine(
                "  tag=" +
                camera.tag +
                " | enabled=" +
                camera.enabled +
                " | active=" +
                camera.gameObject.activeInHierarchy);

            report.AppendLine(
                "  position=" +
                camera.transform.position +
                " | rotation=" +
                camera.transform.rotation.eulerAngles);

            report.AppendLine(
                "  near=" +
                camera.nearClipPlane.ToString(
                    "F3",
                    CultureInfo.InvariantCulture) +
                " | far=" +
                camera.farClipPlane.ToString(
                    "F3",
                    CultureInfo.InvariantCulture));
        }

        report.AppendLine();
    }

    private static void AppendPotentialGameplayManagers(
        StringBuilder report,
        List<Component> components)
    {
        AppendHeader(
            report,
            "POTENTIAL GAMEPLAY MANAGERS");

        foreach (
            Component component in components
                .Where(
                    item =>
                        ContainsAny(
                            item.GetType().Name,
                            "Manager",
                            "Controller",
                            "Logger",
                            "Bridge",
                            "Tracker"))
                .OrderBy(
                    item =>
                        GetHierarchyPath(
                            item.transform))
                .ThenBy(
                    item =>
                        item.GetType().FullName)
        )
        {
            report.AppendLine(
                GetHierarchyPath(
                    component.transform) +
                " | " +
                component.GetType().FullName +
                " | active=" +
                component.gameObject.activeInHierarchy);
        }

        report.AppendLine();
    }

    private static void AppendSourceFiles(
        StringBuilder report)
    {
        AppendHeader(
            report,
            "RELEVANT SOURCE FILES");

        string assetsAbsolutePath =
            Path.GetFullPath("Assets");

        string[] sourceFiles =
            Directory.GetFiles(
                assetsAbsolutePath,
                "*.cs",
                SearchOption.AllDirectories);

        List<string> selectedFiles =
            new List<string>();

        foreach (string sourceFile in sourceFiles)
        {
            string content;

            try
            {
                content =
                    File.ReadAllText(
                        sourceFile);
            }
            catch
            {
                continue;
            }

            if (
                SourceMarkers.Any(
                    marker =>
                        content.IndexOf(
                            marker,
                            StringComparison.Ordinal) >= 0)
            )
            {
                selectedFiles.Add(
                    sourceFile);
            }
        }

        selectedFiles =
            selectedFiles
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    path => path)
                .ToList();

        report.AppendLine(
            "Selected source file count: " +
            selectedFiles.Count);

        report.AppendLine();

        foreach (string sourceFile in selectedFiles)
        {
            string relativePath =
                MakeProjectRelativePath(
                    sourceFile);

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "FILE: " +
                relativePath);

            report.AppendLine(
                "============================================================");

            string[] lines =
                File.ReadAllLines(
                    sourceFile);

            for (
                int index = 0;
                index < lines.Length;
                index++
            )
            {
                report.AppendLine(
                    (index + 1)
                        .ToString(
                            "D4",
                            CultureInfo.InvariantCulture) +
                    " | " +
                    lines[index]);
            }

            report.AppendLine();
        }
    }

    private static void AppendRecommendedIntegrationPoints(
        StringBuilder report)
    {
        AppendHeader(
            report,
            "AUDIT QUESTIONS TO RESOLVE BEFORE IMPLEMENTATION");

        report.AppendLine(
            "1. Which scene object owns WildlifeScoreManager?");

        report.AppendLine(
            "2. Which public methods/properties can reset, read, enable, or disable scoring?");

        report.AppendLine(
            "3. Where is the current score HUD created and how is it updated?");

        report.AppendLine(
            "4. Which exact event marks an arrow as launched?");

        report.AppendLine(
            "5. Which exact code path distinguishes wildlife hit from environment miss?");

        report.AppendLine(
            "6. Which bow/string components can be safely disabled during menus/countdown/results?");

        report.AppendLine(
            "7. Which EventSystem and XR UI raycasters already exist?");

        report.AppendLine(
            "8. Can one world-space menu support both controller and hand UI interaction?");

        report.AppendLine(
            "9. Which wildlife manager/reset methods can prepare a fresh round?");

        report.AppendLine(
            "10. Which existing experiment/logger types can be reused without coupling leaderboard data to study data?");

        report.AppendLine();
    }

    private static bool ContainsAny(
        string source,
        params string[] values)
    {
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        foreach (string value in values)
        {
            if (
                source.IndexOf(
                    value,
                    StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                return true;
            }
        }

        return false;
    }

    private static string GetHierarchyPath(
        Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        Stack<string> names =
            new Stack<string>();

        Transform current =
            transform;

        while (current != null)
        {
            names.Push(
                current.name);

            current =
                current.parent;
        }

        return string.Join(
            "/",
            names.ToArray());
    }

    private static string MakeProjectRelativePath(
        string absolutePath)
    {
        string normalizedProjectPath =
            Path.GetFullPath(".")
                .Replace('\\', '/')
                .TrimEnd('/');

        string normalizedAbsolutePath =
            Path.GetFullPath(
                absolutePath)
                .Replace('\\', '/');

        if (
            normalizedAbsolutePath.StartsWith(
                normalizedProjectPath + "/",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return normalizedAbsolutePath.Substring(
                normalizedProjectPath.Length + 1);
        }

        return normalizedAbsolutePath;
    }

    private static void AppendHeader(
        StringBuilder report,
        string title)
    {
        report.AppendLine(
            new string('=', 88));

        report.AppendLine(
            title);

        report.AppendLine(
            new string('=', 88));
    }
}
