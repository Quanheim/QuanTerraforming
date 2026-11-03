# Valheim Code Dependency

Everything this mod relies on from Valheim, so a game update can be checked quickly. `check-mods.ps1` in the
`.decompiles` repo reads the **Game code** tables and flags any entry the game changed. See "Valheim updates" in
`docs/DEVELOPMENT.md` for the whole check.

**Checked against Valheim:** 1.0.21 (2026-11-02)

Update this file in the same commit as any code that patches, calls or relies on something new in the game, and
update the version line above each time the mod passes the Valheim update check.

## Game code

The first column holds the game class or member in backticks, as `Class` or `Class.Member`. Nested types use their
parent's name, e.g. `Player.PlacementStatus`. Only the first column is read by the script; the rest is for people.

### Harmony patches

What the mod patches. A changed signature stops the patch applying; changed logic can make it wrong.

| Game code | Patch | What it does | If the game changes it |
|---|---|---|---|
| `TerrainComp.LevelTerrain` | Transpiler | Replaces the `-8f`/`8f` clamp on the stored height change with the Lower/Upper limit settings. | Constants moved or changed: logs an error and that limit stays at vanilla. |
| `TerrainComp.RaiseTerrain` | Transpiler | As above. | As above. |
| `TerrainComp.ApplyToHeightmap` | Transpiler | Replaces the two `8f` in `Clamp(h, base - 8, base + 8)`: first with the lower limit, second with the upper limit. | Order of the two constants changes: dig and raise swap. Constants gone: logs an error. |
| `Heightmap.UpdateCornerDepths` | Postfix | Makes the zone's sea re-read its corner depths, so waves follow terrain edits. | Renamed or no longer called on full regenerate: waves go back to the world-generated seabed. |
| `Humanoid.StartAttack` | Prefix | Returns false while the Terraforming Wand is the current weapon, so it doesn't swing. | Renamed: the wand swings like the hammer. |

### Private members reached by reflection

Fields, properties and methods read or called through `AccessTools`, `Traverse` or `System.Reflection`. These fail
at runtime, not compile time, if renamed.

| Game code | Kind | Used for |
|---|---|---|
| `WaterVolume.m_normalizedDepth` | Private field (`float[4]`) | The corner depths the sea is actually using, shown beside the terrain's own. |
| `WaterVolume.m_oneDepth` | Private field (`float`) | Cleared to -1 before re-reading: `DetectWaterDepth` sets it only when all corners match and never clears it. |
| `WaterVolume.DetectWaterDepth` | Private method | Re-reads the heightmap's corner depths. |
| `WaterVolume.SetupMaterial` | Private method | Pushes the new depths to the water shader, which draws the waves. |

### Public game code called or read

Methods, fields and components used directly. A rename or signature change breaks the build, which is easy to spot;
a change in behaviour isn't, so list the ones whose behaviour the mod depends on.

| Game code | Used for |
|---|---|
| `Heightmap.GetAllHeightmaps` | Lists loaded zone heightmaps to read their corner depths. |
| `Heightmap.IsDistantLod` | Skips the distant low-detail terrain, which isn't a zone. |
| `Heightmap.GetOceanDepth` | The four corner depths (array overload), the same values the water uses for waves. |
| `Heightmap.m_width` | With `m_scale`, places each corner in the world. |
| `Heightmap.m_scale` | As above. |
| `Heightmap.GetHeight` | Static `(Vector3, out float)`: terrain height for drawing border lines and unloaded corner posts. |
| `Heightmap.FindHeightmap` | `(Vector3)`: the heightmap of the zone the player is in. |
| `WaterVolume.Instances` | Finds the sea belonging to that zone. |
| `WaterVolume.m_heightmap` | Matches a sea to its zone's heightmap. |
| `WaterVolume.m_waterSurface` | Skips a sea without a surface renderer before calling `SetupMaterial`. |
| `Player.m_localPlayer` | Where to centre the grid. |
| `Humanoid.GetCurrentWeapon` | Whether the player is holding the wand (grid on, no swing). |
| `ItemDrop.ItemData.SharedData.m_buildPieces` | Cleared on the wand, so it isn't a build tool. |
| `ItemDrop.ItemData.SharedData.m_useDurability` | Off for the wand. |
| `ItemDrop.ItemData.SharedData.m_canBeReparied` | Off for the wand. |
| `ItemDrop.ItemData.SharedData.m_weight` | The wand weighs 0.5. |
| `ItemDrop.ItemData.SharedData.m_name` | Recognises the wand by its name token. |

