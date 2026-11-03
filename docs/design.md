# QuanTerraforming: design

Status: **wand and shaping tools designed, not built** (3 Nov 2026). Already built: sea follows terrain edits,
terrain limits, zone grid.

## Why

The terrain tools we have (vanilla hoe, MyDirtyHoe, PlanBuild) all make flat levels and clean straight slopes. Natural
ground isn't like that, and land raised out of the sea looks like a table top. These tools shape gently uneven
ground and natural beaches, as a base for planting and building.

## The Terraforming Wand

A tool you hold, like the hammer or hoe, with its own build menu of shaping tools.

| | |
|---|---|
| Name | Terraforming Wand |
| Description | "Shapes the land the way nature would: rolling ground, natural beaches. Part of QuanTerraforming." |
| Recipe | 2 Stone, no crafting station: made from the crafting panel in your inventory, like the hammer. |
| Model | The hoe's, to start with (cloned with Jotunn). A model of its own can come later. |
| Build menu | Its own piece table with one entry per tool: **Natural ground**, **Beach**, later **Undo**. |

**Decision to confirm:** the request was to put it in QuanPieces' tab on the hammer. That tab holds things you
place; the wand is an item you hold, so it's crafted from the inventory instead, the same as the hammer. Its tools
then live in the wand's own build menu.

Holding the wand shows the zone grid automatically (corner depths matter to a coast, see the README), as well as
the usual toggle.

## Natural ground

Gently uneven ground across a circle, or across a whole zone.

| Step | Mouse | What it sets |
|---|---|---|
| 1. Area | Aim; scroll | Radius, 4–32 m. A key snaps the area to the zone under the cursor instead. |
| 2. Base | Click; scroll | The base height. Starts at the area's current average; a key snaps it to sea level + 0.2 m. |
| 3. Roughness | Click; scroll | How high the rises go above the base, 0–3 m. With a modifier key held, scroll sets how broad they are: from small hummocks (about 4 m across) to long swells (about 30 m). |
| 4. Apply | Click | Applies the preview. Right-click at any step goes back a step; Escape cancels. |

- **Preview** before anything changes: a see-through mesh of the result. A key rerolls the pattern (a new random seed).
- **Shape:** two or three layers of smooth noise (Perlin) at different sizes, added to the base. Rises only, never
  dips below the base, so ground levelled to just above sea level stays dry.
- **Edges blend in:** over the outer quarter of the radius the result fades into the ground already there, so there's
  no rim or step.

## Beach

A natural slope from land down into the sea, made like MyDirtyHoe's ramp.

| Step | Mouse | What it sets |
|---|---|---|
| 1. Top | Click on land | Where the beach starts, at that ground's height. |
| 2. Bottom | Click out in the water; scroll | Where it ends, and its depth: 2 m below sea level to start with, scroll for 0.5–6 m. |
| 3. Width | Scroll | Beach width, 4–40 m. |
| 4. Apply | Click | Applies the preview. |

- **Profile:** not a straight ramp. A short, steeper drop from the land, a long gentle stretch either side of the
  waterline, then a steeper fall under the water. (Rough curve: smoothstep segments, tuned in game.)
- **Natural edge:** low noise along the length of the beach so the waterline wobbles instead of running straight,
  and the sides blend into the ground beside it.
- **Sand:** there's no sand paint in the game (the paints are dirt, cultivated, paved, reset, clear vegetation and
  deep snow). We believe the terrain shader already draws sand by height near the waterline. **Test this first**,
  with a plain slope made by the hoe. If it doesn't, sand needs its own investigation before the beach tool goes ahead.

## How an edit is applied

The same way vanilla terrain edits travel, so multiplayer works without anything new on the server.

1. The player's game finds every zone the area touches (`Heightmap.FindHeightmap(point, radius, list)`) and each
   zone's terrain record (`Heightmap.GetAndCreateTerrainCompiler()`), as `TerrainOp.Awake` does.
2. It sends each one a small custom RPC on the terrain record's network view: the tool, centre, size, heights,
   roughness and **random seed**. A few dozen bytes, however big the area.
3. The game that owns that terrain record works out the height at each 1 m point from those numbers. The same seed
   always gives the same shape, so every zone's piece lines up with its neighbours'.
4. For each point it does what vanilla `LevelTerrain` does with one target height: add (target − current height) to
   the stored height change, clear the smoothing change, mark the point modified, and clamp to the terrain limits.
   Then it saves the record and refreshes the terrain (`Save`, `Heightmap.Poke`, `ClutterSystem.ResetGrass`), as
   `DoOperation` does.
5. Everyone else receives the change like any other terrain edit. The sea fix picks it up with the refresh.

## Undo

Last edit only, per player. Before applying, the player's game copies the current stored height changes for the
points it will touch (it has them locally, synced like any terrain). Undo sends them back with the same RPC as
explicit values. Undo is for "that roll came out wrong"; it doesn't survive a relog.

## Settings

| Section | Setting | Default | Enforced / Personal |
|---|---|---|---|
| 4 - Wand | Max radius | 32 m | Enforced: big areas are big network messages for the zone owner to work through. |
| 4 - Wand | Show zone grid while held | On | Personal |
| (existing) 2 - Terrain limits | Max raise / Max dig | 200 m | Enforced: the tools clamp to these too. |

## Open questions and things to test

- Sand by height near the waterline (above). Decides whether the beach tool is just shape or also needs paint.
- The preview mesh: build it from the computed heights, or draw it as a dense grid of lines (cheaper, uglier).
- Rough-ground grass: `ClutterSystem.ResetGrass` regrows grass on the new surface; check it looks right on slopes.
- The wand's own icon, in the house frame.

## Order of work

1. Test sand by height (by hand, in game).
2. The wand: item, recipe, empty piece table, zone grid while held.
3. Natural ground with preview and undo: the most certain of the two tools.
4. Beach, once sand is understood.
