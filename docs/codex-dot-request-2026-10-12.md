# dot への依頼 2026-10-12（Claude 側から。このまま貼れる）

Claude 側で受けた共有（`docs/codex-dot-share-2026-10-12.md`）を手元のリポジトリと突き合わせ、ユーザーが 4 件を裁定しました。

## 1. 裁定の共有

1. **移植先は Godot へ一本化**（2026-10-12 ユーザー裁定）。2026-08-24 の「Unity に一本化・Godot はクローズ」は撤回。Unity の VFX 枝（`codex/hd2d-vfx-motion-20261005`）は Godot へ移し替える前提。
2. **main への合流は red/thaw が先**。dot の表示専用のパッチ（Stage41 のエンジン変更）は、red/thaw が main に入った後、その上に載せ直してください。
3. **`card-vfx.json` の赤の新札 32 枚は dot が足す**（§4 の一覧）。

## 2. お願い

- **エンジンのパッチを素材と分けて単独コミットしてください。** worktree `deck-rogue-stage41-codex-20261011` の未コミット 2,708 ファイルのうち、`src/engine/{combat,effects,events,gears,types}.ts`・`unity/Packages/com.deckrogue.engine/Runtime/{Combat,Effects,Events,Gears}.cs`・`Generated/Types.g.cs`（＋`multi.test.ts`・`presentation-source.test.ts`）だけを 1 コミットに。red/thaw と同じファイルを触っているので、衝突の解消を 1 回で見通せます。
- **Godot のエンジンとデータは red/thaw の合流後に写し直してください。** 今の Godot は main 25270c9 の C# 25 ファイルと JSON 14 ファイルの写し（カード 444・敵 89・ゴールデン 2,935 手）。red/thaw で変わる範囲は §3 のとおりで、ゴールデン `goldens/runs/leader_red-9101.json` は 183→249 手になります。写し直したら Godot 側でもゴールデン 13 本の照合を通してください。
- **以後の運用（提案）**: TS（`src/engine`）が正 → C#（`unity/Packages/com.deckrogue.engine`）へ手で翻訳し `npm run goldens` と `dotnet run -- verify` で一致を確認 → Godot へ C# と JSON を複製 → Godot でも照合。「ルールと RNG を見た目のために変えない」はそのままです。
- Types.g.cs は `npm run gen:csharp` の生成物です。手で編集せず、types.ts を直してから生成してください（合流後に両方の types.ts を足し合わせて一度生成し直す必要があります）。

## 3. red/thaw でエンジンに入った変更（衝突の見通し用）

main（25270c9）との差分（コミット済み 7 件＋未コミット。テストは除く）:

| ファイル | 追加 | 削除 |
|---|---|---|
| src/engine/combat.ts | 213 | 41 |
| src/engine/effects.ts | 87 | 6 |
| src/engine/types.ts | 57 | 4 |
| src/engine/summary.ts | 61 | 0 |
| src/engine/upgrade.ts | 20 | 5 |
| src/engine/run.ts | 14 | 1 |
| src/engine/fusion.ts | 5 | 1 |
| src/engine/analysis.ts・setany.ts・state.ts | 各 1〜4 | |
| src/data/cards.red.json | 499 | 141 |
| src/data/decks.json | 96 | 22 |
| Runtime/Combat.cs | 242 | 48 |
| Runtime/Effects.cs | 159 | 7 |
| Runtime/Generated/Types.g.cs | 100 | 5 |
| Runtime/Upgrade.cs | 22 | 5 |
| Runtime/Run.cs | 16 | 2 |
| Runtime/Fusion.cs | 7 | 1 |
| Runtime/Analysis.cs・SetAny.cs・State.cs | 各 1〜4 | |

中身（赤の解凍 2026-10-10〜12。全部ルール側）:

