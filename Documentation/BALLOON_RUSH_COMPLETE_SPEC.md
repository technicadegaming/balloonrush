# BALLOON RUSH — COMPLETE UNITY ARCADE REDEMPTION GAME

You are a senior Unity game developer, arcade-game systems engineer, UI/UX designer, and technical artist.

Your task is to create a **complete, fully playable Unity game called "Balloon Rush"** inspired by a colorful modern arcade redemption cabinet.

Do not provide pseudocode, incomplete examples, or isolated snippets.

Build the project as though it will eventually be installed inside a real commercial arcade cabinet.

---

# 1. PROJECT GOAL

Create a fast-paced arcade redemption game named:

# BALLOON RUSH

The player watches balloons travel vertically toward a glowing **HIT ZONE**.

The cabinet has three primary controls:

- LEFT
- POP
- RIGHT

The player uses LEFT and RIGHT to select a balloon lane and presses POP when the balloon in that lane reaches the Hit Zone.

Accurate timing earns points, tickets, combos, bonuses, and jackpots.

Bad balloons such as bombs can break the player's combo or cause penalties.

The game should feel:

- Fast
- Colorful
- Addictive
- Easy to understand
- Difficult to master
- Suitable for children and adults
- Appropriate for a real arcade redemption cabinet

Visual inspiration:

- Neon arcade cabinet
- Deep blue background
- Bright cyan, pink, green, orange, yellow, purple and red accents
- Large readable arcade fonts
- Glowing borders
- Particle effects
- Balloon shine
- Explosions
- Confetti
- Screen flashes
- Combo animations
- Jackpot celebrations

Target orientation:

**Portrait 9:16**

Primary target resolution:

**1080 x 1920**

The UI must scale correctly to other portrait resolutions.

---

# 2. UNITY VERSION

Use:

**Unity 6 LTS or the newest stable Unity 6 release**

Language:

**C#**

Use standard Unity components whenever practical.

Avoid unnecessary third-party packages.

The finished project must run without paid assets.

Use generated shapes, sprites, TextMeshPro, particles, gradients, and placeholder artwork where custom graphics are unavailable.

---

# 3. PROJECT STRUCTURE

Create a clean project structure:

Assets/
BalloonRush/
Animations/
Audio/
Materials/
Particles/
Prefabs/
Balloons/
UI/
Effects/
Scenes/
ScriptableObjects/
Scripts/
Core/
Gameplay/
UI/
Audio/
Input/
Redemption/
SaveSystem/
Editor/
Sprites/
Fonts/

Use namespaces where appropriate:

BalloonRush.Core
BalloonRush.Gameplay
BalloonRush.UI
BalloonRush.Audio
BalloonRush.Input
BalloonRush.Redemption

---

# 4. REQUIRED SCENES

Create these scenes:

1. Boot
2. AttractMode
3. MainGame
4. Results
5. OperatorMenu

The game should transition automatically:

Boot
→ Attract Mode
→ Game Start
→ Main Game
→ Results / Ticket Count
→ Attract Mode

---

# 5. ATTRACT MODE

When nobody is playing, display an animated arcade attract screen.

Show:

BALLOON RUSH

and messages such as:

"POP YOUR WAY TO THE JACKPOT!"

"PRESS START"

"EASY TO PLAY — HARD TO STOP!"

"POP THE GOLDEN BALLOON!"

The attract screen should include:

- Animated balloons
- Logo movement
- Glow pulses
- Confetti
- Demo gameplay
- High score
- Jackpot ticket value
- Flashing Start message

Pressing the configured Start button begins the game if enough credits exist.

For development mode, allow SPACE or ENTER to start without arcade hardware.

---

# 6. CREDIT SYSTEM

Create a proper arcade credit system.

Variables:

Credits
CreditsPerPlay
FreePlay
CoinValue
CardSwipeValue

Default:

CreditsPerPlay = 1

The Operator Menu can enable:

Free Play

or

Credit Mode

Development keyboard input:

C = Add Credit
ENTER = Start Game

Display:

CREDITS: X

when appropriate.

---

