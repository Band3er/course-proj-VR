using System;
using System.Collections.Generic;
using System.Linq;

namespace ForestArchery.TimedGame
{
    public static class LeaderboardService
    {
        public static List<LeaderboardEntry> Build(
            LocalGameData data,
            TimedGameInteractionMode mode,
            int maximumEntries = 10)
        {
            List<LeaderboardEntry> entries =
                new List<LeaderboardEntry>();

            if (
                data == null ||
                data.profiles == null
            )
            {
                return entries;
            }

            foreach (PlayerProfileData profile in data.profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                ModeRecord record =
                    GetModeRecord(
                        profile,
                        mode);

                if (
                    record == null ||
                    record.roundsPlayed <= 0
                )
                {
                    continue;
                }

                entries.Add(
                    new LeaderboardEntry
                    {
                        profileId =
                            profile.profileId,
                        playerName =
                            profile.displayName,
                        bestScore =
                            record.personalBestScore,
                        bestHits =
                            record.bestHits,
                        bestShots =
                            record.bestShots,
                        bestAccuracy =
                            record.bestAccuracy,
                        achievedUtc =
                            record.achievedUtc
                    });
            }

            entries =
                entries
                    .OrderByDescending(
                        item => item.bestScore)
                    .ThenByDescending(
                        item => item.bestAccuracy)
                    .ThenByDescending(
                        item => item.bestHits)
                    .ThenBy(
                        item => item.achievedUtc,
                        StringComparer.Ordinal)
                    .Take(
                        Math.Max(
                            1,
                            maximumEntries))
                    .ToList();

            for (
                int index = 0;
                index < entries.Count;
                index++
            )
            {
                entries[index].rank =
                    index + 1;
            }

            return entries;
        }

        public static ModeRecord GetModeRecord(
            PlayerProfileData profile,
            TimedGameInteractionMode mode)
        {
            if (profile == null)
            {
                return null;
            }

            return
                mode ==
                TimedGameInteractionMode.HandTracking
                    ? profile.handTrackingRecord
                    : profile.controllerRecord;
        }
    }
}
