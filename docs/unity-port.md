# Unity移植作戦 v1（2026-08-24）

移植の一次資料。方針変更時はまずこのファイルを更新する。

## 0. 前提と資産評価

このプロトは初日から移植前提で設計されている。移植コストの実態：

| レイヤー | 規模 | 移植方法 |
|---|---|---|
| `engine/`（純ロジック） | 約2,200行 | **C#へ手書き移植**（本作戦の主対象） |
| `engine/` テスト | 約1,500行・134件 | NUnitへ機械的に翻訳（仕様書として全件移す） |
| `data/*.json` | 約2,200行 | **無変更でそのまま搭載**（設計どおりの共通資産） |
| `sim/`（ボット） | 約320行 | 当面移植しない（バランス検証はTS側の役目のまま） |
| `ui/`（React） | 約1,600行 | 移植しない。**Unityで作り直し**（薄いView＝状態を読んでコマンドを投げるだけ、は同型） |

最大の武器は**決定論**：同じシード＋同じコマンド列＝同じ結果。これが言語間の等価性検証（ゴールデンマスター）を可能にする。

## 1. 戦略の核：ゴールデンマスター検証

「移植が正しいか」を目視やテストの書き直しだけに頼らず、**TS側で大量のリプレイを吐き出し、C#側で同一再生できることを機械照合する**。

1. TS側にダンパーを追加：`(seed, mode, leaderId, コマンド列) → イベントログ型列ハッシュ + 最終状態ハッシュ` をJSONLで出力（simのボット対戦を流用して数千戦ぶん生成）
2. C#側にリプレイランナー：同じ入力を食わせてハッシュ照合
3. **全件一致するまで移植完了と見なさない**。以後もルール変更のたびに再生成→再照合が回帰テストになる

これにより移植バグは「どのシードの何手目で分岐したか」まで自動で特定できる。

## 2. 移植方式の比較

| 方式 | 内容 | 評価 |
|---|---|---|
| **A. 手書きC#移植＋ゴールデンマスター（推奨）** | engineをC#に翻訳。等価性はGMで担保 | C#ネイティブで演出・最適化・IL2CPPと素直に繋がる。翻訳コストはGMがあるので安全に払える |
| B. JSエンジン埋め込み（Jint等） | TSビルド成果物をUnity内のJSインタプリタで実行＝単一ソース | 二重メンテ消滅が魅力だが、デバッグが境界越しになり、GC・マーシャリング・依存が増える。カードゲームの規模ならAの翻訳コストの方が安い |
| C. 自動トランスパイル | TS→C#変換ツール | 実用品質のツールが無い。不採用 |

## 3. 二重メンテの回避（運用ルール）

最大の戦略リスクは「TSとC#の2つのエンジンを両方直し続ける」こと。回避策：

- **移植後もルールの実験室はTS**（ブラウザ即プレイ＋sim1000戦の検証力はUnityでは再現コストが高い）
- ルール変更のフロー：TSで変更→sim検証→ゴールデン再生成→**C#へ追随移植**→GM照合（追随はほぼ機械的作業）
- リリース準備期に**C#へ一本化**を宣言し、TS側は凍結・アーカイブ

## 4. 着手タイミング：凍結チェックリスト

全面移植はルールが暴れている間はやらない（今日だけで威嚇・状態異常・新敵5体が入った）。**→ 2026-09-06 に前倒しで P1 を実施**（緑の完成度が「検証に載る」段階に達したため。ルールはまだ動くので §3 の追随フローで回す）。
ただし**リスクの芯（RNG・決定論）だけ先にスパイク**して、移植可能性を早期に証明しておく。

全面移植のゲート（TS側で先に終わらせる）：
- [ ] チャオス（衝動エンジン化）の構造修理
- [ ] レリックの実装（フックの口は開いている。ルールに関わるため必ずTS側で先に）
- [ ] マップ分岐（作るなら）・エリート/イベント（作るなら）
- [ ] カードプール・敵の「これで面白い」判定（手動プレイテスト）
- [x] 世界観・用語の確定（2026-08-24 済み）

