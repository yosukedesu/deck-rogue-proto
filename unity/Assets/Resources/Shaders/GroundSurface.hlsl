// R2 canonical local floor XZ (units captured base-visible H). Mesh already clipped once.
float Smooth01(float x){ x=saturate(x); return x*x*(3-2*x); }
static const float4 Cracks[17]={
float4(0,0,.30,.06),float4(.30,.06,.60,0),float4(.60,0,.91,.12),float4(.30,.06,.49,.27),float4(.49,.27,.67,.35),float4(.60,0,.77,-.20),
float4(0,0,-.27,.04),float4(-.27,.04,-.56,-.07),float4(-.56,-.07,-.94,-.03),float4(-.27,.04,-.43,.28),float4(-.43,.28,-.64,.37),
float4(0,0,.04,-.23),float4(.04,-.23,-.10,-.46),float4(-.10,-.46,-.32,-.54),float4(.04,-.23,.26,-.40),float4(0,0,-.03,.22),float4(-.03,.22,.08,.48)};
float EarthField(float2 p,float age,float alpha,float front,float linearGrain,out float core){
 float hit=0;
 [unroll]for(int i=0;i<17;i++){
  float2 a=Cracks[i].xy,b=Cracks[i].zw;float2 d=b-a;float q=saturate(dot(p-a,d)/dot(d,d));float dist=length(p-a-q*d);float w=(.007+.002*(i%3))*(1-.52*q);
  hit=max(hit,(1-Smooth01((dist-w*.38)/(w*.62)))*(1-Smooth01((length(p)-front)/.055)));
 }
 float wave=0;
 [unroll]for(int i=0;i<2;i++){
  float birth=i==0?0:.075,speed=i==0?3.6:2.6,a=age-birth;float radius=.14+speed*max(0,a);float2 e=p*float2(1,1.65);
  float cover=(1-Smooth01(abs(length(e)-radius)/.012))*(1-Smooth01(a/.190))*Smooth01(a/.010);
  float segment=.32+.68*Smooth01((sin(atan2(e.y,e.x)*7+birth*19)-.2)/.5);wave=max(wave,cover*segment*.45);
 }
 core=hit*.65;return max(hit,wave)*alpha*(.76+.24*linearGrain);
}
float StompField(float2 p,float age,float alpha){
 float a=max(0,age);float2 e=p*float2(1,1.4);float radius=.13+6.4*a;
 float band=1-Smooth01(abs(length(e)-radius)/(.020+.010*Smooth01(a/.10)));
 float theta=atan2(e.y,e.x);float variation=sin(3*theta+.4)+.55*sin(7*theta-.9)+.25*sin(11*theta+1.1);
 return band*alpha*Smooth01((variation-.15)/.70);
}
float DustCoverage(float2 uv,float phase){float2 p=2*uv-1;float sn=sin(8*p.x+5*p.y+phase);return pow(max(0,1-length(p)),1.25)*(.70+.30*(sn*sn));}
// Decode palette and texture RGB sRGB exactly once. Earth grain is linear luminance.
// Earth RGB=lerp(primary,core,core), gain1.55; stomp lerp(primary,core,.4),gain.95.
// premultiplied output = float4(rgbLinear*gain*coverage,coverage). Blend One OneMinusSrcAlpha.
// Ordinary URP forward transparent, ZTest LEqual, ZWrite Off. No camera-basis floor.
// Rocks are low-poly lit facets (roughness .86, primary albedo, edge emission .22);
// unlit-only ports differ materially and require explicit Unity comparison.
