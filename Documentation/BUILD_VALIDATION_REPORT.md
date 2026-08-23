# Balloon Rush Build Validation Report

**Validation date:** 2026-08-21  
**Project version:** 1.3.0  
**Project target:** Unity 6000.0.82f1, Windows x64, portrait 1080 × 1920

## Delivered foundation

- 88 repository files
- 59 C# source files containing 70 declared types
- 9,887 lines of C# source
- runtime assembly, Editor assembly, and Edit Mode test assembly definitions
- one-click Unity project builder and Windows cabinet build command
- Boot, Attract Mode, Main Game, Results, and Operator Menu scene generation
- three-lane timing gameplay, pooled balloons, combo, score, tickets, Golden Round, jackpot, save, operator, effects, audio, input, serial, and ticket-dispenser systems
- central input debounce, cabinet runtime hardening, rotating error log, and per-session CSV audit telemetry
- Editor payout simulator for estimating average payout, percentiles, jackpot rate, and hard-cap frequency
- Unity generated-project preflight plus automatic `IPreprocessBuildWithReport` build blocking
- reference mockups, Arduino cabinet sketch, Windows launcher/build scripts, delivery manifest, and operator documentation

## Static checks passed

Run from the repository root:

```text
python BuildScripts/validate-source.py
```

The validator checks:

- balanced braces, brackets, parentheses, strings, character literals, and comments
- balanced C# preprocessor directives
- duplicate declared type names
- TODO, FIXME, HACK, pseudocode, and `NotImplementedException` placeholders in C# source
- valid JSON in `Packages/manifest.json` and all `.asmdef` files
- presence of required scripts, tests, documentation, reference art, and hardware files
- payout-cap enforcement paths in operator settings, score accumulation, and final ticket dispensing
- conservative commercial defaults, save version, build version, audit schema, preflight integration, and automatic build guard

Latest result:

```text
Balloon Rush static validation
Project: BalloonRushUnity6
C# files: 59
C# lines: 9887
Declared types: 70
Warnings: 0
Errors: 0
PASS: static project validation succeeded.
```

## Unity preflight included

After `Tools > Balloon Rush > Build Complete Game` creates the generated Unity assets, `BalloonRushPreflightValidator` checks:

- all required config assets and prefabs
- exact five-scene Build Settings order
- portrait resolution and cabinet Player Settings
- jackpot and maximum-ticket limits
- ordered timing windows and ticket multipliers
- non-decreasing combo multipliers
- non-negative spawn weights and a valid common-balloon weight
- all required balloon definition types

The project builder runs this preflight automatically. `BalloonRushBuildPreprocessor` also runs it before every Unity player build, including builds not started through the custom menu.

## Environment limitation

The Unity Editor, Unity batch-mode compiler, .NET SDK, Mono, and a standalone C# compiler were not installed in the delivery environment. The project therefore has **not** been imported, compiled, or play-tested inside Unity here. Static validation is useful, but it cannot detect every Unity serialization, package-resolution, API-version, platform, or scene-runtime issue.

## Required first-run validation

1. Open the repository root in Unity Hub with Unity `6000.0.82f1`.
2. Allow Package Manager to resolve dependencies.
3. Import TextMesh Pro Essential Resources when prompted.
4. Enable the Input System backend when prompted, or set Active Input Handling to Both.
5. Run `Tools > Balloon Rush > Build Complete Game`.
6. Confirm the automatic preflight passes and the Console contains no compilation errors.
7. Open `Assets/BalloonRush/Scenes/Boot.unity` and press Play.
8. Run all Edit Mode tests in `Window > General > Test Runner`.
9. Complete every manual item in `TESTING_AND_TROUBLESHOOTING.md`.
10. Test the actual cabinet buttons, card/coin pulse, ticket dispenser, display rotation, audio amplifier, serial reconnection, and Windows auto-launch.
11. Run at least 100 continuous rounds with hardware connected and 100 with hardware disabled before commercial deployment.

## Cabinet release gate

Do not deploy to a revenue cabinet until Unity compilation, automatic preflight, automated tests, the manual gameplay checklist, ticket-count verification, electrical I/O verification, audit-log review, and soak testing all pass on the exact production PC and cabinet hardware.
