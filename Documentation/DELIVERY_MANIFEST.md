# Balloon Rush Delivery Manifest

**Project version:** 1.3.0  
**Unity target:** 6000.0.82f1  
**Primary platform:** Windows x64 arcade cabinet  
**Display target:** portrait 1080 × 1920  
**Primary controls:** LEFT / POP / RIGHT  
**Default jackpot:** 500 tickets  
**Hard payout limit:** 1,000 tickets per game

## Included foundation

- 59 C# files containing 70 declared types and 9,887 lines of source
- runtime, Editor, and Edit Mode test assemblies
- complete gameplay, state, credit, scoring, combo, payout, Golden Round, jackpot, results, attract, operator, save, audio, effects, and hardware-abstraction systems
- one-click project generator for five scenes, prefabs, ScriptableObjects, placeholder visuals, UI, references, Build Settings, and portrait Player Settings
- Unity preflight validator plus automatic pre-build blocking for invalid scene order, assets, portrait configuration, balloon definitions, or payout safeguards
- Monte Carlo payout simulator and conservative floor-test defaults
- keyboard/gamepad development input, optional serial/Arduino cabinet input, and non-blocking ticket output
- versioned JSON save migration, atomic save replacement, backup recovery, rotating runtime log, and per-session CSV audit data
- three gameplay/reference mockups, revised cabinet UI generator, Arduino controller sketch, Windows launcher, command-line build script, tests, and operator documentation

## Generated-content policy

The repository intentionally does not ship hand-authored Unity YAML scenes, prefabs, or ScriptableObject assets. After scripts compile, run:

`Tools > Balloon Rush > Build Complete Game`

The Editor builder creates those files with valid Unity GUID and serialized references for the Unity installation that imports the project. It can be run again to regenerate the foundation without manual Inspector wiring.

## Validation completed in the delivery environment

- static repository validation: passed
- required-script comparison against the supplied specification: all 33 named scripts present
- JSON and assembly-definition parsing: passed
- placeholder-marker scan: passed
- C# delimiter, comment, string, and preprocessor-balance scan: passed
- duplicate declared type scan: passed
- payout guardrail and production-safeguard checks: passed
- archive integrity test: to be recorded in the external SHA-256 file supplied with the ZIP

## Validation still required on the production PC

Unity Editor was not installed in the delivery environment. Unity import, compilation, generated-scene validation, Test Runner execution, player build, cabinet I/O, exact ticket count, reconnect behavior, display rotation, audio, and soak testing must therefore be completed on the exact production PC before commercial deployment.

## First-run sequence

1. Open the project root in Unity Hub using Unity 6000.0.82f1.
2. Let Package Manager finish.
3. Import TextMesh Pro Essential Resources if prompted.
4. Enable the Input System backend, or set Active Input Handling to Both.
5. Run `Tools > Balloon Rush > Build Complete Game`.
6. Confirm the preflight passes.
7. Open `Assets/BalloonRush/Scenes/Boot.unity` and press Play.
8. Press `C`, then `Enter` or `P`; use Left Arrow/`A`, Right Arrow/`D`, and Up Arrow/`Space`.
9. Run every Edit Mode test and the manual cabinet checklist.
10. Build with `Tools > Balloon Rush > Build Windows Cabinet`.
