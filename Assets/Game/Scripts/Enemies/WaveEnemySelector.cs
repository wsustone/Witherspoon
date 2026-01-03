using UnityEngine;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Handles enemy selection logic for waves based on wave number and enemy families.
    /// </summary>
    public class WaveEnemySelector : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition defaultEnemy;
        [SerializeField] private EnemyFamilyLibrary enemyFamilies;
        [SerializeField] private bool debugWaveSelection = false;

        public void Initialize(EnemyDefinition defaultEnemyDef, EnemyFamilyLibrary families, bool debug = false)
        {
            if (defaultEnemyDef != null) defaultEnemy = defaultEnemyDef;
            if (families != null) enemyFamilies = families;
            debugWaveSelection = debug;
        }

        public EnemyDefinition SelectSpawnableEnemy(EnemyDefinition forced, int waveNumber)
        {
            if (IsSpawnable(forced)) return forced;

            var resolved = ResolveEnemyForWave(waveNumber);
            if (IsSpawnable(resolved)) return resolved;

            if (IsSpawnable(defaultEnemy)) return defaultEnemy;
            if (enemyFamilies != null && IsSpawnable(enemyFamilies.DefaultEnemy))
            {
                return enemyFamilies.DefaultEnemy;
            }

            return null;
        }

        public static bool IsSpawnable(EnemyDefinition candidate) =>
            candidate != null && candidate.Prefab != null;

        private EnemyDefinition ResolveEnemyForWave(int waveNumber)
        {
            if (enemyFamilies == null)
            {
                return IsSpawnable(defaultEnemy) ? defaultEnemy : null;
            }

            EnemyDefinition FirstSpawnable(params EnemyDefinition[] candidates)
            {
                foreach (var candidate in candidates)
                {
                    if (IsSpawnable(candidate)) return candidate;
                }
                return null;
            }

            EnemyDefinition PickFamilyEnemy()
            {
                if (waveNumber <= 2)
                {
                    return FirstSpawnable(enemyFamilies.Shades, enemyFamilies.DefaultEnemy);
                }
                if (waveNumber <= 4)
                {
                    return FirstSpawnable(enemyFamilies.Glimmers, enemyFamilies.Shades, enemyFamilies.DefaultEnemy);
                }
                if (waveNumber <= 6)
                {
                    return FirstSpawnable(enemyFamilies.Husks, enemyFamilies.Glimmers, enemyFamilies.DefaultEnemy);
                }
                if (waveNumber >= 10 && waveNumber % 10 == 0)
                {
                    return FirstSpawnable(enemyFamilies.NightmareOfDread, enemyFamilies.NightmareOfStagnation, enemyFamilies.NightmareOfRuin, enemyFamilies.NightmareOfDiscord, enemyFamilies.DefaultEnemy);
                }
                if (waveNumber % 7 == 0)
                {
                    return FirstSpawnable(enemyFamilies.AnchorBreaker, enemyFamilies.ShardThief, enemyFamilies.Pathforger, enemyFamilies.DefaultEnemy);
                }
                if (waveNumber % 5 == 0)
                {
                    return FirstSpawnable(enemyFamilies.Nightglass, enemyFamilies.Dreadbound, enemyFamilies.Riftrunner, enemyFamilies.DefaultEnemy);
                }

                return FirstSpawnable(enemyFamilies.Shades, enemyFamilies.Glimmers, enemyFamilies.Husks, enemyFamilies.DefaultEnemy);
            }

            var pick = PickFamilyEnemy();
            if (pick != null) return pick;

            return IsSpawnable(defaultEnemy) ? defaultEnemy : null;
        }

        public EnemyDefinition PreviewEnemyForWave(int waveNumber)
        {
            var pick = ResolveEnemyForWave(waveNumber);
            if (IsSpawnable(pick)) return pick;
            if (IsSpawnable(defaultEnemy)) return defaultEnemy;
            if (enemyFamilies != null && IsSpawnable(enemyFamilies.DefaultEnemy)) return enemyFamilies.DefaultEnemy;
            return null;
        }

        public void LogWaveSelection(int waveNumber, EnemyDefinition enemy)
        {
            if (debugWaveSelection && enemy != null)
            {
                Debug.Log($"[EnemySpawner] Wave {waveNumber} -> {enemy.EnemyName}", this);
            }
        }
    }
}
