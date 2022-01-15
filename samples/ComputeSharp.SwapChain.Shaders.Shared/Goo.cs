namespace ComputeSharp.SwapChain.Shaders;

/// <summary>
/// A quick one evening doodle with semi-translucent material shading.
/// Ported from <see href="https://www.shadertoy.com/view/lllBDM"/>.
/// <para>Created by nobi.</para>
/// <para>MIT License: <see href="https://opensource.org/licenses/MIT"/>.</para>
/// </summary>
internal static partial class Goo
{
    /// <summary>
    /// First shader pass, writes to buffer A.
    /// </summary>
    [AutoConstructor]
#if SAMPLE_APP
    [EmbeddedBytecode(DispatchAxis.XY)]
#endif
    public readonly partial struct A : IPixelShader<float4>
    {
        /// <summary>
        /// The current time since the start of the application.
        /// </summary>
        public readonly float time;

        const float pi = 3.14159f;

        private static float3x3 Rotate(in float3 v, in float angle)
        {
            float c = Hlsl.Cos(angle);
            float s = Hlsl.Sin(angle);

            return new(
                c + (1.0f - c) * v.X * v.X,
                (1.0f - c) * v.X * v.Y - s * v.Z,
                (1.0f - c) * v.X * v.Z + s * v.Y,
                (1.0f - c) * v.X * v.Y + s * v.Z,
                c + (1.0f - c) * v.Y * v.Y,
                (1.0f - c) * v.Y * v.Z - s * v.X,
                (1.0f - c) * v.X * v.Z - s * v.Y,
                (1.0f - c) * v.Y * v.Z + s * v.X,
                c + (1.0f - c) * v.Z * v.Z);
        }

        private static float3 hash(float3 p)
        {
            p = new(
                Hlsl.Dot(p, new(127.1f, 311.7f, 74.7f)),
                Hlsl.Dot(p, new(269.5f, 183.3f, 246.1f)),
                Hlsl.Dot(p, new(113.5f, 271.9f, 124.6f)));

            return -1.0f + 2.0f * Hlsl.Frac(Hlsl.Sin(p) * 43758.5453123f);
        }

        private static float4 noised(float3 x)
        {
            float3 p = Hlsl.Floor(x);
            float3 w = Hlsl.Frac(x);
            float3 u = w * w * w * (w * (w * 6.0f - 15.0f) + 10.0f);
            float3 du = 30.0f * w * w * (w * (w - 2.0f) + 1.0f);

            float3 ga = hash(p + new float3(0.0f, 0.0f, 0.0f));
            float3 gb = hash(p + new float3(1.0f, 0.0f, 0.0f));
            float3 gc = hash(p + new float3(0.0f, 1.0f, 0.0f));
            float3 gd = hash(p + new float3(1.0f, 1.0f, 0.0f));
            float3 ge = hash(p + new float3(0.0f, 0.0f, 1.0f));
            float3 gf = hash(p + new float3(1.0f, 0.0f, 1.0f));
            float3 gg = hash(p + new float3(0.0f, 1.0f, 1.0f));
            float3 gh = hash(p + new float3(1.0f, 1.0f, 1.0f));

            float va = Hlsl.Dot(ga, w - new float3(0.0f, 0.0f, 0.0f));
            float vb = Hlsl.Dot(gb, w - new float3(1.0f, 0.0f, 0.0f));
            float vc = Hlsl.Dot(gc, w - new float3(0.0f, 1.0f, 0.0f));
            float vd = Hlsl.Dot(gd, w - new float3(1.0f, 1.0f, 0.0f));
            float ve = Hlsl.Dot(ge, w - new float3(0.0f, 0.0f, 1.0f));
            float vf = Hlsl.Dot(gf, w - new float3(1.0f, 0.0f, 1.0f));
            float vg = Hlsl.Dot(gg, w - new float3(0.0f, 1.0f, 1.0f));
            float vh = Hlsl.Dot(gh, w - new float3(1.0f, 1.0f, 1.0f));

            return new(
                va +
                u.X * (vb - va) + u.Y * (vc - va) +
                u.Z * (ve - va) + u.X * u.Y * (va - vb - vc + vd) +
                u.Y * u.Z * (va - vc - ve + vg) +
                u.Z * u.X * (va - vb - ve + vf) +
                (-va + vb + vc - vd + ve - vf - vg + vh) * u.X * u.Y * u.Z,
                ga +
                u.X * (gb - ga) +
                u.Y * (gc - ga) +
                u.Z * (ge - ga) +
                u.X * u.Y * (ga - gb - gc + gd) +
                u.Y * u.Z * (ga - gc - ge + gg) +
                u.Z * u.X * (ga - gb - ge + gf) +
                (-ga + gb + gc - gd + ge - gf - gg + gh) * u.X * u.Y * u.Z +
                du * (new float3(vb, vc, ve) - va +
                u.YZX * new float3(va - vb - vc + vd, va - vc - ve + vg, va - vb - ve + vf) +
                u.ZXY * new float3(va - vb - ve + vf, va - vb - vc + vd, va - vc - ve + vg) +
                u.YZX * u.ZXY * (-va + vb + vc - vd + ve - vf - vg + vh)));
        }

        private float map(float3 p)
        {
            float d = p.Y;
            float c = Hlsl.Max(0.0f, Hlsl.Pow(Hlsl.Distance(p.XZ, new float2(0, 16)), 1.0f));
            float cc = Hlsl.Pow(Hlsl.SmoothStep(20.0f, 5.0f, c), 2.0f);
            float4 n = noised(new float3(p.XZ * 0.07f, time * 0.5f));
            float nn = n.X * (Hlsl.Length((n.YZW)));

            n = noised(new float3(p.XZ * 0.173f, time * 0.639f));
            nn += 0.25f * n.X * (Hlsl.Length((n.YZW)));
            nn = Hlsl.SmoothStep(-0.5f, 0.5f, nn);
            d -= 6.0f * nn * (cc);

            return d;
        }

        private static float err(float dist)
        {
            dist /= 100.0f;

            return Hlsl.Min(0.01f, dist * dist);
        }

        private float3 dr(float3 origin, float3 direction, float3 position)
        {
            const int iterations = 3;

            for (int i = 0; i < iterations; i++)
            {
                position = position + direction * (map(position) - err(Hlsl.Distance(origin, position)));
            }

            return position;
        }

        private float3 intersect(float3 ro, float3 rd)
        {
            float3 p = ro + rd;
            float t = 0.0f;

            for (int i = 0; i < 150; i++)
            {
                float d = 0.5f * map(p);

                t += d;
                p += rd * d;

                if (d < 0.01f || t > 60.0f) break;
            }

            p = dr(ro, rd, p);

            return p;
        }

        private float3 normal(float3 p)
        {
            float e = 0.01f;

            return Hlsl.Normalize(new float3(
                map(p + new float3(e, 0, 0)) - map(p - new float3(e, 0, 0)),
                map(p + new float3(0, e, 0)) - map(p - new float3(0, e, 0)),
                map(p + new float3(0, 0, e)) - map(p - new float3(0, 0, e))));
        }

        private static float G1V(float dnv, float k)
        {
            return 1.0f / (dnv * (1.0f - k) + k);
        }

        private float ggx(float3 n, float3 v, float3 l, float rough, float f0)
        {
            float alpha = rough * rough;
            float3 h = Hlsl.Normalize(v + l);
            float dnl = Hlsl.Clamp(Hlsl.Dot(n, l), 0.0f, 1.0f);
            float dnv = Hlsl.Clamp(Hlsl.Dot(n, v), 0.0f, 1.0f);
            float dnh = Hlsl.Clamp(Hlsl.Dot(n, h), 0.0f, 1.0f);
            float dlh = Hlsl.Clamp(Hlsl.Dot(l, h), 0.0f, 1.0f);
            float f, d, vis;
            float asqr = alpha * alpha;
            const float pi = 3.14159f;
            float den = dnh * dnh * (asqr - 1.0f) + 1.0f;

            d = asqr / (pi * den * den);
            dlh = Hlsl.Pow(1.0f - dlh, 5.0f);
            f = f0 + (1.0f - f0) * dlh;

            float k = alpha / 1.0f;

            vis = G1V(dnl, k) * G1V(dnv, k);

            float spec = dnl * d * f * vis;

            return spec;
        }

        float subsurface(float3 p, float3 v, float3 n)
        {
            float3 d = Hlsl.Refract(v, n, 1.0f / 1.5f);
            float3 o = p;
            float a = 0.0f;
            const float max_scatter = 2.5f;

            for (float i = 0.1f; i < max_scatter; i += 0.2f)
            {
                o += i * d;

                float t = map(o);

                a += t;
            }

            float thickness = Hlsl.Max(0.0f, -a);
            const float scatter_strength = 16.0f;

            return scatter_strength * Hlsl.Pow(max_scatter * 0.5f, 3.0f) / thickness;
        }

        private float3 shade(float3 p, float3 v)
        {
            float3 lp = new(50, 20, 10);
            float3 ld = Hlsl.Normalize(p + lp);
            float3 n = normal(p);
            float fresnel = Hlsl.Pow(Hlsl.Max(0.0f, 1.0f + Hlsl.Dot(n, v)), 5.0f);
            float3 ambient = new(0.1f, 0.06f, 0.035f);
            float3 albedo = new(0.75f, 0.9f, 0.35f);
            float3 sky = new float3(0.5f, 0.65f, 0.8f) * 2.0f;
            float lamb = Hlsl.Max(0.0f, Hlsl.Dot(n, ld));
            float spec = ggx(n, v, ld, 3.0f, fresnel);
            float ss = Hlsl.Max(0.0f, subsurface(p, v, n));

            lamb = Hlsl.Lerp(lamb, 3.5f * Hlsl.SmoothStep(0.0f, 2.0f, Hlsl.Pow(ss, 0.6f)), 0.7f);

            float3 final = ambient + albedo * lamb + 25.0f * spec + fresnel * sky;

            return final * 0.5f;
        }

        public float4 Execute()
        {
            float2 uv = (ThreadIds.XY - (float2)DispatchSize.XY * 0.5f) / DispatchSize.Y;
            float3 a = float3.Zero;

            const float campos = 5.1f;
            float lerp = 0.5f + 0.5f * Hlsl.Cos(campos * 0.4f - pi);

            lerp = Hlsl.SmoothStep(0.13f, 1.0f, lerp);

            float3 c = Hlsl.Lerp(new float3(-0, 217, 0), new float3(0, 4.4f, -190), Hlsl.Pow(lerp, 1.0f));
            float3x3 rot = Rotate(new float3(1, 0, 0), pi / 2.0f);
            float3x3 ro2 = Rotate(new float3(1, 0, 0), -0.008f * pi / 2.0f);
            float2 u2 = -1.0f + 2.0f * uv;

            u2.X *= DispatchSize.X / (float)DispatchSize.Y;

            float3 d = Hlsl.Lerp(
                Hlsl.Normalize(Hlsl.Mul(new float3(u2, 20), rot)),
                Hlsl.Mul(Hlsl.Normalize(new float3(u2, 20)), ro2),
                Hlsl.Pow(lerp, 1.11f));

            d = Hlsl.Normalize(d);

            float3 ii = intersect(c + 145.0f * d, d);
            float3 ss = shade(ii, d);

            a += ss;

            return new(a * (0.99f + 0.02f * hash(new float3(uv, 0.001f * time))), 1.0f);
        }
    }

