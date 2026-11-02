shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Exact ColdSweep material-only variant. Geometry, UV and alpha coverage remain from291.
// Shared authored_BoundedContactFx.gdshader stays unchanged. UV2.y identifies main/residue/old motes.
// Texture sampling alone flips Unity bottom-left V to Godot top-left V.
// Source formulas produce premultiplied linear RGBA; blend_mix receives straight RGB.
uniform vec4 _Primary = vec4(.65, .65, .65, 1.0);
uniform vec4 _Core = vec4(1.0, 1.0, 1.0, 1.0);

// ArrayMesh COLOR is RGBA8. The bridge enables CUSTOM0 RGBA32F to retain
// authored HDR gains and linear tint/alpha without COLOR quantization.
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
uniform float _Age = 0.0; // Existing material sample age; no separate clock.
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

//297: bounded density-following consumption. The initial material is unchanged.
float cold_consumption(vec2 uv, vec4 density, float begin, float end) {
    if (_Age <= begin) return 1.0;
    if (_Age >= end) return 0.0;
    float phase=smoothstep(begin,end,_Age);
    float lightness=dot(density.rgb,vec3(.2126,.7152,.0722));
    // Authored dark patches go first; irregular pale frost islands persist locally.
    // Cross-section offset prevents a straight guillotine edge through the two faces.
    float contour=uv.x+.42*(lightness-.32)+.14*sin(7.0*uv.y+3.0*uv.x);
    float front=mix(-.32,1.42,phase);
    return smoothstep(front-.045,front+.045,contour);
}

vec4 authored_fragment(vec2 input_uv, vec2 input_effect, vec4 input_color) {

    vec2 centered=input_uv*2.0-1.0;
    float radial=saturate(1.0-dot(centered,centered));
    float edge=input_effect.x>.5?radial*radial:saturate(min(input_uv.y,1.0-input_uv.y)*12.0);
    float alpha=saturate(input_color.a*edge);
    vec3 rgb=mix(_Primary.rgb,_Core.rgb,radial*.55)*max(0.0,input_color.r);
    // Vertex alpha, edge, gain and depth unchanged. Main coverage additionally uses raw alpha.
    if(input_effect.y>.5 && input_effect.y<1.5){
        // 293: authored irregular cold density, original PNG unchanged.
        // Rectangle is in image top-left coordinates; geometry UV remains Unity bottom-left.
        vec2 image_uv=vec2(mix(.06,.94,input_uv.x),mix(.12,.87,1.0-input_uv.y));
        vec4 frost=texture(_BaseMap,image_uv);
        rgb=frost.rgb*max(0.0,input_color.r);
        alpha*=frost.a*cold_consumption(input_uv,frost,.04,.145);
    }else if(input_effect.y>1.5){
        //295: short geometric frost facets keep a broken edge, not the old radial mist mask.
        // Crop retains cool shadow and irregular authored frost; no new raster or full outline.
        vec2 frost_uv=vec2(mix(.30,.48,input_uv.x),mix(.44,.64,1.0-input_uv.y));
        vec4 flake=texture(_BaseMap,frost_uv);
        float frost_face=input_effect.y>2.5?.18:.55;
        rgb=mix(flake.rgb,_Core.rgb,frost_face)*max(0.0,input_color.r);
        alpha=saturate(input_color.a)*flake.a;
        if(input_effect.y<2.5)alpha*=cold_consumption(input_uv,flake,.04,.105);
        else alpha*=cold_consumption(input_uv,flake,.085,.175);
    }
    // UV2.y==0 is the original mote color formula above, byte unchanged.
    return vec4(rgb*alpha,alpha);
   
}

void fragment() {
 vec4 result = authored_fragment(UV, UV2, use_authored_color ? authored_color : COLOR);
 float a = clamp(result.a, 0.0, 1.0);
 ALBEDO = authored_output_rgb(a > 0.0 ? result.rgb / a : vec3(0.0));
 ALPHA = a;
}
