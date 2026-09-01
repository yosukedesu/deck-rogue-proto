# StS2改善提案・全文（採掘結果の保存 2026-09-02）

## [0] (map-structure) 幕別部屋数15/14/13（幕が進むほど短い）への行数テーブル化
- 状態: ✅ c916e77 幕別行数15/14/13
- 根拠: acts.md §1: BaseNumberOfRooms=15/15/14/13（行数=部屋数+1）。sts2-reference §8-1「幕ごとに部屋数を減らすのも『終盤は1戦を重く』の本家式レバー」。ユーザー決定済み。
- 現状(採掘時点): src/engine/map.ts:68 で MAP_ROWS=16 の単一定数（全幕均一。15部屋+ボス行）。BOSS_ROW=15・TREASURE_ROW=8・FORCED_CAMPFIRE_ROWS={14} も全てこの定数から派生し幕非依存。generateMap は act を受けるが行数には使っていない。run.ts の depthStrength/depthHpScale/nextChoices が BOSS_ROW を直参照（run.ts:126/158/287）、幕内前後半境界は row>=7 のリテラル（run.ts:159）。UI は App.tsx:2395/2415/2530/2581/2684 で MAP_ROWS/BOSS_ROW を直参照。テストは map.test.ts:46-58 と run.test.ts:124-378 が定数を名指し固定。
- 実装スケッチ: map.ts に `export const MAP_ROWS_BY_ACT = [16, 15, 14] as const`（=部屋数15/14/13+ボス行）を置き、`export const bossRow = (act:number) => MAP_ROWS_BY_ACT[act-1]-1`・`export const treasureRow = (act:number) => bossRow(act)-7`（本家「ボス7行手前」準拠: 幕1=8で現状不変・幕2=7・幕3=6）・強制焚き火行=bossRow(act)-1 を関数化。generateMap 内の walkRows/typeGrid/freeNodes/DP のループ上限を全て act 由来の行数に差し替え。run.ts は depthStrength(row)→depthStrength(row,act)、depthHpScale の後半境界 row>=7 を `row >= Math.floor(bossRow(act)/2)`（幕1=7・幕2=7・幕3=6。幕1/2は現状値と同一）、nextChoices/afterVictory/launchCombat の BOSS_ROW 参照を bossRow(run.act) に。sim/run.ts:489 の配列サイズは最大行数に。App.tsx は定数でなく run.map.length から描画高を導出（幕で長さが変わるため定数は使えない）。map.test.ts/run.test.ts は act ごとのパラメトライズに書き換え（TREASURE_ROW 名指し箇所は treasureRow(act) に）。副作用: 幕2/3の戦闘・ピック・金が約1行分ずつ減る=経済がさらに絞られる方向（「幕2の谷」対処と同方向）。員数（ショップ・?・焚き火）は総ノード数比例なので自動追随。

## [1] (map-structure) Weak枠の構造保証（幕頭N戦は弱プールからのみ抽選）
- 状態: ✅ dc17ea9 Weak帯3/2/2
- 根拠: acts.md §2: 本家は幕頭に Weak枠（幕1=3戦・幕2/3=2戦）を IsWeak=true プールから先に積み、「最初のN戦だけ弱い敵」をリスト構造そのもので保証。sts2-reference §1「StS2方式は『序盤に強敵が事故で出る』ことが構造的に起きない」。
- 現状(採掘時点): 弱プールの概念なし（encounters.json に weak フィールドは存在しない=grep確認）。map.ts:142 tierFor(act,row) は行を問わず ACT_POOLS 全体を返し、緩衝は depthHpScale の 0.55 のみ。実データで行0に enc_beast_pair（うねる獣90+栗鼠75=素165HP→×0.55≈91実効HP）や囁きの狂信者（毎フェーズ強化+2のカルト型タイマー）が引け、最軽量の物真似の子鬼（42×0.55≈23HP）と比べ初戦の重さに約4倍の分散がある。?→戦闘（run.ts:411 tierFor 経由）も同様。
- 実装スケッチ: map.ts に `const WEAK_POOLS: readonly (readonly string[])[]` と `const WEAK_ROWS = [3, 2, 2] as const`（本家のWeak数と同値）を追加し、tierFor を `if (row < WEAK_ROWS[act-1]) return WEAK_POOLS[act-1]` に拡張。DAGなので「最初のN戦」でなく「最初のN行」で保証する（うちの帯方式と整合し、?→戦闘も自動で追随）。弱プール案: 幕1=探り屋・物真似の子鬼・酸吐きの蛞蝓・見習い巨像（ソロ・教師枠）／幕2=伏せ警戒・岩皮の甲虫／幕3=石殻の番人・大振りの斧鬼。encounters.json に weak:true を持たせる形でも可（データ駆動）。enemies.test.ts に「各幕の弱プールが空でない」機械固定を追加。

## [2] (map-structure) 編成のGrabBagキュー方式（幕内一巡してから繰り返す）
- 状態: 📋 M
- 根拠: acts.md §2: 本家は幕頭に出現順リストを一括生成し、部屋を踏むたび先頭から消費・使い切ったら同プールで補充（=全編成を一巡してから重複が始まる）。うちの再検証ランでも「栗鼠4回/9戦」等の反復感が実測され+2体で対処した経緯があり、リスト構造なら根治する。
- 現状(採掘時点): map.ts:449-479 で生成時にノード単位の動的抽選（直前2行+同一行の同一IDのみ回避）。同じ敵が幕内で3〜4回出ることを止める構造はない。エリートだけは usedElites で未使用優先（map.ts:465-469）=片側だけキューに近い挙動。なお UI/CLI はマップノードに敵名を表示する全体可視設計（App.tsx:2447・play.ts:434）なので、本家式の「訪問時消費」をそのまま入れると敵名の事前表示が失われる。
- 実装スケッチ: 純粋な訪問時消費でなくハイブリッド: generateMap 内で幕頭に GrabBag シーケンス（Weak N体→Normal残数、プールをシャッフルして尽きるまで重複なし・尽きたら補充）を構築し、battle ノードへ (row, col) 昇順で焼き込む。焼き込み式なので全体可視・リプレイ決定性・ChooseNode 互換は全て不変で、変わるのは分布だけ（幕内の同一編成の重複が「プール一巡後」まで起きなくなる）。直前2行回避は「一巡内で隣接した場合の並べ替え」として残す。?→戦闘（run.ts:411）は従来どおり動的抽選のまま（次項のタグ回避で手当て）。

## [3] (map-structure) 編成タグによる同族連続の回避（GrabBagの同タグ回避）
- 状態: ✅ dc17ea9 メンバー交差で同族回避(タグ不要の実装)
- 根拠: acts.md §2: 本家の抽選は「直前に選ばれた Encounter と EncounterTag（Slimes/Workers等）が重複しない候補を優先」。個体IDでなく家族単位で反復を避ける。
- 現状(採掘時点): map.ts:471-473 の回避は encounterId の完全一致のみ。実データで家族被りが素通り: 幕1は探り屋系が3口（enemy_probe / enc_probe_pair / enc_squirrel_probe）、幕3は妖術師系3口（enc_hexer_shadow / enc_breaker_hexer / enc_shell_hexer）・狼系2口+双牙——「探り屋ソロ→次の行で探り屋の二人組」が現行仕様で普通に出る。?→戦闘（run.ts:408-412）に至っては回避が一切効かない（コードのコメントも自認）。
- 実装スケッチ: encounters.json / enemies.json に `tag` フィールド追加（例: probe / thief / squirrel / hexer / wolf / bomber / drummer / moss / colossus。編成はメンバーの主タグを列挙可）。map.ts の recent 判定を「ID一致 or タグ交差」に拡張。RunState に `lastBattleTags: readonly string[]` を持たせ（旧セーブは ?? [] ガード）、resolveUnknown の?→戦闘も直前戦闘とタグが被らない候補を優先。types.ts のスキーマ追随+map.test.ts に「同タグが2行連続しない（フォールバック除く）」の機械固定。

## [4] (map-structure) ショップ員数の固定化（重み5%→固定3/幕）
- 状態: 🔶 どちらの本家に合わせるか(StS1重み vs StS2固定3)+ゴールドシンク総量
- 根拠: acts.md §5: NumOfShops=3 全幕共通・固定。エリートを重みでなく員数固定4にした時と同じ理由（供給の約束を配置の運に委ねない）が本家ではショップにも適用されている。
- 現状(採掘時点): map.ts:86-91 で shop は重み0.05を総ノード数に掛けて round（コメントに「本家の重みテーブル」とあるが、これは本家StS1式の写しで、StS2は固定数）。総ノード実測57〜82なので幕により3個/4個で揺れ、キリ番の丸めでショップ経済（除去・強化・レア枠のシンク量）がシードごとに±33%変動する。2026-08-28に「ルート次第で一度も到達できない」事故から2個→冗長化した経緯があり、到達保証は今も「たまたま散る」頼み。
- 実装スケッチ: map.ts の quota を `['shop', SHOP_COUNT]`（SHOP_COUNT=3 の定数）に差し替え。あわせてエリートの ELITE_PATH_MIN と同型の「ショップを1回は踏める経路の存在」DP保証（F+B>=1）を入れると2026-08-28の事故が機械的に再発不能になる。

## [5] (map-structure) 部屋タイプ員数の幕別テーブル化（?のガウス分布・幕2/3で-1）
- 状態: 🔶 ?=22%は本家一致の物差し
- 根拠: acts.md §5: NumOfUnknowns=NextGaussianInt(平均12,σ1,min10,max14) で幕2/3は-1、NumOfRests は幕別乱数（幕1=6-7・幕2=6-7・幕3=5-7一様）。員数自体にシードごとの揺らぎと幕勾配がある。
- 現状(採掘時点): map.ts:86-91 の ROOM_WEIGHTS は全幕共通の固定重み（event 0.22・campfire 0.08）で、員数=round(総ノード×重み)=総ノード数以外の分散ゼロ。幕による?の減少（本家の-1）も、休憩数の幕別差もない。
- 実装スケッチ: ROOM_WEIGHTS を幕インデックスのテーブル `ROOM_QUOTA_BY_ACT` に置換し、員数を「基準値+シードロール±1」で決める（例: event=幕1:14±1・幕2/3:13±1、campfire=幕1/2:5±1・幕3:4±1。ガウスは整数RNG2回の和で近似=浮動小数を持たない既存規約を守る）。幕別部屋数15/14/13を入れる場合は総ノード減で自動的に幕勾配が付くため、その実測を見てから本項の要否を判定する順番を推奨。

## [6] (map-structure) ボス前3行の焚き火禁止（休憩の上限制約）
- 状態: ✅ dc17ea9 ボス前3行の焚き火禁止
- 根拠: acts.md §5 配置制約: 本家は「最終3行以内には RestSite を置けない」（最終行=全休憩が確定しているため直前の散布休憩を禁じる）。
- 現状(採掘時点): map.ts:319-334 assignable の焚き火制約は CAMPFIRE_MIN_ROW=5（下限）と親子同種禁止のみ。行13は「子が全焚き火行14」の親ルールで自動的に弾かれるが、行12の焚き火は合法で、行12散布+行14強制の「ボス直前に休憩2連」経路が成立する（CAMPFIRE_PATH_MAX=4 は総数しか見ないので通す）。
- 実装スケッチ: map.ts に `const CAMPFIRE_MAX_ROW_OFFSET = 3` を追加し、assignable に `if (t === 'campfire' && r >= BOSS_ROW - CAMPFIRE_MAX_ROW_OFFSET) return false` の1行（行12・13が対象。行13の既存の親ルール依存も明示的な制約に変わり頑健化）。map.test.ts に機械固定を追加。焚き火希少化（8%・回復25%）と同じ「HP経済を絞る」方向の小修正。

## [7] (map-structure) ボス複数種+未撃破優先ローテ（BossDiscoveryOrder）
- 状態: 🔶 ボス3種は大型コンテンツ
- 根拠: acts.md §2: 本家は各幕にボス3種のプールを持ち、未撃破のボスを配列先頭から優先出現（3体制覇でローテ）。「初見の体験を設計で保証する」+リプレイ時の多様性の両取り。
- 現状(採掘時点): map.ts:138 `ACT_BOSSES = ['enemy_brute', 'enemy_turtle', 'enemy_warden']` の各幕1体固定。しかもオーガ(enemy_brute)は幕3通常プールにも入っており、幕1ボスが幕3の雑魚として再登場する。ラン間の撃破記録を engine に渡す口も無い（isFirstRun/runCount 系のフィールドは grep で不存在）。
- 実装スケッチ: ACT_BOSSES を `ACT_BOSS_POOLS: readonly (readonly string[])[]` に拡張し、createRun にオプション `bossesDefeated?: readonly string[]` を追加（UI が localStorage のプロフィールから渡す=engine 純度は不変・未指定なら先頭固定で完全後方互換）。選出は「プール先頭から最初の未撃破」、全撃破後はランRNGで抽選。ボス自体は各幕+2体の新設が必要（例: 幕1に苔まといの主のボス化・幕2に双牙の狼のボス編成化など既存資産の昇格から始める）。generateMap の tierFor ボス分岐と launchCombat の幕スケール表は encounterId 非依存なのでそのまま動く。

## [8] (map-structure) 初回ラン限定のチュートリアル導線（DiscoveryOrder上書き）
- 状態: 🔶 初回導線=ロードマップ枠
- 根拠: acts.md §2: 本家は初回ラン(NumberOfRuns==0)のみ幕1の Normal/Elite/Event の先頭数枠を固定順に差し替える（易しい敵→ギミックの教師の順）。sts2-reference §1「初見の体験を設計で保証する手当て」。
- 現状(採掘時点): 該当機構なし（grep で isFirstRun/NumberOfRuns/runCount いずれも不存在）。初回プレイヤーも seed 次第で初戦からカルト型タイマー（囁きの狂信者）や2体編成を引く。エリート1発目も4種からランダム（「手本」と評価された鬼軍曹が初回に出る保証はない）。
- 実装スケッチ: createRun にオプション `firstRun?: boolean` を追加（UI が localStorage で通算ラン数を数えて渡す。engine 純度不変）。true のとき generateMap へフラグを渡し、幕1の戦闘割当の先頭を固定順（例: 探り屋→うねる獣→酸吐きの蛞蝓→見習い巨像=読みの教師→休符→デバフ教師→タイマー予習）に、エリート1発目を鬼軍曹（「問い→解が1本の線」の手本評価済み）に差し替える。GrabBagキュー方式を入れる場合はキュー先頭の差し替えだけで済む（本家と同じ実装形）。

## [9] (map-structure) イベント専用エンカウンター（?から入る特別報酬付き戦闘）
- 状態: 📋 M
- 根拠: acts.md §4: 本家は幕プール外のイベント専用戦闘を7種持つ（偽商人=報酬300G固定、PunchOff=HPランダム減の2体など）。「戦うか避けるか」を選択肢として提示し、報酬も特注できる器。
- 現状(採掘時点): EventChoiceDef（types.ts:574-614）に戦闘を起動するフィールドは無く、applyEventChoice（run.ts:536-655）にも launchCombat 経路が無い。?→戦闘（resolveUnknown）は幕プールの通常戦闘で報酬も通常のみ。「予告なしで戦わされる」だけで「報酬を見て戦いを選ぶ」イベントが作れない。
- 実装スケッチ: EventChoiceDef に `fight?: { readonly encounterId: string; readonly goldReward?: number; readonly relicReward?: boolean }` を追加し、applyEventChoice で launchCombat(run, false, fight.encounterId) を呼ぶ。特別報酬は RunState に `pendingEventReward` を持たせ afterVictory で精算（旧セーブは ?? ガード）。第1号イベント案: 「偽商人」（高額ゴールドを抱えた敵=盗人型の逆で、勝てば+150G）・「守られた祭壇」（エリート級1体と戦えばレリック確定）。敵は幕プール外IDを encounters.json に追加。ボットは「最後の選択肢=立ち去る」規約で壊れ検知を素通りできる（既存規約と両立）。

## [10] (map-structure) 選択にならない分岐の枝刈り（PruneAndRepair相当）
- 状態: 📋 M
- 根拠: acts.md §5 修復・後処理: 本家は「分岐しても合流して同じになる同一経路セグメント」を検出して枝刈りし、目標員数割れは差し替えで補充（最大3イテレーション）。分岐の見かけと実質を一致させる仕上げ。
- 現状(採掘時点): map.ts に同等機構なし。分岐補強（map.ts:240-268）で出次数は改善済みだが、「両枝とも battle→battle で内容も同等」のひし形は残りうる。ただしうちはノードに敵名を表示する全体可視設計（App.tsx:2447）なので、敵IDが違えば分岐は実質の選択になる=本家より問題は軽い。実害があるのは「両枝の部屋タイプ列も敵IDも一致」の場合のみ。
- 実装スケッチ: generateMap の最終段（ノード実体化後）に検証を追加: 出次数2以上の各ノードから合流点まで（ANCESTOR_DEPTH=5行以内）の2経路について (type, encounterId) 列が完全一致したら、片枝の battle ノードの敵を引き直す（タイプは触らない=員数・制約を壊さない）。一致が解消できなければそのまま通す（生成失敗を増やさない）。map.test.ts に「5行以内で合流する2経路が完全同一にならない」の統計チェック。優先度は低め=全体可視により実害が限定的なことを明記しておく。

## [11] (map-structure) エリート下限行の本家寄せ（行2→行5以降）の再裁定
- 状態: 🔶 エリート行2は序盤レリック供給の設計
- 根拠: acts.md §5: 本家は row<6 に RestSite/Elite を置けない（エリートは序盤6行に出ない）。Weak枠保証と合わせて「序盤は必ず軽い」を二重に守っている。
- 現状(採掘時点): map.ts:103 `ELITE_MIN_ROW = 2`（コメントどおり「序盤のレリック供給を守るため」の意図的逸脱。CLAUDE.mdに2026-08-31「エリート下限（本家の行5以降）は見送り」の記録あり）。行2のエリートは回避可能だが、スターター10枚デッキ+ピック1〜2枚の時点で金羽の大鴉（DPSレース）等に挑む選択肢が提示される。
- 実装スケッチ: ELITE_MIN_ROW を 2→5 に変更（1行）。ELITE_PATH_MIN=3 のDP保証・「全親に出口2以上」制約はそのまま機能する（行5〜13に4個は十分置ける）。Weak枠保証（行0-2弱プール）を先に入れるなら、行3-4のエリートだけが残る中間案 ELITE_MIN_ROW=3 もある。

## [12] (map-structure) 構造つまみの器（高難度段でエリート増・宝箱行→エリート行）
- 状態: 🔶 難易度つまみ凍結中
- 根拠: acts.md §5: 本家は SwarmingElites（エリート数×1.6）と ShouldReplaceTreasureWithElites（宝箱行を全エリート行に置換）を難度バリアントとして持つ。sts2-reference §6: 本家アセンション1段目=エリート増=数値でなく構造・経済で難度を作る。難易度検証3本の一致所見「倍率より『育ったデッキに刺さる軸』を段で動かす」とも一致。
- 現状(採掘時点): 難易度は run.ts:135-146 の DIFFICULTY_TABLE（HP/打点の純倍率）のみで、構造に触る段が無い。ELITE_COUNT=4 と TREASURE_ROW は難易度非依存の固定（map.ts:101/80）。
- 実装スケッチ: DIFFICULTY_TABLE には触らず、並置の新テーブル `DIFFICULTY_STRUCT`（例: 段7以上→ELITE_COUNT 4→5、段9以上→宝箱行を全エリート行に置換=レリック供給1回をエリート戦の対価に変える）を map.ts/generateMap に配線（generateMap に difficulty を渡す引数追加。既定3では現状と完全一致）。倍率ラダーの「ベース再校正後に経済税へ」方針の受け皿を先に作っておく形。

## [13] (map-structure) 幕1の2バイオーム択（Overgrowth/Underdocks方式）
- 状態: 🔶 2バイオームは大型
- 根拠: acts.md §1: 本家は幕1だけ2つの完全に別のプール（Overgrowth/Underdocks）を持ち、ランごとにランダムで置き換わる。最頻で遊ぶ幕1の再訪性を敵プールの二本立てで作っている。
- 現状(採掘時点): map.ts:126-136 ACT_POOLS は各幕1本。幕1は12エントリ（ソロ7+編成5）で、反復感には+2体の増員（2026-08-31）で対処してきたが、プール1本の構造は変わらない。
- 実装スケッチ: ACT_POOLS[0] を A/B の2本に分割し（既存12エントリを6+6に割り、各6体級を新設して8+8へ）、createRun 時にランRNGで一方を選んで RunState.act1Pool として保持（tierFor が参照）。エリート・ボスは共通のまま。新敵6体級の投資が必要なので、ボス複数種より優先度は下（幕1の反復感が再燃したときの構造カード）。

## [14] (behavior-grammar) 前奏（prelude）＝初手固定の器を追加し、weight敵がT1に「その敵の問い」を必ず見せる
- 状態: ✅ ee5569c opener
- 根拠: sts2-reference.md §3-2「初手固定が非常に多い ★」。HauntedShip(初手Dazed5枚)・HunterKiller(初手デバフ)・Fogmog(初手召喚)・ShrinkerBeetle(初手デバフ1回のみ→以降交互ループ)・TrackerRubyRaider(初手Frail→以降多段ループ)。本家は「初手だけ別・一度きり」を器として持ち、確率敵でも初手はほぼ固定（Flyconid/FlailKnightも初手専用分岐）。
- 現状(採掘時点): combat.ts declareIntents(L318-330): sequence敵はローテ先頭が初手を兼ねるが、weight敵は初手から純粋な重み抽選。enemies.jsonの実測: 43体中weight敵12体、うち複数技でT1がランダムになるのは7体（うねる獣・罠壊し・苔の主・道化・苔の癒し手・石殻の番人・大苔スライム）。癒し手がT1に杖で小突く・大苔スライムがT1に防御する等、「問い」が初手に出ない。preludeに相当する機構はgrepで不在確認。
- 実装スケッチ: engine/types.ts の EnemyDef に `prelude?: readonly string[]`（movesのid列。戦闘開始から一度きりで順に消化し、消化後に通常のsequence/重み抽選へ入る）を追加。declareIntentsで `enemy.patternIndex < prelude.length` なら prelude[patternIndex] を使い、以降は `patternIndex - prelude.length` を通常ローテに渡す。適用例: 苔の主 prelude:["slam"]（虚弱付きの叩きつけ=再生レースの問いを初手に）・大苔スライム prelude:["slam"]・石殻の番人 prelude:["shell_bash"]（積みながら殴るを初手に）・うねる獣 prelude:["surge"]（幅8-15の問い）。enemies.test.ts に「prelude保持敵はT1の意図が固定」の機械判定を追加。

## [15] (behavior-grammar) cannotRepeat（直前と同じ技を引かない）をweight抽選に導入
- 状態: ✅ ee5569c noRepeat
- 根拠: sts2-reference.md §3-1「確率分岐には必ずCannotRepeat・UseOnlyOnce・重みが付く。読める揺らぎだけを許す」。実例多数: Mawler・SoulNexus・SludgeSpinner・TwigSlimeM・LeafSlimeS——本家の確率敵はほぼ全員が直前技禁止付き。
- 現状(採掘時点): combat.ts L327 `weightedIndex(rng, baseTable.map((m) => m.weight))` は純粋な重み抽選で直前技の除外なし。enemy状態にlastMoveId相当のフィールドなし（grepで確認）。CLAUDE.mdに実害の記録あり: うねる獣が「4連続防御で休符が無音になる」実測（2026-08-31）を重み2→1の差し戻しで対症した——構造処方が無いため重みでしか調整できない。苔の癒し手のmend連打・道化のdodge_prance連打も現行では起こりうる。
- 実装スケッチ: EnemyMove に `cannotRepeat?: true`、敵インスタンスに `lastMoveId?: string` を追加。declareIntentsの重み抽選前に「cannotRepeat持ちかつ id===lastMoveId の技」を候補から除外（全滅時は素の抽選にフォールバック=決定的）。適用例: うねる獣のcoil（防御）・苔の癒し手のmend・道化のdodge_prance・罠壊しのready_toolsに付与。うねる獣は重み1のままでも防御2連が消え、休符の設計が構造で守られる。

## [16] (behavior-grammar) oncePerCombat（戦闘1回きりの技）をweight抽選に導入
- 状態: ✅ ee5569c once
- 根拠: sts2-reference.md §3-1 のUseOnlyOnce。Mawler(大デバフROARは戦闘1回きり)・TwoTailedRat(増殖は個体3回まで)・FrogKnight(BEETLE_CHARGE 35は一度使うと使用不可)。確率敵に「一度だけの山場」を安全に入れる器。
- 現状(採掘時点): EnemyMove/敵インスタンスに使用済み技の記録なし（grepで確認）。現行のweight敵は全技が無限に出うるため、大技を混ぜると連発リスクがあり、実際weight敵12体は全員が小技のみの平坦なテーブル（最大でも道化のwild_swing 15-19）。
- 実装スケッチ: EnemyMove に `oncePerCombat?: true`、敵インスタンスに `usedMoveIds?: readonly string[]` を追加。抽選候補から使用済みを除外。適用例: うねる獣に新技「大うねり」（attack 18-22・weight1・oncePerCombat）を追加=読みなしの休符敵に一度だけの山場を入れても連発事故が起きない。cannotRepeatと同じ配管なので同時実装が安い。

