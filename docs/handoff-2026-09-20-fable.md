# 引き継ぎ（2026-09-20 夜 → Fable）

## いまの状態

- リポジトリは **コミット 86f94d2**（2026-09-18〜20 の白の仕上げ→再設計「灯」→火種の軸／人形の盤面表示／ひなた v2 と白の挿絵／**灯の器＝真鍮のランタン**／**とげは人形の攻撃に反射しない**）の上に、人間ラン#15 の所見（`docs/playtest-2026-09-20-human-run15-notes.md`・原本 `docs/playtest-2026-09-20-human-run15.md`・CLAUDE.md「敵の設計原則」行の #15）を足したもの（このコミット）。
- 配布物: Windows exe `D:\deck-rogue\DeckRogue-win`（86f94d2 相当）、APK は Google Drive `DeckRogue/DeckRogue-86f94d2.apk`（実機未確認。`DeckRogue-1d9cf02.apk` は #14 で確認済みなので残してある）。
- テスト: vitest 1040 緑・ゴールデン 13/13（TS・C#）・Unity build エラー0。
- デザインカンバス「灯の表示」: https://claude.ai/code/artifact/8ed359bb-52e0-4875-9506-961428065721（裁定＝案B 真鍮のランタン。実装済み）。

## 次の仕事＝人間ラン#15 の裁定と実装

ユーザーの言葉: **「毎戦同じようなプレイパターンで退屈」**（ひなた・難易度3・25勝・91分。面白さ 3.0／3.14／3.33）。
診断（詳細は notes §2〜4）:

1. 幕2〜3の13戦が全部「T1〜3 人形を並べる（与ダメ 5.7/T）→ T4〜 点灯の合図（号令）で削り切る」。与ダメの31%が号令・24%が人形の誘発。1戦で人形10〜18体、号令1回で平均9.6体（最大16）。
2. 灯が縛りにならない（入り20〜29/戦・払い4〜11・放出は幕3で2%）。
3. **人形経由の回復の回し**: 燭の人形×2＋真・燭の人形+（合成→鍛えで攻撃ごと回復4）×号令 → 幕2は7戦とも 83→83。
4. 通常戦 6.0／6.83／6.6T・ターン終了→次の手 中央値7.9秒（人形N体の誘発の順送り）。
5. 死に札（白盾66・灯火の一撃62・灯の矢28回未使用）、強個体1回だけ（レリック5・レア確定0）、罠は幕1で終わり、火種のクリック（89回）。

処方の候補（notes §5・**裁定待ち**。私の推しは A＋B＋E）:

| 案 | 内容 | 触る場所 |
|---|---|---|
| A | 号令で動く人形に上限（例: 灯1につき3体）か、場の人形は座席9まで（10体目で一番古い人形が消える） | `src/engine/effects.ts` `rallyRetainers`（号令の集合）・`triggerRetainersNow`／`dischargeLightRally`（2102行付近）・人形の上限なら `enterPermanent`／`summonPermanent`。C# は `Effects.cs` の同名。表示は `BattleScreen.FillDollPanel`・`Stage.DollSlots`（座席9＋「+N」） |
| B | 人形の回復は号令で鳴らない／燭の合成・鍛えで回復量を伸ばさない | `rallyRetainers` で `gainHp` を持つトリガーを除く、または `runPermanentTriggers` に「号令中は gainHp を飛ばす」。鍛え＝`src/engine/upgrade.ts`（V2 白の単位+1）・合成＝`src/engine/fusion.ts`（同名の真・化＝量の合算） |
| C | 灯が増えるのは札の回復だけ（人形の回復では増えない）／号令の灯コストを頭数比例に | `healPlayer` → `gainLight(s, 1, 'heal')`（effects.ts 400行付近）で `resolvingPermanentUid` があれば産まない。`lightCost` は `CardDef`・支払いは `combat.ts` 1002行付近 |
| D | 開幕の人形をパッシブ側へ＋人形札の半分を「置いた瞬間に殴る/守る」形に | `data/leaders.json` のひなたのパッシブ（あさひ型 `summonPermanent`）・`data/cards.white.json` |
| E | 号令・ターン開始の同種ダメージを1つの数字に束ねる＋粒の間隔を詰める | `unity/Assets/Game/Presenter.cs` `Play`／`PlaySequenced`（`DamageDealt` の gap 0.12／0.4）と `Show` の `DamageDealt`。`sourceUid`（人形）付きの連続する `DamageDealt` を束ねて「人形×9: 27」。engine は触らない |
| F | 強個体の報酬（レア確定・レリック）をマップのノードに見せる | `MapScreen.cs`・Web `App.tsx` の地図 |
| G | 火種は引いたら自動で撃つ／1クリックで全部 | 自動なら engine（`drawCards` 後に `white_spark_token` を解決＝ゴールデンが変わる）。UI だけなら `BattleScreen` に「火種を全部撃つ」ボタン |

