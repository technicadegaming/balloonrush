# Balloon Rush — Improved Production Specification

This document records the practical improvements applied while converting the original design brief into the deliverable Unity foundation. The original brief remains preserved in `BALLOON_RUSH_COMPLETE_SPEC.md`.

## 1. Commercial payout guardrails

- Jackpot is configurable but hard-clamped to **500 tickets**.
- Total game payout is hard-clamped to **1,000 tickets**.
- The cap is enforced in operator validation, live score accumulation, result finalization, and physical dispensing.
- Golden Round consolation awards remain independently configurable below the jackpot.
- Timing, combo, Green, Blue, Mystery, Golden, and all spawn-weight values can be rebalanced without changing code.
- A conservative 35-second floor-test profile replaces the original high-payout defaults.
- `Tools > Balloon Rush > Payout Simulator` estimates average payout, percentiles, jackpot frequency, and cap frequency before floor testing.

## 2. Skill-first repeat-play loop

- LEFT and RIGHT select one of three lanes; POP judges the closest eligible balloon in the selected lane.
- Timing grades are based on normalized distance from the center of the Hit Zone.
- Perfect, Great, and Good windows are operator-configurable and remain ordered after validation.
- Timing ticket multipliers are separately operator-configurable and remain ordered after validation.
- Combo and ticket scaling are separate from competitive score, allowing strong visual progression without uncontrolled redemption payout.
- Results identify new score/ticket records or show the exact score gap to the machine high score.
- Input is centrally debounced so cabinet switch bounce cannot create duplicate moves, pops, credits, or starts.

## 3. Golden Round reliability

- Golden Round reserves time for a final crown balloon.
- Once the final crown spawns, the bonus waits for it to be popped or physically pass the Hit Zone.
- The crown receives a safe minimum travel speed so low operator speed settings cannot make the jackpot target unreachable.
- Perfect awards the jackpot; Great, Good, and Miss use configurable rewards.
- Bonus-mode weights respect the operator's Green, Blue, Mystery, and x2 settings before applying the Golden boost.

## 4. Cabinet-safe architecture

- Gameplay depends on `ArcadeInputManager`, not keyboard keycodes or serial APIs.
- `KeyboardArcadeIO` remains active for development and emergency service testing.
- `SerialArcadeIO` uses reflection and a worker thread so a missing serial assembly, absent COM port, invalid message, or disconnect does not crash the game.
- Ticket dispensing is non-blocking and queued. Scene changes do not cancel an active payout.
- The included Arduino sketch debounces inputs and maintains its own non-blocking ticket-pulse queue.

## 5. Runtime resilience, saves, and auditing

- Cabinet runtime settings disable sleep, keep the application running in the background, hide the cursor in player builds, and enforce the portrait target when enabled.
- Errors and exceptions are written to a small rotating runtime log under `Application.persistentDataPath`.
- Save writes use a temporary file and previous-good backup. A corrupt primary save attempts backup recovery before reverting to defaults.
- Save version migration initializes newly introduced cabinet settings without overwriting existing operator tuning.
- Every completed game appends a CSV audit row containing session ID, timestamps, score, payout, timing performance, record flags, jackpot status, remaining credits, and active payout limits.
- If an older audit schema exists, it is rotated to a timestamped legacy CSV before the new schema is written.

## 6. One-click generated project

`Tools > Balloon Rush > Build Complete Game` creates and wires:

1. Boot
2. AttractMode
3. MainGame
4. Results
5. OperatorMenu

It also creates prefabs, configuration assets, balloon definitions, placeholder presentation, build settings, portrait player settings, and all required references. The user is not asked to guess Inspector assignments.


## 7. Unity preflight and build gate

- `Tools > Balloon Rush > Validate Generated Project` checks generated assets, five-scene build order, portrait Player Settings, payout limits, timing/combo ordering, spawn-weight safety, and all required balloon definitions.
- Project generation runs the preflight automatically before opening the Boot scene.
- An `IPreprocessBuildWithReport` guard runs the same validation before any Unity player build, including builds started outside the custom cabinet menu.
- The Windows build command copies `LaunchBalloonRush.bat` beside the executable.

## 8. Operator and balancing workflow

The operator can configure:

- duration, credits, free play, coin/card values
- jackpot and maximum payout
- balloon ticket values and Golden consolation values
- GOOD/GREAT/PERFECT ticket multipliers
- normal and special balloon spawn weights
- speed, spawn interval, combo timeout, and combo multipliers
- timing windows, x2 duration, and Golden Round duration
- input debounce
- audio and accessibility options
- serial port, baud rate, ticket pulse amount, and pulse delay

Lifetime statistics and the session CSV audit should be used together. Start with conservative values, gather real paid-game samples, and change one category at a time.

## 9. Release gate

The generated foundation is not considered cabinet-release ready until all of the following pass on the exact production PC:

- Unity import and compilation
- Edit Mode automated tests
- complete manual play-test checklist
- physical button and card/coin pulse test
- exact ticket-count verification
- serial unplug/reconnect test
- display rotation and fullscreen test
- audio amplifier test
- save corruption/backup recovery test
- audit CSV verification
- 100-round soak test with hardware enabled
- 100-round soak test with hardware disabled

The repository includes static validation, but Unity and cabinet hardware are still required for final verification.
