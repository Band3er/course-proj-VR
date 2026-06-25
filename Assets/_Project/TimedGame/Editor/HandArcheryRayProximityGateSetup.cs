#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HandArcheryRayProximityGateSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    private const float SuspendDistance =
        0.55f;

    private const float RestoreDistance =
        0.72f;

    private const string LeftPinchSuffix =
        "LeftInteractions/Interactors/Hand and No Controller/HandGrabInteractor/Rigidbody/PinchPoint";

    private const string RightPinchSuffix =
        "RightInteractions/Interactors/Hand and No Controller/HandGrabInteractor/Rigidbody/PinchPoint";

    private const string LeftRaySuffix =
        "LeftInteractions/Interactors/Hand and No Controller/HandRayInteractor";

    private const string RightRaySuffix =
        "RightInteractions/Interactors/Hand and No Controller/HandRayInteractor";

    private const string LeftDistanceSuffix =
        "LeftInteractions/Interactors/Hand and No Controller/DistanceHandGrabInteractor";

    private const string RightDistanceSuffix =
        "RightInteractions/Interactors/Hand and No Controller/DistanceHandGrabInteractor";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10c v2 - Install Hand Archery Ray Proximity Gate")]
    public static void Install()
    {
        try
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            BowDrawController bow =
                UnityEngine.Object
                    .FindFirstObjectByType<BowDrawController>();

            if (bow == null)
            {
                throw new InvalidOperationException(
                    "BowDrawController was not found.");
            }

            GameObject host =
                GameObject.Find(
                    "TimedGameSystem");

            if (host == null)
            {
                TimedGameMenuController menu =
                    UnityEngine.Object
                        .FindFirstObjectByType<TimedGameMenuController>();

                host =
                    menu != null
                        ? menu.gameObject
                        : null;
            }

            if (host == null)
            {
                throw new InvalidOperationException(
                    "TimedGameSystem was not found.");
            }

            Transform leftHandPoint =
                FindByExactPathSuffix(
                    LeftPinchSuffix);

            Transform rightHandPoint =
                FindByExactPathSuffix(
                    RightPinchSuffix);

            GameObject leftRay =
                FindByExactPathSuffix(
                    LeftRaySuffix)
                    .gameObject;

            GameObject rightRay =
                FindByExactPathSuffix(
                    RightRaySuffix)
                    .gameObject;

            GameObject leftDistance =
                FindByExactPathSuffix(
                    LeftDistanceSuffix)
                    .gameObject;

            GameObject rightDistance =
                FindByExactPathSuffix(
                    RightDistanceSuffix)
                    .gameObject;

            CanvasGroup pausePanel =
                FindPausePanel();

            HandArcheryRayProximityGate gate =
                host.GetComponent<HandArcheryRayProximityGate>();

            if (gate == null)
            {
                gate =
                    Undo.AddComponent<HandArcheryRayProximityGate>(
                        host);
            }

            gate.Configure(
                bow,
                leftHandPoint,
                rightHandPoint,
                leftRay,
                rightRay,
                leftDistance,
                rightDistance,
                pausePanel,
                SuspendDistance,
                RestoreDistance);

            EditorUtility.SetDirty(gate);
            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[HAND ARCHERY] Stage 10c v2 installed.\n" +
                "Left pinch point: " +
                GetHierarchyPath(leftHandPoint) +
                "\nRight pinch point: " +
                GetHierarchyPath(rightHandPoint));

            EditorUtility.DisplayDialog(
                "Hand Archery Ray Gate Installed",
                "Stage 10c v2 was installed successfully.\n\n" +
                "The exact direct-grab PinchPoint is now used for each hand.\n" +
                "Hand rays and distance grab suspend near the bow/string.\n" +
                "Direct HandGrabInteractor remains active.\n" +
                "Pause restores the rays.\n\n" +
                "Suspend distance: 0.55 m\n" +
                "Restore distance: 0.72 m",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Stage 10c v2 Installation Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10c v2 - Validate Hand Archery Ray Proximity Gate")]
    public static void Validate()
    {
        HandArcheryRayProximityGate gate =
            UnityEngine.Object
                .FindFirstObjectByType<HandArcheryRayProximityGate>();

        if (gate == null)
        {
            throw new InvalidOperationException(
                "HandArcheryRayProximityGate is not installed.");
        }

        SerializedObject serializedGate =
            new SerializedObject(gate);

        string[] requiredReferences =
        {
            "bow",
            "leftHandPoint",
            "rightHandPoint",
            "leftHandRayRoot",
            "rightHandRayRoot",
            "leftDistanceGrabRoot",
            "rightDistanceGrabRoot",
            "pausePanel"
        };

        foreach (string propertyName in requiredReferences)
        {
            SerializedProperty property =
                serializedGate.FindProperty(
                    propertyName);

            if (
                property == null ||
                property.objectReferenceValue == null
            )
            {
                throw new InvalidOperationException(
                    "Missing Stage 10c v2 reference: " +
                    propertyName);
            }
        }

        Transform leftPoint =
            serializedGate
                .FindProperty("leftHandPoint")
                .objectReferenceValue as Transform;

        Transform rightPoint =
            serializedGate
                .FindProperty("rightHandPoint")
                .objectReferenceValue as Transform;

        ValidateAssignedPath(
            leftPoint,
            LeftPinchSuffix,
            "leftHandPoint");

        ValidateAssignedPath(
            rightPoint,
            RightPinchSuffix,
            "rightHandPoint");

        if (
            gate.RestoreDistance <=
            gate.SuspendDistance
        )
        {
            throw new InvalidOperationException(
                "Restore distance must be greater than suspend distance.");
        }

        Debug.Log(
            "[HAND ARCHERY] Stage 10c v2 validation passed.");

        EditorUtility.DisplayDialog(
            "Hand Archery Ray Gate Validated",
            "All Stage 10c v2 references are assigned correctly.\n\n" +
            "The direct HandGrabInteractor PinchPoint is used for proximity.\n" +
            "Controller Mode, wildlife, scoring, menus and CSV logging were not modified.",
            "OK");
    }

    private static CanvasGroup FindPausePanel()
    {
        CanvasGroup[] groups =
            UnityEngine.Object
                .FindObjectsByType<CanvasGroup>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

        foreach (CanvasGroup group in groups)
        {
            if (
                group != null &&
                group.gameObject.name ==
                    "PausePanel"
            )
            {
                return group;
            }
        }

        throw new InvalidOperationException(
            "PausePanel CanvasGroup was not found.");
    }

    private static Transform FindByExactPathSuffix(
        string requiredSuffix)
    {
        Transform[] transforms =
            Resources
                .FindObjectsOfTypeAll<Transform>();

        List<Transform> matches =
            new List<Transform>();

        foreach (Transform candidate in transforms)
        {
            if (
                candidate == null ||
                !candidate.gameObject.scene.IsValid() ||
                !candidate.gameObject.scene.isLoaded
            )
            {
                continue;
            }

            string path =
                GetHierarchyPath(
                    candidate);

            if (
                path.EndsWith(
                    requiredSuffix,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                matches.Add(
                    candidate);
            }
        }

        if (matches.Count != 1)
        {
            string foundPaths =
                matches.Count == 0
                    ? "<none>"
                    : string.Join(
                        "\n",
                        matches.ConvertAll(
                            item =>
                                GetHierarchyPath(item)));

            throw new InvalidOperationException(
                "Expected exactly one object ending with:\n" +
                requiredSuffix +
                "\n\nFound: " +
                matches.Count +
                "\n" +
                foundPaths);
        }

        return matches[0];
    }

    private static void ValidateAssignedPath(
        Transform assigned,
        string expectedSuffix,
        string fieldName)
    {
        if (assigned == null)
        {
            throw new InvalidOperationException(
                fieldName +
                " is not assigned.");
        }

        string path =
            GetHierarchyPath(
                assigned);

        if (
            !path.EndsWith(
                expectedSuffix,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                fieldName +
                " has the wrong object:\n" +
                path +
                "\n\nExpected suffix:\n" +
                expectedSuffix);
        }
    }

    private static string GetHierarchyPath(
        Transform item)
    {
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