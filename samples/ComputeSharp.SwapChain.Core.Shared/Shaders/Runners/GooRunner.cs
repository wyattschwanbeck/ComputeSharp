using System;
using ComputeSharp.SwapChain.Shaders;
#if WINDOWS_UWP
using ComputeSharp.Uwp;
#else
using ComputeSharp.WinUI;
#endif

#nullable enable

namespace ComputeSharp.SwapChain.Core.Shaders.Runners;

/// <summary>
/// A specialized <see cref="IShaderRunner"/> for <see cref="ContouredLayers"/>.
/// </summary>
public sealed class GooRunner : IShaderRunner
{
    private ReadOnlyTexture2D<Rgba32, Float4>? temporary;

    /// <inheritdoc/>
    public bool TryExecute(IReadWriteTexture2D<Float4> texture, TimeSpan timespan, object? parameter)
    {
        if (this.temporary is null ||
            this.temporary.Width != texture.Width ||
            this.temporary.Height != texture.Height)
        {
            this.temporary?.Dispose();

            this.temporary = texture.GraphicsDevice.AllocateReadOnlyTexture2D<Rgba32, float4>(texture.Width, texture.Height);
        }

        texture.GraphicsDevice.ForEach(texture, new Goo.A((float)timespan.TotalSeconds));

        //((ReadWriteTexture2D<Rgba32, float4>)texture).CopyTo(this.temporary);

        //texture.GraphicsDevice.ForEach(texture, new Goo.B(this.temporary));

        //((ReadWriteTexture2D<Rgba32, float4>)texture).CopyTo(this.temporary);

        //texture.GraphicsDevice.ForEach(texture, new Goo.C((float)timespan.TotalSeconds, this.temporary));

        return true;
    }
}
