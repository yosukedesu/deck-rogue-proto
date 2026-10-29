Shader "DeckRogue/Hd2dColdSweepSurface" {
 Properties { _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) _BaseMap("Original frost density",2D)="white"{} _Age("Actual age",Float)=0 _Gain("HDR gain",Float)=1 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _Primary,_Core;float _Age,_Gain;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 mode:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 mode:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.mode=i.mode;o.color=i.color;return o;}
   float consume(float2 uv,float4 density,float begin,float end) {
    if(_Age<=begin)return 1;if(_Age>=end)return 0;
    float phase=smoothstep(begin,end,_Age);
    float lightness=dot(density.rgb,float3(.2126,.7152,.0722));
    float contour=uv.x+.42*(lightness-.32)+.14*sin(7*uv.y+3*uv.x);
    float front=lerp(-.32,1.42,phase);
    return smoothstep(front-.045,front+.045,contour);
   }
   float4 frag(V i):SV_Target {
    float2 centered=i.uv*2-1;float radial=saturate(1-dot(centered,centered));
    float edge=i.mode.x>.5?radial*radial:saturate(min(i.uv.y,1-i.uv.y)*12);
    float alpha=saturate(i.color.a*edge);
    float3 rgb=lerp(_Primary.rgb,_Core.rgb,radial*.55)*max(0,_Gain);
    if(i.mode.y>.5 && i.mode.y<1.5){
     // Source rectangles use image top-left coordinates; Unity samples bottom-left.
     float4 frost=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(lerp(.06,.94,i.uv.x),1-lerp(.12,.87,1-i.uv.y)));
     rgb=frost.rgb*max(0,_Gain);alpha*=frost.a*consume(i.uv,frost,.04,.145);
    }else if(i.mode.y>1.5){
     float4 flake=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(lerp(.30,.48,i.uv.x),1-lerp(.44,.64,1-i.uv.y)));
     rgb=lerp(flake.rgb,_Core.rgb,i.mode.y>2.5?.18:.55)*max(0,_Gain);
     alpha=saturate(i.color.a)*flake.a;
     alpha*=i.mode.y<2.5?consume(i.uv,flake,.04,.105):consume(i.uv,flake,.085,.175);
    }
    // Native Godot unpremultiplies for blend_mix; Unity's existing bounded blend is premultiplied.
    return float4(rgb*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
