# 3D FX System Design

## Overview
This document defines how all attack modes will work with 3D assets, replacing the current mixed 2D/3D system.

---

## Design Principles

1. **Consistency** - All towers and enemies use 3D meshes
2. **Modularity** - FX are separate prefabs that can be reused
3. **Performance** - Simple geometry, efficient rendering
4. **Clarity** - Visual effects clearly communicate attack type and range
5. **Flexibility** - Easy to swap/customize per tower

---

## Tower Body Visuals

### Standard Tower Structure
Each tower prefab will have:
- **Root GameObject** - TowerController + components
- **Body Mesh** - 3D model (low-poly placeholder initially)
  - Simple geometric shapes: cylinder, cube, pyramid, etc.
  - Colored by `TowerDefinition.HighlightColor`
- **Glow/Accent Mesh** (optional) - Visual interest
  - Sphere or other shape at top
  - Colored by `TowerDefinition.AttackColor`
- **Fire Point** - Empty GameObject marking projectile spawn location
  - Positioned at top/front of tower
  - Used for beam origin, projectile spawn, etc.

### Example Tower Hierarchy
```
Wolf_Tower (TowerController)
├── TowerBody (MeshFilter + MeshRenderer)
│   └── Material: HighlightColor
├── TowerGlow (MeshFilter + MeshRenderer)
│   └── Material: AttackColor
└── FirePoint (Transform)
```

---

## Enemy Body Visuals

### Standard Enemy Structure
Each enemy prefab will have:
- **Root GameObject** - EnemyAgent + components
- **Body Mesh** - 3D model (low-poly placeholder initially)
  - Capsule or simple character shape
  - Colored by `EnemyDefinition.FactionColor`
- **Core/Accent Mesh** (optional) - Visual interest
  - Sphere or crystal shape
  - Slightly brighter/different hue

### Example Enemy Hierarchy
```
Shades_Enemy (EnemyAgent)
├── EnemyBody (MeshFilter + MeshRenderer)
│   └── Material: FactionColor
└── EnemyCore (MeshFilter + MeshRenderer)
    └── Material: AccentColor
```

---

## Attack Mode FX System

### 1. Projectile Mode (AttackStyle.Projectile)

**Current:** Wolf Tower - spawns WolfProjectile.prefab

**3D Design:**
- **Projectile Prefab** - Small 3D mesh that travels to target
  - Sphere, arrow, or custom shape
  - `ProjectileBehaviour` component handles movement
  - Colored by `TowerDefinition.AttackColor`
  - Trail renderer (optional) for motion blur

**Implementation:**
```csharp
// In TowerController.FireAtTarget()
case TowerDefinition.AttackStyle.Projectile:
    if (definition.ProjectilePrefab != null)
    {
        Vector3 spawnPos = _visuals.FirePoint.position;
        var projectile = Instantiate(definition.ProjectilePrefab, spawnPos, Quaternion.identity);
        projectile.GetComponent<ProjectileBehaviour>().Initialize(target, damage, speed, color, this);
    }
    break;
```

**Required Prefabs:**
- Generic projectile prefabs (sphere, arrow, bolt)
- Each tower can reference its preferred projectile in TowerDefinition

---

### 2. Beam Mode (AttackStyle.Beam)

**Current:** Serpent, Mender - uses LineRenderer (broken)

**3D Design:**
- **Beam Visual** - Cylinder mesh stretched between tower and target
  - OR: Particle system with beam shape
  - OR: Keep LineRenderer but with proper 3D material
  - Colored by `TowerDefinition.AttackColor`
  - Glows/pulses during attack

**Implementation Option A - Mesh Beam:**
```csharp
// In TowerController.FireBeamFx()
private IEnumerator FireBeamFx(EnemyAgent target)
{
    GameObject beamObj = Instantiate(beamPrefab, _visuals.FirePoint.position, Quaternion.identity);
    BeamEffect beam = beamObj.GetComponent<BeamEffect>();
    
    beam.SetEndpoints(_visuals.FirePoint, target.transform);
    beam.SetColor(definition.AttackColor);
    beam.Show();
    
    yield return new WaitForSeconds(_visuals.FxDuration);
    
    beam.Hide();
    Destroy(beamObj, 0.5f); // Allow fade out
}
```

