using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game
{
    // Depth-tested transparent geometry in the stage camera, before world post-processing/UI.
    // The atlas is original generated art; no source-game assets are used.
    public static class WorldBattleFx
    {
        sealed class Slot { public Transform T; public MeshRenderer R; public bool Busy; public int Version; public float Started; }
        static readonly Slot[] Pool = new Slot[24];
        static ParticleSystem _embers; static Material _emberMaterial;
        public static int ParticleCount => _embers!=null?_embers.particleCount:0;
        static Mesh _quad; static Material _material, _elementMaterial; static Texture2D _atlas, _silver, _arcane, _guard, _trail;
        static readonly MaterialPropertyBlock Props = new MaterialPropertyBlock();
        internal static void CollectElementRenderers(System.Collections.Generic.List<Renderer> output) {
            output.Clear();if(_elementMaterial==null)return;
            foreach(var slot in Pool)if(slot!=null && slot.Busy && slot.R!=null && slot.R.gameObject.activeInHierarchy && slot.R.sharedMaterial==_elementMaterial)output.Add(slot.R);
        }
        public static int EmittedCount {get;private set;}
        public static void Cancel() {
            if(_embers!=null)_embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach(var slot in Pool)if(slot!=null){slot.Version++;slot.Busy=false;if(slot.T!=null)slot.T.gameObject.SetActive(false);}
        }
        public static int ActiveCount { get {int n=0;foreach(var slot in Pool)if(slot!=null && slot.Busy)n++;return n+ParticleCount;} }
        static bool Ready()
        {
            if (!Stage.CombatWorld || Stage.Camera == null) return false;
            if (_material == null)
            {
                var shader = Shader.Find("DeckRogue/WorldBattleFx");
                _atlas = Resources.Load<Texture2D>("Art/fx/codex-vfx-atlas");
                if (shader == null || !shader.isSupported || _atlas == null) return false;
                _silver=Resources.Load<Texture2D>("Art/fx/cloud-silver-8f");
                _arcane=Resources.Load<Texture2D>("Art/fx/cloud-arcane_impact-8f");
                _guard=Resources.Load<Texture2D>("Art/fx/cloud-guard_break-8f");
                _material = new Material(shader); _material.SetTexture("_BaseMap", _atlas);
                _trail=new Texture2D(64,16,TextureFormat.RGBA32,false);_trail.name="world-strike-ribbon";_trail.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color[64*16];for(int y=0;y<16;y++)for(int x=0;x<64;x++) {
                    float u=(x+.5f)/64f,v=Mathf.Abs((y+.5f)/16f*2-1);
                    pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Sin(u*Mathf.PI),.45f)*Mathf.Pow(1-v,2));
                }_trail.SetPixels(pixels);_trail.Apply(false,true);
                _quad = new Mesh { name = "combat-fx-quad" };
                _quad.vertices = new[] { new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0) };
                _quad.colors = new[]{Color.white,Color.white,Color.white,Color.white};
                _quad.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
                _quad.triangles = new[] { 0,2,1,0,3,2 }; _quad.RecalculateBounds();
            }
            return true;
        }
        static void Emit(Vector3 from, Vector3 to, int cell, Color tint, float size, float duration, float angle, float sweep, bool projectile=false, Texture2D flipbook=null, bool ground=false, float gain=1.3f, float aspect=1f, bool authored=false)
        {
            if (!Ready()) return;
            Slot slot=null;
            int capacity=GfxQuality.Level==GfxLevel.Low ? 12 : 24;
            for(int i=0;i<capacity;i++) { if(Pool[i]==null)Pool[i]=new Slot(); if(Pool[i].T==null || !Pool[i].Busy){slot=Pool[i];break;} }
            // Under rapid AoE, recycle the oldest residue instead of dropping the newest hit.
            if(slot==null){slot=Pool[0];for(int i=1;i<capacity;i++)if(Pool[i].Started<slot.Started)slot=Pool[i];}
            if(slot.T==null)
            {
                var go=new GameObject("world-combat-fx"); slot.T=go.transform;
                go.AddComponent<MeshFilter>().sharedMesh=_quad; slot.R=go.AddComponent<MeshRenderer>();
                slot.R.sharedMaterial=_material; slot.R.shadowCastingMode=ShadowCastingMode.Off; slot.R.receiveShadows=false;
            }
            if(authored && ElementBattleFx.SeparateMaterial) {
                if(_elementMaterial==null){var shader=Shader.Find("DeckRogue/ElementBattleFx");if(shader!=null && shader.isSupported)_elementMaterial=new Material(shader);}
                slot.R.sharedMaterial=_elementMaterial!=null?_elementMaterial:_material;
            } else slot.R.sharedMaterial=_material;
            EmittedCount++;
            int version=++slot.Version; slot.Busy=true; slot.Started=Time.unscaledTime; slot.T.gameObject.SetActive(true);
            float inset=.5f/_atlas.width;
            Vector4 uv=new Vector4(.5f-2*inset,.5f-2*inset,(cell%2)*.5f+inset,cell<2?.5f+inset:inset);
            var facing=ground?Quaternion.Euler(90,0,0):Stage.LayoutRotation;
            Tween.Run(duration,k=>
            {
                if(slot.T==null || slot.Version!=version)return;
                float q=1-Mathf.Pow(1-k,2);
                if(flipbook!=null){int frame=authored?ElementBattleFx.Frame(k):Mathf.Min(7,Mathf.FloorToInt(k*7)+1);if(authored)ElementBattleFx.Observe(flipbook.name,frame);float px=.5f/flipbook.width,py=.5f/flipbook.height;uv=new Vector4(.25f-2*px,.5f-2*py,(frame%4)*.25f+px,frame<4?.5f+py:py);}
                if(flipbook==_trail)uv=new Vector4(1,1,0,0);
                slot.T.position=Vector3.Lerp(from,to,projectile?k:0);
                slot.T.rotation=facing*Quaternion.Euler(0,0,angle+(flipbook==null?sweep*q:0));
                slot.T.localScale=new Vector3(1,aspect,1)*size*(authored||projectile?1:Mathf.Lerp(flipbook!=null?.96f:.65f,1.08f,q));
                float alpha=authored?1:projectile?(flipbook==_trail?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,k)):1):flipbook!=null ? 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(flipbook==_silver?.28f:.68f,1,k)) : Mathf.Min(1,k*24)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,k)));
                Props.Clear(); Props.SetTexture("_BaseMap",flipbook!=null?flipbook:_atlas); Props.SetVector("_AtlasRect",uv); Props.SetColor("_Tint",new Color(tint.r*gain,tint.g*gain,tint.b*gain,tint.a*alpha)); slot.R.SetPropertyBlock(Props);
            },Ease.Linear,()=>{if(slot.Version==version){slot.Busy=false;if(slot.T!=null)slot.T.gameObject.SetActive(false);}});
        }
        static bool Socket(string key, out Vector3 point, out float height)
        {
            height=0f; Vector3 feet; if(!Ready() || !Stage.TryGetUnitBox(key,out feet,out height)){point=default;return false;}
            point=feet+(Stage.LayoutRotation*Vector3.up)*height*.5f-(Stage.LayoutRotation*Vector3.forward)*.28f;
            return true;
        }
        public static bool Hit(string target,string style,Color color,bool heavy,int index=0)
        {
            if(!Socket(target,out var p,out var h))return false;
            return HitAt(p,h,style,color,heavy,index,target);
        }
        public static bool HitAt(Vector3 p,float h,string style,Color color,bool heavy,int index=0,string target=null)
        {
            if(!Ready())return false;
            color=CombatPalette.ForFx(style,color);
            bool spell=style=="spell"||style=="light"||style=="spark"||style=="beam"||style=="throw";
            if(spell) {
                Emit(p,p,2,color,h*(heavy?2.0f:1.65f),.42f,0,0,false,_arcane);
                if(target!=null && Stage.TryGetUnitBox(target,out var feet,out var height)) Emit(feet+Vector3.up*.035f,feet,2,new Color(color.r,color.g,color.b,.65f),h*1.1f,.28f,0,0,false,_arcane,true,1.15f);
            } else if(style=="stomp" || style=="vine") {
                var impact=style=="vine"?new Color(.48f,.86f,.55f,.82f):new Color(.82f,.66f,.44f,.82f);
                if(target!=null && Stage.TryGetUnitBox(target,out var feet,out var height)) Emit(feet+Vector3.up*.045f,feet,2,impact,h*1.6f,.36f,0,0,false,_arcane,true,1.05f);
                Emit(p,p,1,impact,h*.48f,.16f,0,15);
            } else { Sparks(p,h,color); Emit(p,p,0,new Color(color.r,color.g,color.b,.95f),h*(heavy?2.6f:2.15f),.30f,index%2==0?0:90,0,false,_silver,false,2.0f); }
            Embers(p,h,color,heavy);
            Emit(p,p,1,new Color(color.r,color.g,color.b,.65f),h*(heavy?.70f:.48f),.19f,15,12);
            return true;
        }
        public static bool ElementAt(Vector3 p,float h,string element) {
            if(!Ready() || !ElementBattleFx.TryGet(element,out var texture,out var duration))return false;
            Emit(p,p,2,Color.white,h*(element=="heal"?1.25f:1.6f),duration,0,0,false,texture,false,1f,1f,true);
            return true;
        }
        public static void Trace(string target,Color? color=null) {
            if(target=="player")return;
            if(!Socket("player",out var a,out var ah)||!Socket(target,out var b,out var bh))return;
            var delta=Quaternion.Inverse(Stage.LayoutRotation)*(b-a);
            float width=new Vector2(delta.x,delta.y).magnitude;if(width<.2f)return;
            var start=Vector3.Lerp(a,b,.15f);var end=Vector3.Lerp(a,b,.86f);
            Emit(start,end,0,new Color((color??CombatPalette.Silver).r,(color??CombatPalette.Silver).g,(color??CombatPalette.Silver).b,.82f),width*.40f,.075f,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,0,true,_trail,false,2.1f,Mathf.Min(ah,bh)*.018f/(width*.40f));
        }
        static void Sparks(Vector3 p,float h,Color color) {
            // A small asymmetric fan: bright contact then outward fragments, not a full-screen flash.
            float[] angles={18,69,153,226,318};
            for(int i=0;i<angles.Length;i++) {
                float a=angles[i]*Mathf.Deg2Rad;var dir=Stage.LayoutRotation*new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                float len=h*(i%2==0?.65f:.43f);
                Emit(p+dir*h*.08f,p+dir*h*(i%2==0?.85f:.6f),0,new Color(color.r,color.g,color.b,.95f),len,.19f+i*.021f,angles[i],0,true,_trail,false,4f,.022f);
            }
        }
        static void Embers(Vector3 p,float h,Color color,bool heavy) {
            if(_embers==null) {
                var go=new GameObject("world-hit-embers");_embers=go.AddComponent<ParticleSystem>();
                _embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=_embers.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
                main.maxParticles=GfxQuality.Level==GfxLevel.Low?64:128;main.gravityModifier=0;main.startSpeed=0;main.startLifetime=.4f;
                var emission=_embers.emission;emission.enabled=false;var shape=_embers.shape;shape.enabled=false;
                var size=_embers.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var fade=_embers.colorOverLifetime;fade.enabled=true;var grad=new Gradient();
                grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.9f,.2f),new GradientAlphaKey(0,1)});fade.color=grad;
                var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color[256];for(int y=0;y<16;y++)for(int x=0;x<16;x++) {float rad=new Vector2((x-7.5f)/7.5f,(y-7.5f)/7.5f).magnitude;pixels[y*16+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-rad),1.6f));}
                texture.SetPixels(pixels);texture.Apply(false,true);
                _emberMaterial=new Material(_material);_emberMaterial.SetTexture("_BaseMap",texture);_emberMaterial.SetVector("_AtlasRect",new Vector4(1,1,0,0));_emberMaterial.SetColor("_Tint",new Color(5,5,5,1));
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=_emberMaterial;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                _embers.useAutoRandomSeed=false;_embers.randomSeed=4242;
            }
            _embers.Play();int fast=heavy?18:12,drift=heavy?6:4;
            for(int i=0;i<fast+drift;i++) {
                bool slow=i>=fast;float a=(i*137.508f+17)*Mathf.Deg2Rad;
                var dir=Stage.LayoutRotation*new Vector3(Mathf.Cos(a),Mathf.Sin(a)*.7f+.25f,0);
                var e=new ParticleSystem.EmitParams {position=p,velocity=dir*h*(slow?.35f:2.2f+(i%3)*.6f),startLifetime=slow?.32f+(i%3)*.06f:.18f+(i%4)*.03f,startSize=h*(slow?.018f:.024f),startColor=Color.Lerp(color,Color.white,slow?.15f:.45f)};
                _embers.Emit(e,1);
            }
        }
        public static void Cast(string source,string target,Color color) {
            if(!Ready())return;
            foreach(var key in new[]{source,target}) if(Stage.TryGetUnitBox(key,out var feet,out var h)) {
                var p=feet+Vector3.up*.035f;
                Emit(p,p,2,new Color(color.r,color.g,color.b,key==source?.48f:.25f),h*.85f,.3f,0,0,false,_arcane,true,1f);
            }
        }
        public static bool Projectile(string source,string target,Color color,float duration)
        {
            if(!Socket(source,out var a,out var ah)||!Socket(target,out var b,out var bh))return false;
            Emit(a,b,1,color,Mathf.Min(ah,bh)*.23f,duration,0,160,true);return true;
        }
        public static bool Guard(string target)
        {
            if(!Socket(target,out var p,out var h))return false;
            return GuardAt(p,h,target=="player"?1:-1);
        }
        public static bool GuardAt(Vector3 p,float h,float side)
        {
            if(!Ready())return false;
            p+=(Stage.LayoutRotation*Vector3.right)*h*.28f*side;
            Emit(p,p,3,new Color(.6f,.78f,1,.8f),h*.85f,.3f,side<0?180:0,0,false,_guard);return true;
        }
    }
}