## [17] (behavior-grammar) 回数カウンタのフェーズ変化（KnowledgeDemon式）＝技をN回使ったら行動テーブル恒久切替
- 状態: ✅ ee5569c phaseAfterUses(妖術師=呪い2回で打ち止め)
- 根拠: sts2-reference.md §3-4・§8-4。KnowledgeDemon(呪い付与ムーブは3回で打ち止め、以降SLAP起点のループに恒久固定される唯一のボス級)。HP割合以外のフェーズ軸で「凌ぎ切れば毒は止まる」の物語を作る。
- 現状(採掘時点): フェーズ変化の条件はHP50%（movesBelowHalf/sequenceBelowHalf）・伏せ有無（movesVsSet/setAlt）・従者有無（movesVsTokens）のみ（combat.ts declareIntents L287-314で確認）。発動回数によるテーブル切替の機構は不在。泥投げの妖術師は sequence:[mud,curse,slap] で泥呪い（弱体3）を永遠に3ターン周期で投げ続ける＝デバフ圧に「終わり」が無い。
- 実装スケッチ: EnemyDef に `moveCap?: { moveId: string; count: number; sequenceAfter: readonly string[] }` を追加。敵インスタンスにmoveIdごとの使用回数カウンタ（usedMoveIdsを回数付きに拡張して共用）。宣言時に対象moveの累計がcount以上なら以降sequenceAfterへ恒久切替（belowHalfとの優先度は belowHalf > moveCap > 通常）。適用例: 妖術師 moveCap:{moveId:"mud", count:3, sequenceAfter:["curse","slap","slap"]}＝「泥は3投で尽きる」。凌ぎ切る動機と、逆に速攻で3投目前に倒す動機の両方が立つ。ボス級新敵の軸にも転用可能。意図表示に「あとN投」を常時表示（砥石の巨像の『数えられる死』と同じフェアネス）。

## [18] (behavior-grammar) 味方の生死で行動テーブル切替（LivingShield式転職）＝従士に「射手が死んだら本気」を実装
- 状態: ✅ ee5569c movesWhenAlone(従士の転職)
- 根拠: sts2-reference.md §3-4・§8-4。LivingShield: 味方の生存中は弱攻撃6のみ→全滅した瞬間からSMASH16+毎回Strength+3の自己ループ（無限強化の殴り役に転職）。Queen: 随伴TorchHeadAmalgam生存中は支援バフループ、死亡で攻撃ループへ。「キル順の逆問い」を陣形ものに与える本家の定石。
- 現状(採掘時点): 味方の生死を読むのは bondStrength（宣言時に攻撃+N。双牙の狼）のみで、行動テーブル自体は変わらない（combat.ts L266-271で確認）。盾持ちの従士は sequence:[shield_bash(6-8+盾5), brace(10-13)] 固定＝射手を先に倒すと残った従士は低打点の消化試合になる（guardian+守り行動だけが残る）。転職機構は不在。
- 実装スケッチ: EnemyDef に `sequenceOnAllyDeath?: readonly string[]`（と参照先の追加moves）を追加。宣言時に「他の仲間が全滅している」ならこのローテへ恒久切替（判定は宣言時=宣言時固定の既存則と両立。belowHalfより低優先）。適用例: 盾持ちの従士に moves追加 {id:"masterless_rage", kind:"attack", min:10, max:13, alsoBuff:2} と sequenceOnAllyDeath:["masterless_rage"]＝「主なき怒り」。射手から倒す定石に「従士が転職する」対価が付き、従士から倒す（庇うを正面から剥がす）ルートと初めて拮抗する。図鑑・意図表示に「仲間が全滅すると→」の予告行（フェアネス）。

## [19] (behavior-grammar) Queen式SetMoveImmediate＝仲間死亡の瞬間に宣言済み意図を強制差し替え
- 状態: 🔶 宣言時固定の既存則を破る
- 根拠: sts2-reference.md §3-4「味方の生死（Queen: 随伴が死んだ瞬間、次の予定行動を強制差し替えて激怒）」。monsters-m-z.md Queen: Amalgam死亡のAfterDeathフックで、次の予定行動が支援バフだった場合は即座にENRAGEへSetMoveImmediate。
- 現状(採掘時点): 意図は宣言時に確定し以降再計算しない（確定済みルール表「条件付き意図」2026-08-26修正で「窓が嘘をつく」を排除した経緯）。仲間死亡で宣言済み意図が変わる機構は不在。双牙の狼のbondStrengthも「次の宣言から素に戻る」宣言時判定のみ。
- 実装スケッチ: 敵インスタンスの死亡処理（checkCombatEnd/ダメージ解決後）に「生存仲間の宣言済み意図を差し替える」フック `onAllyDeathReplace?: { fromKinds: readonly EnemyActionKind[]; moveId: string }` を追加。例: 双牙の狼——相方が死んだ瞬間、防御(prowl)の意図が「弔いの咆哮」(buff+3)に変わる。差し替えはEnemyIntentDeclaredを再emitしてUI・最悪被ダメ予測も追随、意図表示に「仲間が倒れると→◯◯」の両分岐予告を出す（setAltの予告配管を流用）。

## [20] (behavior-grammar) EncounterMemberのprelude上書き＝スロット役割分化（Exoskeleton式）
- 状態: 📋 S
- 根拠: sts2-reference.md §3-3「スロット位置で役割が変わる（Exoskeleton: 1体目=多段/2体目=単発/3体目=バフ）」。Myte(first→毒/second→吸収)・Wriggler(1,3→噛み/2,4→バフ)・PhantasmalGardener(4体で全役割分担)。位相ずらしでなく初動の役割そのものを変える。
- 現状(採掘時点): EncounterMember は patternOffset（ローテ開始位置ずらし）のみ（types.ts L871-885で確認）。同型ペア（双牙の狼@1・栗鼠@1・歩哨@1）は同じループの位相違いで、役割の分担ではない。またpatternOffsetはweight敵には何の効果もない。
- 実装スケッチ: 提案1のpreludeを前提に、EncounterMember に `prelude?: readonly string[]`（EnemyDefのpreludeを個体単位で上書き）を追加。適用例: 双牙の狼ペアを patternOffset方式から「1頭目 prelude:["twin_bite"]・2頭目 prelude:["prowl"]」に変更＝連撃役と守り役で開幕の顔が違い、以降は同じ3拍ローテに合流。実装は提案1の配管に EncounterMember 優先の1行を足すだけ。

## [21] (behavior-grammar) 技の恒久成長（growPerUse/growHitsPerUse）＝使うたび育つ技で「戻らない恐怖」を作る
- 状態: 📋 M
- 根拠: monsters-m-z.md TestSubject: MULTI_CLAWは「このMoveを使うたびヒット数+1され戦闘中ずっと増加し続ける」。WaterfallGiant: PRESSURE_GUNは使うたびダメージ+5が恒久increase。単調増加のタイマーは「長引かせた自分のせい」という納得を作る。
- 現状(採掘時点): 刺突の書は stab2→stab3→stab4 の3技をsequenceで並べた手書き実装で、ループすると2ヒットに戻る（enemies.json実測: sequence:["stab2","stab3","stab4"]）。技単位の使用回数参照は機構として不在。激昂タイマー（enrageEveryCards/Damage）はプレイヤー行動参照で、敵自身の行動回数参照は無い。
- 実装スケッチ: EnemyMove に `growHitsPerUse?: number` と `growPerUse?: number` を追加（この技の累計使用回数×Nをヒット数/実値に加算。宣言時に幅表示へも乗せる=フェアネス）。使用回数は提案3のusedMoveIds回数記録を共用。適用例: 刺突の書を moves:[{id:"stab", 6-8, hits:2, growHitsPerUse:1}]+sequence:["stab"] に置換＝2→3→4→5…と戻らない成長になり「何ターンで抜けるか」の計算が単調で読める。既存エリートの挙動が強くなる方向なので実装後に幕2でsim確認。

## [22] (behavior-grammar) 敵の召喚行動（kind:'summon'）＝戦闘中に味方を補充する敵
- 状態: 📋 L
- 根拠: sts2-reference.md §3-7「1体で完結しない敵が多い」。Fabricator(味方4体未満なら防御ボット+攻撃ボット補充、満杯なら素の攻撃に転換)・LivingFog(GasBomb召喚)・Ovicopter(空きスロットに卵を最大3体)・TwoTailedRat(条件成立で75%重みの自己増殖・個体3回まで)・Fogmog(初手で幻影召喚)。
- 現状(採掘時点): EnemyActionKind は attack/defend/destroy-set/destroy-token/buff/rally/hex/heal/steal-gold/flee/rest/mill の12種（types.ts L729-741で確認）。敵が敵を場に出す機構は splitInto（死亡時のみ）だけで、生存中の召喚・増殖は不在。処刑順パズルの供給源は応援役・回復役の2種に留まる。
- 実装スケッチ: EnemyActionKind に 'summon' を追加し、EnemyMove に `summon?: { enemyId: string; count: number }` を追加。解決は processSplits の子生成コード（意図付き即出現・素の値・atkScale継承・patternIndexずらし）をそのまま関数化して共用。3体上限（戦闘形式ルール）で空きが無ければno-op（意図表示は出す=「潰すなら今」の合図）。意図表示「👶召喚: 苔スライム×1」。適用例: 幕2新敵「苔の産み手」(HP70・sequence: 召喚→攻撃9-12→防御)＝放置すると頭数=行動回数が増える処刑順の問い。ボットは召喚者優先の集中砲火を教える必要あり（sim/bot.tsに1分岐）。

## [23] (behavior-grammar) 眠りの被ダメ覚醒（wakeOnDamage）＝眠れる鉄卵の「起こす前に削るか」を本物の二択にする
- 状態: 📋 M
- 根拠: monsters-a-l.md LagavulinMatriarch: AsleepPower保持中は空ターン、外部から強制的に起こせるWakeUpMove構造。monsters-m-z.md SlumberingBeetle: 3ターン待てば自然覚醒＋「起こす前に削ればシールド(Plating)を剥がして攻撃が通る」＝眠りが本当の読み合いになっている。
- 現状(採掘時点): 眠れる鉄卵（enemy_elite_iron_egg）は sequence:["sleep"(defend14-18),"awaken","tail"×8] の固定ローテで、眠り中にいくら殴っても覚醒は早まらない（declareIntentsはpatternIndexを進めるだけ。ダメージ起因のローテ操作は不在）。「起こす前に削るか」というフレーバー（flavor文言）と実機構が一致していない。
- 実装スケッチ: EnemyDef に `wakeOnDamage?: number` を追加: patternIndexが眠り区間（preludeまたはsequence先頭のsleep行動）にある間、そのターンの累計被ダメージがN以上なら次の宣言でpatternIndexを覚醒位置（awaken）へスキップ＋「起こしてしまった」イベントをemit。鉄卵に wakeOnDamage:20 を設定＝「静かに1発ずつ削る（装甲22の下で少額×多ターン）か、大技で起こして短期決戦か」。図鑑・敵カードに「20以上のダメージで目覚める」を常時表示（regenBreakと同じフェアネス形式）。

## [24] (behavior-grammar) 敵のアーティファクト（デバフ無効チャージ）＝延焼・急所・威圧・混乱への構造的な問い
- 状態: 🔶 延焼・威圧デッキへの実質ナーフ成分
- 根拠: sts2-reference.md §3-5「ほぼ全敵が開幕パワー持ち」＋monsters-m-z.md MechaKnight「AfterAddedToRoomでArtifactPower 3付与（開幕デバフ無効3回）」・PunchConstruct(Artifact1)・TheAdversary系(0/1/2の段階設計)。本家はデバフデッキに「まず殻を剥げ」の1手を要求する。
- 現状(採掘時点): プレイヤー→敵の付与（延焼applyBurn・急所exposeEnemy・混乱confuse・威圧weakenEnemy）を弾く機構は不在（combat.ts/effects.tsに無効化系フィールドなし。敵の静的性質はburnResist=延焼の減衰量のみで、付与自体は必ず通る）。バーン型はsim96%・「延焼はブロック無視+装甲無視の万能解答」と複数記録があり、数値でなく構造の受けが無い。
- 実装スケッチ: EnemyDef に `artifactCharges?: number` を追加: プレイヤー由来のデバフ付与（burn/exposed/confusion/威圧のstrength減）を最初のN回無効化し1付与=1チャージ消費。敵カードに「⚙️無効×N」常時表示・消費のたびログ。適用例: 幕3の1〜2体（石殻の番人 artifact:1、幕3新エリート artifact:2）に限定配布——幕1・2には置かず「対策の対策」は終盤の問いにする。

## [25] (behavior-grammar) DeathBlow予告＝致死級大技の専用マーク（💀）を意図表示に追加
- 状態: ✅ 75515b2 💀致死級マーク
- 根拠: sts2-reference.md §5: 本家の意図タイプ15種に DeathBlow(致死級大技の専用予告)・Sleep・Stun・CardDebuff(カード汚染予告) があり「予告してから殺す」を専用アイコンで制度化している。
- 現状(採掘時点): 意図表示はkind別の絵文字＋幅表示＋最悪被ダメ予測のみで、大技と通常攻撃は数字の大小でしか区別されない（EnemyMoveにマークのフィールドなし。grepで確認）。火薬樽の大爆発20-24・巨面の圧潰34・大亀の大薙ぎは「気づいた人だけが備える」状態。
- 実装スケッチ: EnemyMove に `deathBlow?: true` を追加し、意図表示に💀を前置（UI/CLIのラベル関数1箇所ずつ。幅表示・実値ロールは不変=実値化ではない）。付与対象: 火薬樽かつぎのbig_boom・巨面の圧潰・斧鬼の大振り・大亀の大薙ぎ・門番の乱打。設計原則①「予告してから殺す」の表示層の制度化で、engineの挙動は一切変えない。

## [26] (behavior-grammar) 開幕パワー（静的性質）の配布率を40%→引き上げ＝裸の敵に既存の器を薄く配る
- 状態: ✅ 742b5fe 巨像2体+影+蛞蝓+泥まとうもの(こそ泥とげはとげ裁定と衝突→取り下げ)
- 根拠: sts2-reference.md §3-5「ほぼ全敵が開幕パワー持ち。素のHP/打点でなく常在パワーが難易度設計の主軸。うちの静的性質は同じ方向だが本家は配布率がほぼ100%」。§8-1「幕1増強は打点でなくHP上端+質の圧で」。
- 現状(採掘時点): enemies.json実測: 静的性質（startingBlock/armor/thorns/regen/burnResist/enrage系/bond/guardian/split/angerOnBlock）を持つのは43体中17体=40%。幕1通常プールでは栗鼠(とげ)・狂信者(enrage)・大苔スライム(分裂)程度で、探り屋・うねる獣・妖術師・狼・こそ泥・蛞蝓・見習い巨像は裸。
- 実装スケッチ: 新機構は作らず既存の静的性質を配る（識別純度の2026-09-01裁定でオーガ・狼・鏡型・とげ・探り屋・用心深い影は素のまま維持）。具体案: 酸吐きの蛞蝓に burnResist:2（ぬめり=濡れた苔と同フレーバー。状態異常の教師に赤への軽い耐性の顔）・見習い巨像に startingBlock:8（2拍子タイマーの予習に「開幕から見えている殻」を足す=幕2の樽・殻の予習を兼ねる）・こそ泥ゴブリンに thorns:1（小突くと引っかかれる=盗みレース中の多段連打に小さな税）。各1行のデータ追加＋図鑑表示は既存配管で自動。

## [27] (behavior-grammar) フェーズ変化の1拍（transitionMove）＝「殻割れ」「牙をむく」の瞬間を行動として見せる
- 状態: 📋 M
- 根拠: sts2-reference.md §3の器＝MustPerformOnce。monsters-m-z.md TestSubject: RESPAWN_MOVEは「倒された直後に発動が強制される(MustPerformOnceBeforeTransitioning)」・WaterfallGiant: ABOUT_TO_BLOW_MOVEも同フラグで必ず1回実行＝変身・転換の瞬間が必ず1行動として画面に出る。
- 現状(採掘時点): combat.ts declareIntents L287-291: HP50%を跨ぐと次の宣言から黙ってmovesBelowHalf/sequenceBelowHalfに切り替わる。石殻の番人「殻割れ」・苔の主「牙をむく」・ボス第2形態は、ログと図鑑で知らない限りテーブルが差し替わったことに気づけない（変身の1拍が存在しない）。
- 実装スケッチ: EnemyDef に `transitionMove?: string`（movesのid）を追加: belowHalf条件を初めて満たした宣言はこの行動で固定（一度きり。次からsequenceBelowHalf）。無償ターン化を避けるため攻撃付きで書く（オーガT1教訓と整合）: 石殻の番人に {id:"shell_crack_roar", kind:"attack", min:8, max:10, alsoBuff:2}＝「殻割れの咆哮」、苔の主に牙をむく1拍。意図表示は通常配管のまま（宣言時に確定するので窓は嘘をつかない）。

## [28] (behavior-grammar) 一時バフ（次の攻撃だけ+N。Vigor型）＝チャージ大技を強化式で書ける器
- 状態: 📋 M
- 根拠: monsters-m-z.md TerrorEel: ThrashMoveで自身にVigorPower6（次の攻撃力上昇バフ）を付与。恒久Strengthと違い「次の1回だけ」なので、剥がす・凌ぐの解答が1ターンで完結する。
- 現状(採掘時点): 敵のbuff/rally/alsoBuffはすべて恒久strength加算のみ（combat.ts executeEnemyAction case 'buff' L1381-1390で確認）。「構え→大技」は大亀・斧鬼がsequenceの数値差で手書きしており、構えターンの防御値と大技の実値が独立＝「構えを打ち消せば大技が萎む」という因果が作れない。
- 実装スケッチ: EnemyMove に `alsoVigor?: number`（この行動と同時に自身へ一時バフ+N。次の攻撃1回の実値と幅表示に加算して消費）を追加し、敵インスタンスに `vigor: number`。buildIntentで攻撃宣言時にvigorを幅・実値へ乗せ、実行後に0へ。打ち消しはvigor付与ごと消す（打ち消しの既存則=行動単位の無効化）。適用例: 幕3新敵「溜め撃ちの射手」(構え=defend+alsoVigor:8→射撃12-15)＝構えを打ち消す/凌ぐで次の被弾が8変わる読み合い。

## [29] (behavior-grammar) 選ばせる呪い（KnowledgeDemon式2択）＝「どの毒を飲むか」をプレイヤーに決めさせる行動
- 状態: 🔶 戦闘内選択UI=新コマンド
- 根拠: sts2-reference.md §4「選ばせる呪い ★: KnowledgeDemonは2択を3回プレイヤーに選ばせてデッキに入れる。呪いの押し付けでなく『どの毒を飲むか』の決断」＋§8-2の拡張候補②。monsters-a-l.md KnowledgeDemon: 3段階のカードセット各2択、回数カウンタで3回打ち止め。
- 現状(採掘時点): デッキ汚染は負傷(捨て札)・がらくた(山札)・火傷(手札)・烙印(ラン永続)の4種すべて敵が一方的に注入する形（applyStatusToPlayer。選択肢なし）。敵フェーズ中にプレイヤーへ選択を求める配管は確認ウィンドウ(set-confirm)のみで、カード選択型の敵行動は不在。
- 実装スケッチ: EnemyActionKind に 'curse-choice' を追加し、EnemyMove に `choices?: readonly [StatusInflict, StatusInflict]` を追加。解決時に確認ウィンドウの配管を流用して2択を提示（例: 「負傷2を捨て札に」vs「火傷1を手札に」）、選んだ側を適用。ボットは規約で常に先頭を選ぶ（?マスの「最後は立ち去る」と同型の壊れ検知規約）。適用例: 幕3ボス級新敵、または妖術師の上位種。提案4の回数カウンタと組めば本家同様「3回で打ち止め」も書ける。

## [30] (behavior-grammar) とげの着脱サイクル（SpinyToad式）＝敵行動で自分のとげを増減する器
- 状態: 📋 M
- 根拠: monsters-m-z.md SpinyToad: 「棘を出す(Thorns+5)→自爆技で棘を消費(Thorns-5)して25ダメ→通常攻撃」の3拍子＝とげが静的値でなく行動と連動するリソース。Toadpole: SPIKEN(+2)で貯めてSPIKE_SPIT(-2)で消費して多段攻撃。
- 現状(採掘時点): thorns はEnemyDefの静的値を戦闘開始時に敵インスタンスへコピーするだけで（combat.ts L171-172）、戦闘中に増減する手段は無い。針毛の栗鼠はとげ2が常時一定＝「いつ殴るか」のタイミング要素が無い。
- 実装スケッチ: EnemyMove に `alsoThorns?: number`（この行動と同時に自身のthornsを±N。下限0）を追加。executeEnemyActionの各kindの解決後に加算し、敵カードのとげ表示は既存のインスタンス値参照なので自動追随。適用例: 幕2新敵「棘丸まり」——sequence: 棘立て(defend 6-8+alsoThorns:3)→棘飛ばし(attack 4-6×3 + alsoThorns:-3)→隙(rest)＝「とげが立っている今殴るか、抜け毛の隙を待つか」のタイミングパズル。とげ持ちは防御行動を持たない裁定（2026-08-30）とは「隙ターンを必ず持つ」ことで両立させる。

## [31] (debuff-system) 脆弱のjustAppliedガード（付与フェーズの即減衰でスタックが1つ蒸発している）
- 状態: ✅ 75515b2 脆弱justAppliedガード(バグ修正)
- 根拠: powers.md §0「Weak/Vulnerable/FrailはEnemyターン終了時に一律1減衰」+ §2-a RitualPowerの_wasJustAppliedByEnemyフラグ（付与直後の無償トリガー防止ガード）。本家は「付与された直後のラウンド境界の減衰を1回スキップ」する思想を敵バフ側にまで適用している。sts2-reference §4「コア3種はStS1と完全に同一…うちの弱体/脆弱/虚弱は本家準拠で正しい」の検証（観点⑧）で発見したズレ
- 現状(採掘時点): combat.ts:1510 finishEnemyPhaseで vulnerable を無条件に-1している。applyStatusToPlayer（combat.ts:1163）は敵の攻撃解決中に即加算するため、脆弱Nは付与フェーズの終端で即1減る。実測の帰結: encounters.jsonのenc_squire_archer（従士index0→射手index1の行動順）では射手の「狙い撃ち+脆弱1」が付与された時点でこのフェーズの攻撃は全て解決済み→フェーズ終端で0に消え、完全な無効果。CLAUDE.md「射手=届かない敵が圧を積む→陣形を崩す動機」の設計意図が実装されていない。道化ソロの脆弱2も実効1フェーズ。弱体/虚弱は自ターン終了時減衰でズレなし（弱体Nは満額Nターン作用する）
- 実装スケッチ: PlayerState に vulnerableJustApplied: boolean を追加。applyStatusToPlayer で status==='vulnerable' の時に true を立て（脆弱は敵行動でしか付与されないため無条件でよい。カード側にStatusInflictを持つ配管は無いことをgrepで確認済み）、finishEnemyPhase（combat.ts:1505）で「フラグが立っていれば減衰をスキップしてフラグを消す」。既存テストは脆弱の残量アサートを+1ずらす。これで脆弱1=次の敵フェーズ1回分の圧、という表示どおりの意味になる

## [32] (debuff-system) 火傷の「1回きり」仕様と実装の乖離を解消（現状は本家Burn型の循環になっている）
- 状態: ✅ 75515b2 火傷の生存則を二重修正(バグ修正)
- 根拠: powers.md §3-a: 本家はBurn（2ダメ・捨て札を回り続ける）と Toxic（5ダメ・ティック後Exhaust）の両方を持ち、循環型と使い切り型を別カードとして区別している。うちのCLAUDE.md（確定済みルール表「敵ギミック第1波」①）は火傷を「ターン終了の全捨てで消える=1回きり」と定義
- 現状(採掘時点): combat.ts:1550-1557 finishEnemyPhase の手札全捨てに火傷の除去フィルタが無く、SCALD_DEF札は捨て札→再シャッフルで山札に戻り、引き直すたび再ティックする＝実装は本家Burn型の循環。しかも applyStatusToPlayer の上限カウント（combat.ts:1176-1180）は hand+drawPile+discardPile を数えており、循環を前提にした書き方になっている。仕様（1回きり）と実装（循環）が食い違っている
- 実装スケッチ: どちらを正とするかの裁定後に統一する。案A（仕様を正）: finishEnemyPhase の全捨て時に SCALD_DEF を exhaustPile へ分流（fireExhaustTriggers/亡骸は発火させない or させるかも要裁定——黒はミル換金があるため）。案B（実装を正・推奨）: CLAUDE.mdの記述を「本家Burn準拠・戦闘内循環」に改め、循環するからこそ捨て/消滅コストで焼却する黒赤の色相性が立つ、と明文化。その上で提案3のToxic段を「1回きり版」として別に立てると本家のラダーと同型になる

