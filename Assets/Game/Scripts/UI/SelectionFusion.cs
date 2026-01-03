using UnityEngine;
using Witherspoon.Game.Towers;
using Witherspoon.Game.Core;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Handles fusion mode logic for merging towers.
    /// </summary>
    public class SelectionFusion : MonoBehaviour
    {
        [Header("Fusion")]
        [SerializeField] private FusionService fusionService;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private KeyCode fusionHotkey = KeyCode.F;
        [SerializeField] private bool debugFusion = false;

        private bool _isFusing;
        private TowerController _fuseSource;
        private SelectionPanel _panel;
        private SelectionInput _input;

        public bool IsFusing => _isFusing;

        public void Initialize(SelectionPanel panel, SelectionInput input)
        {
            _panel = panel;
            _input = input;

            if (fusionService == null)
            {
                fusionService = FindObjectOfType<FusionService>();
            }
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }
        }

        public bool CheckFusionHotkey()
        {
            return fusionHotkey != KeyCode.None && Input.GetKeyDown(fusionHotkey);
        }

        public void TryEnterFusionMode(TowerController selectedTower)
        {
            if (selectedTower == null)
            {
                _panel?.ShowStatusMessage("Select a tower, then press F to start fusion");
                if (debugFusion) Debug.Log("[SelectionFusion] Fusion requested with no selected tower", this);
                return;
            }
            if (fusionService == null || fusionService.Recipes == null || fusionService.Recipes.Count == 0)
            {
                _panel?.ShowStatusMessage("No fusion recipes configured");
                if (debugFusion) Debug.Log("[SelectionFusion] No fusion recipes configured on FusionService", this);
                return;
            }
            _isFusing = true;
            _fuseSource = selectedTower;
            if (debugFusion) Debug.Log($"[SelectionFusion] Entered fusion mode. Source={_fuseSource.name}", this);
            _panel?.ShowStatusMessage("Fusion: Click a partner tower to merge");
        }

        public void ExitFusionMode()
        {
            _isFusing = false;
            _fuseSource = null;
            if (debugFusion) Debug.Log("[SelectionFusion] Exited fusion mode", this);
        }

        public TowerController TryPickFusionTarget()
        {
            if (_fuseSource == null || fusionService == null || economyManager == null || _input == null)
            {
                ExitFusionMode();
                return null;
            }

            if (_input.TrySelectAtCursor(out TowerController tower, out _))
            {
                if (tower != null && tower != _fuseSource)
                {
                    if (debugFusion) Debug.Log($"[SelectionFusion] Fusion click on target={tower.name}", this);
                    
                    var preview = fusionService.PreviewResult(_fuseSource.Definition, tower.Definition);
                    if (preview == null)
                    {
                        _panel?.ShowStatusMessage("No fusion recipe for this pair");
                        if (debugFusion) Debug.Log("[SelectionFusion] No recipe for pair", this);
                        return null;
                    }
                    
                    bool merged = fusionService.TryMerge(_fuseSource, tower, economyManager);
                    if (merged)
                    {
                        if (debugFusion) Debug.Log("[SelectionFusion] Fusion succeeded", this);
                        ExitFusionMode();
                        return _fuseSource;
                    }
                    else
                    {
                        _panel?.ShowStatusMessage("Fusion failed: requirements not met");
                        if (debugFusion) Debug.Log("[SelectionFusion] Fusion failed: requirements not met", this);
                        return null;
                    }
                }
                
                if (tower == _fuseSource)
                {
                    _panel?.ShowStatusMessage("Pick a different tower to fuse with");
                    if (debugFusion) Debug.Log("[SelectionFusion] Clicked source tower again; waiting for different partner", this);
                    return null;
                }
            }

            return null;
        }
    }
}
