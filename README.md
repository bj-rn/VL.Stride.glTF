# VL.Stride.glTF

This library loads glTF 2.0 files into VL.Stride and keeps every UV set, up to 10 sets. The built in FileModel and ModelReader nodes import through Assimp. Assimp drops every UV set after the 8th. glTF exporters, for example Blender, write all UV maps as TEXCOORD_n attributes. Stride shaders read TEXCOORD0 to TEXCOORD9.

## Requirements
- vvvv gamma 8.0, preview 2026.8.0-0123 or later
- Stride 4.3.0.2507
- .NET 10 SDK, for building from source

## Getting started
Install the package via command line:

    nuget install VL.Stride.glTF -pre

See the [guide on managing NuGet packages](https://thegraybook.vvvv.org/reference/hde/managing-nugets.html) for details.

Help patches are found via the Help Browser.

## Status
Work in progress. The nodes are added in the next steps.

## License
MIT
