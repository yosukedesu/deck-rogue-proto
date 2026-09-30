// HD2DShaderCheck.cs — 舞台のシェーダを実際に変換してみる検査 (2026-09-30 HD-2D 見本 P03。計画 docs/design/hd2d-slice-plan-2026-09-30.md §4 P03・§7-2)。
//   Unity.exe -batchmode -nographics -quit -projectPath <proj> -executeMethod DeckRogue.EditorTools.HD2DShaderCheck.Run
//   (GUI ではメニュー「DeckRogue/HD2D/シェーダを検査」)
// -nographics のバッチでは描画しないので、ShaderUtil.ShaderHasError だけでは「その端末の版を変換したら失敗する」を拾えない。そこで
//   1) Resources/Shaders の全シェーダ: ShaderHasError とエラーの行 (ShaderUtil.GetShaderMessages)
//   2) 同じ全シェーダの各パス × 代表のキーワードの組 × D3D (Windows)・Vulkan・GLES3x (Android) × 頂点/画素 を
//      ShaderData.Pass.CompileVariant で実際に変換する。キーワードの組は、そのパスが宣言している物だけに絞る (ShaderUtil.GetPassKeywords)。
//      HD-2D の新シェーダ (StageModule・StageUnitLit・StageShaft・TiltShift) は組を多く、それ以外は「無し」の1組だけ
//   3) SRP Batcher に載るか (内部 API を反射で読む。-nographics では判定できないことがあるので警告だけ・失敗には数えない)
// 結果は "[DeckRogue] [HD2DShaderCheck]" の行でログへ。変換の失敗・シェーダのエラーが1つでもあれば終了コード 1 (バッチの時)。
// 注意: _WRITE_RENDERING_LAYERS は #pragma target 4.5 の版なので GLES3x では試さない。INSTANCING_ON は PC の組にだけ入れる
// (手元の gcc の前処理では試せない版。scratchpad/hd2d/check-P03/oc.py の注記)。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.EditorTools
{
    public static class HD2DShaderCheck
    {
        const string Tag = "[DeckRogue] [HD2DShaderCheck]";
        const string ShaderDir = "Assets/Resources/Shaders";

        /// <summary>HD-2D の新シェーダ (組を多く試す)</summary>
        static readonly string[] Hd2dShaders = { "DeckRogue/StageModule", "DeckRogue/StageUnitLit", "DeckRogue/StageShaft", "DeckRogue/TiltShift" };

        /// <summary>代表のキーワードの組 (パスが宣言していない物は落として使う)。空の組 = 既定の版</summary>
        static readonly string[][] Hd2dKeywordSets =
        {
            new string[0],
            // PC (Forward+・影2段・SSAO・木漏れ日・Rendering Layers)
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS", "_ADDITIONAL_LIGHT_SHADOWS", "_SHADOWS_SOFT", "_SCREEN_SPACE_OCCLUSION",
                    "_LIGHT_COOKIES", "_LIGHT_LAYERS", "_CLUSTER_LIGHT_LOOP", "FOG_EXP2", "_WRITE_RENDERING_LAYERS",
                    "_CASTING_PUNCTUAL_LIGHT_SHADOW", "_GBUFFER_NORMALS_OCT", "INSTANCING_ON" },
            // 半立体・幹 (メッシュの UV・アルファで切る)
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS", "_ADDITIONAL_LIGHT_SHADOWS", "_SHADOWS_SOFT", "_SCREEN_SPACE_OCCLUSION",
                    "_LIGHT_COOKIES", "_LIGHT_LAYERS", "_CLUSTER_LIGHT_LOOP", "FOG_EXP2", "_UV_MESH", "_ALPHATEST_ON" },
            // スマホの段 (影1段・点光源は頂点・線形の霧)
            new[] { "_MAIN_LIGHT_SHADOWS", "_ADDITIONAL_LIGHTS_VERTEX", "_SHADOWS_SOFT_LOW", "FOG_LINEAR", "_UV_MESH" },
            // Forward+ でない時・画面の影
            new[] { "_MAIN_LIGHT_SHADOWS_SCREEN", "_ADDITIONAL_LIGHTS", "_SHADOWS_SOFT_HIGH", "FOG_EXP", "_ALPHATEST_ON" },
        };

        static readonly (ShaderCompilerPlatform platform, BuildTarget target, string name)[] Platforms =
        {
            (ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64, "D3D"),
            (ShaderCompilerPlatform.Vulkan, BuildTarget.Android, "Vulkan"),
            (ShaderCompilerPlatform.GLES3x, BuildTarget.Android, "GLES3x"),
        };

        /// <summary>バッチの入口 (終了コード 0 = 全部通った / 1 = 失敗あり)</summary>
        public static void Run()
        {
            int code;
            try { code = Check() == 0 ? 0 : 1; }
            catch (Exception e) { Debug.LogError($"{Tag} 例外: {e}"); code = 1; }
            Debug.Log($"{Tag} 終了 code={code}");
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        [MenuItem("DeckRogue/HD2D/シェーダを検査")]
        static void RunFromMenu()
        {
            int fails = Check();
            EditorUtility.DisplayDialog("シェーダを検査", fails == 0 ? "全部通った" : $"失敗 {fails} 件 (Console を見る)", "OK");
        }

        /// <summary>検査して失敗の数を返す</summary>
        public static int Check()
        {
            var t0 = DateTime.Now;
            var shaders = LoadShaders();
            if (shaders.Count == 0) { Debug.LogError($"{Tag} シェーダが見つからない ({ShaderDir})"); return 1; }
            foreach (var name in Hd2dShaders)
                if (!shaders.Any(s => s.name == name)) Debug.LogWarning($"{Tag} {name} が見つからない (まだ無いなら飛ばす)");

            int fails = 0, compiles = 0, warns = 0;
            foreach (var shader in shaders)
            {
                // 1) シェーダ全体のエラー
                var msgs = ShaderUtil.GetShaderMessages(shader);
                bool hasError = ShaderUtil.ShaderHasError(shader);
                foreach (var m in msgs)
                {
                    if (m.severity == ShaderCompilerMessageSeverity.Error)
                        Debug.LogError($"{Tag} {shader.name}: {m.message} ({m.file}:{m.line} {m.platform}) {m.messageDetails}");
                    else warns++;
                }
                if (hasError) { fails++; Debug.LogError($"{Tag} {shader.name}: ShaderHasError"); }

                // 2) 版を実際に変換
                bool hd2d = Hd2dShaders.Contains(shader.name);
                var sets = hd2d ? Hd2dKeywordSets : new[] { new string[0] };
                var data = ShaderUtil.GetShaderData(shader);
                if (data == null) { Debug.LogWarning($"{Tag} {shader.name}: ShaderData が無い"); continue; }
                for (int si = 0; si < data.SubshaderCount; si++)
                {
                    var sub = data.GetSubshader(si);
                    for (int pi = 0; pi < sub.PassCount; pi++)
                    {
                        var pass = sub.GetPass(pi);
                        var declared = new HashSet<string>(PassKeywordNames(shader, si, pi));
                        var tried = new HashSet<string>();
                        foreach (var set in sets)
                        {
                            foreach (var (platform, target, pname) in Platforms)
                            {
                                var kws = set.Where(k => declared.Contains(k))
                                             .Where(k => !(platform == ShaderCompilerPlatform.GLES3x && k == "_WRITE_RENDERING_LAYERS"))
                                             .ToArray();
                                string key = pname + "|" + string.Join(" ", kws);
                                if (!tried.Add(key)) continue;
                                foreach (var stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                                {
                                    if (!pass.HasShaderStage(stage)) continue;
                                    compiles++;
                                    ShaderData.VariantCompileInfo info;
                                    try { info = pass.CompileVariant(stage, kws, platform, target); }
                                    catch (Exception e)
                                    {
                                        fails++;
                                        Debug.LogError($"{Tag} {shader.name} [{pass.Name}] {pname} {stage} kw=({string.Join(" ", kws)}): 例外 {e.Message}");
                                        continue;
                                    }
                                    int errs = 0;
                                    if (info.Messages != null)
                                        foreach (var m in info.Messages)
                                        {
                                            if (m.severity == ShaderCompilerMessageSeverity.Error)
                                            {
                                                errs++;
                                                Debug.LogError($"{Tag} {shader.name} [{pass.Name}] {pname} {stage} kw=({string.Join(" ", kws)}): {m.message} ({m.file}:{m.line}) {m.messageDetails}");
                                            }
                                            else warns++;
                                        }
                                    if (!info.Success || errs > 0)
                                    {
                                        fails++;
                                        if (errs == 0) Debug.LogError($"{Tag} {shader.name} [{pass.Name}] {pname} {stage} kw=({string.Join(" ", kws)}): 変換に失敗 (メッセージ無し)");
                                    }
                                }
                            }
                        }
                    }
                }

                // 3) SRP Batcher (新シェーダだけ。警告のみ)
                if (hd2d) CheckSrpBatcher(shader, Math.Min(data.SubshaderCount, OwnSubshaderCount(shader)));   // 代替 (FallBack) の subshader は見ない
            }
            Debug.Log($"{Tag} シェーダ {shaders.Count}・変換 {compiles}・失敗 {fails}・警告 {warns} ({(DateTime.Now - t0).TotalSeconds:0.0}秒)");
            return fails;
        }

        static List<Shader> LoadShaders()
        {
            var list = new List<Shader>();
            foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { ShaderDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var s = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (s != null) list.Add(s);
            }
            return list.OrderBy(s => s.name, StringComparer.Ordinal).ToList();
        }

        static IEnumerable<string> PassKeywordNames(Shader shader, int subshader, int pass)
        {
            // ShaderData の subshader には FallBack (StageUnit・StageUnitLit の Sprites/Default) の分も入る。
            // その番号で元のシェーダに聞くと Unity が内側で「Invalid pass identifier.」をエラーのログに出す (例外ではない) ので、
            // 元のシェーダの範囲の外は聞かずに「キーワード無し」で変換する (W1 統合 2026-09-30)
            // Shader.subshaderCount も代替の分を数えるので、元のファイルの SubShader の数で見分ける。
            if (subshader >= OwnSubshaderCount(shader)) yield break;
            LocalKeyword[] kws;
            try { kws = ShaderUtil.GetPassKeywords(shader, new PassIdentifier((uint)subshader, (uint)pass)); }
            catch (Exception e) { Debug.LogWarning($"{Tag} {shader.name}: GetPassKeywords で例外 {e.Message}"); yield break; }
            foreach (var k in kws) yield return k.name;
        }

        /// <summary>シェーダのファイルに書いてある SubShader の数 (FallBack の分を含まない)。読めなければ数えない (int.MaxValue)</summary>
        static int OwnSubshaderCount(Shader shader)
        {
            try
            {
                var path = AssetDatabase.GetAssetPath(shader);
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return int.MaxValue;
                int n = 0;
                foreach (var line in System.IO.File.ReadAllLines(path))
                {
                    var t = line.TrimStart();
                    if (t.StartsWith("SubShader", StringComparison.Ordinal) && (t.Length == 9 || !char.IsLetterOrDigit(t[9]))) n++;
                }
                return n > 0 ? n : int.MaxValue;
            }
            catch { return int.MaxValue; }
        }

        static void CheckSrpBatcher(Shader shader, int subshaderCount)
        {
            var code = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.NonPublic | BindingFlags.Static);
            var reason = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", BindingFlags.NonPublic | BindingFlags.Static);
            if (code == null) { Debug.Log($"{Tag} {shader.name}: SRP Batcher の判定 API が無いので飛ばす"); return; }
            for (int si = 0; si < subshaderCount; si++)
            {
                int c;
                try { c = (int)code.Invoke(null, new object[] { shader, si }); }
                catch (Exception e) { Debug.Log($"{Tag} {shader.name}: SRP Batcher の判定で例外 {e.InnerException?.Message ?? e.Message}"); return; }
                if (c == 0) { Debug.Log($"{Tag} {shader.name}: SRP Batcher に載る (subshader {si})"); continue; }
                string why = "";
                try { if (reason != null) why = (string)reason.Invoke(null, new object[] { shader, si, c }); } catch { }
                Debug.LogWarning($"{Tag} {shader.name}: SRP Batcher に載らない? (subshader {si} code {c}) {why} — -nographics では判定できないことがある。GUI の Editor の Inspector で確かめる");
            }
        }
    }
}
