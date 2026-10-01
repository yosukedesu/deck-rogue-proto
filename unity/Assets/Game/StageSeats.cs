// StageSeats.cs — Stage の座席 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3)。
// P02 (W1) で Stage.cs から移した部分: 座席の足元の高さの登録簿 (_feetOffsets)・LeaderSlot・EnemySlots・
// 座席の足元の光溜まり (SyncSeatPools)・DollSlots・からくりの匣 (SetKarakuriBox)・SetFeetOffset/FeetOffset。
// P10 (W2) が書いた部分: TryGetSeat・SeatDepthRange・見本 (stage=diorama) の座席の表 (画角ごとの敵と人形の t)。
//  ・見本の座席の t は docs/design/hd2d-slice/seatfit.json (P08 の scripts/hd2d-seatfit.py) の表を写し、画角 22/28/36 の間は線形に補間する
//    (見下ろし 10° の表との差は t で 敵 0.011・人形 0.02 以下 = 画面で 2px ほどなので見下ろしでは分けない)。今の舞台 (stage=old) の t は今のまま。
//  ・見本では座席の足元の光溜まり (F50・今の舞台の地面を明るくする板) を置かない (光は StageLook の舞台の灯。③の座席の帯のむらを増やすため)。
// 二周目 (2026-10-01 レーン A・計画 docs/design/hd2d-round2-plan-2026-10-01.md K1・K13):
//  ・カメラ 22°・見下ろし PC 5°／スマホ 7°・足元の線 PC 0.407／スマホ 0.525 で座席の表を解き直した (scripts/hd2d-seatfit.py・seatfit.md「二周目のカメラ」)。
//    22° の行との差は敵の t で 0.03 未満 = 表はそのまま (差は seatfit.md に)。
//  ・人形の後列は見本だけ t +0.45・s +2.0 (前列の間へ・奥へ)。今の舞台は +0.15・+1.15 のまま (R2A_DollBackDt・R2A_DollBackDs)。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        /// <summary>座席の世界の点 (key → ProjectFeet に渡された足元の世界の点)。ProjectFeet が書く</summary>
        static readonly Dictionary<string, Vector3> _seatWorld = new Dictionary<string, Vector3>();

        /// <summary>
        /// 座席 (key = "player"・"enemy0"…・"doll:&lt;uid&gt;") の足元の世界の点と、その深さでの k (画面 1px あたりの unit)。
        /// 世界の点 = 最後の組み直しで ProjectFeet に渡された足元 (大きい人形は2席の間)。"player" だけはまだ写していなくても LeaderSlot を返す。
        /// k はレイアウト用のカメラ (揺れと寄りと漂いを含まない) で今の深さから計算する (= StageUnit の _k × Depth ÷ _dist と同じ式)。
        /// カメラがまだ無い・知らない key・カメラの後ろなら false
        /// </summary>
        public static bool TryGetSeat(string key, out Vector3 world, out float k)
        {
            world = default; k = 0f;
            if (key == null || !_laidOut) return false;
            Vector3 w;
            if (!_seatWorld.TryGetValue(key, out w))
            {
                if (key != "player") return false;
                w = LeaderSlot();
            }
            float d = Vector3.Dot(w - _camBase, _fwd);
            if (!(d > 0.01f)) return false;
            world = w;
            k = _k * d / _dist;
            return true;
        }

        /// <summary>
        /// 座席の帯の深さの範囲 (カメラからの距離。手前 near・奥 far)。ぼかしの帯 (StageLook.SetSeatBand) と影のカスケードの分割に渡す。
        /// 帯 = 座りうる全部の席 (リーダー・敵1〜4体の表・人形9体・からくりの匣の2か所) = 戦闘ごとに焦点が動かない。
        /// レイアウト用のカメラ (揺れと寄りと漂いを含まない) の深さ。カメラがまだ無ければ 0・0
        /// </summary>
        public static void SeatDepthRange(out float near, out float far)
        {
            near = 0f; far = 0f;
            if (!_laidOut) return;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in SeatBandPoints())
            {
                float d = Vector3.Dot(p - _camBase, _fwd);
                if (d < lo) lo = d;
                if (d > hi) hi = d;
            }
            if (lo < hi) { near = lo; far = hi; }
        }

        /// <summary>座りうる全部の席の足元 (帯の深さ・漂いの幅・dumplayout 用。光溜まりは置かない)</summary>
        static List<Vector3> SeatBandPoints()
        {
            var pts = new List<Vector3>(24) { LeaderSlot() };
            for (int n = 1; n <= 4; n++) pts.AddRange(EnemySeats(n));
            pts.AddRange(DollSlots(9));
            pts.Add(OnPath(-6.3f, 1.7f));    // からくりの匣 (PC = リーダーの左奥。SetKarakuriBox と同じ)
            pts.Add(OnPath(-5.3f, -1.9f));   // からくりの匣 (スマホ = 足元の真ん前)
            // 見本のスマホの匣 (R2A_BoxPhoneDiorama・直しの輪1) は足さない: 深さは帯の内側だが、点の重心が漂いの軸 (ComputeDrift) を動かすため
            return pts;
        }

        // ---- 見本 (stage=diorama) の座席の表 (2026-09-30 P10。docs/design/hd2d-slice/seatfit.json の fovs[].enemyT・dolls.step) ----
        // 条件 (P08 の seatfit): 2〜4体の足元が画面で等間隔 (差 ±5%)・いちばん右の敵の絵と帳面が画面の右端 −16 の内側・先頭の t は人形の列の約束で今のまま。
        // 人形は1体目の t を今のまま、刻みを伸ばして画面の間隔を今 (36°) 以上に。足元の線は PC 0.42・スマホ 0.52 (GroundLineRatio) で解いた値。
        // 二周目 (2026-10-01): 22° の行を PC 22°・5°・0.407／スマホ 22°・7°・0.525・P20 の UI で解き直すと、敵の t の差は最大 0.024 (4体の4体目)・
        // 人形の刻みの差は PC 0.005・スマホ 0 = 表はそのまま (画面で 2〜4px。seatfit.md「二周目のカメラ」)
        static readonly float[] SeatFovs = { 22f, 28f, 36f };
        // 1行 = 1つの画角 (22・28・36)。並びは 1体 | 2体 | 3体 | 4体 (n 体の最初の添字 = (n−1)n/2)
        static readonly float[] SeatEnemyPc =
        {
            4.459f,  3.2f, 7.055f,  2.2f, 5.708f, 8.741f,  1.6f, 4.417f, 6.716f, 10.01f,
            4.517f,  3.2f, 7.276f,  2.2f, 5.831f, 9.005f,  1.6f, 4.492f, 6.863f, 10.404f,
            4.6f,    3.2f, 7.6f,    2.2f, 6.009f, 9.4f,    1.6f, 4.599f, 7.079f, 10.999f,
        };
        static readonly float[] SeatEnemyPhone =
        {
            4.431f,  3.2f, 7.02f,   2.2f, 5.651f, 8.617f,  1.6f, 4.387f, 6.645f, 9.994f,
            4.5f,    3.2f, 7.254f,  2.2f, 5.796f, 8.928f,  1.6f, 4.476f, 6.819f, 10.469f,
            4.6f,    3.2f, 7.6f,    2.2f, 6.007f, 9.4f,    1.6f, 4.605f, 7.077f, 11.2f,
        };
        static readonly float[] SeatDollStepPc = { 0.93f, 0.895f, 0.85f };
        static readonly float[] SeatDollStepPhone = { 0.945f, 0.905f, 0.85f };
        const float SeatDollFirstT = -3.9f;

        /// <summary>画角 fov の表の行 (a・b) と補間の割合 u。22 未満・36 超は端の行</summary>
        static void SeatFovRow(float fov, out int a, out int b, out float u)
        {
            int last = SeatFovs.Length - 1;
            if (!(fov > SeatFovs[0])) { a = b = 0; u = 0f; return; }
            if (fov >= SeatFovs[last]) { a = b = last; u = 0f; return; }
            for (int i = 0; i < last; i++)
            {
                if (fov <= SeatFovs[i + 1]) { a = i; b = i + 1; u = (fov - SeatFovs[i]) / (SeatFovs[i + 1] - SeatFovs[i]); return; }
            }
            a = b = last; u = 0f;
        }

        /// <summary>見本の敵の t (n 体・今の画角と端末)。5体以上は4体の表の先頭〜末尾を等分</summary>
        static float[] DioramaEnemyT(int n)
        {
            if (n > 4)
            {
                var t4 = DioramaEnemyT(4);
                var r5 = new float[n];
                for (int i = 0; i < n; i++) r5[i] = t4[0] + (t4[3] - t4[0]) * i / (n - 1);
                return r5;
            }
            var tbl = UiKit.Phone ? SeatEnemyPhone : SeatEnemyPc;
            int a, b; float u;
            SeatFovRow(CurrentFov, out a, out b, out u);
            int off = (n - 1) * n / 2;
            var r = new float[n];
            for (int i = 0; i < n; i++)
            {
                float va = tbl[a * 10 + off + i], vb = tbl[b * 10 + off + i];
                r[i] = va + (vb - va) * u;
            }
            return r;
        }

        /// <summary>見本の人形の前列の t (5つ)。1体目は今のまま・刻みは表から</summary>
        static float[] DioramaDollT()
        {
            var steps = UiKit.Phone ? SeatDollStepPhone : SeatDollStepPc;
            int a, b; float u;
            SeatFovRow(CurrentFov, out a, out b, out u);
            float step = steps[a] + (steps[b] - steps[a]) * u;
            var r = new float[5];
            for (int j = 0; j < 5; j++) r[j] = SeatDollFirstT + j * step;
            return r;
        }

        static readonly Dictionary<string, float> _feetOffsets = new Dictionary<string, float>();

        public static Vector3 LeaderSlot() { return OnPath(-5.0f, 0.9f); }

        /// <summary>敵は道に沿って奥右へ (本家の3/4ジオラマの対角線の隊列)。横に少しずらして一直線を崩す</summary>
        public static Vector3[] EnemySlots(int n)
        {
            var r = EnemySeats(n);
            if (DioramaCamera) ClearSeatPools();   // 見本では光溜まりを置かない (光は StageLook の舞台の灯)
            else SyncSeatPools(r);   // 座席の足元の光溜まり (F50)。BattleView.SyncField が組み直しのたびにここを通る
            return r;
        }

        /// <summary>敵の座席 (副作用なし。EnemySlots・座席の帯が使う)</summary>
        static Vector3[] EnemySeats(int n)
        {
            n = Math.Max(1, n);
            var r = new Vector3[n];
            // 3体・4体は画面上の足元の間隔がほぼ等しくなるよう奥の席を広げる (2026-09-29 戦闘画面のレビュー p02。奥ほど遠近で詰まるので t は等間隔にしない):
            // PC 4体 1126/1342/1563/1786 (間隔 216/221/223)・スマホ 4体 157/161/154・PC 3体 260/276。先頭 (t=1.6/2.2) は人形の列 (DollSlots「敵① t≥1.6」) の約束で動かさない
            float[] t = n == 1 ? new[] { 4.6f } : n == 2 ? new[] { 3.2f, 7.6f } : n == 3 ? new[] { 2.2f, 5.9f, 9.4f } : n == 4 ? new[] { 1.6f, 4.6f, 7.15f, 11.2f } : null;
            if (DioramaCamera) t = DioramaEnemyT(n);   // 見本: 画角ごとの座席の表 (seatfit)。5体以上も表から
            // 横のずらしは小さく (2026-09-15 スマホ・2026-09-29 PC の3体以上も): ずらしが大きいと奇数の席が画面上で左へ寄り、間隔が 200/296/126 と偏って帳面と意図の札が重なる。PC の2体は今のまま
            float sB = (UiKit.Phone || n >= 3) ? 0.1f : 0.7f;
            for (int i = 0; i < n; i++)
            {
                // 5体以上 (苔の産み手の戦闘で倒れた敵が一覧に残る) は同じ範囲を等分して埋める (表の長さは4 = 範囲外エラーを塞ぐ)
                float ti = t != null ? t[i] : 1.6f + 9.6f * i / (n - 1);
                r[i] = OnPath(ti, (i % 2 == 0) ? -0.5f : sB);
            }
            return r;
        }

        /// <summary>座席の足元の光溜まりを片付ける (見本では置かない。今の舞台へ戻れば次の SyncSeatPools が置き直す)</summary>
        static void ClearSeatPools()
        {
            if (_seatPools.Count == 0 && _seatPoolN < 0) return;
            foreach (var old in _seatPools)
            {
                if (old == null) continue;
                var omr = old.GetComponent<MeshRenderer>();
                if (omr != null) UnityEngine.Object.Destroy(omr.sharedMaterial);
                UnityEngine.Object.Destroy(old);
            }
            _seatPools.Clear();
            _seatPoolN = -1;
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
        /// 上限9 = 超えた分は BattleView が最後の札に「+N」。見本 (stage=diorama) は前列の刻みを画角ごとの表から (DioramaDollT)、
        /// 後列のずらしは t +0.45・s +2.0 (二周目 K13。見本のスマホは +0.5・+2.8 = 直しの輪1。今の舞台は +0.15・+1.15)。副作用なし</summary>
        public static Vector3[] DollSlots(int n)
        {
            n = Math.Max(0, Math.Min(n, 9));
            var r = new Vector3[n];
            float[] t = { -3.9f, -3.05f, -2.2f, -1.35f, -0.5f };   // 0.85 刻み (0.7 は PC で 128px の人形が 73px 間隔に詰まりすぎた)。F45 で列ごと +0.2
            if (DioramaCamera) t = DioramaDollT();   // 見本: 画角ごとの刻み (seatfit。画面の間隔を今以上に保つ。1体目は −3.9 のまま)
            for (int i = 0; i < n; i++)
            {
                int j = i % 5; bool back = i >= 5;
                // PC は1体目だけ手前の道 (s 0.25) に立つ (2026-09-30 F45 の続き)。奥 (s 0.9) のままでは t+0.2 で兜が灯籠の中心の真下に来て
                // (PC 灯籠 x518〜578・下端 y497／兜の上端 y487)、人形が灯籠をかぶった1体に見えた。手前なら足元 (550,610)→(560,626) で兜の上端 503、
                // 灯籠がいちばん下がる待機のコマ (一枚絵と同じ) でも 6px 空く。2体目からの奥と手前の交互はそのまま
                // (交互ごと入れ替えると 1-2体目・3-4体目の間が 78・65px に詰まり、剣が盾の人形に・犬が聖歌の人形に掛かった)。
                // スマホは奥のまま: 1体目はもう灯籠の右 (灯籠 x483〜517／人形 x525〜) にいて、手前へ出すと足元の札が横に並べず盾の人形の札がその体の上へ押し上げられた
                bool nearFirst = i == 0 && !UiKit.Phone;
                float s = (nearFirst ? 0.25f : j % 2 == 0 ? 0.9f : 0.25f) + (back ? R2A_DollBackDs : 0f);
                r[i] = OnPath(t[j] + (back ? R2A_DollBackDt : 0f), s);
            }
            return r;
        }

        // ---- 人形の後列のずらし (二周目 2026-10-01 レーン A・計画 K13) ----
        // 見本 (stage=diorama) の新しいカメラ (22°・5°) は低いので、後列を今の +0.15・+1.15 のまま奥へ置いても画面の縦にほとんど開かない
        // (後列の足元が前列より 13〜22px 上にしか出ず、幅 128px の人形の頭が前列の頭に重なる)。→ 後列を前列の間 (t +0.45 = 刻み約0.93 の半分) へ寄せ、
        // 奥へ s +2.0 下げる: 後列の頭がいちばん近い前列の頭より 21〜32px 上・11〜23px 横 (seatfit.md「二周目のカメラ」の人形の表)。
        // 試しのビルドの人形9体 (T-A-dolls) で、後列の頭がいちばん近い前列の頭より 20px 以上上に出なければ今の値 (+0.15・+1.15) へ戻す (計画 K13)。
        // 今の舞台 (stage=old) は +0.15・+1.15 のまま (1画素も変えない)
        const float R2A_DollBackDtDiorama = 0.45f, R2A_DollBackDsDiorama = 2.0f;
        const float R2A_DollBackDtOld = 0.15f, R2A_DollBackDsOld = 1.15f;
        // 直しの輪1 (2026-10-01): 見本のスマホ (22°・7°・足元 0.525) は人形が横に詰まり、後列 (+0.45・+2.0) の足元の札が前列の札と3組重なった
        // (ArrangeDollTags は1段しか上へ逃がせない。PH 人形9体の L6 3件)。ArrangeDollTags の式を写した計算 (scratchpad integrate/fix1/dollsim.py) で
        // s の開きを振ると、t +0.45 では s ≥2.1 で重なり 0・+0.5 では s ≥2.3。→ スマホの見本だけ t +0.5・s +2.8 (どちらの向きに ±0.1 ずれても 0 のまま。
        // 後列の足元はいちばん近い前列より 27〜29px 上 = K13 の 20px を超える)。PC の見本は +0.45・+2.0 のまま (L6 0件)
        const float R2A_DollBackDtDioramaPhone = 0.5f, R2A_DollBackDsDioramaPhone = 2.8f;
        /// <summary>人形の後列の t のずらし (見本 PC +0.45・見本スマホ +0.5・今の舞台 +0.15)</summary>
        static float R2A_DollBackDt { get { return DioramaCamera ? (UiKit.Phone ? R2A_DollBackDtDioramaPhone : R2A_DollBackDtDiorama) : R2A_DollBackDtOld; } }
        /// <summary>人形の後列の s のずらし (見本 PC +2.0・見本スマホ +2.8・今の舞台 +1.15)</summary>
        static float R2A_DollBackDs { get { return DioramaCamera ? (UiKit.Phone ? R2A_DollBackDsDioramaPhone : R2A_DollBackDsDiorama) : R2A_DollBackDsOld; } }

        // ---- からくりの匣 見本のスマホの置き場 (直しの輪1 2026-10-01) ----
        // 今の舞台のスマホの置き場 (足元の真ん前 t −5.3・s −1.9) は、見本のカメラ (22°・7°・足元 0.525) では画面 x 467・行 398〜470 に写り、
        // 主人公の足 (x 448・行 433) の前に重なった (主人公が匣の上に立って見えた。反証 high)。見本のスマホだけ主人公の左奥 (t −5.6・s 2.4) へ:
        // 画面 x 312〜379・行 359〜420 = 自分の札 (x ≤302) にも足 (x 420〜490) にも、ひなたの衣 (x ≥395) にも掛からない (r2/camera/cam.py と撮影。
        // 1回目の t −5.6 は x 328〜396 でひなたの衣に接した)。主人公より奥なので主人公の絵が手前に描かれる
        const float R2A_BoxPhoneT = -5.75f, R2A_BoxPhoneS = 2.4f;
        /// <summary>見本のスマホの匣の足元 (世界)</summary>
        static Vector3 R2A_BoxPhoneDiorama() { return OnPath(R2A_BoxPhoneT, R2A_BoxPhoneS); }

        /// <summary>UI の入れ物の下端から足元までの高さ (敵ごとに違う。名前札や HP バーは入れ物の下端基準で同じ線に揃う)</summary>
        // ---- からくりの匣 (2026-09-10 世界観「からくりだけ実物」): リーダーの足元に置く小さな木の匣。仕込むと蓋が開き、動かすと閃く ----
        static GameObject _box; static Texture2D _boxClosed, _boxOpen; static int _boxShown = -1; static GameObject _boxGlow, _boxBlob; static bool _boxLeftRear;
        static bool _boxDiorama;   // 直しの輪1: 組んだ時に見本だったか (幕1→幕2 で見本の置き場が今の舞台に残らないよう、変われば作り直す)
        /// <summary>仕込み札の枚数に合わせて匣の蓋を開閉する。fired=true なら一度閃く (動かした)。
        /// leftRear=true ならリーダーの左奥 (t=-6.3・s=1.7)、false ならリーダーの右手前 (t=-3.7・s=-0.75)。
        /// 置き場は BattleView.BoxLeftRear (PC なら左奥・スマホは右手前) で渡す＝戦闘の途中で動かない。
        /// (2026-09-29 戦闘画面のレビュー p13: PC の右手前は自分の札 (y640) に下端と接地影が隠れ、白では人形の2体目の足元を隠していた)</summary>
        public static void SetKarakuriBox(int setCount, bool fired, bool leftRear)
        {
            Ensure();
            if (_box == null || _box.transform.parent != _world || _boxLeftRear != leftRear || _boxDiorama != DioramaCamera)
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
                // 見本のスマホは主人公の左奥 (直しの輪1。R2A_BoxPhoneDiorama の注記)。今の舞台は今まで (−5.3, −1.9) のまま
                var pos = leftRear ? OnPath(-6.3f, 1.7f) : (DioramaCamera ? R2A_BoxPhoneDiorama() : OnPath(-5.3f, -1.9f));
                _box = Plane("karakuri-box", _boxClosed, pos + new Vector3(0f, 0.01f, 0f), 0.62f, 0.5f, false);
                _boxBlob = Blob("shadow-karakuri-box", _world, pos, 0.62f * _boxClosed.width / (float)_boxClosed.height);
                _boxGlow = Glow("karakuri-glow", Px.Glow(new Color(0.55f, 1f, 0.9f, 0.6f)), pos + new Vector3(0f, 0.45f, -0.15f), 1.6f, 1.6f);
                _boxGlow.SetActive(false);
                _boxShown = -1;
                _boxLeftRear = leftRear;
                _boxDiorama = DioramaCamera;
                HD2DFlags.LayoutDumpers["karakuriBox"] = R2A_DumpKarakuriBox;   // dumplayout の extra.karakuriBox (匣の画面の矩形。直しの輪1・hd2d-layout-check の L10)
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

        /// <summary>dumplayout の extra.karakuriBox: 匣の画面の矩形 px [x, y (上から), w, h]・足元 [x, y]・深さ・置き場 (直しの輪1。hd2d-layout-check の L10 が読む)。
        /// 矩形は匣の板 (絵の不透明でない所も含む) の四隅を、揺れと寄りを含まないレイアウト用のカメラで写した物</summary>
        static object R2A_DumpKarakuriBox()
        {
            if (_box == null || !_laidOut) return null;
            var mf = _box.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return null;
            float H = Screen.height;
            var b = mf.sharedMesh.bounds; var m = _box.transform.localToWorldMatrix;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int c = 0; c < 8; c++)
            {
                var w = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f)));
                var s = LayoutScreen(w);
                x0 = Mathf.Min(x0, s.x); x1 = Mathf.Max(x1, s.x); y0 = Mathf.Min(y0, s.y); y1 = Mathf.Max(y1, s.y);
            }
            var feet = LayoutScreen(_box.transform.position);
            return new Dictionary<string, object>
            {
                { "px", new float[] { x0, H - y1, x1 - x0, y1 - y0 } },
                { "feet", new float[] { feet.x, H - feet.y } },
                { "depth", Vector3.Dot(_box.transform.position - _camBase, _fwd) },
                { "place", _boxLeftRear ? "leftRear" : (_boxDiorama ? "dioramaPhone" : "front") },
            };
        }

        public static void SetFeetOffset(string key, float y) { _feetOffsets[key] = y; }
        public static float FeetOffset(string key, float fallback) { float y; return _feetOffsets.TryGetValue(key, out y) ? y : fallback; }
    }
}
