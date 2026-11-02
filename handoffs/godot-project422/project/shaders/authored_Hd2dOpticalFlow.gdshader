shader_type spatial;
// Exact Torrent body only. Add light locally rather than replacing the scene
// with a grey alpha veil; world lamps/floor response remain the existing system.
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;
uniform sampler2D _BaseMap : source_color, filter_linear_mipmap, repeat_disable;
varying vec4 tint;
void vertex(){ tint=CUSTOM0; }
vec3 output_rgb(vec3 c){
    if(!OUTPUT_IS_SRGB)return c;
    return mix(1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-.055,c*12.92,lessThanEqual(c,vec3(.0031308)));
}
void fragment(){
    vec4 raw=texture(_BaseMap,UV);
    vec3 linear_rgb=raw.rgb;
    // Same renderer-dependent colour-space convention as Hd2dPrototype.
    if(OUTPUT_IS_SRGB)linear_rgb=mix(pow((linear_rgb+.055)/1.055,vec3(2.4)),
        linear_rgb/12.92,lessThanEqual(linear_rgb,vec3(.04045)));
    float luminance=dot(linear_rgb,vec3(.2126,.7152,.0722));
    float veil=.16+.24*smoothstep(.10,.60,luminance);
    float filament=.80*smoothstep(.60,.92,luminance);
    float local_gain=max(max(tint.r,tint.g),tint.b);
    vec3 radiance=linear_rgb*(veil+filament)*local_gain;
    float edge=min(min(UV.x,1.0-UV.x),min(UV.y,1.0-UV.y));
    // Raw owns the interior density; only transparent canvas boundary is softened.
    ALBEDO=output_rgb(radiance);
    ALPHA=raw.a*tint.a*smoothstep(0.0,.08,edge);
}
