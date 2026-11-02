shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
uniform bool use_authored_color=true;
uniform float _Age=0.0;
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
uniform vec4 _RoleShadow=vec4(.03,.08,.02,1);
uniform vec4 _RolePrimary=vec4(.15,.40,.08,1);
uniform vec4 _RoleEdge=vec4(.6,.8,.2,1);
varying vec4 tint;
void vertex(){tint=CUSTOM0;}
vec3 output_rgb(vec3 c){if(!OUTPUT_IS_SRGB)return c;return mix(1.055*pow(max(c,vec3(0)),vec3(1.0/2.4))-.055,c*12.92,lessThanEqual(c,vec3(.0031308)));}
void fragment(){
    float u=UV.x;float v=UV.y;
    float gain=dot(tint.rgb,vec3(1))/max(.0001,dot(_RolePrimary.rgb,vec3(1)));
    //299 authored density only: geometry/vertexalpha/age/gain remain298-v2.
    //Interior UV excludes finished outer leaf contour and woody left node.
    vec4 organic=texture(_BaseMap,mix(vec2(.18,.22),vec2(.82,.78),vec2(u,v)));
    vec3 rgb=organic.rgb;
    float edge=min(min(u,1.0-u),min(v,1.0-v));
    float release=smoothstep(.065,.225,_Age);
    float irregular=u+.12*sin(7.0*v+4.0*u);
    float consume=smoothstep(release*.80-.18,release*.80+.02,irregular);
    //300 organic consumption only. Initial material/shape is unchanged through .05s.
    //The outer belly loses irregular tissue first; the existing narrow root follows later.
    //All masks are monotone in local actual age. No second rise or added geometry.
    float body_phase=smoothstep(.050,.135,_Age);
    float root_phase=smoothstep(.100,.170,_Age);
    float tissue=organic.g-organic.r*.25;
    float contour=u+.42*(tissue-.30)+.14*sin(8.0*v+3.0*u)+.10*sin(14.0*v-5.0*u);
    float body_front=mix(-.55,1.70,body_phase);
    float root_front=mix(-.55,1.70,root_phase);
    float body_remain=smoothstep(body_front-.045,body_front+.045,contour);
    float root_remain=smoothstep(root_front-.045,root_front+.045,contour);
    float root_zone=smoothstep(.76,.92,u);
    float organic_consume=mix(body_remain,root_remain,root_zone);
    ALBEDO=output_rgb(rgb*gain);
    ALPHA=tint.a*smoothstep(0.0,.025,edge)*consume*organic.a*organic_consume;
}
