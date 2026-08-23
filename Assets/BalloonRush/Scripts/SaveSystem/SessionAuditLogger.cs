using System;
using System.Globalization;
using System.IO;
using System.Text;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using UnityEngine;

namespace BalloonRush.SaveSystem
{
    /// <summary>
    /// Appends one compact CSV row for each completed game for balancing and
    /// revenue/ticket reconciliation.
    /// </summary>
    public sealed class SessionAuditLogger : MonoBehaviour
    {
        private const string FolderName = "BalloonRushAudit";
        private const string FileName = "sessions.csv";
        private const string Header = "session_id,started_utc,ended_utc,build_version,score,tickets_total,tickets_regular,tickets_bonus,tickets_jackpot,price_cents,estimated_prize_cost_cents,estimated_prize_cost_percent,highest_combo,perfect,great,good,misses,balloons_popped,bombs_hit,golden_balloons,jackpot_won,new_high_score,new_ticket_record,duration_seconds,credits_remaining,hardware_enabled,jackpot_setting,regular_cap_setting,max_payout_setting";
        private readonly object writeLock = new object();
        private SettingsManager settingsManager;
        private bool initialized;

        public string AuditDirectory => Path.Combine(Application.persistentDataPath, FolderName);
        public string AuditPath => Path.Combine(AuditDirectory, FileName);

        public void Initialize(SettingsManager settings)
        {
            if (initialized)
            {
                return;
            }

            settingsManager = settings;
            GameEvents.GameEnded += HandleGameEnded;
            initialized = true;
        }

        private void OnDestroy()
        {
            if (!initialized)
            {
                return;
            }

            GameEvents.GameEnded -= HandleGameEnded;
            initialized = false;
        }

        public void RecordGame(GameSessionResult result)
        {
            if (result == null)
            {
                return;
            }

            try
            {
                lock (writeLock)
                {
                    Directory.CreateDirectory(AuditDirectory);
                    RotateIncompatibleAuditFile();
                    bool writeHeader = !File.Exists(AuditPath) || new FileInfo(AuditPath).Length == 0;
                    using (StreamWriter writer = new StreamWriter(AuditPath, true, new UTF8Encoding(false)))
                    {
                        if (writeHeader)
                        {
                            writer.WriteLine(Header);
                        }

                        OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
                        float estimatedCost = EconomyMath.EstimatePrizeCostCents(result.tickets, settings);
                        float estimatedPercent = settings != null && settings.pricePerPlayCents > 0
                            ? estimatedCost / settings.pricePerPlayCents * 100f
                            : 0f;

                        writer.WriteLine(string.Join(",", new[]
                        {
                            Csv(result.sessionId),
                            Csv(result.startedUtc),
                            Csv(result.endedUtc),
                            Csv(GameServices.Config != null ? GameServices.Config.buildVersion : Application.version),
                            result.score.ToString(CultureInfo.InvariantCulture),
                            result.tickets.ToString(CultureInfo.InvariantCulture),
                            result.regularTickets.ToString(CultureInfo.InvariantCulture),
                            result.bonusTickets.ToString(CultureInfo.InvariantCulture),
                            result.jackpotTickets.ToString(CultureInfo.InvariantCulture),
                            result.pricePerPlayCents.ToString(CultureInfo.InvariantCulture),
                            estimatedCost.ToString("0.###", CultureInfo.InvariantCulture),
                            estimatedPercent.ToString("0.###", CultureInfo.InvariantCulture),
                            result.highestCombo.ToString(CultureInfo.InvariantCulture),
                            result.perfectPops.ToString(CultureInfo.InvariantCulture),
                            result.greatPops.ToString(CultureInfo.InvariantCulture),
                            result.goodPops.ToString(CultureInfo.InvariantCulture),
                            result.misses.ToString(CultureInfo.InvariantCulture),
                            result.balloonsPopped.ToString(CultureInfo.InvariantCulture),
                            result.bombsHit.ToString(CultureInfo.InvariantCulture),
                            result.goldenBalloons.ToString(CultureInfo.InvariantCulture),
                            result.jackpotWon ? "1" : "0",
                            result.newHighScore ? "1" : "0",
                            result.newTicketRecord ? "1" : "0",
                            result.gameDuration.ToString("0.###", CultureInfo.InvariantCulture),
                            result.creditsRemaining.ToString(CultureInfo.InvariantCulture),
                            settings != null && settings.hardwareEnabled ? "1" : "0",
                            (settings != null ? settings.jackpotTickets : 500).ToString(CultureInfo.InvariantCulture),
                            (settings != null ? settings.regularTicketCap : 125).ToString(CultureInfo.InvariantCulture),
                            (settings != null ? settings.maxTicketPayout : 625).ToString(CultureInfo.InvariantCulture)
                        }));
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Balloon Rush could not append the session audit log: {exception.Message}");
            }
        }

        private void RotateIncompatibleAuditFile()
        {
            if (!File.Exists(AuditPath) || new FileInfo(AuditPath).Length == 0)
            {
                return;
            }

            string existingHeader;
            using (StreamReader reader = new StreamReader(AuditPath, Encoding.UTF8, true))
            {
                existingHeader = reader.ReadLine();
            }

            if (string.Equals(existingHeader, Header, StringComparison.Ordinal))
            {
                return;
            }

            string legacyPath = Path.Combine(
                AuditDirectory,
                "sessions-legacy-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv");
            File.Move(AuditPath, legacyPath);
        }

        private void HandleGameEnded(GameSessionResult result)
        {
            RecordGame(result);
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
