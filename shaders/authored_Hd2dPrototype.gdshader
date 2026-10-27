shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
uniform bool use_authored_color = true;
uniform float _Kind = 0.0;
uniform bool _TextureEnabled = false;
uniform bool _AuthoredRgba = false;
uniform float _UseTexture = 0.0;
uniform float _TextureMotion = 0.0;
uniform sampler2D _BaseMap : source_color, filter_linear_mipmap, repeat_disable;
uniform float _Age = 0.0;
varying vec4 tint;
void vertex(){ tint=CUSTOM0; }
vec3 output_rgb(vec3 c){if(!OUTPUT_IS_SRGB)return c;return mix(1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-.055,c*12.92,lessThanEqual(c,vec3(.0031308)));}
float hash2(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise2(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(hash2(i),hash2(i+vec2(1,0)),f.x),mix(hash2(i+vec2(0,1)),hash2(i+vec2(1,1)),f.x),f.y);}
void fragment(){
    vec2 encoded_uv=UV;
    float released_region=0.0;
    if(_TextureEnabled && ((_TextureMotion>26.5 && _TextureMotion<27.5)||(_TextureMotion>24.5 && _TextureMotion<25.5)||(_TextureMotion>21.5 && _TextureMotion<22.5)||(_TextureMotion>19.5 && _TextureMotion<20.5)||(_TextureMotion>15.5 && _TextureMotion<16.5)||(_TextureMotion>12.5 && _TextureMotion<13.5)||(_TextureMotion>7.5 && _TextureMotion<8.5)||(_TextureMotion>3.5 && _TextureMotion<4.5)||(_TextureMotion>9.5 && _TextureMotion<10.5))){
        released_region=floor(encoded_uv.x/2.0);
        encoded_uv.x-=released_region*2.0;
    }
    bool dust=encoded_uv.x>1.5;
    vec2 uv=dust?encoded_uv-vec2(2.0,0.0):encoded_uv;
    float edge=min(min(uv.x,1.0-uv.x),min(uv.y,1.0-uv.y));
    float soft=smoothstep(0.0,_Kind>1.5?.10:.035,edge);
    if(dust) soft=1.0-smoothstep(.15,.50,length(uv-vec2(.5)));
    vec3 rgb=tint.rgb; float alpha=tint.a*soft;
    if(!_TextureEnabled && _Kind>13.5 && _Kind<14.5){
        float radius=length((uv-vec2(.5))*2.0);
        alpha=tint.a*exp(-radius*radius*5.5)*(1.0-smoothstep(.65,1.0,radius));
    }
    // Ground-only looming shadow: broad unequal lobes, no literal paw drawing.
    if(!_TextureEnabled && _Kind>7.5 && _Kind<8.5){
        vec2 q=uv*2.0-1.0;
        float body=1.0-smoothstep(.48,1.0,length(q*vec2(1.05,1.15)));
        float lobe0=1.0-smoothstep(.25,.63,length((q-vec2(-.48,-.10))*vec2(1.0,1.6)));
        float lobe1=1.0-smoothstep(.18,.49,length((q-vec2(-.36,.36))*vec2(1.2,1.0)));
        float lobe2=1.0-smoothstep(.20,.53,length((q-vec2(.25,-.33))*vec2(1.4,1.0)));
        float shape=max(body,max(lobe0,max(lobe1,lobe2)));
        float rupture=smoothstep(0.0,.12,_Age);
        float cracks=noise2(q*vec2(9.0,13.0)+vec2(_Age*2.0,0));
        alpha=tint.a*shape*(.82+.18*noise2(q*17.0))*smoothstep(rupture-.18,rupture+.18,cracks);
        rgb=vec3(.014,.018,.024);
    }
    // Untextured primary ribbons have a luminous shoulder, not flat polygon fill.
    if(!_TextureEnabled && !dust && _Kind<.5)
        alpha*=smoothstep(0.0,.35,min(uv.y,1.0-uv.y));
    // Strike-only optical bands. Geometry retains its weapon/contact path;
    // the broad layer is warm transparent air, not another opaque crescent.
    if(!_TextureEnabled && !dust && _Kind>5.5 && _Kind<6.5){
        float across=abs(uv.y-.5)*2.0;
        alpha=tint.a*exp(-across*across*5.0)*smoothstep(0.0,.08,edge);
    }
    if(!_TextureEnabled && !dust && _Kind>6.5 && _Kind<7.5){
        float across=abs(uv.y-.5)*2.0;
        float hot=1.0-smoothstep(.08,.48,across);
        float shoulder=exp(-across*across*3.8);
        rgb=mix(vec3(.78,.32,.065),tint.rgb,hot);
        alpha=tint.a*(.36*shoulder+.64*hot)*smoothstep(0.0,.045,edge);
    }
    if(!_TextureEnabled && _Kind>12.5 && _Kind<13.5){
        // Receiver-local frosted defensive facets, not an offensive flash.
        float grain=.60+.40*noise2(uv*vec2(43.0,31.0));
        float plane=.82+.22*step(uv.x,uv.y*.68+.16);
        rgb=tint.rgb*plane;
        alpha=tint.a*grain*smoothstep(0.0,.035,edge);
    }
    if(!_TextureEnabled && _Kind>19.5 && _Kind<20.5){
        float across=abs(uv.y-.5)*2.0;
        float grain=noise2(uv*vec2(33.0,7.0)+vec2(_Age*1.8,0));
        float erode=smoothstep(.075,.28,_Age)*.58;
        alpha*=smoothstep(erode-.16,erode+.16,grain)*(1.0-smoothstep(.30,1.0,across));
    }
    if(!_TextureEnabled && _Kind>20.5 && _Kind<21.5){
        float across=(uv.y-.5)*2.0;
        alpha=tint.a*exp(-across*across*7.0)*smoothstep(0.0,.20,uv.x)*(1.0-smoothstep(.78,1.0,uv.x));
    }
    if(!_TextureEnabled && _Kind>21.5 && _Kind<22.5){
        vec2 q=(uv-.5)*2.0;
        alpha=tint.a*exp(-dot(q,q)*3.4)*(1.0-smoothstep(.65,1.0,length(q)));
    }
    if (_TextureEnabled && _UseTexture > .5) {
        vec2 sample_uv=uv;
        if(_TextureMotion>24.5 && _TextureMotion<25.5){
            float move=smoothstep(0.0,.24,_Age);
            sample_uv+=vec2(.009*sin(uv.y*19.0+_Age*17.0),.008*sin(uv.x*23.0-_Age*13.0))*move;
        }
        if(_TextureMotion>.5 && _TextureMotion<1.5){
            float tail=1.0-smoothstep(.45,.88,uv.x);
            sample_uv.y+=tail*(.025*sin(uv.x*21.0+_Age*95.0)+.025*(noise2(uv*12.0+vec2(_Age*14.0,0))-.5));
        }else if(_TextureMotion>1.5 && _TextureMotion<2.5){
            sample_uv+=vec2(.023*sin(uv.y*19.0-_Age*40.0),_Age*.08);
        }
        if(_TextureMotion>3.5 && _TextureMotion<4.5){
            sample_uv+=vec2(.018*sin(uv.y*16.0+_Age*10.0),.015*sin(uv.x*20.0-_Age*12.0));
        }
        if(_TextureMotion>7.5 && _TextureMotion<8.5){
            float release=smoothstep(.115,.24,_Age);
            // Differential flow bends the dominant curl, instead of sliding a rigid decal.
            float upper=1.0-smoothstep(.18,.65,uv.y);
            sample_uv.x+=release*.035*upper*sin(uv.y*11.0-(_Age-.115)*18.0);
            sample_uv.y+=release*.015*sin(uv.x*15.0+_Age*16.0);
        }
        if(_TextureMotion>11.5 && _TextureMotion<13.5){
            float peripheral=clamp(length(uv-vec2(.62,.61))*1.5,0.0,1.0);
            sample_uv+=peripheral*vec2(.012*sin(uv.y*17.0-_Age*24.0),.010*sin(uv.x*21.0+_Age*19.0));
        }
        if(_TextureMotion>17.5 && _TextureMotion<20.5){
            // Wet folds advect gently; unlike fire, liquid keeps a cool
            // translucent interior and bright specular crests from the RGBA art.
            sample_uv+=vec2(.008*sin(uv.y*19.0-_Age*21.0),.009*sin(uv.x*23.0-_Age*17.0));
        }
        if(_TextureMotion>29.5 && _TextureMotion<30.5){
            // Full registered wave art: only fine wet advection, never a
            // luminance recolor or a crop that turns the body into a thin seam.
            float crest=1.0-smoothstep(.60,.79,uv.y);
            sample_uv+=crest*vec2(.003*sin(uv.y*25.0-_Age*16.0),.003*sin(uv.x*29.0-_Age*14.0));
        }
        float release_mask=1.0;
        vec4 material=texture(_BaseMap,sample_uv);
        if(_TextureMotion>2.5 && _TextureMotion<3.5){
            float erosion=smoothstep(.12,.32,_Age);
            material.a*=smoothstep(erosion-.18,erosion+.18,noise2(uv*22.0));
        }
        if(_TextureMotion>4.5 && _TextureMotion<5.5){
            float erase=smoothstep(.015,.12,_Age);
            material.a*=smoothstep(erase-.12,erase+.12,uv.x);
        }
        if(_TextureMotion>1.5 && _TextureMotion<2.5){
            float breakup=noise2(uv*17.0+vec2(0,-_Age*3.0));
            float erosion=smoothstep(.055,.34,_Age)*.88;
            material.a*=smoothstep(erosion-.12,erosion+.12,breakup);
        }
        if(_TextureMotion>5.5 && _TextureMotion<6.5){
            float dissolve=smoothstep(.20,.40,_Age);
            material.a*=smoothstep(dissolve-.16,dissolve+.16,noise2(uv*18.0));
            material.a*=smoothstep(.07,.29,uv.y);
            material.rgb*=vec3(.80,.87,.96);
        }
        if(_TextureMotion>6.5 && _TextureMotion<8.5){
            float erode=smoothstep(.27,.44,_Age)*.55;
            material.a*=smoothstep(erode-.16,erode+.16,noise2(uv*19.0+vec2(0,-_Age*2.0)));
        }
        if(_TextureMotion>7.5 && _TextureMotion<8.5){
            float tear=smoothstep(.155,.215,_Age);
            // Overlapping source regions share a ragged sloping neck partition.
            // Its burn-through lies inside BOTH quads, never on a rectangle edge.
            float boundary=.32+.42*(uv.x-.90)+.014*sin(uv.x*53.0)
                +.025*(noise2(uv*vec2(35.0,19.0))-.5);
            float gap=.041*tear;
            if(released_region>.5 && released_region<1.5)
                release_mask*=1.0-smoothstep(boundary-gap-.012,boundary-gap+.012,uv.y);
            if(released_region>1.5 && released_region<2.5){
                release_mask*=smoothstep(boundary+gap-.012,boundary+gap+.012,uv.y);
                float burn=smoothstep(.205,.35,_Age)*.48;
                release_mask*=smoothstep(burn-.15,burn+.15,noise2(uv*18.0+vec2(0,-_Age*2.5)));
            }
            material.a*=release_mask;
        }
        if(_TextureMotion>8.5 && _TextureMotion<9.5){
            // Upper armor receives less near-ground flash; compress its painted
            // bright trim smoothly, retaining the original material/color identity.
            float upper=1.0-smoothstep(.25,.85,uv.y);
            float highlight=smoothstep(.25,.75,max(max(material.r,material.g),material.b));
            material.rgb*=1.0-.22*upper*highlight;
        }
        if(_TextureMotion>3.5 && _TextureMotion<4.5 && released_region>1.5){
            float dispersion=smoothstep(.14,.42,_Age)*.65;
            material.a*=smoothstep(dispersion-.20,dispersion+.20,
                noise2(uv*14.0+vec2(-_Age*1.5,_Age*2.0)));
        }
        if(_TextureMotion>9.5 && _TextureMotion<10.5 && released_region>.5){
            // Airborne debris leaves the warm contact illumination. Preserve its
            // facets while the painted golden highlights cool; grounded chips keep
            // their separate, already-muted material and visibility.
            float cooling=smoothstep(.16,.38,_Age);
            float value=dot(material.rgb,vec3(.25,.60,.15));
            vec3 neutral=vec3(value)*vec3(.93,.98,1.05);
            material.rgb=mix(material.rgb,neutral,cooling*.55)*mix(1.0,.90,cooling);
        }
        // Compatibility renderer has no bloom pass: author a restrained, local radiance fringe.
        // Preserve the real RGBA core; this is not a replacement silhouette or a screen glow.
        bool fire_material=(_TextureMotion>11.5 && _TextureMotion<13.5)||(_TextureMotion>.5 && _TextureMotion<1.5)||(_TextureMotion>6.5 && _TextureMotion<8.5);
        if(fire_material){
            vec2 d=vec2(.009,0.0);
            float halo=(texture(_BaseMap,sample_uv+d).a+texture(_BaseMap,sample_uv-d).a+
                texture(_BaseMap,sample_uv+d.yx).a+texture(_BaseMap,sample_uv-d.yx).a+
                texture(_BaseMap,sample_uv+vec2(.006,.006)).a+texture(_BaseMap,sample_uv-vec2(.006,.006)).a+
                texture(_BaseMap,sample_uv+vec2(.006,-.006)).a+texture(_BaseMap,sample_uv+vec2(-.006,.006)).a)*.125;
            float fringe=halo*.24*(1.0-material.a)*release_mask;
            float combined=material.a+fringe;
            material.rgb=(material.rgb*material.a+vec3(1.0,.34,.035)*fringe)/max(combined,.0001);
            material.a=combined;
        }
        if(_TextureMotion>12.5 && _TextureMotion<13.5){
            float splitx=.61+.018*sin(uv.y*19.0)+.015*(noise2(uv*24.0)-.5);
            float splity=.67+.018*sin(uv.x*23.0);
            float mask=released_region<.5 ? 1.0-smoothstep(splitx-.012,splitx+.012,uv.x) : smoothstep(splitx-.012,splitx+.012,uv.x);
            if(released_region>.5 && released_region<1.5)mask*=1.0-smoothstep(splity-.012,splity+.012,uv.y);
            if(released_region>1.5)mask*=smoothstep(splity-.012,splity+.012,uv.y);
            float erosion=smoothstep(.23,.46,_Age)*.60;
            material.a*=mask*smoothstep(erosion-.18,erosion+.18,noise2(uv*25.0+vec2(0,-_Age*2.0)));
        }
        if(_TextureMotion>22.5 && _TextureMotion<23.5){
            float boundary=.53+.025*(noise2(uv*29.0)-.5);
            float isolated=(1.0-smoothstep(boundary-.035,boundary+.035,uv.x))
                *smoothstep(.30,.37,uv.y)*(1.0-smoothstep(.75,.81,uv.y));
            float cooling=smoothstep(.065,.26,_Age);
            material.a*=isolated*smoothstep(cooling*.82-.16,cooling*.82+.16,noise2(uv*vec2(29.0,37.0)+vec2(_Age*2.0,0)));
            material.rgb*=1.0-.52*cooling;
        }
        if(_TextureMotion>16.5 && _TextureMotion<17.5){
            float breakaway=smoothstep(-.025,.055,_Age);
            float porous=noise2(uv*vec2(39.0,31.0)+vec2(_Age*5.0,-_Age*9.0));
            material.a*=smoothstep(breakaway*.85-.14,breakaway*.85+.14,porous);
        }
        if(_TextureMotion>13.5 && _TextureMotion<14.5){
            float opening=smoothstep(.025,.14,_Age);
            float porous=noise2(uv*vec2(31.0,23.0)+vec2(_Age*3.0,-_Age*8.0));
            material.a*=smoothstep(opening*.65-.18,opening*.65+.18,porous);
        }
        if((_TextureMotion>14.5 && _TextureMotion<16.5)||(_TextureMotion>25.5 && _TextureMotion<27.5)){
            bool vegetal=_TextureMotion>25.5;
            bool tail_stroke=vegetal?_TextureMotion>26.5:_TextureMotion>15.5;
            if(tail_stroke){
                // Both quads overlap the complete ragged neck. Their separation
                // exposes eroded energy contours, never the UV rectangle edge.
                float boundary=.58+.045*sin(uv.y*31.0)+.025*(noise2(uv*31.0)-.5);
                float tear=smoothstep(.09,.20,_Age)*.018;
                if(released_region<1.5)
                    material.a*=1.0-smoothstep(boundary-tear-.025,boundary-tear+.025,uv.x);
                else material.a*=smoothstep(boundary+tear-.025,boundary+tear+.025,uv.x);
            }
            float erase=tail_stroke?smoothstep(.15,.34,_Age):smoothstep(.055,.23,_Age);
            float ragged=.025*(noise2(uv*vec2(37.0,19.0))-.5);
            material.a*=smoothstep(erase-.09+ragged,erase+.09+ragged,uv.x);
            float value=dot(material.rgb,vec3(.25,.60,.15));
            vec3 warm=(vegetal?vec3(.20,.64,.16):vec3(1.0,.46,.10))*value*1.35;
            if(vegetal){
                float fibres=.48+.52*smoothstep(.20,.78,.5+.5*sin(uv.y*95.0+uv.x*17.0+noise2(uv*16.0)*4.0));
                material.a*=fibres;
                material.rgb=mix(vec3(.11,.32,.075)*value*1.4,vec3(.84,1.0,.69)*value,smoothstep(.32,.92,value));
            }
            material.rgb=mix(warm,material.rgb,smoothstep(.55,.94,value));
            if(tail_stroke){
                // After the directional split, the hot tip cools before the
                // amber outline: it must not look like another live projectile.
                float cooling=smoothstep(.14,.30,_Age);
                material.rgb=mix(material.rgb,(vegetal?vec3(.16,.48,.055):vec3(1.0,.34,.045))*value*.85,cooling*.90);
                material.a*=1.0-cooling*.40*smoothstep(.70,.98,value);
            }
        }
        if(_TextureMotion>17.5 && _TextureMotion<18.5){
            float dispersal=smoothstep(.19,.39,_Age)*.55;
            material.a*=smoothstep(dispersal-.20,dispersal+.20,
                noise2(uv*vec2(27.0,31.0)+vec2(-_Age*2.5,_Age*2.0)));
        }
        if(_TextureMotion>19.5 && _TextureMotion<20.5){
            float boundary=.58+.15*(uv.x-.60)+.018*sin(uv.x*31.0)
                +.018*(noise2(uv*vec2(29.0,23.0))-.5);
            float lower=smoothstep(boundary-.04,boundary+.04,uv.y);
            material.a*=released_region<.5?1.0-lower:lower;
            float release=smoothstep(.15,.34,_Age);
            float neck=1.0-smoothstep(.02,.11,abs(uv.y-boundary));
            float erode=release*(.50+.28*neck);
            material.a*=smoothstep(erode-.20,erode+.20,
                noise2(uv*vec2(31.0,27.0)+vec2(-_Age*2.5,_Age*3.0)));
            material.rgb*=1.0-.35*release;
        }
        if(_TextureMotion>20.5 && _TextureMotion<21.5){
            vec2 q=vec2(uv.x*2.0,uv.y)-vec2(.54,.50);
            float growth=mix(.08,.80,smoothstep(-.01,.055,_Age));
            material.a*=1.0-smoothstep(growth-.045,growth+.025,length(q));
        }
        if(_TextureMotion>21.5 && _TextureMotion<22.5){
            vec2 q=vec2((uv.x-.5)*2.0,uv.y);
            float top=1.0-smoothstep(.46+.12*(q.x-.55)-.025,.46+.12*(q.x-.55)+.025,q.y);
            float right=smoothstep(.53-.025,.53+.025,q.x);
            float mask=released_region<.5?top:released_region<1.5?(1.0-top)*right:(1.0-top)*(1.0-right);
            material.a*=mask;
            float detach=.20*smoothstep(.11,.24,_Age);
            material.a*=smoothstep(detach-.025,detach+.025,length(q-vec2(.54,.50)));
            material.rgb*=1.0-.28*smoothstep(.14,.38,_Age);
        }
        if(_TextureMotion>24.5 && _TextureMotion<25.5){
            // No rectangular crop: complementary porous split through the empty gap.
            float boundary=1.18+.022*sin(uv.x*19.0)+.025*(noise2(uv*23.0)-.5);
            float lower=smoothstep(boundary-.025,boundary+.025,uv.x+uv.y);
            if(released_region<1.5)material.a*=released_region<.5?1.0-lower:lower;
            float consumed=smoothstep(.06,.34,_Age);
            float distance_to_gap=length((uv-vec2(.60,.58))*vec2(1.0,.85));
            float outer=smoothstep(.08,.55,distance_to_gap);
            float threshold=consumed*(.20+.54*outer);
            material.a*=smoothstep(threshold-.13,threshold+.13,noise2(uv*vec2(31.0,27.0)+vec2(_Age*2.0,-_Age*1.5)));
            material.rgb*=1.0-.35*smoothstep(.16,.35,_Age);
        }
        if(_TextureMotion>27.5 && _TextureMotion<28.5){
            vec2 local=(uv-vec2(.035,.39))/vec2(.42,.40);
            float radius=length((local-vec2(.52,.475))*vec2(1.0,.82));
            float front=mix(.02,.76,smoothstep(-.13,.055,_Age));
            material.a*=1.0-smoothstep(front-.06,front+.05,radius);
            float erosion=smoothstep(.28,.64,_Age);
            material.a*=smoothstep(erosion-.18,erosion+.18,noise2(local*vec2(29.0,23.0)));
            material.rgb*=.80;
        }
        if(_TextureMotion>30.5 && _TextureMotion<31.5){
            // Dry grain separates along its length; never advect it like fluid.
            float release=smoothstep(.085,.30,_Age);
            float grain=noise2(uv*vec2(13.0,71.0));
            float knot=1.0-smoothstep(.035,.14,length((uv-vec2(.34,.56))*vec2(1.0,1.1)));
            material.a*=1.0-knot*smoothstep(.035,.105,_Age);
            material.a*=smoothstep(release*.70-.16,release*.70+.16,grain);
            material.rgb*=1.0-.24*smoothstep(.11,.32,_Age);
        }
        if(_TextureMotion>31.5 && _TextureMotion<32.5){
            // Preserve real low-alpha electrical shoulder, without old L^.65 loss.
            float release=smoothstep(.035,.17,_Age);
            material.a*=smoothstep(release*.68-.15,release*.68+.15,noise2(uv*vec2(37.0,29.0)));
            material.rgb*=1.0-.22*release;
        }
        vec3 sampled=material.rgb;
        if(OUTPUT_IS_SRGB) sampled=mix(pow((sampled+.055)/1.055,vec3(2.4)),sampled/12.92,lessThanEqual(sampled,vec3(.04045)));
        rgb = _AuthoredRgba ? sampled * max(max(tint.r,tint.g),tint.b) : rgb * sampled * 1.4;
        alpha *= material.a;
    }
    ALBEDO=output_rgb(rgb);
    ALPHA=alpha;
}
