using System;
using System.Collections;
using UnityEngine;

namespace DeckRogue.Game
{
    // One bounded window per character, never an accumulated queue. Pure time-input logic is reviewable.
    public struct CombatImpactPause
    {
        public const float MaxWindow = .14f, ResumeGap = .050f;
        float _until, _cap, _ready;
        public float Remaining(float now) => Math.Max(0f, _until - now);
        public bool IsPaused(float now) => now < _until;
        public bool Request(float seconds, float now)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            seconds = Math.Min(seconds, MaxWindow);
            if (now < _until) _until = Math.Min(_cap, Math.Max(_until, now + seconds));
            else {
                if (now < _ready) return false;
                _cap = now + MaxWindow; _until = now + seconds;
            }
            _ready = _until + ResumeGap;
            return true;
        }
        public void Clear() { _until = _cap = _ready = 0f; }
    }

    public static partial class Stage
    {
        // World-space motion is owned by the visual unit. RectTransform/UI anchors stay put.
        public const float CombatImpactTime = 0.12f;
        public static bool CombatWorld => HD2DFlags.DioramaHere;

        public static void CombatApproach(string key, string target, bool ranged, int hits = 1, float reachLimit = .4f)
        {
            if (!CombatWorld || !_bound.TryGetValue(key, out var u) || u == null) return;
            Vector3 a, b; float ah, bh;
            if (!TryGetUnitBox(key, out a, out ah) || !TryGetUnitBox(target, out b, out bh)) return;
            int version = ++u.MotionVersion;
            u.ImpactPause.Clear();
            Vector3 start = u.CombatOffset;
            Vector3 delta = b - a; delta.y = 0f;
            float distance = delta.magnitude;
            Vector3 dir = distance > 0.01f ? delta / distance : Vector3.right;
            // Weapon/hand reach: stop before the target silhouette, never cross its centre.
            Vector3 travel = ranged ? Vector3.zero : dir * Mathf.Min(ah*reachLimit,Mathf.Max(0f, distance - (ah * 0.40f + bh * 0.25f)));
            float chain = ranged ? 0f : Mathf.Clamp(hits-1,0,7)*0.4f;
            float release = 0.20f + Mathf.Min(chain,.12f), duration = release + 0.18f;
            u.Play(ranged && u.ArtName!="leader_blue" ? "block" : "attack");
            Tween.Run(duration, k =>
            {
                if (u == null || u.MotionVersion != version) return;
                float t = k * duration, amount;
                if (t < 0.035f) amount = -0.035f * Mathf.SmoothStep(0, 1, t / 0.035f);
                else if (t < CombatImpactTime) amount = Mathf.Lerp(-0.035f, 1, Mathf.SmoothStep(0, 1, (t - 0.035f) / 0.085f));
                else if (t < release) amount = 1;
                else amount = 1 - Mathf.SmoothStep(0, 1, (t - release) / 0.18f);
                u.CombatOffset = start * (1-k) + travel * amount;
            }, Ease.Linear, () => { if (u != null && u.MotionVersion == version) u.CombatOffset = Vector3.zero; });
        }

        // Opt-in choreography uses the same contact time as Presenter; never changes the global clock.
        // HD-2D の攻撃演出 (2026-10-27・dot の Godot 版の移植): reachWorld＝斧の先が相手の縁に届く距離 (Hd2dAttackFx.Approach が計る)、
        // poseLead＝当たりの何秒前に攻撃のコマを始めるか (dot は .22・火は .28。旧は .12)、attackFrameDur＝攻撃のコマの長さ (dot の BattleActor.ApplyHd2dPose)。
        static readonly System.Collections.Generic.Dictionary<StageUnit,float[]> _attackDurOriginal=new System.Collections.Generic.Dictionary<StageUnit,float[]>();
        public static void ShowcaseApproach(string key,string target,bool ranged,int hits,CardVfxProfile.Showcase profile,float? reachWorld=null,float poseLead=.12f,float[] attackFrameDur=null)
        {
            if(profile==null || !CombatWorld || !_bound.TryGetValue(key,out var u) || u==null)return;
            if(!TryGetUnitBox(key,out var a,out var ah) || !TryGetUnitBox(target,out var b,out var bh))return;
            int version=++u.MotionVersion;var start=u.CombatOffset;
            // The queued action acquired the actor after prior contacts and their local pause. An older ticket cannot freeze its windup.
            u.ImpactPause.Clear();
            var delta=b-a;delta.y=0;float distance=delta.magnitude;
            var dir=distance>.01f?delta/distance:LayoutRotation*Vector3.right;
            float reach=reachWorld.HasValue?Mathf.Clamp(reachWorld.Value,0f,Mathf.Max(0f,distance-ah*.05f)):ranged?ah*.035f:Mathf.Min(ah*profile.Reach,Mathf.Max(0,distance-ah*.40f-bh*.25f));
            float release=profile.Contact+Mathf.Max(0,hits-1)*profile.Interval+.045f;
            float duration=release+profile.Recovery;
            if(attackFrameDur!=null){
                float[] original;if(!_attackDurOriginal.TryGetValue(u,out original)){u.FrameDur.TryGetValue("attack",out original);_attackDurOriginal[u]=original;}
                u.FrameDur["attack"]=attackFrameDur;
                Tween.After(duration+.05f,()=>{if(u==null || !_attackDurOriginal.TryGetValue(u,out var back))return;if(back!=null)u.FrameDur["attack"]=back;else u.FrameDur.Remove("attack");});
            }
            // Keep the authored attack frames (including Mizore's fix), shift their start to release.
            u.Play("block");
            Tween.After(Mathf.Max(0,profile.Contact-poseLead),()=>{if(u!=null && u.MotionVersion==version)u.Play("attack");});
            Tween.RunPresentation(ShowcaseMotion(u,duration,()=>u.MotionVersion==version,k=>{
                float t=k*duration,amount;
                if(t<profile.Windup)amount=-.16f*Mathf.SmoothStep(0,1,t/profile.Windup);
                else if(t<profile.Contact)amount=Mathf.Lerp(-.16f,1,Mathf.SmoothStep(0,1,(t-profile.Windup)/(profile.Contact-profile.Windup)));
                else if(t<release)amount=1;
                else amount=1-Mathf.SmoothStep(0,1,(t-release)/profile.Recovery);
                u.CombatOffset=start*(1-k)+dir*reach*amount;
            },()=>u.CombatOffset=Vector3.zero));
        }

        public static void BeginCombatAction(string key) {
            if(!CardVfxProfile.ShowcaseOn || !_bound.TryGetValue(key,out var u)||u==null)return;
            // Every card takes ownership, including block/unknown cards with no ShowcaseApproach.
            u.MotionVersion++;u.ImpactPause.Clear();u.CombatOffset=Vector3.zero;
        }

        // Captured unit identities prevent delayed tickets from stopping replacement units with the same key.
        public sealed class ImpactCapture { public UnityEngine.Object Actor, Target; public int MotionVersion; }
        public static ImpactCapture CaptureImpact(string target) {
            _bound.TryGetValue("player",out var actor);_bound.TryGetValue(target,out var victim);
            return new ImpactCapture {Actor=actor,Target=victim,MotionVersion=actor!=null?actor.MotionVersion:-1};
        }
        // The board/ledger proves identity before requesting reuse. Do not revive a
        // dissolved/inactive actor or preserve a unit across an artwork replacement.
        static bool TryRebindPresentationUnit(string key,RectTransform rect,UnityEngine.UI.Image img,Sprite sprite,float depth) {
            if(!_bound.TryGetValue(key,out var retained) || retained==null || retained.ArtName!=sprite.name
                || retained.BaseTex!=sprite.texture || retained.DissolveK>0f || !retained.gameObject.activeSelf)return false;
            retained.Rect=rect;retained.Img=img;retained.Depth=depth;
            retained.RefreshBinding();return true; // Transform refresh cannot advance pose/flash clocks.
        }
        static readonly System.Collections.Generic.Dictionary<(Texture2D,int,int,int,int),float> _visibleBaseFractions=new System.Collections.Generic.Dictionary<(Texture2D,int,int,int,int),float>();
        internal static bool TryCapturedVisibleHeight(ImpactCapture capture,out float height,out string reason) {
            height=0f;reason="captured_base_geometry_unavailable";
            var u=capture?.Target as StageUnit;
            if(u==null || u.BaseTex==null || !u.HasBox)return false;
            float baseHeight=u.BaseCanvasHeightWorld;
            if(baseHeight<=0f)return false;
            var tex=u.BaseTex;int x0=Mathf.RoundToInt(u.BaseUvOffset.x*tex.width),y0=Mathf.RoundToInt(u.BaseUvOffset.y*tex.height);
            int w=Mathf.RoundToInt(u.BaseUvScale.x*tex.width),h=Mathf.RoundToInt(u.BaseUvScale.y*tex.height);
            if(w<=0 || h<=0 || x0<0 || y0<0 || x0+w>tex.width || y0+h>tex.height)return false;
            var key=(tex,x0,y0,w,h);
            if(!_visibleBaseFractions.TryGetValue(key,out var fraction)) {
                try {
                    var pixels=tex.GetPixels32();int lo=h,hi=-1;
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[(y0+y)*tex.width+x0+x].a>40){lo=Math.Min(lo,y);hi=Math.Max(hi,y);}
                    fraction=hi>=lo?(hi-lo+1f)/h:0f;_visibleBaseFractions[key]=fraction;
                } catch(Exception){reason="captured_base_alpha_unreadable";return false;}
            }
            height=baseHeight*fraction;
            if(height<=0f || float.IsNaN(height) || float.IsInfinity(height)){height=0f;reason="captured_base_alpha_empty";return false;}
            reason=null;return true;
        }
        public static bool ImpactTargetCurrent(string key,ImpactCapture capture) => capture!=null && _bound.TryGetValue(key,out var target) && target!=null && target.Rect!=null && target==capture.Target;
        public static void BindImpactActor(ImpactCapture capture) {
            if(capture==null)return;
            _bound.TryGetValue("player",out var actor);
            capture.Actor=actor;capture.MotionVersion=actor!=null?actor.MotionVersion:-1;
        }
        public static float ImpactPauseRemaining(string key) => _bound.TryGetValue(key,out var u)&&u!=null ? u.ImpactPause.Remaining(Time.time) : 0f;
        public static int ActiveImpactPauseCount {get {int n=0;foreach(var u in _bound.Values)if(u!=null&&u.Rect!=null&&u.ImpactPause.IsPaused(Time.time))n++;return n;}}
        // Runtime review reads actual local pose/ownership; these fields never drive gameplay.
        public static object ImpactPose(string key) {
            if(!_bound.TryGetValue(key,out var u)||u==null||u.Rect==null)return null;
            return new {unit=u.GetEntityId().ToString(),motion=u.MotionVersion,recoil=u.RecoilVersion,anim=u.Anim,frame=u.Frame,frameT=u.FrameT,
                flashRemaining=u.FlashT,materialFlash=u.Mat!=null&&u.Mat.HasProperty("_Flash")?u.Mat.GetFloat("_Flash"):0f,offset=new[]{u.CombatOffset.x,u.CombatOffset.y,u.CombatOffset.z},kick=new[]{u.CombatRecoilOffset.x,u.CombatRecoilOffset.y,u.CombatRecoilOffset.z}};
        }
        public static float ShowcaseImpact(string target,ImpactCapture captured,float seconds,bool heavy) {
            if(!CardVfxProfile.ShowcaseOn || captured==null || !_bound.TryGetValue(target,out var u) || u==null || u.Rect==null || u!=captured.Target)return 0f;
            int version=++u.RecoilVersion;
            u.Play("hurt");
            u.ImpactPause.Request(seconds,Time.time);
            if(_bound.TryGetValue("player",out var actor)&&actor!=null&&actor.Rect!=null&&actor==captured.Actor&&actor.MotionVersion==captured.MotionVersion)
                actor.ImpactPause.Request(seconds,Time.time);
            float h=u.HasBox?u.HeightWorld:2f;
            var kick=(LayoutRotation*Vector3.right)*(target=="player"?-1f:1f)*h*(heavy?.15f:.085f);
            // A small immediate reaction reads on the contact frame; its recoil then continues after the hold.
            Tween.RunPresentation(ShowcaseMotion(u,heavy?.29f:.22f,()=>u.RecoilVersion==version,k=>{
                float amount=k<.18f?Mathf.Lerp(.35f,1f,Mathf.SmoothStep(0,1,k/.18f)):1-Mathf.SmoothStep(0,1,(k-.18f)/.82f);
                u.CombatRecoilOffset=kick*amount;
            },()=>u.CombatRecoilOffset=Vector3.zero));
            if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("impactPause",target+":"+seconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            return u.ImpactPause.Remaining(Time.time);
        }
        static IEnumerator ShowcaseMotion(StageUnit u,float duration,Func<bool> current,Action<float> update,Action done) {
            float elapsed=0f;update(0f);
            while(elapsed<duration) {
                yield return null;
                if(u==null || u.Rect==null || !current())yield break;
                if(u.ImpactPause.IsPaused(Time.time))continue;
                elapsed=Mathf.Min(duration,elapsed+Time.deltaTime);update(elapsed/duration);
            }
            done();
        }

        public static bool HasCombatAnimation { get {
            foreach(var u in _bound.Values)
                if(u!=null && u.Rect!=null && u.gameObject.activeInHierarchy && u.Anim!="idle" && u.Anims.TryGetValue(u.Anim,out var frames) && frames.Count>0)return true;
            return false;
        } }
        public static bool HasCombatMotion { get {foreach(var u in _bound.Values)if(u!=null && (u.CombatOffset.sqrMagnitude+u.CombatRecoilOffset.sqrMagnitude)>.000001f)return true;return false;} }
        public static void CancelCombatMotion() {
            foreach(var u in _bound.Values) if(u!=null){u.MotionVersion++;u.RecoilVersion++;u.ImpactPause.Clear();u.CombatOffset=Vector3.zero;u.CombatRecoilOffset=Vector3.zero;if(u.Rect!=null)u.Play("idle");}
        }
        public static void CombatRecoil(string key, float direction, bool heavy)
        {
            if (!CombatWorld || !_bound.TryGetValue(key, out var u) || u == null) return;
            int version = ++u.RecoilVersion;
            u.Play("hurt");
            float h = u.HasBox ? u.HeightWorld : 2f;
            Tween.Run(heavy ? 0.29f : 0.22f, k =>
            {
                if (u == null || u.RecoilVersion != version) return;
                float kick = k < 0.18f ? Mathf.SmoothStep(0,1,k/0.18f) : 1-Mathf.SmoothStep(0,1,(k-0.18f)/0.82f);
                u.CombatRecoilOffset = (LayoutRotation * Vector3.right) * direction * h * (heavy ? 0.12f : 0.065f) * kick;
            }, Ease.Linear, () => { if (u != null && u.RecoilVersion == version) u.CombatRecoilOffset = Vector3.zero; });
        }
    }
}
