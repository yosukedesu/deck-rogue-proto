shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;
// Compatibility unshaded output MUST use colored ALBEDO. EMISSION-only air was
// historically rejected because it rendered black. No screen copy or blur.
uniform vec4 air_color : source_color = vec4(0.48,0.54,0.70,0.10);
uniform int air_kind = 0;
uniform float elapsed = 0.0;
uniform float phase = 0.0;
void fragment() {
    float mask;
    if (air_kind == 1) {
        // Tapered shaft, with energy continuing toward its lowered endpoint.
        float across = pow(max(sin(UV.x*3.14159265),0.0),2.6);
        mask = across*smoothstep(0.0,0.10,UV.y)*(1.0-smoothstep(0.67,1.0,UV.y));
        mask *= 0.86+0.14*sin(UV.y*18.0+phase+elapsed*0.09);
    } else if (air_kind == 2) {
        float d=length((UV-0.5)*2.0);
        mask=pow(max(0.0,1.0-d),2.0);
    } else {
        float edge = smoothstep(0.0,0.10,UV.x)*(1.0-smoothstep(0.90,1.0,UV.x));
        float bands = 0.77+0.12*sin(UV.x*24.0+elapsed*0.025+phase)+0.09*sin(UV.x*51.0-elapsed*0.013);
        mask=edge*pow(max(sin(UV.y*3.14159265),0.0),1.8)*bands;
    }
    ALBEDO=air_color.rgb;
    ALPHA=clamp(mask*air_color.a,0.0,0.22);
}