## 5. C#設計マッピング

| TS | C# | 備考 |
|---|---|---|
| `GameState`（イミュータブル＋spread） | `record` + `with`式 | Unity 2022.3+/Unity 6はC# 9対応。カードゲームの割当頻度ならGC問題なし |
| 判別共用体（`Command`/`GameEvent`） | 抽象recordの階層＋`switch`式パターンマッチ | TSのswitch網羅性はC#でも同様に効く |
| `DeclarativeEffect`（効果は文字列キー） | 文字列キーのまま＋switch解決 | データ駆動を維持。enum化は後でも可 |
| `readonly T[]` | `IReadOnlyList<T>` / `ImmutableArray<T>` | |
| `types.ts` | C#クラス定義（一次資料からの手変換） | 既定方針どおり |
| JSON読込（import） | `TextAsset`＋Newtonsoft Json.NET（Unity公式パッケージ） | IL2CPP実績が厚い。フィールド名はcamelCaseのまま |
| `rng.ts`（mulberry32） | `uint`演算で厳密移植 | **最重要の罠**（§6） |
| Vitest 134件 | NUnit | 機械的に翻訳。GMと二重の安全網 |

## 6. RNG移植の罠（スパイク第一対象）

`rng.ts` はJS特有の整数強制が絡む：
- `Math.imul` = 符号付き32bit乗算、`>>> 0` = ToUint32、`t + imul(...)` はdouble加算後にビット演算でToInt32
- `0x6d2b79f5 * (counter+1)` はdouble乗算（counterが約490万を超えると精度落ちだが、実測の消費量では到達しない。C#は`ulong`で厳密計算＝同値）
- `/ 4294967296` のdouble除算、`Math.floor(v * n)` はIEEE754でC#と同一

**スパイク手順**：TSから乱数列10万個（`next`/`nextInt`/`weightedIndex`/`shuffle`結果）をダンプ→C#実装と全一致を確認。ここが通れば決定論の残りはロジック忠実性だけの問題になる。

## 7. Unityプロジェクト構成

```
unity/                      # 同一リポジトリ内に併設
├── Packages/
│   └── com.deckrogue.engine/   # エンジン＝UPMローカルパッケージ
│       ├── Runtime/            # 純C#。asmdef: noEngineReferences=true
│       │   └── (combat/effects/run/content/rng/types...)
│       └── Tests/              # NUnit（EditModeテスト）
├── Assets/
│   ├── Game/               # MonoBehaviour/UI層（状態を読んでコマンドを投げるだけ）
│   ├── StreamingAssets/data/   # *.json をTS側からコピー（ビルド時同期スクリプト）
│   └── ...
goldens/                    # ゴールデンマスターJSONL（TS生成・C#照合の共有物）
```

- **`noEngineReferences=true` が oxlint の React import 禁止と同じ役割**：エンジンからUnityEngineへの参照をコンパイラレベルで遮断（純ロジックの担保を移植先でも機械化）
- エンジンは純.NETなので、Unityを起動せず `dotnet test` でNUnit＋GM照合をCI実行できる（GitHub Actions継続）
- Unityバージョン: **Unity 6 LTS**。モバイルはAndroid先行（既存アプリの市場）、IL2CPP
- UI: **uGUI**（カードゲームの実績・情報量。UI Toolkitは見送り）。イベントログ→演出キューの変換口だけ設計しておく（演出自体は製品段階）

## 8. フェーズ計画

