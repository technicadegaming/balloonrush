using System;
using BalloonRush.Core;
using UnityEngine;

namespace BalloonRush.SaveSystem
{
    public sealed class SettingsManager : MonoBehaviour
    {
        private SaveManager saveManager;
        private GameConfig gameConfig;

        public OperatorSettings Current { get; private set; }
        public event Action<OperatorSettings> SettingsChanged;

        public void Initialize(GameConfig config, SaveManager save)
        {
            gameConfig = config;
            saveManager = save;
            Current = saveManager != null && saveManager.Data != null
                ? saveManager.Data.settings
                : (gameConfig != null ? gameConfig.CreateDefaultSettings() : new OperatorSettings());
            Current.Validate();
        }

        public OperatorSettings CreateEditableCopy()
        {
            return Current != null ? Current.Clone() : new OperatorSettings();
        }

        public void Apply(OperatorSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.Validate();
            Current = settings.Clone();
            if (saveManager != null && saveManager.Data != null)
            {
                saveManager.Data.settings = Current;
                saveManager.Save();
            }

            SettingsChanged?.Invoke(Current);
        }

        public void ResetDefaults()
        {
            Current = gameConfig != null ? gameConfig.CreateDefaultSettings() : new OperatorSettings();
            Current.Validate();
            if (saveManager != null && saveManager.Data != null)
            {
                saveManager.Data.settings = Current;
                saveManager.Save();
            }

            SettingsChanged?.Invoke(Current);
        }
    }
}
