
shader_type spatial;
render_mode cull_disabled, diffuse_lambert;
uniform sampler2D side0 : source_color, filter_nearest_mipmap, repeat_enable;
uniform sampler2D side1 : source_color, filter_nearest_mipmap, repeat_enable;
uniform sampler2D side2 : source_color, filter_nearest_mipmap, repeat_enable;
uniform sampler2D side3 : source_color, filter_nearest_mipmap, repeat_enable;
uniform sampler2D top0 : source_color, filter_nearest_mipmap, repeat_enable;
uniform sampler2D top1 : source_color, filter_nearest_mipmap, repeat_enable;
uniform sampler2D top2 : source_color, filter_nearest_mipmap, repeat_enable;
uniform sampler2D top3 : source_color, filter_nearest_mipmap, repeat_enable;
uniform vec4 tint : source_color = vec4(1.0);
uniform float path_angle = 0.383972;
uniform float top_threshold = 0.65;
uniform float top_blend = 0.10;
uniform int side_rotation = 0;
uniform int top_rotation = 0;
varying vec3 stage_position;
varying vec3 stage_normal;
float hash2(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453); }
vec2 tile_uv(vec2 p,int rotation) {
    vec2 cell=floor(p); vec2 uv=fract(p);
    float r=hash2(cell+17.3);
    if(rotation==1 && r>0.5) uv.x=1.0-uv.x;
    if(rotation==2) { if(r<0.25) uv=vec2(uv.y,1.0-uv.x); else if(r<0.5) uv=1.0-uv; else if(r<0.75) uv=vec2(1.0-uv.y,uv.x); }
    return uv;
}
vec3 side_sample(vec2 p) {
    vec2 uv=tile_uv(p,side_rotation); float i=floor(hash2(floor(p))*4.0);
    if(i<1.0)return texture(side0,uv).rgb;
    if(i<2.0)return texture(side1,uv).rgb;
    if(i<3.0)return texture(side2,uv).rgb;
    return texture(side3,uv).rgb;
}
vec3 top_sample(vec2 p) {
    vec2 uv=tile_uv(p,top_rotation); float i=floor(hash2(floor(p))*4.0);
    if(i<1.0)return texture(top0,uv).rgb;
    if(i<2.0)return texture(top1,uv).rgb;
    if(i<3.0)return texture(top2,uv).rgb;
    return texture(top3,uv).rgb;
}
void vertex() {
    stage_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    stage_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL);
}
#include "res://shaders/recovered_stage_profile.gdshaderinc"
void fragment() {
    float c=cos(path_angle),s=sin(path_angle);
    vec3 p=vec3(stage_position.x*c-stage_position.z*s,stage_position.y,-stage_position.x*s-stage_position.z*c);
    vec3 n=vec3(stage_normal.x*c-stage_normal.z*s,stage_normal.y,-stage_normal.x*s-stage_normal.z*c);
    vec2 side_uv=abs(n.z)>=abs(n.x)?vec2(p.x,-p.y):vec2(p.z,-p.y);
    float top_mix=smoothstep(top_threshold-top_blend,top_threshold+top_blend,n.y);
    ALBEDO=mix(side_sample(side_uv*0.390625),top_sample(p.xz*0.390625),top_mix)*tint.rgb*COLOR.rgb;
    ALBEDO = recovered_stage_color(ALBEDO, stage_position, stage_normal);
    ROUGHNESS=0.96; SPECULAR=0.12;
}