| フェーズ | 内容 | 目安 | ゲート |
|---|---|---|---|
| P0 スパイク | RNG厳密移植＋乱数列10万個一致＋戦闘1本のリプレイ一致 | 1〜2日 | **RNG部分は完了（2026-08-24）**: `goldens/rng-golden.json` 全124,100値がC#とbit-exact一致（next×10万/nextInt×1万/weightedIndex×1万/shuffle200順列）。実行: `~/.dotnet/dotnet run --project unity/EngineTests -- goldens/rng-golden.json`。戦闘リプレイ一致はP1冒頭で実施 |
| P0.5 下地 | **済（2026-09-03）**: ゴールデンマスター基盤（`engine/golden.ts` の要約+FNV-1a・`sim/golden-driver.ts`・`npm run goldens` / `goldens:verify`・`goldens/runs/` 8本）／`types.ts`→C# record 生成（`npm run gen:csharp` → `unity/.../Generated/Types.g.cs`。record 140・定数クラス 9・判別共用体 3）／`unity/README.md` に契約を明文化。**エンジン本体の翻訳は始めない**（曲線パッケージでルールが動いている間は追随コスト） | — | 済 |
| P1 エンジン移植 | engine全域→C#、NUnit 134件、GM全件一致 | 3〜5日 | **エンジン翻訳は済（2026-09-06・ユーザー指示「いったん緑をUnityに移植して」）**: `src/engine/` の全モジュール（UI・計測用の summary/traits/analysis を除く）を `unity/Packages/com.deckrogue.engine/Runtime/*.cs` へ手書き翻訳（約8,800行・5担当の並行翻訳＋結合）。**ゴールデン8本1,105手（緑4本485手＋青/黒/赤/白 各1本）が全手一致**。他色の効果86種も全部移植（NotImplemented ゼロ）。規約は `unity/PORTING.md`。NUnit へのテスト翻訳は未着手（当面はゴールデンが安全網）。ルール変更後は TS→`npm run goldens`（8本）→C# 追随→`dotnet run -- verify`（2026-09-06 に人間ラン#7 の裁定4件で初めて回した） |
| P2 Unity骨格 | UPM構成・JSON読込・CI（dotnet test） | 1〜2日 | P1 |
| P3 戦闘UI | Web版と機能同等の戦闘画面（セットアップ→戦闘→勝敗） | 1〜2週 | P2 |
| P4 ランUI | ドラフト連戦・リーダー選択・報酬ピック | 1週 | P3 |
| P5 製品化 | 演出・モバイル入力・ストア対応・外注アート組込 | 別計画 | 面白さ検証済み |

合計（P0〜P4）：**実働3〜5週間相当**。エンジン翻訳・テスト翻訳・GMハーネスはClaude Codeで大部分を自動化できる前提の見積り。

## 9. リスクと対策

| リスク | 対策 |
|---|---|
| RNGの言語差で決定論が壊れる | P0スパイクで最初に潰す。乱数列レベルの照合 |
| 二重メンテの泥沼 | §3の運用ルール（実験室=TS、追随=機械的、最後に一本化） |
| IL2CPP/AOTでのJSONやreflection問題 | Newtonsoft Unity公式パッケージ＋必要ならlink.xml。エンジン本体はreflection不使用で書く |
| UnityのC#バージョン制約 | Unity 6 LTS（C# 9）。record非対応環境は考慮しない |
| UI再実装の工数超過 | Web版のUI構造（薄いView＋コマンド）をそのまま設計図にする。演出はP5まで封印 |
| 移植後にルールが大きく変わる | 凍結チェックリスト（§4）を通過するまでP1に入らない |

## 10. 決定事項と未決事項

決定（2026-08-24）:
- **移植方式はA案（手書きC#＋ゴールデンマスター検証）で確定**
- **着手はP0スパイク先行**（全面移植は§4の凍結チェック通過後）
- **エンジンはUnityに一本化**（Godot / libGDX 案はクローズ）

未決:
- マップ分岐・エリート・イベントを製品スコープに入れるか（入れるならTS側で先にプロト）
- ランメタ進行（アンロック等）の有無（製品企画。エンジン外なのでUnity側で自由に足せる）

## 11. P2/P3 の最小版（2026-09-06）

