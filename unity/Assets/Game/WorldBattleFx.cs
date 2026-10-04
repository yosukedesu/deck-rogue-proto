using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game
{
    // Depth-tested transparent geometry in the stage camera, before world post-processing/UI.
    // The atlas is original generated art; no source-game assets are used.
    public static class WorldBattleFx
    {
        sealed class Slot { public Transform T; public MeshRenderer R; public bool Busy; public int Version; }
        static readonly Slot[] Pool = new Slot[24];
        static Mesh _quad; static Material _material; static Texture2D _atlas;
        static readonly MaterialPropertyBlock Props = new MaterialPropertyBlock();
        static bool Ready()
        {
            if (!Stage.CombatWorld || Stage.Camera == null) return false;
            if (_material == null)
            {
                var shader = Shader.Find("DeckRogue/WorldBattleFx");
                _atlas = Resources.Load<Texture2D>("Art/fx/codex-vfx-atlas");
                if (shader == null || !shader.isSupported || _atlas == null) return false;
                _material = new Material(shader); _material.SetTexture("_BaseMap", _atlas);
                _quad = new Mesh { name = "combat-fx-quad" };
                _quad.vertices = new[] { new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0) };
                _quad.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
                _quad.triangles = new[] { 0,2,1,0,3,2 }; _quad.RecalculateBounds();
            }
            return true;
        }
        static void Emit(Vector3 from, Vector3 to, int cell, Color tint, float size, float duration, float angle, float sweep, bool projectile=false)
        {
            if (!Ready()) return;
            Slot slot=null;
            int capacity=GfxQuality.Level==GfxLevel.Low ? 12 : 24;
            for(int i=0;i<capacity;i++) { if(Pool[i]==null)Pool[i]=new Slot(); if(Pool[i].T==null || !Pool[i].Busy){slot=Pool[i];break;} }
            if(slot==null)return;
            if(slot.T==null)
            {
                var go=new GameObject("world-combat-fx"); slot.T=go.transform;
                go.AddComponent<MeshFilter>().sharedMesh=_quad; slot.R=go.AddComponent<MeshRenderer>();
                slot.R.sharedMaterial=_material; slot.R.shadowCastingMode=ShadowCastingMode.Off; slot.R.receiveShadows=false;
            }
            int version=++slot.Version; slot.Busy=true; slot.T.gameObject.SetActive(true);
            float inset=.5f/_atlas.width;
            Vector4 uv=new Vector4(.5f-2*inset,.5f-2*inset,(cell%2)*.5f+inset,cell<2?.5f+inset:inset);
            var facing=Stage.LayoutRotation;
            Tween.Run(duration,k=>
            {
                if(slot.T==null || slot.Version!=version)return;
                float q=1-Mathf.Pow(1-k,2);
                slot.T.position=Vector3.Lerp(from,to,projectile?k:0);
                slot.T.rotation=facing*Quaternion.Euler(0,0,angle+sweep*q);
                slot.T.localScale=Vector3.one*size*(projectile?1:Mathf.Lerp(.65f,1.08f,q));
                float alpha=projectile?Mathf.Min(1,k*12):Mathf.Min(1,k*18)*(1-Mathf.SmoothStep(.15f,1,k));
                Props.Clear(); Props.SetVector("_AtlasRect",uv); Props.SetColor("_Tint",new Color(tint.r*1.65f,tint.g*1.65f,tint.b*1.65f,tint.a*alpha)); slot.R.SetPropertyBlock(Props);
            },Ease.Linear,()=>{if(slot.Version==version){slot.Busy=false;if(slot.T!=null)slot.T.gameObject.SetActive(false);}});
        }
        static bool Socket(string key, out Vector3 point, out float height)
        {
            height=0f; Vector3 feet; if(!Ready() || !Stage.TryGetUnitBox(key,out feet,out height)){point=default;return false;}
            point=feet+(Stage.LayoutRotation*Vector3.up)*height*.5f-(Stage.LayoutRotation*Vector3.forward)*.12f;
            return true;
        }
        public static bool Hit(string target,string style,Color color,bool heavy,int index=0)
        {
            if(!Socket(target,out var p,out var h))return false;
            bool spell=style=="spell"||style=="light"||style=="spark"||style=="beam"||style=="throw";
            Emit(p,p,spell?2:0,color,h*(heavy?1.45f:1.1f),spell?.25f:.18f,spell?0:(index%2==0?-40:35),spell?22:-36);
            Emit(p,p,1,new Color(1,.82f,.48f,.85f),h*(heavy?.50f:.32f),.13f,15,12);
            return true;
        }
        public static bool Projectile(string source,string target,Color color,float duration)
        {
            if(!Socket(source,out var a,out var ah)||!Socket(target,out var b,out var bh))return false;
            Emit(a,b,1,color,Mathf.Min(ah,bh)*.23f,duration,0,160,true);return true;
        }
        public static bool Guard(string target)
        {
            if(!Socket(target,out var p,out var h))return false;
            float side=target=="player"?1:-1;
            p+=(Stage.LayoutRotation*Vector3.right)*h*.28f*side;
            Emit(p,p,3,new Color(.65f,.88f,1,.8f),h*.85f,.22f,side<0?180:0,0);return true;
        }
    }
}
