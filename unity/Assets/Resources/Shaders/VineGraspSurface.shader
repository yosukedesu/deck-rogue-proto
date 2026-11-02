// VineGraspSurface.shader — dot の Godot 版 Project422 shaders/authored_VineGraspSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/VineGraspSurface" {
 Properties { _Age("Visual age",Float)=0 _BaseMap("Authored density",2D)="white"{} _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "VineGraspSurfaceForward" Tags {"LightMode"="UniversalForward"}
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
    if(input_effect.y>.5){
        float2 image_uv=lerp(float2(.18,.28),float2(.65,.76),float2(input_uv.x,1.0-input_uv.y));
        float4 density=TexBaseMap(image_uv);
        float edge=smoothstep(0.0,.045,min(min(input_uv.x,1.0-input_uv.x),min(input_uv.y,1.0-input_uv.y)));
        //332 preserve initial331 RGBA then consume outside density toward the true chest root.
        float u=input_uv.x,v=input_uv.y;
        float contour=u+.10*sin(11.0*v+3.0*u)+.12*(density.g-density.r);
        float front=-.30+.78*smoothstep(0.0,1.0/30.0,_Age)+.34*smoothstep(1.0/30.0,2.0/30.0,_Age)+.46*smoothstep(2.0/30.0,.100,_Age);
        float consume=smoothstep(front-.035,front+.035,contour);
        float root=smoothstep(.86,.98,u);
        float remain=lerp(consume,1.0-smoothstep(.070,.150,_Age),root);
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
