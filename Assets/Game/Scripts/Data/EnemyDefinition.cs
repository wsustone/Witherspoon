using UnityEngine;

namespace Witherspoon.Game.Data
{
    [CreateAssetMenu(menuName = "Witherspoon/Enemy Definition", fileName = "EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Presentation")]
        [SerializeField] private string enemyName = "Wisp";
        [SerializeField] private Sprite portrait;
        [SerializeField] private Color factionColor = Color.cyan;

        [Header("Prefab")]
        [SerializeField] private Enemies.EnemyAgent prefab;

        [Header("Stats")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private int goldReward = 10;

        [Header("Drops")]
        [SerializeField] private EssenceDefinition dropEssence;
        [SerializeField] private int essenceAmount;

        public string EnemyName => enemyName;
        public Sprite Portrait => portrait;
        public Color FactionColor => factionColor;
        public Enemies.EnemyAgent Prefab => prefab;
        public float BaseHealth => baseHealth;
        public float MoveSpeed => moveSpeed;
        public int GoldReward => goldReward;
        public EssenceDefinition DropEssence => dropEssence;
        public int EssenceAmount => essenceAmount;
    }
}