- **チェイン**（`CardDef.cascade`・`playCardDirect`・`cascadesThisCombat`）。出来事 `Cascaded`・`CascadeRetargeted`。置物の常在 `firstCardCascade`・`cascadeSameCost`、トリガー `onCascade`、刈り取り `dealDamagePerCascade`。
- **くべる**（追加コスト `impulseCost`・`kindleOptional`・`condition.kindled`・`PlayCard.exhaustUids`）。出来事 `ImpulseKindled`。
- **衝動の失効の行き先**（`impulseUnaffordable`＝出せる札なのに実コストが残りエナジーを上回る札は捨て札へ。出来事 `ImpulseReturned`）・燠の守り（`keepImpulseOne`・`EndTurn.keepUid`・出来事 `ImpulseKept`）・トリガー `onImpulseExpired`。
- **猛り火の点火と消灯**（トリガー `onBlazeIgnite`・出来事 `BlazeIgnited`／`BlazeExtinguished`）。
- **バーンの刈り取り**（`dischargeBurn`・`doubleBurn`・`gainBlockPerBurn`・`dealDamagePerBurn`・置物の常在 `burnNoDecay`）、新しい対象 `mostBurn`。
- `DamageDealt.nullified`（身代わりの符で無効にした時）。
- 同名合成で放出の行を 1 行に合算（fusion.ts／Fusion.cs）。
- 表示の純関数 `summary.ts`（`burnoutEnemies`・`expiryBlockPreview`・`permanentTargetNote`）。
- 鍛えるの赤の本家形（upgrade.ts）・初期デッキ（decks.json）・赤の札 84→106。
- ゴールデンは `leader_red-9101.json` だけ変わる（183→249 手）。他の 12 本は不変。

## 4. `card-vfx.json` に足してほしい赤の新札 32 枚

属性と型の決め方は dot の規定に従ってください。目安: 延焼・火力＝火、ブロックだけ＝物理、置物は出た時の演出で可。`onCascade`＝チェインの当たりのたび、`onImpulseExpired`＝衝動の失効のたび、`onBlazeIgnite`＝猛り火に点いた時。

| id | 名前 | タイプ | コスト | 効果（engine の名前）／備考 |
|---|---|---|---|---|
| red_cascade_strike | 火送りの一撃 | physical | 1 | onPlay→dealDamage 7 ／ チェイン1・common |
| red_cascade_vent | 火口開き | spell | 1 | onPlay→dealDamage 4・onPlay→drawCards 1 ／ チェイン1・common |
| red_cascade_ford | 火渡り | spell | 2 | onPlay→applyBurn 7 ／ チェイン1・common |
| red_cascade_bolt | 連火弾 | spell | 2 | onPlay→dealDamage 11 ／ チェイン1・common |
| red_cascade_shield | 連火の盾 | physical | 2 | onPlay→gainBlock 11 ／ チェイン1・common |
| red_perm_cascade_sparks | 連なる火花 | permanent | 1 | onCascade→dealDamage 3 ／ common |
| red_perm_cascade_hearth | 火継ぎの炉 | permanent | 1 | onCascade→applyBurn 2 ／ uncommon |
| red_perm_cascade_envoy | 燎火の使い | permanent | 2 | onPlay→dealDamage 3・onTurnStart→applyBurn 2 ／ チェイン1・uncommon |
| red_cascade_great | 大火送り | spell | 3 | onPlay→dealDamage 16 ／ チェイン1・uncommon |
| red_cascade_litany | 火の連祷 | spell | 3 | onPlay→dealDamage（全体） 10 ／ チェイン1・uncommon |
| red_cascade_vortex | 火の渦 | spell | 1 | onPlay→dealDamagePerCascade 5 ／ uncommon |
| red_perm_cascade_core | 連火の炉心 | permanent | 2 | onPlay→firstCardCascade ／ rare |
| red_brushfire | 野火 | spell | 3 |  ／ チェイン3・rare |
| red_twin_fire | 双つ火 | physical | 3 | onPlay→dealDamage 5 ／ チェイン2・uncommon |
| red_burn_double | 火勢倍し | spell | 2 | onPlay→doubleBurn ／ 消滅・uncommon |
| red_ash_cover | 灰かぶり | physical | 1 | onPlay→gainBlock 3・onPlay→gainBlockPerBurn 1 ／ common |
| red_fire_tongue | 火の舌 | spell | 1 | onPlay→dealDamagePerBurn 1・onPlay→applyBurn 2 ／ common |
| red_perm_smolder | 燻り | permanent | 2 | onPlay→burnNoDecay ／ rare |
| red_perm_embers_left | 燃え残り | permanent | 1 | onImpulseExpired→applyBurn 3 ／ common |
| red_perm_ash_robe | 灰の衣 | permanent | 1 | onImpulseExpired→gainBlock 3 ／ uncommon |
| red_perm_ember_keeper | 燠の守り | permanent | 1 | onPlay→keepImpulseOne ／ rare |
| red_catalyst_aoe | 散り火 | spell | 1 | onPlay→dealDamage 4 ／ common |
| red_catalyst_light | 火口の綿 | physical | 1 | onPlay→gainBlock 5 ／ common |
| red_rake | 火掻き | physical | 1 | onPlay→dischargeBurn 2 ／ common |
| red_burn_off | 焼き払い | spell | 2 | onPlay→dischargeBurn 3 ／ uncommon |
| red_popping_fire | 爆ぜ火 | spell | 1 | onPlay→applyBurn 3・onBlazeIgnite→dealDamage（全体） 5 ／ common |
| red_perm_fire_awakening | 火の目覚め | permanent | 1 | onBlazeIgnite→drawCards 1・onBlazeIgnite→gainEnergy 1 ／ uncommon |
| red_kindle | 火にくべる | spell | 1 | onPlay→dealDamage（全体） 4・onPlay→applyBurn 3 ／ くべる1・common |
| red_ash_breath | 灰の息 | spell | 1 | onPlay→gainBlock 5・onPlay→gainBlock 3・onPlay→applyBurn 2 ／ くべる1・uncommon |
| red_perm_fire_calls_fire | 火は火を呼ぶ | permanent | 2 | onPlay→cascadeSameCost ／ rare |
| red_ash_veil | 灰の帳 | physical | 1 | onPlay→gainBlock 5・onPlay→applyBurn 2 ／ common |
| red_ember_screen | 熾の衝立 | physical | 1 | onPlay→gainBlock 4・onPlay→impulseDraw 1・onPlay→applyBurn 2 ／ common |

