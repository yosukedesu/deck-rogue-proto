shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: BoundedContactFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _Primary = vec4(.65, .65, .65, 1.0);
uniform vec4 _Core = vec4(1.0, 1.0, 1.0, 1.0);

// ArrayMesh COLOR is RGBA8. The bridge enables CUSTOM0 RGBA32F to retain
// authored HDR gains and linear tint/alpha without COLOR quantization.
uniform bool use_authored_color = false;
uniform float _Age=0.0;
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
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
    //314 exact owned receiving pressure only. Old neutral motes remain role0 below.
    float role=input_effect.y;
    if(role>.5){
        float u=input_uv.x,v=input_uv.y;
        if(role>2.5){
            float band=1.0-smoothstep(.20,.50,abs(v-.5));
            float ends=smoothstep(0.0,.18,u)*(1.0-smoothstep(.65,1.0,u));
            float alpha=clamp(input_color.a*band*ends,0.0,1.0);
            vec3 rgb=vec3(.40,.38,.32)*max(0.0,input_color.r);
            return vec4(rgb*alpha,alpha);
        }
        if(role>1.5){
            float shape=smoothstep(0.0,.10,min(min(u,1.0-u),min(v,1.0-v)));
            //318 dark compression crease contrasts with hurt-white; not an opaque black disk.
            float creaseAxis=.46+.10*sin(3.14159265*u);
            float crease=1.0-smoothstep(.045,.22,abs(v-creaseAxis));
            float shortEdge=smoothstep(.32,.48,u)*(1.0-smoothstep(.68,.83,u))
                *(1.0-smoothstep(.025,.075,abs(v-(creaseAxis+.12))));
            vec3 rgb=mix(vec3(.20,.19,.17),vec3(.045,.040,.032),crease);
            rgb=mix(rgb,vec3(.66,.59,.45),shortEdge)*max(0.0,input_color.r);
            float alpha=clamp(input_color.a*shape,0.0,1.0);
            return vec4(rgb*alpha,alpha);
        }
        float transverse=smoothstep(0.0,.18,v)*(1.0-smoothstep(.70,1.0,v));
        //315 main density only: no baked impact flare; existing core remains a separate role.
        vec4 density=texture(_BaseMap,vec2(.35+.23*u,.365+.35*(1.0-v)-.11*u));
        vec3 rgb=density.rgb*max(0.0,input_color.r);
        float contour=u+.10*sin(8.0*v+u);
        float front=mix(-.15,1.15,smoothstep(.035,.120,_Age));
        float remain=smoothstep(front-.025,front+.025,contour);
        float alpha=clamp(input_color.a*density.a*transverse*remain,0.0,1.0);
        return vec4(rgb*alpha,alpha);
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
