using System;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace ComputeSharp.D2D1.Tests.Shaders;

public partial class MotionBlurEffect : ID2D1TransformMapper<MotionBlurEffect.Shader>
{
    public static Shader CreateShader(
        int inputWidth,
        int inputHeight,
        float angle,
        int distance)
    {
        double angleRadians = Math.PI / 180 * angle;
        double sin = Math.Sin(angleRadians);
        double cos = Math.Cos(angleRadians);
        double dx = -(double)distance * cos;
        double dy = +(double)distance * sin;

        float2 startOffset = float2.Zero;
        float2 endOffset = new((float)dx, (float)dy);

        int maxSamples = (1 + distance) * 3 / 2;

        return new(new Shader.Constants(
            (uint)inputWidth,
            (uint)inputHeight,
            startOffset,
            endOffset,
            (uint)maxSamples));
    }

    /// <inheritdoc/>
    void ID2D1TransformMapper<Shader>.MapInputsToOutput(in Shader shader, ReadOnlySpan<Rectangle> inputs, ReadOnlySpan<Rectangle> opaqueInputs, out Rectangle output, out Rectangle opaqueOutput)
    {
        output = MapInputOrOutput(inputs[0], GetImageBounds(in shader), shader.constants.StartOffsetAsSizeF, shader.constants.EndOffsetAsSizeF);
        opaqueOutput = Rectangle.Empty;
    }

    /// <inheritdoc/>
    void ID2D1TransformMapper<Shader>.MapInvalidOutput(in Shader shader, int inputIndex, Rectangle invalidInput, out Rectangle invalidOutput)
    {
        invalidOutput = MapInputOrOutput(invalidInput, GetImageBounds(in shader), shader.constants.StartOffsetAsSizeF, shader.constants.EndOffsetAsSizeF);
    }

    /// <inheritdoc/>
    void ID2D1TransformMapper<Shader>.MapOutputToInputs(in Shader shader, in Rectangle output, Span<Rectangle> inputs)
    {
        Rectangle imageBounds = GetImageBounds(in shader);

        foreach (ref Rectangle input in inputs)
        {
            input = MapInputOrOutput(output, imageBounds, shader.constants.StartOffsetAsSizeF, shader.constants.EndOffsetAsSizeF);
        }
    }

    private static Rectangle MapInputOrOutput(Point inputOrOutput, SizeF startOffset, SizeF endOffset)
    {
        PointF startPoint = PointF.Add(inputOrOutput, startOffset);
        PointF endPoint = PointF.Add(inputOrOutput, endOffset);

        float minX = Math.Min(startPoint.X, endPoint.X);
        float minY = Math.Min(startPoint.Y, endPoint.Y);
        float maxX = Math.Max(startPoint.X, endPoint.X);
        float maxY = Math.Max(startPoint.Y, endPoint.Y);

        return Rectangle.Round(new RectangleF(minX, minY, maxX - minX, maxY - minY));
    }

    private static Rectangle MapInputOrOutput(
        Rectangle inputOrOutput,
        Rectangle imageBounds,
        SizeF startOffset,
        SizeF endOffset)
    {
        Rectangle topLeft0 = MapInputOrOutput(new Point(inputOrOutput.Left, inputOrOutput.Top), startOffset, endOffset);
        Rectangle topLeft = Rectangle.Intersect(topLeft0, imageBounds);

        Rectangle topRight0 = MapInputOrOutput(new Point(inputOrOutput.Right, inputOrOutput.Top), startOffset, endOffset);
        Rectangle topRight = Rectangle.Intersect(topRight0, imageBounds);

        Rectangle bottomLeft0 = MapInputOrOutput(new Point(inputOrOutput.Left, inputOrOutput.Bottom), startOffset, endOffset);
        Rectangle bottomLeft = Rectangle.Intersect(bottomLeft0, imageBounds);

        Rectangle bottomRight0 = MapInputOrOutput(new Point(inputOrOutput.Right, inputOrOutput.Bottom), startOffset, endOffset);
        Rectangle bottomRight = Rectangle.Intersect(bottomRight0, imageBounds);

        Rectangle result0 = Rectangle.Union(topLeft, topRight);
        Rectangle result1 = Rectangle.Union(result0, bottomLeft);
        Rectangle result2 = Rectangle.Union(result1, bottomRight);
        Rectangle result = Rectangle.Intersect(result2, imageBounds);

        return result;
    }

    private static Rectangle GetImageBounds(in Shader shader)
    {
        return new(0, 0, (int)shader.constants.inputWidth, (int)shader.constants.inputHeight);
    }

    [D2DInputCount(1)]
    [D2DInputComplex(0)]
    [D2DRequiresScenePosition]
    [D2DEmbeddedBytecode(D2D1ShaderProfile.PixelShader40)]
    [AutoConstructor]
    public partial struct Shader
        : ID2D1PixelShader
    {
        [AutoConstructor]
        public partial struct Constants
        {
            public uint inputWidth;
            public uint inputHeight;
            public float2 startOffset;
            public float2 endOffset;
            public uint maxSamples;

            public SizeF StartOffsetAsSizeF => Unsafe.As<float2, SizeF>(ref startOffset);

            public SizeF EndOffsetAsSizeF => Unsafe.As<float2, SizeF>(ref endOffset);
        }

        public readonly Constants constants;

        public float4 Execute()
        {
            float2 scenePos = D2D.GetScenePosition().XY;

            float inputWidthF = (float)this.constants.inputWidth;
            float inputHeightF = (float)this.constants.inputHeight;
            float tDenominator = (float)(this.constants.maxSamples - 1);

            uint samples = 0;
            float4 sampleAccumulator = 0;

            for (uint si = 0; si < this.constants.maxSamples; ++si)
            {
                float t = (float)si / tDenominator;
                float2 sampleOffset = Hlsl.Lerp(this.constants.startOffset, this.constants.endOffset, t);
                float2 samplePos = scenePos + sampleOffset;

                if (samplePos.X >= 0 &&
                    samplePos.Y >= 0 &&
                    samplePos.X < inputWidthF &&
                    samplePos.Y < inputHeightF)
                {
                    float4 sampleColor = D2D.SampleInputAtOffset(0, sampleOffset);

                    sampleAccumulator += sampleColor;

                    ++samples;
                }
            }

            float4 result = sampleAccumulator / (float)samples;

            return result;
        }
    }
}