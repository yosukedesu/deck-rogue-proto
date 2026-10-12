# Codex（dot）からの共有 2026-10-12 — Unity の VFX 枝と Godot 完全移植の現在地

dot（Codex）が 2026-10-05〜12 に並行で進めた作業の共有。**引き継ぎではなく、実装は dot 側が続ける。**
受け取ったのは短い共有（§1）と詳細版（§2・添付ファイル `DeckRogue_dot_changes_20261012.txt`）の2通。
§3 は Claude が手元のリポジトリと突き合わせた結果、§4 はユーザーの裁定（ask_user 4件）。
dot への依頼文は `docs/codex-dot-request-2026-10-12.md`。

## 1. 短い共有（原文）

> Deck-Rogueについて、codex dot側で進めている変更を共有します。作業の引き継ぎ依頼ではなく、並行して進めている開発内容の共有です。実装・調整は引き続きdot側で進めています。
>
> 【master／mainとの関係】
> 確認できている基準はmainの25270c9c511a2ceb4dff4806cf9deb7c50c78709です。masterの参照と最新の完全なGit履歴は未確認なので、以下は厳密なmaster対比のファイル差分ではなく、dot側のソース・変更記録・成果物に基づく差分概要です。
> Unity側は専用ブランチcodex/hd2d-vfx-motion-20261005で変更しています。最新PC作業ツリーには別作業の未コミット変更もあるため、その全てをdotの変更とは扱っていません。
>
> 【Unity側で行った主な変更】
>
> 1. ステージ・背景
> Blenderを使ったステージ造形、森のテクスチャ、接地感、霧、月光、局所光、夜の色調を改修しました。目標はオクトパストラベラーのようなHD-2Dの奥行きと神秘的な雰囲気です。低ポリ感、浮いた根、手前の不自然な崖、均一な明るさなどを見直してきました。
>
> 2. キャラクター
> 未整備だった「みぞれ」のドット素材を制作。長髪・杖を持つ少女魔法使いとして、既存キャラとの向き・頭身を調整し、待機フレームの位置ずれも修正対象にしました。攻撃・被弾・待機の動きと足元の安定性も改善しています。
>
> 3. 攻撃エフェクト全般
> 斬撃中心だった表現から、属性攻撃・敵攻撃・各種接触演出へ対象を拡張。炎、水、氷、雷、地面系、光・闇、牙・爪等の素材と描画処理を制作・改修しました。
> 属性別の色規定とパレット、白い発光芯、色付きの周辺光、粒子、残光、着弾表現を整備。小・中・大は単なるサイズ違いではなく、形・発生順序・溜め・着弾・余韻が異なる攻撃シナリオへ変更する方針です。
> ツタはムチ表現を試した後、斬撃風へ変更しています。
>
> 4. エフェクトの描画方式
> イラストを移動・拡大するだけの表現を見直し、ジオメトリ、シェーダー、粒子、テクスチャを組み合わせ、形そのものが連続して変化する構成へ改善しています。
> 光が素材と分離して見える問題、被写界深度による細い雷のぼけ、遮蔽、床への光、テクスチャ取込なども比較検証・修正の対象です。
>
> 5. 戦闘の進行と演出同期
> ヒット時のウェイトと重さ、敵の演出終了を待つターン進行、勝利時のフィニッシュ演出から報酬画面への遷移を改修しました。
> 攻撃の発生元・種別を追跡する処理、演出中のHP等を管理する表示台帳、敵の多段攻撃、ガード、反射、状態による損失なども整備。428件の攻撃ルートを監査し、演出が抜ける攻撃を減らしています。
> カード連続プレイの気持ちよさと、強い攻撃の見応えを両立させる方向で調整中です。
>
> 6. 検証基盤
> 実描画のスクリーンショットや比較動画、段階別・属性別テスト、フレームや光源の検証を追加しました。素材の完成、ソースへの組み込み、APKへの反映、実機での品質確認を別々に管理しています。
>
> 【Unity APKへの反映状況】
> 最新のStage42テストAPKにはGround・Fang・Fire中段階などを含みます。一方、その後に制作したIce、Fire大段階、Water、Light／Darkのクラウド候補まで全て入っているわけではありません。クラウド上の最新素材と配布済みAPKは同一状態ではありません。
>
> 【Godot側で追加した変更】
>
> クラウド中心で開発・描画検証・Androidビルドを進めるため、Godotへの完全移植を開始しました。
> 作業先はdeckrogue-godot-port、ブランチはcodex/godot-full-port-20261012です。元Unityのmainと直接履歴がつながるブランチではなく、独立した移植リポジトリです。
>
> ・元のC#ゲームルール25ファイルとデータJSON14ファイルを、内容を変えずに流用。
> ・カード444、リーダー15、敵89などのゲームデータを取り込み。
> ・セーブ、復旧、履歴等を維持するセッション接続を実装。
> ・タイトル、出発、マップ、戦闘、報酬、ショップ、イベント、休憩、工房、勝敗等の画面と操作を移植。
> ・元の手続き生成カード絵等も移植。
> ・52系統のエフェクトを15描画システムへ接続し、ステージ・キャラ・音・光をGodotに対応。
> ・実描画後のPNGと状態情報を自動保存する検証機能を追加。
> ・Galaxy S25向けの横長画面、文字サイズ、タッチ操作を調整。
> ・Godot .NETのクラウドAndroidビルド環境と、小規模なC#実証APKを作成。
>
> 【現在の検証状況】
> C#側は140,003アサーション、元実装との13シナリオ・2,935段階の状態一致を確認しています。拡張統合テストは149件合格し、クラウド実GUIでも149件失敗ゼロです。
> ただし、これは全エフェクトの美術品質やAndroid実機での完全動作を保証する結果ではありません。Godotの完全移植はまだ進行中です。
>
> 既に配布したGodot APKは、森・みぞれ・雷を確認する試作版です。ゲーム全体を移植した最終APKではありません。
>
> 【引き続きdot側で進めていること】
> 全エフェクトの実描画品質、発光と背景の馴染み、キャラの動き、スマホUI、低FPS時の演出欠落、元機能との細かな違いを詰めています。その後、完全移植版APKの作成・検査とGalaxy S25実機確認を進める予定です。

