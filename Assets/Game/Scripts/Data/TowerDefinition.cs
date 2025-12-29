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
        public string TowerName => archetype != null ? archetype.DisplayName : towerName;
        public Sprite Icon => icon != null ? icon : archetype?.Icon;
        public Color HighlightColor => archetype != null ? archetype.HighlightColor : highlightColor;
        public GameObject TowerPrefab => towerPrefab;
        public int BuildCost => buildCost;
        public float Range => archetype != null ? archetype.Range : range;
        public float FireRate => archetype != null ? archetype.FireRate : fireRate;
        public float Damage => archetype != null ? archetype.Damage : damage;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => archetype != null ? archetype.ProjectileSpeed : projectileSpeed;
        public float ProjectileSpawnOffset => archetype != null ? archetype.ProjectileSpawnOffset : projectileSpawnOffset;
        public bool HitsInstantly => archetype != null ? archetype.HitsInstantly : hitsInstantly;
        public AttackStyle AttackMode => archetype != null ? archetype.AttackStyle : attackMode;
        public Color AttackColor => archetype != null ? archetype.AttackColor : attackColor;
        public float ConeAngle => archetype != null ? archetype.ConeAngle : coneAngle;
        public float ConeRotationOffset => archetype != null ? archetype.ConeRotationOffset : coneRotationOffset;
        public float SlowPercent => archetype != null ? archetype.SlowPercent : slowPercent;
        public float EffectDuration => archetype != null ? archetype.EffectDuration : effectDuration;
    }
}
