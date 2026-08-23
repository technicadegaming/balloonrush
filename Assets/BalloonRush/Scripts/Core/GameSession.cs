using System;

namespace BalloonRush.Core
{
    [Serializable]
    public sealed class GameSessionResult
    {
        public string sessionId;
        public string startedUtc;
        public string endedUtc;
        public int creditsRemaining;
        public int previousTopScore;
        public int previousMostTickets;
        public bool newHighScore;
        public bool newTicketRecord;
        public int score;
        public int tickets;
        public int regularTickets;
        public int bonusTickets;
        public int jackpotTickets;
        public int pricePerPlayCents = 100;
        public int highestCombo;
        public int perfectPops;
        public int greatPops;
        public int goodPops;
        public int misses;
        public int balloonsPopped;
        public int bombsHit;
        public int goldenBalloons;
        public bool jackpotWon;
        public float gameDuration;
    }

    public static class GameSession
    {
        public static GameSessionResult LastResult { get; set; } = new GameSessionResult();
    }
}
