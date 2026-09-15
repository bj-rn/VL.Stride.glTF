using Stride.Core.Mathematics;
using Stride.Graphics;

namespace VL.Stride.glTF.Core;

/// <summary>Holds the CPU side mesh data of one glTF primitive or of one merged group.</summary>
public sealed class GltfGeometry
{
    public required string Name { get; init; }

    public required int MaterialIndex { get; init; }

    public required VertexDeclaration Declaration { get; init; }

    /// <summary>The interleaved vertex data.</summary>
    public required byte[] Vertices { get; init; }

    public required int VertexCount { get; init; }

    /// <summary>The indices of the triangle list.</summary>
    public required int[] Indices { get; init; }

    /// <summary>The number of TEXCOORD_n keys in the file, before the limit of 10 is applied.</summary>
    public required int TexCoordCountInFile { get; init; }

    public required BoundingBox BoundingBox { get; init; }

    public required BoundingSphere BoundingSphere { get; init; }
}
