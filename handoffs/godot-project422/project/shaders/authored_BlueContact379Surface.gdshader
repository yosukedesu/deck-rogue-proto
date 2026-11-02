shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
uniform bool use_authored_color = false;
uniform float _Age = 0.0;
varying vec4 authored_color;
void vertex() { authored_color = CUSTOM0; }
vec3 output_rgb(vec3 c) {
    if (!OUTPUT_IS_SRGB) return c;
    return mix(1.055 * pow(max(c, vec3(0.0)), vec3(1.0 / 2.4)) - vec3(.055), c * 12.92, lessThanEqual(c, vec3(.0031308)));
}
void fragment() {
    vec4 col = use_authored_color ? authored_color : COLOR;
    float role = UV2.x;
    vec3 rgb;
    float alpha;
    if (role < .5) {
        // Authored water surface interior crop; curved receiver-normalized mesh supplies the cutting silhouette.
        vec2 uv = vec2(clamp(UV.x + _Age * .20, 0.0, 1.0), mix(.32, .67, 1.0 - UV.y));
        vec4 water = texture(_BaseMap, uv);
        float edge = smoothstep(.0, .10, UV.y) * (1.0 - smoothstep(.88, 1.0, UV.y));
        float foam = pow(max(0.0, (water.r + water.g + water.b) / 3.0), 1.4);
        rgb = water.rgb * vec3(.75, 1.12, 1.22) + vec3(.10, .22, .26) * foam;
        alpha = water.a * edge * col.a;
    } else if (role < 1.5) {
        vec2 p = UV * 2.0 - 1.0;
        float sphere = max(0.0, 1.0 - dot(p, p));
        rgb = mix(vec3(.015, .19, .32), vec3(.50, .85, .93), sphere * sphere);
        alpha = smoothstep(0.0, .35, sphere) * col.a;
    } else {
        vec4 ice = texture(_BaseMap, vec2(UV.x, 1.0 - UV.y));
        float facet = clamp((role - 3.0) * 2.0, 0.0, .40);
        // Continuous density across irregular cells. No per-triangle UV rim or radial icon lines.
        rgb = ice.rgb * mix(vec3(.30, .70, .91), vec3(.58, .90, 1.0), facet);
        float vein = smoothstep(.50, .83, max(ice.r, max(ice.g, ice.b)));
        rgb += vec3(.025, .06, .07) * vein;
        alpha = ice.a * col.a;
    }
    ALBEDO = output_rgb(rgb * max(col.r, .72));
    ALPHA = clamp(alpha, 0.0, .96);
}
