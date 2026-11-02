shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: PhysicalContactFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _RoleShadow = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RolePrimary = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleEdge = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleCore = vec4(1.0, 1.0, 1.0, 0.0);
uniform float _UseTexture = 1.0;
uniform float _BluntAccent = 0.0;
uniform float _Gain = 1.0;
uniform float _UseBeam = 0.0;
uniform float _VisualAge = 0.0;
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

vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

    float coverage=input_color.a;vec3 linearRgb=input_color.rgb;
    if(_UseBeam>.5){
     float u=input_uv.x,v=input_uv.y,t=_VisualAge;
     float center=saturate(1.0-abs(2.0*v-1.0)),c=pow(center,6.0);
     vec3 role=c<.55?mix(_RolePrimary.rgb,_RoleEdge.rgb,c/.55):mix(_RoleEdge.rgb,_RoleCore.rgb,(c-.55)/.45);
     float field=.5+.25*sin(30.0*u-120.0*t)+.15*sin(70.0*u-170.0*t+2.0*v)+.10*sin(130.0*u-240.0*t-4.0*v);
     float flow=.28+.72*saturate(2.5*(field-.3));
     coverage*=pow(center,.55)*flow;linearRgb=role*input_color.r;
    }else if(_UseTexture>.5){
     vec4 tex=authored_sample(_BaseMap, vec2((input_uv).x, 1.0-(input_uv).y)); 
     coverage*=tex.a*step(0.0,input_uv.x)*step(input_uv.x,1.0)*step(0.0,input_uv.y)*step(input_uv.y,1.0);
     float y=saturate(dot(tex.rgb,vec3(.2126,.7152,.0722)));
     vec3 role=y<.35?mix(_RoleShadow.rgb,_RolePrimary.rgb,y/.35):y<.80?mix(_RolePrimary.rgb,_RoleEdge.rgb,(y-.35)/.45):_BluntAccent>.5?mix(_RoleEdge.rgb,_RoleCore.rgb,(y-.80)/.20):_RoleEdge.rgb;
     linearRgb=role*input_color.r; 
    }
    coverage=saturate(coverage);
    return vec4(linearRgb*_Gain*coverage,coverage); 
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
