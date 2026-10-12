// Inputs already linear from sRGB-enabled texture samplers. straight RGBA; premultiply only here.
float WSmooth(float x){x=saturate(x);return x*x*(3-2*x);}
float3 WFireRamp(float q,float3 shadow,float3 body,float3 edge,float3 core){q=saturate(q);return q<.25?lerp(shadow,body,q/.25):q<.70?lerp(body,edge,(q-.25)/.45):lerp(edge,core,(q-.70)/.30);}
// kind 0 water band,1 fire band/tongue,2drop,3ember,4core. rgba sample is supplied by host.
float4 WideFragment(float2 uv,int kind,float4 rgba,float opacity,float gain,float3 shadow,float3 body,float3 edge,float3 core){
 float a=saturate(opacity);float3 rgb=core;float2 p=2*uv-1;
 if(kind==0){rgb=rgba.rgb;a*=rgba.a;}
 if(kind==1){rgb=WFireRamp(pow(max(0,dot(rgba.rgb,float3(.2126,.7152,.0722))),.75),shadow,body,edge,core);a*=rgba.a;}
 if((kind==0||kind==1)&&(any(uv<0)||any(uv>1)))a=0;
 if(kind==2){rgb=lerp(rgba.rgb,core,.65);a*=WSmooth(min(1-abs(p.y),1-abs(p.x)-.5*abs(p.y))/.20);}
 if(kind==3)rgb=edge;
 if(kind==4)a*=pow(max(0,1-length(p)),.8);
 return float4(rgb*gain*a,a);
}
// One/OneMinusSrcAlpha, ZWriteOff, CullOff; separate core/particle and band batches.
// water band -> water_surface_filaments_v1.png CLIP/bilinear; fire -> fire_projectile_v2.png CLIP/bilinear;
// drop -> waterdrop_color_lookup_r1.png CLAMP/bilinear. No procedural runtime Noise.
