using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BalloonRush.Core
{
    /// <summary>
    /// Applies cabinet-safe runtime settings and keeps a small rotating error log.
    /// This component is created automatically by GameBootstrap and does not need
    /// scene references.
    /// </summary>
    public sealed class CabinetRuntimeManager : MonoBehaviour
    {
        private readonly object logLock = new object();
        private GameConfig config;
        private string runtimeLogPath;
        private bool initialized;

        public string RuntimeLogPath => runtimeLogPath;

        public void Initialize(GameConfig configuredGame)
        {
            if (initialized)
            {
                return;
            }

            config = configuredGame;
            runtimeLogPath = Path.Combine(Application.persistentDataPath, "BalloonRushRuntime.log");
            ApplyRuntimeSettings();
            Application.logMessageReceivedThreaded += HandleLogMessage;
            initialized = true;
        }

        private void OnDestroy()
        {
            if (!initialized)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= HandleLogMessage;
            initialized = false;
        }

        private void ApplyRuntimeSettings()
        {
            int targetFrameRate = config != null ? Mathf.Clamp(config.targetFrameRate, 30, 240) : 60;
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = config == null || config.runInBackground;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if !UNITY_EDITOR
            if (config == null || config.hideCursorInPlayer)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }

            if (config == null || config.enforcePortraitResolutionInPlayer)
            {
                int width = config != null ? Mathf.Max(480, config.targetWidth) : 1080;
                int height = config != null ? Mathf.Max(800, config.targetHeight) : 1920;
                FullScreenMode mode = config != null ? config.playerFullScreenMode : FullScreenMode.FullScreenWindow;
                Screen.SetResolution(width, height, mode);
            }
#endif
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)
            {
                return;
            }

            try
            {
                lock (logLock)
                {
                    string logDirectory = Path.GetDirectoryName(runtimeLogPath);
                    if (!string.IsNullOrEmpty(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }
                    RotateLogIfNeeded();
                    StringBuilder builder = new StringBuilder(512);
                    builder.Append(DateTime.UtcNow.ToString("O"));
                    builder.Append(" [").Append(type).Append("] ");
                    builder.AppendLine(condition ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(stackTrace))
                    {
                        builder.AppendLine(stackTrace);
                    }
                    builder.AppendLine(new string('-', 72));
                    File.AppendAllText(runtimeLogPath, builder.ToString());
                }
            }
            catch
            {
                // Error logging must never create another failure loop.
            }
        }

        private void RotateLogIfNeeded()
        {
            if (string.IsNullOrEmpty(runtimeLogPath) || !File.Exists(runtimeLogPath))
            {
                return;
            }

            int maxKilobytes = config != null ? Mathf.Clamp(config.runtimeLogMaxKilobytes, 128, 16384) : 2048;
            long maxBytes = maxKilobytes * 1024L;
            FileInfo info = new FileInfo(runtimeLogPath);
            if (info.Length < maxBytes)
            {
                return;
            }

            string archive = runtimeLogPath + ".1";
            if (File.Exists(archive))
            {
                File.Delete(archive);
            }
            File.Move(runtimeLogPath, archive);
        }
    }
}
