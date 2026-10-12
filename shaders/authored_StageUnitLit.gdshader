shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque, fog_disabled;
// Core source: Assets/Resources/Shaders/StageUnitLit.shader (frozen audit).
// Includes: Include/HD2DCharLight.hlsl and Include/HD2DPixel.hlsl.
// All scalar/vector material uniforms are already source-linear; no source_color
// hint on them. Only encoded albedo/emission textures decode once per renderer.
// C# binds the fixed layout-camera basis. Godot sprite front +Z corresponds to
// Unity source front -Z after the port's world-Z reflection.
uniform sampler2D _BaseMap : source_color, filter_nearest, repeat_disable;
uniform sampler2D _NormalMap : filter_nearest, repeat_disable;
uniform sampler2D _EmissionMap : source_color, filter_nearest, repeat_disable;
uniform vec4 _BaseColor = vec4(1.0);
uniform float _Cutoff = 0.4;
uniform float _Flash = 0.0;
uniform float _Dissolve = 0.0;
uniform float _Fog = 1.0;
uniform float _Rim = 0.2;
// ShaderLab Color defaults are converted to linear by the binding.
uniform vec3 _RimColor = vec3(0.570481595, 0.638283215, 0.890005407);
uniform float _Receive = 0.6;
uniform float _AmbientScale = 1.0;
uniform float _HeroLift = 1.0;
uniform float _KeyFlip = 0.0;
uniform float _LocalLights = 0.0;
uniform vec3 _OutlineFloor = vec3(0.009116773, 0.007985828, 0.013046822);
uniform float _WhiteCap = 1.0;
uniform vec3 _BlackLift = vec3(0.0);
uniform vec2 _ShadeLift = vec2(0.0);
uniform vec3 _CharTint = vec3(1.0);
uniform float _CharSat = 1.0;
uniform float _HasNormal = 0.0;
uniform float _HasEmission = 0.0;
uniform float _EmissionIntensity = 1.6;
uniform float _NormalYSign = 1.0;
uniform vec4 _BodyShade = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec3 _CharKeyDir = vec3(0.0, -1.0, 0.0);
uniform vec3 _CharKeyColor = vec3(1.150000000, 1.023506218, 0.860676219);
uniform vec3 _CharAmbTop = vec3(0.354692061, 0.393123130, 0.522521554);
uniform vec3 _CharAmbBottom = vec3(0.125967388, 0.132868322, 0.187316668);
// No native-cookie/shadow approximation. Caller may supply source key visibility
// later; active char looks have cookieOnKey=0 and receiveShadows defaults to0.
uniform float _KeyVisibility = 1.0;
// Source-compatible point-light inputs, not Godot LightEnergy units. w in position
// is world range; w in color is1 for tagged attack light, 0 for permanent light.
uniform int _ActorLightCount = 0;
uniform vec4 _ActorLightPositionRange[8];
uniform vec4 _ActorLightColorKind[8];
uniform bool _FogEnabled = false;
uniform vec3 _FogColor = vec3(0.0);
uniform vec2 _FogRange = vec2(0.0, 1.0);
varying vec3 actor_world_position;
varying vec3 actor_right;
varying vec3 actor_up;
varying vec3 actor_front;
varying float actor_view_depth;

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
vec4 source_color_sample(sampler2D source_tex, vec2 source_uv) {
    // Source analytical UV is bottom-left; PNG/Godot sampling is top-left.
    vec4 c = textureLod(source_tex, vec2(source_uv.x, 1.0 - source_uv.y), 0.0);
    if (OUTPUT_IS_SRGB) c.rgb = authored_decode_rgb(c.rgb);
    return c;
}
vec3 source_normal_sample(vec2 source_uv) {
    vec3 t = textureLod(_NormalMap, vec2(source_uv.x, 1.0 - source_uv.y), 0.0).rgb;
    if (dot(t, t) < 0.0001) return vec3(0.0, 0.0, 1.0);
    vec3 n = t * 2.0 - vec3(1.0);
    n.z = max(n.z, 0.05);
    float len2 = dot(n, n);
    return len2 > 0.0001 ? n * inversesqrt(len2) : vec3(0.0, 0.0, 1.0);
}
vec3 source_texel_shift(vec2 texel, vec3 position_world) {
    vec2 dx = dFdx(texel), dy = dFdy(texel);
    vec3 px = dFdx(position_world), py = dFdy(position_world);
    float determinant = dx.x * dy.y - dy.x * dx.y;
    vec2 want = floor(texel) + vec2(0.5) - texel;
    if (abs(determinant) < 0.00000001) return vec3(0.0);
    float sx = (want.x * dy.y - dy.x * want.y) / determinant;
    float sy = (dx.x * want.y - want.x * dx.y) / determinant;
    if (abs(sx) > 8.0 || abs(sy) > 8.0) return vec3(0.0);
    return px * sx + py * sy;
}
void vertex() {
    actor_world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    actor_right = normalize(MODEL_MATRIX[0].xyz);
    actor_up = normalize(MODEL_MATRIX[1].xyz);
    actor_front = normalize(MODEL_MATRIX[2].xyz);
    actor_view_depth = -(MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).z;
}
void fragment() {
    vec2 source_uv = vec2(UV.x, 1.0 - UV.y);
    vec2 size = vec2(textureSize(_BaseMap, 0));
    vec2 texel = source_uv * size;
    vec2 uv_center = (floor(texel) + vec2(0.5)) / max(size, vec2(1.0));
    vec3 pos_center = actor_world_position + source_texel_shift(texel, actor_world_position);
    vec4 c = source_color_sample(_BaseMap, source_uv) * _BaseColor;
    if (c.a < _Cutoff) discard;
    if (_Dissolve > 0.0) {
        vec2 pixel = floor(source_uv * size);
        float noise = fract(sin(dot(pixel, vec2(12.9898, 78.233))) * 43758.5453);
        if (noise * 0.6 + (1.0 - source_uv.y) * 0.4 - _Dissolve * 1.02 < 0.0) discard;
    }
    vec3 albedo = c.rgb;
    vec3 ns = vec3(0.0, 0.0, 1.0);
    if (_HasNormal > 0.5) { ns = source_normal_sample(uv_center); ns.y *= _NormalYSign; }
    vec3 right = normalize(actor_right), up = normalize(actor_up), front = normalize(actor_front);
    vec3 nw = normalize(right * ns.x + up * ns.y + front * ns.z);
    vec3 to_key = -normalize(_CharKeyDir);
    vec3 key_sprite = vec3(dot(to_key, right), dot(to_key, up), dot(to_key, front));
    if (_KeyFlip > 0.5) key_sprite.x = -key_sprite.x;
    vec3 direct = _CharKeyColor * (clamp(dot(ns, key_sprite), 0.0, 1.0) * _KeyVisibility);
    vec3 local = vec3(0.0), attack_local = vec3(0.0);
    for (int i = 0; i < 8; i++) {
        if (i >= _ActorLightCount) break;
        vec3 delta = _ActorLightPositionRange[i].xyz - pos_center;
        float distance2 = max(dot(delta, delta), 0.00006103515625);
        float range = max(_ActorLightPositionRange[i].w, 0.0001);
        float range_ratio = distance2 / (range * range);
        float smooth_factor = max(0.0, 1.0 - range_ratio * range_ratio);
        float attenuation = smooth_factor * smooth_factor / distance2;
        float nd = clamp(dot(nw, delta * inversesqrt(distance2)), 0.0, 1.0);
        vec3 radiance = _ActorLightColorKind[i].rgb * attenuation;
        if (_ActorLightColorKind[i].w > 0.5) attack_local += radiance * (0.3 + 0.7 * nd);
        else local += radiance * nd;
    }
    direct += local * _LocalLights;
    vec3 ambient = mix(_CharAmbBottom, _CharAmbTop, clamp(nw.y * 0.5 + 0.5, 0.0, 1.0));
    vec3 lit = ambient * _AmbientScale + _Receive * direct;
    if (_BodyShade.x > 0.0) {
        float t = clamp((uv_center.y - _BodyShade.y) / max(_BodyShade.z - _BodyShade.y, 0.001), 0.0, 1.0);
        t = pow(max(t, 0.0001), max(_BodyShade.w, 0.05));
        lit *= 1.0 - clamp(_BodyShade.x, 0.0, 1.0) * (1.0 - t);
    }
    vec3 col = albedo * lit * _HeroLift;
    if (_ShadeLift.x > 0.0) {
        float sy = dot(col, vec3(0.2126, 0.7152, 0.0722));
        float knee = max(_ShadeLift.y, 0.001);
        float q = knee / (knee + max(sy, 0.0));
        col *= 1.0 + _ShadeLift.x * q * q;
    }
    col = max(col, min(albedo, _OutlineFloor));
    vec3 lift = clamp(_BlackLift, vec3(0.0), vec3(1.0));
    col = lift + col * (vec3(1.0) - lift);
    if (_WhiteCap < 0.999) {
        float cap = max(_WhiteCap, 0.001), knee = cap * 0.6;
        float luminance = dot(col, vec3(0.2126, 0.7152, 0.0722));
        if (luminance > knee) {
            float over = luminance - knee;
            float shoulder = knee + over / (1.0 + over / max(cap - knee, 0.001));
            col *= shoulder / luminance;
        }
    }
    if (_HasEmission > 0.5) {
        vec3 emission = source_color_sample(_EmissionMap, uv_center).rgb;
        float mask = clamp(max(emission.r, max(emission.g, emission.b)) * 8.0, 0.0, 1.0);
        col = mix(col, albedo * _EmissionIntensity, mask);
    }
    col += albedo * attack_local * 0.75;
    if (abs(_CharSat - 1.0) > 0.001) {
        float luminance = dot(col, vec3(0.2126, 0.7152, 0.0722));
        col = max(vec3(0.0), vec3(luminance) + (col - vec3(luminance)) * _CharSat);
    }
    if (_Rim > 0.0) {
        vec2 step_uv = vec2(1.0) / max(size, vec2(1.0));
        float ar = source_color_sample(_BaseMap, source_uv + vec2(step_uv.x, 0.0)).a;
        float au = source_color_sample(_BaseMap, source_uv + vec2(0.0, step_uv.y)).a;
        if (ar < _Cutoff || au < _Cutoff) col = mix(col, _RimColor, _Rim);
    }
    col *= _CharTint;
    col = mix(col, vec3(1.0), _Flash);
    if (_FogEnabled) {
        float fog = clamp((actor_view_depth - _FogRange.x) / max(_FogRange.y - _FogRange.x, 0.0001), 0.0, 1.0);
        col = mix(col, _FogColor, fog * _Fog);
    }
    ALBEDO = authored_output_rgb(col);
    // No ALPHA write: source uses opaque depth-writing cutout, not transparent blending.
}
