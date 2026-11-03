# QuanTerraforming

Terraform coasts, canals and harbours without the sea ignoring your work.

## Waves follow your terrain

In vanilla Valheim the sea's waves never notice terrain edits. Each 64 × 64 m zone scales its waves by the sea
depth at its **four corners**, but it reads those depths once, when the zone loads, before your terrain edits are
applied. So waves always follow the seabed as the world was generated. Land you've raised out of the sea still gets
waves washing over it, as if it were open water.

With this mod, the sea re-reads the corners whenever the terrain there changes: when the zone loads, when your edits
arrive, and after every new edit. Raise all four corners of a zone to sea level or above and its water goes flat,
even in a storm. Leave one corner deep and waves blend in from that side. Terrain in the middle of a zone doesn't
count; only the corners do.

## Terrain limits

Vanilla only lets you raise or dig terrain 8 m from where the world generated it. This lifts that to the server's
settings (200 m each way by default), replacing Heightmap Unlimited JvL. If that mod is installed too, this part
stands aside and lets it carry on.

Your raised land is stored as those larger height changes. Without either mod it shows cut back to 8 m, and an edit
there saves the cut, so swap the two mods over in one go, with the same limits.

## Zone grid

Press **Left Alt + G** to show or hide it (hidden by default). The key can be changed in the settings.

- **White lines** along zone borders around you.
- **A post at every zone corner**, coloured by its depth:
  - green: at or above sea level, so no waves from that corner
  - yellow to red: deeper; red means 10 m or more (full waves)
  - grey: that zone isn't loaded yet
- **A box on screen** listing the four corners of the zone you're standing in: the ground depth after your edits,
  and the wave strength the zone's sea is actually using. With the fix on, the two agree.

Tip: a canal down the middle of a zone never touches a corner. One along a zone border hits a corner every 64 m, and
each corner is shared by the zones on both sides.

## Settings

- `1 - Sea`: **Waves follow terrain edits** (on). Synced from the server; admins only. Off is vanilla.
- `2 - Terrain limits`: **Max raise** and **Max dig** (200 m each). Synced from the server; admins only.
- `3 - Zone grid (personal)`: show on start, the toggle key, and how many zones to draw around you.

## Multiplayer

Install on the server and every client. Wave height drives how boats and floating objects move, and each client
works that out for the objects it controls, so everyone needs the same behaviour.
