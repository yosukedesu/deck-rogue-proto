Shader "DeckRogue/EarthStompRockR2"
{
    Properties
    {
        _Mode ("Family (0 Earth, 1 Stomp)", Float) = 0
        _RoleShadow ("Linear Shadow Role", Vector) = (0.0666259386438, 0.0466650863369, 0.031896033073, 1)
        _RolePrimary ("Linear Primary Role", Vector) = (0.287440837727, 0.174647403656, 0.0843762115441, 1)
        _RoleEdge ("Linear Edge Role", Vector) = (0.485149940056, 0.327778098057, 0.187820772301, 1)
        _RoleCore ("Linear Core Role", Vector) = (0.693871761292, 0.539479489012, 0.3515325995, 1)
        _Emission ("Edge Emission Multiplier (once)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Lit" }
        Pass
        {
            Name "EarthStompRockLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend One OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS

            // Transparent shadow-coordinate path, but no URP diffuse-only alpha premultiply.
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float4 _RoleShadow;
                float4 _RolePrimary;
                float4 _RoleEdge;
                float4 _RoleCore;
                float _Emission;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                half3 vertexLighting : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                // Host supplies separate outward normals per triangular facet, never smoothed normals.
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.vertexLighting = VertexLighting(output.positionWS, output.normalWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = NormalizeNormalPerPixel(input.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.vertexLighting = input.vertexLighting;
                lighting.bakedGI = SampleSHPixel(half3(0, 0, 0), lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = unity_ProbesOcclusion;

                SurfaceData surface = (SurfaceData)0;
                // Rock RGB is linear host tint (white normally). Geometry Gain=.22 is NOT albedo gain.
                surface.albedo = _RolePrimary.rgb * input.color.rgb;
                surface.metallic = 0;
                surface.specular = half3(0, 0, 0); // Metallic workflow uses URP's dielectric F0.
                surface.smoothness = 0.14; // Frozen perceptual roughness .86.
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                surface.emission = _RoleEdge.rgb * (0.22 * _Emission) * input.color.rgb;
                surface.alpha = 1;
                surface.clearCoatMask = 0;
                surface.clearCoatSmoothness = 0;
                half4 lit = UniversalFragmentPBR(lighting, surface);
                // All lighting, including specular and emission, fades once with authored vertex alpha.
                half alpha = input.color.a;
                return half4(lit.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
