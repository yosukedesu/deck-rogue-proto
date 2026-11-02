// Outcome hierarchy follows verified Unity 7a84644 EndScreen; responsive native
// flows intentionally retain complete statistics and actions at large text sizes.
using System;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private void BuildEnd(RunState run)
    {
        bool won = run.Phase == "won";
        bool arrival = BeginRewardArrival(run.Phase);
        var title = RewardLabel(_body, won ? "走破！" : "敗北", PhoneLayout ? 36 : 56, won ? Gold : Danger, true);
        title.Name = "SourceRunOutcome";
        RewardArrivalAccent(title, arrival, title: true);
        string detail = Content.GetLeaderDef(run.LeaderId).Name + $"  /  シード {run.Seed}  /  難易度 {run.Difficulty}\n" +
            $"幕{run.Act}・行{run.Row + 1}  ·  勝利 {run.BattlesWon} 戦  ·  HP {run.Hp}/{run.MaxHp}  ·  {run.Gold} G\n" +
            $"最終デッキ {run.Deck.Count}枚  ·  レリック {run.Relics.Count}個  ·  魔素 {Gears.ManaLabel(run.Mana.GetValueOrDefault())}";
        var summary = RewardLabel(_body,
            won ? $"{Content.GetLeaderDef(run.LeaderId).Name}と踏破  ·  勝利 {run.BattlesWon}戦  ·  難易度 {run.Difficulty}"
                : $"幕{run.Act}・行{run.Row + 1}まで到達  ·  勝利 {run.BattlesWon}戦  ·  難易度 {run.Difficulty}", 18, Ink);
        summary.Name = "SourceRunOutcomeSummary";
        var record = RewardActions(_body);
        RewardButton(record, "結果の詳細", () => Open(won ? "走破の記録" : "敗北の記録", box => {
            Text(ScrollColumn(box), detail + "\n\n最終デッキの札を選ぶと、説明を確認できます。", 21);
        })).Name = "SourceRunOutcomeDetails";
        if (run.Relics.Count > 0)
        {
            // A bounded, readable collection opens the same existing relic viewer.
            // The full inventory remains accessible even when a long run has many.
            var relics = record;
            relics.Name = "SourceRunOutcomeRelics";
            int visible = Math.Min(run.Relics.Count, PhoneLayout ? 0 : 5);
            for (int i = 0; i < visible; i++)
            {
                var relic = Content.GetRelicDef(run.Relics[i]);
                var b = RewardButton(relics, relic.Name, () => OpenRelics(run));
                b.CustomMinimumSize = new Vector2(0, MinimumHit);
                b.TooltipText = relic.Description;
            }
            if (run.Relics.Count > visible)
                RewardButton(relics, PhoneLayout ? $"レリック {run.Relics.Count}個" : $"ほか {run.Relics.Count - visible} 個", () => OpenRelics(run));
        }
        CardGrid(_body, run.Deck, InspectCard, null);
        var actions = RewardActions(_body);
        actions.Name = "SourceRunOutcomeActions";
        RewardButton(actions, "タイトルへ", ShowTitle, true, true).Name = "SourceRunOutcomeReturn";
        RewardButton(actions, "ランの記録", OpenJournal);
        RewardButton(actions, "メニュー", OpenMenu);
        RewardButton(actions, "レポートを書き出す", OpenExport);
        if ((_session?.History.Count ?? 0) > 0) RewardButton(actions, "直前の戦闘を評価", OpenRating);
    }
}
