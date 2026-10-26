Shader "DeckRogue/SystemOutcomeFx" {
 Properties { _Kind("Solid / rim / face / trace",Float)=0 _VisualAge("Receipt age seconds",Float)=0 _RoleShadow("Linear shadow",Vector)=(0,0,0,0) _RolePrimary("Linear primary",Vector)=(0,0,0,0) _RoleEdge("Linear edge",Vector)=(0,0,0,0) _RoleCore("Linear core",Vector)=(1,1,1,0) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent" }
  Pass {
   Name "SystemOutcomeForward" Tags { "LightMode"="UniversalForward" }
   Blend One OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;float _Kind,_VisualAge;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 phase:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 phase:TEXCOORD1;float4 color:COLOR;};
   V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.phase=a.phase;o.color=a.color;return o;}
   float OutcomeSmooth(float x){float q=saturate(x);return q*q*(3-2*q);}
   float4 frag(V i):SV_Target {
    float coverage=i.color.a;float3 linearRgb=i.color.rgb;float across=max(0,1-abs(2*i.uv.y-1));
    if(_Kind>.5 && _Kind<1.5){
     float d0=abs(i.uv.x-frac(.87-1.6*_VisualAge));d0=min(d0,1-d0);
     float d1=abs(i.uv.x-frac(.87+1.6*_VisualAge));d1=min(d1,1-d1);
     float q=max(exp(-160*d0*d0),exp(-160*d1*d1));
     float3 role=q<.4?lerp(_RolePrimary.rgb,_RoleEdge.rgb,q/.4):lerp(_RoleEdge.rgb,_RoleCore.rgb,(q-.4)/.6);
     linearRgb=role*i.color.r;coverage*=pow(across,.55);
    }else if(_Kind>1.5 && _Kind<2.5){linearRgb=_RolePrimary.rgb*i.color.r;}
    else if(_Kind>2.5 && _Kind<3.5){float front=1-OutcomeSmooth((_VisualAge-.018)/.145);float burn=1-OutcomeSmooth((i.uv.x-front+.08)/.16);linearRgb=_RoleEdge.rgb*i.color.r;coverage*=pow(across,.55)*burn;}
    else if(_Kind>3.5 && _Kind<4.5){linearRgb=_RoleEdge.rgb*i.color.r;coverage*=pow(across,.75);}
    else if(_Kind>4.5 && _Kind<5.5){float3 role=across<.3?lerp(_RoleShadow.rgb,_RolePrimary.rgb,across/.3):lerp(_RolePrimary.rgb,_RoleEdge.rgb,(across-.3)/.7);linearRgb=role*i.color.r;}
    else if(_Kind>5.5 && _Kind<6.5){float heat=pow(across,2.3)*(1-.68*i.uv.x);float3 role=heat<.22?lerp(_RoleShadow.rgb,_RolePrimary.rgb,heat/.22):heat<.70?lerp(_RolePrimary.rgb,_RoleEdge.rgb,(heat-.22)/.48):lerp(_RoleEdge.rgb,_RoleCore.rgb,(heat-.70)/.30);linearRgb=role*i.color.r;coverage*=pow(across,.65);}
    else if(_Kind>6.5 && _Kind<7.5){coverage*=pow(across,.45);}
    else if(_Kind>7.5){float gap=1-.8*OutcomeSmooth((sin(17*i.uv.x+i.phase.x)-.1)/.75);float fall=1-OutcomeSmooth((_VisualAge*3.5-i.uv.x)/.23);linearRgb=_RoleEdge.rgb*i.color.r;coverage*=pow(across,.6)*gap*fall;}
    coverage=saturate(coverage);return float4(linearRgb*coverage,coverage);
   }
   ENDHLSL
  }
 }
}
