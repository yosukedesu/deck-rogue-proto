// Hd2dCanopySurface.shader — dot の Godot 版 Project422 shaders/authored_Hd2dCanopySurface.gdshader を URP の HLSL に写したもの (2026-11-02)。
// 土台は Hd2dPrototype.shader の流儀 (頂点の色 COLOR = 役割の色・線形、TEXCOORD1.x = 明るさ → tint = 色×明るさ = Godot の CUSTOM0)。
// 絵は Godot の画像座標 (左上が原点) の uv で書かれているので、読む時に v を 1-v にする。式と数値は gdshader のまま。
Shader "DeckRogue/Hd2dCanopySurface" {
 Properties { _Age("Visual age",Float)=0 _BaseMap("Authored density",2D)="white"{} _RoleShadow("Linear shadow",Vector)=(.03,.08,.02,1) _RolePrimary("Linear primary",Vector)=(.15,.40,.08,1) _RoleEdge("Linear edge",Vector)=(.6,.8,.2,1) _Kind("Kind",Float)=40 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "Hd2dCanopySurfaceForward" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge;float _Age,_Kind;
   CBUFFER_END
   float4 TexBaseMap(float2 g){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(g.x,1.0-g.y));}
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 gain:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 tint:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.tint=float4(i.color.rgb*i.gain.x,i.color.a);return o;}
   float4 frag(V i):SV_Target {
    float4 tint=i.tint;float2 UV=i.uv;
    float u=UV.x;float v=UV.y;
    float gain=dot(tint.rgb,float3(1,1,1))/max(.0001,dot(_RolePrimary.rgb,float3(1,1,1)));
    //299 authored density only: geometry/vertexalpha/age/gain remain298-v2.
    //Interior UV excludes finished outer leaf contour and woody left node.
    float4 organic=TexBaseMap(lerp(float2(.18,.22),float2(.82,.78),float2(u,v)));
    float3 rgb=organic.rgb;
    float edge=min(min(u,1.0-u),min(v,1.0-v));
    float release=smoothstep(.065,.225,_Age);
    float irregular=u+.12*sin(7.0*v+4.0*u);
    float consume=smoothstep(release*.80-.18,release*.80+.02,irregular);
    //300 organic consumption only. Initial material/shape is unchanged through .05s.
    float body_phase=smoothstep(.050,.135,_Age);
    float root_phase=smoothstep(.100,.170,_Age);
    float tissue=organic.g-organic.r*.25;
    float contour=u+.42*(tissue-.30)+.14*sin(8.0*v+3.0*u)+.10*sin(14.0*v-5.0*u);
    float body_front=lerp(-.55,1.70,body_phase);
    float root_front=lerp(-.55,1.70,root_phase);
    float body_remain=smoothstep(body_front-.045,body_front+.045,contour);
    float root_remain=smoothstep(root_front-.045,root_front+.045,contour);
    float root_zone=smoothstep(.76,.92,u);
    float organic_consume=lerp(body_remain,root_remain,root_zone);
    float alpha=tint.a*smoothstep(0.0,.025,edge)*consume*organic.a*organic_consume;
    return float4(rgb*gain,saturate(alpha));
   }
   ENDHLSL
  }
 }
}