# 7. MAIN GAMEPLAY

The main gameplay area contains:

**3 vertical balloon lanes**

Left Lane
Center Lane
Right Lane

Balloons travel vertically toward a horizontal glowing:

# HIT ZONE

near the upper portion of the gameplay area.

The player selects one of the three lanes.

LEFT:

Move selected lane one position left.

RIGHT:

Move selected lane one position right.

POP:

Attempt to pop the balloon currently crossing the Hit Zone in the selected lane.

Highlight the selected lane using:

- Bright glow
- Arrow
- Outline
- Animated lane indicator

Controls must feel extremely responsive.

---

# 8. BALLOON MOVEMENT

Balloons should spawn continuously during a round.

Each balloon has:

- Lane
- Balloon type
- Speed
- Point value
- Ticket value
- Visual color
- Spawn position
- Hit Zone position
- Timing window

Use object pooling.

DO NOT continuously Instantiate and Destroy balloons during gameplay.

Create:

BalloonPool.cs

for reusable balloon GameObjects.

Difficulty increases during the game by changing:

- Balloon speed
- Spawn frequency
- Balloon spacing
- Bomb frequency
- Timing-window size

---

# 9. TIMING SYSTEM

When POP is pressed, calculate the balloon's distance from the exact center of the Hit Zone.

Timing ratings:

PERFECT
GREAT
GOOD
MISS

Example normalized timing windows:

Perfect:
0–20% from center

Great:
20–45%

Good:
45–75%

Miss:
Outside valid Hit Zone

Make these values configurable.

Timing should affect:

- Score
- Ticket value
- Combo
- Visual effects
- Audio
- Screen feedback

PERFECT should feel especially satisfying.

Display a large animated message such as:

PERFECT POP!

GREAT!

GOOD!

MISS!

---

# 10. BALLOON TYPES

Create a reusable BalloonDefinition ScriptableObject.

Each balloon type contains:

ID
DisplayName
Sprite
Color
BasePoints
BaseTickets
SpawnWeight
IsDangerous
SpecialBehavior

Implement the following balloon types.

## GREEN +1

Award:

+1 ticket base value

Common balloon.

---

## BLUE +5

Award:

+5 tickets base value

Less common.

---

## PURPLE x2

Temporarily doubles ticket awards.

Default duration:

5 seconds

Show:

x2 PAYOUT!

---

## GOLD MYSTERY BALLOON

Display:

?

When popped, randomly award one of several bonuses:

- +5 tickets
- +10 tickets
- +25 tickets
- Double multiplier
- Slow motion
- Instant combo increase
- Golden Balloon chance
- Small jackpot bonus

Display a mystery reveal animation.

---

## RED BOMB BALLOON

Display:

Skull icon

If the player presses POP on it:

- Break current combo
- Apply ticket or score penalty if configured
- Play explosion
- Shake screen
- Flash red
- Show:

BOMB!

or

DON'T POP!

Bomb behavior must be configurable from Operator Settings.

The player can safely allow the bomb to pass.

---

## BLACK SUPER BOMB

Rare dangerous balloon.

If popped:

- Reset combo
- Temporarily reduce multiplier
- Large explosion effect

Do not make the punishment so severe that the game stops being fun.

---

## GOLDEN BALLOON

Very rare.

Glowing gold balloon with a crown symbol.

When popped successfully:

Trigger:

# GOLDEN BALLOON ROUND

Golden Balloons should have:

- Gold glow
- Light rays
- Sparkles
- Special audio
- Distinctive spawn warning

---

# 11. COMBO SYSTEM

Successful consecutive pops build a combo.

Example:

x1
x2
x3
x4
...
x12
...
x25+

Combo increases ticket potential.

Combo resets when:

- Player misses
- Player pops a bomb
- Configured timeout expires

Display combo prominently.

Example:

COMBO x12

Create a vertical combo meter along the side.

The combo meter fills upward.

Use colors progressing from:

Blue
Purple
Pink
Red
Orange
Yellow

At major combo milestones, trigger:

- Screen glow
- Particle burst
- Audio sting
- Ticket multiplier increase

