shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
//415 exact Toss/Boomerang contact contrast. Original coverage/alpha and procedural mote surface retained.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _Primary = vec4(.65, .65, .65, 1.0);
uniform vec4 _Core = vec4(1.0, 1.0, 1.0, 1.0);

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

vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

    vec2 centered=input_uv*2.0-1.0;
    float radial=saturate(1.0-dot(centered,centered));
    float edge=input_effect.x>.5?radial*radial:saturate(min(input_uv.y,1.0-input_uv.y)*12.0);
    float alpha=saturate(input_color.a*edge);
    vec3 rgb=mix(_Primary.rgb,_Core.rgb,radial*.55);
    if(input_effect.y>.5 && input_effect.y<1.5){
        //Chipped, irregular junk with an asymmetric fracture and muted material.
        float cut=min(min(input_uv.x+input_uv.y-.18,1.0-input_uv.x+input_uv.y-.13),
                      min(2.0-input_uv.x-input_uv.y-.28,input_uv.x+1.0-input_uv.y-.12));
        cut=min(cut,.84+2.0*abs(input_uv.x-.41)-input_uv.y);
        alpha*=smoothstep(0.0,.018,cut);
        float facet=smoothstep(.42,.49,input_uv.y-.24*input_uv.x);
        vec3 material=_Primary.rgb*vec3(.56,.43,.32)*mix(.76,1.12,facet);
        float fracture=1.0-smoothstep(.008,.025,abs(input_uv.y-(.35+.28*input_uv.x)));
        material*=1.0-.44*fracture;
        float border=min(min(input_uv.x,1.0-input_uv.x),min(input_uv.y,1.0-input_uv.y));
        material*=mix(.45,1.0,smoothstep(.02,.10,min(border,cut)));
        float glint=(1.0-smoothstep(.012,.040,abs(input_uv.y-(.94-.10*input_uv.x))))
                   *smoothstep(.08,.22,input_uv.x)*(1.0-smoothstep(.58,.75,input_uv.x));
        rgb=mix(material,_Core.rgb*.65,glint*.75);
    }else if(input_effect.y>1.5){
        //One asymmetric curved blade across both quads, with a genuine open inner gap.
        float u=input_uv.x;
        float center=.24+.57*sin(3.14159265*pow(clamp(u,0.0,1.0),.78))+.08*u;
        float taper=pow(max(0.0,sin(3.14159265*u)),.62);
        float width=.070*taper;
        float dist=abs(input_uv.y-center);
        float coverage=1.0-smoothstep(max(0.0,width-.012),width+.012,dist);
        alpha=input_color.a*coverage;
        float edgeGlint=(1.0-smoothstep(.010,.025,abs(input_uv.y-center-width*.65)))
                       *smoothstep(.08,.24,u)*(1.0-smoothstep(.72,.91,u));
        vec3 metal=_Primary.rgb*vec3(.55,.58,.61);
        float bevel=smoothstep(-width,width,input_uv.y-center);
        metal*=mix(.56,1.22,bevel);
        rgb=mix(metal,_Core.rgb*.80,edgeGlint*.78);
    }
    rgb*=max(0.0,input_color.r);
    return vec4(rgb*alpha,alpha);
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
