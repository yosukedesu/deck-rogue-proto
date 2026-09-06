// Combat.cs — src/engine/combat.ts の手書き移植 (unity/PORTING.md)
// 戦闘フロー (ターン進行・敵フェーズ・意図宣言)。
// 敵フェーズは EndTurn 内で同期的に解決する。リアクション方式が割り込みを要求した場合のみ
// 'awaiting-reaction' で中断し、State.ApplyCommand が方式コマンド処理後に ContinueAfterWindow() で再開する。
// 方式固有の if 分岐をここに書いてはならない (フックは Hooks.DispatchHooks 経由)。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    /// <summary>ラン (ドラフト連戦) などが使う戦闘開始オプション (TS: combat.ts の CombatOptions)</summary>
    public sealed record CombatOptions
    {
        /// <summary>使用するデッキの実カード列</summary>
        public IReadOnlyList<CardInstance> Deck { get; init; }
        /// <summary>リーダー (パッシブ置物・最大HP・ドロー枚数・エナジー上限の個性を適用)</summary>
        public string LeaderId { get; init; }
        /// <summary>開始時HP (持ち越し用)。省略時は全快</summary>
        public int? PlayerHp { get; init; }
        /// <summary>最大HP (ランの恒久ボーナス用)。省略時はリーダーの素の値</summary>
        public int? PlayerMaxHp { get; init; }
        /// <summary>敵HPの倍率 (ランの深度スケーリング用)</summary>
        public double? EnemyHpScale { get; init; }
        /// <summary>敵の初期強化</summary>
        public int? EnemyStrength { get; init; }
        /// <summary>敵の打点倍率</summary>
        public double? EnemyAtkScale { get; init; }
        /// <summary>A型レリックの置物</summary>
        public IReadOnlyList<CardInstance> RelicPermanents { get; init; }
        /// <summary>C型レリック (静かな鈴)</summary>
        public int? SetDamageReduction { get; init; }
        /// <summary>デバッグ: 意図の実値を常時公開</summary>
        public bool? RevealIntents { get; init; }
        /// <summary>C型レリック (蜃気楼の面)</summary>
        public bool? RevealOnSet { get; init; }
        /// <summary>実験: 全カード伏せ可</summary>
        public bool? SetAnyCards { get; init; }
        /// <summary>C型レリック (回収の紐): 回収が0E</summary>
        public bool? RetrieveFree { get; init; }
        /// <summary>C型レリック (大樹の心)</summary>
        public int? EnergyMaxRefBonus { get; init; }
        /// <summary>C型レリック (収穫の鎌)</summary>
        public int? HarvestKeep { get; init; }
    }

    public static class Combat
    {
        public const int PLAYER_MAX_HP = 75; // StSスケール (2026-08-25 人間基準化)
        private const int BASE_ENERGY = 3;
        private const int DRAW_PER_TURN = 5;
        /// <summary>がらくた (罠壊し) の1戦闘あたり上限</summary>
        private const int JUNK_CAP = 4;
        /// <summary>負傷 (死に札) の1戦闘上限。ハメ防止</summary>
        private const int WOUND_CAP = 5;
        /// <summary>火傷の1戦闘あたり上限 (負傷と同思想のハメ防止)</summary>
        private const int SCALD_CAP = 5;
        /// <summary>拘束中に1ターンでプレイできるカードの上限 (本家StS2 Sloth 準拠)</summary>
        public const int RESTRAIN_PLAY_CAP = 3;

        // ==== 小さな移植ヘルパ ====

        /// <summary>JS の Math.round (= floor(x + 0.5))。C# の Math.Round は銀行丸めなので使わない</summary>
        private static int JsRound(double v) => (int)Math.Floor(v + 0.5);

        private static List<T> MapIdx<T>(IReadOnlyList<T> src, Func<T, int, T> f)
        {
            var outp = new List<T>(src.Count);
            for (int i = 0; i < src.Count; i++) outp.Add(f(src[i], i));
            return outp;
        }

        private static List<T> Concat<T>(IReadOnlyList<T> a, IEnumerable<T> b)
        {
            var outp = new List<T>(a);
            outp.AddRange(b);
            return outp;
        }

        private static List<T> Append<T>(IReadOnlyList<T> a, T b)
        {
            var outp = new List<T>(a) { b };
            return outp;
        }

        /// <summary>state.enemies.map((x,j) =&gt; j === i ? f(x) : x) の定型</summary>
        private static GameState WithEnemy(GameState s, int index, Func<EnemyState, EnemyState> f)
        {
            var list = new List<EnemyState>(s.Enemies);
            list[index] = f(list[index]);
            return s with { Enemies = list };
        }

        /// <summary>enemies.findIndex(e =&gt; e.hp &gt; 0) (見つからなければ -1)</summary>
        private static int FindAlive(IReadOnlyList<EnemyState> enemies)
        {
            for (int i = 0; i < enemies.Count; i++) if (enemies[i].Hp > 0) return i;
            return -1;
        }

        /// <summary>Math.max(0, findIndex(hp&gt;0))</summary>
        private static int FirstAliveOrZero(GameState s) => Math.Max(0, FindAlive(s.Enemies));

        private static bool IsOver(GameState s) => s.Phase == CombatPhases.Won || s.Phase == CombatPhases.Lost;

        // ==== 戦闘開始 ====

        /// <summary>戦闘前の空状態 (UI/sim が方式を保持するための器)。戦闘は StartCombat で開始する</summary>
        public static GameState CreateInitialState(int seed, string reactionMode)
        {
            return new GameState
            {
                Rng = Rng.Create(seed),
                ReactionMode = reactionMode,
                Phase = CombatPhases.PlayerTurn,
                Turn = 0,
                Player = new PlayerState
                {
                    Hp = PLAYER_MAX_HP,
                    MaxHp = PLAYER_MAX_HP,
                    Block = 0,
                    Energy = BASE_ENERGY,
                    EnergyMax = BASE_ENERGY, // ランプは戦闘ごとにリセット
                    EnergyMaxAtTurnStart = BASE_ENERGY,
                    DrawPerTurn = DRAW_PER_TURN,
                    Hand = new List<CardInstance>(),
                    DrawPile = new List<CardInstance>(),
                    DiscardPile = new List<CardInstance>(),
                    SetCards = new List<CardInstance>(),
                    SetSlots = 1, // 伏せ枠は基本1。リーダー個性 (かすみ=2) で上書き
                    Permanents = new List<CardInstance>(),
                    ExhaustPile = new List<CardInstance>(),
                    Growth = 0,
                    Momentum = 0,
                    IceBlock = 0,
                    CardsPlayedThisTurn = 0,
                    SetsThisTurn = 0,
                    PlaysThisTurn = 0,
                    AttacksPlayedThisTurn = 0,
                    HealsThisTurn = 0,
                    WeakFreshThisPhase = 0,
                    CardsPlayedTotal = 0,
                    Aether = 0,
                    HealsThisCombat = 0,
                    NextCardDiscount = 0,
                    ImpulseUids = new List<string>(),
                    Weak = 0,
                    Vulnerable = 0,
                    Frail = 0,
                    Restrain = 0,
                    SelfHpLost = 0,
                    RandomPlayedThisCombat = 0,
                    DamageTakenLastEnemyPhase = 0,
                    SpellEchoes = 0,
                },
                Enemies = new List<EnemyState>(),
                PendingWindow = null,
                NegateNextAction = false,
                ReactionUsedThisAction = false,
                LastAction = null,
                EventLog = new List<GameEvent>(),
            };
        }

        /// <summary>戦闘開始の実体: デッキシャッフル・敵配置をして第1ターンを開始する</summary>
        public static GameState StartCombatWithOptions(int seed, string reactionMode, string enemyId, CombatOptions options)
        {
            // 敵ID or 編成ID を編成メンバー列に解決
            var members = Content.ResolveEncounter(enemyId);
            var state = CreateInitialState(seed, reactionMode);
            // リーダーの個性: 最大HP・ドロー枚数・エナジー上限・パッシブ置物
            var leader = !string.IsNullOrEmpty(options.LeaderId) ? Content.GetLeaderDef(options.LeaderId) : null;
            if (leader != null)
            {
                state = state with
                {
                    Player = state.Player with
                    {
                        MaxHp = leader.MaxHp,
                        Hp = leader.MaxHp,
                        DrawPerTurn = leader.DrawPerTurn,
                        Energy = leader.EnergyMax,
                        EnergyMax = leader.EnergyMax,
                        EnergyMaxAtTurnStart = leader.EnergyMax + (options.EnergyMaxRefBonus ?? 0), // 大樹の心
                        SetSlots = leader.SetSlots ?? 1,
                        Permanents = new List<CardInstance> { Content.BuildLeaderPassive(leader) },
                    },
                };
            }
            var (deck, rng) = Rng.Shuffle(state.Rng, options.Deck);
            // 群れ補正 (member.hpScale/strength) とランの深度スケーリングは乗算/加算で重なる
            var enemies = new List<EnemyState>(members.Count);
            for (int mi = 0; mi < members.Count; mi++)
            {
                var m = members[mi];
                var def = Content.GetEnemyDef(m.EnemyId);
                int maxHp = JsRound(def.MaxHp * (options.EnemyHpScale ?? 1.0) * (m.HpScale ?? 1.0));
                enemies.Add(new EnemyState
                {
                    EnemyId = m.EnemyId,
                    Hp = maxHp,
                    MaxHp = maxHp,
                    // 開幕ブロック。潜伏の殻は開幕ブロックと同じ器
                    Block = def.Burrow?.Block ?? def.StartingBlock ?? 0,
                    BurrowActive = def.Burrow != null ? (bool?)true : null,
                    Intent = null,
                    Strength = (options.EnemyStrength ?? 0) + (m.Strength ?? 0),
                    AtkScale = (options.EnemyAtkScale != null && options.EnemyAtkScale != 1.0) ? options.EnemyAtkScale : null,
                    Burn = 0,
                    Confusion = 0,
                    Exposed = 0,
                    PatternIndex = m.PatternOffset ?? 0,
                    NoReactTable = m.NoReactTable == true ? (bool?)true : null,
                    // とげは def からコピーして状態に持つ
                    Thorns = def.Thorns,
                    Artifact = def.Artifact,
                    Armor = def.Armor,
                });
            }
            state = state with
            {
                Rng = rng,
                Player = state.Player with
                {
                    DrawPile = deck,
                    MaxHp = options.PlayerMaxHp ?? state.Player.MaxHp,
                    Hp = Math.Min(
                        options.PlayerHp ?? options.PlayerMaxHp ?? state.Player.MaxHp,
                        options.PlayerMaxHp ?? state.Player.MaxHp),
                    // A型レリックはリーダーパッシブと同じ「戦闘開始時から場にある置物」
                    Permanents = Concat(state.Player.Permanents, options.RelicPermanents ?? (IReadOnlyList<CardInstance>)new List<CardInstance>()),
                },
                Enemies = enemies,
                // C型レリック。revealIntents は第1ターンの意図宣言より前に立てる必要がある
                SetDamageReduction = (options.SetDamageReduction ?? 0) != 0 ? options.SetDamageReduction : null,
                RetrieveFree = options.RetrieveFree == true ? (bool?)true : null,
                EnergyMaxRefBonus = (options.EnergyMaxRefBonus ?? 0) != 0 ? options.EnergyMaxRefBonus : null,
                HarvestKeep = (options.HarvestKeep ?? 0) != 0 ? options.HarvestKeep : null,
                RevealIntents = options.RevealIntents == true ? (bool?)true : null,
                RevealOnSet = options.RevealOnSet == true ? (bool?)true : null,
                SetAnyCards = options.SetAnyCards == true ? (bool?)true : null,
            };
            state = Events.Emit(state, new GameEvent_CombatStarted { EnemyId = enemyId });
            var s = StartPlayerTurn(state, 1);
            // onCombatStart: 第1ターンのセットアップの後に1回だけ発火
            s = Effects.RunPermanentTriggers(s, "onCombatStart", FirstAliveOrZero(s));
            return s;
        }

        /// <summary>StartCombat: プリセットデッキ ID から戦闘を開始する (単発戦闘用)</summary>
        public static GameState StartCombat(int seed, string reactionMode, string enemyId,
            string deckId = "starter", string leaderId = null, IReadOnlyList<string> cardIds = null)
        {
            var deck = (cardIds != null && cardIds.Count > 0) ? (IReadOnlyList<CardInstance>)BuildDeckFromIds(cardIds) : Content.BuildDeck(deckId);
            return StartCombatWithOptions(seed, reactionMode, enemyId, new CombatOptions { Deck = deck, LeaderId = leaderId });
        }

        /// <summary>検証用: カードIDの並びからデッキを組む (工房産 fused_ / fusion_ は ResolveFusedDef で復元)</summary>
        public static List<CardInstance> BuildDeckFromIds(IReadOnlyList<string> ids)
        {
            var outp = new List<CardInstance>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                var def = (id.StartsWith("fused_") || id.StartsWith("fusion_")) ? Fusion.ResolveFusedDef(id) : Content.GetCardDef(id);
                if (def == null) throw new InvalidOperationException($"未定義カード: {id}");
                outp.Add(new CardInstance { Uid = $"{id}#{i}", Def = def });
            }
            return outp;
        }

        /// <summary>
        /// 敵の行動テーブル選択: 伏せがあれば movesVsSet、召喚トークンがいれば movesVsTokens を優先
        /// (優先度: 伏せ反応 &gt; トークン反応 &gt; 通常)
        /// </summary>
        public static IReadOnlyList<EnemyMove> SelectMoveTable(EnemyDef def, bool playerHasSetCards, bool playerHasTokens = false)
        {
            if (playerHasSetCards && def.MovesVsSet != null && def.MovesVsSet.Count > 0) return def.MovesVsSet;
            if (playerHasTokens && def.MovesVsTokens != null && def.MovesVsTokens.Count > 0) return def.MovesVsTokens;
            return def.Moves;
        }

        /// <summary>
        /// 時喰らい型タイマー (激昂)。プレイヤーの累計詠唱数が enrageEveryCards の倍数に達した
        /// タイミングで強化する。時間ではなくプレイヤーのテンポに紐づく = 自己調整する。
        /// </summary>
        private static GameState TickCardTimers(GameState state)
        {
            int total = state.Player.CardsPlayedTotal;
            var s = state;
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var e = s.Enemies[i];
                if (e.Hp <= 0) continue;
                var def = Content.GetEnemyDef(e.EnemyId);
                var every = def.EnrageEveryCards;
                if (every == null || every <= 0) continue;
                if (total == 0 || total % every.Value != 0) continue;
                int amount = def.Enrage ?? 0;
                if (amount <= 0) continue;
                s = WithEnemy(s, i, x => x with { Strength = x.Strength + amount });
                s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = i, Amount = amount, Reason = "enrage-cards" });
            }
            return s;
        }

        // ==== 意図宣言 ====

        /// <summary>
        /// 全敵の意図を宣言する。行動と実値は宣言時にロールし、実値は非公開 (幅のみ表示)。
        /// 伏せの有無は宣言時点の状態で判定する。
        /// </summary>
        private static GameState DeclareIntents(GameState state)
        {
            var s = state;
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var rawEnemy = s.Enemies[i];
                if (rawEnemy.Hp <= 0) continue;
                var def = Content.GetEnemyDef(rawEnemy.EnemyId);
                // 連携: 他の仲間が生存中は攻撃+N (宣言時判定)
                bool anyOtherAlive = false;
                for (int j = 0; j < s.Enemies.Count; j++) if (j != i && s.Enemies[j].Hp > 0) { anyOtherAlive = true; break; }
                int bond = (def.BondStrength != null && anyOtherAlive) ? def.BondStrength.Value : 0;
                var enemy = bond > 0 ? rawEnemy with { Strength = rawEnemy.Strength + bond } : rawEnemy;
                // 盗んだ敵は次の宣言で必ず逃走する。flee の move を持たない盗人でも合成の逃走を宣言する
                EnemyMove fleeMove = null;
                for (int k = 0; k < def.Moves.Count; k++) if (def.Moves[k].Kind == EnemyActionKinds.Flee) { fleeMove = def.Moves[k]; break; }
                if (fleeMove == null) fleeMove = new EnemyMove { Id = "forced_flee", Kind = EnemyActionKinds.Flee, Weight = 1 };
                // 潜伏の殻が敵フェーズ中に割れていたら、この宣言は噛みつき
                EnemyMove biteMove = null;
                if (def.Burrow != null)
                {
                    for (int k = 0; k < def.Moves.Count; k++) if (def.Moves[k].Id == def.Burrow.Bite) { biteMove = def.Moves[k]; break; }
                }
                if (enemy.BiteNext == true && biteMove != null)
                {
                    var (biteIntent, rngB) = BuildIntent(s.Rng, biteMove, enemy.Strength, enemy.AtkScale ?? 1.0);
                    var enemiesB = MapIdx(s.Enemies, (e, j) => j == i ? e with { Intent = biteIntent, BiteNext = false } : e);
                    s = Events.Emit(s with { Rng = rngB, Enemies = enemiesB }, new GameEvent_EnemyIntentDeclared { EnemyIndex = i, Intent = biteIntent });
                    continue;
                }
                // バランス崩し: 直前の攻撃を完全に防がれていたら、この宣言は隙 (ローテは進めない)
                if (enemy.StaggeredNext == true)
                {
                    var staggerMove = new EnemyMove { Id = "stagger", Kind = EnemyActionKinds.Rest, Weight = 1 };
                    var (restIntent, rngS) = BuildIntent(s.Rng, staggerMove, enemy.Strength, enemy.AtkScale ?? 1.0);
                    var enemiesS = MapIdx(s.Enemies, (e, j) => j == i ? e with { Intent = restIntent, StaggeredNext = false } : e);
                    s = Events.Emit(s with { Rng = rngS, Enemies = enemiesS }, new GameEvent_EnemyIntentDeclared { EnemyIndex = i, Intent = restIntent });
                    continue;
                }
                if ((enemy.StolenGold ?? 0) > 0 && (enemy.Intent == null || enemy.Intent.Kind != EnemyActionKinds.Flee))
                {
                    var (fleeIntent, rngF) = BuildIntent(s.Rng, fleeMove, enemy.Strength, enemy.AtkScale ?? 1.0);
                    var enemies2 = MapIdx(s.Enemies, (e, j) => j == i ? e with { Intent = fleeIntent } : e);
                    s = Events.Emit(s with { Rng = rngF, Enemies = enemies2 }, new GameEvent_EnemyIntentDeclared { EnemyIndex = i, Intent = fleeIntent });
                    continue;
                }
                // フェーズ変化: HP50%以下の行動テーブルが最優先
                bool belowHalf = enemy.Hp <= enemy.MaxHp * 0.5 && (def.MovesBelowHalf != null || def.SequenceBelowHalf != null);
                // 単独時テーブル: 仲間が全滅したら切替。優先度は HP半分 > 単独時 > 通常
                bool whenAlone = !belowHalf && (def.MovesWhenAlone != null || def.SequenceWhenAlone != null) && !anyOtherAlive;
                // 回数カウンタのフェーズ変化: 対象行動を規定回数宣言したら恒久切替
                bool phaseSwitched = def.PhaseAfterUses != null && (enemy.KeyMoveUses ?? 0) >= def.PhaseAfterUses.Uses;
                IReadOnlyList<string> sequence = belowHalf
                    ? def.SequenceBelowHalf
                    : whenAlone
                        ? def.SequenceWhenAlone
                        : phaseSwitched
                            ? def.PhaseAfterUses?.Sequence
                            : def.Sequence;
                // 反応テーブル (伏せ/従者) を持つ敵は、条件付き意図として両分岐を宣言時に確定する。
                // 優先度は 伏せ反応 > 従者反応。編成で無効化された個体は分岐を持たない
                var vsSet = (enemy.NoReactTable != true && def.MovesVsSet != null && def.MovesVsSet.Count > 0) ? def.MovesVsSet : null;
                var vsTokens = (enemy.NoReactTable != true && def.MovesVsTokens != null && def.MovesVsTokens.Count > 0) ? def.MovesVsTokens : null;
                bool preferTokens = s.Player.SetCards.Count == 0 && Effects.HasHuntableTokens(s);
                IReadOnlyList<EnemyMove> reactTable = (belowHalf || whenAlone)
                    ? null
                    : preferTokens ? (vsTokens ?? vsSet) : (vsSet ?? vsTokens);
                string conditionalOn = reactTable == null ? null : (ReferenceEquals(reactTable, vsSet) ? "set" : "tokens");
                var baseTable = belowHalf
                    ? (def.MovesBelowHalf ?? def.Moves)
                    : whenAlone
                        ? (def.MovesWhenAlone ?? def.Moves)
                        : def.Moves;

                var rng = s.Rng;
                int nextPatternIndex = enemy.PatternIndex;
                // 通常分岐: sequence を持つ敵は固定ローテーション
                EnemyMove move;
                if (sequence != null && sequence.Count > 0)
                {
                    // sequenceLoopFrom: 一度きりの前奏→ループ。最後まで進んだら loopFrom へ戻る
                    int loopFrom = belowHalf
                        ? (def.SequenceBelowHalfLoopFrom ?? 0)
                        : (whenAlone || phaseSwitched) ? 0 : (def.SequenceLoopFrom ?? 0);
                    int len = sequence.Count;
                    int span = len - loopFrom;
                    string moveId = enemy.PatternIndex < len
                        ? sequence[enemy.PatternIndex]
                        : (span > 0 ? sequence[loopFrom + ((enemy.PatternIndex - loopFrom) % span)] : null);
                    EnemyMove found = null;
                    for (int k = 0; k < baseTable.Count; k++) if (baseTable[k].Id == moveId) { found = baseTable[k]; break; }
                    if (found == null) throw new InvalidOperationException($"敵 {def.Id} の sequence が未定義の行動を参照: {moveId}");
                    move = found;
                    nextPatternIndex = enemy.PatternIndex + 1;
                }
                else if (enemy.PatternIndex == 0 && def.Opener != null && !belowHalf && !whenAlone)
                {
                    // 初手固定: 最初の宣言だけ指定の行動 = その敵の問いをT1に見せる
                    EnemyMove found = null;
                    for (int k = 0; k < baseTable.Count; k++) if (baseTable[k].Id == def.Opener) { found = baseTable[k]; break; }
                    if (found == null) throw new InvalidOperationException($"敵 {def.Id} の opener が未定義の行動を参照: {def.Opener}");
                    move = found;
                    nextPatternIndex = enemy.PatternIndex + 1;
                }
                else
                {
                    // noRepeat=直前と同じ技は引かない / once=1戦闘1回。除外で候補が空なら制約なしで引く
                    var usable = new List<EnemyMove>();
                    for (int k = 0; k < baseTable.Count; k++)
                    {
                        var m = baseTable[k];
                        bool blockedOnce = m.Once == true && enemy.UsedOnce != null && enemy.UsedOnce.Contains(m.Id);
                        bool blockedRepeat = m.NoRepeat == true && m.Id == enemy.LastMoveId;
                        if (!blockedOnce && !blockedRepeat) usable.Add(m);
                    }
                    var table = usable.Count > 0 ? (IReadOnlyList<EnemyMove>)usable : baseTable;
                    var weights = new List<double>(table.Count);
                    for (int k = 0; k < table.Count; k++) weights.Add(table[k].Weight);
                    var (moveIdx, rngAfter) = Rng.WeightedIndex(rng, weights);
                    move = table[moveIdx];
                    rng = rngAfter;
                    nextPatternIndex = enemy.PatternIndex + 1;
                }
                // 回数カウンタ: 対象行動の宣言を数え、しきい値到達の瞬間に patternIndex を 0 へ
                var pau = def.PhaseAfterUses;
                int nextKeyUses = enemy.KeyMoveUses ?? 0;
                if (pau != null && !phaseSwitched && move.Id == pau.MoveId)
                {
                    nextKeyUses += 1;
                    if (nextKeyUses >= pau.Uses) nextPatternIndex = 0;
                }
                int usesSoFar = 0;
                if (enemy.MoveGrowth != null && enemy.MoveGrowth.TryGetValue(move.Id, out var uso)) usesSoFar = uso;
                var (intentRaw, rngA) = BuildIntent(rng, move, enemy.Strength, enemy.AtkScale ?? 1.0, usesSoFar);
                // 潜伏中は殻が育たない: 攻防一体のブロックは宣言から外し、防御行動そのものは「隙」に置き換える
                var intent =
                    enemy.BurrowActive != true
                        ? intentRaw
                        : intentRaw.Kind == EnemyActionKinds.Defend
                            ? intentRaw with { Kind = EnemyActionKinds.Rest, ShownMin = 0, ShownMax = 0, Actual = 0, AlsoBuff = null }
                            : intentRaw.AlsoDefend != null
                                ? intentRaw with { AlsoDefend = null }
                                : intentRaw;
                rng = rngA;
                bool growsMove = move.GrowPerUse != null || move.GrowHitsPerUse != null;
                IReadOnlyDictionary<string, int> nextGrowth;
                if (growsMove)
                {
                    var d = enemy.MoveGrowth != null ? new Dictionary<string, int>((IDictionary<string, int>)enemy.MoveGrowth) : new Dictionary<string, int>();
                    d[move.Id] = usesSoFar + 1;
                    nextGrowth = d;
                }
                else nextGrowth = enemy.MoveGrowth;

                EnemyIntentBranch alt = null;
                string condOn = conditionalOn;
                if (reactTable != null && reactTable.Count > 0)
                {
                    var weightsA = new List<double>(reactTable.Count);
                    for (int k = 0; k < reactTable.Count; k++) weightsA.Add(reactTable[k].Weight);
                    var (altIdx, rngB) = Rng.WeightedIndex(rng, weightsA);
                    rng = rngB;
                    var (altIntent, rngC) = BuildIntent(rng, reactTable[altIdx], enemy.Strength, enemy.AtkScale ?? 1.0);
                    rng = rngC;
                    alt = ToBranch(altIntent, null);
                }
                else if (!belowHalf && !whenAlone && enemy.NoReactTable != true && move.SetAlt != null)
                {
                    // 行動単位の条件分岐: 伏せ札があるとこの行動が setAlt の行動に変わる
                    var sa = move.SetAlt;
                    var altMove = new EnemyMove
                    {
                        Id = $"{move.Id}@set",
                        Weight = 1,
                        Kind = sa.Kind,
                        Min = sa.Min,
                        Max = sa.Max,
                        Hits = sa.Hits,
                        Inflict = sa.Inflict,
                        AlsoDefend = sa.AlsoDefend,
                        AlsoBuff = sa.AlsoBuff,
                    };
                    var (altIntent, rngC) = BuildIntent(rng, altMove, enemy.Strength, enemy.AtkScale ?? 1.0);
                    rng = rngC;
                    alt = ToBranch(altIntent, sa.IgnoreFreshness == true ? (bool?)true : null);
                    condOn = "set";
                }

                // 蜃気楼の面 (C型レリック): 実値を常時公開 = 宣言時に幅を実値へ畳む
                var shown = s.RevealIntents == true ? intent with { ShownMin = intent.Actual, ShownMax = intent.Actual } : intent;
                if (alt != null && s.RevealIntents == true) alt = alt with { ShownMin = alt.Actual, ShownMax = alt.Actual };
                var declared = (condOn != null && alt != null) ? shown with { ConditionalOn = condOn, Alt = alt } : shown;
                // 盗みは宣言と同時に成立する
                int stolen = declared.Kind == EnemyActionKinds.StealGold ? declared.Actual : 0;
                var declaredMove = move;
                var nextGrowthLocal = nextGrowth;
                int nextPatternLocal = nextPatternIndex;
                int nextKeyLocal = nextKeyUses;
                var enemies = MapIdx(s.Enemies, (e, j) =>
                {
                    if (j != i) return e;
                    var updated = e with
                    {
                        Intent = declared,
                        PatternIndex = nextPatternLocal,
                        KeyMoveUses = nextKeyLocal,
                        LastMoveId = declaredMove.Id,
                        MoveGrowth = nextGrowthLocal ?? e.MoveGrowth,
                    };
                    if (declaredMove.Once == true)
                    {
                        var uo = e.UsedOnce != null ? new List<string>(e.UsedOnce) : new List<string>();
                        uo.Add(declaredMove.Id);
                        updated = updated with { UsedOnce = uo };
                    }
                    if (stolen > 0) updated = updated with { StolenGold = (e.StolenGold ?? 0) + stolen };
                    return updated;
                });
                s = Events.Emit(s with { Rng = rng, Enemies = enemies }, new GameEvent_EnemyIntentDeclared { EnemyIndex = i, Intent = declared });
                if (stolen > 0) s = Events.Emit(s, new GameEvent_GoldStolen { EnemyIndex = i, Amount = stolen });
            }
            return s;
        }

        private static EnemyIntentBranch ToBranch(EnemyIntent it, bool? ignoreFreshness) => new EnemyIntentBranch
        {
            IgnoreFreshness = ignoreFreshness,
            Kind = it.Kind,
            ShownMin = it.ShownMin,
            ShownMax = it.ShownMax,
            Actual = it.Actual,
            Hits = it.Hits,
            Inflict = it.Inflict,
            AlsoDefend = it.AlsoDefend,
            AlsoBuff = it.AlsoBuff,
        };

        /// <summary>行動1つから意図 (幅表示 + 非公開の実値) を組み立てる。強化は攻撃にのみ乗り、攻撃は最低1にクランプ</summary>
        private static (EnemyIntent Intent, RngState Rng) BuildIntent(RngState rng, EnemyMove move, int strength, double atkScale = 1.0, int uses = 0)
        {
            // 技の恒久成長: 宣言回数×growPerUse を min/max に、×growHitsPerUse をヒット数に加算
            int grow = (move.GrowPerUse ?? 0) * uses;
            int growHits = (move.GrowHitsPerUse ?? 0) * uses;
            int? gMin = move.Min != null ? (int?)(move.Min.Value + grow) : null;
            int? gMax = move.Max != null ? (int?)(move.Max.Value + grow) : null;
            int? gHits = (move.Hits != null || growHits > 0) ? (int?)((move.Hits ?? 1) + growHits) : null;
            int actual = 0;
            var next = rng;
            if (gMin != null && gMax != null)
            {
                var r = Rng.NextInt(rng, gMin.Value, gMax.Value);
                actual = r.Value;
                next = r.Next;
            }
            int bonus = move.Kind == EnemyActionKinds.Attack ? strength : 0;
            // 打点倍率: 攻撃の基礎値だけに乗算・四捨五入。強化は倍率の後に加算
            Func<int, int> scale = v => move.Kind == EnemyActionKinds.Attack ? JsRound(v * atkScale) : v;
            Func<int, int> clamp = v => move.Kind == EnemyActionKinds.Attack ? Math.Max(1, v) : v;
            var intent = new EnemyIntent
            {
                Kind = move.Kind,
                ShownMin = clamp(scale(gMin ?? 0) + bonus),
                ShownMax = clamp(scale(gMax ?? 0) + bonus),
                Actual = clamp(scale(actual) + bonus),
                Hits = gHits,
                MirrorHits = move.MirrorHits == true ? (bool?)true : null,
                Inflict = move.Inflict,
                AlsoDefend = move.AlsoDefend,
                AlsoBuff = move.AlsoBuff,
            };
            return (intent, next);
        }

        /// <summary>自ターン開始: ブロック0リセット・エナジー全回復・置物の開始時効果・ドロー・敵意図宣言</summary>
        private static GameState StartPlayerTurn(GameState state, int turn)
        {
            var s = state with
            {
                Turn = turn,
                Phase = CombatPhases.PlayerTurn,
                // 通常ブロックはリセット。氷壁 (iceBlock) は持ち越される。
                // 上限のスナップショットもここで更新 = このターン中のランプは上限参照札に乗らない
                Player = state.Player with
                {
                    Block = 0,
                    Energy = state.Player.EnergyMax,
                    EnergyMaxAtTurnStart = state.Player.EnergyMax + (state.EnergyMaxRefBonus ?? 0),
                    CardsPlayedThisTurn = 0,
                    SetsThisTurn = 0,
                    PlaysThisTurn = 0,
                    AttacksPlayedThisTurn = 0,
                    HealsThisTurn = 0,
                    WeakFreshThisPhase = 0,
                    FreeResetUid = null,
                    // 見切り: 前のターンから置きっぱなしの伏せ札は「織り込み済み」になる
                    SetCards = MapIdx(state.Player.SetCards, (c, _) => c.SetFresh == true ? c with { SetFresh = false } : c),
                },
            };
            // ターン装甲の累計リセット: 自ターン開始〜次の自ターン開始が「1ターン」
            s = s with { Enemies = MapIdx(s.Enemies, (e, _) => (e.DamageThisTurn ?? 0) > 0 ? e with { DamageThisTurn = 0 } : e) };
            s = Events.Emit(s, new GameEvent_TurnStarted { Turn = turn, Hand = s.Player.Hand.Select(c => c.Def.Name).ToList() });
            // ドローを onTurnStart 誘発より先に行う。霞み: ドロー-2・最低3枚
            s = Effects.DrawCards(s, (s.Player.Mist ?? 0) > 0 ? Math.Max(3, s.Player.DrawPerTurn - 2) : s.Player.DrawPerTurn);
            s = Effects.RunPermanentTriggers(s, "onTurnStart", FirstAliveOrZero(s));
            // ターン開始誘発で敵が全滅したら即座に勝利を確定する
            s = CheckCombatEnd(s);
            if (s.Phase == CombatPhases.Won || s.Phase == CombatPhases.Lost) return s;
            return DeclareIntents(s);
        }

        /// <summary>
        /// 分裂: 倒れた分裂親から小型を場に出す。勝利判定より先に走る。
        /// 分裂体は生成時に意図を宣言し、その敵フェーズから行動する (本家Slime準拠)
        /// </summary>
        private static GameState ProcessSplits(GameState state)
        {
            var s = state;
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var e = s.Enemies[i];
                if (e.Hp > 0 || e.Split == true || e.Fled == true) continue;
                var def = Content.GetEnemyDef(e.EnemyId);
                var splitInto = def.SplitInto;
                if (splitInto == null) continue;
                s = WithEnemy(s, i, x => x with { Split = true });
                s = Events.Emit(s, new GameEvent_EnemySplit { EnemyIndex = i, Into = splitInto.EnemyId, Count = splitInto.Count });
                var childDef = Content.GetEnemyDef(splitInto.EnemyId);
                // HPスケール継承: 親の実効倍率 (maxHp/素) を子にも掛ける
                double hpRatio = def.MaxHp > 0 ? (double)e.MaxHp / def.MaxHp : 1.0;
                int scaledChildHp = Math.Max(1, JsRound(childDef.MaxHp * hpRatio));
                for (int k = 0; k < splitInto.Count; k++)
                {
                    string moveId = null;
                    if (childDef.Sequence != null)
                    {
                        int mod = childDef.Sequence.Count != 0 ? childDef.Sequence.Count : 1;
                        int idx = k % mod;
                        moveId = idx < childDef.Sequence.Count ? childDef.Sequence[idx] : null;
                    }
                    EnemyMove move = null;
                    for (int q = 0; q < childDef.Moves.Count; q++) if (childDef.Moves[q].Id == moveId) { move = childDef.Moves[q]; break; }
                    if (move == null) move = childDef.Moves[0];
                    // stunned: 分裂体の初回意図は「隙」= 出現ターンは動かない
                    int childStrength = splitInto.Strength ?? 0;
                    EnemyIntent intent;
                    RngState rng2;
                    if (splitInto.Stunned == true)
                    {
                        intent = new EnemyIntent { Kind = EnemyActionKinds.Rest, ShownMin = 0, ShownMax = 0, Actual = 0 };
                        rng2 = s.Rng;
                    }
                    else
                    {
                        var r = BuildIntent(s.Rng, move, childStrength, e.AtkScale ?? 1.0);
                        intent = r.Intent;
                        rng2 = r.Rng;
                    }
                    var child = new EnemyState
                    {
                        EnemyId = splitInto.EnemyId,
                        Hp = scaledChildHp,
                        MaxHp = scaledChildHp,
                        Block = childDef.Burrow?.Block ?? childDef.StartingBlock ?? 0,
                        BurrowActive = childDef.Burrow != null ? (bool?)true : null,
                        Intent = intent,
                        Strength = childStrength,
                        AtkScale = e.AtkScale,
                        Burn = 0,
                        Confusion = 0,
                        Exposed = 0,
                        PatternIndex = splitInto.Stunned == true ? 0 : (k + 1) % (childDef.Sequence?.Count ?? 1),
                        Thorns = childDef.Thorns,
                        Artifact = childDef.Artifact,
                        Armor = childDef.Armor,
                    };
                    s = s with { Rng = rng2, Enemies = Append(s.Enemies, child) };
                    s = Events.Emit(s, new GameEvent_EnemyIntentDeclared { EnemyIndex = s.Enemies.Count - 1, Intent = intent });
                }
            }
            return s;
        }

        /// <summary>弔い強化: 仲間が倒れるたび (逃走は除く)、生存する mournStrength 持ちの筋力+N</summary>
        private static GameState ProcessMourning(GameState state)
        {
            var s = state;
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var dead = s.Enemies[i];
                if (dead.Hp > 0 || dead.Fled == true || dead.MournProcessed == true) continue;
                s = WithEnemy(s, i, x => x with { MournProcessed = true });
                for (int j = 0; j < s.Enemies.Count; j++)
                {
                    if (j == i || s.Enemies[j].Hp <= 0) continue;
                    var amount = Content.GetEnemyDef(s.Enemies[j].EnemyId).MournStrength;
                    if (amount == null || amount <= 0) continue;
                    int amt = amount.Value;
                    s = WithEnemy(s, j, x => x with { Strength = x.Strength + amt });
                    s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = j, Amount = amt, Reason = "mourn" });
                }
            }
            return s;
        }

        /// <summary>
        /// 潜伏の殻が自ターン中に割れた敵の意図をその場で噛みつきに差し替える。
        /// プレイヤーの行動が原因なので「窓が嘘をつかない」は保たれる
        /// </summary>
        public static GameState ApplyPendingBites(GameState state)
        {
            if (state.Phase != CombatPhases.PlayerTurn) return state;
            var s = state;
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var e = s.Enemies[i];
                if (e.Hp <= 0 || e.BiteNext != true) continue;
                var def = Content.GetEnemyDef(e.EnemyId);
                EnemyMove bite = null;
                if (def.Burrow != null)
                {
                    for (int k = 0; k < def.Moves.Count; k++) if (def.Moves[k].Id == def.Burrow.Bite) { bite = def.Moves[k]; break; }
                }
                if (bite == null) continue;
                var (intent, rng) = BuildIntent(s.Rng, bite, e.Strength, e.AtkScale ?? 1.0);
                s = s with { Rng = rng, Enemies = MapIdx(s.Enemies, (x, j) => j == i ? x with { Intent = intent, BiteNext = false } : x) };
                s = Events.Emit(s, new GameEvent_EnemyIntentDeclared { EnemyIndex = i, Intent = intent });
            }
            return s;
        }

        /// <summary>勝敗判定。すでに決着済みなら何もしない (CombatEnded の二重記録防止)</summary>
        public static GameState CheckCombatEnd(GameState state)
        {
            state = ApplyPendingBites(state);
            if (state.Phase == CombatPhases.Won || state.Phase == CombatPhases.Lost) return state;
            state = ProcessSplits(state);
            state = ProcessMourning(state);
            if (state.Player.Hp <= 0)
            {
                return Events.Emit(state with { Phase = CombatPhases.Lost }, new GameEvent_CombatEnded { Result = "lost" });
            }
            bool allDead = true;
            for (int i = 0; i < state.Enemies.Count; i++) if (state.Enemies[i].Hp > 0) { allDead = false; break; }
            if (allDead)
            {
                return Events.Emit(state with { Phase = CombatPhases.Won }, new GameEvent_CombatEnded { Result = "won" });
            }
            return state;
        }

        // ==== カードのプレイ ====

        /// <summary>山札/捨て札から選ぶ効果の種別 (引導・回収・サーチ)。1枚の札は1種だけ持てる</summary>
        public static string DeckChooseKindOf(CardDef def)
        {
            var kinds = new[] { "exhaustFromDeckChoose", "retrieveFromDiscard", "searchDeck" };
            foreach (var k in kinds)
            {
                for (int i = 0; i < def.Effects.Count; i++)
                {
                    var e = def.Effects[i];
                    if (e.Effect == k && e.Trigger == "onPlay") return k;
                }
            }
            return null;
        }

        /// <summary>
        /// PlayCard: コスト支払い→捨て札 (置物は場、消滅カードは消滅の山) へ→効果解決。
        /// </summary>
        public static GameState PlayCard(
            GameState state,
            string cardUid,
            int? modeIndex = null,
            IReadOnlyList<string> discardUids = null,
            int? targetIndex = null,
            IReadOnlyList<string> exhaustUids = null,
            string retrieveUid = null,
            IReadOnlyList<string> deckUids = null,
            IReadOnlyList<string> handUids = null,
            int? xAmount = null,
            string permanentUid = null)
        {
            if (state.Phase != CombatPhases.PlayerTurn) throw new InvalidOperationException("自ターン以外はカードをプレイできない");
            CardInstance card = null;
            for (int i = 0; i < state.Player.Hand.Count; i++) if (state.Player.Hand[i].Uid == cardUid) { card = state.Player.Hand[i]; break; }
            if (card == null) throw new InvalidOperationException($"手札にないカード: {cardUid}");
            if (!Effects.IsPlayableFromHand(card)) throw new InvalidOperationException($"{card.Def.Name} はプレイ不可 (リアクション専用)");
            // 殉教の誓い: 従者が場にいる時だけプレイできる
            if (!Effects.RetainerRequirementMet(state, card)) throw new InvalidOperationException($"{card.Def.Name} は場に従者が1体以上いる時だけプレイできる");
            // 拘束: 1ターンにプレイできるカードは上限枚数まで。伏せ・発動は制限しない
            if (state.Player.Restrain > 0 && (state.Player.PlaysThisTurn ?? 0) >= RESTRAIN_PLAY_CAP)
            {
                throw new InvalidOperationException($"拘束中は1ターンに{RESTRAIN_PLAY_CAP}枚までしかプレイできない (すでに{RESTRAIN_PLAY_CAP}枚プレイ済み)");
            }
            // マナ軽減トークン適用後の実効コストで支払う (素のコスト0は割引を消費しない)
            int cost = Effects.EffectiveCost(state, card);
            bool consumesDiscount =
                card.Def.Cost > 0 &&
                state.Player.NextCardDiscount > 0 &&
                card.Def.XCost != true &&
                card.FreeThisCombat != true;
            if (cost > state.Player.Energy) throw new InvalidOperationException($"エナジー不足: {card.Def.Name}");
            // Xコスト: 払う量は 1〜現在のエナジーから選ぶ (省略=全部)
            int xCap = state.Player.Energy;
            if (card.Def.XCost == true && xAmount != null)
            {
                if (xAmount.Value < 1 || xAmount.Value > xCap)
                {
                    throw new InvalidOperationException($"{card.Def.Name} の X は 1〜{xCap} で指定する (xAmount={xAmount})");
                }
            }
            int paidX = card.Def.XCost == true ? (xAmount ?? cost) : 0;
            var effCard = card;
            if (paidX != 0)
            {
                var expanded = new List<DeclarativeEffect>();
                for (int i = 0; i < card.Def.Effects.Count; i++)
                {
                    var e = card.Def.Effects[i];
                    if (e.XHits == true) { for (int k = 0; k < paidX; k++) expanded.Add(e with { XHits = null }); }
                    else expanded.Add(e);
                }
                effCard = card with { Def = card.Def with { Effects = expanded } };
            }
            // 骨刃の強化 (empowerShivs): ナイフトークンのダメージに常在ボーナスを注入
            if (card.Def.ShivToken == true)
            {
                int shivBonus = 0;
                for (int i = 0; i < state.Player.Permanents.Count; i++)
                {
                    var p = state.Player.Permanents[i];
                    for (int k = 0; k < p.Def.Effects.Count; k++)
                    {
                        var e = p.Def.Effects[k];
                        if (e.Effect == "empowerShivs") shivBonus += e.Amount ?? 0;
                    }
                }
                if (shivBonus > 0)
                {
                    var boosted = new List<DeclarativeEffect>();
                    for (int i = 0; i < effCard.Def.Effects.Count; i++)
                    {
                        var e = effCard.Def.Effects[i];
                        boosted.Add(e.Effect == "dealDamage" ? e with { Amount = (e.Amount ?? 0) + shivBonus } : e);
                    }
                    effCard = effCard with { Def = effCard.Def with { Effects = boosted } };
                }
            }

            // 育つ札 (growSelf): この戦闘で積み上げた加算を dealDamage に注入する
            if ((card.GrowBonus ?? 0) > 0)
            {
                int g = card.GrowBonus ?? 0;
                var grown = new List<DeclarativeEffect>();
                for (int i = 0; i < effCard.Def.Effects.Count; i++)
                {
                    var e = effCard.Def.Effects[i];
                    grown.Add((e.Effect == "dealDamage" && e.Trigger == "onPlay") ? e with { Amount = (e.Amount ?? 0) + g } : e);
                }
                effCard = effCard with { Def = effCard.Def with { Effects = grown } };
            }

            // 選択式カードの検証
            var modes = card.Def.Modes ?? (IReadOnlyList<CardMode>)new List<CardMode>();
            CardMode chosenMode = null;
            if (modes.Count > 0)
            {
                if (modeIndex == null || modeIndex.Value < 0 || modeIndex.Value >= modes.Count)
                {
                    throw new InvalidOperationException($"{card.Def.Name} は選択式: modeIndex (0〜{modes.Count - 1}) が必要");
                }
                chosenMode = modes[modeIndex.Value];
            }

            // 手札捨てコストの検証
            int discardCost = card.Def.DiscardCost ?? 0;
            var discards = discardUids ?? (IReadOnlyList<string>)new List<string>();
            if (discardCost > 0)
            {
                if (discards.Count != discardCost)
                {
                    throw new InvalidOperationException($"{card.Def.Name} は追加コストとして手札{discardCost}枚の指定が必要");
                }
                if (new HashSet<string>(discards).Count != discards.Count || discards.Contains(cardUid))
                {
                    throw new InvalidOperationException("捨てるカードの指定が不正 (重複または自分自身)");
                }
                foreach (var uid in discards)
                {
                    if (!state.Player.Hand.Any(c => c.Uid == uid)) throw new InvalidOperationException($"手札にないカード: {uid}");
                }
            }

            // 消滅コストの検証 (黒。捨てより重い代わりに墓地燃料になる)
            int exhaustCost = card.Def.ExhaustCost ?? 0;
            var exhausts = exhaustUids ?? (IReadOnlyList<string>)new List<string>();
            if (exhaustCost > 0)
            {
                if (exhausts.Count != exhaustCost)
                {
                    throw new InvalidOperationException($"{card.Def.Name} は追加コストとして手札{exhaustCost}枚の消滅指定が必要");
                }
                if (new HashSet<string>(exhausts).Count != exhausts.Count ||
                    exhausts.Contains(cardUid) ||
                    exhausts.Any(uid => discards.Contains(uid)))
                {
                    throw new InvalidOperationException("消滅させるカードの指定が不正 (重複・自分自身・捨てコストとの重複)");
                }
                foreach (var uid in exhausts)
                {
                    if (!state.Player.Hand.Any(c => c.Uid == uid)) throw new InvalidOperationException($"手札にないカード: {uid}");
                }
            }

            // 消滅置き場からの選択 (屍集め=手札へ / 死者再生=直接プレイ) の検証
            bool isRetrieve = card.Def.Effects.Any(e => e.Effect == "retrieveFromExhaust");
            bool isPlayFromExhaust = card.Def.Effects.Any(e => e.Effect == "playFromExhaust");
            CardInstance chosenFromExhaust = null;
            if (isRetrieve || isPlayFromExhaust)
            {
                if (retrieveUid == null)
                {
                    throw new InvalidOperationException($"{card.Def.Name} は消滅置き場のカード (retrieveUid) の指定が必要");
                }
                chosenFromExhaust = state.Player.ExhaustPile.FirstOrDefault(c => c.Uid == retrieveUid);
                if (chosenFromExhaust == null) throw new InvalidOperationException($"消滅置き場にないカード: {retrieveUid}");
                if (isPlayFromExhaust)
                {
                    // 直接プレイの制約
                    if (chosenFromExhaust.Def.Type == CardTypes.Reaction)
                    {
                        throw new InvalidOperationException("リアクションは直接プレイできない");
                    }
                    if ((chosenFromExhaust.Def.Modes?.Count ?? 0) > 0)
                    {
                        throw new InvalidOperationException("選択式カードは直接プレイできない");
                    }
                    if (chosenFromExhaust.Def.Effects.Any(e => e.Effect == "playFromExhaust" || e.Effect == "retrieveFromExhaust"))
                    {
                        throw new InvalidOperationException("コスト再利用カード自身は直接プレイできない (再帰の禁止)");
                    }
                }
            }

            // 引導 / 回収 / サーチ: 山札か捨て札から選ぶ札 (deckUids) の検証
            string chooseKind = DeckChooseKindOf(card.Def);
            int deckChooseN = 0;
            for (int i = 0; i < card.Def.Effects.Count; i++)
            {
                var e = card.Def.Effects[i];
                if (chooseKind != null && e.Effect == chooseKind) deckChooseN += e.Amount ?? 1;
            }
            IReadOnlyList<CardInstance> deckPool =
                chooseKind == "retrieveFromDiscard"
                    ? state.Player.DiscardPile
                    : chooseKind == "searchDeck"
                        ? state.Player.DrawPile
                        : (IReadOnlyList<CardInstance>)Concat(state.Player.DrawPile, state.Player.DiscardPile);
            string poolLabel = chooseKind == "retrieveFromDiscard" ? "捨て札" : chooseKind == "searchDeck" ? "山札" : "山札か捨て札";
            var deckChooseUids = deckChooseN > 0 ? (deckUids ?? (IReadOnlyList<string>)new List<string>()) : (IReadOnlyList<string>)new List<string>();
            if (deckChooseN > 0)
            {
                int need = Math.Min(deckChooseN, deckPool.Count);
                if (deckChooseUids.Count != need)
                {
                    throw new InvalidOperationException($"{card.Def.Name} は{poolLabel}から{need}枚の指定 (deckUids) が必要");
                }
                if (new HashSet<string>(deckChooseUids).Count != deckChooseUids.Count)
                {
                    throw new InvalidOperationException("deckUids の指定が重複している");
                }
                foreach (var uid in deckChooseUids)
                {
                    if (!deckPool.Any(c => c.Uid == uid)) throw new InvalidOperationException($"{poolLabel}に無いカード: {uid}");
                }
            }
            // 手札で鍛える (upgradeInHand): 自身以外の鍛えられる手札から選ぶ。候補が無ければ省略可
            int upgradeN = 0;
            for (int i = 0; i < card.Def.Effects.Count; i++)
            {
                var e = card.Def.Effects[i];
                if (e.Effect == "upgradeInHand" && e.Trigger == "onPlay") upgradeN += e.Amount ?? 1;
            }
            var upgradable = upgradeN > 0
                ? state.Player.Hand.Where(c => c.Uid != card.Uid && Upgrade.CanUpgradeInHand(c)).ToList()
                : new List<CardInstance>();
            var upgradeUids = upgradeN > 0 ? (handUids ?? (IReadOnlyList<string>)new List<string>()) : (IReadOnlyList<string>)new List<string>();
            if (upgradeN > 0)
            {
                int need = Math.Min(upgradeN, upgradable.Count);
                if (upgradeUids.Count != need)
                {
                    throw new InvalidOperationException($"{card.Def.Name} は手札から{need}枚の指定 (handUids) が必要");
                }
                if (new HashSet<string>(upgradeUids).Count != upgradeUids.Count) throw new InvalidOperationException("handUids の指定が重複している");
                foreach (var uid in upgradeUids)
                {
                    if (!upgradable.Any(c => c.Uid == uid)) throw new InvalidOperationException($"鍛えられる手札に無いカード: {uid}");
                }
            }

            // 殉教の誓い: 破壊する従者を permanentUid で選ぶ
            int sacrificeN = card.Def.Effects.Count(e => e.Effect == "sacrificeRetainer" && e.Trigger == "onPlay");
            CardInstance sacrificed = null;
            if (sacrificeN > 0)
            {
                if (permanentUid == null) throw new InvalidOperationException($"{card.Def.Name} は破壊する従者 (permanentUid) の指定が必要");
                var t = state.Player.Permanents.FirstOrDefault(p => p.Uid == permanentUid);
                if (t == null || t.Def.Retainer != true || t.Innate == true) throw new InvalidOperationException($"従者ではない、または場に無い置物: {permanentUid}");
                sacrificed = t;
            }

            // StS式ターゲティング: 生存2体以上で単体対象カードは targetIndex 必須。生存1体なら自動
            int aliveCount = state.Enemies.Count(e => e.Hp > 0);
            if (targetIndex != null)
            {
                EnemyState target = (targetIndex.Value >= 0 && targetIndex.Value < state.Enemies.Count) ? state.Enemies[targetIndex.Value] : null;
                if (target == null) throw new InvalidOperationException($"不正な対象: {targetIndex} (敵は{state.Enemies.Count}体)");
                if (target.Hp <= 0)
                {
                    var alive = new List<int>();
                    for (int i = 0; i < state.Enemies.Count; i++) if (state.Enemies[i].Hp > 0) alive.Add(i);
                    // 生存1体なら対象は一意なので、死亡枠を指しても自動でリターゲットする
                    if (alive.Count == 1)
                    {
                        targetIndex = alive[0];
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"対象 {targetIndex} はすでに{(target.Fled == true ? "逃走" : "倒れて")}いる (targetIndexは撃破済みを含む並び順。生存: {string.Join(",", alive)})");
                    }
                }
            }
            if (targetIndex == null && aliveCount > 1 && Effects.CardNeedsTarget(card, modeIndex))
            {
                throw new InvalidOperationException($"{card.Def.Name} は対象の指定 (targetIndex) が必要");
            }
            // 庇う: 護衛が生存中、単体対象は護衛に向かう
            int? redirectedFrom = null;
            if (targetIndex != null)
            {
                var t = (targetIndex.Value >= 0 && targetIndex.Value < state.Enemies.Count) ? state.Enemies[targetIndex.Value] : null;
                if (t != null && Content.GetEnemyDef(t.EnemyId).Guardian != true)
                {
                    int g = -1;
                    for (int i = 0; i < state.Enemies.Count; i++)
                    {
                        if (state.Enemies[i].Hp > 0 && Content.GetEnemyDef(state.Enemies[i].EnemyId).Guardian == true) { g = i; break; }
                    }
                    if (g >= 0 && g != targetIndex.Value)
                    {
                        redirectedFrom = targetIndex;
                        targetIndex = g;
                    }
                }
            }
            int enemyIndex = targetIndex ?? FindAlive(state.Enemies);
            bool isPermanent = card.Def.Type == CardTypes.Permanent;
            // 樹液: 急所を持つ敵が生存していれば消滅しない
            bool isExhaust =
                card.Def.Exhaust == true &&
                !(card.Def.ExhaustUnlessExposedEnemy == true && state.Enemies.Any(e => e.Hp > 0 && e.Exposed > 0));
            var removed = new HashSet<string> { cardUid };
            foreach (var u in discards) removed.Add(u);
            foreach (var u in exhausts) removed.Add(u);
            var discardedCards = state.Player.Hand.Where(c => discards.Contains(c.Uid)).ToList();
            var exhaustedCards = state.Player.Hand.Where(c => exhausts.Contains(c.Uid)).ToList();
            var s = state with
            {
                Player = state.Player with
                {
                    Energy = state.Player.Energy - (card.Def.XCost == true ? paidX : cost),
                    NextCardDiscount = consumesDiscount ? 0 : state.Player.NextCardDiscount,
                    Hand = state.Player.Hand.Where(c => !removed.Contains(c.Uid)).ToList(),
                    // プレイ中のカードはまだ捨て札に置かない (limbo)。火傷は捨て札に入らない
                    DiscardPile = Concat(state.Player.DiscardPile, discardedCards.Where(c => c.Def.Id != Content.SCALD_DEF.Id)),
                    Permanents = isPermanent ? Append(state.Player.Permanents, card) : state.Player.Permanents,
                    // プレイした消滅札自身も limbo (効果解決後に消滅置き場へ)
                    ExhaustPile = Concat(state.Player.ExhaustPile, exhaustedCards),
                },
            };
            if (discardedCards.Count > 0)
            {
                s = Events.Emit(s, new GameEvent_CardsDiscarded { CardIds = discardedCards.Select(c => c.Def.Id).ToList() });
            }
            s = Events.Emit(s, new GameEvent_CardPlayed { CardId = card.Def.Id });
            if (redirectedFrom != null)
            {
                // 庇う: 発生を必ずログに残す
                s = Events.Emit(s, new GameEvent_GuardianRedirected { FromIndex = redirectedFrom.Value, ToIndex = enemyIndex });
            }
            if (isPermanent)
            {
                s = s with { LastEnteredPermanentUid = card.Uid }; // 駆けつけ (ひなた) が「誰が出たか」を読む
                s = Events.Emit(s, new GameEvent_PermanentPlayed { CardId = card.Def.Id });
                // 置物登場の誘発 (白の接着剤)。自身の登場にも誘発する
                s = Effects.RunPermanentTriggers(s, "onPermanentEntered", enemyIndex);
            }
            if (sacrificed != null)
            {
                // 殉教: 選んだ従者を場から除く。置物が出た直後・効果解決の前に行う
                var gone = sacrificed;
                s = s with { Player = s.Player with { Permanents = s.Player.Permanents.Where(p => p.Uid != gone.Uid).ToList() } };
                s = Events.Emit(s, new GameEvent_RetainerSacrificed { CardId = gone.Def.Id });
            }
            // 消滅コストの支払い: 支払い専用誘発 → 消滅誘発 の順で1枚ごとに発火
            foreach (var paid in exhaustedCards)
            {
                s = Events.Emit(s, new GameEvent_CardExhausted { CardId = paid.Def.Id });
                s = Effects.RunPermanentTriggers(s, "onCostExhausted", enemyIndex);
            }
            s = Effects.FireExhaustTriggers(s, exhaustedCards.Count, enemyIndex);
            // 亡骸効果: 消滅コストで支払われた札は「プレイ以外の経路」なので発火する
            s = Effects.FireNecroEffects(s, exhaustedCards, enemyIndex);
            // 引導: 効果解決の前に行う = 直後のドロー効果と競合しない
            if (deckChooseUids.Count > 0 && chooseKind != "exhaustFromDeckChoose")
            {
                // 回収 (捨て札→手札) / サーチ (山札→手札)。山札の並びは崩さない (抜くだけ)
                var chosenSet = new HashSet<string>(deckChooseUids);
                bool fromDraw = chooseKind == "searchDeck";
                var source = fromDraw ? s.Player.DrawPile : s.Player.DiscardPile;
                var moved = source.Where(c => chosenSet.Contains(c.Uid)).ToList();
                s = s with
                {
                    Player = s.Player with
                    {
                        DrawPile = fromDraw ? s.Player.DrawPile.Where(c => !chosenSet.Contains(c.Uid)).ToList() : s.Player.DrawPile,
                        DiscardPile = fromDraw ? s.Player.DiscardPile : s.Player.DiscardPile.Where(c => !chosenSet.Contains(c.Uid)).ToList(),
                        Hand = Concat(s.Player.Hand, moved),
                    },
                };
                s = Events.Emit(s, new GameEvent_CardsMovedToHand { CardIds = moved.Select(c => c.Def.Id).ToList(), From = fromDraw ? "draw" : "discard" });
            }
            if (upgradeUids.Count > 0)
            {
                // 手札で鍛える: 焚き火と同じ UpgradeCard を手札のインスタンスに適用 (この戦闘限り)
                var set = new HashSet<string>(upgradeUids);
                s = s with { Player = s.Player with { Hand = s.Player.Hand.Select(c => set.Contains(c.Uid) ? Upgrade.UpgradeCard(c) : c).ToList() } };
                foreach (var c in s.Player.Hand.Where(c => set.Contains(c.Uid)).ToList()) s = Events.Emit(s, new GameEvent_CardUpgradedInHand { CardId = c.Def.Id });
            }
            if (card.Def.Effects.Any(e => e.Effect == "upgradeAllInHand" && e.Trigger == "onPlay"))
            {
                // 研ぎ澄まし (2026-09-07 本家 Armaments+): 自身以外の鍛えられる手札を全部、この戦闘中鍛える (選択なし)
                var all = new HashSet<string>(s.Player.Hand.Where(c => c.Uid != card.Uid && Upgrade.CanUpgradeInHand(c)).Select(c => c.Uid));
                s = s with { Player = s.Player with { Hand = s.Player.Hand.Select(c => all.Contains(c.Uid) ? Upgrade.UpgradeCard(c) : c).ToList() } };
                foreach (var c in s.Player.Hand.Where(c => all.Contains(c.Uid)).ToList()) s = Events.Emit(s, new GameEvent_CardUpgradedInHand { CardId = c.Def.Id });
            }
            if (deckChooseUids.Count > 0 && chooseKind == "exhaustFromDeckChoose")
            {
                var chosenSet = new HashSet<string>(deckChooseUids);
                var chosenCards = Concat(s.Player.DrawPile, s.Player.DiscardPile).Where(c => chosenSet.Contains(c.Uid)).ToList();
                s = s with
                {
                    Player = s.Player with
                    {
                        DrawPile = s.Player.DrawPile.Where(c => !chosenSet.Contains(c.Uid)).ToList(),
                        DiscardPile = s.Player.DiscardPile.Where(c => !chosenSet.Contains(c.Uid)).ToList(),
                        ExhaustPile = Concat(s.Player.ExhaustPile, chosenCards),
                    },
                };
                foreach (var c in chosenCards) s = Events.Emit(s, new GameEvent_CardExhausted { CardId = c.Def.Id });
                s = Effects.FireExhaustTriggers(s, chosenCards.Count, enemyIndex);
                s = Effects.FireNecroEffects(s, chosenCards, enemyIndex);
            }
            // 反復 (青の呪文コピー): 呪文なら反復トークンを1つ消費し、効果を「2回」解決する
            bool echoed = card.Def.Type == CardTypes.Spell && state.Player.SpellEchoes > 0;
            if (echoed)
            {
                s = s with { Player = s.Player with { SpellEchoes = s.Player.SpellEchoes - 1 } };
                s = Events.Emit(s, new GameEvent_SpellEchoed { CardId = card.Def.Id });
            }
            for (int echoPass = 0; echoPass < (echoed ? 2 : 1); echoPass++)
            {
                if (chosenMode != null)
                {
                    // 虚弱の判定用フラグ (モード効果もカードのプレイ)
                    s = s with { ResolvingCardPlay = true, AngerFiredThisPlay = false };
                    // 共通部 (工房「効果の合体」の置き場) はモードを問わず先に解決する
                    for (int i = 0; i < effCard.Def.Effects.Count; i++)
                    {
                        var effect = effCard.Def.Effects[i];
                        if (effect.Trigger == "onPlay") s = Effects.ResolveEffectTargeted(s, effect, enemyIndex);
                    }
                    for (int i = 0; i < chosenMode.Effects.Count; i++)
                    {
                        s = Effects.ResolveEffectTargeted(s, chosenMode.Effects[i], enemyIndex);
                    }
                    s = s with { ResolvingCardPlay = false };
                }
                else
                {
                    s = Effects.ResolveOnPlayEffects(s, effCard, enemyIndex);
                }
            }
            // 「攻撃プレイ後」誘発: 解決した効果にダメージが含まれていたか (物理・呪文を問わない)
            var resolvedEffects = new List<DeclarativeEffect>();
            for (int i = 0; i < effCard.Def.Effects.Count; i++) if (effCard.Def.Effects[i].Trigger == "onPlay") resolvedEffects.Add(effCard.Def.Effects[i]);
            if (chosenMode != null) resolvedEffects.AddRange(chosenMode.Effects);
            if (resolvedEffects.Any(Effects.IsDamageEffect))
            {
                // 攻撃数参照: 自身の解決後に加算
                s = s with { Player = s.Player with { AttacksPlayedThisTurn = (s.Player.AttacksPlayedThisTurn ?? 0) + 1 } };
                s = Effects.RunPermanentTriggers(s, "onAttackPlayed", enemyIndex);
                s = FireSelfSetTriggers(s, "onAttackPlayed", enemyIndex);
            }
            // 「カードをプレイするたび」の誘発 (種類を問わない)
            s = Effects.RunPermanentTriggers(s, "onCardPlayed", enemyIndex);
            // 呪文プレイの誘発: 伏せ札の自己誘発 + 置物
            if (card.Def.Type == CardTypes.Spell)
            {
                s = FireSelfSetTriggers(s, "onSpellPlayed", enemyIndex);
                s = Effects.RunPermanentTriggers(s, "onSpellPlayed", enemyIndex);
            }
            // 衝動プレイの誘発 (赤の接着剤: 刹那の焔)
            if (state.Player.ImpulseUids.Contains(cardUid))
            {
                s = Effects.RunPermanentTriggers(s, "onImpulsePlayed", enemyIndex);
            }
            // ランダム火力の誘発。カード単位で1回数える
            if (card.Def.Effects.Any(e => e.Effect == "dealDamageRandom"))
            {
                s = s with { Player = s.Player with { RandomPlayedThisCombat = s.Player.RandomPlayedThisCombat + 1 } };
                s = Effects.RunPermanentTriggers(s, "onRandomPlayed", enemyIndex);
            }
            // 詠唱数は効果解決の後に加算する = そのカード自身は数えない
            s = s with
            {
                Player = s.Player with
                {
                    CardsPlayedThisTurn = s.Player.CardsPlayedThisTurn + 1,
                    CardsPlayedTotal = s.Player.CardsPlayedTotal + 1,
                    PlaysThisTurn = (s.Player.PlaysThisTurn ?? 0) + 1,
                },
            };
            s = TickCardTimers(s);
            // 増殖 (addCopyToDiscard): このカードのコピーを捨て札に加える (この戦闘限りのトークン扱い)
            bool hasCardOps = card.Def.Effects.Any(e => e.Effect == "addCopyToDiscard" || e.Effect == "growSelf");
            int copies = 0;
            if (hasCardOps)
            {
                for (int i = 0; i < card.Def.Effects.Count; i++)
                {
                    var e = card.Def.Effects[i];
                    if (e.Effect == "addCopyToDiscard" && e.Trigger == "onPlay") copies += e.Amount ?? 1;
                }
            }
            if (copies > 0)
            {
                var made = new List<CardInstance>(copies);
                for (int i = 0; i < copies; i++)
                {
                    made.Add(new CardInstance { Uid = $"copy_{s.EventLog.Count}_{i}_{card.Def.Id}", Def = card.Def, Token = true });
                }
                s = s with { Player = s.Player with { DiscardPile = Concat(s.Player.DiscardPile, made) } };
                s = Events.Emit(s, new GameEvent_CardCopied { CardId = card.Def.Id, Count = copies });
            }
            // 育つ札 (growSelf): 解決後に加算を積む。捨て札へ置くインスタンスに乗せる
            int grow = 0;
            if (hasCardOps)
            {
                for (int i = 0; i < card.Def.Effects.Count; i++)
                {
                    var e = card.Def.Effects[i];
                    if (e.Effect == "growSelf" && e.Trigger == "onPlay") grow += e.Amount ?? 0;
                }
            }
            var landed = grow > 0 ? card with { GrowBonus = (card.GrowBonus ?? 0) + grow } : card;
            if (grow > 0) s = Events.Emit(s, new GameEvent_CardGrew { CardId = card.Def.Id, Bonus = landed.GrowBonus ?? 0 });
            // limbo からの着地: プレイし終えたカードをここで捨て札 (消滅札は消滅置き場) へ置く
            if (isExhaust)
            {
                s = s with { Player = s.Player with { ExhaustPile = Append(s.Player.ExhaustPile, landed) } };
                s = Events.Emit(s, new GameEvent_CardExhausted { CardId = card.Def.Id });
                s = Effects.FireExhaustTriggers(s, 1, enemyIndex);
            }
            else if (!isPermanent)
            {
                s = s with { Player = s.Player with { DiscardPile = Append(s.Player.DiscardPile, landed) } };
            }
            // 屍集め: 消滅置き場から手札へ戻す。戻した札はこの戦闘中0E。Xコスト札は対象外
            if (isRetrieve && retrieveUid != null)
            {
                var chosen = s.Player.ExhaustPile.FirstOrDefault(c => c.Uid == retrieveUid);
                if (chosen != null)
                {
                    bool free = chosen.Def.XCost != true;
                    s = s with
                    {
                        Player = s.Player with
                        {
                            ExhaustPile = s.Player.ExhaustPile.Where(c => c.Uid != retrieveUid).ToList(),
                            Hand = Append(s.Player.Hand, free ? chosen with { FreeThisCombat = true } : chosen),
                        },
                    };
                    s = Events.Emit(s, new GameEvent_CardRetrieved { CardId = chosen.Def.Id });
                }
            }
            // 死者再生: 消滅置き場のカードをコストを支払わず直接プレイする
            if (isPlayFromExhaust && retrieveUid != null)
            {
                var chosen = s.Player.ExhaustPile.FirstOrDefault(c => c.Uid == retrieveUid);
                if (chosen != null)
                {
                    s = Events.Emit(s, new GameEvent_CardPlayedFromExhaust { CardId = chosen.Def.Id });
                    if (chosen.Def.Type == CardTypes.Permanent)
                    {
                        s = s with
                        {
                            Player = s.Player with
                            {
                                ExhaustPile = s.Player.ExhaustPile.Where(c => c.Uid != retrieveUid).ToList(),
                                Permanents = Append(s.Player.Permanents, chosen),
                            },
                            LastEnteredPermanentUid = chosen.Uid,
                        };
                        s = Events.Emit(s, new GameEvent_PermanentPlayed { CardId = chosen.Def.Id });
                        s = Effects.RunPermanentTriggers(s, "onPermanentEntered", enemyIndex);
                    }
                    s = Events.Emit(s, new GameEvent_CardPlayed { CardId = chosen.Def.Id });
                    s = Effects.ResolveOnPlayEffects(s, chosen, enemyIndex);
                    if (chosen.Def.Effects.Where(e => e.Trigger == "onPlay").Any(Effects.IsDamageEffect))
                    {
                        s = Effects.RunPermanentTriggers(s, "onAttackPlayed", enemyIndex);
                        s = FireSelfSetTriggers(s, "onAttackPlayed", enemyIndex);
                    }
                    if (chosen.Def.Type == CardTypes.Spell)
                    {
                        s = FireSelfSetTriggers(s, "onSpellPlayed", enemyIndex);
                        s = Effects.RunPermanentTriggers(s, "onSpellPlayed", enemyIndex);
                    }
                    // 直接プレイも「プレイ」として詠唱数に数える
                    s = s with
                    {
                        Player = s.Player with
                        {
                            CardsPlayedThisTurn = s.Player.CardsPlayedThisTurn + 1,
                            CardsPlayedTotal = s.Player.CardsPlayedTotal + 1,
                            PlaysThisTurn = (s.Player.PlaysThisTurn ?? 0) + 1,
                        },
                    };
                    s = TickCardTimers(s);
                }
            }
            return CheckCombatEnd(s);
        }

        /// <summary>
        /// 亡骸プレイ (黒): 消滅置き場の necroCost 持ち札を一度だけプレイする。
        /// プレイ後はゲームから完全に取り除かれる。割引・反復の対象外
        /// </summary>
        public static GameState PlayNecro(GameState state, string cardUid, int? targetIndex = null)
        {
            if (state.Phase != CombatPhases.PlayerTurn) throw new InvalidOperationException("自ターン以外はカードをプレイできない");
            var card = state.Player.ExhaustPile.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) throw new InvalidOperationException($"消滅置き場にないカード: {cardUid}");
            var cost = card.Def.NecroCost;
            if (cost == null) throw new InvalidOperationException($"{card.Def.Name} は亡骸プレイを持たない");
            // 拘束は亡骸プレイにも効く (プレイヤー発行のプレイは全て上限の内)
            if (state.Player.Restrain > 0 && (state.Player.PlaysThisTurn ?? 0) >= RESTRAIN_PLAY_CAP)
            {
                throw new InvalidOperationException($"拘束中は1ターンに{RESTRAIN_PLAY_CAP}枚までしかプレイできない (すでに{RESTRAIN_PLAY_CAP}枚プレイ済み)");
            }
            if (cost.Value > state.Player.Energy) throw new InvalidOperationException($"エナジー不足: {card.Def.Name}");
            int aliveCount = state.Enemies.Count(e => e.Hp > 0);
            if (targetIndex != null)
            {
                var target = (targetIndex.Value >= 0 && targetIndex.Value < state.Enemies.Count) ? state.Enemies[targetIndex.Value] : null;
                if (target == null || target.Hp <= 0) throw new InvalidOperationException($"不正な対象: {targetIndex}");
            }
            if (targetIndex == null && aliveCount > 1 && Effects.CardNeedsTarget(card, null))
            {
                throw new InvalidOperationException($"{card.Def.Name} は対象の指定 (targetIndex) が必要");
            }
            int enemyIndex = targetIndex ?? FindAlive(state.Enemies);
            var s = state with
            {
                Player = state.Player with
                {
                    Energy = state.Player.Energy - cost.Value,
                    // ゲームから完全に取り除く (消滅置き場にも戻らない)
                    ExhaustPile = state.Player.ExhaustPile.Where(c => c.Uid != cardUid).ToList(),
                },
            };
            s = Events.Emit(s, new GameEvent_NecroPlayed { CardId = card.Def.Id });
            s = Events.Emit(s, new GameEvent_CardPlayed { CardId = card.Def.Id });
            s = Effects.ResolveOnPlayEffects(s, card, enemyIndex);
            if (card.Def.Effects.Where(e => e.Trigger == "onPlay").Any(Effects.IsDamageEffect))
            {
                s = Effects.RunPermanentTriggers(s, "onAttackPlayed", enemyIndex);
                s = FireSelfSetTriggers(s, "onAttackPlayed", enemyIndex);
            }
            s = Effects.RunPermanentTriggers(s, "onCardPlayed", enemyIndex);
            if (card.Def.Type == CardTypes.Spell)
            {
                s = FireSelfSetTriggers(s, "onSpellPlayed", enemyIndex);
                s = Effects.RunPermanentTriggers(s, "onSpellPlayed", enemyIndex);
            }
            // 亡骸プレイも「プレイ」として詠唱数に数える
            s = s with
            {
                Player = s.Player with
                {
                    CardsPlayedThisTurn = s.Player.CardsPlayedThisTurn + 1,
                    CardsPlayedTotal = s.Player.CardsPlayedTotal + 1,
                    PlaysThisTurn = (s.Player.PlaysThisTurn ?? 0) + 1,
                },
            };
            s = TickCardTimers(s);
            return CheckCombatEnd(s);
        }

        // ==== ターン終了と敵フェーズ ====

        /// <summary>EndTurn: 勢いリセット・衝動の失効・延焼処理をして、敵フェーズを解決する</summary>
        public static GameState EndTurn(GameState state)
        {
            if (state.Phase != CombatPhases.PlayerTurn) throw new InvalidOperationException("自ターン以外はターン終了できない");
            var s = Events.Emit(state, new GameEvent_TurnEnded { Turn = state.Turn, Unplayed = state.Player.Hand.Select(c => c.Def.Name).ToList() });
            // 勢いは自ターン終了時にリセット。弱体・虚弱もここで1減る。
            // 疾風の王: 勢いの半分 (切り捨て) を次のターンへ持ち越す
            bool carryHalf = s.Player.Permanents.Any(p => p.Def.Effects.Any(e => e.Effect == "momentumCarryHalf"));
            s = s with
            {
                Player = s.Player with
                {
                    Momentum = carryHalf ? (int)Math.Floor(s.Player.Momentum / 2.0) : 0,
                    SpellEchoes = 0,
                    Weak = Math.Max(0, s.Player.Weak - 1),
                    Frail = Math.Max(0, s.Player.Frail - 1),
                    Restrain = Math.Max(0, s.Player.Restrain - 1),
                    Mist = Math.Max(0, (s.Player.Mist ?? 0) - 1),
                },
            };
            // 火傷・烙印: 自ターン終了時に手札にあると疼く (火傷=2/枚・烙印=1/枚)。ブロックで防げない
            {
                int scalds = s.Player.Hand.Count(c => c.Def.Id == Content.SCALD_DEF.Id);
                int brands = s.Player.Hand.Count(c => c.Def.Id == Content.BRAND_DEF.Id || c.Def.Id == Content.GUILT_DEF.Id);
                int burnHp = scalds * 2 + brands * 1;
                if (burnHp > 0)
                {
                    s = s with { Player = s.Player with { Hp = s.Player.Hp - burnHp } };
                    s = Events.Emit(s, new GameEvent_ScaldTick { Count = scalds + brands, Amount = burnHp });
                    s = CheckCombatEnd(s);
                    if (s.Phase == CombatPhases.Lost) return s;
                }
            }
            // 衝動 (このターン限りの手札) は未使用なら消滅する
            if (s.Player.ImpulseUids.Count > 0)
            {
                var impulse = new HashSet<string>(s.Player.ImpulseUids);
                var expired = s.Player.Hand.Where(c => impulse.Contains(c.Uid)).ToList();
                s = s with
                {
                    Player = s.Player with
                    {
                        Hand = s.Player.Hand.Where(c => !impulse.Contains(c.Uid)).ToList(),
                        ExhaustPile = Concat(s.Player.ExhaustPile, expired),
                        ImpulseUids = new List<string>(),
                    },
                };
                foreach (var card in expired)
                {
                    s = Events.Emit(s, new GameEvent_CardExhausted { CardId = card.Def.Id });
                }
                // 衝動失効も消滅 = 亡者の合唱が誘発する
                int aliveIdx = FirstAliveOrZero(s);
                s = Effects.FireExhaustTriggers(s, expired.Count, aliveIdx);
                // 亡骸効果: 衝動で引いた亡骸札の失効もプレイ以外の消滅なので発火する
                s = Effects.FireNecroEffects(s, expired, aliveIdx);
            }
            // 敵ブロックはこのタイミングで失効。潜伏の殻はブロックの器を借りているだけで失効しない
            s = s with { Enemies = MapIdx(s.Enemies, (e, _) => e.BurrowActive == true ? e : e with { Block = 0 }) };
            // 憤怒 (逆上) の参照値はフェーズ単位: 敵フェーズ開始時にリセットして受け直す
            s = s with { Player = s.Player with { DamageTakenLastEnemyPhase = 0, AttacksReceivedThisPhase = 0 } };
            // 延焼: 敵フェーズ開始時にダメージ (ブロック無視) を受けて1減る。延焼耐性は追加でN減る
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var enemy = s.Enemies[i];
                if (enemy.Hp <= 0 || enemy.Burn <= 0) continue;
                int amount = enemy.Burn;
                int decay = 1 + (Content.GetEnemyDef(enemy.EnemyId).BurnResist ?? 0);
                s = WithEnemy(s, i, e => e with
                {
                    Hp = e.Hp - amount,
                    Burn = Math.Max(0, e.Burn - decay),
                    // 延焼ティックも与ダメ系カウンタに算入する
                    DamageTakenTotal = (e.DamageTakenTotal ?? 0) + amount,
                    HpLostSinceRegen = (e.HpLostSinceRegen ?? 0) + amount,
                });
                s = Events.Emit(s, new GameEvent_BurnTick { EnemyIndex = i, Amount = amount });
                s = Effects.ApplyWakeCheck(s, i); // 被弾覚醒はどの経路の被弾でも
                // 与ダメ激昂の壁跨ぎ (effects.ts の dealDamageToEnemy と同則)
                {
                    var struck = s.Enemies[i];
                    var defE = Content.GetEnemyDef(struck.EnemyId);
                    if (defE.EnrageEveryDamage != null && struck.Hp > 0)
                    {
                        int total = struck.DamageTakenTotal ?? 0;
                        int before = total - amount;
                        int every = defE.EnrageEveryDamage.Value;
                        int crossings = (int)Math.Floor((double)total / every) - (int)Math.Floor((double)before / every);
                        int gain = crossings * (defE.Enrage ?? 2);
                        if (gain > 0)
                        {
                            s = WithEnemy(s, i, e => e with { Strength = e.Strength + gain });
                            s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = i, Amount = gain, Reason = "enrage-damage" });
                        }
                    }
                }
            }
            s = CheckCombatEnd(s); // 行動前に焼き切れば敵は動けない
            if (IsOver(s)) return s;
            return ProcessEnemyActions(s, 0);
        }

        /// <summary>
        /// 行動解決後の誘発窓 (post窓)。返し系リアクション・置物の茨はここで発動する。
        /// 行動が打ち消されていた場合 (lastAction が無い) は開かない。
        /// </summary>
        private static GameState PostActionStage(GameState state, int enemyIndex)
        {
            var act = state.LastAction;
            if (act == null || act.EnemyIndex != enemyIndex) return state;
            var resolved = new GameEvent_EnemyActionResolved
            {
                EnemyIndex = enemyIndex,
                Kind = act.Kind,
                HpLoss = act.HpLoss,
                Actual = act.Actual,
            };
            var s = Events.Emit(state, resolved);
            s = Hooks.DispatchHooks(s, resolved);
            if (s.Phase == CombatPhases.AwaitingReaction) return s; // post窓の割り込み → コマンド待ち
            return CheckCombatEnd(s);
        }

        /// <summary>割り込み (awaiting-reaction) から敵フェーズを再開する。State.ApplyCommand が方式コマンド処理後に呼ぶ</summary>
        public static GameState ContinueAfterWindow(GameState state)
        {
            var pending = state.PendingWindow;
            if (pending == null) throw new InvalidOperationException("割り込み情報がないのに再開が呼ばれた");
            var s = state with { PendingWindow = null, Phase = CombatPhases.PlayerTurn };
            s = CheckCombatEnd(s); // リアクションで決着していれば以降は実行されない
            if (IsOver(s)) return s;
            if (pending.Stage == "pre")
            {
                // pre窓の続き: 行動を実行し、解決後の post窓も通す
                s = ExecuteEnemyAction(s, pending.EnemyIndex);
                if (s.Enemies.All(e => e.Hp <= 0)) return CheckCombatEnd(s);
                // プレイヤーの致死は post窓の解決後に判定する
                s = PostActionStage(s, pending.EnemyIndex);
                if (s.Phase == CombatPhases.AwaitingReaction) return s;
                s = CheckCombatEnd(s);
                if (IsOver(s)) return s;
            }
            // stage 'post' はこの行動について残る処理なし
            return ProcessEnemyActions(s, pending.EnemyIndex + 1);
        }

        /// <summary>fromIndex 以降の敵の行動を順に解決する</summary>
        private static GameState ProcessEnemyActions(GameState state, int fromIndex)
        {
            var s = state;
            for (int i = fromIndex; i < s.Enemies.Count; i++)
            {
                var enemy = s.Enemies[i];
                if (enemy.Hp <= 0 || enemy.Intent == null) continue;
                var acting = Effects.EffectiveIntent(s, i);
                // 条件付き意図の分岐をここで確定させる (窓が嘘をつかないため)
                var locked = new EnemyIntent
                {
                    Kind = acting.Kind,
                    ShownMin = acting.ShownMin,
                    ShownMax = acting.ShownMax,
                    Actual = acting.Actual,
                    Hits = acting.Hits,
                    MirrorHits = acting.MirrorHits == true ? (bool?)true : null,
                    Inflict = acting.Inflict,
                    AlsoDefend = acting.AlsoDefend,
                    AlsoBuff = acting.AlsoBuff,
                };
                // 行動ごとにリアクション消費フラグをリセット (敵の1行動につき1回まで)
                s = s with
                {
                    Enemies = MapIdx(s.Enemies, (e, j) => j == i ? e with { Intent = locked } : e),
                    LastAction = null,
                    ReactionUsedThisAction = false,
                };
                // 行動実行の直前フック (pre窓): 打ち消し・軽減リアクションがここで発動/割り込みする
                var executing = new GameEvent_EnemyActionExecuting { EnemyIndex = i, Kind = locked.Kind };
                s = Events.Emit(s, executing);
                s = Hooks.DispatchHooks(s, executing);
                if (s.Phase == CombatPhases.AwaitingReaction) return s; // pre窓の割り込み → コマンド待ち
                s = CheckCombatEnd(s);
                if (IsOver(s)) return s;
                s = ExecuteEnemyAction(s, i);
                // 敵が倒れたらここで終了。プレイヤーの致死は post窓 (返し系) の解決後に判定する
                if (s.Enemies.All(e => e.Hp <= 0))
                {
                    s = CheckCombatEnd(s);
                    return s;
                }
                s = PostActionStage(s, i);
                if (s.Phase == CombatPhases.AwaitingReaction) return s;
                s = CheckCombatEnd(s);
                if (IsOver(s)) return s;
            }
            return FinishEnemyPhase(s);
        }

        /// <summary>
        /// 自己誘発リアクション: 伏せ札が自分の行動 (攻撃/呪文プレイ) で起爆する。
        /// 確認ウィンドウは挟まず自動発動し、捨て札へ。
        /// </summary>
        private static GameState FireSelfSetTriggers(GameState state, string trigger, int enemyIndex)
        {
            var firing = state.Player.SetCards.Where(c => c.Def.Effects.Any(e => e.Trigger == trigger)).ToList();
            if (firing.Count == 0) return state;
            var firingUids = new HashSet<string>(firing.Select(c => c.Uid));
            var s = state with
            {
                Player = state.Player with
                {
                    SetCards = state.Player.SetCards.Where(c => !firingUids.Contains(c.Uid)).ToList(),
                    DiscardPile = Concat(state.Player.DiscardPile, firing),
                },
            };
            foreach (var card in firing)
            {
                s = Events.Emit(s, new GameEvent_ReactionTriggered { CardId = card.Def.Id, Mode = s.ReactionMode });
                for (int i = 0; i < card.Def.Effects.Count; i++)
                {
                    var effect = card.Def.Effects[i];
                    if (effect.Trigger == trigger) s = Effects.ResolveEffectTargeted(s, effect, enemyIndex);
                }
            }
            return CheckCombatEnd(s);
        }

        /// <summary>状態異常をプレイヤーに付与する</summary>
        private static GameState ApplyStatusToPlayer(GameState state, StatusInflict inflict)
        {
            string status = inflict.Status;
            int amount = inflict.Amount;
            if (status == PlayerStatuss.Mist)
            {
                return Events.Emit(
                    state with { Player = state.Player with { Mist = (state.Player.Mist ?? 0) + amount } },
                    new GameEvent_StatusInflicted { Status = status, Amount = amount });
            }
            if (status == PlayerStatuss.Slow)
            {
                // justAppliedガードは脆弱と同則
                return Events.Emit(
                    state with { Player = state.Player with { Slow = (state.Player.Slow ?? 0) + amount, SlowFresh = true } },
                    new GameEvent_StatusInflicted { Status = status, Amount = amount });
            }
            if (status == PlayerStatuss.Weak || status == PlayerStatuss.Vulnerable || status == PlayerStatuss.Frail || status == PlayerStatuss.Restrain)
            {
                PlayerState player;
                if (status == PlayerStatuss.Weak)
                {
                    // 敵行動由来の付与のみ通る経路。同フェーズの返しには乗らない
                    player = state.Player with { Weak = state.Player.Weak + amount, WeakFreshThisPhase = (state.Player.WeakFreshThisPhase ?? 0) + amount };
                }
                else if (status == PlayerStatuss.Vulnerable)
                {
                    // 付与ガード: 敵フェーズ中に付与された脆弱は、同じフェーズの終了時減衰をスキップする
                    player = state.Player with { Vulnerable = state.Player.Vulnerable + amount, VulnerableFresh = true };
                }
                else if (status == PlayerStatuss.Frail)
                {
                    player = state.Player with { Frail = state.Player.Frail + amount };
                }
                else
                {
                    player = state.Player with { Restrain = state.Player.Restrain + amount };
                }
                return Events.Emit(state with { Player = player }, new GameEvent_StatusInflicted { Status = status, Amount = amount });
            }
            if (status == PlayerStatuss.Scald)
            {
                // 火傷: 手札に直接押し込む = 即時の圧。上限5枚/戦闘は累計で数える
                int existingScald = state.Player.ScaldsThisCombat ?? 0;
                int addScald = Math.Min(amount, SCALD_CAP - existingScald);
                if (addScald <= 0) return state;
                var scalds = new List<CardInstance>(addScald);
                for (int i = 0; i < addScald; i++)
                {
                    scalds.Add(new CardInstance
                    {
                        Uid = $"{Content.SCALD_DEF.Id}#{existingScald + i}_t{state.Turn}",
                        Def = Content.SCALD_DEF,
                        ScaldFresh = true,
                    });
                }
                var s2 = state with
                {
                    Player = state.Player with
                    {
                        Hand = Concat(state.Player.Hand, scalds),
                        ScaldsThisCombat = existingScald + addScald,
                    },
                };
                return Events.Emit(s2, new GameEvent_StatusInflicted { Status = PlayerStatuss.Scald, Amount = addScald });
            }
            if (status == PlayerStatuss.Junk)
            {
                // がらくた: 山札のランダムな位置に混ぜ込む (負傷と違い、すぐ引かされる)
                int existingJunk =
                    state.Player.Hand.Count(c => c.Def.Id == Content.JUNK_DEF.Id) +
                    state.Player.DrawPile.Count(c => c.Def.Id == Content.JUNK_DEF.Id) +
                    state.Player.DiscardPile.Count(c => c.Def.Id == Content.JUNK_DEF.Id);
                int addJunk = Math.Min(amount, JUNK_CAP - existingJunk);
                if (addJunk <= 0) return state;
                var drawPile = new List<CardInstance>(state.Player.DrawPile);
                var rng = state.Rng;
                for (int i = 0; i < addJunk; i++)
                {
                    var (pos, nextRng) = Rng.NextInt(rng, 0, drawPile.Count);
                    rng = nextRng;
                    drawPile.Insert(pos, new CardInstance { Uid = $"{Content.JUNK_DEF.Id}#{existingJunk + i}_t{state.Turn}", Def = Content.JUNK_DEF });
                }
                var s2 = state with { Rng = rng, Player = state.Player with { DrawPile = drawPile } };
                return Events.Emit(s2, new GameEvent_StatusInflicted { Status = PlayerStatuss.Junk, Amount = addJunk });
            }
            // wound: 全ゾーンの既存枚数を数えて上限までしか増えない
            int existing =
                state.Player.Hand.Count(c => c.Def.Id == Content.WOUND_DEF.Id) +
                state.Player.DrawPile.Count(c => c.Def.Id == Content.WOUND_DEF.Id) +
                state.Player.DiscardPile.Count(c => c.Def.Id == Content.WOUND_DEF.Id) +
                state.Player.ExhaustPile.Count(c => c.Def.Id == Content.WOUND_DEF.Id) +
                state.Player.SetCards.Count(c => c.Def.Id == Content.WOUND_DEF.Id);
            int add = Math.Min(amount, WOUND_CAP - existing);
            if (add <= 0) return state;
            var wounds = new List<CardInstance>(add);
            for (int i = 0; i < add; i++)
            {
                wounds.Add(new CardInstance { Uid = $"{Content.WOUND_DEF.Id}#{existing + i}_t{state.Turn}", Def = Content.WOUND_DEF });
            }
            var sw = state with { Player = state.Player with { DiscardPile = Concat(state.Player.DiscardPile, wounds) } };
            return Events.Emit(sw, new GameEvent_StatusInflicted { Status = PlayerStatuss.Wound, Amount = add });
        }

        /// <summary>敵1体の宣言済み行動を実行する (打ち消しフラグが立っていれば無効化)</summary>
        private static GameState ExecuteEnemyAction(GameState state, int enemyIndex)
        {
            var enemy = state.Enemies[enemyIndex];
            if (enemy.Hp <= 0 || enemy.Intent == null) return state;
            if (state.NegateNextAction)
            {
                // 打ち消しの成功に反応する置物 (青: 還流の水鏡)
                var baseState = state with { NegateNextAction = false };
                // 盗みは宣言と同時に成立するが、打ち消しに成功したら抱えた分を取り戻す
                var eff = Effects.EffectiveIntent(state, enemyIndex);
                if (eff != null && eff.Kind == EnemyActionKinds.StealGold && (enemy.StolenGold ?? 0) > 0)
                {
                    int back = eff.Actual;
                    baseState = WithEnemy(baseState, enemyIndex, e => e with { StolenGold = Math.Max(0, (e.StolenGold ?? 0) - back) });
                }
                var negated = Events.Emit(baseState, new GameEvent_ActionNegated { EnemyIndex = enemyIndex });
                return Effects.RunPermanentTriggers(negated, "onActionNegated", enemyIndex);
            }
            var intent = Effects.EffectiveIntent(state, enemyIndex);
            // 解決した行動を記録する (post窓の誘発判定に使う)
            Func<GameState, int, GameState> markResolved = (s0, hpLoss0) => s0 with
            {
                LastAction = new GameStateLastAction { EnemyIndex = enemyIndex, Kind = intent.Kind, HpLoss = hpLoss0, Actual = intent.Actual },
            };
            switch (intent.Kind)
            {
                case EnemyActionKinds.Attack:
                {
                    // 混乱 (仲間割れ): 攻撃が他のランダム生存敵 (いなければ自分) に向かう。
                    // プレイヤーへの攻撃ではないため post窓 (onAttacked) は開かない
                    if (enemy.Confusion > 0)
                    {
                        var others = new List<int>();
                        for (int i = 0; i < state.Enemies.Count; i++) if (state.Enemies[i].Hp > 0 && i != enemyIndex) others.Add(i);
                        var s = state;
                        int targetIdx = enemyIndex; // 他に生存敵がいなければ自分
                        if (others.Count == 1)
                        {
                            targetIdx = others[0];
                        }
                        else if (others.Count > 1)
                        {
                            var (pick, rng) = Rng.NextInt(state.Rng, 0, others.Count - 1);
                            targetIdx = others[pick];
                            s = s with { Rng = rng };
                        }
                        int total = intent.Actual * (intent.Hits ?? 1);
                        var target = s.Enemies[targetIdx];
                        int blockedC = Math.Min(target.Block, total);
                        int hpLossC = total - blockedC;
                        int ti = targetIdx;
                        s = s with
                        {
                            Enemies = MapIdx(s.Enemies, (e, i) =>
                            {
                                var x = e;
                                if (i == ti) x = x with { Block = x.Block - blockedC, Hp = x.Hp - hpLossC };
                                if (i == enemyIndex) x = x with { Confusion = x.Confusion - 1 };
                                return x;
                            }),
                        };
                        return Events.Emit(s, new GameEvent_ConfusedAttack { EnemyIndex = enemyIndex, TargetIndex = targetIdx, Amount = total });
                    }
                    // 連撃 (hits>1) は1発ずつ解決する。各ヒットに脆弱を補正し、通常ブロック→氷壁の順で消費する。
                    // 手数の鏡: 実行時のヒット数 = このターンにプレイした枚数 (伏せも数える。最低1)
                    int hits = intent.MirrorHits == true
                        ? Math.Max(1, state.Player.CardsPlayedThisTurn + (state.Player.SetsThisTurn ?? 0))
                        : (intent.Hits ?? 1);
                    int block = state.Player.Block;
                    int iceBlock = state.Player.IceBlock;
                    int dealtTotal = 0;
                    int hpLoss = 0;
                    for (int h = 0; h < hits; h++)
                    {
                        int v = intent.Actual;
                        // 威圧 (本家 Weak 化): スタックがあれば各ヒット-25% (切り捨て・最低1)
                        v = Effects.ApplyEnemyWeak(v, state.Enemies[enemyIndex]?.Weak);
                        // 静かな鈴 (C型レリック): 伏せ札がある間、各ヒット-N (最低1クランプは威圧と同則)
                        if ((state.SetDamageReduction ?? 0) > 0 && state.Player.SetCards.Count > 0)
                        {
                            v = Math.Max(1, v - (state.SetDamageReduction ?? 0));
                        }
                        // 脆弱: 敵の攻撃ダメージ50%増 (切り捨て)
                        if (state.Player.Vulnerable > 0) v = (int)Math.Floor(v * 1.5);
                        // 重り: +10%×このターンのプレイ枚数 (切り捨て)
                        if ((state.Player.Slow ?? 0) > 0 && (state.Player.PlaysThisTurn ?? 0) > 0)
                        {
                            v = (int)Math.Floor(v * (1 + 0.1 * (state.Player.PlaysThisTurn ?? 0)));
                        }
                        dealtTotal += v;
                        int blocked = Math.Min(block, v);
                        block -= blocked;
                        int remaining = v - blocked;
                        int iceBlocked = Math.Min(iceBlock, remaining);
                        iceBlock -= iceBlocked;
                        hpLoss += remaining - iceBlocked;
                    }
                    var sa = state with
                    {
                        Player = state.Player with
                        {
                            Block = block,
                            IceBlock = iceBlock,
                            Hp = state.Player.Hp - hpLoss,
                            // 憤怒 (逆上) の参照値: このフェーズで受けた攻撃ダメージを累積する
                            DamageTakenLastEnemyPhase = state.Player.DamageTakenLastEnemyPhase + hpLoss,
                            AttacksReceivedThisPhase = (state.Player.AttacksReceivedThisPhase ?? 0) + 1,
                        },
                    };
                    sa = Events.Emit(sa, new GameEvent_DamageDealt { Source = "enemy", Amount = dealtTotal, HpLoss = hpLoss, EnemyIndex = enemyIndex });
                    // バランス崩し: 攻撃を完全に防がれる (HP損失0) と体勢を崩し、次の宣言が隙になる
                    if (Content.GetEnemyDef(sa.Enemies[enemyIndex].EnemyId).Imbalanced == true && hpLoss == 0 && dealtTotal > 0)
                    {
                        sa = WithEnemy(sa, enemyIndex, e => e with { StaggeredNext = true });
                        sa = Events.Emit(sa, new GameEvent_EnemyStaggered { EnemyIndex = enemyIndex });
                    }
                    // 攻防一体 (alsoDefend): 攻撃と同時に固定ブロックを得る。潜伏中は得ない
                    if (intent.AlsoDefend != null && intent.AlsoDefend > 0 && sa.Enemies[enemyIndex]?.BurrowActive != true)
                    {
                        int ad = intent.AlsoDefend.Value;
                        sa = WithEnemy(sa, enemyIndex, e => e with { Block = e.Block + ad });
                        sa = Events.Emit(sa, new GameEvent_BlockGained { Target = "enemy", Amount = ad });
                    }
                    // 攻撃に付与された状態異常はダメージ後に適用
                    if (intent.Inflict != null) sa = ApplyStatusToPlayer(sa, intent.Inflict);
                    // 攻撃と同時の強化 (alsoBuff)。打ち消せば強化ごと消える
                    if (intent.AlsoBuff != null && enemyIndex < sa.Enemies.Count && sa.Enemies[enemyIndex] != null && sa.Enemies[enemyIndex].Hp > 0)
                    {
                        int ab = intent.AlsoBuff.Value;
                        sa = WithEnemy(sa, enemyIndex, e => e with { Strength = e.Strength + ab });
                        sa = Events.Emit(sa, new GameEvent_StrengthGained { EnemyIndex = enemyIndex, Amount = ab });
                    }
                    // 威圧の消費: 攻撃行動を1回実行するたび1減る (多段は1行動で1)
                    sa = sa with
                    {
                        Enemies = MapIdx(sa.Enemies, (e, j) => (j == enemyIndex && (e.Weak ?? 0) > 0) ? e with { Weak = e.Weak.Value - 1 } : e),
                    };
                    return markResolved(sa, hpLoss);
                }
                case EnemyActionKinds.Hex:
                {
                    // 状態異常の付与のみの行動 (妖術師の呪い)
                    var s = state;
                    if (intent.Inflict != null) s = ApplyStatusToPlayer(s, intent.Inflict);
                    return markResolved(s, 0);
                }
                case EnemyActionKinds.Defend:
                {
                    // 潜伏中は殻が育たない: 防御行動でもブロックを得ない
                    bool shellUp = state.Enemies[enemyIndex]?.BurrowActive == true;
                    var enemies = MapIdx(state.Enemies, (e, i) => (i == enemyIndex && !shellUp) ? e with { Block = e.Block + intent.Actual } : e);
                    var s = Events.Emit(state with { Enemies = enemies }, new GameEvent_BlockGained { Target = "enemy", Amount = intent.Actual });
                    // 防御と同時の強化 (用心深い影「隠れる」)
                    if (intent.AlsoBuff != null && enemyIndex < s.Enemies.Count && s.Enemies[enemyIndex] != null && s.Enemies[enemyIndex].Hp > 0)
                    {
                        int ab = intent.AlsoBuff.Value;
                        s = WithEnemy(s, enemyIndex, e => e with { Strength = e.Strength + ab });
                        s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = enemyIndex, Amount = ab });
                    }
                    return markResolved(s, 0);
                }
                case EnemyActionKinds.Buff:
                {
                    // 強化 (StSの筋力): 以降の攻撃宣言に加算される
                    var enemies = MapIdx(state.Enemies, (e, i) => i == enemyIndex ? e with { Strength = e.Strength + intent.Actual } : e);
                    return markResolved(
                        Events.Emit(state with { Enemies = enemies }, new GameEvent_StrengthGained { EnemyIndex = enemyIndex, Amount = intent.Actual }),
                        0);
                }
                case EnemyActionKinds.Heal:
                {
                    // 回復役: 最もHP割合の低い生存味方 (自分含む) を回復
                    int targetIdx = enemyIndex;
                    double worst = double.PositiveInfinity;
                    for (int i = 0; i < state.Enemies.Count; i++)
                    {
                        var e = state.Enemies[i];
                        if (e.Hp <= 0) continue;
                        double ratio = (double)e.Hp / e.MaxHp;
                        if (ratio < worst) { worst = ratio; targetIdx = i; }
                    }
                    var target = state.Enemies[targetIdx];
                    int healed = Math.Min(intent.Actual, target.MaxHp - target.Hp);
                    int tIdx = targetIdx;
                    var s = Events.Emit(
                        state with { Enemies = MapIdx(state.Enemies, (e, i) => i == tIdx ? e with { Hp = e.Hp + healed } : e) },
                        new GameEvent_EnemyHealed { EnemyIndex = enemyIndex, TargetIndex = targetIdx, Amount = healed });
                    return markResolved(s, 0);
                }
                case EnemyActionKinds.StealGold:
                {
                    // 盗みは宣言時に成立済み。実行時は「袋に詰める」だけの演出 = no-op
                    return markResolved(state, 0);
                }
                case EnemyActionKinds.Flee:
                {
                    // 逃走: 戦闘から離脱。hp:0+fled で既存の死亡判定・勝利判定がそのまま機能する
                    var s = Events.Emit(
                        state with { Enemies = MapIdx(state.Enemies, (e, i) => i == enemyIndex ? e with { Hp = 0, Fled = true, Intent = null } : e) },
                        new GameEvent_EnemyFled { EnemyIndex = enemyIndex });
                    return markResolved(s, 0);
                }
                case EnemyActionKinds.Rest:
                {
                    // 隙: 何もしない (斧鬼の息切れ)
                    return markResolved(state, 0);
                }
                case EnemyActionKinds.Hatch:
                {
                    // 孵化: この敵が hatchInto の敵へ変身する。HP全快・筋力0・ローテ先頭から
                    var def = Content.GetEnemyDef(enemy.EnemyId);
                    var into = def.HatchInto;
                    if (into == null) return markResolved(state, 0);
                    var newDef = Content.GetEnemyDef(into.EnemyId);
                    // HPスケール継承 (分裂体と同じ裁定)
                    double hatchRatio = def.MaxHp > 0 ? (double)enemy.MaxHp / def.MaxHp : 1.0;
                    int hatchedHp = Math.Max(1, JsRound(newDef.MaxHp * hatchRatio));
                    var s = state with
                    {
                        Enemies = MapIdx(state.Enemies, (e, j) => j == enemyIndex
                            ? e with
                            {
                                EnemyId = into.EnemyId,
                                Hp = hatchedHp,
                                MaxHp = hatchedHp,
                                Block = 0,
                                Strength = 0,
                                PatternIndex = 0,
                                KeyMoveUses = 0,
                                LastMoveId = null,
                                UsedOnce = new List<string>(),
                                MoveGrowth = new Dictionary<string, int>(),
                                Woken = false,
                                Artifact = newDef.Artifact ?? 0,
                                Intent = null,
                            }
                            : e),
                    };
                    s = Events.Emit(s, new GameEvent_EnemyHatched { EnemyIndex = enemyIndex, FromId = def.Id, IntoId = into.EnemyId });
                    // 生まれた姿の初手は次の宣言フェーズで決まる
                    return markResolved(s, 0);
                }
                case EnemyActionKinds.Mill:
                {
                    // 山札喰い: 山札の上N枚を消滅させる。亡骸・onCardExhausted は発火する
                    return markResolved(Effects.MillPlayerDeck(state, intent.Actual, enemyIndex), 0);
                }
                case EnemyActionKinds.Rally:
                {
                    // 応援: 生存する味方全体の強化
                    var enemies = MapIdx(state.Enemies, (e, _) => e.Hp > 0 ? e with { Strength = e.Strength + intent.Actual } : e);
                    var s = state with { Enemies = enemies };
                    for (int i = 0; i < s.Enemies.Count; i++)
                    {
                        if (s.Enemies[i].Hp > 0)
                        {
                            s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = i, Amount = intent.Actual });
                        }
                    }
                    return markResolved(s, 0);
                }
                case EnemyActionKinds.DestroyToken:
                {
                    // 従者狩り: 召喚トークンまたは従者 1体をランダムに破壊
                    var tokens = state.Player.Permanents.Where(p => p.Token == true || p.Def.Retainer == true).ToList();
                    if (tokens.Count == 0) return markResolved(state, 0);
                    var (idx, rng) = Rng.NextInt(state.Rng, 0, tokens.Count - 1);
                    var target = tokens[idx];
                    var s = state with
                    {
                        Rng = rng,
                        Player = state.Player with { Permanents = state.Player.Permanents.Where(p => p.Uid != target.Uid).ToList() },
                    };
                    s = Events.Emit(s, new GameEvent_TokenDestroyed { CardId = target.Def.Id });
                    return markResolved(s, 0);
                }
                case EnemyActionKinds.DestroySet:
                {
                    if (state.Player.SetCards.Count == 0) return markResolved(state, 0);
                    var s = state;
                    // 伏せ破壊への罰: onSetDestroyed 効果を破壊した敵に向けて発火
                    foreach (var card in state.Player.SetCards)
                    {
                        for (int i = 0; i < card.Def.Effects.Count; i++)
                        {
                            var effect = card.Def.Effects[i];
                            if (effect.Trigger == "onSetDestroyed") s = Effects.ResolveEffectTargeted(s, effect, enemyIndex);
                        }
                    }
                    s = s with
                    {
                        Player = s.Player with
                        {
                            SetCards = new List<CardInstance>(),
                            DiscardPile = Concat(s.Player.DiscardPile, state.Player.SetCards),
                        },
                    };
                    foreach (var card in state.Player.SetCards)
                    {
                        s = Events.Emit(s, new GameEvent_SetCardDestroyed { CardId = card.Def.Id });
                    }
                    // 伏せ破壊にも状態異常の付与が乗る (罠壊しの「がらくた」)
                    if (intent.Inflict != null) s = ApplyStatusToPlayer(s, intent.Inflict);
                    return markResolved(CheckCombatEnd(s), 0);
                }
            }
            // TS の switch は EnemyActionKind を網羅しているので到達しない
            return state;
        }

        /// <summary>敵フェーズ終端: 空振り計上フック→手札全捨て (3方式共通)→次ターン開始</summary>
        private static GameState FinishEnemyPhase(GameState state)
        {
            var ended = new GameEvent_EnemyPhaseEnded { Turn = state.Turn };
            var s = Events.Emit(state, ended);
            // 守り成功参照: この敵フェーズに攻撃を1回以上受け、HP損失が0なら「完全に凌いだ」
            s = s with
            {
                Player = s.Player with
                {
                    PerfectBlockLastPhase = (s.Player.AttacksReceivedThisPhase ?? 0) > 0 && s.Player.DamageTakenLastEnemyPhase == 0,
                },
            };
            s = Hooks.DispatchHooks(s, ended); // 空振り (ReactionWhiffed) の計上は方式固有
            // 脆弱は作用するフェーズ (敵フェーズ) の終了時に1減る。
            // ただしこのフェーズに付与された分は減らさない (justAppliedガード)
            s = s with
            {
                Player = s.Player with
                {
                    Vulnerable = s.Player.VulnerableFresh == true ? s.Player.Vulnerable : Math.Max(0, s.Player.Vulnerable - 1),
                    VulnerableFresh = false,
                    Slow = s.Player.SlowFresh == true ? (s.Player.Slow ?? 0) : Math.Max(0, (s.Player.Slow ?? 0) - 1),
                    SlowFresh = false,
                },
            };
            // 再生 (HP50%超のみ) と激昂
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var e = s.Enemies[i];
                if (e.Hp <= 0) continue;
                var def = Content.GetEnemyDef(e.EnemyId);
                if (def.Regen != null && e.Hp > e.MaxHp * 0.5)
                {
                    // regenBreak: このターンに閾値以上削られていたら再生しない
                    bool broken = def.RegenBreak != null && (e.HpLostSinceRegen ?? 0) >= def.RegenBreak.Value;
                    if (broken)
                    {
                        s = Events.Emit(s, new GameEvent_RegenBroken { EnemyIndex = i });
                    }
                    else
                    {
                        int amount = Math.Min(def.Regen.Value, e.MaxHp - e.Hp);
                        if (amount > 0)
                        {
                            s = WithEnemy(s, i, x => x with { Hp = x.Hp + amount });
                            s = Events.Emit(s, new GameEvent_RegenTicked { EnemyIndex = i, Amount = amount });
                        }
                    }
                }
                // 再生判定を通過したら累積をリセット
                s = WithEnemy(s, i, x => x with { HpLostSinceRegen = 0 });
                if (def.Enrage != null && def.EnrageEveryCards == null)
                {
                    // 上限なし = 本家のソフトタイマー
                    int amount = def.Enrage.Value;
                    s = WithEnemy(s, i, x => x with { Strength = x.Strength + amount });
                    s = Events.Emit(s, new GameEvent_StrengthGained { EnemyIndex = i, Amount = amount, Reason = "enrage-phase" });
                }
            }
            // 火傷の生存則: このフェーズに注入された火傷 (scaldFresh) は全捨てを生き残り、
            // 自ターンを過ごした火傷は全捨てで消える = 1回きり。保持 (retain) は手札に残る
            var oldHand = s.Player.Hand;
            s = s with
            {
                Player = s.Player with
                {
                    Hand = oldHand
                        .Where(c => (c.Def.Id == Content.SCALD_DEF.Id && c.ScaldFresh == true) || c.Def.Retain == true)
                        .Select(c => c.ScaldFresh == true ? c with { ScaldFresh = false } : c)
                        .ToList(),
                    DiscardPile = Concat(s.Player.DiscardPile, oldHand.Where(c => c.Def.Id != Content.SCALD_DEF.Id && c.Def.Retain != true)),
                },
            };
            return StartPlayerTurn(s, s.Turn + 1);
        }
    }
}
