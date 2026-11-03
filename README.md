# QuanTerraforming

Valheim terraforming helper for the Quanheim server: the sea's waves follow terrain edits, and a zone-grid overlay
shows the corner depths that set wave height.

- What it does and how to use it: [Package/README.md](Package/README.md) (the Thunderstore page).
- Building and releasing: [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).
- Game code it relies on: [docs/valheim-dependencies.md](docs/valheim-dependencies.md).

## How waves use the corners

`WaterVolume.DetectWaterDepth` takes the four corner depths from `Heightmap.GetOceanDepth`, each
`30 - terrain height` at a corner vertex, clamped to 0 and divided by 10. `WaterVolume.Depth` blends them across the
zone, and `CalcWave` scales every wave by that value: 0 means flat water, 1 (10 m or deeper) means full waves.

The vanilla bug: `DetectWaterDepth` runs once, in `Start`, straight after the heightmap is built from the world
generator. The zone's `TerrainComp` (the players' terrain edits) loads afterwards and updates the heightmap's corner
depths, but the sea never re-reads them, so waves always follow the original seabed.

The fix ([SeaFollowsTerrain.cs](SeaFollowsTerrain.cs)) is a postfix on `Heightmap.UpdateCornerDepths`, which runs on
every full regenerate (load, edits arriving, each new edit). It clears the sea's cached `m_oneDepth`, then calls its
`DetectWaterDepth` and `SetupMaterial` again, so both the physics and the wave shader get the new depths.

The overlay ([ZoneGridOverlay.cs](ZoneGridOverlay.cs)) draws the corner posts from the heightmap's depths, and its
on-screen box shows those beside the sea's own copy (`m_normalizedDepth`), so you can see whether they agree.