    /// <summary>
    /// Second shader pass, takes buffer A and writes to buffer B.
    /// </summary>
    [AutoConstructor]
#if SAMPLE_APP
    [EmbeddedBytecode(DispatchAxis.XY)]
#endif
    public readonly partial struct B : IPixelShader<float4>
    {
        private readonly IReadOnlyTexture2D<float4> texture;

        public float4 Execute()
        {
            float2 fragCoord = new(ThreadIds.X, DispatchSize.Y - ThreadIds.Y);
            float2 pp = 1.0f / (float2)DispatchSize.XY;
            float4 color = texture[fragCoord.XY * pp];
            float3 luma = new(0.299f, 0.587f, 0.114f);

            float lumaNW = Hlsl.Dot(texture[(fragCoord + new float2(-1.0f, -1.0f)) * pp].XYZ, luma);
            float lumaNE = Hlsl.Dot(texture[(fragCoord + new float2(1.0f, -1.0f)) * pp].XYZ, luma);
            float lumaSW = Hlsl.Dot(texture[(fragCoord + new float2(-1.0f, 1.0f)) * pp].XYZ, luma);
            float lumaSE = Hlsl.Dot(texture[(fragCoord + new float2(1.0f, 1.0f)) * pp].XYZ, luma);
            float lumaM = Hlsl.Dot(color.XYZ, luma);
            float lumaMin = Hlsl.Min(lumaM, Hlsl.Min(Hlsl.Min(lumaNW, lumaNE), Hlsl.Min(lumaSW, lumaSE)));
            float lumaMax = Hlsl.Max(lumaM, Hlsl.Max(Hlsl.Max(lumaNW, lumaNE), Hlsl.Max(lumaSW, lumaSE)));

            float2 dir = new(-((lumaNW + lumaNE) - (lumaSW + lumaSE)), ((lumaNW + lumaSW) - (lumaNE + lumaSE)));

            float dirReduce = Hlsl.Max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25f * (1.0f / 8.0f)), (1.0f / 128.0f));
            float rcpDirMin = 2.5f / (Hlsl.Min(Hlsl.Abs(dir.X), Hlsl.Abs(dir.Y)) + dirReduce);

