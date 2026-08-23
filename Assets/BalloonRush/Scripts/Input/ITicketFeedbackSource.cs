using System;

namespace BalloonRush.Input
{
    /// <summary>
    /// Optional feedback contract implemented by cabinet I/O devices that can
    /// acknowledge a batch TICKETS:n request.
    /// </summary>
    public interface ITicketFeedbackSource
    {
        event Action<int> TicketsPaid;
        event Action<int> TicketPayoutTimedOut;
        event Action<string> HardwareError;
    }
}
