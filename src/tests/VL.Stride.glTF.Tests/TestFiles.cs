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
    /// <param name="withColors">Set to true to write a COLOR_0 attribute.</param>
    /// <param name="withTangents">Set to true to write a TANGENT attribute.</param>
    /// <param name="mode">The triangle mode of the primitive.</param>
    /// <param name="indexed">Set to false to write the vertices without an index buffer.</param>
    /// <returns>The full path of the file. The extension of <paramref name="fileName"/> selects glb or gltf.</returns>
    public static string WriteQuad(
        string directory,
        string fileName,
        int texCoordCount,
        Matrix4x4 nodeTransform,
        bool withNormals = true,
        int materialCount = 1,
        bool withColors = false,
        bool withTangents = false,
        PrimitiveType mode = PrimitiveType.TRIANGLES,
        bool indexed = true)
    {
        var corners = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };

        // A non indexed triangle list needs one vertex per triangle corner.
        var order = !indexed && mode == PrimitiveType.TRIANGLES
            ? new[] { 0, 1, 2, 0, 2, 3 }
            : new[] { 0, 1, 2, 3 };

        var indices = mode switch
        {
            PrimitiveType.TRIANGLE_STRIP => new[] { 0, 1, 3, 2 },
            PrimitiveType.TRIANGLE_FAN => new[] { 0, 1, 2, 3 },
            _ => new[] { 0, 1, 2, 0, 2, 3 },
        };

        var positions = new Vector3[order.Length];
        var normals = new Vector3[order.Length];
        for (var i = 0; i < order.Length; i++)
        {
            positions[i] = corners[order[i]];
            normals[i] = Vector3.UnitZ;
        }

        var model = ModelRoot.CreateModel();
        var mesh = model.CreateMesh("Quad");
        var primitive = mesh.CreatePrimitive();
        primitive.WithVertexAccessor("POSITION", positions);
        if (withNormals)
            primitive.WithVertexAccessor("NORMAL", normals);

        for (var set = 0; set < texCoordCount; set++)
        {
            var uvs = new Vector2[order.Length];
            for (var i = 0; i < order.Length; i++)
                uvs[i] = new Vector2(set + 0.25f * order[i], 0.5f);
            primitive.WithVertexAccessor($"TEXCOORD_{set}", uvs);
        }

        if (withColors)
        {
            var colors = new Vector4[order.Length];
            for (var i = 0; i < order.Length; i++)
                colors[i] = new Vector4(0.25f * order[i], 0.5f, 0.75f, 1);
            primitive.WithVertexAccessor("COLOR_0", colors);
        }

        if (withTangents)
        {
            var tangents = new Vector4[order.Length];
            for (var i = 0; i < order.Length; i++)
                tangents[i] = new Vector4(0.7071068f, 0.7071068f, 0, 1);
            primitive.WithVertexAccessor("TANGENT", tangents);
        }

        if (indexed)
            primitive.WithIndicesAccessor(mode, indices);
        else
            primitive.DrawPrimitiveType = mode;

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

    /// <summary>
    /// Writes one quad mesh that two nodes use: a child of a translated parent node, and a second root node.
    /// </summary>
    /// <returns>The full path of the saved glb file.</returns>
    public static string WriteNodeTree(string directory, string fileName)
    {
        var positions = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
        var normals = new[] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ };
        var uvs = new[] { new Vector2(0, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.75f, 0.5f) };
        var indices = new[] { 0, 1, 2, 0, 2, 3 };

        var model = ModelRoot.CreateModel();
        var mesh = model.CreateMesh("Quad");
        var primitive = mesh.CreatePrimitive();
        primitive.WithVertexAccessor("POSITION", positions);
        primitive.WithVertexAccessor("NORMAL", normals);
        primitive.WithVertexAccessor("TEXCOORD_0", uvs);
        primitive.WithIndicesAccessor(PrimitiveType.TRIANGLES, indices);

        var scene = model.UseScene("Scene");

        var parent = scene.CreateNode("Parent");
        parent.LocalMatrix = Matrix4x4.CreateTranslation(0, 0, 5);

        var child = parent.CreateNode("Child");
        child.LocalMatrix = Matrix4x4.CreateTranslation(1, 0, 0);
        child.Mesh = mesh;

        var second = scene.CreateNode("Second");
        second.LocalMatrix = Matrix4x4.CreateTranslation(10, 0, 0);
        second.Mesh = mesh;

        var path = Path.Combine(directory, fileName);
        model.SaveGLB(path);

        return path;
    }

    /// <summary>Writes a triangle primitive with two vertices and no indices, which has zero triangles.</summary>
    /// <returns>The full path of the saved glb file.</returns>
    public static string WriteStripWithTwoVertices(string directory, string fileName)
    {
        var positions = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0) };
        var normals = new[] { Vector3.UnitZ, Vector3.UnitZ };

        var model = ModelRoot.CreateModel();
        var mesh = model.CreateMesh("Strip");
        var primitive = mesh.CreatePrimitive();
        primitive.WithVertexAccessor("POSITION", positions);
        primitive.WithVertexAccessor("NORMAL", normals);
        primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;

        var scene = model.UseScene("Scene");
        var node = scene.CreateNode("StripNode");
        node.Mesh = mesh;

        var path = Path.Combine(directory, fileName);
        model.SaveGLB(path);

        return path;
    }
}
