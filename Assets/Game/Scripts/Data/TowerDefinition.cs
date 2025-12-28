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
            Cone
        }

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

        [Header("Attack Visual")]
        [SerializeField] private AttackStyle attackMode = AttackStyle.Projectile;
        [SerializeField] private Color attackColor = Color.white;
        [SerializeField] private float coneAngle = 40f;
        [SerializeField] private float coneRotationOffset = 0f;

        public string TowerName => towerName;
        public Sprite Icon => icon;
        public Color HighlightColor => highlightColor;
        public GameObject TowerPrefab => towerPrefab;
        public int BuildCost => buildCost;
        public float Range => range;
        public float FireRate => fireRate;
        public float Damage => damage;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public bool HitsInstantly => hitsInstantly;
        public AttackStyle AttackMode => attackMode;
        public Color AttackColor => attackColor;
        public float ConeAngle => coneAngle;
        public float ConeRotationOffset => coneRotationOffset;
    }
}
