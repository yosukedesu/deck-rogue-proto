shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: LightningDischargeFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _RoleShadow = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RolePrimary = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleEdge = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleCore = vec4(1.0, 1.0, 1.0, 0.0);
uniform float _Kind = 0.0;
uniform float _Age = 0.0;
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
float Smooth01(float q){q=saturate(q);return q*q*(3.0-2.0*q);}
float LightningClock(vec2 uv,float age){
 vec2 d=vec2(2.0*uv.x-477./887.,uv.y-(1.-442./887.));float r=length(d),theta=atan(d.y,d.x);
 float direction=dot(d,vec2(cos(2.35),sin(2.35)))/(r+1e-6);float leader=Smooth01((direction-.90)/.07);
 if(age<0.0){float q=Smooth01((age+.100)/.100),front=.68*(1.0-q);return .22*pow(q,1.8)*leader*Smooth01((r-front)/.045);}
 float birth=.013*saturate(r/.5);float branch=Smooth01((age-birth)/.005),start=max(.88*leader,branch);
 float end=.068+.042*(.5+.5*sin(5.0*theta+.7));return start*(1.0-Smooth01((age-.020)/(end-.020)));
}
vec3 LightningRamp(float q,vec3 shadow,vec3 primary,vec3 edge,vec3 core){
 q=saturate(q);if(q<=.12)return mix(shadow,primary,q/.12);if(q<=.55)return mix(primary,edge,(q-.12)/.43);return mix(edge,core,(q-.55)/.45);
}
vec4 LightningFragment(vec4 textureLinear,vec2 uv,float age,float opacity,vec3 shadow,vec3 primary,vec3 edge,vec3 core){
 float L=dot(textureLinear.rgb,vec3(.2126,.7152,.0722));float a=textureLinear.a*pow(max(0.0,L),.65)*LightningClock(uv,age)*opacity;
 vec3 rgb=LightningRamp(pow(max(0.0,L),1.20),shadow,primary,edge,core);return vec4(rgb*2.35*a,a);
}
float SoftCoverage(vec2 uv){return pow(max(0.0,1.0-length(2.0*uv-1.0)),.70);}

vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

    float opacity=saturate(input_color.a);
    if(_Kind<0.5) {
     
     vec4 textureLinear=authored_sample(_BaseMap, vec2((input_uv).x, 1.0-(input_uv).y));
     vec4 result=LightningFragment(textureLinear,input_uv,_Age,opacity,_RoleShadow.rgb,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb);
     result.rgb*=input_color.r; 
     return result; 
    }
    float coverage=SoftCoverage(input_uv);
    float alpha=coverage*opacity;
    float nativeGain=_Kind<1.5?3.0:2.1;
    vec3 rgb=mix(_RolePrimary.rgb,_RoleCore.rgb,coverage)*nativeGain*input_color.r;
    return vec4(rgb*alpha,alpha); 
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
