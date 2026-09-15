using Stride.Core.Mathematics;
using Stride.Graphics;

namespace VL.Stride.glTF.Core;

/// <summary>Joins geometries that share the material and the vertex declaration into one geometry.</summary>
public static class GeometryMerger
{
    /// <summary>Merges the geometries per material index and vertex declaration.</summary>
    /// <remarks>The result keeps the order of the first appearance of each group. A group with one member passes through unchanged.</remarks>
    /// <param name="geometries">The geometries to merge.</param>
    public static List<GltfGeometry> Merge(IReadOnlyList<GltfGeometry> geometries)
    {
        ArgumentNullException.ThrowIfNull(geometries);

        var groups = new List<List<GltfGeometry>>();
        var groupsByKey = new Dictionary<(int MaterialIndex, VertexDeclaration Declaration), List<GltfGeometry>>();

        foreach (var geometry in geometries)
        {
            var key = (geometry.MaterialIndex, geometry.Declaration);
            if (!groupsByKey.TryGetValue(key, out var group))
            {
                group = [];
                groupsByKey.Add(key, group);
                groups.Add(group);
            }

            group.Add(geometry);
        }

        var result = new List<GltfGeometry>(groups.Count);
        foreach (var group in groups)
            result.Add(group.Count == 1 ? group[0] : MergeGroup(group));

        return result;
    }

    private static GltfGeometry MergeGroup(List<GltfGeometry> group)
    {
        var first = group[0];
        var stride = first.Declaration.VertexStride;

        var vertexCount = 0;
        var indexCount = 0;
        var texCoordCountInFile = 0;
        foreach (var geometry in group)
        {
            vertexCount += geometry.VertexCount;
            indexCount += geometry.Indices.Length;
            texCoordCountInFile = Math.Max(texCoordCountInFile, geometry.TexCoordCountInFile);
        }

        var vertices = new byte[vertexCount * stride];
        var indices = new int[indexCount];
        var boundingBox = first.BoundingBox;
        var boundingSphere = first.BoundingSphere;

        var vertexOffset = 0;
        var byteOffset = 0;
        var indexOffset = 0;
        foreach (var geometry in group)
        {
            var byteCount = geometry.VertexCount * stride;
            Array.Copy(geometry.Vertices, 0, vertices, byteOffset, byteCount);

            for (var i = 0; i < geometry.Indices.Length; i++)
                indices[indexOffset + i] = geometry.Indices[i] + vertexOffset;

            var box = geometry.BoundingBox;
            BoundingBox.Merge(ref boundingBox, ref box, out var mergedBox);
            boundingBox = mergedBox;

            var sphere = geometry.BoundingSphere;
            BoundingSphere.Merge(ref boundingSphere, ref sphere, out var mergedSphere);
            boundingSphere = mergedSphere;

            vertexOffset += geometry.VertexCount;
            byteOffset += byteCount;
            indexOffset += geometry.Indices.Length;
        }

        return new GltfGeometry
        {
            Name = first.Name,
            MaterialIndex = first.MaterialIndex,
            Declaration = first.Declaration,
            Vertices = vertices,
            VertexCount = vertexCount,
            Indices = indices,
            TexCoordCountInFile = texCoordCountInFile,
            BoundingBox = boundingBox,
            BoundingSphere = boundingSphere,
        };
    }
}
