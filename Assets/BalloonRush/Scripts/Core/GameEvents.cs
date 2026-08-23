using System;
using BalloonRush.Gameplay;

namespace BalloonRush.Core
{
    public static class GameEvents
    {
        public static event Action GameStarted;
        public static event Action<GameSessionResult> GameEnded;
        public static event Action<Balloon, TimingRating> BalloonPopped;
        public static event Action<TimingRating> TimingJudged;
        public static event Action<int> ComboChanged;
        public static event Action<int> TicketsChanged;
        public static event Action<int> ScoreChanged;
        public static event Action GoldenBalloonPopped;
        public static event Action GoldenRoundStarted;
        public static event Action GoldenRoundEnded;
        public static event Action<int> JackpotWon;

        public static void RaiseGameStarted() => GameStarted?.Invoke();
        public static void RaiseGameEnded(GameSessionResult result) => GameEnded?.Invoke(result);
        public static void RaiseBalloonPopped(Balloon balloon, TimingRating rating) => BalloonPopped?.Invoke(balloon, rating);
        public static void RaiseTimingJudged(TimingRating rating) => TimingJudged?.Invoke(rating);
        public static void RaiseComboChanged(int combo) => ComboChanged?.Invoke(combo);
        public static void RaiseTicketsChanged(int tickets) => TicketsChanged?.Invoke(tickets);
        public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
        public static void RaiseGoldenBalloonPopped() => GoldenBalloonPopped?.Invoke();
        public static void RaiseGoldenRoundStarted() => GoldenRoundStarted?.Invoke();
        public static void RaiseGoldenRoundEnded() => GoldenRoundEnded?.Invoke();
        public static void RaiseJackpotWon(int tickets) => JackpotWon?.Invoke(tickets);

        public static void ClearAll()
        {
            GameStarted = null;
            GameEnded = null;
            BalloonPopped = null;
            TimingJudged = null;
            ComboChanged = null;
            TicketsChanged = null;
            ScoreChanged = null;
            GoldenBalloonPopped = null;
            GoldenRoundStarted = null;
            GoldenRoundEnded = null;
            JackpotWon = null;
        }
    }
}
