#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ForestArchery.Finalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalGameplayAdjustmentsSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private const string RabbitPrefabPath =
        "Assets/_Project/Wildlife/Generated/Prefabs/Rabbit_Gameplay.prefab";

    private const float BowForwardDistance =
        0.5f;

    private const float RabbitBodyRadius =
        0.1002f;

    private const float RabbitBodyHeight =
        0.2234f;

    private const float RabbitHeadRadius =
        0.0633f;

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10g v2 - Apply Final Gameplay Adjustments")]
    public static void Apply()
    {
        try
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            ApplyBowPlacement();
            RemoveEnvironmentRoot();
            EnlargeRabbitHitboxes();
            InstallLocomotionGuard();
            RepairOptionalLegacyModeLock();

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[STAGE 10G V2] Final gameplay adjustments applied.");

            EditorUtility.DisplayDialog(
                "Final Gameplay Adjustments Applied",
                "Stage 10G v2 was applied:\n\n" +
                "- Bow starts at eye level, 0.50 m in front.\n" +
                "- EnvironmentRoot target/platform are removed.\n" +
                "- Rabbit hitboxes are approximately 15% larger.\n" +
                "- Controller locomotion remains active at runtime.\n\n" +
                "The old TimedGameInteractionModeLock is optional and is repaired only when present.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 10G v2 Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10g v2 - Validate Final Gameplay Adjustments")]
    public static void Validate()
    {
        ValidateBowPlacement();
        ValidateEnvironmentRoot();
        ValidateRabbitHitboxes();
        ValidateLocomotionGuard();

        Debug.Log(
            "[STAGE 10G V2] Validation passed.");

        EditorUtility.DisplayDialog(
            "Final Gameplay Adjustments Validated",
            "All Stage 10G v2 scene and prefab checks passed.\n\n" +
            "Final locomotion behaviour must be tested on Meta Quest.",
            "OK");
    }

    private static void ApplyBowPlacement()
    {
        GameObject bow =
            FindSceneObjectExact(
                "Bow");

        if (bow == null)
        {
            throw new InvalidOperationException(
                "Bow root was not found.");
        }

        Transform eye =
            FindByExactSuffix(
                "[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");

        BowEyeLevelStartPlacement placement =
            bow.GetComponent<BowEyeLevelStartPlacement>();

        if (placement == null)
        {
            placement =
                Undo.AddComponent<BowEyeLevelStartPlacement>(
                    bow);
        }

        placement.Configure(
            eye,
            BowForwardDistance);

        EditorUtility.SetDirty(
            placement);

        EditorUtility.SetDirty(
            bow);
    }

    private static void RemoveEnvironmentRoot()
    {
        GameObject environmentRoot =
            FindSceneObjectExact(
                "EnvironmentRoot");

        if (environmentRoot == null)
        {
            return;
        }

        Transform shootingArea =
            environmentRoot.transform.Find(
                "ShootingArea");

        Transform cube =
            environmentRoot.transform.Find(
                "ShootingArea/ShootingPosition/Cube");

        Transform target =
            environmentRoot.transform.Find(
                "ShootingArea/TargetArea/Target");

        if (
            environmentRoot.transform.childCount != 1 ||
            shootingArea == null ||
            cube == null ||
            target == null
        )
        {
            throw new InvalidOperationException(
                "EnvironmentRoot contains unexpected content. Deletion was cancelled.");
        }

        Transform[] descendants =
            environmentRoot.GetComponentsInChildren<Transform>(
                true);

        if (descendants.Length != 7)
        {
            throw new InvalidOperationException(
                "EnvironmentRoot hierarchy changed. Deletion was cancelled.");
        }

        Undo.DestroyObjectImmediate(
            environmentRoot);
    }

    private static void EnlargeRabbitHitboxes()
    {
        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(
                RabbitPrefabPath);

        try
        {
            CapsuleCollider body =
                FindChildComponent<CapsuleCollider>(
                    prefabRoot.transform,
                    "BodyHitbox");

            SphereCollider head =
                FindChildComponent<SphereCollider>(
                    prefabRoot.transform,
                    "HeadHitbox");

            if (
                body == null ||
                head == null
            )
            {
                throw new InvalidOperationException(
                    "Rabbit hitboxes were not found.");
            }

            body.radius =
                RabbitBodyRadius;

            body.height =
                RabbitBodyHeight;

            head.radius =
                RabbitHeadRadius;

            EditorUtility.SetDirty(
                body);

            EditorUtility.SetDirty(
                head);

            PrefabUtility.SaveAsPrefabAsset(
                prefabRoot,
                RabbitPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(
                prefabRoot);
        }
    }

    private static void InstallLocomotionGuard()
    {
        GameObject host =
            FindSceneObjectExact(
                "TimedGameSystem");

        if (host == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem was not found.");
        }

        GameObject leftController =
            FindByExactSuffix(
                "LeftInteractions/Interactors/Controller")
                .gameObject;

        GameObject rightController =
            FindByExactSuffix(
                "RightInteractions/Interactors/Controller")
                .gameObject;

        GameObject leftGroup =
            FindByExactSuffix(
                "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup")
                .gameObject;

        GameObject rightGroup =
            FindByExactSuffix(
                "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup")
                .gameObject;

        GameObject leftActions =
            FindByExactSuffix(
                "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerLocomotionSlideActions")
                .gameObject;

        GameObject rightActions =
            FindByExactSuffix(
                "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerLocomotionSlideActions")
                .gameObject;

        GameObject leftSlide =
            FindByExactSuffix(
                "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerSlideInteractor")
                .gameObject;

        GameObject rightSlide =
            FindByExactSuffix(
                "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerSlideInteractor")
                .gameObject;

        GameObject leftTurn =
            FindByExactSuffix(
                "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerTurnerInteractor")
                .gameObject;

        GameObject rightTurn =
            FindByExactSuffix(
                "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerTurnerInteractor")
                .gameObject;

        GameObject leftOutput =
            FindByExactSuffix(
                "LeftInteractions/LocomotionOutput")
                .gameObject;

        GameObject rightOutput =
            FindByExactSuffix(
                "RightInteractions/LocomotionOutput")
                .gameObject;

        GameObject locomotor =
            FindByExactSuffix(
                "[BuildingBlock] OVRInteractionComprehensive/Locomotor")
                .gameObject;

        GameObject playerController =
            FindByExactSuffix(
                "[BuildingBlock] OVRInteractionComprehensive/Locomotor/PlayerController")
                .gameObject;

        ControllerLocomotionRuntimeGuard guard =
            host.GetComponent<ControllerLocomotionRuntimeGuard>();

        if (guard == null)
        {
            guard =
                Undo.AddComponent<ControllerLocomotionRuntimeGuard>(
                    host);
        }

        guard.Configure(
            leftController,
            rightController,
            leftGroup,
            rightGroup,
            leftActions,
            rightActions,
            leftSlide,
            rightSlide,
            leftTurn,
            rightTurn,
            leftOutput,
            rightOutput,
            locomotor,
            playerController);

        EditorUtility.SetDirty(
            guard);

        EditorUtility.SetDirty(
            host);
    }

    private static void RepairOptionalLegacyModeLock()
    {
        Component modeLock =
            FindLegacyModeLockBySerializedFields();

        if (modeLock == null)
        {
            Debug.Log(
                "[STAGE 10G V2] TimedGameInteractionModeLock is not present. " +
                "This is valid in the current project; locomotion is protected by ControllerLocomotionRuntimeGuard.");

            return;
        }

        SerializedObject serialized =
            new SerializedObject(
                modeLock);

        SerializedProperty controllerRoots =
            serialized.FindProperty(
                "controllerInteractionRoots");

        if (
            controllerRoots == null ||
            !controllerRoots.isArray
        )
        {
            return;
        }

        List<GameObject> repaired =
            new List<GameObject>();

        for (
            int index = 0;
            index < controllerRoots.arraySize;
            index++)
        {
            GameObject root =
                controllerRoots
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue as GameObject;

            if (root == null)
            {
                continue;
            }

            Transform locomotionGroup =
                FindDescendantExact(
                    root.transform,
                    "LocomotionControllerInteractorGroup");

            if (
                root.name == "Controller" &&
                locomotionGroup != null
            )
            {
                Transform ray =
                    FindDescendantExact(
                        root.transform,
                        "ControllerRayInteractor");

                if (ray != null)
                {
                    AddUnique(
                        repaired,
                        ray.gameObject);
                }

                continue;
            }

            AddUnique(
                repaired,
                root);
        }

        controllerRoots.arraySize =
            repaired.Count;

        for (
            int index = 0;
            index < repaired.Count;
            index++)
        {
            controllerRoots
                .GetArrayElementAtIndex(index)
                .objectReferenceValue =
                repaired[index];
        }

        serialized
            .ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            modeLock);
    }

    private static Component FindLegacyModeLockBySerializedFields()
    {
        Component[] components =
            Resources.FindObjectsOfTypeAll<Component>();

        List<Component> matches =
            new List<Component>();

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

            bool nameMatch =
                type.Name ==
                    "TimedGameInteractionModeLock" ||
                type.FullName ==
                    "ForestArchery.TimedGame.TimedGameInteractionModeLock";

            if (nameMatch)
            {
                matches.Add(
                    component);

                continue;
            }

            try
            {
                SerializedObject serialized =
                    new SerializedObject(
                        component);

                SerializedProperty controllerRoots =
                    serialized.FindProperty(
                        "controllerInteractionRoots");

                SerializedProperty handRoots =
                    serialized.FindProperty(
                        "handInteractionRoots");

                if (
                    controllerRoots != null &&
                    controllerRoots.isArray &&
                    handRoots != null &&
                    handRoots.isArray
                )
                {
                    matches.Add(
                        component);
                }
            }
            catch
            {
            }
        }

        return matches.Count == 1
            ? matches[0]
            : null;
    }

    private static void ValidateBowPlacement()
    {
        GameObject bow =
            FindSceneObjectExact(
                "Bow");

        if (bow == null)
        {
            throw new InvalidOperationException(
                "Bow root is missing.");
        }

        BowEyeLevelStartPlacement placement =
            bow.GetComponent<BowEyeLevelStartPlacement>();

        if (placement == null)
        {
            throw new InvalidOperationException(
                "BowEyeLevelStartPlacement is missing.");
        }

        if (placement.EyeTransform == null)
        {
            throw new InvalidOperationException(
                "Bow eye reference is missing.");
        }

        if (
            Mathf.Abs(
                placement.ForwardDistance -
                BowForwardDistance) >
            0.001f
        )
        {
            throw new InvalidOperationException(
                "Bow forward distance is incorrect.");
        }
    }

    private static void ValidateEnvironmentRoot()
    {
        if (
            FindSceneObjectExact(
                "EnvironmentRoot") != null
        )
        {
            throw new InvalidOperationException(
                "EnvironmentRoot still exists.");
        }
    }

    private static void ValidateRabbitHitboxes()
    {
        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(
                RabbitPrefabPath);

        try
        {
            CapsuleCollider body =
                FindChildComponent<CapsuleCollider>(
                    prefabRoot.transform,
                    "BodyHitbox");

            SphereCollider head =
                FindChildComponent<SphereCollider>(
                    prefabRoot.transform,
                    "HeadHitbox");

            if (
                body == null ||
                head == null
            )
            {
                throw new InvalidOperationException(
                    "Rabbit hitboxes are missing.");
            }

            ValidateFloat(
                body.radius,
                RabbitBodyRadius,
                "Rabbit body radius");

            ValidateFloat(
                body.height,
                RabbitBodyHeight,
                "Rabbit body height");

            ValidateFloat(
                head.radius,
                RabbitHeadRadius,
                "Rabbit head radius");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(
                prefabRoot);
        }
    }

    private static void ValidateLocomotionGuard()
    {
        GameObject host =
            FindSceneObjectExact(
                "TimedGameSystem");

        if (host == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem is missing.");
        }

        ControllerLocomotionRuntimeGuard guard =
            host.GetComponent<ControllerLocomotionRuntimeGuard>();

        if (guard == null)
        {
            throw new InvalidOperationException(
                "ControllerLocomotionRuntimeGuard is missing.");
        }

        if (!guard.ReferencesAssigned)
        {
            throw new InvalidOperationException(
                "ControllerLocomotionRuntimeGuard has missing references.");
        }

        ValidateActiveState(
            "LeftInteractions/Interactors/Controller",
            true);

        ValidateActiveState(
            "RightInteractions/Interactors/Controller",
            true);

        ValidateActiveState(
            "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup",
            true);

        ValidateActiveState(
            "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup",
            true);

        ValidateActiveState(
            "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerLocomotionSlideActions",
            true);

        ValidateActiveState(
            "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerLocomotionSlideActions",
            true);

        ValidateActiveState(
            "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerSlideInteractor",
            true);

        ValidateActiveState(
            "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerSlideInteractor",
            false);

        ValidateActiveState(
            "LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerTurnerInteractor",
            false);

        ValidateActiveState(
            "RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerTurnerInteractor",
            true);

        ValidateActiveState(
            "LeftInteractions/LocomotionOutput",
            true);

        ValidateActiveState(
            "RightInteractions/LocomotionOutput",
            true);

        ValidateActiveState(
            "[BuildingBlock] OVRInteractionComprehensive/Locomotor",
            true);

        ValidateActiveState(
            "[BuildingBlock] OVRInteractionComprehensive/Locomotor/PlayerController",
            true);
    }

    private static void ValidateActiveState(
        string suffix,
        bool expected)
    {
        Transform item =
            FindByExactSuffix(
                suffix);

        if (
            item.gameObject.activeSelf !=
            expected
        )
        {
            throw new InvalidOperationException(
                suffix +
                " has activeSelf=" +
                item.gameObject.activeSelf +
                ", expected " +
                expected);
        }
    }

    private static void ValidateFloat(
        float actual,
        float expected,
        string label)
    {
        if (
            Mathf.Abs(
                actual -
                expected) >
            0.0002f
        )
        {
            throw new InvalidOperationException(
                label +
                " is " +
                actual.ToString("F4") +
                ", expected " +
                expected.ToString("F4"));
        }
    }

    private static T FindChildComponent<T>(
        Transform root,
        string objectName)
        where T : Component
    {
        Transform child =
            FindDescendantExact(
                root,
                objectName);

        return child != null
            ? child.GetComponent<T>()
            : null;
    }

    private static Transform FindDescendantExact(
        Transform root,
        string objectName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] transforms =
            root.GetComponentsInChildren<Transform>(
                true);

        foreach (Transform item in transforms)
        {
            if (
                item != null &&
                item.name ==
                    objectName
            )
            {
                return item;
            }
        }

        return null;
    }

    private static Transform FindByExactSuffix(
        string requiredSuffix)
    {
        Transform[] transforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        List<Transform> matches =
            new List<Transform>();

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

            string path =
                GetHierarchyPath(
                    item);

            if (
                path.EndsWith(
                    requiredSuffix,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                matches.Add(
                    item);
            }
        }

        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                "Expected one scene object ending with:\n" +
                requiredSuffix +
                "\nFound: " +
                matches.Count);
        }

        return matches[0];
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

    private static void AddUnique(
        List<GameObject> objects,
        GameObject item)
    {
        if (
            item != null &&
            !objects.Contains(item)
        )
        {
            objects.Add(
                item);
        }
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