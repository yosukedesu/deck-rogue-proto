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

namespace DeckRogue.EditorTools
{
    /// <summary>
    /// プレイモードのスモーク (2026-09-07): -batchmode でプレイモードに入り、GameRoot の公開APIで
    /// セットアップ→ラン開始→進路選択→戦闘 (ターン終了×3) を回して例外・エラーログ・画面の空を検出する。
    /// 描画は -nographics で行われないが、UI の組み立て (Rebuild) と engine の呼び出しは全て走る。
    /// 起動: Unity.exe -batchmode -nographics -projectPath ... -executeMethod DeckRogue.EditorTools.PlaySmoke.Run (-quit は付けない)
    /// </summary>
    public static class PlaySmoke
    {
        const string Flag = "DeckRogue.PlaySmoke.Armed";
        static bool _hooked;
        static int _step;
        static int _frames;
        static int _errors;
        static readonly System.Collections.Generic.List<string> _errorLines = new System.Collections.Generic.List<string>();
        static int _transitions;
        static int _turns;

        public static void Run()
        {
            SessionState.SetBool(Flag, true);
            Hook();
            Debug.Log("[DeckRogue] PlaySmoke: プレイモードへ");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            if (SessionState.GetBool(Flag, false)) Hook();
        }

        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (condition.StartsWith("[DeckRogue]")) return;
            // Editor 自身の起動時インデックス作成 (UnityEditor.Search.SearchDatabase) がバッチ初回に投げる ArgumentOutOfRange はゲームと無関係
            if (stackTrace != null && stackTrace.Contains("UnityEditor.Search.")) return;
            _errors++;
            if (_errorLines.Count < 8) _errorLines.Add(condition + (string.IsNullOrEmpty(stackTrace) ? "" : "\n" + stackTrace.Split('\n')[0]));
        }

        static void Finish(int code, string why)
        {
            SessionState.SetBool(Flag, false);
            EditorApplication.update -= Tick;
            foreach (var l in _errorLines) Debug.Log("[DeckRogue] 検出したエラー: " + l);
            Debug.Log($"[DeckRogue] PlaySmoke 終了 code={code} ({why}) errors={_errors} transitions={_transitions} turns={_turns}");
            if (Application.isBatchMode) EditorApplication.Exit(code);
            else EditorApplication.ExitPlaymode();
        }

        static int CountUnder<T>(Component root) where T : Component => root == null ? 0 : root.GetComponentsInChildren<T>(true).Length;

        static void Tick()
        {
            _frames++;
            try
            {
                var g = DeckRogue.Game.GameRoot.I;
                switch (_step)
                {
                    case 0:
                        if (!Application.isPlaying || g == null)
                        {
                            if (_frames > 1500) Finish(1, "GameRoot が起動しない");
                            return;
                        }
                        Debug.Log($"[DeckRogue] boot ok: Content.IsLoaded={Content.IsLoaded} texts={CountUnder<UnityEngine.UI.Text>(g)} buttons={CountUnder<UnityEngine.UI.Button>(g)} error={g.Error ?? "なし"}");
                        if (!Content.IsLoaded || g.Error != null) { Finish(1, "データ読込に失敗"); return; }
                        if (CountUnder<UnityEngine.UI.Text>(g) == 0) { Finish(1, "セットアップ画面が空"); return; }
                        _step = 1; _frames = 0;
                        return;
                    case 1:
                        if (_frames < 5) return;
                        g.Seed = 4242;
                        g.StartRun();
                        Debug.Log($"[DeckRogue] StartRun: phase={g.Rs?.Phase ?? "null"} error={g.Error ?? "なし"} texts={CountUnder<UnityEngine.UI.Text>(g)} buttons={CountUnder<UnityEngine.UI.Button>(g)}");
                        if (g.Rs == null || g.Error != null || g.Rs.Phase != RunPhases.Map) { Finish(1, "ラン開始に失敗"); return; }
                        _step = 2; _frames = 0;
                        return;
                    case 2:
                    {
                        if (_frames < 5) return;
                        var rs = g.Rs;
                        if (rs == null || g.Error != null) { Finish(1, "ラン状態が壊れた: " + (g.Error ?? "")); return; }
                        if (rs.Phase == RunPhases.Combat) { _step = 3; _frames = 0; return; }
                        if (_transitions >= 10) { Finish(1, "10回遷移しても戦闘に入らない: " + rs.Phase); return; }
                        RunCommand cmd = null;
                        switch (rs.Phase)
                        {
                            case RunPhases.Map:
                            {
                                var cols = DeckRogue.Engine.Run.NextChoices(rs);
                                cmd = new RunCommand_ChooseNode { Col = cols[0] };
                                break;
                            }
                            case RunPhases.Event:
                            {
                                // 規約: 最後の選択肢は常に安全な「立ち去る」
                                var eventId = (string)rs.GetType().GetProperty("EventId")?.GetValue(rs);
                                var ev = Content.AllEvents.FirstOrDefault(e => e.Id == eventId);
                                cmd = new RunCommand_EventChoice { Index = ev != null ? ev.Choices.Count - 1 : 0 };
                                break;
                            }
                            case RunPhases.Shop: cmd = new RunCommand_ShopLeave(); break;
                            case RunPhases.Campfire: cmd = new RunCommand_CampfireRest(); break;
                            case RunPhases.Workshop: cmd = new RunCommand_WorkshopSkip(); break;
                            case RunPhases.RelicReward: cmd = new RunCommand_SkipRelic(); break;
                            case RunPhases.Reward: cmd = new RunCommand_SkipReward(); break;
                            default: Finish(1, "想定外のフェーズ: " + rs.Phase); return;
                        }
                        g.Do(cmd);
                        _transitions++;
                        Debug.Log($"[DeckRogue] 遷移{_transitions}: {cmd.Type} → phase={g.Rs?.Phase} error={g.Error ?? "なし"} texts={CountUnder<UnityEngine.UI.Text>(g)} buttons={CountUnder<UnityEngine.UI.Button>(g)}");
                        _frames = 0;
                        return;
                    }
                    case 3:
                    {
                        if (_frames < 5) return;
                        var rs = g.Rs;
                        if (rs == null || g.Error != null) { Finish(1, "戦闘中にエラー: " + (g.Error ?? "")); return; }
                        if (rs.Phase != RunPhases.Combat || _turns >= 3) { _step = 4; _frames = 0; return; }
                        var combat = rs.Combat;
                        if (combat != null && combat.Phase == CombatPhases.AwaitingReaction)
                        {
                            g.DoCombat(new Command_ConfirmReaction { Fire = false });
                            Debug.Log($"[DeckRogue] 温存 → combat.phase={g.Rs?.Combat?.Phase} error={g.Error ?? "なし"}");
                        }
                        else
                        {
                            g.DoCombat(new Command_EndTurn());
                            _turns++;
                            var c = g.Rs?.Combat;
                            Debug.Log($"[DeckRogue] ターン終了{_turns}: run.phase={g.Rs?.Phase} combat.phase={c?.Phase} hp={c?.Player?.Hp} error={g.Error ?? "なし"} texts={CountUnder<UnityEngine.UI.Text>(g)} buttons={CountUnder<UnityEngine.UI.Button>(g)}");
                        }
                        _frames = 0;
                        return;
                    }
                    case 4:
                        Finish(_errors == 0 ? 0 : 1, _errors == 0 ? "OK" : "エラーログあり");
                        return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[DeckRogue] PlaySmoke で例外: " + e);
                Finish(1, "例外");
            }
        }
    }
}

