using BalloonRush.Core;
using BalloonRush.Gameplay;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BalloonRush.UI
{
    /// <summary>
    /// Development/service diagnostics. Escape opens and closes the panel through
    /// GameManager; F2-F6 only act while the panel is visible so accidental keys do
    /// not change a live game.
    /// </summary>
    public sealed class DebugPanelManager : MonoBehaviour
    {
        private GameManager gameManager;
        private UIManager uiManager;
        private float nextRefresh;
        private float smoothFps = 60f;

        public void Configure(GameManager game, UIManager ui)
        {
            gameManager = game;
            uiManager = ui;
        }

        public bool IsDebugAllowed
        {
            get
            {
                return Application.isEditor ||
                       Debug.isDebugBuild ||
                       (GameServices.Config != null && GameServices.Config.allowDebugShortcutsInRelease);
            }
        }

        /// <summary>
        /// Toggles the service overlay and returns its new visible state.
        /// </summary>
        public bool TogglePanel()
        {
            if (uiManager == null || !IsDebugAllowed)
            {
                uiManager?.SetDebugVisible(false);
                return false;
            }

            bool visible = !uiManager.IsDebugVisible;
            uiManager.SetDebugVisible(visible);
            nextRefresh = 0f;
            return visible;
        }

        public void ClosePanel()
        {
            uiManager?.SetDebugVisible(false);
        }

        private void Update()
        {
            PollDebugKeys();
            smoothFps = Mathf.Lerp(smoothFps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.08f);
            if (uiManager == null || !uiManager.IsDebugVisible || Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + 0.2f;
            ScoreManager score = gameManager != null ? gameManager.ScoreManager : null;
            ComboManager combo = gameManager != null ? gameManager.ComboManager : null;
            LaneManager lanes = gameManager != null ? gameManager.LaneManager : null;
            BalloonManager balloons = gameManager != null ? gameManager.BalloonManager : null;
            BalloonPool pool = gameManager != null ? gameManager.BalloonPool : null;
            DifficultyManager difficulty = gameManager != null ? gameManager.DifficultyManager : null;
            GoldenRoundManager golden = gameManager != null ? gameManager.GoldenRoundManager : null;

            uiManager.SetDebugText(
                "DEVELOPER / SERVICE CONTROLS\n" +
                "ESC CLOSE    M OPERATOR MENU\n\n" +
                $"FPS: {smoothFps:0}\n" +
                $"STATE: {GameServices.State?.CurrentState}\n" +
                $"LANE: {(lanes != null ? lanes.SelectedLane + 1 : 0)}\n" +
                $"COMBO: {(combo != null ? combo.CurrentCombo : 0)}\n" +
                $"TICKETS: {(score != null ? score.Tickets : 0)}\n" +
                $"SCORE: {(score != null ? score.Score : 0):N0}\n" +
                $"BALLOONS: {(balloons != null ? balloons.ActiveBalloonCount : 0)}\n" +
                $"POOL: {(pool != null ? pool.AvailableCount : 0)}/{(pool != null ? pool.TotalCount : 0)}\n" +
                $"DIFFICULTY: {(difficulty != null ? difficulty.CurrentDifficultyLabel : "-")}\n" +
                $"GOLDEN: {(golden != null && golden.IsActive ? golden.TimeRemaining.ToString("0.0") : "OFF")}\n" +
                $"BUILD: {(GameServices.Config != null ? GameServices.Config.buildVersion : Application.version)}\n" +
                $"SERIAL: {(GameServices.Settings != null && GameServices.Settings.Current != null && GameServices.Settings.Current.hardwareEnabled ? "ENABLED" : "OFF")}\n" +
                $"PENDING TICKETS: {(GameServices.Tickets != null ? GameServices.Tickets.TicketsRemaining : 0)}\n\n" +
                "F2 GOLDEN BALLOON    F3 BOMB\n" +
                "F4 GOLDEN ROUND      F5 JACKPOT\n" +
                "F6 END GAME");
        }

        private void PollDebugKeys()
        {
            if (!IsDebugAllowed)
            {
                if (uiManager != null && uiManager.IsDebugVisible)
                {
                    uiManager.SetDebugVisible(false);
                }
                return;
            }

            // Debug action keys are intentionally ignored until Escape opens the panel.
            if (uiManager == null || !uiManager.IsDebugVisible || gameManager == null)
            {
                return;
            }

            bool f2 = false;
            bool f3 = false;
            bool f4 = false;
            bool f5 = false;
            bool f6 = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                f2 = keyboard.f2Key.wasPressedThisFrame;
                f3 = keyboard.f3Key.wasPressedThisFrame;
                f4 = keyboard.f4Key.wasPressedThisFrame;
                f5 = keyboard.f5Key.wasPressedThisFrame;
                f6 = keyboard.f6Key.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            f2 = UnityEngine.Input.GetKeyDown(KeyCode.F2);
            f3 = UnityEngine.Input.GetKeyDown(KeyCode.F3);
            f4 = UnityEngine.Input.GetKeyDown(KeyCode.F4);
            f5 = UnityEngine.Input.GetKeyDown(KeyCode.F5);
            f6 = UnityEngine.Input.GetKeyDown(KeyCode.F6);
#endif
            if (f2) gameManager.BalloonSpawner?.SpawnKindForDebug(BalloonKind.GoldenTrigger);
            if (f3) gameManager.BalloonSpawner?.SpawnKindForDebug(BalloonKind.Bomb);
            if (f4) gameManager.GoldenRoundManager?.StartGoldenRound();
            if (f5) gameManager.DebugTriggerJackpot();
            if (f6) gameManager.DebugEndGame();
        }
    }
}
