shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never;
void fragment(){float d=length((UV-vec2(.5))*2.0);ALBEDO=vec3(.015,.019,.025);ALPHA=smoothstep(1.0,.15,d)*.38;}
