// SparkBurstContactSurface.shader — dot の Godot 版 Project422 shaders/authored_SparkBurstContactSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/SparkBurstContactSurface" {
 Properties { _Kind("Core / particles",Float)=0 _Family("Fire / warm / ice / spore",Float)=0 _RolePrimary("Linear primary",Vector)=(0,0,0,0) _RoleEdge("Linear edge",Vector)=(0,0,0,0) _RoleCore("Linear core",Vector)=(1,1,1,0) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "SparkBurstContactSurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "FrozenScatterSurface.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _RolePrimary,_RoleEdge,_RoleCore;float _Kind,_Family;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    float role=input_effect.x;
    if(role>60.5 && role<63.5){
        float u=clamp(input_uv.x,0.0,1.0),v=clamp(input_uv.y,0.0,1.0),cover=1.0;float3 rgb;
        if(role<61.5){
            float du=(u-.46)/.40,dv=(v-(.47+.12*u))/.32;
            float belly=exp(-(du*du+dv*dv));
            cover=smoothstep(0.0,.12,min(v,1.0-v))*smoothstep(0.0,.10,u);
            rgb=lerp(float3(.30,.055,.012),float3(.80,.24,.035),belly);
            float hotU=(u-.68)/.21,hotV=(v-(.43+.18*u))/.18;
            rgb=lerp(rgb,float3(1.0,.91,.68),.42*exp(-(hotU*hotU+hotV*hotV)));
        }else if(role<62.5){
            cover=smoothstep(0.0,.16,u)*smoothstep(0.0,.20,1.0-u)*smoothstep(0.0,.18,min(v,1.0-v));
            rgb=float3(1.0,.91,.68);
        }else{
            cover=smoothstep(0.0,.10,min(v,1.0-v));
            rgb=lerp(float3(.30,.055,.012),float3(.80,.24,.035),smoothstep(.05,.48,u));
            rgb=lerp(rgb,float3(1.0,.91,.68),.82*smoothstep(.52,.88,u));
        }
        float a=clamp(input_color.a,0.0,1.0)*cover;
        return float4(rgb*input_color.r*a,a);
    }
    return ScatterFragment(input_uv,(int)_Family,_Kind<0.5,input_effect.x,saturate(input_color.a),input_color.r,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb);
   }
   ENDHLSL
  }
 }
}
