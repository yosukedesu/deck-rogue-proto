// WraithCurseSurface.shader — dot の Godot 版 Project422 shaders/authored_WraithCurseSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/WraithCurseSurface" {
 Properties { _Age("Visual age",Float)=0 _BaseMap("Authored density",2D)="white"{} _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "WraithCurseSurfaceForward" Tags {"LightMode"="UniversalForward"}
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
    if(input_effect.y>1.5){
        //310 thin original-contact return uses a middle-purple interior region of the SAME density.
        float u=input_uv.x,v=input_uv.y;
        float4 density=TexBaseMap(lerp(float2(.60,.39),float2(.78,.59),float2(u,1.0-v)));
        float across=1.0-smoothstep(.12,.5,abs(v-.5));
        float remain=1.0-smoothstep(.105,.175,_Age);
        float alpha3=clamp(input_color.a*density.a*across*remain,0.0,1.0);
        float3 rgb3=density.rgb*.45*max(0.0,input_color.r);
        return float4(rgb3*alpha3,alpha3);
    }
    if(input_effect.y>.5){
        float4 density=TexBaseMap(lerp(float2(.23,.22),float2(.83,.80),float2(input_uv.x,1.0-input_uv.y)));
        float u=input_uv.x,v=input_uv.y;
        //312 preserve311 initial coverage, then consume the outside belly within the ACTUAL .18s window.
        float contour=u+.12*sin(9.0*v+2.0*u)+.20*(density.g-density.r);
        float initialCoverage=smoothstep(-.385,-.315,contour);
        float front=-.40+.75*smoothstep(0.0,1.0/30.0,_Age)+.30*smoothstep(1.0/30.0,2.0/30.0,_Age)+.75*smoothstep(2.0/30.0,.100,_Age);
        float consume=smoothstep(front-.035,front+.035,contour);
        float root=smoothstep(.78,.94,u);
        float remain=initialCoverage*lerp(consume,1.0-smoothstep(.070,.150,_Age),root);
        float edge=smoothstep(0.0,.035,min(min(u,1.0-u),min(v,1.0-v)));
        float alpha2=clamp(input_color.a*density.a*edge*remain,0.0,1.0);
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
