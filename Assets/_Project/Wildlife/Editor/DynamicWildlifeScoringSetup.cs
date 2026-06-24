using System;
using System.Collections.Generic;
using ForestArchery.Wildlife;
using UnityEditor;
using UnityEngine;

public static class DynamicWildlifeScoringSetup
{
    [MenuItem(
        "Tools/Forest Archery/Wildlife/Stage 07 - Configure Dynamic Scoring")]
    public static void Configure()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:WildlifeSpeciesDefinition",
                new[] { "Assets" });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Dynamic Scoring",
                "No WildlifeSpeciesDefinition assets were found.",
                "OK");

            return;
        }

        List<string> configured =
            new List<string>();

        bool rabbitFound = false;
        bool deerFound = false;
        bool birdFound = false;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            WildlifeSpeciesDefinition definition =
                AssetDatabase.LoadAssetAtPath
                    <WildlifeSpeciesDefinition>(
                        path);

            if (definition == null)
            {
                continue;
            }

            string identity =
                (
                    definition.speciesId +
                    " " +
                    definition.displayName +
                    " " +
                    path
                )
                .ToLowerInvariant();

            if (
                identity.Contains("deer") ||
                identity.Contains("stag")
            )
            {
                definition.baseScore = 60;
                definition.headshotMultiplier = 1.35f;
                definition.movingScoreMultiplier = 1.25f;
                definition.airborneScoreMultiplier = 1.40f;

                deerFound = true;

                configured.Add(
                    "DEER | base=60 | head=x1.35 | moving=x1.25 | " +
                    path);
            }
            else if (
                identity.Contains("rabbit") ||
                identity.Contains("bunny")
            )
            {
                definition.baseScore = 100;
                definition.headshotMultiplier = 1.50f;
                definition.movingScoreMultiplier = 1.25f;
                definition.airborneScoreMultiplier = 1.40f;

                rabbitFound = true;

                configured.Add(
                    "RABBIT | base=100 | head=x1.50 | moving=x1.25 | " +
                    path);
            }
            else if (
                identity.Contains("bird") ||
                definition.movementMode ==
                    WildlifeMovementMode.Flying
            )
            {
                definition.baseScore = 150;
                definition.headshotMultiplier = 1.50f;
                definition.movingScoreMultiplier = 1.25f;
                definition.airborneScoreMultiplier = 1.40f;

                birdFound = true;

                configured.Add(
                    "BIRD | base=150 | moving=x1.25 | flying=x1.40 | " +
                    path);
            }
            else
            {
                continue;
            }

            EditorUtility.SetDirty(
                definition);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary =
            "Dynamic wildlife scoring configured.\n\n" +
            string.Join(
                "\n",
                configured) +
            "\n\nFound:\n" +
            "Rabbit: " + rabbitFound + "\n" +
            "Deer: " + deerFound + "\n" +
            "Bird: " + birdFound + "\n\n" +
            "Distance multipliers:\n" +
            "0-5 m: x1.00\n" +
            "5-10 m: x1.10\n" +
            "10-15 m: x1.25\n" +
            "15-20 m: x1.40\n" +
            "20+ m: x1.55";

        Debug.Log(
            "[WILDLIFE SCORE] Configuration complete\n" +
            summary);

        EditorUtility.DisplayDialog(
            "Dynamic Scoring Configured",
            summary,
            "OK");
    }
}
