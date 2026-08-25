# Balloon Rush v1.9 - Current Serial Protocol

The current project has one SerialArcadeIO owner for the cabinet serial port.

## Recognized incoming messages
- LEFT
- RIGHT
- POP
- START
- COIN
- READER_CREDIT
- CREDIT
- CARD
- SWIPE
- OPERATOR
- BACK
- PAID:n
- PAID_TIMEOUT:n
- READY
- UNO_READY
- PONG
- TICKET_QUEUE:n
- ERR:text

## Outgoing messages used by the game
- TICKETS:n
- PING (diagnostics)

## Important payout rule
Unity must never automatically resend a TICKETS:n command after it was transmitted merely because PAID:n was lost. Physical tickets may already have dispensed. The operator must reconcile the fault manually.

## Card reader connection status
The current architecture does not expose an independent hardware heartbeat for the card reader and ticket dispenser. Both diagnostics statuses therefore derive from the shared cabinet serial link. Last recognized card-reader traffic and ticket acknowledgements are shown separately on the dashboard.

If the controller firmware is later changed to emit separate READER_READY and TICKET_READY messages, a future update can display independent heartbeat status for each device.
