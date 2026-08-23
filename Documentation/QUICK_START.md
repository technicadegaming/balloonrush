# Quick Start

## 1. Open the project

Use Unity Hub to open the repository root—the folder containing `Assets`, `Packages`, and `ProjectSettings`.

Target editor: Unity `6000.0.82f1`. Commit or copy the project before allowing another Unity version to upgrade serialized files.

## 2. Let packages resolve

The project requests Input System, TextMesh Pro, Unity Test Framework, and Unity UI packages. If TextMesh Pro asks for essential resources, import them. If Unity asks to enable the Input System backend, choose **Yes** and allow the Editor to restart; Active Input Handling set to **Both** also supports the development controls.

## 3. Generate the playable game

Run:

`Tools > Balloon Rush > Build Complete Game`

The builder creates and wires:

1. `Boot`
2. `AttractMode`
3. `MainGame`
4. `Results`
5. `OperatorMenu`

It also creates balloon definitions, config assets, prefab templates, scene references, build settings, and a portrait UI. When generation finishes, the Unity preflight validator checks required assets, scene order, portrait settings, balloon definitions, and payout safety.

## 4. Play

Open `Assets/BalloonRush/Scenes/Boot.unity` and press Play.

1. Press `C` to add a credit.
2. Press `Enter` or `P` to begin from Attract Mode.
3. Use Left Arrow/`A` and Right Arrow/`D` to select a lane.
4. Press Up Arrow or `Space` when a balloon crosses the Hit Zone.
5. Avoid red and black bomb balloons.
6. Pop the gold balloon to enter Golden Balloon Round.
7. Hit the final crown balloon perfectly for the 500-ticket jackpot.

## 5. Validate the source and tests

Before opening Unity, the repository-level check is:

`python BuildScripts/validate-source.py`

Inside Unity, run `Tools > Balloon Rush > Validate Generated Project`, then open `Window > General > Test Runner` and run all Edit Mode tests in `BalloonRush.Tests.Editor`. The validator also runs automatically before every Unity player build.

## 6. Review payout assumptions

Run:

`Tools > Balloon Rush > Payout Simulator`

Simulate weak, average, and strong players before changing operator settings. The shipped profile is described in `DEFAULT_BALANCE_PROFILE.md` and uses a 500-ticket jackpot with a 1,000-ticket hard cap.

## 7. Operator menu

From Attract Mode, press `M`. Press `M` again or `Escape` to return.

The operator can tune duration, credits, payout, Green/Blue/special spawn weights, timing and combo ticket multipliers, speed, timing windows, audio, accessibility, serial hardware, input debounce, and ticket pulse behavior. Settings are saved as versioned JSON under `Application.persistentDataPath`.

## 8. Build for Windows

Run:

`Tools > Balloon Rush > Build Windows Cabinet`

The build includes `BalloonRush.exe` and a copied `LaunchBalloonRush.bat` cabinet launcher.

Or edit `BuildScripts/build-windows.bat` and set `UNITY_EXE` to the Unity Editor executable on the build PC.

## 9. Before cabinet installation

Complete every item in `TESTING_AND_TROUBLESHOOTING.md`, verify the audit and runtime logs, then run at least 100 continuous rounds with hardware connected and another 100 with hardware disabled.
