Shader "DeckRogue/Hd2dCrossbowAimedSurface" {
 Properties {_BaseMap("Original Crossbow B pressure",2D)="white"{} _Age("Actual contact age",Float)=0 _Gain("Linear gain",Float)=1}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float _Age,_Gain;float4 _Primary,_Core;
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 mode:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 mode:TEXCOORD1;float alpha:TEXCOORD2;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.mode=i.mode;o.alpha=i.color.a;return o;}
   float4 frag(V i):SV_Target {
    float u=i.uv.x,v=i.uv.y;float a;float3 rgb;
    if(i.mode.y>2.5){
     float band=1-smoothstep(.20,.50,abs(v-.5));float ends=smoothstep(0,.18,u)*(1-smoothstep(.65,1,u));
     a=saturate(i.alpha*band*ends);rgb=float3(.40,.38,.32)*max(0,_Gain);
    }else if(i.mode.y>1.5){
     float shape=smoothstep(0,.10,min(min(u,1-u),min(v,1-v)));
     float axis=.46+.10*sin(3.14159265*u);float crease=1-smoothstep(.045,.22,abs(v-axis));
     float edge=smoothstep(.32,.48,u)*(1-smoothstep(.68,.83,u))*(1-smoothstep(.025,.075,abs(v-(axis+.12))));
     rgb=lerp(lerp(float3(.20,.19,.17),float3(.045,.040,.032),crease),float3(.66,.59,.45),edge)*max(0,_Gain);a=saturate(i.alpha*shape);
    }else if(i.mode.y>.5){
     float transverse=smoothstep(0,.18,v)*(1-smoothstep(.70,1,v));
     float4 d=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(.35+.23*u,1-(.365+.35*(1-v)-.11*u)));
     float contour=u+.10*sin(8*v+u);float front=lerp(-.15,1.15,smoothstep(.035,.120,_Age));
     a=saturate(i.alpha*d.a*transverse*smoothstep(front-.025,front+.025,contour));rgb=d.rgb*max(0,_Gain);
    }else{
     float2 p=i.uv*2-1;float radial=saturate(1-dot(p,p));float edge=i.mode.x>.5?radial*radial:saturate(min(v,1-v)*12);
     a=saturate(i.alpha*edge);rgb=lerp(_Primary.rgb,_Core.rgb,radial*.55)*max(0,_Gain);
    }
    return float4(rgb*a,a);
   }
   ENDHLSL
  }
 }
}
