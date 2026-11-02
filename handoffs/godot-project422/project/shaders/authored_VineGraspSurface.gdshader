shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
//329 exact owned VineGrasp only. New continuous organic body; old motes retain shared formula.
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

    if(input_effect.y>.5){
        vec2 image_uv=mix(vec2(.18,.28),vec2(.65,.76),vec2(input_uv.x,1.0-input_uv.y));
        vec4 density=texture(_BaseMap,image_uv);
        float edge=smoothstep(0.0,.045,min(min(input_uv.x,1.0-input_uv.x),min(input_uv.y,1.0-input_uv.y)));
        //332 preserve initial331 RGBA then consume outside density toward the true chest root.
        //Three positive C1 steps; contour bound[-.22,1.22], initial upperedge-.265, final loweredge1.245.
        float u=input_uv.x,v=input_uv.y;
        float contour=u+.10*sin(11.0*v+3.0*u)+.12*(density.g-density.r);
        float front=-.30+.78*smoothstep(0.0,1.0/30.0,_Age)
            +.34*smoothstep(1.0/30.0,2.0/30.0,_Age)
            +.46*smoothstep(2.0/30.0,.100,_Age);
        float consume=smoothstep(front-.035,front+.035,contour);
        float root=smoothstep(.86,.98,u);
        float remain=mix(consume,1.0-smoothstep(.070,.150,_Age),root);
        float alpha=clamp(input_color.a*density.a*edge*remain,0.0,1.0);
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