## 2. 詳細版（原文・添付ファイル `DeckRogue_dot_changes_20261012.txt`）

```text
DeckRogue dot変更内容共有 UnityとGodot
2026-10-12。作業移管ではなく、dotが進めた変更と現在地の共有。Godot側の状況は後半に掲載。

【比較基準】
厳密な「masterとのgit diff」ではない。クラウドには元PCの履歴とmaster refがなく、保存snapshot・受入記録・patch・manifest・QAから整理した。初期から存在するHD-2D仕様やゲーム全体をdotの新作とは扱わない。
既知main：25270c9c511a2ceb4dff4806cf9deb7c50c78709
PC専用branch：codex/hd2d-vfx-motion-20261005
受領snapshot：7a84644070459b615c262fc15eae44f84a63fb9c（取得時clean）
直前実装：03ddacc、さらに前が94e6f47。
クラウド合成baseline：4bdf5d46442319692e04036611608c8218cecbdb、codex/cloud-edit-7a84644。PCのcommitではなくremoteもない。
後のPC受入commit：22717b96a577d9db32d5e7c4ccad3aadd2cf5214 → 2f310506aa3933a62618a9e3f2714da57ad66cbd。
2f31050以後にはStage patchとnative修正がある。現在のPC HEAD/cleanは今回再検査していない。red/thawの別件未コミット変更も対象外。今回PC操作・build・source変更はしていない。

以下の資料パスは /workspace/scratch/948944000d55/ を基点とするローカル参照で、別環境から自動的に開ける共有URLではない。
根拠：deckrogue_cloud_migration/CLOUD_PATCH_WORKFLOW.md、worktree/SNAPSHOT/{README.md,manifest.json}、verification/pc-accepted-*.json。

【1. 森舞台・素材・接地・照明】
・MOSSBOUND FULL STAGEとして平坦な戦闘床、前景岩、中景土手、小川と橋、遺跡アーチ、灯籠、敷石、草、岩、奥行きのある森を制作。地形/遺跡は3D、木/シダは独自alpha cutout。Octopathの元assetは含まない。Blender、FBX、GLB、texture、foot marker、source-to-batch対応を保存。
・透明画像の矩形でなく見える根元を基準に木を接地。岩、シダ、門、階段、柱、灯籠も支持を監査。
・川縁の規則的48石を16個の変化ある岩へ。前景崖winding、尖った草、共面敷石の黒いちらつきを修正。
・mesh結合でUV-layer名が混在し1,150面がUV0退化した問題を、export前の単一UVMap化で解決。9,180 triangleの位置は維持。
・夜景向けに静かなrock/soil/moss/paving albedo、連続した前景apron、自然な土手を制作。キャラpointと3D地面のfiltered/mipmapを分離。
・AOだけの試作、中景2本の木の構図/枝trim、自然土手の起伏/侵食溝/裾、木6本・シダ16・岩17の再接地を実施。
・最終素材統合候補は自然土手に道23枚だけを移植。残り218mesh不変、16組の共面重なり解消、27,818triangle/16batch/14material。各試作すべてが同時採用された意味ではなく、資料時点の最終美術承認/Android性能は別。
・新sourceへ接地を焼き込んだbank_rock_12に、旧94頂点−0.36m runtime補正を二重適用しない設計。
・コードの月光shaft texture変調と既存256px canopy cookieを調整。camera移動禁止を旧ZoomPunchにも適用。余計な全画面passを増やさず、強い攻撃world lightを維持。
資料：full_stage/README.txt、quality_studies/{ao,cohesion,midground,natural_banks,integrated_forest}/、worktree/docs/art-bible.mdの2026-10-05改善節。

【2. みぞれ・初期VFX・mainに近いテンポの復元】
・斬撃/着弾/魔法/防御atlasと、火/水/氷/雷/光/闇/土/蔦/回復の9属性8frame素材を制作。VFXは滑らかでもキャラのドットは保持。
・みぞれleader_blueのidle_0だけ全身が右8pxずれる不具合を修正。exporterが杖を足のanchorに含めたのが原因。idleの下端8行で足位置を算出し、idle_0・通常画像・atlas該当cellを厳密に左8px移動。他15枚はbyte一致。128×84、足y76、pivot(60/128,8/84)を維持。runtime位置補正ではない。
・main25270c9とのWindows同条件比較で、多段を25ms刻み/240ms上限へ圧縮していた問題を確認。初撃0.13秒、通常多段0.12秒、全体0.03秒、人形batch、敵行動/反応間隔を復元。
・次カードによる前の補助演出の一括中断を除去。確定ticket/fallbackを保持し、通常の連続入力を待たせず最後の命中を見せ切る。
・雷/光の全面Bloom前合成を棄却。本体はBloom後straight-alpha、haloのみBloom前加算へ分割。同じ画像全体を二重加算しない。pool/UV/位置/寿命共有、他7属性と強いworldlightは保持。
資料：element_vfx/final/、mizore_idle_fix/Mizore_IdleAnchorFix/、worktree/docs/design/ux-restoration-2026-10-05.md。

【3. HP・盾・意図・入力・決着の時間順】
・攻撃前にHPだけ減る、盾や次ターン意図が先に切り替わる表示を修正。CombatDisplaySequenceで直前表示を保持し、実際のDamageDealt/BlockGained/TurnStarted/EnemyIntentDeclaredで更新。
・ゲーム確定状態と表示時系列を分離。immutable snapshot、UI revision、target世代で古いcallback/補間の上書きを防止。
・showcase5枚に溜め→踏み込み→接触光/数字/後退→余韻を追加。打撃0.13秒、二連0.16/0.28、水0.24、大地0.30/0.45、踏みつけ0.34秒が目安。
・局所hit pause：打撃65ms、水75ms、踏みつけ120ms、二連25→95ms、大地25→110ms、対象決着140ms。guard/nullify/healは停止せず、AOE対象数で加算しない。最大140ms、復帰50ms。
・命中時35%後退を先に入れ、通常身長8.5%/重い技15%までのけぞって戻す。次カードは防御も含め動作所有権を更新。
・showcase APKの既定ONはbuild-local define。恒久的なProject defineを変更しない。
・後段で敵フェーズと決着だけ入力gateを追加。旧盤面/手札を保持し全敵イベントと着弾後に次ターン公開。通常プレイヤーカードの連打は維持。初期の「敵演出中も次手札を即操作」は後に変更された過去仕様。
・決着はtimeScale0.22を0.45秒、0.20秒で復帰。nested Tween、反応、dissolve、残光、world粒子、非idle spriteまで待って報酬へ。watchdogは予定列長×2+5秒、最低8秒。
・reset/離脱/盤面交換/disableでcallback・gate・slowを解除。GameRoot iteratorのyield break、旧stage勝利/敗北のskipも修正。
PC受入記録：presentation42/42、transition40/40、tempo9/9、VFX25/25、20組A/B engine一致、watchdog/inputlock/timeScale残留0。Android実機の証拠ではない。
資料：deckrogue_cloud_migration/ のpresentation-comparison、hit-impact-pause、battle-transition、iterator-fix、outcome-skip-fix各packageとPC受入JSON。

【4. VFXの部分差替えから全source対応へ】
2026-10-08はgreen_strike/blue_strike/green_finisher_stompをbody/particles/glowの別assetへ。各1024×512、4×2、256pxセル、8frame。実contactで3層をframe2へ固定し、旧ticket/低FPSによる上書きを防止。通常alphaと発光を分離、stomp足元/pivotを補正、旧embersを二重に出さない。

続くStage41累積は、PC2f31050から既存38ファイル置換＋新規309、削除0。保護baseline5,317→5,626。347出力のbefore/after hashと41段の元ZIP/hashをmanifestで管理。
Stage履歴：
01 VFX V2
02 混乱攻撃
03 source/effect routing
04 nested provenance
05 火/光/闇
06 氷/蔦/土
07 shared family
08 雷/回復
09 damage intensity
10 scalar outcome
11 numeric receipt
12 presentation同期
13 蔦slash
14 pool/visibility
15 敵打撃/爪
16 牙/beam
17 guard/nullify
18 reflect/burn
19 selfloss/affliction
20 pierce/meteor
21 slash S/M/L
22 連続水
23 火mode
24 闇/吸収
25 連続回復
26 光R2
27 氷結晶R2
28 土/stomp ground R2
29 雷放電R2
30 scatter4 profile
31 wide水/火
32 local5 profile
33 shared semantic98経路
34 敵metadata
35 敵多段
36 敵29経路
37 card reuse10
38 source coverage
39 scenario基盤
40 blue_strike水M PBF候補（既定OFF）
41 green_strike限定物理M候補（既定OFF）

単なる画像/shader変更ではない。TS/C#両engineのcombat/effects/events/gears/typesに発生元・receipt情報を伝える変更があり、表示側にはCombatPresentationLedger/CombatOutcome/DamageVfxIntensity/DamageVfxReceiptPlan/CombatActionQueue、PresenterNumeric/EnemyContacts、EnemySourceLease、scenario clock/anchor/playback、GroundSurfaceProvider/StrictGroundCapture等を追加・更新。
各familyはFx/Geometry/shader/paletteを持ち、ArtImporter/ElementVfxImporter、audit script、Autopilot、EngineTestsも拡充した。ルール/RNGを見た目のために変更してよいという意味ではない。
資料：attack_coverage_audit_20261011/stage41/{manifest.json,review/cumulative-stage41.patch,payload,verification}/。

【5. coverageの到達点と限界】
Stage41＋Stage42を独立再構築したaudited_sourceで、419定義/428 source-kind経路、15renderer、未割当0、列挙resource欠落0。
内訳：PhysicalSlash30、BoundedContact184、EnemyContact76、Fire36、PlayerContact26、Dark16、Scatter12、Light11、Water9、Local8、Vine6、Wide6、Earth3、Lightning3、Ice2。
minimal neutral151は意味を勝手に武器/属性へ解釈しないための意図的fallback。自由な独立S/M/L geometryは通常slash24経路。56はnoncard固定small、4はsemantic small。その他の多くは同familyのsize/emission/particles/light倍率差で、「全攻撃が小中大の別形状」は未達。
敵2〜12 actual contributionsはordered contact、13以上aggregate、Confusedはaggregate-only。native失敗時はlegacy/element/shared-impact/UI receiptへ戻る。guard/nullifyは別outcome。cancel、0以下の回復/自己損失の非表示を欠落と混同しない。
保存Stage42 report：4,382case/52,484assertion/501engine snapshot一致、全428経路のhit/guard/zero/missing resourcesを含む。ただしstub/CPUであり、全Unity/GPU/Android実playの証拠ではない。後のPC importer/body-union/opticsは監査snapshotの外。
資料：attack_coverage_audit_20261011/{AUDIT_ja.md,summary.json,attack_coverage_428_contexts.csv,renderer_asset_inventory.json,system_event_coverage.csv}。

【6. 雷R9 Stage42と後続統合】
初期Stage42はらいこleader_izzet/blue_aether_burst/aether前1消費1/単体1receipt/実効HP損失5〜14かつmediumだけを採用する既定OFF候補。音/HPは既存PlayTrackedの時計と所有者を維持。他カード、S/L、多段、AoE、不確かなprovenanceは既存へ戻る。
主幹16点、4枝、2細枝、7冠、横弧、ground channel等を連続geometry/shader化。game RNGを使わず凍結source定数を使用。1slot1176頂点/880三角形/2draw、pool2、Low1。肩socketではなくcamera/身長由来proxyで、接地は証明できる水平面に限定。
runtime14ファイル（既存4＋新規10）でStage41の正確なbefore-imageが前提。
その後、normal fallback、雷receipt/body-union/optics、earth/stompのTexture2D import・HDR/SSAO/occlusion、fangの追従・target light・projection、fire R7のscene handle・texture import・runtime light・production bindingを個別修正。候補フォルダの存在をすべて採用済みとは扱わない。
資料：stage42/README_ja.md、lightning_r9_optics_route_patch_20261011/、earth_stomp_import_fix_20261011/、earth_stomp_optics_counter_integration/、fang_optics_counter_integration/、ground_fang_combined_import_v2_20261011/、fire_r7_ground_fang_integration_20261011/。

【7. 最新UnityテストAPK】
保存済み検証と統合情報では、Ground/Fang/Fire Mと従来の雷を含む。新しいIce・Fire L・Water R14・Light/Dark候補はこのAPKに含まれない。
DeckRogue-stage42-ground-fang-fire-test-20261011.apk
132,426,152 bytes
SHA256：958431147747e98eaffe665a0f5518e1b5b93681e89864926ba3049a90d72f50
場所：fire_combined_apk_review_20261011/input/
Windows D3D12通常起動の実UIでred_strike、medium、単体damage6、実receipt1件、provider owner1を確認。legacy body抑止状態、独立engine一致、light endpoint98件一致、57frame/3native screenshot、正常save保持。source92ファイル一致、transition40case、Android build error0/warning69。
独立read-only APK監査では3653entryのCRC、ARM64 native library7本、v2 signature block構造を確認。暗号学的署名検証・Android起動の代わりではない。Android実機の操作/音/画質/FPS/発熱は未確認。
資料：fire_combined_apk_review_20261011/{fire-combined-native-validation.json,independent-source-visual-regression.json,apk_audit/independent_apk_audit.md}。

【8. 最新の別候補（上のAPKとは別）】
氷：water_ice_finish_20261011/にsolid/body/cap、材質/import、binding、接地、native比較、production admissionの各段階。今回最終段を全部再監査していないため、最終採用/美術合格は未確認とする。
火L：fire_r7_large_prepare_20261011/、fire_r7_large_native_review_20261011/。Fire M統合APKとは別。
水L R14：water_r14_runtime_20261012/。既定OFF、production adapter未接続。−250〜+550msの位置/法線/厚み/foamを補間し、17親＋68娘滴の85固定IDを任意時刻評価。単なる49コマ画像切替ではない。cache6,104,164B、crown30,186頂点/60,360三角形、全56,160頂点/slot、最大6draw。共有重VFX予算と局所ground/occluderが必要。Unity/GPU/Android未検証、ガラス粒感解決の主張なし。
水LOD：water_r14_lod_20261012/。独立addon、crown6,072頂点/12,132三角形へ79.9%削減、cache2,824,660B、全32,046頂点/slot、draw6不変。360×270固定cameraの双方向サンプル最大誤差0.798px、720px高さ換算2.127px、1080で3.191px。薄rim法線/表裏の大きな局所差が残る。46,408assertionはCPU。
光R16/闇M R9・L R10：light_dark_runtime_20261012/。既定OFF、既存routing/HP/音/turn gate未変更。光Mは3方向収束/貫通/散乱、光Lは主1＋副2の異なる長さ/曲率/時間を3波で放出。却下された直線5本のカーテンには戻していない。105粒子を任意時刻評価。闇M非対称crossrift、闇L偏心voidの内向き閉鎖＋固定3破片。LightM232頂点/116三角形、LightL412/206、DarkM18,157/35,136、DarkL36,016/69,763。闇Lは重い忠実度基準でmobile通常採用版ではない。cache約32.95MB、既知managed数値payload約73.69MB。13,978,199assertionはCPUで、Unity/GPU/Android合格ではない。budget/receiver bridge未接続時は旧表現へ安全に戻る。

【現在の結論】
dotの変更はGodot移植だけではない。森舞台・素材・みぞれ・9属性VFXから始まり、戦闘の表示時系列、入力と決着、発生源情報/数値receipt、全source routing、native geometry/shader、安全fallback、検証・統合APKまで広がっている。一方、「全候補がUnityに統合済み」「全攻撃が独立S/M/L」「全端末で最高品質」はまだ成立していない。
今回保存資料を読み取りで整理した。最新PC commit、各試作の最終美術承認、すべての候補の最終統合状態は未確認部分として残す。dotの作業継続と矛盾する引き継ぎ・移管の意味はない。



Godot側の追加変更と現在地（2026年10月12日13時17分 JST）

本資料はClaudeへ現在の変更内容を共有するためのものです。作業移管の依頼ではなく、dot側で開発を継続しています。

作業先: /workspace/scratch/948944000d55/deckrogue-godot-port
ブランチ: codex/godot-full-port-20261012
初期コミット: 74e982f。独立リポジトリであり、元mainから直接連なるGit履歴ではありません。現在の実装には未コミット変更があります。

移植基準はクラウドに確保したStage41と監査済みソース。Stage41の5,626ファイルのハッシュを照合し、選択差分込み5,737ファイルを追跡。PC最新red/thawの未取得変更を全て含むとは言えません。

実装・流用した内容
・C#ゲームルール25ファイル17,453行とJSON14ファイルの計39ファイルを元のまま流用。
・カード444、リーダー15、デッキ49、敵89、エンカウント51、イベント37、レリック103、装備38、融合32、出発10。
・ラン操作30種、戦闘操作9種、効果125種、トリガー34種の元APIを利用。
・セーブ、復旧、履歴、評価、メモ等を保持するセッション接続。
・タイトル、出発、マップ、戦闘、報酬、ショップ、イベント、休憩、工房、勝敗等の画面・操作。
・カード選択、対象選択、反応、捨札・消滅、占術、Xコスト等の操作経路。
・元の手続き生成カード絵・リーダー画像等を移植。
・攻撃ルート428、エフェクト52系統を15描画システムと18シェーダーへ接続。元ジオメトリと表示台帳にGodot専用アダプターを組み合わせています。
・演出中表示と実状態の分離、ヒット受領検証、HP/ブロック表示、敵多段、生成・消滅、画面離脱時の後片付け。
・森等のステージ、夜の色調、霧、月光、局所光、足元アンカー、元フレーム間隔、音を移植。
・紙・インク・カード配色、縦横比、マップアイコン、タイトル開始ボタン、スマホ向け文字と操作領域を調整。
・Galaxy S25の横長画面に合わせる横方向拡張設定。
・実描画後PNGと状態・フレーム・解像度・使用レンダラー・SHA256を記録する自動撮影機能。

検証結果
・C#検証140,003アサーション。
・元実装との13ゴールデンシナリオ、2,935段階の状態一致。
・138セーブ/復旧確認、124,100乱数一致等。
・初回の実GUI統合50/50と18PNG確認。その後の拡張149/149はヘッドレスと実GUIの両方で失敗ゼロ。
・52系統/148段階組合せ/15描画系のリソース接続を検証。ただし全エフェクトの美術品質合格を意味しません。
・S25比率1170×540、スマホUI、130%文字設定のヘッドレス検証も149/149。実機検証ではありません。
・元TypeScript全体テストは1,189合格、1失敗、1スキップ。失敗はUnity PhysicalMediumCandidateFxの直接色指定に対する色規定テストで、ルール不一致ではありません。

Android環境と配布物
Godot4.6.3 .NET、.NET SDK9.0.318、Java21、Android SDK、ARM64エクスポート環境を構築。小規模C#実証APKは署名・アラインメント確認済み。不要なリンク時静的アーカイブ除外で実証APKを約103MBから約43MBへ縮小。完成ゲームの容量ではありません。
クラウド実描画はOpenGL/llvmpipe。Vulkan実描画やGalaxy S25実機動作は未確認。
配布済みDeckRogue-Godot-Cloud-Trial-20261012.apkは森・みぞれ・雷を確認する試作版で、完全移植APKではありません。
容量30,643,944bytes、SHA256 a8989ed422ddb958ecb7da6baedee63e7b91589cbed1d261fe3c8ac6fc60ed9c
既存Drive共有先: https://drive.google.com/file/d/1wIeTh1_clyFkuZs-hBA-yfOEj-uLL1d9/view?usp=drivesdk
後日追加した自動撮影はこの配布APKには未反映です。

進行中の調整
全描画系の実画像比較、発光の馴染み、キャラ攻撃/被弾/防御、状態表示、低FPSで短いヒットが未描画になる境界、狭い画面で敵や長い説明がはみ出す問題等を修正中。完成候補APKのビルド、内容・権限・署名・16KBアラインメント検査、Galaxy S25実機確認が残ります。
Godot完全移植はまだ完了していません。実装済み、検証済み、実機確認済み、配布済みAPKへの反映はそれぞれ区別しています。
```

