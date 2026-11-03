import os, UnityPy

root = r"D:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles"
import sys
env = UnityPy.load(os.path.join(root, sys.argv[1]))
gos = [o for o in env.objects if o.type.name == "GameObject" and o.read().m_Name == sys.argv[2]]

def comp_list(g):
    out = []
    for c in g.m_Component:
        ptr = c.component if hasattr(c, "component") else c[1]
        try:
            out.append(ptr.deref())
        except Exception:
            out.append(None)
    return out

def describe(gobj, depth):
    g = gobj.read()
    comps = comp_list(g)
    names = []
    tr = None
    for c in comps:
        if c is None:
            names.append("?"); continue
        t = c.type.name
        if t == "MonoBehaviour":
            try:
                mb = c.read()
                names.append("MB:" + mb.m_Script.read().m_Name)
            except Exception:
                names.append("MB")
        else:
            names.append(t)
        if t == "Transform":
            tr = c.read()
    print("  " * depth + f"{g.m_Name}  [{', '.join(names)}]")
    if tr is not None:
        p, r, s = tr.m_LocalPosition, tr.m_LocalRotation, tr.m_LocalScale
        print("  " * depth + f"   pos=({p.x:.3f},{p.y:.3f},{p.z:.3f}) rot=({r.x:.3f},{r.y:.3f},{r.z:.3f},{r.w:.3f}) scale=({s.x:.3f},{s.y:.3f},{s.z:.3f})")
    for c in comps:
        if c is None: continue
        if c.type.name == "MeshFilter":
            mf = c.read()
            try:
                mesh = mf.m_Mesh.read()
                b = mesh.m_LocalAABB
                print("  " * depth + f"   mesh {mesh.m_Name} readable={getattr(mesh,'m_IsReadable',None)} verts={mesh.m_VertexData.m_VertexCount} center=({b.m_Center.x:.3f},{b.m_Center.y:.3f},{b.m_Center.z:.3f}) extent=({b.m_Extent.x:.3f},{b.m_Extent.y:.3f},{b.m_Extent.z:.3f})")
            except Exception as e:
                print("  " * depth + f"   mesh (unreadable here: {e})")
        if c.type.name in ("MeshRenderer", "SkinnedMeshRenderer"):
            mr = c.read()
            mats = []
            for m in mr.m_Materials:
                try:
                    mats.append(m.read().m_Name)
                except Exception:
                    mats.append("?")
            print("  " * depth + f"   materials {mats}")
    if tr is not None:
        for ch in tr.m_Children:
            try:
                child_tr = ch.read()
                describe(child_tr.m_GameObject.deref(), depth + 1)
            except Exception as e:
                print("  " * (depth + 1) + f"(child unreadable: {e})")

for go in gos:
    print("=====", go.path_id)
    describe(go, 0)
