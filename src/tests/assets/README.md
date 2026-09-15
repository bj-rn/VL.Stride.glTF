# Test assets

This folder holds a real Blender export. The tests and the manual checks use it.

## Files

- `TestCube.fbx`: the Blender export in FBX format.
- `TestCube.gltf`: the same export in glTF format.
- `TestCube.bin`: the binary buffer of `TestCube.gltf`.
- `TestCube.glb`: the same export in glb format.

## Rules

- Keep the glTF files in sync with the fbx file. Export all files from the same Blender scene.
- Do not add a file above 10 MB to this folder.