## 3. 手元のリポジトリとの突き合わせ（Claude・2026-10-12）

- **基準 25270c9 は main の先頭そのもの**（2026-10-04「hd2d/slice を main へ統合」）。red/thaw はそこから 7 コミット＋未コミット。
- **Unity 枝 `codex/hd2d-vfx-motion-20261005`** はローカルの worktree `../deck-rogue-stage41-codex-20261011`。origin には無い。33 コミット・689 ファイル・+146,588 行（森の舞台の素材 `Resources/FullStage*`・`docs/design` 137 件・`Assets/Game` の新規 24 件ほか）。**コミット済みの分は engine も data も触らない**（`unity/Assets/Resources/Data/card-vfx.json` を新設＝札 id → 属性と型。481 件・赤 84）。
- red/thaw と重なる Unity のファイルは Autopilot.cs／BattleScreen.cs／BattleView.cs／GameRoot.cs の 4 つ。コミット済み同士の `git merge-tree` は衝突なし。red/thaw の未コミット分も小さい（3／53／21／14 行）。
- **worktree の未コミットは 2,708 ファイル**。その中に「Stage41」のエンジンのパッチが入っている（表示専用の発生元 `DamagePresentationSource`・数値の控え `CombatPresentationReceipt`・敵の宣言の由来 `EnemyMoveDeclarationPresentation`。「ルールは読まない・RNG と出来事の数は不変」と宣言）:

| ファイル | 追加 | 削除 |
|---|---|---|
| src/engine/combat.ts | 110 | 30 |
| src/engine/effects.ts | 39 | 21 |
| src/engine/events.ts | 18 | 1 |
| src/engine/gears.ts | 1 | 1 |
| src/engine/types.ts | 98 | 16 |
| Runtime/Combat.cs | 117 | 26 |
| Runtime/Effects.cs | 38 | 21 |
| Runtime/Events.cs | 15 | 0 |
| Runtime/Gears.cs | 1 | 1 |
| Runtime/Generated/Types.g.cs | 171 | 3 |

  red/thaw も combat.ts／effects.ts／types.ts／Combat.cs／Effects.cs／Types.g.cs を変えている。合流時は手で衝突を解き、`npm run gen:csharp` で Types.g.cs を作り直し、ゴールデン 13 本を TS と C# の両方で照合し直す。
- **Godot の数字は main と完全に一致**: カード 444・リーダー 15・敵 89・C# 25 ファイル・JSON 14 ファイル・ゴールデン 13 本の手数の合計 2,935。つまり Godot のエンジンは赤の解凍より前の C# の写しで、red/thaw の赤 32 枚（84→106）・チェイン・くべる・失効の行き先は入っていない。red/thaw ではゴールデン `leader_red-9101.json` が 183→249 手に変わっている。
- **赤の新札 32 枚は `card-vfx.json` に無い**。無い札は中立の既定の演出に落ちる。

