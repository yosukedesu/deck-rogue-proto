shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
uniform bool use_authored_color=true;
uniform float _Kind=41.0;
uniform float _Age=0.0;
uniform sampler2D _BaseMap : source_color, filter_linear, repeat_disable;
uniform vec4 _RolePrimary=vec4(.39,.33,.58,1);
uniform vec4 _RoleCore=vec4(1,.97,.86,1);
varying vec4 tint;
void vertex(){tint=CUSTOM0;}
vec3 output_rgb(vec3 c){if(!OUTPUT_IS_SRGB)return c;return mix(1.055*pow(max(c,vec3(0)),vec3(1.0/2.4))-.055,c*12.92,lessThanEqual(c,vec3(.0031308)));}
void fragment(){
    float u=UV.x,v=UV.y;
    bool core_primitive=_Kind>41.5;
    vec3 encoded_role=core_primitive?_RoleCore.rgb:_RolePrimary.rgb;
    float gain=dot(tint.rgb,vec3(1))/max(.0001,dot(encoded_role,vec3(1)));
    vec3 rgb;float coverage;
    if(core_primitive){
        //Existing two triangles become subordinate narrow optical kernels, not white filled shards.
        float across=abs(v-.5);
        float beam=1.0-smoothstep(.12,.43,across);
        float ends=smoothstep(0.0,.16,u)*(1.0-smoothstep(.65,1.0,u));
        coverage=beam*ends;
        rgb=_RoleCore.rgb;
    }else{
        //305 dedicated density: genuine clear alpha outside narrow unequal radiant core.
        //Image axis v~.53 maps to center row; upper warm shoulder maps to upper mesh.
        //No additional broad fill or analytic RGB/mask is added to main sampled RGBA.
        vec4 optical=texture(_BaseMap,mix(vec2(.15,.34),vec2(.86,.72),vec2(u,1.0-v)));
        coverage=optical.a;
        rgb=optical.rgb;
    }
    float edge=min(min(u,1.0-u),min(v,1.0-v));
    ALBEDO=output_rgb(rgb*gain);
    ALPHA=tint.a*coverage*smoothstep(0.0,.025,edge);
}
