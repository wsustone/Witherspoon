using UnityEngine;

namespace Witherspoon.Game.Data
{
    [CreateAssetMenu(menuName = "Witherspoon/Tower Definition", fileName = "TowerDefinition")]
    public class TowerDefinition : ScriptableObject
    {
        public enum AttackStyle
        {
            Projectile,
            Beam,
            Cone,
            Aura,
            Wall
        }

        [Header("Archetype Link")]
        [SerializeField] private GuardianArchetype archetype;

        [Header("Archetype Overrides")]
        [SerializeField] private bool overrideTowerName;
        [SerializeField] private bool overrideIcon;
        [SerializeField] private bool overrideHighlightColor;
        [SerializeField] private bool overrideAttackColor;
        [SerializeField] private bool overrideRange;
        [SerializeField] private bool overrideFireRate;
        [SerializeField] private bool overrideDamage;
        [SerializeField] private bool overrideProjectileSpeed;
        [SerializeField] private bool overrideHitsInstantly;
        [SerializeField] private bool overrideProjectileSpawnOffset;
        [SerializeField] private bool overrideAttackMode;
        [SerializeField] private bool overrideConeAngle;
        [SerializeField] private bool overrideConeRotationOffset;
        [SerializeField] private bool overrideSlowPercent;
        [SerializeField] private bool overrideEffectDuration;

        [Header("Presentation")]
        [SerializeField] private string towerName = "Sentinel";
        [SerializeField] private Sprite icon;
        [SerializeField] private Color highlightColor = Color.white;

        [Header("Gameplay")]
        [SerializeField] private GameObject towerPrefab;
        [SerializeField] private int buildCost = 75;
        [SerializeField] private float range = 3f;
        [SerializeField] private float fireRate = 1.2f;
        [SerializeField] private float damage = 25f;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private bool hitsInstantly;
        [SerializeField] private float projectileSpawnOffset = 0.35f;

        [Header("Attack Visual")]
        [SerializeField] private AttackStyle attackMode = AttackStyle.Projectile;
        [SerializeField] private Color attackColor = Color.white;
        [SerializeField] private float coneAngle = 40f;
        [SerializeField] private float coneRotationOffset = 0f;
        [Header("Status Effects")]
        [Range(0f, 1f)]
        [SerializeField] private float slowPercent = 0.1f;
        [SerializeField] private float effectDuration = 0.5f;

        public GuardianArchetype Archetype => archetype;
        public string TowerName => (!overrideTowerName && archetype != null) ? archetype.DisplayName : towerName;
        public Sprite Icon => (!overrideIcon && archetype != null && archetype.Icon != null) ? archetype.Icon : icon;
        public Color HighlightColor => (!overrideHighlightColor && archetype != null) ? archetype.HighlightColor : highlightColor;
        public GameObject TowerPrefab => towerPrefab;
        public int BuildCost => buildCost;
        public float Range => (!overrideRange && archetype != null) ? archetype.Range : range;
        public float FireRate => (!overrideFireRate && archetype != null) ? archetype.FireRate : fireRate;
        public float Damage => (!overrideDamage && archetype != null) ? archetype.Damage : damage;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => (!overrideProjectileSpeed && archetype != null) ? archetype.ProjectileSpeed : projectileSpeed;
        public float ProjectileSpawnOffset => (!overrideProjectileSpawnOffset && archetype != null) ? archetype.ProjectileSpawnOffset : projectileSpawnOffset;
        public bool HitsInstantly => (!overrideHitsInstantly && archetype != null) ? archetype.HitsInstantly : hitsInstantly;
        public AttackStyle AttackMode => (!overrideAttackMode && archetype != null) ? archetype.AttackStyle : attackMode;
        public Color AttackColor => (!overrideAttackColor && archetype != null) ? archetype.AttackColor : attackColor;
        public float ConeAngle => (!overrideConeAngle && archetype != null) ? archetype.ConeAngle : coneAngle;
        public float ConeRotationOffset => (!overrideConeRotationOffset && archetype != null) ? archetype.ConeRotationOffset : coneRotationOffset;
        public float SlowPercent => (!overrideSlowPercent && archetype != null) ? archetype.SlowPercent : slowPercent;
        public float EffectDuration => (!overrideEffectDuration && archetype != null) ? archetype.EffectDuration : effectDuration;
    }
}
