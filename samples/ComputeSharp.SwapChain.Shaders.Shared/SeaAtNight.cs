namespace ComputeSharp.SwapChain.Shaders;

/// <summary>
/// A shader creating an Hlsl.Abstract and colorful animation.
/// Ported from <see href="https://www.shadertoy.com/view/ssG3Wt"/>.
/// <para>Created by by Bruno Croci (https://bruno.croci.me/).</para>
/// <para>License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.</para>
/// </summary>
internal sealed partial class SeaAtNight
{
    // Float hash
    private static float hash1(float n)
    {
        return Hlsl.Frac(n * 17.0f * Hlsl.Frac(n * 0.3183099f));
    }

    // 3D noise
    private static float noise(in float3 x)
    {
        float3 p = Hlsl.Floor(x);
        float3 w = Hlsl.Frac(x);

        float3 u = w * w * w * (w * (w * 6.0f - 15.0f) + 10.0f);

        float n = p.X + 317.0f * p.Y + 157.0f * p.Z;

        float a = hash1(n + 0.0f);
        float b = hash1(n + 1.0f);
        float c = hash1(n + 317.0f);
        float d = hash1(n + 318.0f);
        float e = hash1(n + 157.0f);
        float f = hash1(n + 158.0f);
        float g = hash1(n + 474.0f);
        float h = hash1(n + 475.0f);

        float k0 = a;
        float k1 = b - a;
        float k2 = c - a;
        float k3 = e - a;
        float k4 = a - b - c + d;
        float k5 = a - c - e + g;
        float k6 = a - b - e + f;
        float k7 = -a + b + c - d + e - f - g + h;

        return -1.0f + 2.0f * (k0 + k1 * u.X + k2 * u.Y + k3 * u.Z + k4 * u.X * u.Y + k5 * u.Y * u.Z + k6 * u.Z * u.X + k7 * u.X * u.Y * u.Z);
    }

    private static float fbm_2(float3 x)
    {
        float f = 2.0f;
        float s = 0.5f;
        float a = 0.0f;
        float b = 0.5f;

        float3x3 m3 = new(0.00f, 0.80f, 0.60f, -0.80f, 0.36f, -0.48f, -0.60f, -0.48f, 0.64f);

        for (int i = 0; i < 2; i++)
        {
            float n = noise(x);

            a += b * n;
            b *= s;
            x = Hlsl.Mul(f * m3, x);
        }

        return a;
    }

    private static float2 wavedx(float2 position, float2 direction, float speed, float frequency, float timeshift)
    {
        float x = Hlsl.Dot(direction, position) * frequency + timeshift * speed;
        float wave = Hlsl.Exp(Hlsl.Sin(x) - 1.0f);
        float dx = wave * Hlsl.Cos(x);

        return new(wave, -dx);
    }

    private const float DRAG_MULT = 0.048f;

    private static float getwaves(float2 position, int iterations, float time)
    {
        float iter = 0.0f;
        float phase = 6.0f;
        float speed = 2.0f;
        float weight = 1.0f;
        float w = 0.0f;
        float ws = 0.0f;

        for (int i = 0; i < iterations; i++)
        {
            float2 p = new(Hlsl.Sin(iter), Hlsl.Cos(iter));
            float2 res = wavedx(position, p, speed, phase, time);

            position += Hlsl.Normalize(p) * res.Y * weight * DRAG_MULT;
            w += res.X * weight;
            iter += 12.0f;
            ws += weight;
            weight = Hlsl.Lerp(weight, 0.0f, 0.2f);
            phase *= 1.18f;
            speed *= 1.07f;
        }

        return w / ws;
    }

    [AutoConstructor]
    public partial struct BufferA : IPixelShader<float4>
    {
        private int ZERO() => Hlsl.Min(iFrame, 0);

        private const int MAX_STEPS = 200;
        private const float MAX_DIST = 25.0f;
        private const float SURFACE_DIST = 0.001f;

        /// <summary>
        /// The current time since the start of the application.
        /// </summary>
        public readonly float iTime;

        /// <summary>
        /// The shader playback frame counter.
        /// </summary>
        public readonly int iFrame;

        private static float3 ro = default;

        private static float clamp01(float x)
        {
            return Hlsl.Max(Hlsl.Min(x, 1.0f), 0.0f);
        }

        private float2 map(float3 p, bool complete)
        {
            float2 v = new(MAX_DIST, 0.0f);

            // water
            float final = getwaves(p.XZ * 0.35f, 20, iTime * 0.5f) * (getwaves(p.XZ * 0.15f + new float2(2.2f, 2.2f), 3, iTime * 0.5f) * 1.5f + 0.4f) * 1.05f;
            float f = Hlsl.Dot(p, new float3(0.0f, 1.0f, 0.0f)) - final;

            v = new float2(f, 1.0f);

            return v;
        }

