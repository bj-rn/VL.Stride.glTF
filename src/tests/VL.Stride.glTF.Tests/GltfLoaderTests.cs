using System.Runtime.InteropServices;
using NUnit.Framework;
using Stride.Core.Mathematics;
using VL.Stride.glTF.Core;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace VL.Stride.glTF.Tests;

[TestFixture]
public class GltfLoaderTests
{
    private const float Tolerance = 1e-5f;

    private string _directory = string.Empty;

    [SetUp]
    public void SetUp() => _directory = TestFiles.CreateDirectory();

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(9)]
    [TestCase(10)]
    public void LoadsEveryTexCoordSet(int texCoordCount)
    {
        var path = TestFiles.WriteQuad(_directory, "quad.glb", texCoordCount, Matrix4x4.Identity);

        var geometries = GltfLoader.Load(path, 1, Vector3.Zero);

        Assert.That(geometries, Has.Count.EqualTo(1));
        var geometry = geometries[0];
        Assert.That(geometry.Declaration.VertexElements.Count(e => e.SemanticName == "TEXCOORD"), Is.EqualTo(texCoordCount));
        Assert.That(geometry.TexCoordCountInFile, Is.EqualTo(texCoordCount));

        var lastSet = texCoordCount - 1;
        var semantic = lastSet == 0 ? "TEXCOORD" : $"TEXCOORD{lastSet}";
        Assert.That(Read<Vector2>(geometry, semantic, 2), Is.EqualTo(new Vector2(lastSet + 0.5f, 0.5f)));
    }

    [Test]
    public void TwelveTexCoordSetsAreLimitedToTen()
    {
        var path = TestFiles.WriteQuad(_directory, "quad.glb", 12, Matrix4x4.Identity);

        var geometry = GltfLoader.Load(path, 1, Vector3.Zero).Single();

        Assert.That(geometry.Declaration.VertexElements.Count(e => e.SemanticName == "TEXCOORD"), Is.EqualTo(10));
        Assert.That(geometry.TexCoordCountInFile, Is.EqualTo(12));
    }

    [Test]
    public void ReversesWindingForPositiveDeterminant()
    {
        var path = TestFiles.WriteQuad(_directory, "quad.glb", 1, Matrix4x4.Identity);

        var geometry = GltfLoader.Load(path, 1, Vector3.Zero).Single();

        Assert.That(geometry.Indices[..3], Is.EqualTo(new[] { 0, 2, 1 }));
    }

    [Test]
    public void KeepsWindingForMirroredNode()
    {
        var path = TestFiles.WriteQuad(_directory, "quad.glb", 1, Matrix4x4.CreateScale(-1, 1, 1));

        var geometry = GltfLoader.Load(path, 1, Vector3.Zero).Single();

        Assert.That(geometry.Indices[..3], Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void AppliesTransformPivotAndScale()
    {
        var path = TestFiles.WriteQuad(_directory, "quad.glb", 1, Matrix4x4.CreateTranslation(1, 2, 3));

        var geometry = GltfLoader.Load(path, 2, new Vector3(1, 0, 0)).Single();

        AssertNear(Read<Vector3>(geometry, "POSITION", 1), new Vector3(2, 4, 6));
        AssertNear(geometry.BoundingBox.Minimum, Read<Vector3>(geometry, "POSITION", 0));
        AssertNear(geometry.BoundingBox.Maximum, Read<Vector3>(geometry, "POSITION", 2));
    }

    [Test]
    public void NormalsStayUnitLengthUnderNonUniformScale()
    {
        var path = TestFiles.WriteQuad(_directory, "quad.glb", 1, Matrix4x4.CreateScale(2, 1, 1));

        var geometry = GltfLoader.Load(path, 1, Vector3.Zero).Single();

        for (var i = 0; i < geometry.VertexCount; i++)
            Assert.That(Read<Vector3>(geometry, "NORMAL", i).Length(), Is.EqualTo(1).Within(Tolerance));
    }

    [Test]
    public void GeneratesNormalsWhenMissing()
    {
        var path = TestFiles.WriteQuad(_directory, "quad.glb", 1, Matrix4x4.Identity, withNormals: false);

        var geometry = GltfLoader.Load(path, 1, Vector3.Zero).Single();

        for (var i = 0; i < geometry.VertexCount; i++)
            AssertNear(Read<Vector3>(geometry, "NORMAL", i), Vector3.UnitZ);
    }

    [Test]
    public void LoadsGltfWithSeparateBin()
    {
        var path = TestFiles.WriteQuad(_directory, "quad.gltf", 1, Matrix4x4.Identity);

        var geometries = GltfLoader.Load(path, 1, Vector3.Zero);

        Assert.That(geometries, Has.Count.EqualTo(1));
        Assert.That(geometries[0].VertexCount, Is.EqualTo(4));
        Assert.That(geometries[0].Indices, Has.Length.EqualTo(6));
        Assert.That(Directory.GetFiles(_directory, "*.bin"), Is.Not.Empty);
    }

    [Test]
    public void ThrowsForMissingFile()
    {
        var path = Path.Combine(_directory, "missing.glb");

        Assert.Throws<FileNotFoundException>(() => GltfLoader.Load(path, 1, Vector3.Zero));
    }

    [Test]
    public void UsesMaterialIndex()
    {
        var first = TestFiles.WriteQuad(_directory, "one.glb", 1, Matrix4x4.Identity, materialCount: 1);
        var second = TestFiles.WriteQuad(_directory, "two.glb", 1, Matrix4x4.Identity, materialCount: 2);

        Assert.That(GltfLoader.Load(first, 1, Vector3.Zero).Single().MaterialIndex, Is.EqualTo(0));
        Assert.That(GltfLoader.Load(second, 1, Vector3.Zero).Single().MaterialIndex, Is.EqualTo(1));
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

    private static void AssertNear(Vector3 actual, Vector3 expected)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(Tolerance));
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(Tolerance));
        Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(Tolerance));
    }
}
