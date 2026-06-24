using System;
using System.Collections.Generic;
using System.IO;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEngine;

public static class TimedGameDataSelfTest
{
    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08b - Run Local Data Self-Test")]
    public static void Run()
    {
        string testDirectory =
            Path.Combine(
                Application.temporaryCachePath,
                "ForestArcheryTimedGameDataSelfTest_" +
                Guid.NewGuid().ToString("N"));

        try
        {
            LocalProfileRepository repository =
                new LocalProfileRepository(
                    testDirectory);

            LocalGameData data =
                repository.LoadOrCreate();

            Require(
                data != null,
                "LoadOrCreate returned null.");

            Require(
                data.profiles.Count == 0,
                "Fresh test storage was not empty.");

            PlayerProfileData valentina =
                repository.CreateProfile(
                    "Valentina");

            PlayerProfileData p01 =
                repository.CreateProfile(
                    "P01");

            PlayerProfileData ioan =
                repository.CreateProfile(
                    "Ioan");

            Require(
                repository.GetSelectedProfile().profileId ==
                ioan.profileId,
                "The latest created profile was not selected.");

            bool duplicateRejected =
                false;

            try
            {
                repository.CreateProfile(
                    "valentina");
            }
            catch (InvalidOperationException)
            {
                duplicateRejected =
                    true;
            }

            Require(
                duplicateRejected,
                "Case-insensitive duplicate profile names were not rejected.");

            repository.RecordRound(
                valentina.profileId,
                TimedGameInteractionMode.Controller,
                4200,
                15,
                25,
                300);

            repository.RecordRound(
                valentina.profileId,
                TimedGameInteractionMode.Controller,
                4800,
                16,
                24,
                300);

            repository.RecordRound(
                valentina.profileId,
                TimedGameInteractionMode.HandTracking,
                3300,
                12,
                25,
                300);

            repository.RecordRound(
                p01.profileId,
                TimedGameInteractionMode.Controller,
                5100,
                17,
                28,
                300);

            repository.RecordRound(
                ioan.profileId,
                TimedGameInteractionMode.Controller,
                3900,
                14,
                22,
                300);

            repository.SelectProfile(
                valentina.profileId);

            LocalProfileRepository reloadedRepository =
                new LocalProfileRepository(
                    testDirectory);

            LocalGameData reloaded =
                reloadedRepository.LoadOrCreate();

            Require(
                reloaded.profiles.Count == 3,
                "Profiles were not preserved after reload.");

            PlayerProfileData reloadedValentina =
                reloadedRepository.FindProfile(
                    valentina.profileId);

            Require(
                reloadedValentina != null,
                "Valentina profile was not found after reload.");

            Require(
                reloadedValentina.controllerRecord.personalBestScore ==
                4800,
                "Controller personal best was not stored correctly.");

            Require(
                reloadedValentina.handTrackingRecord.personalBestScore ==
                3300,
                "Hand Tracking personal best was not stored separately.");

            Require(
                reloadedRepository.GetSelectedProfile().profileId ==
                valentina.profileId,
                "Selected profile was not preserved after reload.");

            List<LeaderboardEntry> controllerBoard =
                LeaderboardService.Build(
                    reloaded,
                    TimedGameInteractionMode.Controller,
                    10);

            Require(
                controllerBoard.Count == 3,
                "Controller leaderboard entry count is incorrect.");

            Require(
                controllerBoard[0].playerName == "P01" &&
                controllerBoard[0].bestScore == 5100,
                "Controller leaderboard first place is incorrect.");

            Require(
                controllerBoard[1].playerName == "Valentina" &&
                controllerBoard[1].bestScore == 4800,
                "Controller leaderboard second place is incorrect.");

            List<LeaderboardEntry> handBoard =
                LeaderboardService.Build(
                    reloaded,
                    TimedGameInteractionMode.HandTracking,
                    10);

            Require(
                handBoard.Count == 1,
                "Hand Tracking leaderboard was not separated correctly.");

            Require(
                handBoard[0].playerName == "Valentina" &&
                handBoard[0].bestScore == 3300,
                "Hand Tracking leaderboard content is incorrect.");

            string jsonBeforeReset =
                File.ReadAllText(
                    reloadedRepository.MainFilePath);

            Require(
                jsonBeforeReset.Contains("Valentina") &&
                jsonBeforeReset.Contains("P01") &&
                jsonBeforeReset.Contains("Ioan"),
                "Saved JSON does not contain the expected profiles.");

            reloadedRepository.DeleteAllData();

            Require(
                !File.Exists(
                    reloadedRepository.MainFilePath),
                "Main JSON file still exists after reset.");

            Require(
                !File.Exists(
                    reloadedRepository.BackupFilePath),
                "Backup file still exists after reset.");

            Debug.Log(
                "[TIMED GAME DATA] Stage 08b self-test passed." +
                "\nProfiles: create/select/reload OK" +
                "\nController personal best: OK" +
                "\nHand Tracking personal best: separate OK" +
                "\nLeaderboards: separate and sorted OK" +
                "\nJSON save/verification: OK" +
                "\nReset local data: OK");

            EditorUtility.DisplayDialog(
                "Stage 08b Self-Test Passed",
                "Local profiles, JSON persistence, separate leaderboards, " +
                "personal best records, and reset were validated.\n\n" +
                "No scene or gameplay object was modified.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08b Self-Test Failed",
                exception.Message +
                "\n\nTest folder:\n" +
                testDirectory,
                "OK");
        }
        finally
        {
            try
            {
                if (
                    Directory.Exists(
                        testDirectory)
                )
                {
                    Directory.Delete(
                        testDirectory,
                        true);
                }
            }
            catch (Exception cleanupException)
            {
                Debug.LogWarning(
                    "[TIMED GAME DATA] Test cleanup warning: " +
                    cleanupException.Message);
            }
        }
    }

    private static void Require(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                message);
        }
    }
}
