# Beam Rendering Issue

## Status
**Not Visible** - Beam attack mode (Serpent/Mender towers) is configured correctly but not rendering visually.

## What's Working
- Beam logic executes correctly
- Damage is applied to enemies
- LineRenderer is created and configured
- Positions are set correctly
- Material and colors are assigned
- Beam is enabled (renderer.enabled = true)

## Debug Logs Confirm
```
[TowerController] Serpent_Tower: Tower at (9.50, 1.50, 0.00), FirePoint at (9.50, 1.50, 0.00), Target at (9.44, 0.44, 0.00)
[TowerController] Serpent_Tower: Firing beam from (9.50, 1.50, 0.00) to (9.44, 0.44, 0.00), renderer.enabled=True
[TowerController] Serpent_Tower: Beam enabled. Material=Sprites/Default, Width=1.0, Color=...
```

## Attempted Fixes
1. ✅ Changed material from `Unlit/Color` to `Sprites/Default`
2. ✅ Increased width from 0.2 → 0.5 → 1.0
3. ✅ Set sortingOrder to 1000
4. ✅ Tried different alignments (View, TransformZ)
5. ✅ Set Z-position to 0 for 2D visibility
6. ✅ Set full opacity (alpha = 1.0)
7. ✅ Used `sharedMaterial` instead of `material`

## Current Configuration
```csharp
beamRenderer.useWorldSpace = true;
beamRenderer.startWidth = 1.0f;
beamRenderer.endWidth = 1.0f;
beamRenderer.alignment = LineAlignment.TransformZ;
beamRenderer.sortingLayerName = "Default";
beamRenderer.sortingOrder = 1000;
beamRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
```

## Possible Causes
1. **Camera Issue** - Orthographic camera might not render LineRenderer at Z=0
2. **Rendering Layer** - LineRenderer might be on wrong layer for camera
3. **Shader Compatibility** - `Sprites/Default` shader might not work with LineRenderer in this setup
4. **Unity Version** - Specific Unity version rendering quirk
5. **Scene Lighting** - Unlit shader might need different setup

## Next Steps to Try
1. Check if beam is visible in **Scene view** vs **Game view**
2. Try creating a test LineRenderer manually in Unity Editor to verify it can render
3. Check camera's culling mask and rendering layers
4. Try `Unlit/Texture` shader instead of `Sprites/Default`
5. Try `Particles/Standard Unlit` shader
6. Check if LineRenderer needs to be on a specific GameObject layer
7. Try setting `shadowCastingMode` and `receiveShadows`

## Workaround
For now, beam towers (Serpent/Mender) apply damage correctly but have no visual effect. All other attack modes work:
- ✅ Projectile (Wolf)
- ✅ Cone (Owl)
- ✅ Aura (Stag)
- ✅ Wall (Bear)

## Files Involved
- `/Assets/Game/Scripts/Towers/TowerVisuals.cs` - Lines 39-83 (EnsureBeamRendererDefaults)
- `/Assets/Game/Scripts/Towers/TowerController.cs` - Lines 181-206 (FireBeamFx)
