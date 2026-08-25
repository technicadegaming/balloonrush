# v1.9.4 Counter semantics

- **Current credits**: paid/unplayed credit units presently available to start games.
- **Pending tickets**: ticket payout units still represented by TicketManager as pending/active. A command already transmitted to hardware is never force-cancelled by the new clear button.
- **Lifetime credits**: `MachineStatistics.totalCredits`. This is an operator-resettable counter. Clearing it does not erase card swipes, coin pulses, or revenue.
- **Lifetime tickets awarded**: `MachineStatistics.totalTicketsAwarded`.
- **Lifetime tickets paid**: `MachineStatistics.totalTicketsPaid`, based on confirmed `PAID:n` hardware acknowledgements.

The existing full **RESET STATISTICS** action remains available when the operator deliberately wants to clear all machine statistics.
