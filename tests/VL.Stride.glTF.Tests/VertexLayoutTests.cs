using System.Runtime.InteropServices;
using NUnit.Framework;
using Stride.Core.Mathematics;
using VL.Stride.glTF.Core;

namespace VL.Stride.glTF.Tests;

[TestFixture]
public class VertexLayoutTests
{
    [Test]
    public void Create_WithNineTexCoords()
    {
        var declaration = VertexLayout.Create(hasTangent: false, texCoordCount: 9, hasColor: false);

        var semantics = declaration.VertexElements.Select(e => e.SemanticAsText).ToArray();
        Assert.That(semantics, Is.EqualTo(new[]
        {
            "POSITION", "NORMAL",
            "TEXCOORD", "TEXCOORD1", "TEXCOORD2", "TEXCOORD3", "TEXCOORD4",
            "TEXCOORD5", "TEXCOORD6", "TEXCOORD7", "TEXCOORD8",
        }));
        Assert.That(declaration.VertexElements.Length, Is.EqualTo(11));
        Assert.That(declaration.VertexStride, Is.EqualTo(96));
    }

    [Test]
    public void Create_WithMaxTexCoords()
    {
        var declaration = VertexLayout.Create(hasTangent: false, texCoordCount: VertexLayout.MaxTexCoordCount, hasColor: false);

        Assert.That(declaration.VertexElements.Length, Is.EqualTo(12));
    }

    [Test]
    public void Create_WithTooManyTexCoords_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => VertexLayout.Create(hasTangent: false, texCoordCount: VertexLayout.MaxTexCoordCount + 1, hasColor: false));
    }

    [Test]
    public void Create_WithNegativeTexCoordCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => VertexLayout.Create(hasTangent: false, texCoordCount: -1, hasColor: false));
    }

    [Test]
    public void Create_WithTangentAndColor()
    {
        var declaration = VertexLayout.Create(hasTangent: true, texCoordCount: 2, hasColor: true);

        var semantics = declaration.VertexElements.Select(e => e.SemanticAsText).ToArray();
        Assert.That(semantics, Is.EqualTo(new[] { "POSITION", "NORMAL", "TEXCOORD", "TEXCOORD1", "COLOR", "TANGENT" }));
        Assert.That(declaration.VertexStride, Is.EqualTo(72));
    }

    [Test]
    public void Interleave_RoundTrip()
    {
        const int count = 3;
        var declaration = VertexLayout.Create(hasTangent: true, texCoordCount: 2, hasColor: true);

        var positions = new[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9) };
        var normals = new[] { new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1) };
        var tangents = new[] { new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, -1), new Vector4(0, 0, 1, 1) };
        var colors = new[] { new Vector4(0.1f, 0.2f, 0.3f, 1), new Vector4(0.4f, 0.5f, 0.6f, 1), new Vector4(0.7f, 0.8f, 0.9f, 1) };
        var texCoords = new List<Vector2[]>
        {
            new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1) },
            new[] { new Vector2(0.5f, 0.5f), new Vector2(0.25f, 0.75f), new Vector2(0, 1) },
        };

        var vertices = VertexLayout.Interleave(declaration, positions, normals, tangents, texCoords, colors);

        var stride = declaration.VertexStride;
        Assert.That(vertices.Length, Is.EqualTo(stride * count));

        foreach (var item in declaration.EnumerateWithOffsets())
        {
            var element = item.VertexElement;
            for (var i = 0; i < count; i++)
            {
                var data = vertices.AsSpan(i * stride + item.Offset);
                switch (element.SemanticAsText)
                {
                    case "POSITION":
                        Assert.That(MemoryMarshal.Read<Vector3>(data), Is.EqualTo(positions[i]));
                        break;
                    case "NORMAL":
                        Assert.That(MemoryMarshal.Read<Vector3>(data), Is.EqualTo(normals[i]));
                        break;
                    case "TEXCOORD":
                        Assert.That(MemoryMarshal.Read<Vector2>(data), Is.EqualTo(texCoords[0][i]));
                        break;
                    case "TEXCOORD1":
                        Assert.That(MemoryMarshal.Read<Vector2>(data), Is.EqualTo(texCoords[1][i]));
                        break;
                    case "COLOR":
                        Assert.That(MemoryMarshal.Read<Vector4>(data), Is.EqualTo(colors[i]));
                        break;
                    case "TANGENT":
                        Assert.That(MemoryMarshal.Read<Vector4>(data), Is.EqualTo(tangents[i]));
                        break;
                    default:
                        Assert.Fail($"Unexpected element {element.SemanticAsText}.");
                        break;
                }
            }
        }
    }

    [Test]
    public void Interleave_WithMissingTangents_Throws()
    {
        var declaration = VertexLayout.Create(hasTangent: true, texCoordCount: 1, hasColor: false);

        var positions = new[] { new Vector3(1, 2, 3) };
        var normals = new[] { new Vector3(0, 1, 0) };
        var texCoords = new List<Vector2[]> { new[] { new Vector2(0, 0) } };

        Assert.Throws<ArgumentException>(
            () => VertexLayout.Interleave(declaration, positions, normals, ReadOnlySpan<Vector4>.Empty, texCoords, ReadOnlySpan<Vector4>.Empty));
    }
}
