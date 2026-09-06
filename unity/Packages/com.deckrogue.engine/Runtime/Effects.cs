// Effects.cs — 宣言的効果 (DeclarativeEffect) の解決。src/engine/effects.ts の手書き移植。
// カード効果は data/*.json の宣言的記述をここで状態遷移に変換する。
// 表現できない効果だけ scriptId で名前付きスクリプトに逃がす (現状は未登録)。
//
// 移植メモ (unity/PORTING.md):
// - TS の export 関数を PascalCase で公開。モジュール内 (非export) 関数は private static。
// - emit の位置・種類・順番は TS と完全一致させる (ゴールデンはイベント型列を見る)。
// - state.enemies[i] の範囲外は TS では undefined。C# では EnemyAt() が null を返す形で揃える。
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    /// <summary>
    /// リアクションの誘発窓 (TS: effects.ts の ReactionWindow 判別共用体)。
    /// pre = 敵の行動の確定時・実行前 (打ち消し onEnemyAction / 軽減 onAttackIncoming)
    /// post = 敵の行動の解決後 (返し onAttacked / onEnemyBuffed / onEnemyDefended。条件判定に HpLoss を使う)
    /// </summary>
    public sealed record ReactionWindow
    {
        /// <summary>'pre' | 'post'</summary>
        public string Stage { get; init; } = default!;
        /// <summary>EnemyActionKind</summary>
        public string Kind { get; init; } = default!;
        /// <summary>その行動の実値 (post 窓でも minActionValue の判定に使う。2026-08-31)</summary>
        public int Actual { get; init; }
        /// <summary>stage='post' のみ意味を持つ (TS の共用体では pre 側にフィールドが無い)</summary>
        public int HpLoss { get; init; }
    }

    /// <summary>カードホバー用のダメージ内訳の1段 (TS: DamageBreakdownStep)</summary>
    public sealed record DamageBreakdownStep
    {
        public string Label { get; init; } = default!;
        public int Value { get; init; }
    }

    /// <summary>カードホバー用のダメージ内訳 (TS: DamageBreakdown)</summary>
    public sealed record DamageBreakdown
    {
        public IReadOnlyList<DamageBreakdownStep> Steps { get; init; } = default!;
        public int Blocked { get; init; }
        public int HpLoss { get; init; }
    }

    public static class Effects
    {
        /// <summary>
        /// 猛り火のしきい値 (確定済みルール表「猛り火」)。全札で単一の8。
        /// カードごとに変えないのは、8ひとつを覚えれば全札が読めるようにするため
        /// </summary>
        public const int BLAZE_THRESHOLD = 8;

        // ---- 小ヘルパ (TS の配列アクセスの undefined 挙動を C# で揃える) ----

        private static EnemyState? EnemyAt(GameState state, int index)
            => index >= 0 && index < state.Enemies.Count ? state.Enemies[index] : null;

        /// <summary>TS の enemies.findIndex((e) =&gt; e.hp &gt; 0) と同じ (居なければ -1)</summary>
        private static int FindAliveIndex(IReadOnlyList<EnemyState> enemies)
        {
            for (int i = 0; i < enemies.Count; i++) if (enemies[i].Hp > 0) return i;
            return -1;
        }

        /// <summary>TS の enemies.map((e,i) =&gt; i === index ? f(e) : e) と同じ (常に新しいリスト)</summary>
        private static IReadOnlyList<EnemyState> MapEnemy(IReadOnlyList<EnemyState> enemies, int index, Func<EnemyState, EnemyState> f)
        {
            var list = new List<EnemyState>(enemies.Count);
            for (int i = 0; i < enemies.Count; i++) list.Add(i == index ? f(enemies[i]) : enemies[i]);
            return list;
        }

        /// <summary>
        /// 猛り火が点いているか = **生存する敵の延焼の合計**がしきい値以上か (2026-08-30)。
        /// 対象1体でなく合計を見るのはユーザー判断——全体延焼 (火の粉の雨・業火の炉) が
        /// そのまま猛り火の燃料になり、ボス単体戦では合計＝そのボスの延焼なので同じ挙動になる
        /// </summary>
        public static int BlazeTotal(GameState state)
        {
            int acc = 0;
            foreach (var e in state.Enemies) acc += e.Hp > 0 ? e.Burn : 0;
            return acc;
        }

        public static bool IsBlazing(GameState state) => BlazeTotal(state) >= BLAZE_THRESHOLD;

        /// <summary>
        /// 常在オーラ (2026-09-02 StS2 Afflictions式): 生存する敵の aura による同タイプのコスト増の合計。
        /// 敵を倒せば即0 = キル順の圧。割引 (discountNext) はオーラ増加分にも効く
        /// </summary>
        /// <summary>ダメージを与える効果を1つでも持つ札か (モードの中も見る)。重圧 attacksOnly の判定</summary>
        private static bool CardHasDamage(CardDef def)
        {
            foreach (var e in def.Effects) if (IsDamageEffect(e)) return true;
            if (def.Modes != null) foreach (var m in def.Modes) foreach (var e in m.Effects) if (IsDamageEffect(e)) return true;
            return false;
        }

        public static int AuraCostUp(GameState state, CardInstance card)
        {
            int up = 0;
            foreach (var e in state.Enemies)
            {
                if (e.Hp <= 0) continue;
                var aura = Content.GetEnemyDef(e.EnemyId).Aura;
                if (aura == null) continue;
                if (aura.CardType != null && aura.CardType != card.Def.Type) continue;
                // 攻撃札だけの重圧 (2026-09-06): ダメージ効果を持つ札 (モード含む) のみ
                if (aura.AttacksOnly == true && !CardHasDamage(card.Def)) continue;
                up += aura.CostUp;
            }
            return up;
        }

        /// <summary>
        /// 実効コスト: マナ軽減トークン (nextCardDiscount) と猛り火の軽減を適用したプレイコスト。
        /// 素のコスト0のカードは割引を消費しない (対象外)。
        /// </summary>
        public static int EffectiveCost(GameState state, CardInstance card)
        {
            // Xコスト: 現在のエナジーを全て支払う (最低1 = エナジー0ではプレイ不可)。割引・オーラの対象外。
            // 実際に払う量は PlayCard.xAmount で選べる (既定=全部)
            if (card.Def.XCost == true) return Math.Max(1, state.Player.Energy);
            // 屍集めで戻した札はこの戦闘中0E (2026-08-31 rework。割引も消費しない。オーラも「0Eの約束」を破らない)
            if (card.FreeThisCombat == true) return 0;
            int up = AuraCostUp(state, card);
            // 手札参照 (年輪=本家 Clash): 手札の他の札がすべて物理なら0E (重圧の上乗せは残る)。
            // 2026-09-06 白の解凍で一般化 (freeIfHandAll='spell'=大城壁)。判定は自身を除く手札
            string? freeType = card.Def.FreeIfHandAll ?? (card.Def.FreeIfHandAllPhysical == true ? CardTypes.Physical : null);
            if (freeType != null)
            {
                bool all = true;
                foreach (var c in state.Player.Hand)
                {
                    bool ok = c.Uid == card.Uid
                        || (freeType == "nonphysical" ? c.Def.Type != CardTypes.Physical : c.Def.Type == freeType);
                    if (!ok) { all = false; break; }
                }
                if (all) return up;
            }
            // 勢い参照 (追い風): 勢いがN以上なら0E (緑 勢いの網 2026-09-04)
            if (card.Def.FreeIfMomentumAtLeast != null && state.Player.Momentum >= card.Def.FreeIfMomentumAtLeast.Value) return up;
            // 素のコスト0のカードは割引と無縁 (消費しない既存則) — オーラの重さはそのまま払う
            if (card.Def.Cost == 0) return up;
            int blaze = card.Def.BlazeDiscount != null && IsBlazing(state) ? card.Def.BlazeDiscount.Value : 0;
            return Math.Max(0, card.Def.Cost + up - blaze - state.Player.NextCardDiscount);
        }

        /// <summary>従者を要求する札 (殉教の誓い 2026-09-06) のプレイ条件: 場に従者 (retainer・innate除く) が1体以上</summary>
        public static bool RetainerRequirementMet(GameState state, CardInstance card)
        {
            if (card.Def.RequiresRetainer != true) return true;
            foreach (var p in state.Player.Permanents)
                if (p.Def.Retainer == true && p.Innate != true) return true;
            return false;
        }

        /// <summary>自ターンにプレイ可能なカードか。リアクションタイプは false。置物・選択式は常にプレイ可能</summary>
        public static bool IsPlayableFromHand(CardInstance card)
        {
            if (card.Def.Type == CardTypes.Reaction) return false;
            if (card.Def.Type == CardTypes.Permanent) return true;
            if ((card.Def.Modes?.Count ?? 0) > 0) return true;
            foreach (var e in card.Def.Effects) if (e.Trigger == "onPlay") return true;
            return false;
        }

        /// <summary>ダメージを与える効果か (「攻撃プレイ後」誘発の判定に使う)</summary>
        private static readonly HashSet<string> DAMAGE_EFFECTS = new HashSet<string>
        {
            "dealDamage",
            "dealDamageRandom",
            "dealDamagePerCardPlayed",
            "dealDamagePerCardPlayedTotal",
            "dealDamagePerEnergyMax",
            "dealDamagePerAttackPlayed",
            "dealDamagePerWeak",
            "dealDamagePerMomentum",
            "dealDamagePerHeal",
            "recycleExhaust",
            "dischargeAether",
            "dischargeGrowth",
            "dealDamageCleave",
            "dealDamagePerBlock",
            "dealDamagePerPermanent",
            "dealDamageDrain",
            "dealDamagePerExhaust",
            "dealDamageDrainPerExhaust",
            "dealDamagePerSelfHpLost",
            "dealDamagePerNegStrength",
            "dischargeBurn",
            "dischargeMomentumBurn",
            "shatterBlockConvert",
            "dealDamageExecute",
            "dealDamagePerDamageTaken",
            "dealDamagePerRandomPlayed",
            "dealDamagePerIceBlock",
            "dealDamagePerHandCard",
            "counter",
        };

        public static bool IsDamageEffect(DeclarativeEffect effect) => DAMAGE_EFFECTS.Contains(effect.Effect);

        /// <summary>敵1体を対象に取る効果 (target:'all' を除く)。StS式ターゲティングの要否判定に使う</summary>
        private static readonly HashSet<string> ENEMY_TARGETED = new HashSet<string>
        {
            "dealDamage",
            "dealDamageRandom",
            "dealDamagePerCardPlayed",
            "dealDamagePerCardPlayedTotal",
            "dealDamagePerEnergyMax",
            "dealDamagePerMomentum",
            "dischargeAether",
            "applyBurn",
            "shatterBlock",
            "confuse",
            "exposeEnemy",
            "dischargeGrowth",
            "dealDamageCleave",
            "weakenEnemy",
            "dealDamagePerBlock",
            "dealDamagePerPermanent",
            "dealDamageDrain",
            "dealDamagePerExhaust",
            "dealDamageDrainPerExhaust",
            "dealDamagePerSelfHpLost",
            "dealDamagePerNegStrength",
            "dischargeBurn",
            "shatterBlockConvert",
            "dealDamageExecute",
            "dealDamagePerDamageTaken",
            "dealDamagePerRandomPlayed",
            "applyBurnPerDamageTaken",
            "dealDamagePerIceBlock",
            "dealDamagePerHandCard",
            "recycleExhaust",
            // 直接プレイ (死者再生): 選んだカードの単体対象効果を同じ対象に解決するため、対象を要求する
            "playFromExhaust",
        };

        /// <summary>
        /// このカードのプレイに対象指定 (targetIndex) が要るか。
        /// 生存敵が2体以上いる時、単体対象効果を含むカードは対象必須 (確定済みルール表「ターゲティング」)
        /// </summary>
        public static bool CardNeedsTarget(CardInstance card, int? modeIndex = null)
        {
            IReadOnlyList<CardMode> modes = card.Def.Modes ?? (IReadOnlyList<CardMode>)Array.Empty<CardMode>();
            IReadOnlyList<DeclarativeEffect> effects;
            if (modes.Count > 0 && modeIndex != null && modeIndex.Value >= 0 && modeIndex.Value < modes.Count)
            {
                effects = modes[modeIndex.Value].Effects;
            }
            else
            {
                effects = card.Def.Effects.Where(e => e.Trigger == "onPlay").ToList();
            }
            foreach (var e in effects)
                if (ENEMY_TARGETED.Contains(e.Effect) && e.Target != "all") return true;
            return false;
        }

        /// <summary>
        /// 置物の指定トリガー効果をすべて解決する。
        /// 置物は判断を挟まず自動で発火する (発動/温存の確認があるのは伏せカードのみ)。
        /// </summary>
        /// <param name="only">誘発させる置物を絞る (進軍の号令=従者だけ。null=全置物)</param>
        public static GameState RunPermanentTriggers(
            GameState state,
            string trigger,
            int enemyIndex,
            Func<CardInstance, bool>? only = null)
        {
            // 対象の敵が倒れていたら先頭の生存敵に読み替える (誘発ダメージの空撃ち防止)
            var at = EnemyAt(state, enemyIndex);
            int alive = at != null && at.Hp > 0 ? enemyIndex : FindAliveIndex(state.Enemies);
            // カードのプレイ中に誘発した置物の効果は「カードのプレイ」ではない:
            // 虚弱の25%減も勢いの加算も受けない (2026-09-05)
            bool prevCardPlay = state.ResolvingCardPlay == true;
            GameState s = state with { ResolvingCardPlay = false };
            // アンセム (白 2026-08-31): blessRetainers 持ち置物の合計ぶん、従者の量つき効果を底上げする
            int anthem = 0;
            foreach (var p in state.Player.Permanents)
                foreach (var e in p.Def.Effects)
                    if (e.Effect == "blessRetainers") anthem += e.Amount ?? 0;
            foreach (var permanent in state.Player.Permanents)
            {
                if (only != null && !only(permanent)) continue;
                foreach (var effect in permanent.Def.Effects)
                {
                    if (effect.Trigger == trigger && BlazeConditionMet(s, effect, enemyIndex))
                    {
                        var boosted =
                            anthem > 0 && permanent.Def.Retainer == true && effect.Amount != null
                                ? effect with { Amount = effect.Amount + anthem }
                                : effect;
                        // innate置物 (リーダーパッシブ・レリック) の解決中は鬼軍曹の怒りを立てない (2026-08-31)
                        bool isInnate = permanent.Innate == true;
                        GameState next = ResolveEffectTargeted(
                            isInnate ? s with { InnateResolving = true } : s,
                            boosted,
                            alive);
                        if (isInnate) next = next with { InnateResolving = false };
                        s = next;
                    }
                }
            }
            return s with { ResolvingCardPlay = prevCardPlay };
        }

        /// <summary>
        /// 回復を適用し、HpHealed を発行して onHealed 置物 (血の月・聖なる鐘) を誘発する。
        /// 過剰回復 (満タンで実回復0) でも誘発する (2026-08-31)。回復量0の効果は誘発しない。
        /// </summary>
        public static GameState HealPlayer(GameState state, int amount, int enemyIndex)
        {
            if (amount <= 0) return state;
            int healed = Math.Min(amount, state.Player.MaxHp - state.Player.Hp);
            GameState s = state with
            {
                Player = state.Player with
                {
                    Hp = state.Player.Hp + Math.Max(0, healed),
                    HealsThisCombat = state.Player.HealsThisCombat + 1, // 過剰回復も1回 (onHealedと同じ回数論)
                    // 白の回復参照 (healedThisTurn 2026-09-06): カードのプレイによる回復だけを数える
                    HealsThisTurn = (state.Player.HealsThisTurn ?? 0) + (state.ResolvingCardPlay == true ? 1 : 0),
                },
            };
            s = Events.Emit(s, new GameEvent_HpHealed { Amount = healed });
            return RunPermanentTriggers(s, "onHealed", enemyIndex);
        }

        /// <summary>
        /// 山札の上N枚を消滅させる (プレイヤーの忘却系カードと敵の山札喰い 'mill' が共用)。
        /// 亡骸 (onSelfExhausted)・onCardExhausted は発火する = ミルの既存則
        /// </summary>
        public static GameState MillPlayerDeck(GameState state, int amount, int enemyIndex)
        {
            int n = Math.Min(amount, state.Player.DrawPile.Count);
            if (n <= 0) return state;
            var milled = state.Player.DrawPile.Take(n).ToList();
            GameState s = state with
            {
                Player = state.Player with
                {
                    DrawPile = state.Player.DrawPile.Skip(n).ToList(),
                    ExhaustPile = state.Player.ExhaustPile.Concat(milled).ToList(),
                },
            };
            s = Events.Emit(s, new GameEvent_CardsMilled { Count = n, CardIds = milled.Select(c => c.Def.Id).ToList() });
            s = FireExhaustTriggers(s, n, enemyIndex);
            // 亡骸効果 (2026-08-31): ミルされた札の onSelfExhausted が発火する
            return FireNecroEffects(s, milled, enemyIndex);
        }

        /// <summary>
        /// ブロック獲得を適用し BlockGained を発行して onBlockGained 置物 (城壁の弩) を誘発する。
        /// 氷壁 (gainIceBlock) は別経路なので誘発しない = 青の柱④を侵さない。
        /// </summary>
        public static GameState GainPlayerBlock(GameState state, int amount, int enemyIndex)
        {
            if (amount <= 0) return state;
            // 虚弱 (2026-09-01 本家Frail相当): カードのプレイで得るブロックだけ25%減 (切り捨て・最低1)
            if (state.ResolvingCardPlay == true && state.Player.Frail > 0)
            {
                amount = Math.Max(1, (int)Math.Floor(amount * 0.75));
            }
            GameState s = state with { Player = state.Player with { Block = state.Player.Block + amount } };
            s = Events.Emit(s, new GameEvent_BlockGained { Target = "player", Amount = amount });
            s = AngerGuardWatchers(s);
            return RunPermanentTriggers(s, "onBlockGained", enemyIndex);
        }

        /// <summary>
        /// 鬼軍曹 (エリート 2026-08-31): プレイヤーが守りを得るたび強化+N。
        /// 通常ブロック・氷壁の両方に反応する (壁は普遍の状態量)
        /// </summary>
        private static GameState AngerGuardWatchers(GameState state)
        {
            // パッシブ・レリック由来の守り (innate解決中) には怒らない = プレイヤーに止める手段が無いため
            if (state.InnateResolving == true) return state;
            // 2026-09-06 ユーザー裁定: 怒るのは**カードのプレイ由来のブロックだけ**。さらに1枚のプレイで1回だけ
            if (state.ResolvingCardPlay != true || state.AngerFiredThisPlay == true) return state;
            bool anyWatcher = false;
            foreach (var e in state.Enemies)
                if (e.Hp > 0 && Content.GetEnemyDef(e.EnemyId).AngerOnBlock != null) { anyWatcher = true; break; }
            if (!anyWatcher) return state;
            GameState s = state with { AngerFiredThisPlay = true };
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                int? anger = Content.GetEnemyDef(s.Enemies[i].EnemyId).AngerOnBlock;
                if (s.Enemies[i].Hp > 0 && anger != null)
                {
                    int gain = anger.Value;
                    s = s with { Enemies = MapEnemy(s.Enemies, i, x => x with { Strength = x.Strength + gain }) };
                    s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = i, Amount = gain });
                }
            }
            return s;
        }

        /// <summary>氷壁の獲得 (青)。IceBlockGained を発行し、守りに反応する敵 (鬼軍曹) を怒らせる</summary>
        public static GameState GainPlayerIceBlock(GameState state, int amount)
        {
            if (amount <= 0) return state;
            GameState s = state with { Player = state.Player with { IceBlock = state.Player.IceBlock + amount } };
            s = Events.Emit(s, new GameEvent_IceBlockGained { Amount = amount });
            return AngerGuardWatchers(s);
        }

        /// <summary>カード効果によるHP損失。selfHpLost に累積し onHpLost 置物 (苦痛の芯) を誘発する</summary>
        public static GameState LosePlayerHp(GameState state, int amount, int enemyIndex)
        {
            if (amount <= 0) return state;
            GameState s = state with
            {
                Player = state.Player with
                {
                    Hp = state.Player.Hp - amount,
                    SelfHpLost = state.Player.SelfHpLost + amount,
                },
            };
            s = Events.Emit(s, new GameEvent_HpLost { Amount = amount });
            return RunPermanentTriggers(s, "onHpLost", enemyIndex);
        }

        /// <summary>消滅の誘発 (亡者の合唱): カードが消滅する「たび」= 1枚につき1回発火する</summary>
        public static GameState FireExhaustTriggers(GameState state, int count, int enemyIndex)
        {
            GameState s = state;
            for (int i = 0; i < count; i++)
            {
                s = RunPermanentTriggers(s, "onCardExhausted", enemyIndex);
            }
            return s;
        }

        /// <summary>
        /// 亡骸効果 (黒 2026-08-31): プレイ以外の経路 (ミル・消滅コスト・衝動失効) で消滅した札の
        /// onSelfExhausted 効果を発火する。発火順は「置物の消滅誘発 → 亡骸」で呼び出し側が揃える
        /// </summary>
        public static GameState FireNecroEffects(GameState state, IReadOnlyList<CardInstance> cards, int enemyIndex)
        {
            GameState s = state;
            foreach (var card in cards)
            {
                var necro = card.Def.Effects.Where(e => e.Trigger == "onSelfExhausted").ToList();
                if (necro.Count == 0) continue;
                s = Events.Emit(s, new GameEvent_NecroFired { CardId = card.Def.Id });
                foreach (var effect in necro)
                {
                    // 対象は現在の生存先頭 (ミルは対象を取らない自動誘発。全体効果はそのまま全体解決)
                    var at = EnemyAt(s, enemyIndex);
                    int idx = at != null && at.Hp > 0 ? enemyIndex : Math.Max(0, FindAliveIndex(s.Enemies));
                    s = ResolveEffectTargeted(s, effect, idx);
                }
            }
            return s;
        }

        /// <summary>効果1つを対象規則に従って解決する。target:'all' は生存する敵全体に順に解決</summary>
        public static GameState ResolveEffectTargeted(GameState state, DeclarativeEffect effect, int enemyIndex)
        {
            if (effect.Target != "all") return ResolveEffect(state, effect, enemyIndex);
            GameState s = state;
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                if (s.Enemies[i].Hp > 0) s = ResolveEffect(s, effect, i);
            }
            return s;
        }

        /// <summary>この誘発窓でカードが発動できるか (トリガー一致 + 追加条件)</summary>
        public static bool ReactionMatches(GameState state, CardInstance card, ReactionWindow win)
        {
            // 全カード伏せ可 (実験): 通常カードはトリガーを窓に差し替えた効果列で照合する
            foreach (var e in SetAny.SetEffectsOf(card))
            {
                bool triggerMatches =
                    win.Stage == "pre"
                        ? e.Trigger == "onEnemyAction" ||
                          (e.Trigger == "onAttackIncoming" && win.Kind == "attack")
                          // 逃がしルールは廃止 (2026-08-30 A2)。破壊されそうな札は回収 (1E) で事前に引き上げる
                        : (e.Trigger == "onAttacked" && win.Kind == "attack") ||
                          (e.Trigger == "onEnemyBuffed" && (win.Kind == "buff" || win.Kind == "rally")) ||
                          (e.Trigger == "onEnemyDefended" && win.Kind == "defend");
                if (!triggerMatches) continue;
                var c = e.Condition;
                if (c == null) return true;
                if (c.HpAtOrBelowRatio != null && state.Player.Hp > state.Player.MaxHp * c.HpAtOrBelowRatio.Value) continue;
                if (c.MinDamageTaken != null && (win.Stage != "post" || win.HpLoss < c.MinDamageTaken.Value)) continue;
                if (c.MaxActionValue != null && (win.Stage != "pre" || win.Actual > c.MaxActionValue.Value)) continue;
                // minActionValue は pre/post 両窓で判定する (2026-08-31)
                if (c.MinActionValue != null && win.Actual < c.MinActionValue.Value) continue;
                if (c.Blaze == true && !IsBlazing(state)) continue;
                if (c.MinGrowth != null && state.Player.Growth < c.MinGrowth.Value) continue;
                if (c.MinMomentum != null && state.Player.Momentum < c.MinMomentum.Value) continue;
                if (c.MinEnergyMax != null && state.Player.EnergyMaxAtTurnStart < c.MinEnergyMax.Value) continue;
                // 行動種別の条件 (共鳴する茨 2026-09-07): 強化・応援だけを打ち消す限定リアクション
                if (c.ActionKinds != null && !c.ActionKinds.Contains(win.Kind)) continue;
                if (c.HealedThisTurn == true && (state.Player.HealsThisTurn ?? 0) <= 0) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// この敵の伏せ分岐が見切り (setFresh) を無視するか = 破壊分岐・罰型。
        /// effectiveIntent の判定と表示層 (CLI/UI の「見切られ」タグ) が同じ述語を読む
        /// </summary>
        public static bool SetReactionIgnoresFreshness(GameState state, int enemyIndex)
        {
            var enemy = EnemyAt(state, enemyIndex);
            var intent = enemy?.Intent;
            if (enemy == null || intent == null || intent.ConditionalOn != "set" || intent.Alt == null) return false;
            return intent.Alt.Kind == "destroy-set"
                || intent.Alt.IgnoreFreshness == true
                || Content.GetEnemyDef(enemy.EnemyId).VsSetIgnoreFreshness == true;
        }

        /// <summary>
        /// 条件付き意図の解決 (確定済みルール表「条件付き意図」)。
        /// 反応テーブルを持つ敵は宣言時に両分岐を確定しており、**実行時の盤面**でどちらになるかが決まる。
        /// </summary>
        public static EnemyIntent? EffectiveIntent(GameState state, int enemyIndex)
        {
            var intent = EnemyAt(state, enemyIndex)?.Intent;
            if (intent == null) return null;
            if (intent.ConditionalOn == null || intent.Alt == null) return intent;
            bool met;
            if (intent.ConditionalOn == "set")
            {
                // 見切り (2026-08-30 A2): 敵の伏せ反応は**そのターンに伏せられた札**にだけ反応する。
                // ただし破壊 (destroy-set) は鮮度を問わない
                met = SetReactionIgnoresFreshness(state, enemyIndex)
                    ? state.Player.SetCards.Count > 0
                    : state.Player.SetCards.Any(c => c.SetFresh == true);
            }
            else
            {
                met = HasHuntableTokens(state);
            }
            if (!met) return intent;
            // TS: { ...intent.alt, conditionalOn, alt } — alt は EnemyIntentBranch なので mirrorHits は引き継がない
            return new EnemyIntent
            {
                Kind = intent.Alt.Kind,
                ShownMin = intent.Alt.ShownMin,
                ShownMax = intent.Alt.ShownMax,
                Actual = intent.Alt.Actual,
                Hits = intent.Alt.Hits,
                Inflict = intent.Alt.Inflict,
                AlsoDefend = intent.Alt.AlsoDefend,
                AlsoBuff = intent.Alt.AlsoBuff,
                ConditionalOn = intent.ConditionalOn,
                Alt = intent.Alt,
            };
        }

        /// <summary>
        /// 確認ウィンドウで「発動すると伏せ枠が空き、後続の敵の条件付き分岐が『伏せなし』側に化ける」
        /// 警告を出すべきか (2026-08-28)。該当する後続の敵の index リストを返す (空なら警告不要)。
        /// </summary>
        public static IReadOnlyList<int> SetBranchFlipRisks(GameState state)
        {
            var pending = state.PendingWindow;
            if (pending == null || state.Player.SetCards.Count != 1) return Array.Empty<int>();
            var risks = new List<int>();
            for (int i = pending.EnemyIndex + 1; i < state.Enemies.Count; i++)
            {
                var e = state.Enemies[i];
                if (e.Hp > 0 && e.Intent?.ConditionalOn == "set" && e.Intent.Alt != null) risks.Add(i);
            }
            return risks;
        }

        /// <summary>
        /// 置物数参照 (集結・隊列の盾・大行進・聖騎士団の突撃) が数える置物の数。
        /// リーダーパッシブとレリックは戦闘開始時から場にある = 「登場」していないので数えない (2026-08-26)
        /// </summary>
        public static int CountedPermanents(GameState state)
        {
            int n = 0;
            foreach (var p in state.Player.Permanents) if (p.Innate != true) n++;
            return n;
        }

        /// <summary>
        /// 従者狩り (destroy-token) の対象になる置物が場にあるか。
        /// 道具・オーラ系置物とリーダーパッシブ・レリックは対象外
        /// </summary>
        public static bool HasHuntableTokens(GameState state)
        {
            foreach (var p in state.Player.Permanents)
                if (p.Token == true || p.Def.Retainer == true) return true;
            return false;
        }

        /// <summary>
        /// 致死状態 (HP&lt;=0) で確認窓を開く価値がある札か。
        /// 回復を伴わない札では生き延びられないので、窓を開いても「もう詰んでいるのに聞かれる」だけになる
        /// </summary>
        public static bool CanSaveFromLethal(CardInstance card, GameState? state = null)
        {
            // 回復量の上限見積もり: gainHp=満額 / ドレイン=与ダメの半分 (敵HPでのクランプは見ない=上限)
            int healCap = 0;
            foreach (var e in card.Def.Effects)
            {
                if (e.Effect == "gainHp") { healCap += e.Amount ?? 0; continue; }
                if (e.Effect == "dealDamageDrain") { healCap += (int)Math.Floor((e.Amount ?? 0) / 2.0); continue; }
                if (e.Effect == "dealDamageDrainPerExhaust")
                {
                    int n = state?.Player.ExhaustPile.Count ?? 0;
                    healCap += (int)Math.Floor(((e.Amount ?? 0) * (double)n) / 2.0);
                    continue;
                }
            }
            if (healCap <= 0) return false;
            // 回復量が不足分に届かない札は「救えないのに聞かれる」だけ (2026-08-31 黒Opusラン指摘)
            if (state != null && state.Player.Hp <= 0)
            {
                return healCap >= 1 - state.Player.Hp; // 発動後にHP1以上へ届くこと
            }
            return true;
        }

        /// <summary>その窓で実際に発動できる伏せ札 (致死状態では不足分を実際に埋められる回復札だけ)</summary>
        public static IReadOnlyList<CardInstance> UsableSetCards(GameState state, ReactionWindow win)
        {
            // 全カード伏せ可 (実験): 通常カードは発動時に印字コストを払うので、払えない札は候補に出さない
            var matched = state.Player.SetCards
                .Where(c => ReactionMatches(state, c, win) && SetAny.SetFireCost(c) <= state.Player.Energy)
                .ToList();
            return state.Player.Hp <= 0
                ? matched.Where(c => CanSaveFromLethal(c, state)).ToList()
                : (IReadOnlyList<CardInstance>)matched;
        }

        /// <summary>窓に合致するが発動コストを払えない伏せ札 (全カード伏せ可の通常札)</summary>
        public static IReadOnlyList<CardInstance> UnaffordableSetCards(GameState state, ReactionWindow win)
        {
            return state.Player.SetCards
                .Where(c => ReactionMatches(state, c, win) && SetAny.SetFireCost(c) > state.Player.Energy)
                .ToList();
        }

        /// <summary>
        /// このプレイヤーは今後この戦闘で伏せられるか (2026-08-30)。
        /// 消滅置き場は数えない (リアクションは死者再生の対象外 = 戻ってこない)
        /// </summary>
        public static bool PlayerCanSet(GameState state)
        {
            var zones = new IReadOnlyList<CardInstance>[]
            {
                state.Player.Hand,
                state.Player.DrawPile,
                state.Player.DiscardPile,
                state.Player.SetCards,
            };
            foreach (var z in zones)
                foreach (var c in z)
                    if (c.Def.Type == CardTypes.Reaction) return true;
            return false;
        }

        /// <summary>現在の中断状態 (pendingWindow) から誘発窓を復元する</summary>
        public static ReactionWindow? WindowFromPending(GameState state)
        {
            var pending = state.PendingWindow;
            if (pending == null) return null;
            var intent = EffectiveIntent(state, pending.EnemyIndex);
            if (intent == null) return null;
            if (pending.Stage == "pre")
            {
                return new ReactionWindow { Stage = "pre", Kind = intent.Kind, Actual = intent.Actual };
            }
            return new ReactionWindow
            {
                Stage = "post",
                Kind = intent.Kind,
                HpLoss = state.LastAction?.HpLoss ?? 0,
                Actual = intent.Actual,
            };
        }

        /// <summary>
        /// プレイヤー側の与ダメージ補正 (成長・勢い・弱体) を適用した値。
        /// ドレインの回復量など「与えたダメージを参照する効果」はこの値を基準にする
        /// </summary>
        public static int PlayerDamageAfterModifiers(GameState state, int baseAmount)
        {
            // 成長は与ダメ全てに乗る。勢いは「以降の攻撃ダメージ」= カードのプレイで与えるダメージだけに乗る (2026-09-05)
            int momentum = state.ResolvingCardPlay == true ? state.Player.Momentum : 0;
            int amount = baseAmount + state.Player.Growth + momentum;
            // 敵フェーズ中に付いた弱体は、その同じフェーズの返しには乗らない (次の自ターンから。2026-09-04)
            int weak = state.Phase == CombatPhases.PlayerTurn
                ? state.Player.Weak
                : state.Player.Weak - (state.Player.WeakFreshThisPhase ?? 0);
            if (weak <= 0 || amount <= 0) return amount;
            // 弱体は25%減 (切り捨て)。ただし1以上の攻撃が0にはならない
            return Math.Max(1, (int)Math.Floor(amount * 0.75));
        }

        /// <summary>
        /// カードホバー用のダメージ内訳 (2026-09-01)。dealDamageToEnemy と同じ手順を数字だけで辿る純関数。
        /// 敵が倒れていれば null
        /// </summary>
        public static DamageBreakdown? DamageBreakdownOf(
            GameState state,
            int enemyIndex,
            int baseAmount,
            bool pierce = false,
            bool applyExpose = true)
        {
            var enemy = EnemyAt(state, enemyIndex);
            if (enemy == null || enemy.Hp <= 0) return null;
            var p = state.Player;
            var steps = new List<DamageBreakdownStep> { new DamageBreakdownStep { Label = "基礎", Value = baseAmount } };
            int amount = baseAmount;
            if (p.Growth > 0)
            {
                amount += p.Growth;
                steps.Add(new DamageBreakdownStep { Label = $"成長+{p.Growth}", Value = amount });
            }
            // 手札のホバー = カードのプレイの見積り。勢いはカードプレイのダメージだけに乗る (2026-09-05)
            if (p.Momentum > 0)
            {
                amount += p.Momentum;
                steps.Add(new DamageBreakdownStep { Label = $"勢い+{p.Momentum}", Value = amount });
            }
            if (p.Weak > 0 && amount > 0)
            {
                int w = Math.Max(1, (int)Math.Floor(amount * 0.75));
                if (w != amount)
                {
                    amount = w;
                    steps.Add(new DamageBreakdownStep { Label = "弱体-25%", Value = amount });
                }
            }
            if (applyExpose && enemy.Exposed > 0)
            {
                amount = (int)Math.Floor(amount * 1.5);
                steps.Add(new DamageBreakdownStep { Label = "急所×1.5", Value = amount });
            }
            if (enemy.Armor != null && amount > enemy.Armor.Value)
            {
                amount = enemy.Armor.Value;
                steps.Add(new DamageBreakdownStep { Label = $"装甲上限{enemy.Armor}", Value = amount });
            }
            // 潜伏 (2026-09-05): 殻は貫通も吸い、尽きるまでHPに通らない
            bool shellUp = enemy.BurrowActive == true && enemy.Block > 0;
            int blocked = pierce && !shellUp ? 0 : Math.Min(enemy.Block, amount);
            if (shellUp) steps.Add(new DamageBreakdownStep { Label = $"潜伏の殻-{blocked}(HPには通らない)", Value = 0 });
            else if (blocked > 0) steps.Add(new DamageBreakdownStep { Label = $"敵ブロック-{blocked}", Value = amount - blocked });
            if (!shellUp && pierce && enemy.Block > 0) steps.Add(new DamageBreakdownStep { Label = "貫通(ブロック無視)", Value = amount });
            int hpLoss = shellUp ? 0 : amount - blocked;
            // 因縁 (無形ターン): 1ヒットのHP損失が1に固定
            if (Content.GetEnemyDef(enemy.EnemyId).Nemesis == true && IsIntangibleTurn(state) && hpLoss > 1)
            {
                hpLoss = 1;
                steps.Add(new DamageBreakdownStep { Label = "無形=1固定", Value = 1 });
            }
            // ターン装甲 (2026-09-02): このターンのHP損失累計の上限
            int? turnArmor = Content.GetEnemyDef(enemy.EnemyId).TurnArmor;
            if (turnArmor != null)
            {
                int remaining = Math.Max(0, turnArmor.Value - (enemy.DamageThisTurn ?? 0));
                if (hpLoss > remaining)
                {
                    hpLoss = remaining;
                    steps.Add(new DamageBreakdownStep { Label = $"ターン装甲(残り{remaining})", Value = hpLoss });
                }
            }
            return new DamageBreakdown { Steps = steps, Blocked = blocked, HpLoss = hpLoss };
        }

        /// <summary>威圧 (敵版弱体 2026-09-03): スタックがあれば与ダメ-25% (切り捨て・最低1)</summary>
        public static int ApplyEnemyWeak(int value, int? weak)
        {
            return (weak ?? 0) > 0 ? Math.Max(1, (int)Math.Floor(value * 0.75)) : value;
        }

        /// <summary>因縁 (Nemesis) の無形ターンか: 奇数ターン (1,3,5…) は無形、偶数ターンに実体化</summary>
        public static bool IsIntangibleTurn(GameState state) => state.Turn % 2 == 1;

        /// <summary>
        /// 潜伏 (Burrowed) の殻が割れたか: 殻 (block) が0になった瞬間に潜伏を解き、次の行動を噛みつきに差し替える。
        /// </summary>
        public static GameState BreakBurrowIfCracked(GameState state, int enemyIndex)
        {
            var e = EnemyAt(state, enemyIndex);
            if (e == null || e.BurrowActive != true || e.Block > 0 || e.Hp <= 0) return state;
            var enemies = MapEnemy(state.Enemies, enemyIndex, x => x with { BurrowActive = false, BiteNext = true });
            return Events.Emit(state with { Enemies = enemies }, new GameEvent_BurrowBroken { EnemyIndex = enemyIndex });
        }

        /// <summary>
        /// 被弾覚醒 (2026-09-02 本家Lagavulin準拠): 累計HP損失がしきい値に達したら眠りの前奏を打ち切り、
        /// ローテを resumeAt へ。宣言済みの意図は変えない (宣言時固定則)
        /// </summary>
        public static GameState ApplyWakeCheck(GameState state, int enemyIndex)
        {
            var e = EnemyAt(state, enemyIndex);
            if (e == null || e.Hp <= 0 || e.Woken == true) return state;
            var wake = Content.GetEnemyDef(e.EnemyId).WakeOnDamage;
            if (wake == null) return state;
            if ((e.DamageTakenTotal ?? 0) < wake.Damage || e.PatternIndex >= wake.ResumeAt) return state;
            var enemies = MapEnemy(state.Enemies, enemyIndex, x => x with { PatternIndex = wake.ResumeAt, Woken = true });
            return Events.Emit(state with { Enemies = enemies }, new GameEvent_EnemyWoken { EnemyIndex = enemyIndex });
        }

        /// <summary>
        /// アーティファクト (2026-09-02 本家Artifact): デバフ付与をN回無効化。弾いたら true を返し、
        /// 呼び出し側は効果を解決しない。延焼は対象外
        /// </summary>
        public static (GameState State, bool Blocked) TryArtifactBlock(GameState state, int enemyIndex, string effectName)
        {
            var e = EnemyAt(state, enemyIndex);
            if (e == null || e.Hp <= 0 || (e.Artifact ?? 0) <= 0) return (state, false);
            var enemies = MapEnemy(state.Enemies, enemyIndex, x => x with { Artifact = (x.Artifact ?? 0) - 1 });
            return (Events.Emit(state with { Enemies = enemies },
                new GameEvent_ArtifactBlocked { EnemyIndex = enemyIndex, Effect = effectName }), true);
        }

        /// <summary>
        /// プレイヤーの与ダメージ処理。成長カウンターと勢いを加算する。
        /// 敵ブロックで軽減。pierce (貫通/トランプル) は敵ブロックを無視する。
        /// </summary>
        public static GameState DealDamageToEnemy(
            GameState state,
            int enemyIndex,
            int baseAmount,
            bool pierce = false,
            bool applyExpose = true)
        {
            int amount = PlayerDamageAfterModifiers(state, baseAmount);
            var enemy = EnemyAt(state, enemyIndex);
            if (enemy == null || enemy.Hp <= 0) return state;
            // 急所 (敵版脆弱): 次に受けるダメージN回が+50%。ブロック変換には乗せない (2026-08-31)
            bool exposed = applyExpose && enemy.Exposed > 0;
            if (exposed) amount = (int)Math.Floor(amount * 1.5);
            // 装甲 (2026-08-30): 1ヒットの被ダメはN以下に頭打ち
            int armorCut = enemy.Armor != null && amount > enemy.Armor.Value ? amount - enemy.Armor.Value : 0;
            if (armorCut > 0) amount = enemy.Armor!.Value;
            // 潜伏 (2026-09-03 本家 Burrowed): 殻が尽きるまでHPにダメージが通らない。貫通も殻に吸われる
            bool shellUp = enemy.BurrowActive == true && enemy.Block > 0;
            int blocked = pierce && !shellUp ? 0 : Math.Min(enemy.Block, amount);
            bool burrowed = shellUp;
            int hpLoss = burrowed ? 0 : amount - blocked;
            int burrowCut = burrowed ? amount - blocked : 0;
            // 因縁 (2026-09-03 本家 Nemesis): 無形ターンは1ヒットのHP損失が1に固定
            int nemesisCut = 0;
            if (Content.GetEnemyDef(enemy.EnemyId).Nemesis == true && IsIntangibleTurn(state) && hpLoss > 1)
            {
                nemesisCut = hpLoss - 1;
                hpLoss = 1;
            }
            // ターン装甲 (2026-09-02 StS2 HardenedShell式): このターンのHP損失累計はN以下
            int? turnArmor = Content.GetEnemyDef(enemy.EnemyId).TurnArmor;
            int turnArmorCut = 0;
            if (turnArmor != null)
            {
                int remaining = Math.Max(0, turnArmor.Value - (enemy.DamageThisTurn ?? 0));
                if (hpLoss > remaining)
                {
                    turnArmorCut = hpLoss - remaining;
                    hpLoss = remaining;
                }
            }
            int blockedF = blocked, hpLossF = hpLoss;
            bool exposedF = exposed;
            var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with
            {
                Block = e.Block - blockedF,
                Hp = e.Hp - hpLossF,
                Exposed = exposedF ? e.Exposed - 1 : e.Exposed,
                // regenBreak の判定用。再生判定のたびにリセットされる
                HpLostSinceRegen = (e.HpLostSinceRegen ?? 0) + hpLossF,
                DamageTakenTotal = (e.DamageTakenTotal ?? 0) + hpLossF,
                DamageThisTurn = (e.DamageThisTurn ?? 0) + hpLossF,
            });
            GameState s = Events.Emit(
                state with { Enemies = enemies },
                new GameEvent_DamageDealt
                {
                    Source = "player",
                    Amount = amount,
                    HpLoss = hpLoss,
                    EnemyIndex = enemyIndex,
                    ArmorCut = armorCut > 0 ? armorCut : (int?)null,
                    TurnArmorCut = turnArmorCut > 0 ? turnArmorCut : (int?)null,
                    BurrowCut = burrowCut > 0 ? burrowCut : (int?)null,
                    NemesisCut = nemesisCut > 0 ? nemesisCut : (int?)null,
                });
            s = ApplyWakeCheck(s, enemyIndex);
            s = BreakBurrowIfCracked(s, enemyIndex);
            // 激昂の与ダメ併用 (2026-08-30): 累計被ダメが enrageEveryDamage の倍数の壁を跨ぐたび強化
            {
                var struck0 = s.Enemies[enemyIndex];
                var defE = Content.GetEnemyDef(struck0.EnemyId);
                if (defE.EnrageEveryDamage != null && struck0.Hp > 0 && hpLoss > 0)
                {
                    int before = (struck0.DamageTakenTotal ?? 0) - hpLoss;
                    int crossings =
                        (int)Math.Floor((struck0.DamageTakenTotal ?? 0) / (double)defE.EnrageEveryDamage.Value) -
                        (int)Math.Floor(before / (double)defE.EnrageEveryDamage.Value);
                    int gain = crossings * (defE.Enrage ?? 2);
                    if (gain > 0)
                    {
                        s = s with { Enemies = MapEnemy(s.Enemies, enemyIndex, e => e with { Strength = e.Strength + gain }) };
                        s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = enemyIndex, Amount = gain, Reason = "enrage-damage" });
                    }
                }
            }
            // とげ (敵の報復): 攻撃ヒットごとにNダメ反射。そのヒットで倒れたら反射しない
            var struck = s.Enemies[enemyIndex];
            if ((struck.Thorns ?? 0) > 0 && struck.Hp > 0)
            {
                int reflect = struck.Thorns!.Value;
                int pBlocked = Math.Min(s.Player.Block, reflect);
                int pIceBlocked = Math.Min(s.Player.IceBlock, reflect - pBlocked);
                int pHpLoss = reflect - pBlocked - pIceBlocked;
                s = s with
                {
                    Player = s.Player with
                    {
                        Block = s.Player.Block - pBlocked,
                        IceBlock = s.Player.IceBlock - pIceBlocked,
                        Hp = s.Player.Hp - pHpLoss,
                    },
                };
                s = Events.Emit(s, new GameEvent_ThornsReflected { EnemyIndex = enemyIndex, Amount = reflect, HpLoss = pHpLoss });
            }
            return s;
        }

        /// <summary>山札から n 枚ドロー。山札が尽きたら捨て札をシャッフルして山札に戻す (StS準拠)</summary>
        public static GameState DrawCards(GameState state, int n)
        {
            var drawPile = new List<CardInstance>(state.Player.DrawPile);
            var discardPile = new List<CardInstance>(state.Player.DiscardPile);
            var rng = state.Rng;
            var drawn = new List<CardInstance>();
            for (int i = 0; i < n; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0) break;
                    var (reshuffled, nextRng) = Rng.Shuffle(rng, discardPile);
                    drawPile = new List<CardInstance>(reshuffled);
                    discardPile = new List<CardInstance>();
                    rng = nextRng;
                }
                drawn.Add(drawPile[0]);
                drawPile.RemoveAt(0);
            }
            GameState next = state with
            {
                Rng = rng,
                Player = state.Player with
                {
                    DrawPile = drawPile,
                    DiscardPile = discardPile,
                    Hand = state.Player.Hand.Concat(drawn).ToList(),
                },
            };
            return drawn.Count > 0
                ? Events.Emit(next, new GameEvent_CardsDrawn { Count = drawn.Count, Cards = drawn.Select(c => c.Def.Name).ToList() })
                : next;
        }

        /// <summary>
        /// 宣言的効果1つを解決する。enemyIndex は対象の敵。
        /// リアクション効果 (counter / negate) もここで解決される。
        /// </summary>
        public static GameState ResolveEffect(GameState state, DeclarativeEffect effect, int enemyIndex)
        {
            // 忘却の刻 (黒のしきい値): 消滅置き場が exhaustThreshold 枚以上なら amountMax に切り替わる
            if (effect.ExhaustThreshold != null && state.Player.ExhaustPile.Count >= effect.ExhaustThreshold.Value)
            {
                effect = effect with { Amount = effect.AmountMax };
            }
            switch (effect.Effect)
            {
                case "dealDamage":
                    // growthMultiplier (大牙=Heavy Blade): 基礎に成長×(N-1) を足す = 成長×N
                    return DealDamageToEnemy(
                        state,
                        enemyIndex,
                        (effect.Amount ?? 0)
                            + (effect.GrowthMultiplier != null ? (int)(state.Player.Growth * (effect.GrowthMultiplier.Value - 1)) : 0)
                            // momentumMultiplier (猛進の角): 勢い×(N-1) を足す = 勢い×N
                            + (effect.MomentumMultiplier != null ? (int)(state.Player.Momentum * (effect.MomentumMultiplier.Value - 1)) : 0),
                        effect.Pierce == true);
                case "dealDamagePerAttackPlayed":
                    // 攻撃数参照 (薙ぎ払い=Conflagration): このターンにプレイした攻撃 (自身は数えない) × amount
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * (state.Player.AttacksPlayedThisTurn ?? 0), effect.Pierce == true);
                case "dealDamagePerHeal":
                    // 回復の換金 (黒 2026-09-01): この戦闘で回復した回数×X (滾る血汐。過剰回復も数える)
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.HealsThisCombat, effect.Pierce == true);
                case "dealDamagePerMomentum":
                    // トランプルの換金 (2026-08-29): 勢い × amount のダメージ。勢いは消費しない
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.Momentum, effect.Pierce == true);
                case "dealDamagePerEnergyMax":
                    // ビッグマナのシグネチャー: エナジー上限 × amount。ターン開始時の上限を読む (2026-08-30)
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.EnergyMaxAtTurnStart, effect.Pierce == true);
                case "counter":
                {
                    // 返しダメージ: 行動してきた敵へのダメージ。成長は乗るが勢いは乗らない
                    return DealDamageToEnemy(state, enemyIndex, effect.Amount ?? 0, effect.Pierce == true);
                }
                case "gainEnergy":
                {
                    // 一時マナ: ターン終了までエナジー+X (energyMax は増えない)
                    int amount = effect.Amount ?? 0;
                    var next = state with { Player = state.Player with { Energy = state.Player.Energy + amount } };
                    return Events.Emit(next, new GameEvent_EnergyGained { Amount = amount });
                }
                case "addMomentum":
                {
                    int amount = effect.Amount ?? 0;
                    var next = state with { Player = state.Player with { Momentum = state.Player.Momentum + amount } };
                    var emitted = Events.Emit(next, new GameEvent_MomentumAdded { Amount = amount });
                    return amount > 0 ? FireGainTrigger(emitted, "onMomentumGained", enemyIndex) : emitted;
                }
                case "gainBlock":
                {
                    int amount = effect.Amount ?? 0;
                    return GainPlayerBlock(state, amount, enemyIndex);
                }
                case "gainIceBlock":
                    // 氷壁 (青): ターン開始で消えず持ち越されるブロック
                    return GainPlayerIceBlock(state, effect.Amount ?? 0);
                case "dealDamagePerCardPlayedTotal":
                    // 大津波 (青 2026-08-31): この戦闘の累計プレイ数 × amount
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.CardsPlayedTotal, effect.Pierce == true);
                case "dealDamagePerCardPlayed":
                    // ストーム攻撃 (青): 詠唱数 × amount のダメージ
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.CardsPlayedThisTurn, effect.Pierce == true);
                case "gainIceBlockPerCardPlayed":
                    // ストーム防御 (青): 詠唱数 × amount の氷壁
                    return GainPlayerIceBlock(state, (effect.Amount ?? 0) * state.Player.CardsPlayedThisTurn);
                case "drawCardsPerCardPlayed":
                    // ストームドロー (青): 詠唱数 × amount 枚ドロー
                    return DrawCards(state, (effect.Amount ?? 0) * state.Player.CardsPlayedThisTurn);
                case "dealDamagePerHandCard":
                    // 抱え込み (青 2026-08-31): 手札の枚数 × amount のダメージ
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.Hand.Count, effect.Pierce == true);
                case "gainIceBlockPerHandCard":
                    // 抱え込み (青): 手札の枚数 × amount の氷壁
                    return GainPlayerIceBlock(state, (effect.Amount ?? 0) * state.Player.Hand.Count);
                case "blessRetainers":
                    // アンセム (白): 常在の静的効果。runPermanentTriggers が読むだけ (登場時のno-op)
                    return state;
                case "addCasts":
                    // 焚べる (青): 詠唱数+N。cardsPlayedTotal (激昂タイマー) には数えない
                    return state with
                    {
                        Player = state.Player with
                        {
                            CardsPlayedThisTurn = state.Player.CardsPlayedThisTurn + (effect.Amount ?? 0),
                        },
                    };
                case "addSpellEcho":
                    // 反復 (青): 次に唱える呪文の効果を2回解決するトークン。消費は combat.ts の playCard 側
                    return state with
                    {
                        Player = state.Player with { SpellEchoes = state.Player.SpellEchoes + (effect.Amount ?? 0) },
                    };
                case "addAether":
                {
                    // 霊気 (青): 妨害・リアクション成功の蓄積。獲得の誘発 (静電の帳) が乗る
                    int amount = effect.Amount ?? 0;
                    GameState s = state with { Player = state.Player with { Aether = state.Player.Aether + amount } };
                    s = Events.Emit(s, new GameEvent_AetherGained { Amount = amount });
                    return RunPermanentTriggers(s, "onAetherGained", enemyIndex);
                }
                case "applyBurn":
                {
                    // 延焼 (赤): 敵に蓄積する継続ダメージ
                    int amount = effect.Amount ?? 0;
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0) return state;
                    bool wasBlazing = IsBlazing(state);
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Burn = e.Burn + amount });
                    GameState s = Events.Emit(state with { Enemies = enemies }, new GameEvent_BurnApplied { EnemyIndex = enemyIndex, Amount = amount });
                    // 猛り火の点火瞬間 (2026-08-31): しきい値を跨いだ瞬間に、そのターンぶんの
                    // ターン開始・猛り火効果を1回発火する = 「点いた瞬間から守られる」
                    if (!wasBlazing && IsBlazing(s) && s.Phase == CombatPhases.PlayerTurn)
                    {
                        var perms = s.Player.Permanents;
                        foreach (var permanent in perms)
                        {
                            foreach (var pe in permanent.Def.Effects)
                            {
                                if (pe.Trigger == "onTurnStart" && pe.Condition?.Blaze == true)
                                {
                                    s = ResolveEffectTargeted(s, pe, enemyIndex);
                                }
                            }
                        }
                    }
                    return s;
                }
                case "confuse":
                {
                    // 混乱 (青): 敵の攻撃が他の生存敵 (いなければ自分) に向かう
                    int amount = effect.Amount ?? 0;
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0) return state;
                    {
                        var (sa, blocked) = TryArtifactBlock(state, enemyIndex, "confuse");
                        if (blocked) return sa;
                    }
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Confusion = e.Confusion + amount });
                    return Events.Emit(state with { Enemies = enemies }, new GameEvent_EnemyConfused { EnemyIndex = enemyIndex, Amount = amount });
                }
                case "gainHp":
                    // 回復 (白の専売)。実回復>0 なら onHealed 置物が誘発する
                    return HealPlayer(state, effect.Amount ?? 0, enemyIndex);
                case "strengthenEnemy":
                {
                    // 敵の筋力+N (2026-09-03 ボスレリック「賢者の石」の代償)。アーティファクトは弾かない
                    int amount = effect.Amount ?? 0;
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0 || amount == 0) return state;
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Strength = e.Strength + amount });
                    return Events.Emit(state with { Enemies = enemies }, new GameEvent_StrengthGained { EnemyIndex = enemyIndex, Amount = amount });
                }
                case "weakenEnemy":
                {
                    // 威圧 (白): 2026-09-03 本家 Weak 化。次の amount 回の攻撃行動の与ダメ-25% のスタックを積む
                    int amount = effect.Amount ?? 0;
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0) return state;
                    {
                        var (sa, blocked) = TryArtifactBlock(state, enemyIndex, "weakenEnemy");
                        if (blocked) return sa;
                    }
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Weak = (e.Weak ?? 0) + amount });
                    return Events.Emit(state with { Enemies = enemies }, new GameEvent_EnemyWeakened { EnemyIndex = enemyIndex, Amount = amount });
                }
                case "dealDamagePerBlock":
                {
                    // 要塞型ペイオフ: 現在のブロック×X
                    var dealt = DealDamageToEnemy(
                        state,
                        enemyIndex,
                        (effect.Amount ?? 0) * state.Player.Block,
                        effect.Pierce == true,
                        false); // 急所はブロック変換に乗らない (2026-08-31)
                    // spendBlock: 壁を売り払う
                    return effect.SpendBlock == true ? dealt with { Player = dealt.Player with { Block = 0 } } : dealt;
                }
                case "dealDamagePerPermanent":
                    // 集結 (白): 置物の数×X (リーダーパッシブ・レリックは数えない)
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * CountedPermanents(state), effect.Pierce == true);
                case "dealDamageDrain":
                {
                    // ドレイン (黒の専売): Xダメージ + floor(X/2)回復
                    int amount = effect.Amount ?? 0;
                    int dealt = PlayerDamageAfterModifiers(state, amount);
                    var s = DealDamageToEnemy(state, enemyIndex, amount, effect.Pierce == true);
                    return HealPlayer(s, (int)Math.Floor(dealt / 2.0), enemyIndex);
                }
                case "dealDamageDrainPerExhaust":
                {
                    // 墓地参照ドレイン (黒): 消滅枚数×Xダメージ + 半分回復
                    int amount = (effect.Amount ?? 0) * state.Player.ExhaustPile.Count;
                    int dealt = PlayerDamageAfterModifiers(state, amount);
                    var s = DealDamageToEnemy(state, enemyIndex, amount, effect.Pierce == true);
                    return HealPlayer(s, (int)Math.Floor(dealt / 2.0), enemyIndex);
                }
                case "dealDamagePerSelfHpLost":
                    // 自傷の換金 (黒): この戦闘でカード効果により失ったHP×X
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.SelfHpLost, effect.Pierce == true);
                case "retrieveFromExhaust":
                case "playFromExhaust":
                    // コスト再利用 (黒): 消滅置き場からの選択は combat.ts の playCard が retrieveUid で解決する
                    return state;
                case "duplicateRetainers":
                {
                    // 分列の奇跡 (白 2026-09-06): 場の従者1体につき同じ従者を1体召喚。
                    // 走査は解決開始時のスナップショット = 複製が複製を産まない
                    var snapshot = state.Player.Permanents.Where(p => p.Def.Retainer == true && p.Innate != true).ToList();
                    GameState s = state;
                    // 複製同士は互いの登場に反応しない (2026-09-06 ユーザー裁定)
                    var batch = new HashSet<string>();
                    foreach (var src in snapshot)
                    {
                        var token = new CardInstance
                        {
                            Uid = $"summon_p{s.Player.Permanents.Count}_{src.Def.Id}",
                            Def = src.Def,
                            Token = true,
                        };
                        batch.Add(token.Uid);
                        s = s with
                        {
                            Player = s.Player with { Permanents = s.Player.Permanents.Concat(new[] { token }).ToList() },
                            LastEnteredPermanentUid = token.Uid,
                        };
                        s = Events.Emit(s, new GameEvent_PermanentPlayed { CardId = src.Def.Id });
                        s = RunPermanentTriggers(s, "onPermanentEntered", enemyIndex, p => p.Uid == token.Uid || !batch.Contains(p.Uid));
                    }
                    return Events.Emit(s, new GameEvent_RetainersDuplicated { Count = snapshot.Count });
                }
                case "sacrificeRetainer":
                    // 殉教の誓い (白 2026-09-06): 破壊する従者は PlayCard.permanentUid で選び playCard が解決する
                    return state;
                case "activateEnteredRetainer":
                {
                    // 駆けつけ (ひなたのパッシブ 2026-09-06): 場に出た従者のターン開始効果を登場時に1回解決
                    var uid = state.LastEnteredPermanentUid;
                    CardInstance? entered = uid != null ? state.Player.Permanents.FirstOrDefault(p => p.Uid == uid) : null;
                    if (entered == null || entered.Def.Retainer != true || entered.Innate == true) return state;
                    if (!entered.Def.Effects.Any(e => e.Trigger == "onTurnStart")) return state;
                    var s = RunPermanentTriggers(state, "onTurnStart", enemyIndex, p => p.Uid == entered.Uid);
                    return Events.Emit(s, new GameEvent_RetainerRushed { CardId = entered.Def.Id });
                }
                case "triggerRetainersNow":
                {
                    // 進軍の号令 (白 2026-09-06 本家 Multi-Cast): 従者のターン開始効果を今すぐ1回解決
                    Func<CardInstance, bool> isRetainer = p => p.Def.Retainer == true && p.Innate != true;
                    int n = state.Player.Permanents.Count(p => isRetainer(p));
                    var s = RunPermanentTriggers(state, "onTurnStart", enemyIndex, isRetainer);
                    return Events.Emit(s, new GameEvent_RetainersTriggered { Count = n });
                }
                case "summonPermanent":
                {
                    // 召喚 (白): summonId の置物トークンを amount 体場に出す
                    var def = Content.GetCardDef(effect.SummonId ?? "");
                    GameState s = state;
                    for (int i = 0; i < (effect.Amount ?? 1); i++)
                    {
                        // token: true = 敵の「トークン破壊」の対象になる
                        var token = new CardInstance
                        {
                            Uid = $"summon_p{s.Player.Permanents.Count}_{def.Id}",
                            Def = def,
                            Token = true,
                        };
                        s = s with
                        {
                            Player = s.Player with { Permanents = s.Player.Permanents.Concat(new[] { token }).ToList() },
                            LastEnteredPermanentUid = token.Uid,
                        };
                        s = Events.Emit(s, new GameEvent_PermanentPlayed { CardId = def.Id });
                        s = RunPermanentTriggers(s, "onPermanentEntered", enemyIndex);
                    }
                    return s;
                }
                case "dealDamagePerNegStrength":
                case "dealDamagePerWeak":
                {
                    // 威圧の換金 (白 断罪の槌): 対象の威圧スタック×X の追加ダメージ
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0 || (enemy.Weak ?? 0) <= 0) return state;
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * (enemy.Weak ?? 0), effect.Pierce == true);
                }
                case "dischargeBurn":
                {
                    // 爆熱 (赤): 対象の延焼×amount のダメージを与え、延焼を全て失わせる
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0 || enemy.Burn <= 0) return state;
                    int burn = enemy.Burn;
                    GameState s = state with { Enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Burn = 0 }) };
                    s = Events.Emit(s, new GameEvent_BurnDischarged { EnemyIndex = enemyIndex, Amount = burn });
                    return DealDamageToEnemy(s, enemyIndex, burn * (effect.Amount ?? 0), effect.Pierce == true);
                }
                case "shatterBlockConvert":
                {
                    // 破城槌 (赤): 敵のブロックを全て破壊し、破壊した値と同じダメージを与える
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0) return state;
                    int shattered = enemy.Block;
                    GameState s = state with { Enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Block = 0 }) };
                    if (shattered > 0) s = Events.Emit(s, new GameEvent_BlockShattered { EnemyIndex = enemyIndex, Amount = shattered });
                    return DealDamageToEnemy(s, enemyIndex, shattered, effect.Pierce == true);
                }
                case "dealDamageExecute":
                {
                    // 処刑 (赤): amount ダメージ。対象のHPが最大の25%以下なら amountMax ダメージ
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0) return state;
                    bool execute = enemy.Hp <= (int)Math.Floor(enemy.MaxHp * 0.25);
                    return DealDamageToEnemy(
                        state,
                        enemyIndex,
                        execute ? (effect.AmountMax ?? effect.Amount ?? 0) : (effect.Amount ?? 0),
                        effect.Pierce == true);
                }
                case "dealDamagePerRandomPlayed":
                    // 一擲乾坤 (赤カオス): この戦闘で撃ったランダム火力の枚数×amount
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.RandomPlayedThisCombat, effect.Pierce == true);
                case "applyBurnPerDamageTaken":
                {
                    // 業腹 (赤): 直前の敵フェーズで受けたダメージ×amount の延焼
                    int burn = (effect.Amount ?? 0) * state.Player.DamageTakenLastEnemyPhase;
                    var target = EnemyAt(state, enemyIndex);
                    if (burn <= 0 || target == null || target.Hp <= 0) return state;
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Burn = e.Burn + burn });
                    return Events.Emit(state with { Enemies = enemies }, new GameEvent_BurnApplied { EnemyIndex = enemyIndex, Amount = burn });
                }
                case "dealDamagePerDamageTaken":
                    // 逆上 (赤の憤怒): 直前の敵フェーズで受けたダメージ×amount
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.DamageTakenLastEnemyPhase, effect.Pierce == true);
                case "gainBlockPerPermanent":
                {
                    // 隊列の盾 (白): 置物の数×X ブロック
                    int amount = (effect.Amount ?? 0) * CountedPermanents(state);
                    return GainPlayerBlock(state, amount, enemyIndex);
                }
                case "gainBlockPerEnergyMax":
                {
                    // 木陰の守り (緑): エナジー上限×X ブロック
                    int amount = (effect.Amount ?? 0) * state.Player.EnergyMaxAtTurnStart;
                    return GainPlayerBlock(state, amount, enemyIndex);
                }
                case "gainBlockPerExhaust":
                {
                    // 亡者の壁 (黒): 消滅した枚数×X ブロック
                    int amount = (effect.Amount ?? 0) * state.Player.ExhaustPile.Count;
                    return GainPlayerBlock(state, amount, enemyIndex);
                }
                case "exhaustFromDeck":
                    // 忘却 (黒): 山札の上X枚を消滅させる
                    return MillPlayerDeck(state, effect.Amount ?? 0, enemyIndex);
                case "exhaustFromDeckChoose":
                    // 引導 (黒): 山札か捨て札から選んで消滅。選択は playCard が deckUids で解決する
                    return state;
                case "retrieveFromDiscard":
                case "searchDeck":
                case "upgradeInHand":
                case "upgradeAllInHand":
                case "addCopyToDiscard":
                case "growSelf":
                    // 緑のカード操作 (2026-09-02): 「プレイした札そのもの」が要るので playCard が解決する
                    return state;
                case "gainMaxHp":
                {
                    // 獲物 (緑 2026-09-07=本家 Feed): 最大HPとHPを+X。勝利時に run.maxHp へ同期する (Run.AfterVictory)
                    int amount = effect.Amount ?? 0;
                    if (amount <= 0) return state;
                    GameState next = state with { Player = state.Player with { MaxHp = state.Player.MaxHp + amount, Hp = state.Player.Hp + amount } };
                    return Events.Emit(next, new GameEvent_MaxHpGained { Amount = amount });
                }
                case "gainBlockPerMomentum":
                {
                    // 風の壁 (緑 2026-09-07): 勢い×amount のブロック。勢いは失わない
                    int block = state.Player.Momentum * (effect.Amount ?? 0);
                    if (block <= 0) return state;
                    GameState s = state with { Player = state.Player with { Block = state.Player.Block + block } };
                    s = Events.Emit(s, new GameEvent_BlockGained { Target = "player", Amount = block });
                    return RunPermanentTriggers(s, "onBlockGained", enemyIndex);
                }
                case "addGrowthPerMomentum":
                {
                    // 根付く勢い (緑 2026-09-07): 勢い2につき成長+amount (切り捨て)。勢いは失わない
                    int gained = (state.Player.Momentum / 2) * (effect.Amount ?? 1);
                    if (gained <= 0) return state;
                    GameState s = state with { Player = state.Player with { Growth = state.Player.Growth + gained } };
                    s = Events.Emit(s, new GameEvent_GrowthAdded { Amount = gained });
                    return FireGainTrigger(s, "onGrowthGained", enemyIndex);
                }
                case "gainSetSlot":
                {
                    // 伏せ枠+X (罠師の茂み 2026-09-02)
                    int amount = effect.Amount ?? 1;
                    GameState next = state with { Player = state.Player with { SetSlots = state.Player.SetSlots + amount } };
                    return Events.Emit(next, new GameEvent_SetSlotGained { Amount = amount });
                }
                case "addCardToHand":
                {
                    // 骨刃 (黒 2026-09-01): summonId のトークン札を手札に加える (この戦闘限り)。
                    // uid は eventLog 長ベース = 単調増加なので衝突せず、シードから決定的
                    var def = Content.GetCardDef(effect.SummonId ?? "");
                    var made = new List<CardInstance>();
                    for (int i = 0; i < (effect.Amount ?? 1); i++)
                    {
                        made.Add(new CardInstance { Uid = $"tok_{state.EventLog.Count}_{i}_{def.Id}", Def = def, Token = true });
                    }
                    GameState s = state with
                    {
                        Player = state.Player with { Hand = state.Player.Hand.Concat(made).ToList() },
                    };
                    return Events.Emit(s, new GameEvent_CardsAddedToHand { CardId = def.Id, Count = made.Count });
                }
                case "recycleExhaust":
                {
                    // 輪廻 (黒 2026-09-01): 消滅置き場を全て山札に混ぜて戻し、戻した枚数×Xダメージ
                    int n = state.Player.ExhaustPile.Count;
                    if (n == 0) return state;
                    var pool = state.Player.DrawPile.Concat(state.Player.ExhaustPile).ToList();
                    var (mixed, rng) = Rng.Shuffle(state.Rng, pool);
                    GameState s = state with
                    {
                        Rng = rng,
                        Player = state.Player with { DrawPile = mixed.ToList(), ExhaustPile = new List<CardInstance>() },
                    };
                    s = Events.Emit(s, new GameEvent_ExhaustRecycled { Count = n });
                    return DealDamageToEnemy(s, enemyIndex, (effect.Amount ?? 0) * n, effect.Pierce == true);
                }
                case "empowerShivs":
                    // 骨刃の強化 (急所読み): 常在パッシブ。加算は playCard がプレイ時に注入する
                    return state;
                case "dealDamagePerExhaust":
                    // 墓地参照 (黒): 消滅した枚数×X
                    return DealDamageToEnemy(state, enemyIndex, (effect.Amount ?? 0) * state.Player.ExhaustPile.Count, effect.Pierce == true);
                case "exposeEnemy":
                {
                    // 急所 (敵版脆弱): 次に受けるプレイヤーダメージN回が+50%
                    int amount = effect.Amount ?? 0;
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Hp <= 0) return state;
                    {
                        var (sa, blocked) = TryArtifactBlock(state, enemyIndex, "exposeEnemy");
                        if (blocked) return sa;
                    }
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Exposed = e.Exposed + amount });
                    return Events.Emit(state with { Enemies = enemies }, new GameEvent_ExposedApplied { EnemyIndex = enemyIndex, Amount = amount });
                }
                case "dischargeGrowth":
                {
                    // 成長放出 (緑): 成長×amount のダメージを与え、成長を全て失う。
                    // 成長を先に0にしてからダメージ解決 = 放出は自分自身を二重に数えない (2026-08-31)
                    int spent = state.Player.Growth;
                    // 収穫の鎌 (2026-09-03 レア): 放出のあと成長がN残る (放出量は全量で計算)
                    int keep = Math.Min(spent, state.HarvestKeep ?? 0);
                    GameState s = state with { Player = state.Player with { Growth = keep } };
                    s = DealDamageToEnemy(s, enemyIndex, spent * (effect.Amount ?? 0), effect.Pierce == true);
                    return Events.Emit(s, new GameEvent_GrowthDischarged { Spent = spent });
                }
                case "dischargeGrowthBlock":
                {
                    // 守りの刈り (緑 2026-08-31): 成長×amount のブロックを得て成長を全て失う
                    int spent = state.Player.Growth;
                    GameState s = GainPlayerBlock(state, spent * (effect.Amount ?? 0), enemyIndex);
                    s = s with { Player = s.Player with { Growth = 0 } };
                    return Events.Emit(s, new GameEvent_GrowthDischarged { Spent = spent });
                }
                case "dischargeMomentumBurn":
                {
                    // 火移し (赤): 勢い×amount の延焼を与え、勢いを全て失う
                    int spent = state.Player.Momentum;
                    int burn = spent * (effect.Amount ?? 0);
                    var target = EnemyAt(state, enemyIndex);
                    if (burn <= 0 || target == null || target.Hp <= 0) return state;
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Burn = e.Burn + burn });
                    GameState s = state with { Enemies = enemies, Player = state.Player with { Momentum = 0 } };
                    return Events.Emit(s, new GameEvent_BurnApplied { EnemyIndex = enemyIndex, Amount = burn });
                }
                case "dischargeMomentumBlock":
                {
                    // 余勢の構え (赤): 勢い×amount のブロックを得て、勢いを全て失う
                    int spent = state.Player.Momentum;
                    int block = spent * (effect.Amount ?? 0);
                    if (block <= 0) return state;
                    GameState s = state with
                    {
                        Player = state.Player with { Momentum = 0, Block = state.Player.Block + block },
                    };
                    s = Events.Emit(s, new GameEvent_BlockGained { Target = "player", Amount = block });
                    return RunPermanentTriggers(s, "onBlockGained", enemyIndex);
                }
                case "dischargeMomentumDamage":
                {
                    // 角の一突き・嵐の角 (緑 勢いの網 2026-09-04): 勢い×amount のダメージを与え、勢いを全て失う。
                    // 放出に勢い加算は乗らない = 先に0にしてから解決
                    int spent = state.Player.Momentum;
                    if (spent <= 0) return state;
                    // 放出が空振りする時は勢いを消費しない (2026-09-04 Opusラン P)
                    bool anyTarget = effect.Target == "all"
                        ? state.Enemies.Any(e => e.Hp > 0)
                        : (EnemyAt(state, enemyIndex)?.Hp ?? 0) > 0;
                    if (!anyTarget) return state;
                    int dmg = spent * (effect.Amount ?? 0);
                    GameState s = state with { Player = state.Player with { Momentum = 0 } };
                    s = Events.Emit(s, new GameEvent_MomentumDischarged { Spent = spent });
                    if (effect.Target == "all")
                    {
                        for (int i = 0; i < s.Enemies.Count; i++)
                        {
                            if (s.Enemies[i].Hp > 0) s = DealDamageToEnemy(s, i, dmg, effect.Pierce == true);
                        }
                        return s;
                    }
                    return DealDamageToEnemy(s, enemyIndex, dmg, effect.Pierce == true);
                }
                case "dischargeMomentumVolley":
                {
                    // 連なる角 (緑 2026-09-04): 勢いを全て失い、勢い×amount のダメージを volleyHits 回に分けて放つ
                    int spent = state.Player.Momentum;
                    if (spent <= 0) return state;
                    if ((EnemyAt(state, enemyIndex)?.Hp ?? 0) <= 0) return state;
                    int per = spent * (effect.Amount ?? 1);
                    GameState s = state with { Player = state.Player with { Momentum = 0 } };
                    s = Events.Emit(s, new GameEvent_MomentumDischarged { Spent = spent });
                    for (int h = 0; h < (effect.VolleyHits ?? 3); h++)
                    {
                        if ((EnemyAt(s, enemyIndex)?.Hp ?? 0) <= 0) break;
                        s = DealDamageToEnemy(s, enemyIndex, per, effect.Pierce == true);
                    }
                    return s;
                }
                case "momentumCarryHalf":
                    return state; // 常在の印。ターン終了時の勢いリセット (combat.ts) が置物の有無を見る
                case "dischargeMomentumGrowth":
                {
                    // 根付く勢い (緑 2026-09-04): 勢いを全て失い、その 1/amount (切り上げ) を成長に変える
                    int spent = state.Player.Momentum;
                    if (spent <= 0) return state;
                    int gained = (int)Math.Ceiling(spent / (double)Math.Max(1, effect.Amount ?? 2));
                    GameState s = state with
                    {
                        Player = state.Player with { Momentum = 0, Growth = state.Player.Growth + gained },
                    };
                    s = Events.Emit(s, new GameEvent_MomentumDischarged { Spent = spent });
                    s = Events.Emit(s, new GameEvent_GrowthAdded { Amount = gained });
                    return gained > 0 ? FireGainTrigger(s, "onGrowthGained", enemyIndex) : s;
                }
                case "dealDamageCleave":
                {
                    // キル連鎖: 対象にXダメージ。倒れたら別の生存敵に同値
                    GameState s = DealDamageToEnemy(state, enemyIndex, effect.Amount ?? 0, effect.Pierce == true);
                    var hit = EnemyAt(s, enemyIndex);
                    if (hit != null && hit.Hp <= 0)
                    {
                        int nextIdx = -1;
                        for (int i = 0; i < s.Enemies.Count; i++)
                        {
                            if (i != enemyIndex && s.Enemies[i].Hp > 0) { nextIdx = i; break; }
                        }
                        if (nextIdx >= 0) s = DealDamageToEnemy(s, nextIdx, effect.Amount ?? 0, effect.Pierce == true);
                    }
                    return s;
                }
                case "shatterBlock":
                {
                    // 粉砕 (赤): 敵のブロックを全て破壊する (潜伏の殻も割れる = 噛みつきが来る)
                    var enemy = EnemyAt(state, enemyIndex);
                    if (enemy == null || enemy.Block == 0) return state;
                    var enemies = MapEnemy(state.Enemies, enemyIndex, e => e with { Block = 0 });
                    var s2 = Events.Emit(
                        state with { Enemies = enemies },
                        new GameEvent_BlockShattered { EnemyIndex = enemyIndex, Amount = enemy.Block });
                    return BreakBurrowIfCracked(s2, enemyIndex);
                }
                case "dealDamageRandom":
                {
                    // ランダム火力 (赤): amount〜amountMax のロール (シードRNG)
                    var (roll, rng) = Rng.NextInt(state.Rng, effect.Amount ?? 0, effect.AmountMax ?? effect.Amount ?? 0);
                    return DealDamageToEnemy(state with { Rng = rng }, enemyIndex, roll, effect.Pierce == true);
                }
                case "impulseDraw":
                {
                    // 衝動 (赤): 山札の上からX枚を「このターン限り」の手札に加える
                    var before = new HashSet<string>(state.Player.Hand.Select(c => c.Uid));
                    GameState s = DrawCards(state, effect.Amount ?? 0);
                    var drawnUids = s.Player.Hand.Where(c => !before.Contains(c.Uid)).Select(c => c.Uid).ToList();
                    if (drawnUids.Count == 0) return s;
                    s = s with
                    {
                        Player = s.Player with { ImpulseUids = s.Player.ImpulseUids.Concat(drawnUids).ToList() },
                    };
                    return Events.Emit(s, new GameEvent_ImpulseDrawn { Count = drawnUids.Count });
                }
                case "loseHp":
                    // 自傷 (赤・黒): ブロックを無視して自分のHPを失う
                    return LosePlayerHp(state, effect.Amount ?? 0, enemyIndex);
                case "discountNext":
                {
                    // マナ軽減トークン: 次にプレイする1枚のコスト-X (消費は playCard 側)
                    int amount = effect.Amount ?? 0;
                    var next = state with
                    {
                        Player = state.Player with { NextCardDiscount = state.Player.NextCardDiscount + amount },
                    };
                    return Events.Emit(next, new GameEvent_DiscountGained { Amount = amount });
                }
                case "dischargeAether":
                {
                    // 霊気放出 (青): 霊気×amount のダメージを与え、霊気を全消費
                    int spent = state.Player.Aether;
                    if (spent == 0) return state;
                    GameState s = state with { Player = state.Player with { Aether = 0 } };
                    s = Events.Emit(s, new GameEvent_AetherDischarged { Spent = spent });
                    return DealDamageToEnemy(s, enemyIndex, spent * (effect.Amount ?? 0), effect.Pierce == true);
                }
                case "gainEnergyMax":
                {
                    // 緑の柱①ランプ: 上限のみ増える。恩恵は次の自ターンから
                    int amount = effect.Amount ?? 0;
                    var next = state with
                    {
                        Player = state.Player with { EnergyMax = state.Player.EnergyMax + amount },
                    };
                    return Events.Emit(next, new GameEvent_EnergyMaxGained { Amount = amount });
                }
                case "addGrowth":
                {
                    int amount = effect.Amount ?? 0;
                    var next = state with { Player = state.Player with { Growth = state.Player.Growth + amount } };
                    var emitted = Events.Emit(next, new GameEvent_GrowthAdded { Amount = amount });
                    return amount > 0 ? FireGainTrigger(emitted, "onGrowthGained", enemyIndex) : emitted;
                }
                case "doubleMomentum":
                {
                    // トランプルの倍加 (2026-08-29): 現在の勢いを2倍にする
                    int amount = state.Player.Momentum;
                    if (amount == 0) return state;
                    GameState doubled = state with { Player = state.Player with { Momentum = state.Player.Momentum * 2 } };
                    return FireGainTrigger(doubled, "onMomentumGained", enemyIndex);
                }
                case "doubleGrowth":
                {
                    // 成長スタックのシグネチャー: 現在の成長カウンターを2倍にする
                    int amount = state.Player.Growth;
                    if (amount == 0) return state;
                    var next = state with { Player = state.Player with { Growth = state.Player.Growth * 2 } };
                    return FireGainTrigger(Events.Emit(next, new GameEvent_GrowthAdded { Amount = amount }), "onGrowthGained", enemyIndex);
                }
                case "drawCards":
                    return DrawCards(state, effect.Amount ?? 0);
                case "negate":
                    // 打ち消し: 次の敵行動を無効化する汎用フラグを立てる
                    return state with { NegateNextAction = true };
                case "negateConvertIce":
                {
                    // 魔力盗み (青): 打ち消し + その行動の実値ぶん氷壁を得る
                    int actual = EffectiveIntent(state, enemyIndex)?.Actual ?? 0;
                    GameState s = state with { NegateNextAction = true };
                    if (actual > 0)
                    {
                        s = GainPlayerIceBlock(s, actual);
                    }
                    return s;
                }
                case "dealDamagePerIceBlock":
                    // 氷の槍 (青): 現在の氷壁×amount (蓄積の換金。氷壁は消費しない)
                    return DealDamageToEnemy(
                        state,
                        enemyIndex,
                        (effect.Amount ?? 0) * state.Player.IceBlock,
                        effect.Pierce == true,
                        false); // 急所は氷壁変換に乗らない (2026-08-31)
                case "dischargeAetherDraw":
                {
                    // 霊気の奔流 (青): 霊気×amount 枚ドローして霊気を全消費
                    int spent = state.Player.Aether;
                    if (spent == 0) return state;
                    GameState s = state with { Player = state.Player with { Aether = 0 } };
                    s = Events.Emit(s, new GameEvent_AetherDischarged { Spent = spent });
                    return DrawCards(s, spent * (effect.Amount ?? 1));
                }
                case "script":
                    throw new InvalidOperationException($"未登録のスクリプト効果: {effect.ScriptId}");
                default:
                    // TS は判別共用体で網羅しているため default が無い (未知の効果名は undefined を返して壊れる)。
                    // C# では分岐漏れを検出できるよう明示的に落とす
                    throw new NotImplementedException($"effect: {effect.Effect}");
            }
        }

        /// <summary>
        /// 猛り火の条件を満たすか (onPlay・置物トリガー用)。リアクション窓は
        /// eligibleReactionEffects 側で同じ判定をしている
        /// </summary>
        public static bool BlazeConditionMet(GameState state, DeclarativeEffect effect, int? enemyIndex = null)
        {
            if (effect.Condition?.Blaze == true && !IsBlazing(state)) return false;
            var c = effect.Condition;
            if (c != null)
            {
                // 参照シナジー (緑 2026-09-03 本家6型): 意図・急所・守り成功・とどめ・完全に凌いだ
                EnemyState? e = enemyIndex != null ? EnemyAt(state, enemyIndex.Value) : null;
                if (c.EnemyIntent != null || c.EnemyIntentNot != null)
                {
                    string? kind = enemyIndex != null ? EffectiveIntent(state, enemyIndex.Value)?.Kind : null;
                    if (c.EnemyIntent != null && kind != c.EnemyIntent) return false;
                    if (c.EnemyIntentNot != null && (kind == null || kind == c.EnemyIntentNot)) return false;
                }
                if (c.EnemyExposed == true)
                {
                    int exposed = 0;
                    if (enemyIndex != null)
                    {
                        var snap = state.ResolvingExposedAtStart;
                        int? fromSnap = snap != null && enemyIndex.Value >= 0 && enemyIndex.Value < snap.Count
                            ? snap[enemyIndex.Value]
                            : (int?)null;
                        exposed = fromSnap ?? e?.Exposed ?? 0;
                    }
                    if (exposed <= 0) return false;
                }
                if (c.PerfectBlockLastPhase == true && state.Player.PerfectBlockLastPhase != true) return false;
                if (c.TargetDead == true && !(e != null && e.Hp <= 0)) return false;
                if (c.LastActionNoHpLoss == true
                    && !(state.LastAction != null && state.LastAction.Kind == "attack" && state.LastAction.HpLoss == 0)) return false;
            }
            // 成長しきい値 (2026-09-02): 解決の時点の成長で判定 = 同じカードの前の効果で積んだ成長も乗る
            if (effect.Condition?.MinGrowth != null && state.Player.Growth < effect.Condition.MinGrowth.Value) return false;
            if (effect.Condition?.MinMomentum != null && state.Player.Momentum < effect.Condition.MinMomentum.Value) return false;
            // 上限しきい値 (緑 2026-09-07 若幹の一撃・大地の唸り): ターン開始時の上限を読む
            if (effect.Condition?.MinEnergyMax != null && state.Player.EnergyMaxAtTurnStart < effect.Condition.MinEnergyMax.Value) return false;
            // 回復参照 (白 2026-09-06 修繕の祈り): このターンに1回でも回復していたら (過剰回復も数える)
            if (effect.Condition?.HealedThisTurn == true && (state.Player.HealsThisTurn ?? 0) <= 0) return false;
            // HP割合条件 (2026-09-03 不動の根)
            if (effect.Condition?.HpAtOrBelowRatio != null
                && state.Player.Hp > state.Player.MaxHp * effect.Condition.HpAtOrBelowRatio.Value) return false;
            return true;
        }

        /// <summary>
        /// 成長/勢いの獲得誘発 (2026-09-02 緑の接着剤)。誘発の中で得た成長/勢いは再誘発しない
        /// (1段で止める = 自己増殖ループを構造的に禁止)
        /// </summary>
        private static GameState FireGainTrigger(GameState state, string trigger, int enemyIndex)
        {
            if (state.ResolvingGainTrigger == true) return state;
            bool any = false;
            foreach (var p in state.Player.Permanents)
            {
                foreach (var e in p.Def.Effects) if (e.Trigger == trigger) { any = true; break; }
                if (any) break;
            }
            if (!any) return state;
            GameState s = state with { ResolvingGainTrigger = true };
            s = RunPermanentTriggers(s, trigger, enemyIndex);
            return s with { ResolvingGainTrigger = false };
        }

        /// <summary>カードの onPlay 効果を順に解決 (target:'all' は全体解決)</summary>
        public static GameState ResolveOnPlayEffects(GameState state, CardInstance card, int enemyIndex)
        {
            // 虚弱 (2026-09-01) の判定用フラグ。ネストしたカードプレイ (死者再生→直接プレイ) があるので退避・復元する
            bool prev = state.ResolvingCardPlay == true;
            var prevExposed = state.ResolvingExposedAtStart;
            // 急所参照はプレイ開始時点の値で判定 (本家 Dismantle の読み)
            GameState s = state with
            {
                ResolvingCardPlay = true,
                AngerFiredThisPlay = false,
                ResolvingExposedAtStart = state.Enemies.Select(e => e.Exposed).ToList(),
            };
            foreach (var effect in card.Def.Effects)
            {
                // 猛り火は「解決の時点」で判定する = 同じカードの前の効果 (着火など) で点いたら乗る
                if (effect.Trigger == "onPlay" && BlazeConditionMet(s, effect, enemyIndex))
                {
                    s = ResolveEffectTargeted(s, effect, enemyIndex);
                }
            }
            return s with { ResolvingCardPlay = prev, ResolvingExposedAtStart = prevExposed };
        }

        private static readonly HashSet<string> REACTION_TRIGGERS = new HashSet<string>
        {
            "onAttackIncoming",
            "onAttacked",
            "onEnemyAction",
            "onEnemyBuffed",
            "onEnemyDefended",
        };

        /// <summary>
        /// カードのリアクション効果を順に解決し、ReactionTriggered を記録する。
        /// カードの移動 (伏せ場/手札→捨て札) とコスト処理は方式固有のため呼び出し側 (ReactionSystem 実装) が行う。
        /// </summary>
        public static GameState ResolveReactionEffects(GameState state, CardInstance card, int enemyIndex)
        {
            // リアクションは「カードのプレイ」ではない = 虚弱・勢いの対象外 (2026-09-05)
            bool prevCardPlay = state.ResolvingCardPlay == true;
            GameState s = Events.Emit(
                state with { ResolvingCardPlay = false },
                new GameEvent_ReactionTriggered { CardId = card.Def.Id, Mode = state.ReactionMode });
            foreach (var effect in SetAny.SetEffectsOf(card))
            {
                // 効果ごとの条件 (2026-09-06 白 報復の光): 発動可否は eligible 側が見るが、
                // 条件つきの効果だけを落とすのはここ。窓専用条件 (minActionValue 等) は BlazeConditionMet が見ないので通る
                if (REACTION_TRIGGERS.Contains(effect.Trigger) && BlazeConditionMet(s, effect, enemyIndex))
                {
                    // target:'all' の返し (茨の爆ぜ) は生存全体に解決する
                    s = ResolveEffectTargeted(s, effect, enemyIndex);
                }
            }
            // 読み勝ちの換金 (2026-08-29): リアクション発動に反応する置物。3方式共通の解決経路なので方式非依存。
            // 全カード伏せ可 (実験): 通常カードの伏せ発動は「リアクションの発動」ではない
            GameState @out = card.Def.Type == CardTypes.Reaction
                ? RunPermanentTriggers(s, "onReactionFired", enemyIndex)
                : s;
            return @out with { ResolvingCardPlay = prevCardPlay };
        }
    }
}
