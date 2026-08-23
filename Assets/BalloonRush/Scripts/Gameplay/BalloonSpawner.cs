using BalloonRush.Core;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class BalloonSpawner : MonoBehaviour
    {
        [SerializeField] private BalloonPool pool;
        [SerializeField] private BalloonManager balloonManager;
        [SerializeField] private LaneManager laneManager;
        [SerializeField] private DifficultyManager difficultyManager;
        [SerializeField] private BalloonDefinition[] definitions;

        private readonly float[] laneCooldowns = new float[3];
        private OperatorSettings settings;
        private bool spawning;
        private bool rushMode;
        private bool goldenMode;
        private bool suspendRegularSpawns;
        private float spawnTimer;
        private int lastLane = -1;
        private BalloonDefinition goldenJackpotDefinition;

        public bool IsSpawning => spawning;
        public bool IsGoldenMode => goldenMode;

        public void Configure(
            BalloonPool configuredPool,
            BalloonManager configuredManager,
            LaneManager configuredLaneManager,
            DifficultyManager configuredDifficulty,
            BalloonDefinition[] configuredDefinitions,
            OperatorSettings operatorSettings)
        {
            pool = configuredPool;
            balloonManager = configuredManager;
            laneManager = configuredLaneManager;
            difficultyManager = configuredDifficulty;
            definitions = configuredDefinitions;
            settings = operatorSettings;
            EnsureDefinitions();
        }

        public void ApplySettings(OperatorSettings operatorSettings)
        {
            settings = operatorSettings;
            EnsureDefinitions();
        }

        public bool SpawnKindForDebug(BalloonKind kind)
        {
            EnsureDefinitions();
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].Kind == kind)
                {
                    return SpawnSpecific(definitions[i]);
                }
            }
            return false;
        }

        public void BeginSpawning()
        {
            EnsureDefinitions();
            spawning = true;
            rushMode = false;
            goldenMode = false;
            suspendRegularSpawns = false;
            spawnTimer = 0.15f;
            for (int i = 0; i < laneCooldowns.Length; i++) laneCooldowns[i] = 0f;
        }

        public void StopSpawning()
        {
            spawning = false;
        }

        public void SetRushMode(bool enabled)
        {
            rushMode = enabled;
        }

        public void SetGoldenMode(bool enabled)
        {
            goldenMode = enabled;
            suspendRegularSpawns = false;
            if (enabled)
            {
                spawnTimer = Mathf.Min(spawnTimer, 0.2f);
            }
        }

        public bool SpawnFinalGoldenBalloon()
        {
            EnsureDefinitions();
            if (goldenJackpotDefinition == null)
            {
                return false;
            }

            suspendRegularSpawns = true;
            return SpawnSpecific(goldenJackpotDefinition, 1);
        }

        public bool SpawnSpecific(BalloonDefinition definition, int forcedLane = -1)
        {
            if (definition == null || pool == null || balloonManager == null || laneManager == null)
            {
                return false;
            }

            Balloon balloon = pool.Acquire();
            if (balloon == null)
            {
                return false;
            }

            int lane = forcedLane >= 0 ? Mathf.Clamp(forcedLane, 0, 2) : ChooseLane();
            GameConfig config = GameServices.Config;
            float spawnY = config != null ? config.spawnY : -6.8f;
            float despawnY = config != null ? config.despawnY : 6.8f;
            float speed = difficultyManager != null
                ? difficultyManager.GetBalloonSpeed(rushMode, goldenMode)
                : (settings != null ? settings.balloonBaseSpeed : 2.65f);

            // The final crown balloon must remain reachable even when an operator has
            // configured a very slow base speed. Other balloons continue to use the
            // normal difficulty calculation.
            if (definition.Kind == BalloonKind.GoldenJackpot)
            {
                speed = Mathf.Max(speed, 4.25f);
            }

            balloon.transform.SetParent(pool.transform, false);
            balloon.Activate(
                balloonManager,
                definition,
                lane,
                laneManager.GetLanePosition(lane, spawnY),
                speed,
                despawnY,
                pool.Release);
            balloonManager.RegisterSpawnedBalloon(balloon);
            laneCooldowns[lane] = 0.28f;
            lastLane = lane;
            return true;
        }

        private void Update()
        {
            for (int i = 0; i < laneCooldowns.Length; i++)
            {
                laneCooldowns[i] = Mathf.Max(0f, laneCooldowns[i] - Time.deltaTime);
            }

            if (!spawning || suspendRegularSpawns)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            BalloonDefinition definition = ChooseDefinition();
            if (definition != null)
            {
                SpawnSpecific(definition);
            }

            spawnTimer = difficultyManager != null
                ? difficultyManager.GetSpawnInterval(rushMode, goldenMode)
                : (settings != null ? settings.spawnInterval : 1.0f);
        }

        private int ChooseLane()
        {
            int candidate = UnityEngine.Random.Range(0, 3);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                candidate = UnityEngine.Random.Range(0, 3);
                if (laneCooldowns[candidate] <= 0f && (candidate != lastLane || attempt >= 3))
                {
                    return candidate;
                }
            }
            return candidate;
        }

        private BalloonDefinition ChooseDefinition()
        {
            EnsureDefinitions();
            float total = 0f;
            for (int i = 0; i < definitions.Length; i++)
            {
                total += GetEffectiveWeight(definitions[i]);
            }

            if (total <= 0f)
            {
                return definitions.Length > 0 ? definitions[0] : null;
            }

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < definitions.Length; i++)
            {
                BalloonDefinition definition = definitions[i];
                float weight = GetEffectiveWeight(definition);
                if (weight <= 0f)
                {
                    continue;
                }

                roll -= weight;
                if (roll <= 0f)
                {
                    return definition;
                }
            }

            return definitions[0];
        }

        private float GetEffectiveWeight(BalloonDefinition definition)
        {
            if (definition == null || definition.Kind == BalloonKind.GoldenJackpot)
            {
                return 0f;
            }

            float dangerScale = difficultyManager != null ? difficultyManager.GetDangerMultiplier() : 1f;
            if (goldenMode)
            {
                if (definition.IsDangerous || definition.Kind == BalloonKind.GoldenTrigger)
                {
                    return 0f;
                }

                float greenWeight = settings != null ? settings.greenSpawnWeight : definition.SpawnWeight;
                float blueWeight = settings != null ? settings.blueSpawnWeight : definition.SpawnWeight;
                float multiplierWeight = settings != null ? settings.multiplierSpawnWeight : definition.SpawnWeight;
                float mysteryWeight = settings != null ? settings.mysterySpawnWeight : definition.SpawnWeight;
                switch (definition.Kind)
                {
                    case BalloonKind.Green: return greenWeight;
                    case BalloonKind.Blue: return Mathf.Max(0.2f, blueWeight * 1.8f);
                    case BalloonKind.Multiplier: return Mathf.Max(0.12f, multiplierWeight * 1.5f);
                    case BalloonKind.Mystery: return Mathf.Max(0.12f, mysteryWeight * 1.5f);
                    default: return definition.SpawnWeight;
                }
            }

            if (settings == null)
            {
                return definition.SpawnWeight;
            }

            switch (definition.Kind)
            {
                case BalloonKind.Green: return settings.greenSpawnWeight;
                case BalloonKind.Blue: return settings.blueSpawnWeight;
                case BalloonKind.Bomb: return settings.bombSpawnWeight * dangerScale;
                case BalloonKind.SuperBomb: return settings.superBombSpawnWeight * dangerScale;
                case BalloonKind.GoldenTrigger: return settings.goldenSpawnWeight;
                case BalloonKind.Mystery: return settings.mysterySpawnWeight;
                case BalloonKind.Multiplier: return settings.multiplierSpawnWeight;
                default: return definition.SpawnWeight;
            }
        }

        private void EnsureDefinitions()
        {
            if (definitions == null || definitions.Length == 0)
            {
                definitions = Resources.LoadAll<BalloonDefinition>("BalloonDefinitions");
            }

            if (definitions == null || definitions.Length == 0)
            {
                definitions = CreateRuntimeDefinitions();
            }

            goldenJackpotDefinition = null;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].Kind == BalloonKind.GoldenJackpot)
                {
                    goldenJackpotDefinition = definitions[i];
                    break;
                }
            }
        }

        private static BalloonDefinition[] CreateRuntimeDefinitions()
        {
            return new[]
            {
                CreateDefinition("green", "Green +1", BalloonKind.Green, new Color(0.2f, 0.9f, 0.25f), 100, 1, 1.0f, false, BalloonSpecialBehavior.None),
                CreateDefinition("blue", "Blue +5", BalloonKind.Blue, new Color(0.1f, 0.55f, 1f), 350, 5, 0.08f, false, BalloonSpecialBehavior.None),
                CreateDefinition("x2", "Payout x2", BalloonKind.Multiplier, new Color(0.65f, 0.2f, 1f), 250, 0, 0.025f, false, BalloonSpecialBehavior.DoublePayout),
                CreateDefinition("mystery", "Mystery", BalloonKind.Mystery, new Color(1f, 0.72f, 0.05f), 250, 0, 0.03f, false, BalloonSpecialBehavior.MysteryReward),
                CreateDefinition("bomb", "Bomb", BalloonKind.Bomb, new Color(0.9f, 0.08f, 0.08f), 0, 0, 0.10f, true, BalloonSpecialBehavior.Dangerous),
                CreateDefinition("superbomb", "Super Bomb", BalloonKind.SuperBomb, new Color(0.07f, 0.07f, 0.08f), 0, 0, 0.01f, true, BalloonSpecialBehavior.Dangerous),
                CreateDefinition("golden", "Golden Balloon", BalloonKind.GoldenTrigger, new Color(1f, 0.72f, 0.02f), 600, 1, 0.0004f, false, BalloonSpecialBehavior.StartGoldenRound),
                CreateDefinition("jackpot", "Golden Jackpot", BalloonKind.GoldenJackpot, new Color(1f, 0.84f, 0.08f), 1000, 0, 0f, false, BalloonSpecialBehavior.ResolveJackpot)
            };
        }

        private static BalloonDefinition CreateDefinition(
            string id,
            string displayName,
            BalloonKind kind,
            Color color,
            int points,
            int tickets,
            float weight,
            bool dangerous,
            BalloonSpecialBehavior behavior)
        {
            BalloonDefinition definition = ScriptableObject.CreateInstance<BalloonDefinition>();
            definition.Configure(id, displayName, kind, null, color, points, tickets, weight, dangerous, behavior);
            return definition;
        }
    }
}
