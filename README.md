# QuanTerraforming

Valheim terraforming mod for the Quanheim server: sea waves follow terrain edits, the 8 m terrain limit is lifted,
and the Terraforming Wand shows the zone grid. Shaping tools (natural ground, beaches) are designed and come next.

- What it does and how to use it: [Package/README.md](Package/README.md) (the Thunderstore page).
- **Where we are and what's next: [docs/design.md](docs/design.md).**
- Building and releasing: [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).
- Game code it relies on: [docs/valheim-dependencies.md](docs/valheim-dependencies.md).

## Code

| File | What it does |
|---|---|
| [Plugin.cs](Plugin.cs) | Settings, Harmony, start-up. |
| [SeaFollowsTerrain.cs](SeaFollowsTerrain.cs) | The wave fix. |
| [TerrainLimits.cs](TerrainLimits.cs) | Lifts the 8 m limit. |
| [ZoneGridOverlay.cs](ZoneGridOverlay.cs) | The zone grid, shown while the wand is held. |
| [Wand/TerraformingWand.cs](Wand/TerraformingWand.cs) | The wand item: cloned from the Hammer, recipe, no swing. |
| [Wand/WandMesh.cs](Wand/WandMesh.cs) | The wand's model, built in code. |
| [Assets/wand_icon.png](Assets/wand_icon.png) | Inventory icon, embedded in the DLL. |
| [tools/](tools/) | Python scripts that read the game's assets to design the wand, its mockup and its icon. |

## How waves use the corners

`WaterVolume.DetectWaterDepth` takes the four corner depths from `Heightmap.GetOceanDepth`, each
`30 - terrain height` at a corner vertex, clamped to 0 and divided by 10. `WaterVolume.Depth` blends them across the
zone, and `CalcWave` scales every wave by that value: 0 means flat water, 1 (10 m or deeper) means full waves.

The vanilla bug: `DetectWaterDepth` runs once, in `Start`, straight after the heightmap is built from the world
generator. The zone's `TerrainComp` (the players' terrain edits) loads afterwards and updates the heightmap's corner
depths, but the sea never re-reads them, so waves always follow the original seabed.

The fix is a postfix on `Heightmap.UpdateCornerDepths`, which runs on every full regenerate (load, edits arriving,
each new edit). It clears the sea's cached `m_oneDepth`, then calls its `DetectWaterDepth` and `SetupMaterial` again,
so both the physics and the wave shader get the new depths.

## Terrain limits

Transpilers on `TerrainComp.LevelTerrain`, `RaiseTerrain` and `ApplyToHeightmap` replace vanilla's 8 m constants
with the synced `1 - Heightmap` Upper/Lower limit settings (vanilla 8 m while `0 - General` Enabled is off). This replaces Heightmap Unlimited JvL (our own code; same idea). With
that mod installed, `Prepare` skips these patches, and start-up logs a warning.

## The wand

A `CustomItem` cloned from `Hammer`, with `m_buildPieces` cleared (not a build tool until it has tools), no
durability, and a prefix on `Humanoid.StartAttack` so it doesn't swing. Its model replaces the mesh on
`Hammer/attach/hammer` with a turned shape built in that mesh's own space, so the hand holds it exactly where it holds
the hammer. Grip and collar use the hammer handle's patch of the `weapons` atlas; the body uses `DarkWood_mat`
(darkwood beams and roof). The dropped item's box collider is refitted to the wand.

The profile (radius along the length) lives in `WandMesh.Profile` and in `tools/render_wand.py`, which renders the
approved mockup ([docs/images/wand-mockup.png](docs/images/wand-mockup.png)). Change both together.
