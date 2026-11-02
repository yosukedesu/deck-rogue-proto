// ObjectContact415Surface.shader — dot の Godot 版 Project422 shaders/authored_ObjectContact415Surface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/ObjectContact415Surface" {
 Properties { _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "ObjectContact415SurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Primary,_Core;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    float2 centered=input_uv*2.0-1.0;
    float radial=saturate(1.0-dot(centered,centered));
    float edge=input_effect.x>.5?radial*radial:saturate(min(input_uv.y,1.0-input_uv.y)*12.0);
    float alpha=saturate(input_color.a*edge);
    float3 rgb=lerp(_Primary.rgb,_Core.rgb,radial*.55);
    if(input_effect.y>.5 && input_effect.y<1.5){
        //Chipped, irregular junk with an asymmetric fracture and muted material.
        float cut=min(min(input_uv.x+input_uv.y-.18,1.0-input_uv.x+input_uv.y-.13),min(2.0-input_uv.x-input_uv.y-.28,input_uv.x+1.0-input_uv.y-.12));
        cut=min(cut,.84+2.0*abs(input_uv.x-.41)-input_uv.y);
        alpha*=smoothstep(0.0,.018,cut);
        float facet=smoothstep(.42,.49,input_uv.y-.24*input_uv.x);
        float3 material=_Primary.rgb*float3(.56,.43,.32)*lerp(.76,1.12,facet);
        float fracture=1.0-smoothstep(.008,.025,abs(input_uv.y-(.35+.28*input_uv.x)));
        material*=1.0-.44*fracture;
        float border=min(min(input_uv.x,1.0-input_uv.x),min(input_uv.y,1.0-input_uv.y));
        material*=lerp(.45,1.0,smoothstep(.02,.10,min(border,cut)));
        float glint=(1.0-smoothstep(.012,.040,abs(input_uv.y-(.94-.10*input_uv.x))))*smoothstep(.08,.22,input_uv.x)*(1.0-smoothstep(.58,.75,input_uv.x));
        rgb=lerp(material,_Core.rgb*.65,glint*.75);
    }else if(input_effect.y>1.5){
        //One asymmetric curved blade across both quads, with a genuine open inner gap.
        float u=input_uv.x;
        float center=.24+.57*sin(3.14159265*pow(clamp(u,0.0,1.0),.78))+.08*u;
        float taper=pow(max(0.0,sin(3.14159265*u)),.62);
        float width=.070*taper;
        float dist=abs(input_uv.y-center);
        float coverage=1.0-smoothstep(max(0.0,width-.012),width+.012,dist);
        alpha=input_color.a*coverage;
        float edgeGlint=(1.0-smoothstep(.010,.025,abs(input_uv.y-center-width*.65)))*smoothstep(.08,.24,u)*(1.0-smoothstep(.72,.91,u));
        float3 metal=_Primary.rgb*float3(.55,.58,.61);
        float bevel=smoothstep(-width,width,input_uv.y-center);
        metal*=lerp(.56,1.22,bevel);
        rgb=lerp(metal,_Core.rgb*.80,edgeGlint*.78);
    }
    rgb*=max(0.0,input_color.r);
    return float4(rgb*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
