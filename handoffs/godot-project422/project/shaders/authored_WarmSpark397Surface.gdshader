shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
//397 exact three-card warm ember material; analytic, not a bitmap surrogate.
uniform float _Kind = 0.0;
uniform float _Family = 1.0;
uniform bool use_authored_color = false;
varying vec4 authored_color;
void vertex(){ authored_color=CUSTOM0; }
vec3 output_rgb(vec3 c){if(!OUTPUT_IS_SRGB)return c;return mix(1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-.055,c*12.92,lessThanEqual(c,vec3(.0031308)));}
void fragment(){
 vec4 tint=use_authored_color?authored_color:COLOR;
 float u=clamp(UV.x,0.0,1.0),v=clamp(UV.y,0.0,1.0);
 // Finite tapered mesh owns the silhouette. Only a narrow edge softening;
 // no halo/white plate that competes with the unchanged actor hit flash.
 float edge=smoothstep(0.0,.11,min(v,1.0-v));
 float hot=smoothstep(.44,.90,u)*(.50+.50*(1.0-abs(2.0*v-1.0)));
 vec3 ember=mix(vec3(.34,.075,.012),vec3(.88,.39,.065),smoothstep(.06,.48,u));
 ember=mix(ember,vec3(1.0,.78,.34),hot*.78);
 if(_Kind<.5)ember=mix(vec3(.62,.22,.027),vec3(.98,.70,.26),hot*.55);
 float a=clamp(tint.a,0.0,1.0)*edge;
 ALBEDO=output_rgb(ember*max(tint.r,0.0));ALPHA=a;
}
