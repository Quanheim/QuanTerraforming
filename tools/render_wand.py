"""Side view: the vanilla hammer (from its real mesh) above the proposed wand, same scale, real textures."""
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

here = os.path.dirname(os.path.abspath(__file__))
weap = np.asarray(Image.open(os.path.join(here, "tex_weapons1.png")).convert("RGB"), dtype=float)
dark = np.asarray(Image.open(os.path.join(here, "tex_DarkWood_mat.png")).convert("RGB"), dtype=float)

# Wand profile in hammer-mesh units: (position along the handle, radius, part). Same numbers the mod uses.
PROFILE = [
    (-0.342, 0.048, "grip"), (-0.326, 0.066, "grip"), (0.120, 0.066, "grip"),
    (0.130, 0.080, "collar"), (0.200, 0.080, "collar"),
    (0.210, 0.0435, "body"), (0.720, 0.039, "body"),         # first half: nearly straight
    (0.960, 0.030, "body"), (1.150, 0.018, "body"),          # last half: most of the taper
    (1.205, 0.012, "body"), (1.228, 0.0, "body"),            # blunt tip, at the end of the hammer's shaft
]
TIP = 1.228
GRIP_UV = (0.807, 0.834, 0.627, 0.734)   # hammer handle's area of the weapons atlas (u0,u1,v0,v1)

SCALE = 420                 # pixels per mesh unit
W, H = 900, 700
img = Image.new("RGB", (W, H), (28, 24, 22))
draw = ImageDraw.Draw(img)
X0 = 120                    # where y = -0.342 sits on screen

def sx(y): return X0 + (y + 0.342) * SCALE

def shade(col, nx, ny):
    light = np.array([-0.35, -0.75, 0.55]); light /= np.linalg.norm(light)
    n = np.array([0.0, ny, nx])
    d = max(0.0, float(n @ light))
    return col * (0.35 + 0.85 * d)

def radius_at(y):
    for (y0, r0, p0), (y1, r1, p1) in zip(PROFILE, PROFILE[1:]):
        if y0 <= y <= y1:
            t = 0 if y1 == y0 else (y - y0) / (y1 - y0)
            return r0 + (r1 - r0) * t, (p1 if t > 0.5 and p0 != p1 else p0)
    return None, None

def tex_sample(part, y, theta):
    a = (theta / math.pi) + 0.5          # 0..1 across the visible half
    if part in ("grip", "collar"):
        u0, u1, v0, v1 = GRIP_UV
        t = (y + 0.342) / (0.21 + 0.342)
        u = u0 + (u1 - u0) * a; v = v0 + (v1 - v0) * t
        tex = weap
    else:
        u = 0.03 + 0.38 * a                # dark, stained half of DarkWoodBeams
        v = ((y - 0.21) / 0.54) * 0.5
        tex = dark
    h, w, _ = tex.shape
    px = int(u * (w - 1)) % w; py = int((1 - (v % 1.0)) * (h - 1))
    c = tex[py, px]
    if part == "collar": c = c * 0.8
    return c

# --- wand ---
cy_w = 560
arr = np.asarray(img, dtype=float).copy()
for x in range(int(sx(-0.342)), int(sx(TIP)) + 1):
    y = (x - X0) / SCALE - 0.342
    r, part = radius_at(y)
    if not r: continue
    rp = r * SCALE
    for dy in range(-int(rp), int(rp) + 1):
        s = max(-1.0, min(1.0, dy / rp))
        theta = math.asin(s)
        col = tex_sample(part, y, theta)
        arr[cy_w + dy, x] = np.clip(shade(col, math.cos(theta), s), 0, 255)
img = Image.fromarray(arr.astype(np.uint8)); draw = ImageDraw.Draw(img)

# --- hammer from its mesh: side view (mesh y along screen x, mesh x up), painter's algorithm ---
V, T, F = [], [], []
for line in open(os.path.join(here, "hammer.obj")):
    s = line.split()
    if not s: continue
    if s[0] == "v": V.append(tuple(map(float, s[1:4])))
    elif s[0] == "vt": T.append(tuple(map(float, s[1:3])))
    elif s[0] == "f": F.append([tuple(int(x) if x else 0 for x in c.split("/")) for c in s[1:]])
cy_h = 230
faces = []
for f in F:
    pts = [V[c[0] - 1] for c in f]
    a, b, c3 = (np.array(p) for p in pts[:3])
    n = np.cross(b - a, c3 - a); nl = np.linalg.norm(n)
    if nl == 0: continue
    n /= nl
    depth = sum(p[2] for p in pts) / len(pts)
    uvs = [T[c[1] - 1] for c in f if len(c) > 1 and c[1]]
    u = sum(t[0] for t in uvs) / len(uvs); v = sum(t[1] for t in uvs) / len(uvs)
    col = weap[int((1 - v) * 255), int(u * 255)]
    faces.append((depth, pts, col, n))
faces.sort(key=lambda t: t[0])
for depth, pts, col, n in faces:
    light = np.array([0.35, 0.75, 0.55]); light /= np.linalg.norm(light)
    d = abs(float(n @ light))
    c = tuple(int(x) for x in np.clip(col * (0.35 + 0.8 * d), 0, 255))
    poly = [(sx(p[1]), cy_h - p[0] * SCALE) for p in pts]
    draw.polygon(poly, fill=c)

# --- labels and guides ---
try:
    font = ImageFont.truetype("segoeui.ttf", 20); small = ImageFont.truetype("segoeui.ttf", 16)
except OSError:
    font = small = ImageFont.load_default()
gold = (200, 140, 60); grey = (170, 160, 150)
for yv, label, ty in ((0.72, "half way", 455), (TIP, "end of the hammer's shaft", 455)):
    cut = sx(yv)
    for yy in range(70, H - 30, 12):
        draw.line([(cut, yy), (cut, yy + 6)], fill=gold, width=2)
    tw = draw.textlength(label, font=small)
    draw.text((min(cut - 60, W - tw - 10), ty), label, fill=gold, font=small)
draw.text((30, 20), "Vanilla hammer", fill=grey, font=font)
draw.text((30, 450), "Terraforming Wand (proposed)", fill=grey, font=font)
draw.text((sx(-0.30), cy_w + 55), "grip: the hammer's own wood", fill=grey, font=small)
draw.text((sx(0.45), cy_w + 55), "body: dark wood (darkwood beams/roof)", fill=grey, font=small)
draw.text((sx(0.13) - 20, cy_w - 70), "collar", fill=grey, font=small)
draw.line([(sx(0.165), cy_w - 48), (sx(0.165), cy_w - 36)], fill=grey, width=2)
draw.text((sx(TIP) - 120, cy_w - 70), "blunt tip", fill=grey, font=small)
draw.line([(sx(TIP) - 90, cy_w - 48), (sx(TIP) - 4, cy_w - 6)], fill=grey, width=2)
out = os.path.join(here, "wand_mockup.png")
img.save(out); print(out)