Milestones should be configurable.

Example:

Combo 5
Combo 10
Combo 15
Combo 20
Combo 30

---

# 12. SCORING

Maintain separately:

Score
TicketsWon
Combo
HighestCombo
PerfectPops
GreatPops
GoodPops
Misses
BalloonsPopped

Score is primarily used for competition.

Tickets are used for redemption payout.

Example formula:

Base Score × Timing Multiplier × Combo Multiplier

Timing examples:

Perfect = 2.0
Great = 1.5
Good = 1.0

All values must be configurable.

---

# 13. TICKET PAYOUT LADDER

Display a payout ladder on the right side.

Example visible ticket tiers:

500
250
100
50
25
10
5
1

The player's performance should move them toward higher payout opportunities.

Make payout values configurable.

The operator should be able to control overall expected ticket payout.

---

# 14. JACKPOT SYSTEM

Include a large visible:

JACKPOT

Default:

500 TICKETS

Make jackpot configurable.

Possible jackpot trigger:

- Successfully complete Golden Balloon Round
- Reach required combo
- Hit several PERFECT pops
- Pop final Golden Balloon PERFECTLY

When jackpot is won:

Pause normal gameplay.

Show:

# JACKPOT!

Display huge animated ticket amount.

Effects:

- Gold confetti
- Fireworks
- Balloon explosion
- Camera shake
- Flashing lights
- Jackpot sound
- Particle shower
- Animated counter

Increase excitement for approximately 4–6 seconds.

---

# 15. GOLDEN BALLOON ROUND

When a Golden Balloon is popped, enter bonus mode.

Show:

# GOLDEN BALLOON ROUND!

Gameplay changes temporarily.

Example:

10 seconds

During bonus mode:

- Spawn more valuable balloons
- Increase game speed slightly
- Add gold visual treatment
- Increase Perfect timing reward
- Remove normal bombs or reduce their frequency
- Play special music

The final balloon should be a special Golden Jackpot Balloon.

PERFECT final pop:

Award jackpot.

GREAT:

Award large bonus.

GOOD:

Award smaller bonus.

MISS:

Award consolation tickets.

All values must be configurable.

---

# 16. ROUND STRUCTURE

Default game duration:

45 seconds

Make configurable between:

20–120 seconds.

Suggested progression:

0–10 sec:
Easy

10–20 sec:
Medium

20–30 sec:
Faster

30–40 sec:
High intensity

Final 5 sec:
Rush Mode

During final seconds display:

5
4
3
2
1

Increase:

- Music intensity
- Balloon speed
- Particle activity
- UI pulsing

Show:

BALLOON RUSH!

---

# 17. JUICE / GAME FEEL

Every successful pop should feel satisfying.

Use:

- Balloon squash before pop
- Scale punch
- Particle burst
- Small screen shake
- Score popup
- Ticket popup
- Pop sound
- Glow flash
- Confetti for Perfect
- Brief hit-stop around 0.03–0.07 seconds for Perfect hits

Create reusable effects systems rather than hardcoding effects inside Balloon.cs.

Create:

EffectsManager.cs
ScreenShake.cs
FloatingTextPool.cs

---

# 18. BALLOON POP ANIMATION

Do not simply make the GameObject disappear.

Sequence:

1. Balloon slightly expands.
2. Balloon compresses.
3. Balloon bursts.
4. Colored particles shoot outward.
5. Ticket/score text appears.
6. Balloon returns to pool.

Total animation:

approximately 0.15–0.25 seconds.

Special balloons receive unique effects.

---

# 19. AUDIO

Create an AudioManager.

Audio categories:

Music
SFX
UI
Jackpot
Voice

Required events:

Balloon pop
Perfect pop
Great pop
Good pop
Miss
Bomb explosion
Button click
Lane move
Combo increase
Combo milestone
Golden Balloon appear
Golden Balloon pop
Bonus start
Countdown
Game over
Ticket counting
Jackpot

Use placeholder clips safely if actual audio assets do not exist.

The game must continue functioning even when an AudioClip is missing.

---

# 20. MUSIC

Support:

