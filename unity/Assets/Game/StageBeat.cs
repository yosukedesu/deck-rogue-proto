// StageBeat.cs — 舞台 (HD-2D の箱庭) が戦闘の出来事に応える口 (2026-11-04 見た目・UX・演出の磨き・束 E)。
// Presenter (出来事の順送り) はここだけを呼ぶ。中身は舞台の系 (StageCamera・StageLook・StageFx・StageUnits) が持つ。
// 約束: ①舞台が無い時 (Stage.CombatWorld でない・StageFx.Live でない・バッチ) は何もしない ②紙の UI には掛けない ③0.5 秒以内の一瞬の変化か ±5% まで
// ④FxSettings の倍率を読む ⑤決定的 (UnityEngine.Random を使わない。粒は黄金角で散らす) ⑥Reset で基へ正確に戻る (StageLook の「基×倍」「基±差」の器を使う)。
//
// 作り (束 E 2026-11-04):
//  ・時計は自前 = StageDriver.LateUpdate が StageMotion.Tick の直後に Tick(dt) を呼ぶ (Time.deltaTime 積算。撮影では 1/60 固定)。Tween の coroutine を使わないので、
//    常在の変化 (危険・猛り火・勝利・敗北) が Presenter.PresentationIdle (演出の所有権) を塞がない。脈 (猛り火・致死級) だけ Tween.UnscaledDt。
//  ・書く主は 1 つ: 露出 (StageLook.SetExposureBoost) は StageMotion の敵の大技 (EnemyBigFlashing) の間は書かない (終わった同じフレームにこちらが書く = 途切れない)。
//    暗転 (SetDim) は使わない (StageMotion の器)。霧の倍率は寄り (CloseUpActive) の間は書かない。
//  ・画質の段 (GfxQuality.Level): 高 = 全部・標準 = 追加の点光源 ×0.5・粒なし・軽量 = 追加の点光源なし・粒なし (露出・減光・彩度・色寄せ・揺れは値だけなので残す)。
//  ・光は StageFx の技の光のプール (hitLight.maxLights) を reuse の番号で使い回す = 光の数を増やさない。粒は 1 回 30 個まで (自前の系 HD2D-BeatMotes・Emit だけ)。
//  ・ScreenFx.Instant (撮影で切り替えの動きを飛ばす) なら勝利・敗北は即最終の姿。戦闘内の一瞬の山 (とどめ・揺れ) は Instant でも動く (play=/endplay= のコマ送りで撮る)。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    public static class StageBeat
    {
        // ================================================================ 門と予算

        /// <summary>舞台が応えられる (有効な箱庭で光が当たっている)。偽なら書かない (状態の時計だけ進める)</summary>
        static bool Live => StageFx.Live && Stage.CombatWorld;

        /// <summary>追加の点光源の倍率 (画質の段: 高 1・標準 0.5・軽量 0 = 灯さない)</summary>
        static float LightMul
        {
            get { var l = GfxQuality.Level; return l == GfxLevel.High ? 1f : l == GfxLevel.Standard ? 0.5f : 0f; }
        }
        /// <summary>粒を出してよい (高の段だけ。Xperia 5 II がカクつく)</summary>
        static bool ParticlesOk => GfxQuality.Level == GfxLevel.High && !ScreenFx.Instant;

        /// <summary>StageBeat.Shake(1) の揺れ (px)。Stage.Shake が箱庭で ×0.6 するので、箱庭で 8px になる量を渡す</summary>
        const float ShakePx = 8f / 0.6f;

        // 色 (役割の色は PaperFx の名前。光の色は Light.color なので α は見ない)
        static readonly Color CoolFilter = new Color(0.88f, 0.94f, 1f);     // 敵の番: 少し冷たく (色の寄せの先)
        static readonly Color WarmFilter = new Color(1f, 0.93f, 0.86f);     // 猛り火: 少し暖かく (色の寄せの先)

        // ================================================================ 口 (Presenter が呼ぶ)

        /// <summary>舞台の揺れ。strength は 0〜1 (0.3 = 小さな当たり・1 = とどめ・幕ボスの大技)。dur 秒 (0.3 以内に丸める)。FxSettings.Shake は Stage.Shake が掛ける。箱庭でも揺れる</summary>
        public static void Shake(float strength, float dur)
        {
            if (float.IsNaN(strength) || strength <= 0f) return;
            float s = Mathf.Clamp(strength, 0f, 1.5f);
            float d = Mathf.Clamp(float.IsNaN(dur) ? 0.2f : dur, 0.05f, 0.3f);
            Stage.Shake(s * (Stage.CombatWorld ? ShakePx : 8f), d);
        }

        /// <summary>敵 (座席 enemyIndex) の体の白い点滅。strength 0〜1 → 白の量 0.35〜0.8・長さ 0.18〜0.26 秒 (シェーダの _Flash は白への lerp = 白飛びしない)</summary>
        public static void HitFlash(int enemyIndex, float strength)
        {
            if (enemyIndex < 0) return;
            float s = Mathf.Clamp01(float.IsNaN(strength) ? 0f : strength);
            Stage.Flash("enemy" + enemyIndex, 0.18f + 0.08f * s, Mathf.Lerp(0.35f, 0.8f, s));
        }

        /// <summary>とどめの一撃の瞬間: 露出の一瞬の上がり (0.08 秒で上げ 0.3 秒で戻す。幕ボスは高く長く)・当たりの位置に白い光・揺れ (1)。worldPos は当たりの位置</summary>
        public static void FinishBlow(Vector3 worldPos, bool boss)
        {
            _any = true;
            float flash = FxSettings.Flash;
            if (flash > 0f) AddExposurePulse(0.08f, boss ? 0.45f : 0.3f, (boss ? 0.75f : 0.55f) * flash);
            if (Live && Ok(worldPos))
            {
                float I = (boss ? 24f : 16f) * LightMul;
                if (I > 0f)
                    _finishSeq = StageFx.MotionLight(worldPos, PaperFx.Paper3, I, boss ? 9.5f : 8f, FxSettings.Dur(boss ? 0.45f : 0.35f), 0.2f, boss, "beat:finish", "finish", _finishSeq);
            }
            Shake(1f, 0.25f);
        }

        /// <summary>敵が崩れて消える瞬間: 足元の光の破裂 (上へ抜ける)・粒の上昇 (18・ボス 30)。ボスは大きく長く・小さな揺れ。worldPos は敵の足元</summary>
        public static void EnemyDown(Vector3 worldPos, bool boss)
        {
            if (!Ok(worldPos)) return;
            _any = true;
            float dur = FxSettings.Dur(boss ? 0.9f : 0.6f);
            float I = (boss ? 22f : 14f) * LightMul;
            if (Live && I > 0f)
            {
                var d = TakeDown();
                d.Start = worldPos + Vector3.up * 0.3f; d.T = 0f; d.Dur = dur; d.Peak = I; d.Range = boss ? 9f : 7f; d.Rise = boss ? 2.2f : 1.6f;
                d.Seq = StageFx.MotionLight(d.Start, boss ? PaperFx.BrassLight : PaperFx.Paper3, I, d.Range, dur, 0.9f, false, "beat:down", "down", d.Seq);
                if (d.Seq == 0) d.T = dur;   // 灯せなかった (消えた扱い)
            }
            if (Live && ParticlesOk) EmitMotes(worldPos, boss ? 30 : 18, boss ? PaperFx.BrassLight : PaperFx.Paper3, boss ? 2.6f : 2f);
            Shake(boss ? 0.6f : 0.25f, boss ? 0.25f : 0.15f);
        }

        /// <summary>勝利の余韻: 2 秒かけて (幕ボスは 2.6 秒) 露出 +5%・霧を薄く (0.85 倍)・灯を暖かく (真鍮の光へ 0.35)・蛍が金色に上がる (24 粒)。Reset で戻る</summary>
        public static void Victory(bool boss)
        {
            _any = true;
            float dur = ScreenFx.Instant ? 0f : FxSettings.Dur(boss ? 2.6f : 2f);
            _winExp.To(0.07f, dur);
            _winFog.To(1f, dur);
            _winLamp.To(0.35f, dur);
            if (!ScreenFx.Instant) Schedule(0.3f, SchedKind.Fireflies, 0);
            else Fireflies();
        }

        /// <summary>敗北: 1.2 秒かけて露出 −15% (−0.24 段)・彩度 −40・周辺減光 +0.12。Reset で戻る</summary>
        public static void Defeat()
        {
            _any = true;
            float dur = ScreenFx.Instant ? 0f : FxSettings.Dur(1.2f);
            _loseExp.To(-0.24f, dur);
            _loseSat.To(-40f, dur);
            _loseVig.To(0.12f, dur);
        }

        /// <summary>危険 (HP 3 割以下・致死級の被ダメ予測): 周辺減光 +0.10 (致死級 +0.18 と 1.7Hz の薄い脈)・彩度 −10 (致死級 −20)。0.5 秒で入り、off で 0.4 秒で戻る。何度呼んでも同じ (Rebuild ごとに呼ばれる)</summary>
        public static void Danger(bool on, bool lethal)
        {
            _any = true;
            float dur = FxSettings.Dur(on ? 0.5f : 0.4f);
            _dangerVig.To(on ? (lethal ? 0.18f : 0.10f) : 0f, dur);
            _dangerSat.To(on ? (lethal ? -20f : -10f) : 0f, dur);
            _lethal = on && lethal;
        }

        /// <summary>猛り火 (赤の覚醒): 点いている間、灯の色が熾火へ寄り (0.45)・色が少し暖かく (0.15)・縁が 1Hz でごく薄く脈打つ (周辺減光 +0.03±0.03)。off で 0.5 秒で戻る</summary>
        public static void Blaze(bool on)
        {
            _any = true;
            float dur = FxSettings.Dur(on ? 0.4f : 0.5f);
            _blazeLamp.To(on ? 0.45f : 0f, dur);
            _blazeFilter.To(on ? 0.15f : 0f, dur);
            _blazePulse.To(on ? 1f : 0f, dur);
        }

        /// <summary>手番の区切り: あなたの番は露出 +0.05 段を 0.3 秒 (0.08 で上げ 0.22 で戻す)、敵の番は色を少し冷たく 0.3 秒 (0.1 で寄せ 0.2 で戻す)</summary>
        public static void TurnBeat(bool playerTurn)
        {
            _any = true;
            if (playerTurn) AddExposurePulse(0.08f, 0.22f, 0.05f);
            else { _cool = new Pulse { Up = FxSettings.Dur(0.1f), Down = FxSettings.Dur(0.2f), Amp = 0.7f }; }   // 頂点で CoolFilter へ 0.7 (赤 −8%)
        }

        /// <summary>戦闘の始まり (敵の登場): 各敵の足元に淡い登場の光 (0.12 + 0.09i 秒後・0.45 秒)。ボスは露出を一度落として戻す「息を呑む」0.6 秒と小さな揺れ。寄り (Dolly) は使わない (酔う裁定)</summary>
        public static void EnemyEnter(int count, bool boss)
        {
            _any = true;
            int n = Mathf.Clamp(count, 0, EnterSlots);
            for (int i = 0; i < n; i++) Schedule(0.12f + 0.09f * i, SchedKind.EnterLight, i);
            if (boss)
            {
                AddExposurePulse(0.25f, 0.35f, -0.35f);   // 落として戻す
                Schedule(0.28f, SchedKind.BossShake, 0);
            }
        }

        /// <summary>自分の被弾: 主人公の足元に薔薇の光の一瞬 (0.3 秒)・揺れ (strength × 0.6)</summary>
        public static void PlayerHit(float strength)
        {
            float s = Mathf.Clamp01(float.IsNaN(strength) ? 0f : strength);
            _any = true;
            if (Live)
            {
                Vector3 p;
                float I = (5f + 7f * s) * LightMul;
                if (I > 0f && FeetPoint("player", 0.3f, out p))
                    _hitSeq = StageFx.MotionLight(p, PaperFx.Rose, I, 4.5f, FxSettings.Dur(0.3f), 0.1f, false, "beat:playerHit", "player", _hitSeq);
            }
            Shake(s * 0.6f, 0.2f);
        }

        /// <summary>仕込み札が鳴った (読み勝ち): 主人公の足元から青緑の光の輪が広がる (届く距離 1.5 → 7・0.4 秒)</summary>
        public static void TrapFired()
        {
            if (!Live) return;
            Vector3 p;
            float I = 9f * LightMul;
            if (I <= 0f || !FeetPoint("player", 0.25f, out p)) return;
            _any = true;
            _trapFeet = p; _trapT = 0f; _trapDur = FxSettings.Dur(0.4f); _trapPeak = I;
            _trapSeq = StageFx.MotionLight(p, PaperFx.Mana, I, 1.5f, _trapDur, 0.9f, false, "beat:trap", "player", _trapSeq);
            if (_trapSeq == 0) _trapT = _trapDur;
        }

        /// <summary>全部の変化を基へ戻す (戦闘を離れる時・Presenter.Reset)。StageMotion が露出・暗転・寄りの霧を書いている間はそれに譲る (StageMotion が自分で戻す)</summary>
        public static void Reset()
        {
            _expPulses.Clear(); _sched.Clear(); _cool = null;
            _winExp.Zero(); _winFog.Zero(); _winLamp.Zero();
            _loseExp.Zero(); _loseSat.Zero(); _loseVig.Zero();
            _dangerVig.Zero(); _dangerSat.Zero(); _lethal = false;
            _blazeLamp.Zero(); _blazeFilter.Zero(); _blazePulse.Zero();
            _pulseClock = 0f;
            _trapSeq = 0; _trapT = 0f;
            foreach (var d in _downs) { d.Seq = 0; d.T = 0f; d.Dur = 0f; }
            if (_motes != null) _motes.Clear(true);
            WriteAll();
            _any = false;
        }

        // ================================================================ 時計 (StageDriver.LateUpdate が StageMotion.Tick の直後に呼ぶ)

        static bool _any;

        /// <summary>毎フレーム。何も走っていなければ何もしない。dt = Time.deltaTime (撮影では 1/60・とどめのスローではゆっくり)</summary>
        internal static void Tick(float dt)
        {
            if (!_any) return;
            if (float.IsNaN(dt) || dt < 0f) dt = 0f;
            // 戦闘の画面が無くなった (決着の余韻の後に報酬へ組み直した・タイトルへ戻った) のに常在の値 (勝利・敗北・危険・猛り火) が残っていれば基へ (StageMotion.Tick と同じ判定。Presenter の Reset が来なくても次の画面へ持ち越さない)
            if (Holding && (GameRoot.I == null || GameRoot.I.Battle == null)) { Reset(); return; }
            bool busy = false;
            // 予約 (登場の光・ボスの揺れ・蛍)
            if (_sched.Count > 0)
            {
                for (int i = _sched.Count - 1; i >= 0; i--)
                {
                    var s = _sched[i];
                    s.T += dt;
                    if (s.T < s.At) continue;
                    _sched.RemoveAt(i);
                    Fire(s);
                }
                busy |= _sched.Count > 0;
            }
            // 一瞬の山 (露出)
            for (int i = _expPulses.Count - 1; i >= 0; i--)
            {
                var p = _expPulses[i];
                p.T += dt;
                if (p.T >= p.Up + p.Down) _expPulses.RemoveAt(i);
            }
            busy |= _expPulses.Count > 0;
            if (_cool != null) { _cool.T += dt; if (_cool.T >= _cool.Up + _cool.Down) _cool = null; else busy = true; }
            // 常在 (勝利・敗北・危険・猛り火)
            busy |= _winExp.Tick(dt) | _winFog.Tick(dt) | _winLamp.Tick(dt);
            busy |= _loseExp.Tick(dt) | _loseSat.Tick(dt) | _loseVig.Tick(dt);
            busy |= _dangerVig.Tick(dt) | _dangerSat.Tick(dt);
            busy |= _blazeLamp.Tick(dt) | _blazeFilter.Tick(dt) | _blazePulse.Tick(dt);
            if (_blazePulse.Cur > 0f || (_lethal && _dangerVig.Cur > 0f)) { _pulseClock += Tween.UnscaledDt; busy = true; }
            else _pulseClock = 0f;
            // 動く光 (からくりの輪・崩れの光)
            if (_trapSeq != 0 && _trapT < _trapDur)
            {
                _trapT += dt;
                float u = Mathf.Clamp01(_trapT / Mathf.Max(0.01f, _trapDur));
                float e = 1f - (1f - u) * (1f - u);   // 速く広がり、ゆっくり届く
                if (!StageFx.Retune(_trapSeq, _trapFeet, _trapPeak * (1f - u), Mathf.Lerp(1.5f, 7f, e))) _trapT = _trapDur;
                busy = true;
            }
            foreach (var d in _downs)
            {
                if (d.Seq == 0 || d.T >= d.Dur) continue;
                d.T += dt;
                float u = Mathf.Clamp01(d.T / Mathf.Max(0.01f, d.Dur));
                float e = 1f - (1f - u) * (1f - u);
                if (!StageFx.Retune(d.Seq, d.Start + Vector3.up * (d.Rise * e), d.Peak * (1f - u * u), d.Range)) d.T = d.Dur;
                busy = true;
            }
            WriteAll();
            if (!busy && !Holding) _any = false;   // 全部 0 で止まった (WriteAll が 0 を書いた後)。常在の値が残る間は毎フレーム書き直す (光の組み直しで基が取り直されても合う)
        }

        /// <summary>常在の値 (勝利・敗北・危険・猛り火) が 0 でない = 書き続ける</summary>
        static bool Holding =>
            _winExp.Cur != 0f || _winFog.Cur != 0f || _winLamp.Cur != 0f || _loseExp.Cur != 0f || _loseSat.Cur != 0f || _loseVig.Cur != 0f
            || _dangerVig.Cur != 0f || _dangerSat.Cur != 0f || _blazeLamp.Cur != 0f || _blazeFilter.Cur != 0f || _blazePulse.Cur != 0f;

        /// <summary>今の状態を舞台へ書く (Live でなければ何も書かない)。書く主が StageMotion の間はその器を触らない</summary>
        static void WriteAll()
        {
            if (!Live) return;
            // 露出 (段) = 一瞬の山の和 + 勝利 + 敗北。StageMotion の敵の大技の間は書かない
            if (!StageMotion.EnemyBigFlashing)
            {
                float exp = _winExp.Cur + _loseExp.Cur;
                foreach (var p in _expPulses) exp += p.Env();
                exp = Mathf.Clamp(exp, -1.5f, 1.5f);
                if (exp != 0f || StageLook.ExposureBoost != 0f) StageLook.SetExposureBoost(Mathf.Abs(exp) < 1e-4f ? 0f : exp);
            }
            // 彩度
            float sat = _loseSat.Cur + _dangerSat.Cur;
            if (sat != 0f || StageLook.SaturationOffset != 0f) StageLook.SetSaturationOffset(Mathf.Abs(sat) < 1e-3f ? 0f : sat);
            // 周辺減光 (+ 猛り火の 1Hz の脈・致死級の 1.7Hz の脈)
            float vig = _loseVig.Cur + _dangerVig.Cur;
            if (_blazePulse.Cur > 0f) vig += _blazePulse.Cur * (0.03f + 0.03f * Mathf.Sin(_pulseClock * 2f * Mathf.PI));
            if (_lethal && _dangerVig.Cur > 0f) vig += 0.03f * Mathf.Sin(_pulseClock * 2f * Mathf.PI * 1.7f);
            if (vig != 0f || StageLook.VignetteOffset != 0f) StageLook.SetVignetteOffset(Mathf.Abs(vig) < 1e-4f ? 0f : Mathf.Max(0f, vig));
            // 色の寄せ (敵の番の冷え + 猛り火の暖かさ)
            float kCool = _cool != null ? Mathf.Clamp01(_cool.Env()) : 0f, kWarm = _blazeFilter.Cur;
            float kF; Color cF;
            Combine(CoolFilter, kCool, WarmFilter, kWarm, out cF, out kF);
            if (kF > 0f || StageLook.ColorFilterMix > 0f) StageLook.SetColorFilterMix(cF, kF < 1e-4f ? 0f : kF);
            // 灯の色 (勝利の真鍮 + 猛り火の熾)
            float kL; Color cL;
            Combine(PaperFx.BrassLight, _winLamp.Cur, PaperFx.Ember, _blazeLamp.Cur, out cL, out kL);
            if (kL > 0f || StageLook.LampTint > 0f) StageLook.SetLampTint(cL, kL < 1e-4f ? 0f : kL);
            // 霧 (勝利で薄く)。寄りの間は StageMotion の器
            if (!StageMotion.CloseUpActive)
            {
                float want = 1f - 0.15f * Mathf.Clamp01(_winFog.Cur);
                if (want < 1f) { if (Mathf.Abs(StageLook.FogScale - want) > 1e-3f) StageLook.SetFogScale(want); _fogWritten = true; }
                else if (_fogWritten) { _fogWritten = false; if (StageLook.FogScale < 1f) StageLook.SetFogScale(1f); }
            }
        }
        static bool _fogWritten;

        /// <summary>2 つの寄せを 1 つに: k = 1 − (1−k1)(1−k2)・先は k で重みづけた平均</summary>
        static void Combine(Color c1, float k1, Color c2, float k2, out Color c, out float k)
        {
            k1 = Mathf.Clamp01(k1); k2 = Mathf.Clamp01(k2);
            k = 1f - (1f - k1) * (1f - k2);
            float w = k1 + k2;
            c = w > 1e-5f ? Color.Lerp(c1, c2, k2 / w) : Color.white;
        }

        // ================================================================ 一瞬の山・常在の値・予約

        /// <summary>一瞬の山: Up 秒で Amp まで直線で上がり、Down 秒で (1−u)² で 0 へ</summary>
        sealed class Pulse
        {
            public float T, Up, Down, Amp;
            public float Env()
            {
                if (T < Up) return Amp * (Up > 0f ? T / Up : 1f);
                float u = Down > 0f ? Mathf.Clamp01((T - Up) / Down) : 1f;
                return Amp * (1f - u) * (1f - u);
            }
        }
        static readonly List<Pulse> _expPulses = new List<Pulse>();
        static Pulse _cool;
        static void AddExposurePulse(float up, float down, float amp)
        {
            if (_expPulses.Count >= 8) _expPulses.RemoveAt(0);
            _expPulses.Add(new Pulse { Up = FxSettings.Dur(up), Down = FxSettings.Dur(down), Amp = amp });
        }

        /// <summary>常在の値: Target へ Speed/秒で寄る (Speed 0 = 即)。Tick は動いた (まだ Target でない) 間 true</summary>
        sealed class Dial
        {
            public float Cur, Target, Speed;
            public void To(float target, float dur)
            {
                Target = target;
                float dist = Mathf.Abs(target - Cur);
                Speed = dur > 1e-4f && dist > 1e-6f ? dist / dur : 0f;
                if (Speed <= 0f) Cur = target;
            }
            public void Zero() { Cur = 0f; Target = 0f; Speed = 0f; }
            public bool Tick(float dt)
            {
                if (Cur == Target) return false;
                Cur = Speed > 0f ? Mathf.MoveTowards(Cur, Target, Speed * dt) : Target;
                return Cur != Target;
            }
        }
        static readonly Dial _winExp = new Dial(), _winFog = new Dial(), _winLamp = new Dial();
        static readonly Dial _loseExp = new Dial(), _loseSat = new Dial(), _loseVig = new Dial();
        static readonly Dial _dangerVig = new Dial(), _dangerSat = new Dial();
        static readonly Dial _blazeLamp = new Dial(), _blazeFilter = new Dial(), _blazePulse = new Dial();
        static bool _lethal;
        static float _pulseClock;

        enum SchedKind { EnterLight, BossShake, Fireflies }
        sealed class Sched { public float T, At; public SchedKind Kind; public int Index; }
        static readonly List<Sched> _sched = new List<Sched>();
        static void Schedule(float at, SchedKind kind, int index)
        {
            if (_sched.Count >= 16) _sched.RemoveAt(0);
            _sched.Add(new Sched { At = FxSettings.Dur(at), Kind = kind, Index = index });
        }
        static void Fire(Sched s)
        {
            switch (s.Kind)
            {
                case SchedKind.EnterLight:
                    {
                        if (!Live) return;
                        float I = 6f * LightMul;
                        Vector3 p;
                        if (I <= 0f || s.Index < 0 || s.Index >= EnterSlots || !FeetPoint("enemy" + s.Index, 0.35f, out p)) return;
                        _enterSeq[s.Index] = StageFx.MotionLight(p, PaperFx.Paper2, I, 5f, FxSettings.Dur(0.45f), 0.2f, false, "beat:enter", "enemy" + s.Index, _enterSeq[s.Index]);
                        return;
                    }
                case SchedKind.BossShake: Shake(0.4f, 0.2f); return;
                case SchedKind.Fireflies: Fireflies(); return;
            }
        }

        // ================================================================ 光の置き場・使い回しの番号

        const int EnterSlots = 6;
        static long _finishSeq, _hitSeq, _trapSeq;
        static readonly long[] _enterSeq = new long[EnterSlots];
        static Vector3 _trapFeet; static float _trapT, _trapDur, _trapPeak;

        sealed class Down { public long Seq; public Vector3 Start; public float T, Dur, Peak, Range, Rise; }
        static readonly Down[] _downs = { new Down(), new Down(), new Down(), new Down() };
        /// <summary>崩れの光の器 (4 体同時まで。空きが無ければいちばん進んだ物を使い回す)</summary>
        static Down TakeDown()
        {
            Down best = _downs[0];
            foreach (var d in _downs)
            {
                if (d.Seq == 0 || d.T >= d.Dur) return d;
                if (d.T / Mathf.Max(0.01f, d.Dur) > best.T / Mathf.Max(0.01f, best.Dur)) best = d;
            }
            return best;
        }

        static bool Ok(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        /// <summary>板 key の足元の上 lift の点 (板が無ければ座席)。カメラの方へ 0.6 寄せる (板の面の上だと影が潰れる = StageFx.Where と同じ)。無ければ false</summary>
        static bool FeetPoint(string key, float lift, out Vector3 p)
        {
            p = default;
            var up = StageFx.UnitPoint(key, new Vector2(0.5f, 0f));
            if (up.HasValue) p = up.Value;
            else
            {
                Vector3 seat; float k;
                if (!Stage.TryGetSeat(key, out seat, out k)) return false;
                p = seat;
            }
            p += Vector3.up * lift;
            var cam = Stage.Camera;
            if (cam != null) { var to = cam.transform.position - p; if (to.sqrMagnitude > 1e-6f) p += to.normalized * 0.6f; }
            return Ok(p);
        }

        // ================================================================ 粒 (自前の系。Emit だけ・乱数を使わない)

        static ParticleSystem _motes;
        static int _emits;

        static ParticleSystem Motes()
        {
            if (_motes != null) return _motes;
            var mat = Stage.MotionGlowMaterial(false);
            if (mat == null) return null;
            var go = new GameObject("HD2D-BeatMotes");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = 20261104u;
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            main.gravityModifier = -0.04f;   // ふわりと上へ
            var em = ps.emission; em.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (r != null) { r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard; r.sortingOrder = 6; }
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 3f; lim.drag = 1.6f;
            ps.Play();
            _motes = ps;
            return ps;
        }

        /// <summary>at の回りから n 粒 (30 まで) を上へ。向きは黄金角で散らす (撮るたびに同じ)</summary>
        static void EmitMotes(Vector3 at, int n, Color color, float speed)
        {
            n = Mathf.Clamp(n, 0, 30);
            if (n <= 0) return;
            var ps = Motes();
            if (ps == null) return;
            var rot = Stage.LayoutRotation;
            Vector3 right = rot * Vector3.right, fwd = rot * Vector3.forward;
            var c = new Color(color.r, color.g, color.b, 1f);
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                float a = i * 2.3999632f + _emits * 0.917f;
                float f1 = Frac(i * 0.6180340f + _emits * 0.3141f);
                float f2 = Frac(i * 0.7548777f + _emits * 0.5698f);
                float f3 = Frac(i * 0.5698403f + _emits * 0.1234f);
                var side = (Mathf.Cos(a) * right + Mathf.Sin(a) * fwd) * (0.15f + 0.3f * f1);
                ep.position = at + side + Vector3.up * (0.1f + 0.5f * f2);
                ep.velocity = side.normalized * (0.3f + 0.5f * f3) + Vector3.up * speed * (0.6f + 0.6f * f1);
                ep.startLifetime = 0.6f + 0.5f * f2;
                ep.startSize = 0.07f + 0.07f * f3;
                ep.startColor = c;
                ps.Emit(ep, 1);
            }
            _emits++;
        }

        /// <summary>勝利の蛍: 舞台の蛍の系 (Stage.FxSystem "fireflies") から金色の 24 粒を上げる (高の段だけ)</summary>
        static void Fireflies()
        {
            if (!Live || !ParticlesOk) return;
            var ps = Stage.FxSystem("fireflies");
            if (ps == null || !ps.gameObject.activeInHierarchy) return;
            var main = ps.main;
            var prev = main.startColor;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 1f));
            ps.Emit(24);
            main.startColor = prev;
        }

        static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>プレイの始まり (ドメインの読み直しなしでも) に時計と器を捨てる (StageMotion.ResetStatics と同じ考え)</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _expPulses.Clear(); _sched.Clear(); _cool = null;
            _winExp.Zero(); _winFog.Zero(); _winLamp.Zero(); _loseExp.Zero(); _loseSat.Zero(); _loseVig.Zero();
            _dangerVig.Zero(); _dangerSat.Zero(); _blazeLamp.Zero(); _blazeFilter.Zero(); _blazePulse.Zero();
            _lethal = false; _pulseClock = 0f; _fogWritten = false; _any = false;
            _finishSeq = _hitSeq = _trapSeq = 0; for (int i = 0; i < _enterSeq.Length; i++) _enterSeq[i] = 0;
            foreach (var d in _downs) { d.Seq = 0; d.T = 0f; d.Dur = 0f; }
            _motes = null; _emits = 0;
        }
    }
}
