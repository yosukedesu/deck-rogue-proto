ウルトラコードで。

# 目的
デッキ構築ローグライク（リポジトリ /home/yosuke/projects/deck-rogue-proto・枝 hd2d/slice）の Unity 版で、**段2＝幕2（先代の坑道・宿場跡）と幕3（埋もれた古代都市）の戦闘の舞台を、幕1 と同じ「全部3Dの箱庭」で作る**。HD-2D の絵はこのゲームの目玉で、本家オクトパストラベラーと並べても見劣りしない美麗さが目標。
**段0（分析と設計）は 2026-10-02〜03 に Fable 5.1 が終え、ユーザーの裁定が出ている。この引き継ぎでは 段1（口を開ける・幕2 の最小の試し撮り・幕2 のレーン）→ 段1b（幕3 のレーン）→ 本番 → 比較シートと動画の左右並べ → 反証 2 役 → 直しの輪 → commit → ユーザーの判定 → APK/exe まで進める。**
担当モデルは Opus 5.5 でよい（判断の重い部分は計画書に決めてあり、精度は確かめの輪＝typecheck・compile・build・画素比べ・撮影・計測・目・反証 2 役が担保する）。

# 先に全部読む
1. **計画 `docs/design/hd2d-stage2-plan-2026-10-02.md`**（一次資料。§0 裁定 10 件・§1 合否・§2 レーン〔口・S・D・C・B・E・A・M・F・統合〕の持ち場と値・§3 統合の手順・§4 試しの撮影・§5 戻し方・§6 10/7）
2. 分析書 `docs/design/hd2d-stage2-analysis-2026-10-02.md`（§0 結論・§1 目標の画・§2 持ち越し・§3 今の幕2/3 と本家の規則・§4 幕2 の統合案〔層の表・光の台本・案の表 R2-1〜11〕・§5 幕3 の統合案〔R3-1〜9〕・§6 座席と UI・§7 移行と回帰・§8 動き・§9 PixelLab・§10 裁定・§11 迷っている所・付録）
3. 三周目の手本: `docs/design/hd2d-round3-plan-2026-10-01.md`（レーンの分け方と統合の手順）・`docs/design/hd2d-r3-stage1-prompt-2026-10-01.md`（この引き継ぎの形）・`docs/design/hd2d-slice-plan-2026-09-30.md` §12（三周目の結果）・`docs/art-bible.md`（画風の規格。段2 で足す項目は計画 §3-7）・CLAUDE.md の「HD-2D 見本 進行中」の行
4. 第1段・第2段の報告と模型（git の管理外・本家の画を含むのでリポジトリに入れない）: `~/.cache/deck-rogue/hd2d-stage2/`（`targets/`〔目標の画と数字 metrics.json〕・`transfer/`・`current23/`・`honke/`・`seats/`〔encounters.md に全編成〕・`migrate/`〔migrate.md §3 最小の差分・states/ の一覧の下書き・hd2d-pixdiff.py〕・`pixellab/`・`critic-A/`・`B/act2/{propA,propB,sheet,verify-*}/`・`B/act3/…`・`motion/`〔motion.md・sheets・tools/{fine.py,sway.py}・videos〕）。**値の出発点**は `B/act2/propA/look_act2.draft.json`・`act2_layout.draft.json`・`B/act2/propB/geom_b2.md`・`B/act3/propA/propA-look_act3-draft.json`・`propA-layout-draft.json`・`B/act3/propB/geom3.txt`（反証の訂正を分析書 §4・§5 の表に写してある＝表が正）。本家 `~/.cache/deck-rogue/hd2d-ref/`。
5. コード: `unity/Assets/Game/`（Stage*.cs・StageActs.cs〔今の舞台＝触らない〕・Diorama*.cs・ReliefMesh.cs・HD2DFlags.cs・StageLook.cs・StageUnits.cs・StageSeats.cs・StageCamera.cs・StageFx.cs・BattleScreen.cs・BattleView.cs・Presenter.cs・Tween.cs・Render/*）、シェーダ `unity/Assets/Resources/Shaders/`、設計図 `unity/Assets/Resources/Stage/`、絵 `unity/Assets/Resources/Art/stage/act1/`（幕2/3 は `act2/`・`act3/` を新しく作る）、生成器 `docs/pixellab/hd2d-act1/gen_art.py`（触らない。import する）・`docs/design/hd2d-slice/r2-layout-gen/{place.py,layoutio.py,gen_r3.py}`
6. 道具: `scripts/typecheck-unity/run.sh`・`scripts/unity-win.sh {compile|build|build-perf|android}`・`scripts/pshots.sh`（並列 2 まで・DET=1）・`scripts/hd2d-measure.py`・`hd2d-r3-targets.py`・`hd2d-layout-check.py`・`hd2d-r2-sheet.py`・`scripts/hd2d-states/r3-*.txt`（段2 の一覧 `s2-*.txt` はレーン F が migrate の下書きから作る）・ffmpeg（/usr/bin）

# 段1 の進め方（計画 §2〜§4）
1. `git status` がきれいで、先頭がこの引き継ぎの commit 以降であることを確かめる。`adb devices` に S25 が居るか見る（居れば幕1 の 15 分を先に 1 回）。
2. **基準を撮り直す**（計画 §3 手順 0。HEAD のビルドで 11 本・約 275 行）。これが無いと移行の画素一致を証明できない。
3. **口（移行）の差分**（migrate.md §3 の 12 件・計画 §2 の「口」の行）を入れ、typecheck → check:colors → compile → build → 同じ 11 本＋`s2-mixed` → `hd2d-pixdiff.py`。**まず幕1 の組で一致、次に今の舞台、最後に `s2-mixed`**。通るまで次へ進まない。
4. **幕2 の最小の試し撮り 1 枚**（計画 §3 手順 2・§4 T2-min）: 岩棚つきの壁 1 枚＋提灯 2（`lights`）＋slab の段だけの設計図と、台本の光だけの look。帯の 3 条件（計画 §1）を目と数字で見て、提灯の range・mist の α・壁の tint・露出をここで決める。
5. レーン S・D・C・B・E・F・M を**ワークフローで並列に**（ファイルは重ならない。各レーンは計画 §2 の自分の行を読み、やることを 1 ページで復唱してから着手。Unity と PixelLab は使わない。`scripts/typecheck-unity/run.sh --quick` で確かめる。commit はしない）。S の新 kind（rail・arch・pillar・lights・暈・ao の口・技の光の受光の係数）は試しのビルドの前に必ず入れる。
6. 統合（この会話の本体だけが Unity を動かす）: typecheck（全部）→ check:colors → compile → build → 試しの撮影（計画 §4 の T2-*）→ 計測（`hd2d-a2-targets.py`・layout-check・`Diorama.Check` result=OK・perf=120）→ 試しのシート（本家 ot2｜今｜箱庭 × PC/PH）＋動画の左右並べ 2 本（`motion/tools` の fine.py で壁 ×N・暖色・保つ秒を測る）。
7. 決める（計画 §3 手順 4）→ 段1b（幕3 を同じ型で並走・T3-* の試し撮り・縞を断つ変種 2 枚・柱 t −1／−1.6）→ 決める。
8. 本番（両幕）→ 撮影（5 場面 × PC/PH × 4 種＋動画 5 場面 × 2 幕）→ 計測 → 比較シート（本家｜今の舞台｜箱庭・5 場面＋384px の「ひと目」）＋動画の左右並べ 1 本/幕 → **反証 2 役**（「本家と並べる」役は effort を高めにし「全部やっても 3.5 に届くか・4 に何が足りないか」を厳しく／「壊していないか」役は幕1・今の舞台の画素一致・演出 R1〜R16・配置の検査・性能）→ 直しの輪は最大 2 回（値だけ・直したら必ず画を見る）。
9. commit はレーンごとに 1 つ（口・S・D・C・B・E・A・M・F）→ 文書（計画 §7 結果・CLAUDE.md の HD-2D の行〔**`git add -p` で自分の行だけ**〕・art-bible の追記〔計画 §3-7〕）。
10. ユーザーに幕2・幕3 それぞれ 5 場面のシート＋動画の左右並べ＋exe で「見劣り 1〜5」を聞く。幕1 と同等以上（3.5）なら既定へ（`ApplyActDefault` の 1 行＋既定 `"1,2"`）。APK（`scripts/unity-win.sh android` → aapt で manifest に fileprovider があることを確かめる → `C:\Users\yosuke\copy-apk.ps1` で Drive の DeckRogue フォルダへ。古い hd2d の APK は消す）と exe（`D:\deck-rogue\DeckRogue-win`。**ユーザーの exe が動いていないことを先に確かめる**）。

# 守ること
- 返事・報告・文書は日本語。内部用語は言い換え、結論から。待たせる時は何をしているかを一言。
- 作業も commit も枝 hd2d/slice（main は今の舞台のまま）。git stash / checkout / reset / restore は使わない。CLAUDE.md は自分の行だけ commit する。
- ルールエンジン（src/engine・unity/Packages/com.deckrogue.engine）は触らない（座席の席の規則は UI 層 `StageSeats.cs` だけ。`spawnEnemies` は engine）。
- **幕1 の見本の見た目は 1 画素も変えない。幕2/3 の既定（今の舞台）はユーザーの判定まで変えない。** 新しい振る舞いは `HD2DFlags.DioramaHere`（箱庭の幕）か look の口の枝に入れる。`StageActs.cs`・`StageWater.shader`・`Stage.Reflection`・`Art/tiles/act2|3_*`・`Art/props/act2|3_*`・`gen_art.py`・keyflip は触らない（今の舞台が読む）。
- 座席での 1 ドット＝画面 4px（PC）。UI は舞台の座席に追従する。新しい生の UI 色を足さない（`npm run check:colors`）。Unity のフォントに「⚙」は無い。
- PixelLab は 10/7 まで呼ばない（幕1 の四周目 約 590 が先。幕2/3 の舞台は約 32〜40＝計画 §6）。PixelLab 以外の AI 生成イラストは使わない。
- ユーザーの exe（D:\deck-rogue\DeckRogue-win\DeckRogue.exe）は落とさない。落としてよいのは D:\deck-rogue\unity-batch\Build から起動したプレイヤーだけ。撮影は pshots.sh で並列 2 まで（3 並列は D3D12 で落ちる）・DET=1。
- **裁定 10 件（計画 §0）を変えない**: 幕2 から・大物は 3D（像・碑は半立体）・動きのレーン M と動画の判定・敵の描き直しは別裁定（1〜2 体の場面は 3.5 が上限と明記）・幕2 は帯型（壁の照明）＋洞窟型の暖色＋5°＋砂の粒・幕3 は「ot7 の作り＋Tomb の材質」＋床が頂点＋真後ろの斜めの柱・既定へ入れるのは判定の後・読み替え 6 件・本家の戦闘画は来たら数字だけ差し替え・幕1 の裁定はそのまま。変えたい時は理由を添えて聞く。
- 本家の画・動画・それを含むシートと模型はリポジトリと外に出さない（`~/.cache` の中だけ）。報告に貼るのは数字と自分で作った図。

# 過去の失敗（繰り返さない）
- 数字の門を全部通しても、本家と並べると見劣りした。門は明るさの分布・位置・鋭さしか測っていない。**合否は目で**、数字は「どの層が足りないか」を言葉にする補助。
- 見劣りの正体は値でなく層の作り方だった。今の幕2/3 は「どの層にも物はあるが、空気と光が無い」（幕2＝暗闇・幕3＝分離の欠落）。幕3 の案 B は「値の入れ替え」で三周目の失敗を繰り返しかけた（床が暗く上ほど明るい＝本家と逆）。
- **模型（2D の合成）の帯は「塗った物」**。提案 A/B の自己申告（中央値 59・帯 行 340 など）は共通の物差しで再現できなかった。実機の試し撮り 1 枚（最小の設計図）で仕組みを先に確かめる。
- 5° では段の天面は 0〜25px。0.3 unit の折れは 1 ドット未満＝無駄。張り出しは 2〜4 unit。立面を平行に積むと横縞＝部品で縞を断つ。
- block は `HeightAtPath` が slab の外で −1.4 を返す＝段は slab。block/slab に底の面は無い＝天井の「下面の投影」は試す前から失敗。
- 両案とも座席の足元の通りの真後ろに柱・提灯・結晶を置いていた。x は Cam で出す（t の順で読まない）。
- `nightGrade.hi` 0.30 は床の光溜まりと帯まで紺に寄せる（線形輝度で測る）。hi は 0.08〜0.12。
- 暗い画素 45%（本家 ot2）は露出では出ない（照らされた面積の問題）。露出で合わせると p95 >174・主人公が床より暗くなる。
- 暗い物と横の減光を重ねると模様のない黒い壁。霧の面の縁は横線。半立体は α 0.4 で切るので房は不透明 0.7 以上。奥のぼかしを強めると 1 ドット幅の筋が消える（筋は 2〜3 ドット幅・明暗差 40 以上）。
- 直しの輪で値を動かすと別の所が壊れる。直したら必ず目で見る。1 つずつ撮影して確かめると何時間も掛かる＝ファイル別のレーンで並列に書き、撮影は最後にまとめて 1 回。
- 本家の戦闘カメラは待機中に動かない（0.00px）＝揺れは足さない。普通の攻撃で背景は光らない（+2〜+6）＝大きい当たりだけ。
- S25 は一度も測れていない（adb に端末なし）。つながっていれば幕1 を先に 1 回。
- 同じ画の数字が 3 レーンで 3 通りになった（UI の除き方）。段2 の最初に hideui/unitsonly の基準を撮って物差しを 1 組にする。

# モデルの割り当て（参考）
- 段1〜本番の実装・統合・撮影・計測: Opus 5.5 でよい。
- 反証「本家と並べる」役: effort を高めにし、「全部やっても 3.5 に届くか・4 に何が足りないか（10/7 の絵か・裁定の内側か・構造か）」を厳しく見る役を必ず置く（段0 と同じ）。
- 最終の「見劣り 1〜5」はユーザーの目で決める（静止画のシート＋動画の左右並べ）。