Attract music
Gameplay music
Rush music
Golden Round music
Jackpot music
Results music

Crossfade between tracks.

Do not abruptly stop normal music unless intentionally creating dramatic effect.

---

# 21. UI LAYOUT

Portrait screen.

Suggested layout:

TOP:
BALLOON RUSH logo

Upper Left:
Tickets won

Upper Right:
Jackpot

Left Side:
Combo meter

Right Side:
Payout ladder

Center:
Three balloon lanes

Across center/upper center:
HIT ZONE

Bottom Center:
Large combo display

Bottom:
LEFT / POP / RIGHT control indicators

During actual cabinet gameplay these graphics represent the physical controls.

---

# 22. HIT ZONE

The Hit Zone must be extremely obvious.

Use:

- Cyan neon outline
- Animated arrows
- Pulsating glow
- Central target marker

When a balloon enters it:

Increase balloon outline/glow slightly.

When Perfect:

Hit Zone flashes green/gold.

When Miss:

Hit Zone flashes red.

---

# 23. INPUT SYSTEM

Support:

Keyboard
Unity Input System
Gamepad
Arcade buttons

Default keyboard:

Left Arrow / A = LEFT
Right Arrow / D = RIGHT
Space = POP
Enter = START
C = ADD CREDIT
Escape = Pause or Operator Exit

Create:

ArcadeInputManager.cs

Expose configurable mappings.

The gameplay code must NOT directly depend on keyboard keycodes.

Gameplay subscribes to abstract input actions.

---

# 24. ARCADE HARDWARE SUPPORT

Design the input/output architecture so the game can later run in a physical cabinet.

Create an interface:

IArcadeIO

Methods/events should support:

Start button
Left button
Right button
Pop button
Coin pulse
Card-reader credit pulse
Ticket dispenser output

Provide:

KeyboardArcadeIO.cs

for development.

Also provide:

SerialArcadeIO.cs

for optional Arduino communication.

The game must run completely without Arduino hardware connected.

---

# 25. OPTIONAL ARDUINO SERIAL PROTOCOL

Implement an optional simple serial protocol.

Unity receives:

LEFT
RIGHT
POP
START
COIN

Unity sends:

TICKETS:25

or similar.

Serial port and baud rate must be configurable.

Suggested baud:

115200

Handle:

- Disconnection
- Missing port
- Invalid serial messages
- Reconnection

without crashing the game.

---

# 26. TICKET DISPENSER

Create:

TicketManager.cs

It receives final number of tickets won.

Development mode:

Simply animate the ticket counter.

Hardware mode:

Send ticket pulses/messages through IArcadeIO.

Support:

TicketsPerPulse
PulseDelay
MaxTicketPayout

Never freeze Unity while dispensing tickets.

Use coroutine or asynchronous logic.

---

# 27. RESULTS SCREEN

After gameplay display:

BALLOON RUSH RESULTS

Tickets Won
Final Score
Highest Combo
Perfect Pops
Golden Balloons
Jackpot Won

Animate ticket count upward.

Example:

0
12
48
103
237

Then show:

YOU WON 237 TICKETS!

Provide buttons or automatic timeout for:

Play Again
Return to Attract

In credit mode, Play Again requires credit.

---

# 28. HIGH SCORE SYSTEM

Save:

Top Score
Highest Combo
Most Tickets
Jackpots Won

Use JSON save data instead of storing everything directly in PlayerPrefs.

PlayerPrefs may only store simple settings if desired.

Create:

SaveManager.cs

Save location should use:

Application.persistentDataPath

Handle missing/corrupted save files gracefully.

---

# 29. OPERATOR MENU

Create an arcade operator/settings screen.

Operator must be able to configure:

Game Duration
Credits Per Play
Free Play
Jackpot Tickets
Maximum Tickets
Balloon Speed
Spawn Frequency
Bomb Frequency
Golden Balloon Frequency
Mystery Balloon Frequency
Combo Multipliers
Perfect Window
Great Window
Good Window
Golden Round Duration
Ticket Payout Values
Audio Volume
Music Volume
Serial Port
Baud Rate
Hardware Enabled

