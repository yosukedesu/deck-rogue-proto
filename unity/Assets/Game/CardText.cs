// CardText.cs — カード定義・敵の意図・戦闘ログを日本語1行に変換する読み取り専用の表示層。
// 語彙は src/ui/App.tsx の EFFECT_JA / COND_JA / TRIGGER_LABEL と src/ui/log.ts に合わせてある。
// 絵文字はフォント次第で豆腐になるので使わず、角括弧のラベルで表す。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class CardText
    {
        /// <summary>手札の表示用: onPlay のダメージ量に成長・勢い・弱体を掛けた実値を返す関数 (CardView が Body の前後で差し込む)。null なら素の数字</summary>
        public static Func<int, int> DamageModifier;
        /// <summary>補正後の数字の色 (上がった=苔・下がった=朱)。本家のカードの数字と同じ読み方</summary>
        public static string Colored(int baseAmt, int shown)
        {
            if (shown == baseAmt) return shown.ToString();
            return (shown > baseAmt ? "<color=#276a34>" : "<color=#a33a30>") + shown + "</color>";   // 紙の上で 5.5:1 (旧 #3f8f4a は 3.4:1)
        }
        // ---- 語彙表 ----

        static readonly Dictionary<string, string> TriggerJa = new Dictionary<string, string>
        {
            { "onPlay", "" },
            { "onAttackIncoming", "被攻撃前" },
            { "onAttacked", "被攻撃後" },
            { "onCardPlayed", "カードをプレイするたび" },
            { "onBlockGained", "ブロックを得るたび" },
            { "onActionNegated", "敵の行動を打ち消すたび" },
            { "onEnemyAction", "敵行動時" },
            { "onEnemyBuffed", "敵強化時" },
            { "onEnemyDefended", "敵防御時" },
            { "onEnemyActed", "敵の行動後" },
            { "onTurnStart", "毎T開始時" },
            { "onCombatStart", "戦闘開始時" },
            { "onAttackPlayed", "攻撃プレイ後" },
            { "onSpellPlayed", "呪文をプレイした時" },
            { "onSetDestroyed", "このからくりが壊された時・期限切れの時" },
            { "onHealed", "HPが回復するたび" },
            { "onHpLost", "カード効果でHPを失うたび" },
            { "onCardExhausted", "カードが消滅するたび" },
            { "onCostExhausted", "消滅コストを支払うたび" },
            { "onPermanentEntered", "置物が場に出るたび" },
            { "onImpulsePlayed", "衝動カードをプレイするたび" },
            { "onRandomPlayed", "運任せの札をプレイするたび" },
            { "onAetherGained", "霊気を得るたび" },
            { "onCardSet", "カードを仕込むたび" },
            { "onReactionFired", "からくりを動かすたび" },
            { "onScry", "占術するたび" }, // 潮読みの極み (青 2026-09-25)
            { "onScryDiscard", "占術で札を捨てるたび(1枚ごと)" }, // 渦見の鏡 (青 2026-09-25 Opus B の処方)
            { "onSetExpired", "からくりが期限切れになるたび" }, // ほどけ泡 (青 2026-09-25)
            { "onSelfExhausted", "亡骸" },
            { "onGrowthGained", "成長を得るたび" },
            { "onMomentumGained", "勢いを得るたび" },
            { "onLightGained", "灯を得るたび" },
            { "onLightDischarged", "灯を放出するたび" },
            { "onSparkPlayed", "火種を撃つたび" },
            { "onTurnEnd", "ターン終了時" },
            { "onShuffle", "山札を切り直すたび" },
            { "onEnemyDied", "敵を倒すたび" },
            { "onDamageTaken", "攻撃でHPを失った後" },
        };

        static readonly Dictionary<string, string> EffectJa = new Dictionary<string, string>
        {
            { "dealDamage", "ダメージN" },
            { "dealDamageRandom", "ランダムダメージN" },
            { "dealDamageDrain", "ドレインN(半分回復)" },
            { "dealDamageCleave", "キル連鎖N" },
            { "dealDamageExecute", "処刑N(HP25%以下で上限)" },
            { "dealDamagePerBlock", "ブロック×Nダメ" },
            { "dealDamagePerIceBlock", "氷壁×Nダメ" },
            { "dealDamagePerCardPlayed", "詠唱数×Nダメ" },
            { "dealDamagePerCardPlayedTotal", "累計プレイ数×Nダメ" },
            { "dealDamagePerEnergyMax", "上限×Nダメ" },
            { "dealDamagePerMomentum", "勢い×Nダメ(非消費)" },
            { "dealDamagePerExhaust", "消滅数×Nダメ" },
            { "dealDamagePerHandCard", "手札数×Nダメ" },
            { "dealDamagePerHeal", "回復回数×Nダメ" },
            { "dealDamagePerDamageTaken", "被ダメ×Nダメ" },
            { "dealDamagePerSelfHpLost", "失ったHP×Nダメ" },
            { "dealDamagePerPermanent", "置物数×Nダメ" },
            { "dealDamagePerRandomPlayed", "運任せ数×Nダメ" },
            { "dealDamagePerNegStrength", "下げた筋力×Nダメ" },
            { "dealDamagePerAttackPlayed", "このTの攻撃数×Nダメ" },
            { "dealDamagePerWeak", "対象の威圧×N追加ダメ" },
            { "gainBlock", "ブロックN" },
            { "gainBlockPerEnergyMax", "上限×Nブロック" },
            { "gainBlockPerPermanent", "置物数×Nブロック" },
            { "gainIceBlock", "氷壁N(持ち越し)" },
            { "gainIceBlockPerCardPlayed", "詠唱数×N氷壁" },
            { "gainIceBlockPerHandCard", "手札数×N氷壁" },
            { "gainBlockPerHandCard", "手札数×Nブロック" },
            { "staggerEnemy", "対象の体勢を崩す（次の行動が隙になる）" },
            { "stripRider", "対象のいまの行動の付随物（状態異常・同時強化・同時防御）を消す" },   // ギア 蝋の栓 (2026-09-18)
            { "singleHit", "対象のいまの攻撃の連撃を1回にする" },   // ギア 錆びた鎖
            { "nullifyNextEnemyAttack", "このターン最初に受ける攻撃1回のHP損失を0にする" },   // ギア 身代わりの符
            { "drawCardsNextTurn", "次のターンの開始時にN枚多くドロー" },
            { "gainEnergyNextTurn", "次のターンの開始時に一時マナ+N" },
            { "addLightNextTurn", "次のターンの開始時に灯+N" }, // 灯の埋め火・灯の集約 (2026-09-23)
            { "gainBlockNextTurn", "次のターンの開始時にブロック+N" },
            { "gainHp", "HP回復N" },
            { "loseHp", "自傷HP-N" },
            { "counter", "返しN" },
            { "negate", "打ち消し" },
            { "negateConvertIce", "打ち消し+実値ぶん氷壁" },
            { "drawCards", "Nドロー" },
            { "drawCardsPerCardPlayed", "詠唱数×Nドロー" },
            { "impulseDraw", "衝動ドローN(このT限り)" },
            { "gainEnergy", "一時マナ+N" },
            { "gainEnergyMax", "エナジー上限+N" },
            { "discountNext", "次のカード-N" },
            { "addGrowth", "成長+N" },
            { "doubleGrowth", "成長2倍" },
            { "dischargeGrowth", "成長放出(×Nダメ・全消費)" },
            { "dischargeGrowthBlock", "成長×Nブロック(全消費)" },
            { "addMomentum", "勢い+N" },
            { "doubleMomentum", "勢い2倍" },
            { "dischargeMomentumBlock", "勢い×Nブロック(全消費)" },
            { "dischargeMomentumBurn", "勢い×N延焼(全消費)" },
            { "dischargeMomentumDamage", "勢い×Nダメ(全消費)" },
            { "dischargeMomentumGrowth", "勢い÷Nを成長に(全消費)" },
            { "dischargeMomentumVolley", "勢い×Nダメを3回(全消費)" },
            { "momentumCarryHalf", "勢いの半分を持ち越す(常在)" },
            { "applyBurn", "延焼+N" },
            { "applyBurnPerDamageTaken", "被ダメ×N延焼" },
            { "dischargeBurn", "爆熱(延焼×Nダメ・全消費)" },
            { "addAether", "霊気+N" },
            { "dischargeAether", "霊気放出(×Nダメ・全消費)" },
            { "dischargeAetherDraw", "霊気×Nドロー(全消費)" },
            { "addLight", "灯+N" },
            { "dischargeLight", "灯を全て放出し、灯1につきNダメージ(灯の数だけヒット。全体は灯×N)" },
            { "dischargeLightRally", "灯を全て放出し、灯Nにつき全ての人形が1回動く" },
            { "dealDamagePerLight", "灯2につきNダメージ(切り捨て。灯は失わない)" }, // 灯篭の人形 (2026-09-20 灯と人形の結び)
            // 火種・放出の軸 (2026-09-20 夜。本家 Soul の白版)
            { "addCardToDraw", "火種N枚を山札のランダムな位置に混ぜる(この戦闘限り)" },
            { "addCardToDiscard", "火種N枚を捨て札に加える(この戦闘限り)" }, // 断ち切り (2026-09-23)
            { "transformDeckToToken", "山札の札N枚を選んで火種に変える" }, // 降霊 (2026-09-23)
            { "lightToSparks", "灯Nにつき火種1枚を山札へ(払った灯だけ失う)" },
            { "dealDamagePerSpark", "この戦闘で撃った火種×Nダメージ" },
            { "gainBlockPerSpark", "この戦闘で撃った火種×Nブロック" },
            // 青の3本柱 (2026-09-25): 潮読み (占術・手札を残す)・罠使い (からくりの回数・期限)
            { "scry", "占術N(山札の上N枚を見て、要らない札を捨て札へ)" },
            { "dealDamagePerScry", "この戦闘で占術で捨てた枚数×Nダメージ" },
            { "dealDamagePerTrapFired", "この戦闘でからくりが動いた回数×Nダメージ" },
            { "extendTrapLife", "仕込んでいるからくりすべての期限をNターン延ばす" },
            { "trapsNeverExpire", "この置物がある間、からくりは期限切れにならない" },
            { "retainHandUpTo", "この置物がある間、ターン終了時に手札をN枚まで選んで残せる" },
            { "retrieveZeroCostFromDiscard", "捨て札のコスト0の札を全て手札に戻す" },
            { "drawTypeFromDeck", "山札の仕込み札N枚を(上から見て最初のものを)手札に加える" },
            { "aetherCarryHalf", "この置物がある間、霊気を放出しても半分が残る" },
            { "retainedCostDown", "この置物がある間、敵ターンの後も手札に残った札はコスト-N(手札を離れると戻る)" }, // 火守りの盾 (2026-09-24 Opus ひなた P4「作る札に刈り取りを内蔵」)
            { "triggerRandomRetainer", "場の人形1体(ランダム)の効果を今1回解決(灯は産まない)" },
            { "dischargeLightWeaken", "灯を全て放出し、灯3につき敵全体に威圧N(灯3未満なら不発)" },
            { "consumeLight", "灯をN失う" }, // amount 付き (2026-09-23 灯の炉心)。省略の札は無い
            { "gainBlockPerLight", "灯2につきNブロック(灯は失わない)" },
            { "drawCardsPerLight", "灯2につきNドロー(上限あり。灯は失わない)" },
            { "lightCarryHalf", "【常在】灯を放出しても半分(切り捨て)が残る" },
            { "doubleLight", "灯を2倍にする" },
            { "addCasts", "詠唱数+N" },
            { "addSpellEcho", "反復+N(次の呪文2回解決)" },
            { "confuse", "混乱+N" },
            { "exposeEnemy", "急所+N" },
            { "weakenEnemy", "威圧N" },
            { "strengthenEnemy", "敵の筋力+N" },
            { "shatterBlock", "粉砕(敵ブロック全壊)" },
            { "shatterBlockConvert", "粉砕+破壊値ダメ" },
            { "exhaustFromDeck", "山札の上N枚を消滅(ミル)" },
            { "exhaustFromDeckChoose", "選んでN枚消滅(引導)" },
            { "recycleExhaust", "輪廻(消滅を山札へ・×Nダメ)" },
            { "retrieveFromExhaust", "消滅置き場から回収" },
            { "playFromExhaust", "消滅置き場から直接プレイ" },
            { "summonPermanent", "召喚N体" },
            { "addCardToHand", "トークンN枚を手札へ" },
            // 画面の語は「人形」(2026-09-24 Opus ひなた T3: 分列・捧げ・駆けつけに「従者」が残っていた)。内部名 (Retainer) は不変
            { "duplicateRetainers", "場の人形を1体ずつ複製" },
            { "sacrificeRetainer", "人形1体を選んで破壊" },
            { "copyRetainer", "人形1体を選び同じ人形をN体出す（残りの期限も写す）" },
            { "copyLastRetainer", "最後に点灯した人形と同じ人形をN体出す（残りの期限も写す）" },
            { "twinNextRetainer", "次に出す人形N体が2体になる（持ち越す）" },
            { "extendRetainerLife", "人形1体を選び期限をNターン延ばす" },
            { "extendAllRetainersLife", "場の人形すべての期限をNターン延ばす" },
            { "dismissUnlessLight", "灯がN未満ならこの置物は消える（捨て札へ）" },
            { "persistRetainer", "人形1体を選び期限を無くす（消えなくなる）" },
            { "triggerRetainersNow", "号令: 場の人形の効果を今すぐ1回ずつ解決 (登場ごとは除く)" },
            { "activateEnteredRetainer", "場に出た人形が即1回動く" },
            { "blessRetainers", "【常在】人形のダメージ・ブロック・回復+N" },
            { "empowerShivs", "【常在】ナイフ与ダメ+N" },
            { "gainSetSlot", "仕込み枠+N(この戦闘中)" },
            { "retrieveFromDiscard", "捨て札からN枚を手札へ(選ぶ)" },
            { "searchDeck", "山札からN枚を手札へ(選ぶ)" },
            { "addCopyToDiscard", "コピーN枚を捨て札へ" },
            { "growSelf", "プレイするたび与ダメ+N(この戦闘中)" },
            { "upgradeInHand", "手札のN枚をこの戦闘中鍛える" },
            { "upgradeAllInHand", "手札の全てをこの戦闘中鍛える" },
            { "gainMaxHp", "最大HP+N(戦闘後も残る)" },
            { "gainBlockPerMomentum", "勢い×Nブロック(失わない)" },
            { "addGrowthPerMomentum", "勢い2につき成長+N(失わない)" },
        };

        static readonly Dictionary<string, string> IntentKindJa = new Dictionary<string, string>
        {
            { "attack", "攻撃" }, { "defend", "防御" }, { "buff", "筋力上げ" }, { "rally", "応援" },
            { "heal", "回復" }, { "hex", "呪い" }, { "destroy-set", "からくり壊し" }, { "destroy-token", "人形狩り" },
            { "steal-gold", "盗み" }, { "flee", "逃走" }, { "mill", "山札喰い" }, { "rest", "隙" }, { "hatch", "孵化" }, { "summon", "召喚" },
        };

        static readonly Dictionary<string, string> StatusJa = new Dictionary<string, string>
        {
            { "weak", "弱体" }, { "vulnerable", "脆弱" }, { "frail", "虚弱" }, { "wound", "負傷" },
            { "junk", "がらくた" }, { "scald", "火傷" }, { "restrain", "拘束" }, { "mist", "霞み" }, { "slow", "重り" },
        };

        static readonly Dictionary<string, string> TypeJaMap = new Dictionary<string, string>
        {
            { "physical", "物理" }, { "spell", "呪文" }, { "reaction", "仕込み札" }, { "permanent", "置物" },
        };

        static readonly Dictionary<string, string> RarityJa = new Dictionary<string, string>
        {
            { "common", "C" }, { "uncommon", "U" }, { "rare", "R" }, { "boss", "ボス" }, { "shop", "店" }, { "event", "?" },
        };

        // ---- 小物 ----

        public static string TypeJa(string t)
        {
            string v;
            return TypeJaMap.TryGetValue(t == null ? "" : t, out v) ? v : (t == null ? "" : t);
        }

        public static string RarityLabel(string r)
        {
            if (string.IsNullOrEmpty(r)) return "";
            string v;
            return RarityJa.TryGetValue(r, out v) ? v : r;
        }

        public static string CostLabel(CardDef def)
        {
            if (def == null) return "";
            return def.XCost == true ? ((def.XBonus ?? 0) > 0 ? "X+" + def.XBonus.Value : "X") : def.Cost.ToString();   // X+N は触媒 cheaper × X (2026-09-14)
        }

        /// <summary>カードIDから名前 (合成札は合成の解決器で復元する)</summary>
        public static string CardName(string cardId)
        {
            try { return Content.GetCardDef(cardId).Name; }
            catch (Exception)
            {
                try
                {
                    var f = Fusion.ResolveFusedDef(cardId);
                    return f != null ? f.Name : cardId;
                }
                catch (Exception) { return cardId; }
            }
        }

        /// <summary>一覧の1行に収める短縮 (legacy Text は折り返すと行が重なるため)</summary>
        public static string Short(string s, int max)
        {
            if (s == null) return "";
            s = s.Replace("\n", " / ");
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }

        static string Num(double d)
        {
            return d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---- カード ----

        /// <summary>誘発の追加条件</summary>
        public static string ConditionLabel(EffectCondition c)
        {
            if (c == null) return "";
            var parts = new List<string>();
            if (c.HpAtOrBelowRatio.HasValue) parts.Add("自分のHPが" + Mathf_Round(c.HpAtOrBelowRatio.Value * 100.0) + "%以下");
            if (c.MinDamageTaken.HasValue) parts.Add(c.MinDamageTaken.Value + "以上のダメージを受けた");
            if (c.MaxActionValue.HasValue) parts.Add("敵の行動値が" + c.MaxActionValue.Value + "以下");
            if (c.MinActionValue.HasValue) parts.Add("敵の行動値が" + c.MinActionValue.Value + "以上");
            if (c.Blaze == true) parts.Add("猛り火(延焼合計8以上)");
            if (c.MinGrowth.HasValue) parts.Add("成長" + c.MinGrowth.Value + "以上");
            if (c.MinMomentum.HasValue) parts.Add("勢い" + c.MinMomentum.Value + "以上");
            if (c.MinLight.HasValue) parts.Add("灯" + c.MinLight.Value + "以上");
            if (c.EnemyIntent != null) parts.Add("対象の意図が" + KindJa(c.EnemyIntent));
            if (c.EnemyIntentNot != null) parts.Add("対象の意図が" + KindJa(c.EnemyIntentNot) + "以外");
            if (c.EnemyExposed == true) parts.Add("対象が急所持ち");
            if (c.PerfectBlockLastPhase == true) parts.Add("直前の敵フェーズを完全に凌いだ");
            if (c.TargetDead == true) parts.Add("とどめ");
            if (c.TargetAlive == true) parts.Add("倒せなければ");
            if (c.PerfectBlockThisPhase == true) parts.Add("この敵フェーズを完全に凌いだら");
            if (c.LastActionNoHpLoss == true) parts.Add("完全に凌いだ時");
            if (c.HealedThisTurn == true) parts.Add("このT先にカードで回復していたら");
            // レリック本家形 (2026-09-12)
            if (c.Turn.HasValue) parts.Add(c.Turn.Value + "ターン目");
            if (c.BlockZero == true) parts.Add("ブロックが0なら");
            if (c.NoAttackThisTurn == true) parts.Add("このターン攻撃札を1枚もプレイしていなければ");
            if (c.MaxPlaysThisTurn.HasValue) parts.Add("このターンのプレイが" + c.MaxPlaysThisTurn.Value + "枚以下なら");
            // 2026-09-09 抜けていた2種 (若幹の一撃・大地の唸りが「ダメージ6 ×2回」に化け、共鳴する茨の打ち消しが無条件に見えていた)
            if (c.MinEnergyMax.HasValue) parts.Add("エナジー上限" + c.MinEnergyMax.Value + "以上");
            if (c.ActionKinds != null && c.ActionKinds.Count > 0)
            {
                var ks = new List<string>();
                for (int i = 0; i < c.ActionKinds.Count; i++) ks.Add(KindJa(c.ActionKinds[i]));
                parts.Add("敵の行動が" + string.Join("・", ks.ToArray()) + "の時");
            }
            if (c.ActionKindsNot != null && c.ActionKindsNot.Count > 0)
            {
                var ks = new List<string>();
                for (int i = 0; i < c.ActionKindsNot.Count; i++) ks.Add(KindJa(c.ActionKindsNot[i]));
                parts.Add("敵の行動が" + string.Join("・", ks.ToArray()) + "以外の時");
            }
            if (parts.Count == 0) return "";
            return "[" + string.Join("かつ", parts.ToArray()) + "] ";
        }

        static int Mathf_Round(double v) { return (int)Math.Round(v, MidpointRounding.AwayFromZero); }

        static string KindJa(string kind)
        {
            string v;
            return IntentKindJa.TryGetValue(kind == null ? "" : kind, out v) ? v : (kind == null ? "" : kind);
        }

        /// <summary>効果1つを1行に</summary>
        public static string EffectLine(DeclarativeEffect e, string holderType)
        {
            var sb = new StringBuilder();
            if (e.Trigger == "onPlay")
            {
                if (holderType == CardTypes.Permanent) sb.Append("登場時: ");
            }
            else
            {
                string tj;
                sb.Append(TriggerJa.TryGetValue(e.Trigger, out tj) ? tj : e.Trigger);
                sb.Append(": ");
            }
            sb.Append(ConditionLabel(e.Condition));
            // every/once (レリック本家形 2026-09-12): 「3回ごと」「戦闘で1回だけ」
            if (e.Every.HasValue && e.Once != null) sb.Append("(" + (e.Once == "turn" ? "1ターンに" : "戦闘で") + e.Every.Value + "回目の時だけ) ");   // 嵐の目 (青 2026-09-25)
            else if (e.Every.HasValue) sb.Append("(" + (e.EveryScope == "turn" ? "1ターンに" : "") + e.Every.Value + "回ごとに1回) ");
            else if (e.Once != null) sb.Append("(" + (e.Once == "turn" ? "ターンに" : "戦闘で") + "1回だけ) ");
            if (e.Target == "all") sb.Append("敵全体に ");
            sb.Append(EffectBody(e));
            if (e.Pierce == true) sb.Append("(貫通)");
            if (e.XHits == true) sb.Append("×X回");
            if (e.VolleyHits.HasValue) sb.Append("×" + e.VolleyHits.Value + "回");
            if (e.GrowthMultiplier.HasValue) sb.Append("〔成長が×" + Num(e.GrowthMultiplier.Value) + "で乗る〕");
            if (e.MomentumMultiplier.HasValue) sb.Append("〔勢いが×" + Num(e.MomentumMultiplier.Value) + "で乗る〕");
            if (e.SpendBlock == true) sb.Append("(解決後にブロックを全て失う)");
            if (e.ExhaustThreshold.HasValue)
            {
                int mx = e.AmountMax.HasValue ? e.AmountMax.Value : 0;
                int am = e.Amount.HasValue ? e.Amount.Value : 0;
                sb.Append("〔忘却の刻" + e.ExhaustThreshold.Value + ": " + (mx < am ? (mx == 0 ? "以降は停止" : mx + "に減少") : mx + "に増える") + "〕");
            }
            return sb.ToString();
        }

        static string EffectBody(DeclarativeEffect e)
        {
            int amt = e.Amount.HasValue ? e.Amount.Value : 0;
            if (e.Effect == "dealDamageRandom")
            {
                return "ランダムダメージ" + amt + "〜" + (e.AmountMax.HasValue ? e.AmountMax.Value : amt);
            }
            if (e.Effect == "summonPermanent") return "召喚" + amt + "体: " + CardName(e.SummonId);
            if (e.Effect == "addCardToHand") return CardName(e.SummonId) + "を" + amt + "枚手札へ";
            if (e.Effect == "searchDeck" && e.CardType == "reaction") return "山札から仕込み札" + amt + "枚を手札へ(選ぶ)";   // 仕掛けの手配 (青 2026-09-25)
            string tpl;
            if (e.Effect == "dealDamage" && DamageModifier != null && (e.Trigger == null || e.Trigger == "onPlay"))
            {
                int shown = DamageModifier(amt);
                return "ダメージ" + Colored(amt, shown);
            }
            if (EffectJa.TryGetValue(e.Effect, out tpl)) return tpl.Replace("N", amt.ToString());
            return e.Effect + (e.Amount.HasValue ? " " + amt : "");
        }

        /// <summary>案B の本文 (2026-09-09): 数字 (+N/-N 含む) を 130% に、「(貫通)」のような短い括弧の注記を小さな下地つきの札に。
        /// タグ (色・スプライト) の中は触らない。表示層だけの加工で Body の語彙は不変</summary>
        public static string Emphasize(string body)
        {
            if (string.IsNullOrEmpty(body)) return body;
            var sb = new StringBuilder();
            int i = 0;
            while (i < body.Length)
            {
                char ch = body[i];
                if (ch == '<')
                {
                    int j = body.IndexOf('>', i);
                    if (j < 0) { sb.Append(body.Substring(i)); break; }
                    sb.Append(body, i, j - i + 1); i = j + 1; continue;
                }
                if (ch == '(')
                {
                    int j = body.IndexOf(')', i);
                    if (j > i + 1 && j - i - 1 <= 6)
                    {
                        sb.Append("<nobr><size=78%><mark=#3b2f2f22> ").Append(body, i + 1, j - i - 1).Append(" </mark></size></nobr>");   // 札の途中で折り返さない
                        i = j + 1; continue;
                    }
                }
                bool sign = (ch == '+' || ch == '-') && i + 1 < body.Length && char.IsDigit(body[i + 1]) && (i == 0 || !char.IsDigit(body[i - 1]));
                if (char.IsDigit(ch) || sign)
                {
                    int j = sign ? i + 1 : i;
                    while (j < body.Length && char.IsDigit(body[j])) j++;
                    sb.Append("<nobr><size=130%>").Append(body, i, j - i).Append("</size></nobr>");   // 「-1」の途中で折り返さない
                    i = j; continue;
                }
                sb.Append(ch); i++;
            }
            return sb.ToString();
        }

        /// <summary>カードの効果行 (選択式はモードごと)。改行区切り</summary>
        public static string Body(CardDef def)
        {
            // 状態異常の札は効果を持たないので本文が空だった (2026-09-23 人間ラン#17: 烙印の疼きの理由が札に無い)。Web の effectLineStrings と同じ文
            if (def.Id == "status_wound") return "使えない（ターン終了時に捨てられる）";
            if (def.Id == "status_scald") return "使えない。自ターン終了時に手札にあるとHP-2（この戦闘限り。捨て/消滅コストの支払いには使える）";
            if (def.Id == "status_brand") return "使えない。自ターン終了時に手札にあるとHP-1（デッキに残る呪い。ショップの除去で取り除ける。青い蝋燭があれば 0E・HP-1・消滅で出せる）";
            if (def.Id == "status_guilt") return "使えない。自ターン終了時に手札にあるとHP-1（仮初の呪い。5戦すると自然に消える）";
            var lines = Collapse(LinesOf(def.Effects, def.Type));
            if (def.Modes != null)
            {
                // 選択式: モード名は効果の言い換え (「7ダメージ」と「ダメージ7」) なので捨て、効果だけを ◆ で並べる (2026-09-09「ダメージ ダメージと読めて2回攻撃と勘違い」)
                for (int m = 0; m < def.Modes.Count; m++)
                {
                    var inner = Collapse(LinesOf(def.Modes[m].Effects, def.Type));
                    lines.Add("◆" + string.Join(" / ", inner.ToArray()));
                }
            }
            return string.Join("\n", lines.ToArray());
        }

        static List<string> LinesOf(IReadOnlyList<DeclarativeEffect> effects, string holderType)
        {
            var lines = new List<string>();
            if (effects == null) return lines;
            for (int i = 0; i < effects.Count; i++) lines.Add(EffectLine(effects[i], holderType));
            return lines;
        }

        /// <summary>同じ行の連続 (二連の蔦打ち = ダメージ4 / ダメージ4) は「ダメージ4 ×2回」に畳む = 多段が一目で分かる</summary>
        static List<string> Collapse(List<string> lines)
        {
            var res = new List<string>();
            int i = 0;
            while (i < lines.Count)
            {
                int j = i;
                while (j + 1 < lines.Count && lines[j + 1] == lines[i]) j++;
                int n = j - i + 1;
                res.Add(n > 1 ? lines[i] + " ×" + n + "回" : lines[i]);
                i = j + 1;
            }
            return res;
        }

        /// <summary>消滅・保持・追加コストなどの注記</summary>
        public static string Notes(CardDef def)
        {
            var n = CostNotes(def);
            n.AddRange(TrailNotes(def));
            return n.Count == 0 ? "" : string.Join(" / ", n.ToArray());
        }

        /// <summary>コスト側の注記 (X・追加コスト・亡骸プレイ・0E 条件・前提)。カードでは本文の先頭に普通の文で置く
        /// (2026-09-09 ユーザー「追加コストは付箋みたいに下に貼るのでなく、普通の表記で効果の最上部に」)</summary>
        public static List<string> CostNotes(CardDef def)
        {
            var n = new List<string>();
            if (def.XCost == true) n.Add((def.XBonus ?? 0) > 0 ? "X: エナジーを全て払う (払った量+" + def.XBonus.Value + "として解決)" : "X: エナジーを全て払う");
            if ((def.DiscardCost.HasValue ? def.DiscardCost.Value : 0) > 0) n.Add("追加コスト: 手札" + def.DiscardCost.Value + "枚を捨てる");
            if ((def.ExhaustCost.HasValue ? def.ExhaustCost.Value : 0) > 0) n.Add("追加コスト: 手札" + def.ExhaustCost.Value + "枚を消滅");
            if ((def.LightCost.HasValue ? def.LightCost.Value : 0) > 0) n.Add("追加コスト: 灯を" + def.LightCost.Value + "払う");
            if (def.NecroCost.HasValue) n.Add("亡骸プレイ " + def.NecroCost.Value + "E");
            if (def.FreeIfHandAllPhysical == true) n.Add("手札が物理だけなら0E");
            if (def.FreeIfHandAll != null) n.Add("手札が" + TypeJa(def.FreeIfHandAll) + "だけなら0E");
            if (def.FreeIfMomentumAtLeast.HasValue) n.Add("勢い" + def.FreeIfMomentumAtLeast.Value + "以上なら0E");
            if (def.RequiresRetainer == true) n.Add("プレイ条件: 場に人形が1体以上");   // 「従者」→「人形」(2026-09-24 T3。Web と同じ文)
            if (def.BlazeDiscount.HasValue) n.Add("猛り火中コスト-" + def.BlazeDiscount.Value);
            return n;
        }

        /// <summary>効果の後ろに付く注記 (消滅・保持・条件付き消滅・札の種類)。カードでは本文の末尾</summary>
        public static List<string> TrailNotes(CardDef def)
        {
            var n = new List<string>();
            if (def.Exhaust == true) n.Add("消滅");
            if (def.Retain == true) n.Add("保持");
            if (def.Echo == true) n.Add("反復内蔵 (効果を2回解決)");
            // 反復の2回目は同じ敵を狙う (2026-09-24 Opus ひなた E11 裁定B＝本家2と同じく据え置き)。反復内蔵の札と反復を配る札に同じ一文 (CLI の ECHO_MISS_NOTE)
            if (def.Echo == true || HasEffect(def, "addSpellEcho")) n.Add(EchoMissNote);
            // 合成の触媒 (2026-09-12): 工房の素材にすると結果に乗る恩恵
            if (def.FusionCatalyst == "cheaper") n.Add("触媒: 素材にすると結果のコスト−1");
            else if (def.FusionCatalyst == "echo") n.Add("触媒: 素材にすると結果の効果を2回解決");
            else if (def.FusionCatalyst == "retain") n.Add("触媒: 素材にすると結果が保持");
            else if (def.FusionCatalyst == "aoe") n.Add("触媒: 素材にすると結果のダメージが全体に");
            if (def.ExhaustUnlessExposedEnemy == true) n.Add("急所持ちがいなければ消滅");
            if (def.Retainer == true)
            {   // 白の語彙 (2026-09-18 従者→人形)。寿命は「期限」(2026-09-22 友人ラン: 寿命を「灯り」と呼ぶと資源の「灯」と同じ字で「灯が減ると人形が消える」と読まれた。からくりと同じ語彙)。
                // 注記は1行 (同日「人形を置いたらどうなるのか分からない」): 出した瞬間に1回動く (点灯) を用語解説の外に出す。火勢の一文はダメージ・ブロックを持つ人形だけ
                bool grows = DollUi.HasGrowth(def);
                n.Add("人形: 出した瞬間に1回動く。" + (def.LifePersist == true ? "期限なし" : "期限" + (def.Life ?? 3) + "ターン（出したターンを含む）") + (grows ? "。1ターンごとにダメージとブロック+1" : ""));
            }
            if (def.ShivToken == true) n.Add("骨のナイフ");
            return n;
        }

        /// <summary>反復の2回目の注記 (2026-09-24 E11 裁定B): 2回目も同じ敵を狙う = 1回目で倒れたら空振り。Web/CLI と同じ文</summary>
        public const string EchoMissNote = "単体の効果は1回目で対象が倒れたら2回目は空振り（別の敵や分裂・残機の次の姿には向かない）";

        /// <summary>札 (選択式のモードも) がその効果を持つか</summary>
        static bool HasEffect(CardDef def, string effect)
        {
            if (def == null) return false;
            if (def.Effects != null) foreach (var e in def.Effects) if (e.Effect == effect) return true;
            if (def.Modes != null) foreach (var m in def.Modes) if (m.Effects != null) foreach (var e in m.Effects) if (e.Effect == effect) return true;
            return false;
        }

        // ---- 敵 ----

        /// <summary>状態異常の表示名 (吹き出しの札用)</summary>
        public static string StatusName(string status) { string ja; return StatusJa.TryGetValue(status, out ja) ? ja : status; }

        /// <summary>アーティファクトが弾くデバフ効果の名前 (weakenEnemy=威圧・exposeEnemy=急所・confuse=混乱)。TS ui/log.ts と同じ表</summary>
        public static string DebuffName(string effect)
        {
            switch (effect)
            {
                case "weakenEnemy": return "威圧";
                case "exposeEnemy": return "急所";
                case "confuse": return "混乱";
                default: return effect ?? "";
            }
        }

        public static string InflictSuffix(StatusInflict inf)
        {
            if (inf == null) return "";
            string dest = inf.Status == "wound" ? "(捨て札へ)" : inf.Status == "junk" ? "(山札へ)" : inf.Status == "scald" ? "(手札へ)" : "";
            string ja;
            if (!StatusJa.TryGetValue(inf.Status, out ja)) ja = inf.Status;
            return " +" + ja + inf.Amount + dest;
        }

        /// <summary>
        /// 意図の1行 (実値公開 2026-09-14 本家形)。攻撃の数字は shownValue (威圧・脆弱・重り込みのライブ値。
        /// Effects.DisplayedIntentValue) を渡す。負なら宣言した実値 (ログ行など状態が無い場所)
        /// </summary>
        public static string IntentLine(EnemyIntent it, int shownValue = -1)
        {
            if (it == null) return "---";
            int v = shownValue >= 0 ? shownValue : it.Actual;
            switch (it.Kind)
            {
                case "attack":
                {
                    string hits = it.MirrorHits == true ? "×手数" : (it.Hits.HasValue && it.Hits.Value > 1 ? "×" + it.Hits.Value : "");
                    string guard = it.AlsoDefend.HasValue ? "+ブロック" + it.AlsoDefend.Value : "";
                    string buff = it.AlsoBuff.HasValue ? "+筋力" + it.AlsoBuff.Value : "";
                    string breaks = it.AlsoDestroySet == true ? "からくり壊し+" : ""; // 壊しつつ殴る (2026-09-14)
                    return breaks + "攻撃 " + v + hits + guard + buff + InflictSuffix(it.Inflict);
                }
                case "defend":
                    return "防御 " + it.Actual + (it.AlsoBuff.HasValue ? " +筋力" + it.AlsoBuff.Value : "");
                case "destroy-set": return "からくり壊し";
                case "destroy-token": return "人形狩り";
                case "buff": return "筋力 +" + it.Actual;
                case "rally": return "応援 +" + it.Actual + " (味方全体の筋力)";
                case "hex": return "呪い" + InflictSuffix(it.Inflict);
                case "heal": return "回復 " + it.Actual + " (最も傷んだ味方)";
                case "steal-gold": return "盗み " + it.Actual + "G";
                case "flee": return "逃走 (倒すか打ち消せば阻止)";
                case "rest": return "隙だらけ";
                case "hatch": return "孵化する";
                case "mill": return "山札喰い " + it.Actual + "枚";
                case "summon": return "召喚 ×" + it.Actual + " (場が4体なら出ない)";
                default: return KindJa(it.Kind);
            }
        }

        /// <summary>意図1つの表示 (実値公開): 攻撃は補正込みのライブ値。補正があれば「(もとは12・威圧で-25%)」を添える</summary>
        public static string LiveIntentLine(GameState st, int enemyIndex, EnemyIntent it)
        {
            if (it == null) return "---";
            // 死に札の rider は上限で畳む (上限に達していれば出さない。2026-09-16 人間#12)
            var shown = it.Inflict != null ? it with { Inflict = Effects.DisplayedInflict(st, it.Inflict) } : it;
            string text = IntentLine(shown, Effects.DisplayedIntentValue(st, enemyIndex, it.Kind, it.Actual));
            var notes = Effects.IntentModifierNotes(st, enemyIndex, it.Kind);
            return notes.Count > 0 ? text + " (もとは" + it.Actual + "・" + string.Join("・", notes) + ")" : text;
        }

        /// <summary>その敵の今の意図 (伏せ分岐の解決込み)</summary>
        public static string IntentText(GameState st, int enemyIndex)
        {
            if (st == null || enemyIndex < 0 || enemyIndex >= st.Enemies.Count) return "---";
            // ルーンの円蓋 (2026-09-12 本家 Runic Dome): 意図は表示しない (からくりの確認の窓では実値が見える)
            if (st.HideIntents == true) return "？ 意図は見えない（ルーンの円蓋）";
            var raw = st.Enemies[enemyIndex].Intent;
            var eff = Effects.EffectiveIntent(st, enemyIndex);
            string s = LiveIntentLine(st, enemyIndex, eff);
            if (raw != null && raw.ConditionalOn != null && raw.Alt != null)
            {
                // EffectiveIntent は条件を満たさない時だけ raw をそのまま返す (参照が同じ)
                bool altActive = !object.ReferenceEquals(eff, raw);
                string what = raw.ConditionalOn == "set" ? "動かせるからくり" : "人形";
                s += "  【" + what + (altActive ? "あり" : "なし") + "分岐】";
                if (!altActive) s += " ※" + what + "があると: " + LiveIntentLine(st, enemyIndex, BranchToIntent(raw.Alt));
            }
            return s;
        }

        static EnemyIntent BranchToIntent(EnemyIntentBranch b)
        {
            return new EnemyIntent
            {
                Kind = b.Kind,
                Actual = b.Actual,
                Hits = b.Hits,
                Inflict = b.Inflict,
                AlsoDefend = b.AlsoDefend,
                AlsoBuff = b.AlsoBuff,
                AlsoDestroySet = b.AlsoDestroySet,
            };
        }

        /// <summary>技の短い表記「攻撃7〜9×2」「防御12〜17」「筋力+2」(予告向け。TS enemyGraph.ts moveLabel)。strength で攻撃の幅に今の筋力を足す</summary>
        public static string MoveShort(EnemyMove m, int strength = 0)
        {
            string mark = KindJa(m.Kind);
            int add = m.Kind == "attack" ? strength : 0;
            int? lo = m.Min.HasValue ? (int?)Math.Max(m.Kind == "attack" ? 1 : m.Min.Value, m.Min.Value + add) : null;
            int? hi = m.Max.HasValue ? (int?)Math.Max(m.Kind == "attack" ? 1 : m.Max.Value, m.Max.Value + add) : null;
            string range = lo.HasValue ? (lo == hi ? lo.Value.ToString() : lo.Value + "〜" + hi) : "";
            string sign = (m.Kind == "buff" || m.Kind == "rally") ? "+" : "";
            string hits = m.MirrorHits == true ? "×手数" : ((m.Hits ?? 1) > 1 ? "×" + m.Hits.Value : "");
            string inflict = m.Inflict != null ? (range != "" ? "+" : "") + StatusName(m.Inflict.Status) + m.Inflict.Amount : "";
            string riders = (m.AlsoDefend.HasValue ? "+ブロック" + m.AlsoDefend.Value : "") + (m.AlsoBuff.HasValue ? "+筋力" + m.AlsoBuff.Value : "") + (m.AlsoDestroySet == true ? "+からくり壊し" : "") + (m.GrowPerUse.HasValue ? "(使うたび+" + m.GrowPerUse.Value + ")" : "") + (m.GrowHitsPerUse.HasValue ? "(使うたびヒット+" + m.GrowHitsPerUse.Value + ")" : "");
            return mark + sign + range + hits + inflict + riders;
        }

        static bool HasOtherAlive(GameState st, int index)
        {
            if (st == null) return true;
            for (int j = 0; j < st.Enemies.Count; j++) if (j != index && st.Enemies[j].Hp > 0) return true;
            return false;
        }

        /// <summary>敵カードに常時出す特性タグ (フェアネス)。st/index を渡すと割り込みの予告に今の筋力・HP半分の線・残りを添える (2026-09-14)</summary>
        public static string EnemyTraits(EnemyDef d, GameState st = null, int index = -1)
        {
            if (d == null) return "";
            var t = new List<string>();
            var e = st != null && index >= 0 && index < st.Enemies.Count ? st.Enemies[index] : null;
            int strength = e != null && st != null ? Effects.EffectiveStrength(st, index) : 0;
            if (d.Armor.HasValue) t.Add("装甲" + d.Armor.Value);
            if (d.TurnArmor.HasValue) t.Add("ターン装甲" + d.TurnArmor.Value);
            if (d.Thorns.HasValue) t.Add("とげ" + d.Thorns.Value);
            if (d.Regen.HasValue) t.Add("再生" + d.Regen.Value + (d.RegenBreak.HasValue ? "(1Tに" + d.RegenBreak.Value + "で止まる)" : ""));
            if (d.BurnResist.HasValue) t.Add("延焼耐性" + d.BurnResist.Value);
            if (d.StartingBlock.HasValue) t.Add("開幕ブロック" + d.StartingBlock.Value);
            if (d.Artifact.HasValue) t.Add("アーティファクト" + d.Artifact.Value);
            if (d.Guardian == true) t.Add("庇う");
            if (d.BondStrength.HasValue) t.Add("連携+" + d.BondStrength.Value);
            if (d.MournStrength.HasValue) t.Add("弔い+" + d.MournStrength.Value);
            if (d.Nemesis == true) t.Add("因縁(奇数Tは無形)");
            if (d.Imbalanced == true) t.Add("バランス崩し");
            if (d.Burrow != null) t.Add("潜伏(殻" + d.Burrow.Block + ")");
            if (d.SplitInto != null) t.Add("分裂→" + d.SplitInto.Count + "体");
            if (d.HatchInto != null) t.Add("孵化");
            // 鎮めの錘 (ギア) で割り込みを止めた敵は豹変しない = 予告を出さない (2026-09-24 Opus ひなた E5。TS interruptPreviews と同じ)
            if (d.Interrupts != null && (e == null || e.InterruptBlocked != true))
            {
                // 行動グラフ (2026-09-14): 割り込み = HP半分の豹変・被弾覚醒・単独時の転職。引き金と最初の技を並べる
                for (int k = 0; k < d.Interrupts.Count; k++)
                {
                    var it = d.Interrupts[k];
                    if (e != null && e.FiredInterrupts != null && e.FiredInterrupts.Contains(k)) continue; // 発火済み
                    // 最初の2手を並べる。攻撃の幅には今の筋力を足す (未宣言の技は幅・宣言済みは実値、の2層)
                    var first = EnemyGraph.FirstMoveOf(d, it.Goto);
                    string arrow = "";
                    if (first != null)
                    {
                        arrow = ": " + MoveShort(first, strength);
                        EnemyNode node; string nextId = d.Nodes.TryGetValue(it.Goto, out node) && node.Move != null ? node.Next : null;
                        var second = nextId != null && nextId != it.Goto ? EnemyGraph.FirstMoveOf(d, nextId) : null;
                        if (second != null) arrow += "→" + MoveShort(second, strength);
                    }
                    if (it.On == EnemyInterruptTriggers.DamageTaken) t.Add((e != null ? "あと" + Math.Max(0, (it.Amount ?? 0) - (e.DamageTakenTotal ?? 0)) + "ダメージで目覚める" : "累計" + (it.Amount ?? 0) + "ダメージを受けると") + arrow);
                    else if (it.On == EnemyInterruptTriggers.HpBelowHalf) t.Add((e != null ? "HPが" + (e.MaxHp / 2) + "以下になると" : "HPが半分以下になると") + arrow);
                    else if (it.On == EnemyInterruptTriggers.Alone) { if (e == null || HasOtherAlive(st, index)) t.Add("仲間が全滅すると" + arrow); }
                    else if (it.On == EnemyInterruptTriggers.AllyDied) t.Add("仲間が倒れると" + arrow);
                }
            }
            if (d.EnrageEveryCards.HasValue) t.Add("激昂(" + d.EnrageEveryCards.Value + "枚ごと筋力+2)");
            if (d.EnrageEveryDamage.HasValue) t.Add("激昂(累計" + d.EnrageEveryDamage.Value + "ダメごと筋力+2)");
            if (d.Enrage.HasValue) t.Add("激昂(毎フェーズ筋力+" + d.Enrage.Value + ")");
            if (d.AngerOnBlock.HasValue) t.Add("守ると怒る+" + d.AngerOnBlock.Value);
            if (d.Aura != null) t.Add("重圧(コスト+" + d.Aura.CostUp + ")");
            return t.Count == 0 ? "" : string.Join(" / ", t.ToArray());
        }

        // ---- 戦闘ログ ----

        /// <summary>1イベント=1行。表示不要なら null</summary>
        public static string LogLine(GameEvent ev)
        {
            var a = ev as GameEvent_CombatStarted; if (a != null) return "戦闘開始: " + SafeEncounter(a.EnemyId);
            var b = ev as GameEvent_TurnStarted; if (b != null) return "--- ターン " + b.Turn + " ---";
            var c = ev as GameEvent_TurnEnded; if (c != null) return "ターン終了 → 敵の行動";
            var d = ev as GameEvent_CardsDrawn; if (d != null) return d.Count + "枚ドロー" + (d.Cards != null && d.Cards.Count > 0 ? ": " + Names(d.Cards) : "");
            var e = ev as GameEvent_CardPlayed; if (e != null) return "プレイ: " + CardName(e.CardId);
            var f = ev as GameEvent_CardSet; if (f != null) return "仕込んだ: " + CardName(f.CardId);
            var g = ev as GameEvent_SetCardExpired;
            if (g != null) return "期限切れ: " + CardName(g.CardId) + "（期限までに鳴らなかったので" + (g.To == "hand" ? "手札へ" : g.To == "exhaust" ? "消滅置き場へ" : "捨て札へ") + "）";
            var sc = ev as GameEvent_Scried;   // 占術 (青 2026-09-25)
            if (sc != null) return "占術: " + (sc.Looked != null ? sc.Looked.Count : 0) + "枚を見た" + (sc.Discarded != null && sc.Discarded.Count > 0 ? "（捨て札へ: " + Names(sc.Discarded) + "）" : "（全部残した）");
            var tl = ev as GameEvent_TrapLifeExtended;   // 潮待ち (青 2026-09-25)
            if (tl != null) return "からくり" + tl.Count + "枚の期限を" + tl.Amount + "ターン延ばした";
            var h = ev as GameEvent_EnemyIntentDeclared; if (h != null) return "敵" + (h.EnemyIndex + 1) + "の意図: " + IntentLine(h.Intent);
            var i2 = ev as GameEvent_ActionNegated; if (i2 != null) return "敵の行動を打ち消した!";
            var j = ev as GameEvent_DamageDealt;
            if (j != null)
            {
                return j.Source == "player"
                    ? "敵に" + j.Amount + "ダメージ (HP-" + j.HpLoss + ")"
                        + (j.Exposed == true ? " [急所]" : "")
                        + (j.Pierced == true ? " [貫通]" : "")
                        + (j.Blocked.HasValue && j.Blocked.Value > 0 ? " [ブロックで" + j.Blocked.Value + "]" : "")
                        + (j.ArmorCut.HasValue && j.ArmorCut.Value > 0 ? " [装甲で" + j.ArmorCut.Value + "切り捨て]" : "")
                        + (j.TurnArmorCut.HasValue && j.TurnArmorCut.Value > 0 ? " [ターン装甲で" + j.TurnArmorCut.Value + "]" : "")
                        + (j.BurrowCut.HasValue && j.BurrowCut.Value > 0 ? " [潜伏の殻で" + j.BurrowCut.Value + "]" : "")
                        + (j.NemesisCut.HasValue && j.NemesisCut.Value > 0 ? " [無形で1固定]" : "")
                    : "敵の攻撃" + j.Amount + " → HP-" + j.HpLoss + (j.Blocked.HasValue && j.Blocked.Value > 0 ? " (ブロックで" + j.Blocked.Value + ")" : "");
            }
            var k = ev as GameEvent_BlockGained; if (k != null) return (k.Target == "player" ? "自分" : "敵") + "がブロック+" + k.Amount;
            var l = ev as GameEvent_IceBlockGained; if (l != null) return "氷壁+" + l.Amount;
            var m = ev as GameEvent_StrengthGained; if (m != null) return "敵の筋力 +" + m.Amount + (m.Reason != null ? " (" + m.Reason + ")" : "");
            var n = ev as GameEvent_BurnApplied; if (n != null) return "敵に延焼+" + n.Amount;
            var o = ev as GameEvent_BurnTick; if (o != null) return "延焼で敵に" + o.Amount + "ダメージ";
            var p = ev as GameEvent_GrowthAdded; if (p != null) return "成長+" + p.Amount;
            var q = ev as GameEvent_MomentumAdded; if (q != null) return "勢い+" + q.Amount;
            var r = ev as GameEvent_HpHealed; if (r != null) return "HP+" + r.Amount + "回復";
            var s = ev as GameEvent_HpLost; if (s != null) return "自傷でHP-" + s.Amount;
            var t2 = ev as GameEvent_StatusInflicted;
            if (t2 != null)
            {
                string ja; if (!StatusJa.TryGetValue(t2.Status, out ja)) ja = t2.Status;
                return ja + t2.Amount + "を付与された";
            }
            var u = ev as GameEvent_ReactionTriggered; if (u != null) return "動かした: " + CardName(u.CardId);
            var v = ev as GameEvent_ReactionHeld; if (v != null) return "巻いたまま: " + Names(v.CandidateIds) + " (敵" + (v.EnemyIndex + 1) + "の" + KindJa(v.Kind) + " " + v.Stage + "窓 / 実値" + v.Value + ")";
            var w = ev as GameEvent_ReactionWhiffed; if (w != null) return "空振り: " + CardName(w.CardId);
            var x = ev as GameEvent_ReactionUnaffordable; if (x != null) return "仕込み札「" + CardName(x.CardId) + "」を動かすには" + x.Cost + "E必要 (残り" + x.Energy + "E) = 巻いたまま";
            var y = ev as GameEvent_SetCardDestroyed; if (y != null) return "からくりを壊された: " + CardName(y.CardId);
            var z = ev as GameEvent_PermanentPlayed; if (z != null) return "置物を設置: " + CardName(z.CardId);
            var a2 = ev as GameEvent_CardExhausted; if (a2 != null) return "消滅: " + CardName(a2.CardId);
            var b2 = ev as GameEvent_CardsMilled; if (b2 != null) return "山札の上" + b2.Count + "枚を忘却" + (b2.CardIds != null ? ": " + Names(b2.CardIds) : "");
            var c2 = ev as GameEvent_ExposedApplied; if (c2 != null) return "敵に急所+" + c2.Amount;
            var d2 = ev as GameEvent_EnemyWeakened; if (d2 != null) return "敵を威圧" + d2.Amount;
            var e2 = ev as GameEvent_BlockShattered; if (e2 != null) return "敵のブロック" + e2.Amount + "を粉砕!";
            var f2 = ev as GameEvent_ThornsReflected; if (f2 != null) return "とげ反射: " + f2.Amount + " (HP-" + f2.HpLoss + ")";
            var g2 = ev as GameEvent_GoldStolen; if (g2 != null) return "盗まれた: " + g2.Amount + "G (逃がす前に倒せば取り返せる)";
            var h2 = ev as GameEvent_EnemyFled; if (h2 != null) return "敵が逃走した";
            var i3 = ev as GameEvent_EnemyHealed; if (i3 != null) return "敵が回復 +" + i3.Amount;
            var j2 = ev as GameEvent_RegenTicked; if (j2 != null) return "敵は再生でHP+" + j2.Amount;
            var k2 = ev as GameEvent_RegenBroken; if (k2 != null) return "再生が止まった";
            var l2 = ev as GameEvent_EnergyGained; if (l2 != null) return "エナジー+" + l2.Amount + " (このターン)";
            var m2 = ev as GameEvent_EnergyMaxGained; if (m2 != null) return "エナジー上限+" + m2.Amount;
            var n2 = ev as GameEvent_DiscountGained; if (n2 != null) return "次のカードのコスト-" + n2.Amount;
            var o2 = ev as GameEvent_ImpulseDrawn; if (o2 != null) return "衝動" + o2.Count + "枚 (このターン限り)";
            var p2 = ev as GameEvent_CardsDiscarded; if (p2 != null) return "コストとして捨てた: " + Names(p2.CardIds);
            var q2 = ev as GameEvent_GrowthDischarged; if (q2 != null) return "成長" + q2.Spent + "を全て放出!";
            var r2 = ev as GameEvent_MomentumDischarged; if (r2 != null) return "勢い" + r2.Spent + "を全て放出!";
            var s2 = ev as GameEvent_AetherGained; if (s2 != null) return "霊気+" + s2.Amount;
            var t3 = ev as GameEvent_AetherDischarged; if (t3 != null) return "霊気" + t3.Spent + "を全て放出!";
            var lg = ev as GameEvent_LightGained; if (lg != null) return "灯+" + lg.Amount + " (" + (lg.Source == "heal" ? "回復" : lg.Source == "retainer" ? "人形" : lg.Source == "passive" ? "灯匠" : lg.Source == "carry" ? "残り火" : "カード") + ")";
            // 灯の火床 (Sparks)・灯の炉心 (Paid) は放出でなく支払い (2026-09-24 Opus ひなた T15。Web/CLI と同じ文)
            var ld = ev as GameEvent_LightDischarged; if (ld != null) return IsLightPayment(ld) ? LightPayLine(ld) : "灯" + ld.Spent + "を放出!";
            var ls = ev as GameEvent_LightSpent; if (ls != null) return "灯-" + ls.Amount + " (" + CardName(ls.CardId) + ")"; // 2026-09-20 夜
            var cad = ev as GameEvent_CardsAddedToDraw; if (cad != null) return CardName(cad.CardId) + cad.Count + "枚を山札に混ぜた";
            var cadd = ev as GameEvent_CardsAddedToDiscard; if (cadd != null) return CardName(cadd.CardId) + cadd.Count + "枚を捨て札に加えた";
            var dct = ev as GameEvent_DeckCardTransformed; if (dct != null) return "山札の" + CardName(dct.CardId) + "が" + CardName(dct.Into) + "に変わった";
            var u2 = ev as GameEvent_EnemySplit; if (u2 != null) return u2.Count == 1 ? "再起動! 倒した敵が次の姿で立ち上がった" : "分裂! 倒した敵から" + u2.Count + "体が現れた";
            var v2 = ev as GameEvent_EnemyHatched; if (v2 != null) return "孵化した!";
            var w2 = ev as GameEvent_GuardianRedirected; if (w2 != null) return "庇われた! 単体対象は護衛に向かった";
            var x2 = ev as GameEvent_BurrowBroken; if (x2 != null) return "潜伏の殻が割れた! 次の行動は噛みつき";
            var y2 = ev as GameEvent_EnemyStaggered; if (y2 != null) return "完全に防いだ! 敵は体勢を崩し、次の行動は隙";
            var zs = ev as GameEvent_EnemySummoned; if (zs != null) return zs.Count > 0 ? "召喚! " + zs.Count + "体が現れた" : "召喚したが場が満杯で出なかった";
            var z2 = ev as GameEvent_EnemyInterrupted; if (z2 != null) return (z2.Trigger == EnemyInterruptTriggers.DamageTaken ? "目を覚ました! 眠りが終わった" : z2.Trigger == EnemyInterruptTriggers.HpBelowHalf ? "HPが半分を切った!" : z2.Trigger == EnemyInterruptTriggers.Alone ? "仲間が全滅した!" : "仲間が倒れた!") + (z2.Replaced ? " 行動が変わった" + (z2.Before != null && z2.After != null ? ": " + IntentLine(z2.Before) + " → " + IntentLine(z2.After) : "") : " 次のターンから行動が変わる");
            var a3 = ev as GameEvent_ArtifactBlocked; if (a3 != null) return "アーティファクトが" + DebuffName(a3.Effect) + "を弾いた（この効果は消えた）";
            var b3 = ev as GameEvent_ScaldTick; if (b3 != null) { var parts = new List<string>(); if ((b3.Scalds ?? 0) > 0) parts.Add("火傷" + b3.Scalds + "枚"); if ((b3.Brands ?? 0) > 0) parts.Add("烙印" + b3.Brands + "枚"); return (parts.Count > 0 ? string.Join("・", parts) : "火傷・烙印" + b3.Count + "枚") + "が疼いた (HP-" + b3.Amount + ")"; }
            var c3 = ev as GameEvent_CombatEnded; if (c3 != null) return c3.Result == "won" ? "=== 勝利 ===" : "=== 敗北 ===";
            var d3 = ev as GameEvent_GearUsed; if (d3 != null) return "ギア「" + d3.Name + "」を組んだ";   // ⚙ は Unity のフォントに無い   // ギア (2026-09-17)
            var e3 = ev as GameEvent_DeathSaved; if (e3 != null) return e3.Source == "gear" ? "蘇りの発条がはじけ、HP" + e3.Hp + "で踏みとどまった" : "蜥蜴の尾が砕け、HP" + e3.Hp + "で踏みとどまった";
            var g3 = ev as GameEvent_RetainerRushed; if (g3 != null) return "点灯: " + CardName(g3.CardId) + "が出た瞬間に1回動いた";   // ひなたのパッシブ (2026-09-19 log に出ていなかった)
            // 人形の期限 (2026-09-21。語彙は 2026-09-22 に「灯り」→「期限」): 切れた・写した・延ばした (人間ラン#16 のレポートに行が無かった)
            var gx = ev as GameEvent_RetainerExpired; if (gx != null) return "期限切れ: " + CardName(gx.CardId) + "が消えた";
            var gd = ev as GameEvent_PermanentDismissed; if (gd != null) return "灯が足りず " + CardName(gd.CardId) + " が場を離れた（捨て札へ）";
            var gc = ev as GameEvent_RetainerCopied; if (gc != null) return "写し: " + CardName(gc.CardId) + "をコピーした（残りの期限も写す）";
            var gl = ev as GameEvent_RetainerLifeExtended; if (gl != null) return gl.Persist == true ? "永遠の灯: " + CardName(gl.CardId) + "の期限が無くなった（消えなくなった）" : "継ぎ火: " + CardName(gl.CardId) + "の期限を" + gl.Amount + "ターン延ばした";
            var h3 = ev as GameEvent_RetainersTriggered; if (h3 != null) return "号令: 人形" + h3.Count + "体がトリガーを問わず今1回ずつ動いた";
            var i4 = ev as GameEvent_RetainersDuplicated; if (i4 != null) return "分列: 人形" + i4.Count + "体が複製された";   // 「従者」→「人形」(2026-09-24 T3)
            // 人形まわりと反復の行 (2026-09-24 T3/E11: 型名 "TokenDestroyed" などがそのまま出ていた)。CLI と同じ文
            var td = ev as GameEvent_TokenDestroyed; if (td != null) return "人形狩り: " + CardName(td.CardId) + "が壊された";
            var rs = ev as GameEvent_RetainerSacrificed; if (rs != null) return "人形を捧げた: " + CardName(rs.CardId);
            var se = ev as GameEvent_SpellEchoed; if (se != null) return "反復: " + CardName(se.CardId) + "の効果が2回解決";
            var f3 = ev as GameEvent_HpLossCapped; if (f3 != null) return f3.Left > 0 ? "脈打つ欠片がHPの損失を20で止めた（あと" + f3.Left + "回）" : "脈打つ欠片がHPの損失を20で止めた（これで最後。戦いの後に砕ける）";   // 2026-09-18
            // 型名がそのまま出ていた行 (2026-09-24 人間ラン#18/#19: EnemyDied 70・DeckShuffled 53・CardsAddedToHand 34 行)。文は Web の log.ts と同じ
            var ed = ev as GameEvent_EnemyDied; if (ed != null) return "敵" + (ed.EnemyIndex + 1) + "を倒した";
            if (ev is GameEvent_DeckShuffled) return "山札を切り直した";
            var cah = ev as GameEvent_CardsAddedToHand; if (cah != null) return CardName(cah.CardId) + "を" + cah.Count + "枚手札に加えた";
            var cmh = ev as GameEvent_CardsMovedToHand; if (cmh != null) return (cmh.From == "draw" ? "サーチ" : "回収") + ": " + Names(cmh.CardIds) + "を手札に加えた";
            var ccp = ev as GameEvent_CardCopied; if (ccp != null) return CardName(ccp.CardId) + "のコピー" + ccp.Count + "枚を捨て札に加えた";
            var cgr = ev as GameEvent_CardGrew; if (cgr != null) return CardName(cgr.CardId) + "が育った（与ダメ+" + cgr.Bonus + "）";
            var cuh = ev as GameEvent_CardUpgradedInHand; if (cuh != null) return CardName(cuh.CardId) + "を鍛えた（この戦闘中）";
            var crt = ev as GameEvent_CardRetrieved; if (crt != null) return "回収: " + CardName(crt.CardId) + "（消滅置き場から手札へ）";
            var cpe = ev as GameEvent_CardPlayedFromExhaust; if (cpe != null) return "直接プレイ: " + CardName(cpe.CardId) + "（消滅置き場から）";
            var nf = ev as GameEvent_NecroFired; if (nf != null) return "亡骸: " + CardName(nf.CardId) + "が消滅して効果が発火";
            var np = ev as GameEvent_NecroPlayed; if (np != null) return "亡骸プレイ: " + CardName(np.CardId) + "（ゲームから取り除かれた）";
            var exr = ev as GameEvent_ExhaustRecycled; if (exr != null) return "輪廻: 消滅置き場" + exr.Count + "枚が山札へ還った";
            var bdc = ev as GameEvent_BurnDischarged; if (bdc != null) return "爆熱: 延焼" + bdc.Amount + "を全て解き放った";
            var ecf = ev as GameEvent_EnemyConfused; if (ecf != null) return "敵に混乱+" + ecf.Amount + "（攻撃が仲間に向かう）";
            var cfa = ev as GameEvent_ConfusedAttack; if (cfa != null) return cfa.EnemyIndex == cfa.TargetIndex ? "混乱した敵は自分自身に" + cfa.Amount + "ダメージ!" : "仲間割れ! 混乱した敵が味方に" + cfa.Amount + "ダメージ";
            var mhg = ev as GameEvent_MaxHpGained; if (mhg != null) return "最大HP+" + mhg.Amount + "（この戦闘後も残る）";
            var ssg = ev as GameEvent_SetSlotGained; if (ssg != null) return "仕込み枠+" + ssg.Amount + "（この戦闘中）";
            var pab = ev as GameEvent_PlayerArtifactBlocked; if (pab != null) return "時計仕掛けの土産が" + StatusName(pab.Status) + "を弾いた";
            // 表示しないもの
            if (ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyActionResolved || ev is GameEvent_EnemyPhaseEnded) return null;
            return ev.Type;
        }

        /// <summary>放出でなく支払いの LightDischarged か: 灯の火床 (Sparks)・灯の炉心などの consumeLight (Paid)。2026-09-24 T15</summary>
        public static bool IsLightPayment(GameEvent_LightDischarged ld)
        {
            return ld != null && ((ld.Sparks ?? 0) > 0 || ld.Paid == true);
        }

        /// <summary>支払いの一文 (ログ・浮き文字が共用。2026-09-24 T15。Web/CLI と同じ文): 火床「灯9を払って火種3を山札へ」・炉心「灯3を払った」</summary>
        public static string LightPayLine(GameEvent_LightDischarged ld)
        {
            return (ld.Sparks ?? 0) > 0 ? "灯" + ld.Spent + "を払って火種" + ld.Sparks.Value + "を山札へ" : "灯" + ld.Spent + "を払った";
        }

        static string SafeEncounter(string id)
        {
            try { return Content.EncounterName(id); }
            catch (Exception) { return id; }
        }

        static string Names(IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0) return "";
            var buf = new List<string>();
            for (int i = 0; i < ids.Count; i++) buf.Add(CardName(ids[i]));
            return string.Join("・", buf.ToArray());
        }
    }
}
