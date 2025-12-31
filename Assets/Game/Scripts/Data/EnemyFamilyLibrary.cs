using UnityEngine;

namespace Witherspoon.Game.Data
{
    [CreateAssetMenu(menuName = "Witherspoon/Enemy Family Library", fileName = "EnemyFamilyLibrary")]
    public class EnemyFamilyLibrary : ScriptableObject
    {
        [Header("Fallback")]
        [SerializeField] private EnemyDefinition defaultEnemy;

        [Header("Lesser Nightmares")]
        [SerializeField] private EnemyDefinition shades;
        [SerializeField] private EnemyDefinition glimmers;
        [SerializeField] private EnemyDefinition husks;

        [Header("Elite Spirits")]
        [SerializeField] private EnemyDefinition nightglass;
        [SerializeField] private EnemyDefinition dreadbound;
        [SerializeField] private EnemyDefinition riftrunner;

        [Header("Objective Creeps")]
        [SerializeField] private EnemyDefinition shardThief;
        [SerializeField] private EnemyDefinition anchorBreaker;
        [SerializeField] private EnemyDefinition pathforger;

        [Header("Nightmares")]
        [SerializeField] private EnemyDefinition nightmareOfDread;
        [SerializeField] private EnemyDefinition nightmareOfStagnation;
        [SerializeField] private EnemyDefinition nightmareOfRuin;
        [SerializeField] private EnemyDefinition nightmareOfDiscord;

        public EnemyDefinition DefaultEnemy => defaultEnemy;
        public EnemyDefinition Shades => shades;
        public EnemyDefinition Glimmers => glimmers;
        public EnemyDefinition Husks => husks;
        public EnemyDefinition Nightglass => nightglass;
        public EnemyDefinition Dreadbound => dreadbound;
        public EnemyDefinition Riftrunner => riftrunner;
        public EnemyDefinition ShardThief => shardThief;
        public EnemyDefinition AnchorBreaker => anchorBreaker;
        public EnemyDefinition Pathforger => pathforger;
        public EnemyDefinition NightmareOfDread => nightmareOfDread;
        public EnemyDefinition NightmareOfStagnation => nightmareOfStagnation;
        public EnemyDefinition NightmareOfRuin => nightmareOfRuin;
        public EnemyDefinition NightmareOfDiscord => nightmareOfDiscord;
    }
}