Buttons:

SAVE
RESET DEFAULTS
TEST INPUTS
TEST TICKETS
BACK

Settings must persist.

---

# 30. OPERATOR STATISTICS

Track lifetime machine statistics:

Games Played
Total Credits
Total Tickets Paid
Average Tickets Per Game
Jackpots Won
Perfect Pops
Total Balloons Popped
Bombs Hit

Provide Reset Statistics with confirmation.

---

# 31. CONFIGURATION SYSTEM

Do not scatter game values throughout scripts.

Create ScriptableObjects such as:

GameConfig
BalloonDefinition
PayoutConfig
DifficultyConfig
AudioConfig

GameConfig should contain global game balancing settings.

Design the system so the operator or developer can rebalance the game without changing code.

---

# 32. GAME STATE MACHINE

Create a clean state machine.

Possible states:

Boot
Attract
WaitingForCredit
Starting
Playing
GoldenRound
RushMode
GameOver
TicketPayout
Results

Create:

GameStateManager.cs

Avoid giant GameManager scripts containing every feature.

---

# 33. REQUIRED CORE SCRIPTS

At minimum implement:

GameBootstrap.cs
GameStateManager.cs
GameManager.cs
RoundManager.cs
BalloonManager.cs
Balloon.cs
BalloonDefinition.cs
BalloonPool.cs
BalloonSpawner.cs
LaneManager.cs
HitZone.cs
TimingEvaluator.cs
ComboManager.cs
ScoreManager.cs
TicketManager.cs
JackpotManager.cs
GoldenRoundManager.cs
DifficultyManager.cs
ArcadeInputManager.cs
IArcadeIO.cs
KeyboardArcadeIO.cs
SerialArcadeIO.cs
AudioManager.cs
EffectsManager.cs
ScreenShake.cs
FloatingTextPool.cs
UIManager.cs
AttractModeManager.cs
ResultsManager.cs
OperatorMenuManager.cs
SettingsManager.cs
SaveManager.cs

Add additional scripts when needed.

---

# 34. EVENTS

Use C# events or a lightweight event system so systems remain decoupled.

Examples:

OnBalloonEnteredHitZone
OnBalloonPopped
OnPerfectPop
OnMiss
OnComboChanged
OnTicketsChanged
OnGoldenBalloonPopped
OnGoldenRoundStarted
OnJackpotWon
OnGameStarted
OnGameEnded

Avoid unnecessary FindObjectOfType calls every frame.

---

# 35. PERFORMANCE

Target:

60 FPS minimum

The game must run efficiently on a normal Windows arcade PC.

Requirements:

- Pool balloons
- Pool floating text
- Pool common particle effects where practical
- Avoid garbage allocations every frame
- Avoid LINQ in Update loops
- Avoid repeated GameObject.Find
- Cache component references
- Use efficient UI updates
- Do not update TextMeshPro labels unless displayed values changed

---

# 36. CAMERA

Use an orthographic camera for the gameplay field.

UI may use Screen Space Overlay or Screen Space Camera.

Add subtle camera shake during:

Perfect hits
Bombs
Jackpot
Major combos

Do not make shake uncomfortable.

---

# 37. VISUAL STYLE

Reproduce the feeling of a premium modern redemption machine.

Background:

Deep navy blue.

Use:

- Neon cyan
- Electric blue
- Magenta
- Purple
- Gold
- Bright green
- Red
- Orange

UI panels should use:

Dark centers
Bright neon outlines
Rounded corners
Outer glows
Subtle gradients

Balloons should appear glossy.

Each balloon can use:

Base colored circle/oval sprite
White highlight
Small knot
String
Glow

Make balloons visually distinguishable without requiring text alone.

---

# 38. ACCESSIBILITY

Avoid relying only on balloon color.

Use icons:

+1
+5
x2
?
Skull
Crown

Provide optional:

Reduced screen shake
Reduced flashes
Master volume controls

---

# 39. EDITOR AUTOMATION

This is important.

Create an Editor script such as:

BalloonRushProjectBuilder.cs

Add menu command:

# Tools > Balloon Rush > Build Complete Game

When selected, this Editor tool should automatically create as much of the project as possible:

- Required folders
- Scenes
- Canvas
- Cameras
- Game managers
- Lane objects
- Hit Zone
- EventSystem
- Balloon prefabs
- Basic UI
- ScriptableObjects
- Default configuration assets
- Buttons
- Required references

The goal is to minimize manual Inspector wiring.

If automatic generation is practical, use it.

---

# 40. ZERO BROKEN REFERENCES

The project must not require the user to guess which GameObject goes into which Inspector field.

Either:

1. Automatically assign references through the Editor builder,

or

2. Give exact setup instructions for every Inspector field.

Never say:

"Assign the references."

Instead say exactly:

GameObject:
MainGameManager

Component:
GameManager

Field:
Score Manager

Assign:
MainGameManager/Systems/ScoreManager

Prefer automated assignment.

---

# 41. PLACEHOLDER ART

Create functional placeholder visuals using Unity-generated sprites and UI components.

Do not leave the game unplayable because custom art is unavailable.

The placeholder version should already resemble an arcade game.

Use:

- Gradient panels
- Circles
- TMP text
- Particle systems
- UI images
- Glow-like duplicate images
- Animated scale
- Simple materials

Make it easy to replace placeholder art later.

---

# 42. STARTUP EXPERIENCE

When the project launches:

Boot scene initializes systems.

It should automatically load Attract Mode.

Development mode should allow:

C

to add a credit.

Then:

ENTER

starts.

The player should immediately be able to use:

A / LEFT
D / RIGHT
SPACE / POP

No additional setup should be necessary after the project builder has been run.

---

# 43. GAMEPLAY EXAMPLE

A normal game might play as follows:

Player starts with 1 credit.

Countdown:

3
2
1
POP!

Green +1 balloon approaches Hit Zone.

Player selects lane.

POP.

PERFECT!

Combo x1.

Blue +5 arrives.

PERFECT!

Combo x2.

Purple x2 balloon appears.

Player pops it.

PAYOUT x2!

Bomb enters another lane.

Player avoids pressing Pop.

Mystery Balloon arrives.

Player pops it.

Mystery reveals:

+25!

Combo reaches x12.

Screen celebrates:

COMBO x12!

Golden Balloon enters.

Special alarm sounds.

Player hits:

PERFECT!

Golden Balloon Round begins.

Player successfully clears bonus balloons.

Final Golden Jackpot Balloon appears.

Player hits:

PERFECT POP!

JACKPOT!

500 TICKETS!

Results screen counts the total ticket payout.

---

# 44. BALANCING

Centralize all balancing values.

Provide reasonable default values but make them easy to change.

Do not design ticket payout in a way that requires recompilation.

Provide settings capable of controlling theoretical payout percentage.

Where exact redemption mathematics are complex, expose sufficient variables for an operator to tune:

Spawn weights
Base tickets
Combo multipliers
Jackpot probability
Game duration
Difficulty
Bonus frequency

---

# 45. DEBUG PANEL

Provide a development-only debug panel.

Toggle:

F1

Display:

FPS
Game State
Current Lane
Combo
Tickets
Score
Balloon Count
Pool Count
Current Difficulty
Golden Round State

Debug shortcuts:

F2 = Spawn Golden Balloon
F3 = Spawn Bomb
F4 = Trigger Golden Round
F5 = Trigger Jackpot
F6 = End Game

Debug functionality should be disabled or hidden in release mode through configuration.

---

# 46. ERROR HANDLING

The game must not crash if:

- Audio clip missing
- Sprite missing
- Serial port unavailable
- Save file missing
- Configuration asset missing
- Ticket hardware disconnected

Log useful warnings.

Create safe defaults.

---

# 47. CODE QUALITY

Follow these rules:

- No giant monolithic scripts
- Single responsibility where practical
- Descriptive names
- Comments around complicated logic
- Avoid unnecessary public fields
- Prefer `[SerializeField] private`
- Null-check external references
- Separate gameplay from hardware
- Separate gameplay from UI
- Separate ticket calculation from ticket dispensing
- Do not put critical logic solely inside animation events

