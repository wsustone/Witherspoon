using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Enemies;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Tracks run state for a single play session and applies GameMode defeat conditions
    /// when enemies reach the goal.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private GameModeDefinition gameMode;
        [SerializeField] private bool pauseOnDefeat = true;

        private int _livesRemaining;
        private int _escapes;
        private float _damageAccum;
        private bool _defeated;
        private bool _initialized;

        public GameModeDefinition Mode => gameMode;
        public int LivesRemaining => _livesRemaining;
        public int Escapes => _escapes;
        public float DamageAccumulated => _damageAccum;
        public bool Defeated => _defeated;
        public bool IsInitialized => _initialized;

        public System.Action<GameSession> OnStatsChanged;
        public System.Action<GameSession, string> OnDefeat;

        private void Awake()
        {
            InitializeState(notifyListeners: false);
        }

        private void OnEnable()
        {
            EnemyAgent.OnAnyReachedGoal += HandleEnemyReachedGoal;
        }

        private void OnDisable()
        {
            EnemyAgent.OnAnyReachedGoal -= HandleEnemyReachedGoal;
        }

        private void HandleEnemyReachedGoal(EnemyAgent agent)
        {
            if (_defeated || !_initialized) return;

            _escapes++;
            float dmgPer = gameMode != null ? gameMode.DamagePerEscape : 1f;
            _damageAccum += dmgPer;

            if (gameMode != null && gameMode.DefeatOnFirstLeak)
            {
                TriggerDefeat("A creep breached the goal (no leaks mode).");
                return;
            }

            if (_livesRemaining > 0)
            {
                int livesLoss = Mathf.Max(1, Mathf.RoundToInt(dmgPer));
                _livesRemaining = Mathf.Max(0, _livesRemaining - livesLoss);
            }

            // Check thresholds
            if (gameMode != null)
            {
                if (gameMode.MaxEscapes > 0 && _escapes >= gameMode.MaxEscapes)
                {
                    TriggerDefeat("Too many creeps escaped.");
                    return;
                }
                if (gameMode.MaxDamage > 0f && _damageAccum >= gameMode.MaxDamage)
                {
                    TriggerDefeat("Base integrity failed.");
                    return;
                }
            }

            if (_livesRemaining <= 0 && (gameMode == null || gameMode.StartingLives > 0))
            {
                TriggerDefeat("Lives depleted.");
                return;
            }

            OnStatsChanged?.Invoke(this);
        }

        private void TriggerDefeat(string reason)
        {
            if (_defeated) return;
            _defeated = true;
            Debug.LogWarning($"GAME OVER: {reason}");
            OnDefeat?.Invoke(this, reason);
            OnStatsChanged?.Invoke(this);

            if (pauseOnDefeat)
            {
                Time.timeScale = 0f;
            }
        }

        public void SetMode(GameModeDefinition mode)
        {
            gameMode = mode;
            InitializeState(notifyListeners: true);
        }

        private void InitializeState(bool notifyListeners)
        {
            var mode = gameMode;
            _livesRemaining = Mathf.Max(0, mode != null ? mode.StartingLives : 0);
            _escapes = 0;
            _damageAccum = 0f;
            _defeated = false;
            _initialized = true;
            if (notifyListeners)
            {
                OnStatsChanged?.Invoke(this);
            }
        }
    }
}
