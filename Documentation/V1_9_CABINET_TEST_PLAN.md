# Balloon Rush v1.9 - Cabinet Integration Test Plan

## Purpose
v1.9 adds a persistent cabinet diagnostics service and an Operator Menu dashboard without changing gameplay, scoring, payout math, or pricing.

## Open the dashboard
1. Start the game from Boot.
2. Press M to enter Operator Menu.
3. Cabinet Diagnostics opens automatically.
4. Close it to reach the existing Operator Settings. A CABINET DIAGNOSTICS button remains available to reopen it.

## Connection indicators
The current cabinet architecture uses one serial link for controls, card-reader credit messages, and ticket commands. Therefore Card Reader and Ticket Controller connection status both derive from that shared serial link.

- CABINET SERIAL: serial port open/closed.
- CARD READER: shared serial link state plus last recognized card/coin pulse.
- TICKET CTRL: shared serial state plus ticket payout fault/busy status.
- PAYOUT: idle, pending, or fault.

## Safe service tests
- +1 TEST CREDIT: adds one credit without recording revenue.
- 1 / 5 / 10 TICKET TEST: physical tests only. They refuse to run if hardware is disabled, disconnected, busy, or has an unresolved payout fault.
- RECONNECT SERIAL: stops and restarts SerialArcadeIO using the current Operator port/baud settings.
- PING CONTROLLER: queues the existing PING protocol command. Connection status remains based on the serial port's open state.
- CLEAR ERROR DISPLAY: clears the diagnostics display only. It intentionally does NOT clear a potentially paid ticket fault.

## Ticket fault safety
Never automatically retry a ticket request that was already transmitted. A missing PAID:n acknowledgement does not prove the physical tickets did not dispense.

Use the existing Operator Menu TEST TICKETS control to review/clear an unresolved payout fault.

## Real $1 swipe test
1. Confirm Price per play = 100 cents.
2. Confirm Credits per play = 1.
3. Confirm Card swipe value = 1.
4. Enable hardware and set the correct COM port/115200 baud.
5. Swipe once.
6. Dashboard should show CARD as a recent input and last reader message as READER_CREDIT / CARD SWIPE.
7. Credits should increase by exactly 1.
8. Start one game. Credits should decrease by exactly 1.
9. Repeat with two quick swipes and verify two credits are received, subject to the configured card debounce.

## Real ticket test
1. Resolve all prior payout faults first.
2. Press 1 TICKET TEST and physically count one ticket/pulse.
3. Verify Last Ticket Command shows TICKETS:1.
4. Verify Last PAID shows PAID:1 and payout returns to IDLE.
5. Repeat with 5 and 10.
6. Disconnect serial and confirm tests refuse to queue.
7. Reconnect serial and confirm state returns to CONNECTED.

## Soak/economy testing
The dashboard reads the existing sessions.csv audit log and displays:
- average score
- highest score
- average/highest combo
- highest session ticket payout
- PERFECT/GREAT/GOOD/MISS totals
- accuracy

The existing lifetime save statistics display:
- games
- swipes
- revenue
- tickets awarded
- confirmed tickets paid
- average tickets per game
- estimated prize cost
- payout failures/mismatches
- jackpots

Recommended before cabinet release: at least 50-100 complete games across weak, average, and strong players.

## Diagnostic files
Under Application.persistentDataPath/BalloonRushAudit:
- sessions.csv - per-game economy/balance audit
- ticket-payouts.csv - ticket transaction audit
- cabinet-diagnostics.csv - serial/credit/payout diagnostic events added in v1.9

Runtime errors remain in BalloonRushRuntime.log.
