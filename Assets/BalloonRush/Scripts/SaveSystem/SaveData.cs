using System;

namespace BalloonRush.SaveSystem
{
    [Serializable]
    public sealed class HighScoreData
    {
        public int topScore;
        public int highestCombo;
        public int mostTickets;
        public int jackpotsWon;
    }

    [Serializable]
    public sealed class MachineStatistics
    {
        public int gamesPlayed;
        public int totalCredits;
        public int cardSwipes;
        public int coinPulses;
        public long totalRevenueCents;
        public long totalTicketsAwarded;
        public long totalTicketsPaid;
        public int ticketPayoutFailures;
        public int ticketPayoutMismatches;
        public int jackpotsWon;
        public long perfectPops;
        public long totalBalloonsPopped;
        public long bombsHit;

        public float AverageTicketsPerGame => gamesPlayed > 0 ? (float)totalTicketsAwarded / gamesPlayed : 0f;
        public float AverageTicketsPaidPerGame => gamesPlayed > 0 ? (float)totalTicketsPaid / gamesPlayed : 0f;
        public float AverageRevenuePerGame => gamesPlayed > 0 ? totalRevenueCents / 100f / gamesPlayed : 0f;

        public void Reset()
        {
            gamesPlayed = 0;
            totalCredits = 0;
            cardSwipes = 0;
            coinPulses = 0;
            totalRevenueCents = 0;
            totalTicketsAwarded = 0;
            totalTicketsPaid = 0;
            ticketPayoutFailures = 0;
            ticketPayoutMismatches = 0;
            jackpotsWon = 0;
            perfectPops = 0;
            totalBalloonsPopped = 0;
            bombsHit = 0;
        }
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 3;
        public OperatorSettings settings = new OperatorSettings();
        public HighScoreData highScores = new HighScoreData();
        public MachineStatistics statistics = new MachineStatistics();
    }
}
