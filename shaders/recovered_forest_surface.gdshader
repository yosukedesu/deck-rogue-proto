
shader_type spatial;
render_mode cull_disabled, diffuse_lambert, ambient_light_disabled, fog_disabled;
uniform sampler2D albedo_texture : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec4 base_color : source_color = vec4(1.0);
uniform float alpha_cutoff=0.0;
uniform float static_neutral=0.5;
uniform float receive=0.8;
uniform float fog_weight=1.0;
uniform vec3 static_tint=vec3(1.0);
uniform float static_saturation=1.0;
uniform vec3 ambient_floor=vec3(0.06325,0.0805,0.09775);
uniform vec3 ambient_sky=vec3(0.128,0.222,0.339);
uniform vec3 ambient_equator=vec3(0.084,0.145,0.221);
uniform vec3 ambient_ground=vec3(0.042,0.065,0.097);
uniform float grade_weight=0.18;
uniform vec3 recovered_lantern_position=vec3(-6.0,2.0,-2.0);
uniform vec4 night_color=vec4(0.0331,0.0685,0.1406,1.0);
uniform float night_strength=0.7;
uniform float vignette_weight=0.6;
uniform float vignette_side=0.5;
uniform float vignette_top=0.06;
uniform float lower_fade=0.0;
uniform float hue_study=1.0;
uniform float distance_scale=1.67;
uniform float path_angle=0.383972;
// Numeric source colors are linear on BOTH rendering paths. Texture samples
// alone need explicit decoding on Compatibility (OUTPUT_IS_SRGB).
uniform vec4 fog_color=vec4(0.1065,0.1626,0.2738,1.0);
uniform vec3 height_color=vec3(0.1065,0.1548,0.2330);
uniform vec4 height_range=vec4(-0.15,1.2,26.0,42.0);
uniform float height_density=0.18;
uniform vec3 far_fog=vec3(32.0,58.0,0.6);
uniform vec3 lobe_color=vec3(0.2352,0.3684,0.6084);
uniform vec3 lobe_shape=vec3(650.0,90.0,1.0);
uniform vec3 seat_light_color=vec3(0.8284,1.3996,2.6);
uniform vec2 fog_range=vec2(22.0,48.0);
uniform vec3 lobe_position=vec3(0.0,2.2,-80.0);
uniform vec3 lamp_emission=vec3(0.0);
// Source intensity/color, independent of native Godot Omni attenuation.
// Authored attack staging is opt-in and zero outside the exact three cues.
// This only changes surface lighting; screen/UI exposure is untouched.
uniform float attack_staging=0.0;
// Optional fire-only crest; caller supplies a short envelope independent of tail.
uniform float attack_warm_crest=0.0;
// Contact-only spectacle envelope, zero for unscoped effects and after release.
uniform float attack_spectacle_crest=0.0;
uniform float attack_spectacle_tone=1.0;
uniform float attack_spectacle_chroma=1.0;
uniform vec3 attack_focus=vec3(0.0);
uniform int forest_light_count=0;
uniform vec4 forest_light_position_range[8];
uniform vec4 forest_light_color_kind[8];
varying vec3 forest_position;
varying vec3 forest_normal;
varying vec3 forest_surface;
varying vec3 forest_original;
varying float forest_visibility;
varying float forest_hue;
varying float forest_staging;
float luminance(vec3 c) { return dot(c,vec3(0.2126,0.7152,0.0722)); }
vec3 forest_decode(vec3 c) {
    return mix(pow(max((c+vec3(0.055))/1.055,vec3(0.0)),vec3(2.4)),c/12.92,lessThanEqual(c,vec3(0.04045)));
}
vec3 forest_output(vec3 c) {
    if(!OUTPUT_IS_SRGB)return c;
    return mix(1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-vec3(0.055),c*12.92,lessThanEqual(c,vec3(0.0031308)));
}
vec3 night_grade(vec3 col) {
    float l=luminance(col); float bright=smoothstep(0.05,0.32,l);
    vec3 navy=night_color.rgb*l/max(luminance(night_color.rgb),0.0001);
    // The moon grade recedes inside the existing amber lantern pool.
    // This is a stage-material color mask, never a new scene light.
    float local_warm=1.0-smoothstep(1.7,4.8,distance(forest_position,recovered_lantern_position));
    return mix(col,navy,night_strength*(1.0-bright)*grade_weight*(1.0-local_warm*0.94));
}
void vertex() { forest_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; forest_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL); }
void fragment() {
    vec4 tex=texture(albedo_texture,UV);vec4 tint=base_color;
    if(OUTPUT_IS_SRGB){tex.rgb=forest_decode(tex.rgb);tint.rgb=forest_decode(tint.rgb);}
    tex*=tint;
    if(tex.a<alpha_cutoff)discard;
    vec3 n=normalize(forest_normal); if(!FRONT_FACING)n=-n;
    // Retain local values around the contact; restrain competing distant surfaces.
    float focus_distance=distance(forest_position.xz,attack_focus.xz);
    forest_staging=1.0-0.15*attack_staging*smoothstep(1.2,5.5,focus_distance);
    forest_original=tex.rgb;
    forest_surface=mix(vec3(luminance(tex.rgb)),tex.rgb,static_saturation)*static_tint;
    float c=cos(path_angle),s=sin(path_angle);
    vec2 path=vec2(forest_position.x*c-forest_position.z*s,-forest_position.x*s-forest_position.z*c);
    float edge=min(min(forest_position.x+9.0,-3.0-forest_position.x),min(-forest_position.z+1.0,7.0+forest_position.z));
    forest_hue=hue_study*smoothstep(0.0,0.75,edge);
    vec3 amb=n.y>=0.0?mix(ambient_equator,ambient_sky,n.y):mix(ambient_equator,ambient_ground,-n.y);
    amb=max(mix(amb,vec3(luminance(amb)),static_neutral),ambient_floor);
    amb*=mix(vec3(1.0),mix(vec3(0.90,0.80,1.12),vec3(0.88,0.98,1.10),max(0.0,n.y)),forest_hue);
    // Source seat lamp mask and pool in path coordinates, restricted to surfaces.
    float dt=max(max(-6.5-path.x,path.x-15.0),0.0);
    float ds=path.y< -1.4?(-1.4-path.y)/3.2:max(0.0,(path.y-1.8)/4.0);
    float band=(1.0-smoothstep(0.0,1.1,dt))*(1.0-smoothstep(0.0,1.0,ds));
    vec2 pool=(path-vec2(3.0,4.8))/vec2(6.5,5.0); pool.x-=pool.y*0.4;
    float pool_weight=1.0-smoothstep(0.65,1.0,length(pool));
    vec3 key_dir=normalize(vec3(-8.0,17.0,14.0));
    float ndl=max(0.0,dot(n,key_dir));
    vec3 key=seat_light_color*ndl*(0.05+0.95*band)*(0.6+0.7*pool_weight);
    key=mix(key,vec3(luminance(key)),static_neutral*(1.0-forest_hue));
    float shade=mix(1.0,mix(0.10,1.0,smoothstep(-3.0,0.05,forest_position.y)),lower_fade);
    vec3 ordinary_points=vec3(0.0),attack_points=vec3(0.0);
    for(int i=0;i<8;i++) {
        if(i>=forest_light_count)break;
        vec3 delta=forest_light_position_range[i].xyz-forest_position;
        float distance2=max(dot(delta,delta),0.00006103515625);
        float range=max(forest_light_position_range[i].w,0.0001);
        float ratio=distance2/(range*range);
        float smooth_factor=max(0.0,1.0-ratio*ratio);
        // Finite emitter footprint avoids a singular white texel at ground contact.
        // Legacy/unscoped attacks keep their original attenuation exactly.
        float emitter_floor=0.36*attack_staging*step(0.5,forest_light_color_kind[i].w);
        float attenuation=smooth_factor*smooth_factor/max(distance2,emitter_floor);
        float point_ndl=max(0.0,dot(n,delta*inversesqrt(distance2)));
        vec3 contribution=forest_light_color_kind[i].rgb*(attenuation*point_ndl);
        if(forest_light_color_kind[i].w>0.5) {
            // Scoped finite-area bounce: a low point emitter's r^-3 floor falloff
            // hid its entire readable pool behind the HP bar in the fixed camera.
            // Retain its core, broaden only upward-facing textured ground shoulders.
            // Position/color/decay still come from the live attack source, not a decal.
            float ground_plane=smoothstep(0.35,0.85,n.y)*(1.0-smoothstep(0.55,1.25,abs(forest_position.y-attack_focus.y)));
            // A short contact-only scene crest, independent of the long faint tail.
            // Real HD-2D peaks illuminate broad textured paving and nearby masonry,
            // not just a foot-sized disc. Never change exposure or the UI layer.
            float crest=attack_spectacle_crest;
            float footprint=1.0+dot(delta.xz,delta.xz)/mix(6.76,36.0,crest);
            float area_irradiance=mix(0.8,1.6,crest)*smooth_factor*smooth_factor/(footprint*footprint);
            vec3 area_contribution=forest_light_color_kind[i].rgb*area_irradiance;
            // Admit elevated and vertical surfaces only during this brief crest.
            // Source-facing normals retain modeled relief; no flat screen mask.
            vec3 broad_direction=normalize(delta+vec3(0.0,1.0,0.0));
            float broad_facing=clamp(max(n.y,0.0)+0.65*max(dot(n,broad_direction),0.0),0.0,1.0);
            float broad_height=1.0-smoothstep(2.4,6.0,abs(forest_position.y-attack_focus.y));
            float scene_receiver=mix(ground_plane,broad_facing*broad_height,crest);
            contribution=mix(contribution,max(contribution,area_contribution),attack_staging*scene_receiver);
            attack_points+=contribution;
        } else ordinary_points+=contribution;
    }
    vec3 neutral_points=mix(ordinary_points,vec3(luminance(ordinary_points)),static_neutral*(1.0-forest_hue));
    vec3 col=forest_surface*(amb+receive*(key+neutral_points))*shade*forest_staging;
    // Shared source position/radiance drives floor and actors. Lift upward-facing
    // textured receivers, never add an untextured disc or blanket screen glow.
    float ground_receiver=smoothstep(0.25,0.85,n.y);
    // A bounded warm crest on textured upward receivers only. Footprint and
    // existing source-light tail are unchanged; no actor or screen exposure change.
    vec3 crest_gain=mix(vec3(1.0),vec3(1.35,2.0,0.85),attack_warm_crest*ground_receiver);
    vec3 reflected_attack=forest_original*receive*attack_points*crest_gain*(1.0+0.65*attack_staging*ground_receiver);
    // Linear-light reflected highlight shaping, not a global exposure/albedo wash.
    // Retain the broad energetic footprint. Hot highlights approach warm white
    // instead of independently clipping R/G into fluorescent yellow-green.
    float attack_luma=luminance(reflected_attack);
    float shoulder_excess=max(attack_luma-0.65,0.0);
    float shoulder_luma=min(attack_luma,0.65)+shoulder_excess/(1.0+shoulder_excess/1.8);
    vec3 shoulder=reflected_attack*(shoulder_luma/max(attack_luma,0.00001));
    float hot_white=0.88*smoothstep(0.25,1.2,attack_luma);
    vec3 shaped_attack=mix(shoulder,vec3(1.06,1.0,0.88)*shoulder_luma,hot_white);
    // Crest-only reflected yellow/green midtones: retain luminance and texture,
    // but turn the acid grass shoulder toward the source's warm gold/cream.
    // Static albedo, dark greens, red-gold soil and white-hot core are untouched.
    float reflected_max=max(max(shaped_attack.r,shaped_attack.g),shaped_attack.b);
    float reflected_min=min(min(shaped_attack.r,shaped_attack.g),shaped_attack.b);
    float reflected_chroma=(reflected_max-reflected_min)/max(reflected_max,0.00001);
    float green_ratio=shaped_attack.g/max(shaped_attack.r,0.00001);
    float yellow_green=smoothstep(0.76,0.96,green_ratio)*(1.0-smoothstep(1.16,1.6,green_ratio));
    float chroma_mask=yellow_green*smoothstep(0.32,0.72,reflected_chroma)*smoothstep(0.04,0.28,luminance(shaped_attack));
    vec3 warm_mid=vec3(1.18,0.98,0.65);
    warm_mid*=luminance(shaped_attack)/luminance(warm_mid);
    shaped_attack=mix(shaped_attack,warm_mid,0.78*chroma_mask*attack_spectacle_chroma);
    col+=mix(reflected_attack,shaped_attack,attack_spectacle_crest*attack_spectacle_tone);
    vec3 unfogged=col;
    vec3 view_pos=(VIEW_MATRIX*vec4(forest_position,1.0)).xyz; float depth=-view_pos.z;
    vec3 view_dir=normalize(forest_position-CAMERA_POSITION_WORLD),lobe_dir=normalize(lobe_position-CAMERA_POSITION_WORLD);
    float dv=asin(clamp(view_dir.y,-1.0,1.0))-asin(clamp(lobe_dir.y,-1.0,1.0));
    // FOREST_FOG_SOURCE_AZIMUTH: reflect Godot -Z back to authored Unity +Z.
    // Keep the source rear +/-PI branch away from the forward field of view.
    float dh=atan(view_dir.x,-view_dir.z)-atan(lobe_dir.x,-lobe_dir.z);
    float lobe=exp(-0.5*(lobe_shape.x*dv*dv+lobe_shape.y*dh*dh));
    float fog_edge=mix(lobe_shape.z,1.0,lobe);
    vec3 fc=(fog_color.rgb*fog_edge+lobe_color*lobe)*forest_staging;
    float hf=height_density*clamp((height_range.y-forest_position.y)/max(height_range.y-height_range.x,0.001),0.0,1.0)*clamp((depth/distance_scale-height_range.z)/max(height_range.w-height_range.z,0.001),0.0,1.0);
    float df=clamp((depth/distance_scale-fog_range.x)/(fog_range.y-fog_range.x),0.0,1.0);
    float ff=far_fog.z*clamp((depth/distance_scale-far_fog.x)/max(far_fog.y-far_fog.x,0.001),0.0,1.0);
    forest_visibility=(1.0-hf)*(1.0-df)*(1.0-ff);
    col=mix(col,height_color*fog_edge*forest_staging,hf); col=mix(col,fc,df); col=mix(col,fc,ff);
    col=mix(unfogged,col,fog_weight);forest_visibility=mix(1.0,forest_visibility,fog_weight);
    vec4 clip=PROJECTION_MATRIX*vec4(view_pos,1.0); vec2 screen=clip.xy/max(clip.w,0.001);
    float side=smoothstep(0.3,0.5,abs(screen.x)*0.5);
    float seat_dt=max(max(-6.5-path.x,path.x-15.0),0.0),seat_ds=max(max(-1.9-path.y,path.y-2.3),0.0);
    side*=1.0-(1.0-smoothstep(0.0,1.6,seat_dt))*(1.0-smoothstep(0.0,1.6,seat_ds));
    float top=1.0-smoothstep(0.0,0.14,0.5-screen.y*0.5);
    float vignette=mix(1.0,(1.0-vignette_side*side)*(1.0-vignette_top*top),vignette_weight);
    forest_visibility*=vignette*shade;
    ALBEDO=vec3(1.0); ROUGHNESS=0.92; SPECULAR=0.0;
    EMISSION=forest_output(night_grade(col*vignette)+lamp_emission*forest_visibility*forest_staging);
    // FOREST_MATERIAL_FACTORS_LINEAR: custom light() receives linear LIGHT_COLOR
    // on both renderers. Godot decodes ALBEDO/EMISSION at its Compatibility
    // boundary, but does not decode these user varyings. Keep their source-linear
    // values; only the EMISSION output above needs forest_output().
}
void light() {
    // Point lights were combined in linear source space before fog/grade above.
    // Suppress their native contribution, including attack endpoints, to avoid
    // double counting. Native moon shadow visibility is retained independently;
    // its composition and source backlight shadows remain optical-parity gaps.
    if(LIGHT_IS_DIRECTIONAL) {
        float ndl=max(0.0,dot(NORMAL,LIGHT));
        vec3 direct=mix(LIGHT_COLOR,vec3(luminance(LIGHT_COLOR)),static_neutral*(1.0-forest_hue));
        vec3 contribution=forest_surface*receive*direct*ATTENUATION*ndl*forest_visibility*forest_staging/3.14159265;
        DIFFUSE_LIGHT+=!OUTPUT_IS_SRGB?night_grade(contribution):contribution;
    }
}