// Stage-only profile reconstructed from accepted native frames; original code lost.
uniform int recovered_stage_act = 0;
vec3 recovered_stage_color(vec3 c, vec3 world_position, vec3 world_normal) {
    float l=dot(c,vec3(0.2126,0.7152,0.0722));
    float upright=1.0-smoothstep(0.30,0.80,abs(world_normal.y));
    if(recovered_stage_act==2) {
        c=mix(c,vec3(l),0.19);
        // Charcoal outside the source rear-wall lantern pool; retain neutral floor.
        vec2 local=(world_position.xy-vec2(-1.2,3.0))/vec2(6.4,3.0);
        float amber=exp(-dot(local,local))*smoothstep(3.0,6.0,-world_position.z);
        vec3 wall=mix(vec3(.39,.38,.37),vec3(1.10,.74,.42),amber);
        c*=mix(vec3(.95,.94,.93),wall,upright);
    } else if(recovered_stage_act==3) {
        c=mix(c,vec3(l),.28);
        // Distant upright tiers recede; the playable top remains silver and legible.
        float distance_weight=smoothstep(5.0,14.0,-world_position.z);
        vec3 wall=mix(vec3(.62,.61,.73),vec3(.43,.44,.53),distance_weight);
        c*=mix(vec3(.895,.855,.894),wall,upright);
    }
    // Darken only the existing lower floor/pond surfaces. Keep native fog intact.
    float lower=1.0-smoothstep(-1.1,-0.05,world_position.y);
    if(recovered_stage_act==2) c*=mix(vec3(1.0),vec3(.32,.29,.27),lower);
    if(recovered_stage_act==3) c*=mix(vec3(1.0),vec3(.08,.12,.20),lower);
    return c;
}
