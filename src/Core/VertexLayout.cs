using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace VL.Stride.glTF.Core;

/// <summary>Builds the vertex declaration and the interleaved vertex buffer of a glTF primitive.</summary>
public static class VertexLayout
{
    // Stride shaders declare TEXCOORD0 to TEXCOORD9.
    public const int MaxTexCoordCount = 10;

    /// <summary>Creates a vertex declaration in the element order of the Stride importer.</summary>
    /// <param name="hasTangent">Set to true to add a TANGENT element.</param>
    /// <param name="texCoordCount">The number of TEXCOORD elements. The value is 0 to <see cref="MaxTexCoordCount"/>.</param>
    /// <param name="hasColor">Set to true to add a COLOR element.</param>
    public static VertexDeclaration Create(bool hasTangent, int texCoordCount, bool hasColor)
    {
        if (texCoordCount < 0 || texCoordCount > MaxTexCoordCount)
            throw new ArgumentOutOfRangeException(nameof(texCoordCount), texCoordCount, $"The number of texture coordinate sets must be 0 to {MaxTexCoordCount}.");

        var elements = new List<VertexElement>(2 + texCoordCount + 2)
        {
            VertexElement.Position<Vector3>(),
            VertexElement.Normal<Vector3>(),
        };

        for (var i = 0; i < texCoordCount; i++)
            elements.Add(VertexElement.TextureCoordinate<Vector2>(i));

        if (hasColor)
            elements.Add(VertexElement.Color<Vector4>());

        if (hasTangent)
            elements.Add(VertexElement.Tangent<Vector4>());

        return new VertexDeclaration(elements.ToArray());
    }

    /// <summary>Writes the source data into one interleaved vertex buffer.</summary>
    /// <remarks>The declaration selects the data. Source data that the declaration does not contain is ignored.</remarks>
    /// <param name="declaration">The vertex declaration.</param>
    /// <param name="positions">The positions. The length gives the vertex count.</param>
    /// <param name="normals">The normals.</param>
    /// <param name="tangents">The tangents.</param>
    /// <param name="texCoords">One array per texture coordinate set.</param>
    /// <param name="colors">The vertex colors.</param>
    /// <exception cref="ArgumentException">The source data of an element of the declaration is missing.</exception>
    public static byte[] Interleave(
        VertexDeclaration declaration,
        ReadOnlySpan<Vector3> positions,
        ReadOnlySpan<Vector3> normals,
        ReadOnlySpan<Vector4> tangents,
        IReadOnlyList<Vector2[]> texCoords,
        ReadOnlySpan<Vector4> colors)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        var stride = declaration.VertexStride;
        var count = positions.Length;
        var vertices = new byte[stride * count];
        var target = vertices.AsSpan();

        foreach (var item in declaration.EnumerateWithOffsets())
        {
            var element = item.VertexElement;
            switch (element.SemanticName)
            {
                case "POSITION":
                    Write(target, stride, item.Offset, positions, count, element.SemanticAsText);
                    break;
                case "NORMAL":
                    Write(target, stride, item.Offset, normals, count, element.SemanticAsText);
                    break;
                case "TANGENT":
                    Write(target, stride, item.Offset, tangents, count, element.SemanticAsText);
                    break;
                case "COLOR":
                    Write(target, stride, item.Offset, colors, count, element.SemanticAsText);
                    break;
                case "TEXCOORD":
                    var set = element.SemanticIndex;
                    if (texCoords is null || set >= texCoords.Count || texCoords[set] is null)
                        throw new ArgumentException($"The source data for {element.SemanticAsText} is missing.", nameof(texCoords));
                    Write(target, stride, item.Offset, texCoords[set], count, element.SemanticAsText);
                    break;
            }
        }

        return vertices;
    }

    private static void Write<T>(Span<byte> target, int stride, int offset, ReadOnlySpan<T> source, int count, string semantic)
        where T : unmanaged
    {
        if (source.Length < count)
            throw new ArgumentException($"The source data for {semantic} is missing.");

        for (var i = 0; i < count; i++)
            MemoryMarshal.Write(target[(i * stride + offset)..], in source[i]);
    }
}
