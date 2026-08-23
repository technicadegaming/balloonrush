# Arcade Hardware Integration

## Architecture

Gameplay does not directly read keyboard keys or serial ports. It subscribes to `ArcadeInputManager`, which combines implementations of `IArcadeIO`.

Included sources:

- `KeyboardArcadeIO` — development and backup controls
- `SerialArcadeIO` — optional Arduino-compatible serial bridge

The game remains playable if the serial device is missing or disconnected.

## Keyboard/service fallback

| Cabinet action | Keyboard fallback |
|---|---|
| Credit | `C` |
| Start / replay | `Enter` or `P` |
| Lane left | Left Arrow or `A` |
| POP | Up Arrow or `Space` |
| Lane right | Right Arrow or `D` |
| Operator Menu | `M` |
| Gameplay debug/service panel | `Escape` |

F2-F6 development actions are ignored until the Escape-controlled debug panel is open. In normal non-development cabinet builds, debug actions remain disabled unless `allowDebugShortcutsInRelease` is deliberately enabled.

## Serial protocol

Default baud: `115200`

Messages from controller to Unity, one per line:

```text
LEFT
RIGHT
POP
START
COIN
CARD
OPERATOR
BACK
```

Messages from Unity to controller:

```text
TICKETS:25
```

The supplied Arduino sketch interprets the number as the number of output pulses to generate. Unity queues additional payouts instead of cancelling a payout already in progress, and the Arduino maintains its own non-blocking pulse queue.

## Suggested Arduino pin map

| Function | Arduino pin | Mode |
|---|---:|---|
| LEFT button | 2 | INPUT_PULLUP |
| POP button | 3 | INPUT_PULLUP |
| RIGHT button | 4 | INPUT_PULLUP |
| START button | 5 | INPUT_PULLUP |
| Coin pulse | 6 | INPUT_PULLUP |
| Card swipe pulse | 7 | INPUT_PULLUP |
| Operator button | 8 | INPUT_PULLUP |
| Back/service button | 9 | INPUT_PULLUP |
| Ticket output | 10 | OUTPUT through driver/opto-isolator |

## Electrical safety

Do not connect a ticket dispenser motor, solenoid, card-reader line, or higher-voltage cabinet circuit directly to an Arduino pin.

Use the correct interface for the device:

- opto-isolator or transistor/MOSFET driver
- flyback protection for inductive loads
- shared ground only when electrically appropriate
- separate regulated logic supply
- fused cabinet power

Verify the exact pulse polarity, voltage, and pulse width required by the ticket dispenser and card system. The supplied sketch defaults to an active-low ticket output and can be changed with constants at the top of the file.

## Operator setup

1. Open the Operator Menu with `M` from Attract Mode.
2. Enable **Serial hardware**.
3. Set the Windows COM port.
4. Set baud to `115200` unless the controller firmware uses another value.
5. Set tickets per pulse to match the dispenser interface.
6. Use **TEST INPUTS**.
7. Use **TEST TICKETS** with no tickets loaded first, then repeat with tickets loaded.

## Intercard / card-reader note

Treat the card reader as a credit pulse source. Its interface must be configured according to the reader/controller documentation and isolated before entering an Arduino digital input. The game’s default `CARD` event adds the configurable `CardSwipeValue` number of credits.

## Windows reliability checklist

- Disable sleep, hibernation, screen saver, notifications, and automatic display rotation.
- Configure the cabinet display as portrait in Windows.
- Disable USB selective suspend for the controller where appropriate.
- Assign a stable COM port in Device Manager.
- Use a powered USB connection for cabinet controllers.
- Launch the game from a dedicated local Windows account.
- Test recovery after unplugging and reconnecting USB during Attract Mode and during gameplay.
