shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: BoundedContactFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _Primary = vec4(.65, .65, .65, 1.0);
uniform vec4 _Core = vec4(1.0, 1.0, 1.0, 1.0);
uniform float _Age = 0.0;

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

    //Private346 pressure roles; old mote role1 falls through byte-identical bounded formula.
    if(input_effect.x>40.5){
        float gain=max(0.0,input_color.r);float alpha=input_color.a;vec3 rgb;
        float age=max(0.0,_Age);
        if(input_effect.x<41.5){
            float u=clamp(input_effect.y,0.0,1.0),v=clamp(input_uv.y,0.0,1.0);
            // One broad low-frequency belly, asymmetric across its curve; no repeated fibres/parallel bars.
            float cross_distance=(v-.57)/.42;float across=exp(-cross_distance*cross_distance);
            float belly_distance=(u-.43)/.24;float belly=exp(-belly_distance*belly_distance);
            float density=across*(.48+.52*belly);
            rgb=mix(vec3(.018,.055,.026),vec3(.12,.38,.08),density)*gain;
            alpha*=smoothstep(0.0,.16,v)*(1.0-smoothstep(.82,1.0,v))*(.62+.38*belly);
            // A single concave-side bite opens unevenly after impact, then the outer pressure is consumed.
            float opening=smoothstep(.020,.065,age);
            float mouth_distance=(u-.57)/.15;float mouth=exp(-mouth_distance*mouth_distance);
            float notch=smoothstep(.25,.70,mouth)*smoothstep(0.0,.18,opening)*(1.0-smoothstep(opening-.10,opening+.10,v));
            float front=1.0-.88*smoothstep(.035,.100,age);
            float main_remaining=1.0-smoothstep(front-.05,front+.025,u);
            float short_outer=smoothstep(.68,.74,u)*(1.0-smoothstep(.86,.94,u))*(1.0-smoothstep(.055,.095,age));
            alpha*=(1.0-notch)*max(main_remaining,short_outer)*(1.0-smoothstep(.100,.150,age));
        }else if(input_effect.x<42.5){
            rgb=vec3(.52,.68,.23)*gain;
            alpha*=smoothstep(0.0,.16,input_uv.y)*smoothstep(0.0,.16,1.0-input_uv.y);
            alpha*=(1.0-smoothstep(.025,.075,age));
        }else{
            //No role43 emitted by349; retain old subordinate-role fallback for cached empty padding.
            float open_edge=smoothstep(.06,.28,input_uv.y-.07*sin(input_uv.x*5.0));
            float across=1.0-smoothstep(.45,1.0,abs(input_uv.y-.52)*2.0);
            alpha*=open_edge*across*(1.0-smoothstep(.55,1.0,input_uv.x));
            rgb=mix(vec3(.24,.22,.18),vec3(.14,.145,.13),input_uv.x)*gain;
        }
        alpha=clamp(alpha,0.0,1.0);return vec4(rgb*alpha,alpha);
    }
    vec2 centered=input_uv*2.0-1.0;
    float radial=saturate(1.0-dot(centered,centered));
    float edge=input_effect.x>.5?radial*radial:saturate(min(input_uv.y,1.0-input_uv.y)*12.0);
    float alpha=saturate(input_color.a*edge);
    vec3 rgb=mix(_Primary.rgb,_Core.rgb,radial*.55)*max(0.0,input_color.r);
    return vec4(rgb*alpha,alpha);
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
