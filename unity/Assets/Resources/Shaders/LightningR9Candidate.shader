Shader "DeckRogue/LightningR9Candidate"
{
    Properties { _Emission("Bounded host emission, applied once", Float) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent" }
        Pass
        {
            Name "LightningR9Additive"
            Tags { "LightMode"="UniversalForward" }
            // Background + emitted radiance. Coverage is already applied exactly once below.
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            ColorMask RGB
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
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
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position.xyz);
                output.profile = input.profile; output.energy = input.energy; output.source = input.source;
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
            float4 frag(Varyings input) : SV_Target
            {
                // Fixed topology retains inactive slots; reject before any erf/exp field work.
                if (all(input.energy <= 0.0)) return float4(0,0,0,0);
                // Additive RGB output has no absorption. Never multiply by opacity a second time.
                return float4(RadianceR9(input.profile, input.energy, input.source) * _Emission, 0.0);
            }
            ENDHLSL
        }
    }
}
