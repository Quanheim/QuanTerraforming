# QuanZoneGrid

Client-only Valheim overlay showing zone (chunk) borders and the sea depth at each zone corner, which is what sets
wave height. Made for terraforming coasts and canals on the Quanheim server.

- What it does and how to use it: [Package/README.md](Package/README.md) (the Thunderstore page).
- Building and releasing: [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).
- Game code it relies on: [docs/valheim-dependencies.md](docs/valheim-dependencies.md).

## How waves use the corners

`WaterVolume.DetectWaterDepth` (on zone load) takes the four corner depths from `Heightmap.GetOceanDepth`, each
`30 - terrain height` at a corner vertex, clamped to 0 and divided by 10. `WaterVolume.Depth` blends them across the
zone, and `CalcWave` scales every wave by that value: 0 means flat water, 1 (10 m or deeper) means full waves.

The catch: `DetectWaterDepth` runs once, in `Start`, straight after the heightmap is built from the world generator.
The zone's `TerrainComp` (your terrain edits) loads afterwards and updates the heightmap's corner depths, but the
sea never re-reads them. So vanilla waves always follow the original seabed. The overlay's posts show the heightmap's
current (edited) depths; the on-screen box shows those beside the sea's own copy (`m_normalizedDepth`).
