// SaveGame.cs — ランのセーブ/続きから (2026-09-15 ユーザー「unity版にもセーブ機能を作ってほしい。本家のようなセーブロード」)。
// 本家形: 進行中のランは1本・自動保存 (1手ごと)・タイトルの「続きから」・メニューの「セーブして終了」と「ランを放棄」。
// 中身はブラウザ/CLI と同じ RunSaveFile (Report.MakeRunSaveFile = 状態丸ごと＋戦闘の保管＋メモ＋選択履歴＋ジャーナル)
// ＝ persistentDataPath/save/run.json は `npx tsx src/sim/play.ts show` やブラウザの📂でもそのまま開ける。
// 書き込みは別スレッド (RunState は不変・一覧は写し) で、最新の1つだけを書く (連打しても溜めない)。
// 落ちても壊れないように run.json.tmp → run.json.bak ← run.json の順で差し替え、読む時は run.json → .bak の順に試す。
// エンジンには何も持たせない。読み戻しは Report.ReadRunSaveFile (丸ごと → 駄目ならジャーナル再生の二重の保険)。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Game
{
    public static class SaveGame
    {
        public static string Dir { get { return Path.Combine(Application.persistentDataPath, "save"); } }
        public static string FilePath { get { return Path.Combine(Dir, "run.json"); } }
        static string BakPath { get { return FilePath + ".bak"; } }
        static string TmpPath { get { return FilePath + ".tmp"; } }

        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        static readonly object Lock = new object();
        static RunSaveFile _pending;
        static bool _writing;

        /// <summary>セーブがあるか (走破/敗北で終わったランは Delete 済みなので、あれば進行中)</summary>
        public static bool Exists { get { return File.Exists(FilePath) || File.Exists(BakPath); } }

        /// <summary>セーブの中身を組む (メインスレッド)。Feedback の一覧と落書きを写す</summary>
        static RunSaveFile Make(GameRoot g, RunState run)
        {
            return Report.MakeRunSaveFile(run, Feedback.History, Feedback.Notes, Feedback.JournalOrNull(), Feedback.Choices, DoodlesToken(g != null ? g.Doodles : null));
        }

        /// <summary>1手ごとの自動保存 (非同期)。連続して呼ばれたら最新だけを書く</summary>
        public static void Write(GameRoot g, RunState run)
        {
            if (run == null) return;
            RunSaveFile sf;
            try { sf = Make(g, run); }
            catch (Exception e) { Debug.LogWarning("[SaveGame] make: " + e.Message); return; }
            lock (Lock)
            {
                _pending = sf;
                if (_writing) return;
                _writing = true;
            }
            ThreadPool.QueueUserWorkItem(delegate { Worker(); });
        }

        static void Worker()
        {
            while (true)
            {
                RunSaveFile sf;
                lock (Lock)
                {
                    sf = _pending;
                    _pending = null;
                    if (sf == null) { _writing = false; return; }
                }
                try { WriteFile(sf); }
                catch (Exception e) { Debug.LogWarning("[SaveGame] write: " + e.Message); }
            }
        }

        /// <summary>今すぐ同期で書く (セーブして終了・バックグラウンド移行・終了)。進行中の書き込みが終わるのを待ってから最新を書く</summary>
        public static void Flush(GameRoot g, RunState run)
        {
            if (run == null) return;
            RunSaveFile sf;
            try { sf = Make(g, run); }
            catch (Exception e) { Debug.LogWarning("[SaveGame] make: " + e.Message); return; }
            lock (Lock) { _pending = null; }
            var t0 = DateTime.UtcNow;
            while (true)
            {
                lock (Lock) { if (!_writing) break; }
                if ((DateTime.UtcNow - t0).TotalSeconds > 3) break;
                Thread.Sleep(5);
            }
            try { WriteFile(sf); }
            catch (Exception e) { Debug.LogWarning("[SaveGame] flush: " + e.Message); }
        }

        /// <summary>tmp に書いてから差し替える (書いている途中で落ちても前のセーブが残る)</summary>
        static void WriteFile(RunSaveFile sf)
        {
            var json = Report.SerializeRunSaveFile(sf);
            Directory.CreateDirectory(Dir);
            File.WriteAllText(TmpPath, json, Utf8);
            if (File.Exists(FilePath))
            {
                if (File.Exists(BakPath)) File.Delete(BakPath);
                File.Move(FilePath, BakPath);
            }
            File.Move(TmpPath, FilePath);
        }

        /// <summary>セーブを消す (ランの放棄・走破/敗北・新しいランの開始)</summary>
        public static void Delete()
        {
            lock (Lock) { _pending = null; }
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                if (File.Exists(BakPath)) File.Delete(BakPath);
                if (File.Exists(TmpPath)) File.Delete(TmpPath);
            }
            catch (Exception e) { Debug.LogWarning("[SaveGame] delete: " + e.Message); }
        }

        /// <summary>セーブを読む。run.json が読めなければ .bak。どちらも駄目なら null と理由</summary>
        public static RunSaveFile Load(out string warning)
        {
            warning = null;
            string firstErr = null;
            foreach (var path in new[] { FilePath, BakPath })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    string w;
                    var sf = Report.ReadRunSaveFile(File.ReadAllText(path, Utf8), out w);
                    if (sf != null && sf.Run != null)
                    {
                        if (w != null) Debug.LogWarning("[SaveGame] load " + Path.GetFileName(path) + ": " + w);
                        warning = w;
                        return sf;
                    }
                    if (firstErr == null) firstErr = w;
                }
                catch (Exception e) { if (firstErr == null) firstErr = e.GetType().Name + ": " + e.Message; }
            }
            warning = firstErr ?? "セーブが無い";
            return null;
        }

        /// <summary>タイトルの「続きから」に出す要約 (読めない時は null)。ファイルの日時も添える</summary>
        public static string Summary(RunSaveFile sf)
        {
            if (sf == null || sf.Run == null) return null;
            var r = sf.Run;
            string leader = r.LeaderId;
            try { var ld = Content.GetLeaderDef(r.LeaderId); if (ld != null) leader = ld.Name; } catch (Exception) { }
            return leader + "　幕" + r.Act + " 行" + (r.Row + 1) + "　HP " + r.Hp + "/" + r.MaxHp + "　" + r.BattlesWon + "勝　難易度 " + r.Difficulty;
        }

        /// <summary>要約の2行目: 戦闘の途中なら「戦闘中 T3」(無ければ null)</summary>
        public static string Detail(RunSaveFile sf)
        {
            if (sf == null || sf.Run == null) return null;
            var r = sf.Run;
            if (r.Phase == RunPhases.Combat && r.Combat != null) return "戦闘中 ターン" + r.Combat.Turn + " の途中";
            return null;
        }

        static RunSaveFile _peekCache;
        static DateTime _peekStamp;
        static bool _peekValid;

        /// <summary>タイトル用: セーブを読んで覚えておく (ファイルの日時が同じなら読み直さない = リーダーを選ぶたびに解析しない)。無い/読めない時は null</summary>
        public static RunSaveFile Peek()
        {
            if (!Exists) { _peekValid = false; _peekCache = null; return null; }
            var stamp = SavedAt() ?? DateTime.MinValue;
            if (_peekValid && stamp == _peekStamp) return _peekCache;
            RunSaveFile sf = null;
            try { string w; sf = Load(out w); }
            catch (Exception e) { Debug.LogWarning("[SaveGame] peek: " + e.Message); }
            _peekCache = sf; _peekStamp = stamp; _peekValid = true;
            return sf;
        }

        public static string PeekSummary() { return Summary(Peek()); }

        /// <summary>最後に保存した時刻 (表示用)。無ければ null</summary>
        public static DateTime? SavedAt()
        {
            try
            {
                if (File.Exists(FilePath)) return File.GetLastWriteTime(FilePath);
                if (File.Exists(BakPath)) return File.GetLastWriteTime(BakPath);
            }
            catch (Exception) { }
            return null;
        }

        // ---- 落書き (UI 層の状態) ⇄ JSON。{"1":[{"color":0,"points":[[x,y],...]}]} ----

        public static JToken DoodlesToken(Dictionary<int, List<DoodleStroke>> doodles)
        {
            if (doodles == null) return null;
            var jo = new JObject();
            foreach (var kv in doodles)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                var arr = new JArray();
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var s = kv.Value[i];
                    var pts = new JArray();
                    for (int j = 0; j < s.Points.Count; j++) pts.Add(new JArray(s.Points[j].x, s.Points[j].y));
                    arr.Add(new JObject { { "color", s.Color }, { "points", pts } });
                }
                jo[kv.Key.ToString()] = arr;
            }
            return jo.Count > 0 ? jo : null;
        }

        public static Dictionary<int, List<DoodleStroke>> DoodlesFromToken(JToken token)
        {
            var result = new Dictionary<int, List<DoodleStroke>>();
            var jo = token as JObject;
            if (jo == null) return result;
            foreach (var prop in jo.Properties())
            {
                int act;
                if (!int.TryParse(prop.Name, out act)) continue;
                var list = new List<DoodleStroke>();
                var arr = prop.Value as JArray;
                if (arr == null) continue;
                foreach (var st in arr)
                {
                    var so = st as JObject;
                    if (so == null) continue;
                    var stroke = new DoodleStroke { Color = (int?)so["color"] ?? 0 };
                    var pts = so["points"] as JArray;
                    if (pts != null) foreach (var pt in pts)
                        {
                            var pa = pt as JArray;
                            if (pa != null && pa.Count >= 2) stroke.Points.Add(new Vector2((float)pa[0], (float)pa[1]));
                        }
                    if (stroke.Points.Count > 0) list.Add(stroke);
                }
                result[act] = list;
            }
            return result;
        }
    }
}