- **実機検証の導線（2026-09-07・Unity 6000.6.0f1 が Windows 側に入った）**: `scripts/unity-win.sh {compile|verify|sync}` が WSL から
  Windows の Unity.exe をバッチ起動する（作業コピー `C:\Users\yosuke\deck-rogue-unity` へ rsync → `-batchmode -nographics -quit`）。
  `verify` は `Assets/Editor/BatchTools.cs` の `VerifyGoldens`（EngineTests の照合ループと同じ手順を Unity 内で回す＝Mono 上でも
  TS と同じハッシュを出す確認）。初回の学び: `com.unity.modules.vr` は 6.6 に無い（manifest から除去）。
  **結果（2026-09-07）**: Input System を同梱版 1.20.0（1.11.2 は 6.6 で TreeViewItem の obsolete エラー）・Newtonsoft 3.2.2・uGUI 2.6.0 に固定し、
  エンジンの `string.EnumerateRunes`（Unity のランタイムに無い）をサロゲート対応ループへ直した後、**コンパイル0エラー（Assets/Game 2,400行も一発）・
  Unity 内ゴールデン照合8/8一致・プレイモードのスモーク（`play`）でセットアップ→ラン開始→進路→戦闘3ターンがエラーなし**（Text 26→61・Button 19→13 と画面が組み替わり、HP 72→59＝敵が殴っている）。
  残る未確認は「絵」＝描画（-nographics では見えない）。Hub で作業コピーを開いて Play すれば見られる。
- **URP へ移行（同日・ユーザー報告「Built-In Render Pipeline は非推奨」）**: URP 17.6.0 を入れ `setup-urp` でアセット生成→割当。学び: `UniversalRenderPipelineAsset.Create()` は
  レンダラー欄を空で作る（プレイモードのスモークが「Default Renderer is missing」を10件検出）→ `UniversalRendererData` を `ResourceReloader.ReloadAllNullIn` で埋めて保存し、
  SerializedObject で `m_RendererDataList[0]` に差す（GUID を保つ）。修正後のスモークは errors=0。バッチ用とGUI用の作業コピーは別フォルダ（同じプロジェクトを2つの Unity は開けない）。

- P2 骨格: `unity/` がそのままプロジェクトルート。`Packages/manifest.json`（Newtonsoft 3.2.1・uGUI・Input System）、`ProjectSettings/ProjectVersion.txt`（Unity 6）、エンジン asmdef の Newtonsoft 参照、`Assets/StreamingAssets/data/`（`npm run unity:sync`）。開き方は `unity/README.md`。
- P3 最小: `Assets/Game/`（GameRoot / UiKit / CardText / CombatScreen / RunScreens・約2,400行）。シーン・プレハブ無しで空シーンの Play から起動し、セットアップ→マップ→戦闘→報酬/レリック/焚き火/工房/ショップ/イベント→勝敗が動く設計。Unity API のスタブで dotnet コンパイル 0 error、CardText 全410札とヘッドレス走行で例外ゼロ。**実機コンパイルは Windows 側が初**。
- 次: Claude Code から Unity をバッチ起動できる環境（`Unity.exe -batchmode` でコンパイル・テスト・スクショ）を作ってから、画面1つ＝1タスクで積む。TextMeshPro＋Noto Sans JP・Android IL2CPP（反射の link.xml）は製品段階の前に。

## 12. 製品版UIの計画（2026-09-07 ユーザー「製品版並みのプレイしやすいUX/UIが欲しい」→ ask_user 裁定）

裁定: **uGUI・コード生成＋テーマ**（UI Toolkit・プレハブ手組みは不採用）／解像度の基準は**本家と同じ**（1920×1080 の16:9基準・ドット絵は整数倍）／
画像は **PixelLab のドット絵を最終形**とし、**プレースホルダー先行→後で差し替え**（`Assets/Resources/Art/<種別>/<id>.png`・発注書 `docs/pixellab-assets.md`）／
着手は **M1 から、発注書は並行**。

構造の柱＝**演出キュー**: エンジンの状態は一瞬で確定し、画面は eventLog（CardPlayed・DamageDealt・BlockGained…）を順に取り出してトゥイーンで見せる
（StS のアクションキューと同型）。純ロジックのまま「カードが飛ぶ・数字が弾ける・敵が揺れる」を足せる。

