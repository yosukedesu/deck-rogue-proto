// MomentumWhipContactSurface.shader — dot の Godot 版 Project422 shaders/authored_MomentumWhipContactSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/MomentumWhipContactSurface" {
 Properties { _Age("Visual age",Float)=0 _BaseMap("Authored density",2D)="white"{} _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "MomentumWhipContactSurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _Primary,_Core;float _Age;
   CBUFFER_END
   float4 TexBaseMap(float2 g){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(g.x,1.0-g.y));}
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    //356 authored pressure only; old role1 motes follow byte-identical generic branch.
    if(input_effect.x>70.5){
        float u=input_uv.x,v=1.0-input_uv.y;
        float4 density=TexBaseMap(float2(1605.0*u/1619.0,(12.0+959.0*v)/971.0));
        float ax=(u-.56)/.22,ay=(v-.32)/.34,bx=(u-.29)/.17,by=(v-.77)/.25;
        float a=exp(-(ax*ax+ay*ay)),b=exp(-(bx*bx+by*by));
        float death=clamp(.065+.065*u-.026*a-.014*b,.067,.150);
        float remain=1.0-smoothstep(death-.012,death,_Age);
        float alpha2=clamp(input_color.a*density.a*remain,0.0,1.0);
        float3 rgb2=density.rgb*max(0.0,input_color.r);
        return float4(rgb2*alpha2,alpha2);
    }
    float2 centered=input_uv*2.0-1.0;
    float radial=saturate(1.0-dot(centered,centered));
    float edge=input_effect.x>.5?radial*radial:saturate(min(input_uv.y,1.0-input_uv.y)*12.0);
    float alpha=saturate(input_color.a*edge);
    float3 rgb=lerp(_Primary.rgb,_Core.rgb,radial*.55)*max(0.0,input_color.r);
    return float4(rgb*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
