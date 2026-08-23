using System;
using System.IO;
using BalloonRush.Core;
using BalloonRush.Input;
using UnityEngine;

namespace BalloonRush.SaveSystem
{
    public sealed class SaveManager : MonoBehaviour
    {
        private const string FileName = "BalloonRushSave.json";
        private const int CurrentSaveVersion = 3;
        private GameConfig gameConfig;

        public GameSaveData Data { get; private set; }
        public string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public string BackupPath => SavePath + ".bak";

        public void Initialize(GameConfig config)
        {
            gameConfig = config;
            Load();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Data = CreateDefaultData();
                    Save();
                    return;
                }

                string json = File.ReadAllText(SavePath);
                Data = JsonUtility.FromJson<GameSaveData>(json);
                bool migrated = MigrateData();
                EnsureValidData();
                if (migrated)
                {
                    Save();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Balloon Rush could not load save data. Attempting backup recovery. {exception.Message}");
                TryBackupCorruptFile();
                if (TryLoadBackup())
                {
                    try
                    {
                        if (File.Exists(SavePath))
                        {
                            File.Delete(SavePath);
                        }
                    }
                    catch
                    {
                        // Save() below still has guarded error handling.
                    }
                    Save();
                    return;
                }

                Data = CreateDefaultData();
                Save();
            }
        }

        public void Save()
        {
            EnsureValidData();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string json = JsonUtility.ToJson(Data, true);
                string temporaryPath = SavePath + ".tmp";
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(SavePath))
                {
                    try
                    {
                        File.Replace(temporaryPath, SavePath, BackupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceWithPortableFallback(temporaryPath);
                    }
                    catch (IOException)
                    {
                        ReplaceWithPortableFallback(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, SavePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Balloon Rush could not save data: {exception.Message}");
            }
        }

        public void ResetSettings()
        {
            EnsureValidData();
            Data.settings = gameConfig != null ? gameConfig.CreateDefaultSettings() : new OperatorSettings();
            Data.version = CurrentSaveVersion;
            Data.settings.Validate();
            Save();
        }

        public void ResetStatistics()
        {
            EnsureValidData();
            Data.statistics.Reset();
            Save();
        }

        public void RecordGame(GameSessionResult result)
        {
            if (result == null)
            {
                return;
            }

            EnsureValidData();
            MachineStatistics stats = Data.statistics;
            HighScoreData scores = Data.highScores;

            stats.gamesPlayed++;
            stats.totalTicketsAwarded += Math.Max(0, result.tickets);
            stats.perfectPops += Math.Max(0, result.perfectPops);
            stats.totalBalloonsPopped += Math.Max(0, result.balloonsPopped);
            if (result.jackpotWon)
            {
                stats.jackpotsWon++;
                scores.jackpotsWon++;
            }

            scores.topScore = Math.Max(scores.topScore, result.score);
            scores.highestCombo = Math.Max(scores.highestCombo, result.highestCombo);
            scores.mostTickets = Math.Max(scores.mostTickets, result.tickets);
            Save();
        }

        public void RecordCredit(CreditPulseType type, int creditsAdded, int transactionRevenueCents)
        {
            if (creditsAdded <= 0)
            {
                return;
            }

            EnsureValidData();
            MachineStatistics stats = Data.statistics;
            stats.totalCredits += creditsAdded;
            if (type == CreditPulseType.CardSwipe)
            {
                stats.cardSwipes++;
            }
            else
            {
                stats.coinPulses++;
            }

            stats.totalRevenueCents += Math.Max(0, transactionRevenueCents);
            Save();
        }

        public void RecordTicketsPaid(int requestedTickets, int paidTickets, bool verified)
        {
            EnsureValidData();
            Data.statistics.totalTicketsPaid += Math.Max(0, paidTickets);
            if (!verified || requestedTickets != paidTickets)
            {
                Data.statistics.ticketPayoutMismatches++;
            }
            Save();
        }

        public void RecordTicketPayoutFailure()
        {
            EnsureValidData();
            Data.statistics.ticketPayoutFailures++;
            Save();
        }

        public void RecordBombHit()
        {
            EnsureValidData();
            Data.statistics.bombsHit++;
            Save();
        }

        private void ReplaceWithPortableFallback(string temporaryPath)
        {
            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, true);
                File.Delete(SavePath);
            }
            File.Move(temporaryPath, SavePath);
        }

        private bool TryLoadBackup()
        {
            try
            {
                if (!File.Exists(BackupPath))
                {
                    return false;
                }

                string json = File.ReadAllText(BackupPath);
                Data = JsonUtility.FromJson<GameSaveData>(json);
                MigrateData();
                EnsureValidData();
                Debug.LogWarning("Balloon Rush restored settings and statistics from the previous save backup.");
                return true;
            }
            catch (Exception backupException)
            {
                Debug.LogWarning($"Balloon Rush backup recovery also failed: {backupException.Message}");
                return false;
            }
        }

        private GameSaveData CreateDefaultData()
        {
            GameSaveData data = new GameSaveData
            {
                version = CurrentSaveVersion,
                settings = gameConfig != null ? gameConfig.CreateDefaultSettings() : new OperatorSettings(),
                highScores = new HighScoreData(),
                statistics = new MachineStatistics()
            };
            data.settings.Validate();
            return data;
        }

