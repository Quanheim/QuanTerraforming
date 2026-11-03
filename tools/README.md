# tools

Python scripts used to design the Terraforming Wand from the game's own assets. Not part of the mod build.

They read Valheim's asset bundles (`valheim_Data\StreamingAssets\SoftRef\Bundles`, path hard-coded at the top of
each script) with [UnityPy](https://pypi.org/project/UnityPy/). Anything they extract (textures, meshes) is Iron
Gate's and is gitignored: never commit it.

```powershell
python -m venv tools\.venv
tools\.venv\Scripts\python -m pip install UnityPy numpy
tools\.venv\Scripts\python -I tools\textures.py      # -> tex_weapons1.png, tex_DarkWood_mat.png, ...
tools\.venv\Scripts\python -I tools\hammer_mesh.py   # -> hammer.obj (the vanilla hammer's mesh)
tools\.venv\Scripts\python -I tools\render_wand.py   # -> wand_mockup.png (hammer vs wand, same scale)
tools\.venv\Scripts\python -I tools\render_icon.py   # -> wand_icon.png: copy to Assets\ for the mod
```

| Script | What it does |
|---|---|
| `find_hammer.py` | Searches every bundle for the `Hammer` and `Hoe` GameObjects. The item prefabs are in `c4210710` (738 MB, slow to load); same-named objects elsewhere are UI. |
| `dump_hammer.py <bundle> <name>` | Prints a prefab's hierarchy, transforms, meshes and materials. |
| `hammer_mesh.py` | Exports `hammer_Cube.006` (the hammer's only mesh) as OBJ. |
| `analyse.py` | Profiles that mesh along its length and finds the handle's UV patch in the atlas. |
| `textures.py` | Exports `weapons1` and every darkwood material's main texture. |
| `render_wand.py` | Renders the approved mockup. **Its `PROFILE` must match `WandMesh.Profile`.** |
| `render_icon.py` | Renders the inventory icon from the same profile. |

What we learnt about the hammer (Valheim 1.0.21): `Hammer/attach/hammer` holds the mesh, rotated 90° about X and
scaled 0.3. The mesh runs along +Y from the butt (-0.342) to the top of the shaft (1.228); the head spans about 0.72
to 1.18. The handle's texture is the patch u 0.807–0.834, v 0.627–0.734 of the `weapons` atlas.
