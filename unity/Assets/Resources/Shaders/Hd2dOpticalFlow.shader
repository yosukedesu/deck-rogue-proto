// Hd2dOpticalFlow.shader — dot の Godot 版 Project422 shaders/authored_Hd2dOpticalFlow.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は Hd2dPrototype.shader の流儀 (頂点の色 COLOR = 役割の色・線形、TEXCOORD1.x = 明るさ → tint = 色×明るさ = Godot の CUSTOM0)。
// 絵は Godot の画像座標 (左上が原点) の uv で書かれているので、読む時に v を 1-v にする。式と数値は gdshader のまま。
Shader "DeckRogue/Hd2dOpticalFlow" {
 Properties { _BaseMap("Authored density",2D)="white"{} _Age("Visual age",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "Hd2dOpticalFlowForward" Tags {"LightMode"="UniversalForward"}
   Blend One One ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float _Age;
   CBUFFER_END
   float4 TexBaseMap(float2 g){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(g.x,1.0-g.y));}
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 gain:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 tint:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.tint=float4(i.color.rgb*i.gain.x,i.color.a);return o;}
   float4 frag(V i):SV_Target {
    float4 tint=i.tint;float2 UV=i.uv;
    // Exact Torrent body only. Add light locally rather than replacing the scene
    // with a grey alpha veil; world lamps/floor response remain the existing system.
    float4 raw=TexBaseMap(UV);
    float3 linear_rgb=raw.rgb;
    float luminance=dot(linear_rgb,float3(.2126,.7152,.0722));
    float veil=.16+.24*smoothstep(.10,.60,luminance);
    float filament=.80*smoothstep(.60,.92,luminance);
    float local_gain=max(max(tint.r,tint.g),tint.b);
    float3 radiance=linear_rgb*(veil+filament)*local_gain;
    float edge=min(min(UV.x,1.0-UV.x),min(UV.y,1.0-UV.y));
    // Raw owns the interior density; only transparent canvas boundary is softened.
    float alpha=raw.a*tint.a*smoothstep(0.0,.08,edge);
    return float4(radiance*alpha,alpha);   // Godot の blend_add (src*alpha + dst)
   }
   ENDHLSL
  }
 }
}
