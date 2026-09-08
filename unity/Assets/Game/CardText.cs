// CardText.cs — カード定義・敵の意図・戦闘ログを日本語1行に変換する読み取り専用の表示層。
// 語彙は src/ui/App.tsx の EFFECT_JA / COND_JA / TRIGGER_LABEL と src/ui/log.ts に合わせてある。
// 絵文字はフォント次第で豆腐になるので使わず、角括弧のラベルで表す。
using System;
using System.Collections.Generic;
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
            return (shown > baseAmt ? "<color=#3f8f4a>" : "<color=#c0453a>") + shown + "</color>";
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
            { "onTurnStart", "毎T開始時" },
            { "onCombatStart", "戦闘開始時" },
            { "onAttackPlayed", "攻撃プレイ後" },
            { "onSpellPlayed", "呪文をプレイした時" },
            { "onSetDestroyed", "この伏せが破壊された時" },
            { "onHealed", "HPが回復するたび" },
            { "onHpLost", "カード効果でHPを失うたび" },
            { "onCardExhausted", "カードが消滅するたび" },
            { "onCostExhausted", "消滅コストを支払うたび" },
            { "onPermanentEntered", "置物が場に出るたび" },
            { "onImpulsePlayed", "衝動カードをプレイするたび" },
            { "onRandomPlayed", "運任せの札をプレイするたび" },
            { "onAetherGained", "霊気を得るたび" },
            { "onCardSet", "カードを伏せるたび" },
            { "onReactionFired", "リアクションが発動するたび" },
            { "onSelfExhausted", "亡骸" },
            { "onGrowthGained", "成長を得るたび" },
            { "onMomentumGained", "勢いを得るたび" },
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
            { "duplicateRetainers", "場の従者を1体ずつ複製" },
            { "sacrificeRetainer", "従者1体を選んで破壊" },
            { "triggerRetainersNow", "従者のターン開始効果を今すぐ解決" },
            { "activateEnteredRetainer", "場に出た従者が即1回動く" },
            { "blessRetainers", "【常在】従者の効果+N" },
            { "empowerShivs", "【常在】ナイフ与ダメ+N" },
            { "gainSetSlot", "伏せ枠+N(この戦闘中)" },
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
            { "heal", "回復" }, { "hex", "呪い" }, { "destroy-set", "伏せ破壊" }, { "destroy-token", "従者狩り" },
            { "steal-gold", "盗み" }, { "flee", "逃走" }, { "mill", "山札喰い" }, { "rest", "隙" }, { "hatch", "孵化" },
        };

        static readonly Dictionary<string, string> StatusJa = new Dictionary<string, string>
        {
            { "weak", "弱体" }, { "vulnerable", "脆弱" }, { "frail", "虚弱" }, { "wound", "負傷" },
            { "junk", "がらくた" }, { "scald", "火傷" }, { "restrain", "拘束" }, { "mist", "霞み" }, { "slow", "重り" },
        };

        static readonly Dictionary<string, string> TypeJaMap = new Dictionary<string, string>
        {
            { "physical", "物理" }, { "spell", "呪文" }, { "reaction", "リアクション" }, { "permanent", "置物" },
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
            return def.XCost == true ? "X" : def.Cost.ToString();
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
            if (c.EnemyIntent != null) parts.Add("対象の意図が" + KindJa(c.EnemyIntent));
            if (c.EnemyIntentNot != null) parts.Add("対象の意図が" + KindJa(c.EnemyIntentNot) + "以外");
            if (c.EnemyExposed == true) parts.Add("対象が急所持ち");
            if (c.PerfectBlockLastPhase == true) parts.Add("直前の敵フェーズを完全に凌いだ");
            if (c.TargetDead == true) parts.Add("とどめ");
            if (c.LastActionNoHpLoss == true) parts.Add("完全に凌いだ時");
            if (c.HealedThisTurn == true) parts.Add("このT先にカードで回復していたら");
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
            string tpl;
            if (e.Effect == "dealDamage" && DamageModifier != null && (e.Trigger == null || e.Trigger == "onPlay"))
            {
                int shown = DamageModifier(amt);
                return "ダメージ" + Colored(amt, shown);
            }
            if (EffectJa.TryGetValue(e.Effect, out tpl)) return tpl.Replace("N", amt.ToString());
            return e.Effect + (e.Amount.HasValue ? " " + amt : "");
        }

        /// <summary>カードの効果行 (選択式はモードごと)。改行区切り</summary>
        public static string Body(CardDef def)
        {
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
            var n = new List<string>();
            if (def.Exhaust == true) n.Add("消滅");
            if (def.Retain == true) n.Add("保持");
            if (def.XCost == true) n.Add("Xコスト(エナジーを全て払う)");
            if ((def.DiscardCost.HasValue ? def.DiscardCost.Value : 0) > 0) n.Add("追加コスト:手札" + def.DiscardCost.Value + "枚を捨てる");
            if ((def.ExhaustCost.HasValue ? def.ExhaustCost.Value : 0) > 0) n.Add("追加コスト:手札" + def.ExhaustCost.Value + "枚を消滅");
            if (def.NecroCost.HasValue) n.Add("亡骸プレイ " + def.NecroCost.Value + "E");
            if (def.FreeIfHandAllPhysical == true) n.Add("手札が物理だけなら0E");
            if (def.FreeIfHandAll != null) n.Add("手札が" + TypeJa(def.FreeIfHandAll) + "だけなら0E");
            if (def.FreeIfMomentumAtLeast.HasValue) n.Add("勢い" + def.FreeIfMomentumAtLeast.Value + "以上なら0E");
            if (def.RequiresRetainer == true) n.Add("場に従者が必要");
            if (def.ExhaustUnlessExposedEnemy == true) n.Add("急所持ちがいなければ消滅");
            if (def.BlazeDiscount.HasValue) n.Add("猛り火中コスト-" + def.BlazeDiscount.Value);
            if (def.Retainer == true) n.Add("従者");
            if (def.ShivToken == true) n.Add("骨のナイフ");
            return n.Count == 0 ? "" : string.Join(" / ", n.ToArray());
        }

        // ---- 敵 ----

        /// <summary>状態異常の表示名 (吹き出しの札用)</summary>
        public static string StatusName(string status) { string ja; return StatusJa.TryGetValue(status, out ja) ? ja : status; }

        public static string InflictSuffix(StatusInflict inf)
        {
            if (inf == null) return "";
            string dest = inf.Status == "wound" ? "(捨て札へ)" : inf.Status == "junk" ? "(山札へ)" : inf.Status == "scald" ? "(手札へ)" : "";
            string ja;
            if (!StatusJa.TryGetValue(inf.Status, out ja)) ja = inf.Status;
            return " +" + ja + inf.Amount + dest;
        }

        public static string IntentLine(EnemyIntent it)
        {
            if (it == null) return "---";
            switch (it.Kind)
            {
                case "attack":
                {
                    string hits = it.MirrorHits == true ? "×手数" : (it.Hits.HasValue && it.Hits.Value > 1 ? "×" + it.Hits.Value : "");
                    string guard = it.AlsoDefend.HasValue ? "+盾" + it.AlsoDefend.Value : "";
                    string buff = it.AlsoBuff.HasValue ? "+筋力" + it.AlsoBuff.Value : "";
                    return "攻撃 " + it.ShownMin + "〜" + it.ShownMax + hits + guard + buff + InflictSuffix(it.Inflict);
                }
                case "defend":
                    return "防御 " + it.ShownMin + "〜" + it.ShownMax + (it.AlsoBuff.HasValue ? " +筋力" + it.AlsoBuff.Value : "");
                case "destroy-set": return "伏せ破壊";
                case "destroy-token": return "従者狩り";
                case "buff": return "筋力 +" + it.ShownMin + "〜" + it.ShownMax;
                case "rally": return "応援 +" + it.ShownMin + "〜" + it.ShownMax + " (味方全体の筋力)";
                case "hex": return "呪い" + InflictSuffix(it.Inflict);
                case "heal": return "回復 " + it.ShownMin + "〜" + it.ShownMax + " (最も傷んだ味方)";
                case "steal-gold": return "盗み " + it.ShownMin + "〜" + it.ShownMax + "G";
                case "flee": return "逃走 (倒すか打ち消せば阻止)";
                case "rest": return "隙だらけ";
                case "hatch": return "孵化する";
                case "mill": return "山札喰い " + it.ShownMin + "〜" + it.ShownMax + "枚";
                default: return KindJa(it.Kind);
            }
        }

        /// <summary>その敵の今の意図 (伏せ分岐の解決込み)</summary>
        public static string IntentText(GameState st, int enemyIndex)
        {
            if (st == null || enemyIndex < 0 || enemyIndex >= st.Enemies.Count) return "---";
            var raw = st.Enemies[enemyIndex].Intent;
            var eff = Effects.EffectiveIntent(st, enemyIndex);
            string s = IntentLine(eff);
            if (raw != null && raw.ConditionalOn != null && raw.Alt != null)
            {
                // EffectiveIntent は条件を満たさない時だけ raw をそのまま返す (参照が同じ)
                bool altActive = !object.ReferenceEquals(eff, raw);
                string what = raw.ConditionalOn == "set" ? "伏せ札" : "従者";
                s += "  【" + what + (altActive ? "あり" : "なし") + "分岐】";
                if (!altActive) s += " ※" + what + "があると: " + IntentLine(BranchToIntent(raw.Alt));
            }
            return s;
        }

        static EnemyIntent BranchToIntent(EnemyIntentBranch b)
        {
            return new EnemyIntent
            {
                Kind = b.Kind,
                ShownMin = b.ShownMin,
                ShownMax = b.ShownMax,
                Actual = b.Actual,
                Hits = b.Hits,
                Inflict = b.Inflict,
                AlsoDefend = b.AlsoDefend,
                AlsoBuff = b.AlsoBuff,
            };
        }

        /// <summary>敵カードに常時出す特性タグ (フェアネス)</summary>
        public static string EnemyTraits(EnemyDef d)
        {
            if (d == null) return "";
            var t = new List<string>();
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
            if (d.WakeOnDamage != null) t.Add("被弾覚醒" + d.WakeOnDamage.Damage);
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
            var f = ev as GameEvent_CardSet; if (f != null) return "伏せた: " + CardName(f.CardId);
            var g = ev as GameEvent_SetCardRetrieved; if (g != null) return "回収: " + CardName(g.CardId);
            var h = ev as GameEvent_EnemyIntentDeclared; if (h != null) return "敵" + (h.EnemyIndex + 1) + "の意図: " + IntentLine(h.Intent);
            var i2 = ev as GameEvent_ActionNegated; if (i2 != null) return "敵の行動を打ち消した!";
            var j = ev as GameEvent_DamageDealt;
            if (j != null)
            {
                return j.Source == "player"
                    ? "敵に" + j.Amount + "ダメージ (HP減 " + j.HpLoss + ")"
                        + (j.ArmorCut.HasValue && j.ArmorCut.Value > 0 ? " [装甲で" + j.ArmorCut.Value + "切り捨て]" : "")
                        + (j.TurnArmorCut.HasValue && j.TurnArmorCut.Value > 0 ? " [ターン装甲で" + j.TurnArmorCut.Value + "]" : "")
                        + (j.BurrowCut.HasValue && j.BurrowCut.Value > 0 ? " [潜伏の殻で" + j.BurrowCut.Value + "]" : "")
                        + (j.NemesisCut.HasValue && j.NemesisCut.Value > 0 ? " [無形で1固定]" : "")
                    : "敵の攻撃" + j.Amount + " → HP減 " + j.HpLoss;
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
            var u = ev as GameEvent_ReactionTriggered; if (u != null) return "リアクション発動: " + CardName(u.CardId);
            var v = ev as GameEvent_ReactionHeld; if (v != null) return "温存: " + Names(v.CandidateIds) + " (敵" + (v.EnemyIndex + 1) + "の" + KindJa(v.Kind) + " " + v.Stage + "窓 / 実値" + v.Value + ")";
            var w = ev as GameEvent_ReactionWhiffed; if (w != null) return "空振り: " + CardName(w.CardId);
            var x = ev as GameEvent_ReactionUnaffordable; if (x != null) return "伏せ札「" + CardName(x.CardId) + "」は発動に" + x.Cost + "E必要 (残り" + x.Energy + "E) = 温存";
            var y = ev as GameEvent_SetCardDestroyed; if (y != null) return "伏せカード破壊: " + CardName(y.CardId);
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
            var u2 = ev as GameEvent_EnemySplit; if (u2 != null) return "分裂! 倒した敵から" + u2.Count + "体が現れた";
            var v2 = ev as GameEvent_EnemyHatched; if (v2 != null) return "孵化した!";
            var w2 = ev as GameEvent_GuardianRedirected; if (w2 != null) return "庇われた! 単体対象は護衛に向かった";
            var x2 = ev as GameEvent_BurrowBroken; if (x2 != null) return "潜伏の殻が割れた! 次の行動は噛みつき";
            var y2 = ev as GameEvent_EnemyStaggered; if (y2 != null) return "完全に防いだ! 敵は体勢を崩し、次の行動は隙";
            var z2 = ev as GameEvent_EnemyWoken; if (z2 != null) return "目を覚ました!";
            var a3 = ev as GameEvent_ArtifactBlocked; if (a3 != null) return "アーティファクトが弾いた (" + a3.Effect + ")";
            var b3 = ev as GameEvent_ScaldTick; if (b3 != null) return "火傷・烙印" + b3.Count + "枚が疼いた (HP-" + b3.Amount + ")";
            var c3 = ev as GameEvent_CombatEnded; if (c3 != null) return c3.Result == "won" ? "=== 勝利 ===" : "=== 敗北 ===";
            // 表示しないもの
            if (ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyActionResolved || ev is GameEvent_EnemyPhaseEnded) return null;
            return ev.Type;
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
