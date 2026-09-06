// Reactions/SetBase.cs — src/engine/reactions/set-base.ts の手書き移植 (unity/PORTING.md)
// set-auto / set-confirm が共有する「伏せる」処理 (hold-manual は伏せないので使わない)
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class SetBase
    {
        /// <summary>伏せる時のコスト: 専用リアクションは印字 (屍集めの0E札は0)、通常カードは実験の固定手数料</summary>
        private static int SetCostOf(CardInstance card)
        {
            if (card.Def.Type != CardTypes.Reaction) return SetAny.SET_ANY_FEE;
            return card.FreeThisCombat == true ? 0 : card.Def.Cost;
        }

        /// <summary>この札を伏せ対象にできるか (型の判定。エナジー・枠は別)</summary>
        private static bool SettableType(GameState state, CardInstance card)
        {
            return card.Def.Type == CardTypes.Reaction || (state.SetAnyCards == true && SetAny.CanSetAsNormal(card.Def));
        }

        /// <summary>SetCard の可否判定 (UI のボタン活性にも使う)</summary>
        public static bool CanSetCard(GameState state, string cardUid)
        {
            if (state.Phase != CombatPhases.PlayerTurn) return false;
            // 伏せ枠は setSlots まで (基本1。かすみ=2)
            if (state.Player.SetCards.Count >= state.Player.SetSlots) return false;
            var card = state.Player.Hand.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) return false;
            if (!SettableType(state, card)) return false;
            return SetCostOf(card) <= state.Player.Energy;
        }

        /// <summary>SetCard: コスト事前払いで手札から伏せる</summary>
        public static GameState SetCard(GameState state, string cardUid)
        {
            if (state.Phase != CombatPhases.PlayerTurn) throw new InvalidOperationException("自ターン以外は伏せられない");
            if (state.Player.SetCards.Count >= state.Player.SetSlots)
            {
                throw new InvalidOperationException($"伏せは同時{state.Player.SetSlots}枚まで");
            }
            var card = state.Player.Hand.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) throw new InvalidOperationException($"手札にないカード: {cardUid}");
            if (!SettableType(state, card))
            {
                throw new InvalidOperationException(
                    state.SetAnyCards == true
                        ? $"{card.Def.Name} は伏せられない (X・モード・追加コスト・ドロー/マナ系は対象外)"
                        : $"{card.Def.Name} は伏せられない (リアクションタイプのみ)");
            }
            // 回収ターンの伏せ直しは0E。屍集めで戻した札 (freeThisCombat) も0E。通常カード (実験) は固定1E
            int setCost = SetCostOf(card);
            bool freeReset = state.Player.FreeResetUid == card.Uid;
            if (!freeReset && setCost > state.Player.Energy) throw new InvalidOperationException($"エナジー不足: {card.Def.Name}");
            var setCards = new List<CardInstance>(state.Player.SetCards)
            {
                // 見切りの拡張: 一度伏せた札の伏せ直しは「新鮮」にならない = 敵は反応しない。
                // 0Eで伏せた札 (毒針の囮・回収ターンの伏せ直し・屍集めの0E札) も「気配」にならない
                card with { SetFresh = card.WasSet != true && !freeReset && setCost >= 1, WasSet = true },
            };
            var s = state with
            {
                Player = state.Player with
                {
                    Energy = state.Player.Energy - (freeReset ? 0 : setCost),
                    FreeResetUid = freeReset ? null : state.Player.FreeResetUid,
                    Hand = state.Player.Hand.Where(c => c.Uid != cardUid).ToList(),
                    SetCards = setCards,
                    SetsThisTurn = (state.Player.SetsThisTurn ?? 0) + 1,
                },
            };
            // 蜃気楼の面: 伏せた瞬間からこのターンの意図の実値を公開する
            var revealed = s;
            if (s.RevealOnSet == true)
            {
                var enemies = new List<EnemyState>(s.Enemies.Count);
                for (int i = 0; i < s.Enemies.Count; i++)
                {
                    var e = s.Enemies[i];
                    if (e.Intent == null) { enemies.Add(e); continue; }
                    var intent = e.Intent with { ShownMin = e.Intent.Actual, ShownMax = e.Intent.Actual };
                    if (e.Intent.Alt != null)
                    {
                        intent = intent with { Alt = e.Intent.Alt with { ShownMin = e.Intent.Alt.Actual, ShownMax = e.Intent.Alt.Actual } };
                    }
                    enemies.Add(e with { Intent = intent });
                }
                revealed = s with { Enemies = enemies };
            }
            // 伏せに反応する置物 (レリック: 符師の懐=伏せるたび1ドロー)
            int alive = -1;
            for (int i = 0; i < s.Enemies.Count; i++) if (s.Enemies[i].Hp > 0) { alive = i; break; }
            return Effects.RunPermanentTriggers(
                Events.Emit(revealed, new GameEvent_CardSet { CardId = card.Def.Id }),
                "onCardSet",
                Math.Max(0, alive));
        }

        /// <summary>
        /// 回収: 1E払って伏せ札を手札に戻す。払った伏せコストは返らない。
        /// 読み違いの代償を「枠の固定死」から「1E払って賭け直し」に変える
        /// </summary>
        public static GameState RetrieveSetCard(GameState state, string cardUid)
        {
            if (state.Phase != CombatPhases.PlayerTurn) throw new InvalidOperationException("回収は自ターンのみ");
            var card = state.Player.SetCards.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) throw new InvalidOperationException($"伏せ場にないカード: {cardUid}");
            int cost = state.RetrieveFree == true ? 0 : 1; // 回収の紐: 回収が0E
            if (state.Player.Energy < cost) throw new InvalidOperationException("エナジー不足: 回収には1E必要");
            var restored = card with { SetFresh = null };
            var hand = new List<CardInstance>(state.Player.Hand) { restored };
            return Events.Emit(
                state with
                {
                    Player = state.Player with
                    {
                        Energy = state.Player.Energy - cost,
                        SetCards = state.Player.SetCards.Where(c => c.Uid != cardUid).ToList(),
                        Hand = hand,
                        FreeResetUid = cardUid, // このターン中の伏せ直しは0E
                    },
                },
                new GameEvent_SetCardRetrieved { CardId = card.Def.Id });
        }

        /// <summary>
        /// 伏せカードを発動する: 効果解決→伏せ場から捨て札 (消滅札なら消滅置き場) へ。
        /// コストは伏せ時に支払い済み。敵の1行動につき1回まで、の消費フラグを立てる
        /// </summary>
        public static GameState FireSetCard(GameState state, CardInstance card, int enemyIndex)
        {
            bool exhausts = card.Def.Exhaust == true;
            // 全カード伏せ可 (実験): 通常カードは発動時に印字コストを持ち越しエナジーから払う
            int fireCost = SetAny.SetFireCost(card);
            if (fireCost > state.Player.Energy) return state;
            var discardPile = new List<CardInstance>(state.Player.DiscardPile);
            var exhaustPile = new List<CardInstance>(state.Player.ExhaustPile);
            if (exhausts) exhaustPile.Add(card); else discardPile.Add(card);
            var s = state with
            {
                ReactionUsedThisAction = true,
                Player = state.Player with
                {
                    Energy = state.Player.Energy - fireCost,
                    SetCards = state.Player.SetCards.Where(c => c.Uid != card.Uid).ToList(),
                    DiscardPile = discardPile,
                    ExhaustPile = exhaustPile,
                },
            };
            s = Effects.ResolveReactionEffects(s, card, enemyIndex);
            if (exhausts)
            {
                s = Events.Emit(s, new GameEvent_CardExhausted { CardId = card.Def.Id });
                s = Effects.FireExhaustTriggers(s, 1, enemyIndex);
            }
            return s;
        }

        /// <summary>空振り計上: 敵フェーズ終端に伏せが残っていれば、そのターンは発動しなかった</summary>
        public static GameState EmitWhiffForRemainingSet(GameState state)
        {
            var s = state;
            foreach (var card in state.Player.SetCards)
            {
                s = Events.Emit(s, new GameEvent_ReactionWhiffed { CardId = card.Def.Id });
            }
            return s;
        }
    }
}