| 段 | 中身 | 目安 |
|---|---|---|
| M1 目を作る | Windows プレイヤーを自動操縦で起動して各画面の PNG を吐く（`scripts/unity-win.sh build` → `shots`。`Assets/Game/Autopilot.cs`）。TextMeshPro＋Noto Sans JP（`Assets/Resources/Fonts`・OFL）。テーマ（配色・9スライス枠・カード枠・アイコンをコードで生成＝PixelLab と同名同寸のプレースホルダー）。トゥイーン基盤と演出キューの骨格 | 1〜2日 |
| M2 戦闘画面 | 手札の扇とホバー拡大、ドラッグで対象指定＋矢印、敵カードと意図アイコン、ダメージ数字・シェイク・フラッシュ、ターン開始/終了バナー、伏せ・確認ウィンドウの見せ方、キーワードのツールチップ、SE のフック | 1〜2週 |
| M3 ラン画面 | マップ（パン・経路ハイライト）、報酬ピック、ショップ、焚き火、工房、イベント、レリック帯、デッキ閲覧、設定と続きから | 1週 |
| M4 仕上げ | 画面遷移、SE/BGM（フリー素材）、チュートリアル、Android のタッチ、性能 | 随時 |

作法: 画面ごとに「作る → `shots` で自己検証（Claude が PNG を見る） → ユーザーが Hub で Play して手触りを採点」を1サイクルにする。エンジンは触らない（ゴールデン8本が守る）。

**M1 済（2026-09-07）**: `build`→`shots` で7枚の PNG（セットアップ／マップ／戦闘3ターン）を Claude が読めた。TextMeshPro＋Noto Sans JP（動的 SDF）で日本語が出る。
Theme（9スライスのボタン枠・16px アイコン13種）、Tween（移動・拡縮・フェード・パンチ・浮き文字）、Presenter（イベントログ差分→ダメージ/ブロック/回復の浮き文字とパンチ。
レイアウト確定後に的の座標を読む・LayoutGroup の子は位置でなく拡縮で揺らす・連続演出は0.12秒ずつずらす）。基準解像度 1920×1080（旧画面は 1.5 倍の入れ物）。
学び: Rebuild 直後は LayoutGroup 未計算＝的が原点にいる（`Canvas.ForceUpdateCanvases()`）／実行時生成の `TMP_SpriteAsset` は旧形式の移行処理で落ちる（版と表を先に用意）／
スモークの空画面判定は TMP_Text を数える。残: インラインアイコンの検証（M2 の最初の実機ランで）。

**M2 進行中（2026-09-07・ユーザー「プレイ画面の構造も変更していい。前時代的。モダンな戦闘UIを」）**: `Assets/Game/BattleScreen.cs` を新設し戦闘を丸ごと置き換えた。
上=状況バー（幕/行・HP/E/G・レリック・ログの引き出し）／中=戦場（左にリーダー・伏せ場・置物、右に敵。頭上に意図アイコン＋数値、分岐など短縮表示に無い情報だけ詳細行。スプライトは
`Creature`（id のハッシュから左右対称のドット絵を生成するプレースホルダー）、HP/ブロック、状態チップ、特性）／下=扇状の手札（`CardView` 200×290。ホバーで持ち上げ、
ドラッグで敵に落として対象指定→即プレイ、戦場に落としてプレイ、右クリック/ホバーの「伏せる」、選択式はポップで選ぶ）、山札/捨て札（クリックで一覧）、ターン終了。
モーダル: 確認ウィンドウ（候補の伏せ札をカードで並べ「発動」、実値と分岐反転の警告）、対象選択の金帯、追加コストのピッカー。演出: カードの残像が戦場へ飛ぶ、敵フェーズは
古い盤面の上で順送り（攻撃0.4秒・バナー0.6秒、入力は塞ぐ）→終わってから組み直す、ダメージ/ブロック/回復の浮き文字、敵の踏み込みと被弾のパンチ、ターンバナー。
ツールチップ（敵の意図・特性、状態チップ、カードの用語解説＝`KeywordHelp.g.cs` を `scripts/gen-keyword-help.ts` が TS から生成）、カードのダメージ予測（対象が決まっている時、
`Effects.DamageBreakdownOf` の実値）。自動操縦 `shots battle <seed>`（シード6=2体編成＋絡み蔦＋茨の返しの初手）で素・ホバー・ログ・敵ツールチップ・山札一覧・モード・対象・伏せ・
敵フェーズ途中・確認・温存・翌ターンの各状態を撮る。学び: CardView のアンカーは親の中央（手札の位置ずれ）／`StartRun` は入力欄のシードを優先（自動操縦は `SetSeed` で欄も更新）。