## 4. 裁定（2026-10-12 ask_user 4件）

1. **Godot へ一本化**（Unity から乗り換え。2026-08-24 の「Unity 一本化・Godot はクローズ」を撤回）。Unity の VFX 枝は Godot へ移し替える前提。
2. **合流の順は red/thaw が先**。ルールを先に main へ入れ、dot の表示専用のパッチはその上に載せ直す。
3. **`card-vfx.json` の赤 32 枚は dot が足す**（一覧は依頼文に）。
4. こちらでやること＝この文書・CLAUDE.md と ledger への記録・dot への依頼文。

## 5. dot の返答（2026-10-12・依頼文への応答）

> PC側の実コミットだけは、dotのPC作業が利用上限で停止したままなので今は実行できません。先にクラウドでエンジンだけの差分と赤32枚のVFX追加を準備します。
>
> red/thaw合流後は、合流先のコミットIDと更新ソースを受け取ってからGodotへ反映します。今の旧基準のテスト合格を、新しい赤のルールまで検証済みとは扱いません。

**合意した手順**:
1. dot＝クラウドで「エンジンだけの差分」（Stage41 の表示専用パッチ）と `card-vfx.json` の赤 32 枚を先に用意する。PC のコミットは利用上限が明けてから。
2. Claude＝red/thaw をコミットして main へ合流（ユーザーの指示で実行）。合流後に dot へ渡す包み: **合流先のコミット ID**・C# エンジン 25 ファイル（`unity/Packages/com.deckrogue.engine/Runtime`）・データ JSON 14 ファイル（`src/data`）・ゴールデン 13 本（`goldens/runs`）・25270c9 からの patch・各ファイルの SHA256 の一覧。dot のクラウドには git の履歴も remote も無いので、ファイルで渡す。
3. dot＝受け取ってから Godot へ反映し、Godot 側でゴールデン 13 本を照合し直す。旧基準（2,935 手）の合格は新しい赤のルールの検証には数えない。