**Implementation Option B - LineRenderer (simpler):**
- Keep current LineRenderer approach
- Fix material assignment to use sharedMaterial
- Add proper 3D shader with glow
- Pool LineRenderer objects for performance

**Required Prefabs:**
- BeamEffect.prefab (cylinder mesh or particle system)
- OR: Configure LineRenderer properly on tower prefabs

**Recommendation:** Option B (LineRenderer) - simpler, already partially implemented

---

### 3. Cone Mode (AttackStyle.Cone)

**Current:** Owl - uses SpriteRenderer wedge (broken)

**3D Design:**
- **Cone Visual** - 3D cone mesh that appears during attack
  - Cone primitive or custom mesh
  - Scales to match `ConeAngle` and `Range`
  - Rotates to point at target
  - Semi-transparent material
  - Colored by `TowerDefinition.AttackColor`

**Implementation:**
```csharp
// In TowerController.FireConeFx()
private IEnumerator FireConeFx(EnemyAgent target)
{
    GameObject coneObj = Instantiate(conePrefab, _visuals.FirePoint.position, Quaternion.identity);
    ConeEffect cone = coneObj.GetComponent<ConeEffect>();
    
    Vector3 direction = (target.transform.position - _visuals.FirePoint.position).normalized;
    cone.SetDirection(direction);
    cone.SetSize(definition.ConeAngle, definition.Range);
    cone.SetColor(definition.AttackColor);
    cone.Show();
    
    yield return new WaitForSeconds(_visuals.FxDuration);
    
    cone.Hide();
    Destroy(coneObj, 0.5f);
}
```

**ConeEffect Component:**
```csharp
public class ConeEffect : MonoBehaviour
{
    private MeshRenderer _renderer;
    private Material _material;
    
    public void SetDirection(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    public void SetSize(float angle, float range)
    {
        // Scale cone mesh to match angle and range
        float radius = Mathf.Tan(angle * Mathf.Deg2Rad * 0.5f) * range;
        transform.localScale = new Vector3(radius, range, radius);
    }
    
    public void SetColor(Color color)
    {
        color.a = 0.4f; // Semi-transparent
        _material.color = color;
    }
    
    public void Show() => _renderer.enabled = true;
    public void Hide() => _renderer.enabled = false;
}
```

**Required Prefabs:**
- ConeEffect.prefab (cone mesh with transparent material)

---

### 4. Aura Mode (AttackStyle.Aura)

**Current:** Stag - uses cone renderer for pulse effect

**3D Design:**
- **Aura Visual** - Expanding sphere or ring around tower
  - Sphere mesh that scales up during pulse
  - OR: Particle system with radial emission
  - Semi-transparent material
  - Colored by `TowerDefinition.AttackColor`
  - Pulses every few seconds

**Implementation:**
```csharp
// In TowerController.PulseAuraFx()
private void PulseAuraFx()
{
    if (_auraEffect == null)
    {
        _auraEffect = Instantiate(auraPrefab, transform.position, Quaternion.identity, transform);
    }
    
    _auraEffect.GetComponent<AuraEffect>().Pulse(definition.Range, definition.AttackColor);
}
```

**AuraEffect Component:**
```csharp
public class AuraEffect : MonoBehaviour
{
    private MeshRenderer _renderer;
    private Material _material;
    
    public void Pulse(float range, Color color)
    {
        StartCoroutine(PulseCoroutine(range, color));
    }
    
    private IEnumerator PulseCoroutine(float range, Color color)
    {
        color.a = 0.5f;
        _material.color = color;
        
        float elapsed = 0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Scale up
            float scale = Mathf.Lerp(0.5f, range * 2f, t);
            transform.localScale = Vector3.one * scale;
            
            // Fade out
            Color c = color;
            c.a = Mathf.Lerp(0.5f, 0f, t);
            _material.color = c;
            
            yield return null;
        }
        
        _renderer.enabled = false;
    }
}
```

**Required Prefabs:**
- AuraEffect.prefab (sphere mesh with transparent material)

---

### 5. Wall Mode (AttackStyle.Wall)

**Current:** Bear - unknown implementation

**3D Design:**
- **Wall Visual** - Barrier mesh that appears in front of tower
  - Flat plane or fence-like mesh
  - Blocks enemy path
  - Semi-transparent material
  - Colored by `TowerDefinition.AttackColor`
  - Stays visible while active

