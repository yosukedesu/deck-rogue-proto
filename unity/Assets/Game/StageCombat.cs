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
        public static void ShowcaseApproach(string key,string target,bool ranged,int hits,CardVfxProfile.Showcase profile)
        {
            if(profile==null || !CombatWorld || !_bound.TryGetValue(key,out var u) || u==null)return;
            if(!TryGetUnitBox(key,out var a,out var ah) || !TryGetUnitBox(target,out var b,out var bh))return;
            int version=++u.MotionVersion;var start=u.CombatOffset;
            // A new input owns the actor immediately. An older ticket cannot freeze its windup.
            u.ImpactPause.Clear();
            var delta=b-a;delta.y=0;float distance=delta.magnitude;
            var dir=distance>.01f?delta/distance:LayoutRotation*Vector3.right;
            float reach=ranged?ah*.035f:Mathf.Min(ah*profile.Reach,Mathf.Max(0,distance-ah*.40f-bh*.25f));
            float release=profile.Contact+Mathf.Max(0,hits-1)*profile.Interval+.045f;
            float duration=release+profile.Recovery;
            // Keep the authored attack frames (including Mizore's fix), shift their start to release.
            u.Play("block");
            Tween.After(Mathf.Max(0,profile.Contact-.12f),()=>{if(u!=null && u.MotionVersion==version)u.Play("attack");});
            Tween.I.StartCoroutine(ShowcaseMotion(u,duration,()=>u.MotionVersion==version,k=>{
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
        public static float ImpactPauseRemaining(string key) => _bound.TryGetValue(key,out var u)&&u!=null ? u.ImpactPause.Remaining(Time.time) : 0f;
        public static int ActiveImpactPauseCount {get {int n=0;foreach(var u in _bound.Values)if(u!=null&&u.Rect!=null&&u.ImpactPause.IsPaused(Time.time))n++;return n;}}
        // Runtime review reads actual local pose/ownership; these fields never drive gameplay.
        public static object ImpactPose(string key) {
            if(!_bound.TryGetValue(key,out var u)||u==null||u.Rect==null)return null;
            return new {unit=u.GetEntityId().ToString(),motion=u.MotionVersion,recoil=u.RecoilVersion,anim=u.Anim,frame=u.Frame,frameT=u.FrameT,
                offset=new[]{u.CombatOffset.x,u.CombatOffset.y,u.CombatOffset.z},kick=new[]{u.CombatRecoilOffset.x,u.CombatRecoilOffset.y,u.CombatRecoilOffset.z}};
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
            Tween.I.StartCoroutine(ShowcaseMotion(u,heavy?.29f:.22f,()=>u.RecoilVersion==version,k=>{
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

        public static bool HasCombatMotion { get {foreach(var u in _bound.Values)if(u!=null && (u.CombatOffset.sqrMagnitude+u.CombatRecoilOffset.sqrMagnitude)>.000001f)return true;return false;} }
        public static void CancelCombatMotion() {
            foreach(var u in _bound.Values) if(u!=null){u.MotionVersion++;u.RecoilVersion++;u.ImpactPause.Clear();u.CombatOffset=Vector3.zero;u.CombatRecoilOffset=Vector3.zero;}
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
