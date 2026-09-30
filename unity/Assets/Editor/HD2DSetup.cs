// HD2DSetup.cs — HD-2D 見本の「設定の道具」(2026-09-30 P06。docs/design/hd2d-slice-plan-2026-09-30.md §2-5〜§2-7・§3・P06)。
//
// prep (Apply) が変えるのはプロジェクトの設定だけ (実行時の見た目の値は StageLook・旗が持つ)。どれも「ビルドの時に決まる」物:
//   1. 色空間 Gamma → Linear (PlayerSettings.colorSpace)。次の起動でテクスチャが取り込み直される
//   2. Unity のレイヤー名 8 StageUnit・9 StageSet・10 StageFx (コードは定数 HD2DLayers で持つ。名前は付けるだけ)
//   3. Rendering Layers の名前 bit1 Characters・bit2 Environment (TagManager の m_RenderingLayers)
//   4. PC の URP (URP-Default): Rendering Layers を使う・追加ライトの影 (舞台の灯＝スポットがキャラの影を落とす)・
//      月の影のカスケード2 (分割は実行時に P10 が座席の奥行きから決める)。
//      ※ どれも「全部の URP アセットで切られていると、その版のシェーダがビルドで削られる」ので資産の側で入れる。
//      ※ MSAA・深度テクスチャは入れない (旗 Aa・StageLook が実行時に入れる。旧舞台 stage=old を色空間以外で変えないため)
//   5. スマホの URP (URP-Phone＋URP-Phone-Renderer) を作り、品質レベル2 (Android の既定) に割り当てる (§2-7):
//      MSAA なし・月の影 512・スポットの影 512・カスケード1・SSAO なし。細かい詰めは W4 の P31
//   6. Graphics の霧の版を残す (Fog Modes: Automatic → Custom で Linear/Exp/Exp2。Main.unity は霧なしなので Automatic だと削られうる)
// 戻し方は2段 (§0): 受光0 (シェーダ側) と、これの Revert (控えの値に丸ごと戻す)。
//   控え = ProjectSettings/HD2DSetupBackup.json (変えた項目ごとに「元の値」。最初の Apply の値を保つ＝2回 Apply しても元の値は消えない)。
//   作ったアセットは GUID を名前から決める＝Revert → Apply の往復でファイルが1バイトも変わらない。
// 入口 (バッチ): scripts/unity-win.sh prep | revert-prep | checks | build-perf [android|win]
//   -executeMethod DeckRogue.EditorTools.HD2DSetup.Apply / Revert / Checks / BuildPerfAndroid / BuildPerfWindows
//   GUI の Editor ではメニュー「DeckRogue/HD2D/…」。
// ログは「[DeckRogue][HD2DSetup] 」で始まる (unity-win.sh の要約の grep に載る)。Revert が消したファイルは
// 「[DeckRogue][HD2DSetup] deleted: <パス>」の行で出す (unity-win.sh がそれを読んで正本からも消す)。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DeckRogue.EditorTools
{
    public static class HD2DSetup
    {
        public const string BackupPath = "ProjectSettings/HD2DSetupBackup.json";
        const string SettingsDir = "Assets/Settings";
        public const string PcAssetPath = SettingsDir + "/URP-Default.asset";
        public const string PhoneAssetPath = SettingsDir + "/URP-Phone.asset";
        public const string PhoneRendererPath = SettingsDir + "/URP-Phone-Renderer.asset";
        const string TagManagerPath = "ProjectSettings/TagManager.asset";
        const string QualityPath = "ProjectSettings/QualitySettings.asset";
        const string GraphicsPath = "ProjectSettings/GraphicsSettings.asset";
        const string PlayerSettingsPath = "ProjectSettings/ProjectSettings.asset";
        const string ScenePath = "Assets/Scenes/Main.unity";   // BuildTools と同じ
        /// <summary>スマホの品質レベル (Android の既定。P01 の tier=phone も SetQualityLevel(2))</summary>
        public const int PhoneQualityLevel = 2;
        /// <summary>レイヤー名 (HD2DFlags.cs の HD2DLayers と同じ番号)</summary>
        static readonly (int index, string name)[] LayerNames = { (8, "StageUnit"), (9, "StageSet"), (10, "StageFx") };
        /// <summary>Rendering Layers の名前 (HD2DLayers.RenderingCharacters = bit1・RenderingEnvironment = bit2)</summary>
        static readonly (int bit, string name)[] RenderingLayerNames = { (1, "Characters"), (2, "Environment") };
        const string Tag = "[DeckRogue][HD2DSetup] ";

        // ---- 控え ----
        [Serializable]
        class Entry
        {
            public string key;      // "<アセットのパス>|<プロパティのパス>"
            public string asset;
            public string path;
            public string kind;     // bool / int / float / string / objref / arraysize / colorspace
            public string before;   // 最初の Apply の前の値 (Revert はこれに戻す)
            public string after;    // Apply が書いた値
        }

        [Serializable]
        class Backup
        {
            public int version = 1;
            public List<Entry> entries = new List<Entry>();
            public List<string> created = new List<string>();   // Apply が作ったアセット (Revert が消す)
        }

        /// <summary>1回の Apply / Revert の間の状態 (控えと、変えた一覧)</summary>
        class Ctx
        {
            public Backup Bk;
            public readonly List<string> Changes = new List<string>();
            public int Errors;
            public Ctx(Backup bk) { Bk = bk; }

            Entry Find(string key) => Bk.entries.FirstOrDefault(e => e.key == key);

            /// <summary>設定の1項目を書く。record のとき、控えに無ければ元の値を残す (有れば元の値は保つ)</summary>
            public void Set(string assetPath, string propPath, string kind, string value, bool record = true)
            {
                var obj = LoadSettingsObject(assetPath);
                if (obj == null) { Err($"アセットが無い: {assetPath}"); return; }
                var so = new SerializedObject(obj);
                var p = so.FindProperty(propPath);
                if (p == null) { Err($"項目が無い: {assetPath} {propPath}"); return; }
                string cur = Read(p, kind);
                if (record)
                {
                    string key = assetPath + "|" + propPath;
                    var e = Find(key);
                    if (e == null) Bk.entries.Add(new Entry { key = key, asset = assetPath, path = propPath, kind = kind, before = cur, after = value });
                    else e.after = value;
                }
                if (cur == value) return;
                if (!Write(p, kind, value)) { Err($"書けない: {assetPath} {propPath} ({kind}) = {value}"); return; }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(obj);
                Changes.Add($"{assetPath} {propPath}: {cur} → {value}");
            }

            /// <summary>配列の長さを少なくとも n にする (元の長さを控えに残す。Revert は要素を戻した後で長さを戻す)</summary>
            public void ArraySizeAtLeast(string assetPath, string propPath, int n)
            {
                var obj = LoadSettingsObject(assetPath);
                if (obj == null) { Err($"アセットが無い: {assetPath}"); return; }
                var so = new SerializedObject(obj);
                var p = so.FindProperty(propPath);
                if (p == null || !p.isArray) { Err($"配列が無い: {assetPath} {propPath}"); return; }
                int cur = p.arraySize;
                string key = assetPath + "|" + propPath + ".Array.size";
                var e = Find(key);
                int target = Math.Max(cur, n);
                if (e == null) Bk.entries.Add(new Entry { key = key, asset = assetPath, path = propPath, kind = "arraysize", before = cur.ToString(), after = target.ToString() });
                else e.after = target.ToString();
                if (cur >= n) return;
                p.arraySize = n;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(obj);
                Changes.Add($"{assetPath} {propPath}.size: {cur} → {n}");
            }

            public void ColorSpaceTo(ColorSpace cs)
            {
                string key = PlayerSettingsPath + "|colorSpace";
                string cur = PlayerSettings.colorSpace.ToString();
                var e = Find(key);
                if (e == null) Bk.entries.Add(new Entry { key = key, asset = PlayerSettingsPath, path = "colorSpace", kind = "colorspace", before = cur, after = cs.ToString() });
                else e.after = cs.ToString();
                if (PlayerSettings.colorSpace == cs) return;
                PlayerSettings.colorSpace = cs;
                Changes.Add($"PlayerSettings.colorSpace: {cur} → {cs}");
            }

            public void Err(string msg) { Errors++; Debug.LogError(Tag + msg); }
        }

        // ---- 入口 ----
        public static void Apply() => ExitIfBatch(Run("Apply", ApplyCore));
        public static void Revert() => ExitIfBatch(Run("Revert", RevertCore));

        [MenuItem("DeckRogue/HD2D/設定を適用 (prep・Linear へ)")]
        static void ApplyMenu() => Run("Apply", ApplyCore);

        [MenuItem("DeckRogue/HD2D/設定を元に戻す (revert-prep)")]
        static void RevertMenu() => Run("Revert", RevertCore);

        [MenuItem("DeckRogue/HD2D/検査 (checks)")]
        static void ChecksMenu() => RunChecks();

        static int Run(string what, Func<int> body)
        {
            try { return body(); }
            catch (Exception e) { Debug.LogError(Tag + what + " で例外: " + e); return 1; }
        }

        static void ExitIfBatch(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        // ---- Apply ----
        static int ApplyCore()
        {
            var bk = LoadBackup() ?? new Backup();
            var ctx = new Ctx(bk);
            try { ApplySteps(ctx); }
            finally
            {
                // 途中で止まっても、そこまでに変えた項目の元の値は控えに残す (Revert で戻せるように)
                AssetDatabase.SaveAssets();
                if (bk.entries.Count > 0 || bk.created.Count > 0) WriteBackup(bk);
            }
            if (ctx.Errors == 0) VerifyFiles(ctx, applied: true);
            return Finish(ctx, "Apply");
        }

        static void ApplySteps(Ctx ctx)
        {
            var bk = ctx.Bk;
            // 1. 色空間 (§0・§7-1: 戻す時は Revert)
            ctx.ColorSpaceTo(ColorSpace.Linear);

            // 2. レイヤー名 (§3: 名前は prep で付けるだけ)
            foreach (var (index, name) in LayerNames)
            {
                var cur = ReadString(TagManagerPath, $"layers.Array.data[{index}]");
                if (!string.IsNullOrEmpty(cur) && cur != name && !HasEntry(bk, TagManagerPath + $"|layers.Array.data[{index}]"))
                    Debug.LogWarning(Tag + $"レイヤー {index} には既に名前「{cur}」がある。{name} に書き換える (Revert で戻る)");
                ctx.Set(TagManagerPath, $"layers.Array.data[{index}]", "string", name);
            }

            // 3. Rendering Layers の名前 (bit1 Characters・bit2 Environment)
            int needLayers = RenderingLayerNames.Max(r => r.bit) + 1;
            ctx.ArraySizeAtLeast(TagManagerPath, "m_RenderingLayers", needLayers);
            foreach (var (bit, name) in RenderingLayerNames)
                ctx.Set(TagManagerPath, $"m_RenderingLayers.Array.data[{bit}]", "string", name);

            var pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PcAssetPath);
            if (pc == null) { ctx.Err($"PC の URP が無い: {PcAssetPath}"); return; }
            var pcRenderer = RendererOf(pc);
            if (pcRenderer == null) { ctx.Err("PC の URP にレンダラーが無い"); return; }

            // 4. スマホの URP を写して作る (PC の値を変える前のディスクの中身から＝何度やっても同じファイル)
            var phoneRenderer = EnsureCopy(ctx, AssetDatabase.GetAssetPath(pcRenderer), PhoneRendererPath);
            var phone = EnsureCopy(ctx, PcAssetPath, PhoneAssetPath);
            if (phone == null || phoneRenderer == null) return;

            // 5. PC の URP
            ctx.Set(PcAssetPath, "m_SupportsLightLayers", "bool", "1");            // Rendering Layers (ライトごとの影のレイヤー)
            ctx.Set(PcAssetPath, "m_AdditionalLightShadowsSupported", "bool", "1"); // 舞台の灯 (スポット) の影・逆光の影 (PC)
            ctx.Set(PcAssetPath, "m_AnyShadowsSupported", "bool", "1");             // 上の2つから導く値 (SerializedObject では自動で直らない)
            // 月の影は2カスケード (§2-4)。旧舞台の月の影の解像度の配り方も変わる (-hd2dskip cascade で飛ばせる。
            // スマホは1なので、飛ばしても PC=1・スマホ=1 になり _MAIN_LIGHT_SHADOWS_CASCADE の版が削られる＝P10 が実行時に2へ上げても効かない)
            if (!Skipped("cascade")) ctx.Set(PcAssetPath, "m_ShadowCascadeCount", "int", "2");
            else Debug.Log(Tag + "カスケードの設定は飛ばした (-hd2dskip cascade)");

            // 6. スマホの URP の値と品質レベル2
            // 作ったアセットは Revert で丸ごと消すので控えに残さない (record: false)
            ctx.Set(PhoneAssetPath, "m_RendererDataList.Array.data[0]", "objref", ObjRefString(phoneRenderer), false);
            ctx.Set(PhoneAssetPath, "m_DefaultRendererIndex", "int", "0", false);
            ctx.Set(PhoneAssetPath, "m_MSAA", "int", "1", false);                            // MSAA なし (§2-7)
            ctx.Set(PhoneAssetPath, "m_SupportsLightLayers", "bool", "1", false);
            ctx.Set(PhoneAssetPath, "m_MainLightShadowmapResolution", "int", "512", false); // 影 512
            ctx.Set(PhoneAssetPath, "m_AdditionalLightShadowsSupported", "bool", "1", false);// 影つきの光は2つ (月＋スポット)
            ctx.Set(PhoneAssetPath, "m_AdditionalLightsShadowmapResolution", "int", "512", false);
            ctx.Set(PhoneAssetPath, "m_AnyShadowsSupported", "bool", "1", false);
            ctx.Set(PhoneAssetPath, "m_ShadowCascadeCount", "int", "1", false);
            ctx.Set(PhoneAssetPath, "m_RenderScale", "float", "1", false);                   // 解像度は下げない (§2-7)
            SetSsaoActive(ctx, PhoneRendererPath, false);                                    // SSAO は PC だけ (§2-6)

            // 7. 霧の版をビルドで削らない (Graphics の Fog Modes を Automatic → Custom＋Linear/Exp/Exp2 を残す)。
            //    Automatic はビルドに入るシーン (Main.unity は霧なし) から決めるので、実行時に RenderSettings.fog を立てる
            //    舞台 (距離の霧・高さの霧 §2-4) の FOG_* の版が削られうる。残すだけなので描画を壊すことはない
            //    ※ 今のビルドで霧の版が削られていたなら、prep の後は旧舞台 (stage=old) にも Stage.cs の霧が出る。
            //      Linear だけの差で比べたい時は -hd2dskip fog (unity-win.sh の PREP_SKIP=fog) で飛ばせる
            if (!Skipped("fog"))
            {
                ctx.Set(GraphicsPath, "m_FogStripping", "int", "1");
                ctx.Set(GraphicsPath, "m_FogKeepLinear", "bool", "1");
                ctx.Set(GraphicsPath, "m_FogKeepExp", "bool", "1");
                ctx.Set(GraphicsPath, "m_FogKeepExp2", "bool", "1");
            }
            else Debug.Log(Tag + "霧の版の設定は飛ばした (-hd2dskip fog)");

            var names = QualitySettings.names;
            if (names.Length <= PhoneQualityLevel) ctx.Err($"品質レベルが {names.Length} 段しかない (スマホは {PhoneQualityLevel})");
            else
            {
                ctx.Set(QualityPath, $"m_QualitySettings.Array.data[{PhoneQualityLevel}].customRenderPipeline", "objref", ObjRefString(phone));
                SetPerPlatformQuality(ctx, "Android", PhoneQualityLevel);
                Debug.Log(Tag + $"品質レベル{PhoneQualityLevel}「{names[PhoneQualityLevel]}」に {PhoneAssetPath} を割り当てた");
            }
        }

        // ---- Revert ----
        static int RevertCore()
        {
            var bk = LoadBackup();
            if (bk == null) { Debug.Log(Tag + "控えが無い (prep していない)。何もしない"); return 0; }
            var ctx = new Ctx(bk);
            // 書いた順の逆に戻す (配列の長さは要素の後で戻る)
            for (int i = bk.entries.Count - 1; i >= 0; i--)
            {
                var e = bk.entries[i];
                if (e.kind == "colorspace")
                {
                    if (Enum.TryParse<ColorSpace>(e.before, out var cs) && PlayerSettings.colorSpace != cs)
                    {
                        ctx.Changes.Add($"PlayerSettings.colorSpace: {PlayerSettings.colorSpace} → {cs}");
                        PlayerSettings.colorSpace = cs;
                    }
                    continue;
                }
                if (e.kind == "arraysize")
                {
                    var obj = LoadSettingsObject(e.asset);
                    var so = obj != null ? new SerializedObject(obj) : null;
                    var p = so?.FindProperty(e.path);
                    if (p == null || !p.isArray) { ctx.Err($"配列が無い: {e.asset} {e.path}"); continue; }
                    if (int.TryParse(e.before, out var n) && p.arraySize != n)
                    {
                        ctx.Changes.Add($"{e.asset} {e.path}.size: {p.arraySize} → {n}");
                        p.arraySize = n;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(obj);
                    }
                    continue;
                }
                // 要素の書き戻し。配列の外に落ちた要素 (後で長さを縮める所) も一度書いてから縮める
                ctx.Set(e.asset, e.path, e.kind, e.before, false);
            }
            AssetDatabase.SaveAssets();
            if (ctx.Errors > 0)
            {
                // 戻しきれなかった: 作ったアセットも控えも残す (参照の切れた品質レベルを作らない・もう一度 Revert できる)
                Debug.LogError(Tag + "戻しきれない項目があったので、作ったアセットと控えは消さない");
                return Finish(ctx, "Revert");
            }

            // 作ったアセットを消す (品質レベルの参照を戻した後)
            for (int i = bk.created.Count - 1; i >= 0; i--)
            {
                var path = bk.created[i];
                if (!File.Exists(path)) continue;
                if (AssetDatabase.DeleteAsset(path)) { Debug.Log(Tag + "deleted: " + path); ctx.Changes.Add("削除 " + path); }
                else ctx.Err("消せない: " + path);
            }
            if (File.Exists(BackupPath))
            {
                File.Delete(BackupPath);
                Debug.Log(Tag + "deleted: " + BackupPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            VerifyFiles(ctx, applied: false);
            return Finish(ctx, "Revert");
        }

        static int Finish(Ctx ctx, string what)
        {
            foreach (var c in ctx.Changes) Debug.Log(Tag + "変更 " + c);
            if (what == "Apply")
                Debug.Log(Tag + $"{what} 終わり: 変更 {ctx.Changes.Count} 件・prep が作ったアセット {ctx.Bk.created.Count} 個・控え {BackupPath} ({ctx.Bk.entries.Count} 項目)・エラー {ctx.Errors} 件");
            else
                Debug.Log(Tag + $"{what} 終わり: 変更 {ctx.Changes.Count} 件・エラー {ctx.Errors} 件");
            return ctx.Errors > 0 ? 1 : 0;
        }

        // ---- スマホのアセットを作る (GUID を名前から決める＝往復で同じファイル) ----
        static Object EnsureCopy(Ctx ctx, string srcPath, string dstPath)
        {
            string srcName = Path.GetFileNameWithoutExtension(srcPath);
            string dstName = Path.GetFileNameWithoutExtension(dstPath);
            if (File.Exists(dstPath))
            {
                if (!ctx.Bk.created.Contains(dstPath))
                    Debug.LogWarning(Tag + $"{dstPath} は既にある (prep が作った物ではない)。そのまま使う。Revert では消さない");
                return AssetDatabase.LoadMainAssetAtPath(dstPath);
            }
            if (!File.Exists(srcPath) || !File.Exists(srcPath + ".meta")) { ctx.Err("写す元が無い: " + srcPath); return null; }
            string guid = StableGuid("DeckRogue/HD2D/" + dstName);
            var other = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(other) && other != dstPath) { ctx.Err($"GUID {guid} が {other} と重なる"); return null; }
            var utf8 = new UTF8Encoding(false);
            // 本体: 主オブジェクトの名前だけ書き換える (レンダラーの中の「SSAO」などの子は触らない)
            var text = File.ReadAllText(srcPath, utf8);
            var replaced = Regex.Replace(text, @"(?m)^([ \t]*m_Name:[ \t]*)" + Regex.Escape(srcName) + @"(?=[ \t]*\r?$)", "${1}" + dstName);
            if (replaced == text) { ctx.Err($"{srcPath} に m_Name: {srcName} が無い"); return null; }
            File.WriteAllText(dstPath, replaced, utf8);
            // meta: GUID だけ差し替える
            var meta = File.ReadAllText(srcPath + ".meta", utf8);
            var meta2 = Regex.Replace(meta, @"(?m)^guid:[ \t]*[0-9a-fA-F]{32}(?=[ \t]*\r?$)", "guid: " + guid);
            if (meta2 == meta) { ctx.Err($"{srcPath}.meta に guid の行が無い"); File.Delete(dstPath); return null; }
            File.WriteAllText(dstPath + ".meta", meta2, utf8);
            AssetDatabase.ImportAsset(dstPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var obj = AssetDatabase.LoadMainAssetAtPath(dstPath);
            if (obj == null) { ctx.Err("取り込めない: " + dstPath); return null; }
            if (!ctx.Bk.created.Contains(dstPath)) ctx.Bk.created.Add(dstPath);
            ctx.Changes.Add($"作成 {dstPath} ({srcPath} の写し・guid {guid})");
            return obj;
        }

        static string StableGuid(string seed)
        {
            using (var md5 = MD5.Create())
            {
                var h = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
                var sb = new StringBuilder(32);
                foreach (var b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        static ScriptableRendererData RendererOf(UniversalRenderPipelineAsset asset)
        {
            var p = new SerializedObject(asset).FindProperty("m_RendererDataList");
            if (p == null || !p.isArray || p.arraySize == 0) return null;
            return p.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
        }

        static void SetSsaoActive(Ctx ctx, string rendererPath, bool active)
        {
            bool found = false;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(rendererPath))
            {
                if (!(o is ScriptableRendererFeature f) || f.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                found = true;
                var so = new SerializedObject(f);
                var p = so.FindProperty("m_Active");
                if (p == null) { ctx.Err("SSAO の m_Active が無い"); continue; }
                if (p.boolValue == active) continue;
                p.boolValue = active;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(f);
                ctx.Changes.Add($"{rendererPath} SSAO.m_Active: {!active} → {active}");
            }
            if (!found) Debug.Log(Tag + $"{rendererPath} に SSAO は無い (何もしない)");
        }

        /// <summary>品質の「プラットフォームごとの既定」(map) の1つを書く。見つからなければ警告だけ</summary>
        static void SetPerPlatformQuality(Ctx ctx, string platform, int level)
        {
            var obj = LoadSettingsObject(QualityPath);
            var p = obj != null ? new SerializedObject(obj).FindProperty("m_PerPlatformDefaultQuality") : null;
            if (p == null || !p.isArray) { Debug.LogWarning(Tag + "m_PerPlatformDefaultQuality が読めない (Android の既定の品質は確かめていない)"); return; }
            for (int i = 0; i < p.arraySize; i++)
            {
                var el = p.GetArrayElementAtIndex(i);
                var first = el.FindPropertyRelative("first");
                if (first == null || first.propertyType != SerializedPropertyType.String || first.stringValue != platform) continue;
                ctx.Set(QualityPath, $"m_PerPlatformDefaultQuality.Array.data[{i}].second", "int", level.ToString());
                return;
            }
            Debug.LogWarning(Tag + $"m_PerPlatformDefaultQuality に {platform} が無い (足していない)");
        }

        // ---- 値の読み書き ----
        static Object LoadSettingsObject(string assetPath)
        {
            if (assetPath.StartsWith("ProjectSettings/", StringComparison.Ordinal))
            {
                var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                return all != null && all.Length > 0 ? all[0] : null;
            }
            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        static string ReadString(string assetPath, string propPath)
        {
            var obj = LoadSettingsObject(assetPath);
            var p = obj != null ? new SerializedObject(obj).FindProperty(propPath) : null;
            return p != null && p.propertyType == SerializedPropertyType.String ? p.stringValue : null;
        }

        static bool HasEntry(Backup bk, string key) => bk.entries.Any(e => e.key == key);

        static string Read(SerializedProperty p, string kind)
        {
            switch (kind)
            {
                case "bool": return p.boolValue ? "1" : "0";
                case "int": return p.intValue.ToString();
                case "float": return p.floatValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                case "string": return p.stringValue ?? "";
                case "objref": return ObjRefString(p.objectReferenceValue);
                default: return "";
            }
        }

        static bool Write(SerializedProperty p, string kind, string v)
        {
            switch (kind)
            {
                case "bool": p.boolValue = v == "1" || v == "true" || v == "True"; return true;
                case "int":
                    if (!int.TryParse(v, out var i)) return false;
                    p.intValue = i; return true;   // 列挙 (ShadowResolution・MsaaQuality) も intValue は中身の値
                case "float":
                    if (!float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f)) return false;
                    p.floatValue = f; return true;
                case "string": p.stringValue = v ?? ""; return true;
                case "objref":
                    if (!TryResolveObjRef(v, out var o)) return false;
                    p.objectReferenceValue = o; return true;
                default: return false;
            }
        }

        /// <summary>参照を「guid:<GUID>;fid:<ローカルID>」の文字で持つ (控えに書ける形)。null は "null"</summary>
        static string ObjRefString(Object o)
        {
            if (o == null) return "null";
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long fid)) return $"guid:{guid};fid:{fid}";
            return "null";
        }

        static bool TryResolveObjRef(string v, out Object o)
        {
            o = null;
            if (string.IsNullOrEmpty(v) || v == "null") return true;
            var m = Regex.Match(v, @"^guid:([0-9a-fA-F]{32});fid:(-?\d+)$");
            if (!m.Success) return false;
            var path = AssetDatabase.GUIDToAssetPath(m.Groups[1].Value);
            if (string.IsNullOrEmpty(path)) return false;
            long fid = long.Parse(m.Groups[2].Value);
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out _, out long f2) && f2 == fid) { o = a; return true; }
            o = AssetDatabase.LoadMainAssetAtPath(path);
            return o != null;
        }

        static Backup LoadBackup()
        {
            if (!File.Exists(BackupPath)) return null;
            try { return JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath, Encoding.UTF8)); }
            catch (Exception e) { Debug.LogError(Tag + "控えが読めない: " + e.Message); return null; }
        }

        static void WriteBackup(Backup bk)
        {
            // 時刻などは書かない (Revert → Apply の往復で同じファイルになるように)
            File.WriteAllText(BackupPath, JsonUtility.ToJson(bk, true) + "\n", new UTF8Encoding(false));
        }

        /// <summary>保存した設定ファイルを読み直して確かめる (SaveAssets が ProjectSettings を書かなかった時に気付くため)</summary>
        static void VerifyFiles(Ctx ctx, bool applied)
        {
            void Expect(string path, string needle, bool present)
            {
                string t = File.Exists(path) ? File.ReadAllText(path) : "";
                if (t.Contains(needle) != present) ctx.Err($"保存の確認に失敗: {path} に「{needle}」が{(present ? "無い" : "残っている")}");
            }
            if (applied)
            {
                Expect(PlayerSettingsPath, "m_ActiveColorSpace: 1", true);
                Expect(TagManagerPath, "StageUnit", true);
                Expect(TagManagerPath, "Characters", true);
                Expect(PcAssetPath, "m_SupportsLightLayers: 1", true);
                Expect(PcAssetPath, "m_AdditionalLightShadowsSupported: 1", true);
                Expect(QualityPath, StableGuid("DeckRogue/HD2D/URP-Phone"), true);
            }
            else
            {
                var cse = ctx.Bk.entries.FirstOrDefault(e => e.kind == "colorspace");
                if (cse != null && Enum.TryParse<ColorSpace>(cse.before, out var csBefore) && csBefore != ColorSpace.Uninitialized)
                    Expect(PlayerSettingsPath, "m_ActiveColorSpace: " + (csBefore == ColorSpace.Linear ? 1 : 0), true);
                Expect(TagManagerPath, "StageUnit", false);
                Expect(QualityPath, StableGuid("DeckRogue/HD2D/URP-Phone"), false);
                if (File.Exists(PhoneAssetPath)) ctx.Err("消えていない: " + PhoneAssetPath);
            }
        }

        // ---- checks ----
        /// <summary>
        /// W1 以降の統合の「checks」: シェーダの実変換 (P03 の HD2DShaderCheck) と箱庭の Check (P04) を1回の起動で回す。
        /// 相手の名前は反射で探す (並列のレーンが同時に書くので型で縛らない)。探す名前:
        ///   箱庭 = DioramaLayoutTool.{Check|RunCheck|Checks|Validate} → Diorama.{Check|Checks|Validate}
        ///   シェーダ = HD2DShaderCheck.{Check|CheckAll|RunChecks|Validate|Run}
        /// 起動引数 -hd2dchecks "型.関数;型.関数" で差し替えられる。int を返せば 0 が合格、bool なら true が合格、
        /// void なら例外もエラーのログも無ければ合格。結果の物 (DioramaStats) は bool の Ok で合否・Summary() を記録に出す。
        /// int 1つを取る関数には幕 1 を渡す。Diorama.Check は「組んである箱庭を数える」ので、先に一時の親へ幕1を組んでから呼ぶ。
        /// どの検査もエラーのログ (LogType.Error/Exception) が1件でも出たら不合格。
        /// void の Run は自分で EditorApplication.Exit するかもしれないので最後に回す。見つからない検査は不合格。
        /// </summary>
        public static void Checks() => ExitIfBatch(RunChecks());

        static int RunChecks()
        {
            var plan = new List<(string label, MethodInfo m)>();
            int failures = 0;
            var over = GetArg("-hd2dchecks");
            if (!string.IsNullOrEmpty(over))
            {
                foreach (var spec in over.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var s = spec.Trim();
                    int dot = s.LastIndexOf('.');
                    var m = dot > 0 ? FindStaticMethod(s.Substring(0, dot), new[] { s.Substring(dot + 1) }) : null;
                    if (m == null) { Debug.LogError(Tag + "checks: 見つからない " + s); failures++; }
                    else plan.Add((s, m));
                }
            }
            else
            {
                var dio = FindStaticMethod("DioramaLayoutTool", new[] { "Check", "RunCheck", "Checks", "Validate" })
                       ?? FindStaticMethod("Diorama", new[] { "Check", "Checks", "Validate" });
                var sh = FindStaticMethod("HD2DShaderCheck", new[] { "Check", "CheckAll", "RunChecks", "Validate", "Run" });
                if (dio == null) { Debug.LogError(Tag + "checks: 箱庭の Check が見つからない (DioramaLayoutTool / Diorama)"); failures++; }
                else plan.Add(("箱庭", dio));
                if (sh == null) { Debug.LogError(Tag + "checks: HD2DShaderCheck が見つからない"); failures++; }
                else plan.Add(("シェーダ", sh));
            }
            // 自分で終了するかもしれない void の関数を後ろへ
            plan = plan.OrderBy(x => x.m.ReturnType == typeof(void) && x.m.Name == "Run" ? 1 : 0).ToList();
            foreach (var (label, m) in plan)
            {
                int errs = 0;
                void OnLog(string msg, string st, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errs++; }
                Application.logMessageReceived += OnLog;
                bool ok;
                string detail = "";
                Action cleanup = null;
                try
                {
                    var ps = m.GetParameters();
                    object[] args = ps.Length == 0 ? null : ps.Length == 1 && ps[0].ParameterType == typeof(int) ? new object[] { 1 } : null;
                    if (ps.Length > 0 && args == null) throw new Exception($"引数の形が分からない ({string.Join(",", ps.Select(p => p.ParameterType.Name))})");
                    // Diorama.Check は「いま組んである箱庭を数える」ので、先に幕1を組む (約束の形 Diorama.Build(act, parent, look)・StageLook.Load(act))
                    if (m.DeclaringType.Name == "Diorama" && ps.Length == 0) cleanup = BuildDioramaForCheck(m.DeclaringType);
                    Debug.Log(Tag + $"checks: {label} = {m.DeclaringType.FullName}.{m.Name} を回す");
                    var r = m.Invoke(null, args);
                    if (r is int i) { ok = i == 0; detail = "戻り値 " + i; }
                    else if (r is bool b) { ok = b; detail = "戻り値 " + b; }
                    else if (r != null)
                    {
                        // 結果の物 (DioramaStats など): bool の Ok があれば合否に使い、Summary() があれば記録に出す
                        var rt = r.GetType();
                        var okP = rt.GetProperty("Ok", BindingFlags.Public | BindingFlags.Instance);
                        ok = okP == null || okP.PropertyType != typeof(bool) || (bool)okP.GetValue(r);
                        var sum = rt.GetMethod("Summary", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                        detail = sum != null && sum.ReturnType == typeof(string) ? (string)sum.Invoke(r, null) : r.ToString();
                    }
                    else ok = true;
                }
                catch (TargetInvocationException e) { ok = false; detail = "例外 " + (e.InnerException ?? e); }
                catch (Exception e) { ok = false; detail = "例外 " + e; }
                finally
                {
                    try { cleanup?.Invoke(); } catch (Exception e) { Debug.LogWarning(Tag + "checks: 片付けで例外 " + e.Message); }
                    Application.logMessageReceived -= OnLog;
                }
                if (errs > 0) { ok = false; detail += $" (エラーのログ {errs} 件)"; }
                if (!ok) failures++;
                Debug.Log(Tag + $"checks: {label} {(ok ? "合格" : "不合格")} {detail}");
            }
            Debug.Log(Tag + $"checks 終わり: {plan.Count} 件のうち不合格 {failures} 件");
            return failures > 0 ? 1 : 0;
        }

        /// <summary>checks の前に幕1の箱庭を一時の親の下に組む。戻り値は片付け (Diorama.Clear と親の破棄)。組めなければ null</summary>
        static Action BuildDioramaForCheck(Type diorama)
        {
            const BindingFlags F = BindingFlags.Static | BindingFlags.Public;
            var build = diorama.GetMethods(F).FirstOrDefault(x => x.Name == "Build" && x.GetParameters().Length == 3
                && x.GetParameters()[0].ParameterType == typeof(int) && x.GetParameters()[1].ParameterType == typeof(Transform));
            if (build == null) { Debug.LogWarning(Tag + "checks: Diorama.Build(int, Transform, look) が無いので組まずに数える"); return null; }
            var lookType = FindType("StageLook");
            var load = lookType?.GetMethod("Load", F, null, new[] { typeof(int) }, null);
            object look = load != null ? load.Invoke(null, new object[] { 1 }) : null;
            var go = new GameObject("HD2DChecks-Diorama") { hideFlags = HideFlags.DontSave };
            build.Invoke(null, new object[] { 1, go.transform, look });
            var clear = diorama.GetMethod("Clear", F, null, Type.EmptyTypes, null);
            return () =>
            {
                clear?.Invoke(null, null);
                if (go != null) Object.DestroyImmediate(go);
            };
        }

        static Type FindType(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] ts;
                try { ts = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { ts = e.Types.Where(t => t != null).ToArray(); }
                catch { continue; }
                var hit = ts.FirstOrDefault(t => t.Name == typeName && (t.Namespace ?? "").StartsWith("DeckRogue", StringComparison.Ordinal));
                if (hit != null) return hit;
            }
            return null;
        }

        static MethodInfo FindStaticMethod(string typeName, string[] methodNames)
        {
            const BindingFlags F = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var types = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] ts;
                try { ts = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { ts = e.Types.Where(t => t != null).ToArray(); }
                catch { continue; }
                foreach (var t in ts)
                    if (t.FullName == typeName || t.Name == typeName) types.Add(t);
            }
            // 同じ名前の型が複数なら DeckRogue の名前空間を先に
            foreach (var t in types.OrderBy(t => (t.Namespace ?? "").StartsWith("DeckRogue", StringComparison.Ordinal) ? 0 : 1))
                foreach (var n in methodNames)
                {
                    var m = t.GetMethods(F).FirstOrDefault(x => x.Name == n && !x.IsGenericMethodDefinition
                        && (x.GetParameters().Length == 0 || (x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(int))));
                    if (m != null) return m;
                }
            return null;
        }

        /// <summary>prep で飛ばす項目 (起動引数 -hd2dskip "fog,cascade")</summary>
        static bool Skipped(string item)
        {
            var s = GetArg("-hd2dskip");
            return !string.IsNullOrEmpty(s) && s.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x.Trim() == item);
        }

        static string GetArg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }

        // ---- 計測用のビルド (build-perf) ----
        /// <summary>
        /// 計測用 APK = Build/DeckRogue-perf.apk。applicationId は com.deckrogue.proto.perf (ユーザーのセーブのある本体と別のアプリ＝消さない)、
        /// 表示名「DeckRogue Perf」、Frame Timing Stats を入れる (FrameTimingManager で GPU 時間を取るため)。
        /// ビルドの後で ID・表示名・Frame Timing Stats は元に戻す (unity-win.sh が ProjectSettings を正本へ書き戻すので、残すと本体の APK が変わる)。
        /// 手順書は W4 の P31 (docs/design/hd2d-slice/perf-runbook.md)。
        /// </summary>
        public static void BuildPerfAndroid() => ExitIfBatch(BuildPerf(BuildTarget.Android));

        /// <summary>PC の計測用 = Build/perf-win/DeckRogue.exe (Frame Timing Stats あり・表示名「DeckRogue Perf」＝ユーザーの exe と persistentDataPath を分ける)</summary>
        public static void BuildPerfWindows() => ExitIfBatch(BuildPerf(BuildTarget.StandaloneWindows64));

        static int BuildPerf(BuildTarget target)
        {
            int code = 0;
            bool android = target == BuildTarget.Android;
            var nbt = android ? UnityEditor.Build.NamedBuildTarget.Android : UnityEditor.Build.NamedBuildTarget.Standalone;
            string oldId = PlayerSettings.GetApplicationIdentifier(nbt);
            string oldName = PlayerSettings.productName;
            bool oldFts = PlayerSettings.enableFrameTimingStats;
            try
            {
                BuildTools.EnsureScene();
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                if (android)
                {
                    // BuildTools.BuildAndroid と同じ (ID だけ別)
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                    PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                    PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    PlayerSettings.Android.forceSDCardPermission = false;
                    EditorUserBuildSettings.buildAppBundle = false;
                    PlayerSettings.SetApplicationIdentifier(nbt, "com.deckrogue.proto.perf");
                }
                PlayerSettings.productName = "DeckRogue Perf";
                PlayerSettings.enableFrameTimingStats = true;
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = android ? "Build/DeckRogue-perf.apk" : "Build/perf-win/DeckRogue.exe",
                    target = target,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(opts);
                var s = report.summary;
                Debug.Log(Tag + $"perf build ({target}): result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} size={s.totalSize / (1024 * 1024)}MB time={s.totalTime.TotalSeconds:F0}s → {s.outputPath}");
                if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) code = 1;
            }
            catch (Exception e)
            {
                Debug.LogError(Tag + "perf build で例外: " + e);
                code = 1;
            }
            finally
            {
                if (android) PlayerSettings.SetApplicationIdentifier(nbt, oldId);
                PlayerSettings.productName = oldName;
                PlayerSettings.enableFrameTimingStats = oldFts;
                AssetDatabase.SaveAssets();
            }
            return code;
        }
    }
}
