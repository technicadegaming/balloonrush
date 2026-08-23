# Balloon Rush v1.6.3 Transient Missing Script Watcher

Your static scan and manual Play Mode scan both returned zero missing scripts,
but Unity still logs:

`The referenced script (Unknown) on this Behaviour is missing!`

That usually means a runtime GameObject exists only briefly during scene/bootstrap setup.

## Install

Copy the Assets folder into:

C:\Projects\BalloonRushUnity6

Wait for Unity to compile.

## Use

1. STOP Play Mode.
2. Clear the Unity Console.
3. Run:

   Tools > Balloon Rush > Missing Scripts > Enable TRANSIENT Watcher

4. Press Play.
5. Let the warnings happen.

If a short-lived GameObject contains a missing MonoBehaviour, the watcher should produce a red message:

TRANSIENT MISSING SCRIPT CAUGHT

with:
- scene
- full hierarchy path
- missing component count
- Instance ID

Click the red watcher message and Unity should select/ping the object if it still exists.

## When finished

Tools > Balloon Rush > Missing Scripts > Disable TRANSIENT Watcher
