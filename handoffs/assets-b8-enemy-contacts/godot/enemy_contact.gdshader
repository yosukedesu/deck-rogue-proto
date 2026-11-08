shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, shadows_disabled;

// Image colors are authored sRGB; Godot converts source_color to linear here.
// This shader owns no clock, light, events, position or gameplay state.
uniform sampler2D contact_atlas : source_color, filter_nearest, repeat_disable;
uniform float opacity : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec4 texel = texture(contact_atlas, UV);
    if (texel.a < 0.5 || opacity <= 0.0) { discard; }
    ALBEDO = texel.rgb;
    ALPHA = texel.a * opacity;
}
