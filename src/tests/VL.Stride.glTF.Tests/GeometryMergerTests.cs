using System.Runtime.InteropServices;
using NUnit.Framework;
using Stride.Core.Mathematics;
using VL.Stride.glTF.Core;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace VL.Stride.glTF.Tests;

[TestFixture]
public class GeometryMergerTests
{
    private string _directory = string.Empty;

    [SetUp]
    public void SetUp() => _directory = TestFiles.CreateDirectory();

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void MergesSameLayoutAndMaterial()
    {
        var input = new List<GltfGeometry>
        {
            LoadQuad("one.glb", 2, Matrix4x4.Identity),
            LoadQuad("two.glb", 2, Matrix4x4.CreateTranslation(2, 0, 0)),
        };

        var result = GeometryMerger.Merge(input);

        Assert.That(result, Has.Count.EqualTo(1));
        var merged = result[0];
        var stride = merged.Declaration.VertexStride;
        Assert.That(merged.VertexCount, Is.EqualTo(8));
        Assert.That(merged.Indices, Has.Length.EqualTo(12));
        Assert.That(merged.Indices[6..12], Is.EqualTo(input[1].Indices.Select(i => i + 4).ToArray()));
        Assert.That(merged.Vertices, Has.Length.EqualTo(8 * stride));
        Assert.That(Read<Vector3>(merged, "POSITION", 5), Is.EqualTo(Read<Vector3>(input[1], "POSITION", 1)));
        Assert.That(merged.BoundingBox.Maximum.X, Is.EqualTo(3));
    }

    [Test]
    public void KeepsDifferentTexCoordCountsApart()
    {
        var input = new List<GltfGeometry>
        {
            LoadQuad("one.glb", 2, Matrix4x4.Identity),
            LoadQuad("two.glb", 3, Matrix4x4.Identity),
        };

        var result = GeometryMerger.Merge(input);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].TexCoordCountInFile, Is.EqualTo(2));
        Assert.That(result[1].TexCoordCountInFile, Is.EqualTo(3));
    }

    [Test]
    public void KeepsDifferentMaterialsApart()
    {
        var input = new List<GltfGeometry>
        {
            LoadQuad("one.glb", 1, Matrix4x4.Identity, materialCount: 1),
            LoadQuad("two.glb", 1, Matrix4x4.Identity, materialCount: 2),
        };

        var result = GeometryMerger.Merge(input);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].MaterialIndex, Is.EqualTo(0));
        Assert.That(result[1].MaterialIndex, Is.EqualTo(1));
    }

    [Test]
    public void PassesSingleGeometryThrough()
    {
        var input = new List<GltfGeometry> { LoadQuad("one.glb", 1, Matrix4x4.Identity) };

        var result = GeometryMerger.Merge(input);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.SameAs(input[0]));
    }

    [Test]
    public void MergesInOrderOfFirstAppearance()
    {
        var input = new List<GltfGeometry>
        {
            LoadQuad("one.glb", 1, Matrix4x4.Identity, materialCount: 1),
            LoadQuad("two.glb", 1, Matrix4x4.Identity, materialCount: 2),
            LoadQuad("three.glb", 1, Matrix4x4.Identity, materialCount: 1),
        };

        var result = GeometryMerger.Merge(input);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].MaterialIndex, Is.EqualTo(0));
        Assert.That(result[0].VertexCount, Is.EqualTo(8));
        Assert.That(result[1].MaterialIndex, Is.EqualTo(1));
        Assert.That(result[1].VertexCount, Is.EqualTo(4));
    }

    private GltfGeometry LoadQuad(string fileName, int texCoordCount, Matrix4x4 nodeTransform, int materialCount = 1)
    {
        var path = TestFiles.WriteQuad(_directory, fileName, texCoordCount, nodeTransform, materialCount: materialCount);
        return GltfLoader.Load(path, 1, Vector3.Zero).Single();
    }

    /// <summary>Reads one element of one vertex from the interleaved vertex data.</summary>
    private static T Read<T>(GltfGeometry geometry, string semantic, int vertexIndex) where T : unmanaged
    {
        foreach (var item in geometry.Declaration.EnumerateWithOffsets())
        {
            if (item.VertexElement.SemanticAsText == semantic)
                return MemoryMarshal.Read<T>(geometry.Vertices.AsSpan(vertexIndex * geometry.Declaration.VertexStride + item.Offset));
        }

        throw new AssertionException($"The declaration has no element {semantic}.");
    }
}
