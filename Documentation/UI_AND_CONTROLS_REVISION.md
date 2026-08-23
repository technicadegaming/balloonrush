# Balloon Rush 1.3.0 UI and Controls Revision

## Why this revision was made

The first generated scenes were mechanically playable, but the screenshots exposed several presentation problems that would be distracting on a real portrait arcade cabinet:

- Attract Mode balloons were not clipped to the demo field and could cover the logo, high score, and jackpot area.
- The title, score, tagline, and controls competed for the same space.
- Main Game timing feedback could overlap the Hit Zone label.
- The side HUD panels were difficult to read at portrait scale.
- The flat rectangular control indicators did not resemble the cabinet's three physical buttons.
- TextMesh Pro warned about unsupported star and skull characters in the generated font asset.
- The builder completion dialog and documentation still showed the previous control scheme.

## New keyboard controls

| Action | Key |
|---|---|
| Add credit | C |
| Start or replay | Enter or P |
| Move selected lane left | Left Arrow or A |
| Move selected lane right | Right Arrow or D |
| POP | Up Arrow or Space |
| Open Operator Menu | M |
| Open or close gameplay debug/service panel | Escape |
| Debug actions | F2-F6, only while the debug panel is open |

## UI changes

### Attract Mode

- Added a masked demo playfield so moving balloons cannot escape into the header.
- Separated the POP badge, logo, high score, instructions, demo field, cabinet controls, credit/start prompt, and service hint.
- Replaced flat control boxes with larger circular button graphics and explicit keyboard hints.
- Added clearer ready/not-ready prompts based on credits or Free Play.

### Main Game

- Added a framed world-space playfield behind the three lanes.
- Reorganized tickets, score, timer, jackpot, combo meter, payout ladder, lane indicators, and cabinet controls.
- Moved timing ratings and system messages into separate non-overlapping regions.
- Added a large Escape-controlled debug/service panel with live diagnostics.
- Kept the gameplay, ticket, jackpot, save, and hardware systems independent from the new presentation layer.

### Results

- Added a stronger ticket-result hierarchy and a separate replay/service prompt area.
- Updated replay to Enter or P and Operator Menu access to M.

### Operator Menu

- Retained the full operator control set for duration, credits, payout, balloon weights, timing, combo multipliers, audio, accessibility, serial hardware, debounce, and ticket pulses.
- Improved field widths for a 1080-pixel portrait display.
- Added clearer section headers, outlines, status feedback, keyboard guidance, a full-width settings list, and compact lifetime statistics.
- M or Escape returns to Attract Mode; while TEST INPUTS is active, those controls are reported instead of leaving.

## Regenerating the scenes

After replacing or opening this project version:

1. Allow Unity to finish compiling.
2. Run `Tools > Balloon Rush > Build Complete Game`.
3. The builder will regenerate all five scenes and their references using the revised UI.
4. Open `Assets/BalloonRush/Scenes/Boot.unity` and press Play.
5. Do not judge the update from previously generated scene files; rerun the builder so the new scene-generation code is applied.

## Required checks in Unity

- Confirm there are no C# compilation errors.
- Confirm TextMesh Pro Essential Resources are imported.
- Verify Attract Mode balloons remain inside the demo frame.
- Verify Enter and P both start, and Up Arrow and Space both POP.
- Verify M opens Operator Settings from Attract, Main Game, and Results.
- Verify Escape opens and closes the debug panel during Main Game.
- Verify F2-F6 do nothing until the debug panel is open.
- Verify all operator settings save and survive a restart.
- Verify the 500-ticket jackpot and 1,000-ticket hard payout cap remain enforced.