**dot の代わりに Claude が PC でコミット（2026-10-12 ユーザー「あなたがdotのかわりにコミットできない？」）**: worktree `../deck-rogue-stage41-codex-20261011`・枝 `codex/hd2d-vfx-motion-20261005`（2f31050 の上）。コミットの前に worktree で vitest（1189 passed・1 failed＝`src/sim/unity-colors.test` の直書きの色・1 skipped）・`npm run goldens:verify`（13 本 OK）・C# `dotnet run -- verify`（13 本すべて一致）を通した。

| コミット | 中身 |
|---|---|
| `87677aa5c3d4ce8213942cfe6d89475cb7ac7ab1` | **エンジンだけ**（13 ファイル +823/−120）: src/engine の combat・effects・events・gears・types＋multi.test・presentation-source.test／Runtime の Combat・Effects・Events・Gears・Types.g.cs／EngineTests/Program.cs |
| `2a5bc32fe5e93e23312967a5537c48e55e8e5cc4` | **残り全部**（2,695 ファイル +347,769/−604）: Game の .cs 210・Editor・Resources（Art 51・Shaders 28・Data 17＝card-vfx.json ほか）・StreamingAssets・docs/art-bible.md・scripts/audit-vfx-coverage.py・Unity の .meta 2,504 件・packages-lock.json。「採用済み」を意味しない |