            dir = Hlsl.Min(new float2(8.0f, 8.0f), Hlsl.Max(new float2(-8.0f, -8.0f), dir * rcpDirMin)) * pp;

            float3 rgbA = 0.5f * (
                texture[fragCoord * pp + dir * (1.0f / 3.0f - 0.5f)].XYZ +
                texture[fragCoord * pp + dir * (2.0f / 3.0f - 0.5f)].XYZ);

            float3 rgbB = rgbA * 0.5f + 0.25f * (
                texture[fragCoord * pp + dir * -0.5f].XYZ +
                texture[fragCoord * pp + dir * 0.5f].XYZ);

            float lumaB = Hlsl.Dot(rgbB, luma);
            float4 fragColor;

            if ((lumaB < lumaMin) || (lumaB > lumaMax))
            {
                fragColor = new float4(rgbA, color.W);
            }
            else
            {
                fragColor = new float4(rgbB, color.W);
            }

            return fragColor;
        }
    }

    /// <summary>
    /// Final shader pass, takes buffer B and writes to buffer C.
    /// </summary>
    [AutoConstructor]
#if SAMPLE_APP
    [EmbeddedBytecode(DispatchAxis.XY)]
#endif
    public readonly partial struct C : IPixelShader<float4>
    {
        /// <summary>
        /// The current time since the start of the application.
        /// </summary>
        public readonly float time;

        private readonly IReadOnlyTexture2D<float4> texture;

        // Tone mapping and post processing
        private static float hash(float c)
        {
            return Hlsl.Frac(Hlsl.Sin(c * 12.9898f) * 43758.5453f);
        }

        // linear white point
        const float W = 1.2f;
        const float T2 = 7.5f;

        float filmic_reinhard_curve(float x)
        {
            float q = (T2 * T2 + 1.0f) * x * x;

            return q / (q + x + T2 * T2);
        }

        float3 filmic_reinhard(float3 x)
        {
            float w = filmic_reinhard_curve(W);

            return new float3(
                filmic_reinhard_curve(x.R),
                filmic_reinhard_curve(x.G),
                filmic_reinhard_curve(x.B)) / w;
        }

        const int N = 8;

        float3 ca(float2 UV, float4 sampl)
        {
            float2 uv = 1.0f - 2.0f * UV;
            float3 c = 0;

            float rf = 1.0f;
            float gf = 1.0f;
            float bf = 1.0f;
            float f = 1.0f / N;

            for (int i = 0; i < N; ++i)
            {
                c.R += f * texture[0.5f - 0.5f * (uv * rf)].R;
                c.G += f * texture[0.5f - 0.5f * (uv * gf)].G;
                c.B += f * texture[0.5f - 0.5f * (uv * bf)].B;

                rf *= 0.9972f;
                gf *= 0.998f;
                bf /= 0.9988f;

                c = Hlsl.Clamp(c, 0.0f, 1.0f);
            }
            return c;
        }

        public float4 Execute()
        {
            float2 fragCoord = new(ThreadIds.X, DispatchSize.Y - ThreadIds.Y);
            const float brightness = 1.0f;
            float2 pp = fragCoord / DispatchSize.XY;
            float2 r = DispatchSize.XY;
            float2 p = 1.0f - 2.0f * fragCoord / r.XY;

            p.Y *= r.Y / r.X;

            // a little chromatic aberration
            float4 sampl = texture[pp];
            float3 color = ca(pp, sampl);

            // final output
            float vignette = 1.25f / (1.1f + 1.1f * Hlsl.Dot(p, p));

            vignette *= vignette;
            vignette = Hlsl.Lerp(1.0f, Hlsl.SmoothStep(0.1f, 1.1f, vignette), 0.25f);

            float noise = 0.012f * hash(Hlsl.Length(p) * time);

            color = color * vignette + noise;
            color = filmic_reinhard(brightness * color);
            color = Hlsl.SmoothStep(-0.025f, 1.0f, color);
            color = Hlsl.Pow(color, 1.0f / 2.2f);

            return new(color, 1.0f);
        }
    }
}
