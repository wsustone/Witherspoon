using UnityEngine;
using Witherspoon.Game.Map;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Simple spawner that instantiates enemy prefabs along a predefined lane.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition defaultEnemy;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform goalPoint;

        public void SpawnWave(int waveNumber, GridManager grid)
        {
            int count = Mathf.Clamp(3 + waveNumber, 3, 25);
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(defaultEnemy, spawnPoint.position, goalPoint.position, i * 0.75f);
            }
        }

        private void SpawnEnemy(EnemyDefinition definition, Vector3 start, Vector3 goal, float delay)
        {
            if (definition?.Prefab == null) return;
            var enemy = Instantiate(definition.Prefab, start, Quaternion.identity);
            if (enemy.TryGetComponent(out EnemyAgent agent))
            {
                agent.Initialize(definition, goal, delay, _attachedGrid);
            }
        }

        private GridManager _attachedGrid;

        public void AttachGrid(GridManager grid)
        {
            _attachedGrid = grid;
        }
    }
}
