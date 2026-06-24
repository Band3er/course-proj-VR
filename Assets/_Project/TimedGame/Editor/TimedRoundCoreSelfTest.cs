using System;
using System.Collections.Generic;
using System.IO;
using ForestArchery.TimedGame;
using UnityEditor;
using UnityEngine;

public static class TimedRoundCoreSelfTest
{
    [MenuItem(
        "Tools/Forest Archery/Timed Game/Stage 08c - Run Timed Round Core Self-Test")]
    public static void Run()
    {
        string testDirectory =
            Path.Combine(
                Application.temporaryCachePath,
                "ForestArcheryTimedRoundSelfTest_" +
                Guid.NewGuid().ToString("N"));

        try
        {
            TimedRoundSession session =
                new TimedRoundSession();

            List<TimedRoundState> states =
                new List<TimedRoundState>();

            List<int> countdownValues =
                new List<int>();

            List<int> timeValues =
                new List<int>();

            List<string> messages =
                new List<string>();

            TimedRoundResult endedResult =
                null;

            session.StateChanged +=
                states.Add;

            session.CountdownChanged +=
                countdownValues.Add;

            session.TimeChanged +=
                timeValues.Add;

            session.MessageRequested +=
                messages.Add;

            session.RoundEnded +=
                result =>
                    endedResult = result;

            session.BeginRound(
                "PROFILE_TEST",
                TimedGameInteractionMode.Controller,
                12f,
                3f);

            Require(
                session.State ==
                    TimedRoundState.Countdown,
                "The round did not enter Countdown.");

            Require(
                !session.IsGameplayAllowed &&
                !session.IsScoringAllowed,
                "Gameplay or scoring was allowed during Countdown.");

            session.RegisterArrowLaunched();
            session.UpdateScoreSnapshot(
                999,
                9);

            Require(
                session.ArrowsLaunched == 0 &&
                session.CurrentScore == 0 &&
                session.CurrentHits == 0,
                "Countdown accepted gameplay statistics.");

            session.Tick(1.01f);
            session.Tick(1.01f);
            session.Tick(1.01f);

            Require(
                session.State ==
                    TimedRoundState.Playing,
                "The round did not enter Playing.");

            Require(
                session.IsGameplayAllowed &&
                session.IsScoringAllowed,
                "Gameplay and scoring were not enabled in Playing.");

            Require(
                countdownValues.Contains(3) &&
                countdownValues.Contains(2) &&
                countdownValues.Contains(1) &&
                countdownValues.Contains(0),
                "Countdown values were incomplete.");

            Require(
                messages.Contains("GO!"),
                "GO message was not emitted.");

            for (
                int index = 0;
                index < 5;
                index++
            )
            {
                session.RegisterArrowLaunched();
            }

            session.UpdateScoreSnapshot(
                1500,
                3);

            session.Tick(2.1f);

            Require(
                messages.Contains(
                    "10 SECONDS!"),
                "10-second warning was not emitted.");

            while (
                session.State ==
                TimedRoundState.Playing
            )
            {
                session.Tick(
                    1.01f);
            }

            Require(
                session.State ==
                    TimedRoundState.Results,
                "The round did not enter Results.");

            Require(
                !session.IsGameplayAllowed &&
                !session.IsScoringAllowed,
                "Gameplay or scoring remained enabled in Results.");

            Require(
                messages.Contains("5") &&
                messages.Contains("4") &&
                messages.Contains("3") &&
                messages.Contains("2") &&
                messages.Contains("1"),
                "Final five-second messages were incomplete.");

            Require(
                messages.Contains(
                    "TIME'S UP!"),
                "TIME'S UP message was not emitted.");

            Require(
                endedResult != null,
                "RoundEnded did not provide a result.");

            Require(
                endedResult.score == 1500,
                "Final score is incorrect.");

            Require(
                endedResult.hits == 3,
                "Final hits are incorrect.");

            Require(
                endedResult.arrowsLaunched == 5,
                "Final arrows launched are incorrect.");

            Require(
                endedResult.misses == 2,
                "Final misses are incorrect.");

            Require(
                Mathf.Abs(
                    endedResult.accuracy -
                    60f) <
                0.001f,
                "Final accuracy is incorrect.");

            Require(
                Mathf.Abs(
                    endedResult.averageScorePerArrow -
                    300f) <
                0.001f,
                "Average score per arrow is incorrect.");

            int scoreAfterEnd =
                session.CurrentScore;

            int arrowsAfterEnd =
                session.ArrowsLaunched;

            session.UpdateScoreSnapshot(
                9999,
                99);

            session.RegisterArrowLaunched();

            Require(
                session.CurrentScore ==
                    scoreAfterEnd &&
                session.ArrowsLaunched ==
                    arrowsAfterEnd,
                "Statistics changed after the round ended.");

            LocalProfileRepository repository =
                new LocalProfileRepository(
                    testDirectory);

            repository.LoadOrCreate();

            PlayerProfileData profile =
                repository.CreateProfile(
                    "P01");

            RoundRecord savedRound =
                repository.RecordRound(
                    profile.profileId,
                    endedResult.interactionMode,
                    endedResult.score,
                    endedResult.hits,
                    endedResult.arrowsLaunched,
                    endedResult.durationSeconds);

            Require(
                savedRound.score == 1500 &&
                savedRound.misses == 2,
                "Round result was not stored correctly.");

            Require(
                profile.controllerRecord.personalBestScore ==
                    1500,
                "Round result did not update the Controller personal best.");

            session.AcknowledgeResults();

            Require(
                session.State ==
                    TimedRoundState.Idle,
                "AcknowledgeResults did not return to Idle.");

            TimedRoundSession immediateSession =
                new TimedRoundSession();

            immediateSession.BeginRound(
                "PROFILE_TEST_2",
                TimedGameInteractionMode.HandTracking,
                5f,
                0f);

            Require(
                immediateSession.State ==
                    TimedRoundState.Playing,
                "Zero-countdown round did not start immediately.");

            Require(
                states.Contains(
                    TimedRoundState.Countdown) &&
                states.Contains(
                    TimedRoundState.Playing) &&
                states.Contains(
                    TimedRoundState.Results),
                "Expected state transitions were not recorded.");

            Debug.Log(
                "[TIMED ROUND] Stage 08c core self-test passed." +
                "\nCountdown 3-2-1-GO: OK" +
                "\nUnscaled tick state machine: OK" +
                "\nGameplay/scoring gates: OK" +
                "\n12-second simulated round: OK" +
                "\n10-second and final 5-second messages: OK" +
                "\nTIME'S UP and frozen result: OK" +
                "\nScore/hits/arrows/misses/accuracy: OK" +
                "\nLocal profile result integration: OK" +
                "\nDefault production duration in controller: 300 seconds");

            EditorUtility.DisplayDialog(
                "Stage 08c Self-Test Passed",
                "Countdown, timer, warning messages, gameplay/scoring gates, " +
                "round statistics, final result, and profile integration were validated.\n\n" +
                "No scene or existing gameplay script was modified.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception);

            EditorUtility.DisplayDialog(
                "Stage 08c Self-Test Failed",
                exception.Message +
                "\n\nTemporary test folder:\n" +
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
                    "[TIMED ROUND] Test cleanup warning: " +
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
