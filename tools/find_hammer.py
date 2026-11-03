import os, sys, UnityPy

root = r"D:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles"
files = sorted((os.path.join(root, f) for f in os.listdir(root)), key=os.path.getsize)
limit = int(sys.argv[1]) if len(sys.argv) > 1 else 800 * 1024 * 1024
for path in files:
    if os.path.getsize(path) > limit:
        continue
    try:
        env = UnityPy.load(path)
    except Exception as e:
        continue
    for obj in env.objects:
        if obj.type.name != "GameObject":
            continue
        try:
            name = obj.peek_name() if hasattr(obj, "peek_name") else obj.read().m_Name
        except Exception:
            continue
        if name in ("Hammer", "Hoe"):
            print(os.path.basename(path), os.path.getsize(path) // 1024, "KB", name, obj.path_id, flush=True)
