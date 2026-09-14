// Feedback.cs — フィードバックの記録とレポートの書き出し (2026-09-14 ユーザー「プロトと同じようにフィードバックを記録、解析用のログを出力する機構を APK/Unity 版でも」)。
// ブラウザ版 (src/ui/App.tsx + ui/report.ts) と同じ4つを UI 層で持つ:
//   ①戦闘の保管 (BattleArchive = 決着ごとのログ・計測・評価)  ②プレイ中メモ (📝)  ③選択履歴 (ピック/鍛錬/合成/購入/イベント)  ④ジャーナル (全コマンドと時刻 = リプレイ・判断時間)
// 書き出しは Report.BuildReport (md) と Report.BuildRunSaveFile (json) = ブラウザ/CLI と同じ形。置き場は Application.persistentDataPath/reports/。
// Android は FileProvider の共有シートで Drive 等へ送れる (Plugins/Android/DeckRogueShare.androidlib)。PC はフォルダを開く。
// 落ちても失わないように、戦闘の決着と戦闘外の操作のたび・バックグラウンドへ回った時に autosave.md/json を書く (ブラウザの localStorage バックアップ相当)。
// エンジンには何も持たせない (純ロジック不変)。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class Feedback
    {
        public static readonly List<BattleArchive> History = new List<BattleArchive>();
        public static readonly List<PlayNote> Notes = new List<PlayNote>();
        public static readonly List<RunChoice> Choices = new List<RunChoice>();
        static ReplayOrigin _origin;
        static readonly List<RunCommand> _commands = new List<RunCommand>();
        static readonly List<long> _times = new List<long>();

        // ---- ダイアログの状態 (UI 層) ----
        /// <summary>📝 メモの入力窓を開いているか。下書きは Rebuild をまたいで残す</summary>
        public static bool MemoOpen;
        public static string MemoDraft = "";
        /// <summary>戦闘直後の評価ダイアログを開いているか (決定/スキップで閉じる。「評価」ボタンで再び開く)</summary>
        public static bool RatingOpen;
        /// <summary>ダイアログの下書き (押した点数はその場で反映せず、決定で1回だけ確定する = ブラウザ版と同じ)</summary>
        public static int DraftStrength = -1, DraftFun = -1;
        public static string DraftLossFeel;
        public static string DraftNote = "";
        /// <summary>書き出しの結果 (画面の通知用)。null = まだ</summary>
        public static string LastExportPath;
        /// <summary>自動操縦 (スクショ) では共有シート/フォルダを開かない</summary>
        public static bool Silent;

        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>レポートの置き場 (Android: /storage/emulated/0/Android/data/com.deckrogue.proto/files/reports = adb pull / USB で読める)</summary>
        public static string ReportsDir
        {
            get { return Path.Combine(Application.persistentDataPath, "reports"); }
        }

        /// <summary>ログ行と意図の文言は表示層 (CardText) のもの</summary>
        public static ReportText Text
        {
            get { return new ReportText { LogLine = CardText.LogLine, IntentText = CardText.IntentText }; }
        }

        static long NowMs() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        /// <summary>ランの開始 (StartRun / チェックポイント開始)。記録を白紙にして origin を控える</summary>
        public static void BeginRun(ReplayOrigin origin)
        {
            History.Clear(); Notes.Clear(); Choices.Clear();
            _commands.Clear(); _times.Clear();
            _origin = origin;
            MemoOpen = false; MemoDraft = "";
            RatingOpen = false; ResetDraft();
            LastExportPath = null;
        }

        public static void ResetDraft() { DraftStrength = -1; DraftFun = -1; DraftLossFeel = null; DraftNote = ""; }

        /// <summary>続きから (2026-09-15): セーブに同梱した戦闘の保管・メモ・選択履歴・ジャーナルを戻す (以後の手はその続きに記録される)</summary>
        public static void Restore(RunSaveFile sf)
        {
            History.Clear(); Notes.Clear(); Choices.Clear();
            _commands.Clear(); _times.Clear();
            _origin = null;
            if (sf.History != null) History.AddRange(sf.History);
            if (sf.PlayNotes != null) Notes.AddRange(sf.PlayNotes);
            if (sf.Choices != null) Choices.AddRange(sf.Choices);
            if (sf.Journal != null && sf.Journal.Origin != null && sf.Journal.Commands != null)
            {
                _origin = sf.Journal.Origin;
                _commands.AddRange(sf.Journal.Commands);
                var times = sf.Journal.Times;
                for (int i = 0; i < _commands.Count; i++) _times.Add(times != null && i < times.Count ? times[i] : (_times.Count > 0 ? _times[_times.Count - 1] : NowMs()));
            }
            MemoOpen = false; MemoDraft = "";
            RatingOpen = false; ResetDraft();
            LastExportPath = null;
        }

        public static RunJournal JournalOrNull()
        {
            if (_origin == null) return null;
            return new RunJournal { Origin = _origin, Commands = new List<RunCommand>(_commands), Times = new List<long>(_times) };
        }

        /// <summary>
        /// 成功したランコマンドを記録する (GameRoot.Do)。ジャーナル・選択履歴・戦闘の決着 (BattleArchive) の3つ。
        /// 戦闘外の操作と決着のたびに autosave も書く (戦闘中の1手ごとには書かない = 端末の負担を抑える)
        /// </summary>
        public static void Record(RunState prev, RunCommand cmd, RunState next)
        {
            if (prev == null || next == null || cmd == null) return;
            if (_origin != null) { _commands.Add(cmd); _times.Add(NowMs()); }
            try
            {
                var line = Report.DescribeRunChoice(prev, cmd, next);
                if (line != null) Choices.Add(line);
            }
            catch (Exception e) { Debug.LogWarning("[Feedback] choice: " + e.Message); }

            bool archived = false;
            var c = next.Combat;
            bool ended = c != null && (c.Phase == CombatPhases.Won || c.Phase == CombatPhases.Lost);
            if (ended && prev.Combat != null && prev.Combat.Phase != c.Phase)
            {
                try
                {
                    // 敵IDはノードでなく戦闘ログから取る (?マス発の戦闘はノードが encounterId を持たない)
                    string enemyId = null;
                    for (int k = 0; k < c.EventLog.Count; k++) { var st = c.EventLog[k] as GameEvent_CombatStarted; if (st != null) { enemyId = st.EnemyId; break; } }
                    MapNode node = null;
                    try { node = DeckRogue.Engine.Run.CurrentNode(prev); } catch (Exception) { }
                    if (enemyId == null) enemyId = node != null && node.EncounterId != null ? node.EncounterId : "unknown";
                    int battleNo = prev.BattlesWon + 1;
                    bool dup = false;
                    for (int i = 0; i < History.Count; i++) if (History[i].BattleNo == battleNo) dup = true;
                    if (!dup)
                    {
                        History.Add(Report.ArchiveBattle(c, battleNo, enemyId, prev.CurrentElite, prev.Hp, prev.Deck.Count, prev.Act, node != null && node.Type == MapNodeTypes.Boss, Text));
                        archived = true;
                        RatingOpen = true;   // 決着直後に1回だけダイアログで聞く (決定/スキップで閉じる)
                        ResetDraft();
                    }
                }
                catch (Exception e) { Debug.LogWarning("[Feedback] archive: " + e.Message); }
            }
            if (archived || !(cmd is RunCommand_Combat)) Autosave(next);
        }

        /// <summary>プレイ中メモ。文脈 (幕/行/フェーズ/ターン/HP) を自動で付ける (ブラウザ版 addNote と同じ形)</summary>
        public static void AddNote(RunState run, string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return;
            Notes.Add(new PlayNote { At = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"), Context = NoteContext(run), Text = text });
            if (run != null) { Autosave(run); SaveGame.Write(GameRoot.I, run); }   // メモもセーブに同梱 (2026-09-15)
        }

        public static string NoteContext(RunState run)
        {
            if (run == null) return "セットアップ";
            var c = run.Combat;
            bool inCombat = run.Phase == RunPhases.Combat && c != null;
            return "幕" + run.Act + " 行" + (run.Row + 1) + " " + run.Phase + (inCombat ? " T" + c.Turn : "") + " " + run.BattlesWon + "勝 HP" + (inCombat ? c.Player.Hp : run.Hp);
        }

        /// <summary>最後の戦闘の保管 (評価ダイアログの対象)。決着直後のフェーズでだけ意味を持つ</summary>
        public static BattleArchive LastBattle { get { return History.Count > 0 ? History[History.Count - 1] : null; } }

        /// <summary>評価ダイアログを出すか: 決着直後のフェーズで、開いている時だけ</summary>
        public static bool ShouldShowRating(RunState run)
        {
            if (!RatingOpen || run == null || LastBattle == null) return false;
            return run.Phase == RunPhases.Reward || run.Phase == RunPhases.RelicReward || run.Phase == RunPhases.Won || run.Phase == RunPhases.Lost;
        }

        /// <summary>「評価」ボタンを出すか (決着直後のフェーズなら、閉じた後でも直せる)</summary>
        public static bool CanRate(RunState run)
        {
            if (run == null || LastBattle == null) return false;
            return run.Phase == RunPhases.Reward || run.Phase == RunPhases.RelicReward || run.Phase == RunPhases.Won || run.Phase == RunPhases.Lost;
        }

        /// <summary>ダイアログを開く (既に付けた評価を下書きに写す)</summary>
        public static void OpenRating()
        {
            var last = LastBattle;
            ResetDraft();
            if (last != null && last.Rating != null)
            {
                DraftStrength = last.Rating.Strength ?? -1;
                DraftFun = last.Rating.Fun ?? -1;
                DraftLossFeel = last.Rating.LossFeel;
                DraftNote = last.Rating.Note ?? "";
            }
            RatingOpen = true;
        }

        /// <summary>決定: 下書きを最後の戦闘の評価として確定する (敗北時は敗因も必須 = 呼び出し側が活性を制御)</summary>
        public static void CommitRating(RunState run)
        {
            var last = LastBattle;
            if (last != null)
            {
                var r = last.Rating ?? new BattleRating();
                if (DraftStrength > 0) r.Strength = DraftStrength;
                if (DraftFun > 0) r.Fun = DraftFun;
                if (DraftLossFeel != null) r.LossFeel = DraftLossFeel;
                var note = (DraftNote ?? "").Trim();
                if (note.Length > 0) r.Note = note;
                last.Rating = r;
            }
            RatingOpen = false;
            if (run != null) { Autosave(run); SaveGame.Write(GameRoot.I, run); }   // 評価もセーブに同梱 (2026-09-15)
        }

        // ---- 書き出し ----

        static string Stamp()
        {
            var d = DateTime.Now;
            return d.ToString("yyyyMMdd-HHmm");
        }

        /// <summary>レポート (md) とセーブ (json) を reports/ に書き、Android なら共有シート・PC ならフォルダを開く。戻り値は md の道</summary>
        public static string Export(RunState run)
        {
            Directory.CreateDirectory(ReportsDir);
            string stamp = Stamp();
            string md = Path.Combine(ReportsDir, "play-" + stamp + ".md");
            string json = Path.Combine(ReportsDir, "save-" + stamp + ".json");
            var journal = JournalOrNull();
            File.WriteAllText(md, Report.BuildReport(run, null, History, "", Notes, Choices, journal, Text, DateTime.Now), Utf8);
            if (run != null) File.WriteAllText(json, Report.BuildRunSaveFile(run, History, Notes, journal, Choices), Utf8);
            LastExportPath = md;
            Deliver(run != null ? new[] { md, json } : new[] { md }, "DeckRogue play " + stamp);
            return md;
        }

        /// <summary>落ちても失わない保険: autosave.md / autosave.json を上書きする (ブラウザ版の localStorage バックアップ相当)</summary>
        public static void Autosave(RunState run)
        {
            if (run == null) return;
            try
            {
                Directory.CreateDirectory(ReportsDir);
                var journal = JournalOrNull();
                File.WriteAllText(Path.Combine(ReportsDir, "autosave.md"), Report.BuildReport(run, null, History, "自動保存", Notes, Choices, journal, Text, DateTime.Now), Utf8);
                File.WriteAllText(Path.Combine(ReportsDir, "autosave.json"), Report.BuildRunSaveFile(run, History, Notes, journal, Choices), Utf8);
            }
            catch (Exception e) { Debug.LogWarning("[Feedback] autosave: " + e.Message); }
        }

        public static bool HasAutosave { get { return File.Exists(Path.Combine(ReportsDir, "autosave.md")); } }

        /// <summary>タイトル画面から: 前回の autosave を日付つきの名前に複製して届ける (落ちた/閉じたランのデータ回収)</summary>
        public static string ExportAutosave()
        {
            string src = Path.Combine(ReportsDir, "autosave.md");
            if (!File.Exists(src)) return null;
            string stamp = File.GetLastWriteTime(src).ToString("yyyyMMdd-HHmm");
            string md = Path.Combine(ReportsDir, "play-" + stamp + "-autosave.md");
            File.Copy(src, md, true);
            var files = new List<string> { md };
            string srcJson = Path.Combine(ReportsDir, "autosave.json");
            if (File.Exists(srcJson))
            {
                string json = Path.Combine(ReportsDir, "save-" + stamp + "-autosave.json");
                File.Copy(srcJson, json, true);
                files.Add(json);
            }
            LastExportPath = md;
            Deliver(files.ToArray(), "DeckRogue play " + stamp + " (autosave)");
            return md;
        }

        /// <summary>書いたファイルを届ける: Android は共有シート (FileProvider)・PC/エディタはフォルダを開いて md をクリップボードへ</summary>
        static void Deliver(string[] paths, string subject)
        {
            if (Silent) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try { ShareAndroid(paths, subject); }
            catch (Exception e) { Debug.LogWarning("[Feedback] share: " + e.Message); }
#else
            try
            {
                if (paths.Length > 0 && paths[0].EndsWith(".md"))
                {
                    var text = File.ReadAllText(paths[0], Utf8);
                    if (text.Length < 400000) GUIUtility.systemCopyBuffer = text;   // ブラウザ版と同じくクリップボードにも (大きすぎる時は省く)
                }
                Application.OpenURL("file:///" + ReportsDir.Replace('\\', '/'));
            }
            catch (Exception e) { Debug.LogWarning("[Feedback] open folder: " + e.Message); }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Android の共有シート (ACTION_SEND_MULTIPLE)。persistentDataPath は他アプリから読めないので FileProvider の content:// で渡す。
        /// authorities は Plugins/Android/DeckRogueShare.androidlib/AndroidManifest.xml の ${applicationId}.fileprovider
        /// </summary>
        static void ShareAndroid(string[] paths, string subject)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            using (var uris = new AndroidJavaObject("java.util.ArrayList"))
            using (var fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider"))
            {
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND_MULTIPLE"));
                intent.Call<AndroidJavaObject>("setType", "*/*");
                string authority = Application.identifier + ".fileprovider";
                for (int i = 0; i < paths.Length; i++)
                {
                    using (var file = new AndroidJavaObject("java.io.File", paths[i]))
                    {
                        var uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", activity, authority, file);
                        uris.Call<bool>("add", uri);
                    }
                }
                intent.Call<AndroidJavaObject>("putParcelableArrayListExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uris);
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
                intent.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));
                using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "レポートを送る"))
                {
                    activity.Call("startActivity", chooser);
                }
            }
        }
#endif
    }
}
