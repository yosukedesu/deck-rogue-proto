// LightBoltOpticalSurface.shader — dot の Godot 版 Project422 shaders/authored_LightBoltOpticalSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は Hd2dPrototype.shader の流儀 (頂点の色 COLOR = 役割の色・線形、TEXCOORD1.x = 明るさ → tint = 色×明るさ = Godot の CUSTOM0)。
// 絵は Godot の画像座標 (左上が原点) の uv で書かれているので、読む時に v を 1-v にする。式と数値は gdshader のまま。
Shader "DeckRogue/LightBoltOpticalSurface" {
 Properties { _Kind("Kind",Float)=41 _Age("Visual age",Float)=0 _BaseMap("Authored density",2D)="white"{} _RolePrimary("Linear primary",Vector)=(.39,.33,.58,1) _RoleCore("Linear core",Vector)=(1,.97,.86,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "LightBoltOpticalSurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RolePrimary,_RoleCore;float _Kind,_Age;
   CBUFFER_END
   float4 TexBaseMap(float2 g){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(g.x,1.0-g.y));}
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 gain:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 tint:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.tint=float4(i.color.rgb*i.gain.x,i.color.a);return o;}
   float4 frag(V i):SV_Target {
    float4 tint=i.tint;float2 UV=i.uv;
    float u=UV.x,v=UV.y;
    bool core_primitive=_Kind>41.5;
    float3 encoded_role=core_primitive?_RoleCore.rgb:_RolePrimary.rgb;
    float gain=dot(tint.rgb,float3(1,1,1))/max(.0001,dot(encoded_role,float3(1,1,1)));
    float3 rgb;float coverage;
    if(core_primitive){
        //Existing two triangles become subordinate narrow optical kernels, not white filled shards.
        float across=abs(v-.5);
        float beam=1.0-smoothstep(.12,.43,across);
        float ends=smoothstep(0.0,.16,u)*(1.0-smoothstep(.65,1.0,u));
        coverage=beam*ends;
        rgb=_RoleCore.rgb;
    }else{
        //305 dedicated density: genuine clear alpha outside narrow unequal radiant core.
        float4 optical=TexBaseMap(lerp(float2(.15,.34),float2(.86,.72),float2(u,1.0-v)));
        coverage=optical.a;
        rgb=optical.rgb;
    }
    float edge=min(min(u,1.0-u),min(v,1.0-v));
    float alpha=tint.a*coverage*smoothstep(0.0,.025,edge);
    return float4(rgb*gain,saturate(alpha));
   }
   ENDHLSL
  }
 }
}
