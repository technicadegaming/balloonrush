# Testing and Troubleshooting

## Unity preflight

After running the project builder, run:

`Tools > Balloon Rush > Validate Generated Project`

The same preflight is executed automatically after generation and before every Unity player build. Treat any preflight error as a build blocker. Warnings still require review before cabinet deployment.

## Automated tests

Open:

`Window > General > Test Runner`

Run all Edit Mode tests in `BalloonRush.Tests.Editor`.

Covered systems include:

- timing window ratings
- combo build/reset
- timing and combo ticket multiplier ordering
- ticket multiplier math
- ticket maximum clamp
- conservative default profile guardrails
- input debounce clamp
- normal balloon reward
- Perfect Golden final-balloon jackpot

## Manual play-test checklist

### Startup and flow

- [ ] Boot loads Attract Mode automatically.
- [ ] C adds one credit.
- [ ] Enter or P cannot start without credit unless Free Play is enabled.
- [ ] One play consumes the configured credits.
- [ ] Countdown displays 3, 2, 1, POP.
- [ ] The default round lasts approximately 35 seconds before any Golden extension.
- [ ] Results return to Attract Mode after timeout.
- [ ] Play Again requires another credit.
- [ ] Results show a new-record callout or a score-gap replay message.
- [ ] `M` opens Operator Settings from Attract, Main Game, and Results.
- [ ] `Escape` opens/closes the gameplay debug panel and returns from Results/Operator menus as documented.

### Gameplay

- [ ] Left Arrow and `A` both move left; Right Arrow and `D` both move right.
- [ ] Up Arrow and `Space` both POP.
- [ ] LEFT and RIGHT cannot move beyond three lanes.
- [ ] POP feels immediate.
- [ ] A single physical press does not create duplicate actions at the 25 ms default debounce.
- [ ] Perfect, Great, Good, and Miss are distinguishable.
- [ ] Green and Blue rewards use operator values.
- [ ] Green and Blue spawn weights respond to operator changes.
- [ ] GOOD/GREAT/PERFECT ticket multipliers respond to operator changes and remain ordered after validation.
- [ ] x2 starts and expires after the configured duration.
- [ ] A Super Bomb cancels active x2 and clearly reports the loss.
- [ ] Mystery ticket outcomes remain within the operator minimum/maximum unless the Golden chance is selected.
- [ ] Bombs break the combo and play a clear warning.
- [ ] Passed reward balloons follow the configured combo behavior.
- [ ] Rush Mode begins in the last five seconds.
- [ ] Timing feedback appears above the Hit Zone and does not cover the Hit Zone label.
- [ ] The Console does not report missing star, skull, arrow, or crown glyphs from generated UI.
- [ ] F2-F6 do nothing until the Escape debug panel is visible.

### Golden round and jackpot

- [ ] A Golden Balloon starts the bonus only when no Golden Round is already active.
- [ ] Normal bombs are suppressed during Golden Round.
- [ ] Bonus-mode Green, Blue, Mystery, and x2 frequency responds to operator weights.
- [ ] The final crown balloon spawns with enough lead time to reach the Hit Zone at the minimum configured speed.
- [ ] At zero seconds, Golden Round waits for the final crown balloon to be hit or pass before resolving.
- [ ] Perfect awards the configured jackpot.
- [ ] Great, Good, and Miss award configured consolation values.
- [ ] Total tickets never exceed 1,000.

### Operator, save data, and audit

- [ ] Every field can be edited and saved.
- [ ] Invalid values are clamped safely.
- [ ] Settings survive a restart.
- [ ] Version 1 save data migrates without losing existing operator values.
- [ ] A valid previous-good `.bak` save is recovered when the primary JSON is corrupt.
- [ ] Statistics increment correctly.
- [ ] Reset Statistics requires confirmation.
- [ ] `BalloonRushAudit/sessions.csv` receives one row per completed game.
- [ ] Existing older-schema audit CSV is moved to a timestamped `sessions-legacy-*.csv` file.
- [ ] New high-score and ticket-record flags match the results screen.
- [ ] Runtime errors are appended to `BalloonRushRuntime.log` and the log rotates at the configured size.

