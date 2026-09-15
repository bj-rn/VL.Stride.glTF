using Stride.Core.Mathematics;
using VL.Core;
using VL.Core.Import;
using VL.Model;
using VL.Stride.glTF.Core;
using Path = VL.Lib.IO.Path;
using StrideModel = Stride.Rendering.Model;

namespace VL.Stride.glTF.Nodes;

/// <summary>Loads a glTF 2.0 model from disk and keeps every UV set the file contains, up to 10. Blocks the frame while it loads.</summary>
[ProcessNode]
public sealed class GltfReader : IDisposable
{
    private readonly GameServices services;
    private string? previousPath;
    private float previousImportScale;
    private Vector3 previousPivotPosition;
    private bool previousMergeMeshes;
    private StrideModel? cachedModel;
    private int cachedTexcoordCount;

    public GltfReader(NodeContext nodeContext) => services = new GameServices(nodeContext);

    /// <param name="model">The loaded model. Null while no file is set.</param>
    /// <param name="texcoordCount">The number of UV sets found in the file. Stride uses at most 10 of them.</param>
    /// <param name="path">The .gltf or .glb file.</param>
    /// <param name="importScale">Scale applied to all positions.</param>
    /// <param name="pivotPosition">Subtracted from all positions before the scale.</param>
    /// <param name="mergeMeshes">Combines primitives with the same material and vertex layout into one mesh.</param>
    /// <param name="reload">Loads the file again.</param>
    public void Update(out StrideModel? model, out int texcoordCount, Path? path,
        float importScale = 1f, Vector3 pivotPosition = default, [Pin(Visibility = PinVisibility.Optional)] bool mergeMeshes = true,
        bool reload = false)
    {
        var currentPath = path?.Value;

        var hasChanged = previousPath != currentPath
            || previousImportScale != importScale
            || previousPivotPosition != pivotPosition
            || previousMergeMeshes != mergeMeshes
            || reload;

        if (hasChanged)
        {
            // Store the inputs before the load. A failed load does not retry every frame.
            previousPath = currentPath;
            previousImportScale = importScale;
            previousPivotPosition = pivotPosition;
            previousMergeMeshes = mergeMeshes;

            if (cachedModel is not null)
                ModelBuilder.ReleaseGraphicsResources(cachedModel);
            cachedModel = null;
            cachedTexcoordCount = 0;

            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var geometries = GltfLoader.Load(currentPath, importScale, pivotPosition);
                if (mergeMeshes)
                    geometries = GeometryMerger.Merge(geometries);

                cachedModel = ModelBuilder.Build(services.Device, geometries);

                foreach (var geometry in geometries)
                    cachedTexcoordCount = Math.Max(cachedTexcoordCount, geometry.TexCoordCountInFile);
            }
        }

        model = cachedModel;
        texcoordCount = cachedTexcoordCount;
    }

    public void Dispose()
    {
        if (cachedModel is not null)
            ModelBuilder.ReleaseGraphicsResources(cachedModel);
        cachedModel = null;
        services.Dispose();
    }
}
