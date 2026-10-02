// DioramaLayoutTool.cs — 箱庭の設計図を目で詰める道具 (2026-09-30 HD-2D 見本 P04。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-2)。
//  メニュー「DeckRogue/箱庭/幕1を組む」 = 開いているシーンに、部品を1つずつの GameObject (名前 "p012:rock:名前") で組む (保存されない)。
//          GUI の Editor で部品を動かし・回し・大きさを変えてから「書き戻す」と、Assets/Resources/Stage/act1_layout.json の
//          その行の t・s・y・yaw・scale だけが書き換わる (段 slab・額縁 frame は書き戻さない = JSON を直す)。
//  メニュー「DeckRogue/箱庭/片付ける」 = 組んだ物を捨てる。「DeckRogue/箱庭/点検」 = 溶かして組み、数を出して捨てる。
//  バッチ: -executeMethod DeckRogue.EditorTools.DioramaLayoutTool.CheckBatch [-dioramaAct 1] [-dioramaCheckOut <path.json>]
//          ログに "[Diorama] check parts=… result=OK|NG" を出し、終了コード 0 = 門を満たす / 1 = 満たさない / 2 = 設計図が無い・例外。
//          scripts/unity-win.sh の checks (P06) から呼ぶ想定。
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using DeckRogue.Game;

namespace DeckRogue.EditorTools
{
    public static class DioramaLayoutTool
    {
        const string EditRootName = "diorama-edit";

        [MenuItem("DeckRogue/箱庭/幕1を組む")]
        public static void BuildAct1() { BuildForEdit(1); }

        [MenuItem("DeckRogue/箱庭/幕2を組む")]
        public static void BuildAct2() { BuildForEdit(2); }   // 段2 (2026-10-03)

        [MenuItem("DeckRogue/箱庭/幕3を組む")]
        public static void BuildAct3() { BuildForEdit(3); }

        [MenuItem("DeckRogue/箱庭/書き戻す")]
        public static void WriteBackMenu()
        {
            int n = WriteBack(out var msg);
            EditorUtility.DisplayDialog("箱庭の書き戻し", msg, "OK");
            Debug.Log("[Diorama] 書き戻し: " + msg + " (" + n + " 行)");
        }