### Hardware

- [ ] Missing COM port does not crash.
- [ ] Disconnecting USB does not freeze gameplay.
- [ ] Reconnecting the controller restores input after retry.
- [ ] Ticket output is non-blocking.
- [ ] Exactly the requested number of pulses is produced.
- [ ] Starting another game while tickets are still dispensing does not discard the remaining payout.
- [ ] Multiple payouts queue instead of replacing the active payout.
- [ ] Returning to Attract Mode does not stop physical ticket output.
- [ ] Switch bounce does not create duplicate coin, card, START, LEFT, RIGHT, or POP events.

### Soak and release testing

- [ ] Run 100 consecutive rounds with hardware disabled.
- [ ] Run 100 consecutive rounds with serial hardware connected.
- [ ] Unplug/reconnect the serial controller during Attract and gameplay.
- [ ] Verify pool counts return to normal after each game.
- [ ] Verify memory usage does not climb continuously.
- [ ] Reconcile total audit tickets against physical dispenser counts.
- [ ] Test Windows restart and cabinet auto-launch.

## Static validation

Run from the repository root:

`python BuildScripts/validate-source.py`

The validator checks repository completeness, JSON/assembly-definition syntax, balanced C# delimiters and preprocessor blocks, duplicate type declarations, placeholder markers, payout safety paths, versioned production safeguards, preflight integration, required documentation, reference art, and hardware files.

Static validation does not replace Unity compilation, Test Runner, scene execution, or physical cabinet testing. See `BUILD_VALIDATION_REPORT.md`.

## Common problems

### No generated scenes

Run `Tools > Balloon Rush > Build Complete Game` and wait for Asset Database refresh to finish. The builder runs preflight automatically; correct every reported error before pressing Play.

### Text is missing or pink

Import TextMesh Pro Essential Resources, then run the builder again.

### Keyboard input does nothing

Check `Edit > Project Settings > Player > Active Input Handling`. Enable the Input System or use **Both**, then restart Unity if prompted.

### Serial port unavailable

- confirm the COM port in Device Manager
- close Arduino Serial Monitor and other programs using the port
- confirm baud rate
- unplug/reconnect the controller
- keep hardware disabled while testing normal gameplay
- if the player backend does not expose `System.IO.Ports`, use the keyboard mode to verify gameplay and test a Windows Mono build for the cabinet

### Ticket dispenser produces the wrong count

Confirm whether the hardware expects one ticket per pulse, one pulse per group, active-high or active-low output, and the required pulse interval. Adjust `TicketsPerPulse`, `PulseDelay`, and the Arduino sketch constants.

### Payout seems too high or too low

1. Export a payout simulation for weak, average, and strong assumptions.
2. Review at least 500 rows from `sessions.csv`.
3. Change only one group at a time.
4. Start with spawn weights before changing base ticket values.
5. Keep Jackpot at 500 and the maximum at 1,000 or lower.

### Debug shortcuts do not work in a cabinet release

This is intentional. In the Editor and Development Builds, press `Escape` during gameplay to open the debug/service panel. F2-F6 only perform debug actions while that panel is visible. To expose the panel temporarily in a normal release, enable `allowDebugShortcutsInRelease` on `BalloonRushConfig`, rebuild, and disable it again before deployment.

### Pool exhausted warning

Increase `balloonPoolSize` in `BalloonRushConfig`, or reduce peak spawn frequency. The game skips a spawn rather than allocating during gameplay.

### Save file reset

The primary save is `BalloonRushSave.json` under `Application.persistentDataPath`. The previous good copy is `BalloonRushSave.json.bak`. A malformed primary file is also copied with a `.corrupt-<timestamp>` suffix before backup recovery or safe defaults are attempted.