namespace DeckRogue.EditorTools
{
    /// <summary>
    /// URP セットアップ (2026-09-07): Built-in Render Pipeline が Unity 6.5 で非推奨になった (6.7 で終了) ため、
    /// URP アセットを作って Graphics/Quality に割り当てる。uGUI のオーバーレイ描画だけなのでパイプライン差は無い。
    /// 起動: Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod DeckRogue.EditorTools.UrpSetup.Run
    /// (manifest に com.unity.render-pipelines.universal が入っていること)
    /// </summary>
    public static class UrpSetup
    {
        public static void Run()
        {
            int code = 0;
            try
            {
                const string dir = "Assets/Settings";
                const string assetPath = dir + "/URP-Default.asset";
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Settings");
                var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>(assetPath);
                UnityEngine.Rendering.RenderPipelineAsset asset = existing;
                // レンダラーデータ (URP 17 の Create() は m_RendererDataList を空のまま作る＝実行時に「Default Renderer が無い」)。
                // メニューの「URP Universal Renderer」と同じ手順: CreateInstance → パッケージのシェーダー参照を埋める → 保存
                const string rendererPath = dir + "/URP-Renderer.asset";
                var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(rendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
                    UnityEngine.Rendering.ResourceReloader.ReloadAllNullIn(renderer, UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.packagePath);
                    AssetDatabase.CreateAsset(renderer, rendererPath);
                }
                if (asset == null)
                {
                    asset = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                // 既存アセット (GUID を保つ) のレンダラー欄を SerializedObject で埋める
                {
                    var so = new SerializedObject(asset);
                    var list = so.FindProperty("m_RendererDataList");
                    if (list != null)
                    {
                        if (list.arraySize < 1) list.arraySize = 1;
                        list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    }
                    var idx = so.FindProperty("m_DefaultRendererIndex");
                    if (idx != null) idx.intValue = 0;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                }
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = asset;
                var names = QualitySettings.names;
                for (int i = 0; i < names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = asset;
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var check = new SerializedObject(asset).FindProperty("m_RendererDataList");
                Debug.Log($"[DeckRogue] URP レンダラー: {(check != null && check.arraySize > 0 && check.GetArrayElementAtIndex(0).objectReferenceValue != null ? check.GetArrayElementAtIndex(0).objectReferenceValue.name : "なし")}");
                Debug.Log($"[DeckRogue] URP を設定: {assetPath} / defaultRenderPipeline={(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.name : "null")} / quality levels={names.Length}");
            }
            catch (Exception e)
            {
                Debug.LogError("[DeckRogue] URP 設定で例外: " + e);
                code = 1;
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
