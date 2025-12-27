# Witherspoon Mythic Tower Defense – Architecture Notes

## Project Layout
- `Assets/Game/Scripts/Core`: GameLoop, WaveManager, EconomyManager.
- `Assets/Game/Scripts/Map`: GridManager for placement, coordinate conversion.
- `Assets/Game/Scripts/Enemies`: Enemy definitions, agents, and spawner hooks.
- `Assets/Game/Scripts/Towers`: TowerController logic + tie-ins to data.
- `Assets/Game/Scripts/Data`: ScriptableObjects (enemies, towers) so balancing happens outside code.
- `Assets/Mythic`: Placeholder for guardian art/audio/animation drops.
- `Assets/Levels`: Maps (tile palettes, lane definitions) and URP lighting profiles.
- `Assets/Scenes`: Runtime scenes (initial `MythicPlayfield` scene TBD).

## Runtime Flow
1. **GameLoop** initializes GridManager, EconomyManager, and WaveManager.
2. **WaveManager** drives spawn cadence; delegates to `EnemySpawner` with wave number context.
3. **EnemySpawner** creates `EnemyAgent` instances using ScriptableObject data (`EnemyDefinition`). Agents march toward a goal transform and raise `OnKilled` / `OnReachedGoal` callbacks.
4. **EconomyManager** tracks gold (start, passive income, wave reward) and exposes spend/earn hooks for UI and towers.
5. **TowerController** queries enemies within range (Physics2D circle) and applies damage. Replaces the VFX/projectile system once assets arrive.
6. **GridManager** exposes world/grid conversions and placement validation, plus path-blocking toggles for future tower placement rules.

## Immediate Next Steps
1. **Create base scene** (e.g., `MythicPlayfield.unity`) wiring:
   - GridManager + tilemap visuals.
   - EconomyManager / WaveManager / EnemySpawner prefabs.
   - Dummy path (spawn + goal transforms) so waves visibly traverse.
2. **UI/HUD pass**
   - Gold counter binding to EconomyManager.OnGoldChanged.
   - Wave timer / health meter placeholders.
   - Tower build menu stub referencing `TowerDefinition` assets.
3. **Placement + Pathing**
   - Implement placement controller to drop towers onto GridManager cells.
   - Add waypoint path component for EnemyAgent (list of transforms or baked path).
4. **Combat polish**
   - Projectile prefab + effects per tower.
   - Enemy hit feedback (flash, dissolve, death VFX).
5. **Mythic asset integration**
   - Import guardian glyphs/statues, assign to towers/enemies.
   - URP volume for ethereal lighting, fog, and color grading to match Mythic Guardians theme.
6. **Persistence / meta goals**
   - Save system for progression (optional, later once core loop is fun).

Track these tasks in TODO / issues so we can iterate quickly as assets land.
