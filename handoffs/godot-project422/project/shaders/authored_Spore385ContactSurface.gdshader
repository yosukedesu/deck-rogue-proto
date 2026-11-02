shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
//385 local procedural botanical cluster. No image sampler, floor wash, or radial rays.
uniform bool use_authored_color=false;
varying vec4 authored_color;
void vertex(){authored_color=CUSTOM0;}
vec3 authored_output_rgb(vec3 c){if(!OUTPUT_IS_SRGB)return c;return mix(1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-.055,c*12.92,lessThanEqual(c,vec3(.0031308)));}
void fragment(){
 vec4 color=use_authored_color?authored_color:COLOR;vec2 p=UV*2.0-1.0;
 float opacity=0.0;vec3 rgb=vec3(.18,.32,.035);
 if(UV2.x<.5){
  //A connected irregular low-opacity body joins the seeds briefly, without a fog halo.
  float d=min(length((p-vec2(-.29,.05))/vec2(.66,.74)),min(length((p-vec2(.22,.13))/vec2(.61,.65)),length((p-vec2(.02,-.23))/vec2(.63,.62))));
  opacity=(1.0-smoothstep(.40,1.0,d))*(.86+.14*sin(7.0*p.x+2.0*p.y)*sin(6.0*p.y));
  rgb=vec3(.14,.28,.025);
 }else{
  float seed=UV2.y;
  vec2 q=p+vec2(.065*sin(3.1*p.y+seed),.045*sin(4.2*p.x+seed*.7));
  q.x*=1.03+.12*q.y;
  float d=length(q);float cover=1.0-smoothstep(.72,.98,d);
  float inner=1.0-smoothstep(.48,.78,d);
  float highlight=(1.0-smoothstep(.12,.72,length(q-vec2(-.20,.22))))*.55;
  float grain=.93+.07*sin(q.x*25.0+seed)*sin(q.y*21.0+seed);
  rgb=mix(vec3(.055,.115,.012),vec3(.37,.56,.085),inner);
  rgb=mix(rgb,vec3(.70,.76,.24),highlight)*grain;
  opacity=cover;
 }
 ALBEDO=authored_output_rgb(rgb);ALPHA=clamp(opacity*color.a,0.0,1.0);
}