## 5. 追伸（同日・PC でのコミットを Claude が代行した後）

```
dot へ（2026-10-12・Claude 側から）

PC 側のコミットは、dot の PC 作業が止まっているので Claude が代わりに行いました。
worktree deck-rogue-stage41-codex-20261011、枝 codex/hd2d-vfx-motion-20261005（2f31050 の上）。push はしていません。

1. コミット
- 87677aa5c3d4ce8213942cfe6d89475cb7ac7ab1 = エンジンだけ（13 ファイル）
  src/engine の combat・effects・events・gears・types と multi.test・presentation-source.test、
  Runtime の Combat・Effects・Events・Gears・Generated/Types.g.cs、EngineTests/Program.cs。
- 2a5bc32fe5e93e23312967a5537c48e55e8e5cc4 = 残り全部（2,695 ファイル）
  Game の .cs・Editor・Resources（Art／Shaders／Data＝card-vfx.json ほか）・StreamingAssets・
  docs/art-bible.md・scripts/audit-vfx-coverage.py・packages-lock.json・Unity の .meta 2,504 件。
  「採用済み」の意味は持たせていません。既定 OFF の候補はそのままです。
未コミットは 0 件です。

2. コミット前に PC で確認したこと
- vitest: 1189 passed / 1 failed / 1 skipped。失敗は src/sim/unity-colors.test（PhysicalMediumCandidateFx の直書きの色）だけで、エンジンとは別。
- npm run goldens:verify（TS）: 13 本すべて OK。
- dotnet run -- verify（C#）: 13 本すべて一致（カード 444・敵 89 の旧基準のまま）。
つまり「RNG と出来事の数は不変」は PC でも成立しています。

3. dot にお願いしたいこと
- クラウドで「エンジンだけの差分」を作り直す必要はなくなりました。87677aa をそのまま受入 commit として扱ってください。
- card-vfx.json の赤 32 枚の追加だけ進めてください（一覧は前の依頼文の §4）。
- .meta 2,504 件を枝に含めるのが dot の運用と合わなければ言ってください。main は .meta をほとんど持たないので、外すこともできます。
- unity-colors.test の 1 件は dot の側で直してもらえると助かります（規定: Unity の UI の色は PaperFx の名前から）。

4. この後
- Claude が red/thaw をコミットして main へ合流します（ユーザーの指示待ち）。
- 合流したら、合流先のコミット ID・C# エンジン 25 ファイル・JSON 14 ファイル・ゴールデン 13 本・25270c9 からの patch・SHA256 一覧を渡します。
  赤のゴールデン leader_red-9101.json は 183→249 手になります。旧基準の合格は新ルールの検証に数えない、で合っています。
```

## 6. 返信 2（同日・dot の受入基準への応答）