## [33] (debuff-system) 手札滞留ダメージ札のラダー化（Toxic/Beckon段の追加）と滞留ダメージの共通API化
- 状態: 📋 M
- 根拠: powers.md §3-a / sts2-reference §4: 本家は Burn(2)/Infection(3)/Toxic(5+消滅)/Beckon(6) の4段ラダーを持ち、OnTurnEndInHand という共通APIで「手札に居座ると発火する」を汎用化している（StS1のカード個別実装からの一般化=★新）
- 現状(採掘時点): うちは火傷2・烙印1の2段のみで、しかも combat.ts:979 で `scalds * 2 + brands * 1` とID直参照のハードコード。滞留ダメージ量を第3の札に拡張する口が無い。幕2の焚きつけのインプと火薬樽の大爆発しか火傷を配らず、幕3に滞留圧の上位段が存在しない
- 実装スケッチ: ①CardDef に lingerDamage?: number（自ターン終了時に手札にあるとNダメ）を追加し、combat.ts:977-985 のティックを「手札の lingerDamage 合計」の汎用ループに置換（SCALD_DEF/BRAND_DEFはフィールド値2/1へ移行、挙動不変をテストで固定）。②上位段「劇痛」status_toxic（lingerDamage:5・ティック後は消滅置き場へ=1回きり）を新設し、幕3の新敵かエリート（例: 焚きつけのインプの上位種「業火のインプ」）の大技に inflict: {status:'toxic', amount:1} で配布。上限は火傷と同じ思想で3枚/戦闘。捨て/消滅コストで処分可の既存則は共通APIが自然に継承する

## [34] (debuff-system) 行動数制限デバフ「怠惰」（Sloth式・このターンN枚まで）＝手数デッキへの新しい問い
- 状態: ✅ ee5569c 拘束(1ターン3枚まで)
- 根拠: powers.md §1-b SlothPower（1ターンのプレイ可能枚数をN枚に制限）+ §3-b Normality。sts2-reference §4「『何枚プレイできるか』がデバフの主戦場」——Sloth/Ringing/Smoggy/Normalityと4種も刷っている本家の主力レイヤーがうちに丸ごと無い
- 現状(採掘時点): PlayerStatus は weak/vulnerable/frail/wound/junk/scald の6種（types.ts:744）で行動数制限系はゼロ。手数への問いは物真似の鏡（mirrorHits=事後の罰）と激昂タイマーのみで、「プレイ枚数そのものを縛る」事前制約が存在しない。playCard のガード（combat.ts:545前後）にも枚数上限チェックは無い
- 実装スケッチ: PlayerStatus に 'sloth'（怠惰: 残りNターン、1ターンにプレイできるカード3枚まで）を追加。amount=残りターン数・上限は固定3枚（猛り火8と同じ「単一しきい値をひとつ覚えれば読める」哲学）。実装: ①applyStatusToPlayer にカウンター加算 ②playCard 冒頭で sloth>0 && cardsPlayedThisTurn>=3 なら throw ③自ターン終了時に-1（弱体と同じ対称則） ④意図表示に「💤怠惰N」予告 ⑤UIは手札を灰色化+チップ表示。配布先: 幕3新敵「惰眠の霧霊」（怠惰1→攻撃→攻撃のローテ）か眠れる鉄卵の尾を弱体2→怠惰1に差し替え。伏せる・リアクション発動は枚数に数えない（プレイでないため）＝set-confirmの読み合いは縛らない設計が自然に出る

## [35] (debuff-system) ドロー減衰デバフ「霞み」（MindRot式）——リソース削り系の安全な第一歩
- 状態: ✅ a5e1c52 霞み(梟の大技)
- 根拠: powers.md §1-b MindRotPower（ドロー枚数-N=Weakのドロー版）/ NoDrawPower（完全0化）。sts2-reference §4「-25%の上に『完全に0』の段が別にある」
- 現状(採掘時点): リソース（ドロー/エナジー）に触るプレイヤーデバフはゼロ。drawCards は startPlayerTurn（combat.ts:460）で drawPerTurn を素のまま引く。手札は敵フェーズ後に全捨てされる仕様のため、本家式の完全NoDrawを直輸入すると「次ターン手札0枚=丸ごとスキップ」の即死級になり「予告してから殺す」でも受けが無い
- 実装スケッチ: PlayerStatus に 'mist'（霞み: 残りNターン、ターン開始のドロー-2・最低3枚）を追加。startPlayerTurn のドローを drawCards(s, Math.max(3, s.player.drawPerTurn - (mist>0?2:0))) に変更、自ターン終了時に-1。意図予告・UIチップは弱体と同配管。配布先: 泥投げの妖術師の幕3変種か、読み手の梟の「見抜き」に追加（伏せを見た梟が視界を奪う=フレーバー整合）。完全0化（NoDraw/NoEnergyGain）は全捨てルールと衝突するため導入しない、と規約に明記する

## [36] (debuff-system) ブロック0倍化の劇薬「守崩し」（NoBlock式・1ターン限定）＝虚弱の上位段
- 状態: 📋 M
- 根拠: powers.md §1-b NoBlockPower「Frailの-25%と違いブロックを完全ゼロ化する上位互換的デバフ。StS1未確認」。sts2-reference §4のリソースゼロ化3種のうち、うちの全捨てルールと衝突しない唯一の1つ
- 現状(採掘時点): ブロック系デバフは虚弱（-25%・combat.ts effects.ts:242-243）のみで上位段が無い。虚弱は敵圧監査（2026-09-01）で「殴り切るデッキに無風=守るデッキ専用の罰」という構造非対称が既知。氷壁・リアクション由来は虚弱の対象外（resolvingCardPlayフラグ）
- 実装スケッチ: PlayerStatus に 'noGuard'（守崩し: 次の自ターン、カードのプレイで得るブロックが0になる）を追加。実装は gainPlayerBlock（effects.ts:238）の虚弱ガードの隣に「noGuard>0 なら amount=0」を追加し、自ターン終了時に-1（実質1ターン限定）。虚弱と同じく氷壁・リアクション・置物は対象外＝色の個性を侵さない既存裁定を継承。配布先: ボス級の一回きり大技専用（例: 大亀の第2形態「甲羅砕き」に inflict:{status:'noGuard',amount:1}、UseOnlyOnce相当はsequenceで1回だけ踏む位置に置く）。予告必須（意図表示「🛡️✖守崩し」）——「次のターンは受けられない→リアクションか氷壁か回避で凌げ」という読みの問いになり、set-confirmの伏せ札が解答になる

## [37] (debuff-system) 常在オーラ（Afflictions-lite）＝「この敵が生きている間ルールが歪む」レイヤー
- 状態: ✅ ee5569c 重圧オーラ(effectiveCostフック)
- 根拠: powers.md §4 Afflictionsシステム（本家最大の新機構）: Tangled=攻撃カードのコスト+N・Weighted=二重コスト・Hexed=全カードEthereal化等を、敵Powerの生存中だけデッキ全体に掛け「敵が死ぬと全部剥がれる」。sts2-reference §8-2③「打ち消し・キル順と好相性」の名指し推薦
- 現状(採掘時点): grep で aura 系フィールドは engine/data に不存在。敵の存在がプレイヤー側のルールを常時歪める機構はゼロ（angerOnBlock/mirrorHits は行動への反応であり常在制約ではない）。effectiveCost（effects.ts:38-46）は xCost/freeThisCombat/blazeDiscount/nextCardDiscount のみを見る単一のコスト門で、フックを1箇所刺せば全域に効く構造が既にある
- 実装スケッチ: EnemyDef に aura?: { kind: 'attackCostUp' | 'spellCostUp', amount: number } を追加。effectiveCost に「生存中の敵の aura を走査し、該当タイプのカードのコストに+amount」を追加（Math.max(0,…)の前）。敵が死ねば自動で解除（毎回生存チェックなので解除処理不要=イミュータブル設計と相性が良い）。カード1枚ずつのAffliction配布は重いので、この「敵単位の常在ルール」形で本質（キル順の動機付け）だけ移植する。第1号: 幕2新敵「縛鎖の霊」（HP48・攻撃6-9のみの弱い行動+aura: 攻撃カードコスト+1）——本体は弱いが生かしておくと1ターンの手数が1枚減る=先に潰す動機が数値でなくルールで立つ。UI: 敵カードに常時チップ「⛓️攻撃+1コスト」（burnResistと同じフェアネス）、手札のコスト表示も effectiveCost 経由に統一されているか要確認・summary.ts の cardCostLabel に反映

## [38] (debuff-system) デバフのメタ増幅（Debilitate式）＝弱体の効果量そのものを深くする敵
- 状態: 📋 M
- 根拠: powers.md §1-b DebilitatePower/CrueltyPower「Weak/Vulnerableの効果式に外部フックが刺さる設計自体が新しい」（★新まとめ代表2番）。固定倍率だった基幹デバフの-25%/+50%を動的に動かす
- 現状(採掘時点): 弱体は effects.ts:528-531 で ×0.75 固定、脆弱は combat.ts:1320 で ×1.5 固定。倍率に触る外部フックは無い。敵圧監査で弱体3の「割合デバフだけが完成デッキに自動スケールする」実証済み＝倍率を深くする器は幕2の谷への追加レバーになりうる
- 実装スケッチ: EnemyDef に debuffAmp?: { weakRate?: number, vulnRate?: number } を追加（例: weakRate: 0.6 = この敵が生存中、弱体の与ダメが×0.75でなく×0.6）。effects.ts の弱体適用2箇所と combat.ts の脆弱適用1箇所で、生存敵の debuffAmp の最小/最大を採る。第1号: 鎖持ちの奴隷商の幕3変種「呪鎖の奴隷商」に weakRate:0.6（鎖のフレーバー直結。既に弱体2+脆弱3の使い手なので機構が二乗で効く）。敵カードに常時チップ「⛓️弱体強化」。付与量のインフレでなく質を深くする=「数値でなくバリエーション」の方針に整合

## [39] (debuff-system) 敵アーティファクト（デバフ付与をN回弾く）＝デバフ依存デッキへの問い
- 状態: 🔶 同上(アーティファクト)
- 根拠: powers.md §2-a ArtifactPower「次に受けるデバフ付与をN回無効化（付与ごとにCounterが1減る）」+ sts2-reference §3-5「ほぼ全敵が開幕パワー持ち。常在パワーが難易度設計の主軸」
- 現状(採掘時点): プレイヤーが敵に付与するデバフ（延焼・急所・混乱・威圧・虚弱化は無いが weakenEnemy）を妨げる機構はゼロ。grep で artifact 系フィールド不存在。延焼は burnResist（毎フェーズ追加減衰）があるが「付与自体を弾く」層は無く、バーン型ボット勝率90%等のデバフ依存デッキは全敵に同じ強さで通る
- 実装スケッチ: EnemyDef/EnemyState に artifact?: number を追加（戦闘開始時にdefからコピー）。applyBurn/exposeEnemy/confuse/weakenEnemy の敵向けデバフ適用の共通入口で「artifact>0 なら付与を無効化して-1・ArtifactBroken イベント発行」。開幕から常時チップ表示「🛡️✨結界N」（とげ・延焼耐性と同じフェアネス則）。配布は2〜3体に留める（例: 石殻の番人の幕3変種に2・新エリートに3）——本家の配布率ほぼ100%は真似ず、「まず素のデバフを1回捨てて剥がすか、素通しの物理で押すか」のマッチアップとして点在させる。0コストの捨てデバフ（火花の延焼等）が剥がし札として自然に価値を持つ

## [40] (debuff-system) 重りデバフ（SlowPower式）＝手数の罰の被弾版。cardsPlayedThisTurn流用で実装最小
- 状態: ✅ a5e1c52 重り(奴隷商の錘)
- 根拠: powers.md §1-b SlowPower「カードをプレイするたび内部カウンタ+1し被弾+10%×カウンタ。次の自ターン開始でリセット」。sts2-reference §4「手数への罰を被弾側に付けた形。うちのmirrorHits（手数の鏡）の親戚」
- 現状(採掘時点): 手数への圧は物真似の mirrorHits（敵の攻撃ヒット数=プレイ枚数）のみ。被弾倍率を手数に連動させる機構は無い。実装素材は揃っている: cardsPlayedThisTurn は敵フェーズ中も保持され（combat.ts:1302-1306 で mirrorHits が既に読んでいる）、脆弱の倍率適用点（combat.ts:1320）に1行で挿せる
- 実装スケッチ: PlayerStatus に 'slow'（重り: 残りNフェーズ、敵の攻撃ダメージが+10%×このターンのプレイ枚数・切り捨て）を追加。攻撃解決（combat.ts:1314-1320）で slow>0 なら v = floor(v * (1 + 0.1 * cardsPlayedThisTurn))、脆弱と同じく敵フェーズ終了時に-1（提案1のjustAppliedガードを共用）。配布先: 写し身の魔人の「視線」を弱体3→重り2に差し替えるか、幕2の新敵に。mirrorHits（ヒット数が増える=威圧・氷壁で受けやすい）と重り（1発が重くなる=ブロック総量で受ける）は同じ問いの別解で、手数デッキに2種類の受け方を要求できる

## [41] (debuff-system) 選ばせる呪い（KnowledgeDemon式）＝?イベント「知識の悪魔」で2種の毒から1つを飲ませる
- 状態: ✅ a5e1c52 イベント版(毒の三杯)として実装。戦闘内2択UIは裁定枠
- 根拠: powers.md §3-a MindRot/WasteAway/Sloth/Disintegration は KnowledgeDemon.IChoosable で「複数の呪いから1つを選ばされる」専用選択肢。sts2-reference §4「呪いの押し付けでなく『どの毒を飲むか』の決断」＋§8-2②の名指し推薦
- 現状(採掘時点): 呪いを配る?イベントは黒曜の偶像・血の祭壇の2件（events.json:719-741）で、いずれも烙印（BRAND_DEF固定）を「量」で払う形。呪いの「種類」を選ぶ決断は存在しない。run.ts:548 applyOutcome は gold/hp/hpRatio/wounds/brands のみ対応でカードID指定の呪い配布口が無い
- 実装スケッチ: ①applyOutcome の brands を curses?: { id: string, count: number } に一般化（brandsは互換維持）。②呪い札を2種追加: 「疑念の烙印」status_doubt（自ターン終了時に手札にあると弱体+1。powers.md Doubt準拠・ラン永続・除去可能）と「後悔の烙印」status_regret（自ターン終了時、手札の枚数ぶんHP損失。powers.md Regret準拠）。③?イベント「知識の悪魔」（kind:once）: 「禁断の知識を授かる（レリック1個 or 220G）——代わりに疑念か後悔のどちらかを選んでデッキへ」＋立ち去る。後悔は青の抱え込み（手札×N）アーキへの初のカウンター圧になる＝どの毒が自分のデッキに刺さらないかを読む決断。ターン終了フックは提案3の lingerDamage 汎用化と同じ場所に lingerInflict?: StatusInflict として同居できる

## [42] (debuff-system) 呪い札の軽量変種: N戦で自然消滅する「仮初の烙印」（Guilty式）と金を削る「借金」（Debt式）
- 状態: ✅ a5e1c52 時限呪い=仮初の烙印(借金はrun層純度の検討要=見送り)
- 根拠: powers.md §3-b Guilty「デッキに残ったまま5戦闘を経過すると自動的に消える（★新: タイマー付き呪い）」/ Debt「手札滞留でゴールドをN失う（★新）」。sts2-reference §8-3「烙印◎（本家は呪い札16種。Guilty=5戦で自然消滅、Debt=金を削る等の変種）」
- 現状(採掘時点): ラン永続の呪いは烙印1種のみ（BRAND_DEF・毎ターン手札滞留でHP-1・除去には焚き火/ショップの実費）。「軽い対価」の通貨が無いため、?イベントの取引はHP・ゴールド・烙印の3択に固定されている。CardInstance に寿命フィールドは無い（grep確認）
- 実装スケッチ: ①CardInstance に expiresAfterBattles?: number を追加し、run層の戦闘決着処理でデッキ内の該当札を-1・0で自動除去（「仮初の烙印」status_guilt = 烙印と同じ滞留HP-1だが3戦で消える）。イベントの中間対価として「重い報酬+烙印」と「軽い報酬+仮初」の2段が作れる。②「借金」status_debt（自ターン終了時に手札にあるとゴールド-3・最低0。ラン永続）を追加——盗み・懸賞金でゴールドが戦闘と接続済みのうちでは、HPでなく財布を削る呪いが除去サービス逓増（+50G）と絡んで「除去する金を呪いが食う」皮肉なループになる。ショップ強盗系イベント（つけ払いの行商人: 好きなカード1枚無料+借金1）の新設とセットで

