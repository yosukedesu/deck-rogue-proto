// WarmSpark397Surface.shader — dot の Godot 版 Project422 shaders/authored_WarmSpark397Surface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/WarmSpark397Surface" {
 Properties { _Kind("Core / flecks",Float)=0 _Family("Family",Float)=1 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "WarmSpark397SurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _Kind,_Family;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    //397 exact three-card warm ember material; analytic, not a bitmap surrogate.
    float4 tint=input_color;
    float u=clamp(input_uv.x,0.0,1.0),v=clamp(input_uv.y,0.0,1.0);
    // Finite tapered mesh owns the silhouette. Only a narrow edge softening;
    // no halo/white plate that competes with the unchanged actor hit flash.
    float edge=smoothstep(0.0,.11,min(v,1.0-v));
    float hot=smoothstep(.44,.90,u)*(.50+.50*(1.0-abs(2.0*v-1.0)));
    float3 ember=lerp(float3(.34,.075,.012),float3(.88,.39,.065),smoothstep(.06,.48,u));
    ember=lerp(ember,float3(1.0,.78,.34),hot*.78);
    if(_Kind<.5)ember=lerp(float3(.62,.22,.027),float3(.98,.70,.26),hot*.55);
    float a=clamp(tint.a,0.0,1.0)*edge;
    return float4(ember*max(tint.r,0.0)*a,a);
   }
   ENDHLSL
  }
 }
}
