float LSmooth(float x){x=saturate(x);return x*x*(3-2*x);}
float3 LColor(float q,float3 shadow,float3 body,float3 edge,float3 core){q=saturate(q);return q<.2?lerp(shadow,body,q/.2):q<.65?lerp(body,edge,(q-.2)/.45):lerp(edge,core,(q-.65)/.35);}
// kind 0 mapped texture(dark/light/blunt),1water band,2waterdrop,3core,4spore,5pod,6facet,7solid
// rgba is straight linear sampled data, role is already-decoded palette role. Output HDR premultiplied once.
float4 LocalFragment(float2 uv,int kind,float4 rgba,float opacity,float gain,float phase,float3 shadow,float3 body,float3 edge,float3 core,float3 role){
 float a=saturate(opacity);float3 rgb=role;float2 p=2*uv-1;
 if(kind==0){rgb=LColor(dot(rgba.rgb,float3(.2126,.7152,.0722)),shadow,body,edge,core);a*=rgba.a;}
 if(kind==1||kind==2){rgb=rgba.rgb;if(kind==1)a*=rgba.a;}
 if((kind==0||kind==1)&&(any(uv<0)||any(uv>1)))a=0;
 if(kind==2)a*=LSmooth((1-abs(p.x)-abs(p.y))/.25);
 if(kind==3){float c=pow(max(0,1-length(p)),.7);a*=c;rgb=lerp(body,core,c);}
 if(kind==4){float c=pow(max(0,1-length(p)+.045*sin(9*uv.x+phase)*sin(10*uv.y-phase)),.65)*LSmooth(min(min(uv.x,uv.y),min(1-uv.x,1-uv.y))/.10);a=min(a,.9)*c;rgb=lerp(body,role,min(1,c*.9));}
 if(kind==5){float ed=abs(p.y);float ridge=exp(-70*(uv.y-.5)*(uv.y-.5));float q=saturate(.18+.42*ed+.38*ridge);rgb=lerp(body,edge,q);a*=.80+.20*sin(3.141592653589793*uv.x);}
 if(kind==6)rgb=(uv.x+uv.y<1)?body:edge;
 return float4(rgb*gain*a,a);
}
// ordinary Mesh/MeshRenderer; One/OneMinusSrcAlpha; ZWriteOff,CullOff. No compute or geometry shaders.
