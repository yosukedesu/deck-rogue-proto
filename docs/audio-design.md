# 音の設計書（BGM・SE）— 2026-09-13

ユーザー「そろそろ BGM や SE をちゃんと選定したい。効率的に決めるには」→ ask_user 裁定: **仮はフリー素材・本命は後で発注／音の肌はオクトラ寄り／今回は設計書＋配管＋試聴シートまで**。
絵と同じ手順（言葉を先行→仮素材で画面を完成→面白さ検証後に本命だけ発注）に音も乗せる。

## 1. 音の肌（言葉）

- **オクトラ寄り**: 生楽器のオーケストラ（弦・木管・ホルン）＋民族楽器（ハープ・笛・打楽器）。HD-2D の舞台に合わせる。
- 絵本の紙の肌（UI）は SE 側で担う: 札・ボタンは**紙**の音、からくり（伏せ）は**歯車と発条**の金属音。
- 世界の理屈: 夜だけ濃く湧くマナ／獣と古機／深いほど古代。幕1 森（ケルト・木漏れ日）→ 幕2 坑道（不思議・静か）→ 幕3 古代都市（厳か・機械）。
- **AI 生成の音は使わない**（絵と同じ裁定。AI 臭）。

## 2. BGM の場面（12＋2）

| 鍵 | 場面 | 狙い | いま（8bit チップチューン・要総入れ替え） |
|---|---|---|---|
| title | タイトル | 壮大だが静かな入口 | title.mp3 |
| map1/2/3 | 幕のマップ | 森／坑道／古代都市の空気 | map1-3 |
| rest | 焚き火・ショップ・イベント・工房 | 一息。マップと分ける | （無し→マップ曲） |
| battle1/2/3 | 通常戦闘 | 幕が進むほど重く | battle1-3 |
| elite | 強個体 | 「嫌な予感」。通常と区別 | （無し→幕の曲） |
| boss1/2/3 | 幕ボス | 山場 | boss1-3 |
| won / lost | 走破／敗北 | リザルト／静かな終わり | title／（無し） |

`unity/Assets/Resources/Audio/audio.json` の `bgm` 表が場面→素材名（`Resources/Audio/bgm/<name>`）。`elite`/`rest`/`lost` が null なら幕の曲・マップの曲へ落ちる（`GameRoot.UpdateBgm`）。

## 3. SE の一覧はエンジンのイベントから機械的に出す

戦闘の出来事はすべて `GameEvent`（型 88 種）なので、SE の一覧はイベント型の一覧そのもの。`audio.json` の `sfx` 表は**鍵＝イベント型名**（`CardSet`・`ReactionTriggered`・`SetCardExpired`・`ActionNegated`・`EnemyDied`…）と **`ui.xxx`**（pick_card・pick_relic・buy・upgrade・fuse・rest・node…）。
- `Presenter.TableSound` が新しいイベントを見て表にあれば鳴らす（絵で特別扱いするダメージ・ブロック・回復・ターン・状態異常はコード側）。
- 札の飛び・撃破の消えに合わせる音（CardSet・CardsDrawn・CardPlayed・EnemyDied/Fled）は `BattleView` が `Audio.Key` で鳴らし、Presenter は二重に鳴らさない。
- 画面の操作は `Audio.Ui("...")`。コードに素材名を書かない＝差し替えは表の1行。
- 素材が無い名前は `Synth` が合成する（trap_fire・trap_expire・trap_break・negate・exhaust・permanent・gold・relic・forge・rest・burn は今は合成＝差し替え候補）。

**署名音**（最優先で本物にする）: 罠が鳴る `ReactionTriggered`（発条が弾ける金属音）・仕込む `CardSet`（歯車を巻く）・期限切れ `SetCardExpired`（発条が緩む）。伏せの手触りは音で半分決まる。

## 4. 入手元と権利

| 用途 | 入手元 | 権利 | 備考 |
|---|---|---|---|
| BGM 仮〜準本命 | **PeriTune**（日本・オーケストラ／民族） | 商用可・クレジット任意・改変可・Content ID 登録不可・再配布不可 | 361 曲の目録 `docs/design/audio/peritune-catalog.json`。ループ用ファイル同梱 |
| SE 基礎 | Kenney（同梱済み） | CC0 | rpg-audio / impact / ui |
| **SE 本命** | **Sonniss GDC Game Audio Bundle**（GDC 2021-2023 の 14 パート） | 商用可・クレジット不要・改変可・再配布不可・AI 学習不可 | プロ音源メーカーの無償配布＝インディーの事実上の標準。**2026-09-13 ユーザー「効果音ラボはチープすぎる。デファクトから選びたい」→ 効果音ラボ案は撤回** |
| 本命 BGM | Audiostock（1曲 3千〜1万円）／作曲家外注（3〜10万円/曲） | 買い切り | **ボス3曲＋タイトルの4曲だけ**。面白さ検証の後に投資判断（リーダー立ち絵と同じ枠） |

`unity/Assets/Resources/Audio/bgm/CREDITS.md` に出典を残す（PeriTune はクレジット任意だが敬意として）。

## 5. 決め方（工程）

