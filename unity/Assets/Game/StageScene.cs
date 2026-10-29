// StageScene.cs — 戦闘以外の画面の「場面」(品質スプリント #3 2026-10-29 `docs/design/quality-sprint-2026-10-28.md` §10)。
// 夜の森の箱庭 (Stage.Paint) の上に、画面が置いた UI の矩形に追従する板 (BindUnit) で焚き火・丸太・座る主人公などの小物を立て、火の光を付ける。
// 小物の板は key が "scene-" で始まる (BindUnit が props/anim のコマを引き、呼吸はしない)。矩形が消えれば板も消える (StageUnit の自壊)
using UnityEngine;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        /// <summary>
        /// 場面の小物の座席 (BindUnit の前に呼ぶ): 道の座標 (t=道なり・s=横。LeaderSlot は (-5.0, 0.9)・s が大きいほど奥) の地面の点を世界の座席として登録し、
        /// その足元の画面の位置 (キャンバスの単位) を返す。矩形の足元をその点に置けば、箱庭の板は座席の世界の点に立つ (戦闘の座席→ProjectFeet→BindUnit と同じ経路。
        /// 画面の矩形だけで置くと箱庭のカメラと合わず、板が画面の外に出る)
        /// </summary>
        public static Vector2 SceneSeat(string key, float t, float s)
        {
            Ensure();
            var w = OnPath(t, s);
            return ProjectFeet(key, w);
        }

        /// <summary>key の板 (unit-key) に点光源を付ける (焚き火・灯)。yFrac は板の高さに対する高さ (0=足元・1=頭)。前に置いた光は捨てる</summary>
        public static void AttachLight(string key, Color color, float intensity, float range, float yFrac = 0.5f, float flicker = 0.12f)
        {
            Ensure();
            if (_units == null) return;
            var t = _units.Find("unit-" + key);
            if (t == null) return;
            var old = t.Find("scene-light");
            if (old != null) Object.Destroy(old.gameObject);
            var go = new GameObject("scene-light");
            go.transform.SetParent(t, false);
            go.transform.localPosition = new Vector3(0f, yFrac - 0.5f, -0.25f);   // 板の中心が原点 (quad は中心基準)。板の手前に少し出す
            var li = go.AddComponent<Light>();
            li.type = LightType.Point; li.range = range; li.intensity = intensity; li.color = color; li.shadows = LightShadows.None;
            if (flicker > 0f) { var f = go.AddComponent<SceneFlicker>(); f.Base = intensity; f.Amount = flicker; }
        }

        /// <summary>key の板に光の暈 (加算の円。火の明かりが地面と空気に滲む)。size は世界の単位。板の子なので板と一緒に消える</summary>
        public static void AttachGlow(string key, Color color, float size, float yFrac = 0.4f)
        {
            Ensure();
            if (_units == null) return;
            var t = _units.Find("unit-" + key);
            if (t == null) return;
            var old = t.Find("scene-glow");
            if (old != null) Object.Destroy(old.gameObject);
            var g = Glow("scene-glow", Px.Glow(color), Vector3.zero, size, size);
            g.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
            g.transform.SetParent(t, false);
            var ls = t.localScale;
            g.transform.localScale = new Vector3(size / Mathf.Max(0.001f, ls.x), size / Mathf.Max(0.001f, ls.y), 1f);
            g.transform.localPosition = new Vector3(0f, yFrac - 0.5f, -0.2f);
        }

        /// <summary>火の揺らぎ: 強さを 2 つの周期のゆっくりした波で ±Amount (決定的な撮影では Tween.UnscaledDt で進む)</summary>
        public class SceneFlicker : MonoBehaviour
        {
            public float Base = 3f, Amount = 0.12f;
            float _t; Light _l;
            void Awake() { _l = GetComponent<Light>(); _t = (transform.position.x * 3.7f) % 5f; }
            void Update()
            {
                if (_l == null) return;
                _t += Tween.UnscaledDt;
                float w = 0.6f * Mathf.Sin(_t * 7.3f) + 0.4f * Mathf.Sin(_t * 2.1f + 1.3f);
                _l.intensity = Base * (1f + Amount * w);
            }
        }
    }
}
