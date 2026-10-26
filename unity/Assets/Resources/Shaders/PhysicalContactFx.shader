Shader "DeckRogue/PhysicalContactFx" {
 Properties { _BaseMap("Straight sRGB material",2D)="white"{} _UseTexture("Texture ramp or linear solid",Float)=1 _BluntAccent("Blunt core highlight",Float)=0 _Gain("Bounded intensity gain",Float)=1 _UseBeam("Procedural neutral ray",Float)=0 _VisualAge("Contact relative seconds",Float)=0 _RoleShadow("Linear shadow",Vector)=(0,0,0,0) _RolePrimary("Linear primary",Vector)=(0,0,0,0) _RoleEdge("Linear edge",Vector)=(0,0,0,0) _RoleCore("Linear core",Vector)=(1,1,1,0) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent" }
  Pass {
   Name "PhysicalContactForward" Tags { "LightMode"="UniversalForward" }
   Blend One OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;float _UseTexture,_BluntAccent,_Gain,_UseBeam,_VisualAge;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.color=a.color;return o;}
   float4 frag(V i):SV_Target {
    float coverage=i.color.a;float3 linearRgb=i.color.rgb;
    if(_UseBeam>.5){
     float u=i.uv.x,v=i.uv.y,t=_VisualAge;
     float center=saturate(1-abs(2*v-1)),c=pow(center,6);
     float3 role=c<.55?lerp(_RolePrimary.rgb,_RoleEdge.rgb,c/.55):lerp(_RoleEdge.rgb,_RoleCore.rgb,(c-.55)/.45);
     float field=.5+.25*sin(30*u-120*t)+.15*sin(70*u-170*t+2*v)+.10*sin(130*u-240*t-4*v);
     float flow=.28+.72*saturate(2.5*(field-.3));
     coverage*=pow(center,.55)*flow;linearRgb=role*i.color.r;
    }else if(_UseTexture>.5){
     float4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv); // sRGB sampler decodes RGB once; alpha stays linear.
     coverage*=tex.a*step(0,i.uv.x)*step(i.uv.x,1)*step(0,i.uv.y)*step(i.uv.y,1);
     float y=saturate(dot(tex.rgb,float3(.2126,.7152,.0722)));
     float3 role=y<.35?lerp(_RoleShadow.rgb,_RolePrimary.rgb,y/.35):y<.80?lerp(_RolePrimary.rgb,_RoleEdge.rgb,(y-.35)/.45):_BluntAccent>.5?lerp(_RoleEdge.rgb,_RoleCore.rgb,(y-.80)/.20):_RoleEdge.rgb;
     linearRgb=role*i.color.r; // surface color.r carries authored layer gain only.
    }
    coverage=saturate(coverage);
    return float4(linearRgb*_Gain*coverage,coverage); // Exactly one premultiply, no additive duplicate.
   }
   ENDHLSL
  }
 }
}
