shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: IceCrystalFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _RoleShadow = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RolePrimary = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleEdge = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleCore = vec4(1.0, 1.0, 1.0, 0.0);
uniform float _Kind = 0.0;
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
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

vec3 authored_decode_rgb(vec3 srgb) {
    vec3 low = srgb / 12.92;
    vec3 high = pow((srgb + vec3(0.055)) / 1.055, vec3(2.4));
    return mix(high, low, lessThanEqual(srgb, vec3(0.04045)));
}
vec4 authored_sample_texel(sampler2D source_tex, ivec2 pixel, ivec2 extent) {
    pixel = clamp(pixel, ivec2(0), extent - ivec2(1));
    vec4 rgba = texelFetch(source_tex, pixel, 0);
    return vec4(authored_decode_rgb(rgba.rgb), rgba.a);
}
// source_color supplies linear filtering on Forward+/Mobile. Compatibility
// samples encoded RGB, so decode each bilinear tap before interpolation, once.
vec4 authored_sample(sampler2D source_tex, vec2 source_uv) {
    if (!OUTPUT_IS_SRGB) return texture(source_tex, source_uv);
    ivec2 extent = textureSize(source_tex, 0);
    vec2 texel = source_uv * vec2(extent) - vec2(0.5);
    ivec2 corner = ivec2(floor(texel));
    vec2 weight = fract(texel);
    vec4 top = mix(authored_sample_texel(source_tex, corner, extent),
        authored_sample_texel(source_tex, corner + ivec2(1, 0), extent), weight.x);
    vec4 bottom = mix(authored_sample_texel(source_tex, corner + ivec2(0, 1), extent),
        authored_sample_texel(source_tex, corner + ivec2(1, 1), extent), weight.x);
    return mix(top, bottom, weight.y);
}

float saturate(float x) { return clamp(x, 0.0, 1.0); }

   float IceSmooth(float x){x=saturate(x);return x*x*(3.0-2.0*x);}
   vec3 IceRamp(float q){if(q<=.20)return mix(_RoleShadow.rgb,_RolePrimary.rgb,saturate(q/.20));if(q<=.65)return mix(_RolePrimary.rgb,_RoleEdge.rgb,saturate((q-.20)/.45));return mix(_RoleEdge.rgb,_RoleCore.rgb,saturate((q-.65)/.35));}
   
vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {
float a=saturate(input_color.a);vec3 c=input_color.rgb;
    if(_Kind<.5){vec4 tex=authored_sample(_BaseMap, vec2((input_uv).x, 1.0-(input_uv).y));float q=saturate(pow(max(0.0,dot(tex.rgb,vec3(.2126,.7152,.0722))),1.35));c=IceRamp(q)*input_color.r;a*=saturate(tex.a)*(1.0-IceSmooth(((2.0*input_uv.x+input_uv.y)*.5-input_effect.x)/.045));a*=step(0.0,input_uv.x)*step(input_uv.x,1.0)*step(0.0,input_uv.y)*step(input_uv.y,1.0);}
    else if(_Kind<1.5){float coverage=pow(max(0.0,1.0-length(2.0*input_uv-1.0)),.70);c=mix(_RolePrimary.rgb,_RoleCore.rgb,coverage)*input_color.r;a*=coverage;}
    return vec4(c*a,a);
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
