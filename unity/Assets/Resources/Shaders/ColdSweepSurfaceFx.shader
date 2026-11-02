// ColdSweepSurfaceFx.shader — dot の Godot 版 Project422 shaders/authored_ColdSweepSurfaceFx.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/ColdSweepSurfaceFx" {
 Properties { _Age("Visual age",Float)=0 _BaseMap("Authored density",2D)="white"{} _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "ColdSweepSurfaceFxForward" Tags {"LightMode"="UniversalForward"}
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
   //297: bounded density-following consumption. The initial material is unchanged.
   float ColdConsumption(float2 uv,float4 density,float begin,float end){
    if(_Age<=begin)return 1.0;
    if(_Age>=end)return 0.0;
    float phase=smoothstep(begin,end,_Age);
    float lightness=dot(density.rgb,float3(.2126,.7152,.0722));
    // Authored dark patches go first; irregular pale frost islands persist locally.
    float contour=uv.x+.42*(lightness-.32)+.14*sin(7.0*uv.y+3.0*uv.x);
    float front=lerp(-.32,1.42,phase);
    return smoothstep(front-.045,front+.045,contour);
   }
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    float2 centered=input_uv*2.0-1.0;
    float radial=saturate(1.0-dot(centered,centered));
    float edge=input_effect.x>.5?radial*radial:saturate(min(input_uv.y,1.0-input_uv.y)*12.0);
    float alpha=saturate(input_color.a*edge);
    float3 rgb=lerp(_Primary.rgb,_Core.rgb,radial*.55)*max(0.0,input_color.r);
    // Vertex alpha, edge, gain and depth unchanged. Main coverage additionally uses raw alpha.
    if(input_effect.y>.5 && input_effect.y<1.5){
        // 293: authored irregular cold density, original PNG unchanged.
        float2 image_uv=float2(lerp(.06,.94,input_uv.x),lerp(.12,.87,1.0-input_uv.y));
        float4 frost=TexBaseMap(image_uv);
        rgb=frost.rgb*max(0.0,input_color.r);
        alpha*=frost.a*ColdConsumption(input_uv,frost,.04,.145);
    }else if(input_effect.y>1.5){
        //295: short geometric frost facets keep a broken edge, not the old radial mist mask.
        float2 frost_uv=float2(lerp(.30,.48,input_uv.x),lerp(.44,.64,1.0-input_uv.y));
        float4 flake=TexBaseMap(frost_uv);
        float frost_face=input_effect.y>2.5?.18:.55;
        rgb=lerp(flake.rgb,_Core.rgb,frost_face)*max(0.0,input_color.r);
        alpha=saturate(input_color.a)*flake.a;
        if(input_effect.y<2.5)alpha*=ColdConsumption(input_uv,flake,.04,.105);
        else alpha*=ColdConsumption(input_uv,flake,.085,.175);
    }
    // UV2.y==0 is the original mote color formula above, byte unchanged.
    return float4(rgb*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
