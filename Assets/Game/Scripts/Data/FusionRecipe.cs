using UnityEngine;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.Data
{
    [CreateAssetMenu(menuName = "Witherspoon/Fusion Recipe", fileName = "FusionRecipe")]
    public class FusionRecipe : ScriptableObject
    {
        [Header("Inputs (by Archetype)")]
        [SerializeField] private GuardianArchetype inputLeft;
        [SerializeField] private GuardianArchetype inputRight; // Leave null to require two of inputLeft

        [Header("Result")]
        [SerializeField] private TowerDefinition resultDefinition;

        public GuardianArchetype InputLeft => inputLeft;
        public GuardianArchetype InputRight => inputRight;
        public TowerDefinition ResultDefinition => resultDefinition;

        public bool Matches(TowerDefinition a, TowerDefinition b)
        {
            if (a == null || b == null) return false;
            var aArc = a.Archetype;
            var bArc = b.Archetype;
            if (aArc == null || bArc == null) return false;

            // Case 1: explicit pair
            if (inputLeft != null && inputRight != null)
            {
                return (aArc == inputLeft && bArc == inputRight) || (aArc == inputRight && bArc == inputLeft);
            }

            // Case 2: two of the same archetype (inputLeft)
            if (inputLeft != null && inputRight == null)
            {
                return aArc == inputLeft && bArc == inputLeft;
            }

            return false;
        }
    }
}