---

# 48. DELIVERABLE FORMAT

Build the actual Unity project files when filesystem access is available.

Do not only describe the implementation.

Create each required `.cs` file with complete source code.

Create Editor automation needed to construct scenes and prefabs.

When you cannot directly create `.unity`, `.prefab`, or `.asset` files safely, generate them through Unity Editor scripts.

After writing the project, provide:

1. Project folder structure
2. Complete scripts
3. Scene-builder/editor script
4. Setup instructions
5. Input mappings
6. Inspector configuration
7. Play-test procedure
8. Hardware integration instructions
9. Build instructions
10. Troubleshooting guide

---

# 49. DEVELOPMENT ORDER

Implement the project in this order:

## Phase 1

Project architecture

## Phase 2

Core game state system

## Phase 3

Balloon lane/spawning system

## Phase 4

Hit Zone and timing

## Phase 5

Pop mechanics

## Phase 6

Scoring and combo

## Phase 7

Tickets

## Phase 8

Special balloons

## Phase 9

Golden Round

## Phase 10

Jackpot

## Phase 11

UI

## Phase 12

Effects

## Phase 13

Audio

## Phase 14

Attract Mode

## Phase 15

Operator Menu

## Phase 16

Save system

## Phase 17

Arcade hardware interface

## Phase 18

Editor project builder

## Phase 19

Testing

Do not skip phases.

---

# 50. TESTING REQUIREMENTS

Verify:

### Normal Balloons

Correct reward.

### Timing

Perfect/Great/Good/Miss correctly detected.

### Lane Selection

Cannot leave lane range.

### Combo

Increases and resets correctly.

### Bomb

Penalty works.

### Mystery

Random reward works.

### x2

Multiplier starts and expires.

### Golden Balloon

Bonus round starts.

### Jackpot

Correctly awards configured tickets.

### Timer

Game ends correctly.

### Results

Correct numbers displayed.

### Credits

Cannot start without credits unless Free Play.

### Saving

Settings survive restart.

### Hardware

Disconnected serial device does not crash game.

### Object Pool

Balloons are reused.

---

# 51. AUTOMATED TESTS

Where practical, create Unity Test Framework tests for:

TimingEvaluator
ComboManager
Score calculations
Ticket calculations
Balloon reward logic
Jackpot conditions

Gameplay math should be testable without loading the full game scene whenever possible.

---

# 52. IMPORTANT FINAL REQUIREMENTS

The finished game must be:

- Fully playable
- Compilable
- Organized
- Expandable
- Portrait oriented
- Arcade cabinet friendly
- Redemption/ticket ready
- Keyboard playable
- Arduino-ready
- Visually exciting
- Easy to rebalance
- Free of TODO placeholders
- Free of obvious missing-reference errors

Do not stop after giving me several scripts.

Continue until the complete Balloon Rush foundation exists.

Whenever you create a script that references another class, make sure that class is also created.

Whenever you create an Inspector field, make sure the field is automatically connected or explicitly configured.

Whenever you introduce a package, explain exactly why it is required.

If compilation problems appear, fix them before proceeding.

---

# 53. FINAL PLAYABLE TARGET

When I press Play in Unity, I should be able to:

1. See Balloon Rush Attract Mode.
2. Press C to add a credit.
3. Press Enter to start.
4. See a 3-2-1 countdown.
5. Control the selected lane with Left/Right.
6. Press Space to pop balloons.
7. Receive timing ratings.
8. Build combos.
9. Avoid bombs.
10. Pop +1 and +5 balloons.
11. Activate x2 balloons.
12. Receive Mystery bonuses.
13. Trigger Golden Balloon rounds.
14. Win a jackpot.
15. Finish the timed round.
16. See my final ticket payout.
17. Return automatically to Attract Mode.
18. Change machine settings from the Operator Menu.
19. Play repeatedly without errors or memory leaks.

The result should feel like a real arcade redemption game rather than a Unity tutorial project.

# BEGIN BUILDING BALLOON RUSH NOW.