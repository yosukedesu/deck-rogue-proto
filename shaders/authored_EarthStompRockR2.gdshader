shader_type spatial;
render_mode blend_mix, cull_back, depth_draw_never, fog_disabled, diffuse_lambert, specular_schlick_ggx;
// Frozen EarthStompRockR2: linear host tint, perceptual roughness .86,
// dielectric metallic workflow, and edge emission .22 * _Emission.
// Godot PBR is the native analogue; URP light/probe evaluation is not bit-identical.
uniform float _Mode = 0.0;
uniform vec4 _RoleShadow = vec4(0.0666259386438, 0.0466650863369, 0.031896033073, 1.0);
uniform vec4 _RoleCore = vec4(0.693871761292, 0.539479489012, 0.3515325995, 1.0);
uniform vec4 _RolePrimary = vec4(0.287440837727, 0.174647403656, 0.0843762115441, 1.0);
uniform vec4 _RoleEdge = vec4(0.485149940056, 0.327778098057, 0.187820772301, 1.0);
uniform float _Emission=1.0;

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
void fragment() {
    vec4 input_color = use_authored_color ? authored_color : COLOR;
    ALBEDO = authored_output_rgb(_RolePrimary.rgb * input_color.rgb);
    ROUGHNESS = 0.86;
    METALLIC = 0.0;
    SPECULAR = 0.5; // Godot dielectric F0=.04, matching URP's metallic workflow.
    AO = 1.0;
    EMISSION = authored_output_rgb(_RoleEdge.rgb * (0.22 * _Emission) * input_color.rgb);
    // blend_mix fades diffuse, specular, and emission together exactly once.
    ALPHA = input_color.a;
}
