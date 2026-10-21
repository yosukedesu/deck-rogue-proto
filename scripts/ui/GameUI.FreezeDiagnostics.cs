using System;
using DeckRogue.GodotPort.Debugging;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private FreezeDiagnosticLog? _freezeDiagnostics;
    private string? _freezeSelectionNeed;

    public void SetFreezeDiagnostics(FreezeDiagnosticLog? diagnostics) { _freezeDiagnostics = diagnostics; }

    public FreezeDiagnosticState CaptureFreezeDiagnosticState()
    {
        var play = _play;
        if (play == null) return new FreezeDiagnosticState(Target: -1, Selection: "none");
        int flags = 1 | (play.ModeIndex.HasValue ? 2 : 0) | (play.Kindle.HasValue ? 4 : 0)
            | (play.RetrieveUid != null ? 8 : 0) | (play.PermanentUid != null ? 16 : 0)
            | (play.XAmount.HasValue ? 32 : 0);
        return new FreezeDiagnosticState(CardId: play.Card.Def.Id, Target: play.TargetIndex ?? -1,
            Selection: _freezeSelectionNeed ?? "ready", SelectionFlags: flags,
            SelectionCount: play.Discard.Count + play.Exhaust.Count + play.Deck.Count + play.Hand.Count);
    }

    private void RecordFreezeSelection(string method) { _session?.RecordFreezeEvent(method); }

    private void AddFreezeDiagnosticsMenuButton(Control box)
    {
        Text(box, "停止時の診断ログ", 23, Gold);
        Text(box, "直近の操作状態を端末内に自動記録します。セーブ本文やメモは含まず、外部への自動送信はありません。", 17, Muted);
        Button(box, "今回・前回の起動ログを確認", OpenFreezeDiagnostics);
    }

    private void OpenFreezeDiagnostics()
    {
        RecordFreezeSelection("GameUI.OpenFreezeDiagnostics");
        Open("停止時の診断ログ", outer =>
        {
            // Keep the modal heading/close control fixed; both log sections must remain reachable on short screens.
            var box = ScrollColumn(outer);
            Text(box, "各ログは最大128件・64 KiB。前回起動の記録は次の起動で入れ替わります。", 17, Muted);
            if (_freezeDiagnostics == null)
            {
                Text(box, "この起動では診断ログを利用できません。ゲームは通常どおり続けられます。", 18, Muted);
                return;
            }
            if (_freezeDiagnostics.StorageFailed)
                Text(box, "端末への保存・読み込みができない記録があります。今回のメモリ内の記録は確認できます。", 18, Danger);
            if (!_freezeDiagnostics.Initialized) Text(box, "前回の記録を確認中です。いったん閉じて開き直してください。", 17, Muted);
            string current = _freezeDiagnostics.CurrentText, previous = _freezeDiagnostics.PreviousText;
            ShowLog("今回の起動", current);
            ShowLog("前回の起動", previous);
            void ShowLog(string title, string value)
            {
                Text(box, title, 21, Gold);
                if (string.IsNullOrEmpty(value)) { Text(box, "記録はありません。", 17, Muted); return; }
                var viewer = new TextEdit { Text = value, Editable = false, CustomMinimumSize = new Vector2(0, 160),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill, WrapMode = TextEdit.LineWrappingMode.Boundary };
                viewer.AddThemeFontSizeOverride("font_size", 14);
                box.AddChild(viewer);
                var feedback = Text(box, "コピーすると、この端末のクリップボードに入ります。", 16, Muted);
                Button(box, title + "のログをコピー", () =>
                {
                    try
                    {
                        if (!ClipboardAvailable) { feedback.Text = "この端末ではコピーを利用できません。"; return; }
                        DisplayServer.ClipboardSet(value);
                        feedback.Text = DisplayServer.ClipboardGet() == value ? "コピーしました。" : "コピー内容を確認できませんでした。";
                    }
                    catch { feedback.Text = "コピーできませんでした。時間をおいて再試行してください。"; }
                });
            }
        });
    }
}
