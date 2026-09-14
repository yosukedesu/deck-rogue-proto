// FeedbackUi.cs — フィードバック記録の画面 (2026-09-14): 戦闘直後の評価ダイアログ・プレイ中メモの窓・上部バーの「メモ」「レポート」ボタン。
// ブラウザ版 (App.tsx の BattleRatingBar / NoteBar / 📄) と同じ入力: 強さ1〜5・面白さ1〜5・敗北時は敗因の2択が必須・ひとことメモ・決定/スキップ。
// 状態は Feedback (UI 層)。エンジンには触れない。
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class FeedbackUi
    {
        /// <summary>上部バーの右側に「メモ N」「レポート」を足す (ラン画面と戦闘画面で共通)</summary>
        public static void TopBarButtons(GameRoot g, Transform bar)
        {
            var memo = UiKit.Btn(bar, Feedback.Notes.Count > 0 ? "メモ " + Feedback.Notes.Count : "メモ", delegate { Feedback.MemoOpen = !Feedback.MemoOpen; g.Rebuild(); }, 13);
            BattleScreen.SetSize(memo, Feedback.Notes.Count > 0 ? 92f : 72f, 36f);
            Tooltip.Attach(memo.gameObject, delegate { return "プレイ中メモ: 気づいたことをその場で残す (レポートに同梱される)"; });
            var rep = UiKit.Btn(bar, "レポート", delegate { ExportNow(g); }, 13);
            BattleScreen.SetSize(rep, 92f, 36f);
            Tooltip.Attach(rep.gameObject, delegate { return "プレイレポート (md) とセーブ (json) を書き出す\n" + Feedback.ReportsDir; });
        }

        /// <summary>レポートを書き出して通知に道を出す</summary>
        public static void ExportNow(GameRoot g)
        {
            try
            {
                var md = Feedback.Export(g.Rs);
                g.Error = null;
                g.Notice = "レポートを書き出した: " + md;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                g.Error = "書き出しに失敗: " + e.Message;
            }
            g.Rebuild();
        }

        /// <summary>決着直後のフェーズで、閉じた後でも評価を直せる小さなボタン (画面左下)</summary>
        public static void RateButton(GameRoot g, RectTransform root)
        {
            if (!Feedback.CanRate(g.Rs) || Feedback.RatingOpen) return;
            var last = Feedback.LastBattle;
            bool rated = last != null && last.Rating != null && last.Rating.Strength.HasValue && last.Rating.Fun.HasValue;
            var b = UiKit.Btn(root, rated ? "評価 済" : "評価（未入力）", delegate { Feedback.OpenRating(); g.Rebuild(); }, 14, true, rated ? (Color?)null : UiKit.Hex("#f0d58a"));
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 40f), new Vector2(190f, 80f));
            Tooltip.Attach(b.gameObject, delegate { return "この戦闘の評価を直す (強さ・面白さ・メモ)"; });
        }

        // ---- 入力欄 ----

        /// <summary>紙の入力欄 (RunScreens.MakeSeedField と同じ作り)。onChange は下書きを Rebuild をまたいで残すためのもの</summary>
        public static TMP_InputField TextField(Transform parent, string initial, string placeholder, bool multiline, float height, Action<string> onChange)
        {
            var pan = UiKit.Pan(parent, UiKit.ColPanel2, "textField");
            UiKit.Le(pan, -1f, height, -1f, height);
            var viewport = UiKit.NewRect("viewport", pan.transform);
            UiKit.Stretch(viewport, 10f, 10f, 6f, 6f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var txt = UiKit.Txt(viewport, "", 16, UiKit.ColInk, multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft);
            UiKit.Stretch(txt.rectTransform, 0f, 0f, 0f, 0f);
            var ph = UiKit.Txt(viewport, placeholder, 16, UiKit.ColInkSoft, multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft);
            UiKit.Stretch(ph.rectTransform, 0f, 0f, 0f, 0f);
            var field = pan.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = viewport;
            field.textComponent = txt;
            field.placeholder = ph;
            field.caretColor = UiKit.ColInk;
            field.customCaretColor = true;
            field.targetGraphic = pan;
            field.contentType = TMP_InputField.ContentType.Standard;
            field.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            field.characterLimit = multiline ? 2000 : 300;
            field.text = initial ?? "";
            if (onChange != null) field.onValueChanged.AddListener(delegate (string v) { onChange(v); });
            return field;
        }

        // ---- プレイ中メモ ----

        public static void MemoDialog(GameRoot g, RectTransform root)
        {
            var inner = BattleScreen.Modal(root, 960f, Feedback.Notes.Count > 0 ? 560f : 420f, "memo");
            UiKit.Head(inner, "プレイメモ — 気づいたことをその場で", 24);
            var ctx = UiKit.Txt(inner, "文脈: " + Feedback.NoteContext(g.Rs) + "（自動で付く。レポートの「プレイメモ」に同梱）", 14, UiKit.ColInkSoft);
            UiKit.Le(ctx, -1f, 24f, -1f, 24f);
            var field = TextField(inner, Feedback.MemoDraft, "例: 探り屋の3拍目が読めなかった／この報酬は全部いらない", true, 200f, delegate (string v) { Feedback.MemoDraft = v; });
            var rows = UiKit.NewRect("btns", inner);
            UiKit.Le(rows, -1f, 56f, -1f, 56f);
            var hg = UiKit.Horz(rows, 14, 0);
            hg.childAlignment = TextAnchor.MiddleRight;
            hg.childForceExpandHeight = false;
            var close = UiKit.Btn(rows, "閉じる", delegate { Feedback.MemoOpen = false; g.Rebuild(); }, 18);
            BattleScreen.SetSize(close, 180f, 50f);
            var save = UiKit.Btn(rows, "記録する", delegate
            {
                Feedback.AddNote(g.Rs, Feedback.MemoDraft);
                Feedback.MemoDraft = "";
                Feedback.MemoOpen = false;
                g.Notice = "メモを記録した（" + Feedback.Notes.Count + "件）";
                g.Rebuild();
            }, 18, true, UiKit.Hex("#cfeacc"));
            BattleScreen.SetSize(save, 200f, 50f);
            // 直近のメモ (3件) = 何を書いたかの確認
            int n = Feedback.Notes.Count;
            if (n > 0)
            {
                UiKit.Head(inner, "これまでのメモ " + n + "件（直近3件）", 16);
                for (int i = Math.Max(0, n - 3); i < n; i++)
                {
                    var note = Feedback.Notes[i];
                    var t = UiKit.Txt(inner, "・[" + (note.At.Length >= 16 ? note.At.Substring(11, 5) : note.At) + " " + note.Context + "] " + note.Text, 14, UiKit.ColInk);
                    UiKit.Le(t, -1f, 26f, -1f, 26f);
                }
            }
            field.ActivateInputField();
        }

        // ---- 戦闘直後の評価 ----

        public static void RatingDialog(GameRoot g, RectTransform root)
        {
            var last = Feedback.LastBattle;
            if (last == null) return;
            bool lost = last.Result == "lost";
            var inner = BattleScreen.Modal(root, 1000f, lost ? 470f : 410f, "rating");
            UiKit.Head(inner, last.BattleNo + "戦目 " + Report.SafeEncounterName(last.EnemyId) + (last.Elite ? "（強個体）" : "") + " はどうでしたか？", 24);
            var sub = UiKit.Txt(inner, "1タップずつ・スキップ可（任意。調整の材料になります）", 14, UiKit.ColInkSoft);
            UiKit.Le(sub, -1f, 24f, -1f, 24f);

            ScoreRow(g, inner, "敵の強さ", Feedback.DraftStrength, delegate (int v) { Feedback.DraftStrength = v; });
            ScoreRow(g, inner, "面白さ", Feedback.DraftFun, delegate (int v) { Feedback.DraftFun = v; });
            if (lost)
            {
                var row = UiKit.NewRect("lossfeel", inner);
                UiKit.Le(row, -1f, 52f, -1f, 52f);
                var hg = UiKit.Horz(row, 10, 0);
                hg.childAlignment = TextAnchor.MiddleLeft;
                hg.childForceExpandHeight = false;
                var lbl = UiKit.Txt(row, "敗因の感触 <color=#9c3a2a>（敗北時は必須）</color>", 16, UiKit.ColInk, TextAnchor.MiddleLeft);
                UiKit.Le(lbl, 250f, 44f, 250f, 44f);
                foreach (var f in new[] { "build", "unfair" })
                {
                    string key = f;
                    bool on = Feedback.DraftLossFeel == f;
                    var b = UiKit.Btn(row, f == "build" ? "構築の失敗" : "理不尽", delegate { Feedback.DraftLossFeel = key; g.Rebuild(); }, 16, true, on ? UiKit.Hex("#f2b8b0") : (Color?)null);
                    BattleScreen.SetSize(b, 150f, 44f);
                }
                var hint = UiKit.Txt(row, "理不尽が2本一致したら数値でなく構造を作り直す", 12, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
                UiKit.Le(hint, 200f, 44f, -1f, 44f, 1f);
            }
            TextField(inner, Feedback.DraftNote, "ひとことメモ（任意。理不尽だった瞬間・退屈だった理由など）", false, 48f, delegate (string v) { Feedback.DraftNote = v; });

            bool canCommit = Feedback.DraftStrength > 0 && Feedback.DraftFun > 0 && (!lost || Feedback.DraftLossFeel != null);
            var rows = UiKit.NewRect("btns", inner);
            UiKit.Le(rows, -1f, 56f, -1f, 56f);
            var hg2 = UiKit.Horz(rows, 14, 0);
            hg2.childAlignment = TextAnchor.MiddleRight;
            hg2.childForceExpandHeight = false;
            var skip = UiKit.Btn(rows, "スキップ", delegate { Feedback.RatingOpen = false; g.Rebuild(); }, 18);
            BattleScreen.SetSize(skip, 180f, 50f);
            var ok = UiKit.Btn(rows, "決定", delegate { Feedback.CommitRating(g.Rs); g.Notice = "評価を記録した"; g.Rebuild(); }, 18, canCommit, UiKit.Hex("#cfeacc"));
            BattleScreen.SetSize(ok, 200f, 50f);
        }

        static void ScoreRow(GameRoot g, Transform parent, string title, int value, Action<int> onPick)
        {
            var row = UiKit.NewRect("score", parent);
            UiKit.Le(row, -1f, 52f, -1f, 52f);
            var hg = UiKit.Horz(row, 10, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            var lbl = UiKit.Txt(row, title, 16, UiKit.ColInk, TextAnchor.MiddleLeft);
            UiKit.Le(lbl, 250f, 44f, 250f, 44f);
            for (int n = 1; n <= 5; n++)
            {
                int v = n;
                bool on = value == n;
                var b = UiKit.Btn(row, n.ToString(), delegate { onPick(v); g.Rebuild(); }, 18, true, on ? UiKit.Hex("#bcd2f0") : (Color?)null);
                BattleScreen.SetSize(b, 60f, 44f);
            }
            var note = UiKit.Txt(row, title == "敵の強さ" ? "1=弱い … 5=強い" : "1=退屈 … 5=最高", 12, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(note, 120f, 44f, -1f, 44f, 1f);
        }
    }
}
