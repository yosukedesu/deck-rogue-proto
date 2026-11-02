# Project422整理版・引き継ぎ追記

## 変更範囲と正本

元公開コミット `05c4d208a0ae7968bbbe5073c5b22ab79d02c3e2` と元ブランチは固定して残します。この後継版は `project/scripts/presentation/authored_geometry/*.cs` の1バイト（改行だけ）の25スタブを除去します。完全なパスと元ハッシュは `SUCCESSOR-DELTA.json` に記録しています。実装の正本は対応する `project/RecoveredSource/DeckRogue.Game/*.cs` です。

`DeckRogue.csproj` は `EnableDefaultCompileItems=false` で、25本すべてのRecoveredSource側を明示指定し、旧スタブを含めていません。C#・csproj・Godot設定・シーン・リソース・JSON・import・UID・文書を検索し、旧スタブの相対パス参照は0件。旧スタブに対応する `.cs.uid` は0件でした。他のUIDファイルは削除していません。

ベースラインの `DELIVERED-UNION-MANIFEST.json`、README v2、技術記録は元のまま保持し、現行版の完全一致を意味するものとして扱いません。現行 `project/` は元10,653件から25件を除いた10,628件。残る全件のハッシュが元マニフェストと同じであることを検証します。71必須依存（重複を含む配布契約の件数）、`.godot/imported`、`lib/GodotSharp.dll`、`lib/Newtonsoft.Json.dll` も変更しません。

## 保持する旧画像9枚

以下はすべて `project/assets/Art/fx/` 内の非v2素材で、同伴importとともに原状保存します。

| slash | stomp | water |
|---|---|---|
| `attack-slash-body-8f.png` | `attack-stomp-body-8f.png` | `attack-water-body-8f.png` |
| `attack-slash-glow-8f.png` | `attack-stomp-glow-8f.png` | `attack-water-glow-8f.png` |
| `attack-slash-particles-8f.png` | `attack-stomp-particles-8f.png` | `attack-water-particles-8f.png` |

目的は旧演出の参照・互換性と来歴の保持です。現行v2素材へ混ぜたり、ファイル名をv2へ読み替えたりしません。v2群（`attack-…-body-v2-8f.png`、`…-emission-v2-8f.png`、`…-particles-v2-8f.png`等）は別物です。各routeが実際に使う素材はfactoryの `Texture` とbatchごとの `Texture` を確認してください。

C#・JSON・シーン・Godotリソース・importの9つの正確なbasename検索では、同伴import以外の参照は見つかりませんでした。主要コードで `attack-slash-` / `attack-stomp-` / `attack-water-` の明示参照も0件でした。ただし `scripts/presentation/AuthoredGeometryFx.cs` は `b.Texture` を優先し、なければ `e.Texture` から `res://assets/Art/fx/` + name + `.png` を動的に組み立てます。任意の外部データや全実行経路を網羅した証明ではないため、「絶対に未使用」とは断定せず保存します。v2群も全ファイルが常時使用中という意味ではありません。

## 次回以降のコミット単位

選択routeのプロフィール・所有権判定/橋・factory・shader・textureの変更は同じ1コミットで渡してください。変更しない共有部品はコミット本文で「既存を再利用」と具体的なパスを列挙し、同梱のためだけに不要な編集を作りません。この整理コミットも実装・描画値を変更しません。

本文には次を記載します。

- routeのKey / DefinitionId / SourceKind / Family / Renderer / Element / FixedSmallと実イベント型。
- 発生元UIDのフィールド名と実コマンドとの照合方法。`PresentationSource.Uid` と `DamageDealt.SourceUid` を同一視せず、UIDが存在しない経路に捏造しない。
- 対象index・actor・定義/生存状態と、before/afterの対応。複数対象は各receiptに対して区別する。
- commandの種類・CardUid・TargetIndex・ModeIndex等の必要条件、同じ変更セットのイベント順序とevent参照の同一性、ledger generation。
- 登録/予測と着弾確定を区別する。projectionだけで確定接触面を出さず、そのrouteで必要な `receipt.Committed && receipt.Exact && receipt.Reason == null`、current generation、同一event参照を満たしてから出す。共有の予備動作や飛翔は既存契約を再利用する。
- 接触基準時刻、終了、Clear/再利用時のゲート初期化、変更した値、再利用した値、検証範囲。

### ColdSweepの具体例（実装の説明、変更なし）