**M2-4 残留UI（同日・ユーザー「まだまだかなり荒い」→ ask_user: 荒さは動き/見た目/レイアウトの全部、進め方は動きから）**: `Assets/Game/BattleView.cs`。
コマンドごとの丸ごと作り直しをやめ、敵パネル・リーダー欄の入れ物と手札のカード（uid ごと）を持ち越す。手札は差分＝消えた札は行き先へ飛ぶ（プレイ→対象の敵→捨て札、伏せ→伏せ場、置物→リーダー、
消滅→砕ける、ターン終了→捨て札）、新しい札は山札から扇へ、並び替えは滑る。HP バーは `HpBarInfo` で前の値から滑り、順送りの敵フェーズでは被弾のたびに先に減る（`NudgeEnemyHp/NudgePlayerHp`）。
学び: エンジンは敵フェーズ中も手札を持つ（全捨ては敵ターン終了後）ので TurnEnded で手札を飛ばすと同じ札が山札から戻ってくる＝差分だけに任せ、捨てが飛び終わってからドロー（0.28秒）。
見た目のパス（同日）: 背景を縦グラデ＋ビネット、足元の影、コスト玉・レア度の宝石・カードの紋章（id のハッシュ）をコード生成（`ThemeFx`）、カードの面を名前板/紋章の窓/本文に、アイコンは 32/48px の整数倍、手札の左にエナジー玉。

**M2-5 音と攻撃演出（同日・ユーザー「SEとかBGMつけてほしい。2を進めて」）**: `Synth.cs` が効果音18種と BGM（幕ごと battle1-3／boss／map／title＝8小節ループ。矩形波・三角波・ノイズのレトロ調）を
コードで合成し、`Audio.cs` が再生（AudioSource プール・ピッチ揺らぎ・ホバー音の連打抑制・BGM のクロスフェード・PlayerPrefs の音量）。素材は `Assets/Resources/Audio/sfx|bgm/<name>` を置けば差し替わる（絵と同じ規約）。
演出: 敵の攻撃＝踏み込み（`Tween.Lunge`）→自分に斬撃の筋→画面揺れ（`ScreenRoot` を揺らす）→赤い点滅、自分の攻撃＝カードが飛ぶ→斬撃＋白い点滅＋大きい時は揺れ、ブロック＝盾が膨らむ、回復＝ハート、撃破＝白く光って沈む＋音、ターン/敵の番の音、カードのドロー/プレイ/伏せ/ボタン/ホバーの音、勝敗のジングル。
素材（同日・ユーザー「アセットストアからいけない？」）: Asset Store は Editor からしか落とせない（ユーザーが Import → `scripts/unity-win.sh pull` で回収 → `Audio.Map` で対応付け）ので、効果音は CC0 の Kenney（rpg-audio / impact-sounds / ui-audio / music-jingles）を直接落として `Resources/Audio/sfx/` に同梱した（番号違いは `_2` `_3` で鳴らすたびに選ぶ）。BGM は Kenney に無いので合成のまま＝Asset Store か CC0 のループ集の候補。ジングル（heal/buff/win/lose）は聴かずに選んだので要差し替え確認。
→ ユーザー「cc0」: OpenGameArt を CC0 縛りで検索し、各ページのライセンス欄で CC0 を確認した10曲を同梱（title=Town Theme RPG／map1=8-bit Forest Theme／map2=Cave Theme／map3=Dark Forest Theme／
battle1=8-Bit Battle Loop／battle2-3=Juhani Junkala Chiptune Adventures Stage 1-2／boss1=Great Boss／boss2=Junkala Boss Fight／boss3=8-bit Slay The Evil。約22MB）。Junkala の Action パックは WAV 49MB なので見送り。