        private float3 calcNormal(float3 p)
        {
            float3 n = default;

            for (int i = ZERO(); i < 4; i++)
            {
                float3 e = 0.5773f * (2.0f * new float3((((i + 3) >> 1) & 1), ((i >> 1) & 1), (i & 1)) - 1.0f);

                n += e * map(p + 0.0001f * e, true).X;
            }

            return Hlsl.Normalize(n);
        }

        private float2 rayMarch(float3 ro, float3 rd)
        {
            float t = 0.0f;
            float3 p;
            float2 obj = default;

            for (int i = 0; i < MAX_STEPS; i++)
            {
                p = ro + t * rd;

                obj = map(p, true);

                if (Hlsl.Abs(obj.X) < SURFACE_DIST || Hlsl.Abs(t) > MAX_DIST) break;

                t += obj.X;
            }

            obj.X = t;

            return obj;
        }

        private float getVisibility(float3 p0, float3 p1, float k)
        {
            float3 rd = Hlsl.Normalize(p1 - p0);
            float t = 10.0f * SURFACE_DIST;
            float maxt = Hlsl.Length(p1 - p0);
            float f = 1.0f;

            while (t < maxt || t < MAX_DIST)
            {
                float2 o = map(p0 + rd * t, false);

                if (o.X < SURFACE_DIST)
                {
                    return 0.0f;
                }

                f = Hlsl.Min(f, k * o.X / t);
                t += o.X;
            }

            return f;
        }

        private float4 render(float2 obj, float3 p, float3 rd, float2 uv)
        {
            float3 col = default;

            float3 normal = calcNormal(p);

            float3 background_color = new(0.0f, 0.01f, 0.02f);
            float3 background = background_color;

            float2 pos = uv - new float2(0.0f, 0.2f) - new float2(0.0f, 0.2f) * Hlsl.Sin(iTime * 0.5f) * 0.1f;

            background += Hlsl.Pow(clamp01(1.0f - Hlsl.Length(pos * 1.5f)), 1.9f) * background * 20.0f;
            background += Hlsl.Pow(clamp01(1.0f - Hlsl.Length(pos * 6.5f)), 3.9f) * background * 80.0f;

            float n = fbm_2(new float3(pos * 52.0f + iTime * 0.1f, 1.0f)) * 1.8f;

            n = Hlsl.SmoothStep(0.72f, 0.78f, n) * 8.5f;
            background += n * background_color;

            float c = 1.0f;

            if (obj.X >= MAX_DIST)
            {
                col = background;
            }
            else
            {
                float3 albedo = new(0.0f, 0.0f, 0.0f);
                float a = Hlsl.Pow(1.0f - Hlsl.Clamp(Hlsl.Dot(-rd, normal), 0.0f, 1.0f), 2.6f);
                float m = Hlsl.Pow(Hlsl.Length(ro - p) * 0.2f, 1.4f) * 0.8f;

                c = Hlsl.Pow(clamp01(1.0f - Hlsl.Length((uv - new float2(0.0f, -0.4f)) * 0.4f)), 5.0f) * 3.0f;

                float diff_mask = a * m * c;
                float ambient_mask = a * m + 0.06f;

                albedo = new float3(0.0f, 0.044f, 0.09f) * 10.0f;

                float spec_power = 80.0f;
                float spec_mask = 6.7f * m;

                // Moon Light
                {
                    float3 light_pos = new(-0.0f, 40.0f, 100.4f);
                    float3 light_col = new(0.2f, 0.2f, 0.2f);
                    float3 refd = Hlsl.Reflect(rd, normal);
                    float3 light_dir = Hlsl.Normalize(light_pos - p);
                    float diffuse = Hlsl.Dot(light_dir, normal);
                    float visibility = getVisibility(p, light_pos, 10.0f);
                    float spec = Hlsl.Pow(Hlsl.Max(0.0f, Hlsl.Dot(refd, light_dir)), spec_power);

                    col += diff_mask * diffuse * albedo * visibility * light_col * 1.86f;
                    col += spec * (light_col * albedo) * spec_mask * visibility * c;
                }

                // Fill Light
                {
                    float3 light_pos = new(0.0f, 100.0f, 0.0f);
                    float3 light_col = new(0.0f, 0.4f, 0.2f);
                    float3 refd = Hlsl.Reflect(rd, normal);
                    float3 light_dir = Hlsl.Normalize(light_pos - p);
                    float diffuse = Hlsl.Dot(light_dir, normal);
                    float visibility = getVisibility(p, light_pos, 10.0f);
                    float spec = Hlsl.Pow(Hlsl.Max(0.0f, Hlsl.Dot(refd, light_dir)), spec_power);

                    col += diff_mask * diffuse * albedo * visibility * light_col * 0.1f;
                    col += spec * (light_col * albedo) * spec_mask * visibility * 0.03f;
                }

                // Ambient light
                col += albedo * 0.2f * ambient_mask;
            }

            return new(col, obj.X);
        }

