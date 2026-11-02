shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: BoundedContactFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform float _Age = 0.0;
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
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

    //352 raw face only. Existing role1 motes retain the original shared shader path.
    if(input_effect.x>57.5){
        vec2 image_uv=vec2((48.0+1472.0*input_uv.x)/1536.0,
            (182.0+664.0*(1.0-input_uv.y))/1024.0);
        vec4 density=texture(_BaseMap,image_uv);
        float u=input_uv.x,v=input_uv.y;
        float contour=u+.07*sin(11.0*v+3.0*u)+.08*(density.b-density.r);
        float front=-.15+.30*smoothstep(0.0,.037,_Age)
            +.43*smoothstep(.037,.070,_Age)+.32*smoothstep(.070,.100,_Age)
            +.40*smoothstep(.100,.150,_Age);
        float remain=smoothstep(front-.055,front+.055,contour);
        float alpha=clamp(input_color.a*density.a*remain,0.0,1.0);
        vec3 rgb=density.rgb*max(0.0,input_color.r);
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
