using UnityEngine;

namespace Witherspoon.Game.Data
{
    public enum GameModeType
    {
        NoLeaks,
        Lives,
        Survival
    }

    [CreateAssetMenu(fileName = "GameMode", menuName = "Witherspoon/Game Mode", order = 10)]
    public class GameModeDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "No Leaks";
        [SerializeField] private GameModeType type = GameModeType.NoLeaks;

        [Header("Constraints")]
        [SerializeField] private bool defeatOnFirstLeak = true;
        [SerializeField] private int startingLives = 0;
        [SerializeField] private int maxEscapes = 0;
        [SerializeField] private float maxDamage = 0f;
        [SerializeField] private float damagePerEscape = 1f;

        public string DisplayName => displayName;
        public GameModeType Type => type;
        public bool DefeatOnFirstLeak => defeatOnFirstLeak;
        public int StartingLives => startingLives;
        public int MaxEscapes => maxEscapes;
        public float MaxDamage => maxDamage;
        public float DamagePerEscape => Mathf.Max(0f, damagePerEscape);
    }
}
