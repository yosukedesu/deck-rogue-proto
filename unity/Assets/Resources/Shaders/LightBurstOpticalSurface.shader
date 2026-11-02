// LightBurstOpticalSurface.shader — dot の Godot 版 Project422 shaders/authored_LightBurstOpticalSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/LightBurstOpticalSurface" {
 Properties { _BaseMap("Authored density",2D)="white"{} _LookupMap("Lookup",2D)="white"{} _Kind("Kind",Float)=0 _Family("Family",Float)=0 _RoleShadow("Linear shadow",Vector)=(0,0,0,0) _RolePrimary("Linear primary",Vector)=(0,0,0,0) _RoleEdge("Linear edge",Vector)=(0,0,0,0) _RoleCore("Linear core",Vector)=(1,1,1,0) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "LightBurstOpticalSurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;float _Kind,_Family;
   CBUFFER_END
   float4 TexBaseMap(float2 g){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(g.x,1.0-g.y));}
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    //324 private optical shoulder/main plus short arrival core. Gain is CUSTOM0.r, as original Local material.
    float4 tint=input_color;float u=input_uv.x,v=input_uv.y;
    float3 rgb;float coverage;
    if(_Kind>41.5){
        coverage=(1.0-smoothstep(.12,.43,abs(v-.5)))*smoothstep(0.0,.12,u)*(1.0-smoothstep(.72,1.0,u));
        rgb=_RoleCore.rgb;
    }else{
        float4 optical=TexBaseMap(lerp(float2(.08,.12),float2(.92,.88),float2(u,1.0-v)));
        rgb=optical.rgb;coverage=optical.a;
    }
    float edge=min(min(u,1.0-u),min(v,1.0-v));
    float a=clamp(tint.a,0.0,1.0)*coverage*smoothstep(0.0,.025,edge);
    return float4(rgb*tint.r*a,a);
   }
   ENDHLSL
  }
 }
}