**M3 ラン画面（2026-09-07・ユーザー「m3」）**: 戦闘以外の画面を戦闘と同じ文法（`ScreenRoot` 直下 1920×1080・`Theme`/`CardView`/`Tween`/`Audio`）で作り直した。
`TitleScreen`（15人のリーダーを 5×3 の立ち絵カードで並べ、右に説明・初期デッキ・シード・難易度・開始）／`MapScreen`（本家式の縦スクロール地図。
7列格子にノードを置き線で結ぶ。進める道＝緑の光る線＋ノードの発光、通ってきた道＝金、現在地＝リーダーの小さな絵、種別のアイコン8種と凡例、ホバーで中身の説明）／
`RewardScreen`（カード報酬＝大きなカード3枚＋「取る」、レリック＝紋章・名前・レア度・説明のパネル）／`CampfireScreen`（「休む」「鍛える」の二択の大札→鍛えるはデッキのグリッド、
ホバーで「鍛えると→」）／`ShopScreen`（棚にカードと値札、右列にレリックと除去/鍛えるのサービス、選ぶとデッキのグリッド）／`WorkshopScreen`（左にデッキのグリッド＝選ぶ/外す、
右に素材2枠＋合成結果のカード＋注記＝ブラウザ版の追従パネルと同形）／`EventScreen`（中央のパネルに題・物語・選択肢。効果のヒントを小さく添える。カード指定の選択肢はグリッドへ）／
`EndScreen`（走破/敗北・戦績・レリック・最終デッキ）。共通部品 `RunUi`（上部バー＝幕/行・HP・G・デッキ一覧ボタン・レリック帯、デッキ一覧モーダル、カードのグリッド、下部ボタン、通知行）。
レリックの絵は `ThemeFx.RelicGlyph`（id のハッシュから左右対称の紋章。`Resources/Art/relics/<id>` があればそれ）。地図のアイコン（骸骨・王冠・槌・？・宝箱・旗）を `Theme` に追加。
自動操縦 `shots run <seed>`＝戦闘を自動で勝ち（使える攻撃札を撃ってターン終了・確認は温存）ながら経路上の画面を撮り、踏まなかった工房は状態を差し替えて撮る。
学び: 戦闘→報酬の遷移で `Presenter` が戦闘のバナーを重ねていた（戦闘フェーズ以外では演出を止め FX 層を掃除する）／`CenteredButton` は行の入れ物付きなので固定位置には向かない（`RunUi.BottomButton`）／
絵文字（レリックの sprite・✕）は Noto Sans JP に無く豆腐になる＝文字でなく絵で出す／`GridLayoutGroup` の一覧は `Scroll` の縦レイアウトを外してから付ける。
旧 `RunScreens.cs` はシード入力欄の生成（`MakeSeedField`）だけが残り、画面としては使われない（次の整理で削る）。残: 設定（音量）、続きから、マップのパン操作の手触り、PixelLab の絵の差し替え。

**M2/M3 の肌を「絵本」に（2026-09-07・デザインカンバス「戦闘画面 作り直し」第5版の決定を実装）**: `Assets/Game/PaperFx.cs` に紙の9スライス（角丸の SDF で外から淡い線1・紙3・墨2、1テクセル=1px）、
水彩のにじみ・紙の粒（Tiled）・貼り絵の縁（ドット絵の影絵を4倍解像度で膨らませて紙色に。縁≈3px）・タイプのしおり・マスキングテープ・円盤とリング（エナジーの太陽）を生成。
`Theme.Panel/Button/CardFrame` は紙に差し替え（`UiKit.Frame` は名前が paper で始まる絵を1倍で貼る）、`UiKit` は色を紙（舞台の上）と墨（紙の上）の2系統に、フォントは本文 Klee One／名前 Kaisei Decol（`Resources/Fonts`・OFL）。
`CardView` は作り直し（しおり=タイプにコスト、星=レア度、紙の台紙に挿絵 `Art/cards/<id>.png` を2倍か紋章16×16を3倍、左下の剣・右下の盾のにじみの札＝効果から導出 `RoleLabels`）。
`BattleScreen` は上部の札・紙の吹き出しの意図・名前札・紙のHPバー・貼り絵の敵とリーダー・伏せ札の裏・付箋の置物・紙の山札札・エナジーの太陽・紙の頁のモーダル・紙のログ。
ラン画面は Theme の差し替えで追随し、紙の上の文字を墨に一括変更（RunUi.TopBar は札に）。学び: レイアウトの外に置く `UiKit.Icon` は sizeDelta を明示しないと 100px になる／紋章は '-' を墨・'#' を紙にして線画にする。

