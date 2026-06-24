using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VRBowInteractionAudit
{
    private const string ArrowPrefabPath =
        "Assets/Prefabs/Arrow.prefab";

    [MenuItem("Tools/VR Project/Flip Arrow Visual 180 Degrees")]
    public static void FlipArrowVisual()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Oprește Play Mode înainte să modifici prefab-ul săgeții.",
                "OK");

            return;
        }

        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(
                ArrowPrefabPath);

        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Arrow prefab not found",
                "Nu am putut deschide:\n" + ArrowPrefabPath,
                "OK");

            return;
        }

        try
        {
            Transform arrowVisual =
                FindChildRecursive(
                    prefabRoot.transform,
                    "ArrowVisual");

            if (arrowVisual == null)
            {
                throw new InvalidOperationException(
                    "Nu am găsit copilul ArrowVisual în prefab-ul săgeții.");
            }

            Vector3 oldEuler =
                arrowVisual.localEulerAngles;

            // Întoarce înainte/înapoi modelul vizual.
            // Root-ul, colliderul și direcția fizică rămân neschimbate.
            arrowVisual.localRotation =
                arrowVisual.localRotation *
                Quaternion.Euler(0f, 180f, 0f);

            PrefabUtility.SaveAsPrefabAsset(
                prefabRoot,
                ArrowPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Arrow Orientation Fix] ArrowVisual rotit cu 180° pe axa locală Y. " +
                $"Rotație anterioară: {oldEuler}; " +
                $"rotație nouă: {arrowVisual.localEulerAngles}");

            EditorUtility.DisplayDialog(
                "Arrow orientation fixed",
                "ArrowVisual a fost rotit cu 180°.\n\n" +
                "Root-ul Arrow, colliderul, Rigidbody-ul și codul de lansare nu au fost modificate.\n\n" +
                "Dacă rezultatul nu este corect, rulează aceeași comandă încă o dată pentru a reveni.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Arrow orientation fix failed",
                exception.Message,
                "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(
                prefabRoot);
        }
    }

    [MenuItem("Tools/VR Project/Export Bow Interaction Audit")]
    public static void ExportBowInteractionAudit()
    {
        try
        {
            Scene activeScene =
                SceneManager.GetActiveScene();

            if (!activeScene.IsValid() ||
                !activeScene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Nu există nicio scenă activă.");
            }

            GameObject bow =
                Resources
                    .FindObjectsOfTypeAll<GameObject>()
                    .Where(
                        item =>
                            item.scene == activeScene &&
                            item.name == "Bow" &&
                            item.hideFlags == HideFlags.None)
                    .OrderByDescending(
                        item => item.activeInHierarchy)
                    .FirstOrDefault();

            if (bow == null)
            {
                throw new InvalidOperationException(
                    "Nu am găsit obiectul Bow în scena activă.");
            }

            string projectRoot =
                Directory.GetParent(
                    Application.dataPath).FullName;

            string reportPath =
                Path.Combine(
                    projectRoot,
                    "BowInteractionAudit.txt");

            StringBuilder report =
                new StringBuilder();

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "VR BOW INTERACTION AUDIT");

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            report.AppendLine(
                $"Unity: {Application.unityVersion}");

            report.AppendLine(
                $"Scene: {activeScene.path}");

            report.AppendLine(
                $"Bow hierarchy path: {GetHierarchyPath(bow.transform)}");

            report.AppendLine();

            report.AppendLine(
                "GOAL:");

            report.AppendLine(
                "- Arrow visual orientation");

            report.AppendLine(
                "- Handle-only grab zone");

            report.AppendLine(
                "- Fixed hand/controller grab position and rotation");

            report.AppendLine();

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "BOW ROOT");

            report.AppendLine(
                "============================================================");

            WriteTransform(
                report,
                bow.transform,
                0);

            Rigidbody bowRigidbody =
                bow.GetComponent<Rigidbody>();

            if (bowRigidbody != null)
            {
                report.AppendLine(
                    $"Rigidbody: mass={bowRigidbody.mass}, " +
                    $"kinematic={bowRigidbody.isKinematic}, " +
                    $"gravity={bowRigidbody.useGravity}, " +
                    $"constraints={bowRigidbody.constraints}");
            }
            else
            {
                report.AppendLine(
                    "Rigidbody: MISSING");
            }

            report.AppendLine();

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "FULL BOW HIERARCHY AND COMPONENTS");

            report.AppendLine(
                "============================================================");

            DumpHierarchy(
                report,
                bow.transform,
                0);

            report.AppendLine();

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "RELEVANT SERIALIZED COMPONENT DATA");

            report.AppendLine(
                "============================================================");

            Component[] components =
                bow.GetComponentsInChildren<Component>(
                    true);

            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                string typeName =
                    component.GetType().FullName;

                if (!IsRelevantComponent(typeName))
                {
                    continue;
                }

                report.AppendLine();

                report.AppendLine(
                    "------------------------------------------------------------");

                report.AppendLine(
                    $"Object: {GetHierarchyPath(component.transform)}");

                report.AppendLine(
                    $"Component: {typeName}");

                DumpSerializedProperties(
                    report,
                    component);
            }

            report.AppendLine();

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "COLLIDERS");

            report.AppendLine(
                "============================================================");

            Collider[] colliders =
                bow.GetComponentsInChildren<Collider>(
                    true);

            foreach (Collider collider in colliders)
            {
                report.AppendLine(
                    $"Object: {GetHierarchyPath(collider.transform)}");

                report.AppendLine(
                    $"Type: {collider.GetType().FullName}");

                report.AppendLine(
                    $"Enabled: {collider.enabled}");

                report.AppendLine(
                    $"Trigger: {collider.isTrigger}");

                report.AppendLine(
                    $"Bounds center world: {collider.bounds.center}");

                report.AppendLine(
                    $"Bounds size world: {collider.bounds.size}");

                if (collider is BoxCollider box)
                {
                    report.AppendLine(
                        $"Box center local: {box.center}");

                    report.AppendLine(
                        $"Box size local: {box.size}");
                }

                if (collider is CapsuleCollider capsule)
                {
                    report.AppendLine(
                        $"Capsule center local: {capsule.center}");

                    report.AppendLine(
                        $"Capsule radius: {capsule.radius}");

                    report.AppendLine(
                        $"Capsule height: {capsule.height}");

                    report.AppendLine(
                        $"Capsule direction: {capsule.direction}");
                }

                report.AppendLine();
            }

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "IMPORTANT TRANSFORMS");

            report.AppendLine(
                "============================================================");

            string[] importantNames =
            {
                "BowMesh",
                "BowVisual",
                "HandGrabPoint_Left",
                "StringGrabPoint",
                "String Rest Point",
                "ArrowSpawnPoint"
            };

            foreach (string objectName in importantNames)
            {
                Transform target =
                    FindChildRecursive(
                        bow.transform,
                        objectName);

                if (target == null)
                {
                    report.AppendLine(
                        $"{objectName}: NOT FOUND");

                    continue;
                }

                WriteTransform(
                    report,
                    target,
                    0);

                report.AppendLine();
            }

            report.AppendLine(
                "============================================================");

            report.AppendLine(
                "ARROW PREFAB ORIENTATION");

            report.AppendLine(
                "============================================================");

            DumpArrowPrefab(
                report);

            File.WriteAllText(
                reportPath,
                report.ToString(),
                Encoding.UTF8);

            Debug.Log(
                "[VR Bow Audit] Report written to: " +
                reportPath);

            EditorUtility.DisplayDialog(
                "Bow interaction audit complete",
                "Raportul a fost creat aici:\n\n" +
                reportPath +
                "\n\nTrimite-mi fișierul BowInteractionAudit.txt.",
                "OK");

            EditorUtility.RevealInFinder(
                reportPath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Bow audit failed",
                exception.Message +
                "\n\nVerifică Unity Console.",
                "OK");
        }
    }

    private static void DumpHierarchy(
        StringBuilder report,
        Transform transform,
        int depth)
    {
        string indent =
            new string(' ', depth * 2);

        report.AppendLine(
            $"{indent}- {transform.name} " +
            $"[activeSelf={transform.gameObject.activeSelf}, " +
            $"activeHierarchy={transform.gameObject.activeInHierarchy}, " +
            $"tag={transform.tag}, " +
            $"layer={LayerMask.LayerToName(transform.gameObject.layer)}]");

        WriteTransform(
            report,
            transform,
            depth + 1);

        Component[] components =
            transform.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
            {
                report.AppendLine(
                    $"{indent}  Component: MISSING SCRIPT");

                continue;
            }

            report.AppendLine(
                $"{indent}  Component: {component.GetType().FullName}");
        }

        foreach (Transform child in transform)
        {
            DumpHierarchy(
                report,
                child,
                depth + 1);
        }
    }

    private static void WriteTransform(
        StringBuilder report,
        Transform transform,
        int depth)
    {
        string indent =
            new string(' ', depth * 2);

        report.AppendLine(
            $"{indent}Transform {GetHierarchyPath(transform)}");

        report.AppendLine(
            $"{indent}  localPosition={transform.localPosition}");

        report.AppendLine(
            $"{indent}  localEulerAngles={transform.localEulerAngles}");

        report.AppendLine(
            $"{indent}  localScale={transform.localScale}");

        report.AppendLine(
            $"{indent}  worldPosition={transform.position}");

        report.AppendLine(
            $"{indent}  worldEulerAngles={transform.eulerAngles}");
    }

    private static void DumpSerializedProperties(
        StringBuilder report,
        Component component)
    {
        SerializedObject serializedObject =
            new SerializedObject(component);

        SerializedProperty property =
            serializedObject.GetIterator();

        int propertyCount = 0;

        while (property.NextVisible(true))
        {
            if (propertyCount++ > 500)
            {
                report.AppendLine(
                    "  ... property limit reached ...");

                break;
            }

            report.AppendLine(
                $"  {property.propertyPath} " +
                $"[{property.propertyType}] = " +
                GetPropertyValue(property));
        }
    }

    private static string GetPropertyValue(
        SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue.ToString();

            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();

            case SerializedPropertyType.Float:
                return property.floatValue.ToString("0.######");

            case SerializedPropertyType.String:
                return property.stringValue ?? "";

            case SerializedPropertyType.Color:
                return property.colorValue.ToString();

            case SerializedPropertyType.ObjectReference:
                if (property.objectReferenceValue == null)
                {
                    return "None";
                }

                UnityEngine.Object reference =
                    property.objectReferenceValue;

                string assetPath =
                    AssetDatabase.GetAssetPath(reference);

                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    return $"{reference.name} | Asset: {assetPath}";
                }

                if (reference is Component component)
                {
                    return
                        $"{reference.name} | Scene: " +
                        GetHierarchyPath(component.transform);
                }

                if (reference is GameObject gameObject)
                {
                    return
                        $"{reference.name} | Scene: " +
                        GetHierarchyPath(gameObject.transform);
                }

                return reference.name;

            case SerializedPropertyType.Enum:
                return
                    $"{property.enumValueIndex} " +
                    $"({property.enumDisplayNames[property.enumValueIndex]})";

            case SerializedPropertyType.Vector2:
                return property.vector2Value.ToString();

            case SerializedPropertyType.Vector3:
                return property.vector3Value.ToString();

            case SerializedPropertyType.Vector4:
                return property.vector4Value.ToString();

            case SerializedPropertyType.Quaternion:
                return property.quaternionValue.eulerAngles.ToString();

            case SerializedPropertyType.Rect:
                return property.rectValue.ToString();

            case SerializedPropertyType.Bounds:
                return property.boundsValue.ToString();

            case SerializedPropertyType.ArraySize:
                return property.intValue.ToString();

            default:
                return property.propertyType.ToString();
        }
    }

    private static bool IsRelevantComponent(
        string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        string lower =
            typeName.ToLowerInvariant();

        return
            lower.Contains("grab") ||
            lower.Contains("grabbable") ||
            lower.Contains("pose") ||
            lower.Contains("transformer") ||
            lower.Contains("pointable") ||
            lower.Contains("interactable") ||
            lower.Contains("rigidbody") ||
            lower.Contains("bowdraw");
    }

    private static void DumpArrowPrefab(
        StringBuilder report)
    {
        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(
                ArrowPrefabPath);

        if (prefabRoot == null)
        {
            report.AppendLine(
                "Arrow prefab could not be loaded.");

            return;
        }

        try
        {
            Transform arrowVisual =
                FindChildRecursive(
                    prefabRoot.transform,
                    "ArrowVisual");

            WriteTransform(
                report,
                prefabRoot.transform,
                0);

            if (arrowVisual != null)
            {
                WriteTransform(
                    report,
                    arrowVisual,
                    0);
            }
            else
            {
                report.AppendLine(
                    "ArrowVisual: NOT FOUND");
            }

            Collider[] colliders =
                prefabRoot.GetComponentsInChildren<Collider>(
                    true);

            foreach (Collider collider in colliders)
            {
                report.AppendLine(
                    $"Arrow collider: {collider.GetType().Name} " +
                    $"on {GetHierarchyPath(collider.transform)}");

                report.AppendLine(
                    $"Bounds center={collider.bounds.center}, " +
                    $"size={collider.bounds.size}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(
                prefabRoot);
        }
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

        Transform parent =
            transform.parent;

        while (parent != null)
        {
            path =
                parent.name +
                "/" +
                path;

            parent =
                parent.parent;
        }

        return path;
    }

    private static Transform FindChildRecursive(
        Transform root,
        string exactName)
    {
        return root
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(
                item =>
                    string.Equals(
                        item.name,
                        exactName,
                        StringComparison.Ordinal));
    }
}
