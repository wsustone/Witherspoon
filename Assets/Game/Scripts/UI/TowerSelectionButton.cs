using UnityEngine;
using UnityEngine.UI;
using Witherspoon.Game.Data;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Wire this to a UI Button so clicking it selects a specific tower definition.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TowerSelectionButton : MonoBehaviour
    {
        [SerializeField] private TowerPlacementController placementController;
        [SerializeField] private TowerDefinition towerDefinition;

        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(SelectTower);
        }

        private void SelectTower()
        {
            if (placementController != null && towerDefinition != null)
            {
                placementController.SelectTower(towerDefinition);
            }
        }
    }
}
