using System.Collections;
using BalloonRush.Audio;
using BalloonRush.Input;
using BalloonRush.Redemption;
using BalloonRush.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public const string BootSceneName = "Boot";
        public const string AttractSceneName = "AttractMode";
        public const string MainGameSceneName = "MainGame";
        public const string ResultsSceneName = "Results";
        public const string OperatorSceneName = "OperatorMenu";

        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private bool automaticallyEnterAttractMode = true;

        public static GameBootstrap Instance { get; private set; }
        public GameConfig Config => gameConfig;

        private GameStateManager stateManager;
        private SaveManager saveManager;
        private SettingsManager settingsManager;
        private ArcadeInputManager inputManager;
        private CreditManager creditManager;
        private AudioManager audioManager;
        private TicketManager ticketManager;
        private CabinetRuntimeManager cabinetRuntimeManager;
        private SessionAuditLogger sessionAuditLogger;
        private KeyboardArcadeIO keyboardIO;
        private SerialArcadeIO serialIO;
        private bool initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }

        private IEnumerator Start()
        {
            yield return null;

            if (automaticallyEnterAttractMode && SceneManager.GetActiveScene().name == BootSceneName)
            {
                GoToAttractMode();
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
            GameEvents.ClearAll();
            GameServices.Reset();
        }

        public void GoToAttractMode()
        {
            stateManager.ChangeState(GameState.Attract);
            LoadScene(AttractSceneName);
        }

        public void GoToMainGame()
        {
            stateManager.ChangeState(GameState.Starting);
            LoadScene(MainGameSceneName);
        }

        public void GoToResults(GameSessionResult result)
        {
            GameSession.LastResult = result ?? new GameSessionResult();
            stateManager.ChangeState(GameState.Results);
            LoadScene(ResultsSceneName);
        }

        public void GoToOperatorMenu()
        {
            stateManager.ChangeState(GameState.OperatorMenu);
            LoadScene(OperatorSceneName);
        }

        private void InitializeServices()
        {
            if (initialized)
            {
                return;
            }

            gameConfig = gameConfig != null ? gameConfig : Resources.Load<GameConfig>("BalloonRushConfig");
            if (gameConfig == null)
            {
                gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                gameConfig.payoutConfig = ScriptableObject.CreateInstance<PayoutConfig>();
                gameConfig.difficultyConfig = ScriptableObject.CreateInstance<DifficultyConfig>();
                gameConfig.audioConfig = ScriptableObject.CreateInstance<AudioConfig>();
                Debug.LogWarning("BalloonRushConfig was missing. Runtime safe defaults were created. Run Tools > Balloon Rush > Build Complete Game to create persistent assets.");
            }

            stateManager = EnsureComponent<GameStateManager>();
            saveManager = EnsureComponent<SaveManager>();
            settingsManager = EnsureComponent<SettingsManager>();
            inputManager = EnsureComponent<ArcadeInputManager>();
            creditManager = EnsureComponent<CreditManager>();
            audioManager = EnsureComponent<AudioManager>();
            ticketManager = EnsureComponent<TicketManager>();
            cabinetRuntimeManager = EnsureComponent<CabinetRuntimeManager>();
            sessionAuditLogger = EnsureComponent<SessionAuditLogger>();
            keyboardIO = EnsureComponent<KeyboardArcadeIO>();
            serialIO = EnsureComponent<SerialArcadeIO>();

            cabinetRuntimeManager.Initialize(gameConfig);
            saveManager.Initialize(gameConfig);
            settingsManager.Initialize(gameConfig, saveManager);
            sessionAuditLogger.Initialize(settingsManager);
            inputManager.ConfigureSources(keyboardIO, serialIO);
            inputManager.Initialize(settingsManager);
            creditManager.Initialize(settingsManager, saveManager, inputManager);
            audioManager.Initialize(gameConfig.audioConfig, settingsManager);
            ticketManager.Initialize(settingsManager, inputManager, saveManager);

            GameServices.Bootstrap = this;
            GameServices.Config = gameConfig;
            GameServices.State = stateManager;
            GameServices.Save = saveManager;
            GameServices.Settings = settingsManager;
            GameServices.Credits = creditManager;
            GameServices.Input = inputManager;
            GameServices.Audio = audioManager;
            GameServices.Tickets = ticketManager;
            GameServices.Cabinet = cabinetRuntimeManager;
            GameServices.Audit = sessionAuditLogger;
            initialized = true;
        }

        private T EnsureComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void LoadScene(string sceneName)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
