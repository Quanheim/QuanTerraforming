"""Wand card for the README, cut from Quandru's in-game crafting screenshot (9.png): the top of the card (icon,
title, description) joined to the requirement row, with the empty middle and the Craft button removed."""
import sys
from PIL import Image

src = Image.open(sys.argv[1]).convert("RGB")
out = sys.argv[2]

top = src.crop((0, 0, src.width, 200))      # icon, title, description
row = src.crop((0, 426, src.width, 514))     # requirement tiles (2 Wood)
blend = 14
card = Image.new("RGB", (src.width, top.height + row.height - blend))
card.paste(top, (0, 0))
card.paste(row, (0, top.height - blend))
# Soften the join: cross-fade the overlapping strip.
for i in range(blend):
    a = i / (blend - 1)
    y = top.height - blend + i
    t = top.crop((0, top.height - blend + i, src.width, top.height - blend + i + 1))
    r = row.crop((0, i, src.width, i + 1))
    card.paste(Image.blend(t, r, a), (0, y))
card.save(out)
print(out, card.size)
