using SharpGLTF.Schema2;
using Stride.Core.Mathematics;

namespace VL.Stride.glTF.Core;

/// <summary>Reads the triangle geometry of a glTF or glb file into <see cref="GltfGeometry"/> objects.</summary>
public static class GltfLoader
{
    /// <summary>Loads every triangle primitive of the default scene, in node order.</summary>
    /// <param name="path">The path of the .gltf or .glb file.</param>
    /// <param name="importScale">The factor that scales the positions after the pivot is subtracted.</param>
    /// <param name="pivotPosition">The world space point that becomes the origin.</param>
    /// <exception cref="FileNotFoundException">The path is empty or the file does not exist.</exception>
    /// <exception cref="InvalidDataException">A triangle primitive has no POSITION attribute.</exception>
    public static List<GltfGeometry> Load(string path, float importScale, Vector3 pivotPosition)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException($"The glTF file was not found: '{path}'.", path);

        var model = ModelRoot.Load(path);
        var scene = model.DefaultScene ?? model.LogicalScenes.FirstOrDefault();

        var result = new List<GltfGeometry>();
        if (scene is null)
            return result;

        foreach (var node in scene.VisualChildren)
            LoadNode(node, importScale, pivotPosition, result);

        return result;
    }

    private static void LoadNode(Node node, float importScale, Vector3 pivotPosition, List<GltfGeometry> result)
    {
        if (node.Mesh is { } mesh)
        {
            Matrix world = node.WorldMatrix;
            foreach (var primitive in mesh.Primitives)
            {
                if (LoadPrimitive(node, mesh, primitive, world, importScale, pivotPosition) is { } geometry)
                    result.Add(geometry);
            }
        }

        foreach (var child in node.VisualChildren)
            LoadNode(child, importScale, pivotPosition, result);
    }

    private static GltfGeometry? LoadPrimitive(Node node, Mesh mesh, MeshPrimitive primitive, Matrix world, float importScale, Vector3 pivotPosition)
    {
        if (primitive.DrawPrimitiveType is not (PrimitiveType.TRIANGLES or PrimitiveType.TRIANGLE_STRIP or PrimitiveType.TRIANGLE_FAN))
            return null;

        var positionAccessor = primitive.GetVertexAccessor("POSITION")
            ?? throw new InvalidDataException($"The mesh '{mesh.Name}' has a primitive without a POSITION attribute.");

        var positions = ReadVector3(positionAccessor);
        var vertexCount = positions.Length;

        // Stride renders clockwise front faces, glTF stores counter clockwise ones.
        // A mirrored node (negative determinant) already flips the winding.
        var determinant = world.Determinant();
        var indices = ReadIndices(primitive, reverseWinding: determinant >= 0);

        var normalAccessor = primitive.GetVertexAccessor("NORMAL");
        var normals = normalAccessor is not null
            ? ReadVector3(normalAccessor)
            : GenerateNormals(positions, primitive);

        var tangentAccessor = primitive.GetVertexAccessor("TANGENT");
        var tangents = tangentAccessor is not null ? ReadVector4(tangentAccessor) : [];

        var colorAccessor = primitive.GetVertexAccessor("COLOR_0");
        var colors = colorAccessor is not null ? ReadColors(colorAccessor) : [];

        var texCoordCountInFile = 0;
        while (primitive.GetVertexAccessor($"TEXCOORD_{texCoordCountInFile}") is not null)
            texCoordCountInFile++;

        var texCoords = new List<Vector2[]>();
        for (var i = 0; i < Math.Min(texCoordCountInFile, VertexLayout.MaxTexCoordCount); i++)
            texCoords.Add(ReadVector2(primitive.GetVertexAccessor($"TEXCOORD_{i}")));

        for (var i = 0; i < vertexCount; i++)
            positions[i] = (Vector3.TransformCoordinate(positions[i], world) - pivotPosition) * importScale;

        var normalMatrix = Matrix.Transpose(Matrix.Invert(world));
        TransformNormals(normals, normalMatrix);
        TransformTangents(tangents, normalMatrix, determinant < 0);

        var declaration = VertexLayout.Create(tangents.Length > 0, texCoords.Count, colors.Length > 0);

        return new GltfGeometry
        {
            Name = mesh.Name ?? node.Name ?? "Mesh",
            MaterialIndex = primitive.Material?.LogicalIndex ?? 0,
            Declaration = declaration,
            Vertices = VertexLayout.Interleave(declaration, positions, normals, tangents, texCoords, colors),
            VertexCount = vertexCount,
            Indices = indices,
            TexCoordCountInFile = texCoordCountInFile,
            BoundingBox = BoundingBox.FromPoints(positions),
            BoundingSphere = BoundingSphere.FromPoints(positions),
        };
    }

    private static int[] ReadIndices(MeshPrimitive primitive, bool reverseWinding)
    {
        var indices = new List<int>();
        foreach (var (a, b, c) in primitive.GetTriangleIndices())
        {
            indices.Add(a);
            indices.Add(reverseWinding ? c : b);
            indices.Add(reverseWinding ? b : c);
        }
        return indices.ToArray();
    }

    /// <summary>Computes area weighted vertex normals from the untransformed positions and the glTF winding.</summary>
    private static Vector3[] GenerateNormals(Vector3[] positions, MeshPrimitive primitive)
    {
        var normals = new Vector3[positions.Length];
        foreach (var (a, b, c) in primitive.GetTriangleIndices())
        {
            var faceNormal = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            normals[a] += faceNormal;
            normals[b] += faceNormal;
            normals[c] += faceNormal;
        }

        for (var i = 0; i < normals.Length; i++)
            normals[i] = Vector3.Normalize(normals[i]);

        return normals;
    }

    private static void TransformNormals(Vector3[] normals, Matrix normalMatrix)
    {
        for (var i = 0; i < normals.Length; i++)
            normals[i] = Vector3.Normalize(Vector3.TransformNormal(normals[i], normalMatrix));
    }

    private static void TransformTangents(Vector4[] tangents, Matrix normalMatrix, bool mirrored)
    {
        for (var i = 0; i < tangents.Length; i++)
        {
            var xyz = Vector3.Normalize(Vector3.TransformNormal(new Vector3(tangents[i].X, tangents[i].Y, tangents[i].Z), normalMatrix));
            var w = mirrored ? -tangents[i].W : tangents[i].W;
            tangents[i] = new Vector4(xyz, w);
        }
    }

    private static Vector2[] ReadVector2(Accessor accessor)
    {
        var source = accessor.AsVector2Array();
        var result = new Vector2[source.Count];
        for (var i = 0; i < result.Length; i++)
            result[i] = source[i];
        return result;
    }

    private static Vector3[] ReadVector3(Accessor accessor)
    {
        var source = accessor.AsVector3Array();
        var result = new Vector3[source.Count];
        for (var i = 0; i < result.Length; i++)
            result[i] = source[i];
        return result;
    }

    private static Vector4[] ReadVector4(Accessor accessor)
    {
        var source = accessor.AsVector4Array();
        var result = new Vector4[source.Count];
        for (var i = 0; i < result.Length; i++)
            result[i] = source[i];
        return result;
    }

    private static Vector4[] ReadColors(Accessor accessor)
    {
        var source = accessor.AsColorArray();
        var result = new Vector4[source.Count];
        for (var i = 0; i < result.Length; i++)
            result[i] = source[i];
        return result;
    }
}
