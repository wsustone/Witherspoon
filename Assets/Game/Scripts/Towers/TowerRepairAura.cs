using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Witherspoon.Game.Core;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Manages repair aura functionality for healing nearby allied towers.
    /// </summary>
    public class TowerRepairAura : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;

        private float _repairGoldCarry;
        private float _lastRepairFxTime;
        private TowerController _controller;
        private TowerAudio _audio;
        private TowerVisuals _visuals;

        public bool HasRepairAura
        {
            get
            {
                var def = _controller?.Definition;
                return def != null && def.RepairAuraEnabled && _controller.CurrentRepairPerSecond > 0f;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<TowerController>();
            _audio = GetComponent<TowerAudio>();
            _visuals = GetComponent<TowerVisuals>();
        }

        private void Start()
        {
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }
        }

        public void UpdateRepairAura(float deltaTime)
        {
            if (!HasRepairAura || economyManager == null || deltaTime <= 0f) return;

            float radius = _controller.CurrentRange;
            float totalBudgetHP = _controller.CurrentRepairPerSecond * deltaTime;
            if (totalBudgetHP <= 0f) return;
            float perAllyCapHP = _controller.CurrentRepairPerAllyCap * deltaTime;

            // If repair has a gold cost, require at least 1 gold capacity (carry + wallet) before healing
            if (_controller.CurrentRepairGoldPerHP > 0f)
            {
                float availableFloat = _repairGoldCarry + Mathf.Max(0, economyManager.CurrentGold);
                if (availableFloat < 1f) return;
            }

            // Build eligible list
            Vector3 origin = _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position;
            var eligible = new List<TowerController>();
            foreach (var t in TowerController.ActiveTowers.ToList())
            {
                if (t == null) continue;
                var tHealth = t.GetComponent<TowerHealth>();
                if (tHealth == null) continue;
                if (!_controller.Definition.RepairAffectsSelf && t == _controller) continue;
                if ((t.transform.position - origin).sqrMagnitude > radius * radius) continue;
                if (tHealth.CurrentHealth >= tHealth.MaxHealth - 0.01f) continue;
                eligible.Add(t);
            }
            if (eligible.Count == 0) return;

            // Sort by lowest health ratio first
            eligible = eligible.OrderBy(t =>
            {
                var h = t.GetComponent<TowerHealth>();
                return h != null ? h.CurrentHealth / Mathf.Max(1f, h.MaxHealth) : 1f;
            }).ToList();

            const float minMissingHp = 0.5f; // avoid micro-heals on nearly full towers
            foreach (var t in eligible)
            {
                if (totalBudgetHP <= 0f) break;
                var tHealth = t.GetComponent<TowerHealth>();
                if (tHealth == null) continue;

                float missing = Mathf.Max(0f, tHealth.MaxHealth - tHealth.CurrentHealth);
                if (missing < minMissingHp) continue;
                float maxThis = perAllyCapHP > 0f ? Mathf.Min(perAllyCapHP, missing) : missing;
                float toHeal = Mathf.Min(maxThis, totalBudgetHP);
                if (_controller.CurrentRepairGoldPerHP > 0f)
                {
                    // Bound by what we can afford (carry + current gold)
                    float availableFloat = _repairGoldCarry + Mathf.Max(0, economyManager.CurrentGold);
                    float affordableHp = availableFloat / _controller.CurrentRepairGoldPerHP;
                    toHeal = Mathf.Min(toHeal, affordableHp);
                }
                if (toHeal <= 0f) continue;

                // Determine gold requirement with fractional carry
                if (_controller.CurrentRepairGoldPerHP > 0f)
                {
                    float requiredGoldFloat = toHeal * _controller.CurrentRepairGoldPerHP;
                    _repairGoldCarry += requiredGoldFloat;
                    int spendInt = Mathf.FloorToInt(_repairGoldCarry + 0.0001f);
                    if (spendInt > 0)
                    {
                        if (!economyManager.TrySpend(spendInt))
                        {
                            // Can't afford this chunk; rollback and stop repairing this tick
                            _repairGoldCarry -= requiredGoldFloat;
                            break;
                        }
                        _repairGoldCarry -= spendInt;
                    }
                    else
                    {
                        // No full gold to spend yet; don't heal on credit. Keep carry for future, but skip healing now.
                        _repairGoldCarry -= requiredGoldFloat; // maintain carry by not advancing this tick
                        break;
                    }
                }

                float healed = tHealth.ApplyRepair(toHeal);
                totalBudgetHP -= healed;

                if (healed > 0.01f)
                {
                    SpawnRepairFx(origin, t.transform.position);
                    _audio?.PlayRepairTickSfx();
                }
            }
        }

        private void SpawnRepairFx(Vector3 from, Vector3 to)
        {
            const float fxInterval = 0.08f;
            if (Time.time - _lastRepairFxTime < fxInterval) return;
            _lastRepairFxTime = Time.time;

            var go = new GameObject("RepairLinkFx");
            go.transform.position = from;
            var fx = go.AddComponent<RepairLinkFx>();
            Color c = _controller?.Definition != null ? _controller.Definition.AttackColor : new Color(0.3f, 0.9f, 0.6f, 0.9f);
            fx.Initialize(from, to, c, 0.035f, 0.18f);
        }
    }
}