`SourceShowcaseProfiles.cs` の `ColdSweepContactVfxProfiles` は `card:blue_cold_sweep / card / cold_sweep_contact / ice / !FixedSmall` を選び、`BoundedContactFx` から `ColdSweepContactFx` へ切り替えます。`BattleStage.IsOwnedColdSweepContact` は実 `GameEvent_DamageDealt` の `Source==player`・正のHpLoss、実PlayCardのCardUidと `PresentationSource.Uid` の一致、前の手札の同UID/同カード、TargetIndexとEnemyIndex、同変更セット内の先行CardPlayedを確認します。プロフィールはreceiptのenemy対象/indexとValidも確認します。

`HasCommittedColdSweepContact` は同じevent参照、現ledger generation、Committed/Exact/Reasonなしを要求。factory `NativeGeometryBridge.CreateColdSweepContact` はcommittedがfalseならalphaを消去します。`AuthoredGeometryFx` のBegin/更新/Clearでゲートを渡します。共有 `BoundedContactGeometry` と `cold-frost-density-v1`、専用 `authored_ColdSweepSurfaceFx.gdshader` を再利用します。直接攻撃と後続の防御Iceイベントを混同しません。

### SparkBurstの具体例（実装の説明、変更なし）

`card:white_spark_burst / card / scatter_warm_spark / light / !FixedSmall` を `ScatterProfileFx` から `SparkBurstContactFx` へ選択します。`BattleStage.IsOwnedSparkBurstContact` はleader_whiteの実PlayCard、`PresentationSource.Uid == play.CardUid`、`hit.SourceUid == null`、ModeIndexなし、同じTargetIndex/EnemyIndex、前の手札に同UIDのspell、前対象HP>3・Block0・before/afterの敵定義一致・実actor一致を確認します。

同変更セットはCardPlayed1件→同じDamageDealt1件（Amount3/HpLoss3）→CardsAddedToDraw1件（white_spark_token3）の順序。追加された3つのtoken UIDが新規かつ一意で、tokenイベントも同じownerに属することを確認します。receiptは同じevent/Values/current generationかつValid。`HasCommittedSparkBurstContact` はさらにSource==hero・実Target参照・Committed/Exact/Reasonなしを要求します。tokenイベントが早期ダメージ時点ですでにcommit済みであることまでは要求しません。予測登録とダメージの着弾確定を分けてください。

`SourceShowcaseProfiles.cs`、`BattleStage.cs`、`NativeGeometryBridge.cs`、`AuthoredGeometryFx.cs` のselected-route material条件（Kind61/Space0）、`authored_SparkBurstContactSurface.gdshader` を一組として追跡します。今回これらは全て未変更の再利用です。

## Unity側の追加報告（ユーザー報告、今回の独立実行ではありません）

ColdSweepSurfaceFxとSparkBurstContactSurfaceはUnityで描画されています。以前見えなかったのは0.1秒間隔の撮影と3枚目から切り出したモンタージュによる見落としで、この2件の不可視報告は撮影問題として解消済みです。

報告値: Landでcommitted=true・age=0。採取age .017–.117秒、最大alphaはCold .546→.159、Spark 1→.506。gainはCold1、Spark1.7/1.45、emissionSmall .72。寿命はCold .18秒、Spark .19秒。UnityのageはすでにLand相対なのでwindupを再減算しません。この報告だけで他routeや全端末の検証済み扱いにはしません。

受光係数は減衰として扱います。追加ライト、描画挙動変更、JSON出力は今回実装しません。将来JSON化する場合は色空間/色・時間基準/寿命・範囲/座標・所有権/確定条件まで含めた契約が必要です。

## 整理版の検証

残存10,628ファイルのサイズ・SHA-256を元マニフェストと照合し、実体のあるソース・素材・依存が未変更であることを確認。csprojの明示Compileパスは全件存在し、削除スタブはその集合の外にあります。

利用可能な.NET SDK 10.0.400 x64で、元のnet9.0ターゲットとGodot.NET.Sdk 4.6.3のまま `dotnet build DeckRogue.csproj -c Debug` が成功しました（0エラー、1,926警告）。警告は変更していないソースから発生し、元配布の記録と同数ですが、SDK9でのベースライン再実行や警告1件ずつの比較ではありません。最初の制限付きネットワークでの復元失敗後、通常のNuGet復元が可能な実行で成功しています。ビルド生成物はコミットしません。

Godot実行ファイルはPATHと確認した既存の配置先で見つからず、正式インポート・起動・実行時資源読込は今回未実施です。ビルド成功をそれらの検証済み扱いにはしません。全native VFX、Unity、Android、音声の再テストも今回の結果には含めません。
