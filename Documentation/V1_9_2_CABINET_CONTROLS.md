# Balloon Rush v1.9.2 - Cabinet Controls and Credit Flow

## Actual cabinet input map

| Physical function | Keyboard | USB joystick encoder |
|---|---|---|
| Left | Left Arrow / A | JoystickButton1 |
| Pop / Up / Select | Up Arrow / Space | JoystickButton2 |
| Right | Right Arrow / D | JoystickButton7 |
| Operator keyed switch | M | JoystickButton4 |
| Dev Start | Enter / P | keyboard only |
| Dev test credit | C / V | keyboard only |
| Back (dev) | Escape | keyboard only |

The exact JoystickButton mappings use Unity's legacy Input Manager API. In Player Settings, `Active Input Handling` must include **Input Manager (Old)**, normally **Both**.

## Attract / paid credit flow

- With zero credits, the game remains in Attract Mode.
- A card/reader credit is added to Balloon Rush's existing CreditManager.
- The credit display updates immediately.
- A newly received credit auto-starts after about 1 second.
- Exactly one credit is consumed by the existing AttractModeManager start path.
- If multiple credits are stacked, unused credits remain in memory.
- After Results returns to Attract, a waiting credit automatically starts the next game after about 3 seconds.
- Pressing POP while a credit is waiting starts immediately and cancels the short delay.
- Turning the keyed Operator switch cancels any pending auto-start before Operator Mode opens.

## Operator menu

- Key switch (M / JoystickButton4): open from Attract; press again to exit Operator Mode.
- LEFT / RIGHT: move highlighted setting/button.
- POP: activate, toggle, or begin editing.
- While editing: LEFT decreases, RIGHT increases, POP confirms.
- Diagnostics use the same three-button navigation.

## Why the older WOWCade code was not copied wholesale

The old code had useful credit-flow behavior, but Balloon Rush already has a safer separation of responsibilities:

- CreditManager owns credits.
- SerialArcadeIO owns the shared cabinet serial port.
- TicketManager queues TICKETS:n, waits for PAID:n, audits payouts, and avoids unsafe automatic re-payments.
- SessionAuditLogger records per-game economy data.

v1.9.2 ports the useful auto-start behavior without replacing those newer systems.
