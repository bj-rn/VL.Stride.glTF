# VL.Stride.glTF

This library loads glTF 2.0 files into VL.Stride and keeps every UV set, up to 10 sets. The built in FileModel and ModelReader nodes import through Assimp. Assimp drops every UV set after the 8th. glTF exporters, for example Blender, write all UV maps as `TEXCOORD_n` attributes. Stride shaders declare `TexCoord` (`TEXCOORD0`) to `TexCoord9` (`TEXCOORD9`), so Stride can use 10 sets.

## Requirements

- vvvv gamma 8.0, preview 2026.8.0-0123 or later
- .NET 10 SDK, for building from source

## Install

Install via the vvvv package manager (`CTRL + F3`)


See the [Packman](https://thegraybook.vvvv.org/reference/hde/packman.html) for details.

The help patch `HowTo Load a glTF with multiple UV sets` is in the Help Browser.

## Nodes

The category `Stride.Models` has two nodes:

- `GltfReader`: loads the file on the main thread. This blocks the frame while it loads.
- `GltfReader (Async)`: loads the file on a background thread. It outputs the last loaded model while a new one loads, and has the extra output `Is Loading`.

Shared pins:

| Pin | Direction | Description |
| --- | --- | --- |
| Path | Input | The .gltf or .glb file. |
| Import Scale | Input | Scale applied to the positions. Default is 1. |
| Pivot Position | Input | World space point that becomes the origin. Default is 0,0,0. |
| Merge Meshes | Input | Combines primitives with the same material and vertex layout into one mesh. Default is true. |
| Reload | Input | Loads the file again. |
| Model | Output | The loaded Stride model. |
| Texcoord Count | Output | The number of UV sets found in the file. |

`GltfReader (Async)` has one more output: `Is Loading`. It is true while a file loads in the background.

Reload on GltfReader loads the file on every frame where the pin is true, like ModelReader. Reload on GltfReader (Async) loads once per rising edge. Use a Bang IOBox for both.

## UV sets in Stride

Use a UV set of a loaded model like this:

- Material texture nodes, for example TextureMaterial, have a `Texcoord Index` pin. The values are `Texcoord0` to `Texcoord9`.
- In a vertex declaration, the sets are the elements `TEXCOORD`, `TEXCOORD1` to `TEXCOORD9`.
- In SDSL, the streams are `TexCoord`, `TexCoord1` to `TexCoord9`.

## Conventions

- Node transforms are baked into the vertices. The model has no skeleton.
- Triangle winding is reversed to clockwise, because Stride uses clockwise front faces. A mirrored node, with a negative determinant, keeps its winding, because glTF already stores it as clockwise for that node.
- UVs pass through unchanged. glTF and Direct3D both use a top left origin.
- Positions are not axis converted. Both systems are right handed and Y up.
- Import Scale and Pivot Position apply as `p' = (world * p - pivot) * scale`. This matches the Stride importer.
- Normals are transformed with the inverse transpose of the world matrix, then normalized.
- Tangents are transformed the same way, with the inverse transpose, then normalized. This matches the Stride importer. The handedness in W is negated for a mirrored node.
- A primitive without normals gets area weighted vertex normals.

Vertex layout: POSITION, NORMAL, TEXCOORD0 to TEXCOORDn, COLOR (when COLOR_0 exists in the file), TANGENT (float4, when TANGENT exists in the file). All elements are float. The index buffer is 16 bit when the vertex count fits, otherwise 32 bit.

Merge Meshes:

- True combines primitives that share the material index and the vertex declaration into one Mesh.
- False gives one Mesh per glTF primitive.
- Each Mesh keeps its `MaterialIndex` and `Name`.

## Limitations

- Materials are not imported. `Model.Materials` stays empty. Assign materials in vvvv.
- Skins, morph targets, animations, cameras and lights are ignored.
- Point and line primitives are skipped.
- A file with more than 10 UV sets loads only the first 10. `Texcoord Count` reports the number found in the file.
- The loader validates strictly, the SharpGLTF default. A malformed file throws, and the node turns red.

## Testing

Run the unit tests:

    dotnet test src/tests/VL.Stride.glTF.Tests/VL.Stride.glTF.Tests.csproj

Most tests use synthetic glTF files written with SharpGLTF.Toolkit. A few tests use the real Blender export in `src/tests/assets`.

The fixture `VlDocumentTests` compiles the .vl documents against a vvvv installation, with VL.TestFramework. It is marked Explicit, because the vvvv 8.0 preview test host cannot resolve the VL.Skia version inside the shipped VL.Stride.Windows document. Run it on its own:

    dotnet test src/tests/VL.Stride.glTF.Tests/VL.Stride.glTF.Tests.csproj --filter FullyQualifiedName~VlDocumentTests

Set the environment variable `VVVV_DIR` to the vvvv folder when it differs from `D:\_vvvv\vvvv_gamma_8.0-0123`.

## Development

Build the library:

    dotnet build src/VL.Stride.glTF.csproj -c Release

Only Release builds write to `lib/net10.0`. This folder is committed, because the forwarding document `VL.Stride.glTF.vl` references the dll in it.

`NuGet.config` adds the vvvv feed for the VL packages.

Start vvvv with `--package-repositories "D:\_Dev\_vl-libs"` to use this repository as a local source. Use the parent folder of the repository as the path.

## Credits

- [SharpGLTF](https://github.com/vpenades/SharpGLTF) by Vicente Penades (MIT).
- `BackgroundComputation` and the game service helper come from [VL.Stride.Text3d](https://github.com/bj-rn/VL.Stride.Text3d) (MIT).

## Sponsoring

Initial development was sponsored by [Refik Anadol Studio](https://refikanadolstudio.com/).

## License

MIT
