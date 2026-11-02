// PresenterHd2dSupport.cs — 支え (守り・氷壁・回復) の HD-2D 演出への配線 (dot の Godot 版 Project422 の SupportVfxProfiles.Describe・2026-11-02)。
// Godot では BattleStage が 1 コマンドの出来事の束 (SessionChange) から「主たるプレイの札」と「その札で得た最初の 1 回の守り/氷壁/回復」を選び、Hd2dSupportFx で受けた側 (自分) の上に描いた。
// Unity では Presenter が BlockGained / IceBlockGained / HpHealed を見せる所で、記録の中の同じコマンド (直前の CardPlayed から) を見て同じ判定をする。claimed はコマンドごとに 1 回。
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game.Hd2d;

namespace DeckRogue.Game
{
    public static partial class Presenter
    {
        static class Hd2dSupport
        {
            static IReadOnlyList<GameEvent> _claimedLog; static readonly HashSet<int> _claimed = new HashSet<int>();
            /// <summary>この出来事が主たるプレイの札の支えなら Hd2dSupportFx で描く。描いたら true (旧い盾の絵は出さない)</summary>
            internal static bool TryPlay(GameRoot g, ReactionCtx ctx, GameEvent ev)
            {
                if (!HD2DFlags.Hd2dFx || ev == null) return false;
                var log = ctx?.CapturedState?.EventLog ?? g?.Rs?.Combat?.EventLog;
                if (log == null) return false;
                int i = -1; for (int k = log.Count - 1; k >= 0; k--) if (ReferenceEquals(log[k], ev)) { i = k; break; }
                if (i < 0) return false;
                int start = i; while (start > 0 && !Hd2dOwner.IsBoundary(log[start])) start--;
                if (log[start] is not GameEvent_CardPlayed played) return false;   // 主たるプレイ (Godot の RunCommand_Combat{Command_PlayCard}) だけ。仕込み札・置物・敵の番の支えは選ばない
                int end = i + 1; while (end < log.Count && !Hd2dOwner.IsBoundary(log[end])) end++;
                // 近似: Godot は Command_PlayCard の ModeIndex を見る (絡み蔦のブロックの面 = 0)。Unity は同じコマンドに自分の打撃が無ければ守りの面とする
                int? modeIndex = null;
                if (played.CardId == "green_entangle") { bool hit = false; for (int k = start; k < end; k++) if (log[k] is GameEvent_DamageDealt d && d.Source == "player") hit = true; modeIndex = hit ? 1 : 0; }
                if (!ReferenceEquals(_claimedLog, log)) { _claimedLog = log; _claimed.Clear(); }
                bool claimed = _claimed.Contains(start);
                var route = Hd2dOwnership.DescribeSupport(ev, played.CardId, modeIndex, ref claimed, out float duration);
                if (claimed) _claimed.Add(start);
                if (route == null) return false;
                return Hd2dAttackFx.BeginSupport(route, duration, ev);
            }
        }
    }
}
