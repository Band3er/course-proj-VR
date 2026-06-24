using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VRRequirementsAudit
{
    private const string ReportsFolder =
        "Assets/_Project/ComplianceAudit/Reports";

    [MenuItem(
        "Tools/Forest Archery/Compliance/Stage 06a - Generate Full Requirements Audit")]
    public static void GenerateAudit()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Requirements Audit",
                "Exit Play Mode before generating the audit.",
                "OK");
            return;
        }

        try
        {
            EnsureFolder(
                "Assets/_Project",
                "ComplianceAudit");

            EnsureFolder(
                "Assets/_Project/ComplianceAudit",
                "Reports");

            string timestamp =
                DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string reportAssetPath =
                ReportsFolder +
                "/VR_Requirements_Audit_" +
                timestamp +
                ".txt";

            string reportAbsolutePath =
                Path.GetFullPath(
                    reportAssetPath);

            StringBuilder report =
                new StringBuilder(1024 * 256);

            Scene scene =
                SceneManager.GetActiveScene();

            AppendHeader(
                report,
                "FOREST ARCHERY - FULL REQUIREMENTS AUDIT");

            report.AppendLine(
                "Generated: " +
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            report.AppendLine(
                "Unity version: " +
                Application.unityVersion);

            report.AppendLine(
                "Active scene: " +
                scene.path);

            report.AppendLine(
                "Scene saved: " +
                (!scene.isDirty));

            report.AppendLine();

            List<GameObject> sceneObjects =
                GetAllSceneObjects(scene);

            List<Component> sceneComponents =
                GetAllComponents(
                    sceneObjects);

            Camera mainCamera =
                Camera.main != null
                    ? Camera.main
                    : sceneComponents
                        .OfType<Camera>()
                        .FirstOrDefault();

            AppendSceneSummary(
                report,
                scene,
                sceneObjects,
                sceneComponents,
                mainCamera);

            AppendTargetDistanceAudit(
                report,
                sceneObjects,
                mainCamera);

            AppendComponentCategory(
                report,
                "BOW / ARROW / STRING COMPONENTS",
                sceneComponents,
                new[]
                {
                    "bow",
                    "arrow",
                    "string",
                    "nock",
                    "trajectory",
                    "aimassist"
                });

            AppendComponentCategory(
                report,
                "LOCOMOTION COMPONENTS",
                sceneComponents,
                new[]
                {
                    "locomotion",
                    "continuousmove",
                    "continuous move",
                    "moveprovider",
                    "teleport",
                    "turnprovider",
                    "charactercontroller",
                    "xrorigin",
                    "xr origin",
                    "ovrplayercontroller"
                });

            AppendComponentCategory(
                report,
                "HAND TRACKING / PINCH / GRAB COMPONENTS",
                sceneComponents,
                new[]
                {
                    "handtracking",
                    "hand tracking",
                    "ovrhand",
                    "handgrab",
                    "hand grab",
                    "pinch",
                    "grabinteractor",
                    "grab interact",
                    "synthetichand",
                    "synthetic hand",
                    "handvisual",
                    "hand visual"
                });

            AppendComponentCategory(
                report,
                "EXPERIMENT / LOGGING / UI COMPONENTS",
                sceneComponents,
                new[]
                {
                    "experiment",
                    "logger",
                    "logging",
                    "participant",
                    "condition",
                    "trial",
                    "archeryui",
                    "score",
                    "feedback",
                    "hud"
                });

            AppendRelevantSceneObjects(
                report,
                sceneObjects);

            AppendRelevantPrefabAudit(
                report);

            AppendSourceAudit(
                report);

            AppendProjectConfiguration(
                report);

            AppendInterpretationChecklist(
                report);

            File.WriteAllText(
                reportAbsolutePath,
                report.ToString(),
                new UTF8Encoding(false));

            AssetDatabase.ImportAsset(
                reportAssetPath,
                ImportAssetOptions.ForceUpdate);

            TextAsset reportAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    reportAssetPath);

            Selection.activeObject =
                reportAsset;

            if (reportAsset != null)
            {
                EditorGUIUtility.PingObject(
                    reportAsset);
            }

            Debug.Log(
                "[COMPLIANCE AUDIT] Full VR requirements audit generated" +
                " | report=" +
                reportAssetPath);

            EditorUtility.RevealInFinder(
                reportAbsolutePath);

            EditorUtility.DisplayDialog(
                "Requirements Audit Generated",
                "The report was generated successfully.\n\n" +
                reportAssetPath +
                "\n\nUpload this TXT file in the chat.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Requirements Audit Failed",
                exception.Message,
                "OK");
        }
    }

    private static List<GameObject> GetAllSceneObjects(
        Scene scene)
    {
        List<GameObject> result =
            new List<GameObject>();

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
                if (transform != null)
                {
                    result.Add(
                        transform.gameObject);
                }
            }
        }

        return result
            .Distinct()
            .OrderBy(
                item =>
                    GetHierarchyPath(
                        item.transform))
            .ToList();
    }

    private static List<Component> GetAllComponents(
        List<GameObject> sceneObjects)
    {
        List<Component> result =
            new List<Component>();

        foreach (GameObject sceneObject in sceneObjects)
        {
            Component[] components =
                sceneObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component != null)
                {
                    result.Add(component);
                }
            }
        }

        return result;
    }

    private static void AppendSceneSummary(
        StringBuilder report,
        Scene scene,
        List<GameObject> sceneObjects,
        List<Component> sceneComponents,
        Camera mainCamera)
    {
        AppendHeader(
            report,
            "SCENE SUMMARY");

        report.AppendLine(
            "Root objects: " +
            scene.GetRootGameObjects().Length);

        report.AppendLine(
            "Total GameObjects: " +
            sceneObjects.Count);

        report.AppendLine(
            "Total Components: " +
            sceneComponents.Count);

        report.AppendLine(
            "Main camera: " +
            (
                mainCamera != null
                    ? GetHierarchyPath(
                        mainCamera.transform)
                    : "<not found>"
            ));

        if (mainCamera != null)
        {
            report.AppendLine(
                "Main camera position: " +
                mainCamera.transform.position);

            report.AppendLine(
                "Main camera tag: " +
                mainCamera.tag);
        }

        report.AppendLine();
    }

    private static void AppendTargetDistanceAudit(
        StringBuilder report,
        List<GameObject> sceneObjects,
        Camera mainCamera)
    {
        AppendHeader(
            report,
            "TARGET DISTANCE AUDIT");

        if (mainCamera == null)
        {
            report.AppendLine(
                "No camera found; distances could not be calculated.");

            report.AppendLine();
            return;
        }

        Vector3 origin =
            mainCamera.transform.position;

        List<GameObject> candidates =
            sceneObjects
                .Where(
                    item =>
                        IsTargetCandidate(item))
                .OrderBy(
                    item =>
                        Vector3.Distance(
                            origin,
                            item.transform.position))
                .ToList();

        report.AppendLine(
            "Distance origin: Main Camera position " +
            origin);

        report.AppendLine(
            "Target candidates found: " +
            candidates.Count);

        report.AppendLine();

        if (candidates.Count == 0)
        {
            report.AppendLine(
                "<no target candidate found by name, tag or component>");

            report.AppendLine();
            return;
        }

        foreach (GameObject target in candidates)
        {
            Vector3 delta =
                target.transform.position -
                origin;

            float distance3D =
                delta.magnitude;

            float horizontalDistance =
                new Vector2(
                    delta.x,
                    delta.z).magnitude;

            Collider collider =
                target.GetComponent<Collider>();

            Renderer renderer =
                target.GetComponent<Renderer>();

            report.AppendLine(
                "TARGET: " +
                GetHierarchyPath(
                    target.transform));

            report.AppendLine(
                "  Position: " +
                target.transform.position);

            report.AppendLine(
                "  3D distance: " +
                distance3D.ToString("F3") +
                " m");

            report.AppendLine(
                "  Horizontal distance: " +
                horizontalDistance.ToString("F3") +
                " m");

            report.AppendLine(
                "  Meets >= 10 m (3D): " +
                (distance3D >= 10f));

            report.AppendLine(
                "  Meets >= 10 m (horizontal): " +
                (horizontalDistance >= 10f));

            report.AppendLine(
                "  Tag: " +
                target.tag);

            report.AppendLine(
                "  Layer: " +
                LayerMask.LayerToName(
                    target.layer));

            report.AppendLine(
                "  Collider: " +
                (
                    collider != null
                        ? collider.GetType().FullName
                        : "<none on this object>"
                ));

            report.AppendLine(
                "  Renderer: " +
                (
                    renderer != null
                        ? renderer.GetType().FullName
                        : "<none on this object>"
                ));

            report.AppendLine();
        }
    }

    private static bool IsTargetCandidate(
        GameObject item)
    {
        string name =
            item.name.ToLowerInvariant();

        string tag =
            item.tag.ToLowerInvariant();

        if (
            name.Contains("target") ||
            name.Contains("bullseye") ||
            name.Contains("bullseye") ||
            name.Contains("archery board") ||
            name.Contains("score zone") ||
            tag.Contains("target")
        )
        {
            return true;
        }

        Component[] components =
            item.GetComponents<Component>();

        return components.Any(
            component =>
                component != null &&
                ContainsAny(
                    component.GetType().FullName,
                    new[]
                    {
                        "target",
                        "bullseye",
                        "scorezone",
                        "scoringzone"
                    }));
    }

    private static void AppendComponentCategory(
        StringBuilder report,
        string title,
        List<Component> components,
        string[] keywords)
    {
        AppendHeader(
            report,
            title);

        List<Component> matches =
            components
                .Where(
                    component =>
                        ComponentMatches(
                            component,
                            keywords))
                .OrderBy(
                    component =>
                        GetHierarchyPath(
                            component.transform))
                .ThenBy(
                    component =>
                        component.GetType().FullName)
                .ToList();

        report.AppendLine(
            "Matching components: " +
            matches.Count);

        report.AppendLine();

        if (matches.Count == 0)
        {
            report.AppendLine(
                "<none>");

            report.AppendLine();
            return;
        }

        foreach (Component component in matches)
        {
            report.AppendLine(
                "OBJECT: " +
                GetHierarchyPath(
                    component.transform));

            report.AppendLine(
                "COMPONENT: " +
                component.GetType().FullName);

            MonoBehaviour behaviour =
                component as MonoBehaviour;

            if (behaviour != null)
            {
                MonoScript script =
                    MonoScript.FromMonoBehaviour(
                        behaviour);

                report.AppendLine(
                    "SCRIPT: " +
                    (
                        script != null
                            ? AssetDatabase.GetAssetPath(
                                script)
                            : "<unknown>"
                    ));

                AppendSerializedProperties(
                    report,
                    behaviour,
                    40,
                    "  ");
            }

            report.AppendLine();
        }
    }

    private static bool ComponentMatches(
        Component component,
        string[] keywords)
    {
        if (component == null)
        {
            return false;
        }

        string typeName =
            component.GetType().FullName ?? string.Empty;

        string objectName =
            component.gameObject.name;

        if (
            ContainsAny(
                typeName,
                keywords) ||
            ContainsAny(
                objectName,
                keywords)
        )
        {
            return true;
        }

        MonoBehaviour behaviour =
            component as MonoBehaviour;

        if (behaviour == null)
        {
            return false;
        }

        MonoScript script =
            MonoScript.FromMonoBehaviour(
                behaviour);

        string scriptPath =
            script != null
                ? AssetDatabase.GetAssetPath(
                    script)
                : string.Empty;

        return ContainsAny(
            scriptPath,
            keywords);
    }

    private static void AppendRelevantSceneObjects(
        StringBuilder report,
        List<GameObject> sceneObjects)
    {
        AppendHeader(
            report,
            "RELEVANT SCENE OBJECT HIERARCHY");

        string[] keywords =
        {
            "bow",
            "arrow",
            "string",
            "target",
            "bullseye",
            "xr",
            "camera",
            "hand",
            "controller",
            "experiment",
            "logger",
            "ui",
            "hud",
            "score",
            "locomotion",
            "teleport",
            "wildlife"
        };

        List<GameObject> matches =
            sceneObjects
                .Where(
                    item =>
                        ContainsAny(
                            item.name,
                            keywords))
                .OrderBy(
                    item =>
                        GetHierarchyPath(
                            item.transform))
                .ToList();

        foreach (GameObject item in matches)
        {
            report.AppendLine(
                GetHierarchyPath(
                    item.transform));

            report.AppendLine(
                "  ActiveSelf: " +
                item.activeSelf);

            report.AppendLine(
                "  ActiveInHierarchy: " +
                item.activeInHierarchy);

            report.AppendLine(
                "  Tag: " +
                item.tag);

            report.AppendLine(
                "  Layer: " +
                LayerMask.LayerToName(
                    item.layer));

            Component[] components =
                item.GetComponents<Component>();

            report.AppendLine(
                "  Components: " +
                string.Join(
                    ", ",
                    components
                        .Where(
                            component =>
                                component != null)
                        .Select(
                            component =>
                                component.GetType().FullName)));

            report.AppendLine();
        }
    }

    private static void AppendRelevantPrefabAudit(
        StringBuilder report)
    {
        AppendHeader(
            report,
            "RELEVANT PREFAB AUDIT");

        string[] prefabGuids =
            AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets" });

        string[] keywords =
        {
            "bow",
            "arrow",
            "target",
            "bullseye",
            "hand",
            "controller",
            "experiment",
            "trajectory"
        };

        List<string> prefabPaths =
            prefabGuids
                .Select(
                    AssetDatabase.GUIDToAssetPath)
                .Where(
                    path =>
                        ContainsAny(
                            path,
                            keywords))
                .Distinct()
                .OrderBy(path => path)
                .Take(80)
                .ToList();

        report.AppendLine(
            "Relevant prefabs: " +
            prefabPaths.Count);

        report.AppendLine();

        foreach (string prefabPath in prefabPaths)
        {
            report.AppendLine(
                "PREFAB: " +
                prefabPath);

            GameObject root = null;

            try
            {
                root =
                    PrefabUtility.LoadPrefabContents(
                        prefabPath);

                if (root == null)
                {
                    report.AppendLine(
                        "  <could not load>");

                    report.AppendLine();
                    continue;
                }

                Component[] components =
                    root.GetComponentsInChildren<Component>(
                        true);

                foreach (
                    IGrouping<Type, Component> group in
                    components
                        .Where(
                            component =>
                                component != null)
                        .GroupBy(
                            component =>
                                component.GetType())
                        .OrderBy(
                            group =>
                                group.Key.FullName)
                )
                {
                    report.AppendLine(
                        "  " +
                        group.Key.FullName +
                        " = " +
                        group.Count());
                }
            }
            catch (Exception exception)
            {
                report.AppendLine(
                    "  ERROR: " +
                    exception.Message);
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(
                        root);
                }
            }

            report.AppendLine();
        }
    }

    private static void AppendSourceAudit(
        StringBuilder report)
    {
        AppendHeader(
            report,
            "RELEVANT C# SOURCE AUDIT");

        string projectAssetsPath =
            Application.dataPath;

        string[] allFiles =
            Directory.GetFiles(
                projectAssetsPath,
                "*.cs",
                SearchOption.AllDirectories);

        string[] keywords =
        {
            "arrowcontroller",
            "bowcontroller",
            "bow",
            "arrow",
            "string",
            "trajectory",
            "aimassist",
            "experimentmanager",
            "experimentlogger",
            "archeryui",
            "handtracking",
            "ovrhand",
            "pinch",
            "grab",
            "locomotion",
            "continuousmove",
            "teleport",
            "hit",
            "miss",
            "feedback",
            "target"
        };

        List<string> relevantFiles =
            new List<string>();

        foreach (string absolutePath in allFiles)
        {
            string normalized =
                absolutePath.Replace('\\', '/');

            if (
                normalized.Contains(
                    "/ComplianceAudit/",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            string relativePath =
                "Assets" +
                normalized.Substring(
                    projectAssetsPath
                        .Replace('\\', '/')
                        .Length);

            string fileName =
                Path.GetFileNameWithoutExtension(
                    absolutePath);

            string content =
                File.ReadAllText(
                    absolutePath);

            if (
                ContainsAny(
                    fileName,
                    keywords) ||
                ContainsAny(
                    relativePath,
                    keywords) ||
                ContainsAny(
                    content,
                    keywords)
            )
            {
                relevantFiles.Add(
                    relativePath);
            }
        }

        relevantFiles =
            relevantFiles
                .Distinct()
                .OrderBy(path => path)
                .Take(100)
                .ToList();

        report.AppendLine(
            "Relevant C# files: " +
            relevantFiles.Count);

        report.AppendLine();

        Regex methodRegex =
            new Regex(
                @"\b(?:public|private|protected|internal)\s+" +
                @"(?:static\s+)?(?:[\w<>\[\],.?]+\s+)+" +
                @"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
                RegexOptions.Compiled);

        string[] importantTerms =
        {
            "OnCollisionEnter",
            "OnTriggerEnter",
            "Stick",
            "Attach",
            "Parent",
            "isKinematic",
            "useGravity",
            "linearVelocity",
            "AddForce",
            "Hit",
            "Miss",
            "Feedback",
            "Score",
            "Pinch",
            "Hand",
            "Controller",
            "Condition",
            "Trial",
            "CSV",
            "Application.persistentDataPath"
        };

        foreach (string relativePath in relevantFiles)
        {
            string absolutePath =
                Path.GetFullPath(
                    relativePath);

            if (!File.Exists(absolutePath))
            {
                continue;
            }

            string[] lines =
                File.ReadAllLines(
                    absolutePath);

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "FILE: " +
                relativePath);

            report.AppendLine(
                "============================================================");

            List<string> methods =
                new List<string>();

            List<string> importantLines =
                new List<string>();

            for (
                int index = 0;
                index < lines.Length;
                index++
            )
            {
                string line =
                    lines[index];

                Match match =
                    methodRegex.Match(line);

                if (match.Success)
                {
                    methods.Add(
                        "L" +
                        (index + 1) +
                        ": " +
                        line.Trim());
                }

                if (
                    ContainsAny(
                        line,
                        importantTerms)
                )
                {
                    importantLines.Add(
                        "L" +
                        (index + 1) +
                        ": " +
                        line.Trim());
                }
            }

            report.AppendLine(
                "METHOD SIGNATURES:");

            if (methods.Count == 0)
            {
                report.AppendLine(
                    "  <none detected>");
            }
            else
            {
                foreach (string method in methods)
                {
                    report.AppendLine(
                        "  " +
                        method);
                }
            }

            report.AppendLine(
                "IMPORTANT LINES:");

            if (importantLines.Count == 0)
            {
                report.AppendLine(
                    "  <none detected>");
            }
            else
            {
                foreach (
                    string importantLine in
                    importantLines.Take(160)
                )
                {
                    report.AppendLine(
                        "  " +
                        importantLine);
                }
            }

            report.AppendLine();
            report.AppendLine(
                "FULL SOURCE:");

            report.AppendLine(
                File.ReadAllText(
                    absolutePath));

            report.AppendLine();
        }
    }

    private static void AppendProjectConfiguration(
        StringBuilder report)
    {
        AppendHeader(
            report,
            "PROJECT CONFIGURATION");

        string manifestPath =
            Path.GetFullPath(
                "Packages/manifest.json");

        string lockPath =
            Path.GetFullPath(
                "Packages/packages-lock.json");

        string tagManagerPath =
            Path.GetFullPath(
                "ProjectSettings/TagManager.asset");

        string projectSettingsPath =
            Path.GetFullPath(
                "ProjectSettings/ProjectSettings.asset");

        AppendFileIfExists(
            report,
            "Packages/manifest.json",
            manifestPath);

        AppendFileIfExists(
            report,
            "Packages/packages-lock.json",
            lockPath);

        AppendFileIfExists(
            report,
            "ProjectSettings/TagManager.asset",
            tagManagerPath);

        AppendFileIfExists(
            report,
            "ProjectSettings/ProjectSettings.asset",
            projectSettingsPath);
    }

    private static void AppendInterpretationChecklist(
        StringBuilder report)
    {
        AppendHeader(
            report,
            "STRICT REQUIREMENTS CHECKLIST TO VERIFY");

        report.AppendLine(
            "[ ] Controller bow grab is stable.");

        report.AppendLine(
            "[ ] Controller string grab, draw and release are stable.");

        report.AppendLine(
            "[ ] Arrow launch force depends on draw.");

        report.AppendLine(
            "[ ] Arrow uses Rigidbody and gravity.");

        report.AppendLine(
            "[ ] Arrow orientation follows flight.");

        report.AppendLine(
            "[ ] Arrow remains fixed in target, ground and static objects.");

        report.AppendLine(
            "[ ] Arrow recovery or equivalent reset exists.");

        report.AppendLine(
            "[ ] User can move from shooting position to target.");

        report.AppendLine(
            "[ ] Official target distance is >= 10 Unity meters.");

        report.AppendLine(
            "[ ] Success feedback is explicit.");

        report.AppendLine(
            "[ ] Failure feedback is explicit and different.");

        report.AppendLine(
            "[ ] Hand Tracking mode supports bow grab.");

        report.AppendLine(
            "[ ] Hand Tracking mode supports string pinch/grab.");

        report.AppendLine(
            "[ ] Hand Tracking mode supports draw and release.");

        report.AppendLine(
            "[ ] Controller and Hand Tracking modes are clearly separated.");

        report.AppendLine(
            "[ ] Experimental condition is logged.");

        report.AppendLine(
            "[ ] Objective and subjective metrics are planned.");

        report.AppendLine();
    }

    private static void AppendSerializedProperties(
        StringBuilder report,
        MonoBehaviour behaviour,
        int maxProperties,
        string indent)
    {
        try
        {
            SerializedObject serialized =
                new SerializedObject(
                    behaviour);

            SerializedProperty iterator =
                serialized.GetIterator();

            bool enterChildren =
                true;

            int propertyCount =
                0;

            while (
                iterator.NextVisible(
                    enterChildren)
            )
            {
                enterChildren =
                    false;

                if (
                    iterator.name ==
                    "m_Script"
                )
                {
                    continue;
                }

                report.AppendLine(
                    indent +
                    iterator.propertyPath +
                    " | " +
                    iterator.propertyType +
                    " | " +
                    GetSerializedValueSummary(
                        iterator));

                propertyCount++;

                if (
                    propertyCount >=
                    maxProperties
                )
                {
                    report.AppendLine(
                        indent +
                        "<property output truncated>");

                    break;
                }
            }

            if (propertyCount == 0)
            {
                report.AppendLine(
                    indent +
                    "<no serialized properties>");
            }
        }
        catch (Exception exception)
        {
            report.AppendLine(
                indent +
                "<serialized property error: " +
                exception.Message +
                ">");
        }
    }

    private static string GetSerializedValueSummary(
        SerializedProperty property)
    {
        try
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();

                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();

                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("F4");

                case SerializedPropertyType.String:
                    return "\"" +
                        property.stringValue +
                        "\"";

                case SerializedPropertyType.ObjectReference:
                    return
                        property.objectReferenceValue != null
                            ? property.objectReferenceValue.name +
                                " | " +
                                AssetDatabase.GetAssetPath(
                                    property.objectReferenceValue)
                            : "<null>";

                case SerializedPropertyType.Enum:
                    return
                        property.enumDisplayNames != null &&
                        property.enumValueIndex >= 0 &&
                        property.enumValueIndex <
                            property.enumDisplayNames.Length
                            ? property.enumDisplayNames[
                                property.enumValueIndex]
                            : property.enumValueIndex.ToString();

                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();

                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();

                case SerializedPropertyType.Color:
                    return property.colorValue.ToString();

                case SerializedPropertyType.LayerMask:
                    return property.intValue.ToString();

                default:
                    return property.propertyType.ToString();
            }
        }
        catch
        {
            return "<unavailable>";
        }
    }

    private static bool ContainsAny(
        string value,
        IEnumerable<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (string keyword in keywords)
        {
            if (
                value.IndexOf(
                    keyword,
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

        List<string> names =
            new List<string>();

        Transform current =
            transform;

        while (current != null)
        {
            names.Add(
                current.name);

            current =
                current.parent;
        }

        names.Reverse();

        return string.Join(
            "/",
            names);
    }

    private static void AppendFileIfExists(
        StringBuilder report,
        string label,
        string absolutePath)
    {
        report.AppendLine(
            "============================================================");

        report.AppendLine(
            "FILE: " +
            label);

        report.AppendLine(
            "============================================================");

        if (File.Exists(absolutePath))
        {
            report.AppendLine(
                File.ReadAllText(
                    absolutePath));
        }
        else
        {
            report.AppendLine(
                "<not found>");
        }

        report.AppendLine();
    }

    private static void AppendHeader(
        StringBuilder report,
        string title)
    {
        report.AppendLine(
            "============================================================");

        report.AppendLine(
            title);

        report.AppendLine(
            "============================================================");

        report.AppendLine();
    }

    private static void EnsureFolder(
        string parent,
        string name)
    {
        string path =
            parent +
            "/" +
            name;

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(
                parent,
                name);
        }
    }
}
