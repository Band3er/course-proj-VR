#if UNITY_EDITOR
using System;
using ForestArchery.Finalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayHUDAndBowSessionFixSetup
{
    private const string ScenePath =
        "Assets/Liu_Environment/Scenes/ForestArcheryScene.unity";

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10h v2 - Apply HUD Alignment + Bow Respawn")]
    public static void Apply()
    {
        try
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            GameObject timedGameSystem =
                FindSceneObjectExact(
                    "TimedGameSystem");

            if (timedGameSystem == null)
            {
                throw new InvalidOperationException(
                    "TimedGameSystem was not found.");
            }

            GameplayHUDAndBowSessionFix fix =
                timedGameSystem.GetComponent<GameplayHUDAndBowSessionFix>();

            if (fix == null)
            {
                fix =
                    Undo.AddComponent<GameplayHUDAndBowSessionFix>(
                        timedGameSystem);
            }

            fix.ResolveReferences();

            if (!fix.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    "Stage 10H v2 could not resolve one or more required objects:\n" +
                    "- CenterEyeAnchor\n" +
                    "- Bow\n" +
                    "- TrajectoryButton\n" +
                    "- WildlifeHUD_RabbitPrototype");
            }

            fix.ApplyHudAlignment();

            EditorUtility.SetDirty(
                fix);

            EditorUtility.SetDirty(
                timedGameSystem);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[STAGE 10H V2] HUD alignment and bow session respawn applied.");

            EditorUtility.DisplayDialog(
                "Stage 10H v2 Applied",
                "Applied successfully:\n\n" +
                "- score panel now matches the trajectory button's vertical line and depth\n" +
                "- Quit was not moved\n" +
                "- each new gameplay session respawns the bow relative to the current headset pose\n\n" +
                "Now run the Stage 10H v2 validator.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 10H v2 Failed",
                exception.Message,
                "OK");

            throw;
        }
    }

    [MenuItem(
        "Tools/Forest Archery/Finalization/Stage 10h v2 - Validate HUD Alignment + Bow Respawn")]
    public static void Validate()
    {
        GameObject timedGameSystem =
            FindSceneObjectExact(
                "TimedGameSystem");

        if (timedGameSystem == null)
        {
            throw new InvalidOperationException(
                "TimedGameSystem was not found.");
        }

        GameplayHUDAndBowSessionFix fix =
            timedGameSystem.GetComponent<GameplayHUDAndBowSessionFix>();

        if (fix == null)
        {
            throw new InvalidOperationException(
                "GameplayHUDAndBowSessionFix is missing.");
        }

        fix.ResolveReferences();
        fix.ApplyHudAlignment();

        if (!fix.HasRequiredReferences)
        {
            throw new InvalidOperationException(
                "GameplayHUDAndBowSessionFix still has missing references.");
        }

        if (!fix.IsHudAligned(0.035f))
        {
            throw new InvalidOperationException(
                "Trajectory and score are not aligned within the expected tolerance.");
        }

        GameObject bow =
            FindSceneObjectExact(
                "Bow");

        if (
            bow == null ||
            bow.GetComponent<BowEyeLevelStartPlacement>() == null
        )
        {
            throw new InvalidOperationException(
                "BowEyeLevelStartPlacement is missing from Bow.");
        }

        Debug.Log(
            "[STAGE 10H V2] Validation passed.");

        EditorUtility.DisplayDialog(
            "Stage 10H v2 Validated",
            "Validation passed.\n\n" +
            "Final UI appearance and second-session bow respawn must be tested on Meta Quest.",
            "OK");
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
                return
                    item.gameObject;
            }
        }

        return null;
    }
}
#endif