未コミットは 0 件。origin には push していない（枝はローカルのまま）。.meta 2,504 件は dot の写しの忠実さを優先して含めた（main は .meta を 55 件しか持たない＝方針が違うので、要らなければ後で外す）。

**dot の返答 2（同日）**: 受入基準＝エンジン `87677aa`・素材と表示 `2a5bc32`（13 ファイル版を基準にし、dot 側の 12 ファイルの分離パッチは不要）／赤 32 枚は純追加で作成済み（火 28・物理防御 4。既存 481 は不変。合流後の正確な札の定義と出来事の出力を見てから本体へ接続）／.meta は枝に残す（GUID と再現性のため。Godot へは選別して取り込む）／unity-colors.test の 1 件は dot が PaperFx の名前付きの色へ小さく直す／合流後に欲しい物＝合流コミット ID・C# 25・JSON 14・ゴールデン 13・25270c9 からの patch・SHA256 一覧・**types.ts／run.ts／map.ts と scripts/gen-csharp-types.ts**（Types.g.cs を生成し直すため）。順序＝red/thaw→main → dot の表示用エンジン差分を載せ直し → TS/C# 照合 → Godot へ同期 → Godot で 13 本再照合。旧基準の合格と新ルールの合格は分ける。
→ 包みは `scripts/pack-engine-for-godot.sh [<commit>] [<出力先>]` で作る（コミットの中身を git show で取り出す＝ID と中身が一致。Runtime 全部・src/data・goldens/runs・src/engine 全部・gen-csharp-types／dump-goldens／verify-goldens・EngineTests・PORTING.md・patch・MANIFEST.md・SHA256SUMS・zip）。

**dot の返答 3（同日）**: 梱包と順序は了解。追加の依頼 2 件＝①package.json・package-lock.json・tsconfig も同梱（型の再生成と TS の検証を同じ依存関係で）②新しい 7 種の出来事（Cascaded／CascadeRetargeted／ImpulseKindled／ImpulseReturned／ImpulseKept／BlazeIgnited／BlazeExtinguished）の実例＝該当コマンド番号・実行コマンド・出来事の順序と対象と発生元が分かる JSON を同じコミットから（ゴールデンに出ない出来事は未出現と明記）。赤 32 枚は純追加候補まで完成。色規定の修正は PhysicalMediumCandidateFx を physical-contact パレットの名前付きの色の参照に（PaperFx へは置換しない。元の 5 色と linear 変換は維持。TS 1,190 passed・型チェック OK・Unity コンパイルと実描画は未検証。雷 R9 の直書きも見つかっているが混ぜない）。zip を受け取るまで Godot を推測で変えない。
→ pack に package.json・package-lock.json・tsconfig×4・vite.config.ts・`scripts/dump-events.ts` を追加し、`events-sample.json`（ゴールデン 13 本の再生）を同梱する。

**dot の返答 4（同日・まとめ）**: 受入基準と順序は一致／zip は git show のコミット固定で OK・**展開した zip だけで型の再生成と 13 本の照合が通ることも確認してほしい**／**旧版の赤は 183 手**（こちらの「185」は誤り。183 が正）・新 249 手・合計 3,001 手は受領ファイルで確認／events-sample.json の形式で OK・**未出現の CascadeRetargeted と ImpulseKept は再生できる戦闘ジャーナルを通常のゴールデンと分けて**／赤 32 枚は純追加候補まで完成（火 28・物理防御 4）／色規定の修正は PhysicalMediumCandidateFx.cs だけ（physical-contact パレットの名前付きの色。隔離スナップショットで 1,190 passed・型 OK・lint 0 errors）／`DeckRogue_Red32_ColorFix_20261012.zip` にまとめた（適用時は before ハッシュを確認）。
→ こちらの対応: ①pack に自己検証（`SELFTEST=1` 既定）＝展開した包みの中だけで npm ci→gen:csharp（Types.g.cs が同梱と byte 一致）→TS ゴールデン照合→tsc(sim)→C# verify。そのために src/sim・src/ui/log.ts・vocab.ts・docs/unity-port.md・scripts/check-unity-colors.ts を同梱。red/thaw 先頭（aebb925）で試して 6 項目すべて OK（14 秒・zip 1.1MB） ②`goldens/events/`（通常のゴールデンと別）に `scripts/find-event-journals.ts` がボットで探した 2 本＝`leader_red-run_red_s2_c-7017-act2-CascadeRetargeted.json`（44 手・コマンド 41: 燎火の使い→火口開き→火花で敵 0 を倒し fromIndex 0→toIndex 1）・`leader_red-run_red_s2_b-7001-act2-ImpulseKept.json`（58 手・コマンド 29: EndTurn で熾火の一閃を残す）。TS/C# とも全手一致 ③共用の純関数 `src/sim/event-scan.ts`（dump-events／find-event-journals が読む）④文書の 185→183 を訂正。

