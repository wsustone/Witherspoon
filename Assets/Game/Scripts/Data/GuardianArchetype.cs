using UnityEngine;

namespace Witherspoon.Game.Data
{
    [CreateAssetMenu(menuName = "Witherspoon/Guardian Archetype", fileName = "GuardianArchetype")]
    public class GuardianArchetype : ScriptableObject
    {
        [System.Serializable]
        public struct SynergyMorph
        {
            public string comboName;
            public string partnerArchetype;
            [TextArea] public string description;
        }

        [Header("Identity")]
        [SerializeField] private string displayName = "Wolf Sentinel";
        [SerializeField] private string role = "Multi-hit DPS";
        [SerializeField] [TextArea] private string loreSnippet;

        [Header("Visuals")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color highlightColor = Color.white;
        [SerializeField] private Color attackColor = Color.white;

        [Header("Combat Stats")]
        [SerializeField] private TowerDefinition.AttackStyle attackStyle = TowerDefinition.AttackStyle.Projectile;
        [SerializeField] private float range = 3f;
        [SerializeField] private float fireRate = 1f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private bool hitsInstantly = false;
        [SerializeField] private float projectileSpawnOffset = 0.35f;
        [SerializeField] private float coneAngle = 40f;
        [SerializeField] private float coneRotationOffset = 0f;

        [Header("Crowd Control")]
        [Range(0f, 1f)]
        [SerializeField] private float slowPercent = 0.1f;
        [SerializeField] private float effectDuration = 0.5f;

        [Header("Synergy Morphs")]
        [SerializeField] private SynergyMorph[] synergyMorphs;

        public string DisplayName => displayName;
        public string Role => role;
        public string LoreSnippet => loreSnippet;
        public Sprite Icon => icon;
        public Color HighlightColor => highlightColor;
        public Color AttackColor => attackColor;
        public TowerDefinition.AttackStyle AttackStyle => attackStyle;
        public float Range => range;
        public float FireRate => fireRate;
        public float Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public bool HitsInstantly => hitsInstantly;
        public float ProjectileSpawnOffset => projectileSpawnOffset;
        public float ConeAngle => coneAngle;
        public float ConeRotationOffset => coneRotationOffset;
        public float SlowPercent => slowPercent;
        public float EffectDuration => effectDuration;
        public SynergyMorph[] SynergyMorphs => synergyMorphs;
    }
}
