using System;
using System.Collections.Generic;

namespace ForestArchery.TimedGame
{
    public enum TimedGameInteractionMode
    {
        Controller = 0,
        HandTracking = 1
    }

    [Serializable]
    public sealed class LocalGameData
    {
        public int schemaVersion = 1;
        public string selectedProfileId = string.Empty;
        public List<PlayerProfileData> profiles =
            new List<PlayerProfileData>();
    }

    [Serializable]
    public sealed class PlayerProfileData
    {
        public string profileId = string.Empty;
        public string displayName = string.Empty;
        public string createdUtc = string.Empty;

        public ModeRecord controllerRecord =
            new ModeRecord();

        public ModeRecord handTrackingRecord =
            new ModeRecord();

        public List<RoundRecord> recentRounds =
            new List<RoundRecord>();
    }

    [Serializable]
    public sealed class ModeRecord
    {
        public int personalBestScore;
        public int bestHits;
        public int bestShots;
        public float bestAccuracy;
        public string achievedUtc = string.Empty;
        public int roundsPlayed;
    }

    [Serializable]
    public sealed class RoundRecord
    {
        public string roundId = string.Empty;
        public string profileId = string.Empty;
        public string playerName = string.Empty;
        public TimedGameInteractionMode interactionMode =
            TimedGameInteractionMode.Controller;

        public int score;
        public int hits;
        public int arrowsLaunched;
        public int misses;
        public float accuracy;
        public float averageScorePerArrow;
        public int durationSeconds;
        public string completedUtc = string.Empty;
    }

    public sealed class LeaderboardEntry
    {
        public int rank;
        public string profileId = string.Empty;
        public string playerName = string.Empty;
        public int bestScore;
        public int bestHits;
        public int bestShots;
        public float bestAccuracy;
        public string achievedUtc = string.Empty;
    }
}
