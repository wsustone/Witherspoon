using UnityEngine;

namespace Witherspoon.Game.Data
{
    [CreateAssetMenu(menuName = "Witherspoon/Essence Definition", fileName = "EssenceDefinition")]
    public class EssenceDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Boss Essence";
        [SerializeField] private Sprite icon;
        [SerializeField] private Color color = new Color(0.9f, 0.7f, 0.2f);

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color Color => color;
    }
}