**合流（2026-10-12 ユーザー「mainへの合流していい」）**: red/thaw を 4 コミット（`4552453` ルール＝engine/data/C#/ゴールデン／`1a03f45` 表示・CLI・ボット／`d8e5cb6` Godot 同期の道具／`0cef0d8` 文書）にまとめ、main へ `--no-ff` で合流＝**`8401ec1c4caff08c2480dd9db453b8b98fbd83e4`**（合流前の確認: vitest 1224 passed・goldens:verify 13＋events 2 OK・typecheck OK・lint は既存の警告のみ）。包み `../deck-rogue-godot-sync/8401ec1.zip`（1,251,446 bytes・SHA256 `997310bca507b3c2cdd8eb770b9de7220e866f1b3caf5014f91d7c32baa6e541`）。展開した包みだけでの自己検証 6 項目すべて OK。goldens/runs 13 本 3,001 手（赤 249）＋goldens/events 2 本。events-sample.json は 7 種すべて出現（Cascaded 15・CascadeRetargeted 1・ImpulseKindled 3・ImpulseReturned 85・ImpulseKept 2・BlazeIgnited 10・BlazeExtinguished 10）。

**dot の返答 5（同日）**: `DeckRogue-godot-sync-8401ec1.zip` を取得＝サイズ・SHA256 一致・SHA256SUMS 194 項目すべて一致。新 main 単体の TS/C#・13＋2 本の検証中（表示差分の載せ直し・Godot 同期はまだ）。追加依頼＝**87677aa だけの git format-patch を Drive で**（クラウドに git オブジェクトが無い。13 ファイル版を正確に当てたい）。梱包の指摘＝MANIFEST.md に不完全な UTF-8（`cut -c` がバイトで切った）・記載 189 に対し実際 195 ファイル（生成物 6 件を数えていなかった）。
→ こちらの対応: ①`DeckRogue-codex-engine-87677aa.patch`（146,537 bytes・SHA256 `537dfc85…`）を Drive へ。**親 2f31050 には素直に当たるが、8401ec1 には combat.ts・effects.ts・types.ts・Combat.cs で衝突する**（`git apply --check` で確認）ので ②3-way 用の前後像 `DeckRogue-codex-engine-87677aa-3way.zip`（base＝2f31050・theirs＝87677aa の 13 ファイル＋README・590,134 bytes・SHA256 `560ab6db…`）も Drive へ ③pack の直し（件名は awk で文字単位・ファイル数は生成物込み）＝main `e3bd2cd`。

**dot の返答 5b（同日・返答 5 の再送＋追記）**: **新 main（8401ec1.zip）の検証が通った＝TS/C# とも通常 13 本・3,001 手と追加 2 本・102 手（58＋44）が一致、型の再生成も同一**。Godot 側はスマホ比率の実描画でキャラ・攻撃位置と手札の重なりの解消を確認。新 main を Godot へ反映する工程は 87677aa の正式パッチ待ち（＝返信 6 の Drive の 2 ファイルがまだ dot に届いていない時点の返答）。

**dot の返答 6（同日・衝突解消済みの差分 `DeckRogue_EngineMerge_8401ec1_87677aa_20261012.zip`・99,936 bytes・SHA256 `b5956bfd…`）**: 正規の 87677aa パッチを 8401ec1 へ 3-way で統合（衝突 5 箇所＝TS combat/effects/types・C# Combat/Effects。main 側の nullified・猛り火の条件と発火順・赤の 7 出来事を残して表示情報を追加）。追加の適応 1 件＝ResolveIgniteEffects に実際の札の定義 ID と uid を表示専用の引数で渡す（条件・対象・効果・順・RNG は不変）。Types.g.cs は生成（215 records／107 event types）。dot の検証＝TS/C# 13＋2 本 3,103 手一致・重点テスト 125/125・広いテスト 1,202 合格 1 失敗（CLAUDE.md が zip に無い文書検査）・独立レビュー 3,118 状態で差 0。**Godot 本体へのエンジン・データ同期（39 ファイル）も完了**。UI_ADOPTION_HANDOFF.md に新しい 7 出来事の文・keepUid・summary.ts の予測の移植注意。
→ こちらの検証（同日）: 包みの SHA256SUMS 全一致／patch は 8401ec1 にも main の先頭にも素直に当たる／適用後＝vitest 1247 passed・TS ゴールデン 13＋2 OK・C# verify 15/15・gen:csharp を回しても Types.g.cs 同一・typecheck OK・lint 既存の警告のみ・dot の candidate_source_hashes.json と 190 ファイルがバイト単位で一致。**main `dadcc1600a03ccd98b650aad8013a1d3905e607e` としてコミット**（dot の patch をそのまま適用）。これで「red/thaw→main→表示差分の載せ直し→TS/C# 照合→Godot 同期」まで両側で完了。残り＝Godot での再照合（dot）・新しい赤の操作と表示の移植（dot）・Unity の中継のコンパイル確認（Claude）。
→ Unity の中継のコンパイル（`scripts/unity-win.sh compile`・main dadcc16）: 0 エラーで完了（2026-10-12）。包み `DeckRogue-godot-sync-dadcc16.zip`（1,296,334 bytes・SHA256 `89d9f1b5…`・Drive id `1AM2otkMoowDnSzAvNX-gheSAGfQuO0i8`）を Drive へ。