### 舞台（段階1）— 2026-09-07

背景を UI から分離した。`Stage.cs` が Main Camera を舞台カメラ（正射影・URP のポスト処理 ON）にし、Screen Space - Camera の舞台キャンバスに幕の背景を描く。
ランタイム生成の VolumeProfile（Bloom threshold 0.72 / intensity 1.2、Vignette 0.3、ColorAdjustments の colorFilter を幕で切替）と、ワールド空間の ParticleSystem（蛍・ほこり・葉。素材は `Resources/Materials/ParticleSprite.mat`＝Sprites/Default をビルドに含めるため）。
`BattleScreen.BuildBackground` は舞台へ委譲し UI 側には何も置かない。URP のレンダラーに `PostProcessData` が無いとポスト処理は描かれないので `UrpSetup` が割り当てる（`scripts/unity-win.sh setup-urp` で反映）。
副産物のバグ修正: 戦闘の最初の組み立てで直前の画面（マップ）が ScreenRoot に残っていた（不透明な背景で隠れていた）→ `Rebuild` が Battle 未生成の戦闘でも掃除する。

### 舞台＝HD-2D ジオラマ — 2026-09-07（段階1 の「板の背景」を置換）

ユーザー「やっぱ3D表現がほしい」→ ask_user 3択で「HD-2D ジオラマ」（＝「オクトラ風なら」）。オクトラの構造そのもの: 舞台だけ本物の3D、キャラは2Dドット。
- `Stage.cs` を全面作り直し。Main Camera を透視（FOV 30°・見下ろし 12°）にし、地面 y=0・キャラの立つ線 z=0・1 unit=焦点面で 100px の座標系で
  コード生成の箱庭（地面と道・段々の台地と崖・遺跡の柱・板の木/茂み/岩・ランタン・空の板・月・地平線の木立・低い霧）を組む。幕ごとにパレット。
- **キャラは UI の矩形に追従するビルボード** (`Stage.BindUnit`): 焦点面（視線に垂直・距離 D）上では画面ピクセルと world が 1:1 なので、uGUI の
  sprite 枠（`FitPixel` の 4 倍矩形）をそのまま焦点面へ写す。整数 px に丸めるので「1ドット=画面4px」が保たれ、被写界深度も焦点面では 0。
  Image は非表示にして色（生死・点滅）だけ読む。名前札・HPバー・吹き出しは UI の既存レイアウトのまま。Tween.Lunge/Punch は矩形が動くので自動で追従。
- 影: 月光の平行光（Soft）。ドット絵の板も `StageUnit.shader` の ShadowCaster パスで影を落とす（`_ALPHATEST_ON` を #define して UnlitInput + ShadowCasterPass を流用）。
  被写界深度は Bokeh（focalLength 300 / aperture 1.6 で強めのティルトシフト）。DepthOnly パスも持つ。
- 画面揺れは `Stage.Shake`（カメラ）へ移し、紙の UI は揺れない。被弾の点滅は `Stage.Flash`（シェーダの `_Flash`）。
- 素材の差し替え口: `Art/tiles/act<N>_{grass,dirt,stone,cliff}.png`（ArtImporter が `/Art/tiles/` を Repeat に）・`Art/bg/act<N>.png`。
  URP Lit を確実にビルドへ含めるため `Resources/Materials/Diorama.mat`（Lit の GUID 直書き）を土台に材質を作る。

