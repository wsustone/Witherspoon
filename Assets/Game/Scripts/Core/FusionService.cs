using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.Core
{
    public class FusionService : MonoBehaviour
    {
        [SerializeField] private List<FusionRecipe> recipes = new();

        public IReadOnlyList<FusionRecipe> Recipes => recipes;

        public bool TryMerge(TowerController a, TowerController b, EconomyManager economy)
        {
            if (a == null || b == null || economy == null) return false;
            var aDef = a.Definition;
            var bDef = b.Definition;
            if (aDef == null || bDef == null) return false;

            var recipe = FindRecipe(aDef, bDef);
            if (recipe == null || recipe.ResultDefinition == null) return false;

            // Check essence requirement from the RESULT tower (either-or)
            var inv = economy.Essences;
            var result = recipe.ResultDefinition;
            var req1 = result.FusionRequiredEssence;
            int amt1 = result.FusionRequiredEssenceAmount;
            var req2 = result.FusionRequiredEssenceAlt;
            int amt2 = result.FusionRequiredEssenceAltAmount;

            bool req1Valid = req1 != null && amt1 > 0;
            bool req2Valid = req2 != null && amt2 > 0;
            if (req1Valid || req2Valid)
            {
                if (inv == null)
                {
                    return false;
                }
                bool consumed = false;
                int have1 = req1Valid ? inv.GetCount(req1) : 0;
                int have2 = req2Valid ? inv.GetCount(req2) : 0;
                if (req1Valid && have1 >= amt1)
                {
                    consumed = inv.TryConsume(req1, amt1);
                }
                else if (req2Valid && have2 >= amt2)
                {
                    consumed = inv.TryConsume(req2, amt2);
                }
                if (!consumed)
                {
                    return false;
                }
            }

            // Determine morph (downtime) from result tower's first upgrade tier
            float morphSeconds = 0f;
            var tiers = result.UpgradeTiers;
            if (tiers != null && tiers.Length > 0 && tiers[0] != null)
            {
                morphSeconds = tiers[0].UpgradeTime;
            }

            if (morphSeconds > 0f)
            {
                a.BeginMorph(morphSeconds);
                StartCoroutine(FinishFusionAfterDelay(a, b, result, morphSeconds));
            }
            else
            {
                a.TransformTo(result);
                if (b != null)
                {
                    Destroy(b.gameObject);
                }
            }
            return true;
        }

        private IEnumerator FinishFusionAfterDelay(TowerController a, TowerController b, TowerDefinition result, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (a != null && result != null)
            {
                a.TransformTo(result);
            }
            if (b != null)
            {
                Destroy(b.gameObject);
            }
        }

        public TowerDefinition PreviewResult(TowerDefinition a, TowerDefinition b)
        {
            return FindRecipe(a, b)?.ResultDefinition;
        }

        private FusionRecipe FindRecipe(TowerDefinition a, TowerDefinition b)
        {
            if (recipes == null || recipes.Count == 0) return null;
            foreach (var r in recipes)
            {
                if (r != null && r.Matches(a, b)) return r;
            }
            return null;
        }
    }
}
