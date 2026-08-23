# Changelog

## 1.3.0 - UI and cabinet-control revision

- Updated development controls to C for credit, Enter or P for start, Left Arrow/A and Right Arrow/D for lane movement, and Up Arrow/Space for POP.
- Changed Operator Menu access to M from Attract, Main Game, and Results.
- Changed the gameplay debug/service panel toggle to Escape; F2-F6 now work only while that panel is visible.
- Rebuilt the Attract Mode layout with a masked demo playfield so balloons no longer overlap the logo, score, or prompts.
- Reworked the gameplay HUD with clearer hierarchy, larger cabinet controls, improved lane indicators, a more readable timer, separated feedback text, and stronger neon framing.
- Reworked Results and Operator Menu layouts for portrait readability.
- Improved operator-row sizing, input-field widths, field outlines, keyboard guidance, status presentation, and lifetime-statistics formatting.
- Enabled dynamic TMP atlas growth and removed reliance on unsupported symbol glyphs from generated UI.
- Updated builder dialogs, setup documentation, validation checks, and cabinet instructions for the new controls.

## 1.2.0 — Improved cabinet build

- Added conservative 35-second commercial floor-test defaults.
- Kept jackpot at 500 tickets and enforced a 1,000-ticket hard cap.
- Added operator-editable Green and Blue spawn weights.
- Added operator-editable GOOD, GREAT, and PERFECT ticket multipliers.
- Reduced default combo payout growth while preserving score/visual progression.
- Updated Mystery rewards to obey operator minimum and maximum values.
- Added centrally configurable 25 ms cabinet input debounce.
- Added results-screen new-record and score-gap replay prompts.
- Added save version migration, previous-good backup, atomic replacement, and backup recovery.
- Added rotating runtime error logging and per-session CSV audit telemetry.
- Added audit schema rotation for older CSV files.
- Added the Unity Editor payout simulator and conservative profile documentation.
- Added cabinet runtime resolution, cursor, sleep, frame-rate, and background settings.
- Expanded automated tests and static repository validation.
- Added a Unity preflight validator and automatic pre-build release guard.
- Added cabinet launcher copying and a more robust command-line Windows build script.
- Prevented development ticket-pulse logging from spamming production players.

## 1.1.0 — Production hardening foundation

- Added cabinet runtime manager, session audit logger, input debounce, and payout simulator.
- Added build versioning and additional debug panel diagnostics.

## 1.0.0 — Complete gameplay foundation

- Added one-click project builder, five-scene flow, pooled three-lane gameplay, timing, combo, scoring, tickets, special balloons, Golden Round, jackpot, operator menu, save data, keyboard/gamepad controls, optional serial I/O, ticket dispensing, fallback audio/effects, Arduino sketch, and edit-mode tests.
