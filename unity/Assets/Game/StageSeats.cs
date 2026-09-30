// StageSeats.cs — Stage の座席 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3)。
// P02 (W1) で Stage.cs から移した部分 (中身は1文字も変えていない): 座席の足元の高さの登録簿 (_feetOffsets)・LeaderSlot・EnemySlots・
// 座席の足元の光溜まり (SyncSeatPools)・DollSlots・からくりの匣 (SetKarakuriBox)・SetFeetOffset/FeetOffset。
// TryGetSeat・SeatDepthRange の中身は P10 が書く。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        /// <summary>
        /// 座席 (key = "player"・"enemy0"…・"doll:&lt;uid&gt;") の足元の世界の点と、その深さでの k (画面 1px あたりの unit)。
        /// 無ければ false。骨組み: P10 が書く
        /// </summary>
        public static bool TryGetSeat(string key, out Vector3 world, out float k)
        {
            world = default; k = 0f;
            return false;
        }

        /// <summary>座席の帯の深さの範囲 (カメラからの距離。手前 near・奥 far)。ぼかしの帯に渡す。骨組み: P10 が書く (まだ無ければ 0・0)</summary>
        public static void SeatDepthRange(out float near, out float far)
        {
            near = 0f; far = 0f;
        }

        static readonly Dictionary<string, float> _feetOffsets = new Dictionary<string, float>();

        public static Vector3 LeaderSlot() { return OnPath(-5.0f, 0.9f); }

        /// <summary>敵は道に沿って奥右へ (本家の3/4ジオラマの対角線の隊列)。横に少しずらして一直線を崩す</summary>
        public static Vector3[] EnemySlots(int n)
        {
            n = Math.Max(1, n);
            var r = new Vector3[n];
            // 3体・4体は画面上の足元の間隔がほぼ等しくなるよう奥の席を広げる (2026-09-29 戦闘画面のレビュー p02。奥ほど遠近で詰まるので t は等間隔にしない):
            // PC 4体 1126/1342/1563/1786 (間隔 216/221/223)・スマホ 4体 157/161/154・PC 3体 260/276。先頭 (t=1.6/2.2) は人形の列 (DollSlots「敵① t≥1.6」) の約束で動かさない
            float[] t = n == 1 ? new[] { 4.6f } : n == 2 ? new[] { 3.2f, 7.6f } : n == 3 ? new[] { 2.2f, 5.9f, 9.4f } : n == 4 ? new[] { 1.6f, 4.6f, 7.15f, 11.2f } : null;
            // 横のずらしは小さく (2026-09-15 スマホ・2026-09-29 PC の3体以上も): ずらしが大きいと奇数の席が画面上で左へ寄り、間隔が 200/296/126 と偏って帳面と意図の札が重なる。PC の2体は今のまま
            float sB = (UiKit.Phone || n >= 3) ? 0.1f : 0.7f;
            for (int i = 0; i < n; i++)
            {
                // 5体以上 (苔の産み手の戦闘で倒れた敵が一覧に残る) は同じ範囲を等分して埋める (表の長さは4 = 範囲外エラーを塞ぐ)
                float ti = t != null ? t[i] : 1.6f + 9.6f * i / (n - 1);
                r[i] = OnPath(ti, (i % 2 == 0) ? -0.5f : sB);
            }
            SyncSeatPools(r);   // 座席の足元の光溜まり (F50)。BattleView.SyncField が組み直しのたびにここを通る
            return r;
        }

        // ---- 座席の足元の光溜まり (2026-09-30 F50 ユーザー裁定「地面を3か所明るく」) ----
        // 画面の画素の約7割が暗 (60未満) で中間の群 (60〜169) が12〜13%しかなく、紙の UI が黒い画面に貼った札に見えた (I24)。
        // 空・樹冠・露出は触らず、中間の群を舞台の地面で作る (ほかの2か所は BuildTerrain の道の帯と BattleView の desk-shade)。
        // 敵の座席の足元に、光溜まり (moon-pool と同じ寒色の放射状の暈を地面に寝かせる) を置く。半径は隣の座席までの距離の 0.6 倍
        // (隣の暈とは縁の薄いところで触れるだけ)。寒色 = 「暖色は街灯の範囲だけ」の外。リーダーの足元は moon-pool-hero が先にある。
        // 座席は戦闘ごとに敵の数で変わるので、EnemySlots が呼ばれるたびに数と幕が変わっていれば置き直す
        static readonly Color SeatPoolColor = new Color(0.8f, 0.88f, 1f, 0.12f);
        const float SeatPoolRadius = 0.6f;
        static readonly List<GameObject> _seatPools = new List<GameObject>();
        static int _seatPoolN = -1, _seatPoolAct = -1;
        static bool _seatPoolPhone;
        static Texture2D _seatPoolTex;

        static void SyncSeatPools(Vector3[] seats)
        {
            if (_world == null) return;
            bool same = _seatPoolN == seats.Length && _seatPoolAct == _paintedAct && _seatPoolPhone == UiKit.Phone && _seatPools.Count == seats.Length;
            if (same) foreach (var old in _seatPools) if (old == null) { same = false; break; }
            if (same) return;
            foreach (var old in _seatPools)
            {
                if (old == null) continue;
                var omr = old.GetComponent<MeshRenderer>();
                if (omr != null) UnityEngine.Object.Destroy(omr.sharedMaterial);
                UnityEngine.Object.Destroy(old);
            }
            _seatPools.Clear();
            if (_seatPoolTex == null) _seatPoolTex = Px.Radial(SeatPoolColor);
            for (int i = 0; i < seats.Length; i++)
            {
                float gap = float.MaxValue;
                for (int j = 0; j < seats.Length; j++)
                {
                    if (j == i) continue;
                    float dx = seats[j].x - seats[i].x, dz = seats[j].z - seats[i].z;
                    gap = Mathf.Min(gap, Mathf.Sqrt(dx * dx + dz * dz));
                }
                if (gap == float.MaxValue) gap = 4.4f;   // 1体 (幕ボス) は2体の時の座席の間隔 (t 3.2→7.6) で
                float dia = 2f * SeatPoolRadius * gap;
                var p = seats[i];
                var pool = Glow("seat-pool", _seatPoolTex, new Vector3(p.x, GroundY(p.x, p.z) + 0.035f, p.z), 1f, 1f);
                pool.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                pool.transform.localScale = new Vector3(dia, dia, 1f);
                pool.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 2999;   // 接地影 (GlowMaterial の 3000) より先に描く = 足元の影を明るく抜かない
                _seatPools.Add(pool);
            }
            _seatPoolN = seats.Length; _seatPoolAct = _paintedAct; _seatPoolPhone = UiKit.Phone;
        }

        /// <summary>人形 (白の従者) の座席 (2026-09-19 人形の盤面表示・案A「灯りの列」→ ユーザー「B との中間」= ひなたのすぐ前から):
        /// 点灯した順に、ひなた (t=-5) と敵① (t≥1.6) の間の道に並ぶ。前列5体 (t=-3.9…-0.5・0.85 刻み・奥と手前を交互に。PC の1体目だけは灯籠の下を避けて手前) ＋ 後列4体 (一歩奥・半歩右)。
        /// 2026-09-30 F45 (ユーザー裁定「列を少し右へ」): 旧 t=-4.1…-0.7 では1体目 (PC x483〜567) の兜が灯籠の下枠に 3〜5px 重なり、袖の角に接し、裾との間が 0〜3px で、
        /// ひなたと1体目がひとつの塊に見えた → 列ごと t+0.2 (画面で約 +22px)。灯籠の下に立つこと自体は「灯りの列」の結果なので残す。
        /// 敵4体の時の5体目 (t=-0.5) と敵① (t=1.6) の絵の端の間は PC 69→51px・スマホ 105→87px (射影の計算。足元の札と帳面は ArrangeDollTags が別に解く)。
        /// からくりの匣は PC ではリーダーの左奥 (t=-6.3・s=1.7) に置くので、人形の列 (t≥-3.9) とは重ならない (2026-09-29 戦闘画面のレビュー p13)。
        /// スマホの匣はリーダーの足元の真ん前 (t=-5.3・s=-1.9。2026-09-30 F46) で、人形の列とは重ならない (左奥は自分の札にもぐるため。SetKarakuriBox の注記)。
        /// 上限9 = 超えた分は BattleView が最後の札に「+N」</summary>
        public static Vector3[] DollSlots(int n)
        {
            n = Math.Max(0, Math.Min(n, 9));
            var r = new Vector3[n];
            float[] t = { -3.9f, -3.05f, -2.2f, -1.35f, -0.5f };   // 0.85 刻み (0.7 は PC で 128px の人形が 73px 間隔に詰まりすぎた)。F45 で列ごと +0.2
            for (int i = 0; i < n; i++)
            {
                int j = i % 5; bool back = i >= 5;
                // PC は1体目だけ手前の道 (s 0.25) に立つ (2026-09-30 F45 の続き)。奥 (s 0.9) のままでは t+0.2 で兜が灯籠の中心の真下に来て
                // (PC 灯籠 x518〜578・下端 y497／兜の上端 y487)、人形が灯籠をかぶった1体に見えた。手前なら足元 (550,610)→(560,626) で兜の上端 503、
                // 灯籠がいちばん下がる待機のコマ (一枚絵と同じ) でも 6px 空く。2体目からの奥と手前の交互はそのまま
                // (交互ごと入れ替えると 1-2体目・3-4体目の間が 78・65px に詰まり、剣が盾の人形に・犬が聖歌の人形に掛かった)。
                // スマホは奥のまま: 1体目はもう灯籠の右 (灯籠 x483〜517／人形 x525〜) にいて、手前へ出すと足元の札が横に並べず盾の人形の札がその体の上へ押し上げられた
                bool nearFirst = i == 0 && !UiKit.Phone;
                float s = (nearFirst ? 0.25f : j % 2 == 0 ? 0.9f : 0.25f) + (back ? 1.15f : 0f);
                r[i] = OnPath(t[j] + (back ? 0.15f : 0f), s);
            }
            return r;
        }

        /// <summary>UI の入れ物の下端から足元までの高さ (敵ごとに違う。名前札や HP バーは入れ物の下端基準で同じ線に揃う)</summary>
        // ---- からくりの匣 (2026-09-10 世界観「からくりだけ実物」): リーダーの足元に置く小さな木の匣。仕込むと蓋が開き、動かすと閃く ----
        static GameObject _box; static Texture2D _boxClosed, _boxOpen; static int _boxShown = -1; static GameObject _boxGlow, _boxBlob; static bool _boxLeftRear;
        /// <summary>仕込み札の枚数に合わせて匣の蓋を開閉する。fired=true なら一度閃く (動かした)。
        /// leftRear=true ならリーダーの左奥 (t=-6.3・s=1.7)、false ならリーダーの右手前 (t=-3.7・s=-0.75)。
        /// 置き場は BattleView.BoxLeftRear (PC なら左奥・スマホは右手前) で渡す＝戦闘の途中で動かない。
        /// (2026-09-29 戦闘画面のレビュー p13: PC の右手前は自分の札 (y640) に下端と接地影が隠れ、白では人形の2体目の足元を隠していた)</summary>
        public static void SetKarakuriBox(int setCount, bool fired, bool leftRear)
        {
            Ensure();
            if (_box == null || _box.transform.parent != _world || _boxLeftRear != leftRear)
            {
                // 置き場が変わった (別のリーダー・別の端末で撮り直した) 時は、匣・閃き・接地影を作り直す
                if (_box != null) UnityEngine.Object.Destroy(_box);
                if (_boxGlow != null) UnityEngine.Object.Destroy(_boxGlow);
                if (_boxBlob != null) UnityEngine.Object.Destroy(_boxBlob);
                _boxClosed = Px.KarakuriBox(false); _boxOpen = Px.KarakuriBox(true);
                // 左奥 (PC): リーダー (t=-5, s=0.9) の左奥 = 自分の札 (y642) より上で足元と影が収まり (下端 ≈614)、下の札の「からくり」区画の真上に来る。人形の列 (t≥-3.9) とも重ならない。
                // 右手前 (スマホ): スマホの自分の札 (x≤325・上端 y382) はリーダーの足元 (y≈405) より上まで来る。左奥は s=1.7 で下端 ≈404 (札に 22px もぐる)、
                // 奥へ逃がしても幕1の地面がそこで沈む (高さ −0.2〜−0.3) ので s=2.2〜3.6 のどれも札の縁に接した (2026-09-29 撮影で確認)。右手前は切れていない
                // スマホはリーダーの足元の真ん前 (2026-09-30 F46: 旧・右手前 (−3.7,−0.75) は白の人形の2体目の脚を隠し、剣の人形の札が匣に乗った。
                // 足元の前は3幕とも平らに均した場の内側で、自分の札・人形の札・手札・確認の窓のどれにも掛からない。人形の数では動かさない)
                var pos = leftRear ? OnPath(-6.3f, 1.7f) : OnPath(-5.3f, -1.9f);
                _box = Plane("karakuri-box", _boxClosed, pos + new Vector3(0f, 0.01f, 0f), 0.62f, 0.5f, false);
                _boxBlob = Blob("shadow-karakuri-box", _world, pos, 0.62f * _boxClosed.width / (float)_boxClosed.height);
                _boxGlow = Glow("karakuri-glow", Px.Glow(new Color(0.55f, 1f, 0.9f, 0.6f)), pos + new Vector3(0f, 0.45f, -0.15f), 1.6f, 1.6f);
                _boxGlow.SetActive(false);
                _boxShown = -1;
                _boxLeftRear = leftRear;
            }
            bool open = setCount > 0;
            if (_boxShown != (open ? 1 : 0))
            {
                var mr = _box.GetComponent<MeshRenderer>();
                var tex = open ? _boxOpen : _boxClosed;
                mr.sharedMaterial.SetTexture("_BaseMap", tex); mr.sharedMaterial.mainTexture = tex;
                _boxShown = open ? 1 : 0;
            }
            if (fired && _boxGlow != null)
            {
                _boxGlow.SetActive(true);
                var t = _boxGlow.transform; var s0 = new Vector3(1.6f, 1.6f, 1f);
                Tween.Run(0.45f, k => { if (_boxGlow == null || t == null) return; t.localScale = s0 * (1f + k * 0.9f); if (k >= 1f) _boxGlow.SetActive(false); }, Ease.OutQuad);
            }
        }

        public static void SetFeetOffset(string key, float y) { _feetOffsets[key] = y; }
        public static float FeetOffset(string key, float fallback) { float y; return _feetOffsets.TryGetValue(key, out y) ? y : fallback; }
    }
}
