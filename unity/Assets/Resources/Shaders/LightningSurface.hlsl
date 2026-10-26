float Smooth01(float q){q=saturate(q);return q*q*(3-2*q);}
float LightningClock(float2 uv,float age){
 float2 d=float2(2*uv.x-477./887.,uv.y-(1.-442./887.));float r=length(d),theta=atan2(d.y,d.x);
 float direction=dot(d,float2(cos(2.35),sin(2.35)))/(r+1e-6);float leader=Smooth01((direction-.90)/.07);
 if(age<0){float q=Smooth01((age+.100)/.100),front=.68*(1-q);return .22*pow(q,1.8)*leader*Smooth01((r-front)/.045);}
 float birth=.013*saturate(r/.5);float branch=Smooth01((age-birth)/.005),start=max(.88*leader,branch);
 float end=.068+.042*(.5+.5*sin(5*theta+.7));return start*(1-Smooth01((age-.020)/(end-.020)));
}
float3 LightningRamp(float q,float3 shadow,float3 primary,float3 edge,float3 core){
 q=saturate(q);if(q<=.12)return lerp(shadow,primary,q/.12);if(q<=.55)return lerp(primary,edge,(q-.12)/.43);return lerp(edge,core,(q-.55)/.45);
}
float4 LightningFragment(float4 textureLinear,float2 uv,float age,float opacity,float3 shadow,float3 primary,float3 edge,float3 core){
 float L=dot(textureLinear.rgb,float3(.2126,.7152,.0722));float a=textureLinear.a*pow(max(0,L),.65)*LightningClock(uv,age)*opacity;
 float3 rgb=LightningRamp(pow(max(0,L),1.20),shadow,primary,edge,core);return float4(rgb*2.35*a,a);
}
float SoftCoverage(float2 uv){return pow(max(0,1-length(2*uv-1)),.70);}
// Decode PNG RGB and sRGB palette once. Alpha remains linear coverage.
// Native core/sparks: mix primary->core by SoftCoverage, independent opacity, gains3.0/2.1.
// Output HDR premultiplied once, Blend One OneMinusSrcAlpha, ZWriteOff,CullOff.
// 13-vertex soft core plus12 moving quads. No periodic flashing or peak/trail texture cut.
