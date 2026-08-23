#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    /// <summary>
    /// Monte Carlo balancing aid. This is an estimate, not payout certification;
    /// final tuning must use real cabinet telemetry and actual prize costs.
    /// </summary>
    public sealed class PayoutSimulatorWindow : EditorWindow
    {
        private const string ConfigPath = "Assets/BalloonRush/Resources/BalloonRushConfig.asset";

        private int simulations = 25000;
        private int seed = 20260822;
        private float successfulPopRate = 0.78f;
        private float perfectShare = 0.24f;
        private float greatShare = 0.43f;
        private float bombAvoidanceRate = 0.90f;
        private float goldenFinalSuccessRate = 0.42f;
        private Vector2 scroll;
        private SimulationReport lastReport;

        [MenuItem("Tools/Balloon Rush/Payout Simulator", priority = 5)]
        private static void Open()
        {
            GetWindow<PayoutSimulatorWindow>("Balloon Rush Payout");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Balloon Rush Payout Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Uses the same ticket multipliers, regular cap, jackpot cap, and mystery Golden chance as gameplay. Compare its estimate with real sessions.csv data before final cabinet deployment.",
                MessageType.Info);

            simulations = EditorGUILayout.IntSlider("Simulated games", simulations, 1000, 100000);
            seed = EditorGUILayout.IntField("Random seed", seed);
            successfulPopRate = EditorGUILayout.Slider("Successful pop rate", successfulPopRate, 0.20f, 0.99f);
            perfectShare = EditorGUILayout.Slider("Perfect share of hits", perfectShare, 0f, 0.80f);
            greatShare = EditorGUILayout.Slider("Great share of hits", greatShare, 0f, 0.90f);
            bombAvoidanceRate = EditorGUILayout.Slider("Bomb avoidance rate", bombAvoidanceRate, 0.20f, 1f);
            goldenFinalSuccessRate = EditorGUILayout.Slider("Golden final success", goldenFinalSuccessRate, 0f, 1f);

            if (perfectShare + greatShare > 1f)
            {
                EditorGUILayout.HelpBox("Perfect + Great cannot exceed 1. Values are normalized during simulation.", MessageType.Warning);
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("RUN SIMULATION", GUILayout.Height(34f)))
            {
                lastReport = RunSimulation();
            }

            if (lastReport != null)
            {
                DrawReport(lastReport);
                if (GUILayout.Button("EXPORT REPORT CSV"))
                {
                    ExportReport(lastReport);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private SimulationReport RunSimulation()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = CreateInstance<GameConfig>();
                config.payoutConfig = CreateInstance<PayoutConfig>();
                config.difficultyConfig = CreateInstance<DifficultyConfig>();
            }

            OperatorSettings settings = config.CreateDefaultSettings();
            settings.Validate();
            PayoutConfig payoutConfig = config.payoutConfig != null ? config.payoutConfig : CreateInstance<PayoutConfig>();
            System.Random random = new System.Random(seed);
            int count = Mathf.Clamp(simulations, 1000, 100000);
            List<int> payouts = new List<int>(count);
            int jackpots = 0;
            int capHits = 0;
            long total = 0;

            for (int i = 0; i < count; i++)
            {
                int payout = SimulateRound(random, settings, payoutConfig, out bool jackpot);
                payouts.Add(payout);
                total += payout;
                if (jackpot) jackpots++;
                if (payout >= settings.maxTicketPayout) capHits++;
            }

            payouts.Sort();
            double average = (double)total / count;
            SimulationReport report = new SimulationReport
            {
                games = count,
                seed = seed,
                average = average,
                median = Percentile(payouts, 0.50f),
                p75 = Percentile(payouts, 0.75f),
                p90 = Percentile(payouts, 0.90f),
                p95 = Percentile(payouts, 0.95f),
                p99 = Percentile(payouts, 0.99f),
                minimum = payouts[0],
                maximum = payouts[payouts.Count - 1],
                jackpotRate = (double)jackpots / count,
                capRate = (double)capHits / count,
                estimatedCostCents = EconomyMath.EstimatePrizeCostCents((float)average, settings),
                estimatedCostPercent = EconomyMath.EstimatePrizeCostPercent((float)average, settings),
                targetAverageTickets = EconomyMath.CalculateTargetAverageTickets(settings),
                settings = settings.Clone()
            };
            Repaint();
            return report;
        }

        private int SimulateRound(System.Random random, OperatorSettings settings, PayoutConfig payoutConfig, out bool jackpotWon)
        {
            float averageInterval = Mathf.Max(0.2f, settings.spawnInterval * 0.76f);
            int spawnCount = Mathf.Max(1, Mathf.RoundToInt(settings.gameDuration / averageInterval));
            float regularTickets = 0f;
            int bonusTickets = 0;
            int jackpotTickets = 0;
            int combo = 0;
            int x2RemainingSpawns = 0;
            bool goldenTriggered = false;
            jackpotWon = false;

            float greenWeight = Mathf.Max(0f, settings.greenSpawnWeight);
            float blueWeight = Mathf.Max(0f, settings.blueSpawnWeight);
            float multiplierWeight = Mathf.Max(0f, settings.multiplierSpawnWeight);
            float mysteryWeight = Mathf.Max(0f, settings.mysterySpawnWeight);
            float goldenWeight = Mathf.Max(0f, settings.goldenSpawnWeight);
            float normalBombWeight = Mathf.Max(0f, settings.bombSpawnWeight);
            float superBombWeight = Mathf.Max(0f, settings.superBombSpawnWeight);
            float totalWeight = greenWeight + blueWeight + multiplierWeight + mysteryWeight + goldenWeight + normalBombWeight + superBombWeight;
            if (totalWeight <= 0.0001f)
            {
                greenWeight = 1f;
                totalWeight = 1f;
            }

            for (int spawn = 0; spawn < spawnCount; spawn++)
            {
                float roll = (float)random.NextDouble() * totalWeight;
                BalloonKind kind;
                if ((roll -= greenWeight) < 0f) kind = BalloonKind.Green;
                else if ((roll -= blueWeight) < 0f) kind = BalloonKind.Blue;
                else if ((roll -= multiplierWeight) < 0f) kind = BalloonKind.Multiplier;
                else if ((roll -= mysteryWeight) < 0f) kind = BalloonKind.Mystery;
                else if ((roll -= goldenWeight) < 0f) kind = BalloonKind.GoldenTrigger;
                else if ((roll -= normalBombWeight) < 0f) kind = BalloonKind.Bomb;
                else kind = BalloonKind.SuperBomb;

                if (kind == BalloonKind.Bomb || kind == BalloonKind.SuperBomb)
                {
                    if (random.NextDouble() > bombAvoidanceRate)
                    {
                        combo = 0;
                        regularTickets = Mathf.Max(0f, regularTickets - settings.bombTicketPenalty);
                        if (kind == BalloonKind.SuperBomb)
                        {
                            x2RemainingSpawns = 0;
                        }
                    }
                    continue;
                }

                if (random.NextDouble() > successfulPopRate)
                {
                    combo = 0;
                    continue;
                }

                combo++;
                TimingRating rating = RollTiming(random);
                float activePayoutMultiplier = x2RemainingSpawns > 0 ? 2f : 1f;

                switch (kind)
                {
                    case BalloonKind.Green:
                        regularTickets = AddRegular(
                            regularTickets,
                            CalculateNormalAward(settings.greenTickets, rating, combo, activePayoutMultiplier, settings, payoutConfig),
                            settings.regularTicketCap);
                        break;
                    case BalloonKind.Blue:
                        regularTickets = AddRegular(
                            regularTickets,
                            CalculateNormalAward(settings.blueTickets, rating, combo, activePayoutMultiplier, settings, payoutConfig),
                            settings.regularTicketCap);
                        break;
                    case BalloonKind.Multiplier:
                        x2RemainingSpawns = Mathf.Max(x2RemainingSpawns, Mathf.RoundToInt(settings.x2Duration / averageInterval));
                        break;
                    case BalloonKind.Mystery:
                        ResolveMystery(
                            random,
                            settings,
                            ref regularTickets,
                            ref bonusTickets,
                            ref jackpotTickets,
                            ref combo,
                            ref x2RemainingSpawns,
                            averageInterval,
                            ref goldenTriggered,
                            ref jackpotWon);
                        break;
                    case BalloonKind.GoldenTrigger:
                        regularTickets = AddRegular(
                            regularTickets,
                            CalculateNormalAward(settings.goldenTriggerTickets, rating, combo, activePayoutMultiplier, settings, payoutConfig),
                            settings.regularTicketCap);
                        if (!goldenTriggered)
                        {
                            goldenTriggered = true;
                            SimulateGoldenRoundRegularPops(random, settings, payoutConfig, ref regularTickets, ref combo, ref x2RemainingSpawns, averageInterval);
                            ResolveGoldenFinal(random, settings, ref bonusTickets, ref jackpotTickets, out jackpotWon);
                        }
                        break;
                }

                if (x2RemainingSpawns > 0)
                {
                    x2RemainingSpawns--;
                }
            }

            int regularWhole = Mathf.FloorToInt(Mathf.Min(settings.regularTicketCap, regularTickets) + 0.0001f);
            int total = Mathf.Clamp(regularWhole + bonusTickets + jackpotTickets, 0, settings.maxTicketPayout);
            return Mathf.Max(Mathf.Min(settings.minimumTicketPayout, settings.maxTicketPayout), total);
        }

        private void SimulateGoldenRoundRegularPops(
            System.Random random,
            OperatorSettings settings,
            PayoutConfig payoutConfig,
            ref float regularTickets,
            ref int combo,
            ref int x2RemainingSpawns,
            float averageInterval)
        {
            int bonusSpawns = Mathf.Clamp(Mathf.RoundToInt(settings.goldenRoundDuration / Mathf.Max(0.35f, averageInterval * 0.75f)), 4, 20);
            for (int i = 0; i < bonusSpawns; i++)
            {
                if (random.NextDouble() > successfulPopRate)
                {
                    combo = 0;
                    continue;
                }

                combo++;
                TimingRating rating = RollTiming(random);
                double kindRoll = random.NextDouble();
                if (kindRoll < 0.70)
                {
                    float multiplier = x2RemainingSpawns > 0 ? 2f : 1f;
                    regularTickets = AddRegular(
                        regularTickets,
                        CalculateNormalAward(settings.greenTickets, rating, combo, multiplier, settings, payoutConfig),
                        settings.regularTicketCap);
                }
                else if (kindRoll < 0.90)
                {
                    float multiplier = x2RemainingSpawns > 0 ? 2f : 1f;
                    regularTickets = AddRegular(
                        regularTickets,
                        CalculateNormalAward(settings.blueTickets, rating, combo, multiplier, settings, payoutConfig),
                        settings.regularTicketCap);
                }
                else
                {
                    x2RemainingSpawns = Mathf.Max(x2RemainingSpawns, Mathf.RoundToInt(settings.x2Duration / averageInterval));
                }

                if (x2RemainingSpawns > 0)
                {
                    x2RemainingSpawns--;
                }
            }
        }

        private TimingRating RollTiming(System.Random random)
        {
            float perfect = Mathf.Clamp01(perfectShare);
            float great = Mathf.Clamp01(greatShare);
            float sum = perfect + great;
            if (sum > 1f)
            {
                perfect /= sum;
                great /= sum;
            }

            double roll = random.NextDouble();
            if (roll < perfect) return TimingRating.Perfect;
            if (roll < perfect + great) return TimingRating.Great;
            return TimingRating.Good;
        }

        private static float CalculateNormalAward(
            int baseTickets,
            TimingRating rating,
            int combo,
            float payoutMultiplier,
            OperatorSettings settings,
            PayoutConfig payoutConfig)
        {
            float goodMultiplier = settings != null ? settings.goodTicketMultiplier : (payoutConfig != null ? payoutConfig.goodTicketMultiplier : 1f);
            float greatMultiplier = settings != null ? settings.greatTicketMultiplier : (payoutConfig != null ? payoutConfig.greatTicketMultiplier : 1f);
            float perfectMultiplier = settings != null ? settings.perfectTicketMultiplier : (payoutConfig != null ? payoutConfig.perfectTicketMultiplier : 1.10f);
            return TicketMath.CalculateRawAward(
                baseTickets,
                rating,
                combo,
                payoutMultiplier,
                goodMultiplier,
                greatMultiplier,
                perfectMultiplier,
                settings.combo5Multiplier,
                settings.combo10Multiplier,
                settings.combo15Multiplier,
                settings.combo20Multiplier,
                settings.combo30Multiplier);
        }

        private void ResolveMystery(
            System.Random random,
            OperatorSettings settings,
            ref float regularTickets,
            ref int bonusTickets,
            ref int jackpotTickets,
            ref int combo,
            ref int x2RemainingSpawns,
            float averageInterval,
            ref bool goldenTriggered,
            ref bool jackpotWon)
        {
            int minimum = settings.mysteryMinimum;
            int maximum = settings.mysteryMaximum;
            int outcome = random.Next(0, 7);
            switch (outcome)
            {
                case 0:
                    regularTickets = AddRegular(regularTickets, minimum, settings.regularTicketCap);
                    break;
                case 1:
                    regularTickets = AddRegular(regularTickets, Mathf.RoundToInt((minimum + maximum) * 0.5f), settings.regularTicketCap);
                    break;
                case 2:
                    regularTickets = AddRegular(regularTickets, random.Next(minimum, maximum + 1), settings.regularTicketCap);
                    break;
                case 3:
                    x2RemainingSpawns = Mathf.Max(x2RemainingSpawns, Mathf.RoundToInt(settings.x2Duration / averageInterval));
                    break;
                case 4:
                    break;
                case 5:
                    combo += 3;
                    break;
                default:
                    if (!goldenTriggered && random.NextDouble() < settings.mysteryGoldenChance)
                    {
                        goldenTriggered = true;
                        ResolveGoldenFinal(random, settings, ref bonusTickets, ref jackpotTickets, out bool mysteryJackpot);
                        jackpotWon |= mysteryJackpot;
                    }
                    else
                    {
                        regularTickets = AddRegular(regularTickets, maximum, settings.regularTicketCap);
                    }
                    break;
            }
        }

        private void ResolveGoldenFinal(
            System.Random random,
            OperatorSettings settings,
            ref int bonusTickets,
            ref int jackpotTickets,
            out bool jackpotWon)
        {
            jackpotWon = false;
            if (random.NextDouble() < goldenFinalSuccessRate)
            {
                TimingRating rating = RollTiming(random);
                if (rating == TimingRating.Perfect)
                {
                    jackpotTickets += settings.jackpotTickets;
                    jackpotWon = true;
                }
                else if (rating == TimingRating.Great)
                {
                    bonusTickets += settings.goldenGreatReward;
                }
                else
                {
                    bonusTickets += settings.goldenGoodReward;
                }
            }
            else
            {
                bonusTickets += settings.goldenMissReward;
            }

            int combined = Mathf.Clamp(bonusTickets + jackpotTickets, 0, settings.maxTicketPayout);
            jackpotTickets = Mathf.Min(jackpotTickets, combined);
            bonusTickets = Mathf.Max(0, combined - jackpotTickets);
        }

        private static float AddRegular(float current, float amount, int cap)
        {
            return Mathf.Clamp(current + Mathf.Max(0f, amount), 0f, Mathf.Max(1, cap));
        }

        private static int Percentile(List<int> sorted, float percentile)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt((sorted.Count - 1) * percentile), 0, sorted.Count - 1);
            return sorted[index];
        }

        private static void DrawReport(SimulationReport report)
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Estimated results", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Games", report.games.ToString("N0", CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Average tickets", report.average.ToString("0.00", CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Target average from economics", report.targetAverageTickets.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Estimated prize cost / play", $"{report.estimatedCostCents:0.00} cents ({report.estimatedCostPercent:0.0}% of ${report.settings.pricePerPlayCents / 100f:0.00})");
            EditorGUILayout.LabelField("Median / P75", $"{report.median} / {report.p75}");
            EditorGUILayout.LabelField("P90 / P95 / P99", $"{report.p90} / {report.p95} / {report.p99}");
            EditorGUILayout.LabelField("Minimum / Maximum", $"{report.minimum} / {report.maximum}");
            EditorGUILayout.LabelField("Estimated jackpot rate", report.jackpotRate.ToString("P3", CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Payout-cap hit rate", report.capRate.ToString("P3", CultureInfo.InvariantCulture));

            if (report.estimatedCostPercent > report.settings.targetPrizeCostPercent)
            {
                EditorGUILayout.HelpBox("Estimated prize cost exceeds the operator target. Lower ticket values/frequencies or verify the entered prize cost per ticket.", MessageType.Warning);
            }
            if (report.capRate > 0.02)
            {
                EditorGUILayout.HelpBox("More than 2% of simulated games hit the hard payout cap. Lower ticket values, multipliers, x2 duration, or Golden frequency.", MessageType.Warning);
            }
        }

        private static void ExportReport(SimulationReport report)
        {
            string path = EditorUtility.SaveFilePanel("Export Balloon Rush payout report", string.Empty, "BalloonRushPayoutReport.csv", "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.WriteLine("games,seed,average,target_average,estimated_cost_cents,estimated_cost_percent,median,p75,p90,p95,p99,minimum,maximum,jackpot_rate,cap_rate");
                writer.WriteLine(string.Join(",", new[]
                {
                    report.games.ToString(CultureInfo.InvariantCulture),
                    report.seed.ToString(CultureInfo.InvariantCulture),
                    report.average.ToString("0.###", CultureInfo.InvariantCulture),
                    report.targetAverageTickets.ToString(CultureInfo.InvariantCulture),
                    report.estimatedCostCents.ToString("0.###", CultureInfo.InvariantCulture),
                    report.estimatedCostPercent.ToString("0.###", CultureInfo.InvariantCulture),
                    report.median.ToString(CultureInfo.InvariantCulture),
                    report.p75.ToString(CultureInfo.InvariantCulture),
                    report.p90.ToString(CultureInfo.InvariantCulture),
                    report.p95.ToString(CultureInfo.InvariantCulture),
                    report.p99.ToString(CultureInfo.InvariantCulture),
                    report.minimum.ToString(CultureInfo.InvariantCulture),
                    report.maximum.ToString(CultureInfo.InvariantCulture),
                    report.jackpotRate.ToString("0.######", CultureInfo.InvariantCulture),
                    report.capRate.ToString("0.######", CultureInfo.InvariantCulture)
                }));
            }

            EditorUtility.RevealInFinder(path);
        }

        [Serializable]
        private sealed class SimulationReport
        {
            public int games;
            public int seed;
            public double average;
            public int targetAverageTickets;
            public float estimatedCostCents;
            public float estimatedCostPercent;
            public int median;
            public int p75;
            public int p90;
            public int p95;
            public int p99;
            public int minimum;
            public int maximum;
            public double jackpotRate;
            public double capRate;
            public OperatorSettings settings;
        }
    }
}
#endif
