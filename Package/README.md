# QuanZoneGrid

A builder's overlay that shows Valheim's zone (chunk) borders and how deep the sea is at every zone corner.

## Why

Wave height has nothing to do with the biome. The game scales waves in each 64 × 64 m zone by the sea depth at
that zone's **four corners**, blended across the zone. Raise all four corners to sea level or above and the zone's
water goes flat, even in a storm. Leave one corner deep and waves blend back in from that side. Terrain in the middle
of the zone makes no difference.

This shows you where the corners are and what depth the game reads at each one, so you can terraform a coast, canal
or harbour without waves washing over it.

## What you see

Press **Left Alt + G** to show or hide it (hidden by default). The key can be changed in the settings.

- **White lines** along zone borders around you.
- **A post at every zone corner**, coloured by the depth the waves use:
  - green: at or above sea level, so no waves from that corner
  - yellow to red: deeper; red means 10 m or more (full waves)
  - grey: that zone isn't loaded yet
- **A box on screen** listing the four corners of the zone you're standing in, with each depth and how much wave
  height it allows.

The colours follow your terrain edits within a second. The water itself only re-reads the corners when the zone
loads, so log out and back in (or travel away and back) to see the change in the sea.

Tip: a canal down the middle of a zone never touches a corner. One along a zone border hits a corner every 64 m, and
each corner is shared by the zones on both sides.

## Settings

All personal, in `1 - Display (personal)`: show on start, the toggle key, and how many zones to draw around you.

## Multiplayer

Client-only. Nothing is synced or saved, so install it just for yourself; the server and other players don't need it.
