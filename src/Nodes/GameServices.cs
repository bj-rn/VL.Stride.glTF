// Gets the Stride Game from the vvvv app host without a reference to VL.Stride.Runtime.
// Adapted from VL.Stride.Text3d (MIT).

using Stride.Engine;
using Stride.Graphics;
using VL.Core;
using VL.Lib.Basics.Resources;

namespace VL.Stride.glTF.Nodes;

internal sealed class GameServices : IDisposable
{
    private readonly NodeContext nodeContext;
    private IResourceHandle<Game>? gameHandle;

    public GameServices(NodeContext nodeContext) => this.nodeContext = nodeContext;

    public Game Game
    {
        get
        {
            gameHandle ??= ((IResourceProvider<Game>?)nodeContext.AppHost.Services.GetService(typeof(IResourceProvider<Game>)))?.GetHandle()
                ?? throw new InvalidOperationException("No Stride Game is available. Load VL.Stride first.");
            return gameHandle.Resource;
        }
    }

    public GraphicsDevice Device => Game.GraphicsDevice;

    public void Dispose() => gameHandle?.Dispose();
}
