// Independent evaluation math; see PROVENANCE.md and NOTICES.txt.
// Actor-only provisional SDR adapter. Current stage: GL Filmic, exposure1.12, white1.
// No alpha, authored input, lighting, or texture sampler change. Baseline switch
// belongs at call site. Do not enable under a different renderer/stage transfer.
vec3 eval_rows(vec3 v, vec3 r, vec3 g, vec3 b) {
    return vec3(dot(v,r),dot(v,g),dot(v,b));
}
vec3 eval_ap1_xyz(vec3 v) {
    return eval_rows(v,vec3(.6624541811,.1340042065,.1561876870),vec3(.2722287168,.6740817658,.0536895174),vec3(-.0055746495,.0040607335,1.0103391003));
}
vec3 eval_aces_rgb(vec3 input_linear) {
    vec3 a=eval_rows(input_linear,vec3(.4397010,.3829780,.1773350),vec3(.0897923,.8134230,.0967616),vec3(.0175440,.1115440,.8707040));
    float low=min(a.x,min(a.y,a.z)), high=max(a.x,max(a.y,a.z));
    float saturation=(max(high,.0001)-max(low,.0001))/max(high,.01);
    vec3 difference=a-a.yzx;
    float yc=(a.x+a.y+a.z+1.75*sqrt(max(0.0,dot(difference,difference)*.5)))/3.0;
    float z=(saturation-.4)/.2;
    float t=max(0.0,1.0-abs(z)*.5);
    float shape=(1.0+sign(z)*(1.0-t*t))*.5;
    float glow=.05*shape;
    if(yc>=.16) glow=0.0;
    else if(yc>.08*2.0/3.0) glow*=.08/yc-.5;
    a*=1.0+glow;
    float hue=0.0;
    if(a.x!=a.y || a.y!=a.z) hue=degrees(atan(sqrt(3.0)*(a.y-a.z),2.0*a.x-a.y-a.z));
    float h=clamp(1.0-abs(hue)/67.5,0.0,1.0);
    float s=h*h*(3.0-2.0*h);
    a.x+=(.03-a.x)*.18*s*s*saturation;
    vec3 p=max(vec3(0.0),eval_rows(a,vec3(1.4514393161,-.2365107469,-.2149285693),vec3(-.0765537734,1.1762296998,-.0996759264),vec3(.0083161484,-.0060324498,.9977163014)));
    vec3 yweights=vec3(.272229,.674082,.0536895);
    p=.96*p+.04*vec3(dot(p,yweights));
    p=(p*p+.0245786*p-vec3(.000090537))/(.983729*p*p+.432951*p+vec3(.238081));
    vec3 xyz=eval_ap1_xyz(p);
    float total=max(xyz.x+xyz.y+xyz.z,.0001);
    vec2 chromaticity=xyz.xy/total;
    float luminance=pow(clamp(xyz.y,0.0,65504.0),.9811);
    float factor=luminance/max(chromaticity.y,.0001);
    xyz=vec3(chromaticity.x*factor,luminance,(1.0-chromaticity.x-chromaticity.y)*factor);
    p=eval_rows(xyz,vec3(1.6410233797,-.3248032942,-.2364246952),vec3(-.6636628587,1.6153315917,.0167563477),vec3(.0117218943,-.0082844420,.9883948585));
    p=.93*p+.07*vec3(dot(p,yweights));
    xyz=eval_ap1_xyz(p);
    xyz=eval_rows(xyz,vec3(.98722400,-.00611327,.0159533),vec3(-.00759836,1.00186000,.0053302),vec3(.00307257,-.00509595,1.0816800));
    return eval_rows(xyz,vec3(3.2409699419,-1.5373831776,-.4986107603),vec3(-.9692436363,1.8759675015,.0415550574),vec3(.0556300797,-.2039769589,1.0569715142));
}
vec3 eval_grade_lut(sampler2D lut, vec3 mapped) {
    vec3 p=clamp(mapped,vec3(0.0),vec3(1.0))*31.0;
    float slice=floor(p.b);
    vec2 uv0=vec2(p.r+32.0*slice+.5,p.g+.5)/vec2(1024.0,32.0);
    vec2 uv1=vec2(p.r+32.0*min(31.0,slice+1.0)+.5,p.g+.5)/vec2(1024.0,32.0);
    return mix(textureLod(lut,uv0,0.0).rgb,textureLod(lut,uv1,0.0).rgb,fract(p.b));
}
vec3 eval_srgb(vec3 linear) {
    return mix(1.055*pow(max(linear,vec3(0.0)),vec3(1.0/2.4))-vec3(.055),linear*12.92,lessThanEqual(linear,vec3(.0031308)));
}
float eval_compensate_channel(float desired_srgb) {
    float display=clamp(desired_srgb,0.0,1.0);
    if(display<=0.0) return 0.0;
    float target=pow((display+.055)/1.055,2.4);
    float q=.5783549783549783*target+(.01/.3);
    float a=.88*(1.0-q),b=.6*(.1-q),c=.2*(.01-.3*q);
    // For allowed display[0,1], a is strictly positive and discriminant >=0.
    // Guards prevent a NaN if this helper is accidentally used outside contract.
    if(a<=0.0) return 0.0;
    float discriminant=b*b-4.0*a*c;
    if(discriminant<0.0) return 0.0;
    float root=sqrt(discriminant);
    float exposed=b>=0.0 ? -2.0*c/max(b+root,.00000001) : (-b+root)/(2.0*a);
    float linear=exposed/1.12;
    if(linear<0.0 || linear>1.0) return 0.0;
    float x=linear<=.0031308 ? linear*12.92 : 1.055*pow(linear,1.0/2.4)-.055;
    for(int i=0;i<6;i++) {
        float value=x*(x*(.305306011*x+.682171111)+.012522878);
        float slope=.915918033*x*x+1.364342222*x+.012522878;
        x=clamp(x-(value-linear)/slope,0.0,1.0);
    }
    return x;
}
vec3 eval_actor_albedo(vec3 authored_prepost, vec2 source_uv, sampler2D lut,
    float exposure_ev, vec2 vignette_center, float vignette_intensity,
    float vignette_smoothness, vec3 vignette_color, float vignette_roundness) {
    vec2 d=abs(source_uv-vignette_center)*(vignette_intensity*3.0);
    d.x*=vignette_roundness; // 1 if unrounded, viewport aspect if rounded.
    float radial=pow(clamp(1.0-dot(d,d),0.0,1.0),vignette_smoothness*5.0);
    vec3 source=authored_prepost*mix(vignette_color,vec3(1.0),radial)*exp2(exposure_ev);
    vec3 display=eval_srgb(eval_grade_lut(lut,eval_aces_rgb(source)));
    return vec3(eval_compensate_channel(display.r),eval_compensate_channel(display.g),eval_compensate_channel(display.b));
}
