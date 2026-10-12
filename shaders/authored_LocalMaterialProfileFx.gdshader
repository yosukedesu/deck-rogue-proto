shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: LocalMaterialProfileFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _RoleShadow = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RolePrimary = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleEdge = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleCore = vec4(1.0, 1.0, 1.0, 0.0);
uniform float _Kind = 0.0;
uniform float _Family = 0.0;
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
uniform sampler2D _LookupMap : source_color, filter_linear, repeat_disable;
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
float LSmooth(float x){x=saturate(x);return x*x*(3.0-2.0*x);}
vec3 LColor(float q,vec3 shadow,vec3 body,vec3 edge,vec3 core){q=saturate(q);return q<.2?mix(shadow,body,q/.2):q<.65?mix(body,edge,(q-.2)/.45):mix(edge,core,(q-.65)/.35);}

vec4 LocalFragment(vec2 uv,int kind,vec4 rgba,float opacity,float gain,float phase,vec3 shadow,vec3 body,vec3 edge,vec3 core,vec3 role){
 float a=saturate(opacity);vec3 rgb=role;vec2 p=2.0*uv-1.0;
 if(kind==0){rgb=LColor(dot(rgba.rgb,vec3(.2126,.7152,.0722)),shadow,body,edge,core);a*=rgba.a;}
 if(kind==1||kind==2){rgb=rgba.rgb;if(kind==1)a*=rgba.a;}
 if((kind==0||kind==1)&&(any(lessThan(uv,vec2(0.0)))||any(greaterThan(uv,vec2(1.0)))))a=0.0;
 if(kind==2)a*=LSmooth((1.0-abs(p.x)-abs(p.y))/.25);
 if(kind==3){float c=pow(max(0.0,1.0-length(p)),.7);a*=c;rgb=mix(body,core,c);}
 if(kind==4){float c=pow(max(0.0,1.0-length(p)+.045*sin(9.0*uv.x+phase)*sin(10.0*uv.y-phase)),.65)*LSmooth(min(min(uv.x,uv.y),min(1.0-uv.x,1.0-uv.y))/.10);a=min(a,.9)*c;rgb=mix(body,role,min(1.0,c*.9));}
 if(kind==5){float ed=abs(p.y);float ridge=exp(-70.0*(uv.y-.5)*(uv.y-.5));float q=saturate(.18+.42*ed+.38*ridge);rgb=mix(body,edge,q);a*=.80+.20*sin(3.141592653589793*uv.x);}
 if(kind==6)rgb=(uv.x+uv.y<1.0)?body:edge;
 return vec4(rgb*gain*a,a);
}

vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

    float roleIndex=input_effect.y;
    vec3 role=roleIndex<.5?_RoleShadow.rgb:roleIndex<1.5?_RolePrimary.rgb:roleIndex<2.5?_RoleEdge.rgb:_RoleCore.rgb;
    vec4 sampleRGBA=vec4(1.0,1.0,1.0,1.0);
    if(_Kind<1.5)sampleRGBA=authored_sample(_BaseMap, vec2((input_uv).x, 1.0-(input_uv).y));
    else if(_Kind<2.5)sampleRGBA=authored_sample(_LookupMap, vec2((input_uv).x, 1.0-(input_uv).y));
    return LocalFragment(input_uv,int(_Kind),sampleRGBA,saturate(input_color.a),input_color.r,input_effect.x,_RoleShadow.rgb,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb,role);
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
