import os, UnityPy

root = r"D:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles"
here = os.path.dirname(os.path.abspath(__file__))
env = UnityPy.load(os.path.join(root, "c4210710"))
mats = {}
for o in env.objects:
    if o.type.name == "Material":
        m = o.read()
        n = m.m_Name
        if n == "weapons1" or "darkwood" in n.lower():
            mats[n] = m
for n, m in mats.items():
    texs = []
    for entry in m.m_SavedProperties.m_TexEnvs:
        key, env_ = entry if isinstance(entry, tuple) else (entry[0], entry[1])
        kname = key if isinstance(key, str) else getattr(key, "name", str(key))
        tex = env_.m_Texture
        try:
            t = tex.read()
            texs.append((kname, t.m_Name, t.m_Width, t.m_Height))
            if kname == "_MainTex":
                t.image.save(os.path.join(here, f"tex_{n}.png"))
        except Exception:
            pass
    print(n, texs)
