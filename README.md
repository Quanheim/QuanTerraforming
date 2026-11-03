# QuanZoneGrid

Client-only Valheim overlay showing zone (chunk) borders and the sea depth at each zone corner, which is what sets
wave height. Made for terraforming coasts and canals on the Quanheim server.

- What it does and how to use it: [Package/README.md](Package/README.md) (the Thunderstore page).
- Building and releasing: [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).
- Game code it relies on: [docs/valheim-dependencies.md](docs/valheim-dependencies.md).

## How waves use the corners

`WaterVolume.DetectWaterDepth` (on zone load) takes the four corner depths from `Heightmap.GetOceanDepth`, each
`30 - terrain height` at a corner vertex, clamped to 0 and divided by 10. `WaterVolume.Depth` blends them across the
zone, and `CalcWave` scales every wave by that value: 0 means flat water, 1 (10 m or deeper) means full waves. The
overlay reads the same `GetOceanDepth` values, so its colours match what the water will use once the zone reloads.
