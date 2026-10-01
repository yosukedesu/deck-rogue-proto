ウルトラコードで。

# 目的
デッキ構築ローグライク（リポジトリ /home/yosuke/projects/deck-rogue-proto・枝 hd2d/slice）の Unity 版の戦闘画面を、本家オクトパストラベラー（HD-2D）と並べても見劣りしない美麗さにする三周目。
**段0（分析）は 2026-10-01 に Fable 5.1 が終え、ユーザーの裁定8件が出ている。この引き継ぎでは 段1（実装・試しのビルド）→ 段2 → 本番 → 比較シート → 反証 → 直しの輪 → commit → APK/exe まで進める。**
担当モデルは Opus 5.5 でよい（判断の重い部分は計画書に決めてあり、精度は確かめの輪＝typecheck・compile・build・撮影・計測・目・反証2役が担保する）。

# 先に全部読む
1. **計画 `docs/design/hd2d-round3-plan-2026-10-01.md`**（一次資料。§0 裁定8件・§1 合否・§2 レーン A〜F＋S の持ち場と値・§3 統合の手順・§4 試しの撮影・§5 戻し方）
2. 分析書 `docs/design/hd2d-r3-analysis-2026-10-01.md`（冒頭の裁定・§0 結論・§7 改善案 R1〜R15 の順位表〔値・ファイル・確かめ方・予想図の場所〕・§8 進め方・§11 迷っている所）
3. CLAUDE.md の「HD-2D 見本 進行中」の行（二周目と三周目の要点）・`docs/art-bible.md`（画風の規格。縁1px の例外と接地影の裁定を追記済み）・`docs/design/hd2d-slice-plan-2026-09-30.md` §11（二周目の結果と残った差）・`docs/design/hd2d-round2-plan-2026-10-01.md`（二周目のレーンの分け方の手本）
4. 画（git の管理外。本家の画は著作物なのでリポジトリに入れない）: 本家 `~/.cache/deck-rogue/hd2d-ref/`（ot_921570_16 が主な目標）／二周目の最終の撮影 `~/.cache/deck-rogue/hd2d-r2/shots-final/r2-slice/`／三周目の分析のシートと模型 `~/.cache/deck-rogue/hd2d-r3/`（eyes・layers・root・tech・char・frame・proposals-A/B・verify）
5. コード: `unity/Assets/Game/`（Stage*.cs・Diorama*.cs・ReliefMesh.cs・HD2DFlags.cs・StageLook.cs・StageUnits.cs・BattleScreen.cs・BattleView.cs・Render/*）、シェーダ `unity/Assets/Resources/Shaders/`、設計図 `unity/Assets/Resources/Stage/`（look_act1*.json・act1_layout*.json）、絵 `unity/Assets/Resources/Art/stage/act1/`、絵の作り方 `docs/pixellab/hd2d-act1/gen_art.py`、設計図の作り方 `docs/design/hd2d-slice/r2-layout-gen/`
6. 道具: `scripts/typecheck-unity/run.sh`・`scripts/unity-win.sh {compile|build|build-perf|android}`・`scripts/pshots.sh`（並列2まで・DET=1）・`scripts/hd2d-measure.py`・`hd2d-r2-targets.py`・`hd2d-char-metrics.py`・`hd2d-layout-check.py`・`hd2d-r2-sheet.py`・`scripts/hd2d-states/r2-*.txt`（三周目の一覧 `r3-*.txt` はレーン F が作る）

# 段1 の進め方（計画 §2〜§4）
1. `git status` がきれいで、先頭が `c87c4eb`（三周目の分析の commit）以降であることを確かめる。
2. **二周目の写しを先に作る**（B: `look_act1_r2.json`・`look_act1_r2.dof.json`／E: `look_act1_r2char.json`／C: `act1_layout_r2.json`）＝戻し先。以後、新しいキーを足したら写しに「off／二周目の値」を書く。
3. レーン A・B・C・D・E・F・S を**ワークフローで並列に**（ファイルは重ならない。各レーンは計画 §2 の自分の行を読み、やることを1ページで復唱してから着手。Unity と PixelLab は使わない。`scripts/typecheck-unity/run.sh --quick` で確かめる。commit はしない）。**レーン S（シェーダ）は試しのビルドの前に必ず入れる**。C は各レーンの「置く物の表」を受けて設計図を焼く。
4. 統合（この会話の本体が担当。Unity を動かすのは統合だけ）: typecheck（全部）→ `npm run check:colors` → `scripts/unity-win.sh compile` → `build`。**S25 が `adb devices` に居れば `build-perf android` で15分測る。居なければ「未計測」と明記**。
5. 試しの撮影 `DET=1 scripts/pshots.sh scripts/hd2d-states/r3-trial.txt <出力> 2`（計画 §4 の16枚）→ 計測（新しい物差し `hd2d-r3-targets.py`・layout-check・`Diorama.Check` result）→ 試しのシート（本家｜二周目｜試し）。
6. 決める（計画 §3-4）: 霧の end（40／45）・`lensFar`（3.0／1.6〜2.0＝幹の筋が読めるか）・上の覆いの枚数と tint（暗い画素 ≤66%・帯の頂点 ≥145 を守る）・株の本数。**R3 の UI の試し撮り1枚をユーザーに見せ、「本番（7〜9日）に入れるか」を AskUserQuestion で聞く**（裁定 Q1 の2段目）。
7. 段2（値の既定化・霧の面の廃止・E の変種の採否・残り）→ compile → build → 本番の撮影（約200枚・並列2）→ 計測 → 比較シート **本家｜W5｜二周目｜三周目（5場面＝オーガ・狼・4体・人形9体・スマホ）＋384px の「ひと目」**。
8. **反証2役**（「本家と並べる」役＝厳しく4に届くかを見る／「壊していないか」役＝幕2/3 の画素一致・演出 R1〜R16・配置の検査・性能）→ 直しの輪は最大2回（値だけ・直したら必ず画を見る）。
9. commit はレーンごとに1つ（S・D・C・B・E・A・F）→ 文書（`hd2d-slice-plan` に §12 三周目・CLAUDE.md の HD-2D の行・art-bible の該当節）。**CLAUDE.md は `git add -p` で自分の行だけ。**
10. ユーザーに5場面のシートと exe で「見劣り 1〜5」を聞く → APK（`scripts/unity-win.sh android` → aapt で manifest に fileprovider があることを確かめる → `C:\Users\yosuke\copy-apk.ps1` で Drive の DeckRogue フォルダへ。古い hd2d の APK は消す）と exe（`D:\deck-rogue\DeckRogue-win`。**ユーザーの exe が動いていないことを先に確かめる**）。

# 守ること
- 返事・報告・文書は日本語。内部用語は言い換え、結論から。待たせる時は何をしているかを一言。
- 作業も commit も枝 hd2d/slice。git stash / checkout / reset / restore は使わない。
- ルールエンジン（src/engine・unity/Packages/com.deckrogue.engine）は触らない。幕2/3（stage=old）の見た目は変えない。新しい振る舞いは「箱庭の時だけ」の枝に。
- 座席での1ドット＝画面4px（PC）。UI は舞台の座席に追従する。新しい生の UI 色を足さない（npm run check:colors）。Unity のフォントに「⚙」は無い。
- PixelLab は 10/7 まで呼ばない（発注書の下書き `docs/pixellab/hd2d-r3-order-draft.md` はそのまま）。PixelLab 以外の AI 生成イラストは使わない。
- ユーザーの exe（D:\deck-rogue\DeckRogue-win\DeckRogue.exe）は落とさない。落としてよいのは D:\deck-rogue\unity-batch\Build から起動したプレイヤーだけ。撮影は pshots.sh で並列2まで（3並列は D3D12 で落ちる）・DET=1。
- **裁定8件（計画 §0）を変えない**: 主人公62／1ドット4px／霧の帯はキャラのすぐ上／札は夜色・輪とターン終了は真鍮／外注なし・舞台は全部3Dの箱庭／10/7 の描き直しは幕ボス160と幕1の敵80（描き方は変え、形と色の設計は変えない）／UI の作り直しは試し撮り→裁定／縁1px は許す／接地影は薄く広く／獣の枠は80のまま段1を撮ってから／主人公の画風は据え置き／針葉樹はコード生成で先に・箱庭の数の門は三周目の値／判定は5場面・S25 の計測が条件。変えたい時は理由を添えて聞く。

# 過去の失敗（繰り返さない）
- 数字の門を全部通しても本家と並べると見劣りした（二周目 3〜3.5）。門は明るさの分布・位置・鋭さしか測っていない。**合否は目で**、数字は「どの層が足りないか」を言葉にする補助。
- 「霧の中の幹の縁がくっきり」の物差しを通すために奥のぼかしを 3.0 に強めた結果、1ドット幅の樹皮の筋が消えて幹が灰色の柱になった（帯の幹は1ドット≈3px・ぼかし 2.4〜3.9px）。**幹の筋は2〜3ドット幅・明暗差 ≥40 で描く**か、ぼかしを戻す。
- 株を80〜120本敷くと座席が本家よりうるさくなる（模型で逆効果）。40〜60本・中央は土を見せる。
- 上の覆いを 50〜60% にすると暗い画素 77%（本家 65%）で霧の光が沈む。35〜45%・帯の頂点 ≥145 を守る。
- 半立体は α 0.4 で切るので、不透明 35〜45% の房は半分が消えて「網・旗」になる。房・茂み・草の帯は不透明 0.7 以上の影絵で。
- 箱庭は幕の始めに1回組み、座席は戦闘ごと＝株と茂みは全座席の足元の通りを空ける。
- 直しの輪で値を動かすと別の所が壊れる（mist-lip の横線）。直したら必ず画を見る。
- 1つずつ撮影して確かめると何時間も掛かる。ファイル別のレーンで並列に書き、撮影は最後にまとめて1回。
- S25 の重さは二周目で一度も測れていない（adb に端末なし）。スマホの見込みは全部推測＝つながっていれば必ず先に測る。
- 高さの霧で帯を作ると低いカメラでは頂点が頭の後ろに来る（距離の霧のまま）。暗い木立と横の減光を重ねると模様のない黒い壁になる。霧の面の縁は真っ直ぐな横線に見える。

# モデルの割り当て（参考）
- 段1〜3 の実装・統合・撮影・計測: Opus 5.5 でよい。
- 反証「本家と並べる」役: effort を高めにし、「全部やっても4に届くか」を厳しく見る役を必ず置く（段0 と同じ）。
- 最終の「見劣り 1〜5」はユーザーの目で決める。
