# Requirements Map

| Requirement area | Primary implementation |
|---|---|
| Boot and persistent services | `GameBootstrap`, `GameServices`, `SceneBootstrapGuard` |
| State machine | `GameState`, `GameStateManager` |
| Credit and free-play | `CreditManager`, `OperatorSettings` |
| Three lanes and selection | `LaneManager` |
| Balloon movement and pooling | `Balloon`, `BalloonPool`, `BalloonSpawner` |
| Hit timing | `HitZone`, `TimingEvaluator`, `TimingRating` |
| Combo and scoring | `ComboManager`, `ScoreManager`, `TicketMath` |
| Special balloons | `BalloonDefinition`, `BalloonManager` |
| Golden round and jackpot | `GoldenRoundManager`, `JackpotManager` |
| Round progression and Rush Mode | `RoundManager`, `DifficultyManager` |
| UI and attract mode | `UIManager`, `AttractModeManager` |
| Results, records, and replay prompt | `ResultsManager`, `GameSessionResult` |
| Operator settings/statistics | `OperatorMenuManager`, `SettingsManager`, `SaveManager` |
| Versioned save migration and backup recovery | `SaveManager`, `GameSaveData` |
| Ticket payout and cap | `TicketManager`, `ScoreManager`, `OperatorSettings.Validate` |
| Keyboard/gamepad input | `KeyboardArcadeIO`, `ArcadeInputManager` |
| Input switch debounce | `ArcadeInputManager`, `OperatorSettings` |
| Arduino/serial I/O | `SerialArcadeIO`, `IArcadeIO`, included `.ino` sketch |
| Audio fallbacks | `AudioManager`, `AudioConfig` |
| Effects and feedback | `EffectsManager`, `ScreenShake`, `FloatingTextPool` |
| Cabinet runtime safety and rotating error log | `CabinetRuntimeManager` |
| Per-game CSV audit telemetry | `SessionAuditLogger` |
| Payout estimation | `PayoutSimulatorWindow` |
| One-click scene construction | `BalloonRushProjectBuilder` |
| Unity asset/scene/payout preflight and build blocking | `BalloonRushPreflightValidator`, `BalloonRushBuildPreprocessor` |
| Automated tests | `Assets/BalloonRush/Tests/Editor` |
| Static repository validation | `BuildScripts/validate-source.py` |