        [MenuItem("DeckRogue/箱庭/片付ける")]
        public static void ClearMenu()
        {
            Diorama.Clear();
            var old = GameObject.Find(EditRootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }

        [MenuItem("DeckRogue/箱庭/点検")]
        public static void CheckMenu()
        {
            var st = RunCheck(1);
            EditorUtility.DisplayDialog("箱庭の点検", st != null ? st.Summary() + (st.Missing.Count > 0 ? "\n見つからない: " + string.Join(", ", st.Missing) : "") : "設計図が無い", "OK");
        }

        /// <summary>部品ごとの GameObject で組む (溶かさない・StaticBatching もしない)</summary>
        public static void BuildForEdit(int act)
        {
            ClearMenu();
            var holder = new GameObject(EditRootName) { hideFlags = HideFlags.DontSave };
            Diorama.Build(act, holder.transform, StageLook.Load(act), new DioramaBuildOptions { Merge = false, StaticBatch = false });
            if (Diorama.Root == null) { UnityEngine.Object.DestroyImmediate(holder); return; }
            Selection.activeGameObject = Diorama.Root.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        /// <summary>
        /// 組んだ部品の Transform を設計図へ書き戻す。書き換えるのは位置 (t・s・y)・yaw・scale だけ (変わった行だけ)。
        /// 戻り値 = 書き換えた行の数。msg = 結果の一文
        /// </summary>
        public static int WriteBack(out string msg)
        {
            var layout = Diorama.Layout;
            if (layout == null || Diorama.Root == null) { msg = "先に「幕1を組む」で組んでください"; return 0; }
            var parts = layout.Raw["parts"] as JArray;
            if (parts == null) { msg = "設計図に parts が無い"; return 0; }
            int changed = 0, skipped = 0;
            var st = Diorama.Root.Find("static");
            if (st == null) { msg = "組んだ部品が見つからない"; return 0; }
            for (int i = 0; i < st.childCount; i++)
            {
                var tr = st.GetChild(i);
                int idx = Diorama.PartIndexFromName(tr.name);
                if (idx < 0 || idx >= parts.Count) continue;
                var o = parts[idx] as JObject;
                if (o == null) continue;
                string kind = (string)o["kind"];
                if (kind == "slab" || kind == "frame") { skipped++; continue; }
                var pos = Diorama.Root.InverseTransformPoint(tr.position);
                Diorama.ToPath(pos.x, pos.z, out var t, out var s);
                bool abs = o["abs"] != null && o["abs"].Type == JTokenType.Boolean && (bool)o["abs"];
                float y = abs ? pos.y : pos.y - Diorama.HeightAtPath(t, s);
                float worldYaw = (Quaternion.Inverse(Diorama.Root.rotation) * tr.rotation).eulerAngles.y;
                bool pathAligned = Diorama.IsPathAligned(kind);   // 段2: rail・arch・pillar も道に沿う部品 (Diorama の判定と同じ)
                float yaw = Norm180(pathAligned ? worldYaw - layout.PathYaw : worldYaw);
                float scale = Mathf.Abs(tr.localScale.x);
                bool any = false;
                any |= SetNum(o, "t", t, 0f);
                any |= SetNum(o, "s", s, 0f);
                any |= SetNum(o, "y", y, 0f);
                any |= SetNum(o, "yaw", yaw, 0f);
                any |= SetNum(o, "scale", scale, 1f);
                if (any) changed++;
            }
            if (changed > 0)
            {
                var path = Path.Combine(Application.dataPath, "Resources", Diorama.LayoutResource(layout.Act) + ".json");
                File.WriteAllText(path, Format(layout.Raw), new UTF8Encoding(false));
                AssetDatabase.ImportAsset("Assets/Resources/" + Diorama.LayoutResource(layout.Act) + ".json");
            }
            msg = changed + " 行を書き換えた" + (skipped > 0 ? " (段・額縁 " + skipped + " 行は書き戻さない)" : "");
            return changed;
        }

        static float Norm180(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a <= -180f) a += 360f;
            return a;
        }

        /// <summary>値が 0.001 以上変わった時だけ書く。def と同じで元に無いキーは足さない</summary>
        static bool SetNum(JObject o, string key, float v, float def)
        {
            float r = (float)Math.Round(v, 3);
            var cur = o[key];
            float old = cur != null && (cur.Type == JTokenType.Float || cur.Type == JTokenType.Integer) ? cur.Value<float>() : def;
            if (Mathf.Abs(old - r) < 0.001f) return false;
            if (cur == null && Mathf.Abs(r - def) < 0.001f) return false;
            o[key] = r;
            return true;
        }

        /// <summary>設計図の書式: 上の段は字下げ2、parts は1行に1部品 (差分が読みやすい。scratchpad の生成スクリプトと同じ形)</summary>
        public static string Format(JObject root)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            int i = 0, n = root.Count;
            foreach (var kv in root)
            {
                sb.Append("  ").Append(JsonConvert.ToString(kv.Key)).Append(": ");
                if (kv.Key == "parts" && kv.Value is JArray arr)
                {
                    sb.Append("[\n");
                    for (int k = 0; k < arr.Count; k++)
                        sb.Append("    ").Append(arr[k].ToString(Formatting.None)).Append(k < arr.Count - 1 ? ",\n" : "\n");
                    sb.Append("  ]");
                }
                else
                {
                    var txt = kv.Value.ToString(Formatting.Indented).Replace("\r\n", "\n").Replace("\n", "\n  ");
                    sb.Append(txt);
                }
                sb.Append(++i < n ? ",\n" : "\n");
            }
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>溶かして組み、数えて捨てる (エディタでもバッチでも)</summary>
        public static DioramaStats RunCheck(int act)
        {
            var layout = Diorama.LoadLayout(act);
            if (layout == null) return null;
            var holder = new GameObject("diorama-check") { hideFlags = HideFlags.DontSave };
            try
            {
                Diorama.Build(act, holder.transform, StageLook.Load(act), new DioramaBuildOptions { Merge = true, StaticBatch = false, Layout = layout });
                var st = Diorama.LastStats ?? Diorama.Check();
                return st;
            }
            finally
            {
                Diorama.Clear();
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        /// <summary>バッチの入口 (scripts/unity-win.sh checks)。引数 -dioramaAct N・-dioramaCheckOut path</summary>
        public static void CheckBatch()
        {
            int code;
            try
            {
                int act = 1;
                string outPath = null;
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-dioramaAct") int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out act);
                    if (args[i] == "-dioramaCheckOut") outPath = args[i + 1];
                }
                var st = RunCheck(act);
                if (st == null) { Debug.LogError("[Diorama] check 設計図が無い: Resources/" + Diorama.LayoutResource(act)); code = 2; }
                else
                {
                    Debug.Log("[Diorama] " + st.Summary());
                    foreach (var m in st.Missing) Debug.Log("[Diorama] check missing " + m);
                    foreach (var m in st.IntrusionNames) Debug.Log("[Diorama] check seat-intrusion " + m);
                    if (!string.IsNullOrEmpty(outPath)) File.WriteAllText(outPath, st.ToJson(), new UTF8Encoding(false));
                    code = st.Ok ? 0 : 1;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Diorama] check で例外: " + e);
                code = 2;
            }
            Debug.Log("[Diorama] check 終了 code=" + code);
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