進め方は今までどおり: 判断点は AskUserQuestion で推奨案つきで確認 → CLAUDE.md のルール表を先に直す → 実装 → `npm test` → `npm run goldens`（変わったら C# 追随）→ `cd unity/EngineTests && ~/.dotnet/dotnet run -c Release -- verify ../../goldens/runs/*.json --data ../../src/data` → Unity。
**報告はやさしい日本語で**（同日ユーザー「もっとわかりやすい日本語で」＝「与ダメの出所」「順送り」のような内輪の言葉を並べない。1文1つの事実、数字は「何が・いくつ」）。

## 検証と配布の手順（今セッションで踏んだ道）

- Unity のコードを触ったら `scripts/unity-win.sh build`（Windows プレイヤー。17〜60秒）→ `STATE="…" scripts/unity-win.sh shots state 4242`。灯籠の撮影キー: `plight=<N>`、号令 `hand=white_march_order,white_strike;plight=4;play=0;playshots=10;playevery=5`、放出 `hand=white_light_bolt,white_strike;plight=6;play=0;playshots=6;playevery=4`、得る `plight=2;endplay=1;endshots=14;endevery=8`。スマホは `SHOT_W=1920 SHOT_H=886 UISCALE=1.6`。
- `shots state` がたまに Autopilot の前で固まる（player.log が `UnloadTime:` で止まる）。バッチ側の DeckRogue.exe（`unity-batch\Build`）だけ `taskkill.exe /PID <pid> /F` して打ち直す。**ユーザーが遊んでいる `D:\deck-rogue\DeckRogue-win\DeckRogue.exe` は殺さない**（`powershell Get-Process DeckRogue | select Id,Path`）。
- APK: `scripts/unity-win.sh android`（IL2CPP。8〜10分）→ コミットしてから `C:\Users\yosuke\copy-apk.ps1` の `$dst` を新ハッシュ・`$old` を「Drive にある2つのうち古い方」に sed で直して `powershell.exe -NoProfile -ExecutionPolicy Bypass -File 'C:\Users\yosuke\copy-apk.ps1'`（Drive は最新＋1つ前だけ残す）。実機が adb に居れば `scripts/unity-win.sh install` → logcat の `E Unity`。
- Windows 版: exe が終了していることを確かめてから `rsync -a --delete --exclude '*.apk' /mnt/d/deck-rogue/unity-batch/Build/ /mnt/d/deck-rogue/DeckRogue-win/`。
- Windows のプレイレポートは `/mnt/c/Users/yosuke/AppData/LocalLow/DeckRogue/DeckRogue/reports/play-*.md`（`npm run analyze -- <md>`・ジャーナルの times で手の間隔。長い戦闘のログは300行で切れる）。

## 残っている糸

- 灯籠の絵は **コード生成**（`ThemeFx.Lantern`）。PixelLab で作るなら `docs/pixellab-assets.md`「灯の器」の条件（硝子 x7〜24・y14〜35 を暗く空ける）。
- Web プロトの灯はピル「灯 N」のまま（Unity だけ灯籠）。
- 86f94d2 の APK は実機未確認（IL2CPP）。
- 幕ボス回復75%（段4〜6）が全回復と同じだった件（#14 §2）は据え置きのまま。
