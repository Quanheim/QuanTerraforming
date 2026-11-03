import os, collections
p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "hammer.obj")
V, T, F = [], [], []
for line in open(p):
    s = line.split()
    if not s: continue
    if s[0] == "v": V.append(tuple(map(float, s[1:4])))
    elif s[0] == "vt": T.append(tuple(map(float, s[1:3])))
    elif s[0] == "f": F.append([tuple(int(x) if x else 0 for x in c.split("/")) for c in s[1:]])
print(len(V), "verts", len(F), "faces")
ys = sorted(v[1] for v in V)
print("y range", ys[0], ys[-1])
bands = collections.defaultdict(list)
for v in V:
    bands[round(v[1], 2)].append(v)
for y in sorted(bands):
    vs = bands[y]
    xs = [v[0] for v in vs]; zs = [v[2] for v in vs]
    print(f"y={y:+.2f} n={len(vs):3d} x[{min(xs):+.3f},{max(xs):+.3f}] z[{min(zs):+.3f},{max(zs):+.3f}]")
# UVs used by faces whose verts are all below a y cut, vs above
def face_uvs(pred):
    us, vs_ = [], []
    for f in F:
        if all(pred(V[c[0]-1]) for c in f):
            for c in f:
                if len(c) > 1 and c[1]:
                    u, v = T[c[1]-1]; us.append(u); vs_.append(v)
    return (min(us), max(us), min(vs_), max(vs_)) if us else None
for cut in (0.3, 0.5, 0.7, 0.9):
    print("cut", cut, "below uv", face_uvs(lambda v: v[1] < cut), "above uv", face_uvs(lambda v: v[1] >= cut))
