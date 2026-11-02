using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class CardText
{
	public struct TraitBadge
	{
		public string Icon;

		public string Label;

		public string Full;

		public string Term;
	}

	public static Func<int, int> DamageModifier;

	private static readonly Dictionary<string, string> TriggerJa = new Dictionary<string, string>
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
		{ "onTurnStart", "毎ターン開始時" },
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
		{ "onCascade", "チェインで札がプレイされるたび" },
		{ "onImpulseExpired", "衝動の札が失効するたび" },
		{ "onBlazeIgnite", "猛り火に点いた時" },
		{ "onRandomPlayed", "運任せの札をプレイするたび" },
		{ "onAetherGained", "霊気を得るたび" },
		{ "onCardSet", "カードを仕込むたび" },
		{ "onReactionFired", "からくりを動かすたび" },
		{ "onScry", "占術するたび" },
		{ "onScryDiscard", "占術で札を捨てるたび(1枚ごと)" },
		{ "onSetExpired", "からくりが期限切れになるたび" },
		{ "onSelfExhausted", "亡骸" },
		{ "onGrowthGained", "成長を得るたび" },
		{ "onMomentumGained", "勢いを得るたび" },
		{ "onLightGained", "灯を得るたび" },
		{ "onLightDischarged", "灯を放出するたび" },
		{ "onSparkPlayed", "火種を撃つたび" },
		{ "onTurnEnd", "ターン終了時" },
		{ "onShuffle", "山札を切り直すたび" },
		{ "onEnemyDied", "敵を倒すたび" },
		{ "onDamageTaken", "攻撃でHPを失った後" }
	};

	private static readonly Dictionary<string, string> EffectJa = new Dictionary<string, string>
	{
		{ "dealDamagePerCascade", "この戦闘でチェインが当たった回数×Nダメージ" },
		{ "doubleBurn", "対象の延焼を2倍" },
		{ "gainBlockPerBurn", "対象の延焼×Nブロック(延焼は失わない)" },
		{ "dealDamagePerBurn", "対象の延焼×Nダメージ(延焼は失わない)" },
		{ "firstCardCascade", "この置物がある間、毎ターン最初にプレイする札にチェインがつく" },
		{ "burnNoDecay", "この置物がある間、延焼は敵フェーズで減らない(延焼を消費して刈る札とは噛み合わない)" },
		{ "keepImpulseOne", "この置物がある間、ターン終了時に失効する衝動の札を1枚選んで手札に残す(次のターンまで。省略すると最も高コストの札)" },
		{ "cascadeSameCost", "この置物がある間、チェインは同じコストの札だけに当たる(安い札は飛ばす)" },
		{ "dealDamage", "ダメージN" },
		{ "dealDamageRandom", "ランダムダメージN" },
		{ "dealDamageDrain", "ドレインN(半分回復)" },
		{ "dealDamageCleave", "キル連鎖N" },
		{ "dealDamageExecute", "処刑N(HP25%以下で上限)" },
		{ "dealDamagePerBlock", "ブロック×Nダメージ" },
		{ "dealDamagePerIceBlock", "氷壁×Nダメージ" },
		{ "dealDamagePerCardPlayed", "詠唱数×Nダメージ" },
		{ "dealDamagePerCardPlayedTotal", "累計プレイ数×Nダメージ" },
		{ "dealDamagePerEnergyMax", "上限×Nダメージ" },
		{ "dealDamagePerMomentum", "勢い×Nダメージ(非消費)" },
		{ "dealDamagePerExhaust", "消滅数×Nダメージ" },
		{ "dealDamagePerHandCard", "手札数×Nダメージ" },
		{ "dealDamagePerHeal", "回復回数×Nダメージ" },
		{ "dealDamagePerDamageTaken", "受けたダメージ×Nダメージ" },
		{ "dealDamagePerSelfHpLost", "失ったHP×Nダメージ" },
		{ "dealDamagePerPermanent", "置物数×Nダメージ" },
		{ "dealDamagePerRandomPlayed", "運任せ数×Nダメージ" },
		{ "dealDamagePerNegStrength", "下げた筋力×Nダメージ" },
		{ "dealDamagePerAttackPlayed", "このターンの攻撃数×Nダメージ" },
		{ "dealDamagePerWeak", "対象の威圧×N追加ダメージ" },
		{ "gainBlock", "ブロックN" },
		{ "gainBlockPerEnergyMax", "上限×Nブロック" },
		{ "gainBlockPerPermanent", "置物数×Nブロック" },
		{ "gainIceBlock", "氷壁N(持ち越し)" },
		{ "gainIceBlockPerCardPlayed", "詠唱数×N氷壁" },
		{ "gainIceBlockPerHandCard", "手札数×N氷壁" },
		{ "gainBlockPerHandCard", "手札数×Nブロック" },
		{ "staggerEnemy", "対象の体勢を崩す（次の行動が隙になる）" },
		{ "stripRider", "対象のいまの行動の付随物（状態異常・同時強化・同時防御）を消す" },
		{ "singleHit", "対象のいまの攻撃の連撃を1回にする" },
		{ "nullifyNextEnemyAttack", "このターン最初に受ける攻撃1回のHP損失を0にする" },
		{ "drawCardsNextTurn", "次のターンの開始時にN枚多くドロー" },
		{ "gainEnergyNextTurn", "次のターンの開始時に一時マナ+N" },
		{ "addLightNextTurn", "次のターンの開始時に灯+N" },
		{ "gainBlockNextTurn", "次のターンの開始時にブロック+N" },
		{ "gainHp", "HP回復N" },
		{ "loseHp", "自傷HP-N" },
		{ "counter", "返しN" },
		{ "negate", "打ち消し" },
		{ "negateConvertIce", "打ち消し+実値ぶん氷壁" },
		{ "drawCards", "Nドロー" },
		{ "drawCardsPerCardPlayed", "詠唱数×Nドロー" },
		{ "impulseDraw", "衝動ドローN(このターン限り)" },
		{ "gainEnergy", "一時マナ+N" },
		{ "gainEnergyMax", "エナジー上限+N" },
		{ "discountNext", "次のカード-N" },
		{ "addGrowth", "成長+N" },
		{ "doubleGrowth", "成長2倍" },
		{ "dischargeGrowth", "成長放出(×Nダメージ・全消費)" },
		{ "dischargeGrowthBlock", "成長×Nブロック(全消費)" },
		{ "addMomentum", "勢い+N" },
		{ "doubleMomentum", "勢い2倍" },
		{ "dischargeMomentumBlock", "勢い×Nブロック(全消費)" },
		{ "dischargeMomentumBurn", "勢い×N延焼(全消費)" },
		{ "dischargeMomentumDamage", "勢い×Nダメージ(全消費)" },
		{ "dischargeMomentumGrowth", "勢い÷Nを成長に(全消費)" },
		{ "dischargeMomentumVolley", "勢い×Nダメージを3回(全消費)" },
		{ "momentumCarryHalf", "勢いの半分を持ち越す(常在)" },
		{ "applyBurn", "延焼+N" },
		{ "applyBurnPerDamageTaken", "受けたダメージ×N延焼" },
		{ "dischargeBurn", "爆熱(延焼×Nダメージ・全消費)" },
		{ "addAether", "霊気+N" },
		{ "dischargeAether", "霊気放出(×Nダメージ・全消費)" },
		{ "dischargeAetherDraw", "霊気×Nドロー(全消費)" },
		{ "addLight", "灯+N" },
		{ "dischargeLight", "灯を全て放出し、灯1につきNダメージ(灯の数だけヒット。全体は灯×N)" },
		{ "dischargeLightRally", "灯を全て放出し、灯Nにつき全ての人形が1回動く" },
		{ "dealDamagePerLight", "灯2につきNダメージ(切り捨て。灯は失わない)" },
		{ "addCardToDraw", "火種N枚を山札のランダムな位置に混ぜる(この戦闘限り)" },
		{ "addCardToDiscard", "火種N枚を捨て札に加える(この戦闘限り)" },
		{ "transformDeckToToken", "山札の札N枚を選んで火種に変える" },
		{ "lightToSparks", "灯Nにつき火種1枚を山札へ(払った灯だけ失う)" },
		{ "dealDamagePerSpark", "この戦闘で撃った火種×Nダメージ" },
		{ "gainBlockPerSpark", "この戦闘で撃った火種×Nブロック" },
		{ "scry", "占術N(山札の上N枚を見て、要らない札を捨て札へ)" },
		{ "dealDamagePerScry", "この戦闘で占術で捨てた枚数×Nダメージ" },
		{ "dealDamagePerTrapFired", "この戦闘でからくりが動いた回数×Nダメージ" },
		{ "extendTrapLife", "仕込んでいるからくりすべての期限をNターン延ばす" },
		{ "trapsNeverExpire", "この置物がある間、からくりは期限切れにならない" },
		{ "retainHandUpTo", "この置物がある間、ターン終了時に手札をN枚まで選んで残せる" },
		{ "retrieveZeroCostFromDiscard", "捨て札のコスト0の札を全て手札に戻す" },
		{ "drawTypeFromDeck", "山札の仕込み札N枚を(上から見て最初のものを)手札に加える" },
		{ "aetherCarryHalf", "この置物がある間、霊気を放出しても半分が残る" },
		{ "retainedCostDown", "この置物がある間、敵ターンの後も手札に残った札はコスト-N(手札を離れると戻る)" },
		{ "triggerRandomRetainer", "場の人形1体(ランダム)の効果を今1回解決(灯は産まない)" },
		{ "dischargeLightWeaken", "灯を全て放出し、灯3につき敵全体に威圧N(灯3未満なら不発)" },
		{ "consumeLight", "灯をN失う" },
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
		{ "shatterBlockConvert", "粉砕+破壊値ダメージ" },
		{ "exhaustFromDeck", "山札の上N枚を消滅(ミル)" },
		{ "exhaustFromDeckChoose", "選んでN枚消滅(引導)" },
		{ "recycleExhaust", "輪廻(消滅を山札へ・×Nダメージ)" },
		{ "retrieveFromExhaust", "消滅置き場から回収" },
		{ "playFromExhaust", "消滅置き場から直接プレイ" },
		{ "summonPermanent", "召喚N体" },
		{ "addCardToHand", "トークンN枚を手札へ" },
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
		{ "empowerShivs", "【常在】ナイフ与ダメージ+N" },
		{ "gainSetSlot", "仕込み枠+N(この戦闘中)" },
		{ "retrieveFromDiscard", "捨て札からN枚を手札へ(選ぶ)" },
		{ "searchDeck", "山札からN枚を手札へ(選ぶ)" },
		{ "addCopyToDiscard", "コピーN枚を捨て札へ" },
		{ "growSelf", "プレイするたび与ダメージ+N(この戦闘中)" },
		{ "upgradeInHand", "手札のN枚をこの戦闘中鍛える" },
		{ "upgradeAllInHand", "手札の全てをこの戦闘中鍛える" },
		{ "gainMaxHp", "最大HP+N(戦闘後も残る)" },
		{ "gainBlockPerMomentum", "勢い×Nブロック(失わない)" },
		{ "addGrowthPerMomentum", "勢い2につき成長+N(失わない)" }
	};

	private static readonly Dictionary<string, string> IntentKindJa = new Dictionary<string, string>
	{
		{ "attack", "攻撃" },
		{ "defend", "防御" },
		{ "buff", "筋力上げ" },
		{ "rally", "応援" },
		{ "heal", "回復" },
		{ "hex", "呪い" },
		{ "destroy-set", "からくり壊し" },
		{ "destroy-token", "人形狩り" },
		{ "steal-gold", "盗み" },
		{ "flee", "逃走" },
		{ "mill", "山札喰い" },
		{ "seal", "技封じ" },
		{ "rest", "隙" },
		{ "hatch", "孵化" },
		{ "summon", "召喚" }
	};

	private static readonly Dictionary<string, string> StatusJa = new Dictionary<string, string>
	{
		{ "weak", "弱体" },
		{ "vulnerable", "脆弱" },
		{ "frail", "虚弱" },
		{ "wound", "負傷" },
		{ "junk", "がらくた" },
		{ "scald", "火傷" },
		{ "restrain", "拘束" },
		{ "mist", "霞み" },
		{ "slow", "重り" }
	};

	private static readonly Dictionary<string, string> TypeJaMap = new Dictionary<string, string>
	{
		{ "physical", "物理" },
		{ "spell", "呪文" },
		{ "reaction", "仕込み札" },
		{ "permanent", "置物" }
	};

	private static readonly Dictionary<string, string> RarityJa = new Dictionary<string, string>
	{
		{ "common", "C" },
		{ "uncommon", "U" },
		{ "rare", "R" },
		{ "boss", "ボス" },
		{ "shop", "店" },
		{ "event", "?" }
	};

	public const char ClauseOpen = '\ue000';

	public const char ClauseClose = '\ue001';

	public static bool MarkClauses;

	public const string EchoMissNote = "単体の効果は1回目で対象が倒れたら2回目は空振り（別の敵や分裂・残機の次の姿には向かない）";

	public static string Colored(int baseAmt, int shown)
	{
		return shown.ToString();
	}

	public static string TypeJa(string t)
	{
		if (!TypeJaMap.TryGetValue((t == null) ? "" : t, out var v))
		{
			if (t != null)
			{
				return t;
			}
			return "";
		}
		return v;
	}

	public static string RarityLabel(string r)
	{
		if (string.IsNullOrEmpty(r))
		{
			return "";
		}
		if (!RarityJa.TryGetValue(r, out var v))
		{
			return r;
		}
		return v;
	}

	public static string CostLabel(CardDef def)
	{
		if (def == null)
		{
			return "";
		}
		if (def.XCost == true)
		{
			if (def.XBonus.GetValueOrDefault() <= 0)
			{
				return "X";
			}
			return "X+" + def.XBonus.Value;
		}
		return def.Cost.ToString();
	}

	public static string CardName(string cardId)
	{
		try
		{
			return Content.GetCardDef(cardId).Name;
		}
		catch (Exception)
		{
			try
			{
				CardDef f = Fusion.ResolveFusedDef(cardId);
				return (f != null) ? f.Name : cardId;
			}
			catch (Exception)
			{
				return cardId;
			}
		}
	}

	public static string Short(string s, int max)
	{
		if (s == null)
		{
			return "";
		}
		s = s.Replace("\n", " / ");
		if (s.Length > max)
		{
			return s.Substring(0, max - 1) + "…";
		}
		return s;
	}

	private static string Num(double d)
	{
		return d.ToString("0.##", CultureInfo.InvariantCulture);
	}

	public static string ConditionLabel(EffectCondition c)
	{
		if (c == null)
		{
			return "";
		}
		List<string> parts = new List<string>();
		if (c.HpAtOrBelowRatio.HasValue)
		{
			parts.Add("自分のHPが" + Mathf_Round(c.HpAtOrBelowRatio.Value * 100.0) + "%以下");
		}
		if (c.MinDamageTaken.HasValue)
		{
			parts.Add(c.MinDamageTaken.Value + "以上のダメージを受けた");
		}
		if (c.MaxActionValue.HasValue)
		{
			parts.Add("敵の行動値が" + c.MaxActionValue.Value + "以下");
		}
		if (c.MinActionValue.HasValue)
		{
			parts.Add("敵の行動値が" + c.MinActionValue.Value + "以上");
		}
		if (c.Kindled == true)
		{
			parts.Add("くべたら");
		}
		if (c.Blaze == true)
		{
			parts.Add("猛り火(延焼合計8以上)");
		}
		if (c.MinGrowth.HasValue)
		{
			parts.Add("成長" + c.MinGrowth.Value + "以上");
		}
		if (c.MinMomentum.HasValue)
		{
			parts.Add("勢い" + c.MinMomentum.Value + "以上");
		}
		if (c.MinLight.HasValue)
		{
			parts.Add("灯" + c.MinLight.Value + "以上");
		}
		if (c.EnemyIntent != null)
		{
			parts.Add("対象の意図が" + KindJa(c.EnemyIntent));
		}
		if (c.EnemyIntentNot != null)
		{
			parts.Add("対象の意図が" + KindJa(c.EnemyIntentNot) + "以外");
		}
		if (c.EnemyExposed == true)
		{
			parts.Add("対象が急所持ち");
		}
		if (c.PerfectBlockLastPhase == true)
		{
			parts.Add("直前の敵フェーズを完全に凌いだ");
		}
		if (c.TargetDead == true)
		{
			parts.Add("とどめ");
		}
		if (c.TargetAlive == true)
		{
			parts.Add("倒せなければ");
		}
		if (c.PerfectBlockThisPhase == true)
		{
			parts.Add("この敵フェーズを完全に凌いだら");
		}
		if (c.LastActionNoHpLoss == true)
		{
			parts.Add("完全に凌いだ時");
		}
		if (c.HealedThisTurn == true)
		{
			parts.Add("このターン、先にカードで回復していたら");
		}
		if (c.Turn.HasValue)
		{
			parts.Add(c.Turn.Value + "ターン目");
		}
		if (c.BlockZero == true)
		{
			parts.Add("ブロックが0なら");
		}
		if (c.NoAttackThisTurn == true)
		{
			parts.Add("このターン攻撃札を1枚もプレイしていなければ");
		}
		if (c.MaxPlaysThisTurn.HasValue)
		{
			parts.Add("このターンのプレイが" + c.MaxPlaysThisTurn.Value + "枚以下なら");
		}
		if (c.MinEnergyMax.HasValue)
		{
			parts.Add("エナジー上限" + c.MinEnergyMax.Value + "以上");
		}
		if (c.ActionKinds != null && c.ActionKinds.Count > 0)
		{
			List<string> ks = new List<string>();
			for (int i = 0; i < c.ActionKinds.Count; i++)
			{
				ks.Add(KindJa(c.ActionKinds[i]));
			}
			parts.Add("敵の行動が" + string.Join("・", ks.ToArray()) + "の時");
		}
		if (c.ActionKindsNot != null && c.ActionKindsNot.Count > 0)
		{
			List<string> ks2 = new List<string>();
			for (int j = 0; j < c.ActionKindsNot.Count; j++)
			{
				ks2.Add(KindJa(c.ActionKindsNot[j]));
			}
			parts.Add("敵の行動が" + string.Join("・", ks2.ToArray()) + "以外の時");
		}
		if (parts.Count == 0)
		{
			return "";
		}
		string join = string.Join("かつ", parts.ToArray());
		string clause = join + ((join.EndsWith("ら") || join.EndsWith("ば") || join.EndsWith("時")) ? "、" : "なら、");
		if (!MarkClauses)
		{
			return clause;
		}
		return "\ue000" + clause + "\ue001";
	}

	private static int Mathf_Round(double v)
	{
		return (int)Math.Round(v, MidpointRounding.AwayFromZero);
	}

	public static string KindJa(string kind)
	{
		if (!IntentKindJa.TryGetValue((kind == null) ? "" : kind, out var v))
		{
			if (kind != null)
			{
				return kind;
			}
			return "";
		}
		return v;
	}

	public static string EffectLine(DeclarativeEffect e, string holderType, bool hideTrigger = false)
	{
		StringBuilder sb = new StringBuilder();
		if (!hideTrigger)
		{
			sb.Append(Heading(e, holderType));
		}
		sb.Append(ConditionLabel(e.Condition));
		string every = null;
		if (e.Every.HasValue && e.Once != null)
		{
			every = "(" + ((e.Once == "turn") ? "1ターンに" : "戦闘で") + e.Every.Value + "回目の時だけ) ";
		}
		else if (e.Every.HasValue)
		{
			every = "(" + ((e.EveryScope == "turn") ? "1ターンに" : "") + e.Every.Value + "回ごとに1回) ";
		}
		else if (e.Once != null)
		{
			every = "(" + ((e.Once == "turn") ? "ターンに" : "戦闘で") + "1回だけ) ";
		}
		if (every != null)
		{
			sb.Append(MarkClauses ? ("\ue000" + every + "\ue001") : every);
		}
		if (e.Target == "all")
		{
			sb.Append("敵全体に ");
		}
		else if (e.Target == "mostBurn")
		{
			sb.Append("延焼がいちばん多い敵に ");
		}
		else if (e.Target == "mostHp")
		{
			sb.Append("HPがいちばん多い敵に ");
		}
		if (e.Target == null)
		{
			sb.Append(Effects.PermanentTargetNote(e, holderType));
		}
		sb.Append(EffectBody(e));
		if (e.Pierce == true)
		{
			sb.Append("(貫通)");
		}
		if (e.XHits == true)
		{
			sb.Append("×X回");
		}
		if (e.VolleyHits.HasValue)
		{
			sb.Append("×" + e.VolleyHits.Value + "回");
		}
		if (e.GrowthMultiplier.HasValue)
		{
			sb.Append("〔成長が×" + Num(e.GrowthMultiplier.Value) + "で乗る〕");
		}
		if (e.MomentumMultiplier.HasValue)
		{
			sb.Append("〔勢いが×" + Num(e.MomentumMultiplier.Value) + "で乗る〕");
		}
		if (e.SpendBlock == true)
		{
			sb.Append("(解決後にブロックを全て失う)");
		}
		if (e.ExhaustThreshold.HasValue)
		{
			int mx = (e.AmountMax.HasValue ? e.AmountMax.Value : 0);
			int am = (e.Amount.HasValue ? e.Amount.Value : 0);
			sb.Append("〔忘却の刻" + e.ExhaustThreshold.Value + ": " + ((mx >= am) ? (mx + "に増える") : ((mx == 0) ? "以降は停止" : (mx + "に減少"))) + "〕");
		}
		return sb.ToString();
	}

	private static string Heading(DeclarativeEffect e, string holderType)
	{
		if (e.Trigger == null || e.Trigger == "onPlay")
		{
			if (!(holderType == "permanent"))
			{
				return "";
			}
			return "登場時: ";
		}
		string tj;
		return (TriggerJa.TryGetValue(e.Trigger, out tj) ? tj : e.Trigger) + ": ";
	}

	private static string EffectBody(DeclarativeEffect e)
	{
		int amt = (e.Amount.HasValue ? e.Amount.Value : 0);
		if (e.Effect == "dealDamageRandom")
		{
			return "ランダムダメージ" + amt + "〜" + (e.AmountMax.HasValue ? e.AmountMax.Value : amt);
		}
		if (e.Effect == "summonPermanent")
		{
			return "召喚" + amt + "体: " + CardName(e.SummonId);
		}
		if (e.Effect == "addCardToHand")
		{
			return CardName(e.SummonId) + "を" + amt + "枚手札へ";
		}
		if (e.Effect == "searchDeck" && e.CardType == "reaction")
		{
			return "山札から仕込み札" + amt + "枚を手札へ(選ぶ)";
		}
		if (e.Effect == "dealDamage" && DamageModifier != null && (e.Trigger == null || e.Trigger == "onPlay"))
		{
			int shown = DamageModifier(amt);
			return "ダメージ" + Colored(amt, shown);
		}
		if (EffectJa.TryGetValue(e.Effect, out var tpl))
		{
			return tpl.Replace("N", amt.ToString());
		}
		return e.Effect + (e.Amount.HasValue ? (" " + amt) : "");
	}

	private static bool IsKanaWord(char c)
	{
		if (c < 'ァ' || c > 'ヺ')
		{
			return c == 'ー';
		}
		return true;
	}

	public static string NoBreak(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return Emphasize("\ue000" + s + "\ue001", null, phrase: true);
		}
		return s;
	}

	private static bool IsKanji(char c)
	{
		if (c < '一' || c > '鿿')
		{
			return c == '々';
		}
		return true;
	}

	private static bool IsHira(char c)
	{
		if (c >= 'ぁ')
		{
			return c <= 'ゟ';
		}
		return false;
	}

	public static string Emphasize(string body, string clauseColor = null, bool phrase = false)
	{
		if (string.IsNullOrEmpty(body))
		{
			return body;
		}
		StringBuilder sb = new StringBuilder();
		bool clause = false;
		int tailEnd = -1;
		int i = 0;
		while (i < body.Length)
		{
			if (tailEnd >= 0 && i >= tailEnd)
			{
				sb.Append("</nobr>");
				tailEnd = -1;
			}
			bool inTail = tailEnd >= 0;
			char ch = body[i];
			if (ch == '\ue000' || ch == '\ue001')
			{
				clause = ch == '\ue000';
				if (clauseColor != null)
				{
					sb.Append(clause ? ("<color=#" + clauseColor + ">") : "</color>");
				}
				i++;
				continue;
			}
			if (ch == '<')
			{
				int j = body.IndexOf('>', i);
				if (j < 0)
				{
					sb.Append(body.Substring(i));
					break;
				}
				sb.Append(body, i, j - i + 1);
				i = j + 1;
				continue;
			}
			if (ch == ':' && i + 1 < body.Length && body[i + 1] == ' ' && !inTail)
			{
				int end = body.IndexOf('\n', i + 2);
				if (end < 0)
				{
					end = body.Length;
				}
				int vis = VisibleLength(body, i + 2, end);
				sb.Append(": ");
				i += 2;
				if (vis > 0 && vis <= 8 && body.IndexOf('\ue000', i, end - i) < 0)
				{
					sb.Append("<nobr>");
					tailEnd = end;
				}
				continue;
			}
			if (ch == '(' && !clause)
			{
				int j2 = body.IndexOf(')', i);
				if (j2 > i + 1 && j2 - i - 1 <= 8)
				{
					sb.Append(Nb("<size=90%><mark=#3b2f2f22> " + body.Substring(i + 1, j2 - i - 1) + " </mark></size>", inTail));
					i = j2 + 1;
					continue;
				}
			}
			if (ch == '×' && TryTimes(body, i, out var k))
			{
				string t = body.Substring(i, k - i);
				sb.Append(Nb(clause ? t : ("<size=115%>" + t + "</size>"), inTail));
				i = k;
				continue;
			}
			string num;
			if (phrase && !inTail && IsKanji(ch))
			{
				int l;
				for (l = i; l < body.Length && IsKanji(body[l]); l++)
				{
				}
				int m;
				for (m = l; m < body.Length && IsHira(body[m]) && m - l < 4; m++)
				{
				}
				if (TryNumber(body, m, clause, out k, out num))
				{
					if (k < body.Length && "枚回体EG".IndexOf(body[k]) >= 0)
					{
						num += body[k];
						k++;
					}
					if (m - i + (k - m) <= 12)
					{
						sb.Append(Nb(body.Substring(i, m - i) + num, inOuter: false));
						i = k;
						continue;
					}
				}
				if (m > l && m - i <= 6)
				{
					sb.Append(Nb(body.Substring(i, m - i), inOuter: false));
					i = m;
					continue;
				}
			}
			if (IsKanaWord(ch))
			{
				int n;
				for (n = i; n < body.Length && IsKanaWord(body[n]); n++)
				{
				}
				if (n - i >= 2)
				{
					string word = body.Substring(i, n - i);
					if (TryNumber(body, n, clause, out k, out num))
					{
						word += num;
						n = k;
					}
					sb.Append(Nb(word, inTail));
					i = n;
					continue;
				}
			}
			if (TryNumber(body, i, clause, out k, out num))
			{
				if (k < body.Length && "枚回体EG".IndexOf(body[k]) >= 0)
				{
					num += body[k];
					k++;
				}
				else if (k < body.Length && IsKanaWord(body[k]))
				{
					int num2;
					for (num2 = k; num2 < body.Length && IsKanaWord(body[num2]); num2++)
					{
					}
					num += body.Substring(k, num2 - k);
					k = num2;
				}
				sb.Append(Nb(num, inTail));
				i = k;
			}
			else
			{
				sb.Append(ch);
				i++;
			}
		}
		if (tailEnd >= 0)
		{
			sb.Append("</nobr>");
		}
		return sb.ToString();
	}

	private static string Nb(string s, bool inOuter)
	{
		if (!inOuter)
		{
			return "<nobr>" + s + "</nobr>";
		}
		return s;
	}

	private static int VisibleLength(string s, int from, int to)
	{
		int n = 0;
		for (int i = from; i < to; i++)
		{
			switch (s[i])
			{
			case '<':
			{
				int j = s.IndexOf('>', i);
				if (j >= 0 && j < to)
				{
					i = j;
					continue;
				}
				break;
			}
			default:
				n++;
				continue;
			case '\ue000':
			case '\ue001':
				continue;
			}
			break;
		}
		return n;
	}

	private static bool TryTimes(string s, int i, out int end)
	{
		end = i;
		int k = i + 1;
		if (k < s.Length && s[k] == 'X')
		{
			k++;
		}
		else
		{
			int d = k;
			for (; k < s.Length && char.IsDigit(s[k]); k++)
			{
			}
			if (k == d)
			{
				return false;
			}
		}
		if (k >= s.Length || s[k] != '回')
		{
			return false;
		}
		end = k + 1;
		return true;
	}

	private static bool TryNumber(string s, int pos, bool plain, out int end, out string markup)
	{
		end = pos;
		markup = null;
		int p = pos;
		string open = null;
		if (p < s.Length && s[p] == '<' && string.CompareOrdinal(s, p, "<color=", 0, 7) == 0)
		{
			int q = s.IndexOf('>', p);
			if (q < 0)
			{
				return false;
			}
			open = s.Substring(p, q - p + 1);
			p = q + 1;
		}
		if (p >= s.Length)
		{
			return false;
		}
		bool sign = (s[p] == '+' || s[p] == '-') && p + 1 < s.Length && char.IsDigit(s[p + 1]) && (p == 0 || !char.IsDigit(s[p - 1]));
		if (!char.IsDigit(s[p]) && !sign)
		{
			return false;
		}
		int d;
		for (d = (sign ? (p + 1) : p); d < s.Length && char.IsDigit(s[d]); d++)
		{
		}
		string digits = s.Substring(p, d - p);
		string close = null;
		if (open != null && string.CompareOrdinal(s, d, "</color>", 0, 8) == 0)
		{
			close = "</color>";
			d += 8;
		}
		markup = open + (plain ? digits : ("<size=130%>" + digits + "</size>")) + close;
		end = d;
		return true;
	}

	public static string Body(CardDef def, bool withTriggers = true, ICollection<string> hideTriggers = null)
	{
		if (def.Id == "status_wound")
		{
			return "使えない（ターン終了時に捨てられる）";
		}
		if (def.Id == "status_scald")
		{
			return "使えない。自ターン終了時に手札にあるとHP-2（この戦闘限り。捨て/消滅コストの支払いには使える）";
		}
		if (def.Id == "status_brand")
		{
			return "使えない。自ターン終了時に手札にあるとHP-1（デッキに残る呪い。ショップの除去で取り除ける。青い蝋燭があればコスト0・HP-1・消滅で出せる）";
		}
		if (def.Id == "status_guilt")
		{
			return "使えない。自ターン終了時に手札にあるとHP-1（仮初の呪い。5戦すると自然に消える）";
		}
		List<string> lines = LinesOf(def.Effects, def.Type, withTriggers, hideTriggers);
		if (def.Cascade.GetValueOrDefault() > 0)
		{
			lines.Insert(0, "チェイン" + ((def.Cascade > 1) ? ("×" + def.Cascade) : "") + ": 山札からより低コストの札をコストを払わずにプレイ(本体より先)");
		}
		if (def.Modes != null)
		{
			for (int m = 0; m < def.Modes.Count; m++)
			{
				List<string> inner = LinesOf(def.Modes[m].Effects, def.Type, withTriggers, hideTriggers);
				lines.Add("◆" + string.Join(MarkClauses ? "、" : " / ", inner.ToArray()));
			}
		}
		return string.Join("\n", lines.ToArray());
	}

	private static List<string> LinesOf(IReadOnlyList<DeclarativeEffect> effects, string holderType, bool withTriggers = true, ICollection<string> hideTriggers = null)
	{
		List<string> lines = new List<string>();
		if (effects == null)
		{
			return lines;
		}
		List<string> heads = new List<string>();
		List<string> bodies = new List<string>();
		for (int i = 0; i < effects.Count; i++)
		{
			DeclarativeEffect e = effects[i];
			bool hide = !withTriggers || (hideTriggers != null && e.Trigger != null && hideTriggers.Contains(e.Trigger));
			heads.Add(hide ? "" : Heading(e, holderType));
			bodies.Add(EffectLine(e, holderType, hideTrigger: true));
		}
		string prevHead = null;
		int a = 0;
		while (a < bodies.Count)
		{
			bool folded = false;
			for (int k = 2; k <= 3; k++)
			{
				if (folded)
				{
					break;
				}
				if (a + 2 * k > bodies.Count)
				{
					break;
				}
				bool sameHead = true;
				bool allSame = true;
				for (int j = 1; j < k; j++)
				{
					if (heads[a + j] != heads[a])
					{
						sameHead = false;
					}
					if (bodies[a + j] != bodies[a])
					{
						allSame = false;
					}
				}
				if (!sameHead | allSame)
				{
					continue;
				}
				int reps;
				for (reps = 1; a + (reps + 1) * k <= bodies.Count; reps++)
				{
					bool match = true;
					for (int l = 0; l < k; l++)
					{
						if (heads[a + reps * k + l] != heads[a + l] || bodies[a + reps * k + l] != bodies[a + l])
						{
							match = false;
							break;
						}
					}
					if (!match)
					{
						break;
					}
				}
				if (reps >= 2)
				{
					List<string> cyc = new List<string>();
					for (int m = 0; m < k; m++)
					{
						cyc.Add(bodies[a + m]);
					}
					string hh = heads[a];
					lines.Add(((hh.Length > 0 && hh == prevHead) ? "" : hh) + "［" + string.Join(" → ", cyc.ToArray()) + "］×" + reps + "回");
					prevHead = hh;
					a += reps * k;
					folded = true;
				}
			}
			if (!folded)
			{
				int b;
				for (b = a; b + 1 < bodies.Count && heads[b + 1] == heads[a] && bodies[b + 1] == bodies[a]; b++)
				{
				}
				int n = b - a + 1;
				string text = ((n > 1) ? (bodies[a] + " ×" + n + "回") : bodies[a]);
				string h = heads[a];
				lines.Add(((h.Length > 0 && h == prevHead) ? "" : h) + text);
				prevHead = h;
				a = b + 1;
			}
		}
		return lines;
	}

	public static string Notes(CardDef def)
	{
		List<string> n = CostNotes(def);
		n.AddRange(TrailNotes(def));
		if (n.Count != 0)
		{
			return string.Join(" / ", n.ToArray());
		}
		return "";
	}

	public static List<string> CostNotes(CardDef def)
	{
		List<string> n = new List<string>();
		if (def.XCost == true)
		{
			n.Add((def.XBonus.GetValueOrDefault() > 0) ? ("X: エナジーを全て払う (払った量+" + def.XBonus.Value + "として解決)") : "X: エナジーを全て払う");
		}
		if (((def.DiscardCost.HasValue && def.DiscardCost.Value != 0) ? 1 : 0) > 0)
		{
			n.Add("追加コスト: 手札" + def.DiscardCost.Value + "枚を捨てる");
		}
		if (((def.ExhaustCost.HasValue && def.ExhaustCost.Value != 0) ? 1 : 0) > 0)
		{
			n.Add("追加コスト: 手札" + def.ExhaustCost.Value + "枚を消滅");
		}
		if (def.ImpulseCost.GetValueOrDefault() > 0)
		{
			n.Add(((def.KindleOptional == true) ? "くべてもよい: " : "追加コスト: ") + "衝動の札" + def.ImpulseCost + "枚を消滅" + ((def.KindleOptional == true) ? "(くべると追加効果)" : ""));
		}
		if (((def.LightCost.HasValue && def.LightCost.Value != 0) ? 1 : 0) > 0)
		{
			n.Add("追加コスト: 灯を" + def.LightCost.Value + "払う");
		}
		if (def.NecroCost.HasValue)
		{
			n.Add("亡骸プレイ（コスト" + def.NecroCost.Value + "）");
		}
		if (def.FreeIfHandAllPhysical == true)
		{
			n.Add("手札が物理だけならコスト0");
		}
		if (def.FreeIfHandAll != null)
		{
			n.Add("手札が" + TypeJa(def.FreeIfHandAll) + "だけならコスト0");
		}
		if (def.FreeIfMomentumAtLeast.HasValue)
		{
			n.Add("勢い" + def.FreeIfMomentumAtLeast.Value + "以上ならコスト0");
		}
		if (def.RequiresRetainer == true)
		{
			n.Add("プレイ条件: 場に人形が1体以上");
		}
		if (def.BlazeDiscount.HasValue)
		{
			n.Add("猛り火の間はコスト-" + def.BlazeDiscount.Value);
		}
		return n;
	}

	public static List<string> TrailNotes(CardDef def, bool face = false)
	{
		List<string> n = new List<string>();
		if (def.Exhaust == true)
		{
			n.Add("消滅");
		}
		if (def.Retain == true)
		{
			n.Add("保持");
		}
		if (def.Echo == true)
		{
			n.Add("反復内蔵 (効果を2回解決)");
		}
		if (def.Echo == true || HasEffect(def, "addSpellEcho"))
		{
			n.Add(face ? "2回目も同じ敵を狙う" : "単体の効果は1回目で対象が倒れたら2回目は空振り（別の敵や分裂・残機の次の姿には向かない）");
		}
		if (def.FusionCatalyst == "cheaper")
		{
			n.Add("触媒: 素材にすると結果のコスト−1");
		}
		else if (def.FusionCatalyst == "echo")
		{
			n.Add("触媒: 素材にすると結果の効果を2回解決");
		}
		else if (def.FusionCatalyst == "retain")
		{
			n.Add("触媒: 素材にすると結果が保持");
		}
		else if (def.FusionCatalyst == "aoe")
		{
			n.Add("触媒: 素材にすると結果のダメージが全体に");
		}
		if (def.ExhaustUnlessExposedEnemy == true)
		{
			n.Add("急所持ちがいなければ消滅");
		}
		if (def.Retainer == true)
		{
			bool grows = def.Effects.Any((DeclarativeEffect e) => e.Amount.HasValue && Effects.DOLL_GROWTH_EFFECTS.Contains(e.Effect));
			n.Add("人形: 出した瞬間に1回動く。" + ((def.LifePersist == true) ? "期限なし" : ("期限" + (def.Life ?? 3) + "ターン" + (face ? "" : "（出したターンを含む）"))) + (grows ? "。1ターンごとにダメージとブロック+1" : ""));
		}
		if (def.ShivToken == true)
		{
			n.Add("骨のナイフ");
		}
		return n;
	}

	private static bool HasEffect(CardDef def, string effect)
	{
		if (def == null)
		{
			return false;
		}
		if (def.Effects != null)
		{
			foreach (DeclarativeEffect effect2 in def.Effects)
			{
				if (effect2.Effect == effect)
				{
					return true;
				}
			}
		}
		if (def.Modes != null)
		{
			foreach (CardMode m in def.Modes)
			{
				if (m.Effects == null)
				{
					continue;
				}
				foreach (DeclarativeEffect effect3 in m.Effects)
				{
					if (effect3.Effect == effect)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static string StatusName(string status)
	{
		if (!StatusJa.TryGetValue(status, out var ja))
		{
			return status;
		}
		return ja;
	}

	public static string DebuffName(string effect)
	{
		return effect switch
		{
			"weakenEnemy" => "威圧", 
			"exposeEnemy" => "急所", 
			"confuse" => "混乱", 
			_ => effect ?? "", 
		};
	}

	public static string InflictSuffix(StatusInflict inf)
	{
		if (inf == null)
		{
			return "";
		}
		string dest;
		if (inf.Status == "wound")
		{
			dest = "(捨て札へ)";
		}
		else if (inf.Status == "junk")
		{
			dest = "(山札へ)";
		}
		else
		{
			dest = ((inf.Status == "scald") ? "(手札へ)" : "");
		}
		if (!StatusJa.TryGetValue(inf.Status, out var ja))
		{
			ja = inf.Status;
		}
		return " +" + ja + inf.Amount + dest;
	}

	public static string IntentLine(EnemyIntent it, int shownValue = -1)
	{
		if (it == null)
		{
			return "---";
		}
		int v = ((shownValue >= 0) ? shownValue : it.Actual);
		switch (it.Kind)
		{
		case "attack":
		{
			string hits;
			if (it.MirrorHits == true)
			{
				hits = "×手数";
			}
			else
			{
				hits = ((it.Hits.HasValue && it.Hits.Value > 1) ? ("×" + it.Hits.Value) : "");
			}
			string guard = (it.AlsoDefend.HasValue ? ("+ブロック" + it.AlsoDefend.Value) : "");
			string buff = (it.AlsoBuff.HasValue ? ("+筋力" + it.AlsoBuff.Value) : "");
			string breaks = ((it.AlsoDestroySet == true) ? "からくり壊し+" : "");
			return breaks + "攻撃 " + v + hits + guard + buff + InflictSuffix(it.Inflict);
		}
		case "defend":
			return "防御 " + it.Actual + (it.AlsoBuff.HasValue ? (" +筋力" + it.AlsoBuff.Value) : "");
		case "destroy-set":
			return "からくり壊し";
		case "destroy-token":
			return "人形狩り";
		case "buff":
			return "筋力 +" + it.Actual;
		case "rally":
			return "応援 +" + it.Actual + " (味方全体の筋力)";
		case "hex":
			return "呪い" + InflictSuffix(it.Inflict);
		case "heal":
			return "回復 " + it.Actual + " (最も傷んだ味方)";
		case "steal-gold":
			return "盗み " + it.Actual + "G";
		case "flee":
			return "逃走 (倒すか打ち消せば阻止)";
		case "rest":
			return "隙だらけ";
		case "hatch":
			return "孵化する";
		case "seal":
			return "技封じ (山札でいちばんレアな札を1枚、倒すまで封じる)";
		case "mill":
			return "山札喰い " + it.Actual + "枚" + (it.StrengthPerMilled.HasValue ? (" (食べた1枚ごとに筋力+" + it.StrengthPerMilled.Value + ")") : "");
		case "summon":
			return "召喚 ×" + it.Actual + " (場が4体なら出ない)";
		default:
			return KindJa(it.Kind);
		}
	}

	public static string LiveIntentLine(GameState st, int enemyIndex, EnemyIntent it)
	{
		if (it == null)
		{
			return "---";
		}
		string text = IntentLine((it.Inflict != null) ? it with
		{
			Inflict = Effects.DisplayedInflict(st, it.Inflict)
		} : it, Effects.DisplayedIntentValue(st, enemyIndex, it.Kind, it.Actual));
		List<string> notes = Effects.IntentModifierNotes(st, enemyIndex, it.Kind);
		int str = 0;
		try
		{
			str = Effects.EffectiveStrength(st, enemyIndex);
		}
		catch (Exception)
		{
		}
		bool strNote = it.Kind == "attack" && it.Base.HasValue && str != 0;
		if (!strNote && notes.Count == 0)
		{
			return text;
		}
		string lead = (strNote ? ("もとは" + it.Base.Value + "・筋力" + ((str > 0) ? "+" : "") + str + "で" + it.Actual) : ("もとは" + it.Actual));
		return text + " (" + lead + ((notes.Count > 0) ? ("・" + string.Join("・", notes)) : "") + ")";
	}

	public static string IntentText(GameState st, int enemyIndex)
	{
		if (st == null || enemyIndex < 0 || enemyIndex >= st.Enemies.Count)
		{
			return "---";
		}
		if (st.HideIntents == true)
		{
			return "？ 意図は見えない（ルーンの円蓋）";
		}
		EnemyIntent raw = st.Enemies[enemyIndex].Intent;
		EnemyIntent eff = Effects.EffectiveIntent(st, enemyIndex);
		string s = LiveIntentLine(st, enemyIndex, eff);
		if (raw != null && raw.ConditionalOn != null && raw.Alt != null)
		{
			bool altActive = (object)eff != raw;
			string what = ((raw.ConditionalOn == "set") ? "発動できる仕込み札" : "人形");
			s = s + "  【" + what + (altActive ? "あり" : "なし") + "分岐】";
			if (!altActive)
			{
				s = s + " ※" + what + "があると: " + LiveIntentLine(st, enemyIndex, BranchToIntent(raw.Alt));
			}
		}
		return s;
	}

	private static EnemyIntent BranchToIntent(EnemyIntentBranch b)
	{
		return new EnemyIntent
		{
			Kind = b.Kind,
			Actual = b.Actual,
			Hits = b.Hits,
			Inflict = b.Inflict,
			AlsoDefend = b.AlsoDefend,
			AlsoBuff = b.AlsoBuff,
			AlsoDestroySet = b.AlsoDestroySet
		};
	}

	public static string MoveShort(EnemyMove m, int strength = 0)
	{
		string mark = KindJa(m.Kind);
		int add = ((m.Kind == "attack") ? strength : 0);
		int? lo = (m.Min.HasValue ? new int?(Math.Max((m.Kind == "attack") ? 1 : m.Min.Value, m.Min.Value + add)) : ((int?)null));
		int? hi = (m.Max.HasValue ? new int?(Math.Max((m.Kind == "attack") ? 1 : m.Max.Value, m.Max.Value + add)) : ((int?)null));
		object obj;
		if (!lo.HasValue)
		{
			obj = "";
		}
		else if (lo != hi)
		{
			string text = lo.Value.ToString();
			int? num = hi;
			int? num2 = num;
			obj = text + "〜" + num2;
		}
		else
		{
			obj = lo.Value.ToString();
		}
		string range = (string)obj;
		if (m.DamageFromPlayerHp != null)
		{
			range = "(あなたのHP÷" + m.DamageFromPlayerHp.Divisor + "+" + m.DamageFromPlayerHp.Add + ((add != 0) ? (((add > 0) ? "+" : "") + add) : "") + ")";
		}
		string sign = ((m.Kind == "buff" || m.Kind == "rally") ? "+" : "");
		string hits;
		if (m.MirrorHits == true)
		{
			hits = "×手数";
		}
		else
		{
			hits = (((m.Hits ?? 1) > 1) ? ("×" + m.Hits.Value) : "");
		}
		string inflict = ((m.Inflict != null) ? (((range != "") ? "+" : "") + StatusName(m.Inflict.Status) + m.Inflict.Amount) : "");
		string riders = (m.AlsoDefend.HasValue ? ("+ブロック" + m.AlsoDefend.Value) : "") + (m.AlsoBuff.HasValue ? ("+筋力" + m.AlsoBuff.Value) : "") + ((m.AlsoDestroySet == true) ? "+からくり壊し" : "") + (m.StrengthPerMilled.HasValue ? ("+食べた1枚ごとに筋力" + m.StrengthPerMilled.Value) : "") + (m.GrowPerUse.HasValue ? ("(使うたび+" + m.GrowPerUse.Value + ")") : "") + (m.GrowHitsPerUse.HasValue ? ("(使うたびヒット+" + m.GrowHitsPerUse.Value + ")") : "");
		return mark + sign + range + hits + inflict + riders;
	}

	private static bool HasOtherAlive(GameState st, int index)
	{
		if (st == null)
		{
			return true;
		}
		for (int j = 0; j < st.Enemies.Count; j++)
		{
			if (j != index && st.Enemies[j].Hp > 0)
			{
				return true;
			}
		}
		return false;
	}

	public static string EnemyTraits(EnemyDef d, GameState st = null, int index = -1)
	{
		if (d == null)
		{
			return "";
		}
		List<string> t = new List<string>();
		EnemyState e = ((st != null && index >= 0 && index < st.Enemies.Count) ? st.Enemies[index] : null);
		int strength = ((e != null && st != null) ? Effects.EffectiveStrength(st, index) : 0);
		if (d.Armor.HasValue)
		{
			t.Add("装甲" + d.Armor.Value);
		}
		if (d.TurnArmor.HasValue)
		{
			t.Add("ターン装甲" + d.TurnArmor.Value);
		}
		if (d.Thorns.HasValue)
		{
			t.Add("とげ" + d.Thorns.Value);
		}
		if (d.Regen.HasValue)
		{
			t.Add("再生" + d.Regen.Value + (d.RegenBreak.HasValue ? ("(1Tに" + d.RegenBreak.Value + "で止まる)") : ""));
		}
		if (d.BurnResist.HasValue)
		{
			t.Add("延焼耐性" + d.BurnResist.Value);
		}
		if (d.StartingBlock.HasValue)
		{
			t.Add("開幕ブロック" + d.StartingBlock.Value);
		}
		if (d.Artifact.HasValue)
		{
			t.Add("アーティファクト" + d.Artifact.Value);
		}
		if (d.Guardian == true)
		{
			t.Add("庇う");
		}
		if (d.BondStrength.HasValue)
		{
			t.Add("連携+" + d.BondStrength.Value);
		}
		if (d.MournStrength.HasValue)
		{
			t.Add("弔い+" + d.MournStrength.Value);
		}
		if (d.Nemesis == true)
		{
			t.Add("因縁(奇数Tは無形)");
		}
		if (d.Imbalanced == true)
		{
			t.Add("バランス崩し");
		}
		if (e != null && e.Sealed != null && e.Sealed.Count > 0)
		{
			t.Add("封じている札: " + string.Join("・", e.Sealed.Select((CardInstance c) => c.Def.Name)) + " (倒せば手札に戻る)");
		}
		if (d.Slippery.HasValue)
		{
			t.Add((e != null && e.Slippery.GetValueOrDefault() <= 0) ? "朧(霧が晴れた)" : ("朧" + ((e != null) ? e.Slippery.GetValueOrDefault() : d.Slippery.Value) + "回"));
		}
		if (d.Burrow != null)
		{
			t.Add("潜伏(殻" + d.Burrow.Block + ")");
		}
		if (d.SplitInto != null)
		{
			t.Add("分裂→" + d.SplitInto.Count + "体");
		}
		if (d.HatchInto != null)
		{
			t.Add("孵化");
		}
		if (d.Interrupts != null && (e == null || e.InterruptBlocked != true))
		{
			for (int k = 0; k < d.Interrupts.Count; k++)
			{
				EnemyInterrupt it = d.Interrupts[k];
				if (e != null && e.FiredInterrupts != null && e.FiredInterrupts.Contains(k))
				{
					continue;
				}
				EnemyMove first = EnemyGraph.FirstMoveOf(d, it.Goto);
				string arrow = "";
				if (first != null)
				{
					arrow = ": " + MoveShort(first, strength);
					string nextId = ((d.Nodes.TryGetValue(it.Goto, out EnemyNode node) && node.Move != null) ? node.Next : null);
					EnemyMove second = ((nextId != null && nextId != it.Goto) ? EnemyGraph.FirstMoveOf(d, nextId) : null);
					if (second != null)
					{
						arrow = arrow + "→" + MoveShort(second, strength);
					}
				}
				if (it.On == "damageTaken")
				{
					t.Add(((e != null) ? ("あと" + Math.Max(0, it.Amount.GetValueOrDefault() - e.DamageTakenTotal.GetValueOrDefault()) + "ダメージで目覚める") : ("累計" + it.Amount.GetValueOrDefault() + "ダメージを受けると")) + arrow);
				}
				else if (it.On == "hpBelowHalf")
				{
					t.Add(((e != null) ? ("HPが" + e.MaxHp / 2 + "以下になると") : "HPが半分以下になると") + arrow);
				}
				else if (it.On == "alone")
				{
					if (e == null || HasOtherAlive(st, index))
					{
						t.Add("仲間が全滅すると" + arrow);
					}
				}
				else if (it.On == "allyDied")
				{
					t.Add("仲間が倒れると" + arrow);
				}
			}
		}
		if (d.EnrageEveryCards.HasValue)
		{
			t.Add("激昂(" + d.EnrageEveryCards.Value + "枚ごと筋力+2)");
		}
		if (d.EnrageEveryDamage.HasValue)
		{
			t.Add("激昂(累計" + d.EnrageEveryDamage.Value + "ダメごと筋力+2)");
		}
		if (d.Enrage.HasValue)
		{
			t.Add("激昂(毎フェーズ筋力+" + d.Enrage.Value + ")");
		}
		if (d.AngerOnBlock.HasValue)
		{
			t.Add("守ると怒る+" + d.AngerOnBlock.Value);
		}
		if (d.Aura != null)
		{
			t.Add("重圧(コスト+" + d.Aura.CostUp + ")");
		}
		if (t.Count != 0)
		{
			return string.Join(" / ", t.ToArray());
		}
		return "";
	}

	public static List<TraitBadge> EnemyTraitBadges(EnemyDef d)
	{
		List<TraitBadge> list = new List<TraitBadge>();
		if (d == null)
		{
			return list;
		}
		Action<string, string, string, string> add = (string icon, string label, string full, string term) =>
		{
			list.Add(new TraitBadge
			{
				Icon = icon,
				Label = label,
				Full = full,
				Term = term
			});
		};
		if (d.Guardian == true)
		{
			add(null, "庇う", "庇う (生きている間、単体の札はこの敵に向かう)", "庇う");
		}
		if (d.Thorns.HasValue)
		{
			add("counter", "とげ" + d.Thorns.Value, "とげ" + d.Thorns.Value, "とげ");
		}
		if (d.Armor.HasValue)
		{
			add("armor", "装甲" + d.Armor.Value, "装甲" + d.Armor.Value, "装甲");
		}
		if (d.TurnArmor.HasValue)
		{
			add(null, "ターン装甲" + d.TurnArmor.Value, "ターン装甲" + d.TurnArmor.Value, "ターン装甲");
		}
		if (d.SplitInto != null)
		{
			if (d.SplitInto.Count <= 1)
			{
				add(null, "残機", "残機 (倒すと次の形態で起き上がる)", "残機");
			}
			else
			{
				add(null, "分裂→" + d.SplitInto.Count + "体", "分裂 (倒すと" + d.SplitInto.Count + "体に分かれる)", "分裂");
			}
		}
		if (d.HatchInto != null)
		{
			add(null, "孵化", "孵化", "孵化");
		}
		if (d.BondStrength.HasValue)
		{
			add(null, "連携+" + d.BondStrength.Value, "連携+" + d.BondStrength.Value + " (仲間が生きている間、攻撃+" + d.BondStrength.Value + ")", "連携");
		}
		if (d.MournStrength.HasValue)
		{
			add(null, "弔い+" + d.MournStrength.Value, "弔い+" + d.MournStrength.Value + " (仲間が倒れるたび筋力+" + d.MournStrength.Value + ")", "弔い");
		}
		if (d.Regen.HasValue)
		{
			add("heart", "再生" + d.Regen.Value, "再生" + d.Regen.Value + (d.RegenBreak.HasValue ? (" (1ターンに" + d.RegenBreak.Value + "ダメージ受けると止まる)") : ""), "再生");
		}
		if (d.BurnResist.HasValue)
		{
			add(null, "延焼耐性" + d.BurnResist.Value, "延焼耐性" + d.BurnResist.Value, "延焼耐性");
		}
		if (d.Nemesis == true)
		{
			add(null, "因縁", "因縁 (奇数ターンは無形)", "因縁");
		}
		if (d.Imbalanced == true)
		{
			add(null, "バランス崩し", "バランス崩し", "バランス崩し");
		}
		if (d.Aura != null)
		{
			add(null, "重圧+" + d.Aura.CostUp, "重圧 (コスト+" + d.Aura.CostUp + ")", "重圧");
		}
		if (d.EnrageEveryCards.HasValue || d.EnrageEveryDamage.HasValue || d.Enrage.HasValue)
		{
			List<string> en = new List<string>();
			if (d.EnrageEveryCards.HasValue)
			{
				en.Add(d.EnrageEveryCards.Value + "枚ごと");
			}
			if (d.EnrageEveryDamage.HasValue)
			{
				en.Add("累計" + d.EnrageEveryDamage.Value + "ダメージごと");
			}
			string head = ((en.Count > 0) ? (string.Join("・", en.ToArray()) + "に筋力+2") : "");
			string tail = (d.Enrage.HasValue ? ("毎フェーズ筋力+" + d.Enrage.Value) : "");
			add(null, "激昂", "激昂 (" + head + ((head.Length > 0 && tail.Length > 0) ? "・" : "") + tail + ")", "激昂");
		}
		if (d.AngerOnBlock.HasValue)
		{
			add(null, "守ると怒る+" + d.AngerOnBlock.Value, "守ると怒る+" + d.AngerOnBlock.Value, "ブロック反応");
		}
		return list;
	}

	public static string[] BranchForms(GameState st, int i)
	{
		string full = BranchShort(st, i, narrow: false);
		if (full == null)
		{
			return null;
		}
		EnemyIntent raw = st.Enemies[i].Intent;
		bool set = raw.ConditionalOn == "set";
		if (set && (raw.Alt.AlsoDestroySet == true || raw.Alt.Kind == "destroy-set"))
		{
			return new string[3] { full, "からくりを先に壊す", "からくり壊し" };
		}
		if (!set)
		{
			if (!(raw.Alt.Kind == "destroy-token"))
			{
				return new string[3] { full, "人形で行動が変わる", "人形で変わる" };
			}
			return new string[2] { full, "人形狩り" };
		}
		return new string[3] { full, "からくりで行動が変わる", "からくりで変わる" };
	}

	public static string BranchShort(GameState st, int i, bool narrow)
	{
		if (st == null || i < 0 || i >= st.Enemies.Count || st.HideIntents == true)
		{
			return null;
		}
		EnemyIntent raw = st.Enemies[i].Intent;
		if (raw == null || raw.ConditionalOn == null || raw.Alt == null)
		{
			return null;
		}
		if ((object)Effects.EffectiveIntent(st, i) != raw)
		{
			return null;
		}
		bool set = raw.ConditionalOn == "set";
		if (narrow)
		{
			if (!set)
			{
				return "人形で行動が変わる";
			}
			return "からくりで行動が変わる";
		}
		if (set)
		{
			if (raw.Alt.AlsoDestroySet != true && !(raw.Alt.Kind == "destroy-set"))
			{
				return "からくりがあると 行動が変わる";
			}
			return "からくりがあると 先に壊す";
		}
		if (!(raw.Alt.Kind == "destroy-token"))
		{
			return "人形がいると 行動が変わる";
		}
		return "人形がいると 人形狩り";
	}

	public static bool IsPositiveLog(GameEvent ev)
	{
		if (!(ev is GameEvent_BlazeIgnited) && !(ev is GameEvent_Cascaded { HitId: not null }))
		{
			if (ev is GameEvent_DamageDealt { Source: "enemy", Nullified: var nullified } && nullified.HasValue)
			{
				return nullified == true;
			}
			return false;
		}
		return true;
	}

	public static string LogLine(GameEvent ev)
	{
		GameEvent_CombatStarted a = ev as GameEvent_CombatStarted;
		if (a != null)
		{
			return "戦闘開始: " + SafeEncounter(a.EnemyId);
		}
		GameEvent_TurnStarted b = ev as GameEvent_TurnStarted;
		if (b != null)
		{
			return "--- ターン " + b.Turn + " ---";
		}
		if (ev as GameEvent_TurnEnded != null)
		{
			return "ターン終了 → 敵の行動";
		}
		GameEvent_CardsDrawn d = ev as GameEvent_CardsDrawn;
		if (d != null)
		{
			return d.Count + "枚ドロー" + ((d.Cards != null && d.Cards.Count > 0) ? (": " + Names(d.Cards)) : "");
		}
		GameEvent_CardPlayed e = ev as GameEvent_CardPlayed;
		if (e != null)
		{
			return "プレイ: " + CardName(e.CardId);
		}
		GameEvent_CardSet f = ev as GameEvent_CardSet;
		if (f != null)
		{
			return "仕込んだ: " + CardName(f.CardId);
		}
		GameEvent_SetCardExpired g = ev as GameEvent_SetCardExpired;
		if (g != null)
		{
			return "期限切れ: " + CardName(g.CardId) + "（期限までに鳴らなかったので" + ((g.To == "hand") ? "手札へ" : ((g.To == "exhaust") ? "消滅置き場へ" : "捨て札へ")) + "）";
		}
		GameEvent_Scried sc = ev as GameEvent_Scried;
		if (sc != null)
		{
			return "占術: " + ((sc.Looked != null) ? sc.Looked.Count : 0) + "枚を見た" + ((sc.Discarded != null && sc.Discarded.Count > 0) ? ("（捨て札へ: " + Names(sc.Discarded) + "）") : "（全部残した）");
		}
		GameEvent_TrapLifeExtended tl = ev as GameEvent_TrapLifeExtended;
		if (tl != null)
		{
			return "からくり" + tl.Count + "枚の期限を" + tl.Amount + "ターン延ばした";
		}
		GameEvent_EnemyIntentDeclared h = ev as GameEvent_EnemyIntentDeclared;
		if (h != null)
		{
			return "敵" + (h.EnemyIndex + 1) + "の意図: " + IntentLine(h.Intent);
		}
		if (ev as GameEvent_ActionNegated != null)
		{
			return "敵の行動を打ち消した!";
		}
		GameEvent_DamageDealt j = ev as GameEvent_DamageDealt;
		if (j != null)
		{
			if (!(j.Source == "player"))
			{
				return "敵の攻撃" + j.Amount + " → HP-" + j.HpLoss + ((j.Nullified == true) ? " (身代わりの符で無効)" : ((j.Blocked.HasValue && j.Blocked.Value > 0) ? (" (ブロックで" + j.Blocked.Value + ")") : ""));
			}
			return "敵に" + j.Amount + "ダメージ (HP-" + j.HpLoss + ")" + ((j.Exposed == true) ? " [急所]" : "") + ((j.Pierced == true) ? " [貫通]" : "") + ((j.Blocked.HasValue && j.Blocked.Value > 0) ? (" [ブロックで" + j.Blocked.Value + "]") : "") + ((j.ArmorCut.HasValue && j.ArmorCut.Value > 0) ? (" [装甲で" + j.ArmorCut.Value + "切り捨て]") : "") + ((j.TurnArmorCut.HasValue && j.TurnArmorCut.Value > 0) ? (" [ターン装甲で" + j.TurnArmorCut.Value + "]") : "") + ((j.BurrowCut.HasValue && j.BurrowCut.Value > 0) ? (" [潜伏の殻で" + j.BurrowCut.Value + "]") : "") + ((j.NemesisCut.HasValue && j.NemesisCut.Value > 0) ? " [無形で1固定]" : "") + ((j.SlipperyCut.HasValue && j.SlipperyCut.Value > 0) ? " [朧で1]" : "");
		}
		GameEvent_BlockGained k = ev as GameEvent_BlockGained;
		if (k != null)
		{
			return ((k.Target == "player") ? "自分" : "敵") + "がブロック+" + k.Amount;
		}
		GameEvent_IceBlockGained l = ev as GameEvent_IceBlockGained;
		if (l != null)
		{
			return "氷壁+" + l.Amount;
		}
		GameEvent_StrengthGained m = ev as GameEvent_StrengthGained;
		if (m != null)
		{
			return "敵の筋力 +" + m.Amount + ((m.Reason != null) ? (" (" + StrengthReasonJa(m.Reason) + ")") : "");
		}
		GameEvent_BurnApplied n = ev as GameEvent_BurnApplied;
		if (n != null)
		{
			return "敵に延焼+" + n.Amount;
		}
		GameEvent_BurnTick o = ev as GameEvent_BurnTick;
		if (o != null)
		{
			return "延焼で敵に" + o.Amount + "ダメージ";
		}
		GameEvent_GrowthAdded p = ev as GameEvent_GrowthAdded;
		if (p != null)
		{
			return "成長+" + p.Amount;
		}
		GameEvent_MomentumAdded q = ev as GameEvent_MomentumAdded;
		if (q != null)
		{
			return "勢い+" + q.Amount;
		}
		GameEvent_HpHealed r = ev as GameEvent_HpHealed;
		if (r != null)
		{
			return "HP+" + r.Amount + "回復";
		}
		GameEvent_HpLost s = ev as GameEvent_HpLost;
		if (s != null)
		{
			return "自傷でHP-" + s.Amount;
		}
		GameEvent_StatusInflicted t2 = ev as GameEvent_StatusInflicted;
		if (t2 != null)
		{
			if (!StatusJa.TryGetValue(t2.Status, out var ja))
			{
				ja = t2.Status;
			}
			return ja + t2.Amount + "を付与された";
		}
		GameEvent_ReactionTriggered u = ev as GameEvent_ReactionTriggered;
		if (u != null)
		{
			return "発動: " + CardName(u.CardId);
		}
		GameEvent_ReactionHeld v = ev as GameEvent_ReactionHeld;
		if (v != null)
		{
			return "温存: " + Names(v.CandidateIds) + " (敵" + (v.EnemyIndex + 1) + "の" + KindJa(v.Kind) + " " + v.Stage + "窓 / 実値" + v.Value + ")";
		}
		GameEvent_ReactionWhiffed w = ev as GameEvent_ReactionWhiffed;
		if (w != null)
		{
			return "空振り: " + CardName(w.CardId);
		}
		GameEvent_ReactionUnaffordable x = ev as GameEvent_ReactionUnaffordable;
		if (x != null)
		{
			return "仕込み札「" + CardName(x.CardId) + "」を発動するには" + x.Cost + "E必要 (残り" + x.Energy + "E) = 温存";
		}
		GameEvent_SetCardDestroyed y = ev as GameEvent_SetCardDestroyed;
		if (y != null)
		{
			return "からくりを壊された: " + CardName(y.CardId);
		}
		GameEvent_PermanentPlayed z = ev as GameEvent_PermanentPlayed;
		if (z != null)
		{
			return "置物を設置: " + CardName(z.CardId);
		}
		GameEvent_CardExhausted a2 = ev as GameEvent_CardExhausted;
		if (a2 != null)
		{
			return "消滅: " + CardName(a2.CardId);
		}
		GameEvent_CardSealed sl = ev as GameEvent_CardSealed;
		if (sl != null)
		{
			return "敵" + (sl.EnemyIndex + 1) + "が「" + CardName(sl.CardId) + "」を封じた (倒せば手札に戻る)";
		}
		GameEvent_CardUnsealed us = ev as GameEvent_CardUnsealed;
		if (us != null)
		{
			return "封じられていた「" + CardName(us.CardId) + "」が手札に戻った";
		}
		GameEvent_CardsMilled b2 = ev as GameEvent_CardsMilled;
		if (b2 != null)
		{
			return "山札の上" + b2.Count + "枚を忘却" + ((b2.CardIds != null) ? (": " + Names(b2.CardIds)) : "");
		}
		GameEvent_ExposedApplied c2 = ev as GameEvent_ExposedApplied;
		if (c2 != null)
		{
			return "敵に急所+" + c2.Amount;
		}
		GameEvent_EnemyWeakened d2 = ev as GameEvent_EnemyWeakened;
		if (d2 != null)
		{
			return "敵を威圧" + d2.Amount;
		}
		GameEvent_BlockShattered e2 = ev as GameEvent_BlockShattered;
		if (e2 != null)
		{
			return "敵のブロック" + e2.Amount + "を粉砕!";
		}
		GameEvent_ThornsReflected f2 = ev as GameEvent_ThornsReflected;
		if (f2 != null)
		{
			return "とげ反射: " + f2.Amount + " (HP-" + f2.HpLoss + ")";
		}
		GameEvent_GoldStolen g2 = ev as GameEvent_GoldStolen;
		if (g2 != null)
		{
			return "盗まれた: " + g2.Amount + "G (逃がす前に倒せば取り返せる)";
		}
		if (ev as GameEvent_EnemyFled != null)
		{
			return "敵が逃走した";
		}
		GameEvent_EnemyHealed i3 = ev as GameEvent_EnemyHealed;
		if (i3 != null)
		{
			return "敵が回復 +" + i3.Amount;
		}
		GameEvent_RegenTicked j2 = ev as GameEvent_RegenTicked;
		if (j2 != null)
		{
			return "敵は再生でHP+" + j2.Amount;
		}
		if (ev as GameEvent_RegenBroken != null)
		{
			return "再生が止まった";
		}
		GameEvent_EnergyGained l2 = ev as GameEvent_EnergyGained;
		if (l2 != null)
		{
			return "エナジー+" + l2.Amount + " (このターン)";
		}
		GameEvent_EnergyMaxGained m2 = ev as GameEvent_EnergyMaxGained;
		if (m2 != null)
		{
			return "エナジー上限+" + m2.Amount;
		}
		GameEvent_DiscountGained n2 = ev as GameEvent_DiscountGained;
		if (n2 != null)
		{
			return "次のカードのコスト-" + n2.Amount;
		}
		if (ev is GameEvent_Cascaded chain)
		{
			return "チェイン: " + CardName(chain.CardId) + " → " + ((chain.HitId != null) ? (CardName(chain.HitId) + "をコストを払わずにプレイ") : "当たりなし") + ((chain.Revealed.Count > 0) ? ("（" + chain.Revealed.Count + "枚は山札の底へ）") : "");
		}
		if (ev is GameEvent_ImpulseReturned returned)
		{
			return "衝動の失効: " + CardName(returned.CardId) + "は払えなかったので捨て札へ";
		}
		if (ev is GameEvent_ImpulseKept kept)
		{
			return "燠の守り: " + CardName(kept.CardId) + "は失効せず手札に残った";
		}
		if (ev is GameEvent_ImpulseKindled kindled)
		{
			return CardName(kindled.CardId) + "の追加コストとして" + Names(kindled.CardIds) + "を火にくべた（消滅）";
		}
		if (ev is GameEvent_BlazeIgnited)
		{
			return "猛り火に点いた（延焼の合計が8以上）";
		}
		if (ev is GameEvent_BlazeExtinguished)
		{
			return "猛り火が消えた（延焼の合計が8未満）";
		}
		if (ev is GameEvent_CascadeRetargeted redirected)
		{
			return "チェインの当たりが対象を倒したので、" + CardName(redirected.CardId) + "の本体は敵" + (redirected.ToIndex + 1) + "へ";
		}
		GameEvent_ImpulseDrawn o2 = ev as GameEvent_ImpulseDrawn;
		if (o2 != null)
		{
			return "衝動" + o2.Count + "枚 (このターン限り)";
		}
		GameEvent_CardsDiscarded p2 = ev as GameEvent_CardsDiscarded;
		if (p2 != null)
		{
			return "コストとして捨てた: " + Names(p2.CardIds);
		}
		GameEvent_GrowthDischarged q2 = ev as GameEvent_GrowthDischarged;
		if (q2 != null)
		{
			return "成長" + q2.Spent + "を全て放出!";
		}
		GameEvent_MomentumDischarged r2 = ev as GameEvent_MomentumDischarged;
		if (r2 != null)
		{
			return "勢い" + r2.Spent + "を全て放出!";
		}
		GameEvent_AetherGained s2 = ev as GameEvent_AetherGained;
		if (s2 != null)
		{
			return "霊気+" + s2.Amount;
		}
		GameEvent_AetherDischarged t3 = ev as GameEvent_AetherDischarged;
		if (t3 != null)
		{
			return "霊気" + t3.Spent + "を全て放出!";
		}
		GameEvent_LightGained lg = ev as GameEvent_LightGained;
		if (lg != null)
		{
			return "灯+" + lg.Amount + " (" + ((lg.Source == "heal") ? "回復" : ((lg.Source == "retainer") ? "人形" : ((lg.Source == "passive") ? "灯匠" : ((lg.Source == "carry") ? "残り火" : "カード")))) + ")";
		}
		GameEvent_LightDischarged ld = ev as GameEvent_LightDischarged;
		if (ld != null)
		{
			if (!IsLightPayment(ld))
			{
				return "灯" + ld.Spent + "を放出!";
			}
			return LightPayLine(ld);
		}
		GameEvent_LightSpent ls = ev as GameEvent_LightSpent;
		if (ls != null)
		{
			return "灯-" + ls.Amount + " (" + CardName(ls.CardId) + ")";
		}
		GameEvent_CardsAddedToDraw cad = ev as GameEvent_CardsAddedToDraw;
		if (cad != null)
		{
			return CardName(cad.CardId) + cad.Count + "枚を山札に混ぜた";
		}
		GameEvent_CardsAddedToDiscard cadd = ev as GameEvent_CardsAddedToDiscard;
		if (cadd != null)
		{
			return CardName(cadd.CardId) + cadd.Count + "枚を捨て札に加えた";
		}
		GameEvent_DeckCardTransformed dct = ev as GameEvent_DeckCardTransformed;
		if (dct != null)
		{
			return "山札の" + CardName(dct.CardId) + "が" + CardName(dct.Into) + "に変わった";
		}
		GameEvent_EnemySplit u2 = ev as GameEvent_EnemySplit;
		if (u2 != null)
		{
			if (u2.Count != 1)
			{
				return "分裂! 倒した敵から" + u2.Count + "体が現れた";
			}
			return "再起動! 倒した敵が次の姿で立ち上がった";
		}
		if (ev as GameEvent_EnemyHatched != null)
		{
			return "孵化した!";
		}
		if (ev as GameEvent_GuardianRedirected != null)
		{
			return "庇われた! 単体対象は護衛に向かった";
		}
		if (ev as GameEvent_BurrowBroken != null)
		{
			return "潜伏の殻が割れた! 次の行動は噛みつき";
		}
		if (ev as GameEvent_EnemyStaggered != null)
		{
			return "完全に防いだ! 敵は体勢を崩し、次の行動は隙";
		}
		GameEvent_EnemySummoned zs = ev as GameEvent_EnemySummoned;
		if (zs != null)
		{
			if (zs.Count <= 0)
			{
				return "召喚したが場が満杯で出なかった";
			}
			return "召喚! " + zs.Count + "体が現れた";
		}
		GameEvent_EnemyInterrupted z2 = ev as GameEvent_EnemyInterrupted;
		if (z2 != null)
		{
			string text = ((z2.Trigger == "damageTaken") ? "目を覚ました! 眠りが終わった" : ((z2.Trigger == "hpBelowHalf") ? "HPが半分を切った!" : ((z2.Trigger == "alone") ? "仲間が全滅した!" : "仲間が倒れた!")));
			return text + (z2.Replaced ? (" 行動が変わった" + ((z2.Before != null && z2.After != null) ? (": " + IntentLine(z2.Before) + " → " + IntentLine(z2.After)) : "")) : " 次のターンから行動が変わる");
		}
		GameEvent_ArtifactBlocked a3 = ev as GameEvent_ArtifactBlocked;
		if (a3 != null)
		{
			return "アーティファクトが" + DebuffName(a3.Effect) + "を弾いた（この効果は消えた）";
		}
		GameEvent_ScaldTick b3 = ev as GameEvent_ScaldTick;
		if (b3 != null)
		{
			List<string> parts = new List<string>();
			if (b3.Scalds.GetValueOrDefault() > 0)
			{
				parts.Add("火傷" + b3.Scalds + "枚");
			}
			if (b3.Brands.GetValueOrDefault() > 0)
			{
				parts.Add("烙印" + b3.Brands + "枚");
			}
			return ((parts.Count > 0) ? string.Join("・", parts) : ("火傷・烙印" + b3.Count + "枚")) + "が疼いた (HP-" + b3.Amount + ")";
		}
		GameEvent_CombatEnded c3 = ev as GameEvent_CombatEnded;
		if (c3 != null)
		{
			if (!(c3.Result == "won"))
			{
				return "=== 敗北 ===";
			}
			return "=== 勝利 ===";
		}
		GameEvent_GearUsed d3 = ev as GameEvent_GearUsed;
		if (d3 != null)
		{
			return "ギア「" + d3.Name + "」を組んだ";
		}
		GameEvent_DeathSaved e3 = ev as GameEvent_DeathSaved;
		if (e3 != null)
		{
			if (!(e3.Source == "gear"))
			{
				return "蜥蜴の尾が砕け、HP" + e3.Hp + "で踏みとどまった";
			}
			return "蘇りの発条がはじけ、HP" + e3.Hp + "で踏みとどまった";
		}
		GameEvent_RetainerRushed g3 = ev as GameEvent_RetainerRushed;
		if (g3 != null)
		{
			return "点灯: " + CardName(g3.CardId) + "が出た瞬間に1回動いた";
		}
		GameEvent_RetainerExpired gx = ev as GameEvent_RetainerExpired;
		if (gx != null)
		{
			return "期限切れ: " + CardName(gx.CardId) + "が消えた";
		}
		GameEvent_PermanentDismissed gd = ev as GameEvent_PermanentDismissed;
		if (gd != null)
		{
			return "灯が足りず " + CardName(gd.CardId) + " が場を離れた（捨て札へ）";
		}
		GameEvent_RetainerCopied gc = ev as GameEvent_RetainerCopied;
		if (gc != null)
		{
			return "写し: " + CardName(gc.CardId) + "をコピーした（残りの期限も写す）";
		}
		GameEvent_RetainerLifeExtended gl = ev as GameEvent_RetainerLifeExtended;
		if (gl != null)
		{
			if (gl.Persist != true)
			{
				return "継ぎ火: " + CardName(gl.CardId) + "の期限を" + gl.Amount + "ターン延ばした";
			}
			return "永遠の灯: " + CardName(gl.CardId) + "の期限が無くなった（消えなくなった）";
		}
		GameEvent_RetainersTriggered h3 = ev as GameEvent_RetainersTriggered;
		if (h3 != null)
		{
			return "号令: 人形" + h3.Count + "体がトリガーを問わず今1回ずつ動いた";
		}
		GameEvent_RetainersDuplicated i4 = ev as GameEvent_RetainersDuplicated;
		if (i4 != null)
		{
			return "分列: 人形" + i4.Count + "体が複製された";
		}
		GameEvent_TokenDestroyed td = ev as GameEvent_TokenDestroyed;
		if (td != null)
		{
			return "人形狩り: " + CardName(td.CardId) + "が壊された";
		}
		GameEvent_RetainerSacrificed rs = ev as GameEvent_RetainerSacrificed;
		if (rs != null)
		{
			return "人形を捧げた: " + CardName(rs.CardId);
		}
		GameEvent_SpellEchoed se = ev as GameEvent_SpellEchoed;
		if (se != null)
		{
			return "反復: " + CardName(se.CardId) + "の効果が2回解決";
		}
		GameEvent_HpLossCapped f3 = ev as GameEvent_HpLossCapped;
		if (f3 != null)
		{
			if (f3.Left <= 0)
			{
				return "脈打つ欠片がHPの損失を20で止めた（これで最後。戦いの後に砕ける）";
			}
			return "脈打つ欠片がHPの損失を20で止めた（あと" + f3.Left + "回）";
		}
		GameEvent_EnemyDied ed = ev as GameEvent_EnemyDied;
		if (ed != null)
		{
			return "敵" + (ed.EnemyIndex + 1) + "を倒した";
		}
		if (ev is GameEvent_DeckShuffled)
		{
			return "山札を切り直した";
		}
		GameEvent_CardsAddedToHand cah = ev as GameEvent_CardsAddedToHand;
		if (cah != null)
		{
			return CardName(cah.CardId) + "を" + cah.Count + "枚手札に加えた";
		}
		GameEvent_CardsMovedToHand cmh = ev as GameEvent_CardsMovedToHand;
		if (cmh != null)
		{
			return ((cmh.From == "draw") ? "サーチ" : "回収") + ": " + Names(cmh.CardIds) + "を手札に加えた";
		}
		GameEvent_CardCopied ccp = ev as GameEvent_CardCopied;
		if (ccp != null)
		{
			return CardName(ccp.CardId) + "のコピー" + ccp.Count + "枚を捨て札に加えた";
		}
		GameEvent_CardGrew cgr = ev as GameEvent_CardGrew;
		if (cgr != null)
		{
			return CardName(cgr.CardId) + "が育った（与ダメ+" + cgr.Bonus + "）";
		}
		GameEvent_CardUpgradedInHand cuh = ev as GameEvent_CardUpgradedInHand;
		if (cuh != null)
		{
			return CardName(cuh.CardId) + "を鍛えた（この戦闘中）";
		}
		GameEvent_CardRetrieved crt = ev as GameEvent_CardRetrieved;
		if (crt != null)
		{
			return "回収: " + CardName(crt.CardId) + "（消滅置き場から手札へ）";
		}
		GameEvent_CardPlayedFromExhaust cpe = ev as GameEvent_CardPlayedFromExhaust;
		if (cpe != null)
		{
			return "直接プレイ: " + CardName(cpe.CardId) + "（消滅置き場から）";
		}
		GameEvent_NecroFired nf = ev as GameEvent_NecroFired;
		if (nf != null)
		{
			return "亡骸: " + CardName(nf.CardId) + "が消滅して効果が発火";
		}
		GameEvent_NecroPlayed np = ev as GameEvent_NecroPlayed;
		if (np != null)
		{
			return "亡骸プレイ: " + CardName(np.CardId) + "（ゲームから取り除かれた）";
		}
		GameEvent_ExhaustRecycled exr = ev as GameEvent_ExhaustRecycled;
		if (exr != null)
		{
			return "輪廻: 消滅置き場" + exr.Count + "枚が山札へ還った";
		}
		GameEvent_BurnDischarged bdc = ev as GameEvent_BurnDischarged;
		if (bdc != null)
		{
			return "爆熱: 延焼" + bdc.Amount + "を全て解き放った";
		}
		GameEvent_EnemyConfused ecf = ev as GameEvent_EnemyConfused;
		if (ecf != null)
		{
			return "敵に混乱+" + ecf.Amount + "（攻撃が仲間に向かう）";
		}
		GameEvent_ConfusedAttack cfa = ev as GameEvent_ConfusedAttack;
		if (cfa != null)
		{
			if (cfa.EnemyIndex != cfa.TargetIndex)
			{
				return "仲間割れ! 混乱した敵が味方に" + cfa.Amount + "ダメージ";
			}
			return "混乱した敵は自分自身に" + cfa.Amount + "ダメージ!";
		}
		GameEvent_MaxHpGained mhg = ev as GameEvent_MaxHpGained;
		if (mhg != null)
		{
			return "最大HP+" + mhg.Amount + "（この戦闘後も残る）";
		}
		GameEvent_SetSlotGained ssg = ev as GameEvent_SetSlotGained;
		if (ssg != null)
		{
			return "仕込み枠+" + ssg.Amount + "（この戦闘中）";
		}
		GameEvent_PlayerArtifactBlocked pab = ev as GameEvent_PlayerArtifactBlocked;
		if (pab != null)
		{
			return "時計仕掛けの土産が" + StatusName(pab.Status) + "を弾いた";
		}
		if (ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyActionResolved || ev is GameEvent_EnemyPhaseEnded)
		{
			return null;
		}
		return ev.Type;
	}

	public static bool IsLightPayment(GameEvent_LightDischarged ld)
	{
		if (ld != null)
		{
			if (ld.Sparks.GetValueOrDefault() <= 0)
			{
				return ld.Paid == true;
			}
			return true;
		}
		return false;
	}

	public static string LightPayLine(GameEvent_LightDischarged ld)
	{
		if (ld.Sparks.GetValueOrDefault() <= 0)
		{
			return "灯" + ld.Spent + "を払った";
		}
		return "灯" + ld.Spent + "を払って火種" + ld.Sparks.Value + "を山札へ";
	}

	private static string SafeEncounter(string id)
	{
		try
		{
			return Content.EncounterName(id);
		}
		catch (Exception)
		{
			return id;
		}
	}

	private static string Names(IReadOnlyList<string> ids)
	{
		if (ids == null || ids.Count == 0)
		{
			return "";
		}
		List<string> buf = new List<string>();
		for (int i = 0; i < ids.Count; i++)
		{
			buf.Add(CardName(ids[i]));
		}
		return string.Join("・", buf.ToArray());
	}

	private static string StrengthReasonJa(string reason)
	{
		return reason switch
		{
			"enrage-cards" => "激昂〔プレイ枚数の節目〕", 
			"enrage-damage" => "激昂〔累計被ダメの節目〕", 
			"enrage-phase" => "激昂〔毎フェーズ〕", 
			"mourn" => "弔い〔仲間が倒れた〕", 
			_ => reason, 
		};
	}
}
