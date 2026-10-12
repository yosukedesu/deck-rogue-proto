// family:0fire,1warm display sparks,2ice,3botanical spores. No texture.
float ScatterSoft(float2 p,float exponent){return pow(max(0,1-length(p)),exponent);}
float4 ScatterFragment(float2 uv,int family,bool core,float phase,float opacity,float gain,float3 primary,float3 edge,float3 bright){
 float2 p=2*uv-1;float cover=1,factor=0;float3 rgb;
 if(core){cover=ScatterSoft(p,.8);rgb=lerp(primary,bright,cover);}
 else if(family==3){float2 q=p+float2(.11*sin(3*p.y+phase),.08*sin(4*p.x-phase));float sn=sin(8*p.x+5*p.y+phase);float ed=saturate(min(min(uv.x,uv.y),min(1-uv.x,1-uv.y))/.10);cover=ScatterSoft(q,.75)*(.85+.15*sn*sn)*(ed*ed*(3-2*ed));rgb=lerp(primary,bright,cover*.85);}
 else if(family==2){factor=(uv.x+uv.y>1)?.78:.20;rgb=lerp(primary,bright,factor);}
 else{factor=saturate((1-abs(p.y))*(.95-.6*uv.x));rgb=lerp(primary,family==0?edge:bright,factor);}
 float a=opacity*cover;return float4(rgb*gain*a,a);
}
// sRGB palette -> linear once. Output premultiplied once; Blend One OneMinusSrcAlpha.
// ZWriteOff,CullOff; core and particle batches separate. phase is per particle, not per draw.
