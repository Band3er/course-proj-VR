using System;

namespace ForestArchery.TimedGame
{
    public enum TimedRoundState
    {
        Idle = 0,
        Countdown = 1,
        Playing = 2,
        Results = 3,
        Cancelled = 4,
        Paused = 5
    }

    [Serializable]
    public sealed class TimedRoundResult
    {
        public string profileId = string.Empty;
        public TimedGameInteractionMode interactionMode =
            TimedGameInteractionMode.Controller;

        public int score;
        public int hits;
        public int arrowsLaunched;
        public int misses;
        public float accuracy;
        public float averageScorePerArrow;
        public int durationSeconds;
    }
}
