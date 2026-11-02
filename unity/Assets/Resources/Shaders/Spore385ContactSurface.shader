// Spore385ContactSurface.shader — dot の Godot 版 Project422 shaders/authored_Spore385ContactSurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は BoundedContactFx.shader (頂点の色 r = 明るさ・a = 不透明度、uv1 = (柔らかさ, 役割))。絵は Godot の画像座標 (左上が原点) で書かれているので、読む時に v を 1-v にする。
// 出力は乗算済み (rgb*alpha, alpha)・Blend One OneMinusSrcAlpha = Godot の blend_mix と同じ見え方。式と数値は gdshader のまま。
Shader "DeckRogue/Spore385ContactSurface" {
 Properties { _Age("Visual age",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "Spore385ContactSurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _Age;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 effect:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.effect=i.effect;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 input_uv=i.uv;float2 input_effect=i.effect;float4 input_color=i.color;
    //385 local procedural botanical cluster. No image sampler, floor wash, or radial rays.
    float4 color=input_color;float2 p=input_uv*2.0-1.0;
    float opacity=0.0;float3 rgb=float3(.18,.32,.035);
    if(input_effect.x<.5){
        //A connected irregular low-opacity body joins the seeds briefly, without a fog halo.
        float d=min(length((p-float2(-.29,.05))/float2(.66,.74)),min(length((p-float2(.22,.13))/float2(.61,.65)),length((p-float2(.02,-.23))/float2(.63,.62))));
        opacity=(1.0-smoothstep(.40,1.0,d))*(.86+.14*sin(7.0*p.x+2.0*p.y)*sin(6.0*p.y));
        rgb=float3(.14,.28,.025);
    }else{
        float seed=input_effect.y;
        float2 q=p+float2(.065*sin(3.1*p.y+seed),.045*sin(4.2*p.x+seed*.7));
        q.x*=1.03+.12*q.y;
        float d=length(q);float cover=1.0-smoothstep(.72,.98,d);
        float inner=1.0-smoothstep(.48,.78,d);
        float highlight=(1.0-smoothstep(.12,.72,length(q-float2(-.20,.22))))*.55;
        float grain=.93+.07*sin(q.x*25.0+seed)*sin(q.y*21.0+seed);
        rgb=lerp(float3(.055,.115,.012),float3(.37,.56,.085),inner);
        rgb=lerp(rgb,float3(.70,.76,.24),highlight)*grain;
        opacity=cover;
    }
    float a=clamp(opacity*color.a,0.0,1.0);
    return float4(rgb*a,a);
   }
   ENDHLSL
  }
 }
}
