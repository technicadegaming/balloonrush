using BalloonRush.Effects;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using BalloonRush.UI;
using UnityEngine;

namespace BalloonRush.Core
{
    /// <summary>
    /// Defensive runtime wiring for MainGame.
    ///
    /// The generated scene normally contains all references already, but repeated UI/editor
    /// patching can leave one of the serialized gameplay references disconnected. This component
    /// runs before GameManager.Start and reconnects the critical gameplay graph by type/name.
    /// It does not start the round itself; GameManager remains the authority for countdown,
    /// gameplay, scoring, results, tickets, and scene flow.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MainGameRuntimeRepair : MonoBehaviour
    {
        private void Awake()
        {
            Time.timeScale = 1f;

            GameManager gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            RoundManager roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
            BalloonManager balloonManager = FindFirstObjectByType<BalloonManager>(FindObjectsInactive.Include);
            BalloonSpawner balloonSpawner = FindFirstObjectByType<BalloonSpawner>(FindObjectsInactive.Include);
            BalloonPool balloonPool = FindFirstObjectByType<BalloonPool>(FindObjectsInactive.Include);
            LaneManager laneManager = FindFirstObjectByType<LaneManager>(FindObjectsInactive.Include);
            HitZone hitZone = FindFirstObjectByType<HitZone>(FindObjectsInactive.Include);
            ComboManager comboManager = FindFirstObjectByType<ComboManager>(FindObjectsInactive.Include);
            ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Include);
            DifficultyManager difficultyManager = FindFirstObjectByType<DifficultyManager>(FindObjectsInactive.Include);
            GoldenRoundManager goldenRoundManager = FindFirstObjectByType<GoldenRoundManager>(FindObjectsInactive.Include);
            JackpotManager jackpotManager = FindFirstObjectByType<JackpotManager>(FindObjectsInactive.Include);

            UIManager uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            DebugPanelManager debugPanelManager = FindFirstObjectByType<DebugPanelManager>(FindObjectsInactive.Include);
            EffectsManager effectsManager = FindFirstObjectByType<EffectsManager>(FindObjectsInactive.Include);
            ScreenShake screenShake = FindFirstObjectByType<ScreenShake>(FindObjectsInactive.Include);
            FloatingTextPool floatingTextPool = FindFirstObjectByType<FloatingTextPool>(FindObjectsInactive.Include);

            EnsureAudioListener();
            RepairLaneManager(laneManager);

            OperatorSettings settings = GameServices.Settings != null
                ? GameServices.Settings.Current
                : null;

            if (balloonSpawner != null)
            {
                BalloonDefinition[] definitions = Resources.LoadAll<BalloonDefinition>("BalloonDefinitions");
                balloonSpawner.Configure(
                    balloonPool,
                    balloonManager,
                    laneManager,
                    difficultyManager,
                    definitions,
                    settings);
            }

            if (gameManager != null)
            {
                gameManager.enabled = true;
                gameManager.Configure(
                    roundManager,
                    balloonManager,
                    balloonSpawner,
                    balloonPool,
                    laneManager,
                    hitZone,
                    comboManager,
                    scoreManager,
                    difficultyManager,
                    goldenRoundManager,
                    jackpotManager,
                    uiManager,
                    effectsManager,
                    screenShake,
                    floatingTextPool,
                    debugPanelManager);
            }

            ReportStatus(
                gameManager,
                roundManager,
                balloonManager,
                balloonSpawner,
                balloonPool,
                laneManager,
                hitZone,
                comboManager,
                scoreManager,
                difficultyManager,
                goldenRoundManager,
                jackpotManager,
                uiManager);
        }

        private static void RepairLaneManager(LaneManager laneManager)
        {
            if (laneManager == null)
                return;

            Transform field = FindTransformByName("Gameplay Field");
            if (field == null)
                return;

            Transform[] anchors = new Transform[3];
            SpriteRenderer[] highlights = new SpriteRenderer[3];

            for (int i = 0; i < 3; i++)
            {
                Transform lane = FindChildRecursive(field, "Lane " + (i + 1));
                anchors[i] = lane;
                highlights[i] = lane != null ? lane.GetComponent<SpriteRenderer>() : null;
            }

            if (anchors[0] != null && anchors[1] != null && anchors[2] != null)
                laneManager.Configure(anchors, highlights);
        }

        private static void EnsureAudioListener()
        {
            AudioListener listener = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);
            if (listener != null)
            {
                listener.enabled = true;
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
                camera = FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

            if (camera != null)
                camera.gameObject.AddComponent<AudioListener>();
        }

        private static Transform FindTransformByName(string name)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in all)
            {
                if (t != null && t.name == name)
                    return t;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
                return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t != null && t.name == name)
                    return t;
            }
            return null;
        }

        private static void ReportStatus(
            GameManager gameManager,
            RoundManager roundManager,
            BalloonManager balloonManager,
            BalloonSpawner balloonSpawner,
            BalloonPool balloonPool,
            LaneManager laneManager,
            HitZone hitZone,
            ComboManager comboManager,
            ScoreManager scoreManager,
            DifficultyManager difficultyManager,
            GoldenRoundManager goldenRoundManager,
            JackpotManager jackpotManager,
            UIManager uiManager)
        {
            bool criticalReady =
                gameManager != null &&
                roundManager != null &&
                balloonManager != null &&
                balloonSpawner != null &&
                balloonPool != null &&
                laneManager != null &&
                hitZone != null &&
                comboManager != null &&
                scoreManager != null &&
                difficultyManager != null &&
                goldenRoundManager != null &&
                jackpotManager != null &&
                uiManager != null;

            if (criticalReady)
            {
                Debug.Log(
                    "Balloon Rush runtime wiring PASS: GameManager, round, spawner, pool, lanes, " +
                    "hit zone, score, combo, difficulty, golden round, jackpot and UI are connected.",
                    gameManager);
                return;
            }

            Debug.LogError(
                "Balloon Rush runtime wiring FAILED. Missing:" +
                Missing(" GameManager", gameManager) +
                Missing(" RoundManager", roundManager) +
                Missing(" BalloonManager", balloonManager) +
                Missing(" BalloonSpawner", balloonSpawner) +
                Missing(" BalloonPool", balloonPool) +
                Missing(" LaneManager", laneManager) +
                Missing(" HitZone", hitZone) +
                Missing(" ComboManager", comboManager) +
                Missing(" ScoreManager", scoreManager) +
                Missing(" DifficultyManager", difficultyManager) +
                Missing(" GoldenRoundManager", goldenRoundManager) +
                Missing(" JackpotManager", jackpotManager) +
                Missing(" UIManager", uiManager),
                gameManager);
        }

        private static string Missing(string label, Object value)
        {
            return value == null ? label : string.Empty;
        }
    }
}
