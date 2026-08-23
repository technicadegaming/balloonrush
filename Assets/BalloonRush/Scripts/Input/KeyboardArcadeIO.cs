using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BalloonRush.Input
{
    /// <summary>
    /// Development/cabinet-keyboard implementation of IArcadeIO.
    /// The gameplay systems only consume abstract actions; key bindings remain here.
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
#if ENABLE_INPUT_SYSTEM
            PollInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
            PollLegacyInput();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void PollInputSystem()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            bool left = keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame);
            left |= gamepad != null && (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftShoulder.wasPressedThisFrame);
            if (left)
            {
                LeftPressed?.Invoke();
            }

            bool right = keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
            right |= gamepad != null && (gamepad.dpad.right.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame);
            if (right)
            {
                RightPressed?.Invoke();
            }

            bool pop = keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            pop |= gamepad != null && (gamepad.dpad.up.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame);
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

            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                OperatorPressed?.Invoke();
            }

            bool back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            back |= gamepad != null && gamepad.selectButton.wasPressedThisFrame;
            if (back)
            {
                BackPressed?.Invoke();
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private void PollLegacyInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                LeftPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D))
            {
                RightPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                PopPressed?.Invoke();
            }

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

            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                OperatorPressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                BackPressed?.Invoke();
            }
        }
#endif
    }
}
