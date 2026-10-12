shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: EarthStompSurfaceR2.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform float _Age = 0.0;
uniform float _Front = 0.0;
uniform float _Mode = 0.0;
uniform vec4 _RoleShadow = vec4(0.0666259386438, 0.0466650863369, 0.031896033073, 1.0);
uniform vec4 _RolePrimary = vec4(0.287440837727, 0.174647403656, 0.0843762115441, 1.0);
uniform vec4 _RoleEdge = vec4(0.485149940056, 0.327778098057, 0.187820772301, 1.0);
uniform vec4 _RoleCore = vec4(0.693871761292, 0.539479489012, 0.3515325995, 1.0);
uniform float _Kind = 0.0;
uniform float _Emission = 1.0;
uniform float _Coverage = 1.0;
uniform sampler2D _GrainTex : source_color, filter_linear, repeat_enable;

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
    pixel = (pixel % extent + extent) % extent;
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

float Smooth01(float x){ x=saturate(x); return x*x*(3.0-2.0*x); }
const vec4 Cracks[17]=vec4[17](
vec4(0.0,0.0,.30,.06),vec4(.30,.06,.60,0.0),vec4(.60,0.0,.91,.12),vec4(.30,.06,.49,.27),vec4(.49,.27,.67,.35),vec4(.60,0.0,.77,-.20),
vec4(0.0,0.0,-.27,.04),vec4(-.27,.04,-.56,-.07),vec4(-.56,-.07,-.94,-.03),vec4(-.27,.04,-.43,.28),vec4(-.43,.28,-.64,.37),
vec4(0.0,0.0,.04,-.23),vec4(.04,-.23,-.10,-.46),vec4(-.10,-.46,-.32,-.54),vec4(.04,-.23,.26,-.40),vec4(0.0,0.0,-.03,.22),vec4(-.03,.22,.08,.48));
float EarthField(vec2 p,float age,float alpha,float front,float linearGrain,out float core){
 float hit=0.0;
 for(int i=0;i<17;i++){
  vec2 a=Cracks[i].xy,b=Cracks[i].zw;vec2 d=b-a;float q=saturate(dot(p-a,d)/dot(d,d));float dist=length(p-a-q*d);float w=(.007+.002*float(i%3))*(1.0-.52*q);
  hit=max(hit,(1.0-Smooth01((dist-w*.38)/(w*.62)))*(1.0-Smooth01((length(p)-front)/.055)));
 }
 float wave=0.0;
 for(int i=0;i<2;i++){
  float birth=i==0?0.0:.075,speed=i==0?3.6:2.6,a=age-birth;float radius=.14+speed*max(0.0,a);vec2 e=p*vec2(1.0,1.65);
  float cover=(1.0-Smooth01(abs(length(e)-radius)/.012))*(1.0-Smooth01(a/.190))*Smooth01(a/.010);
  float segment=.32+.68*Smooth01((sin(atan(e.y,e.x)*7.0+birth*19.0)-.2)/.5);wave=max(wave,cover*segment*.45);
 }
 core=hit*.65;return max(hit,wave)*alpha*(.76+.24*linearGrain);
}
float StompField(vec2 p,float age,float alpha){
 float a=max(0.0,age);vec2 e=p*vec2(1.0,1.4);float radius=.13+6.4*a;
 float band=1.0-Smooth01(abs(length(e)-radius)/(.020+.010*Smooth01(a/.10)));
 float theta=atan(e.y,e.x);float variation=sin(3.0*theta+.4)+.55*sin(7.0*theta-.9)+.25*sin(11.0*theta+1.1);
 return band*alpha*Smooth01((variation-.15)/.70);
}
float DustCoverage(vec2 uv,float phase){vec2 p=2.0*uv-1.0;float sn=sin(8.0*p.x+5.0*p.y+phase);return pow(max(0.0,1.0-length(p)),1.25)*(.70+.30*(sn*sn));}

vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

                float alpha;
                vec3 rgb;
                if (_Kind > 0.5)
                {
                    
                    alpha = DustCoverage(input_uv, input_effect.x) * input_color.a;
                    rgb = input_color.rgb;
                }
                else
                {
                    
                    vec2 p = (input_uv - 0.5) * vec2(2.50, 1.70);
                    float core = 0.4;
                    if (_Mode < 0.5)
                    {
                        
                        vec3 grainRgb = authored_sample(_GrainTex, vec2(( input_uv).x, 1.0-( input_uv).y)).rgb;
                        float grain = dot(grainRgb, vec3(0.2126, 0.7152, 0.0722));
                        alpha = EarthField(p, _Age, input_color.a, _Front, grain, core);
                    }
                    else
                    {
                        alpha = StompField(p, _Age, input_color.a);
                    }
                    
                    rgb = mix(_RolePrimary.rgb, _RoleCore.rgb, core) * input_color.rgb;
                }
                alpha *= _Coverage;
                
                return vec4(rgb * _Emission * alpha, alpha);
            
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
