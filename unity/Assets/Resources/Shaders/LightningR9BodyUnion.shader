Shader "DeckRogue/LightningR9BodyUnion"
{
    Properties { _Emission("Bounded host emission, applied once", Float) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent" }
        Pass
        {
            Name "LightningR9BodyPathUnion"
            Tags { "LightMode"="UniversalForward" }
            // Background + emitted radiance. Coverage is already applied exactly once below.
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            ColorMask RGB
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            // Dense bounded packet: (112 * 5 + 16) float4 = 9216 bytes.
            // Emission occupies one further 16-byte register: 9232-byte owned block.
            // No textures, structured buffers, per-path padding, or second body pass.
            float4 _R9Segment0[112];
            float4 _R9Segment1[112];
            float4 _R9Segment2[112];
            float4 _R9Segment3[112];
            float4 _R9Segment4[112];
            float4 _R9Paths[16];
            float _Emission;
            CBUFFER_END
            // Fixed R9 sRGB palette decoded to linear once offline; no texture or exposure gain.
            static const float3 R9Shadow = float3(0.033104766571,0.035601314875,0.095307466631);
            static const float3 R9Primary = float3(0.287440837727,0.262250657530,0.564711505705);
            static const float3 R9Edge = float3(0.491020849848,0.456411023180,0.768151147248);
            static const float3 R9Core = float3(0.854992608124,0.822785754396,0.955973353249);
            static const float3 R9Violet = float3(0.184474994500,0.130136476690,0.479320183101);
            struct Attributes
            {
                float4 position : POSITION;
                float4 profile : TEXCOORD0;
                float4 energy : TEXCOORD1;
                float4 source : TEXCOORD2;
            };
            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 profile : TEXCOORD0;
                float4 energy : TEXCOORD1;
                float4 source : TEXCOORD2;
                nointerpolation int kind : TEXCOORD3;
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position.xyz);
                output.profile = input.profile; output.energy = input.energy; output.source = input.source;
                output.kind = isfinite(input.profile.w) && input.profile.w >= 0.0 && input.profile.w <= 30.0
                    ? (int)round(input.profile.w) : -1;
                return output;
            }
            float SmoothR9(float value)
            {
                float t = saturate(value); return t * t * (3.0 - 2.0 * t);
            }
            float ErfR9(float value)
            {
                float a = abs(value), t = rcp(1.0 + 0.3275911 * a);
                float result = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * exp(-a * a);
                return value < 0.0 ? -result : result;
            }
            float ProfileR9(float distance, float width, float sigma)
            {
                float inv = rcp(1.4142135623730951 * sigma);
                return max(0.0, 0.5 * (ErfR9((distance + width * 0.5) * inv) - ErfR9((distance - width * 0.5) * inv)));
            }
            float GaussianR9(float2 pixel, float2 center, float2 sigma)
            {
                float2 q = (pixel - center) / sigma; return exp(-0.5 * dot(q, q));
            }
            float3 RadianceR9(float4 profile, float4 energy, float4 source)
            {
                int kind = (int)round(profile.w);
                if (kind == 8 || kind == 9)
                {
                    float2 uv = (profile.xy + 1.0) * 0.5;
                    float window = SmoothR9(uv.x / 0.1) * SmoothR9((1.0 - uv.x) / 0.1) * SmoothR9(uv.y / 0.1) * SmoothR9((1.0 - uv.y) / 0.1);
                    float2 pixel = profile.xy * (kind == 9 ? float2(16.0, -10.0) : float2(62.0, -62.0));
                    float density = 0.0;
                    if (kind == 9)
                        density = GaussianR9(pixel, float2(0,0), float2(3.3,2.0)) + 0.38 * GaussianR9(pixel, float2(-6,-2), float2(4.5,1.8));
                    else
                        density = (0.82 * GaussianR9(pixel,float2(-10,-4),float2(17,13)) + 0.42 * GaussianR9(pixel,float2(18,5),float2(20,8)) + 0.24 * GaussianR9(pixel,float2(4,-23),float2(11,21))) * (1.0 - 0.67 * GaussianR9(pixel,float2(6,7),float2(8,13)));
                    // Density may exceed one. It is an emissive field, never an alpha to clamp.
                    return (kind == 9 ? R9Core : R9Violet) * (energy.x * density * window);
                }
                float d = profile.x, width = profile.z, radius = source.z;
                if (abs(d) >= radius || source.w <= 0.0 || width <= 0.0) return float3(0,0,0);
                float coreCoverage = SmoothR9((width * 0.5 - abs(d)) / max(0.06, width * 0.15)) * SmoothR9(source.w / max(0.1, width));
                float plasma = 0.0, air = 0.0, scatter = 0.0, endWindow = SmoothR9(source.w / 7.0);
                float3 coreColor = R9Core, plasmaColor = R9Primary, airColor = R9Primary;
                if (kind == 0)
                {
                    float w = width * 2.55;
                    plasma = ProfileR9(d,w,0.7) * SmoothR9((radius - abs(d)) / 1.0);
                    float wave = sin(source.x * 0.064 + source.y * 0.041);
                    air = ProfileR9(d,w,5.4) * (0.54 + 0.46 * wave * wave) * SmoothR9((radius - abs(d)) / 3.24);
                    scatter = ProfileR9(d,w,14.0) * SmoothR9((radius - abs(d)) / 8.4);
                }
                else if (kind == 1)
                {
                    coreColor = R9Edge; plasmaColor = R9Violet;
                    plasma = ProfileR9(d,width,2.4) * SmoothR9((radius - abs(d)) / 1.44);
                }
                else if (kind == 2) plasma = ProfileR9(d,width,1.7) * SmoothR9((radius - abs(d)) / 1.02);
                else if (kind == 4)
                {
                    coreColor = R9Edge; airColor = R9Violet;
                    air = ProfileR9(d,width,3.2) * SmoothR9((radius - abs(d)) / 1.92);
                }
                else if (kind == 7)
                {
                    coreColor = R9Primary; airColor = R9Shadow;
                    air = ProfileR9(d,width,2.0) * SmoothR9((radius - abs(d)) / 1.2);
                }
                else if (kind == 5 || kind == 10) coreColor = R9Primary;
                else coreColor = R9Edge;
                return coreColor * (energy.x * coreCoverage)
                    + plasmaColor * (energy.y * plasma * endWindow)
                    + airColor * (energy.z * air * endWindow)
                    + R9Shadow * (energy.w * scatter * endWindow);
            }
            // Return four independent, already-strength-weighted radiance layers.
            // The max is applied to EACH layer within ONE source path, before its
            // fixed palette is applied once. Different path quads remain additive.
            float4 LayersR9(float distance, float width, float radius, float endDistance,
                float2 sourcePixel, int kind, float4 strength)
            {
                if (distance >= radius || endDistance <= 0.0 || width <= 0.0) return float4(0,0,0,0);
                float core = SmoothR9((width * 0.5 - distance) / max(0.06, width * 0.15)) * SmoothR9(endDistance / max(0.1, width));
                float plasma = 0.0, air = 0.0, scatter = 0.0;
                float edgeDistance = radius - distance;
                if (kind == 0)
                {
                    float w = width * 2.55;
                    plasma = ProfileR9(distance, w, 0.7) * SmoothR9(edgeDistance);
                    float wave = sin(sourcePixel.x * 0.064 + sourcePixel.y * 0.041);
                    air = ProfileR9(distance, w, 5.4) * (0.54 + 0.46 * wave * wave) * SmoothR9(edgeDistance / 3.24);
                    scatter = ProfileR9(distance, w, 14.0) * SmoothR9(edgeDistance / 8.4);
                }
                else if (kind == 1) plasma = ProfileR9(distance, width, 2.4) * SmoothR9(edgeDistance / 1.44);
                else if (kind == 2) plasma = ProfileR9(distance, width, 1.7) * SmoothR9(edgeDistance / 1.02);
                else if (kind == 4) air = ProfileR9(distance, width, 3.2) * SmoothR9(edgeDistance / 1.92);
                else if (kind == 7) air = ProfileR9(distance, width, 2.0) * SmoothR9(edgeDistance / 1.2);
                float endWindow = SmoothR9(endDistance / 7.0);
                return strength * float4(core, plasma * endWindow, air * endWindow, scatter * endWindow);
            }
            float SampleWidthR9(float4 widths, float t)
            {
                if (t <= 0.25) return lerp(widths.x, widths.y, t * 4.0);
                if (t <= 0.75) return lerp(widths.y, widths.z, (t - 0.25) * 2.0);
                return lerp(widths.z, widths.w, (t - 0.75) * 4.0);
            }
            float3 PathPaletteR9(float4 layers, int kind)
            {
                float3 core = kind == 0 || kind == 2 ? R9Core : kind == 7 ? R9Primary : R9Edge;
                float3 plasma = kind == 1 ? R9Violet : R9Primary;
                float3 air = kind == 4 ? R9Violet : kind == 7 ? R9Shadow : R9Primary;
                return core * layers.x + plasma * layers.y + air * layers.z + R9Shadow * layers.w;
            }
            float3 PathUnionR9(float2 pixel, int path)
            {
                if (path < 0 || path >= 15 || !all(isfinite(pixel))) return float3(0,0,0);
                float4 header = _R9Paths[path];
                if (!all(isfinite(header)) || header.x < 0.0 || header.x > 111.0 || header.y < 1.0 || header.y > 15.0 ||
                    header.z < 0.0 || header.z > 7.0 || any(header.xyz != round(header.xyz))) return float3(0,0,0);
                int start = (int)round(header.x), count = (int)round(header.y), kind = (int)round(header.z);
                if (start < 0 || count < 1 || count > 15 || start > 112 - count || header.w <= 0.0 ||
                    !(kind == 0 || kind == 1 || kind == 2 || kind == 3 || kind == 4 || kind == 7)) return float3(0,0,0);
                float4 layers = float4(0,0,0,0);
                [loop] for (int segment = 0; segment < 15; segment++)
                {
                    if (segment >= count) break;
                    int index = start + segment;
                    float4 strength = _R9Segment1[index];
                    if (!all(isfinite(strength))) return float3(0,0,0);
                    if (all(strength <= 0.0)) continue;
                    float4 endpoints = _R9Segment0[index], widths = _R9Segment2[index];
                    float4 runAndScale = _R9Segment3[index], source = _R9Segment4[index];
                    if (!all(isfinite(endpoints)) || !all(isfinite(widths)) || !all(isfinite(runAndScale)) || !all(isfinite(source)) ||
                        any(strength < 0.0) || any(widths <= 0.0) || any(runAndScale.xy < 0.0) || any(runAndScale.zw <= 0.0)) return float3(0,0,0);
                    float2 chord = endpoints.zw - endpoints.xy;
                    float lengthSquared = dot(chord, chord);
                    if (!isfinite(lengthSquared) || lengthSquared <= 1e-16) return float3(0,0,0);
                    // Euclidean clamped distance gives round joins. Deliberately do
                    // not reconstruct the old skewed/overlapping wide ribbon mesh.
                    float u = saturate(dot(pixel - endpoints.xy, chord) / lengthSquared);
                    float scale = lerp(runAndScale.z, runAndScale.w, u);
                    if (!isfinite(scale) || scale <= 0.0) return float3(0,0,0);
                    float distance = length(pixel - lerp(endpoints.xy, endpoints.zw, u)) / scale;
                    if (distance >= header.w) continue;
                    // u is a projected-plane parameter. This inverse perspective
                    // ratio recovers source t; equal scales also cover orthographic.
                    float t = saturate(u * runAndScale.w / scale);
                    float sourceLength = length(source.zw - source.xy);
                    if (!isfinite(sourceLength) || sourceLength <= 0.0) return float3(0,0,0);
                    float endDistance = min(runAndScale.x + sourceLength * t, runAndScale.y + sourceLength * (1.0 - t));
                    float width = SampleWidthR9(widths, t);
                    float4 candidate = LayersR9(distance, width, header.w, endDistance, lerp(source.xy, source.zw, t), kind, strength);
                    if (!all(isfinite(candidate))) return float3(0,0,0);
                    layers = max(layers, candidate);
                }
                return PathPaletteR9(layers, kind);
            }
            float4 frag(Varyings input) : SV_Target
            {
                // Fixed topology retains inactive slots; reject before any erf/exp field work.
                if (input.kind < 0 || !all(isfinite(input.profile)) || !all(isfinite(input.energy)) || !all(isfinite(input.source)) ||
                    all(input.energy <= 0.0)) return float4(0,0,0,0);
                // Additive RGB output has no absorption. Never multiply by opacity a second time.
                if (!isfinite(_Emission) || _Emission < 0.0) return float4(0,0,0,0);
                float3 radiance = input.kind >= 16 ? PathUnionR9(input.profile.xy, input.kind - 16)
                    : RadianceR9(input.profile, input.energy, input.source);
                float3 emitted = radiance * _Emission;
                return all(isfinite(emitted)) ? float4(emitted, 0.0) : float4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}
