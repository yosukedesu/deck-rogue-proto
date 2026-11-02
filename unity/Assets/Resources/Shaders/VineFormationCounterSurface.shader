// VineFormationCounterSurface.shader — dot の Godot 版 Project422 shaders/authored_VineFormationCounterSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/VineFormationCounterSurface" {
 Properties { _Age("Visual age",Float)=0 _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "VineFormationCounterSurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Primary,_Core;float _Age;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    //Private346 pressure roles; old mote role1 falls through byte-identical bounded formula.
    if(input_effect.x>40.5){
        float gain=max(0.0,input_color.r);float alpha=input_color.a;float3 rgb;
        float age=max(0.0,_Age);
        if(input_effect.x<41.5){
            float u=clamp(input_effect.y,0.0,1.0),v=clamp(input_uv.y,0.0,1.0);
            // One broad low-frequency belly, asymmetric across its curve; no repeated fibres/parallel bars.
            float cross_distance=(v-.57)/.42;float across=exp(-cross_distance*cross_distance);
            float belly_distance=(u-.43)/.24;float belly=exp(-belly_distance*belly_distance);
            float density=across*(.48+.52*belly);
            rgb=lerp(float3(.018,.055,.026),float3(.12,.38,.08),density)*gain;
            alpha*=smoothstep(0.0,.16,v)*(1.0-smoothstep(.82,1.0,v))*(.62+.38*belly);
            // A single concave-side bite opens unevenly after impact, then the outer pressure is consumed.
            float opening=smoothstep(.020,.065,age);
            float mouth_distance=(u-.57)/.15;float mouth=exp(-mouth_distance*mouth_distance);
            float notch=smoothstep(.25,.70,mouth)*smoothstep(0.0,.18,opening)*(1.0-smoothstep(opening-.10,opening+.10,v));
            float front=1.0-.88*smoothstep(.035,.100,age);
            float main_remaining=1.0-smoothstep(front-.05,front+.025,u);
            float short_outer=smoothstep(.68,.74,u)*(1.0-smoothstep(.86,.94,u))*(1.0-smoothstep(.055,.095,age));
            alpha*=(1.0-notch)*max(main_remaining,short_outer)*(1.0-smoothstep(.100,.150,age));
        }else if(input_effect.x<42.5){
            rgb=float3(.52,.68,.23)*gain;
            alpha*=smoothstep(0.0,.16,input_uv.y)*smoothstep(0.0,.16,1.0-input_uv.y);
            alpha*=(1.0-smoothstep(.025,.075,age));
        }else{
            //No role43 emitted by349; retain old subordinate-role fallback for cached empty padding.
            float open_edge=smoothstep(.06,.28,input_uv.y-.07*sin(input_uv.x*5.0));
            float across=1.0-smoothstep(.45,1.0,abs(input_uv.y-.52)*2.0);
            alpha*=open_edge*across*(1.0-smoothstep(.55,1.0,input_uv.x));
            rgb=lerp(float3(.24,.22,.18),float3(.14,.145,.13),input_uv.x)*gain;
        }
        alpha=clamp(alpha,0.0,1.0);return float4(rgb*alpha,alpha);
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
