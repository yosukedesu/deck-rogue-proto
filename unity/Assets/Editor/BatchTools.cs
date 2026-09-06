// BatchTools.cs — バッチ起動 (scripts/unity-win.sh) から呼ぶ検証の入口 (2026-09-07)。
//   Unity.exe -batchmode -nographics -quit -projectPath <proj> -executeMethod DeckRogue.EditorTools.BatchTools.VerifyGoldens
// EngineTests/Program.cs の VerifyGoldenRuns と同じ手順を Unity のランタイム (Mono/IL2CPP 前段) で回し、
// エンジンが Unity 内でも TS と同じハッシュを出すことを確かめる。結果は [DeckRogue] 接頭辞でログに出す。
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.EditorTools
{
    public static class BatchTools
    {
        public static void VerifyGoldens()
        {
            int code;
            try
            {
                code = VerifyCore();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DeckRogue] verify で例外: {e}");
                code = 1;
            }
            Debug.Log($"[DeckRogue] verify 終了 code={code}");
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static int VerifyCore()
        {
            var dataDir = Path.Combine(Application.streamingAssetsPath, "data");
            var goldenDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "goldens", "runs"));
            if (!Directory.Exists(dataDir)) { Debug.LogError($"[DeckRogue] データが無い: {dataDir}"); return 2; }
            if (!Directory.Exists(goldenDir)) { Debug.LogError($"[DeckRogue] ゴールデンが無い: {goldenDir}"); return 2; }

            Content.Load(dataDir);
            Debug.Log($"[DeckRogue] データ読込: カード{Content.AllCards.Count} 敵{Content.AllEnemies.Count} 編成{Content.AllEncounters.Count} " +
                      $"デッキ{Content.AllDecks.Count} リーダー{Content.AllLeaders.Count} レリック{Content.AllRelics.Count} イベント{Content.AllEvents.Count}");

            int failedFiles = 0;
            var files = Directory.GetFiles(goldenDir, "*.json").OrderBy(f => f).ToArray();
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var golden = JObject.Parse(File.ReadAllText(file));
                var origin = (JObject)golden["origin"];
                var commands = (JArray)golden["commands"];
                var hashes = (JArray)golden["hashes"];
                if (origin == null || commands == null || hashes == null) { Debug.LogError($"[DeckRogue] {name}: origin/commands/hashes が無い"); failedFiles++; continue; }
                var kind = (string)origin["kind"] ?? "run";
                if (kind != "run") { Debug.LogError($"[DeckRogue] {name}: origin.kind={kind} は未対応"); failedFiles++; continue; }

                var seed = (int)origin["seed"];
                var leaderId = (string)origin["leaderId"];
                var deckId = (string)origin["deckId"];
                var difficulty = origin["difficulty"] != null ? (int)origin["difficulty"] : Run.DEFAULT_DIFFICULTY;

                RunState run;
                try { run = Run.CreateRun(seed, ReactionModes.SetConfirm, leaderId, deckId, difficulty); }
                catch (Exception e) { Debug.LogError($"[DeckRogue] {name}: ラン生成で例外: {e.Message}"); failedFiles++; continue; }

                var count = Math.Min(commands.Count, hashes.Count);
                var ok = true;
                for (int i = 0; i < count; i++)
                {
                    var raw = commands[i].ToString(Formatting.None);
                    RunCommand cmd;
                    try { cmd = JsonUnions.FromToken<RunCommand>(commands[i]); }
                    catch (Exception e) { Debug.LogError($"[DeckRogue] {name}: index={i} のコマンドを読めない: {e.Message} / {raw}"); ok = false; break; }
                    try { run = Run.ApplyRunCommand(run, cmd); }
                    catch (Exception e) { Debug.LogError($"[DeckRogue] {name}: index={i} ({i + 1}手目) で例外: {e.Message} / 手={raw}"); ok = false; break; }
                    var expected = (string)hashes[i];
                    var actual = Golden.RunHash(run);
                    if (actual != expected)
                    {
                        Debug.LogError($"[DeckRogue] {name}: index={i} ({i + 1}手目) でハッシュ不一致 期待{expected} 実際{actual} / 手={raw}");
                        ok = false;
                        break;
                    }
                }
                if (ok) Debug.Log($"[DeckRogue] {name}: 全{count}手が一致 ✅");
                else failedFiles++;
            }
            Debug.Log(failedFiles == 0
                ? $"[DeckRogue] ランのゴールデン照合: {files.Length}ファイルすべて一致 ✅"
                : $"[DeckRogue] ランのゴールデン照合: {failedFiles}/{files.Length} ファイルが不一致");
            return failedFiles == 0 ? 0 : 1;
        }
    }
}
