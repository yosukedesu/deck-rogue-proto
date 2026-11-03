// StageBeat.cs — 舞台 (HD-2D の箱庭) が戦闘の出来事に応える口 (2026-11-04 見た目・UX・演出の磨き)。
// Presenter (出来事の順送り) はここだけを呼ぶ。中身は舞台の系 (StageCamera・StageLook・StageFx・StageUnits) が持つ。
// 約束: ①舞台が無い時 (Stage.CombatWorld でない・StageFx.Live でない・バッチ) は何もしない ②紙の UI には掛けない ③0.5 秒以内の一瞬の変化か ±5% まで
// ④FxSettings の倍率を読む ⑤決定的 (UnityEngine.Random を使わない) ⑥Reset で基へ正確に戻る (StageLook の「基×倍」の器を使う)。
using UnityEngine;

namespace DeckRogue.Game
{
    public static class StageBeat
    {
        /// <summary>舞台の揺れ。strength は 0〜1 (0.3 = 小さな当たり・1 = とどめ・幕ボスの大技)。dur 秒。FxSettings.Shake を掛ける。箱庭でも揺れる (旧舞台の Stage.Shake と違い CombatWorld で止めない)</summary>
        public static void Shake(float strength, float dur) { }
        /// <summary>敵 (座席 enemyIndex) の体の白い点滅。strength 0〜1。箱庭では今 0.10 倍で見えないので、ここでは見える量にする</summary>
        public static void HitFlash(int enemyIndex, float strength) { }
        /// <summary>とどめの一撃の瞬間: 舞台の露出の一瞬の上がり (白の閃き)・小さな揺れ。worldPos は当たりの位置</summary>
        public static void FinishBlow(Vector3 worldPos, bool boss) { }
        /// <summary>敵が崩れて消える瞬間の舞台の反応 (足元の光の破裂・粒の上昇)。worldPos は敵の足元</summary>
        public static void EnemyDown(Vector3 worldPos, bool boss) { }
        /// <summary>勝利の余韻: 舞台が少し明るく温かく (灯の範囲だけ)・霧が薄れ・蛍が上がる。幕ボスはより長く。Reset か次の戦闘で戻る</summary>
        public static void Victory(bool boss) { }
        /// <summary>敗北: 舞台が暗く冷たく (露出を下げ彩度を落とす)。Reset で戻る</summary>
        public static void Defeat() { }
        /// <summary>危険 (HP 3 割以下・致死級の被ダメ予測): 舞台の縁が暗く・彩度が少し落ちる。lethal で強め。off で戻る</summary>
        public static void Danger(bool on, bool lethal) { }
        /// <summary>猛り火 (赤の覚醒): 点いている間、灯の色が熾火へ寄り舞台の縁が暖かく脈打つ。off で戻る</summary>
        public static void Blaze(bool on) { }
        /// <summary>手番の区切り: あなたの番は一瞬明るく、敵の番は一瞬冷たく (0.3 秒以内)</summary>
        public static void TurnBeat(bool playerTurn) { }
        /// <summary>戦闘の始まり (敵の登場)。count 体・boss なら長い寄りと低い光</summary>
        public static void EnemyEnter(int count, bool boss) { }
        /// <summary>自分の被弾: 主人公の足元に薔薇の光の一瞬・揺れ (strength 0〜1)</summary>
        public static void PlayerHit(float strength) { }
        /// <summary>仕込み札が鳴った (読み勝ち) の瞬間: 青緑の光が主人公の足元から広がる</summary>
        public static void TrapFired() { }
        /// <summary>全部の変化を基へ戻す (戦闘を離れる時・Presenter.Reset)</summary>
        public static void Reset() { }
    }
}
