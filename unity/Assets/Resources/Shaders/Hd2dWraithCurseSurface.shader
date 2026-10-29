Shader "DeckRogue/Hd2dWraithCurseSurface" {
 Properties {_BaseMap("Original Wraith pressure",2D)="white"{} _Age("Actual contact age",Float)=0 _Gain("Linear gain",Float)=1}
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
    if(i.mode.y>1.5){
     float4 d=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(lerp(.60,.78,u),1-lerp(.39,.59,1-v)));
     a=saturate(i.alpha*d.a*(1-smoothstep(.12,.5,abs(v-.5)))*(1-smoothstep(.105,.175,_Age)));rgb=d.rgb*.45*max(0,_Gain);
    }else if(i.mode.y>.5){
     float4 d=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(lerp(.23,.83,u),1-lerp(.22,.80,1-v)));
     float contour=u+.12*sin(9*v+2*u)+.20*(d.g-d.r);
     float initial=smoothstep(-.385,-.315,contour);
     float front=-.40+.75*smoothstep(0,1.0/30,_Age)+.30*smoothstep(1.0/30,2.0/30,_Age)+.75*smoothstep(2.0/30,.100,_Age);
     float remain=initial*lerp(smoothstep(front-.035,front+.035,contour),1-smoothstep(.070,.150,_Age),smoothstep(.78,.94,u));
     float edge=smoothstep(0,.035,min(min(u,1-u),min(v,1-v)));
     a=saturate(i.alpha*d.a*edge*remain);rgb=d.rgb*max(0,_Gain);
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
