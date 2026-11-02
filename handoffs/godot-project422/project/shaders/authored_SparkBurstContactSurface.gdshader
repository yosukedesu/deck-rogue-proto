shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: ScatterProfileFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _RolePrimary = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleEdge = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleCore = vec4(1.0, 1.0, 1.0, 0.0);
uniform float _Kind = 0.0;
uniform float _Family = 0.0;

// ArrayMesh COLOR is RGBA8. The bridge enables CUSTOM0 RGBA32F to retain
// authored HDR gains and linear tint/alpha without COLOR quantization.
uniform bool use_authored_color = false;
varying vec4 authored_color;
void vertex() {
    authored_color = CUSTOM0;
}

// Unity roles and vertex payloads are already linear. Compatibility alone expects
// sRGB ALBEDO/EMISSION and decodes them inside Godot after fragment().
vec3 authored_output_rgb(vec3 linear_rgb) {
    if (!OUTPUT_IS_SRGB) return linear_rgb;
    vec3 low = linear_rgb * 12.92;
    vec3 high = 1.055 * pow(max(linear_rgb, vec3(0.0)), vec3(1.0 / 2.4)) - vec3(0.055);
    return mix(high, low, lessThanEqual(linear_rgb, vec3(0.0031308)));
}
float saturate(float x) { return clamp(x, 0.0, 1.0); }

float ScatterSoft(vec2 p,float exponent){return pow(max(0.0,1.0-length(p)),exponent);}
vec4 ScatterFragment(vec2 uv,int family,bool core,float phase,float opacity,float gain,vec3 primary,vec3 edge,vec3 bright){
 vec2 p=2.0*uv-1.0;float cover=1.0,factor=0.0;vec3 rgb;
 if(core){cover=ScatterSoft(p,.8);rgb=mix(primary,bright,cover);}
 else if(family==3){vec2 q=p+vec2(.11*sin(3.0*p.y+phase),.08*sin(4.0*p.x-phase));float sn=sin(8.0*p.x+5.0*p.y+phase);float ed=saturate(min(min(uv.x,uv.y),min(1.0-uv.x,1.0-uv.y))/.10);cover=ScatterSoft(q,.75)*(.85+.15*sn*sn)*(ed*ed*(3.0-2.0*ed));rgb=mix(primary,bright,cover*.85);}
 else if(family==2){factor=(uv.x+uv.y>1.0)?.78:.20;rgb=mix(primary,bright,factor);}
 else{factor=saturate((1.0-abs(p.y))*(.95-.6*uv.x));rgb=mix(primary,family==0?edge:bright,factor);}
 float a=opacity*cover;return vec4(rgb*gain*a,a);
}

vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

    float role=input_effect.x;
    if(role>60.5 && role<63.5){
        float u=clamp(input_uv.x,0.0,1.0),v=clamp(input_uv.y,0.0,1.0),cover=1.0;vec3 rgb;
        if(role<61.5){
            float du=(u-.46)/.40,dv=(v-(.47+.12*u))/.32;
            float belly=exp(-(du*du+dv*dv));
            cover=smoothstep(0.0,.12,min(v,1.0-v))*smoothstep(0.0,.10,u);
            rgb=mix(vec3(.30,.055,.012),vec3(.80,.24,.035),belly);
            float hotU=(u-.68)/.21,hotV=(v-(.43+.18*u))/.18;
            rgb=mix(rgb,vec3(1.0,.91,.68),.42*exp(-(hotU*hotU+hotV*hotV)));
        }else if(role<62.5){
            cover=smoothstep(0.0,.16,u)*smoothstep(0.0,.20,1.0-u)*smoothstep(0.0,.18,min(v,1.0-v));
            rgb=vec3(1.0,.91,.68);
        }else{
            cover=smoothstep(0.0,.10,min(v,1.0-v));
            rgb=mix(vec3(.30,.055,.012),vec3(.80,.24,.035),smoothstep(.05,.48,u));
            rgb=mix(rgb,vec3(1.0,.91,.68),.82*smoothstep(.52,.88,u));
        }
        float a=clamp(input_color.a,0.0,1.0)*cover;
        return vec4(rgb*input_color.r*a,a);
    }
    return ScatterFragment(input_uv,int(_Family),_Kind<0.5,input_effect.x,saturate(input_color.a),input_color.r,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb);
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
