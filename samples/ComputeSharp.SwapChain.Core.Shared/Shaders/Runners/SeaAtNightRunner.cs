using System;
using System.IO;
using ComputeSharp.SwapChain.Shaders;
#if WINDOWS_UWP
using ComputeSharp.Uwp;
#else
using ComputeSharp.WinUI;
#endif
using Windows.ApplicationModel;

#nullable enable

namespace ComputeSharp.SwapChain.Core.Shaders.Runners;

/// <summary>
/// A specialized <see cref="IShaderRunner"/> for <see cref="ContouredLayers"/>.
/// </summary>
public sealed class SeaAtNightRunner : IShaderRunner
{
    private ReadWriteTexture2D<Rgba32, float4>? texture1;
    private ReadOnlyTexture2D<Rgba32, float4>? texture2;
    private int frameCount;

    /// <inheritdoc/>
    public void Execute(IReadWriteTexture2D<Float4> texture, TimeSpan timespan)
    {
        if (this.texture1 is null)
        {
            texture1 = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, float4>(texture.Width, texture.Height);
            texture2 = Gpu.Default.AllocateReadOnlyTexture2D<Rgba32, float4>(texture.Width, texture.Height);
        }

        float time = (float)timespan.TotalSeconds;

        Gpu.Default.ForEach(texture1, new SeaAtNight.BufferA(time, frameCount++));

        texture1.CopyTo(texture2!);

        Gpu.Default.ForEach(texture, new SeaAtNight.Image(time, texture2!));
    }
}
