shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Frozen Unity source: SystemOutcomeFx.shader. Analytical UV/UV2 remain unchanged.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _RoleShadow = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RolePrimary = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleEdge = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 _RoleCore = vec4(1.0, 1.0, 1.0, 0.0);
uniform float _Kind = 0.0;
uniform float _VisualAge = 0.0;

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

   float OutcomeSmooth(float x){float q=saturate(x);return q*q*(3.0-2.0*q);}
   
vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

    float coverage=input_color.a;vec3 linearRgb=input_color.rgb;float across=max(0.0,1.0-abs(2.0*input_uv.y-1.0));
    if(_Kind>.5 && _Kind<1.5){
     float d0=abs(input_uv.x-fract(.87-1.6*_VisualAge));d0=min(d0,1.0-d0);
     float d1=abs(input_uv.x-fract(.87+1.6*_VisualAge));d1=min(d1,1.0-d1);
     float q=max(exp(-160.0*d0*d0),exp(-160.0*d1*d1));
     vec3 role=q<.4?mix(_RolePrimary.rgb,_RoleEdge.rgb,q/.4):mix(_RoleEdge.rgb,_RoleCore.rgb,(q-.4)/.6);
     linearRgb=role*input_color.r;coverage*=pow(across,.55);
    }else if(_Kind>1.5 && _Kind<2.5){linearRgb=_RolePrimary.rgb*input_color.r;}
    else if(_Kind>2.5 && _Kind<3.5){float front=1.0-OutcomeSmooth((_VisualAge-.018)/.145);float burn=1.0-OutcomeSmooth((input_uv.x-front+.08)/.16);linearRgb=_RoleEdge.rgb*input_color.r;coverage*=pow(across,.55)*burn;}
    else if(_Kind>3.5 && _Kind<4.5){linearRgb=_RoleEdge.rgb*input_color.r;coverage*=pow(across,.75);}
    else if(_Kind>4.5 && _Kind<5.5){vec3 role=across<.3?mix(_RoleShadow.rgb,_RolePrimary.rgb,across/.3):mix(_RolePrimary.rgb,_RoleEdge.rgb,(across-.3)/.7);linearRgb=role*input_color.r;}
    else if(_Kind>5.5 && _Kind<6.5){float heat=pow(across,2.3)*(1.0-.68*input_uv.x);vec3 role=heat<.22?mix(_RoleShadow.rgb,_RolePrimary.rgb,heat/.22):heat<.70?mix(_RolePrimary.rgb,_RoleEdge.rgb,(heat-.22)/.48):mix(_RoleEdge.rgb,_RoleCore.rgb,(heat-.70)/.30);linearRgb=role*input_color.r;coverage*=pow(across,.65);}
    else if(_Kind>6.5 && _Kind<7.5){coverage*=pow(across,.45);}
    else if(_Kind>7.5){float gap=1.0-.8*OutcomeSmooth((sin(17.0*input_uv.x+input_effect.x)-.1)/.75);float fall=1.0-OutcomeSmooth((_VisualAge*3.5-input_uv.x)/.23);linearRgb=_RoleEdge.rgb*input_color.r;coverage*=pow(across,.6)*gap*fall;}
    coverage=saturate(coverage);return vec4(linearRgb*coverage,coverage);
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