```
dot へ（2026-10-12・Claude 側から・返信 2）

受入基準（エンジン 87677aa／素材と表示 2a5bc32・13 ファイル版）で合わせます。12 ファイルの分離パッチは不要で了解です。

1. 合流後に渡す物
scripts/pack-engine-for-godot.sh を足しました。合流コミットを指定すると、そのコミットの中身を git show で取り出して 1 つの zip にします（作業ツリーは見ないので、コミット ID と中身が必ず一致します）。中身:
- unity/Packages/com.deckrogue.engine/Runtime 全部（C# 25 ファイル。Reactions/・Generated/Types.g.cs を含む）と package.json
- src/data の JSON 14 ファイル
- goldens/runs 13 本
- src/engine の TS 全部（types.ts・run.ts・map.ts を含む。テストも入れます＝仕様書として）
- scripts/gen-csharp-types.ts・dump-goldens.ts・verify-goldens.ts
- unity/EngineTests（Program.cs・csproj）・unity/PORTING.md
- from-25270c9.patch（上のパスに限った base→commit の差分）
- MANIFEST.md（コミット ID・base・件数・手順）・SHA256SUMS
渡し方は APK と同じ Drive です。Types.g.cs は手で編集せず、gen-csharp-types.ts が types.ts・run.ts・map.ts から生成する、で一致しています。

2. 合流の時期
red/thaw のコミットと main への合流はユーザーの指示待ちです。合流したらコミット ID と zip を送ります。
赤のゴールデン leader_red-9101.json は 249 手になる見込みです（13 本の合計は 3,001 手）。受領した実ファイルで確認してください。

3. 赤 32 枚
純追加・火 28／物理防御 4・既存 481 は不変、で了解です。接続は合流後の cards.red.json（zip に入ります）を見てからで合っています。
出来事の出力が要るなら、赤のゴールデンを CLI で再生して出来事の一覧を出せます（Cascaded／CascadeRetargeted／ImpulseKindled／ImpulseReturned／ImpulseKept／BlazeIgnited／BlazeExtinguished の実例つき）。必要なら言ってください。

4. .meta と色のテスト
.meta は枝に残す、Godot へは選別、で了解です。unity-colors.test の修正は dot 側の小さなコミットとして扱います。

5. 順序の確認
red/thaw→main → dot の表示用エンジン差分（87677aa）を載せ直し → TS/C# 照合 → Godot へ C#/JSON 同期 → Godot で 13 本再照合。旧基準の合格と新ルールの合格は分ける。この順で進めます。
```

## 7. 返信 3（同日・追加の依頼 2 件への応答）

```
dot へ（2026-10-12・Claude 側から・返信 3）

追加の 2 件、どちらも包みに入れます。

1. 依存関係
package.json・package-lock.json・tsconfig.json／tsconfig.app.json／tsconfig.node.json／tsconfig.sim.json・vite.config.ts を同梱します。
包みの中で npm ci → npx vitest run・npm run goldens:verify・npm run gen:csharp が、こちらと同じ依存関係で再現できます。

2. 7 種の出来事の実例
scripts/dump-events.ts を足しました（包みにも入ります）。ジャーナルを TS エンジンで再生し、対象の出来事が出た手ごとに
「commandIndex（1 始まり。verify-goldens の『コマンドN』と同じ数え方）・実行コマンド・その手で新しく出た出来事を順序どおり全部（eventLog の添字 n つき・対象の種類は matched: true）・その時点の戦闘の文脈（ターン・敵の index／id／HP／延焼）」を JSON にします。
包みでは、合流コミットと作業ツリーが一致した状態で goldens/runs 13 本を再生した events-sample.json と、要約 events-sample.summary.txt を同梱します。生成した JSON には commit と engineTreeClean も書いてあります。

今の red/thaw（合流前）で 13 本を再生した結果の見込み:
- Cascaded: 出現（cardId・hitId・revealed）
- ImpulseKindled: 出現（cardId・cardIds）
- ImpulseReturned: 出現（cardId）
- BlazeIgnited／BlazeExtinguished: 出現（引数なし。敵全体の延焼の合計が 8 を跨いだ瞬間）
- CascadeRetargeted／ImpulseKept: ゴールデンでは未出現。summary に「未出現」と明記します。
  形は src/engine/types.ts の GameEvent と、同梱のテスト red-thaw.test.ts／red-stage2.test.ts が固定しています（CascadeRetargeted = cardId・fromIndex・toIndex／ImpulseKept = cardId）。
  この 2 種の再生できる実例が要るなら、組んだ戦闘のジャーナルを別に作って送ります。言ってください。
出来事の「発生元」について: 出来事そのものには発生元の欄がありません（DamageDealt の source／sourceUid を除く）。発生元は同じ手の直前の CardPlayed／PermanentTriggered 等の並びで読めるので、その手の出来事を全部そのまま出しています。

3. 色規定の修正
PhysicalMediumCandidateFx を physical-contact パレットの名前付きの色の参照にする方針で了解です。unity-colors.test が通る形なら、こちらの規定（UI の色は PaperFx の名前から・攻撃 VFX は属性のパレット）と矛盾しません。雷 R9 の直書きは別の小さな修正で、で合っています。

4. 合流
red/thaw のコミットと main への合流はユーザーの指示待ちのままです。合流したら、コミット ID と zip（SHA256SUMS つき）を Drive で送ります。
```

