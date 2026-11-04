# QuanTerraforming: design and status

**Read this first when picking the mod up.** Last updated 4 Nov 2026, after Quandru's first in-game test.

## Where we are

| Part | State |
|---|---|
| Sea follows terrain edits | Built. Not yet confirmed in game (Quandru was playing client-only without it). |
| Heightmap limits (replaces Heightmap Unlimited JvL) | Built. Not yet tested in game. |
| Zone grid | Built and seen working in game (earlier toggle-key version). The 4 Nov test showed no grid with the wand in hand: the check used `GetCurrentWeapon`, which skips tools. Fixed to read the right-hand item; needs a retest. |
| Terraforming Wand item | Seen in game 4 Nov: crafting card, icon and shape all good. Collar slimmed (thumb clipped it). No tools on it yet. |
| Natural ground tool | Designed below. Not started. |
| Beach tool | Designed below. Waiting on the sand test. |

### V1 (1.0.0) release checklist

1. **Quandru:** import `dist\Quandru-QuanTerraforming-1.0.0.zip` into a separate Gale profile (with Jotunn) and check:
   - the wand appears in the inventory crafting panel for 2 Wood, with its icon, name and description;
   - in hand it looks like the mockup ([images/wand-mockup.png](images/wand-mockup.png)), held where the hammer is,
     and doesn't swing;
   - holding it shows the grid and the on-screen box; putting it away hides them;
   - dropped on the ground it lies flat (collider refit);
   - standing at a raised coast, the box's "ground" and "sea waves" figures agree, and the waves there are flat;
   - raising terrain past 8 m works.
2. **Quandru:** screenshots of the wand's crafting card and the wand in hand with the grid, for `Package/README.md`
   (there's a placeholder comment where they go; copy the QuanPieces README's image layout, images in
   `docs/images/`, linked by raw GitHub URL).
3. Fix whatever the test finds, then release per `docs/DEVELOPMENT.md` section 6: tag `v1.0.0`, upload the zip to
   Thunderstore (team Quandru).
4. **Server switch-over, in one push from the server's Gale profile:** add QuanTerraforming, remove Heightmap
   Unlimited JvL, leave `1 - Heightmap` at 200/200. Never let anyone play with neither mod: raised land shows cut
   back to 8 m and edits there save the cut.
5. Give the mod its own Gale dev profile ("QuanTerraforming", with Jotunn). `Local.props` currently borrows the
   QuanChunkLoader profile for build references only.

## Settings (as built)

| Section | Setting | Default | Enforced / Personal |
|---|---|---|---|
| 0 - General | Enabled | On | Enforced. Off = vanilla everything, including the 8 m limit (raised land shows cut back). |
| 1 - Heightmap | Upper limit | 200 m | Enforced. How far terrain can be raised above the generated ground. |
| 1 - Heightmap | Lower limit | 200 m | Enforced. How far terrain can be dug below it. |
| 2 - Sea | Waves follow terrain edits | On | Enforced: wave height drives physics on each object's owner. |
| 3 - Zone grid (personal) | Radius | 3 zones | Personal. |

Later, for the tools: `4 - Wand` with **Max radius** (32 m, enforced: big areas are big messages for the zone owner).

## The Terraforming Wand (as built)

| | |
|---|---|
| Name | Terraforming Wand |
| Description | "A wand for shaping the land, from QuanTerraforming. Hold it to see the zone grid ... which is what sets wave height there." (No mention of future tools: Quandru, 4 Nov.) |
| Two-handed | The tooltip says "Two-handed": the game says that of every tool (hammer, hoe), and equipping a tool does put away whatever's in both hands. Making it truly one-handed (torch in the other hand) would mean changing its item type: asked Quandru, 4 Nov. |
| Recipe | 2 Wood, no crafting station (inventory crafting panel, like the hammer). |
| Where | Not in QuanPieces' hammer tab: that tab is for placed pieces, and the wand is an item you hold. Agreed. |
| Behaviour (V1) | Not a build tool yet (no piece table), doesn't swing, no durability, weight 0.5. Holding it shows the zone grid. No key binding: holding the wand is the switch. |
| Model | A clone of the Hammer with its mesh replaced by a turned wand (`Wand/WandMesh.cs`), built in the hammer mesh's own space so it sits in the hand exactly where the hammer does. |

**The approved shape** (Quandru, 3 Nov), in hammer-mesh units along the shaft (butt -0.342 to top of shaft 1.228):

- **Grip:** the hammer's own wood (handle patch of the `weapons` atlas), straight at radius 0.066 from a small bevel
  at the butt to the collar. No flare.
- **Collar:** 0.13–0.20, radius 0.072, same wood slightly darker. (Was 0.08; the thumb clipped through it in game,
  4 Nov.)
- **Body:** `DarkWood_mat` (darkwood beams/roof), radius 0.0435 just after the collar (25% thinner than first drawn),
  nearly straight to half way (0.039 at 0.72), then most of the taper in the second half to a blunt tip at 1.228:
  the full length of the hammer's shaft, through where the head was.