## [43] (debuff-system) 粘液札（Slimed式）＝1エナジーで掃除できる柔らかい汚染。大苔スライムに配布
- 状態: 📋 M
- 根拠: powers.md §3-a Slimed「コスト1・プレイすると1ドロー・消滅」——プレイ可能なステータス札。sts2-reference §4「注入先は捨て札が多い（Slimed・Dazed・Wound）。SlimedBerserkerは1技でSlimed10枚を捨て札に注入」＝量で圧をかけても掃除の口があるから成立する
- 現状(採掘時点): うちの汚染札4種（負傷・がらくた・火傷・烙印）は全て使用不可の完全な死に札で、「エナジーを払えば処分できる」中間硬度が無い。分裂持ちの大苔スライム（2026-09-02実装済み）はスライム系なのに粘液を注入しない——本家スライムのアイデンティティが片翼
- 実装スケッチ: STATUS札「粘液」status_slime を新設: コスト1・効果なし・プレイで消滅置き場へ（onPlay効果を「消滅のみ」にすれば isPlayableFromHand が true になる既存配管で実装可。effects:[{trigger:'onPlay', effect:'exhaustSelf'相当…既存に無ければ exhaust:true+空効果で可視化）。PlayerStatus に 'slime' を追加し捨て札へ混入（wound と同配管・上限5）。大苔スライムの叩きつけに inflict:{status:'slime',amount:2}、分裂体の噛みつきに amount:1 を追加。負傷（永続死に札）との差別化=「1E払えば今掃除できるが、そのエナジーは攻防から抜ける」というテンポの問い。黒はプレイ消滅が刻・亡者の合唱の燃料になる=色相性も出る

## [44] (debuff-system) 錯乱デバフ（Confused/Snecko式）＝ドローごとにコストを0〜3へ乱数化するボス級の霧
- 状態: 🔶 ボス級の霧=錯乱
- 根拠: powers.md §1-a ConfusedPower「カードをドローするたび、そのカードのコストを0〜3のランダム値に上書き」（StS1 Snecko系と同一仕様が本家でも現役）
- 現状(採掘時点): コストに触るプレイヤーデバフはゼロ。CardInstance にコスト上書きフィールドは無い（freeThisCombat=0E固定はあるが乱数上書きは不可）。名前空間の注意: 'confuse' は既存の敵向け混乱（青の攻撃逸らし）が使用中のため別名が必要
- 実装スケッチ: PlayerStatus に 'delirium'（錯乱: 残りNターン、ドローした札のコストが0〜3のシード乱数に上書きされる）を追加。CardInstance に costOverride?: number を追加し、drawCards 内で delirium>0 の時に nextInt(rng,0,3) でロールして刻印（シードRNG=リプレイ再現性維持）、effectiveCost の冒頭で costOverride を優先。自ターン終了時に-1・札が手札を離れたら override は消す。UI は上書きコストを紫色表示。配布先はボス級1体限定（例: 幕3の新エリート「夢喰いの獏」の大技で錯乱2）——0Eの当たりと3Eの外れが混ざる「賭けの手札」は激昂・怠惰と違う軸の悩みで、幅あり意図と同じ「不確実性を読む」というゲームの文法に沿う

## [45] (debuff-system) 1ターン被ダメ累計上限（HardenedShell式）＝装甲の相補変種・バースト対策
- 状態: 📋 M
- 根拠: powers.md §2-b HardenedShellPower「1ターンあたりの被ダメ累計に上限を設け超過分をカット。ターン開始でリセット」——armor（1ヒット上限=HardToKill相当）と対を成す本家の防御パワー
- 現状(採掘時点): うちの armor は1ヒット上限のみ（types.ts:129・確定済みルール表「装甲」）。難易度検証の副見④「装甲が緑多段に無力（20×9ヒットは上限35に一度も触れない）」が実測済み＝多段には別の問いを出せていない。ターン累計のキャップ機構は不存在（grep確認）
- 実装スケッチ: EnemyDef に turnDamageCap?: number を追加。EnemyState に damageTakenThisTurn を持ち、dealDamageToEnemy で「このターンの累計が cap を超える分をカット」、自ターン終了時（endTurn）にリセット。常時チップ表示「⛔累計N上限」（armor と同じフェアネス則）。配布は装甲と排他にする（両方持つと理不尽）——多段デッキには「手数を割っても累計で頭打ち=2ターンに分けろ」、一撃デッキには「1発で cap まで」と、armor と正反対の問いになる。第1号: 幕3の甲殻系新個体か不滅の騎士の変種に cap=40（幕3想定ターン火力66の6割）

## [46] (debuff-system) 虚無札（Void式）＝引いた瞬間に痛む山札の地雷
- 状態: 📋 M
- 根拠: powers.md §3-a Void「ドローされた瞬間にエナジーを1失う（手札に来るだけで損をする）+ Ethereal自動消滅」——滞留型（Burn）とも死に札型（Dazed/Wound）とも違う第3の汚染タイミング
- 現状(採掘時点): うちの汚染4種の発火タイミングは「滞留ティック」（火傷・烙印）と「枠の占有」（負傷・がらくた）の2種のみ。「ドローの瞬間」に発火する汚染は無い（drawCards にステータス札フックは不存在）。がらくたは山札に混ざるが引いても無害＝引く恐怖が無い
- 実装スケッチ: STATUS札「虚脱」status_void を新設: 使用不可・ドローされた瞬間にエナジー-1（最低0）・自ターン終了の全捨て時に消滅置き場へ（Ethereal相当=1回きり）。drawCards 内で引いた札の def.id を判定して player.energy を減らし VoidDrained イベントを発行（ターン開始ドローでもエナジー全回復の後に走る順序を機械固定）。上限3枚/戦闘。配布先: 幕3の妖術師系ペア新編成の大技（inflict:{status:'void',amount:2}）——がらくた（枠を潰す）との差別化は「引く前から怖い」時限性で、ミル・ドローエンジン（青の泉・黒の契約）ほど早く踏む=ドロー依存デッキへの逆進的な問いになる

## [47] (debuff-system) 意図アイコンの「カード汚染予告」分離（CardDebuff式）
- 状態: ✅ 75515b2 汚染の行き先予告
- 根拠: sts2-reference §5 意図タイプ15種: 本家は Debuff と CardDebuff（カード汚染予告）・DebuffStrong（強デバフ専用）を別アイコンで制度化——「予告してから殺す」を情報の質で支えている
- 現状(採掘時点): うちの意図表示は inflict の内容を予告する（確定済みルール表「状態異常」のフェアネス則）が、カウンター系（弱体・脆弱・虚弱）と札汚染系（負傷・がらくた・火傷）が同じ「付与予告」の見た目に混ざる。汚染はデッキ品質への恒久/準恒久ダメージでカウンターより意思決定の重みが違うのに、表示上の区別が無い
- 実装スケッチ: 表示層のみの変更（engine不変）。意図予告のラベルマップで status の種別により接頭アイコンを分岐: カウンター系=「🌀弱体2」、札汚染系=「🃏負傷2」「🃏がらくた1」、手札直撃系=「🔥火傷2」。UI の意図チップと CLI の show 出力の2箇所。提案3〜16で汚染の種類が増えるほど効く安い先行投資

## [48] (debuff-system) 処刑ライン（Doom式）の黒カード機構化——「HPしきい値の常時監視」
- 状態: 🔶 黒カード機構=カードパワー凍結中
- 根拠: powers.md §1-b DoomPower「自ターン終了時、HPがDoom値以下なら即死処理」（★新まとめ代表3番）——一撃処刑（とどめの一撃=HP25%以下で8→24）とは別の「継続する処刑ライン」型
- 現状(採掘時点): 処刑系は赤のとどめの一撃（プレイ時の一回判定）のみ。敵に付与して残る「しきい値監視」デバフは不存在。黒のドレイン・ミル・自傷という「じわじわ削る」柱と処刑ラインの相性は良いが機構が無い
- 実装スケッチ: 敵向けデバフ効果 'applyDoom'（対象に死告N: 敵フェーズ終了時、その敵のHPがN以下なら即死）を新設。finishEnemyPhase の再生判定の隣に判定を追加・DoomKill イベント発行・敵カードに「☠️N」チップ常時表示。黒のレア1枚（例: 「死神の刻印」2E・死告13・消滅）として導入すれば、削り切れない大型ボス戦の黒の出口になり、回復役・再生持ちへの構造的な回答にもなる（回復してもラインを跨げなければ死ぬ）

## [49] (gimmick-variants) スタン付き死亡時召喚（罰型の分裂）— splitInto.stunned 拡張
- 状態: ✅ 66c83e8 スタン付き分裂(蛙鬼)
- 根拠: StS2のInfestedPower（PhrogParasiteElite: 死亡時にスタン済みの子Wrigglerを4体召喚）とSurprisePower（ゴブリン系: 死亡時増援）。docs/sts2/powers.md §2-b。増援は必ずSPAWNED_MOVE（1ターン様子見）から入る＝monsters-m-z.md Wriggler「後から出現した個体は1ターン様子見してから合流」
- 現状(採掘時点): src/engine/combat.ts processSplits (L475-510) は分裂体を生成した瞬間に buildIntent で意図を宣言し、その敵フェーズから行動させる（本家Slime準拠のコメントあり）。スタン付き・様子見付きで出す選択肢は無い。splitInto の型 (types.ts L846) は {enemyId, count} のみ
- 実装スケッチ: types.ts の splitInto に readonly stunned?: boolean を追加。processSplits で stunned===true なら子の初回意図を合成の 'rest' 行動（id:'spawn_daze', kind:'rest'）にして patternIndex:0 から次宣言で本来の sequence に入る。使い先: 幕3新敵「蟲抱えの蛙鬼」(HP90・splitInto:{enemyId:'enemy_wriggler', count:3, stunned:true}。幼体=HP18・噛み4-6)。大苔スライム（即行動×2＝圧の継続）に対し「3体湧くが1ターンの猶予がある＝全体攻撃の売り時」という別の問い。チップ表示は既存の🫠分裂チップ (App.tsx L1647) に「（出現ターンは動かない）」を追記

## [50] (gimmick-variants) 残機（Stock式）— splitIntoチェーンの再利用でエンジン変更ゼロ
- 状態: ✅ 66c83e8 残機(不滅の骸兵)
- 根拠: StS2のStockPower（Axebot: 死亡時、在庫が残っていれば下位個体を再生成）とAdaptablePower（TestSubject: 死亡のたび第2形態212HP→第3形態313HPへ変身して復活。2回倒すまで死なない）。monsters-a-l.md Axebot / monsters-m-z.md TestSubject
- 現状(採掘時点): processSplits (combat.ts L475-510) は splitInto の enemyId を任意に取れ、split フラグは個体ごとなので「Aを倒すとBが出て、Bを倒すとCが出る」チェーンは既存実装のまま成立する（コードで確認済み・未使用）。現行の使用例は大苔スライム1件のみ (enemies.json enemy_big_slime)
- 実装スケッチ: データのみで実装: 幕3新エリート「不滅の骸兵・参」(HP60・斬り14-18) → splitInto:{enemyId:'enemy_husk_2', count:1} → 「骸兵・弐」(HP40・斬り12-16・強化+1で出す) → 「骸兵・壱」(HP25・斬り10-14・強化+2)。倒すたび体は縮むが斬撃は鋭くなる=TestSubject式の逆スケール。合計HP125を3回に分けて払わせる＝オーバーキルが無駄になる（大技一撃デッキへの問い）。UI: 分裂チップの文言を count:1 のとき「♻️残機: 倒すと◯◯として再起動」に分岐 (App.tsx L1648)。エリート報酬配管は既存のELITE_POOLS追加のみ

## [51] (gimmick-variants) 増殖 — 新行動kind 'summon'（上限付き自己召喚・打ち消し可）
- 状態: 📋 M
- 根拠: StS2のTwoTailedRat CALL_FOR_BACKUP（召喚可能条件〔召喚後2ターン経過・召喚3回未満・空きスロットあり〕が揃うと75%の重みで同族を1体追加召喚・個体ごと一度きり）と LivingFog BLOAT（GasBombを1体召喚・満杯ならスキップ）。monsters-m-z.md / monsters-a-l.md
- 現状(採掘時点): EnemyActionKind (types.ts L729-741) に召喚系は無い。敵が戦闘中に増える経路は splitInto の死亡トリガーだけ（grepで確認）。応援(rally)・回復(heal)の「潰す順パズル」配管はあるが、頭数そのものを増やす敵はいない
- 実装スケッチ: ①types.ts: EnemyActionKind に 'summon' を追加、EnemyMove に summonEnemyId?: string と summonCap?: number（生存敵の総数がこれ以上なら宣言時に別の行動へ差し替え。既定3）。②combat.ts: 解決時に processSplits の子生成ブロックを spawnEnemy() ヘルパーに抽出して共用。生成体は 'rest' の初回意図（様子見1ターン=StS2 SPAWNED準拠）。打ち消し可（negate で召喚ごと消える=打ち消しの新しい使い道）。意図表示「👥増援: ◯◯を呼ぶ」。③使い先: 幕2新敵「二尾の大鼠」HP55: 引っ掻き6-8→威嚇(弱体1)→増援(小鼠HP16・噛み4-6)のsequence。sim/play.ts の意図ラベル辞書 (L137,202) に追記。simボットは既存の集中砲火（最低HP優先）で自然に対応

## [52] (gimmick-variants) 召喚エコシステム（場が空くと補充・満杯なら攻撃）— summonのエリート応用
- 状態: 📋 M
- 根拠: StS2のFabricator（生存する味方が4体未満なら召喚50%/攻撃+召喚50%、4体で満杯ならDISINTEGRATE攻撃固定に切替。子分にMinionPower付与）とOvicopter（味方3体以下なら産卵、そうでなければ自己バフ）。monsters-a-l.md Fabricator / monsters-m-z.md Ovicopter
- 現状(採掘時点): 宣言時の盤面条件分岐は伏せ(movesVsSet/setAlt)・従者(movesVsTokens)・HP半分(movesBelowHalf)の3種のみ (combat.ts declareIntents)。味方の頭数を見る分岐は無い。連携(bondStrength)が宣言時判定の前例 (combat.ts L267-270)
- 実装スケッチ: 提案3の 'summon' kind を前提に、EnemyMove.summonCap 到達時のフォールバックを setAlt と同形の readonly fullAlt?: { kind, min, max, hits } で持たせる（宣言時に頭数で判定=宣言時固定の既存則。bondと同じ場所で分岐）。使い先: 幕3新エリート「据えつけの造り手」HP130: 造る(summon: 'enemy_scrap_bot' HP20・突き7-9 / fullAlt: attack 16-20)→薙ぎ12-15→造る…。「子分を無視して本体レース」vs「子分を刈って本体の攻撃化を許す」の二択が毎ターン更新される=Fabricatorの問いそのもの。子分は splitInto なし・低HP＝全体攻撃の的

## [53] (gimmick-variants) 孵化 — hatchIntoによるタイマー変身（打ち消しで孵化を止められる）
- 状態: ✅ 66c83e8 孵化(抱卵の走竜と卵)
- 根拠: StS2のToughEgg + HatchPower（ターン経過カウンタ→HATCH_MOVEで全パワー除去・HPを孵化後の値で再ロール・名義もHATCHLINGに変わる）。powers.md §2-b HatchPower / monsters-m-z.md ToughEgg。「起こす前に削るか」(鉄卵=Lagavulin型)の逆＝「孵る前に割るか」
- 現状(採掘時点): 敵の形態変化はHP50%の行動テーブル切替 (movesBelowHalf) のみで、敵の定義(enemyId)自体が別物に変わる機構は無い。眠れる鉄卵 (enemies.json enemy_elite_iron_egg) は sequence の sleep→awaken→tail で「同じ敵が起きる」だけ。ターン経過で強い別個体になる敵はゼロ
- 実装スケッチ: ①types.ts: EnemyDef.hatchInto?: { enemyId: string } と EnemyActionKind 'hatch'。②combat.ts: hatch 解決時にその index の敵を { enemyId: 新def, hp/maxHp: 新defのmaxHp, strength: 0, patternIndex: 0 } で置換し、新defのsequence[0]で意図を即宣言。打ち消し可＝孵化そのものを1ターン遅らせられる（青の新しい読みどころ）。意図「🐣孵化する」・チップ「🥚孵化予告: 3ターン目に◯◯になる」。③使い先: 幕2新編成「抱卵の走竜と卵×2」— 卵(HP16・sequence: 殻ごもり(defend4-6)→殻ごもり→hatch)→「走竜の仔」(HP22・噛み7-9)。卵のうちに割る(安いが手数を食う)か、親を先に殴って孵化を許すかの資源配分パズル

## [54] (gimmick-variants) 蒸気圧タイマー — 可視の圧力カウンタを積んで自爆する時限爆弾
- 状態: 📋 M
- 根拠: StS2のWaterfallGiant + SteamEruptionPower（ほぼ全行動に+3ずつ蒸気圧を蓄積→ABOUT_TO_BLOWで蓄積量を記録→EXPLODEで蓄積ぶんのダメージを与えて自滅）とGasBombのDeathBlowIntent（攻撃実行直後に自分をKillする自爆技）。monsters-m-z.md WaterfallGiant / monsters-a-l.md GasBomb。「予告してから殺す」原則の模範例
- 現状(採掘時点): 火薬樽かつぎ (enemies.json enemy_bomber) は固定3拍子の big_boom 20-24+火傷2 で、蓄積カウンタも自爆（自分が死ぬ）も無い。砥石の巨像などタイマー型は「数えられる死」だが打点は固定値。敵の自壊機構はエンジンに存在しない（flee の hp:0+fled が唯一の自発退場）
- 実装スケッチ: ①types.ts: EnemyState.pressure?: number、EnemyMove.alsoPressure?: number（行動と同時に自分の圧+N。意図に「+💨3」併記）、EnemyActionKind 'erupt'（ダメージ=圧力×2を与えて自分も hp:0 になる。圧力は可視ステートなので意図は実値表示「💥自爆: 圧力N×2」＝幅表示ルールの例外でなく公開情報の表示）。②打ち消し裁定: negate で爆発は不発、敵はそのまま死ぬ（蒸気が抜ける）＝パーミッションへのマッチアップ報酬。③使い先: 幕3新エリート「湯気吹きの晶像」HP110: 体当たり10-12(+圧3)→踏み15-17(+圧3)→自己回復8(+圧3)→…7手目にerupt（素で圧18×2=36）。「育ちきる前に倒すDPSレースか、36を受け切る算段か」。チップ「💨圧力N」常時表示・log.ts に爆発行

## [55] (gimmick-variants) カード盗み — デッキの人質（盗んだ敵は必ず逃げる・倒せば返る）
- 状態: 🔶 カード人質は体験が過激
- 根拠: StS2のSwipePower + ThievingHopper（山札/捨て札からレアリティ優先度付きで1枚盗んで保持→死亡すると特別報酬として返る。THIEVERY→浮上→攻撃→ESCAPEの一方通行シーケンス）。powers.md §2-b SwipePower / monsters-m-z.md ThievingHopper。盗み経済（Thievery/Heist=金）のカード版
- 現状(採掘時点): 盗み配管は金のみ: stolenGold (types.ts L142)+宣言即成立+盗んだ敵は次宣言で強制逃走 (combat.ts declareIntents L273-285)+勝利時run層精算 (run.ts L971-977)。カードを奪う敵は無い。ミル(大喰らいの蟲)はカードを消滅させる=取り返し不能で、「取り返せる略奪」は未実装
- 実装スケッチ: ①types.ts: EnemyState.stolenCards?: readonly CardInstance[]、EnemyActionKind 'steal-card'。②combat.ts: 解決時に山札の上からN枚（min..maxロール、例1-2）を敵の stolenCards へ移す（山札→敵の懐。亡骸は発火しない=消滅ではない）。盗んだ敵は stolenGold と同じ分岐で次宣言に強制逃走。倒せば stolenCards を捨て札に戻す。逃げられたら**この戦闘中は失うが、勝利すればランのデッキには残る**（combat層のデッキはrun.deckのコピーなので恒久喪失はrun層の追加精算が必要=まずは戦闘内人質で入れる）。意図「🃏カード盗み1〜2枚」。③使い先: 幕2新敵「手癖の悪い小猿」HP55: つまみ食い(steal-card)→引っ掻き7-10→(強制逃走)。ミルと違い「奪われた札がこの戦闘のキーカードなら取り返しに行く」個別性が出る。ラン恒久喪失版（本家準拠のきつさ）は別途裁定

## [56] (gimmick-variants) 棘の着脱サイクル — とげを立てるターンと消えるターンのリズム
- 状態: 📋 S
- 根拠: StS2のSpinyToad（Thorns+5を付与→棘を消費する自爆攻撃25→素の舌打ち、の3拍子で棘が点滅する）とToadpole（SPIKEN+2で貯めてSPIKE_SPIT多段で-2消費する棘貯蓄）。monsters-m-z.md SpinyToad / Toadpole
- 現状(採掘時点): thorns は EnemyDef の静的フィールド (types.ts L807) を戦闘開始時に EnemyState へコピーするだけで、戦闘中に増減する手段が無い。とげの問いは常時一定＝「多段を自制するか一撃で抜くか」が戦闘開始時に一度だけ決まる。裁定「とげ持ちは防御行動を持たない」(CLAUDE.md) は維持可能（防御でなくとげ付与で受ける）
- 実装スケッチ: ①types.ts: EnemyMove.alsoThorns?: number（行動と同時に自分のとげ+N。負値で消費）。意図表示に「+🌵3」併記。②combat.ts: 行動解決時に enemy.thorns を加算（下限0）。③使い先: 幕2新敵「棘吹きの蟇蛙」HP80・素のthorns0: 棘立て(kind:'buff' 強化0+alsoThorns3)→棘爆ぜ(攻撃16-20・alsoThorns:-3=全消費)→舌打ち12-14素、の3拍子。「とげが立つターンは手数を止め、消えた直後のターンに叩き込む」＝とげの問いを時間軸に展開し、手数デッキに『いつ』の判断を毎ターン要求する。既存の🌵チップは現在値を表示しているのでUI変更は最小

## [57] (gimmick-variants) 完全ブロックでスタン（体勢崩し）— 「完全に凌いだ」の機械的報酬化
- 状態: 🔶 完全ブロック報酬は要設計
- 根拠: StS2のImbalancedPower（BowlbugRock: 自分の攻撃が完全ブロックされるとスタンして1ターン様子見）とPlowPower（CeremonialBeast: 蓄積を削り切られるとスタン+第2形態）。powers.md §2-b / monsters-a-l.md BowlbugRock・CeremonialBeast
- 現状(採掘時点): 撃破サマリーは「完全に凌いだ回数」を既に数えている (engine/summary.ts) が、凌ぎ切っても敵側には何も起きない＝守りの読み勝ちの報酬は被弾0のみ。敵のスタン機構はエンジンに無い（'rest' は敵が自発的に選ぶ隙だけ。grepで確認）
- 実装スケッチ: ①types.ts: EnemyDef.stunOnFullBlock?: true。②combat.ts: attack行動の解決後、hpLoss===0 かつ negate でなくブロック/氷壁で受け切った場合、その敵の次の宣言を強制 'rest'（意図「🌀体勢を崩している」）に。実装は stolenGold の強制逃走と同じ declareIntents 冒頭の差し替え分岐+EnemyState.staggered?: boolean フラグ。③チップ「⚖️猪突: 攻撃を完全に受け切ると次のターン隙を見せる」常時表示（フェアネス）。④使い先: 幕1新敵「猪突の大兎」HP60: 突進10-14⇄跳ね回り6-8×2。pre窓の軽減リアクション・要塞・氷壁に攻めの報酬を与える＝set-confirmの読み勝ちを敵側の隙に換金する初の機構で、幕1の「守る価値の教師」ポジション

## [58] (gimmick-variants) 飛行 — 被弾で墜落するダメージ半減（多段デッキへのご褒美マッチアップ）
- 状態: 📋 M
- 根拠: StS2のFlutterPower（被弾ダメージ50%カット・被弾のたびカウンタ-1・0でスタン=飛行→墜落）とSoarPower（OwlMagistrate: 浮上→急降下の2段構え）。powers.md §2-b FlutterPower/SoarPower
- 現状(採掘時点): 被ダメ軽減の敵機構は armor（1ヒットの上限キャップ。types.ts L865）のみ。ヒット『回数』を敵を崩す資源として数える機構は無い。多段ヒットへの敵側の応答は現状すべて罰（とげ・装甲）で、多段が最短解になる敵がいない
- 実装スケッチ: ①types.ts: EnemyDef.flying?: number → EnemyState.flightCharges。②combat.ts dealDamageToEnemy: flightCharges>0 の間、各ヒットのダメージを半減（切り捨て・armor適用より前）しヒットごとに-1。0になった瞬間 staggered を立て次宣言を 'rest'（「🪶墜落」）にし、以降この戦闘では飛ばない。延焼ティックはヒットでないので半減もカウント減も無し（armorと同じ裁定＝バーンは別解）。③チップ「🪶飛行N: 受けるダメージ半減。N回当てれば墜落」。④使い先: 幕2新敵「風切りの大鳶」HP70・flying3: 急降下14-18⇄羽ばたき3×3。とげ（多段の罰）と対になる「多段のご褒美」で、緑の多段・黒の骨刃・赤の手数に見せ場を作る

## [59] (gimmick-variants) 弔い強化 — 仲間が倒れるたび筋力+N（連携の逆問い）
- 状態: ✅ 66c83e8 弔い強化(弔いの獣)
- 根拠: StS2のCrabRagePower（味方が死ぬとStrength+ブロックの弔い強化）・RavenousPower（CorpseSlug: 味方が死ぬと捕食演出後にStrength獲得）・Queen（随伴TorchHeadAmalgamの死亡を検知した瞬間に支援ループから激怒ループへ強制切替）。powers.md §2-b / monsters-m-z.md Queen
- 現状(採掘時点): 仲間の死亡をトリガーにする敵機構はゼロ（死亡トリガーは splitInto のみ、grepで確認）。bondStrength (types.ts L857) は「生存中+N」＝先に殺せば弱くなる一方向の問いで、キル順の逆側（殺すと強くなる）が無い
- 実装スケッチ: ①types.ts: EnemyDef.mournStrength?: number。②combat.ts: 敵の死亡が確定する点（checkCombatEnd 冒頭の走査 or dealDamageToEnemy の死亡処理）で、生存する mournStrength 持ちに strength+N を emit（StrengthGained に reason:'mourn' を追加）。逃走(fled)では発火しない。③チップ「🕯️弔いN: 仲間が倒れるたび筋力+N」常時表示。④使い先: 幕3新編成「弔いの獣二匹」＝双牙の狼の対概念: HP95×2・mournStrength4・噛み2連と大跳びのローテずらし。連携（早く1体目を殺したい）と弔い（殺すと残りが+4）を別編成で出すことで、キル順の教科書が「早く割れ」と「同時に削って同時に落とせ」の2冊になる。全体攻撃=同時撃破が構造的な最適解＝赤緑の全体札の出番

## [60] (gimmick-variants) 眠りの被弾覚醒 — 鉄卵に「傷つけると目を覚ます」を追加
- 状態: 🔶 鉄卵の挙動変更=校正直後
- 根拠: StS2のAsleepPower（LagavulinMatriarch: 実ダメージを受けた瞬間に覚醒して行動を差し込む。ターン経過でも自動覚醒）とSlumberPower（SlumberingBeetle: 被弾するたびカウンタが減り早く目覚める。起こす前に削ればPlatingPower=装甲を剥がして殴れる読み合い）。powers.md §2-b / monsters-a-l.md LagavulinMatriarch / monsters-m-z.md SlumberingBeetle
- 現状(採掘時点): 眠れる鉄卵 (enemies.json enemy_elite_iron_egg) の眠りは固定sequence（sleep=防御14-18→awaken→tail×8）で、攻撃してもしなくても3手目に起きる＝プレイヤーの行動が覚醒タイミングに一切影響しない。「起こす前に削るか」の二択が実際には存在しない（削っても代償ゼロ）
- 実装スケッチ: ①types.ts: EnemyDef.sleepUntilIndex?: number（patternIndex がこの値未満の間は眠り状態。この間にHP損失を受けたら次宣言で patternIndex をこの値へ即ジャンプ=早期覚醒）。②鉄卵に sleepUntilIndex:2 を設定（sleep,sleep,awaken,tail…に組み替え）: 放置すれば眠りながら殻を2回積む（現行の放置コスト維持）が、殴れば防御を1回スキップさせられる代わりに1ターン早く尾が飛んでくる。③チップ「💤浅い眠り: 傷つけると目を覚ます」。装甲22と合わせて「多段でつつくと起きる/大技一発で起こして正面から」の分岐が本物になる

## [61] (gimmick-variants) sequenceLoopFrom — 「一度きりの前奏→ループ」を1フィールドで書けるようにする
- 状態: ✅ 66c83e8 sequenceLoopFrom
- 根拠: StS2の頻出パターン: DampCultist/CalcifiedCultist/DevotedSculptor（INCANTATION=儀式を開始時1回だけ→以降は攻撃の自己ループ）・ShrinkerBeetle（初手デバフ1回のみ）・KnowledgeDemon（呪い付与は3回で打ち止め→以降SLAP起点ループ）・ThievingHopper（盗み→逃走の一方通行でループしない）。monsters-a-l.md 横断補足2「フェーズ変化の実装方式が多様」
- 現状(採掘時点): combat.ts L321 は sequence[patternIndex % sequence.length] で配列全体を丸ごとループする。「開始時だけの行動」は書けず、眠れる鉄卵は tail×8 を並べて疑似的に引き延ばしている（enemies.json で確認。11ターン目には再び眠る）。一方通行・前奏1回・N回で打ち止めのパターンは現行データ形式で表現不能
- 実装スケッチ: types.ts: EnemyDef.sequenceLoopFrom?: number を追加し、combat.ts の参照を idx >= len ? loopFrom + (idx - loopFrom) % (len - loopFrom) : idx に変更（未指定は現行どおり 0 = 完全後方互換）。これだけで①儀式1回→永久攻撃（囁きの狂信者の変種: 大詠唱で強化+4を初手1回だけ→以降は殴り）②鉄卵の tail×8 を sleep,awaken,tail+loopFrom:2 に短縮 ③盗み→逃走の一方通行 ④「呪いはN回で打ち止め」ボスの全パターンが書ける。今後の敵拡充全体の表現力を上げる基盤投資。sequenceBelowHalf にも同名フィールドを対で追加

## [62] (gimmick-variants) 虚脱札 — ドローした瞬間エナジー-1で自壊する状態異常（ドローエンジンへの問い）
- 状態: 📋 M
- 根拠: StS2のVoidステータス（Unplayable+Ethereal・ドローされた瞬間にエナジーを1失う=手札に来るだけで損）。powers.md §3-a。NoDrawPower/MindRotPower等「ドロー資源そのものを攻める」のがStS2デバフ群の新機軸（§1-b・§5まとめ5）
- 現状(採掘時点): PlayerStatus は weak/vulnerable/frail/wound/junk/scald の6種 (types.ts L744)。注入先は捨て札(負傷)・山札ランダム(がらくた L1193-1214)・手札(火傷)と揃うが、いずれも「引いても素通り or 捨てれば終わり」でドローそのものは無税。青の抱え込み・賢者の泉・黒の0E契約などドロー枚数が回転の本体のデッキに刺さる敵デバフが存在しない
- 実装スケッチ: ①types.ts: PlayerStatus に 'void'（表示名: 虚脱）。②combat.ts: 付与は junk と同じ山札ランダム位置注入（上限3/戦闘）。drawCards() にフック: 虚脱札を引いた瞬間 energy を-1（下限0）して札は完全除去（消滅置き場に入らない=onSelfExhausted/onCardExhausted は発火しないと明記→黒の亡骸経済に不労所得を与えない）。イベント VoidDrawn を追加しUI浮き数字対応。③意図に「🕳️虚脱N枚」予告（フェアネス）。④使い手: 幕3新敵「虚ろの語り部」hexer HP85: 囁き(hex: void2)→爪10-14→睨み(弱体2)のローテ。ドローで回るデッキほど税が重い=マッチアップの色。引かずに温存はできない（山札に沈んでいる）ので「早く掘って毒抜きするか、回転を落とすか」

## [63] (gimmick-variants) 成長吸い — プレイヤーの成長を奪い、倒せば返す（憑依型）
- 状態: 🔶 成長吸いは緑への実質ナーフ成分
- 根拠: StS2のLagavulinMatriarch SOUL_SIPHON（全対象のStrength/Dexterity-2を奪い自身+2）とPossessStrengthPower/PossessSpeedPower（奪った量を記憶し、自分の死亡時にプレイヤーへ丸ごと返す=憑依の解放）。monsters-a-l.md / powers.md §2-b
- 現状(採掘時点): プレイヤーの成長 (player.growth, types.ts L74) に干渉する敵は存在しない（敵の干渉手段は状態異常6種とミルのみ）。緑の成長スタックは一度積めば戦闘終了まで無風の安全資産で、積み時を問う敵がいない
- 実装スケッチ: ①types.ts: EnemyActionKind 'siphon'、EnemyState.stolenGrowth?: number。②combat.ts: 解決時に min..max（例2固定）ぶんプレイヤーの growth を奪い（下限0クランプ・実際に奪えた量だけ）自分の strength に加算、stolenGrowth に累積。この敵を倒すと stolenGrowth をプレイヤーの growth に全額返す（死亡確定点で処理。逃走なし個体に限定）。打ち消し可・意図「🫳成長吸い2」。③使い先: 幕3新敵「憑き纏いの霊」HP95: siphon2⇄爪13-16の2拍子。「積んでから殴られて奪われる」を体験させ、成長の売り時（放出・刈り取り札）に敵側から締切を作る。倒せば全額戻る=理不尽ではなくレース

## [64] (gimmick-variants) ターン装甲（硬化した殻）— 1ターンの被ダメ累計キャップ（バーストへの問い）
- 状態: 📋 M
- 根拠: StS2のHardenedShellPower（1ターンあたりの被ダメ累計に上限を設け超過分をカット・ターン開始でリセット）。powers.md §2-b。per-hit上限のHardToKillPower（=うちのarmor相当と明記されている）と対になる設計
- 現状(採掘時点): armor は1ヒットあたりの上限のみ (types.ts L865・多段には「ヒット数で押し切れ」の問い)。1ターン累計のキャップは無く、収穫型・全知の一撃・Xコスト大技などの「1枚で全部出す」バーストに問いを出す敵構造が存在しない（装甲は多段より一撃に甘い）
- 実装スケッチ: ①types.ts: EnemyDef.turnArmor?: number → EnemyState に turnDamageTaken を持ちターン開始でリセット。②combat.ts dealDamageToEnemy: このターンの累計HP損失が turnArmor を超える分をカット（armor の後に適用）。延焼ティックは対象外（armor裁定踏襲=バーンが解答）。③チップ「🐚硬殻N: 1ターンにNより多くは通らない」常時表示。④設計規約をテストで固定: turnArmor 持ちは低HP（40以下）かつタイマー系行動（alsoBuff か enrage）併用を必須にし、スタール（削り切れない膠着）を構造的に防ぐ。使い先: 幕2新敵「殻籠りの巻貝」HP45・turnArmor12: 噛み8-10+alsoBuff1⇄殻打ち6-8。「2枚に分けて2ターンで抜く」計画性=バースト特化への自制の問い

## [65] (gimmick-variants) 明滅（無敵サイクル）— 1ターンおきに実体を失う亡霊
- 状態: 📋 M
- 根拠: StS2のNemesisPower（自ターン終了ごとにIntangibleの付与/剥奪を交互に繰り返す=1ターンおきに無敵化）とSoulFysh FADE_MOVE（Intangible2で2ターン実質無敵→無敵ターンを避けて攻撃を集中するタイミングパズル、と解析済み）。powers.md §2-b / monsters-m-z.md SoulFysh
- 現状(採掘時点): 無敵・被ダメ1キャップの機構はエンジンに無い（grepで確認。armor の最小値運用も未使用）。「今ターン吐くか溜めるか」の売り時パズルを敵側から作る構造は蒸気圧含め未実装
- 実装スケッチ: ①types.ts: EnemyDef.flicker?: true → 奇数戦闘ターンは実体・偶数ターンは非実体（受けるヒットダメージを1にキャップ。ターン番号基準なので決定的・宣言時に次状態を予告できる）。延焼ティックは素通し（armor裁定踏襲=バーンとDoTが解答）。②チップ「👻明滅: 次のターンは実体がない/ある」を毎ターン更新表示（フェアネス=タイミングパズルを公開情報にする）。③使い先: 幕3新敵「明滅する亡霊」HP90: 爪12-15⇄薄れ流し6-9の2拍子。反復・衝動・溜め置物など「1ターンに寄せる」手段の売り時が敵都合で半分に制限される=計画の問い。パーミッションは打ち消しで実体ターンの攻撃だけ消せばよい=色ごとに別解

## [66] (gimmick-variants) 群れの初動ずらしの体系化 — patternOffsetの自動割当（同型複数体の位相分散）
- 状態: 📋 S
- 根拠: StS2の横断パターン（monsters-a-l.md 補足4「群れ内での個体差別化」）: CorpseSlug の EnsureCorpseSlugsStartWithDifferentMoves（同時出現個体が互いに異なる開始Moveを取るようRNG割当）・Exoskeleton のスロット位置別初手・Wriggler のスロット名分岐。同一モンスター複数体の単調な同時攻撃を意図的に避けている
- 現状(採掘時点): patternOffset は EncounterMember の手書きフィールド (types.ts L878) で、encounters.json の各編成に人力で 1,2 を振っている（enc_probe_trio 等で確認）。振り忘れると同型ローテーションが同期し大技が同時に飛ぶ（設計コメントに「同期スパイク防止」とある通り既知のリスクだが、機械保証が無い）
- 実装スケッチ: ①combat.ts の戦闘構築（L169近辺の patternIndex 初期化）で、同一 enemyId が複数体いて patternOffset が全員未指定の場合に限り、index順に 0,1,2… を自動付与（明示指定があれば常に優先=完全後方互換・決定的でシードすら不要）。②enemies.test.ts に「同一defのsequence持ちを2体以上並べた編成は初期patternIndexが全員異なる」の機械固定を追加。手書き漏れの事故クラスを構造的に消し、今後のペア編成拡充（分裂体・増援・残機チェーンで同型複数体が増える）の前提整備になる

## [67] (enemy-numbers) 幕1のHP帯を引き上げ（depthHpScale 0.55/0.65→0.62/0.72・打点は据え置き）
- 状態: ✅ 742b5fe 幕1 HP 0.62/0.72
- 根拠: StS2実測（docs/sts2-reference.md §2）: 幕1 Normal上端はHP74（Fogmog）・打点4〜16。§8の示唆1が名指しで「幕1増強は打点でなくHP上端で」と結論。うちの幕1実効上端は58.5で本家より約21%低い。打点は本家同水準なので触らない。
- 現状(採掘時点): src/engine/run.ts depthHpScale() の table が [0.55, 0.65]（幕1）。実効HP: 探り屋75→41/49・うねる獣90→49.5/58.5・見習い巨像70→38.5/45.5。2026-08-31/09-01 の引き上げは幕2/3のみ（+0.10と+0.15）で幕1は未着手。
- 実装スケッチ: src/engine/run.ts depthHpScale の幕1行を [0.55, 0.65] → [0.62, 0.72] に変更（+13%・実効上端 58.5→64.8）。エリート・ボスは素通り（elite分岐とBOSS_ROW=1.0で自動除外）なので影響は幕1通常戦のみ。あわせてCLAUDE.md「敵の数値基準」行の帯表を実測で更新（幕1 HP43〜65。幕2/3の記述60-80/90-130も現行実測63〜104/108〜169と乖離しているので同時に直す）。ガード: 幕1は焚き火前のHP予算が最大HP分しかないため、1段でこれ以上は上げず人間ランで再測してから次を判断。

## [68] (enemy-numbers) 幕1に帯上端ソロ「硬いが遅い」個体を新設（Fogmog枠・実効HP65〜72）
- 状態: ✅ 742b5fe 泥まとうもの(タンク教師)
- 根拠: StS2 幕1 Normal上端のFogmog（HP74・打点控えめ）は「硬い的で長期戦の練習をさせる」枠。§2の結論「本家は硬いが1ターンの危険は控えめ、質の圧を毎ターン積む」。うちの幕1プール12種に実効HP60超のソロが1体もいない（獣58.5が最大）＝スケール一律引き上げだけでは上端の「体格の問い」が出せない。
- 現状(採掘時点): src/engine/map.ts ACT_POOLS[0] の12種を全数確認: ソロ7種の基本HPは42〜90（実効23〜58.5）。防御・再生などで長引かせる「タンク型の教師」は幕1に不在（石殻・苔の主は幕3）。
- 実装スケッチ: src/data/enemies.json に幕1ソロ1体を追加（例: 「泥まとうもの」maxHp 105〔実効65/75.6〕・打点4〜7の弱攻撃＋防御8〜11のローテ・inflictで虚弱1か弱体1を低頻度で混ぜる=質の圧）。ACT_POOLS[0] に追加。狙い: 幕1で初めて「3〜4ターンかけて削る戦闘」を教え、ドロー/エンジン札の価値を序盤に見せる。打点を盛らないので幕1のHP予算は侵さない。

## [69] (enemy-numbers) 幕2に帯上端ソロを新設（HunterKiller/LouseProgenitor枠・実効HP120〜135）
- 状態: 📋 M
- 根拠: StS2 幕2 Normalの上端はHunterKiller 121・LouseProgenitor 134-136。うちの幕2実効上端103.5（罠壊し）は本家より24%低く、「幕2の谷＝敵が2ターンで溶けタイマー・デバフが鳴る前に死ぬ」（2026-09-01 段3新ベース検証の実測）のHP側の受けがまだ足りない。
- 現状(採掘時点): src/engine/map.ts ACT_POOLS[1] のソロは set_wary 85・set_breaker 90・bomber 85・砥石78・道化鏡68・インプ62・甲虫30・大スライム72（分裂込み124）。基本HP90超はゼロ。depthHpScale 1.05/1.15 を掛けても最大103.5。
- 実装スケッチ: src/data/enemies.json に幕2ソロ1体を追加（例: 「鎧殻の狩り手」maxHp 110〔実効115.5/126.5〕・攻撃10〜14+alsoDefend 6 の攻防一体×2→大技18〜22 の3拍ローテ・初撃に脆弱1）。ACT_POOLS[1] に追加。既存個体の基本HPを上げる案（罠壊し90→100等）も可能だが、罠壊しは幕3プールと共有のため新設の方が副作用がない。

## [70] (enemy-numbers) 幕3に帯上端ソロを新設（SlimedBerserker枠・実効HP230前後・札汚染で殴る大物）
- 状態: 📋 M
- 根拠: StS2 幕3 NormalのSlimedBerserker 266は「通常戦なのにボス級の体格＋Slimed10枚注入」の名物枠。幕3 Normal帯40〜266に対しうちの上端169（オーガ）は37%低い。§2の結論「硬さ＋質の圧（カード汚染）」の組み合わせがうちの幕3通常には無い。
- 現状(採掘時点): src/engine/map.ts ACT_POOLS[2] のソロ4種: brute130・moss115・axe_ogre110・shell_guard100（実効120〜169）。がらくた/負傷を注入する通常敵は罠壊し（junk1/回）と妖術師（wound2）のみで、量が本家の1/5以下。
- 実装スケッチ: src/data/enemies.json に幕3ソロ1体を追加（例: 「汚泥の狂戦士」maxHp 185〔実効222/240.5〕・攻撃14〜18にinflict junk 2 を毎回付ける×2→防御10〜14 の3拍。がらくた上限4/戦の既存ガードがハメを防ぐ）。ACT_POOLS[2] に追加。「削り切るまでにデッキが汚れていく」＝完成デッキへの自動スケール圧（割合デバフと同じ設計思想）で、幕3後半の被ダメ0戦闘（2026-09-01 再走実測: 幕3通常5連続被ダメ0〜5）への受けになる。

## [71] (enemy-numbers) 幕1ボス係数 ×1.25→×1.35（オーガ実効162.5→175.5＝本家最弱ボス水準）
- 状態: ✅ 742b5fe 幕1ボス×1.35
- 根拠: StS2 幕1ボス帯は173〜252（最弱Vantom 173）。うちの幕1ボス（オーガ130×1.25=162.5）は本家最弱を6%下回る。2026-08-29の×1.0→×1.25はユーザー体感「1幕ボスが弱くひりつきが薄い」への処方で、同方向の半歩。
- 現状(採掘時点): src/engine/run.ts launchCombat() の boss倍率配列 [1.25, 1.6, 2.4]。オーガはarmor25持ちだが強化+1・打点9〜13は本家帯の下端。
- 実装スケッチ: src/engine/run.ts の [1.25, 1.6, 2.4] → [1.35, 1.6, 2.4]（幕1のみ）。実効175.5で本家最弱ボス173に一致。打点・強化は触らない（HPだけ＝戦闘が半〜1ターン延びて激昂・第2形態が見える確率が上がる）。

## [72] (enemy-numbers) 幕2/3ボス係数の引き上げ（×1.6→×1.9・×2.4→×2.6）
- 状態: 🔶 幕2/3ボスは校正済み裁定
- 根拠: StS2 幕2ボス帯321〜408に対しうちの大亀224は最弱比でも30%低い（本家との差が全帯で最大）。幕3も門番372対本家上端489。ボスは「山場なので長くてよい」が確定方針であり、HP増は路線と整合する。
- 現状(採掘時点): src/engine/run.ts の boss倍率 [1.25, 1.6, 2.4]。大亀140×1.6=224+開幕ブロック10+装甲30、門番155×2.4=372+開幕ブロック15+装甲35。CLAUDE.mdに「幕2/3は3幕走破ランで校正済みのため据え置き」の裁定が明記されている。
- 実装スケッチ: boss倍率を [1.35, 1.9, 2.6] へ（幕2: 大亀266・幕3: 門番403）。幕2は本家最弱321にまだ届かないが、大亀は防御サイクル+装甲30で実効HPが印字より高いため一気に寄せない。段階案: まず幕2のみ×1.9にして幕3は据え置き→人間ランで幕3の消化試合感が残るなら×2.6。

## [73] (enemy-numbers) 幕2/3エリートのHP引き上げ（ギミックが1周する前に死ぬ帯の是正・7体）
- 状態: ✅ 742b5fe エリート7体HP引き上げ
- 根拠: StS2エリート帯: 幕2〜200（InfestedPrism）・幕3 93〜300（MechaKnight 300）。うちの上端は幕2=112・幕3=140で、本家の56〜47%。エリートは固有ギミックが「問い」なのに、幕2/3の育ったデッキ（ターン火力40〜66実測）だと2〜3ターンで死に、3拍ローテや時限歌が1周も回らない＝ギミックの露出不足はHP不足が原因。
- 現状(採掘時点): src/data/enemies.json: 奴隷商95・梟95・魔人96（幕2）／刺突の書130・巨面140・唱い手125・不滅の騎士120（幕3）。エリートはdepthHpScale対象外（素の値）を機械確認済み（run.ts:309-314）。
- 実装スケッチ: src/data/enemies.json で7体を引き上げ: 奴隷商95→120（chain→net→lashが2周＝デバフ漬けの名の実体化）・梟95→110（talon_diveの伏せ読みを最低2回見せる）・魔人96→110・刺突の書130→165（stab4まで必ず到達）・巨面140→175（crushを2回脅かす）・唱い手125→150（枚数タイマーが実際に鳴る）・不滅の騎士120→135。据え置き: 大鴉58と蟲70（DPSレースの数学が校正済み・HP増はレース破壊）・鉄卵112（眠り=防御化で校正直後）・鬼軍曹82（「唯一プランを変えさせた敵」の手本評価を崩さない）。

## [74] (enemy-numbers) 歩哨を双子→三つ子化（42×2=84 → 42×3=126・本家Sentries対照）
- 状態: ✅ 742b5fe 歩哨三つ子
- 根拠: StS2/StS1のSentriesは3体構成（合計約120）。うちの幕1エリート上端は双子の84で、本家幕1エリート帯61〜140の下半分に全員が沈んでいる。3体化は数値でなく頭数＝「潰す順」の問いが立体化する（ボルト→ビームの位相が3つずれる）。
- 現状(採掘時点): src/data/encounters.json enc_elite_sentries はメンバー2体（patternOffset 0/1）。歩哨単体はHP42・bolt（junk2付き）→beamの2拍。がらくたは1戦闘上限4枚のガードが combat 側に実装済み。
- 実装スケッチ: src/data/encounters.json の enc_elite_sentries に3体目 {enemyId: enemy_elite_sentry, patternOffset: 2} を追加。合計HP126＝本家帯の中央。がらくた注入は既存の上限4/戦で頭打ちになるため理不尽化しない（3体でも上限は同じ＝圧は主に頭数分の打点13〜16/Tに移る）。幕1エリートとしては重いので、勝てば報酬レア確定の既存契約と釣り合う。

## [75] (enemy-numbers) 群れ編成のhpScale引き上げ＋幕3共有編成の専用版複製（strengthペナルティ緩和込み）
- 状態: 📋 M
- 根拠: StS2の群れは合計HPが濃い（Decimillipede 40-46×3≈130・Knights 93/97/101≈291）。うちの幕2群れ合計は実効90〜103・幕3で135〜156と、本家の同幕ソロ上端にすら届かない。また幕2用に作った編成（enc_hexer_shadow等）を幕3プールが使い回しており、hpScale・strength補正が幕2基準のまま幕3に出ている。
- 現状(採掘時点): src/data/encounters.json: enc_probe_trio hpScale0.38（合計実効90）・enc_squire_archer 0.75/0.75（実効91）・enc_fang_twins 0.55/0.55（実効139）。enc_hexer_shadow / enc_breaker_hexer は map.ts の幕2・幕3両プールに登場（同一定義）。幕3編成のstrength補正は-2〜-3のまま。
- 実装スケッチ: ①enc_probe_trio 0.38→0.45（合計実効106）②enc_squire_archer 0.75→0.85（実効103・陣形ギミックの露出延長）③enc_fang_twins 0.55→0.62（実効156・連携+2が2周乗る）④enc_hexer_shadow・enc_breaker_hexer の幕3専用版（例: enc_hexer_shadow_act3）を複製し hpScale+0.1・strength を各+1（-3→-2, -2→-1）して ACT_POOLS[2] の参照を差し替え——幕2版は不変なので幕2の帯を汚さない。⑤幕3残りの編成（wolf_drummer・axe_drummer・shell_hexer・moss_healer・wolf_pair）も strength を各+1（打点+2/行動・引き上げ方向のみ）。

## [76] (enemy-numbers) 静的性質（開幕ブロック）の配布拡大——巨像2体・用心深い影・刺突の書
- 状態: ✅ 742b5fe 静的性質の配布拡大
- 根拠: StS2は「ほぼ全敵が開幕パワー持ち」（sts2-reference §3-5: AfterAddedToRoomでの付与率ほぼ100%・素のHP/打点でなく常在パワーが難易度設計の主軸）。うちの静的性質持ちは42定義中19で半分未満。3幕フルランのOpus診断「開幕から見えている静的な性質だけが1ターン目から問いを出せる」とも同方向。
- 現状(採掘時点): src/data/enemies.json で startingBlock 持ちは7体（大亀10・門番15・樽12・石殻10・甲虫12・大鴉6・従士6）。見習い巨像・砥石の巨像・用心深い影・刺突の書は素のHPのみ（grep確認済み）。
- 実装スケッチ: src/data/enemies.json に追加: 見習い巨像 startingBlock 8（石の体・「構え」の型のフレーバー通り）／砥石の巨像 startingBlock 10（3拍タイマーの1拍目を守り切らせる＝「数えられる死」の完走率向上）／用心深い影 startingBlock 6（様子見の構え）／刺突の書 startingBlock 8（表紙の装丁）。いずれも貫通・粉砕・延焼の解答が既にプレイヤー全色にあり、T1から問いになる。装甲（armor）は「ボスと甲殻系のみ」の裁定があるため今回は使わない。

## [77] (enemy-numbers) 巨面にarmor 20を追加（幕3エリートの実効体格をHP以外で補う）
- 状態: 🔶 巨面の装甲は「ボスと甲殻のみ」裁定と衝突
- 根拠: StS2幕3エリート上端300に対し巨面140は半分以下。HP引き上げ（別提案）に加え、二拍子の「睨み→潰し」を多段デッキが1ターンで飛ばせてしまう現状に対し、装甲は「石の巨面」のフレーバーと合致し、ヒット数で押すか一撃で抜くかの問いを足せる。
- 現状(採掘時点): src/data/enemies.json enemy_elite_giant_face: maxHp140・moves glare(buff2-3)→crush(30-34)のみ。armor・startingBlockなし。armor配布は現在オーガ25・大亀30・門番35・石殻18・鉄卵22・不滅20の6体。
- 実装スケッチ: enemy_elite_giant_face に "armor": 20 を追加。1ヒット20超のダメージが頭打ちになり、一撃特化には「2ターンかけて2回削る」を、多段には「ヒット数で押し切る」を要求する（既存の装甲仕様のまま・表示も既存の常時表示に自動で乗る）。

## [78] (enemy-numbers) 帯設計方針の明文化——「幕1増強はHP・幕2/3増強は上端個体・打点はこれ以上触らない」
- 状態: 📋 S
- 根拠: sts2-reference §2の結論: 本家は「HPの上端が高く打点は同水準・質の圧を毎ターン積む」。うちは2026-09-01に打点側（+15%・難易度×3.0ラダー）を先に伸ばしており、これ以上打点を盛ると「難しい」でなく「理不尽」に寄る（難易度検証3本の一致所見と同根）。本家との整合点をルール表に固定しないと、次の「敵が弱い」フィードバックのたびに打点つまみに手が伸びる。
- 現状(採掘時点): CLAUDE.md「敵の数値基準」行は『打点帯は据え置きでHPだけ削減＝同じ危険を短く濃く』（2026-08-29テンポ再校正）のまま。その後の引き上げ（幕2/3 HP+0.15・打点+15%）との関係が未整理で、幕1の増強手段も未記載。
- 実装スケッチ: CLAUDE.md「敵の数値基準」行に追記: 『幕1の増強はHP側で行い打点は据え置き（本家幕1上端74対照）。幕2/3の今後の増強は帯上端個体の追加（Fogmog/HunterKiller/SlimedBerserker枠）で行い、一律倍率と打点には触らない。短く濃く路線は通常戦の中央値に適用し、帯の上端2割は本家式「硬い+質の圧」を置く』。実装変更なしのドキュメント裁定。

## [79] (enemy-numbers) ボス随伴（KinPriest型: 本体+子×2）の導入検討
- 状態: 🔶 ボス随伴は大型
- 根拠: StS2幕1ボス3体中1体は随伴付き（KinPriest 190+子58×2=合計306）で、ボス帯の体格を「単体HP」でなく「合計HP+潰す順の問い」で作っている。うちのボス3体は全員ソロで、体格差を埋める手段が倍率しかない。応援役・回復役・庇う等の複数体ギミックが既に揃っており、ボス戦だけがそれを使えていない。
- 現状(採掘時点): CLAUDE.md「戦闘形式」行に『ボスは単体』が明記。src/engine/map.ts ACT_BOSSES はソロ敵ID3つ（enemy_brute/enemy_turtle/enemy_warden）で、tierForはボス行に1体のみ返す。encounters.jsonにボス編成は存在しない。
- 実装スケッチ: 第4のボス候補として『祭司型ボス編成』を encounters.json に新設（例: 本体HP170・毎ターンrally+1の随伴HP55×2）し、幕2の代替ボスとしてローテーション投入するか、まず単発検証モードで人間プレイ検証。ACT_BOSSESを編成ID対応にする改修が必要（tierForの返り値は既に編成IDを扱える）。倍率でなく頭数でボスの体格を作る本家式の口を開ける。

## [80] (fairness-intent-ui) 致死予告（DeathBlow相当）: 単体で死にうる攻撃意図に💀マークと赤強調
- 状態: ✅ 75515b2 💀致死級予告
- 根拠: StS2は意図タイプ15種のうち DeathBlow=致死級大技の専用予告アイコンを持ち「予告してから殺す」を制度化している（docs/sts2-reference.md §5）
- 現状(採掘時点): App.tsx 1858-1877 の最悪被ダメ予測パネルは合計が現HP以上なら forecast-danger クラスで強調するが、複数体戦で「どの敵の一撃が致死か」は表示されない。intentText（ui/log.ts 21-42）にも致死マークは無い
- 実装スケッチ: App.tsx の conditionalIntentText 呼び出し側で、その敵単体の脅威 = shownMax×hits（mirrorHitsは現在プレイ枚数）×（脆弱中は1.5切り捨て）が player.hp + block + iceBlock 以上なら意図行の先頭に「💀致死級」を付与し CSS で赤強調（.intent-lethal 新設）。sim/play.ts の意図表示（199行付近）にも同判定を追加。表示層のみで engine 変更なし

## [81] (fairness-intent-ui) 混乱した敵の攻撃を最悪被ダメ予測から除外し、意図に「仲間に向かう」を注記
- 状態: ✅ 75515b2 混乱除外+仲間に向かう注記
- 根拠: StS2の意図は「実際に起きること」を正確に表示する（§5 実値表示・補正込み）。うちの混乱は攻撃が丸ごと他の敵へ向かうのに、予測が嘘の過大値を出している
- 現状(採掘時点): combat.ts 1271-1298: confusion>0 の敵の攻撃は全ヒットが他の生存敵（いなければ自分）へ向かいプレイヤーは無傷。しかし App.tsx 1861-1867 の最悪被ダメ予測と sim/play.ts 259 の worst 計算は confusion を一切見ず、混乱敵の攻撃もプレイヤー被ダメに合算している。intentText にも注記なし。混乱は青の派手枠（仲間割れ）なのに、その成果が予測数値に反映されない
- 実装スケッチ: App.tsx の forecast ループと sim/play.ts の worst 計算で `e.confusion > 0` の敵の attack をスキップ（混乱は行動1回で1減なのでこのフェーズの攻撃は必ず逸れる）。あわせて conditionalIntentText / play.ts の意図行に confusion>0 なら「😵‍💫仲間に向かう」サフィックスを追加

## [82] (fairness-intent-ui) 脆弱中は意図表示の幅にも補正込みの値を併記する
- 状態: ✅ 予測には脆弱算入済み(既存)。意図幅への併記は見送り
- 根拠: 本家2作とも意図は弱体・脆弱の補正まで計算した正確な値を表示する（sts2-reference §5「実値表示（弱体・脆弱の補正まで計算した正確なダメージを表示）」）
- 現状(採掘時点): 最悪被ダメ予測（App.tsx 1865）は vulnerable×1.5 を適用するが、敵ごとの意図行（log.ts intentText 24-28）は素の shownMin〜shownMax のまま。脆弱中はパネルと意図行の数字が食い違い、プレイヤーが各敵の脅威を暗算し直す必要がある
- 実装スケッチ: intentText にオプション ctx（vulnerable: boolean）を追加し、attack のとき「⚔️ 攻撃 6〜12【脆弱中 9〜18】」の併記に。App.tsx conditionalIntentText と sim/play.ts branchText の両方から ctx を渡す（log.ts は UI/CLI/レポート共用なので1箇所の修正で全表示が揃う）。幅表示の原則は不変＝実値は公開しない

## [83] (fairness-intent-ui) カード汚染予告（CardDebuff相当）の専用アイコンと行き先明示
- 状態: ✅ 75515b2 行き先予告(手札へ/山札へ/捨て札へ)
- 根拠: StS2は CardDebuff=カード汚染の専用意図アイコンを持ち、ステータス低下（Debuff/DebuffStrong）と区別する（§5）。注入先（手札/捨て札/山札）の違いはStS2でも設計上の要点（§4: 手札滞留は今ターンの手数を奪う）
- 現状(採掘時点): log.ts inflictSuffix（16-19）は全状態異常を「＋負傷1」「＋火傷2」の同形テキストで出すだけ。負傷=捨て札・がらくた=山札・火傷=手札という行き先の違い（今ターンの脅威度が全く違う）は StatusInflicted の事後ログでしか分からない。1戦闘の上限（負傷5・がらくた4・火傷5）も戦闘中は非表示
- 実装スケッチ: inflictSuffix を状態のカテゴリで分岐: カード汚染系（wound/junk/scald）は「🃏負傷1→捨て札」「🃏火傷2→手札(終了時HP-2/枚)」の形式に、ステータス系（weak/vulnerable/frail）は現行の「＋弱体2」を維持。App.tsx の KEYWORD_HELP（169行）に負傷/がらくた/火傷の上限枚数を追記してチップのツールチップで読めるようにする

## [84] (fairness-intent-ui) 行動の表示ラベル（Sleep/Stun相当）: EnemyMove.label で意図に固有名を出す
- 状態: 📋 M
- 根拠: StS2は Sleep・Stun・Summon の専用意図タイプを持つ（§5）。眠りのフェーズは「起こすか削り続けるか」の問いそのものなのに、うちは汎用の防御アイコンに畳まれている
- 現状(採掘時点): 眠れる鉄卵（data/enemies.json enemy_elite_iron_egg）の 'sleep' は kind:'defend' なので意図表示は「🛡️ 防御 14〜18」＝眠っていることが flavor テキストでしか分からない。斧鬼の rest は「😮‍💨 隙だらけ」の専用表示があるのに、眠り・目覚め・大技チャージには器が無い。EnemyMove（types.ts 753-758）に表示名フィールドは存在しない
- 実装スケッチ: EnemyMove に `label?: string`（表示専用・機構不変）を追加し、buildIntent（combat.ts 393）で EnemyIntent に伝搬、intentText で kind 絵文字の後に「💤眠り（防御14〜18）」のように前置。第1弾データ: 鉄卵 sleep=「💤眠り」・awaken=「😳目覚め」、大亀のチャージ、巨面の睨み。図鑑・調整モードは文字列フィールドを触らないので影響なし。Unity移植も文字列1個の追加のみ

## [85] (fairness-intent-ui) 「×手数」の現在値をブラウザ意図表示にも出す（CLIとの格差解消）
- 状態: ✅ 75515b2 ×手数の現在値
- 根拠: CLAUDE.md ランの敵並び行は物真似の道化について「意図表示は『×手数(現在N)』で常時言語化」と記すが、実装されているのはCLIだけ。StS2の実値主義（§5）とも整合する精度改善
- 現状(採掘時点): sim/play.ts 192 は「×手数(あなたが今ターンプレイした枚数ぶん。現在N)」を表示するが、ブラウザ共用の log.ts intentText 25 は「×手数」のみで現在値なし。ルール表の記述と実装が食い違っている
- 実装スケッチ: 提案3の ctx 拡張に cardsPlayedThisTurn を追加し、intentText の mirrorHits 分岐を「×手数(現在N)」に。最悪被ダメ予測（App.tsx 1866）は既に現在値を使っているので、表示と予測の根拠が一致する

## [86] (fairness-intent-ui) 激昂タイマーの進捗カウンタ表示（あとN枚/あとNダメで鳴る）
- 状態: ✅ 75515b2 激昂の残りカウンタ
- 根拠: StS2の常在パワーはスタック数が常時見える（§3-5 ほぼ全敵が開幕パワー持ち＝数字が主役）。うちの激昂は「8枚ごと」のルールだけ見えて現在の針が見えず、2026-09-01検証ランでも「跨いだ瞬間を後から確認できない」処方がログ側にしか入っていない
- 現状(採掘時点): App.tsx 1670-1676 の激昂チップは「😡 激昂 +2/8枚プレイ・+2/被ダメ80」と規則のみ表示。進捗を導出できる player.cardsPlayedTotal（types.ts 82）と enemy.damageTakenTotal（136）は state に存在するのに未使用。プレイヤーは自分で数え続けるしかない
- 実装スケッチ: 激昂チップに進捗を追記: enrageEveryCards 持ちは「（あと{every - cardsPlayedTotal % every}枚）」、enrageEveryDamage 持ちは「（あと{every - damageTakenTotal % every}ダメ）」。判定と同じカウンタを読むので表示と発火が一致（combat.ts 229-248 と同源）。砥石の巨像等シーケンスタイマーは対象外（ローテは読みの領分）

## [87] (fairness-intent-ui) 威圧のマイナス筋力を可視化する（strength<0 でチップが消える穴）
- 状態: ✅ 75515b2 マイナス筋力可視化
- 根拠: StS2/StS1とも筋力はマイナス値も常時表示される。威圧は全色に配った基幹の解答札なのに、効果が画面から消えるのは投資の見返りが見えない情報欠陥
- 現状(採掘時点): App.tsx 1615-1617 は `enemy.strength > 0` のときだけ「💪 筋力 +N」チップを描画。威圧（weakenEnemy はマイナス可・攻撃最低1クランプ）で筋力が負になると何も表示されず、プレイヤーは自分の威圧が効いているか確認できない（意図の幅は下がるが、素の幅を記憶していないと比較不能）
- 実装スケッチ: 条件を `enemy.strength !== 0` にし、負のときは「🙇 筋力 {N}（威圧されている）」を別色チップ（chip-good系）で表示。KEYWORD_HELP に威圧の項を追加

## [88] (fairness-intent-ui) 盗み成立チップに「次のターン必ず逃走」を予告する
- 状態: ✅ 75515b2 盗み後の逃走予告
- 根拠: 「予告してから殺す」原則の盗み版。StS2のEscape意図は逃走を必ず1ターン前に見せる。うちは逃走保証ルール（2026-08-30制定）自体は良い設計だが、初見プレイヤーには次の宣言まで見えない
- 現状(採掘時点): combat.ts 272-286: stolenGold>0 の敵は次の宣言で必ず flee を宣言する（確定済みルール）。しかし App.tsx 1659-1661 のチップは「💰 15G 抱え込み」だけで、逃走が確定していることは次ターンの意図宣言まで分からない。「1ターン以内に倒せ」のレースは予告されて初めて成立する
- 実装スケッチ: チップ文言を「💰 15G 抱え込み（次のターン逃走する——先に倒せば取り返せる+懸賞金）」へ。sim/play.ts の敵表示にも同文言を追加。表示のみで engine 不変

## [89] (fairness-intent-ui) 最悪被ダメ予測を「今フェーズの脅威一覧」へ拡張（非ダメージ脅威+火傷疼きの合算）
- 状態: 📋 M
- 根拠: StS2は複数の意図を合成せず並べて表示し（§5）、質の圧（デバフ・カード汚染・ミル）を毎ターンの主戦場にしている（§2・§4）。うちの予測パネルは攻撃ダメージしか集計せず、質の圧が濃くなった現行環境（敵圧監査後）と釣り合っていない
- 現状(採掘時点): App.tsx 1858-1877 は kind==='attack' のみ合算。hex（弱体3等）・mill（山札喰い3枚）・steal-gold・destroy-set・inflict付き攻撃の付与内容は個別の意図行を目視で拾うしかない。また自ターン終了時の火傷疼き（手札の scald×2+烙印×1 の直接HP損失）は確実に起きるのに予測に入らない
- 実装スケッチ: 予測パネルを2行構成に: 1行目は現行の被ダメ合算＋手札の火傷・烙印枚数から「＋🔥疼き{2×scald枚+brand枚}」を加算（ScaldTickと同式）。2行目に非ダメージ脅威の合算「このフェーズ: 弱体2・山札喰い3枚・盗み15G・伏せ破壊」を生存敵の意図（effectiveIntent の inflict/kind）から列挙。sim/play.ts の worst 表示にも同等を追加

## [90] (fairness-intent-ui) 静かな鈴の軽減を最悪被ダメ予測に算入する
- 状態: ✅ 75515b2 静かな鈴を予測に算入
- 根拠: 本家の意図は所持レリック・パワーの補正を織り込んだ最終値を出す（§5）。軽減が予測に乗らないと「レリックが仕事をしている実感」が数値で見えない
- 現状(採掘時点): combat.ts 1315-1318: 静かな鈴（setDamageReduction）は実行時に各ヒット-1（最低1）するが、App.tsx の予測は shownMax をそのまま使うため、伏せ札を出している間は実際より過大な予測が出続ける（連撃敵ではヒット数ぶん乖離）。意図行にも軽減の注記なし
- 実装スケッチ: 予測ループで `s.setDamageReduction>0 && player.setCards.length>0` のとき perHit を `Math.max(1, perHit - setDamageReduction)` に（脆弱適用順は実処理と同じ「軽減→脆弱」に合わせる。combat.ts 1316-1320参照）。予測パネルに「🔔静かな鈴 -1/ヒット」の注記を出す

## [91] (fairness-intent-ui) 撃破サマリーに「死線を凌いだ回数」を追加
- 状態: 📋 M
- 根拠: StS2開発は「インゲームのフィードバックとテレメトリが調整の指針」と明言（§7）。死線（最悪被ダメ≥現HPのターン）は2026-09-01難易度検証で既に評価指標として使われており、これを「俺の戦いだった」の1行に昇格させると体験の山が数値で残る
- 現状(採掘時点): engine/summary.ts の BattleSummary は turns/totalDealt/bestTurnDealt/hpLost/reactionsFired/perfectBlocks/negates のみ。死線カウントは検証エージェントがログを手で読んで数えている状態で、UI/レポートには出ない
- 実装スケッチ: battleSummary に hpAtStart 引数を追加し、eventLog の畳み込みで各ターン開始時HPを復元（DamageDealt.hpLoss/HpLost/HpHealed/ScaldTick/ThornsReflected の逆算）。EnemyIntentDeclared の attack 意図合計（shownMax×hits）がそのターンのHP以上だったターンを「死線」と数え、summaryLine に「死線N回を凌いだ」を追加（脆弱の厳密復元は初版では省略と注記）。UI（App.tsx 1843）とCLIの表示は summaryLine 共用なので自動で揃う

## [92] (fairness-intent-ui) HP半分で豹変する敵に「😾 HP半分で牙をむく」を事前表示
- 状態: ✅ 75515b2 HP半分豹変の事前予告
- 根拠: うちの常時表示哲学（再生・とげ・装甲・分裂は事前に見せる=フェアネス）の一貫性。分裂（🫠倒すと〜×2）が事前表示なのに、同じ「あるラインを跨ぐと起きること」のフェーズ変化だけ事後表示なのは非対称
- 現状(採掘時点): App.tsx 1667-1669: movesBelowHalf/sequenceBelowHalf 持ちの敵は hp≤50% になって初めて「😾 牙をむいている」チップが出る。跨ぐ前は何の予告もなく、初見では「半分を切ったら行動が変わる」ことを知る手段が戦闘内に無い（図鑑の敵タブには行動テーブルとして載っている＝情報自体は既に公開済み）
- 実装スケッチ: hp>50% かつ !dead のとき「😾 {kw('豹変')} HP半分で行動が変わる」チップを薄い色で表示し、跨いだら現行の「牙をむいている」へ差し替え。KEYWORD_HELP に豹変の説明を追加。表示のみ・具体的な変化内容は見せない（読む楽しみは残す）

## [93] (fairness-intent-ui) 攻撃ライダー（+🛡️/+💪/＋状態異常）の分離表示
- 状態: 📋 S
- 根拠: StS2は複数の意図を合成せず並べて表示する（§5「複数の意図は合成せず並べて表示（StS1から変更）」）。うちの攻防一体・同時強化・付与は1行に連結され、リッチな敵ほど意図行が読みにくくなっている
- 現状(採掘時点): log.ts intentText 24-28: attack は「⚔️ 攻撃 5〜7×3+🛡️12+💪1＋弱体2」と全ライダーを1行連結。牙嵐の狼・石殻の番人・従士など敵ギミック第1波以降この形の敵が増えており、意図行が横に伸びる一方
- 実装スケッチ: App.tsx の意図表示だけ2行構成に: 主行動「⚔️ 攻撃 5〜7×3」+ ライダー行「同時に: 🛡️12・💪1・弱体2」（小さめフォント）。log.ts の1行形式はCLI/ログ用に維持（テキスト出力は1行が正）。CSSの .intent-riders 追加のみ

## [94] (fairness-intent-ui) 【裁定要】実値表示モードの標準トグル化（退屈診断④の判定実験）
- 状態: 🔶 幅あり意図=検討事項④
- 根拠: sts2-reference §5: 本家2作とも意図は実値表示（StS2はアイコン絵柄のみ5段階）。「うちの幅あり表示は本家2作に無い独自要素——退屈診断④（幅表示が計算パズルを曖昧化している疑い・未決）の直接証拠」と明記。§8-6は「幅表示を守るなら独自の読み合いとしての価値を人間ランで立証する必要がある」と結論
- 現状(採掘時点): 幅あり表示が既定。実値公開の機構は蜃気楼の面（C型レリック）の revealIntents フラグ（combat.ts 363-369）として実装済みで、宣言時に shownMin=shownMax=actual へ畳むだけ＝表示層・予測・条件分岐の両側まで自動で実値化される。ただしレリック入手時のみで、ラン設定からは選べない
- 実装スケッチ: ラン開始設定（難易度選択の並び）に「意図の実値表示」トグルを追加し、ON なら RunState 経由で revealIntents を全戦闘に適用（エンジンは既存フラグの配線のみ）。人間ランで幅/実値をA/B比較し、幅表示の「リスクの読み」が実際に面白さへ寄与しているかを検証④の判定材料にする。幅あり表示の廃止そのものではなく実験の器の追加

## [95] (fairness-intent-ui) 【裁定要】不明意図（❓Hidden）の限定導入——「どちらの技か伏せる」敵
- 状態: 🔶 Hidden意図はフェアネス原則と緊張
- 根拠: StS2は意図タイプに Hidden/Unknown を持つ（§5）。全情報公開が既定のうちでは、逆に「一部だけ隠す」ことが読み合いの新しい変種になる（敵側は数値でなくバリエーションで追い詰める方針とも整合）
- 現状(採掘時点): 全敵・全行動が宣言時に意図公開される（declareIntents が必ず EnemyIntentDeclared を emit。combat.ts 260-390）。意図を隠す機構は存在しない。用心深い影・写し身など「読み」を主題にした敵ですら手の内は常に公開
- 実装スケッチ: EnemyMove に `hiddenAs?: string[]`（この行動は「候補リスト」として予告される）を追加し、意図表示を「❓次のどれか: ⚔️10〜14 / 🛡️12 / 🧿弱体2」の列挙形式に（完全な闇ではなく候補全公開＝『予告してから殺す』を守る部分不明）。最悪被ダメ予測は候補中の最大値を採用（過大方向で安全）。第1号は幕3の新敵1体で実験。確認ウィンドウでは実値公開の既存則どおり

## [96] (fairness-intent-ui) 意図アイコンの脅威段階（StS2の5段階絵柄）をCSS階調で再現
- 状態: 📋 S
- 根拠: StS2は攻撃意図のアイコン絵柄を実値の帯（<5/<10/<20/<40/40+）で5段階に描き分け、数字を読む前に「どれくらい怖いか」が視覚で伝わる（§5）
- 現状(採掘時点): App.tsx 1679: 意図行のクラスは intent / intent-defend の2種のみ。攻撃3も攻撃30×2も同じ見た目の「⚔️」で、複数体戦では数字を全部読まないと優先順位が付かない
- 実装スケッチ: conditionalIntentText を包む div のクラスに脅威合計（shownMax×hits・脆弱補正込み）の帯で intent-t1(<8)/t2(<16)/t3(<28)/t4(28+) を付与し、CSSで文字色・背景の階調（黄→橙→赤）を付ける。幅表示の数字は不変＝「読む前に伝わる」レイヤーの追加のみ。帯の閾値はうちの敵の数値基準（幕1打点5-13/幕3 15-28）から取る

## [97] (economy-events) イベント戦闘の器＋「偽商人」特殊報酬戦
- 状態: 📋 M
- 根拠: acts.md §4: 本家は幕プール外のイベント専用encounterを7種持つ（FakeMerchantEventEncounter=偽商人・報酬300G固定戦、DenseVegetation=Wriggler×4、PunchOff=傷ついたコンストラクト2体、MysteriousKnight=筋力+6装甲+6の強化騎士）。monsters-a-l.md FakeMerchantMonster: HP165・コイン吐き2ダメ×8・レリック投げ9+虚弱1・激怒+2という「商人らしい技」で作り込まれている。?から入る戦闘に通常戦と別の報酬構造がある
- 現状(採掘時点): EventChoiceDef（types.ts:574-617）に戦闘を起動するフィールドは無く、events.json 33件は全て gold/hp/wounds/relic 等の宣言的効果のみ。?→戦闘（run.ts resolveUnknown:409-413）は tierFor の通常プールから抽選する通常戦で、報酬も afterVictory の標準（12〜18G+通常ピック）。grep で eventBattle/goldReward/combatReward 該当ゼロ
- 実装スケッチ: EventChoiceDef に `combat?: { encounterId: string; goldBonus?: number }` を追加。applyEventChoice で既存の launchCombat(run, false, encounterId)（?→戦闘と同じ配管）を呼び、RunState に `pendingEventGoldBonus?: number` を積んで afterVictory で加算・クリア。第1弾: イベント「怪しい行商人」（kind:oneTime・幕2）——「品を定める（50%: レリック-120G購入 / 50%: 正体を現す→偽商人戦・撃破で+150G固定）」＋立ち去る。専用敵 enemy_fake_merchant（HP95・幕2帯・コイン吐き2×6連撃/レリック投げ8+虚弱1/激怒=筋力+2 のローテ）を enemies.json に追加（イベント専用=幕プールに入れない。ELITE_POOLS と同様に encounters.json のプール外参照で成立）

## [98] (economy-events) 訓練ダミー（制限ターン内チャレンジ戦）
- 状態: 📋 M
- 根拠: acts.md §4 BattlewornDummyEventEncounter（3種の設定違いから1体）＋monsters-a-l.md BattleFriend V1/V2/V3（HP75/150/300・一切攻撃しない・BattlewornDummyTimeLimitPower量3=制限ターンで戦闘から離脱）＋powers.md BattlewornDummyTimeLimitPower（RanOutOfTimeフラグで離脱）。「制限ターン内に倒せるか」のDPS検定を敵側の器で実装している
- 現状(採掘時点): 時間制限チャレンジ戦は存在しない。ただし部品は揃っている: 逃走（fled=hp:0+fledフラグ）は実装済みで、逃がすと報酬が減る前例も rollRewards の eliteEscaped（run.ts:914=逃がすとレア確定喪失）と afterVictory の fledLoss/bounty（run.ts:972-977）で確立済み
- 実装スケッチ: 敵 enemy_training_dummy（HP140・moves は kind:'rest' のみ＋sequence [rest, rest, flee]=3ターン目に必ず逃走・攻撃ゼロ）を追加。イベント「使い古しの案山子」（oneTime・全幕）:「挑む（3ターン以内に倒し切れば+80G。逃げられたら何もなし）」＋立ち去る。提案1のイベント戦闘の器（combat フィールド+goldBonus）に相乗り——撃破時のみ goldBonus が入り、fled 成立なら afterVictory が自然に0精算する（既存配管で完結）。HPを幕別に140/200/280と変えると「今のデッキの瞬間火力」の自己診断イベントになる

## [99] (economy-events) カードを盗む敵（Swipe式・倒せば特別報酬で返る）
- 状態: 🔶 カード盗み
- 根拠: powers.md §2-b SwipePower: プレイヤーの手札からカードを1枚奪って保持し、自分が死亡すると奪ったカードが特別報酬として返る（ThievingHopper が使用・IsInstanced で「誰から奪ったか」を記憶）。本家の盗み経済は金（Thievery/Heist）とカード（Swipe）の2系統
- 現状(採掘時点): 盗みは金のみ。enemies.json の盗み系 kind は steal-gold と flee だけ（こそ泥ゴブリン・金羽の大鴉 grand_steal 40-60G）。grep で steal-card/stolenCard 該当ゼロ。金の盗みレース（宣言即成立→次宣言で逃走→倒せば全額+懸賞金10G）は確立済み（combat.ts:273-285, run.ts:970-977）
- 実装スケッチ: 新move kind 'steal-card': 宣言即成立で手札のランダム1枚（伏せ札は対象外）を enemy.stolenCardUids へ移す（意図表示「🃏カード盗み」・打ち消し成功で差し戻し=盗み金の2026-08-31裁定と同型）。盗んだ次の宣言で必ず逃走（既存則をそのまま適用）。撃破すれば戦闘中に手札へ戻る+懸賞金。逃走成立時の重さが裁定事項: (a)この戦闘中だけ失う（ラン層のデッキは無傷=マイルド） (b)ランのデッキから永続喪失（本家式・盗みのドラマ最大）。幕2に「掠め取りの跳ね虫」（HP50・つつき5-7→カード盗み→逃走）を1体

## [100] (economy-events) 時限呪い（Guilty式・N戦で自然消滅する呪い札）
- 状態: ✅ a5e1c52 仮初の烙印+疚しい取引
- 根拠: powers.md §3-b Guilty: デッキに残ったまま5戦闘を経過すると自動的に消える呪い（StS1未確認の新種と明記）。「恒久汚染ほど重くないが、序盤に受けるほど長く痛い」中間帯の対価をタイマー構造で作っている
- 現状(採掘時点): CardInstance（types.ts:690-714）に期限系フィールドは無い（token/freeThisCombat/setFresh/innate のみ）。呪い札は負傷（無反応・除去可）と烙印（恒久HP1 tick・除去可）の2段しかなく、対価の粒度が粗い
- 実装スケッチ: CardInstance に `expiresAfterBattles?: number` を追加し、afterVictory でデッキ全体をデクリメント・0で除去（選択履歴に「呪いが薄れて消えた」を記録）。新状態異常札 status_guilt（使用不可・効果なし=デッドカード）を content.ts に追加。EventChoiceDef に `timedCurses?: number`（5戦時限の status_guilt をN枚混入）。イベント第1弾:「疚しい取引」（act:1）——「受け取る（+100G・時限呪い2がデッキに5戦の間）」＋立ち去る。幕1の5戦=幕の1/3を占めるので序盤ほど重い自己調整型の対価になる

## [101] (economy-events) 借金呪い（Debt式・手札にあるとゴールドを失う）
- 状態: ❌ combat層はゴールドを知らない(純度裁定)と衝突。run層精算の設計が必要
- 根拠: powers.md §3-b Debt: 手札に残ったままターンを終えるとゴールドをN失う呪い（StS1未確認・★新と明記）。呪いの支払い先をHPでなく経済に向けた変種で、金満ラン（うちの実測605G）に効く蛇口違いのシンク
- 現状(採掘時点): 呪い札のtickはHPのみ（combat.ts:1023-1033: 火傷×2+烙印×1の直接HP損失）。combat層はゴールドを一切知らない設計（盗みの精算はrun層で行う純度維持、combat.ts:1473のコメントで明文化）で、ゴールドに触る呪いは書けない
- 実装スケッチ: 新状態異常札 status_debt（使用不可・自ターン終了時に手札にあると GameState.player.debtTicks を+1するだけ=combat層は数えるだけ）。afterVictory で gold -= debtTicks×3（最低0でクランプ）——盗みの stolenGold と同じ「combat が数え、run が精算する」設計をそのまま踏襲するので純度は破らない。配布口: イベント「高利貸しの亡霊」（act:2・「借りる: 即金+180G・借金2がデッキに」＋立ち去る）。ScaldTick と同じ位置に DebtTick イベントを emit してUIで可視化

## [102] (economy-events) 「どの毒を飲むか」イベント（KnowledgeDemon式・対価の性質を選ばせる）
- 状態: ✅ a5e1c52 毒の三杯
- 根拠: powers.md §3-a/§1-b: KnowledgeDemon は MindRot（ドロー-1）/WasteAway（最大エナジー-1）/Sloth（プレイ枚数制限）/Disintegration（毎T自傷）の呪い2択を3回プレイヤーに選ばせる。sts2-reference §4「呪いの押し付けでなく『どの毒を飲むか』の決断」——報酬は固定で対価の性質だけを選ばせる構図
- 現状(採掘時点): events.json 33件は全て「対価の量の差」か「報酬の種類の差」。黒曜の偶像=報酬の大きさに対価が比例（+130G烙印1 / レリック烙印2）、心の花=同じ負傷2で報酬が違う——つまり「同一の大報酬×性質の違う対価3択」は1件も無い
- 実装スケッチ: イベント「毒の三杯」（kind:oneTime・幕2以降）: 報酬は3択とも共通（レリック1個+60G）で、対価だけが違う——①烙印2（恒久のHP tick=長い痛み） ②負傷3（デッキ肥大=引きの汚染） ③HP-18（即払い=今の危険）。＋安全な「立ち去る」（規約「最後は立ち去る」維持=ボット互換）。既存フィールド（brands/wounds/hp/relic/gold）だけで書けるのでデータ追加のみで完結。時限呪い・借金（提案4・5）が入れば選択肢を差し替えてさらに性質の幅が出る

## [103] (economy-events) 幕頭の祝福（Ancient/Neow式の幕遷移ブーン選択）
- 状態: 🔶 幕頭祝福=大型
- 根拠: acts.md §1: 本家は各幕に祠(Ancient)候補を持つ（Act1=Neow・Act2=Orobas/Pael/Tezcatara・Act3=Nonupeipe/Tanx/Vakuu）＝幕頭の祝福選択がラン開始だけでなく毎幕の構造。ボス撃破報酬（戦闘の対価）と幕頭の祝福（次の幕への方針選択）を分けている
- 現状(採掘時点): createRun は phase:'map' 直行（run.ts:658-729）でラン開始時の祝福なし。advanceActIfBossCleared（run.ts:998-1018）も新マップ生成→'map' 直行。幕遷移の報酬はボス側に全部束ねてある（全回復+レリック3択+ボスゴールド+カード報酬）
- 実装スケッチ: 新フェーズ 'blessing' と data/blessings.json（幕別プール）。幕1はラン開始時・幕2/3はボス報酬の後に3択を提示: 例) ①最大HP+7 ②+80G ③ランダムレア1枚 ④除去1枚+鍛え1枚 ⑤「何も受けない→この幕の最初のエリート報酬にレリック+1」。RunState に blessingOptions を追加し、RunCommand に PickBlessing/SkipBlessing。抽選はランRNG（リプレイ互換）。sim/play.ts のボットは「最後の選択肢」約束と同様に固定選択で壊れ検知を通す

## [104] (economy-events) 消耗品（ポーション）システムの最小導入
- 状態: 🔶 ポーション=大型
- 根拠: sts2-reference §6: 本家のアセンション1〜7は全部経済税で、その中に「ポーション枠-1」が入っている=StS2もポーション経済を難易度の主要レバーとして維持している。戦闘報酬・ショップ・イベントに第3の資源（1回きりの切り札）が流れる構造
- 現状(採掘時点): 消耗品はゼロ。CLAUDE.md が「ポーション・回復レリック不在の補正」として幕ボス撃破の全回復を明示的に代償にしている。戦闘報酬はカードのみ・ショップはカード/レリック/サービスのみ
- 実装スケッチ: 最小3種から: 回復瓶（HP20）・爆炎瓶（敵全体10ダメ）・明晰の瓶（2ドロー+一時マナ1）。所持上限2枠・戦闘報酬で15%ドロップ（シードRNG）・ショップに1枠（50G）。効果は既存の DeclarativeEffect を1回きり実行する形（resolveEffect の再利用）で実装コストを圧縮。戦闘中いつでも使用可（エナジー消費なし=本家準拠）。RunState.potions と Command UsePotion を追加

## [105] (economy-events) 呪い札の性質ラダー（Doubt/Shame/Regret式）
- 状態: 📋 S
- 根拠: powers.md §3-b: Doubt=手札に残ったまま終了で弱体+1（★新）・Shame=同構造で虚弱+1（★新）・Regret=ターン終了時にその時点の手札枚数ぶんダメージ。「手札に居座ると発火する」共通API（OnTurnEndInHand）に多様な支払い先を載せている
- 現状(採掘時点): 状態異常・呪い札は4種のみ: 負傷（無反応）・がらくた（山札混入・無反応）・火傷（HP-2 tick・戦闘限り）・烙印（HP-1 tick・恒久）——content.ts:125-172。全部が「無反応かHP直撃」で、デバフを付与する呪いや手札枚数参照の呪いは無い（既存の弱体/虚弱の付与経路は敵行動の inflict のみ）
- 実装スケッチ: ScaldTick と同じフック（combat.ts:1020-1033）に2種追加: status_doubt（自ターン終了時に手札にあると弱体+1=次ターンの与ダメ-25%が対価）・status_regret（手札枚数×1のHP損失=青の抱え込み軸への狙い撃ち圧・「呪いを抱えて手札を溜める」の自己矛盾を作る）。配布口は?イベントの対価バリエーション（提案6の選択肢差し替え）と、将来の敵 inflict の器。両方とも戦闘限り（火傷準拠）で開始し、恒久版は実測後に検討

## [106] (economy-events) 継続窃盗の敵（Thievery式・毎ターン少額ドレイン）
- 状態: 🔶 継続窃盗
- 根拠: powers.md §2-b ThieveryPower: 毎ターン対象のゴールドをAmountまで奪って自分の中に貯蔵し続ける（HeistPower=死亡時に貯蔵分が報酬化=倒せば取り返せる）。うちの一撃盗み（盗んだら即逃走）と違い「生かしておく1ターンごとに損害が積もる」時間課金型のレース
- 現状(採掘時点): 盗みは1回宣言型のみ。しかも「盗んだ敵は次の宣言で必ず逃走する」強制則（combat.ts:273-285・2026-08-30裁定）がエンジンに固定されており、継続盗みは現行則では構造的に書けない
- 実装スケッチ: EnemyMove に `keepStealing?: true`（このフラグ持ちの盗みは強制逃走則を適用しない）を追加し、combat.ts:282 の逃走差し込みを `!move.keepStealing` でガード。幕2に「銭浚いの小鬼」（HP55・盗み4-6G→小突き7-9のローテ・HP半分以下で逃走move）——毎ターン数Gずつ吸われ続けるので「後回しにする時間」が金額で見える。倒せば全額奪還+懸賞金（既存精算がそのまま機能）。一撃盗み（こそ泥・大鴉）との差=「1ターン以内に倒せ」vs「早く倒すほど得」の別の問い

## [107] (economy-events) ショップ員数の固定保証（本家=固定3/幕）
- 状態: 🔶 #4と同件
- 根拠: acts.md §5: NumOfShops=3 全幕共通・固定（Rest/Unknownは乱数だがショップだけ固定値）。sts2-reference §1 対照表も「ショップ 固定3/幕 vs うち重み5%」と差分を明記。金を使う機会の供給だけは運に委ねない設計
- 現状(採掘時点): quota = Math.round(total×0.05)（map.ts:318・ROOM_WEIGHTS.shop=0.05）。総ノード数はシードで変動（実測57〜82）するため、小さいマップでは2個に丸まりうる=金余りの一因になりうる。エリートだけは員数固定4の前例あり（map.ts:118 ELITE_COUNT）
- 実装スケッチ: quota のショップ行を `['shop', 3]` の固定員数に変更（エリートと同じ「設計約束は員数固定」方式）。逓増サービス（除去75+50/回・強化100+50/回）とレア枠150Gのシンクが毎幕確実に3回開く=ゴールドシンク強化（2026-08-31）の趣旨とも整合

## [108] (economy-events) エリート員数5/幕への増枠（レリック経済の本家対照）
- 状態: 🔶 エリート5=供給経済
- 根拠: acts.md §5: NumOfElites=5（目標値・配置制約で調整）。sts2-reference §1 対照表「エリート 目標5/幕 vs うち4/幕」。本家はエリート=レリック供給の主蛇口を幕5回分用意している
- 現状(採掘時点): ELITE_COUNT=4（map.ts:118）・ELITE_PATH_MIN=3（1経路で踏める保証は3個）。エリート報酬=レリック3択+レア確定+懸賞金30-40G。エリート専用敵は各幕4種に拡充済み（2026-08-31）なので、5枠でも同一個体の重複は幕内で最大1回
- 実装スケッチ: ELITE_COUNT を 5 に変更（ELITE_PATH_MIN=3 は据え置き=「3個以上踏める経路の保証」と「完全回避ルートの成立」は不変・供給の上限だけ+1）。配置制約（出口2以上・行2以降・非隣接系）は既存DPがそのまま検証する。レリック供給+30-40G×1回分の経済上振れが増え、「挑む/避ける」のルート選択の振れ幅が本家水準になる

## [109] (design-guardrails) タイマー敵の設計規約をCLAUDE.md確定行+機械判定テストに昇格
- 状態: ✅ e6eca24 balance-policy.md+機械固定
- 根拠: StS2 §7: タイマー即死ボスTheInsatiableは恒常的に炎上——解決手段が専用札（FranticEscape）を引けるか依存だと「構築の失敗」と「理不尽」の区別が付かない。§8-7が「解決手段が専用札依存のタイマー禁止」を新規規約候補として名指し。
- 現状(採掘時点): 実データ確認: タイマー機構持ちは7体（門番=enrageEveryCards8+enrageEveryDamage80・囁きの狂信者=enrage2毎フェーズ・終焉の唱い手=enrageEveryCards6・砥石の巨像/見習い巨像/火薬樽かつぎ=シーケンス大技・巨面=睨み→圧潰34）。全て「筋力の漸増」か「予告付き大技(kind:'attack')」で、威圧・打ち消し・防御の汎用手段で受けられる形＝現状はコンプライアント。だがCLAUDE.mdに規約行は無く、enemies.test.tsにタイマー規約のテストも無い（grep確認）。
- 実装スケッチ: ①CLAUDE.md「敵の設計原則」の近くに確定行を追加:「タイマー敵の規約: タイマーの罰は(a)筋力の漸増（威圧で剥がせる）(b)予告付き大技（kind:'attack'＝防御・打ち消し・リアクションで受けられる）のみ。専用札を引けないと敗北が確定する即死・敗北条件型タイマーは作らない（StS2 TheInsatiable炎上の教訓）」。②enemies.test.tsに機械判定を追加: enrage/enrageEveryCards/enrageEveryDamage持ち全敵について、行動テーブル（moves/movesBelowHalf/movesVsSet/setAlt）に'attack'/'defend'/'buff'等の既存kind以外が無いこと・sequenceのチャージ最終手がkind:'attack'であることをoffenders空配列方式で固定。将来「countdown-instakill」的な新kindを足す時に必ずこのテストと規約行に衝突する構造にする。

## [110] (design-guardrails) 威圧の全色アクセス保証（タイマーの汎用解決手段）を機械固定
- 状態: ✅ e6eca24 威圧の全色非レア保証テスト
- 根拠: StS2/StS1の激昂抑制3点セット「筋力を剥がす手段が全キャラにある」（CLAUDE.md激昂行にも記載）。タイマー規約の裏面＝解決手段の供給保証。§7の専用札依存禁止を「汎用札の存在保証」で担保する。
- 現状(採掘時点): 実データ確認（weakenEnemy持ちをスクリプトで抽出）: 緑1（絡め捕る根U）・青1（凍てつく枷U）・赤2（砕牙U・red_smoke C）・黒1（衰滅の呪いU）・白8。全色に非レアで存在するが、テストで固定されていない（cardrules.test.tsに該当なし）。黒Opusラン残課題③「威圧がコミット型ドラフトだと黒に届かない」も既知＝1枚だと提示に出ない供給問題が観測済み。
- 実装スケッチ: cardrules.test.tsに「各色のカードプールに weakenEnemy を持つ非レア札が1枚以上ある」をカラーパイ不変条件として機械固定（削除・レア昇格の事故を防ぐ）。供給を2枚に増やす（黒・青・緑に威圧のコモンラダーを1枚ずつ追加）は新カード追加＝別途ユーザー裁定の枠として提案書に添える。

## [111] (design-guardrails) アーキ別勝率の分散を一級指標化（sim要約行＋構造詰みセルのwhitelistテスト）
- 状態: 📋 M
- 根拠: StS2 §7: Doormakerは「データ上は幕3最弱ボス」でも特定アーキだけ詰む構造で炎上→全面リワーク。平均勝率でなく分散（どのデッキが詰むか）が炎上の火種。
- 現状(採掘時点): src/sim/run.ts のsimulateBattlesはdeck×enemy行とdeck×ALL行のCSVのみで、敵ごとのデッキ間スプレッド・最小勝率の要約が無い（コード確認）。既知の構造詰み（deck_necro対ボス0%・パーミッション対分裂62%・要塞90→47%等）はCLAUDE.mdの散在メモで手動管理されており、新しい詰みが「気づかず生まれる」経路が開いている。
- 実装スケッチ: ①simulateBattlesの末尾に敵ごとの要約行を追加出力: `SUMMARY,enemy,minDeck,minRate,maxDeck,maxRate,spread`（実装は既存Accの集計を敵キーでも持つだけ）。②新テスト src/sim/variance.test.ts: 理想形デッキ全種×ボス/エリート×10シードを回し、勝率0%のセルは KNOWN_STRUCTURAL_HOLES 定数（初期値: deck_necro×各幕ボス）に載っていなければ落ちる——loop.test.tsのoffenders空配列と同じパターンで「新しい構造詰みは意識的にwhitelistへ書く1手」を強制する。閾値0%は膠着とボットの下手さを含まない最も保守的な線なので機械固定に安全。

## [112] (design-guardrails) 敗北の感触を2分類する検証プロトコル（評価バーに「構築の失敗/理不尽」・LLMラン定型質問）
- 状態: 📋 M
- 根拠: StS2 §7: TheInsatiable炎上の芯は「デッキ構築の失敗と理不尽の区別がプレイヤーに付かない」こと。Mega Critも「インゲームのフィードバックツールが調整の指針」と明言＝うちの評価バー路線の強化が本家の答え。
- 現状(採掘時点): ui/report.ts の BattleRating は strength/fun/note の3フィールドのみ（コード確認）。敗北時にどちらの感触だったかを構造化して記録する口が無い。LLMランの指示（CLAUDE.md「LLMプレイテストのコスト規律」）にも敗北時の定型質問は無い。
- 実装スケッチ: ①BattleRatingに任意フィールド `lossFeel?: 'build' | 'unfair'` を追加し、敗北画面の評価バーにのみ「負けの感触: 🧩構築の失敗 / ⚡理不尽」の2択ボタンを出す。レポートの戦闘履歴テーブルに列を追加。②CLAUDE.mdのLLM規律節（または人格プロンプトのテンプレ）に敗北時の定型質問を追加:「この敗北は構築の失敗か理不尽か」「このデッキ軸で構造的に不可能（何を引いても解けない）と感じた敵はいたか」。『理不尽』回答が同一の敵に2本一致したら作り直し候補、をルール化。

## [113] (design-guardrails) 「作り直し基準」の明文化: 統計が健全でも特定アーキ詰みの報告2本一致で構造リワーク
- 状態: ✅ e6eca24 作り直し基準の明文化
- 根拠: StS2 §7: 開発が「データ上は最弱」と統計で説明しても不満は消えず、Doormakerは最終的に全面作り直し。統計の健全性はリワーク不要の証明にならない、が本家の実地教訓。
- 現状(採掘時点): CLAUDE.md「敵の設計原則」は「問いの多様化で落とす・予告してから殺す」等の設計原則のみで、リワーク判断の基準行が無い。装甲行に「分散幅は監視」とあるが、監視の出口（どうなったら作り直すか）が未定義。
- 実装スケッチ: CLAUDE.md「敵の設計原則」行に判断基準を1文追加:「ボス・エリートの作り直し基準: sim勝率が健全でも、人間/LLMランで『特定アーキで構造的に不可能』の報告が独立2本一致したら、数値調整でなく構造（問いの形）を作り直す（StS2 Doormaker→Aeonglassの教訓）。数値の言い訳で監視を続けない」。提案4の lossFeel='unfair' 集計がこの基準の入力になる。

## [114] (design-guardrails) ナーフ運用規約の一本化（docs/balance-policy.md＋確定行）
- 状態: ✅ e6eca24 balance-policy.md
- 根拠: StS2 §7: ナーフパッチのたびにレビュー爆撃（1日8千〜1.5万件）。本家の防波堤は「テレメトリを指針にする・環境側で受ける」。うちの散在裁定を炎上前に成文化しておく。
- 現状(採掘時点): ナーフ関連の裁定がCLAUDE.mdに散在: 「ナーフは要許可・基本は上昇」（赤の水準行）・「過剰になったら敵側で受けカードは戻さない」（報酬プール下限行）・「ユーザー許可済みナーフ」の都度記録（焔の目録・全てを燃やせ等）・「レアリティは単独コミットで先行」（sim差分の切り分け前例）。docs/にbalance-policy.mdは無い（ls確認）。
- 実装スケッチ: docs/balance-policy.md を新設し4原則を成文化: ①ナーフは事前にユーザー裁定必須（既存裁定の昇格） ②ナーフは単独コミット（sim差分の切り分け——レアリティ先行の前例を一般化） ③ナーフ案には必ず代替案（敵側処方・買い戻しバフ・供給側調整）を併記して選ばせる ④当てる前にロールバック条件（例: 人間ラン2本で面白さ低下が一致したら戻す）を宣言する。CLAUDE.md確定済みルール表に「バランス運用」行を1行追加して同文書を一次資料に指定。

## [115] (design-guardrails) 戦闘評価テレメトリの横断集計スクリプト（敵×評価の平均表＝作り直し候補の機械抽出）
- 状態: 📋 M
- 根拠: StS2 §7: Mega Crit「Steamレビューよりインゲームのフィードバックツールとテレメトリが調整の指針として役立った」。1ラン1レポートの点データを、敵単位の面データに変える。
- 現状(採掘時点): 評価バー（強さ/面白さ/メモ）はレポートmd 1本ごとに埋まるだけで、複数ラン横断の集計手段が無い。scripts/はcard-audit・dump-rng-golden・map-stats・verify-map-uiの4本のみ（ls確認）で評価集計は無い。
- 実装スケッチ: scripts/ratings-summary.ts を新設: 引数のプレイレポートmd複数（またはsave-*.jsonのhistory）をパースし、敵/編成ID×（強さ平均・面白さ平均・件数・敗北率・lossFeel内訳〔提案4導入後〕）の表をstdoutに出す。「面白さ平均≤2.5かつ件数≥3」の敵を『作り直し候補』として先頭に明示（提案5の基準の運用ツール）。レポートの戦闘履歴テーブルは既に機械可読な形（|区切り）なのでパースは軽い。

## [116] (design-guardrails) 幕1序盤のWeak帯プール分離（StS2 GrabBagの簡易版＝初見体験の構造保証）
- 状態: ✅ dc17ea9 Weak帯として実装済み
- 根拠: StS2 §1: 幕頭に「Weak→Normal」の出現順リストを一括生成し、「最初のN戦だけ弱い敵」をリスト構造そのもので保証（幕1=3回）。序盤に強敵が事故で出ることが構造的に起きない。
- 現状(採掘時点): src/engine/map.ts tierFor(159-162行)は幕単位の単一プール（幕1=12編成の一様抽選+直前2行回避）。行0からタイマー型（囁きの狂信者=毎フェーズ強化+2）やペア編成（こそ泥の二人組等）が抽選され得る。序盤緩和はHP倍率0.55のみで、質（ギミック・頭数）の緩和は無い。
- 実装スケッチ: ACT_POOLSの幕1のみ WEAK_POOL（教師系ソロ: 探り屋・うねる獣・針毛の栗鼠・酸吐きの蛞蝓・物真似の子鬼あたり5〜6体）を分離し、tierForで行0〜1はWEAK_POOLから抽選（行2以降は現行プール）。map.test.tsに「行0〜1の候補にenrage系タイマー・ペア編成が入らない」を機械固定。既存の直前2行回避・シード決定性はそのまま。

## [117] (design-guardrails) 初回ラン限定の教師順（チュートリアル導線）はロードマップ項目として記録に留める
- 状態: 🔶 ロードマップ記録
- 根拠: StS2 §1: 初回ランはNormal/Elite/Eventの先頭数枠を固定順（易しい敵→ギミックの教師）に差し替え、ボスは未撃破優先。「初見の体験を設計で保証する」手当て。
- 現状(採掘時点): 初回ラン判定の機構は無い（RunStateにfirstRunフラグ無し・UI層のlocalStorageにも初回検知無し。grep確認）。ボスは常に固定難度順（オーガ→大亀→門番）なので「未撃破ボス優先」は現状不要。
- 実装スケッチ: 今は実装せず、docs/roadmap.md のPhase C（リリース準備）に「初回ラン教師順: RunState.originにfirstRunフラグ→幕1の最初の3戦を固定並び（例: 探り屋→酸吐きの蛞蝓→用心深い影＝ローテ・デバフ・伏せ検定の教師順）」を1項目として記録。プロトの検証対象（面白さ）に初見導線は含まれないため、実装はゲートA通過後。

## [118] (design-guardrails) 敵の数値帯band監査テスト（確定行「敵の数値基準」の機械固定）
- 状態: 📋 M
- 根拠: StS2 §2の数値帯対照が物差しとして整備された今、うちの確定行（幕1 HP38-55/幕2 60-80/幕3 90-130・打点帯）をテスト化できる。過去の帯違反（苔の主150→135、2026-08-28）は手動発見だった。
- 現状(採掘時点): grep確認: structure.test.ts・enemies.test.tsに帯検査は無い。enemies.jsonのmaxHpは素値（例: うねる獣90）で、幕スケール（幕1前半0.55〜幕3後半1.3）を掛けた実効値が帯に入る設計。新敵追加・スケール定数変更のどちらでも帯を静かに割れる。
- 実装スケッチ: enemies.test.tsに band 監査を追加: 各幕プール（ACT_POOLS）の敵・編成メンバーについて (a) maxHp×その幕の前半/後半スケールが確定行の帯±10%に収まる (b) 1行動の最大打点（max×hits、alsoDefend除く）が帯上限（幕1=13・予告付き大技16/幕2=20/幕3=28）以下——をoffenders空配列方式で固定。エリート専用敵・幕ボスは各自の校正済み（depthHpScale対象外の裁定あり）なので対象外。現行データで違反が出たらそれ自体が発見（修正は別途ユーザー裁定）。

## [119] (design-guardrails) 裁定済み敵規約の未テスト3件を機械固定（enemy-conventions.test.ts）
- 状態: ✅ e6eca24 enemy-conventions.test.ts
- 根拠: StS2 §3: 本家の行動設計は「文法」（CannotRepeat・UseOnlyOnce等）として一貫し、パッチをまたいで安定している。うちはCLAUDE.mdに裁定を書いてもテスト化漏れがあり、次の敵追加で静かに破れる。
- 現状(採掘時点): grep確認でテスト未固定の裁定3件: ①「とげ持ちは防御行動を持たない」（2026-08-30裁定・栗鼠の丸まり除去。thorns×defendのテスト無し） ②「応援役は毎ターン積めない」（2026-08-28間欠化。鼓吹きはsequence[war_drum,thump]だがrally連続禁止の一般テスト無し） ③「盗んだ敵は次の宣言で必ず逃走」——こそ泥の4拍ローテはenemies2.test.ts:88で固定済みだが、fleeモーブを持たない盗人（金羽の大鴉）の合成逃走（2026-08-31修正）はテスト無し（grep enemy_elite_gold_raven in *.test.ts = 0件）。
- 実装スケッチ: src/engine/enemy-conventions.test.ts を新設し全敵走査で固定: (a) thorns>0の敵のmoves/movesBelowHalfにkind:'defend'が無い (b) rallyを持つ敵はsequenceを持ち、rallyが2連続しない (c) steal-goldを持つ全敵について「盗み成立済み+盗んだ額>0」の状態から次の意図宣言がfleeになる（大鴉の合成逃走を実プレイで検証）。既存のoffenders空配列パターンを踏襲。

## [120] (design-guardrails) 予告（フェアネス）表示の網羅性テスト＋ラベル表の純モジュール化
- 状態: 📋 S
- 根拠: StS2 §5: 本家は予告を専用アイコン15種で制度化（DeathBlow・CardDebuff等）。「予告してから殺す」が確定原則のうちでは、予告の表示網羅が機械保証されているべき。
- 現状(採掘時点): STATUS_LABELはui/log.ts:14のRecord<string,string>でweak/vulnerable/frail/wound/junk/scaldの6種。ui/log.ts:18のinflictSuffixにフォールバックが無く、enemies.jsonに新statusを足すと意図表示が「＋undefined2」になる。coverage テストは0件（grep STATUS_LABEL in *.test.ts）。調整UIのEFFECT_JA（66効果の日本語名）はApp.tsx内にあり、こちらは生名フォールバックがあるが網羅テストは無い。
- 実装スケッチ: ①STATUS_LABEL・EFFECT_JA等のラベル表をui/labels.ts（React非依存の純モジュール）へ切り出す（App.tsxはimportに変更）。②新テスト: enemies.jsonの全inflict.status（moves/movesVsSet/movesBelowHalf/setAlt内を全部走査）⊆ STATUS_LABELのキー、全カードdefのeffect名 ⊆ EFFECT_JAのキー。③inflictSuffixに `?? status` フォールバックを追加（テストが先に落ちるので実害は残らないが二重の防御）。

## [121] (design-guardrails) 幕1のデバフ密度の床を分布テストに追加（現状固定＝退行防止）
- 状態: ✅ e6eca24 幕1デバフ床テスト
- 根拠: StS2 §4: 「幕1から普通にデバフが飛ぶ」（VineShamblerのTangled・FlyconidのVuln2/Frail2）。StS1の段階的導入は廃止。§8-1「幕1増強は打点でなくHP上端+質の圧で」。
- 現状(採掘時点): enemies.test.ts:384-403の分布不変条件は幕2/3のみ（≥0.6）。幕1はスクリプト実測で付与源持ちが12編成中1（酸吐きの蛞蝓のみ。囁きの狂信者はenrageタイマーでinflict無し）＝8%。敵圧監査（2026-09-01）で幕1に教師2体を足した成果が、テスト上は無防備で次のプール再編で消え得る。
- 実装スケッチ: enemies.test.tsの分布テストにact1を床1/12（≥0.08）で追加し「幕1の状態異常ゼロへの退行」を機械的に禁止する。幕1の密度自体を本家水準（3割前後）へ引き上げるのは敵バリエーション追加＝敵担当次元・ユーザー裁定の枠として別送。

## [122] (design-guardrails) 開幕から見える性質の配布状況を監査テストで固定＋新敵規約に追記
- 状態: 🔶 配布率の目標値設定
- 根拠: StS2 §3-5: 「ほぼ全敵が開幕パワー持ち（配布率ほぼ100%）。素のHP/打点でなく常在パワーが難易度設計の主軸」。うちの静的性質（startingBlock/armor/thorns等）は同方向だが配布が薄い。
- 現状(採掘時点): スクリプト実測: 静的性質・ギミックフィールド（startingBlock/armor/thorns/regen/burnResist/enrage系/guardian/bondStrength/splitInto/angerOnBlock）持ちは43体中17体（40%）。「T1から問いを出せるのは静的な性質だけ」はOpus診断で確立済み（CLAUDE.md静的性質行）だが、新敵追加時の規約にはなっていない。
- 実装スケッチ: ①CLAUDE.md「敵の設計原則」に追記:「新敵は原則、開幕から見える静的性質かギミックを1つ持つ。素の数値だけの敵は『休符』として意図的に選ぶ（うねる獣の前例）」。②監査テスト: 性質持ち敵のIDリストをtoEqualでスナップショット固定（うっかり削除の退行検知）。既存敵への一括配布は挙動変更なのでやらない——規約は新敵から適用。

## [123] (design-guardrails) 新敵・新機構追加のDone定義（壊れ検知チェックリスト）を明文化
- 状態: ✅ e6eca24 Done定義(balance-policy.md)
- 根拠: StS2 §7: 本家はテレメトリと検証体制が防波堤。うちの敵ギミック第1波では「壊れ検知sim(60戦×6デッキ×5敵)」をアドホックに実施しており、次回も同じ水準で回る保証が無い。
- 現状(採掘時点): loop.test.tsが全デッキ×全敵×10シードの無限ループ/膠着0件を常時固定（新敵はallEnemies経由で自動包含＝良い骨格）。ただし「敵追加時に何を確認したら完了か」のチェックリスト文書は無く、第1波の検証観点（スタール・リダイレクトループ・パーミッションの構造的苦手の記録）は個別コミットメッセージとCLAUDE.md行に散在。
- 実装スケッチ: CLAUDE.mdの「LLMプレイテストのコスト規律」節の直後に「敵追加のDone定義」を4行で明文化: ①loop.test緑（ループ・膠着0） ②variance.test緑または新規詰みセルのwhitelist追記（分散監視・提案3導入後） ③enemy-conventions.test緑（規約遵守・提案11導入後） ④体感はLLM 1ランに相乗り（新設問「この敵は何の問いか答えられるか」）。機構検証をLLMに出さない既存規律と整合。

## [124] (design-guardrails) 幅あり意図のAB計測プロトコル（蜃気楼の面を実験器に退屈診断④の判定材料を取る）
- 状態: 🔶 実値表示AB=人間ラン設計
- 根拠: StS2 §5: 本家2作とも意図は実値表示。幅あり表示はうち独自で、「独自の読み合いとしての価値を人間ランで立証する必要がある」（§8-6）＝未決の退屈診断④の直接証拠が出た。
- 現状(採掘時点): 蜃気楼の面（C型レリック・宣言時にshownMin=shownMax=actualへ畳む＝実値公開）が実装済み（combat.ts:119・192行で確認）＝実値化のABに使える実験器は既にある。ただし比較計測のプロトコルは無く、レリックの引き運でしか実値ランが発生しない。
- 実装スケッチ: 廃止提案ではなく計測提案: ①デバッグのチェックポイント開始（createDebugCheckpointRun）に「蜃気楼の面を強制装備」フラグを追加（実装は初期レリック列に1個足すだけ） ②roadmapのゲートA検証項目に「幅あり vs 実値のAB人間ラン各1本（同seed・同リーダー）を評価バーの面白さ・納得感で比較」を登録。判定材料が揃うまで幅あり表示は現状維持。

## [125] (design-guardrails) simulateRunsの敗北分布に「死因の敵」を追加（ラン層の分散監視）
- 状態: ✅ e6eca24 死因の敵top5+集中警告
- 根拠: StS2 §7の分散監視をラン層にも: どの行で死んだかだけでなく「どの敵が死因か」の偏りが、特定敵の作り直し候補を機械的に浮かせる（Doormaker検知のsim版）。
- 現状(採掘時点): src/sim/run.ts:489-597のsimulateRunsは deathsByBattle（行番号分布）のみをstderr出力。敵ID別の死因分布は取っていない（敗北時のrun.combatにencounterIdはあるが未集計）。
- 実装スケッチ: simulateRunsの敗北時に直前戦闘のencounterId（またはenemyId列）をMap<string,number>で集計し、リーダーごとに「死因の敵 top5（件数）」をstderrに1行追加。単一の敵が死因の50%超なら『⚠死因集中』の警告行を出す。CSVスキーマは不変（stderrのみ）なので既存の集計スクリプトを壊さない。

## [126] (design-guardrails) 延焼ティックとタイマー系カウンタの相互作用を明文化＋テスト固定（解決順の未定義火種の除去）
- 状態: ✅ e6eca24 延焼→カウンタ算入に是正(実は未算入の盲点だった)+テスト固定
- 根拠: StS2 §7: 「毒とタイマーの解決順の噛み合わせで特定アーキ（純毒）が構造的に間に合わない穴」が指摘された——DoTとタイマーの相互作用の未定義は炎上の種。うちの対応物は延焼×（enrageEveryDamage・regenBreak・装甲）。
- 現状(採掘時点): effects.ts:635-654で延焼ティックもdealDamage経路でdamageTakenTotalに乗る＝enrageEveryDamage・regenBreakに数えられる実装（コード確認）。「延焼は装甲無視」はenemies2.test.ts:272で固定済みだが、「延焼がenrageEveryDamageを進める（バーンで殴るほど門番が怒る）」「延焼がregenBreakに算入される（バーンで再生を止められる）」は仕様として明文化もテスト固定もされていない。
- 実装スケッチ: ①CLAUDE.md延焼行に追記:「延焼ティックは与ダメ系カウンタ（enrageEveryDamage・regenBreak）に算入される＝バーンはタイマーを進める代償と引き換えに再生・装甲の解答になる（意図された緊張）」。②blaze.test.tsまたはenemies2.test.tsに2ケースを固定: 延焼ティックのみでenrageEveryDamageの壁を跨ぐと強化が乗る／延焼ティックがregenBreak閾値に算入される。現行挙動の追認固定であり挙動変更なし。

## [127] (critic) 編成内メンバーのランダム構成（RubyRaiders/Slimes式 memberPool）
- 状態: 📋 M
- 根拠: acts.md §3-1: RubyRaidersNormal=「5種のRuby Raiderから重複無しで3体ランダム」、SlimesWeak=「小スライム2種から1+中スライムランダム+残り1種」、SlitheringStranglerNormal=「相方を3パターンからランダム1」。同じ編成IDを踏んでも中身が毎回違う＝敵を増やさずに反復感を消す仕組み。既出のGrabBag/タグ回避提案は「どの編成を引くか」の抽選で、「編成の中身の抽選」は誰も提案していない
- 現状(採掘時点): src/data/encounters.json の EncounterMember（src/engine/types.ts:916-930）は enemyId 固定のみ。全編成がメンバー完全固定で、例えば enc_thief_pair は常に enemy_thief×2。幕1で栗鼠が9戦中4回出た反復感（CLAUDE.md記載の実測）への処方は編成の頭数追加だけだった
- 実装スケッチ: EncounterDef に `memberSlots?: readonly { pool: readonly EncounterMember[]; noDuplicate?: boolean }[]` を追加し、戦闘構築時（combat.ts の編成展開）に戦闘シードRNGで各スロットを抽選（決定的＝リプレイ安全）。既存 members と併用可（固定メンバー+ランダム枠1）。第1弾: 「こそ泥と相棒」=こそ泥固定+{針毛の栗鼠|うねる獣|探り屋}から1、幕2「ならず者の三人組」=幕2雑魚4種から重複無しで3体。抽選結果はCombatStartedイベントに記録

## [128] (critic) 味方への防御支援 kind:'shield-ally'（Rampart/Guardbot式・第3の支援動詞）
- 状態: 📋 S
- 根拠: monsters-m-z.md Guardbot: 「自分ではなくFabricator本体に15ブロックを付与する支援専用行動」を毎ターンループ。powers.md RampartPower: 「自ターン開始時、味方のTurretOperatorにブロックを配る支援型（砲台編成）」。acts.md Glory Weak枠の TurretOperatorWeak=LivingShield+TurretOperator も同構造。応援（バフ）・回復に続く第3の支援動詞で、既出提案（召喚エコシステム・弔い強化等）はどれもこれを含まない
- 現状(採掘時点): src/engine/types.ts:741-753 の EnemyActionKind に defend（自分にブロック）と heal（最もHP割合の低い味方を回復）はあるが、味方にブロックを配る種別は無い。「潰す順」パズルは rally（応援役）と heal（癒し手）の2種のみ
- 実装スケッチ: EnemyActionKind に 'shield-ally' を追加: 自分以外の生存味方のうち最も脅威（意図の実値が最大）の1体に min〜max のブロックを付与（意図表示「🛡️➡仲間 8〜12」）。打ち消し可。ソロでは defend にフォールバック。第1号: 幕3新編成「砲手と盾持ち」=盾持ち（HP45・shield-ally 10〜14ループ・低火力）+砲手（HP50・多段5×4+リロード強化）——盾持ちを先に潰すか砲手をゴリ押すかのキル順逆問い（rally=攻撃を伸ばす支援に対し、こちらは守りを伸ばす支援＝延焼・貫通が輝く）

## [129] (critic) 多節の再接続（Decimillipede/Reattach式）＝仲間生存中は倒しても復活する節
- 状態: 📋 M
- 根拠: sts2-reference.md §3-7 と monsters-a-l.md Decimillipede: 「3節が個別に死んで自動再接続（ReattachPower 25回分）。死亡→wither→REATTACH_MOVE（Heal意図で予告）→ランダム行動から通常ローテへ再合流」。分裂（倒すと増える）・残機（順に弱くなる）とは別の第3の不死変種＝「全部を同時に落とさないと意味がない」で、全体攻撃・延焼の的。既出の残機（Stock式splitIntoチェーン）提案は単体の形態変化で、仲間参照の復活は未提案
- 現状(採掘時点): src/engine/types.ts:864 に splitInto（倒すと小型N体が即出現）はあるが、死亡した敵が復活する機構は無い。checkCombatEnd は hp0=死亡確定。エリートの不死性は不滅の騎士の再生5+regenBreak33（回復）だけで、「同時撃破の要求」を出す敵は存在しない
- 実装スケッチ: EnemyDef に `reattach?: { charges: number; reviveHpRatio: number }` を追加。hp0時に同じ reattach 持ちの生存仲間がいれば dead にせず downed 状態（hp0・行動は kind:'rest' の「繋ぎ直し」を宣言・意図に「♻️復活予告: 次ターンHP25%で再接続」）にし、次の敵フェーズ開始時に maxHp×reviveHpRatio で復活・charges-1。全員 downed なら全滅＝勝利。第1号: 幕2新エリート「三節百足」（HP40×3節・charges2・各節は締め付け8/膨れ6+強化2/身悶え5×2の位相ずらしローテ）。復活は打ち消し対象外（行動でなく状態）と明記。loop.test でスタール0を機械確認

## [130] (critic) 潜伏（Tunneler/Burrowed式）＝「ブロックを割られたら」の新フェーズ条件
- 状態: 📋 M
- 根拠: monsters-m-z.md Tunneler: 「潜る=ブロック32獲得+BurrowedPower→地中から強打23」、powers.md BurrowedPower: 「ブロックが尽きるまで攻撃を受け付けない実質シールド。ブロックが割れた瞬間に潜行攻撃(Bite)へ移行」＝ブロック残量がフェーズ変化のトリガー。うちのフェーズ条件はHP半分のみで、sts2-reference.md §3-4「フェーズ変化の条件が多様」の未回収分。粉砕（割ると起こす）と貫通（割らずに削る＝起こさず倒せる隠し解法）で色ごとに別の攻略が生まれる
- 現状(採掘時点): src/engine/types.ts:829-831 のフェーズ変化は movesBelowHalf/sequenceBelowHalf（HP50%）のみ。startingBlock（types.ts:858）は静的な初期値で、割れても何も起きない。ブロック量を参照する敵側の状態遷移は存在しない
- 実装スケッチ: EnemyDef に `sequenceOnBlockBroken?: readonly string[]`（+参照先moves）を追加: 戦闘中に敵のブロックがプレイヤーのダメージで0になった瞬間、行動テーブルを恒久切替（belowHalfより優先度低・一度きり）。意図に「⛏️殻が割れると豹変する」を常時表示（フェアネス）。第1号: 幕2新敵「土潜りの大蚯蚓」HP68・開幕ブロック26・潜伏中は尻尾3〜5+ブロック8を積む低圧ローテ→割れたら「地中からの強打14〜18」の高圧ローテ。貫通はブロック無視の既存則で「割らずにHPだけ削って倒す」が成立＝緑の隠し解法として明記

## [131] (critic) 突進の反動（CeremonialBeast/Plow式）＝しきい値到達で蓄積筋力の全没収+スタン
- 状態: 📋 S
- 根拠: powers.md PlowPower: 「被弾しHPが閾値以下になると、蓄積したStrengthを全て失いスタンする」、monsters-a-l.md CeremonialBeast（幕1ボス・HP252）: 突進のたびStrength+2で育つが、Plowを破壊されるとスタン1回→第2形態へ。雪だるま式に育つ敵を「プレイヤーの削りで崩せる」＝威圧（1回きりで追いつかない、が既知の弱点）とは別の、バースト側に開いた雪だるま対策。既出のtransitionMove提案は表示の1拍のみで、没収+隙の報酬は未提案
- 現状(採掘時点): src/engine/combat.ts の敵フェーズ変化（movesBelowHalf）はテーブル切替のみで、strength は持ち越される。蓄積強化への解答は威圧（weakenEnemy・一回きり）と激昂の自己調整のみ。regenBreak（types.ts:838）は再生専用で筋力には触れない。カルト型（囁きの狂信者・毎フェーズ強化+2）は「口を塞ぐ=早く倒す」以外の崩し方が無い
- 実装スケッチ: EnemyDef に `plowBreak?: { hpRatio: number }` を追加: HPがこの比率を初めて下回った瞬間、strength を初期値へリセットし、次の宣言を kind:'rest'（「反動でよろめく」）で1回固定（movesBelowHalfへの切替と併用可・処理順は 没収→rest→belowHalfテーブル）。敵カードに「💢HP50%で蓄積した筋力を失いよろめく」を常時表示。第1号: 幕2新エリートまたは既存カルト型の幕3個体に配布（例: 「暴れ猪鬼」HP110・毎ターン強化+2の突進ローテ・plowBreak 0.5）——「育ち切る前に半分まで削れるか」のレースが数えられる問いになる

## [132] (critic) 誘い水（InfestedPrism/VitalSpark式）＝殴ったプレイヤーに一時マナを与える逆説ギミック
- 状態: 📋 S
- 根拠: powers.md VitalSparkPower: 「攻撃してきたプレイヤーにこちらから毎ターン1回だけエナジーを付与（=殴らせて得させる逆説的ギミック）」。monsters-a-l.md InfestedPrism（幕2エリートHP200）が開幕付与し、JAB22/旋風9×3/ブロック20+強化4の重ローテと組む＝「餌をもらいながら長引かせるほど敵も育つ」時間の取引。とげ（殴ると損）の正反対の問いで、既出提案に「殴ると得」系は無い
- 現状(採掘時点): プレイヤーのエナジー獲得はカード効果（gainEnergy）とレリックのみ。src/engine/combat.ts の dealDamageToEnemy に敵側からプレイヤーへ資源を与える経路は無い。敵の被攻撃フックは thorns（反射ダメージ）と angerOnBlock 等の罰系だけで、報酬系フックはゼロ
- 実装スケッチ: EnemyDef に `vitalSpark?: number` を追加: この敵がプレイヤーのカード攻撃でHPを失った最初の1回/ターン、プレイヤーは一時マナ+N（意図とは別に敵カードへ「⚡誘い水: 攻撃するとこのターン一時マナ+1（1回）」常時表示。ログにも出す）。装甲・自己強化ローテと同居させ「もらったマナで削り続けるか、換金の湧きを断つため無視するか」を作る。第1号: 幕2〜3の重量級1体（例: 石殻の番人の変種か新敵「霊気孕みの結晶」HP90・装甲18・毎ターンブロック10+強化2）。sim壊れ検知でマナ供給の悪用ループ（0マナ攻撃×多段で毎ターン確定+1）が過剰でないか確認——判定は「HPを失った」基準なので多段でも1回

## [133] (critic) 【裁定要】リソース完全ゼロ化の1ターン劇薬（NoEnergyGain/NoDraw式）
- 状態: 🔶 リソース完全ゼロ化は劇薬
- 根拠: powers.md §1-b: NoDrawPower（このターンのドロー強制0）/ NoEnergyGainPower（次のエナジー獲得を強制0）/ NoBlockPower の3兄弟＝「-25%の上に『完全に0』の段が別にある」（sts2-reference.md §4）。既出のdebuff-system提案は霞み（ドロー-2・最低3）と守崩し（NoBlock）まで——エナジー版と完全ゼロ版は未提案
- 現状(採掘時点): src/engine/types.ts:756 の PlayerStatus は weak/vulnerable/frail/wound/junk/scald/restrain のみ。エナジーに触るデバフは存在しない。うちはエナジーを敵ターンへ持ち越し次ターン全回復する仕様（CLAUDE.md「エナジー」行）なので、「全回復を1回止める」は温存分だけが残る＝温存経済（set-confirmの魂）と噛み合う下地がある
- 実装スケッチ: PlayerStatus に 'choke'（窒息: 次の自ターン開始時のエナジー全回復が発生しない。温存した持ち越し分はそのまま使える）を追加。startPlayerTurn の全回復を choke>0 でスキップし-1。意図予告は「🫁窒息1（次ターン、エナジーが回復しない——今の余りが命綱）」。付与元は幕3の1体のみから開始（例: 大振りの斧鬼の大技にライダーで付与し「息切れの窓とセットの呼吸の奪い合い」に）。NoDraw（ドロー0）は理不尽リスクが高いので見送り、霞み案（-2床3）に譲る

## [134] (critic) 【裁定要】ステータス札を弾薬に変える対抗策カード（FlakCannon/Compact式）
- 状態: 🔶 プレイヤーカード追加=凍結中
- 根拠: powers.md §3-a: FlakCannon「手札/山札上のステータスカードを全消滅させ、その消滅数ぶんランダム多段攻撃（8ダメ×N）」・Compact「6ブロック+ステータス札をFuelへ全変換」・RocketPunch「ステータス札として配られる当たり枠」＝本家はデッキ汚染を撒くだけでなく、汚染を資源に変える回収口をカード側に用意している。敵ギミック第1波で火傷・粘液系の汚染が増える一方、この観点は8次元の誰も提案していない
- 現状(採掘時点): 汚染への対抗はショップの除去サービス（75G+逓増）・焚き火の取り除く・黒の引導（exhaustFromDeckChoose・任意札1枚）のみ。負傷/がらくた/火傷を参照・換金するカードは0枚（grep: status_wound/status_junk/status_scald を効果側から参照する札は無し）。黒の亡骸（onSelfExhausted）もステータス札には付かない
- 実装スケッチ: 各色でなく1〜2枚の実験枠から: 黒「骨拾いの焔」（1E・アンコモン・手札と山札の状態異常札〔負傷/がらくた/火傷/粘液〕を全て消滅させ、1枚につき敵全体に3ダメージ・消滅）——ミルの色が汚染も食べる整合。白は既存の removeAllWounds（?イベント効果）のカード化を検討枠に。cardrules.test に「状態異常札の消滅は亡骸を発火させない」を機械固定。査定は card-power.md の期待値係数（汚染0〜5枚の分布）で新設

## [135] (critic) 【裁定要】アセンション式・経済税の器（数値でなく経済で締める難易度列）
- 状態: 🔶 難易度つまみ凍結中
- 根拠: sts2-reference.md §6: 本家の難易度10段は「1〜7段は全部『経済税』（エリート増→行商減→金減→ポーション枠-1→呪い札→物価上昇→報酬減）。敵の数値は8段(HP+5%)と9段(打点+12%)でようやく触り、しかも薄い」。うちの段10=打点×3.0は本家に存在しない水準で、CLAUDE.md自身が「ベース再校正までの暫定・将来は×1.5程度+本家アセンション式の経済税」と記す。既出のDIFFICULTY_STRUCT提案はエリート増・宝箱行→エリート行の構造つまみのみで、経済税の列は未提案
- 現状(採掘時点): src/engine/run.ts:135 の DIFFICULTY_TABLE は { hp, atk } の2列のみ。ゴールド・ショップ価格・報酬提示枚数・開始時呪いに難易度は一切効いていない（報酬・ゴールドは据え置き=本家アセンション準拠、とCLAUDE.mdに記載があるが、実際の本家は経済こそ主戦場だった）
- 実装スケッチ: DIFFICULTY_TABLE には触れず、並置の `DIFFICULTY_ECON: readonly { goldMul?: number; shopMul?: number; startBrands?: number; rewardSlots?: number }[]` を新設し読み点4箇所（勝利ゴールドロール・ショップ価格式・createRunの初期デッキ・報酬提示数）から参照。例示: 段5+→勝利ゴールド×0.9／段6+→ショップ価格×1.15／段7+→ラン開始時に烙印1（②の呪い札=烙印機構を流用）／段9+→報酬提示4→3枚。値はすべて暫定でベース再校正時にユーザーが埋める前提の「器」として実装
