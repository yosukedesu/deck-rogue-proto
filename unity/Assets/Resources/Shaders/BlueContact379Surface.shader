// BlueContact379Surface.shader — dot の Godot 版 Project422 shaders/authored_BlueContact379Surface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/BlueContact379Surface" {
 Properties { _Age("Visual age",Float)=0 _BaseMap("Authored density",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "BlueContact379SurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float _Age;
   CBUFFER_END
   float4 TexBaseMap(float2 g){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(g.x,1.0-g.y));}
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    float4 col=input_color;
    float role=input_effect.x;
    float3 rgb;float alpha;
    if(role<.5){
        // Authored water surface interior crop; curved receiver-normalized mesh supplies the cutting silhouette.
        float2 uv=float2(clamp(input_uv.x+_Age*.20,0.0,1.0),lerp(.32,.67,1.0-input_uv.y));
        float4 water=TexBaseMap(uv);
        float edge=smoothstep(0.0,.10,input_uv.y)*(1.0-smoothstep(.88,1.0,input_uv.y));
        float foam=pow(max(0.0,(water.r+water.g+water.b)/3.0),1.4);
        rgb=water.rgb*float3(.75,1.12,1.22)+float3(.10,.22,.26)*foam;
        alpha=water.a*edge*col.a;
    } else if(role<1.5){
        float2 p=input_uv*2.0-1.0;
        float sphere=max(0.0,1.0-dot(p,p));
        rgb=lerp(float3(.015,.19,.32),float3(.50,.85,.93),sphere*sphere);
        alpha=smoothstep(0.0,.35,sphere)*col.a;
    } else {
        float4 ice=TexBaseMap(float2(input_uv.x,1.0-input_uv.y));
        float facet=clamp((role-3.0)*2.0,0.0,.40);
        // Continuous density across irregular cells. No per-triangle UV rim or radial icon lines.
        rgb=ice.rgb*lerp(float3(.30,.70,.91),float3(.58,.90,1.0),facet);
        float vein=smoothstep(.50,.83,max(ice.r,max(ice.g,ice.b)));
        rgb+=float3(.025,.06,.07)*vein;
        alpha=ice.a*col.a;
    }
    rgb*=max(col.r,.72);
    alpha=clamp(alpha,0.0,.96);
    return float4(rgb*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