        /// <inheritdoc/>
        public float4 Execute()
        {
            float2 fragCoord = new(ThreadIds.X, ThreadIds.Y);

            float v = 1.7f + Hlsl.Sin(iTime * 0.5f) * 0.5f;
            float3 ta = new(0.0f, 0.0f, 20.0f);
            float3 ro = new(0.0f, v, 0.0f);
            float4 tot = default;
            float2 uv = (2.0f * fragCoord - DispatchSize.XY) / DispatchSize.Y;

            // Ray direction
            float3 ww = Hlsl.Normalize(ta - ro);
            float3 uu = Hlsl.Normalize(Hlsl.Cross(ww, new float3(0.0f, 1.0f, 0.0f)));
            float3 vv = Hlsl.Normalize(Hlsl.Cross(uu, ww));
            float3 rd = Hlsl.Normalize(uv.X * uu + uv.Y * vv + 2.3f * ww);

            // render	
            float2 obj = rayMarch(ro, rd);
            float3 p = ro + obj.X * rd;

            float4 col = render(obj, p, rd, uv);

            tot += col;

            return tot;
        }
    }

    [AutoConstructor]
    public partial struct Image : IPixelShader<float4>
    {
        private const float DISPLAY_GAMMA = 1.9f;
        private const float GOLDEN_ANGLE = 2.39996323f;
        private const float MAX_BLUR_SIZE = 30.0f;
        private const float RAD_SCALE = 0.5f; // Smaller = nicer blur, larger = faster
        private const float uFar = 12.0f;
        private const float FOCUS_SCALE = 35.0f;

        /// <summary>
        /// The current time since the start of the application.
        /// </summary>
        public readonly float iTime;

        public readonly IReadOnlyTexture2D<float4> iChannel0;

        private static float getBlurSize(float depth, float focusPoint, float focusScale)
        {
            float coc = Hlsl.Clamp((1.0f / focusPoint - 1.0f / depth) * focusScale, -1.0f, 1.0f);

            return Hlsl.Abs(coc) * MAX_BLUR_SIZE;
        }

        private float3 depthOfField(float2 texCoord, float focusPoint, float focusScale, int2 dispatchSize)
        {
            float4 Input = iChannel0[texCoord].RGBA;
            float centerDepth = Input.A * uFar;
            float centerSize = getBlurSize(centerDepth, focusPoint, focusScale);
            float3 color = Input.RGB;
            float tot = 1.0f;
            float2 texelSize = 1.0f / (float2)dispatchSize.XY;
            float radius = RAD_SCALE;

            for (float ang = 0.0f; radius < MAX_BLUR_SIZE; ang += GOLDEN_ANGLE)
            {
                float2 tc = texCoord + new float2(Hlsl.Cos(ang), Hlsl.Sin(ang)) * texelSize * radius;
                float4 sampleInput = iChannel0[tc].RGBA;
                float3 sampleColor = sampleInput.RGB;
                float sampleDepth = sampleInput.A * uFar;
                float sampleSize = getBlurSize(sampleDepth, focusPoint, focusScale);

                if (sampleDepth > centerDepth)
                {
                    sampleSize = Hlsl.Clamp(sampleSize, 0.0f, centerSize * 2.0f);
                }

                float m = Hlsl.SmoothStep(radius - 0.5f, radius + 0.5f, sampleSize);

                color += Hlsl.Lerp(color / tot, sampleColor, m);
                tot += 1.0f;
                radius += RAD_SCALE / radius;
            }

            return color /= tot;
        }

        /// <inheritdoc/>
        public float4 Execute()
        {
            float2 fragCoord = new(ThreadIds.X, ThreadIds.Y);
            float2 uv = fragCoord.XY / DispatchSize.XY;

            float4 color = iChannel0[uv].RGBA;

            float focusPoint = 58.0f - Hlsl.Sin(iTime * 0.3f) * 20.0f;

            color.RGB = depthOfField(uv, focusPoint, FOCUS_SCALE, DispatchSize.XY);

            //tone mapping
            color.RGB = new float3(1.7f, 1.8f, 1.6f) * color.RGB / (1.0f + color.RGB);

            //inverse gamma correction
            return new(Hlsl.Pow(color.RGB, 1.0f / DISPLAY_GAMMA), 1.0f);
        }
    }
}
