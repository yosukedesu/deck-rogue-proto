// BattleView.cs — 戦闘画面の「残留UI」(2026-09-07 M2-4「動きから」)。
// 旧: コマンドごとに画面を丸ごと作り直す (カードが瞬間移動する)。
// 新: 戦場 (敵パネル・リーダー) と手札のカードは GameObject を持ち越し、状態の差分をトゥイーンで見せる。
//   - 手札: uid ごとにカードを保持。並び替えは扇の位置へ滑る。ドローは山札から飛んでくる。プレイ/捨て/伏せ/消滅は行き先へ飛んで消える
//   - 敵・リーダー: 入れ物 (アンカー) を持ち越し、中身だけ組み直す。HP バーは前の値から滑る
//   - 上部バー・山札・ターン終了・モーダルは毎回作り直す (動かないので安い)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public class BattleView
    {
        public RectTransform Root;
        public RectTransform FieldLayer;
        public RectTransform HandLayer;
        public RectTransform UiLayer;
        RectTransform _enemiesArea;
        RectTransform _playerArea;
        // 自分の札の入れ物 (HD-2D 見本の箱庭だけ。2026-09-30 P20 2周目): リーダーの入れ物 (_playerArea = 演出の的 "player") と同じ矩形の兄弟。
        // 被弾の押し縮み (Presenter の Tween.Punch(入れ物, keepBottom)) は入れ物を拡大と 2.5° の傾きで揺らすので、子の自分の札も一緒に
        // 上へ 27px・画面の外へ 20px 動き、足元を 16〜36px 隠していた (W3 の regress R06・R07・R10 の L2/L4)。札を兄弟へ移すと演出は絵だけに掛かる
        // (「画面揺れは舞台が揺れ、紙の UI は揺れない」)。名前は同じ "player" = layout-check (/player/hpwrap)・hideui・hidezone が今と同じに拾う。今の舞台は null (今のまま入れ物の子)
        RectTransform _playerSelf;
        readonly List<RectTransform> _enemyPanels = new List<RectTransform>();
        readonly List<Image> _enemyHits = new List<Image>();
        float[] _enemyGaps = new float[0];   // 隣の敵との間隔 (帳面の一行の幅を絞る。確認の窓も同じ幅を読む)
        int[] _shownEnemyHp = new int[0];
        int _shownPlayerHp = -1;
        // 帳面の盾の数字の「今見えている値」(順送りの敵フェーズで得た/減ったブロックをその場で動かす。2026-09-17 ユーザー「置物の誘発とかで得たブロックがキャラの表記に更新されなくない？」)
        int[] _shownEnemyBlock = new int[0];
        int _shownPlayerBlock = 0;
        int _shownPlayerIce = 0;
        int _bgAct = -1;
        // 人形 (白の従者) の入れ物 (2026-09-19 人形の盤面表示): uid ごとに持ち越し、中身だけ組み直す。崩した (灯が消えた) 人形は組み直しでも描かない
        RectTransform _dollsArea;
        readonly Dictionary<string, RectTransform> _dollPanels = new Dictionary<string, RectTransform>();
        readonly HashSet<string> _dollGone = new HashSet<string>();
        /// <summary>舞台に立てる人形の上限 (前列5＋後列4)。超えた分は最後の札に「+N」</summary>
        public const int DollCap = 9;

        public class HandCard
        {
            public RectTransform Rt;
            public CardInstance Card;
            public Vector2 BasePos;
            public float BaseRot;
            public int Index;
            public bool Playable;
            public bool Settable;
            public int Cost;        // 表示したコスト (割引・重圧で変わったら描き直す。2026-09-09)
            public int Preview;     // 表示に使った対象の敵 (-1 = 無し)
            public bool Compact;    // 名前を左寄せで描いた (スマホで手札7枚以上。6枚と7枚をまたいだら描き直す。2026-09-29 p09)
            public float BodyInset; // 本文の右の余白に足した量 (スマホで隣の札に覆われる幅。2026-09-30 F01)
        }
        readonly Dictionary<string, HandCard> _hand = new Dictionary<string, HandCard>();

        /// <summary>直前にプレイした札 (行き先の演出用)。BattleScreen.PlayCard が設定し、SyncHand が消費する</summary>
        public string LastPlayedUid;
        public int LastPlayedTarget = -1;

        public static BattleView Ensure(GameRoot g, RectTransform root)
        {
            if (g.Battle != null && g.Battle.Root == root && g.Battle.FieldLayer != null) return g.Battle;
            var v = new BattleView();
            v.Root = root;
            v.FieldLayer = UiKit.NewRect("field", root);
            UiKit.Stretch(v.FieldLayer, 0f, 0f, 0f, 0f);
            v.HandLayer = UiKit.NewRect("handlayer", root);
            UiKit.Anchor(v.HandLayer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-700f, BattleScreen.HandY), new Vector2(700f, BattleScreen.HandY + CardView.H * BattleScreen.CardScale + 40f));
            v.UiLayer = UiKit.NewRect("ui", root);
            UiKit.Stretch(v.UiLayer, 0f, 0f, 0f, 0f);
            g.Battle = v;
            return v;
        }

        public void Destroy()
        {
            if (Root == null) return;
            foreach (var l in new[] { FieldLayer, HandLayer, UiLayer })
            {
                if (l == null) continue;
                l.SetParent(null, false);
                UnityEngine.Object.Destroy(l.gameObject);
            }
            FieldLayer = null; HandLayer = null; UiLayer = null;
            _hand.Clear();
            _enemyPanels.Clear();
            _enemyHits.Clear();
            _dollPanels.Clear();
            _dollGone.Clear();
            _dollSeat.Clear();
            _dollWide.Clear();
            _dollDying.Clear();
            _dollsArea = null;
        }

        int _boxLogSeen;   // 匣の閃きに使った EventLog の読み位置

        public void ClearUi()
        {
            for (int i = UiLayer.childCount - 1; i >= 0; i--)
            {
                var c = UiLayer.GetChild(i);
                c.SetParent(null, false);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        // ---- 戦場 ----

        public void SyncField(GameRoot g, GameState st)
        {
            var run = g.Rs;
            if (_bgAct != run.Act)
            {
                var old = FieldLayer.Find("bg");
                if (old != null) { old.SetParent(null, false); UnityEngine.Object.Destroy(old.gameObject); }
                var bg = UiKit.NewRect("bg", FieldLayer);
                UiKit.Stretch(bg, 0f, 0f, 0f, 0f);
                bg.SetAsFirstSibling();
                BattleScreen.BuildBackground(bg, run);
                _bgAct = run.Act;
            }
            // 手札の後ろの手前の地面を地の色で沈める (2026-09-29 I44): 幕1 の手前の草は座席より明るく (下の隅 L≈52〜55 対 戦闘の帯 L≈46)、
            // ぼけた草・タイルの柄の境目・幕2 の額縁の柱の頭が手札・エナジーの輪・山札・ターン終了の真後ろで騒いでいた。
            // 範囲は画面の下端から足元の線 (StatusLineY) の少し上まで。下の 40% は一定、その上を smoothstep で 0 へ (上端に線は付けない＝案B の作業台にはしない)。
            // 紙の UI (HandLayer・UiLayer) と帳面 (StatusLineY から上) より下の層 = 光と空気は舞台だけ、の規約の内側 (SyncDanger と同じ層)
            // 2026-09-30 F50 (ユーザー裁定「地面を3か所明るく」): α 0.8 では手札の左右の地面の輝度 Y が 0.040→0.013 に落ち、手札の紙がほぼ黒の上に置かれて
            // 「紙の UI が黒い画面に貼った札」(I24) を手札のまわりでかえって強めた → α 0.55 にし、上端のフェードを StatusLineY+40 まで伸ばして沈みの境目をなだらかに
            const float DeskShadeAlpha = 0.55f, DeskShadeOver = 40f;   // 下の 40% の濃さ・足元の線より上へ伸ばす高さ (F50)
            // HD-2D 見本の箱庭 (2026-09-30 P20「desk-shade を縮める」): 手前の段 (羊歯・根・岩) と額縁の下隅が手札のまわりで見えるように、上端を足元の線の 40 下まで縮める
            // (濃さは今のまま。Linear で暗幕の濃さを合わせるのは P21 の見立て)。今の舞台は今のまま
            const float DeskShadeUnderDiorama = 40f;
            float deskTop = BattleScreen.Hd2dLayout ? StatusLineY - DeskShadeUnderDiorama : StatusLineY + DeskShadeOver;
            // 手札の置き場は手札を沈めた量 (BattleScreen.HandY) に追う (Ensure は戦闘の最初に1回だけ。旗が後から変わっても組み直しで揃う)
            if (HandLayer != null)
                UiKit.Anchor(HandLayer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-700f, BattleScreen.HandY), new Vector2(700f, BattleScreen.HandY + CardView.H * BattleScreen.CardScale + 40f));
            var desk = FieldLayer.Find("desk-shade") as RectTransform;
            Image deskImg;
            if (desk == null)
            {
                desk = UiKit.NewRect("desk-shade", FieldLayer);
                deskImg = desk.gameObject.AddComponent<Image>();
                deskImg.sprite = UiKit.LinearSprite(ThemeFx.FadeUp(PaperFx.Ground, DeskShadeAlpha, "fade-up-ground-055"), 0.25f); deskImg.type = Image.Type.Simple; deskImg.preserveAspect = false; deskImg.raycastTarget = false;   // Linear (W3 P21 の申し送り2): 絵に焼いた α を Gamma と同じ濃さへ (下の地面 0.25 を仮定)
            }
            else deskImg = desk.GetComponent<Image>();
            if (R3A_DeskHand)
            {   // 三周目 (2026-10-01 レーン A・分析 R6): 箱庭の既定 deskshade=hand = 手札の矩形 (HandLayer。画面の下端から手札の上の余白 40 まで) の内側だけ α0.3。
                // 左右の端は幅の 15% で 0 へぼかす (硬い縦の縁を作らない)。旗 deskshade=full で二周目の全幅の暗幕。今の舞台は下の else のまま
                var want = R3A_DeskHandSprite();
                if (deskImg != null && deskImg.sprite != want) deskImg.sprite = want;
                float handTop = BattleScreen.HandY + CardView.H * BattleScreen.CardScale + 40f;
                UiKit.Anchor(desk, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-700f, 0f), new Vector2(700f, Mathf.Max(1f, handTop)));
            }
            else
            {
                var want = UiKit.LinearSprite(ThemeFx.FadeUp(PaperFx.Ground, DeskShadeAlpha, "fade-up-ground-055"), 0.25f);   // 作った時と同じ絵 (deskshade=hand から戻した時だけ差し替わる)
                if (deskImg != null && deskImg.sprite != want) deskImg.sprite = want;
                UiKit.Anchor(desk, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, deskTop));
            }
            var bgRt = FieldLayer.Find("bg");
            desk.SetSiblingIndex(bgRt != null ? bgRt.GetSiblingIndex() + 1 : 0);
            // 敵の入れ物: 数が変わったら作り直す (分裂・孵化)
            int prevCount = _enemiesArea != null ? _enemyPanels.Count : 0;   // 増えた分は登場の演出 (2026-09-17)
            if (_enemiesArea == null || _enemyPanels.Count != st.Enemies.Count)
            {
                if (_enemiesArea != null) { _enemiesArea.SetParent(null, false); UnityEngine.Object.Destroy(_enemiesArea.gameObject); }
                _enemyPanels.Clear();
                _enemyHits.Clear();
                _enemiesArea = UiKit.NewRect("enemies", FieldLayer);
                UiKit.Stretch(_enemiesArea, 0f, 0f, 0f, 0f);
                for (int i = 0; i < st.Enemies.Count; i++)
                {
                    var pan = UiKit.NewRect("enemy" + i, _enemiesArea);
                    var hit = pan.gameObject.AddComponent<Image>();
                    hit.color = new Color(0f, 0f, 0f, 0f);
                    var btn = pan.gameObject.AddComponent<Button>();
                    btn.targetGraphic = hit;
                    btn.transition = Selectable.Transition.None;
                    int captured = i;
                    btn.onClick.AddListener(delegate { g.OnEnemyClicked(captured); });
                    Tooltip.Attach(pan.gameObject, delegate { return BattleScreen.EnemyTip(g, captured); });
                    _enemyPanels.Add(pan);
                    _enemyHits.Add(hit);
                }
                _shownEnemyHp = new int[st.Enemies.Count];
                for (int i = 0; i < st.Enemies.Count; i++) _shownEnemyHp[i] = st.Enemies[i].Hp;
                _shownEnemyBlock = new int[st.Enemies.Count];
            }
            if (_shownEnemyBlock.Length != st.Enemies.Count) _shownEnemyBlock = new int[st.Enemies.Count];
            // 座席: 舞台 (HD-2D) が決める。手前左から奥右へ斜めに並び、足元 (パネル下端+130) がその座席の地面に来る。奥の敵ほど先に描く
            // 名前札・HPバー・チップは全員同じ線 (入れ物の下端 = StatusLineY・手札の上)。足元だけ座席の高さへ
            var slots = Stage.EnemySlots(st.Enemies.Count);
            var centers = new float[st.Enemies.Count];
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                var feet = Stage.ProjectFeet("enemy" + i, slots[i]);
                float w = UiKit.Phone ? 220f : 330f, h = 720f;   // スマホは敵の間隔が狭い (キャンバスの高さで横の広がりも決まる) ので名前札・HPバーの幅を絞る
                UiKit.Anchor(_enemyPanels[i], new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(feet.x - w / 2f, StatusLineY), new Vector2(feet.x + w / 2f, StatusLineY + h));
                Stage.SetFeetOffset("enemy" + i, feet.y - StatusLineY);
                _enemyPanels[i].SetSiblingIndex(st.Enemies.Count - 1 - i);
                centers[i] = feet.x;
            }
            // 隣の敵との間隔 (スマホで3体以上の吹き出しが重ならないよう、吹き出しの幅を間隔で絞る。2026-09-14) と、
            // 自分の欄が伸びてよい右端 (2026-09-18 ユーザー「ギアが集まると枠が左の敵のステータス表示と重なる」):
            // いちばん左の敵の表示 (PC は足元の帳面の左端・スマホは頭上の意図の札の左端) より左で止める。倒れた敵の座席も数える (戦闘中に欄が伸び縮みしない)
            if (_enemyGaps.Length != st.Enemies.Count) _enemyGaps = new float[st.Enemies.Count];
            float zoneRight = float.MaxValue;
            float minGap = float.MaxValue;
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                float gap = float.MaxValue;
                if (i > 0) gap = Mathf.Min(gap, Mathf.Abs(centers[i] - centers[i - 1]));
                if (i + 1 < centers.Length) gap = Mathf.Min(gap, Mathf.Abs(centers[i + 1] - centers[i]));
                _enemyGaps[i] = gap;
                minGap = Mathf.Min(minGap, gap);
            }
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                // 全員の帳面と意図の札は、いちばん狭い間隔から1つの幅に揃える (2026-09-29 p02「1体でも4体でも同じ形」: 数 px の差で名前の行の形が
                // ①②「①嚙み…＋筋力-2」／③④「③嚙みつく…＋-2」と割れていた)。帳面の中心は足元の x のまま (HP は足元)
                _enemyGaps[i] = minGap;
                float half = UiKit.Phone ? 112f : BattleScreen.StripW(minGap, st.Enemies.Count == 1) / 2f;   // 意図の札は最大 ≈220 幅で頭の真上に中央揃え
                zoneRight = Mathf.Min(zoneRight, centers[i] - half);
            }
            SelfZoneRight = st.Enemies.Count > 0 ? zoneRight : -1f;
            // 1体でも帳面に3段目 (特性・庇われている・分岐・予告) があれば全員の帳面を同じ高さに (名前の行と HP バーが一直線。2026-09-29 p01)
            bool anyForecast = false;
            for (int i = 0; i < st.Enemies.Count && !anyForecast; i++) if (BattleScreen.HasForecast(st, i, _enemyGaps[i])) anyForecast = true;
            // 1体でも意図の札の rider が下の段に回るなら、rider を持つ札は全員下ろす (2026-09-30 F13「1体でも4体でも同じ形」)
            bool anyStack = false;
            for (int i = 0; i < st.Enemies.Count && !anyStack; i++) if (BattleScreen.IntentNeedsStack(st, i, _enemyGaps[i])) anyStack = true;
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                var pan = _enemyPanels[i];
                bool alive = st.Enemies[i].Hp > 0;
                _enemyHits[i].raycastTarget = alive;
                _enemyHits[i].GetComponent<Button>().interactable = alive;
                for (int c = pan.childCount - 1; c >= 0; c--) { var ch = pan.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
                g.RegisterAnchor("enemy" + i, pan);
                bool wasAlive = _shownEnemyHp[i] > 0;
                float gap = _enemyGaps[i];
                // 倒れた瞬間 (2026-09-17): 絵と帳面を生前の姿で描いておき、EnemyDied/EnemyFled の出来事 (Presenter → KillEnemy) が着弾の後に崩す。
                // 出来事が先に来ていた (順送りの敵フェーズで倒れた) なら _died に印があるので何も描かない。出来事が来なければ 0.6 秒後に崩す (保険)
                if (alive) _died.Remove(i);
                bool dyingNow = wasAlive && !alive && !_died.Contains(i);
                BattleScreen.FillEnemyPanel(g, pan, st, i, _shownEnemyHp[i], gap, dyingNow, anyForecast, anyStack);
                _shownEnemyHp[i] = st.Enemies[i].Hp;
                _shownEnemyBlock[i] = st.Enemies[i].Block;
                if (dyingNow) { int ci = i; bool fled = st.Enemies[i].Fled == true; Tween.After(0.6f, () => KillEnemy(g, ci, fled)); }
                else if (prevCount > 0 && i >= prevCount && alive)
                {   // 登場 (召喚・分裂・孵化の子。2026-09-17): 小さく現れて弾んで等身大に、足元に青緑の輪
                    var sprRt = pan.Find("sprite") as RectTransform;
                    if (sprRt != null)
                    {
                        var origin = sprRt.anchoredPosition; float h = sprRt.rect.height; float pivotY = sprRt.pivot.y;
                        var srt = sprRt;
                        Tween.Run(0.45f, k => { if (srt == null) return; float sc = 0.2f + 0.8f * Tween.Apply(Ease.OutBack, k); srt.localScale = new Vector3(sc, sc, 1f); srt.anchoredPosition = origin + new Vector2(0f, -h * pivotY * (1f - sc)); }, Ease.Linear, () => { if (srt != null) { srt.localScale = Vector3.one; srt.anchoredPosition = origin; } });
                        if (g.FxLayer != null) Tween.RingBurst(g.FxLayer, Tween.CenterIn(sprRt, g.FxLayer) + new Vector2(0f, -h * 0.4f), PaperFx.Mana, 200f, 0.5f);
                    }
                }
            }
            // リーダー
            if (_playerArea == null)
            {
                _playerArea = UiKit.NewRect("player", FieldLayer);
                _shownPlayerHp = st.Player.Hp;
            }
            {
                // リーダーの足元 (欄の左下から +130,+130) を舞台の座席へ
                var feet = Stage.ProjectFeet("player", Stage.LeaderSlot());
                UiKit.Anchor(_playerArea, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(feet.x - 130f, StatusLineY), new Vector2(feet.x - 130f + 690f, StatusLineY + 720f));
                Stage.SetFeetOffset("player", feet.y - StatusLineY);
            }
            for (int c = _playerArea.childCount - 1; c >= 0; c--) { var ch = _playerArea.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
            SyncPlayerSelf();
            g.RegisterAnchor("player", _playerArea);
            BattleScreen.FillPlayerPanel(g, _playerArea, st, _shownPlayerHp, _playerSelf);
            _shownPlayerHp = st.Player.Hp;
            _shownPlayerBlock = st.Player.Block;
            _shownPlayerIce = st.Player.IceBlock;
            // 人形 (白の従者) は舞台に立つ (2026-09-19)
            SyncDolls(g, st);
            // からくりの匣 (2026-09-10 世界観「からくりだけ実物」): 舞台のリーダーの足元。仕込み札があれば蓋が開き、動かした (ReactionTriggered) 直後は閃く
            bool fired = false;
            for (int i = _boxLogSeen; i < st.EventLog.Count; i++) if (st.EventLog[i] is GameEvent_ReactionTriggered) fired = true;
            _boxLogSeen = st.EventLog.Count;
            Stage.SetKarakuriBox(st.Player.SetCards.Count, fired, BoxLeftRear);
            SyncDanger(st);
        }

        /// <summary>舞台のからくりの匣をリーダーの左奥に置くか (2026-09-29 戦闘画面のレビュー p13): PC は左奥 (右手前は自分の札 y642 に下端と接地影が隠れ、
        /// 白では人形の2体目の足元も隠していた)。スマホはリーダーの足元の真ん前 (2026-09-30 F46: 右手前は白の人形の2体目の脚を隠した。左奥は自分の札 x≤325・上端 y382 にもぐる)。
        /// 人形の数では決めない＝戦闘の途中で匣が動かない</summary>
        public static bool BoxLeftRear { get { return !UiKit.Phone; } }

        /// <summary>自分の欄 (からくり・ギア・置物) が伸びてよい右端 (キャンバス x)。いちばん左の敵の表示の左端。敵がいなければ -1 (2026-09-18)</summary>
        public static float SelfZoneRight = -1f;

        /// <summary>名前札・HPバーの線 (入れ物の下端)。手札の上端 (約290) のすぐ上。スマホは等倍の札の上端 (14+290) に合わせる。
        /// HD-2D 見本の箱庭 (2026-09-30 P20): 手札を沈めた分だけ下げる。PC 285 = 沈めた手札の真ん中の札の上端 (11+266.8=277.8) の 7.2 上
        /// (layout-check L1 の 6 以上。今の 300 は上端 296.8 の 3.2 上で、人形の多い白の自分の札 (幅 924) だけ L1 に掛かっていた)。スマホは式のまま (−3+290+6 = 293)</summary>
        public static float StatusLineY { get { return UiKit.Phone ? BattleScreen.HandY + CardView.H * BattleScreen.CardScale + 6f : (BattleScreen.Hd2dLayout ? 285f : 300f); } }

        // ---- 三周目 (2026-10-01 レーン A・分析 R6・計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 A): 箱庭の手札の後ろの暗幕を手札の矩形だけに ----
        /// <summary>暗幕 desk-shade を手札の矩形の内側だけにするか (箱庭で deskshade=hand。旗を書かない時はこの画面の割り付け (HD2DFlags.UiLayoutHere。PC は uilayout・スマホは uilayoutphone＝既定 r2) が r3 → hand・r2 → full = 二周目の全幅 (HD2DFlags.DeskShade の既定)。今の舞台は旗によらず false)</summary>
        static bool R3A_DeskHand { get { return BattleScreen.Hd2dLayout && HD2DFlags.DeskShade == HD2DDeskShade.Hand; } }
        /// <summary>手札の矩形の暗幕の濃さ (Gamma の見た目の α。下の 40% で一定)・左右の端のぼかしの幅 (矩形の幅の割合)</summary>
        const float R3A_DeskHandAlpha = 0.3f, R3A_DeskHandFeather = 0.15f;
        static Sprite _r3aDeskHand;
        /// <summary>
        /// 手札の矩形の暗幕の絵: 縦は ThemeFx.FadeUp と同じ形 (下の 40% は R3A_DeskHandAlpha のまま一定・その上を smoothstep で 0)、
        /// 横は左右の端から幅の R3A_DeskHandFeather で smoothstep の 0 へ。色は PaperFx.Ground。FadeUp の 0.55 (鍵 fade-up-ground-055) とは別の絵 = 別の鍵
        /// (FadeUp のキャッシュは鍵だけで引くので、同じ鍵で α を変えると先に作った方が返る)。Linear の補正は今の暗幕と同じ (下の地面 0.25)
        /// </summary>
        static Sprite R3A_DeskHandSprite()
        {
            if (_r3aDeskHand != null) return _r3aDeskHand;
            const int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "r3a-desk-hand-030";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var c = PaperFx.Ground;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float k = y / (float)(h - 1);                      // 0 = 下端・1 = 上端
                float u = Mathf.Clamp01((k - 0.4f) / 0.6f);
                float v = 1f - u * u * (3f - 2f * u);
                for (int x = 0; x < w; x++)
                {
                    float kx = x / (float)(w - 1);
                    float d = Mathf.Clamp01(Mathf.Min(kx, 1f - kx) / R3A_DeskHandFeather);
                    float hx = d * d * (3f - 2f * d);
                    px[y * w + x] = new Color(c.r, c.g, c.b, R3A_DeskHandAlpha * v * hx);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "r3a-desk-hand-030";
            _r3aDeskHand = UiKit.LinearSprite(s, 0.25f);
            return _r3aDeskHand;
        }

        /// <summary>手札 UI に残っている札の数 (自動操作の検証用)</summary>
        public int HandCount { get { return _hand.Count; } }

        /// <summary>敵の入れ物 (帳面の一行の x を確認の窓が読む)。無ければ null</summary>
        public RectTransform EnemyPanel(int index)
        {
            return index >= 0 && index < _enemyPanels.Count ? _enemyPanels[index] : null;
        }

        /// <summary>隣の敵との間隔 (帳面の一行の幅)。無ければ無限大</summary>
        public float EnemyGap(int index)
        {
            return index >= 0 && index < _enemyGaps.Length ? _enemyGaps[index] : float.MaxValue;
        }

        public RectTransform EnemySprite(int index)
        {
            if (index < 0 || index >= _enemyPanels.Count || _enemyPanels[index] == null) return null;
            return _enemyPanels[index].Find("sprite") as RectTransform;
        }

        public RectTransform PlayerSprite()
        {
            return _playerArea != null ? _playerArea.Find("sprite") as RectTransform : null;
        }

        /// <summary>自分の札 (hpwrap・からくり・ギア・置物) の入れ物。箱庭では演出の的と別の兄弟 (_playerSelf)、今の舞台はリーダーの入れ物そのもの</summary>
        public RectTransform SelfArea { get { return _playerSelf != null ? _playerSelf : _playerArea; } }

        /// <summary>
        /// 自分の札の入れ物を、リーダーの入れ物と同じ矩形・すぐ後ろの兄弟に置き、中身を空にする (箱庭の時だけ。2026-09-30 P20 2周目)。
        /// 同じ矩形なので札の置き場 (PcSelfStrip・PhoneSelfColumn の座標) は1画素も変わらず、変わるのは被弾の押し縮みが札に掛からないことだけ。
        /// 描く順も今と同じ (リーダーの入れ物の直後 = 人形の札より後)。旗が今の舞台へ戻ったら消して、札はリーダーの入れ物の子に戻る
        /// </summary>
        void SyncPlayerSelf()
        {
            if (!BattleScreen.Hd2dLayout)
            {
                if (_playerSelf != null) { _playerSelf.SetParent(null, false); UnityEngine.Object.Destroy(_playerSelf.gameObject); _playerSelf = null; }
                return;
            }
            if (_playerSelf == null) _playerSelf = UiKit.NewRect("player", FieldLayer);
            _playerSelf.pivot = _playerArea.pivot;   // 先に (pivot を後で変えると矩形が動く)
            // 直前に SyncField がリーダーの入れ物を休んでいる時の矩形へ置き直している (押し縮みの途中でも offset は今の置き場) のでそれを写す
            UiKit.Anchor(_playerSelf, _playerArea.anchorMin, _playerArea.anchorMax, _playerArea.offsetMin, _playerArea.offsetMax);
            int p = _playerArea.GetSiblingIndex(), s = _playerSelf.GetSiblingIndex();
            if (s != p + 1) _playerSelf.SetSiblingIndex(s < p ? p : p + 1);   // 前にいれば抜いた分だけ番号が詰まる
            for (int c = _playerSelf.childCount - 1; c >= 0; c--) { var ch = _playerSelf.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
        }

        // ---- 人形 (白の従者) の舞台の座席 (2026-09-19 ユーザー「人形は戦場の盤面にも表示するようにしたい」→ デザインカンバス「人形の盤面表示」案A「灯りの列」) ----
        // 人形 (retainer:true の置物。リーダーパッシブ=innate は除く) は、点灯した順にひなたの前の道に一体ずつ立つ (Stage.DollSlots)。
        // 絵は敵と同じ器 (座席→ProjectFeet→BindUnit・接地影・呼吸・崩れ)。足元に「何が出るか」の小さな札 (絵＋数字。輝き増し込み)。
        // 付箋 (紙の帯) にも残す (ユーザー裁定)。壊された/捧げた人形は Presenter の KillDoll で崩れ、以後は描かない

        /// <summary>舞台に立っている人形 (状態の順・上限まで・崩した人形は除く)</summary>
        public List<CardInstance> StageDolls(GameState st)
        {
            var list = new List<CardInstance>();
            foreach (var p in st.Player.Permanents) if (p.Def.Retainer == true && p.Innate != true && !_dollGone.Contains(p.Uid)) list.Add(p);
            return list;
        }

        void SyncDolls(GameRoot g, GameState st)
        {
            bool first = _dollsArea == null;   // 戦闘の最初の組み直し (続きから・デバッグの perms=): 立っている人形は点灯の演出なし
            if (_dollsArea == null)
            {
                _dollsArea = UiKit.NewRect("dolls", FieldLayer);
                UiKit.Stretch(_dollsArea, 0f, 0f, 0f, 0f);
                // 敵の入れ物の次 (自分の欄の紙より下) に描く。絵は舞台のビルボードなので UI の順は札とタップの的だけに効く
                if (_enemiesArea != null) _dollsArea.SetSiblingIndex(_enemiesArea.GetSiblingIndex() + 1);
            }
            var dolls = StageDolls(st);
            var alive = new HashSet<string>();
            foreach (var d in dolls) alive.Add(d.Uid);
            // 状態から消えた人形 (壊された・捧げた) の入れ物は残しておき、出来事 (TokenDestroyed/RetainerSacrificed → KillDoll) が崩す。
            // 出来事が来ない経路の保険は 0.6 秒後に崩す。崩れるまで座席は空けない (倒れた敵と同じ = 座席は詰めない)
            foreach (var kv in _dollPanels)
            {
                if (alive.Contains(kv.Key) || _dollGone.Contains(kv.Key) || kv.Value == null) continue;
                string uidC = kv.Key;
                if (_dollDying.Add(uidC)) Tween.After(0.6f, () => KillDoll(g, uidC, false));
            }
            // 座席: 立っている人形は今の座席を保ち、新しい人形は空いている最も前の座席へ (上限 DollCap)
            var used = new HashSet<int>();
            foreach (var kv in _dollSeat) if (_dollPanels.ContainsKey(kv.Key)) { used.Add(kv.Value); if (_dollWide.Contains(kv.Key)) used.Add(kv.Value + 1); }   // 崩れかけの人形の座席も塞いだまま (大きい人形は隣の座席も)
            var stale = new List<string>();
            foreach (var kv in _dollSeat) if (!_dollPanels.ContainsKey(kv.Key)) stale.Add(kv.Key);
            foreach (var k in stale) _dollSeat.Remove(k);
            int overflow = 0; string lastShownUid = null; int lastSeat = -1;
            var slots = Stage.DollSlots(DollCap);
            float w = UiKit.Phone ? 110f : 170f, h = 360f;
            foreach (var d in dolls)
            {
                int seat;
                if (!_dollSeat.TryGetValue(d.Uid, out seat))
                {
                    seat = -1;
                    // 大きい人形 (48 ドットの絵＝竜・獅子 2026-09-26) は同じ列の隣り合う2席を取り、その間に立つ (1席だと隣と重なる)
                    if (IsWideDoll(d))
                        for (int i = 0; i + 1 < DollCap; i++)
                            if (!used.Contains(i) && !used.Contains(i + 1) && (i + 1) % 5 != 0) { seat = i; _dollWide.Add(d.Uid); break; }
                    if (seat < 0) for (int i = 0; i < DollCap; i++) if (!used.Contains(i)) { seat = i; break; }
                    if (seat < 0) { overflow++; continue; }
                    _dollSeat[d.Uid] = seat;
                }
                used.Add(seat);
                bool wide = _dollWide.Contains(d.Uid);
                if (wide) used.Add(seat + 1);
                string key = "doll:" + d.Uid;
                RectTransform pan; bool fresh = false;
                if (!_dollPanels.TryGetValue(d.Uid, out pan) || pan == null)
                {
                    pan = UiKit.NewRect(key, _dollsArea);
                    var hit = pan.gameObject.AddComponent<Image>();
                    hit.color = new Color(0f, 0f, 0f, 0f);
                    var btn = pan.gameObject.AddComponent<Button>();
                    btn.targetGraphic = hit; btn.transition = Selectable.Transition.None;
                    string uidC = d.Uid;
                    btn.onClick.AddListener(delegate { g.OnDollClicked(uidC); });
                    Tooltip.Attach(pan.gameObject, delegate { return BattleScreen.DollTip(g, uidC); });
                    var infoC = pan.gameObject.AddComponent<DollInfo>(); infoC.CardId = d.Def.Id;
                    _dollPanels[d.Uid] = pan;
                    fresh = true;
                }
                { var info = pan.GetComponent<DollInfo>(); if (info != null) info.Order = seat; }
                var feet = Stage.ProjectFeet(key, wide ? (slots[seat] + slots[seat + 1]) * 0.5f : slots[seat]);
                float pw = wide ? w * 1.5f : w;
                UiKit.Anchor(pan, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(feet.x - pw / 2f, StatusLineY), new Vector2(feet.x + pw / 2f, StatusLineY + h));
                Stage.SetFeetOffset(key, feet.y - StatusLineY);
                pan.SetSiblingIndex(Math.Max(0, DollCap - 1 - seat));   // 奥 (後列・右) ほど先に描く
                for (int c = pan.childCount - 1; c >= 0; c--) { var ch = pan.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
                g.RegisterAnchor(key, pan);
                BattleScreen.FillDollPanel(g, pan, st, d, 0);
                if (seat > lastSeat) { lastSeat = seat; lastShownUid = d.Uid; }
                if (fresh && !first) EnterDoll(g, pan);   // 点灯 (登場): 小さく現れて弾む＋暖色の光＋判
            }
            // 上限を超えた分は、いちばん奥の人形の札に「+N」
            if (overflow > 0 && lastShownUid != null)
            {
                RectTransform lp; CardInstance ld = null;
                foreach (var d in dolls) if (d.Uid == lastShownUid) { ld = d; break; }
                if (ld != null && _dollPanels.TryGetValue(lastShownUid, out lp) && lp != null)
                {
                    var tagOld = lp.Find("tag"); if (tagOld != null) { tagOld.SetParent(null, false); UnityEngine.Object.Destroy(tagOld.gameObject); }
                    BattleScreen.FillDollTag(lp, st, ld, overflow);
                }
            }
            // 足元の札の重なりをほどく (隣と重なれば2段・PC は自分の札に潜らない。2026-09-29 p16)。
            // 人形が混んで2段でも置けない札があれば、全部の札を短い形 (「・あとN」の代わりに右端の丸い数字) に組み直してもう一度並べる (形は画面でそろえる)
            if (!BattleScreen.ArrangeDollTags(_dollsArea))
            {
                foreach (var d in dolls)
                {
                    RectTransform dp;
                    if (!_dollPanels.TryGetValue(d.Uid, out dp) || dp == null || !_dollSeat.ContainsKey(d.Uid)) continue;
                    var tagOld = dp.Find("tag"); if (tagOld == null) continue;
                    tagOld.SetParent(null, false); UnityEngine.Object.Destroy(tagOld.gameObject);
                    BattleScreen.FillDollTag(dp, st, d, d.Uid == lastShownUid ? overflow : 0, true);
                }
                BattleScreen.ArrangeDollTags(_dollsArea, true);   // 2回目 = 見本だけ2段上・半札横まで探す (直しの輪1。今の舞台は今まで)
            }
        }
        readonly Dictionary<string, int> _dollSeat = new Dictionary<string, int>();   // uid → 座席 (崩れるまで保つ)
        readonly HashSet<string> _dollWide = new HashSet<string>();   // 2席を取っている大きい人形 (竜・獅子 2026-09-26)

        /// <summary>絵が 32 ドットより大きい人形 (竜・獅子の 48 ドット) = 2席ぶん場所を取る</summary>
        static bool IsWideDoll(CardInstance d)
        {
            var art = Creature.Get("dolls", d.Def.Id, true, 32);
            return art != null && Mathf.Max(art.rect.width, art.rect.height) > 32f;
        }
        readonly HashSet<string> _dollDying = new HashSet<string>();               // 状態から消えたが、まだ崩していない

        /// <summary>点灯 (登場): 暗い人形が座席に置かれ、灯りが点って等身大に弾む。足元に暖色の光の輪と判「点灯」</summary>
        void EnterDoll(GameRoot g, RectTransform pan)
        {
            var sprRt = pan.Find("sprite") as RectTransform;
            if (sprRt == null) return;
            var origin = sprRt.anchoredPosition; float h = sprRt.rect.height; float pivotY = sprRt.pivot.y;
            var srt = sprRt;
            Tween.Run(0.45f, k => { if (srt == null) return; float sc = 0.2f + 0.8f * Tween.Apply(Ease.OutBack, k); srt.localScale = new Vector3(sc, sc, 1f); srt.anchoredPosition = origin + new Vector2(0f, -h * pivotY * (1f - sc)); }, Ease.Linear, () => { if (srt != null) { srt.localScale = Vector3.one; srt.anchoredPosition = origin; } });
            if (g.FxLayer != null)
            {
                var at = Tween.CenterIn(sprRt, g.FxLayer);
                Tween.RingBurst(g.FxLayer, at + new Vector2(0f, -h * 0.4f), PaperFx.BrassLight, 160f, 0.45f);
                Tween.Stamp(g.FxLayer, at + new Vector2(0f, h * 0.55f + 14f), "点灯", PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 17, 0.55f, -6f);
            }
            Audio.Key("PermanentPlayed");
        }

        public RectTransform DollPanel(string uid)
        {
            RectTransform pan;
            return uid != null && _dollPanels.TryGetValue(uid, out pan) ? pan : null;
        }

        public RectTransform DollSprite(string uid)
        {
            var pan = DollPanel(uid);
            return pan != null ? pan.Find("sprite") as RectTransform : null;
        }

        /// <summary>同じ札の人形のうち、いちばん新しく立った (状態の末尾) もの。uid が分からない出来事 (駆けつけ) の的</summary>
        public RectTransform LastDollPanel(GameState st, string cardId)
        {
            if (st == null) return null;
            for (int i = st.Player.Permanents.Count - 1; i >= 0; i--)
            {
                var p = st.Player.Permanents[i];
                if (p.Def.Id == cardId && p.Def.Retainer == true && p.Innate != true) { var pan = DollPanel(p.Uid); if (pan != null) return pan; }
            }
            return null;
        }

        /// <summary>同じ札の人形のうち、舞台に立っている最後の1体の uid (uid を持たない古いログの保険)</summary>
        public string DollUidByCard(string cardId)
        {
            string found = null; int best = -1;
            foreach (var kv in _dollPanels)
            {
                if (kv.Value == null) continue;
                var info = kv.Value.GetComponent<DollInfo>();
                if (info != null && info.CardId == cardId && info.Order > best) { best = info.Order; found = kv.Key; }
            }
            return found;
        }

        /// <summary>入れ物に札の id を持たせる (DollUidByCard の索引)</summary>
        public class DollInfo : MonoBehaviour { public string CardId; public int Order; }

        /// <summary>灯が消える (人形壊し・灯の捧げ): 光が抜けて灰になり、頭から崩れる (倒れた敵と同じ _Dissolve)。以後の組み直しでは描かない</summary>
        public void KillDoll(GameRoot g, string uid, bool sacrificed) { KillDoll(g, uid, sacrificed, null); }

        /// <summary>stamp=判の文言を差し替える (期限切れ「期限切れ」2026-09-21。語彙は 2026-09-22 に灯り→期限。null なら壊し/捧げの既定)</summary>
        public void KillDoll(GameRoot g, string uid, bool sacrificed, string stamp)
        {
            if (uid == null || _dollGone.Contains(uid)) return;
            _dollGone.Add(uid);
            _dollDying.Remove(uid);
            RectTransform pan;
            if (!_dollPanels.TryGetValue(uid, out pan) || pan == null) { _dollPanels.Remove(uid); _dollSeat.Remove(uid); return; }
            _dollPanels.Remove(uid);
            var hit = pan.GetComponent<Image>(); if (hit != null) hit.raycastTarget = false;
            var btn = pan.GetComponent<Button>(); if (btn != null) btn.interactable = false;
            var tagRt = pan.Find("tag") as RectTransform;
            if (tagRt != null) UnityEngine.Object.Destroy(tagRt.gameObject);
            var srt = pan.Find("sprite") as RectTransform;
            string key = "doll:" + uid;
            var fx = g.FxLayer;
            var panC = pan;
            if (srt == null) { pan.SetParent(null, false); UnityEngine.Object.Destroy(pan.gameObject); return; }
            Stage.Flash(key, 0.18f);
            if (fx != null)
            {
                var c = Tween.CenterIn(srt, fx); float hh = srt.rect.height;
                Tween.Stamp(fx, c + new Vector2(0f, hh * 0.55f + 14f), stamp ?? (sacrificed ? "捧げた" : "灯が消えた"), sacrificed ? PaperFx.BrassLight : new Color(0.85f, 0.83f, 0.8f, 1f), sacrificed ? PaperFx.BrassInk : PaperFx.InkSoft, sacrificed ? PaperFx.Brass : new Color(0.54f, 0.53f, 0.5f, 1f), 16, 0.7f, -6f);
            }
            Tween.After(0.1f, () =>
            {
                if (srt == null) return;
                var origin = srt.anchoredPosition;
                Tween.Run(0.5f, k =>
                {
                    if (srt == null) return;
                    Stage.Dissolve(key, k);
                    srt.anchoredPosition = origin + new Vector2(0f, -6f * k);
                }, Ease.Linear, () => { _dollSeat.Remove(uid); if (panC != null) { panC.SetParent(null, false); UnityEngine.Object.Destroy(panC.gameObject); } });
                if (fx != null)
                {
                    var c = Tween.CenterIn(srt, fx); float w = srt.rect.width * 0.35f, hh = srt.rect.height * 0.45f;
                    for (int m = 0; m < 7; m++)
                    {
                        float dl = 0.4f * (m / 7f);
                        var p0 = c + new Vector2(UnityEngine.Random.Range(-w, w), UnityEngine.Random.Range(-hh, hh));
                        bool brass = m % 3 == 0;
                        Tween.After(dl, () => Mote(fx, p0, brass ? PaperFx.BrassLight : PaperFx.Paper));
                    }
                }
            });
        }

        /// <summary>演出の途中で HP バーだけ先に動かす (順送りの敵フェーズ: 被弾のたびに減る)</summary>
        public void NudgeEnemyHp(int index, int delta)
        {
            if (index < 0 || index >= _enemyPanels.Count || index >= _shownEnemyHp.Length) return;
            _shownEnemyHp[index] = Math.Max(0, _shownEnemyHp[index] + delta);
            BattleScreen.TweenHpBar(_enemyPanels[index], _shownEnemyHp[index]);
        }

        public void NudgePlayerHp(int delta)
        {
            if (_playerArea == null) return;
            _shownPlayerHp = Math.Max(0, _shownPlayerHp + delta);
            BattleScreen.TweenHpBar(SelfArea, _shownPlayerHp);   // 札は箱庭では兄弟の入れ物 (SelfArea。P20 2周目)
            BattleScreen.RefreshIncomingLine(SelfArea, _shownPlayerBlock, _shownPlayerIce, _shownPlayerHp);
        }

        /// <summary>順送りで敵の攻撃が届いた (ブロック前の量)。受けるダメージの見込みから引き、残りの敵の攻撃だけを言う＝HP と見込みで二重に引かない。
        /// HP バーの削られる分の帯は左端 (見込みの残り HP) を保ったまま、塗りが縮んだ分だけ短くなる (2026-09-29 p08)</summary>
        public void LandPlayerIncoming(int amount)
        {
            if (_playerArea == null) return;
            BattleScreen.ConsumeIncoming(SelfArea, amount);
            BattleScreen.RefreshIncomingLine(SelfArea, _shownPlayerBlock, _shownPlayerIce, _shownPlayerHp);
        }

        // ---- ブロックの数字を演出の途中で動かす (2026-09-17) ----
        // 順送りの敵フェーズは古い盤面の上で見せるので、置物・レリック・仕込み札で得たブロックと敵の攻撃が削った分を
        // 帳面の盾の数字にその場で反映する (HP バーの Nudge と同じ考え)。組み直しの時に実値へ揃う

        /// <summary>今見えている自分のブロック (順送りの途中の値)</summary>
        public int ShownPlayerBlock { get { return _shownPlayerBlock; } }

        public void NudgePlayerBlock(int delta)
        {
            SetPlayerBlock(_shownPlayerBlock + delta);
        }

        public void SetPlayerBlock(int value)
        {
            if (_playerArea == null) return;
            value = Math.Max(0, value);
            bool changed = value != _shownPlayerBlock;
            _shownPlayerBlock = value;
            if (changed) { BattleScreen.SetPlayerBlockBadge(SelfArea, value); BattleScreen.RefreshIncomingLine(SelfArea, value, _shownPlayerIce, _shownPlayerHp); }
        }

        /// <summary>敵の攻撃が吸われた量を「通常ブロック→氷壁」の順で差し引く (エンジンの消費順と同じ)</summary>
        public void AbsorbPlayerBlock(int blockedTotal)
        {
            if (blockedTotal <= 0) return;
            int fromBlock = Math.Min(blockedTotal, _shownPlayerBlock);
            int fromIce = Math.Min(blockedTotal - fromBlock, _shownPlayerIce);
            if (fromBlock > 0) NudgePlayerBlock(-fromBlock);
            if (fromIce > 0) NudgePlayerIce(-fromIce);
        }

        public void NudgePlayerIce(int delta)
        {
            if (_playerArea == null) return;
            _shownPlayerIce = Math.Max(0, _shownPlayerIce + delta);
            BattleScreen.SetPlayerIceText(SelfArea, _shownPlayerIce);
            BattleScreen.RefreshIncomingLine(SelfArea, _shownPlayerBlock, _shownPlayerIce, _shownPlayerHp);   // 見込みのブロックは氷壁も足す (p08)
        }

        public void NudgeEnemyBlock(int index, int delta)
        {
            if (index < 0 || index >= _shownEnemyBlock.Length) return;
            SetEnemyBlock(index, _shownEnemyBlock[index] + delta);
        }

        public void SetEnemyBlock(int index, int value)
        {
            if (index < 0 || index >= _enemyPanels.Count || index >= _shownEnemyBlock.Length || _enemyPanels[index] == null) return;
            value = Math.Max(0, value);
            bool changed = value != _shownEnemyBlock[index];
            _shownEnemyBlock[index] = value;
            if (changed) BattleScreen.SetEnemyBlockBadge(_enemyPanels[index], value);
        }

        /// <summary>敵フェーズの始まりで敵のブロックは失効する (潜伏の殻は残る)。順送りの TurnEnded で呼ぶ</summary>
        public void ResetEnemyBlocks(GameState visible)
        {
            for (int i = 0; i < _shownEnemyBlock.Length; i++)
            {
                bool shell = visible != null && i < visible.Enemies.Count && visible.Enemies[i].BurrowActive == true;
                if (!shell) SetEnemyBlock(i, 0);
            }
        }

        /// <summary>確認の窓 (発動/温存) を閉じる: 順送りの続き (発動の後の敵の行動) を古い盤面の上で見せる前に、窓と暗がりだけ先に畳む (2026-09-17)</summary>
        public void CloseReactionWindow()
        {
            BattleScreen.RestoreSelfStrip(SelfArea);   // 窓が上げられず打ち切った自分の札を元の幅に (2026-09-29 p11)
            BattleScreen.R3U_RestoreDollTags();         // r3: 窓と重なって畳んだ人形の足元の札を戻す (2026-10-02 仕様 §8)
            if (UiLayer == null) return;
            for (int i = UiLayer.childCount - 1; i >= 0; i--)
            {
                var ch = UiLayer.GetChild(i) as RectTransform;
                if (ch == null) continue;
                if (ch.name == "reaction")
                {
                    var cg = ch.gameObject.GetComponent<CanvasGroup>() ?? ch.gameObject.AddComponent<CanvasGroup>();
                    cg.blocksRaycasts = false; cg.interactable = false;
                    var rt = ch; var g0 = cg;
                    Tween.Run(0.16f, k => { if (rt == null) return; float sc = 1f - 0.12f * k; rt.localScale = new Vector3(sc, sc, 1f); if (g0 != null) g0.alpha = 1f - k; }, Ease.OutCubic, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
                }
                else if (ch.name == "dim")
                {
                    var img = ch.GetComponent<Image>();
                    if (img != null) img.raycastTarget = false;
                    var rt = ch; var im = img; float a0 = img != null ? img.color.a : 0f;
                    Tween.Run(0.16f, k => { if (im != null) { var c = im.color; c.a = a0 * (1f - k); im.color = c; } }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
                }
            }
        }

        // ---- 手札 ----

        public HandCard CardAt(int index)
        {
            foreach (var kv in _hand) if (kv.Value.Index == index) return kv.Value;
            return null;
        }

        /// <summary>予測に使う敵: 狙いを付けた敵、無ければ生存が1体の時だけその敵 (SyncHand と同じ規則)。庇われている敵を狙っていれば護衛 (単体の札は護衛に向かう。2026-09-29 p01)</summary>
        public static int PreviewTargetFor(GameRoot g, GameState st)
        {
            int alive = 0, firstAlive = -1;
            for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) { alive++; if (firstAlive < 0) firstAlive = i; }
            return g.PreferredTarget >= 0 && g.PreferredTarget < st.Enemies.Count && st.Enemies[g.PreferredTarget].Hp > 0 ? BattleScreen.GuardRedirect(st, g.PreferredTarget) : (alive == 1 ? firstAlive : -1);
        }

        /// <summary>ドラッグ中に敵の上へ来た/離れた時、その札だけ描き直す (本家と同じく敵に当てた時に数字が変わる。2026-09-09)</summary>
        public void RefreshHandCard(GameRoot g, GameState st, HandCard hc, int previewEnemy)
        {
            int preview = previewEnemy >= 0 ? BattleScreen.GuardRedirect(st, previewEnemy) : PreviewTargetFor(g, st);
            if (hc.Preview == preview) return;
            hc.Preview = preview;
            CardView.PreviewEnemy = preview;
            CardView.CompactName = hc.Compact;   // 手札の今の名前の置き方のまま描き直す (p09)
            CardView.BodyRightInset = hc.BodyInset;
            CardView.InHand = true;
            try { CardView.Refill(hc.Rt, hc.Card, st, hc.Playable || hc.Settable, true); }   // 面は「出せる、または仕込める」(判定は Playable のまま。2026-09-29 p05)
            finally { CardView.PreviewEnemy = -1; CardView.CompactName = false; CardView.BodyRightInset = 0f; CardView.InHand = false; }
            if (BattleScreen.R3) BattleScreen.R3A_HoverCatch(hc.Rt);   // r3: Refill が子を全部捨てるので、上がった札の下の透明な的を付け直す
            if (BattleScreen.R3 && hc.Rt.localScale.x > BattleScreen.CardScale + 0.01f) CardView.SetKeyNumVisible(hc.Rt, false);   // 上がっている札は要の数字の札を隠したまま
        }

        // ---- 狙いの矢 (スマホ。2026-09-29 p09 ユーザー裁定「狙いの矢だけ」) ----
        // 敵を狙う札 (選択式でない単体の札) をドラッグすると、札は手札の元の位置・等倍・回転0に留まり、札の上端の中央から指まで
        // 真鍮の点 (2次ベジェの上に14個) と指先の輪が伸びる (本家 StS の対象指定と同じ形)。旧: 0.8倍の札の中心が指の真下に来て、
        // 描き直した与ダメの数字 (2026-09-09「敵に当てた時に数字が変わる」) も狙った敵の帳面も親指と札に隠れていた。
        // 指が画面の下 36% (EndDrag の取り消しの線) にある間は矢を隠す＝そこで離すと取り消し。
        // 指の下の敵が変わった時だけ、その敵 (庇われていれば護衛＝札の向かう先) の意図の札と帳面に真鍮の縁 "dragedge" を付ける。
        // 点は敵の帳面・意図の札の上には描かない (紙の後ろを通って見える＝HP と意図の数字を点で隠さない)。
        // 置き場は UiLayer (組み直しの ClearUi で一緒に消え、次の Drag で作り直す)。どれも raycastTarget=false
        const int AimDotCount = 14;
        const float AimDotSize = 10f, AimDotRim = 13f, AimRingSize = 48f;
        RectTransform _aim, _aimLine, _aimRing;
        readonly List<RectTransform> _aimDots = new List<RectTransform>();
        readonly List<GameObject> _aimEdges = new List<GameObject>();
        int _aimEdgeIdx = -1;
        static readonly string[] AimBlockers = { "strip", "intent-tag" };
        static readonly Vector3[] _aimCorners = new Vector3[4];
        static Sprite _aimDotSprite;

        /// <summary>狙いの矢を指の位置 (画面の座標) まで引き直す。over = 指の下の敵 (-1 = 無し)。札は手札の元の位置に押さえる</summary>
        public void UpdateAimArrow(GameRoot g, GameState st, HandCard hc, Vector2 screenPos, int over)
        {
            if (UiLayer == null || hc == null || hc.Rt == null || st == null) return;
            if (_aim == null) BuildAimArrow();   // 組み直し (ClearUi) で消えていれば作り直す
            var pin = _aim.GetComponent<AimPin>();
            if (pin != null) { pin.Card = hc.Rt; pin.Pos = hc.BasePos; pin.Scale = BattleScreen.CardScale; pin.Apply(); }
            bool show = screenPos.y > BattleScreen.DropLineScreen(Root);   // 取り消しの線 = EndDrag の「場に出す」線 (r3 は沈めた手札の上端＋111)
            _aimLine.gameObject.SetActive(show);
            SetAimEdge(g, st, show ? over : -1);
            // 撃とうとしている札に真鍮の縁 (2026-09-30 F36: 札は手札の元の位置のままで、どの札から矢が出ているかが形から読めなかった)。
            // 札の子は RefreshHandCard の描き直しで全部消えるので、無ければ付け直す。取り消しの線より下では矢と一緒に隠す
            var de = hc.Rt.Find("dragedge");
            if (de == null)
            {
                var e = PaperFx.Sheet(hc.Rt, PaperFx.Tag, "dragedge", PaperFx.BrassLight);
                UiKit.Stretch(e.rectTransform, -5f, -5f, -5f, -5f);
                e.raycastTarget = false;
                e.transform.SetAsFirstSibling();   // 紙の後ろ＝縁だけが外に見える
                de = e.transform;
            }
            de.gameObject.SetActive(show);
            if (!show) return;
            Vector2 p2;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_aim, screenPos, null, out p2)) return;
            Vector2 p0 = _aim.InverseTransformPoint(hc.Rt.TransformPoint(new Vector3(0f, CardView.H / 2f + 8f, 0f)));   // 札の上端の中央の 8 上 (押さえた直後なので回転0・等倍)
            // 札からまっすぐ立ち上がり、指より少し高い所から入る (F35: 旧は指と同じ高さを水平に走り、奥の敵を狙うと手前の敵の頭の上を横切った)
            Vector2 p1 = new Vector2(p0.x, Mathf.Max(p0.y, p2.y) + 70f);
            // リーダーの体を横切るなら、頂点を狙いの側へ倒す (2026-09-30 最終の答え合わせ: スマホで扇が左へ寄り、左端の札から立ち上がる矢がリーダーの体を縦に貫いた)。
            // 倒す量は 0.15 刻みで 0.6 まで。体の上に点を描かないのではなく、体を避けて通す (矢は手前の札から出ているので、体の後ろを通って見えるのは嘘)
            var leaderSpr = g.Anchor("player") != null ? g.Anchor("player").Find("sprite") as RectTransform : null;
            if (leaderSpr != null && leaderSpr.gameObject.activeInHierarchy)
            {
                var lb = AimBodyRect(leaderSpr);
                for (int step = 1; step <= 4 && CurveHits(p0, p1, p2, lb); step++)
                    p1 = new Vector2(Mathf.Lerp(p0.x, p2.x, 0.15f * step), p1.y);
            }
            // 敵の帳面と意図の札 (生きている敵) の矩形: この上に来る点は描かない。狙っていない敵の体の上にも描かない (その敵の後ろを通って見える。F35)
            int aimIdx = over >= 0 && over < st.Enemies.Count && st.Enemies[over].Hp > 0 ? BattleScreen.GuardRedirect(st, over) : -1;
            var blocks = new List<Rect>();
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                if (st.Enemies[i].Hp <= 0) continue;
                var pan = g.Anchor("enemy" + i);
                if (pan == null) continue;
                foreach (var nm in AimBlockers)
                {
                    var r = pan.Find(nm) as RectTransform;
                    if (r != null) blocks.Add(LocalRectIn(_aim, r));
                }
                if (i != over && i != aimIdx)
                {
                    var spr = pan.Find("sprite") as RectTransform;
                    if (spr != null && spr.gameObject.activeInHierarchy) blocks.Add(AimBodyRect(spr));
                }
            }
            // 点は曲線の長さで等間隔に (t で等分すると、横に長い曲線では指の近くに点が詰まる)
            const int samples = 48;
            var pts = new Vector2[samples + 1];
            var acc = new float[samples + 1];
            for (int k = 0; k <= samples; k++)
            {
                float t = k / (float)samples, u = 1f - t;
                pts[k] = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                acc[k] = k == 0 ? 0f : acc[k - 1] + (pts[k] - pts[k - 1]).magnitude;
            }
            float total = acc[samples];
            float clear = AimRingSize / 2f + 4f;   // 指先の輪の内側にも点は置かない
            int seg = 1;
            for (int i = 0; i < _aimDots.Count; i++)
            {
                var d = _aimDots[i];
                if (d == null) continue;
                float s = total * i / (float)AimDotCount;   // 最初の点は札のすぐ上 (F36: 旧は1間隔ぶん先から始まり、札から約50浮いて見えた)
                while (seg < samples && acc[seg] < s) seg++;
                float span = acc[seg] - acc[seg - 1];
                Vector2 p = span > 0.001f ? Vector2.Lerp(pts[seg - 1], pts[seg], (s - acc[seg - 1]) / span) : pts[seg];
                d.anchoredPosition = p;
                bool hide = (p - p2).sqrMagnitude < clear * clear;
                for (int b = 0; b < blocks.Count && !hide; b++) if (blocks[b].Contains(p)) hide = true;
                d.gameObject.SetActive(!hide);
            }
            if (_aimRing != null) _aimRing.anchoredPosition = p2;
            UpdateAimNote(g, st, over, p2);
        }

        string _aimNoteText;
        RectTransform _aimNote;

        /// <summary>指の下に敵がいない間の案内 (2026-09-30 F34): 離すと前の狙いへ撃つ (タップと同じ) ので「離すと ①牙嵐の狼 へ」、前の狙いが無く敵が2体以上なら
        /// 「離すと 敵を選ぶ」。指先の輪の右上に夜色の札。前の狙いが無い時は輪の芯を淡い紙色に (真鍮＝その先へ撃つ、ではない合図)。操作は変えない</summary>
        void UpdateAimNote(GameRoot g, GameState st, int over, Vector2 p2)
        {
            string text = null;
            bool dimRing = false;
            if (over < 0)
            {
                int alive = 0; for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) alive++;
                int pt = g.PreferredTarget;
                if (pt >= 0 && pt < st.Enemies.Count && st.Enemies[pt].Hp > 0 && alive >= 2)
                {
                    int ti = BattleScreen.GuardRedirect(st, pt);
                    string nm;
                    try { nm = Content.GetEnemyDef(st.Enemies[ti].EnemyId).Name; } catch (Exception) { nm = st.Enemies[ti].EnemyId; }
                    string circled = "①②③④⑤⑥⑦⑧";
                    text = "離すと " + (ti < circled.Length ? circled[ti].ToString() : "") + nm + " へ";
                }
                else if (alive >= 2) { text = "離すと 敵を選ぶ"; dimRing = true; }
            }
            if (_aimRing != null)
            {
                var core = _aimRing.Find("brass");
                var ci = core != null ? core.GetComponent<Image>() : null;
                if (ci != null) ci.color = dimRing ? PaperFx.PaperDim : PaperFx.Brass;
            }
            if (text != _aimNoteText || (_aimNote == null && text != null))
            {
                if (_aimNote != null) { _aimNote.SetParent(null, false); UnityEngine.Object.Destroy(_aimNote.gameObject); _aimNote = null; }
                _aimNoteText = text;
                if (text != null && _aimLine != null)
                {
                    _aimNote = PaperFx.NightNote(_aimLine, text, 15, 320f, true, "aimnote");
                    _aimNote.anchorMin = _aimNote.anchorMax = new Vector2(0.5f, 0.5f);
                    _aimNote.pivot = new Vector2(0f, 0f);
                }
            }
            if (_aimNote != null)
            {
                float rr = AimRingSize / 2f + 8f;
                var pos = p2 + new Vector2(rr, rr);
                // 画面の右に出るなら輪の左へ
                var half = _aim.rect.size / 2f;
                if (pos.x + _aimNote.sizeDelta.x > half.x - 8f) { pos.x = p2.x - rr - _aimNote.sizeDelta.x; }
                if (pos.y + _aimNote.sizeDelta.y > half.y - 8f) pos.y = p2.y - rr - _aimNote.sizeDelta.y;
                _aimNote.anchoredPosition = pos;
            }
        }

        /// <summary>2次ベジェ p0→p1→p2 が矩形を通るか (24 分割の点で見る)</summary>
        static bool CurveHits(Vector2 p0, Vector2 p1, Vector2 p2, Rect r)
        {
            for (int k = 1; k < 24; k++)
            {
                float t = k / 24f, u = 1f - t;
                if (r.Contains(u * u * p0 + 2f * u * t * p1 + t * t * p2)) return true;
            }
            return false;
        }

        /// <summary>敵の絵の体の矩形 (_aim の座標・透明な余白を除く＋点の半径ぶん広げる)。狙いの矢の点を描かない範囲 (F35)</summary>
        Rect AimBodyRect(RectTransform spr)
        {
            var r = LocalRectIn(_aim, spr);
            var img = spr.GetComponent<Image>();
            var sp = img != null ? img.sprite : null;
            if (sp != null && sp.rect.width > 0f)
            {
                float k = r.width / sp.rect.width;
                var side = Creature.SideMargins(sp);
                float top = Creature.TopMargin(sp) * k;
                var body = Rect.MinMaxRect(r.xMin + side.x * k, r.yMin, r.xMax - side.y * k, r.yMax - top);
                if (body.width > 0f && body.height > 0f) r = body;
            }
            float pad = AimDotRim / 2f;
            return Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad);
        }

        /// <summary>狙いの矢と狙いの縁を片付ける (EndDrag。札の押さえも外れる)</summary>
        public void ClearAimArrow()
        {
            if (_aim != null)
            {
                var pin = _aim.GetComponent<AimPin>();
                if (pin != null)
                {
                    var de = pin.Card != null ? pin.Card.Find("dragedge") : null;   // 撃とうとしていた札の縁 (F36)。取り消しで扇へ戻る札に残さない
                    if (de != null) { de.gameObject.SetActive(false); UnityEngine.Object.Destroy(de.gameObject); }
                    pin.Card = null; pin.enabled = false;   // Destroy は次のフレームなので、この後の LateUpdate で札を引き戻さない
                }
                _aim.SetParent(null, false); UnityEngine.Object.Destroy(_aim.gameObject);
            }
            _aim = null; _aimLine = null; _aimRing = null; _aimNote = null; _aimNoteText = null;
            _aimDots.Clear();
            ClearAimEdges();
        }

        void BuildAimArrow()
        {
            _aimDots.Clear();
            _aim = UiKit.NewRect("aimline", UiLayer);
            UiKit.Stretch(_aim, 0f, 0f, 0f, 0f);
            var cg = _aim.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false;
            _aim.gameObject.AddComponent<AimPin>();
            _aimLine = UiKit.NewRect("line", _aim);
            UiKit.Stretch(_aimLine, 0f, 0f, 0f, 0f);
            // 点: 真鍮の芯 (10) に墨の縁 (13)。夜の舞台でも紙の上でも輪郭が立つ
            for (int i = 0; i < AimDotCount; i++)
            {
                var d = UiKit.NewRect("dot", _aimLine);
                d.anchorMin = d.anchorMax = new Vector2(0.5f, 0.5f);
                d.sizeDelta = new Vector2(AimDotRim, AimDotRim);
                var di = d.gameObject.AddComponent<Image>();
                di.sprite = AimDotSprite(); di.color = PaperFx.Ink; di.raycastTarget = false;
                var core = UiKit.NewRect("core", d);
                core.anchorMin = core.anchorMax = new Vector2(0.5f, 0.5f);
                core.sizeDelta = new Vector2(AimDotSize, AimDotSize); core.anchoredPosition = Vector2.zero;
                var ci = core.gameObject.AddComponent<Image>();
                ci.sprite = AimDotSprite(); ci.color = PaperFx.Brass; ci.raycastTarget = false;
                _aimDots.Add(d);
            }
            // 指先の輪: 真鍮の輪 (48) の外側に墨の縁
            _aimRing = UiKit.NewRect("ring", _aimLine);
            _aimRing.anchorMin = _aimRing.anchorMax = new Vector2(0.5f, 0.5f);
            _aimRing.sizeDelta = new Vector2(AimRingSize + 4f, AimRingSize + 4f);
            var ri = _aimRing.gameObject.AddComponent<Image>();
            ri.sprite = PaperFx.Ring(10); ri.color = PaperFx.Ink; ri.raycastTarget = false;
            var rc = UiKit.NewRect("brass", _aimRing);
            rc.anchorMin = rc.anchorMax = new Vector2(0.5f, 0.5f);
            rc.sizeDelta = new Vector2(AimRingSize, AimRingSize); rc.anchoredPosition = Vector2.zero;
            var rci = rc.gameObject.AddComponent<Image>();
            rci.sprite = PaperFx.Ring(6); rci.color = PaperFx.Brass; rci.raycastTarget = false;
        }

        /// <summary>指の下の敵 (庇われていれば護衛) の帳面と意図の札に真鍮の縁。敵が変わった時と、組み直しで縁が消えた時だけ付け直す</summary>
        void SetAimEdge(GameRoot g, GameState st, int over)
        {
            int idx = over >= 0 && over < st.Enemies.Count && st.Enemies[over].Hp > 0 ? BattleScreen.GuardRedirect(st, over) : -1;
            bool stale = false;
            foreach (var o in _aimEdges) if (o == null) { stale = true; break; }
            if (idx == _aimEdgeIdx && !stale) return;
            ClearAimEdges();
            _aimEdgeIdx = idx;
            if (idx < 0) return;   // 指の下に敵がいない＝離すと今までの狙い (PreferredTarget) に撃つ。その敵の縁 "edge" はそのまま見せる
            var pan = g.Anchor("enemy" + idx);
            if (pan == null) return;
            foreach (var nm in AimBlockers)
            {
                var host = pan.Find(nm);
                if (host == null) continue;
                var edge = PaperFx.Sheet(host, PaperFx.Tag, "dragedge", PaperFx.Brass);   // 狙っている敵の縁 (LedgerStrip・IntentTag の "edge") と同じ形
                UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f);
                edge.raycastTarget = false;
                edge.transform.SetAsFirstSibling();   // 紙の後ろ＝縁だけが外に見える
                _aimEdges.Add(edge.gameObject);
            }
            // 別の敵に付いている前の狙いの縁は、指を離すまで隠す (真鍮の縁が2体に付くと、どちらへ撃つのか分からない。離すと指の下の敵が新しい狙いになる)
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                if (i == idx) continue;
                var other = g.Anchor("enemy" + i);
                if (other == null) continue;
                foreach (var nm in AimBlockers)
                {
                    var host = other.Find(nm);
                    var old = host != null ? host.Find("edge") : null;
                    if (old != null && old.gameObject.activeSelf) { old.gameObject.SetActive(false); _aimHidden.Add(old.gameObject); }
                }
            }
        }

        readonly List<GameObject> _aimHidden = new List<GameObject>();   // 指で別の敵を狙っている間だけ隠した前の狙いの縁

        void ClearAimEdges()
        {
            foreach (var o in _aimEdges) if (o != null) UnityEngine.Object.Destroy(o);
            _aimEdges.Clear();
            foreach (var o in _aimHidden) if (o != null) o.SetActive(true);
            _aimHidden.Clear();
            _aimEdgeIdx = -1;
        }

        static Rect LocalRectIn(RectTransform space, RectTransform r)
        {
            r.GetWorldCorners(_aimCorners);
            Vector2 a = space.InverseTransformPoint(_aimCorners[0]), b = a;
            for (int k = 1; k < 4; k++) { Vector2 p = space.InverseTransformPoint(_aimCorners[k]); a = Vector2.Min(a, p); b = Vector2.Max(b, p); }
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        /// <summary>白い丸 (32×32・縁はなめらか)。色は Image で乗せる (狙いの矢の点)</summary>
        static Sprite AimDotSprite()
        {
            if (_aimDotSprite != null) return _aimDotSprite;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            float c = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(c - r));
                }
            tex.SetPixels(px); tex.Apply(false, false);
            _aimDotSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _aimDotSprite.name = "aim-dot";
            return _aimDotSprite;
        }

        /// <summary>ドラッグ中の札を手札の元の位置・等倍・回転0に押さえる (押した時の持ち上げのトゥイーンが後から上書きしないよう LateUpdate で)</summary>
        class AimPin : MonoBehaviour
        {
            public RectTransform Card; public Vector2 Pos; public float Scale = 1f;
            public void Apply()
            {
                if (Card == null) return;
                Card.anchoredPosition = Pos; Card.localScale = new Vector3(Scale, Scale, 1f); Card.localRotation = Quaternion.identity;
            }
            void LateUpdate() { Apply(); }
        }

        /// <summary>
        /// この札を今プレイできるか (2026-09-29 p12: 扇の沈み・札の面・モードの窓・ターン終了の合図が同じ関数を読む)。
        /// 自分の番・狙いを選んでいない・手札から出せる型・実際に払うコスト ≤ エナジー・灯コスト ≤ 灯・人形の要件・拘束／首輪の上限 (Combat.PlayCapOf)。
        /// 仕込めるかは別 (SetBase.CanSetCard)
        /// </summary>
        public static bool CanActNow(GameRoot g, GameState st, CardInstance c)
        {
            if (st == null || c == null || st.Phase != CombatPhases.PlayerTurn || (g != null && g.Pending != null)) return false;
            if (!Effects.IsPlayableFromHand(c, st)) return false;
            int cost = c.Def.Cost;
            try { cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
            if (cost > st.Player.Energy) return false;
            if ((c.Def.LightCost ?? 0) > (st.Player.Light ?? 0)) return false;   // 灯コスト (白 2026-09-20)
            if (!Effects.RetainerRequirementMet(st, c)) return false;
            var cap = Combat.PlayCapOf(st);
            if (cap != null && (st.Player.PlaysThisTurn ?? 0) >= cap.Value) return false;
            return true;
        }

        /// <summary>r3 の扇の下がり (中央から1枚ごと。二周目は 10)</summary>
        const float R3U_FanDrop = 4f, R3U_FanDropMax = 12f, R3U_FanTiltMax = 6f;
        /// <summary>r3 の出せない札の沈み (二周目は 16)。0 = 沈めず、紙の色と灰の玉だけで知らせる (要の数字の札の門 36。hd2d-seatfit.py の R3_FAN unplay_sink と同じ値)</summary>
        const float R3U_UnplayableSink = 0f;

        public void SyncHand(GameRoot g, GameState st, bool animate)
        {
            var hand = st.Player.Hand;
            int n = hand.Count;
            // 扇の幅: スマホはエナジーの円盤・灯籠とターン終了の間 (キャンバス幅 − 610) に、両端の札の外縁まで収める (2026-09-29 p12)。
            // 旧 (2026-09-14) は間隔を handSpan/n で割っていたので両端の札が枠の外へ出て、5枚目がターン終了の縁に重なっていた (6枚以上で 20 以上)。
            // 610 は、ターン終了の左端 (キャンバス幅 − 254) との間を 5枚で 43・10枚で 25 以上 (傾き −13.5° の角を込み) 空ける値。PC は今のまま
            // 2026-09-30 F01: スマホは右端 (回転前の最後の札の右の縁) を p12 の cs.x−305 のまま固定し、左端を左の列 (山札・エナジーの輪・灯籠) の右＋24 まで広げ、
            // 扇をその間の中央に置く。旧は扇をキャンバスの中央に置いたので左に約115 空いたまま5枚でも37 重なり、仕込み札の本文の右端 (「+2」「打ち消し」) が隣の札に隠れた。
            // 左の余白には1枚目の傾き (左上の角が左へ出る量) も足す (押せる面どうし 24 以上＝I14)
            float cw = CardView.W * BattleScreen.CardScale;
            float spacing, fanShift = 0f;
            if (UiKit.Phone)
            {
                float csx = BattleScreen.CanvasSize(Root).x;
                float leftCol = LightUi.ShouldShow(g.Rs, st) ? UiKit.Edge + 128f + 12f + LightUi.DotsW * 2.5f : UiKit.Edge + 150f;   // 灯籠の右端 or 山札の右端
                float tilt = n > 1 ? (BattleScreen.R3 ? Mathf.Min((n - 1) / 2f * 3f, R3U_FanTiltMax) : (n - 1) / 2f * 3f) * Mathf.Deg2Rad : 0f;
                float overhang = Mathf.Max(0f, CardView.H / 2f * Mathf.Sin(tilt) - CardView.W / 2f * (1f - Mathf.Cos(tilt))) * BattleScreen.CardScale;
                float L = leftCol + 24f + overhang, R = csx - 305f;
                spacing = n > 1 ? Mathf.Max(20f, Mathf.Min(cw + 12f, (R - L - cw) / (n - 1))) : 0f;
                fanShift = (L + R) / 2f - csx / 2f;
            }
            else spacing = n <= 0 ? 0f : Mathf.Min(cw + 12f, 1180f / n);
            // 覆われる札の本文の右の余白 (スマホ・6枚以下。7枚以上は名前の左寄せと長押しの拡大で読む)。本文の枠の右端 (余白14) を隣の札の縁＋傾きの4＋4 まで下げる
            float coverInset = 0f;
            if (UiKit.Phone && n >= 2 && n <= 6)
            {
                float coverLocal = (cw - spacing) / BattleScreen.CardScale;
                coverInset = Mathf.Round(Mathf.Clamp(coverLocal + 8f - 14f, 0f, 32f));
            }
            float center = (n - 1) / 2f;
            float areaH = CardView.H * BattleScreen.CardScale + 40f;
            bool myTurn = st.Phase == CombatPhases.PlayerTurn;
            // スマホで7枚以上は札が半分近く重なり、中央寄せの名前 (x44〜188) が隣の札に隠れる (10枚で見える幅 72)。
            // 見えている左側に読めるよう、手札の札を全部同時に名前だけ左寄せ (コスト玉の右から) で描く。拡大の窓・報酬・店・PC は中央のまま (2026-09-29 p09)
            bool compact = UiKit.Phone && n >= 7;

            // 予測の対象: 狙いを付けた敵 (庇われていれば護衛)、無ければ生存が1体の時だけその敵
            int preview = PreviewTargetFor(g, st);

            // 1) 手札から消えた札 → 行き先へ飛ばして消す
            var gone = new List<string>();
            foreach (var kv in _hand)
            {
                bool still = false;
                for (int i = 0; i < n; i++) if (hand[i].Uid == kv.Key) { still = true; break; }
                if (!still) gone.Add(kv.Key);
            }
            float outDelay = 0f;
            foreach (var uid in gone)
            {
                var hc = _hand[uid];
                _hand.Remove(uid);
                AnimateOut(g, st, hc, outDelay);
                outDelay += 0.04f;
            }

            // 1') 捨てが起きたターンの切り替わりは、捨てが飛び終わってからドローする
            float drawDelay = gone.Count > 0 && LastPlayedUid == null ? 0.28f : 0f;
            // 2) 新しい札 → 山札の位置に作る
            var drawRt = g.Anchor("pile-draw");
            Vector2 drawPos = drawRt != null ? Tween.CenterIn(drawRt, HandLayer) : new Vector2(-900f, -100f);
            int newCount = 0;
            for (int i = 0; i < n; i++)
            {
                var c = hand[i];
                int cost = c.Def.Cost;
                try { cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
                bool playable = CanActNow(g, st, c);   // ターン終了の合図と同じ判定 (2026-09-29 p12。拘束・首輪の上限も見る)
                bool settable = myTurn && g.Pending == null && SetBase.CanSetCard(st, c.Uid);
                // 札の面は「プレイできる、または仕込める」で明るく描く (仕込み札を「出せない札」の灰色にしない。2026-09-29 p05)。
                // hc.Playable / hc.Settable は判定用 (ドラッグ・クリックの分岐・16px 沈める) にそのまま持つ
                bool face = playable || settable;
                float inset = i < n - 1 ? coverInset : 0f;   // いちばん右 (最前面) の札は覆われない
                HandCard hc;
                bool fresh = !_hand.TryGetValue(c.Uid, out hc);
                if (!fresh && (hc.Playable != playable || hc.Settable != settable || !ReferenceEquals(hc.Card.Def, c.Def) || hc.Card.GrowBonus != c.GrowBonus || hc.Cost != cost || hc.Preview != preview || hc.Compact != compact || hc.BodyInset != inset))
                {
                    // 見た目が変わる (プレイ可否・鍛え・育つ・コスト・狙った敵・名前の置き方) → 同じ位置で作り直す
                    var pos = hc.Rt.anchoredPosition; var rot = hc.Rt.localRotation; var scl = hc.Rt.localScale;
                    hc.Rt.SetParent(null, false);
                    UnityEngine.Object.Destroy(hc.Rt.gameObject);
                    _hand.Remove(c.Uid);
                    fresh = true;
                    hc = null;
                    CardView.PreviewEnemy = preview;
                    CardView.CompactName = compact;
                    CardView.BodyRightInset = inset; CardView.InHand = true;
                    RectTransform rt2;
                    try { rt2 = CardView.Build(HandLayer, c, st, face, true, "hand" + i); }
                    finally { CardView.PreviewEnemy = -1; CardView.CompactName = false; CardView.BodyRightInset = 0f; CardView.InHand = false; }
                    rt2.anchoredPosition = pos; rt2.localRotation = rot; rt2.localScale = scl;
                    if (BattleScreen.R3 && scl.x > BattleScreen.CardScale + 0.01f) CardView.SetKeyNumVisible(rt2, false);   // 触れて上がっている札は要の数字の札を隠したまま
                    hc = new HandCard { Rt = rt2, Card = c, Playable = playable, Settable = settable, Cost = cost, Preview = preview, Compact = compact, BodyInset = inset };
                    _hand[c.Uid] = hc;
                    fresh = false;
                    Attach(g, hc, c);
                }
                if (fresh)
                {
                    CardView.PreviewEnemy = preview;
                    CardView.CompactName = compact;
                    CardView.BodyRightInset = inset; CardView.InHand = true;
                    RectTransform rt;
                    try { rt = CardView.Build(HandLayer, c, st, face, true, "hand" + i); }
                    finally { CardView.PreviewEnemy = -1; CardView.CompactName = false; CardView.BodyRightInset = 0f; CardView.InHand = false; }
                    hc = new HandCard { Rt = rt, Card = c, Playable = playable, Settable = settable, Cost = cost, Preview = preview, Compact = compact, BodyInset = inset };
                    _hand[c.Uid] = hc;
                    Attach(g, hc, c);
                    if (animate)
                    {
                        rt.anchoredPosition = drawPos;
                        rt.localScale = Vector3.one * 0.35f;
                        rt.localRotation = Quaternion.Euler(0f, 0f, -20f);
                        CardBack(rt);   // めくり (2026-09-17 ⑦): 山札から飛ぶ間は裏、途中で表に返る
                    }
                    newCount++;
                }
                hc.Card = c;
                hc.Index = i;
                hc.Rt.name = "hand" + i;
                float dx = (i - center) * spacing + fanShift;
                // r3 は扇の下がり 4 で 12 まで・傾きは端で ±6° まで (仕様 §4 の 4 に加えて頭打ち: 傾き 3°×枚数のままだと 7枚以上で左端の札の左上 = 要の数字の札が
                // 回転で下がり、画面の下から PC 35px・スマホ 30px を割った。頭打ちで 10枚でも PC 40px・スマホ 42px 以上。5枚以下は仕様のまま)
                float dy = BattleScreen.R3 ? -Mathf.Min(Mathf.Abs(i - center) * R3U_FanDrop, R3U_FanDropMax) : -Mathf.Abs(i - center) * 10f;
                // ⑦ (2026-09-17): 出せない札 (エナジー不足など) は扇の中で少し沈む。r3 は沈めない (R3U_UnplayableSink 0): 沈めた手札で端の札が 16 下がると
                // 要の数字の札の下端が画面の下から PC 24・スマホ 21px まで落ち、門 36 を割った (2026-10-02 読み合わせの指摘。ターンの終わりは全部の札が出せない)。
                // 合図は紙の沈んだ色 (CardView の PaperFx.DimTint) と灰のコスト玉が担う
                if (myTurn && g.Pending == null && !playable && !settable) dy -= BattleScreen.R3 ? R3U_UnplayableSink : 16f;
                hc.BasePos = new Vector2(dx, -areaH / 2f + CardView.H * BattleScreen.CardScale / 2f + dy);
                hc.BaseRot = BattleScreen.R3 ? Mathf.Clamp(-(i - center) * 3f, -R3U_FanTiltMax, R3U_FanTiltMax) : -(i - center) * 3f;
            }
            // 3) 並び順と位置
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                HandCard hc;
                if (!_hand.TryGetValue(hand[i].Uid, out hc)) continue;
                hc.Rt.SetSiblingIndex(Mathf.Min(i, HandLayer.childCount - 1));
                var target = hc.BasePos;
                var rot = Quaternion.Euler(0f, 0f, hc.BaseRot);
                if (!animate)
                {
                    hc.Rt.anchoredPosition = target; hc.Rt.localRotation = rot; hc.Rt.localScale = Vector3.one * BattleScreen.CardScale;
                    continue;
                }
                bool isNew = (hc.Rt.anchoredPosition - drawPos).sqrMagnitude < 1f;
                float delay = isNew ? drawDelay + 0.05f * (k++) : 0f;
                var rtc = hc.Rt;
                Tween.After(delay, () =>
                {
                    if (rtc == null) return;
                    if (isNew)
                    {
                        // めくり (2026-09-17 ⑦): 裏のまま飛び出し、道中で横幅が 0 まで細くなって表に返り、扇の位置で等身大に
                        Audio.Key("CardsDrawn");
                        var p0 = rtc.anchoredPosition; var r0 = rtc.localRotation; float s0 = rtc.localScale.y;
                        var back = rtc.Find("back");
                        Tween.Run(0.34f, t =>
                        {
                            if (rtc == null) return;
                            float e = Tween.Apply(Ease.OutCubic, t);
                            rtc.anchoredPosition = Vector2.LerpUnclamped(p0, target, e);
                            float sc = Mathf.Lerp(s0, BattleScreen.CardScale, Tween.Apply(Ease.OutQuad, t));
                            float flip = Mathf.Abs(Mathf.Cos(t * Mathf.PI));
                            rtc.localScale = new Vector3(sc * Mathf.Max(0.03f, flip), sc, 1f);
                            rtc.localRotation = Quaternion.Slerp(r0, rot, Tween.Apply(Ease.OutQuad, t));
                            if (back != null && t >= 0.5f) { UnityEngine.Object.Destroy(back.gameObject); back = null; }
                        }, Ease.Linear, () => { if (rtc != null) { rtc.localScale = Vector3.one * BattleScreen.CardScale; rtc.anchoredPosition = target; rtc.localRotation = rot; } var b2 = rtc != null ? rtc.Find("back") : null; if (b2 != null) UnityEngine.Object.Destroy(b2.gameObject); });
                        return;
                    }
                    Tween.Move(rtc, target, 0.2f, Ease.OutCubic);
                    Tween.Scale(rtc, Vector3.one * BattleScreen.CardScale, 0.2f, Ease.OutQuad);
                    var rr0 = rtc.localRotation;
                    Tween.Run(0.2f, t => { if (rtc != null) rtc.localRotation = Quaternion.Slerp(rr0, rot, t); }, Ease.OutQuad);
                });
            }
            LastPlayedUid = null;
            LastPlayedTarget = -1;
        }

        readonly HashSet<int> _died = new HashSet<int>();   // この戦闘で崩した (消した) 敵の番号

        /// <summary>
        /// 撃破・逃走 (2026-09-17 ユーザー「倒した敵は消えるようにしたほうが良くない？」): Presenter が EnemyDied/EnemyFled の出来事で呼ぶ (着弾の後)。
        /// 撃破＝白く光り、ドットが頭から崩れて消える (StageUnit の _Dissolve)。崩れる間、体から光の粒が立ちのぼる。逃走＝右へ走りながら崩れる。
        /// 帳面は薄れて消え、最後に絵と帳面を捨てる (以後の組み直しでは描かない)。同じ敵に二度は効かない
        /// </summary>
        public void KillEnemy(GameRoot g, int index, bool fled)
        {
            if (_died.Contains(index)) return;
            _died.Add(index);
            var pan = index >= 0 && index < _enemyPanels.Count ? _enemyPanels[index] : null;
            if (pan == null) return;
            var sprRt = pan.Find("sprite") as RectTransform;
            var stripRt = pan.Find("strip") as RectTransform;
            var tagRt = pan.Find("intent-tag") as RectTransform;
            if (tagRt != null) UnityEngine.Object.Destroy(tagRt.gameObject);
            if (index < _enemyHits.Count && _enemyHits[index] != null) { _enemyHits[index].raycastTarget = false; var b = _enemyHits[index].GetComponent<Button>(); if (b != null) b.interactable = false; }
            Audio.Key(fled ? "EnemyFled" : "EnemyDied");
            string key = "enemy" + index;
            var fx = g.FxLayer;
            var srt = sprRt;
            if (fled)
            {   // 逃走: 奥へ走り去る = 右へ滑って崩れて消える
                if (srt != null)
                {
                    Tween.Move(srt, srt.anchoredPosition + new Vector2(420f, 40f), 0.55f, Ease.InQuad);
                    Tween.Run(0.55f, k => Stage.Dissolve(key, k), Ease.InQuad, () => { if (srt != null) UnityEngine.Object.Destroy(srt.gameObject); });
                }
                FadeOutStrip(stripRt, 0.1f, 0.4f);
                return;
            }
            float h = srt != null ? srt.rect.height : 200f;
            float dur = h > 300f ? 0.95f : 0.62f;   // ボス (384) は長めに
            if (srt != null)
            {
                Stage.Flash(key, 0.22f);
                Tween.After(0.12f, () =>
                {
                    if (srt == null) return;
                    var origin = srt.anchoredPosition;
                    Tween.Run(dur, k =>
                    {
                        if (srt == null) return;
                        Stage.Dissolve(key, k);
                        srt.anchoredPosition = origin + new Vector2(0f, -10f * k);
                    }, Ease.Linear, () => { if (srt != null) UnityEngine.Object.Destroy(srt.gameObject); });
                    // 光の粒 (紙色と真鍮) が体から立ちのぼる
                    if (fx != null)
                    {
                        var c = Tween.CenterIn(srt, fx); float w = srt.rect.width * 0.35f, hh = srt.rect.height * 0.45f;
                        int n = h > 300f ? 22 : 12;
                        for (int m = 0; m < n; m++)
                        {
                            float dl = dur * 0.8f * (m / (float)n);
                            var p0 = c + new Vector2(UnityEngine.Random.Range(-w, w), UnityEngine.Random.Range(-hh, hh));
                            bool brass = m % 3 == 0;
                            Tween.After(dl, () => Mote(fx, p0, brass ? PaperFx.BrassLight : PaperFx.Paper));
                        }
                    }
                });
            }
            FadeOutStrip(stripRt, 0.15f, 0.45f);
        }

        /// <summary>光の粒: ゆらゆら上がって薄れる</summary>
        static void Mote(RectTransform fx, Vector2 p0, Color color)
        {
            if (fx == null) return;
            var rt = UiKit.NewRect("mote", fx);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float sz = UnityEngine.Random.Range(10f, 22f);
            rt.sizeDelta = new Vector2(sz, sz); rt.anchoredPosition = p0;
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = ThemeFx.Glow(); img.color = color; img.raycastTarget = false;
            float rise = UnityEngine.Random.Range(50f, 110f), sway = UnityEngine.Random.Range(-18f, 18f), dur = UnityEngine.Random.Range(0.5f, 0.8f), ph = UnityEngine.Random.value * 6.28f;
            Tween.Run(dur, k => { if (rt == null) return; rt.anchoredPosition = p0 + new Vector2(sway * Mathf.Sin(k * 3f + ph), rise * k); img.color = new Color(color.r, color.g, color.b, 1f - k * k); rt.localScale = Vector3.one * (1f - 0.4f * k); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>帳面を薄れさせて捨てる</summary>
        static void FadeOutStrip(RectTransform strip, float delay, float dur)
        {
            if (strip == null) return;
            var cg = strip.GetComponent<CanvasGroup>();
            if (cg == null) cg = strip.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            var srt = strip;
            Tween.After(delay, () => { if (srt == null) return; Tween.Run(dur, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (srt != null) UnityEngine.Object.Destroy(srt.gameObject); }); });
        }

        /// <summary>札の裏 (めくりの前半だけ見える): 夜色の紙にからくりの印。表の上に重ねる</summary>
        static void CardBack(RectTransform card)
        {
            var back = UiKit.NewRect("back", card);
            UiKit.Stretch(back, 0f, 0f, 0f, 0f);
            var sheet = PaperFx.Sheet(back, PaperFx.Card, "paper", PaperFx.Window);
            UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f); sheet.raycastTarget = false;
            // 真鍮の細い枠 (上下左右の4本)
            foreach (var side in new[] { 0, 1, 2, 3 })
            {
                var ln = UiKit.NewRect("frame", back);
                if (side < 2) UiKit.Anchor(ln, new Vector2(0f, side), new Vector2(1f, side), new Vector2(10f, side == 0 ? 10f : -12f), new Vector2(-10f, side == 0 ? 12f : -10f));
                else UiKit.Anchor(ln, new Vector2(side - 2, 0f), new Vector2(side - 2, 1f), new Vector2(side == 2 ? 10f : -12f, 10f), new Vector2(side == 2 ? 12f : -10f, -10f));
                var li = ln.gameObject.AddComponent<Image>(); li.color = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.8f); li.raycastTarget = false;
            }
            var em = UiKit.Icon(back, "star", 84f, new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.55f));
            em.rectTransform.anchorMin = em.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            em.rectTransform.sizeDelta = new Vector2(84f, 84f); em.rectTransform.anchoredPosition = Vector2.zero;
            back.SetAsLastSibling();
        }

        // ---- HP 危険域 (2026-09-17 ⑪) ----
        RectTransform _danger; Image _dangerImg; float _dangerLevel;   // 0=無し 1=3割以下 2=致死級

        /// <summary>HP が 3 割以下なら舞台の縁が薔薇色に脈打つ (致死級の被ダメ予測なら速く強く)。紙の UI には掛けない</summary>
        void SyncDanger(GameState st)
        {
            var p = st.Player;
            float ratio = p.MaxHp > 0 ? (float)p.Hp / p.MaxHp : 1f;
            int incoming = 0; try { incoming = Effects.IncomingTotal(st); } catch (Exception) { }
            // 氷壁も差し引く (2026-09-29 p08: Web の App.tsx・自分の札の見込みと同じ式。旧は青の致死の脈動が誤って出た)
            bool lethal = incoming > 0 && p.Hp - Math.Max(0, incoming - (p.Block + p.IceBlock)) <= 0 && st.HideIntents != true;
            float level = lethal ? 2f : ratio <= 0.3f ? 1f : 0f;
            if (level <= 0f)
            {
                if (_danger != null) { var d = _danger; var di = _dangerImg; _danger = null; _dangerImg = null; Tween.Run(0.4f, k => { if (di != null) di.color = new Color(di.color.r, di.color.g, di.color.b, di.color.a * (1f - k)); }, Ease.Linear, () => { if (d != null) UnityEngine.Object.Destroy(d.gameObject); }); }
                _dangerLevel = 0f;
                return;
            }
            if (_danger == null)
            {
                _danger = UiKit.NewRect("danger", FieldLayer);
                UiKit.Stretch(_danger, -40f, -40f, -40f, -40f);
                _dangerImg = _danger.gameObject.AddComponent<Image>();
                _dangerImg.sprite = UiKit.LinearSprite(ThemeFx.Vignette(PaperFx.Rose, "vignette-rose"), 0.03f); _dangerImg.type = Image.Type.Simple; _dangerImg.preserveAspect = false; _dangerImg.raycastTarget = false;
                _dangerImg.color = new Color(1f, 1f, 1f, 0f);
                UiKit.LinearShade(_dangerImg, 0.03f);   // Linear (W3 P21 の申し送り1): 脈打つ頂点の α も舞台の縁 (0.03) を仮定して写す
                var pulse = _danger.gameObject.AddComponent<DangerPulse>();
                pulse.Img = _dangerImg;
            }
            _danger.SetAsLastSibling();
            var pl = _danger.GetComponent<DangerPulse>();
            if (pl != null) { pl.Level = level; }
            _dangerLevel = level;
        }

        /// <summary>縁の脈動: 3割以下は 0.9Hz でゆっくり、致死級は 1.7Hz で強く</summary>
        class DangerPulse : MonoBehaviour
        {
            public Image Img; public float Level = 1f; float _t;
            void Update()
            {
                if (Img == null) return;
                _t += Tween.UnscaledDt;   // det の撮影では 1/60 秒 (2026-09-30 HD-2D 見本 P00)
                float hz = Level >= 2f ? 1.7f : 0.9f;
                float baseA = Level >= 2f ? 0.55f : 0.32f, amp = Level >= 2f ? 0.3f : 0.16f;
                float a = baseA + amp * Mathf.Sin(_t * hz * Mathf.PI * 2f);
                var c = Img.color; Img.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
            }
        }

        void Attach(GameRoot g, HandCard hc, CardInstance c)
        {
            BattleScreen.HookHandCard(g, hc, c);
            var cardRef = c;
            Tooltip.Attach(hc.Rt.gameObject, delegate { return BattleScreen.KeywordsOnly(cardRef.Def); }, false);
        }

        /// <summary>ターン終了: 手札を全部捨て札へ飛ばす (順送り演出の TurnEnded で呼ぶ)</summary>
        public void DiscardHand(GameRoot g)
        {
            var list = new List<HandCard>(_hand.Values);
            _hand.Clear();
            list.Sort((a, b) => a.Index.CompareTo(b.Index));
            for (int i = 0; i < list.Count; i++) FlyTo(g, list[i].Rt, "pile-discard", 0.05f * i, 0.3f, true);
        }

        void AnimateOut(GameRoot g, GameState st, HandCard hc, float delay)
        {
            string uid = hc.Card.Uid;
            var rt = hc.Rt;
            rt.SetAsLastSibling();
            // 行き先を状態から判定
            bool inSet = false; int setIdx = -1;
            for (int i = 0; i < st.Player.SetCards.Count; i++) if (st.Player.SetCards[i].Uid == uid) { inSet = true; setIdx = i; }
            bool inPerm = false;
            for (int i = 0; i < st.Player.Permanents.Count; i++) if (st.Player.Permanents[i].Uid == uid) inPerm = true;
            bool inExhaust = false;
            for (int i = 0; i < st.Player.ExhaustPile.Count; i++) if (st.Player.ExhaustPile[i].Uid == uid) inExhaust = true;

            if (inSet) { Audio.Key("CardSet"); FlyTo(g, rt, "setslot" + setIdx, delay, 0.3f, true, 0.5f); return; }
            if (inPerm) { FlyTo(g, rt, "player", delay, 0.3f, true, 0.4f); return; }
            if (uid == LastPlayedUid)
            {
                // プレイ: 対象の敵 (無ければ中央上) へ飛んでから、消滅なら砕け、それ以外は捨て札へ
                var fx = HandLayer;
                Vector2 to = new Vector2(0f, 420f);
                var target = LastPlayedTarget >= 0 ? g.Anchor("enemy" + LastPlayedTarget) : null;
                if (target != null) to = Tween.CenterIn(target, fx) + new Vector2(0f, 40f);
                Audio.Key("CardPlayed");
                Tween.Move(rt, to, 0.2f, Ease.OutCubic);
                Tween.Scale(rt, Vector3.one * 0.6f, 0.2f, Ease.OutQuad);
                rt.localRotation = Quaternion.identity;
                string dest = inExhaust ? null : "pile-discard";
                Tween.After(0.26f, () => { if (rt != null) { if (dest == null) Shatter(rt); else FlyTo(g, rt, dest, 0f, 0.25f, true); } });
                return;
            }
            if (inExhaust) { Tween.After(delay, () => { if (rt != null) Shatter(rt); }); return; }
            FlyTo(g, rt, "pile-discard", delay, 0.3f, true);
        }

        void Shatter(RectTransform rt)
        {
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();   // ?? は不可: エディタでは無いコンポーネントが例外を投げる null オブジェクトで返る
            cg.blocksRaycasts = false;
            Tween.Scale(rt, Vector3.one * 0.2f, 0.3f, Ease.InQuad);
            Tween.Run(0.3f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        void FlyTo(GameRoot g, RectTransform rt, string anchor, float delay, float dur, bool destroy, float endScale = 0.3f)
        {
            var dest = g.Anchor(anchor);
            Vector2 to = dest != null ? Tween.CenterIn(dest, HandLayer) : new Vector2(900f, -100f);
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();   // ?? は不可: エディタでは無いコンポーネントが例外を投げる null オブジェクトで返る
            cg.blocksRaycasts = false;
            Tween.After(delay, () =>
            {
                if (rt == null) return;
                rt.SetAsLastSibling();
                Tween.Move(rt, to, dur, Ease.InOutQuad);
                Tween.Scale(rt, Vector3.one * endScale, dur, Ease.InQuad);
                Tween.Run(dur, k => { if (cg != null) cg.alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f; }, Ease.Linear, () => { if (destroy && rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
            });
        }
    }
}
