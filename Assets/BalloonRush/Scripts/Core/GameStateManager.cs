using System;
using UnityEngine;

namespace BalloonRush.Core
{
    public sealed class GameStateManager : MonoBehaviour
    {
        public GameState CurrentState { get; private set; } = GameState.Boot;
        public event Action<GameState, GameState> StateChanged;

        public void ChangeState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            GameState previous = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previous, nextState);
        }
    }
}
