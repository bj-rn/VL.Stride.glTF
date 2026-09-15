using Stride.Core.Mathematics;
using VL.Core;
using VL.Core.Import;
using VL.Stride.glTF.Core;
using Path = VL.Lib.IO.Path;
using StrideModel = Stride.Rendering.Model;

namespace VL.Stride.glTF.Nodes;

/// <summary>Loads a glTF 2.0 model from disk on a background thread and keeps every UV set the file contains, up to 10. Outputs the last loaded model while a new one loads.</summary>
[ProcessNode(Name = "GltfReader (Async)")]
public sealed class GltfReaderAsync : IDisposable
{
    private readonly GameServices services;
    private BackgroundComputation<List<GltfGeometry>>? computation;
    private StrideModel? cachedModel;
    private int cachedTexcoordCount;
    private int reloadCount;
    private bool lastReload;

    public GltfReaderAsync(NodeContext nodeContext) => services = new GameServices(nodeContext);

    /// <param name="model">The last loaded model. Null while no file is set.</param>
    /// <param name="texcoordCount">The number of UV sets found in the file. Stride uses at most 10 of them.</param>
    /// <param name="isLoading">True while a file loads in the background.</param>
    /// <param name="path">The .gltf or .glb file.</param>
    /// <param name="importScale">Scale applied to all positions.</param>
    /// <param name="pivotPosition">Subtracted from all positions before the scale.</param>
    /// <param name="mergeMeshes">Combines primitives with the same material and vertex layout into one mesh.</param>
    /// <param name="reload">Loads the file again.</param>
    public void Update(out StrideModel? model, out int texcoordCount, out bool isLoading, Path? path,
        float importScale = 1f, Vector3 pivotPosition = default, bool mergeMeshes = true,
        bool reload = false)
    {
        // Reload is a bang. Only a rising edge starts a new load.
        if (reload && !lastReload)
            reloadCount++;
        lastReload = reload;

        var currentPath = path?.Value;

        if (string.IsNullOrWhiteSpace(currentPath))
        {
            if (cachedModel is not null)
                ModelBuilder.ReleaseGraphicsResources(cachedModel);
            cachedModel = null;
            cachedTexcoordCount = 0;

            // Drop the last result, so the same path loads again later.
            computation = null;

            model = null;
            texcoordCount = 0;
            isLoading = false;
            return;
        }

        var hash = HashCode.Combine(currentPath, importScale, pivotPosition, mergeMeshes, reloadCount);

        computation ??= new();
        var adopted = computation.Poll(hash, out var result, out var needsStart, out isLoading);
        if (needsStart)
        {
            // Copy the inputs. The background thread must not read the pins.
            var p = currentPath;
            var s = importScale;
            var pv = pivotPosition;
            var merge = mergeMeshes;
            computation.Start(hash, () =>
            {
                var g = GltfLoader.Load(p, s, pv);
                return merge ? GeometryMerger.Merge(g) : g;
            });
            isLoading = true;
        }

        if (adopted && result is not null)
        {
            if (cachedModel is not null)
                ModelBuilder.ReleaseGraphicsResources(cachedModel);

            cachedModel = ModelBuilder.Build(services.Device, result);

            cachedTexcoordCount = 0;
            foreach (var geometry in result)
                cachedTexcoordCount = Math.Max(cachedTexcoordCount, geometry.TexCoordCountInFile);
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
