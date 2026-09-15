using System.Numerics;
using SharpGLTF.Schema2;

namespace VL.Stride.glTF.Tests;

/// <summary>Writes small synthetic glTF files for the loader tests.</summary>
internal static class TestFiles
{
    /// <summary>Creates a fresh temp directory for one test.</summary>
    public static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VL.Stride.glTF.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Writes a unit quad in the XY plane with counter clockwise triangles as seen from +Z.
    /// UV set i has the value (i + 0.25 * corner, 0.5) for corner 0 to 3.
    /// The primitive gets the last created material.
    /// </summary>
    /// <returns>The full path of the file. The extension of <paramref name="fileName"/> selects glb or gltf.</returns>
    public static string WriteQuad(string directory, string fileName, int texCoordCount, Matrix4x4 nodeTransform, bool withNormals = true, int materialCount = 1)
    {
        var positions = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
        var normals = new[] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ };
        var indices = new[] { 0, 1, 2, 0, 2, 3 };

        var model = ModelRoot.CreateModel();
        var mesh = model.CreateMesh("Quad");
        var primitive = mesh.CreatePrimitive();
        primitive.WithVertexAccessor("POSITION", positions);
        if (withNormals)
            primitive.WithVertexAccessor("NORMAL", normals);

        for (var set = 0; set < texCoordCount; set++)
        {
            var uvs = new Vector2[4];
            for (var corner = 0; corner < 4; corner++)
                uvs[corner] = new Vector2(set + 0.25f * corner, 0.5f);
            primitive.WithVertexAccessor($"TEXCOORD_{set}", uvs);
        }

        primitive.WithIndicesAccessor(PrimitiveType.TRIANGLES, indices);

        Material? material = null;
        for (var i = 0; i < materialCount; i++)
            material = model.CreateMaterial($"Material{i}");
        if (material is not null)
            primitive.WithMaterial(material);

        var scene = model.UseScene("Scene");
        var node = scene.CreateNode("QuadNode");
        node.Mesh = mesh;
        node.LocalMatrix = nodeTransform;

        var path = Path.Combine(directory, fileName);
        if (string.Equals(Path.GetExtension(fileName), ".glb", StringComparison.OrdinalIgnoreCase))
            model.SaveGLB(path);
        else
            model.SaveGLTF(path);

        return path;
    }
}
