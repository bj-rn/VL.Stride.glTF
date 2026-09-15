using NUnit.Framework;
using SharpGLTF.Schema2;
using Stride.Core.Mathematics;
using VL.Stride.glTF.Core;

namespace VL.Stride.glTF.Tests;

/// <summary>Loads the real Blender export in <c>src/tests/assets</c> with the production loader.</summary>
[TestFixture]
public class RealAssetTests
{
    /// <summary>Every glb and gltf file in the assets folder.</summary>
    public static IEnumerable<string> AssetFiles()
    {
        if (!Directory.Exists(TestPaths.AssetsDirectory))
            yield break;

        var files = Directory.GetFiles(TestPaths.AssetsDirectory, "*.glb")
            .Concat(Directory.GetFiles(TestPaths.AssetsDirectory, "*.gltf"))
            .OrderBy(path => path, StringComparer.Ordinal);

        foreach (var file in files)
            yield return file;
    }

    [TestCaseSource(nameof(AssetFiles))]
    public void LoadsEveryTexCoordSet(string path)
    {
        var geometries = GltfLoader.Load(path, 1f, Vector3.Zero);

        Assert.That(geometries, Is.Not.Empty);

        foreach (var geometry in geometries)
        {
            var expectedTexCoordCount = Math.Min(geometry.TexCoordCountInFile, VertexLayout.MaxTexCoordCount);
            var texCoordElementCount = geometry.Declaration.VertexElements.Count(e => e.SemanticName == "TEXCOORD");
            var triangleCount = geometry.Indices.Length / 3;

            TestContext.WriteLine($"{geometry.Name}: {geometry.TexCoordCountInFile} UV sets in file, " +
                $"{geometry.VertexCount} vertices, {triangleCount} triangles.");

            Assert.That(texCoordElementCount, Is.EqualTo(expectedTexCoordCount));
            Assert.That(geometry.VertexCount, Is.GreaterThan(0));
            Assert.That(geometry.Indices.Length % 3, Is.EqualTo(0));
            Assert.That(geometry.Vertices.Length, Is.EqualTo(geometry.VertexCount * geometry.Declaration.VertexStride));
        }

        var model = ModelRoot.Load(path);
        var firstPrimitive = model.LogicalMeshes[0].Primitives[0];
        var uvKeyCount = firstPrimitive.VertexAccessors.Keys.Count(key => key.StartsWith("TEXCOORD_", StringComparison.Ordinal));

        Assert.That(uvKeyCount, Is.EqualTo(geometries[0].TexCoordCountInFile));
    }

    [Test]
    public void AssetsArePresent()
    {
        if (!AssetFiles().Any())
            Assert.Ignore($"No glb or gltf file was found in '{TestPaths.AssetsDirectory}'.");
    }
}
