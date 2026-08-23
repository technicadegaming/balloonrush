using BalloonRush.Audio;
using BalloonRush.Input;
using BalloonRush.Redemption;
using BalloonRush.SaveSystem;

namespace BalloonRush.Core
{
    public static class GameServices
    {
        public static GameBootstrap Bootstrap { get; internal set; }
        public static GameConfig Config { get; internal set; }
        public static GameStateManager State { get; internal set; }
        public static SaveManager Save { get; internal set; }
        public static SettingsManager Settings { get; internal set; }
        public static CreditManager Credits { get; internal set; }
        public static ArcadeInputManager Input { get; internal set; }
        public static AudioManager Audio { get; internal set; }
        public static TicketManager Tickets { get; internal set; }
        public static CabinetRuntimeManager Cabinet { get; internal set; }
        public static SessionAuditLogger Audit { get; internal set; }

        public static bool IsReady => Bootstrap != null && Config != null && Settings != null && Input != null;

        internal static void Reset()
        {
            Bootstrap = null;
            Config = null;
            State = null;
            Save = null;
            Settings = null;
            Credits = null;
            Input = null;
            Audio = null;
            Tickets = null;
            Cabinet = null;
            Audit = null;
        }
    }
}
