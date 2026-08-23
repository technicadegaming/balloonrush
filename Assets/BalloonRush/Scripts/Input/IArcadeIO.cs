using System;

namespace BalloonRush.Input
{
    public enum CreditPulseType
    {
        Coin,
        CardSwipe
    }

    public interface IArcadeIO
    {
        event Action LeftPressed;
        event Action RightPressed;
        event Action PopPressed;
        event Action StartPressed;
        event Action<CreditPulseType> CreditPulse;
        event Action OperatorPressed;
        event Action BackPressed;

        bool IsAvailable { get; }
        void StartIO();
        void StopIO();
        void SendTicketPulse(int ticketCount);
    }
}
