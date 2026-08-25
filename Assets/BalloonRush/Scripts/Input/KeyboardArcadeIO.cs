using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BalloonRush.Input
{
    /// <summary>
    /// Keyboard + USB joystick cabinet implementation of IArcadeIO.
    ///
    /// Real WOWCade cabinet bindings:
    /// LEFT       = LeftArrow / A / JoystickButton1
    /// POP / UP   = UpArrow / Space / JoystickButton2
    /// RIGHT      = RightArrow / D / JoystickButton7
    /// OPERATOR   = M / JoystickButton4 (cabinet key switch)
    ///
    /// Keyboard development shortcuts remain available.
    /// </summary>
    public sealed class KeyboardArcadeIO : MonoBehaviour, IArcadeIO
    {
        public event Action LeftPressed;
        public event Action RightPressed;
        public event Action PopPressed;
        public event Action StartPressed;
        public event Action<CreditPulseType> CreditPulse;
        public event Action OperatorPressed;
        public event Action BackPressed;

        public bool IsAvailable => true;

        public void StartIO()
        {
            enabled = true;
#if !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogWarning(
                "Balloon Rush: exact cabinet JoystickButton1/2/4/7 bindings require " +
                "Player Settings > Active Input Handling to include Input Manager (Old) / Both. " +
                "Keyboard and Input System fallbacks remain available.");
#endif
        }

        public void StopIO()
        {
            enabled = false;
        }

        public void SendTicketPulse(int ticketCount)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ticketCount > 0)
            {
                Debug.Log($"[Balloon Rush Development Ticket Output] {ticketCount}");
            }
#endif
        }

        private void Update()
        {
            // Prefer the legacy Input Manager whenever it is available because the
            // physical cabinet USB encoder exposes the exact controls as raw
            // JoystickButton1 / 2 / 4 / 7 values. If the project is set to Both,
            // this path intentionally runs instead of polling both systems and
            // accidentally double-firing a cabinet button.
#if ENABLE_LEGACY_INPUT_MANAGER
            PollLegacyInput();
#elif ENABLE_INPUT_SYSTEM
            PollInputSystem();
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private void PollLegacyInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) ||
                UnityEngine.Input.GetKeyDown(KeyCode.A) ||
                UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                LeftPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) ||
                UnityEngine.Input.GetKeyDown(KeyCode.D) ||
                UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                RightPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Space) ||
                UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton2))
            {
                PopPressed?.Invoke();
            }

            // Keyboard-only development start remains separate from POP. Do not
            // map JoystickButton2 to StartPressed or one physical press could
            // consume two stacked credits through two independent start paths.
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) ||
                UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                StartPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                CreditPulse?.Invoke(CreditPulseType.Coin);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.V))
            {
                CreditPulse?.Invoke(CreditPulseType.CardSwipe);
            }

            // Physical keyed service switch on the cabinet.
            if (UnityEngine.Input.GetKeyDown(KeyCode.M) ||
                UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                OperatorPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                BackPressed?.Invoke();
            }
        }
#endif

#if ENABLE_INPUT_SYSTEM
        private void PollInputSystem()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            bool left = keyboard != null &&
                        (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame);
            left |= gamepad != null && gamepad.dpad.left.wasPressedThisFrame;
            if (left)
            {
                LeftPressed?.Invoke();
            }

            bool right = keyboard != null &&
                         (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
            right |= gamepad != null && gamepad.dpad.right.wasPressedThisFrame;
            if (right)
            {
                RightPressed?.Invoke();
            }

            bool pop = keyboard != null &&
                       (keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            pop |= gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
            if (pop)
            {
                PopPressed?.Invoke();
            }

            bool start = keyboard != null &&
                         (keyboard.enterKey.wasPressedThisFrame ||
                          keyboard.numpadEnterKey.wasPressedThisFrame ||
                          keyboard.pKey.wasPressedThisFrame);
            start |= gamepad != null && gamepad.startButton.wasPressedThisFrame;
            if (start)
            {
                StartPressed?.Invoke();
            }

            if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
            {
                CreditPulse?.Invoke(CreditPulseType.Coin);
            }

            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                CreditPulse?.Invoke(CreditPulseType.CardSwipe);
            }

            // New Input System fallback. The real cabinet should use the legacy
            // raw JoystickButton4 mapping above; Select is only a development/gamepad fallback.
            bool operatorPressed = keyboard != null && keyboard.mKey.wasPressedThisFrame;
            operatorPressed |= gamepad != null && gamepad.selectButton.wasPressedThisFrame;
            if (operatorPressed)
            {
                OperatorPressed?.Invoke();
            }

            bool back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            if (back)
            {
                BackPressed?.Invoke();
            }
        }
#endif
    }
}
