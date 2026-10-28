# 261「死中の活」— Unity 意味移植資料（選定261・限定QA終端）

**Godot 全文は比較元です。Unity renderer / math / shader / Stage / Actor を丸ごと置換しないでください。** Unity 側の Linear / palette `c.linear` / HDR gain UV / 510 triangle indices / zero-safe math 等の既存適応を維持し、変更した factory・exact selector・dispatch の意図だけを既存 Unity API に写します。261 は以下の限定美術/技術gateを閉じた選定版です。Git公開状態とは別で、作成時点の公開HEADは258、260/261公開は親GO待ちです。

## 目的と限定経路
fresh260 の実「死中の活」は、頭左上の大きな闇塊と胸の小塊が独立して浮いて見えました。改善理由はこの登録/主従の不足であり、drain20という値や名前を理由に巨大化することではありません。

`ReactionContactVfxProfiles.LastStandRoute` は既存 `SelectedRoute` の full tuple に `DefinitionId==black_reaction_last_stand` を追加するだけです。
`card:black_reaction_last_stand / SourceKind reaction / dark_drain / dark / Hd2dReactionContactFx / FixedSmall=true`。
既存の実 ConfirmReaction、pending post attack、同一 set UID、TrapAge、previous-state HP 条件、実 enemy receipt、正 HpLoss 判定を変更しません。SourceUid は null で、反応の PresentationSource UID とは別の資格です。

3 C# 挿入を除去すれば selected260 各全文 SHA に戻ることを確認済みです。旧 CreateDark / CreateReactionDark / Curse / CorpseKiss / other necro / installed は byte 不変。Stage・Actor・カメラ・床光・shader・raw・palette の変更はゼロです。新しい身体/武器/死者/飛来弾/回復戻り線もありません。

## 数値を伴う変更
受け手ローカルの共通 root を `(0.26,0.01,0.025)` とします。旧共有 contact は `(0.035,0.14,0.025)`、さらに2房へ異なる offset を加えていました。261 は両房の可視原本 pivot `(0.60,0.58)` を同じ root に登録します。固定画面pixelではありません。

- 主房 half extent `(0.65,0.46)`、短副房 `(0.30,0.25)`。旧共有 local の初期値は主 `(0.6864,0.6084)`・副 `(0.6474,0.5928)`。拡大ではありません。
- `dt=age*1.25-part*0.012`、`close=Ease(dt/0.20)`、onset=`Ease(dt/0.018)`。
- `rx *= 1-0.22*close`、`ry *= 1-0.28*close` は旧率を維持。
- 主角 `-0.12+0.11*close` rad、副角 `-0.65+0.13*close` rad。
- phase01 `1-Ease((dt-0.055)/0.035)`。
- phase02 `Ease((dt-0.055)/0.035)*(1-Ease((dt-0.145)/0.050))`。
- phase03 `Ease((dt-0.145)/0.050)*(1-Ease((dt-0.25)/0.10))`。
- 元原本 phase01/02/03・opacity/gain は固定。7本の短い内向き mote は元の相対軌道/値を共通 root へ移すだけです。
- sampler 有効窓 `0<=age<=0.44`、旧 local の負 age は元から透明。
- `TargetPeak=0.028`, `FloorPeak=0`, `StageFocus=0`、正 age light=`1-Ease((age-0.025)/0.12)`、floor light 0 は旧値。
- 4 batches kind `[16,16,16,2]`。各 capacity512、written 最大 `[12,12,12,42]`。Count は使用頂点数ではありません。

## 実証と資格
fresh260 baseline は実初期energy3、Set→2実EndTurn→実DarkPact非致死調整→実EndTurn→同UID Confirm。previous HP37/80 で条件成立、敵200→180、後続の実drain heal10でhero47。**回復後HP>半分を理由に previous 資格を再判定しません。** 手札の多数Pactは合法QA準備の実stateでありUX変更ではありません。

261 標準572 assertions / exit0、同actual sequence/全oracleを比較。Astra 原寸0020/0022静止は限定PASS：大塊の可視根が胴につながり、副は同点へ従属。ピーク顔前部は一時被覆するものの20/HP/体輪郭を維持、後半顔が戻ります。直接等速/Unity/実端末性能は未確認。連続frameと最小boundsの終端結果は以下のとおりです。

Audio Ogg shutdown notice は既知の別issueであり、本VFX checkpointには修正を混ぜません。全runのVSync driver warningとresource shutdown noticeは分けて記録します。


## 最終QA：3762 assertions / 7 successful exit-0 runs

standard572、正常movieB648、above-half577、eligible decline288、旧共有Underworld526、Moss564、lifecycle587。これは重複assertを含む限定7runであり全matrixの網羅数ではありません。全7runでshutdown-resource noticeは非再現、既知VSync driver warningはあり、旧Audio Ogg問題を解決したとの主張はしません。

元movieAは638checks末尾の疎capture用 firstpositive observer 未更新というQA誤assertでexit1、別失敗として原本保持。Bでは連続側の実observerを更新し、要求を削除していません。製品source不変、A/B MP4 bytesは同一です。

- **連続美術**：正常Bのidx17–34を実decode/clock照合。18/.017〜19/.050で胴根付き一主圧、20/.083〜21/.117で外側が短くなり胸側残留、22/.150ほぼ消失。別々の再膨張/浮遊塊保持なし。receipt18唯一、27–29空、30–46は独立heal、47+全effect空。直接等速の主観的心地よさを測定したものではありません。
- **条件false**：最後の実EndTurn直前にsameUID/live age2/HP47を証明。実poke6後HP41/80は半分より上、pending/ReactionTriggeredなし、合法Whiffed/Expiredを経てturn4/set空。終了時にもsetが残るという旧誤assertは元案未run段階で訂正しました。全state/event oracleは不変。
- **eligible decline**：条件成立pendingから実sameUID Fire=false。条件不成立とは別の否例。専用作用なし。
- **旧共有**：実冥府の手の正反応で旧7batch、専用LastStandなし。共有Darkに漏れないことを確認。
- **別体格**：実MossSlime encounter、合法Pact調整後positive20、960×540の4keys。上前部へ根付き、浮遊/床影化なし。ピークの顔一部被覆は残ります。HP/体輪郭/初期20は可読ですが、VFX clear後frame28の上昇20は既存intent『攻撃5』と重なるため、全時間数値重なりなしとは言いません。Probe+Mossの2体格のみ、全敵/実phoneへの普遍保証ではありません。
- **lifecycle**：実pending fixture reloadで接触前・自然完走・実接触後cancel・actor component dead+cancelを確認。最後はEngine致死による経路証明ではありません。
- **未確認**：全block。RockBeetle殻12では20を全吸収できず、worm殻30は合法到達recipe未実証のため偽block注入で埋めていません。所有なし等のselector unit否例を実全吸収とは呼びません。

新raw/新shader/import設定追加なし。既存 `hd2d-dark-contraction-01/02/03` は原本参照のまま、リサイズ不要。Unity側の既存世界光/receiver placement/clock実装に上記意味を写し、ユーザーUnity動画で最終motion/照明/登録を確認してください。

## 保存元の資格
今回の全baseline/比較/連続/boundsは正規Library素材から復旧した作業場で新たに取得した証拠です。元環境の旧QA/fullmanifestはアクセス未復帰・未復元であり、永久消失と断定しません。選定261logical249と差分SHAを維持します。最終sidecarが古いpending記載より優先しますが、凍結契約や失敗原QAは書換えません。