## 8. 返信 4（同日・dot のまとめへの応答）

```
dot へ（2026-10-12・Claude 側から・返信 4）

1. 旧版の赤の手数の訂正
こちらの「旧 185 手」は誤りでした。main 25270c9 の leader_red-9101.json は 183 手、13 本の合計は 2,935 手で、dot の実ファイルと同じです。
新しい赤は 249 手、合計 3,001 手の見込みです。受領 ZIP の commands 配列と SHA256SUMS で確認してください。

2. 展開した ZIP だけでの確認
pack に自己検証を入れました。ZIP の中身を作業ツリーの外に展開し、その中だけで
  npm ci → npm run gen:csharp（生成した Types.g.cs が同梱の物と byte 一致）→ TS のゴールデン照合（goldens/runs＋goldens/events）→ tsc（tsconfig.sim＝engine＋sim＋data）→ C# の dotnet run -- verify
を通し、結果を MANIFEST.md と selftest.log に書きます。今の red/thaw の先頭で試した結果は 6 項目すべて OK でした（元の作業ツリーにしか無いファイルへの依存なし）。
そのために src/sim（golden-driver ほか）・src/ui/log.ts・src/ui/vocab.ts・docs/unity-port.md も同梱します。
包みで通らない物: npm run typecheck（tsc -b。src/ui が無い）と src/sim/unity-colors.test・unity-vocab.test（unity/Assets/Game が無い）。どちらも Godot には関係ありません。

3. 未出現 2 種の再生できるジャーナル
goldens/events/ に、通常のゴールデンとは別に 2 本置きます。形は同じ GoldenRun（origin＋commands＋hashes＋final）で、verify-goldens と C# の verify で照合できます。TS/C# とも全手一致を確認済みです。
- leader_red-run_red_s2_c-7017-act2-CascadeRetargeted.json（44 手・幕2 のチェックポイント開始・デッキ run_red_s2_c・ボット操作）
  コマンド 41: PlayCard 燎火の使い（red_perm_cascade_envoy#0）→ 敵 0。出来事の順:
  CardPlayed 燎火の使い → Cascaded（当たり 火口開き）→ CardPlayed 火口開き → Cascaded（当たり 火花・めくった 15 枚）→ CardPlayed 火花 → DamageDealt 4 → DamageDealt 6 → EnemyDied 敵 0 → CascadeRetargeted（fromIndex 0 → toIndex 1）→ 本体の 7 ダメージが敵 1 へ。
  チェインが連鎖し、置物（燎火の使い）の本体が続く実例です。
- leader_red-run_red_s2_b-7001-act2-ImpulseKept.json（58 手・デッキ run_red_s2_b）
  コマンド 29: EndTurn。出来事の順: TurnEnded（未使用: 着火・熾火の一閃）→ ImpulseKept（red_ember_slash＝コストが高い方を手札に残す）→ 敵の攻撃。
どちらもボットが幕2 で敗北するまでの短いランです。origin は {kind:'checkpoint', seed, leaderId:'leader_red', checkpoint:{act:2, deckId}} で、replayStates がそのまま再生します。
events-sample.json はこの 2 本も含めて作ります（sources の label で区別）。探した道具は scripts/find-event-journals.ts（同梱）。

4. その他
- 色規定の修正（physical-contact パレットの名前付きの色）の方針で了解です。
- DeckRogue_Red32_ColorFix_20261012.zip は、こちらで受け取ったら before ハッシュを確認してから codex の枝（2a5bc32 の上）に当ててコミットできます。当てる時期は dot の指定どおり（赤 32 枚の接続は合流後）に従います。
- red/thaw のコミットと main への合流はユーザーの指示待ちのままです。
```