### Behaviour the mod assumes

Things the game does that the mod counts on without calling them, e.g. "only the owner runs
`Pickable.UpdateRespawn`". These break silently, so name the code where the behaviour lives.

| Game code | Assumption |
|---|---|
| `Heightmap.UpdateCornerDepths` | Corner order is 0=(xmin,zmax), 1=(xmax,zmax), 2=(xmax,zmin), 3=(xmin,zmin); depth is `max(0, 30 - height)`. |
| `WaterVolume.DetectWaterDepth` | Waves read the heightmap's corner depths once, on `Start`, divided by 10 and clamped to 0..1. That runs before the zone's `TerrainComp` loads, so vanilla keeps the unedited world-generated depths; calling it again is safe. |
| `Heightmap.Regenerate` | Height edits (and the `TerrainComp` loading) request a full regenerate, which calls `UpdateCornerDepths`. |
| `WaterVolume.GetWaterSurface` | Floating objects and ships get wave height from `Depth`, so the fix changes physics as well as looks. |
| `WaterVolume.Depth` | Wave strength is a bilinear blend of the four corners across the zone. |
| `WaterVolume.CalcWave` | Wave height scales linearly with that blended depth (0 = flat). |
| `ZoneSystem.m_zoneSize` | Zones are 64 m, centred on multiples of 64 (hard-coded in the overlay). |
| `ZoneSystem.m_waterLevel` | Sea level is 30 m (hard-coded in the overlay). |
| `VisEquipment` | Shows a held item by copying its prefab's `attach` child into the hand, so replacing the mesh under `attach` changes what's held. |

### Planned (shaping tools, see docs/design.md)

*Planned*, not in the code yet. These move into the tables above, as built, when the tools land. Kept out of the
tables so `check-mods.ps1` only checks what the mod really uses.

- *planned* `TerrainComp` private `m_levelDelta`, `m_smoothDelta`, `m_modifiedHeight`, `Save`, `m_nview`: writing
  per-point heights on the owner, as `LevelTerrain` and `DoOperation` do.
- *planned* `Heightmap.GetAndCreateTerrainCompiler`, `Heightmap.FindHeightmap(Vector3, float, List)`,
  `Heightmap.GetHeight(int, int)`, `Heightmap.WorldToVertex`, `Heightmap.Poke`: finding and refreshing each zone.
- *planned* `ClutterSystem.ResetGrass`: regrowing grass on the new surface.
- *planned* A postfix on `TerrainComp.Awake` to register the wand's RPC on each terrain record.
- *planned* `ItemDrop.ItemData.SharedData.m_buildPieces` set to the wand's own piece table (in place of null).

## Prefabs and assets

Not game code, so no script can check these. They come from the game's asset bundles: vanilla prefabs the mod
clones or reads, child objects it finds by name, materials, shader properties and layer names. Test them in game
after each update.

| Prefab or asset | Used for | Breaks if |
|---|---|---|
| Shader `Sprites/Default` (falls back to `Hidden/Internal-Colored`, then `UI/Default`) | Line material for borders and posts. | Lines are invisible or pink: the build no longer includes these shaders. |
| Prefab `Hammer` | Cloned for the wand (hand position, animations, sounds). | Renamed: no wand. |
| `Hammer/attach/hammer` (MeshFilter + MeshRenderer, mesh `hammer_Cube.006`, rotated 90° on X, scale 0.3) | Its mesh is swapped for the wand's; the wand is built in that mesh's space (+Y from -0.342 to 1.228). | Moved or renamed: logs an error and the wand looks like a hammer. Re-oriented or rescaled: the wand sits wrong in the hand. |
| `Hammer/collider` (BoxCollider) | Refitted to the wand for the dropped item. | Missing: the dropped wand keeps the hammer's collider (cosmetic). |
| Material `weapons1` (on the hammer), atlas patch u 0.807–0.834, v 0.627–0.734 | The wand's grip and collar. | Atlas rearranged: the grip shows the wrong texture. |
| Material `DarkWood_mat` (darkwood beams/roof), left half of `DarkWoodBeams_d` | The wand's body. | Renamed: logs a warning and the body uses the grip's wood. |
| Prefab `Wood` | Recipe: 2 Wood. | Renamed: no recipe. |
