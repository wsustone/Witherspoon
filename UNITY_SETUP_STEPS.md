# Unity Setup Steps for 3D FX System

## Step 1: Check Compilation

1. Open Unity
2. Check Console for errors
3. Fix any compilation errors before proceeding

---

## Step 2: Create FX Prefabs

### Create ConeEffect Prefab

1. In Unity, create: `Assets/Resources/Prefabs/FX/` folder structure
2. Create new GameObject: `ConeEffect`
3. Add components:
   - **MeshFilter** - Assign cone mesh (Create > 3D Object > Cone, then drag mesh)
   - **MeshRenderer** - Assign a transparent material
   - **ConeEffect script** (should auto-attach from RequireComponent)
4. Configure material:
   - Shader: Standard or Unlit/Transparent
   - Rendering Mode: Transparent
   - Color: White with alpha ~0.4
5. Save as prefab: `Assets/Resources/Prefabs/FX/ConeEffect.prefab`
6. Delete from scene

### Create AuraEffect Prefab

1. Create new GameObject: `AuraEffect`
2. Add components:
   - **MeshFilter** - Assign sphere mesh (Create > 3D Object > Sphere, then drag mesh)
   - **MeshRenderer** - Assign a transparent material
   - **AuraEffect script** (should auto-attach)
3. Configure material:
   - Shader: Standard or Unlit/Transparent
   - Rendering Mode: Transparent
   - Color: White with alpha ~0.5
4. Save as prefab: `Assets/Resources/Prefabs/FX/AuraEffect.prefab`
5. Delete from scene

### Create WallEffect Prefab

1. Create new GameObject: `WallEffect`
2. Add components:
   - **MeshFilter** - Assign quad/plane mesh
   - **MeshRenderer** - Assign a transparent material
   - **WallEffect script** (should auto-attach)
3. Configure material:
   - Shader: Standard or Unlit/Transparent
   - Rendering Mode: Transparent
   - Color: White with alpha ~0.6
4. Save as prefab: `Assets/Resources/Prefabs/FX/WallEffect.prefab`
5. Delete from scene

---

## Step 3: Test Without Prefab Changes (Temporary)

Before migrating all prefabs, test that the code compiles and runs:

1. Enter Play mode
2. Place a tower (any type)
3. Check Console for warnings about missing FX prefabs
4. Verify tower still functions (even without visuals)

Expected warnings:
- "ConeEffect prefab not found" (for cone towers)
- "AuraEffect prefab not found" (for aura towers)

This is normal - we'll fix it by creating the prefabs above.

---

## Step 4: Add 3D Meshes to Tower Prefabs (After FX work)

For each tower prefab (Wolf, Serpent, Owl, Stag, Bear, Mender):

1. Open prefab in Prefab mode
2. Add child GameObject: `TowerBody`
   - Add **MeshFilter** - Assign cylinder or custom mesh
   - Add **MeshRenderer** - Assign material with tower color
3. Add child GameObject: `TowerGlow` (optional)
   - Add **MeshFilter** - Assign sphere mesh
   - Add **MeshRenderer** - Assign material with attack color
4. Add child GameObject: `FirePoint` (empty transform)
   - Position at top/front of tower
5. In TowerVisuals component:
   - Assign `FirePoint` transform
6. Remove any old SpriteRenderer components
7. Save prefab

---

## Step 5: Add 3D Meshes to Enemy Prefabs (After towers work)

For each enemy prefab:

1. Open prefab in Prefab mode
2. Add child GameObject: `EnemyBody`
   - Add **MeshFilter** - Assign capsule or custom mesh
   - Add **MeshRenderer** - Assign material with faction color
3. Add child GameObject: `EnemyCore` (optional)
   - Add **MeshFilter** - Assign sphere mesh
   - Add **MeshRenderer** - Assign accent color
4. Remove any old SpriteRenderer components
5. Save prefab

---

## Step 6: Test Each Attack Mode

### Projectile (Wolf)
- Should work as before (uses existing WolfProjectile.prefab)

### Beam (Serpent, Mender)
- Should show LineRenderer beam when firing
- Check that beam appears between tower and target

### Cone (Owl)
- Should instantiate ConeEffect prefab
- Cone should point at target
- Cone should scale based on angle and range
- Should disappear after fxDuration

### Aura (Stag)
- Should create persistent AuraEffect
- Should pulse every 3 seconds
- Sphere should expand and fade

### Wall (Bear)
- Not yet implemented - will need WallEffect prefab setup

---

## Troubleshooting

### "Prefab not found in Resources/Prefabs/FX/"
- Make sure prefabs are in exact path: `Assets/Resources/Prefabs/FX/`
- Resources folder must be directly under Assets
- Prefab names must match exactly: ConeEffect, AuraEffect, WallEffect

### "NullReferenceException" on FX components
- Make sure each FX prefab has the correct script component
- Check that MeshRenderer and MeshFilter are present

### Towers/enemies invisible
- Make sure you've added MeshRenderer + MeshFilter to prefabs
- Check that materials are assigned
- Verify meshes are assigned to MeshFilter

### FX not appearing
- Check fxDuration in TowerVisuals (should be ~0.3)
- Verify FirePoint is assigned and positioned correctly
- Check Console for instantiation errors

---

## Current Status

- ✅ Code updated (TowerVisuals, EnemyVisuals, TowerController)
- ✅ FX component scripts created (ConeEffect, AuraEffect, WallEffect)
- ⏳ FX prefabs need to be created in Unity
- ⏳ Tower prefabs need 3D meshes
- ⏳ Enemy prefabs need 3D meshes
- ⏳ Testing required

---

## Order of Operations

1. **First:** Check compilation in Unity Console
2. **Second:** Create FX prefabs (ConeEffect, AuraEffect, WallEffect)
3. **Third:** Test in Play mode - towers should work but may have missing FX warnings
4. **Fourth:** Add 3D meshes to one tower prefab as a test
5. **Fifth:** Test that tower in Play mode
6. **Sixth:** Migrate remaining tower prefabs
7. **Seventh:** Migrate enemy prefabs
8. **Eighth:** Full regression test
