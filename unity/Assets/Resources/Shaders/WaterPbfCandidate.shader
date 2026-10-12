Shader "DeckRogue/WaterPbfCandidate" {
 Properties {_Fade("Tail coverage, linear",Range(0,1))=1}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "CachedWaterForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Back
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _Fade;
   CBUFFER_END
   struct A {float4 p:POSITION;float3 n:NORMAL;float2 kind:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 local:TEXCOORD2;float kind:TEXCOORD3;};
   float3 SafeUnit(float3 x){return x*rsqrt(max(dot(x,x),1e-12));}
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.world=TransformObjectToWorld(i.p.xyz);o.normal=TransformObjectToWorldNormal(i.n);o.local=i.p.xyz;o.kind=i.kind.x;return o;}
   // Portable bounded directional microstructure, not Blender Noise or physical refraction.
   float Ridge(float3 p){float n=.5+.25*sin(dot(p,float3(1.8,26,32)))+.15*sin(dot(p,float3(3.6,52,64))+1.3)+.10*sin(dot(p,float3(7.2,104,128))+2.6);return saturate((n-.46)/.12)*.18;}
   float Fresnel1333(float cosine){float c=saturate(cosine);float st2=(1-c*c)/(1.333*1.333);float ct=sqrt(max(0,1-st2));float rs=(c-1.333*ct)/max(1e-6,c+1.333*ct);float rp=(1.333*c-ct)/max(1e-6,1.333*c+ct);return saturate((rs*rs+rp*rp)*.5);}
   float4 frag(V i):SV_Target {
    float3 n=SafeUnit(i.normal),view=SafeUnit(GetWorldSpaceViewDir(i.world));
    // Source fixed light direction converted once from Z-up to Y-up; rotate with this captured effect.
    float3 highlight=SafeUnit(TransformObjectToWorldDir(float3(-.35,.82,-.45)));
    float glint=pow(saturate(dot(n,SafeUnit(view+highlight))),80);
    float f=Fresnel1333(abs(dot(n,view))),ridge=Ridge(i.local);
    float white=saturate(f*.50+glint*.88+ridge);
    float3 body=lerp(float3(.20,.30,.34),float3(.33,.43,.47),ridge/.18);
    body=lerp(body,float3(.82,.89,.92),white);
    Light key=GetMainLight();float diffuse=.35+.65*saturate(dot(n,key.direction));
    float3 lighting=max(float3(.2,.2,.2),SampleSH(n))+key.color*diffuse;
    float3 rgb=body*lighting+float3(.88,.93,.95)*glint*.65;
    float a=min(.92,.12+step(.5,i.kind)*.09+f*.42+glint*.55+ridge);
    if(i.kind>1.5){rgb=float3(.90,.94,.96)*lighting+float3(.8,.85,.88)*.45;a=1;}
    a=saturate(a)*saturate(_Fade);return float4(rgb*a,a);
   }
   ENDHLSL
  }
 }
}
