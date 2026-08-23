# Applying the Balloon Rush 1.3.0 Update

This revision changes runtime input, generated scene layouts, the operator screen, debug access, and generated-font usage. Existing generated Unity scenes from v1.2.0 will not automatically inherit those changes.

## Full-project update

1. Back up the current Unity project and any custom art/audio.
2. Open the v1.3.0 project in Unity `6000.0.82f1`.
3. Wait for scripts and packages to finish importing.
4. Import TextMesh Pro Essential Resources if Unity requests them.
5. Run `Tools > Balloon Rush > Build Complete Game`.
6. Confirm the preflight reports no errors.
7. Open `Assets/BalloonRush/Scenes/Boot.unity` and press Play.

## Patch update over v1.2.0

1. Close Unity.
2. Copy the patch contents into the existing project root, preserving folders and allowing the listed files to be replaced.
3. Reopen Unity and wait for compilation.
4. Run `Tools > Balloon Rush > Build Complete Game` to regenerate all five scenes and generated assets.
5. Test the exact controls below.

## Revised controls

- `C`: add credit
- `Enter` or `P`: start/replay
- Left Arrow or `A`: lane left
- Right Arrow or `D`: lane right
- Up Arrow or `Space`: POP
- `M`: Operator Menu
- `Escape`: gameplay debug/service panel; back from Results/Operator screens
- `F2`-`F6`: debug actions only while the debug panel is visible

## Existing machine settings and statistics

Runtime operator settings and statistics are stored under `Application.persistentDataPath`, not inside generated scenes. Regenerating the project scenes does not intentionally erase that save. Still back up `BalloonRushSave.json` before a cabinet update.

## Visual regression checks

- Attract balloons stay clipped inside the demo field.
- The POP badge, title, high score, jackpot, and taglines do not overlap.
- Timing feedback does not cover the Hit Zone label.
- The Operator Menu uses the full portrait width for its scrollable settings.
- No TextMesh Pro warnings appear for unsupported star, skull, arrow, or crown characters.
- The 500-ticket jackpot and 1,000-ticket absolute payout cap remain enforced.
