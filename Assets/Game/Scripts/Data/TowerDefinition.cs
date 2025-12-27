using UnityEngine;

namespace Witherspoon.Game.Data
{
    [CreateAssetMenu(menuName = "Witherspoon/Tower Definition", fileName = "TowerDefinition")]
    public class TowerDefinition : ScriptableObject
    {
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

        public string TowerName => towerName;
        public Sprite Icon => icon;
        public Color HighlightColor => highlightColor;
        public GameObject TowerPrefab => towerPrefab;
        public int BuildCost => buildCost;
        public float Range => range;
        public float FireRate => fireRate;
        public float Damage => damage;
    }
}
