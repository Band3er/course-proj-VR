using System;
using System.Linq;
using ForestArchery.TimedGame;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TimedGameShootingModeGateSetup
{
    private const string RequiredScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 09a.1 - Install Safe Shooting Mode Gate")]
    public static void Install()
    {
        if (!ValidateScene())
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stage 09a.1",
                "Exit Play Mode before installing the shooting-mode gate.",
                "OK");

            return;
        }

        Scene scene =
            SceneManager.GetActiveScene();

        try
        {
            GameObject systemObject =
                FindSceneObject(
                    scene,
                    "TimedGameSystem");

            GameObject bowObject =
                FindSceneObject(
                    scene,
                    "Bow");

            if (systemObject == null)
            {
                throw new InvalidOperationException(
                    "TimedGameSystem was not found.");
            }

            if (bowObject == null)
            {
                throw new InvalidOperationException(
                    "Bow was not found.");
            }

            AbortIfLegacyModeLockIsStillAttached(
                systemObject);

            TimedRoundController roundController =
                systemObject
                    .GetComponent<TimedRoundController>();

            if (roundController == null)
            {
                throw new InvalidOperationException(
                    "TimedRoundController is missing from TimedGameSystem.");
            }

            GrabInteractable[] controllerInteractables =
                bowObject
                    .GetComponentsInChildren
                        <GrabInteractable>(
                            true)
                    .Where(
                        component =>
                            component != null)
                    .Distinct()
                    .ToArray();

            HandGrabInteractable[] handInteractables =
                bowObject
                    .GetComponentsInChildren
                        <HandGrabInteractable>(
                            true)
                    .Where(
                        component =>
                            component != null)
                    .Distinct()
                    .ToArray();

            if (
                controllerInteractables.Length <
                2
            )
            {
                throw new InvalidOperationException(
                    "Expected at least two controller GrabInteractables under Bow, found " +
                    controllerInteractables.Length +
                    ".");
            }

            if (
                handInteractables.Length <
                2
            )
            {
                throw new InvalidOperationException(
                    "Expected at least two HandGrabInteractables under Bow, found " +
                    handInteractables.Length +
                    ".");
            }

            TimedGameShootingModeGate gate =
                systemObject
                    .GetComponent
                        <TimedGameShootingModeGate>();

            if (gate == null)
            {
                gate =
                    Undo.AddComponent
                        <TimedGameShootingModeGate>(
                            systemObject);
            }

            SerializedObject serialized =
                new SerializedObject(
                    gate);

            SetObjectReference(
                serialized,
                "roundController",
                roundController);

            SetObjectArray(
                serialized,
                "controllerInteractables",
                controllerInteractables);

            SetObjectArray(
                serialized,
                "handInteractables",
                handInteractables);

            SetBoolean(
                serialized,
                "enableBothOutsideActiveRound",
                true);

            SetBoolean(
                serialized,
                "enforceWhileRoundIsActive",
                true);

            SetBoolean(
                serialized,
                "verboseLogging",
                false);

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                systemObject);

            EditorUtility.SetDirty(
                gate);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();

            ValidateInstalledState();

            Selection.activeGameObject =
                systemObject;

            Debug.Log(
                "[STAGE 09A.1] Safe shooting-mode gate installed." +
                "\nController GrabInteractables found: " +
                controllerInteractables.Length +
                "\nHandGrabInteractables found: " +
                handInteractables.Length +
                "\nNo Meta rig roots, tracking objects, ray interactors, canvases, or locomotion components were modified.");

            EditorUtility.DisplayDialog(
                "Stage 09a.1 Installed",
                "The safe shooting-mode gate was installed.\n\n" +
                "Only bow/string interactable components are controlled.\n\n" +
                "Controller Mode:\n" +
                "- controller GrabInteractables enabled\n" +
                "- HandGrabInteractables disabled\n\n" +
                "Hand Tracking Mode:\n" +
                "- controller GrabInteractables disabled\n" +
                "- HandGrabInteractables enabled\n\n" +
                "Menus, controller rays, hand rays, tracking roots, locomotion and Canvases were not modified.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 09a.1 Failed",
                exception.Message,
                "OK");
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 09a.1 - Validate Safe Shooting Mode Gate")]
    public static void Validate()
    {
        if (!ValidateScene())
        {
            return;
        }

        try
        {
            ValidateInstalledState();

            Debug.Log(
                "[STAGE 09A.1] Safe shooting-mode gate validation passed.");

            EditorUtility.DisplayDialog(
                "Stage 09a.1 Validation Passed",
                "The gate references the timed round plus all bow/string controller and hand interactables.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 09a.1 Validation Failed",
                exception.Message,
                "OK");
        }
    }

    private static void ValidateInstalledState()
    {
        GameObject systemObject =
            GameObject.Find(
                "TimedGameSystem");

        if (systemObject == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem was not found.");
        }

        AbortIfLegacyModeLockIsStillAttached(
            systemObject);

        TimedGameShootingModeGate gate =
            systemObject
                .GetComponent
                    <TimedGameShootingModeGate>();

        if (gate == null)
        {
            throw new InvalidOperationException(
                "TimedGameShootingModeGate is missing.");
        }

        SerializedObject serialized =
            new SerializedObject(
                gate);

        ValidateObjectReference(
            serialized,
            "roundController");

        ValidateArrayMinimum(
            serialized,
            "controllerInteractables",
            2);

        ValidateArrayMinimum(
            serialized,
            "handInteractables",
            2);

        SerializedProperty outsideProperty =
            serialized.FindProperty(
                "enableBothOutsideActiveRound");

        SerializedProperty enforceProperty =
            serialized.FindProperty(
                "enforceWhileRoundIsActive");

        if (
            outsideProperty == null ||
            !outsideProperty.boolValue
        )
        {
            throw new InvalidOperationException(
                "enableBothOutsideActiveRound must be enabled.");
        }

        if (
            enforceProperty == null ||
            !enforceProperty.boolValue
        )
        {
            throw new InvalidOperationException(
                "enforceWhileRoundIsActive must be enabled.");
        }
    }

    private static void AbortIfLegacyModeLockIsStillAttached(
        GameObject systemObject)
    {
        MonoBehaviour[] behaviours =
            systemObject
                .GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            if (
                string.Equals(
                    behaviour
                        .GetType()
                        .FullName,
                    "ForestArchery.TimedGame.TimedGameInteractionModeLock",
                    StringComparison.Ordinal)
            )
            {
                throw new InvalidOperationException(
                    "The old TimedGameInteractionModeLock is still attached to TimedGameSystem. " +
                    "Do not continue because it can disable the Meta rig. Restore the Stage 08f.1 scene first.");
            }
        }
    }

    private static bool ValidateScene()
    {
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
                "Stage 09a.1",
                "Open the required scene first:\n\n" +
                RequiredScenePath,
                "OK");

            return false;
        }

        return true;
    }

    private static GameObject FindSceneObject(
        Scene scene,
        string exactName)
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
                        exactName,
                        StringComparison.Ordinal)
                )
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private static void SetObjectReference(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing property: " +
                propertyName);
        }

        property.objectReferenceValue =
            value;
    }

    private static void SetBoolean(
        SerializedObject serialized,
        string propertyName,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                "Missing property: " +
                propertyName);
        }

        property.boolValue =
            value;
    }

    private static void SetObjectArray<T>(
        SerializedObject serialized,
        string propertyName,
        T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (
            property == null ||
            !property.isArray
        )
        {
            throw new InvalidOperationException(
                "Missing array property: " +
                propertyName);
        }

        property.arraySize =
            values.Length;

        for (
            int index = 0;
            index < values.Length;
            index++
        )
        {
            property
                .GetArrayElementAtIndex(
                    index)
                .objectReferenceValue =
                values[index];
        }
    }

    private static void ValidateObjectReference(
        SerializedObject serialized,
        string propertyName)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (
            property == null ||
            property.objectReferenceValue == null
        )
        {
            throw new InvalidOperationException(
                "Missing object reference: " +
                propertyName);
        }
    }

    private static void ValidateArrayMinimum(
        SerializedObject serialized,
        string propertyName,
        int minimumCount)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (
            property == null ||
            !property.isArray
        )
        {
            throw new InvalidOperationException(
                "Missing array: " +
                propertyName);
        }

        if (
            property.arraySize <
            minimumCount
        )
        {
            throw new InvalidOperationException(
                propertyName +
                " contains " +
                property.arraySize +
                " entries; expected at least " +
                minimumCount +
                ".");
        }

        for (
            int index = 0;
            index < property.arraySize;
            index++
        )
        {
            if (
                property
                    .GetArrayElementAtIndex(
                        index)
                    .objectReferenceValue ==
                null
            )
            {
                throw new InvalidOperationException(
                    propertyName +
                    " contains a null reference at index " +
                    index +
                    ".");
            }
        }
    }
}
