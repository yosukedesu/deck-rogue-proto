// StageFx.cs — 舞台の技の光 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-4)。
// 骨組み (P00) の口に、P09 (W1) が中身を書いた: 点光源のプール (設計図 look_act1.json の hitLight.maxLights 個・既定3。StageLook.Apply が用意する)。
// 演出からの呼び出しは P13 (W2) が書いた: Presenter の2Dの当たりと同じ所で、下の「演出からの口」(PlayerHit・FoeHit・Guard・Cast・Finish) を呼ぶ。
// 色は当たりの形ごと (斬撃＝暖かい白・呪文＝脈の青緑・灯＝暖色・火種＝橙・敵の当たり＝朱・防いだ＝空色・とどめ＝白)。大きい当たりは強さ×1.6 で影あり (PC だけ)。
// 同時に灯るのは3つまで (空きが無ければいちばん古い光を使い回す)。
// stage=old・StageLook が光を当てていない時は何もしない (口は頭の Live で返る。乱数も Tween も使わない = old の画を1画素も変えない)。
// 影は大きい当たり (shadow=true) だけ。影を落とす光の数の上限 (PC 3・スマホ 2) は StageLook と貸し借りする (上限に届いていれば逆光の影を一時的に止めて借りる)。
// 光の減衰は Time.deltaTime (det の撮影では 1/60 秒ずつ・ヒットストップの間はゆっくり)。
// 口の強さ・長さ・色は設計図 look_act1.json の hitLight に任意のキーで書ける (無ければコードの既定。StageLookData.Raw から読む。下の Tune)。
// 段2 (2026-10-03 レーン M・約束 docs/design/hd2d-stage2/contracts.md §C5): 寄りの技の光の口 MotionLight と、敵の大技の赤い合図の口 MotionCue を足した。
// 呼ぶのは StageMotion だけ (門 = StageMotion.On が真の時だけ)。寄りの光は届く距離を光ごとに持ち、印 (StageHitReceive.Mark) を付ける。
// 同じ光を当たりごとに灯し直す (reuse = 前に返した番号。まだ灯っていればその光の明るさ・色・場所を新しくする = 多段で光の数が増えない)。
// 印の付いた光を普通の当たりに使い回す時は印を外す (Slot.Marked)。門が偽なら印は一度も付かない = 今の口 (PlayerHit・FoeHit・Guard・Cast・Finish) の通り方は今と同じ。
// 印は寄りの光が灯っている間だけ: 減衰で消えた時 (Tick)・全部消す時 (StopAll)・動きを止める時 (MotionStop) に外す (2026-10-03 反証: 消えた光の印が次の幕へ残っていた)。
// 直し 2026-10-03: MotionCue は灯した光の番号を返す (敵の大技を光で出す時、暖の段が合図の光を MotionLight の reuse で灯し直す = 光の数を増やさない)。
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public static class StageFx
    {
        /// <summary>
        /// 当たりの光: world に色 color の点光源を灯し、dur 秒で消す (灯した瞬間がいちばん明るく、(1 − 経過÷dur)² で弱まる)。
        /// intensity = 設計図の hitLight.refDist (既定 1.5 unit) の距離での明るさ (Light.intensity = intensity × refDist²。URP の点光源は距離の2乗で弱まるため)。
        /// 届く距離は hitLight.range (既定 4)。同時に hitLight.maxLights (既定 3) まで。空きが無ければ、いちばん古い光を使い回す (P13: 計画の「古い物から消す」)。
        /// shadow = true (大きい当たり) の時だけ影を落とす。上限を超えるなら StageLook が逆光の影を一時的に止めて貸し、それもできなければ影なしで灯す
        /// </summary>
        public static void HitLight(Vector3 world, Color color, float intensity, float dur, bool shadow)
        {
            LightAt(world, color, intensity, dur, 0f, shadow, "direct", null);
        }

        /// <summary>HitLight の中身。hold = 灯してから dur のこの割合までは弱めない (その後 (残り÷(1−hold))² で消える)。kind・key は記録 (dumplayout・ログ) 用。
        /// range = 届く距離 (0 以下 = 設計図の hitLight.range。段2 の寄りの光だけが渡す)・mark = 技の光の印 (StageHitReceive.Mark。寄りの光だけ true)。
        /// 返り値 = 灯した光の番号 (MotionLight の灯し直しに使う。灯さなかったら 0)</summary>
        static long LightAt(Vector3 world, Color color, float intensity, float dur, float hold, bool shadow, string kind, string key, float range = -1f, bool mark = false, Slot assigned = null)
        {
            if (!Live) return 0;
            if (!(intensity > 0f) || !(dur > 0f) || float.IsInfinity(intensity) || float.IsInfinity(dur)) return 0;
            if (float.IsNaN(world.x) || float.IsNaN(world.y) || float.IsNaN(world.z)) return 0;
            var h = StageLook.Current != null ? StageLook.Current.Hit : Defaults;
            Prepare(h);
            var s = assigned ?? Pick();
            if (s == null) return 0;
            bool evicted = s.Lit;
            if (evicted) _evicted++;
            Release(s);   // 使い回す光が借りていた影を先に返す
            var l = s.Light;
            if (s.Marked != mark) { StageHitReceive.Mark(l, mark); s.Marked = mark; }   // 段2: 寄りの光に使った光を普通の当たりに使う時は印を外す (印を付けたことが無ければ呼ばない)
            l.transform.position = world;
            l.color = new Color(color.r, color.g, color.b, 1f);
            l.range = Mathf.Max(0.1f, range > 0f ? range : h.Range);
            s.Peak = intensity * h.RefDist * h.RefDist;
            s.T = 0f;
            s.Dur = dur;
            s.Hold = Mathf.Clamp(hold, 0f, 0.9f);
            s.AuthoredHold=0f;s.DecayTau=0f;s.ReleasePhase=0f;s.AuthoredStarted=0f;s.ContractOwner=null;
            s.Seq = ++_seq;
            s.Kind = kind;
            s.Key = key;
            l.intensity = s.Peak;
            l.shadows = LightShadows.None;
            if (shadow && StageLook.TryBorrowShadowSlot()) { s.Borrowed = true; l.shadows = LightShadows.Hard; }
            l.enabled = true;
            s.Lit = true;
            _calls++;
            int lit = LitCount;
            if (lit > _maxLit) _maxLit = lit;
            Remember(s, world, intensity, evicted);
            if (HD2DFlags.Det || HD2DFlags.DumpLayout)
                Debug.Log("[StageFx] 技の光 " + kind + (key != null ? " " + key : "") + " 強さ " + F(intensity) + " " + F(dur) + "秒" + (s.Borrowed ? " 影" : "")
                    + " 灯っている " + lit + "/" + Math.Min(_want, _slots.Count) + (evicted ? " (いちばん古い光を使い回した)" : "") + " frame " + Time.frameCount);
            OwnPresentationLight(s);
            return s.Seq;
        }
        static void OwnPresentationLight(Slot slot) {
            int version=++slot.PresentationVersion;long sequence=slot.Seq;
            slot.PresentationOwned=Tween.PresentationActive;
            if(slot.PresentationOwned)Tween.TrackPresentation(()=>slot.Lit && slot.Seq==sequence && slot.PresentationVersion==version,
                ()=>RetirePresentationLight(slot,sequence,version));
        }
        static void RetirePresentationLight(Slot slot,long sequence,int version) {
            if(slot==null || slot.Seq!=sequence || slot.PresentationVersion!=version || !slot.PresentationOwned)return;
            slot.Lit=false;slot.PresentationOwned=false;
            if(slot.Light!=null){slot.Light.enabled=false;slot.Light.intensity=0f;}
            Release(slot);Unmark(slot);
        }
        public static void CancelPresentationLights() {
            foreach(var slot in _slots)if(slot.PresentationOwned)RetirePresentationLight(slot,slot.Seq,slot.PresentationVersion);
        }

        // ================================================================ 演出からの口 (P13・W2。Presenter が2Dの当たりと同じ所で呼ぶ)

        /// <summary>技の光を灯せる時 (stage=diorama で StageLook が光を当てている)。false なら下の口は全部なにもしない (old の画を変えない)</summary>
        public static bool Live => HD2DFlags.DioramaHere && StageLook.Active;

        /// <summary>
        /// 自分の札の当たり (Presenter の PlayerHitFx と同じ所)。key = 当たった敵の板 ("enemy0"…)。
        /// style = 当たりの形 (Presenter.CardHitStyle: slash・fang・horn・vine・stomp・spell・light・spark)。色は形ごと (物理＝暖かい白・spell＝脈の青緑・light＝暖色・spark＝橙)。
        /// big = 大きい当たり (2Dと同じ: 15以上か急所) → 強さ×bigMul (既定 1.6)・影 (PC だけ)。蔦は鞭の先が届く vineDelay (既定 0.13 秒) 後に灯す
        /// </summary>
        public static void PlayerHit(string key, string style, bool big, Color? effectColor=null,DamageVfxIntensity.Visual visual=null)
        {
            if (!Live) return;
            if(Stage.CombatWorld) { WorldStrike(key,big,CombatPalette.ForFx(style,effectColor??Tune.ColorFor(style)),visual?.Light); return; }
            var t = Tune;
            Color c = t.ColorFor(style);
            float k = visual?.Light??(big ? t.BigMul : 1f);
            if(visual!=null){big=visual.Light>1f;SyncReceiptShadow(key,big);}
            float delay = style == "vine" ? t.VineDelay : 0f;
            At(key, new Vector2(0.5f, t.Height), c, t.Intensity * k, t.Dur, t.Hold, big && ShadowOk, "hit:" + (style ?? "slash") + (big ? "+" : ""), delay);
        }

        /// <summary>
        /// 敵→自分の当たり (Presenter の HitFx と同じ所。とげの反射も)。key = 自分の板 ("player")。tint = 2Dの飛び道具・光線の色 (null = 朱)。
        /// big = 2Dの大きい当たり (HP を12以上失った) → 強さ×bigMul・影 (PC だけ)
        /// </summary>
        public static void FoeHit(string key, Color? tint, bool big,DamageVfxIntensity.Visual visual=null)
        {
            if (!Live) return;
            var t = Tune;
            Color c = tint.HasValue ? tint.Value : t.Foe;
            float k = (visual?.Light??(big ? t.BigMul : 1f)) * t.FoeMul;
            if(visual!=null){big=visual.Light>1f;SyncReceiptShadow(key,big);}
            At(key, new Vector2(0.5f, t.Height), c, t.Intensity * k, t.Dur, t.Hold, big && ShadowOk, "foe" + (big ? "+" : ""), 0f);
        }

        /// <summary>
        /// 完全に防いだ (Presenter の GuardFx と同じ所)。key = 盾で受けた側の板。side = 盾の面を構える向き (攻撃が来る側。−1 = 左・+1 = 右)。
        /// 空色・guardDur (既定 0.18 秒)・影なし
        /// </summary>
        public static void Guard(string key, float side)
        {
            if (!Live) return;
            var t = Tune;
            float x = 0.5f + Mathf.Clamp(side, -1f, 1f) * 0.3f;
            At(key, new Vector2(x, t.Height), t.GuardColor, t.Intensity * t.GuardMul, t.GuardDur, t.Hold, false, "guard", 0f);
        }

        /// <summary>
        /// 呪文の構え (Presenter の CastFx と同じ所)。key = 自分の板。体の前 (敵の側) で、色は形 (spell＝脈の青緑・light＝暖色)。castDur (既定 0.3 秒)・影なし
        /// </summary>
        public static void Cast(string key, string style, Color? effectColor=null)
        {
            if (!Live) return;
            var t = Tune;
            At(key, new Vector2(0.78f, 0.5f), effectColor??t.ColorFor(style), t.Intensity * t.CastMul, t.CastDur, t.Hold, false, "cast:" + (style ?? "spell"), 0f);
        }

        /// <summary>
        /// とどめ (戦闘を決めた一撃。Presenter の HitStop と同じ所)。key = 倒れる敵の板。白・finishDur (既定 0.3 秒)・強さ×finishMul (既定 2)・影 (PC だけ)。
        /// 同じ一撃の PlayerHit の代わりに呼ぶ (2つ灯さない)。ヒットストップの間は時間がゆっくりなので長く残る
        /// </summary>
        public static void Finish(string key, Color? effectColor=null,DamageVfxIntensity.Visual visual=null)
        {
            if (!Live) return;
            var t = Tune;
            if(Stage.CombatWorld && effectColor.HasValue){WorldStrike(key,true,effectColor.Value,visual?.Light);return;}
            At(key, new Vector2(0.5f, t.Height), t.FinishColor, t.Intensity * (visual?.Light??t.FinishMul), t.FinishDur, t.Hold, visual==null&&ShadowOk, "finish", 0f);
        }

        static readonly Dictionary<string,long> _worldContacts = new Dictionary<string,long>();
        static long _worldBounce;
        // Contact + shared broad bounce. Repeated hits refresh the same light, never extinguish it.
        // Four enemies + player + one bounce fit the bounded six-light pool.
        // Preserve the approved world light; weak mode exists only for isolated autopilot comparison.
        public static void ElementHit(string key,string element,Color color,bool heavy=false,DamageVfxIntensity.Visual visual=null) {
            if(!Live)return;
            if(ElementBattleFx.LightReviewMode!="weak"){WorldStrike(key,heavy,color,visual?.Light);return;}
            if(visual!=null)SyncReceiptShadow(key,false);
            var point=Where(key,new Vector2(.5f,.48f));if(!point.HasValue)return;
            float contact=element=="dark"?1.6f:element=="earth"?1.2f:element=="vine"?2f:4f;
            float bouncePower=element=="earth"?4f:element=="vine"?6f:element=="dark"?9f:12f;
            float power=visual?.Light??(heavy?1.2f:1f);var p=point.Value;
            _worldContacts.TryGetValue(key,out var reuse);
            _worldContacts[key]=MotionLight(p,Color.Lerp(color,Color.white,.15f),contact*power,5f,.24f,.18f,false,"element-contact",key,reuse);
            var bounce=p+Vector3.up*1.2f;var source=UnitPoint("player",new Vector2(.5f,.5f));
            if(source.HasValue)bounce=Vector3.Lerp(p,source.Value,.3f)+Vector3.up*1.2f;
            _worldBounce=MotionLight(bounce,color,bouncePower*power,11f,.30f,.18f,false,"element-bounce",key,_worldBounce);
        }

        // A reused large contact must relinquish its shadow for a later bounded small
        // receipt. Only the new policy calls this; legacy null callers keep their behavior.
        static void SyncReceiptShadow(string key,bool requested) {
            if(!Stage.CombatWorld || (requested && ShadowOk) || !_worldContacts.TryGetValue(key,out var sequence))return;
            foreach(var slot in _slots)if(slot.Lit && slot.Seq==sequence){Release(slot);break;}
        }
        static void WorldStrike(string key,bool heavy,Color effectColor,float? finalPower=null) {
            if(finalPower.HasValue){heavy=finalPower.Value>1f;SyncReceiptShadow(key,heavy);} // Do not retain raw/exposed heavy range on a small clamped receipt.
            var point=Where(key,new Vector2(.5f,.48f));if(!point.HasValue)return;
            float power=finalPower.HasValue?Mathf.Clamp(finalPower.Value,.50f,1.20f):heavy?1.65f:1f;var p=point.Value;
            _worldContacts.TryGetValue(key,out var reuse);
            _worldContacts[key]=MotionLight(p,Color.Lerp(effectColor,Color.white,.65f),18f*power,heavy?8.5f:7f,.32f,.3125f,heavy,"world-contact",key,reuse);
            var bounce=p+Vector3.up*1.2f;
            var source=UnitPoint("player",new Vector2(.5f,.5f));
            if(source.HasValue)bounce=Vector3.Lerp(p,source.Value,.3f)+Vector3.up*1.2f;
            _worldBounce=MotionLight(bounce,effectColor,28f*power,heavy?15f:13f,.34f,.2941176f,false,"world-bounce",key,_worldBounce);
        }

        // Three approved attack profiles use captured origins and their own color/decay.
        // The strong world-contact/bounce peaks, ranges, bounded pool and shadow rules remain.
        // No image frame, particle stop or local actor pause owns this clock.
        // Existing scaled delta is intentional: terminal FinishSlow stretches lights with the scene.
        public static void AuthoredAttackHit(string key,Vector3 feet,float height,string attack,bool heavy,string presentation=null,DamageVfxIntensity.Visual visual=null) {
            if(!Live)return;
            // Manifest colors are linear; Light.color accepts sRGB and URP converts back to linear.
            var spec=WorldBattleFx.AttackProfile(attack);if(spec==null || spec.ContractLighting)return;
            float power=visual?.Light??((heavy?1.65f:1f)*WorldBattleFx.AuthoredLightScale(presentation));
            if(visual!=null){heavy=visual.Light>1f;SyncReceiptShadow(key,heavy);}
            var p=feet+(Stage.LayoutRotation*Vector3.up)*(height*.48f);
            var toward=Stage.Camera!=null?Stage.Camera.transform.position-p:-(Stage.LayoutRotation*Vector3.forward);
            if(toward.sqrMagnitude>1e-6f)p+=toward.normalized*Tune.TowardCamera;
            _worldContacts.TryGetValue(key,out var reuse);
            _worldContacts[key]=MotionLight(p,spec.TargetColor.gamma,18f*power,heavy?8.5f:7f,spec.End,0f,heavy,"attack-v2-contact",key,reuse);
            SetAuthoredDecay(_worldContacts[key],spec.LightHold,spec.TargetTau,spec.LightReleasePhase);
            var bounce=p+Vector3.up*1.2f;
            var source=UnitPoint("player",new Vector2(.5f,.5f));
            if(source.HasValue)bounce=Vector3.Lerp(p,source.Value,.3f)+Vector3.up*1.2f;
            _worldBounce=MotionLight(bounce,spec.FloorColor.gamma,28f*power,heavy?15f:13f,spec.End,0f,false,"attack-v2-floor",key,_worldBounce);
            SetAuthoredDecay(_worldBounce,spec.LightHold,spec.FloorTau,spec.LightReleasePhase);
        }
        // New elemental families: one bounded contribution per valid renderer generation.
        // Images and actor pauses never drive these clocks; actual Land owns age zero.
        public sealed class ContractLightPlayback {
            internal string Key; internal object Identity; internal int Epoch; internal long Serial;
            internal Vector3 Impact,Feet; internal float Height,Start,Power; internal bool Active=true,Landed;
            internal WorldBattleFx.AttackSpec Spec; internal Func<bool> Valid; internal Func<float> AgeProvider;
            internal Material ReceiverMaterial;internal float ReceiverScale=1f;internal Func<bool> ReceiverValid;
            internal float Age=>AgeProvider!=null?AgeProvider():Landed?Time.time-Start:Mathf.Min(-.000001f,Time.time-Start);
            public bool Land() {
                if(!Active || (Valid!=null && !Valid()))return false;
                if(!Landed){Landed=true;Start=Time.time;RefreshContractLights(true);}return true;
            }
            public void Cancel(){if(!Active)return;Active=false;RefreshContractLights(false);}
        }
        sealed class ContractLightGroup {
            public string Key,MaxCombineGroup; public object Identity; public int Epoch; public long Serial;
            public Slot Target,Floor; public bool Selected;
            public readonly List<ContractLightPlayback> Contributions=new List<ContractLightPlayback>();
        }
        static readonly HashSet<Material> OwnedReceiverMaterials=new HashSet<Material>();
        static readonly List<ContractLightPlayback> ContractContributions=new List<ContractLightPlayback>();
        static readonly List<ContractLightGroup> ContractGroups=new List<ContractLightGroup>();
        static long _contractSerial;static int _contractRecordEvictions,_contractGroupSuppressions;
        static float _contractTargetSum,_contractFloorSum;
        // Opt-in target-only candidates may decline light rather than replacing
        // any older light/group. Read-only admission; existing callers are unchanged.
        internal static bool CanAdmitContractTargetWithoutEviction(string key,object identity,int epoch,string maxCombineGroup) {
            if(!Live || identity==null || ContractContributions.Count>=24)return false;
            int lights=Math.Min(_want,_slots.Count),budget=Math.Max(1,lights/2);
            if(ContractGroups.Count>budget)return false;
            // Begin/Land refresh every group. Decline if any existing endpoint would
            // have to be reacquired, which could otherwise evict a legacy light.
            foreach(var group in ContractGroups) {
                if(!group.Selected)return false;
                bool target=false,floor=false;
                foreach(var c in group.Contributions){target|=c.Spec.TargetPeakMultiplier>0f;floor|=c.Spec.FloorPeakMultiplier>0f;}
                if(target && (group.Target==null || !group.Target.Lit || group.Target.Light==null || !ReferenceEquals(group.Target.ContractOwner,group)))return false;
                if(floor && (group.Floor==null || !group.Floor.Lit || group.Floor.Light==null || !ReferenceEquals(group.Floor.ContractOwner,group)))return false;
            }
            foreach(var group in ContractGroups)
                if(group.Key==key && group.Epoch==epoch && group.MaxCombineGroup==maxCombineGroup && ReferenceEquals(group.Identity,identity))
                    return group.Target!=null && group.Target.Lit && group.Target.Light!=null && ReferenceEquals(group.Target.ContractOwner,group);
            if(ContractGroups.Count>=budget)return false;
            for(int i=0;i<lights;i++)if(_slots[i].Light!=null && !_slots[i].Lit)return true;
            return false;
        }
        internal static ContractLightPlayback BeginContractAttackLight(string key,object identity,int epoch,Vector3 impact,Vector3 feet,float height,WorldBattleFx.AttackSpec spec,float age,bool heavy,Func<bool> valid,float presentationLightScale=1f,float? finalPower=null,Func<float> ageProvider=null) {
            if(!Live || !spec.ContractLighting)return null;
            EnsureDriver();
            ContractContributions.RemoveAll(c=>!c.Active);
            if(ContractContributions.Count>=24){ContractContributions[0].Active=false;ContractContributions.RemoveAt(0);_contractRecordEvictions++;}
            var item=new ContractLightPlayback {Key=key,Identity=identity??new object(),Epoch=epoch,Impact=impact,Feet=feet,Height=height,Spec=spec,Start=Time.time-age,Power=finalPower.HasValue?Mathf.Clamp(finalPower.Value,.50f,1.20f):(heavy && !spec.StandardContractPower?1.65f:1f)*Mathf.Clamp(presentationLightScale,0f,1f),Valid=valid,AgeProvider=ageProvider,Serial=++_contractSerial};
            ContractContributions.Add(item);RefreshContractLights(true);return item;
        }
        internal static float ContractEnvelope(WorldBattleFx.AttackSpec spec,float age,bool floor) {
            if((floor?spec.FloorPeakMultiplier:spec.TargetPeakMultiplier)<=0f)return 0f; // Disabled endpoint neither dims others nor consumes its cap.
            if(age<spec.Boundaries[0] || age>=spec.End)return 0f;
            if(spec.Envelope!=null)return Mathf.Clamp01(spec.Envelope(age));
            if(age<0f) {
                float q=Mathf.Clamp01((age-spec.Boundaries[0])/-spec.Boundaries[0]);
                return (floor?spec.FloorPreGain:spec.TargetPreGain)*Mathf.Pow(q,floor?spec.FloorPreExponent:spec.TargetPreExponent);
            }
            return AuthoredLightEnvelope(age,spec.LightHold,floor?spec.FloorTau:spec.TargetTau,spec.End);
        }
        internal static float ContractCapScale(float rawSum)=>rawSum>1.12f?1.12f/rawSum:1f;
        static void RetireContractSlot(Slot slot,ContractLightGroup owner) {
            if(slot==null || !ReferenceEquals(slot.ContractOwner,owner))return;
            slot.ContractOwner=null;slot.Lit=false;
            if(slot.Light!=null){slot.Light.enabled=false;slot.Light.intensity=0f;}
            Release(slot);Unmark(slot);
        }
        static void ClearContractLights() {
            foreach(var c in ContractContributions)c.Active=false;
            foreach(var g in ContractGroups){RetireContractSlot(g.Target,g);RetireContractSlot(g.Floor,g);}
            ContractContributions.Clear();ContractGroups.Clear();_contractTargetSum=_contractFloorSum=0f;RefreshOwnedReceivers();
        }
        static Slot PickContractSlot(bool allowEviction) {
            Slot best=null;long oldest=long.MaxValue;
            for(int i=0;i<Math.Min(_want,_slots.Count);i++) {
                var slot=_slots[i];if(slot.Light==null)continue;
                if(!slot.Lit)return slot;
                // Never steal another selected new-profile endpoint. Legacy ownership can
                // be replaced only at Begin/Land/selection, not in an endless LateUpdate fight.
                if(allowEviction && slot.ContractOwner==null && slot.Seq<oldest){oldest=slot.Seq;best=slot;}
            }
            return best;
        }
        static Slot UpdateContractEndpoint(ContractLightGroup group,Slot slot,bool floor,float capScale,bool allowEviction) {
            bool enabled=false;foreach(var c in group.Contributions)if((floor?c.Spec.FloorPeakMultiplier:c.Spec.TargetPeakMultiplier)>0f){enabled=true;break;}
            if(!enabled){RetireContractSlot(slot,group);return null;}
            float weight=0f;Vector3 rgb=Vector3.zero;ContractLightPlayback latest=null;
            foreach(var c in group.Contributions) {
                float raw=ContractEnvelope(c.Spec,c.Age,floor),w=raw*c.Power*(floor?c.Spec.FloorPeakMultiplier:c.Spec.TargetPeakMultiplier);
                var color=floor?c.Spec.FloorColor:c.Spec.TargetColor;
                if(group.MaxCombineGroup!=null){if(w>weight){rgb=new Vector3(color.r,color.g,color.b)*w;weight=w;}}
                else {rgb+=new Vector3(color.r,color.g,color.b)*w;weight+=w;}
                if(latest==null || c.Serial>latest.Serial)latest=c;
            }
            if(latest==null)return slot;
            Vector3 targetPoint=latest.Spec.TargetLightHeightFraction>=0f
                ?latest.Impact+(Stage.LayoutRotation*Vector3.up)*(latest.Height*(latest.Spec.TargetLightHeightFraction-.5f)):latest.Impact;
            Vector3 position=floor?latest.Feet+Vector3.up*(latest.Height*.20f):targetPoint;
            float peak=(floor?28f:18f)*weight*capScale;
            Color linear=weight>0f?new Color(rgb.x/weight,rgb.y/weight,rgb.z/weight):Color.black;
            if(slot==null || !slot.Lit || !ReferenceEquals(slot.ContractOwner,group) || slot.Light==null) {
                slot=PickContractSlot(allowEviction);if(slot==null){_missed++;return null;}
                // LightAt's temporary full peak is overwritten synchronously below, before rendering.
                LightAt(position,linear.gamma,Mathf.Max(.00001f,peak),latest.Spec.End,0f,false,floor?"contract-attack-floor":"contract-attack-target",latest.Key,floor?13f:7f,true,slot);
                slot.ContractOwner=group;
            }
            var h=StageLook.Current!=null?StageLook.Current.Hit:Defaults;
            slot.Light.transform.position=position;slot.Light.color=linear.gamma;
            slot.Peak=peak*h.RefDist*h.RefDist;slot.Light.intensity=slot.Peak;slot.Light.enabled=peak>0f;
            slot.T=latest.Age;slot.Dur=latest.Spec.End;
            return slot;
        }
        static void RefreshContractLights(bool allowEviction) {
            if(!Live){ClearContractLights();return;}
            for(int i=ContractContributions.Count-1;i>=0;i--) {
                var c=ContractContributions[i];
                if(!c.Active || (c.Valid!=null && !c.Valid()) || (c.Landed && c.Age>=c.Spec.End)) {c.Active=false;ContractContributions.RemoveAt(i);}
            }
            foreach(var g in ContractGroups){g.Contributions.Clear();g.Serial=0;}
            _contractTargetSum=_contractFloorSum=0f;
            foreach(var c in ContractContributions) {
                ContractLightGroup group=null;
                foreach(var g in ContractGroups)if(g.Key==c.Key && g.Epoch==c.Epoch && g.MaxCombineGroup==c.Spec.MaxCombineGroup && ReferenceEquals(g.Identity,c.Identity)){group=g;break;}
                if(group==null){group=new ContractLightGroup {Key=c.Key,Identity=c.Identity,Epoch=c.Epoch,MaxCombineGroup=c.Spec.MaxCombineGroup};ContractGroups.Add(group);}
                group.Contributions.Add(c);group.Serial=Math.Max(group.Serial,c.Serial);

            }
            foreach(var group in ContractGroups) {
                float target=0f,floor=0f;
                foreach(var c in group.Contributions){float t=ContractEnvelope(c.Spec,c.Age,false),f=ContractEnvelope(c.Spec,c.Age,true);
                    if(group.MaxCombineGroup!=null){target=Mathf.Max(target,t);floor=Mathf.Max(floor,f);}else {target+=t;floor+=f;}}
                _contractTargetSum+=target;_contractFloorSum+=floor;
            }
            for(int i=ContractGroups.Count-1;i>=0;i--)if(ContractGroups[i].Contributions.Count==0) {
                var g=ContractGroups[i];RetireContractSlot(g.Target,g);RetireContractSlot(g.Floor,g);ContractGroups.RemoveAt(i);
            }
            ContractGroups.Sort((a,b)=>b.Serial.CompareTo(a.Serial));
            int lights=Math.Min(_want,_slots.Count),budget=Math.Max(1,lights/2);
            for(int i=budget;i<ContractGroups.Count;i++) {
                var g=ContractGroups[i];if(g.Selected)_contractGroupSuppressions++;
                RetireContractSlot(g.Target,g);RetireContractSlot(g.Floor,g);g.Target=g.Floor=null;g.Selected=false;
            }
            for(int i=0;i<Math.Min(budget,ContractGroups.Count);i++) {
                var g=ContractGroups[i];bool admit=allowEviction || !g.Selected;g.Selected=true;
                g.Target=UpdateContractEndpoint(g,g.Target,false,ContractCapScale(_contractTargetSum),admit);
                if(lights>1)g.Floor=UpdateContractEndpoint(g,g.Floor,true,ContractCapScale(_contractFloorSum),admit);
            }
    RefreshOwnedReceivers();
        }
        // Per-material receiver projection; actual world Light properties are untouched.
        static void RefreshOwnedReceivers(){
            bool owned=false;foreach(var c in ContractContributions)if(c.ReceiverMaterial!=null){owned=true;break;}
            if(!owned&&OwnedReceiverMaterials.Count==0)return;
            foreach(var m in OwnedReceiverMaterials)if(m!=null)m.SetFloat("_OwnedHealReceiveCount",0);OwnedReceiverMaterials.Clear();
            var entries=new Dictionary<Material,List<Vector4>>();
            foreach(var group in ContractGroups)if(group.Selected)foreach(bool floor in new[]{false,true}){
                var slot=floor?group.Floor:group.Target;if(slot?.Light==null||!slot.Light.isActiveAndEnabled)continue;
                ContractLightPlayback winner=null;float weight=0;
                // Selected heal's existing max-combine winner. Mixed additive groups fail closed.
                if(group.MaxCombineGroup!="heal-uplift-continuous-r1")continue;
                foreach(var c in group.Contributions){float w=ContractEnvelope(c.Spec,c.Age,floor)*c.Power*(floor?c.Spec.FloorPeakMultiplier:c.Spec.TargetPeakMultiplier);if(w>weight){winner=c;weight=w;}}
                if(winner?.ReceiverMaterial==null||winner.ReceiverValid?.Invoke()!=true)continue;
                var pos=slot.Light.transform.position;bool collision=false;
                foreach(var other in _slots)if(other.Light!=null&&other.Light!=slot.Light&&other.Light.isActiveAndEnabled&&(other.Light.transform.position-pos).sqrMagnitude<1e-4f){collision=true;break;}
                if(collision)continue;
                var m=winner.ReceiverMaterial;if(!entries.TryGetValue(m,out var list)){list=new List<Vector4>();entries.Add(m,list);}list.Add(new Vector4(pos.x,pos.y,pos.z,Mathf.Clamp01(winner.ReceiverScale)));
            }
            foreach(var entry in entries){if(entry.Value.Count>2)continue;var values=new Vector4[2];int n=entry.Value.Count;for(int i=0;i<n;i++)values[i]=entry.Value[i];entry.Key.SetVectorArray("_OwnedHealReceiveLights",values);entry.Key.SetFloat("_OwnedHealReceiveCount",n);OwnedReceiverMaterials.Add(entry.Key);}
        }

        static void SetAuthoredDecay(long sequence,float hold,float tau,float releasePhase) {
            if(sequence<=0)return;
            foreach(var slot in _slots)if(slot.Lit && slot.Seq==sequence){slot.AuthoredHold=hold;slot.DecayTau=tau;slot.ReleasePhase=releasePhase;slot.AuthoredStarted=Time.time;break;}
        }
        internal static float AuthoredLightEnvelope(float elapsed,float hold,float tau,float end,float releasePhase=0f)=>
            elapsed<0f || elapsed>=end?0f:elapsed<hold?1f:Mathf.Exp(-(elapsed-hold+releasePhase)/Mathf.Max(.0001f,tau));

        // ================================================================ 段2 の口 (2026-10-03 レーン M。呼ぶのは StageMotion だけ = 門 StageMotion.On が真の時だけ)

        /// <summary>
        /// 寄りの技の光 (約束 §C5「技の光」): world に色 color・強さ intensity (refDist の距離での明るさ)・届く距離 range の点光源を灯し、dur 秒で消す
        /// (hold = dur のこの割合までは弱めない)。技の光の印 (StageHitReceive.Mark) を付ける = 「技の光にだけ強く受ける」材質 (_HitReceive) が強く受ける。
        /// reuse = 前にこの口が返した番号。その光がまだ灯っていれば、新しい光を取らずにその光を灯し直す (場所・色・強さ・距離を新しくし、経過を 0 へ)。
        /// shadow はスマホの段では付けない (ShadowOk)。返り値 = 灯した光の番号 (灯さなかったら 0)
        /// </summary>
        internal static long MotionLight(Vector3 world, Color color, float intensity, float range, float dur, float hold, bool shadow, string kind, string key, long reuse)
        {
            if (!Live) return 0;
            if (!(intensity > 0f) || !(dur > 0f) || float.IsInfinity(intensity) || float.IsInfinity(dur)) return 0;
            if (float.IsNaN(world.x) || float.IsNaN(world.y) || float.IsNaN(world.z)) return 0;
            if (reuse > 0)
            {
                int n = Math.Min(_want, _slots.Count);
                for (int i = 0; i < n; i++)
                {
                    var s = _slots[i];
                    if (!s.Lit || s.Light == null || s.Seq != reuse) continue;
                    var h = StageLook.Current != null ? StageLook.Current.Hit : Defaults;
                    var l = s.Light;
                    StageHitReceive.Mark(l, true); s.Marked = true;   // 毎回付け直す (Prepare が光の設定を書き直しても印が残るように)
                    l.transform.position = world;
                    l.color = new Color(color.r, color.g, color.b, 1f);
                    l.range = Mathf.Max(0.1f, range > 0f ? range : h.Range);
                    s.Peak = intensity * h.RefDist * h.RefDist;
                    s.T = 0f;
                    s.Dur = dur;
                    s.Hold = Mathf.Clamp(hold, 0f, 0.9f);
                    s.AuthoredHold=0f;s.DecayTau=0f;s.ReleasePhase=0f;s.AuthoredStarted=0f;s.ContractOwner=null;
                    s.Kind = kind;
                    s.Key = key;
                    l.intensity = s.Peak;
                    if (shadow && ShadowOk && !s.Borrowed && StageLook.TryBorrowShadowSlot()) { s.Borrowed = true; l.shadows = LightShadows.Hard; }
                    _calls++;
                    Remember(s, world, intensity, false);
                    if (HD2DFlags.Det || HD2DFlags.DumpLayout)
                        Debug.Log("[StageFx] 寄りの光を灯し直した " + kind + (key != null ? " " + key : "") + " 強さ " + F(intensity) + " 距離 " + F(l.range) + " " + F(dur) + "秒 frame " + Time.frameCount);
                    OwnPresentationLight(s);
                    return s.Seq;
                }
            }
            return LightAt(world, color, intensity, dur, hold, shadow && ShadowOk, kind, key, range, true);
        }

        /// <summary>
        /// 敵の大技の赤い合図 (約束 §C5「赤の合図 0.17」・分析書 §8 の guardian 22.48s「敵の絵が赤く染まり、足元が光る」): world に赤い点光源を dur 秒。
        /// 影なし・印なし (技の光ではない)。届く距離 range。返り値 = 灯した光の番号 (敵の大技を光で出す時、暖の段がこの光を MotionLight の reuse で灯し直す = 光の数を増やさない。灯さなかったら 0)
        /// </summary>
        internal static long MotionCue(Vector3 world, Color color, float intensity, float range, float dur, string key)
        {
            if (!Live) return 0;
            return LightAt(world, color, intensity, dur, 0.6f, false, "cue", key, range, false);
        }

        /// <summary>
        /// 動きを止める時 (StageMotion.StopAll: 門が閉じた・戦闘の画面が無くなった): 寄りの光 (印の付いた光) を消して印を外し、借りていた影を返す。
        /// 普通の当たりの光はそのまま減衰させる (今の口の通り方を変えない)。印の付いた光が無ければ何もしない
        /// </summary>
        internal static void MotionStop()
        {
            ClearContractLights();
            foreach (var s in _slots)
            {
                if (!s.Marked) continue;
                if (s.Light != null) { s.Light.enabled = false; s.Light.intensity = 0f; s.Light.shadows = LightShadows.None; }
                s.Lit = false;
                Release(s);
                Unmark(s);
            }
        }

        /// <summary>影を落としてよい段 (スマホの段では技の光に影を付けない。計画 P13「影あり (PC だけ)」)</summary>
        static bool ShadowOk => HD2DFlags.Tier != HD2DTier.Phone;

        /// <summary>板 key の点 v に灯す (delay 秒後。場所は灯す時の板の位置で決める = 踏み込みの途中でも当たった所に灯る)</summary>
        static void At(string key, Vector2 v, Color color, float intensity, float dur, float hold, bool shadow, string kind, float delay)
        {
            if (delay > 0f)
            {
                // Preserve descendant ownership for a delayed presentation light;
                // unrelated diagnostic lights retain their existing driver queue.
                if(Tween.PresentationActive){Tween.After(delay,()=>At(key,v,color,intensity,dur,hold,shadow,kind,0f));return;}
                _pending.Add(new Pending { Left = delay, Key = key, V = v, Color = color, Intensity = intensity, Dur = dur, Hold = hold, Shadow = shadow, Kind = kind });
                EnsureDriver();
                return;
            }
            Vector3? p = Where(key, v);
            if (!p.HasValue) { _missed++; return; }
            if(Stage.CombatWorld) {
                _worldContacts.TryGetValue(key,out var reuse);
                _worldContacts[key]=MotionLight(p.Value,color,intensity,-1f,dur,hold,shadow,kind,key,reuse);
            } else LightAt(p.Value, color, intensity, dur, hold, shadow, kind, key);
        }

        /// <summary>
        /// 光を置く世界の点: 板の上の点 (UnitPoint) を、カメラの方へ towardCamera (既定 0.6 unit) 寄せる
        /// (板の面の上に置くと、その板の影が潰れて面の手前の地面も照らしにくい。本家のブーストの灯と同じく「体の前」で光る)。
        /// 板が無ければ座席 (Stage.TryGetSeat) から、既定の高さ fallbackHeight の板とみなして出す。どちらも無ければ null (灯さない)
        /// </summary>
        static Vector3? Where(string key, Vector2 v)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var t = Tune;
            Vector3? p = UnitPoint(key, v);
            if (!p.HasValue)
            {
                Vector3 seat; float k;
                if (!Stage.TryGetSeat(key, out seat, out k)) return null;
                var rot = Stage.LayoutRotation;
                p = seat + (rot * Vector3.up) * (v.y * t.FallbackHeight) + (rot * Vector3.right) * ((v.x - 0.5f) * t.FallbackHeight);
            }
            var q = p.Value;
            if (t.TowardCamera > 0f)
            {
                var cam = Stage.Camera;
                Vector3 to = cam != null ? cam.transform.position - q : -(Stage.LayoutRotation * Vector3.forward);
                if (to.sqrMagnitude > 1e-6f) q += to.normalized * t.TowardCamera;
            }
            return q;
        }

        /// <summary>
        /// キャラの板 (key) の上の点を世界で返す。v は板の中の位置 (x 0=左・1=右、y 0=足元・1=頭)。
        /// Stage.TryGetUnitBox (足元の中心と板の高さ) を読む。板の幅は高さと同じとみなす (幅が要る時は aspect つきの方を使う)。
        /// 板はレイアウト用のカメラの回転 (Stage.LayoutRotation) を向く。板が無ければ null
        /// </summary>
        public static Vector3? UnitPoint(string key, Vector2 v) => UnitPoint(key, v, 1f);

        /// <summary>UnitPoint の幅つき: 板の幅 = 高さ × aspect (絵の幅 ÷ 高さ)</summary>
        public static Vector3? UnitPoint(string key, Vector2 v, float aspect)
        {
            Vector3 feet; float h;
            if (string.IsNullOrEmpty(key) || !Stage.TryGetUnitBox(key, out feet, out h) || !(h > 0f)) return null;
            var rot = Stage.LayoutRotation;
            Vector3 up = rot * Vector3.up, right = rot * Vector3.right;
            return feet + up * (v.y * h) + right * ((v.x - 0.5f) * h * Mathf.Max(0.01f, aspect));
        }

        /// <summary>いま灯っている技の光の数</summary>
        public static int LitCount
        {
            get
            {
                int n = 0;
                foreach (var s in _slots) if (s.Lit && s.Light != null) n++;
                return n;
            }
        }

        // ---------------------------------------------------------------- 中身

        sealed class Slot
        {
            public Light Light;
            public float Peak, T, Dur, Hold, AuthoredHold, DecayTau, ReleasePhase, AuthoredStarted;
            public bool Lit, Borrowed, PresentationOwned; public int PresentationVersion;
            public object ContractOwner;
            public bool Marked;       // 段2: 技の光の印 (StageHitReceive.Mark) を付けている (寄りの光に使った)
            public long Seq;          // 灯した順 (いちばん古い光を使い回すため)
            public string Kind, Key;  // 記録用 (dumplayout の extra.hitLight・ログ)
        }

        /// <summary>遅れて灯す光 (蔦の鞭の先が届くまで)。灯す時に板の位置を読む</summary>
        sealed class Pending
        {
            public float Left, Intensity, Dur, Hold;
            public string Key, Kind;
            public Vector2 V;
            public Color Color;
            public bool Shadow;
        }

        static readonly List<Slot> _slots = new List<Slot>();
        static readonly List<Pending> _pending = new List<Pending>();
        static readonly StageLookData.HitLook Defaults = new StageLookData.HitLook();
        static GameObject _root;
        static int _want = 3;
        // 記録 (Apply ごとに 0 へ戻す): 灯した回数・同時に灯った最大・使い回した回数・板も座席も無くて灯せなかった回数
        static long _seq;
        static int _calls, _maxLit, _evicted, _missed;
        static readonly List<Dictionary<string, object>> _recent = new List<Dictionary<string, object>>();
        const int RecentMax = 16;

        /// <summary>
        /// 口の強さ・長さ・色。設計図 look_act1.json の hitLight に任意のキー (intensity・bigMul・dur・hold・guardDur・guardMul・castDur・castMul・
        /// foeMul・finishDur・finishMul・height・towardCamera・vineDelay・fallbackHeight・colors{slash,fang,horn,vine,stomp,spell,light,spark,foe,guard,finish}=[r,g,b])
        /// を書けば上書きする。無ければ下の既定 (計画 P13 の値)。設計図が変わった時 (Apply) だけ読み直す
        /// </summary>
        sealed class HitTune
        {
            // Dur 0.26 = 0.3秒以内に消える (計画 P13 の確かめ方「0.3秒で戻る」)。Hold 0.3 = 最初の約4.7フレームは灯した明るさのまま (「2〜6フレーム後に +20」)
            public float Intensity = 1f, BigMul = 1.6f, Dur = 0.26f, Hold = 0.3f;
            public float GuardDur = 0.18f, GuardMul = 0.7f, CastDur = 0.3f, CastMul = 0.8f, FoeMul = 1f;
            public float FinishDur = 0.3f, FinishMul = 2f;
            public float Height = 0.45f, TowardCamera = 0.6f, VineDelay = 0.13f, FallbackHeight = 2.4f;
            // 色 (光の色なので明るさ1に寄せた値。UI の役割の色ではない = check:colors の対象外の Stage*.cs に置く)
            public Color Physical = new Color(1f, 0.93f, 0.8f);    // 斬撃・牙・角・蔦・踏みつけ = 暖かい白
            public Color Spell = new Color(0.4f, 1f, 0.9f);        // 呪文 = 脈の青緑
            public Color Lamp = new Color(1f, 0.8f, 0.5f);         // 灯 = 暖色
            public Color Spark = new Color(1f, 0.55f, 0.22f);      // 火種 = 橙
            public Color Foe = new Color(1f, 0.42f, 0.3f);         // 敵の当たり = 朱
            public Color GuardColor = new Color(0.6f, 0.8f, 1f);   // 完全に防いだ = 空色
            public Color FinishColor = new Color(1f, 1f, 1f);      // とどめ = 白
            public readonly Dictionary<string, Color> Styles = new Dictionary<string, Color>();

            public Color ColorFor(string style)
            {
                Color c;
                if (!string.IsNullOrEmpty(style) && Styles.TryGetValue(style, out c)) return c;
                switch (style)
                {
                    case "spell": return Spell;
                    case "light": return Lamp;
                    case "spark": return Spark;
                    default: return Physical;
                }
            }
        }

        static HitTune _tune;
        static StageLookData _tuneOf;

        static HitTune Tune
        {
            get
            {
                var cur = StageLook.Current;
                if (_tune != null && ReferenceEquals(_tuneOf, cur)) return _tune;
                _tuneOf = cur;
                _tune = ReadTune(cur);
                return _tune;
            }
        }

        static HitTune ReadTune(StageLookData d)
        {
            var t = new HitTune();
            JObject o = null;
            try { o = d != null && d.Raw != null ? d.Raw["hitLight"] as JObject : null; } catch (Exception) { o = null; }
            if (o == null) return t;
            try
            {
                t.Intensity = Num(o, "intensity", t.Intensity);
                t.BigMul = Num(o, "bigMul", t.BigMul);
                t.Dur = Num(o, "dur", t.Dur);
                t.Hold = Num(o, "hold", t.Hold);
                t.GuardDur = Num(o, "guardDur", t.GuardDur);
                t.GuardMul = Num(o, "guardMul", t.GuardMul);
                t.CastDur = Num(o, "castDur", t.CastDur);
                t.CastMul = Num(o, "castMul", t.CastMul);
                t.FoeMul = Num(o, "foeMul", t.FoeMul);
                t.FinishDur = Num(o, "finishDur", t.FinishDur);
                t.FinishMul = Num(o, "finishMul", t.FinishMul);
                t.Height = Num(o, "height", t.Height);
                t.TowardCamera = Num(o, "towardCamera", t.TowardCamera);
                t.VineDelay = Num(o, "vineDelay", t.VineDelay);
                t.FallbackHeight = Num(o, "fallbackHeight", t.FallbackHeight);
                var cs = o["colors"] as JObject;
                if (cs != null)
                    foreach (var p in cs.Properties())
                    {
                        Color c;
                        if (!Col(p.Value, out c)) continue;
                        switch (p.Name)
                        {
                            case "foe": t.Foe = c; break;
                            case "guard": t.GuardColor = c; break;
                            case "finish": t.FinishColor = c; break;
                            default: t.Styles[p.Name] = c; break;
                        }
                    }
            }
            catch (Exception e) { Debug.LogWarning("[StageFx] 設計図の hitLight を読めない (既定で灯す): " + e.Message); }
            return t;
        }

        static float Num(JObject o, string name, float def)
        {
            var v = o[name];
            if (v == null || (v.Type != JTokenType.Float && v.Type != JTokenType.Integer)) return def;
            float f = v.Value<float>();
            return float.IsNaN(f) || float.IsInfinity(f) || f < 0f ? def : f;
        }

        static bool Col(JToken v, out Color c)
        {
            c = default;
            var a = v as JArray;
            if (a == null || a.Count < 3) return false;
            for (int i = 0; i < 3; i++) if (a[i].Type != JTokenType.Float && a[i].Type != JTokenType.Integer) return false;
            c = new Color(a[0].Value<float>(), a[1].Value<float>(), a[2].Value<float>(), 1f);
            return true;
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>灯した記録を残す (新しい順に RecentMax 件)</summary>
        static void Remember(Slot s, Vector3 world, float intensity, bool evicted)
        {
            var r = new Dictionary<string, object>
            {
                { "frame", Time.frameCount }, { "time", Time.time }, { "kind", s.Kind }, { "key", s.Key }, { "pos", world },
                { "color", s.Light != null ? s.Light.color : Color.white }, { "intensity", intensity }, { "dur", s.Dur }, { "hold", s.Hold },
                { "shadow", s.Borrowed }, { "evicted", evicted }, { "litAfter", LitCount },
            };
            _recent.Insert(0, r);
            if (_recent.Count > RecentMax) _recent.RemoveRange(RecentMax, _recent.Count - RecentMax);
        }

        /// <summary>
        /// dumplayout (layout.json の extra.hitLight) の記録: 今灯っている光 (種類・板・位置・明るさ・経過)・同時に灯った最大 (≦3 を確かめる)・
        /// 灯した回数・使い回した回数・灯せなかった回数・遅れて灯す待ち・最近の記録。Prepare が登録し、StopAll (Apply の頭と Restore) が外す
        /// </summary>
        public static object DebugInfo()
        {
            var o = new Dictionary<string, object>();
            o["live"] = Live;
            o["cap"] = Math.Min(_want, _slots.Count);
            o["lit"] = LitCount;
            o["maxLit"] = _maxLit;
            o["calls"] = _calls;
            o["evicted"] = _evicted;
            o["missed"] = _missed;
            o["pending"] = _pending.Count;
            o["contractContributions"]=ContractContributions.Count;o["contractGroups"]=ContractGroups.Count;
            o["contractRecordEvictions"]=_contractRecordEvictions;o["contractGroupSuppressions"]=_contractGroupSuppressions;
            o["contractTargetRawSum"]=_contractTargetSum;o["contractFloorRawSum"]=_contractFloorSum;
            var slots = new List<object>();
            foreach (var s in _slots)
            {
                if (s.Light == null) continue;
                slots.Add(new Dictionary<string, object>
                {
                    { "lit", s.Lit }, { "kind", s.Kind }, { "key", s.Key }, { "pos", s.Light.transform.position }, { "color", s.Light.color },
                    { "intensity", s.Light.intensity }, { "peak", s.Peak }, { "t", s.T }, { "dur", s.Dur }, { "shadow", s.Borrowed }, { "authoredHold", s.AuthoredHold }, { "decayTau", s.DecayTau }, { "releasePhase", s.ReleasePhase },
                });
            }
            o["slots"] = slots;
            o["recent"] = new List<Dictionary<string, object>>(_recent);
            return o;
        }

        internal static object CaptureLightReview() {
            var slots=new List<object>();foreach(var s in _slots)if(s.Light!=null && s.Lit)slots.Add(new{kind=s.Kind,key=s.Key,enabled=s.Light.enabled,intensity=s.Light.intensity,color=new[]{s.Light.color.r,s.Light.color.g,s.Light.color.b},position=new[]{s.Light.transform.position.x,s.Light.transform.position.y,s.Light.transform.position.z},peak=s.Peak,age=s.T,duration=s.Dur});
            var contacts=new List<object>();foreach(var c in ContractContributions)contacts.Add(new{serial=c.Serial,key=c.Key,active=c.Active,landed=c.Landed,age=c.Age,power=c.Power,targetPeak=c.Spec.TargetPeakMultiplier,floorPeak=c.Spec.FloorPeakMultiplier});
            var receivers=new List<object>();foreach(var m in OwnedReceiverMaterials)if(m!=null){var a=m.GetVectorArray("_OwnedHealReceiveLights");receivers.Add(new{material=m.GetEntityId().ToString(),count=m.GetFloat("_OwnedHealReceiveCount"),lights=Array.ConvertAll(a,v=>new[]{v.x,v.y,v.z,v.w}),flash=m.GetFloat("_Flash"),whiteCap=m.GetFloat("_WhiteCap")});}
            var actorCounts=new List<object>();foreach(var key in new[]{"player","enemy0"}){var c=Stage.CaptureImpact(key)?.Target as Component;var m=c?.GetComponent<MeshRenderer>()?.sharedMaterial;if(m!=null)actorCounts.Add(new{key,material=m.GetEntityId().ToString(),count=m.GetFloat("_OwnedHealReceiveCount")});}
            return new{targetRawSum=_contractTargetSum,floorRawSum=_contractFloorSum,slots,contacts,receivers,actorCounts};
        }
        static void EnsureDriver()
        {
            if (_root == null) Prepare(StageLook.Current != null ? StageLook.Current.Hit : Defaults);
        }

        /// <summary>プールを用意する (無ければ作る・足りなければ足す)。光は消えたまま。StageLook.Apply が呼ぶ</summary>
        internal static void Prepare(StageLookData.HitLook h)
        {
            if (h == null) h = Defaults;
            _want = Mathf.Clamp(h.MaxLights, 1, 8);
            if (_root == null)   // 初めて・場面ごと消えた (Unity の null)
            {
                _slots.Clear();
                _root = new GameObject("HD2D-HitLights");
                _root.layer = HD2DLayers.StageFx;
                _root.AddComponent<HitLightDriver>();
            }
            while (_slots.Count < _want)
            {
                var go = new GameObject("HD2D-HitLight" + _slots.Count);
                go.transform.SetParent(_root.transform, false);
                go.layer = HD2DLayers.StageFx;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.enabled = false;
                l.intensity = 0f;
                l.shadows = LightShadows.None;
                l.shadowStrength = 0.8f;
                l.shadowBias = 0.05f;
                l.shadowNormalBias = 0.4f;
                _slots.Add(new Slot { Light = l });
            }
            foreach (var s in _slots)
            {
                if (s.Light == null) continue;
                try
                {
                    var ad = s.Light.GetUniversalAdditionalLightData();
                    ad.usePipelineSettings = false;
                    ad.customShadowLayers = true;
                    ad.renderingLayers = 0xFFFFFFFFu;        // 地面もキャラも照らす
                    ad.shadowRenderingLayers = 0xFFFFFFFFu;  // 大きい当たりはキャラと大物の影を地面に落とす (本家のブーストの灯)
                    if (Application.isPlaying) ad.additionalLightsShadowResolutionTier = h.ShadowTier;
                    if (s.Marked) StageHitReceive.Mark(s.Light, true);   // 段2: 寄りの光の印を、上で書き直した設定の上に付け直す (印を付けたことが無ければ呼ばない)
                }
                catch (Exception e) { Debug.LogWarning("[StageFx] 技の光の URP の設定に失敗: " + e.Message); }
            }
            HD2DFlags.LayoutDumpers["hitLight"] = DebugInfo;   // dumplayout の extra.hitLight (P13)
        }

        /// <summary>全部消して、借りていた影を返す (StageLook の Apply の頭と Restore が呼ぶ)。遅れて灯す待ちも捨て、記録を 0 へ戻す</summary>
        internal static void StopAll()
        {
            ClearContractLights();
            if (_calls > 0 || _missed > 0)
                Debug.Log("[StageFx] 技の光の記録: 灯した " + _calls + " 回・同時の最大 " + _maxLit + "・使い回し " + _evicted + " 回・灯せなかった " + _missed + " 回");
            foreach (var s in _slots)
            {
                if (s.Light != null) { s.Light.enabled = false; s.Light.intensity = 0f; s.Light.shadows = LightShadows.None; }
                s.Lit = false;
                Release(s);
                Unmark(s);   // 段2: 印も外す (印を付けたことが無ければ何もしない)
            }
            _pending.Clear();
            _calls = 0; _maxLit = 0; _evicted = 0; _missed = 0;
            _recent.Clear();
            _tune = null; _tuneOf = null;
            HD2DFlags.LayoutDumpers.Remove("hitLight");
        }

        /// <summary>プールの光 (dumplayout の記録用)</summary>
        internal static IEnumerable<Light> PoolLights()
        {
            foreach (var s in _slots) if (s.Light != null) yield return s.Light;
        }

        /// <summary>空いている光。無ければ、いちばん古く灯した光 (計画 P13「同時に点く光は3つまで・古い物から消す」)</summary>
        static Slot Pick()
        {
            Slot best = null;
            long bestSeq = long.MaxValue;
            int n = Math.Min(_want, _slots.Count);
            for (int i = 0; i < n; i++)
            {
                var s = _slots[i];
                if (s.Light == null) continue;
                if (!s.Lit) return s;
                if (s.Seq < bestSeq) { bestSeq = s.Seq; best = s; }
            }
            return best;
        }

        /// <summary>段2: 寄りの光の印 (StageHitReceive.Mark) を外す。印を付けていなければ何もしない (門が偽なら印は一度も付かない = 呼んでも何も起きない)</summary>
        static void Unmark(Slot s)
        {
            if (!s.Marked) return;
            s.Marked = false;
            if (s.Light != null) StageHitReceive.Mark(s.Light, false);
        }

        static void Release(Slot s)
        {
            if (!s.Borrowed) return;
            s.Borrowed = false;
            if (s.Light != null) s.Light.shadows = LightShadows.None;
            StageLook.ReturnShadowSlot();
        }

        /// <summary>光を弱めて、dur を過ぎたら消す。StageLook が光を当てていなければすぐ消す。遅れて灯す待ちを進める</summary>
        static void Tick(float dt)
        {
            if (_pending.Count > 0)
            {
                if (!Live) _pending.Clear();
                else
                {
                    // 灯すと _pending が増えることは無い (delay 0 で呼ぶ) が、念のため写しで回す
                    var due = new List<Pending>();
                    for (int i = _pending.Count - 1; i >= 0; i--)
                    {
                        var p = _pending[i];
                        p.Left -= dt;
                        if (p.Left <= 0f) { due.Add(p); _pending.RemoveAt(i); }
                    }
                    for (int i = due.Count - 1; i >= 0; i--)   // 待ちに入った順に灯す
                    {
                        var p = due[i];
                        At(p.Key, p.V, p.Color, p.Intensity, p.Dur, p.Hold, p.Shadow, p.Kind, 0f);
                    }
                }
            }
            foreach (var s in _slots)
            {
                if (!s.Lit) continue;
                if(s.ContractOwner!=null)continue;
                if (s.Light == null) { s.Lit = false; Release(s); Unmark(s); continue; }
                s.T = s.DecayTau>0f?Mathf.Max(0f,Time.time-s.AuthoredStarted):s.T+dt;
                float u = s.Dur > 0f ? s.T / s.Dur : 1f;
                if (u >= 1f || !StageLook.Active)
                {
                    s.Lit = false;
                    s.Light.enabled = false;
                    s.Light.intensity = 0f;
                    Release(s);
                    Unmark(s);   // 段2: 寄りの光が消えたら印も外す (印を付けたことが無ければ何もしない)
                    continue;
                }
                // hold までは灯した明るさのまま、その後 (残り÷(1−hold))² で消える (hold 0 = P09 の (1 − 経過÷dur)²)
                float e = u <= s.Hold ? 1f : (1f - u) / Mathf.Max(1e-4f, 1f - s.Hold);
                s.Light.intensity = s.Peak * (s.DecayTau>0f?AuthoredLightEnvelope(s.T,s.AuthoredHold,s.DecayTau,s.Dur,s.ReleasePhase):e*e);
            }
            RefreshContractLights(false);
        }

        sealed class HitLightDriver : MonoBehaviour
        {
            void LateUpdate() { Tick(Time.deltaTime); }

            void OnDestroy()
            {
                if (_root != null && _root != gameObject) return;   // 別のプールの持ち主 (念のため)
                ClearContractLights();
                foreach (var s in _slots) { s.Lit = false; Release(s); }
                _slots.Clear();
                _pending.Clear();
            }
        }
    }
}
