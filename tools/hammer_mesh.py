import os, UnityPy

root = r"D:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles"
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "hammer.obj")
env = UnityPy.load(os.path.join(root, "c4210710"))
for o in env.objects:
    if o.type.name == "Mesh":
        m = o.read()
        if m.m_Name == "hammer_Cube.006":
            with open(out, "w") as f:
                f.write(m.export())
            print("wrote", out)
            break
