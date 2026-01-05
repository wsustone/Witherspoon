# Visual System Audit - Towers & Enemies

## Attack Mode Reference
- 0 = Projectile
- 1 = Beam
- 2 = Cone
- 3 = Aura
- 4 = Wall

---

## Tower Prefabs (6 total)

### Current Attack Modes by Tower:

| Tower | Attack Mode | Current Visual Type | Notes |
|-------|-------------|---------------------|-------|
| **Wolf** | Projectile (0) | WolfProjectile.prefab | Working - uses projectile prefab |
| **Serpent** | Beam (1) | LineRenderer | Needs LineRenderer + material |
| **Owl** | Cone (2) | SpriteRenderer (wedge) | Needs ConeEffect child with sprite |
| **Stag** | Aura (3) | SpriteRenderer (cone) | Uses cone renderer for aura pulse |
| **Bear** | Wall (4) | Unknown | Wall mode - needs investigation |
| **Mender** | Beam (1) | LineRenderer | Needs LineRenderer + material |

### Fusion/Upgrade Towers (in definitions but may not have prefabs):
- **TimeGrove** - Projectile (0)
- **SkymarkVolley** - Projectile (0)
- **BulwarkBloom** - Projectile (0)
- **DireHowl** - Projectile (0)
- **ObsidianTorrent** - Projectile (0)

---

## Enemy Prefabs (13 total)

**ALL enemies currently use SpriteRenderer (2D sprites)**

| Enemy | Prefab Name | Visual Type | Components |
|-------|-------------|-------------|------------|
| Shades | ShadesAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Glimmer | GlimmerAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Husk | HuskAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Discord | DiscordAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Dread | DreadAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Dreadbound | DreadboundAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Nightglass | NightglassAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Pathforger | PathforgerAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Riftrunner | RiftrunnerAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Ruin | RuinAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| ShardThief | ShardThiefAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| Stagnation | StagnationAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |
| AnchorBreaker | AnchorBreakerAgent.prefab | SpriteRenderer | Transform, SpriteRenderer, EnemyAgent |

### EnemyVisuals Component Analysis:
- **Has placeholder mesh generation** - `BuildPlaceholderMesh()` creates 3D capsule + sphere
- **Disables sprites when placeholder is used** - `DisableSpriteRenderers()`
- **Path visualization** - Uses LineRenderer for showing enemy paths
- **Currently set to use placeholder** - `usePlaceholderMesh = true` by default
- **Same issue as towers** - Placeholder generation conflicts with existing sprites

---

## Current FX Prefabs

| Prefab | Type | Used By | Notes |
|--------|------|---------|-------|
| WolfProjectile.prefab | Projectile | Wolf Tower | Working |
| Beam.prefab | Beam FX | Unknown | May be unused |
| ConeEffect.prefab | Cone FX | Owl/Stag | Needs sprite assignment |
| Wall.prefab | Wall FX | Bear Tower | Need to inspect |

---

## Issues Identified

### Tower Visual Issues:
1. **Mixed sprite/3D system** - Some towers use SpriteRenderer, some use placeholder meshes
2. **Cone renderer conflicts** - ConeEffect creation interferes with existing tower sprites
3. **Beam material errors** - LineRenderer trying to access `.material` instead of `.sharedMaterial` on prefabs
4. **Missing FX assignments** - Serpent and Mender need LineRenderer properly configured
5. **Placeholder mesh generation** - BuildPlaceholderMesh() creates geometry even when prefabs have visuals

### Enemy Visual Issues:
1. **All enemies use 2D sprites** - 100% SpriteRenderer based, no 3D meshes
2. **Placeholder mesh conflicts** - EnemyVisuals tries to create 3D geometry over sprites
3. **Same pattern as towers** - DisableSpriteRenderers() hides original visuals when placeholder is built

---

## Recommended Migration Path

### Phase 1: Standardize Tower Visuals
1. Convert all towers to use 3D mesh bodies (low-poly placeholders initially)
2. Create separate 3D FX prefabs for each attack mode:
   - **Projectile** → Small mesh projectile (sphere/arrow)
   - **Beam** → Particle system or mesh tube
   - **Cone** → Mesh cone that scales/rotates
   - **Aura** → Expanding sphere mesh or particle ring
   - **Wall** → Mesh barrier

### Phase 2: Standardize Enemy Visuals
1. Inspect all enemy prefabs to catalog current setup
2. Convert all enemies to 3D meshes
3. Update EnemyVisuals to handle 3D only

### Phase 3: Clean Up Code
1. Remove all SpriteRenderer handling from TowerVisuals
2. Remove BuildPlaceholderMesh() - require real meshes
3. Update FX coroutines to use 3D FX prefabs instead of renderers
4. Document new prefab requirements

---

## Summary of Findings

### Current State:
- **All 13 enemies** use 2D SpriteRenderer exclusively
- **All 6 tower prefabs** have mixed visual setup (some sprites, some attempting 3D)
- **Both systems** have placeholder mesh generation that conflicts with existing visuals
- **FX system** is inconsistent - projectiles work, beams/cones broken

### Root Cause:
The refactoring created `TowerVisuals` and `EnemyVisuals` components that try to generate 3D placeholder meshes, but the original prefabs all use 2D sprites. The placeholder generation disables the sprites and creates 3D geometry, breaking the original visuals.

### The Path Forward:
**Option A: Full 3D Migration (Recommended)**
- Convert all towers and enemies to 3D meshes
- Create 3D FX prefabs for all attack modes
- Remove all sprite handling code
- Clean, consistent system

**Option B: Full 2D Reversion**
- Remove placeholder mesh generation entirely
- Keep all sprites
- Fix FX to work with 2D sprites only
- Simpler but less flexible

**Recommendation: Option A** - Full 3D migration will provide:
- Better visual consistency
- Easier to add new content
- More professional appearance
- Cleaner codebase

---

## Next Steps

1. ✅ **Audit complete** - This document
2. ✅ **Inspect enemy prefabs** - All use SpriteRenderer (2D)
3. ⏳ **Design 3D FX system** - Define how each attack mode will work with 3D
4. ⏳ **Create placeholder 3D assets** - Simple meshes for testing
5. ⏳ **Update TowerVisuals/EnemyVisuals** - Remove sprite code, add 3D FX slots
6. ⏳ **Migrate prefabs** - Update each tower/enemy one by one
7. ⏳ **Test and document** - Verify all attack modes work correctly
