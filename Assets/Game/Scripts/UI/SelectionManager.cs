using UnityEngine;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Coordinates tower/enemy selection and feeds data to the SelectionPanel.
    /// </summary>
    [RequireComponent(typeof(SelectionInput))]
    [RequireComponent(typeof(SelectionFusion))]
    public class SelectionManager : MonoBehaviour
    {
        [SerializeField] private TowerPlacementController placementController;
        [SerializeField] private SelectionPanel selectionPanel;
        [SerializeField] private KeyCode clearSelectionKey = KeyCode.Escape;

        private TowerController _selectedTower;
        private EnemyAgent _selectedEnemy;

        private SelectionInput _input;
        private SelectionFusion _fusion;

        private void Awake()
        {
            _input = GetComponent<SelectionInput>();
            _fusion = GetComponent<SelectionFusion>();
        }

        private void Start()
        {
            _fusion?.Initialize(selectionPanel, _input);
        }

        private void Update()
        {
            if (selectionPanel == null) return;

            _input?.EnsureCamera();

            if (placementController != null && placementController.IsPlacing)
            {
                ClearSelection();
                return;
            }

            if (clearSelectionKey != KeyCode.None && Input.GetKeyDown(clearSelectionKey))
            {
                if (_fusion != null && _fusion.IsFusing)
                {
                    _fusion.ExitFusionMode();
                    if (_selectedTower != null)
                    {
                        selectionPanel.ShowTower(_selectedTower);
                    }
                    else
                    {
                        selectionPanel.Hide();
                    }
                }
                else
                {
                    ClearSelection();
                }
                return;
            }

            if (_fusion != null && _fusion.CheckFusionHotkey())
            {
                _fusion.TryEnterFusionMode(_selectedTower);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (_fusion != null && _fusion.IsFusing)
                {
                    var fusedTower = _fusion.TryPickFusionTarget();
                    if (fusedTower != null)
                    {
                        _selectedTower = fusedTower;
                        _selectedEnemy = null;
                        selectionPanel.ShowTower(_selectedTower);
                    }
                }
                else
                {
                    if (_input != null && _input.IsPointerOverUI(selectionPanel))
                    {
                        return;
                    }
                    TrySelectAtCursor();
                }
            }
        }

        private void TrySelectAtCursor()
        {
            if (_input == null) return;

            if (_input.TrySelectAtCursor(out TowerController tower, out EnemyAgent enemy))
            {
                if (tower != null)
                {
                    SelectTower(tower);
                    return;
                }

                if (enemy != null)
                {
                    SelectEnemy(enemy);
                    return;
                }
            }

            ClearSelection();
        }

        private void SelectTower(TowerController tower)
        {
            _selectedTower = tower;
            _selectedEnemy = null;
            selectionPanel?.ShowTower(tower);
        }

        private void SelectEnemy(EnemyAgent enemy)
        {
            _selectedEnemy = enemy;
            _selectedTower = null;
            selectionPanel?.ShowEnemy(enemy);
        }

        private void ClearSelection()
        {
            if (_selectedTower == null && _selectedEnemy == null) return;

            _selectedTower = null;
            _selectedEnemy = null;
            selectionPanel?.Hide();
        }
    }
}