        private void EnsureValidData()
        {
            if (Data == null)
            {
                Data = CreateDefaultData();
            }

            if (Data.settings == null)
            {
                Data.settings = gameConfig != null ? gameConfig.CreateDefaultSettings() : new OperatorSettings();
            }

            if (Data.highScores == null)
            {
                Data.highScores = new HighScoreData();
            }

            if (Data.statistics == null)
            {
                Data.statistics = new MachineStatistics();
            }

            Data.version = Mathf.Max(Data.version, CurrentSaveVersion);
            Data.settings.Validate();
        }

        private bool MigrateData()
        {
            if (Data == null)
            {
                return false;
            }

            bool migrated = false;
            if (Data.settings == null)
            {
                Data.settings = gameConfig != null ? gameConfig.CreateDefaultSettings() : new OperatorSettings();
                migrated = true;
            }
            if (Data.statistics == null)
            {
                Data.statistics = new MachineStatistics();
                migrated = true;
            }
            if (Data.highScores == null)
            {
                Data.highScores = new HighScoreData();
                migrated = true;
            }

            if (Data.version < 2)
            {
                if (Data.settings.inputDebounceMilliseconds <= 0)
                {
                    Data.settings.inputDebounceMilliseconds = 25;
                }
                migrated = true;
            }

            if (Data.version < 3)
            {
                // Version 2 counted awards as "paid" before PAID:n verification
                // existed. Preserve that history in both fields, then track them
                // separately for all new games.
                if (Data.statistics.totalTicketsAwarded <= 0 && Data.statistics.totalTicketsPaid > 0)
                {
                    Data.statistics.totalTicketsAwarded = Data.statistics.totalTicketsPaid;
                }

                bool legacyDefaultBalance = LooksLikeVersion2DefaultProfile(Data.settings);
                string previousPort = Data.settings.serialPort;
                if (legacyDefaultBalance)
                {
                    Data.settings.ApplyCommercialBalanceProfile(true);
                    if (string.IsNullOrWhiteSpace(previousPort) || string.Equals(previousPort, "COM3", StringComparison.OrdinalIgnoreCase))
                    {
                        Data.settings.serialPort = "COM8";
                    }
                    Debug.Log("Balloon Rush upgraded the untouched v1.3 balance to the $1 commercial profile.");
                }
                else
                {
                    InitializeVersion3Fields(Data.settings);
                }

                Data.version = 3;
                migrated = true;
            }

            return migrated;
        }

        private static void InitializeVersion3Fields(OperatorSettings settings)
        {
            if (settings.pricePerPlayCents <= 0) settings.pricePerPlayCents = 100;
            if (settings.estimatedPrizeCostPerTicketCents <= 0f) settings.estimatedPrizeCostPerTicketCents = 0.50f;
            if (settings.targetPrizeCostPercent <= 0f) settings.targetPrizeCostPercent = 20f;
            if (settings.minimumTicketPayout <= 0) settings.minimumTicketPayout = 5;
            if (settings.regularTicketCap <= 0) settings.regularTicketCap = 125;
            if (settings.mysteryGoldenChance <= 0f) settings.mysteryGoldenChance = 0.01f;
            if (settings.coinDebounceMilliseconds <= 0) settings.coinDebounceMilliseconds = 100;
            if (settings.cardSwipeDebounceMilliseconds <= 0) settings.cardSwipeDebounceMilliseconds = 750;
            if (settings.ticketHardwareWaitTimeoutSeconds <= 0f) settings.ticketHardwareWaitTimeoutSeconds = 12f;
            if (settings.ticketPaidAckTimeoutSeconds <= 0f) settings.ticketPaidAckTimeoutSeconds = 30f;
            if (string.IsNullOrWhiteSpace(settings.serialPort)) settings.serialPort = "COM8";
            settings.Validate();
        }

        private static bool LooksLikeVersion2DefaultProfile(OperatorSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            return Approximately(settings.gameDuration, 35f) &&
                   settings.creditsPerPlay == 1 &&
                   settings.cardSwipeValue == 1 &&
                   settings.jackpotTickets == 500 &&
                   settings.maxTicketPayout == 1000 &&
                   settings.greenTickets == 1 &&
                   settings.blueTickets == 5 &&
                   settings.goldenTriggerTickets == 3 &&
                   settings.mysteryMinimum == 2 &&
                   settings.mysteryMaximum == 10 &&
                   settings.goldenGreatReward == 50 &&
                   settings.goldenGoodReward == 20 &&
                   settings.goldenMissReward == 5 &&
                   Approximately(settings.spawnInterval, 1f) &&
                   Approximately(settings.blueSpawnWeight, 0.12f) &&
                   Approximately(settings.goldenSpawnWeight, 0.008f) &&
                   Approximately(settings.mysterySpawnWeight, 0.05f) &&
                   Approximately(settings.multiplierSpawnWeight, 0.04f) &&
                   Approximately(settings.x2Duration, 3.5f);
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.0005f;
        }

        private void TryBackupCorruptFile()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return;
                }

                string corruptPath = SavePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                File.Copy(SavePath, corruptPath, true);
            }
            catch
            {
                // Recovery should continue even if the corrupt file cannot be copied.
            }
        }
    }
}
