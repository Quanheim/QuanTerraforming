"""Inventory icon for the wand: same profile and textures as the mockup, diagonal, transparent background."""
import math, os, sys
import numpy as np
from PIL import Image

here = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, here)
weap = np.asarray(Image.open(os.path.join(here, "tex_weapons1.png")).convert("RGB"), dtype=float)
dark = np.asarray(Image.open(os.path.join(here, "tex_DarkWood_mat.png")).convert("RGB"), dtype=float)

PROFILE = [
    (-0.342, 0.048, "grip"), (-0.326, 0.066, "grip"), (0.120, 0.066, "grip"),
    (0.130, 0.080, "collar"), (0.200, 0.080, "collar"),
    (0.210, 0.0435, "body"), (0.720, 0.039, "body"),
    (0.960, 0.030, "body"), (1.150, 0.018, "body"),
    (1.205, 0.012, "body"), (1.228, 0.0, "body"),
]
GRIP_UV = (0.807, 0.834, 0.627, 0.734)
Y0, Y1 = -0.342, 1.228
THICK = 1.35                     # exaggerate girth a little so it reads at icon size
S = 1000                         # px per unit for the long render
L = int((Y1 - Y0) * S) + 20
Hh = 340
img = np.zeros((Hh, L, 4))

def radius_at(y):
    for (y0, r0, p0), (y1, r1, p1) in zip(PROFILE, PROFILE[1:]):
        if y0 <= y <= y1:
            t = 0 if y1 == y0 else (y - y0) / (y1 - y0)
            return r0 + (r1 - r0) * t, (p1 if t > 0.5 and p0 != p1 else p0)
    return None, None

light = np.array([-0.35, -0.75, 0.55]); light /= np.linalg.norm(light)
for x in range(L):
    y = (x - 10) / S + Y0
    r, part = radius_at(y)
    if not r: continue
    rp = r * S * THICK
    for dy in range(-int(rp), int(rp) + 1):
        s = max(-1.0, min(1.0, dy / rp)); th = math.asin(s); a = th / math.pi + 0.5
        if part in ("grip", "collar"):
            u0, u1, v0, v1 = GRIP_UV
            u = u0 + (u1 - u0) * a; v = v0 + (v1 - v0) * (y - Y0) / (0.21 - Y0); tex = weap
        else:
            u = 0.03 + 0.38 * a; v = ((y - 0.21) / 0.54) * 0.5; tex = dark
        h, w, _ = tex.shape
        c = tex[int((1 - (v % 1.0)) * (h - 1)), int(u * (w - 1)) % w]
        if part == "collar": c = c * 0.8
        if part == "body": c = c * 1.35   # lift the dark wood so it reads on the dark inventory slot
        d = max(0.0, float(np.array([0.0, s, math.cos(th)]) @ light))
        img[Hh // 2 + dy, x, :3] = np.clip(c * (0.45 + 0.8 * d), 0, 255)
        img[Hh // 2 + dy, x, 3] = 255

im = Image.fromarray(img.astype(np.uint8), "RGBA")
im = im.rotate(40, resample=Image.BICUBIC, expand=True)   # grip bottom-left, tip top-right
im = im.crop(im.getbbox())
side = max(im.size)
canvas = Image.new("RGBA", (side + 40, side + 40), (0, 0, 0, 0))
canvas.paste(im, ((canvas.width - im.width) // 2, (canvas.height - im.height) // 2), im)
canvas = canvas.resize((128, 128), Image.LANCZOS)
out = os.path.join(here, "wand_icon.png")
canvas.save(out); print(out)
