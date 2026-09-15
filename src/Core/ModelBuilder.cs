using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;
using StrideModel = Stride.Rendering.Model;

namespace VL.Stride.glTF.Core;

/// <summary>Builds a Stride model with GPU buffers from <see cref="GltfGeometry"/> objects.</summary>
public static class ModelBuilder
{
    /// <summary>Creates one mesh per geometry and returns the model.</summary>
    /// <remarks>The model owns the GPU buffers. The caller must call <see cref="ReleaseGraphicsResources"/> when the model is no longer used.</remarks>
    /// <param name="device">The graphics device that creates the buffers.</param>
    /// <param name="geometries">The geometries to upload.</param>
    public static StrideModel Build(GraphicsDevice device, IReadOnlyList<GltfGeometry> geometries)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(geometries);

        var model = new StrideModel();
        var boundingBox = BoundingBox.Empty;
        var boundingSphere = default(BoundingSphere);

        for (var i = 0; i < geometries.Count; i++)
        {
            var geometry = geometries[i];
            model.Add(CreateMesh(device, geometry));

            if (i == 0)
            {
                boundingBox = geometry.BoundingBox;
                boundingSphere = geometry.BoundingSphere;
                continue;
            }

            var box = geometry.BoundingBox;
            BoundingBox.Merge(ref boundingBox, ref box, out var mergedBox);
            boundingBox = mergedBox;

            var sphere = geometry.BoundingSphere;
            BoundingSphere.Merge(ref boundingSphere, ref sphere, out var mergedSphere);
            boundingSphere = mergedSphere;
        }

        model.BoundingBox = boundingBox;
        model.BoundingSphere = boundingSphere;

        return model;
    }

    /// <summary>Disposes the vertex buffers and the index buffers of every mesh of the model.</summary>
    /// <param name="model">The model to release. A null model does nothing.</param>
    public static void ReleaseGraphicsResources(StrideModel model)
    {
        if (model?.Meshes is null)
            return;

        foreach (var mesh in model.Meshes)
        {
            if (mesh?.Draw is null)
                continue;

            mesh.Draw.IndexBuffer?.Buffer?.Dispose();

            if (mesh.Draw.VertexBuffers is null)
                continue;

            foreach (var vertexBuffer in mesh.Draw.VertexBuffers)
                vertexBuffer.Buffer?.Dispose();
        }
    }

    private static Mesh CreateMesh(GraphicsDevice device, GltfGeometry geometry)
    {
        var vertices = geometry.Vertices;
        var vertexBuffer = Buffer.Vertex.New(device, (Span<byte>)vertices).RecreateWith(vertices);
        var vertexBinding = new VertexBufferBinding(vertexBuffer, geometry.Declaration, geometry.VertexCount);

        IndexBufferBinding indexBinding;
        if (geometry.VertexCount <= ushort.MaxValue)
        {
            var indices = new ushort[geometry.Indices.Length];
            for (var i = 0; i < indices.Length; i++)
                indices[i] = (ushort)geometry.Indices[i];

            indexBinding = new IndexBufferBinding(Buffer.Index.New(device, indices).RecreateWith(indices), false, indices.Length);
        }
        else
        {
            var indices = geometry.Indices;
            indexBinding = new IndexBufferBinding(Buffer.Index.New(device, indices).RecreateWith(indices), true, indices.Length);
        }

        var meshDraw = new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            DrawCount = geometry.Indices.Length,
            VertexBuffers = [vertexBinding],
            IndexBuffer = indexBinding,
        };

        return new Mesh
        {
            Draw = meshDraw,
            Name = geometry.Name,
            MaterialIndex = geometry.MaterialIndex,
            BoundingBox = geometry.BoundingBox,
            BoundingSphere = geometry.BoundingSphere,
        };
    }
}
