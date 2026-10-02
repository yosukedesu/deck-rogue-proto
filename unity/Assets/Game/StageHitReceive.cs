// StageHitReceive.cs — 技の光にだけ強く受ける口 (段2 2026-10-03・計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 S・M)。
// 約束 (docs/design/hd2d-stage2/contracts.md §C1-6): 印を付けた光 (技の光) の当たりだけを、材質の _HitReceive 倍で受ける (既定 1 = 今と同じ)。
// 舞台の灯の二重の陰影を避けるため、舞台の灯・月・逆光・lights[] には印を付けない。印を付けるのはレーン M (技の光のプール StageFx)。
//
// 印の仕組み (レーン S が決めた・2026-10-03): シェーダのグローバルの小さな配列。
//   _HD2DHitLights[8] (float4: xyz = 印の付いた光の世界の位置・w = 1 なら有効)・_HD2DHitLightCount (有効な数。0 = 使わない)。
//   StageModule (地形・大物・半立体・札) の Forward は、_HitReceive ≠ 1 かつ数 > 0 の時だけ、点光源のループ (Forward+ の cluster と通常の両方) で
//   光の位置 (URP の _AdditionalLightsPosition) をこの配列と比べ (差の2乗 < 1e-4)、一致した光の寄与を別にも数えて (_HitReceive − 1) 倍を足す。
//   _HitReceive = 1 (既定) か印が 0 個なら新しい行は何もしない = 今と同じ式・同じ順 (幕1 の見本・今の舞台は1画素も変わらない)。
// URP の光の rendering layer の bit を使わない理由: このプロジェクトの URP (17.6) は光の層を「TagManager に名前のある層」(Default・Characters・Environment の 3 bit)
//   に切る (RenderingLayerUtils.ToValidRenderingLayers) ので、新しい bit は ProjectSettings に層を足さないと届かない。既にある bit の組で印を作ると
//   「その光がどの物を照らすか」まで変わる (全部の物が bit0 を持つ)。位置で見分けるので、技の光は他の光と同じ場所 (1 cm 以内) に置かないこと。
// 位置は毎フレームの描く前 (RenderPipelineManager.beginContextRendering) に押す (技の光は当たりの点へ動く・消えた光は外す)。
// 頂点の光の版 (_ADDITIONAL_LIGHTS_VERTEX) では分けられない (効かない)。両方の URP アセットは Forward+ なので画素の版を通る。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game
{
    public static class StageHitReceive
    {
        /// <summary>同時に印を付けられる光の数 (シェーダの HD2D_HIT_LIGHTS_MAX と同じ。技の光のプールは StageFx の maxLights ≦ 8)</summary>
        public const int MaxMarked = 8;

        static readonly List<Light> _marked = new List<Light>();
        static readonly Vector4[] _buf = new Vector4[MaxMarked];
        static readonly int IdLights = Shader.PropertyToID("_HD2DHitLights");
        static readonly int IdCount = Shader.PropertyToID("_HD2DHitLightCount");
        static bool _hooked;
        static int _lastPushed;

        /// <summary>光 l に「技の光」の印を付ける (on=true) か外す。印の付いた光は材質の _HitReceive 倍で受ける。レーン M が技の光のプールに使う</summary>
        public static void Mark(Light l, bool on)
        {
            if (l == null) return;
            int i = _marked.IndexOf(l);
            if (on)
            {
                if (i < 0)
                {
                    if (_marked.Count >= MaxMarked) { Debug.LogWarning("[StageHitReceive] 印は " + MaxMarked + " 個まで (" + l.name + " は付けない)"); return; }
                    _marked.Add(l);
                }
                Hook(true);
            }
            else if (i >= 0) _marked.RemoveAt(i);
            Push();
        }

        /// <summary>光 l に印が付いているか</summary>
        public static bool IsMarked(Light l) { return l != null && _marked.Contains(l); }

        /// <summary>印を付けた光の数 (消えた光・無効な光も数える。シェーダへ押すのは有効な物だけ)</summary>
        public static int Count { get { return _marked.Count; } }

        /// <summary>印を全部外す (場面を捨てる時など)。シェーダの数は 0 に戻る</summary>
        public static void Clear()
        {
            _marked.Clear();
            Push();
        }

        /// <summary>dumplayout の記録 (Diorama の extra.diorama.stage2.hitReceive)。印の数・最後にシェーダへ押した数・方式</summary>
        public static Dictionary<string, object> DebugInfo()
        {
            var names = new List<string>();
            foreach (var l in _marked) names.Add(l != null ? l.name : "(消えた光)");
            return new Dictionary<string, object>
            {
                ["mode"] = "globalArray", ["marked"] = _marked.Count, ["pushed"] = _lastPushed, ["names"] = names, ["hooked"] = _hooked,
            };
        }

        /// <summary>有効な印の光の位置をシェーダの配列へ押す。印が 0 個になったら数 0 を書いて描く前の呼び出しを外す</summary>
        static void Push()
        {
            int n = 0;
            for (int i = _marked.Count - 1; i >= 0; i--) if (_marked[i] == null) _marked.RemoveAt(i);   // 場面ごと消えた光 (Unity の null)
            for (int i = 0; i < _marked.Count && n < MaxMarked; i++)
            {
                var l = _marked[i];
                if (!l.isActiveAndEnabled) continue;   // 消えている光は URP が渡さない = 比べなくてよい
                var p = l.transform.position;
                _buf[n++] = new Vector4(p.x, p.y, p.z, 1f);
            }
            for (int i = n; i < MaxMarked; i++) _buf[i] = Vector4.zero;
            // 配列の長さは最初に押した長さで決まる (Unity の決まり) = いつも MaxMarked で押す
            Shader.SetGlobalVectorArray(IdLights, _buf);
            Shader.SetGlobalFloat(IdCount, n);
            _lastPushed = n;
            if (_marked.Count == 0) Hook(false);
        }

        static void Hook(bool on)
        {
            if (on == _hooked) return;
            if (on) RenderPipelineManager.beginContextRendering += OnBeginContext;
            else RenderPipelineManager.beginContextRendering -= OnBeginContext;
            _hooked = on;
        }

        static void OnBeginContext(ScriptableRenderContext ctx, List<Camera> cams) { Push(); }

        /// <summary>プレイの始まり (ドメインの読み直しなしでも) に印と呼び出しを捨てる。シェーダの数は 0 = 使わない</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (_hooked) RenderPipelineManager.beginContextRendering -= OnBeginContext;
            _hooked = false;
            _marked.Clear();
            _lastPushed = 0;
            Shader.SetGlobalFloat(IdCount, 0f);
        }
    }
}
