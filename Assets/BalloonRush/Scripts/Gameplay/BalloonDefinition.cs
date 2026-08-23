using UnityEngine;

namespace BalloonRush.Gameplay
{
    public enum BalloonKind
    {
        Green,
        Blue,
        Multiplier,
        Mystery,
        Bomb,
        SuperBomb,
        GoldenTrigger,
        GoldenJackpot
    }

    public enum BalloonSpecialBehavior
    {
        None,
        DoublePayout,
        MysteryReward,
        Dangerous,
        StartGoldenRound,
        ResolveJackpot
    }

    [CreateAssetMenu(menuName = "Balloon Rush/Balloon Definition", fileName = "BalloonDefinition")]
    public sealed class BalloonDefinition : ScriptableObject
    {
        [SerializeField] private string id = "balloon";
        [SerializeField] private string displayName = "Balloon";
        [SerializeField] private BalloonKind kind;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color visualColor = Color.green;
        [SerializeField] private int basePoints = 100;
        [SerializeField] private int baseTickets = 1;
        [SerializeField] private float spawnWeight = 1f;
        [SerializeField] private bool isDangerous;
        [SerializeField] private BalloonSpecialBehavior specialBehavior;

        public string Id => id;
        public string DisplayName => displayName;
        public BalloonKind Kind => kind;
        public Sprite Sprite => sprite;
        public Color VisualColor => visualColor;
        public int BasePoints => basePoints;
        public int BaseTickets => baseTickets;
        public float SpawnWeight => Mathf.Max(0f, spawnWeight);
        public bool IsDangerous => isDangerous;
        public BalloonSpecialBehavior SpecialBehavior => specialBehavior;

        public void Configure(
            string configuredId,
            string configuredDisplayName,
            BalloonKind configuredKind,
            Sprite configuredSprite,
            Color configuredColor,
            int configuredPoints,
            int configuredTickets,
            float configuredSpawnWeight,
            bool configuredDangerous,
            BalloonSpecialBehavior configuredBehavior)
        {
            id = configuredId;
            displayName = configuredDisplayName;
            kind = configuredKind;
            sprite = configuredSprite;
            visualColor = configuredColor;
            basePoints = configuredPoints;
            baseTickets = configuredTickets;
            spawnWeight = configuredSpawnWeight;
            isDangerous = configuredDangerous;
            specialBehavior = configuredBehavior;
        }
    }
}
