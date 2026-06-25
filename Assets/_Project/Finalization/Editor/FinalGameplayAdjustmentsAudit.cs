#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalGameplayAdjustmentsAudit
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private const string ReportPath =
        "C:/Universidad_de_Zaragoza/Virtual Reality/PROJECT/APP/TOOLS/FinalGameplayAudit_Stage10g0.txt";

    [MenuItem(
        "Tools/Forest Archery/Diagnostics/Stage 10g0 - Final Gameplay Read-Only Audit")]
    public static void RunAudit()
    {
        try
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            StringBuilder report =
                new StringBuilder();

            WriteHeader(
                report,
                "FOREST ARCHERY - FINAL GAMEPLAY READ-ONLY AUDIT");

            report.AppendLine(
                "Generated: " +
                DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss"));

            report.AppendLine(
                "Unity version: " +
                Application.unityVersion);

            report.AppendLine(
                "Scene: " +
                scene.path);

            report.AppendLine(
                "Scene saved: " +
                (!scene.isDirty));

            AuditSceneRoots(
                report,
                scene);

            AuditEnvironmentRoot(
                report);

            AuditBowAndCamera(
                report);

            AuditRabbitHitboxes(
                report);

            AuditLocomotion(
                report);

            AuditRelevantSourceFiles(
                report);

            string directory =
                Path.GetDirectoryName(
                    ReportPath);

            if (
                !string.IsNullOrWhiteSpace(
                    directory)
            )
            {
                Directory.CreateDirectory(
                    directory);
            }

            File.WriteAllText(
                ReportPath,
                report.ToString(),
                new UTF8Encoding(false));

            Debug.Log(
                "[STAGE 10G0] Read-only audit complete:\n" +
                ReportPath);

            EditorUtility.DisplayDialog(
                "Final Gameplay Audit Complete",
                "The project was not modified.\n\n" +
                "Report created at:\n" +
                ReportPath,
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 10g0 Audit Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    private static void AuditSceneRoots(
        StringBuilder report,
        Scene scene)
    {
        WriteHeader(
            report,
            "SCENE ROOTS");

        GameObject[] roots =
            scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            int descendants =
                root.GetComponentsInChildren<Transform>(
                    true).Length;

            int components =
                root.GetComponentsInChildren<Component>(
                    true).Length;

            report.AppendLine(
                root.name +
                " | active=" +
                root.activeSelf +
                " | descendants=" +
                descendants +
                " | components=" +
                components);
        }
    }

    private static void AuditEnvironmentRoot(
        StringBuilder report)
    {
        WriteHeader(
            report,
            "ENVIRONMENT ROOT");

        GameObject environmentRoot =
            FindSceneObjectExact(
                "EnvironmentRoot");

        if (environmentRoot == null)
        {
            report.AppendLine(
                "EnvironmentRoot: NOT FOUND");

            return;
        }

        report.AppendLine(
            "Path: " +
            GetHierarchyPath(
                environmentRoot.transform));

        report.AppendLine(
            "ActiveSelf: " +
            environmentRoot.activeSelf);

        report.AppendLine(
            "Position: " +
            FormatVector(
                environmentRoot.transform.position));

        report.AppendLine(
            "Direct children: " +
            environmentRoot.transform.childCount);

        report.AppendLine();

        report.AppendLine(
            "DIRECT CHILD SUMMARY:");

        for (
            int index = 0;
            index < environmentRoot.transform.childCount;
            index++)
        {
            Transform child =
                environmentRoot.transform.GetChild(
                    index);

            Renderer[] renderers =
                child.GetComponentsInChildren<Renderer>(
                    true);

            Collider[] colliders =
                child.GetComponentsInChildren<Collider>(
                    true);

            Transform[] transforms =
                child.GetComponentsInChildren<Transform>(
                    true);

            report.AppendLine(
                "- " +
                child.name +
                " | active=" +
                child.gameObject.activeSelf +
                " | descendants=" +
                transforms.Length +
                " | renderers=" +
                renderers.Length +
                " | colliders=" +
                colliders.Length);
        }

        report.AppendLine();
        report.AppendLine(
            "FULL ENVIRONMENTROOT HIERARCHY:");

        Transform[] allTransforms =
            environmentRoot.GetComponentsInChildren<Transform>(
                true);

        foreach (Transform item in allTransforms)
        {
            string indent =
                new string(
                    ' ',
                    Mathf.Max(
                        0,
                        GetDepthFrom(
                            item,
                            environmentRoot.transform)) *
                        2);

            string componentNames =
                string.Join(
                    ", ",
                    item.GetComponents<Component>()
                        .Where(
                            component =>
                                component != null)
                        .Select(
                            component =>
                                component.GetType().FullName));

            report.AppendLine(
                indent +
                GetHierarchyPath(item) +
                " | active=" +
                item.gameObject.activeSelf +
                " | components=[" +
                componentNames +
                "]");
        }
    }

    private static void AuditBowAndCamera(
        StringBuilder report)
    {
        WriteHeader(
            report,
            "BOW AND CAMERA");

        Camera mainCamera =
            Camera.main;

        if (mainCamera == null)
        {
            mainCamera =
                UnityEngine.Object
                    .FindFirstObjectByType<Camera>();
        }

        if (mainCamera == null)
        {
            report.AppendLine(
                "Main camera: NOT FOUND");
        }
        else
        {
            report.AppendLine(
                "Camera path: " +
                GetHierarchyPath(
                    mainCamera.transform));

            report.AppendLine(
                "Camera position: " +
                FormatVector(
                    mainCamera.transform.position));

            report.AppendLine(
                "Camera forward: " +
                FormatVector(
                    mainCamera.transform.forward));
        }

        Component bowController =
            FindComponentByTypeName(
                "BowDrawController");

        if (bowController == null)
        {
            report.AppendLine(
                "BowDrawController: NOT FOUND");

            return;
        }

        GameObject bow =
            bowController.gameObject;

        report.AppendLine();
        report.AppendLine(
            "Bow path: " +
            GetHierarchyPath(
                bow.transform));

        report.AppendLine(
            "Bow parent: " +
            (
                bow.transform.parent != null
                    ? GetHierarchyPath(
                        bow.transform.parent)
                    : "<root>"
            ));

        report.AppendLine(
            "Bow world position: " +
            FormatVector(
                bow.transform.position));

        report.AppendLine(
            "Bow local position: " +
            FormatVector(
                bow.transform.localPosition));

        report.AppendLine(
            "Bow rotation: " +
            FormatVector(
                bow.transform.eulerAngles));

        Rigidbody rigidbody =
            bow.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            report.AppendLine(
                "Rigidbody: kinematic=" +
                rigidbody.isKinematic +
                " gravity=" +
                rigidbody.useGravity +
                " constraints=" +
                rigidbody.constraints +
                " collision=" +
                rigidbody.collisionDetectionMode);
        }

        report.AppendLine();
        report.AppendLine(
            "Components on Bow:");

        foreach (Component component in bow.GetComponents<Component>())
        {
            if (component == null)
            {
                continue;
            }

            report.AppendLine(
                "- " +
                component.GetType().FullName +
                " | enabled=" +
                GetEnabledState(component));
        }

        report.AppendLine();
        report.AppendLine(
            "Possible startup/reset/position components:");

        foreach (
            MonoBehaviour behaviour in
            bow.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null)
            {
                continue;
            }

            string name =
                behaviour.GetType().FullName;

            if (
                name.IndexOf(
                    "spawn",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(
                    "reset",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(
                    "position",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(
                    "upright",
                    StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                report.AppendLine(
                    "- " +
                    name);
            }
        }

        if (mainCamera != null)
        {
            Vector3 desired =
                mainCamera.transform.position +
                mainCamera.transform.forward *
                0.5f;

            report.AppendLine();
            report.AppendLine(
                "Desired eye-level spawn at audit time: " +
                FormatVector(
                    desired));

            report.AppendLine(
                "Current bow distance from camera: " +
                Vector3.Distance(
                    bow.transform.position,
                    mainCamera.transform.position)
                    .ToString("F3") +
                " m");
        }
    }

    private static void AuditRabbitHitboxes(
        StringBuilder report)
    {
        WriteHeader(
            report,
            "RABBIT HITBOXES");

        string[] prefabGuids =
            AssetDatabase.FindAssets(
                "rabbit t:Prefab");

        if (prefabGuids.Length == 0)
        {
            report.AppendLine(
                "No rabbit prefabs found.");
        }

        foreach (string guid in prefabGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            if (prefab == null)
            {
                continue;
            }

            report.AppendLine();
            report.AppendLine(
                "PREFAB: " +
                path);

            report.AppendLine(
                "Root name: " +
                prefab.name);

            report.AppendLine(
                "Root scale: " +
                FormatVector(
                    prefab.transform.localScale));

            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(
                    true);

            if (renderers.Length > 0)
            {
                Bounds rendererBounds =
                    renderers[0].bounds;

                for (
                    int index = 1;
                    index < renderers.Length;
                    index++)
                {
                    rendererBounds.Encapsulate(
                        renderers[index].bounds);
                }

                report.AppendLine(
                    "Combined renderer bounds size: " +
                    FormatVector(
                        rendererBounds.size));
            }

            Collider[] colliders =
                prefab.GetComponentsInChildren<Collider>(
                    true);

            report.AppendLine(
                "Collider count: " +
                colliders.Length);

            foreach (Collider collider in colliders)
            {
                report.AppendLine(
                    "- " +
                    GetHierarchyPathRelative(
                        collider.transform,
                        prefab.transform) +
                    " | " +
                    DescribeCollider(
                        collider));
            }

            MonoBehaviour[] behaviours =
                prefab.GetComponentsInChildren<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                string typeName =
                    behaviour.GetType().FullName;

                if (
                    typeName.IndexOf(
                        "hitbox",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf(
                        "hitzone",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf(
                        "wildlife",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    report.AppendLine(
                        "  Behaviour: " +
                        GetHierarchyPathRelative(
                            behaviour.transform,
                            prefab.transform) +
                        " | " +
                        typeName);
                }
            }
        }

        report.AppendLine();
        report.AppendLine(
            "RABBIT OBJECTS CURRENTLY IN SCENE:");

        Transform[] sceneTransforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform item in sceneTransforms)
        {
            if (
                item == null ||
                !item.gameObject.scene.IsValid() ||
                !item.gameObject.scene.isLoaded ||
                item.name.IndexOf(
                    "rabbit",
                    StringComparison.OrdinalIgnoreCase) < 0
            )
            {
                continue;
            }

            Collider[] colliders =
                item.GetComponentsInChildren<Collider>(
                    true);

            report.AppendLine(
                GetHierarchyPath(item) +
                " | active=" +
                item.gameObject.activeInHierarchy +
                " | colliders=" +
                colliders.Length);

            foreach (Collider collider in colliders)
            {
                report.AppendLine(
                    "  - " +
                    GetHierarchyPath(
                        collider.transform) +
                    " | " +
                    DescribeCollider(
                        collider));
            }
        }
    }

    private static void AuditLocomotion(
        StringBuilder report)
    {
        WriteHeader(
            report,
            "LOCOMOTION");

        Component[] allComponents =
            Resources.FindObjectsOfTypeAll<Component>();

        List<Component> matches =
            new List<Component>();

        foreach (Component component in allComponents)
        {
            if (
                component == null ||
                !component.gameObject.scene.IsValid() ||
                !component.gameObject.scene.isLoaded
            )
            {
                continue;
            }

            string fullName =
                component.GetType().FullName;

            if (
                fullName.IndexOf(
                    "locomotion",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf(
                    "moveprovider",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf(
                    "continuousmove",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf(
                    "charactercontroller",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf(
                    "xrorigin",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf(
                    "ovrplayercontroller",
                    StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                matches.Add(
                    component);
            }
        }

        report.AppendLine(
            "Matching components: " +
            matches.Count);

        foreach (
            Component component in
            matches.OrderBy(
                item =>
                    GetHierarchyPath(
                        item.transform)))
        {
            report.AppendLine();
            report.AppendLine(
                "OBJECT: " +
                GetHierarchyPath(
                    component.transform));

            report.AppendLine(
                "COMPONENT: " +
                component.GetType().FullName);

            report.AppendLine(
                "GameObject activeSelf=" +
                component.gameObject.activeSelf +
                " activeInHierarchy=" +
                component.gameObject.activeInHierarchy +
                " componentEnabled=" +
                GetEnabledState(
                    component));

            SerializedObject serialized =
                new SerializedObject(
                    component);

            SerializedProperty iterator =
                serialized.GetIterator();

            bool enterChildren =
                true;

            int propertyCount =
                0;

            while (
                iterator.NextVisible(
                    enterChildren) &&
                propertyCount < 80
            )
            {
                enterChildren =
                    false;

                if (
                    iterator.propertyPath ==
                    "m_Script"
                )
                {
                    continue;
                }

                if (
                    iterator.propertyType ==
                    SerializedPropertyType.ObjectReference ||
                    iterator.propertyType ==
                    SerializedPropertyType.Boolean ||
                    iterator.propertyType ==
                    SerializedPropertyType.Float ||
                    iterator.propertyType ==
                    SerializedPropertyType.Integer ||
                    iterator.propertyType ==
                    SerializedPropertyType.Enum ||
                    iterator.propertyType ==
                    SerializedPropertyType.String
                )
                {
                    report.AppendLine(
                        "  " +
                        iterator.propertyPath +
                        " | " +
                        iterator.propertyType +
                        " | " +
                        GetSerializedValue(
                            iterator));

                    propertyCount++;
                }
            }
        }

        WriteHeader(
            report,
            "CONTROLLER LOCOMOTION ROOT STATES");

        string[] requiredNames =
        {
            "LeftInteractions",
            "RightInteractions",
            "LocomotionControllerInteractorGroup",
            "ControllerLocomotionSlideActions",
            "ControllerSlideInteractor",
            "ControllerTurnerInteractor"
        };

        Transform[] transforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform item in transforms)
        {
            if (
                item == null ||
                !item.gameObject.scene.IsValid() ||
                !item.gameObject.scene.isLoaded
            )
            {
                continue;
            }

            bool nameMatch =
                requiredNames.Any(
                    name =>
                        item.name.Equals(
                            name,
                            StringComparison.OrdinalIgnoreCase));

            if (!nameMatch)
            {
                continue;
            }

            report.AppendLine(
                GetHierarchyPath(item) +
                " | activeSelf=" +
                item.gameObject.activeSelf +
                " | activeInHierarchy=" +
                item.gameObject.activeInHierarchy);
        }
    }

    private static void AuditRelevantSourceFiles(
        StringBuilder report)
    {
        WriteHeader(
            report,
            "RELEVANT SOURCE FILES");

        string[] searchRoots =
        {
            "Assets/_Project",
            "Assets/Scripts"
        };

        string[] terms =
        {
            "DynamicMoveProvider",
            "ContinuousMoveProvider",
            "LocomotionControllerInteractorGroup",
            "SetActive",
            "Controller and Hand",
            "Controller",
            "EnvironmentRoot",
            "Rabbit",
            "BowDrawController"
        };

        foreach (string root in searchRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] files =
                Directory.GetFiles(
                    root,
                    "*.cs",
                    SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string content;

                try
                {
                    content =
                        File.ReadAllText(
                            file);
                }
                catch
                {
                    continue;
                }

                List<string> found =
                    new List<string>();

                foreach (string term in terms)
                {
                    if (
                        content.IndexOf(
                            term,
                            StringComparison.OrdinalIgnoreCase) >= 0
                    )
                    {
                        found.Add(
                            term);
                    }
                }

                if (found.Count == 0)
                {
                    continue;
                }

                report.AppendLine(
                    file.Replace(
                        "\\",
                        "/") +
                    " | terms=[" +
                    string.Join(
                        ", ",
                        found) +
                    "]");
            }
        }
    }

    private static GameObject FindSceneObjectExact(
        string objectName)
    {
        Transform[] transforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform item in transforms)
        {
            if (
                item != null &&
                item.gameObject.scene.IsValid() &&
                item.gameObject.scene.isLoaded &&
                item.gameObject.name ==
                    objectName
            )
            {
                return item.gameObject;
            }
        }

        return null;
    }

    private static Component FindComponentByTypeName(
        string shortOrFullName)
    {
        Component[] components =
            Resources.FindObjectsOfTypeAll<Component>();

        foreach (Component component in components)
        {
            if (
                component == null ||
                !component.gameObject.scene.IsValid() ||
                !component.gameObject.scene.isLoaded
            )
            {
                continue;
            }

            Type type =
                component.GetType();

            if (
                type.Name ==
                    shortOrFullName ||
                type.FullName ==
                    shortOrFullName
            )
            {
                return component;
            }
        }

        return null;
    }

    private static int GetDepthFrom(
        Transform item,
        Transform root)
    {
        int depth =
            0;

        Transform current =
            item;

        while (
            current != null &&
            current != root
        )
        {
            depth++;
            current =
                current.parent;
        }

        return depth;
    }

    private static string DescribeCollider(
        Collider collider)
    {
        if (collider == null)
        {
            return "<null>";
        }

        string common =
            collider.GetType().Name +
            " | enabled=" +
            collider.enabled +
            " | trigger=" +
            collider.isTrigger +
            " | boundsSize=" +
            FormatVector(
                collider.bounds.size);

        BoxCollider box =
            collider as BoxCollider;

        if (box != null)
        {
            return
                common +
                " | center=" +
                FormatVector(
                    box.center) +
                " | size=" +
                FormatVector(
                    box.size);
        }

        SphereCollider sphere =
            collider as SphereCollider;

        if (sphere != null)
        {
            return
                common +
                " | center=" +
                FormatVector(
                    sphere.center) +
                " | radius=" +
                sphere.radius.ToString("F4");
        }

        CapsuleCollider capsule =
            collider as CapsuleCollider;

        if (capsule != null)
        {
            return
                common +
                " | center=" +
                FormatVector(
                    capsule.center) +
                " | radius=" +
                capsule.radius.ToString("F4") +
                " | height=" +
                capsule.height.ToString("F4") +
                " | direction=" +
                capsule.direction;
        }

        return common;
    }

    private static string GetSerializedValue(
        SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                return
                    property.objectReferenceValue != null
                        ? property.objectReferenceValue.name +
                          " | " +
                          AssetDatabase.GetAssetPath(
                              property.objectReferenceValue)
                        : "<null>";

            case SerializedPropertyType.Boolean:
                return
                    property.boolValue.ToString();

            case SerializedPropertyType.Float:
                return
                    property.floatValue.ToString("F4");

            case SerializedPropertyType.Integer:
                return
                    property.intValue.ToString();

            case SerializedPropertyType.Enum:
                return
                    property.enumDisplayNames != null &&
                    property.enumValueIndex >= 0 &&
                    property.enumValueIndex <
                    property.enumDisplayNames.Length
                        ? property.enumDisplayNames[
                            property.enumValueIndex]
                        : property.enumValueIndex.ToString();

            case SerializedPropertyType.String:
                return
                    property.stringValue;

            default:
                return
                    "<unsupported>";
        }
    }

    private static string GetEnabledState(
        Component component)
    {
        Behaviour behaviour =
            component as Behaviour;

        if (behaviour != null)
        {
            return
                behaviour.enabled.ToString();
        }

        Collider collider =
            component as Collider;

        if (collider != null)
        {
            return
                collider.enabled.ToString();
        }

        Renderer renderer =
            component as Renderer;

        if (renderer != null)
        {
            return
                renderer.enabled.ToString();
        }

        return "<n/a>";
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

    private static string GetHierarchyPathRelative(
        Transform item,
        Transform root)
    {
        if (item == null)
        {
            return "<null>";
        }

        if (item == root)
        {
            return root.name;
        }

        List<string> names =
            new List<string>();

        Transform current =
            item;

        while (
            current != null &&
            current != root
        )
        {
            names.Add(
                current.name);

            current =
                current.parent;
        }

        names.Add(
            root.name);

        names.Reverse();

        return string.Join(
            "/",
            names);
    }

    private static string FormatVector(
        Vector3 value)
    {
        return
            "(" +
            value.x.ToString("F4") +
            ", " +
            value.y.ToString("F4") +
            ", " +
            value.z.ToString("F4") +
            ")";
    }

    private static void WriteHeader(
        StringBuilder report,
        string title)
    {
        report.AppendLine();
        report.AppendLine(
            "============================================================");

        report.AppendLine(
            title);

        report.AppendLine(
            "============================================================");
    }
}
#endif