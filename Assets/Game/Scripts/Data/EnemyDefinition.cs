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

        [Header("UI Display Flags")]
        [SerializeField] private bool uiShowHealth = true;
        [SerializeField] private bool uiShowMoveSpeed = true;
        [SerializeField] private bool uiShowGoldReward = true;
        [SerializeField] private bool uiShowEssenceDrop = true;
        [SerializeField] private bool uiShowArmor = true;
        [SerializeField] private bool uiShowResistances = true;
        [SerializeField] private bool uiShowSlowResist = true;
        [SerializeField] private bool uiShowTraits = true;

        [Header("Prefab")]
        [SerializeField] private Enemies.EnemyAgent prefab;

        [Header("Stats")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private int goldReward = 10;

        [Header("Defense & Traits")]
        [SerializeField] private float armor = 0f;
        [Tooltip("Multiplier to damage taken from Projectile towers")] [Range(0f, 5f)] [SerializeField] private float dmgTakenMulProjectile = 1f;
        [Tooltip("Multiplier to damage taken from Beam towers")] [Range(0f, 5f)] [SerializeField] private float dmgTakenMulBeam = 1f;
        [Tooltip("Multiplier to damage taken from Cone towers")] [Range(0f, 5f)] [SerializeField] private float dmgTakenMulCone = 1f;
        [Tooltip("Multiplier to damage taken from Aura towers")] [Range(0f, 5f)] [SerializeField] private float dmgTakenMulAura = 1f;
        [Tooltip("Multiplier to damage taken from Wall towers")] [Range(0f, 5f)] [SerializeField] private float dmgTakenMulWall = 1f;
        [Tooltip("0 = immune to slows, 1 = full effect")] [Range(0f, 1f)] [SerializeField] private float slowEffectiveness = 1f;
        [SerializeField] private string specialTraits;

        [Header("Tower Aggression")]
        [SerializeField] private bool canAttackTowers = false;
        [SerializeField] private float attackRange = 0.75f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackInterval = 1.2f;

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
        public bool UiShowHealth => uiShowHealth;
        public bool UiShowMoveSpeed => uiShowMoveSpeed;
        public bool UiShowGoldReward => uiShowGoldReward;
        public bool UiShowEssenceDrop => uiShowEssenceDrop;
        public bool UiShowArmor => uiShowArmor;
        public bool UiShowResistances => uiShowResistances;
        public bool UiShowSlowResist => uiShowSlowResist;
        public bool UiShowTraits => uiShowTraits;
        public float Armor => Mathf.Max(0f, armor);
        public float DmgTakenMulProjectile => dmgTakenMulProjectile;
        public float DmgTakenMulBeam => dmgTakenMulBeam;
        public float DmgTakenMulCone => dmgTakenMulCone;
        public float DmgTakenMulAura => dmgTakenMulAura;
        public float DmgTakenMulWall => dmgTakenMulWall;
        public float SlowEffectiveness => Mathf.Clamp01(slowEffectiveness);
        public string SpecialTraits => specialTraits;
        public bool CanAttackTowers => canAttackTowers;
        public float AttackRange => Mathf.Max(0f, attackRange);
        public float AttackDamage => Mathf.Max(0f, attackDamage);
        public float AttackInterval => Mathf.Max(0.05f, attackInterval);
    }
}