**Implementation:**
```csharp
// In TowerController - Wall mode creates persistent barrier
case TowerDefinition.AttackStyle.Wall:
    if (_wallEffect == null)
    {
        Vector3 wallPos = transform.position + transform.forward * 1.5f;
        _wallEffect = Instantiate(wallPrefab, wallPos, transform.rotation, transform);
        _wallEffect.GetComponent<WallEffect>().Initialize(definition.Range, definition.AttackColor);
    }
    // Wall damages enemies that touch it
    break;
```

**Required Prefabs:**
- WallEffect.prefab (plane mesh with transparent material + collider)

---

## Component Architecture

### TowerVisuals (Updated)

**Remove:**
- All SpriteRenderer handling
- `BuildPlaceholderMesh()` - require real meshes in prefabs
- `coneRenderer` field

**Keep:**
- `FirePoint` transform reference
- `fxDuration` for effect timing

**Add:**
- FX prefab references (optional, can be in TowerDefinition instead)

```csharp
public class TowerVisuals : MonoBehaviour
{
    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fxDuration = 0.3f;
    
    public Transform FirePoint => firePoint;
    public float FxDuration => fxDuration;
    
    // No more placeholder generation
    // No more sprite handling
    // Just a simple component for FX timing
}
```

### EnemyVisuals (Updated)

**Remove:**
- All SpriteRenderer handling
- `BuildPlaceholderMesh()` - require real meshes in prefabs

**Keep:**
- Path visualization (LineRenderer for showing enemy paths)

```csharp
public class EnemyVisuals : MonoBehaviour
{
    private LineRenderer _pathRenderer;
    
    // Only handles path visualization
    // No mesh generation
    // Enemies must have real 3D models in prefabs
}
```

### New FX Components

Create simple effect components:
- `BeamEffect.cs` - Handles beam visual (if using mesh approach)
- `ConeEffect.cs` - Handles cone visual
- `AuraEffect.cs` - Handles aura pulse visual
- `WallEffect.cs` - Handles wall barrier visual

---

## Migration Strategy

### Phase 1: Create FX Prefabs
1. Create simple placeholder meshes for each FX type
2. Create prefabs with appropriate components
3. Test each FX type in isolation

### Phase 2: Update Code
1. Remove sprite handling from TowerVisuals/EnemyVisuals
2. Update TowerController FX coroutines to use new system
3. Add FX prefab references to TowerDefinition

### Phase 3: Update Prefabs
1. Add 3D meshes to all tower prefabs (simple placeholders)
2. Add 3D meshes to all enemy prefabs (simple placeholders)
3. Assign FirePoint transforms
4. Remove all SpriteRenderer components

### Phase 4: Test & Polish
1. Test each tower type
2. Test each enemy type
3. Verify all attack modes work
4. Document new prefab requirements

---

## File Structure

```
Assets/Game/Prefabs/FX/
├── ProjectileSphere.prefab
├── ProjectileArrow.prefab
├── BeamEffect.prefab
├── ConeEffect.prefab
├── AuraEffect.prefab
└── WallEffect.prefab

Assets/Game/Scripts/FX/
├── BeamEffect.cs
├── ConeEffect.cs
├── AuraEffect.cs
└── WallEffect.cs

Assets/Game/Prefabs/Towers/
├── Wolf_Tower.prefab (with 3D mesh)
├── Serpent_Tower.prefab (with 3D mesh)
├── Owl_Tower.prefab (with 3D mesh)
└── ... (all with 3D meshes)

Assets/Game/Prefabs/Enemies/
├── Shades_Enemy.prefab (with 3D mesh)
├── Glimmer_Enemy.prefab (with 3D mesh)
└── ... (all with 3D meshes)
```

---

## Next Steps

1. ✅ Design complete - This document
2. ⏳ Create FX component scripts (BeamEffect, ConeEffect, AuraEffect, WallEffect)
3. ⏳ Create FX prefabs with placeholder meshes
4. ⏳ Update TowerVisuals/EnemyVisuals (remove sprite code)
5. ⏳ Update TowerController FX coroutines
6. ⏳ Create tower/enemy placeholder 3D meshes
7. ⏳ Migrate all prefabs
8. ⏳ Test and verify
