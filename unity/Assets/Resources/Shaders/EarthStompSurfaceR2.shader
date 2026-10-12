Shader "DeckRogue/EarthStompSurfaceR2"
{
    Properties
    {
        [NoScaleOffset] _GrainTex ("Earth RGB Grain (sRGB / Repeat / Bilinear)", 2D) = "gray" {}
        _Age ("Native Visual Age (seconds)", Float) = 0
        _Front ("Earth Reveal Front (Hfx)", Float) = 0
        _Mode ("Family (0 Earth, 1 Stomp)", Float) = 0
        _RoleShadow ("Linear Shadow Role", Vector) = (0.0666259386438, 0.0466650863369, 0.031896033073, 1)
        _RolePrimary ("Linear Primary Role", Vector) = (0.287440837727, 0.174647403656, 0.0843762115441, 1)
        _RoleEdge ("Linear Edge Role", Vector) = (0.485149940056, 0.327778098057, 0.187820772301, 1)
        _RoleCore ("Linear Core Role", Vector) = (0.693871761292, 0.539479489012, 0.3515325995, 1)
        _Kind ("Batch (0 Ground, 1 Dust)", Float) = 0
        _Emission ("Emission Multiplier (once)", Float) = 1
        _Coverage ("Ground Envelope (dust = 1)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "EarthStompSurface"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend One OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GroundSurface.hlsl"

            TEXTURE2D(_GrainTex);
            // Coupled sampler inherits Repeat/Bilinear from the host texture, including GLES.
            // Texture must be imported as sRGB=true. Hardware decodes RGB before filtering.
            SAMPLER(sampler_GrainTex);
            CBUFFER_START(UnityPerMaterial)
                float _Age;
                float _Front;
                float _Mode;
                float4 _RoleShadow;
                float4 _RolePrimary;
                float4 _RoleEdge;
                float4 _RoleCore;
                float _Kind;
                float _Emission;
                float _Coverage;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 phase : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float phase : TEXCOORD1;
                float4 color : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.phase = input.phase.x;
                output.color = input.color;
                return output;
            }
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float alpha;
                float3 rgb;
                if (_Kind > 0.5)
                {
                    // Dust vertex RGB already contains linear edge palette * .65 * host Particles.
                    alpha = DustCoverage(input.uv, input.phase) * input.color.a;
                    rgb = input.color.rgb;
                }
                else
                {
                    // UV0 is exactly x/2.50+.5,z/1.70+.5; do not clamp/reproject.
                    float2 p = (input.uv - 0.5) * float2(2.50, 1.70);
                    float core = 0.4;
                    if (_Mode < 0.5)
                    {
                        // Texture alpha is deliberately ignored; analytic field owns coverage.
                        float3 grainRgb = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, input.uv).rgb;
                        float grain = dot(grainRgb, float3(0.2126, 0.7152, 0.0722));
                        alpha = EarthField(p, _Age, input.color.a, _Front, grain, core);
                    }
                    else
                    {
                        alpha = StompField(p, _Age, input.color.a);
                    }
                    // Ground RGB is linear host tint * surface gain (1.55 Earth / .95 Stomp).
                    rgb = lerp(_RolePrimary.rgb, _RoleCore.rgb, core) * input.color.rgb;
                }
                alpha *= _Coverage;
                // Exactly one coverage premultiply and one global multiplier for both branches.
                return float4(rgb * _Emission * alpha, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
