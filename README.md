# Balloon Rush — Unity 6 Arcade Redemption Game

Balloon Rush is a portrait, three-button arcade redemption game for a real cabinet. Balloons travel upward through three lanes toward a glowing hit zone. The player moves the selected lane with **LEFT** and **RIGHT**, then presses **POP** at the correct moment to build combos, earn tickets, avoid bombs, trigger the Golden Balloon Round, and chase a configurable **500-ticket jackpot**.

The repository contains complete C# gameplay systems plus a Unity Editor builder that generates all scenes, prefabs, default ScriptableObjects, UI, references, and build settings. Custom art and audio are optional: the project includes generated placeholder graphics, particles, and fallback sound effects so the game can be played before final production assets are added.

## Target

- Unity: **6000.0.82f1**
- Language: C#
- Display: portrait 9:16, reference resolution 1080 × 1920
- Platform: Windows arcade PC
- Controls: LEFT / POP / RIGHT, plus START, credit pulse, and operator access
- Default round: 35 seconds
- Default jackpot: 500 tickets
- Hard maximum payout: 1,000 tickets per paid game
- Build version: 1.3.0

## First launch

1. Extract the project and add its folder to Unity Hub.
2. Open it in Unity 6. Let Package Manager finish resolving packages.
3. If Unity displays a TextMesh Pro resources prompt, choose **Import TMP Essential Resources**.
4. If Unity asks to enable the new Input System backend, choose **Yes** and allow the Editor to restart; setting Active Input Handling to **Both** also works.
5. Run **Tools > Balloon Rush > Build Complete Game**.
6. The builder creates the generated assets, runs the Unity preflight validator, and opens `Assets/BalloonRush/Scenes/Boot.unity`.
7. Press Play.

The builder is intentionally the source of truth for generated `.unity`, `.prefab`, and `.asset` files. Run it again at any time to rebuild the generated foundation. **After upgrading from v1.2.0, rerun the builder before judging the UI; old generated scenes do not update themselves.**

## Development controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Add coin credit | C | - |
| Add card-swipe credit | V | - |
| Start / replay | Enter or P | Start |
| Lane left | Left Arrow or A | D-pad Left / Left Shoulder |
| Pop during gameplay | Up Arrow or Space | D-pad Up / South Button |
| Lane right | Right Arrow or D | D-pad Right / Right Shoulder |
| Operator menu | M | - |
| Debug/service panel in gameplay | Escape | Select |
| Close debug panel / back from menus | Escape | Select |
| Spawn Golden Balloon | F2, while debug panel is open | - |
| Spawn Bomb | F3, while debug panel is open | - |
| Start Golden Round | F4, while debug panel is open | - |
| Trigger debug jackpot | F5, while debug panel is open | - |
| End round | F6, while debug panel is open | - |

## Implemented cabinet behavior

- Green, Blue, and every special-balloon spawn weight are operator-configurable.
- GOOD, GREAT, PERFECT, and combo ticket multipliers are operator-configurable and validated as non-decreasing.
- Combo ticket scaling is configurable at the x5, x10, x15, x20, and x30 milestones.
- Purple `x2` balloons temporarily double normal rewards; the rare Super Bomb cancels the active x2 effect without ending the round.
- The Golden Round reserves time for its final crown balloon and will wait for that target to be resolved instead of ending while it is still approaching the Hit Zone.
- A Perfect final crown pop awards the configured jackpot; Great, Good, and Miss use configurable consolation values.
- Results queue the physical ticket payout immediately. The persistent `TicketManager` continues non-blocking dispensing even if the screen returns to Attract Mode.
- Additional payouts are queued rather than replacing an unfinished payout, and replay is temporarily locked while the visible ticket total is counting.
- Audio is separated into Music, SFX, UI, Jackpot, and Voice channels, with safe generated fallback tones when production clips are missing.
- The jackpot is clamped to 500 tickets and total payout is hard-capped at 1,000 tickets through settings validation, scoring, and final dispensing.

## Production balancing and audit tools

- Run **Tools > Balloon Rush > Payout Simulator** to estimate average payout, percentiles, jackpot frequency, and payout-cap frequency before floor testing.
- Run **Tools > Balloon Rush > Validate Generated Project** at any time. The same validation also runs after project generation and automatically before every Unity player build.
- The default commercial profile is documented in `Documentation/DEFAULT_BALANCE_PROFILE.md`; its simplified benchmark is about 74 tickets per game under the stated average-player assumptions, not a guaranteed floor result.
- Completed rounds are appended to `Application.persistentDataPath/BalloonRushAudit/sessions.csv` with score, payout, timing, record, jackpot, credit, and configuration data.
- Runtime errors are written to a rotating `BalloonRushRuntime.log` in `Application.persistentDataPath`.
- JSON settings/statistics use version migration, atomic replacement, and a previous-good backup.
- See `Documentation/IMPROVED_PRODUCTION_SPEC.md` for the applied commercial and reliability improvements.

## One-click Windows build

After generating the game, run:

**Tools > Balloon Rush > Build Windows Cabinet**

The executable is created at:

`Builds/Windows/BalloonRush.exe`

The cabinet launcher is copied beside the executable as `Builds/Windows/LaunchBalloonRush.bat`.

A command-line entry point is also included:

`BalloonRush.Editor.BalloonRushProjectBuilder.BuildWindowsCabinetCommandLine`

See `BuildScripts/build-windows.bat` for an example.

## Main folders

- `Assets/BalloonRush/Scripts/Core` — bootstrap, game flow, credits, state machine, round control
- `Assets/BalloonRush/Scripts/Gameplay` — spawning, pooling, timing, combo, score, balloons, jackpot
- `Assets/BalloonRush/Scripts/Input` — keyboard/gamepad and optional serial cabinet I/O
- `Assets/BalloonRush/Scripts/Redemption` — non-blocking ticket dispensing
- `Assets/BalloonRush/Scripts/UI` — attract, gameplay HUD, results, operator menu, debug panel
- `Assets/BalloonRush/Scripts/Audio` — music/SFX manager and generated fallback tones
- `Assets/BalloonRush/Scripts/Effects` — particles, screen shake, pooled floating text
- `Assets/BalloonRush/Scripts/SaveSystem` — JSON settings, high scores, and lifetime statistics
- `Assets/BalloonRush/Editor` — complete scene/prefab/configuration generator, preflight/build guard, payout simulator, and Windows build command
- `Assets/BalloonRush/Tests/Editor` — edit-mode tests for timing, rewards, combo, payout cap, and jackpot
- `Assets/BalloonRush/ReferenceArt` — gameplay, Golden Round, and results mockups
- `Hardware/Arduino` — optional cabinet controller sketch
- `Documentation` — original specification, delivery manifest, improved production design, default balance profile, setup, hardware, testing, validation, and troubleshooting

## Important production note

The project was assembled and statically reviewed outside the Unity Editor. Open it in the specified Unity version, run the builder, and complete the play-test checklist before placing it in a revenue cabinet. The included tests cover core gameplay math, but final cabinet validation must also include the actual card reader, buttons, ticket dispenser, display rotation, audio amplifier, and Windows startup environment. Escape opens the debug/service panel in the Unity Editor and Development Builds; F2-F6 debug actions only work while that panel is open. Normal cabinet builds keep those actions disabled unless `allowDebugShortcutsInRelease` is deliberately enabled in `BalloonRushConfig`.