1. **試聴シート** `docs/design/audio/bgm-sheet.html` をブラウザで開く（14 場面×3〜4 候補＝55 曲、YouTube 埋め込み）。場面ごとに「採用」を1つ選び「選んだ結果をコピー」→ AI に貼る。
2. AI が PeriTune から該当曲のループ版を落として `Resources/Audio/bgm/<鍵>.ogg` に置き、`audio.json` を書き換え、CREDITS を更新（`scripts/audio-fetch.mjs` を作る予定）。
3. SE は `audio.json` の合成音のものから順に、効果音ラボで「欲しい音（言葉）」に合う素材を AI が候補3つ用意→ユーザーが聴く。
4. 実機で1ラン通して、うるさい／足りないを `📝` メモに残す。
5. 面白さ検証の後にボス・タイトルの発注判断。

## 6. 配管（2026-09-13 実装）

- `Audio.cs`: `audio.json` を読む `BgmFor(scene)`／`Key(eventName)`／`Ui(action)`／`HasKey`。素材名の直書きを画面から追放。
- `GameRoot.UpdateBgm`: 場面名→表。elite/rest/lost の落とし先。
- `Presenter.TableSound`: 表にあるイベントは音だけ鳴らす（0.12s の間）。
- `BattleView`/各画面: `Audio.Key`/`Audio.Ui` 経由。

## 7. 選定の記録

- **第1ラウンド（2026-09-13）**: title=World_OP2／map3=Augury／battle1=Prairie4／elite=Puppeteer（Thunderclap も好評＝控え）／boss1=TaishoRoman_Battle（ユーザー指名）／boss2=EpicBattle_Deity／boss3=Rituale Machina。実機に配置済み（`Resources/Audio/bgm/`・CREDITS.md）。
  map2 の Sylviranda は **BOOTH ¥200 の有料曲**（フリー素材でない）＝購入判断待ち。
- **第2ラウンド**: map1・rest・battle2・battle3・won・lost を `docs/design/audio/bgm-sheet-2.html` で再提示（前回と重ならない24曲）。
- SE はシートに選択の仕組みが無かった（一覧のみ）。次は署名音3つから効果音ラボの候補を試聴できる形にする。

## 8. SE の入手元を Sonniss に変更（2026-09-13）

効果音ラボの候補（`se-sheet.html`）は「チープすぎる」で撤回。裁定＝**Sonniss GDC バンドル（無料）＋必要なら からくり だけ購入／予算は無料のみ／試聴は鍵ごとに候補3〜5のシート**。

**バンドル内の当てになるライブラリ**（GDC 2021-2023 の目録 `docs/design/audio/sonniss-tracklist-2018-2024.json` から。目録は各ライブラリ4件だけの抜粋で、実体はもっと多い）:

| 用途 | ライブラリ（供給元） |
|---|---|
| **からくり（仕込む・鳴る・期限切れ）** | Steampunk Mechanical Sounds（BluezoneCorp）／Steampunk Mech SFX（David Dumais）／Clocks and Mechanics（Justsoundeffects）／Essentials 02 Clocks（InspectorJ）／Nuts And Bolts（344 Audio＝ラチェット） |
| 札（プレイ・ドロー・伏せる） | Board Games – Gamedesigners Audio Toolkit 01（CB Sound Design＝トランプの擦り）／Ultimate Chess SFX（344 Audio＝駒・掛け金）／Turning The Page（Sonic Bat） |
| 打撃・斧・盾 | Melee Weapons Sound Effects Pack 1（David Dumais）／Epic Impacts Vol.1（344 Audio） |
| 魔法・打ち消し・消滅 | Magic Sound FX Pack 2（David Dumais）／Druid Magic（Rogue Waves）／Whoosh And Push（CB） |
| 回復・鈴 | Dreamcatcher（CB＝Enchanting Bells）／Cymbals From Hell Vol.3（344＝chimes） |
| 金・レリック | Essential Sounds Vol.01 Coins（CB）／Videogame Foley Essentials Vol.I（Sonic Bat） |
| 撃破 | Monster Sound FX Pack 2（David Dumais） |
| 勝敗ジングル | **RPG Orchestral Essentials (Music FX)**（InspectorJ＝Discovery／Failure／Mystery のオーケストラ短句） |
| 焚き火 | Pure Nature Ambiences（Campfire） |
| UI | Organic User Interface（344）／Retro Game SFX（RYK） |

**入手の手順**（ダウンロードは Cloudflare で curl から通らないので人が落とす）:
1. https://sonniss.com/gameaudiogdc の「GDC 2021-2023」14 パート（zip。合計 30GB 級。トレントが速い）を Windows の1フォルダへ（例 `C:\Users\yosuke\Downloads\sonniss\`）。
2. AI が WSL から zip の一覧を読み、上の表のライブラリだけを抜き出して鍵ごとに候補3〜5を並べた試聴シートを作る（前回と同じ形・ローカル再生）。
3. 採用を選んだら ogg に変換して `Resources/Audio/sfx/<鍵の音名>.ogg` へ。CREDITS に出典（Sonniss はクレジット不要だが記録として）。