`WandMesh.Profile` and `tools/render_wand.py` hold the same numbers; change both together. `tools/README.md` explains
how the game's assets were read.

## Shaping tools (next)

The tools we have (vanilla hoe, MyDirtyHoe, PlanBuild) only make flat levels and clean straight slopes. These shape
gently uneven ground and natural beaches, as a base for planting and building. They live in the wand's own build
menu: the wand gets a piece table with one entry per tool (**Natural ground**, **Beach**, later **Undo**), and each
entry is a mode; the wand then takes over clicks and the scroll wheel, the way MyDirtyHoe's ramp does.

### Natural ground

| Step | Mouse | What it sets |
|---|---|---|
| 1. Area | Aim; scroll | Radius, 4–32 m. A key snaps the area to the zone under the cursor instead. |
| 2. Base | Click; scroll | The base height. Starts at the area's current average; a key snaps it to sea level + 0.2 m. |
| 3. Roughness | Click; scroll | How high the rises go above the base, 0–3 m. With a modifier key held, scroll sets how broad they are: from small hummocks (about 4 m across) to long swells (about 30 m). |
| 4. Apply | Click | Applies the preview. Right-click goes back a step; Escape cancels. |

- **Preview** before anything changes: a see-through mesh of the result. A key rerolls the pattern (new seed).
- **Shape:** two or three layers of smooth noise (Perlin) at different sizes, added to the base. Rises only, never
  dips below the base, so ground levelled just above sea level stays dry. (With the sea fix, flat water there too.)
- **Edges blend in** over the outer quarter of the radius, so there's no rim or step.

### Beach

| Step | Mouse | What it sets |
|---|---|---|
| 1. Top | Click on land | Where the beach starts, at that ground's height. |
| 2. Bottom | Click in the water; scroll | Where it ends, and its depth: 2 m below sea level to start, scroll for 0.5–6 m. |
| 3. Width | Scroll | 4–40 m. |
| 4. Apply | Click | Applies the preview. |

- **Profile:** not a straight ramp: a short steeper drop from the land, a long gentle stretch either side of the
  waterline, then a steeper fall under the water (smoothstep segments, tuned in game).
- **Natural edge:** low noise along the beach so the waterline wobbles; the sides blend into the ground beside it.
- **Sand:** there's no sand paint (the paints are dirt, cultivated, paved, reset, clear vegetation, deep snow). We
  believe the terrain shader draws sand by height near the waterline. **Test first**, with a plain hoe slope into the
  sea. If it doesn't, sand needs its own investigation before the beach tool goes ahead. Quandru also floated a
  circle-shaped version (half on land, half in sea); two clicks give clearer control, so that's the plan.

### How an edit is applied (multiplayer)

The same way vanilla terrain edits travel, so nothing new is needed on the server.

1. The player's game finds every zone the area touches (`Heightmap.FindHeightmap(point, radius, list)`) and each
   zone's terrain record (`Heightmap.GetAndCreateTerrainCompiler()`), as `TerrainOp.Awake` does.
2. It sends each one a small custom RPC on the terrain record's network view: tool, centre, size, heights,
   roughness and **random seed**. A few dozen bytes, however big the area.
3. The game that owns that terrain record works out the height at each 1 m point. The same seed gives the same
   shape, so every zone's piece lines up with its neighbours'.
4. For each point it does what vanilla `LevelTerrain` does with a single target height: add (target − current
   height) to `m_levelDelta`, fold in and clear `m_smoothDelta`, set `m_modifiedHeight`, clamp to the heightmap
   limits. Then `Save`, `Heightmap.Poke`, `ClutterSystem.ResetGrass`, as `DoOperation` does.
5. Everyone else receives it like any terrain edit. The sea fix picks it up with the refresh.

### Undo

Last edit only, per player. Before applying, copy the current stored height changes for the points to be touched
(the player has them locally); undo sends them back with the same RPC as explicit values. Doesn't survive a relog.

### Open questions

- Sand by height near the waterline (above).
- Preview: a mesh of the computed heights, or a dense grid of lines (cheaper, uglier).
- Grass regrowth on slopes after `ClutterSystem.ResetGrass`.
- Tool icons for the wand's menu, in the house frame.

### Order of work

1. V1 release (checklist above).
2. Test sand by height (by hand, in game).
3. Give the wand a piece table and mode handling.
4. Natural ground, with preview and undo.
5. Beach, once sand is understood.

## Background: how we got here

- Waves were washing over land Quandru had raised out of the sea in a Meadows zone. Biome is irrelevant: waves scale
  with sea depth at the zone's four corners. A first overlay (QuanZoneGrid) showed the corners; its second reading
  (the sea's own copy) exposed that vanilla never re-reads them after terrain edits load. Hence the sea fix, and
  the rename to QuanTerraforming.
- The "waves on a pond" in one screenshot was a red herring: MyDirtyHoe's Large Pond was only the selected tool.
- F7 clashed with Magic Mike's Mod Settings; the key was later dropped entirely in favour of holding the wand.
