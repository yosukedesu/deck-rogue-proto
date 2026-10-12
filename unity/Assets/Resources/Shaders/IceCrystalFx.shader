Shader "DeckRogue/IceCrystalFx" {
 Properties {_BaseMap("Straight sRGB ice source",2D)="white"{} _Kind("Texture / core / solid",Float)=0 _RoleShadow("Linear shadow",Vector)=(0,0,0,0) _RolePrimary("Linear primary",Vector)=(0,0,0,0) _RoleEdge("Linear edge",Vector)=(0,0,0,0) _RoleCore("Linear core",Vector)=(1,1,1,0)}
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "IceCrystalForward" Tags {"LightMode"="UniversalForward"} Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;float _Kind;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float IceSmooth(float x){x=saturate(x);return x*x*(3-2*x);}
   float3 IceRamp(float q){if(q<=.20)return lerp(_RoleShadow.rgb,_RolePrimary.rgb,saturate(q/.20));if(q<=.65)return lerp(_RolePrimary.rgb,_RoleEdge.rgb,saturate((q-.20)/.45));return lerp(_RoleEdge.rgb,_RoleCore.rgb,saturate((q-.65)/.35));}
   float4 frag(V i):SV_Target {float a=saturate(i.color.a);float3 c=i.color.rgb;
    if(_Kind<.5){float4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);float q=saturate(pow(max(0,dot(tex.rgb,float3(.2126,.7152,.0722))),1.35));c=IceRamp(q)*i.color.r;a*=saturate(tex.a)*(1-IceSmooth(((2*i.uv.x+i.uv.y)*.5-i.effect.x)/.045));a*=step(0,i.uv.x)*step(i.uv.x,1)*step(0,i.uv.y)*step(i.uv.y,1);}
    else if(_Kind<1.5){float coverage=pow(max(0,1-length(2*i.uv-1)),.70);c=lerp(_RolePrimary.rgb,_RoleCore.rgb,coverage)*i.color.r;a*=coverage;}
    return float4(c*a,a);
   }
   ENDHLSL
  }
 }
}
