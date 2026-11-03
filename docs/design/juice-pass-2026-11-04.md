# 見た目・UX・演出の磨き（2026-11-04 夜・自律作業の記録）

ユーザーの指示（就寝前）: 「見た目・UX・演出の課題をまとめて、単独で解決できる箇所を全て実装・改善。大きく見た目や仕組みを変えてもいい。大手ゲームデベロッパー並みの UI/UX・面白さ・ドーパミンが出る演出」「実装までしちゃっていいよ」。
残件の一覧は `docs/remaining-2026-11-04.md`。凍結は品質スプリントのまま（engine・data・バランス・新色・人間ラン・dot への新しい依頼）。

## 0. 進め方

1. 棚卸し: 8 つの系（戦闘の演出・戦闘 UI・戦闘以外の画面・音・舞台とカメラ・部品と規約・撮影道具・物差し）を並列の読み手で読み、今ある物・差し込み口・落差を構造化する。
2. 設計: 落差を「快感への効き × 手間」で並べ、ファイルの重なりが無い束（レーン）に分ける。
3. 実装: 束ごとに入れ、コンパイル → ビルド → 撮影（コマ送り）→ 見る → 直す。
4. 記録: ここに「何を・どこに・どう見えるか」と撮ったシートを残す。朝の採点（1〜5）はユーザー。

## 1. 直す前の姿（main 7694708e のビルド・`scripts/juice-states/before.txt` の 16 場面）

- 画面の一覧 `docs/design/quality-sprint/juice-before-screens-2026-11-04.png`（題・支度・地図・戦闘・報酬・遺物・店・焚き火・出来事・工房・走破・敗北）
- とどめ→報酬 `juice-before-kill-2026-11-04.png`（4 コマ/60fps ごと 25 枚）: 当たり → 数字「6」の輪 → 敵が粒になって消える → **空の舞台が 0.4 秒** → 幕 → 報酬の画面に評価の窓が被さる。**勝利の帯も「もらえた」も無い**（`GameRoot.cs:314` の `!Stage.CombatWorld` の門で、幕1＝箱庭では決着の余韻が一度も出ていなかった）。
- 敵の番 `juice-before-enemyphase-2026-11-04.png`: 「敵の番」の帯 → 狼の当たりで**全画面が真っ赤**（α 0.35 の板）→ 小さな「-7」。揺れ無し（箱庭では `Stage.Shake` が no-op）。
- 走破・敗北: 題 1 行・統計 1 行・札の一覧・ボタン 2 つ（62 行）。遺物は文字。「もう一度」が無い。

## 2. 棚卸しの結果（8 系・gaps 125 件。全文 `docs/design/quality-sprint/juice-inventory-2026-11-04.json`）

| 系 | 要点 |
|---|---|
| 戦闘の演出 | 出来事の可視化は網羅的（浮き数字・判・輪・幽霊の札・崩れ）。だが**幕1（箱庭）では勝利の帯・登場・名前の帯・揺れ・ズーム・技の光が全部門で止まり、敵の白い点滅は 0.10 倍**。数字は 36/46/50 の 3 段で量に比例しない。多段の積み上がり・G と魔素が飛ぶ「もらえた」・赤の新しい出来事（チェイン・猛り火・くべる）の絵と音が無い |
| 戦闘 UI | 札は平面の板（傾き・影・慣性なし）。**狙った敵の HP バーに「この札でここまで減る」の帯と「倒せる」の印が無い**（StS2 の核）。エナジーを払った手応え・狙われた敵の反応・HP バーの残像・状態異常の札の出方が無い |
| 戦闘以外 | 動きの文法 5 条は入っている。報酬の札が表向きで即・レアが光らない・遺物に名前の帯が無い・店で買っても硬貨も売切も無い・焚き火の休む／鍛えるが 0 コマ・工房の合成が 0 コマ・幕の題字が無い・**走破／敗北が事務的**・評価の窓が勝利の流れを断つ・設定に揺れの強さが無い |
| 音 | 鍵 88（実音 63）。**BGM の抑え（ダッキング）が無い**・クロスフェードが偽（1 本）・とどめ／撃破／硬貨／鍛える／幕／紙の音が無い・チェイン／猛り火が無音・低 HP の鼓動が無い・環境音（森の夜・焚き火）が無い・同じ音の連打に揺らぎが無い。勝敗のジングルは鳴らさない裁定（守る） |
| 舞台 | **箱庭でカメラが一切動かない**（Shake/ZoomPunch/Dolly が早期 return）。とどめ・勝利・敗北・危険・猛り火・手番・登場・撃破で舞台（露出・霧・灯・ビネット・彩度）が無反応。裁定「技でカメラが変わるのは酔う」→ ズームは使わず、閃光・時間・光・音で埋める |
| 部品 | Tween/ScreenFx/UiKit/PaperFx は揃っているが共通口が無く各画面が手書き（脈が 2 重・数え上げが G 専用・FlyTo が直線）。粒は Image の使い捨て（プール無し）。揺れの共通口が無い |
| 撮影 | STATE の口 7 つ（play/endplay/fire/fx/enter/usegear/uxdrag）は戦闘だけ。報酬を取る・休む・買う・倒れる・幕の題字を撮る口が無い。既定 `ScreenFx.Instant` で切り替えの動きは写らない |
| 物差し | 本家の作法 35 項目（札を出す・当たり・致死・勝利・報酬・レア・金・間・画面の反応・数字・連鎖・敗北・題の待機）。守る裁定: 場面は舞台・店主なし・戦闘以外にキャラなし・StS2 の構図を写さない・字は Noto Sans JP・色の役割・通過した画面の配置は変えない |

## 3. 設計（束と順）

共通の口を先に置き（be403c4e: `FxSettings`・`StageBeat` の空の口・`Audio.Duck/Muffle/Ambience/Heartbeat/KeyPitched` の空の口・決着の門から `!Stage.CombatWorld` を外す・`Presenter.PrevRun`）、ファイルの持ち主が重ならない 7 つの束を並列に実装する。音の鍵の名前は契約として固定（`hit_tier1〜3`・`finish_blow`・`enemy_down`・`coin_fly/coin_land/mana_land`・`victory_band`・`tally_tick`・`lethal_mark`・`preview_tick`・`cascade_flip/cascade_hit`・`blaze_ignite/blaze_out`・`aether_gain`・`combo_up`・`danger_in`・`defeat_fall`・`card_lift/card_cancel`・`energy_spend`・`enemy_telegraph`・`trap_fire_big`・`status_apply`・`impulse_burn`・`card_flip`・`rare_reveal`・`relic_get`・`upgrade_shine`・`rest_heal`・`buy_coin`・`sold_stamp`・`fuse_melt`・`act_title`・`paper_swish`・`land_card/land_relic`・`event_gain/event_loss`・`map_arrive`・`button_appear`・`boss_start`・`ambience.forest_night/campfire/cave/ruins`）。

| 束 | 持ち主のファイル | 主な狙い |
|---|---|---|
| A 戦闘の演出 | Presenter*.cs・Tween.cs・Combat*.cs | 勝利の山（帯→硬貨が飛ぶ→記録）・とどめの閃き・数字の 4 段・多段の積み上がり・被弾は縁・鼓動・登場・赤の出来事・意図の脈・敗北 |
| B 戦闘 UI | BattleScreen・BattleView・CardView・CardPopup・GearUi・Tooltip・LightUi | 予測の帯と「倒せる」・札の傾きと影・エナジーの輪・狙われた敵の反応・HP バーの残像・消滅は燃える・状態異常の出方 |
| C1 報酬〜工房 | RewardScreen・ShopScreen・CampfireScreen・EventScreen・WorkshopScreen・RunUi・ScreenFx・UiKit | 札が返る・レアの光・遺物の帯・硬貨と売切・休むの数え上げ・鍛えるの光・合成が溶ける・飛びの弧と着地 |
| C2 節目 | EndScreen・GameRoot・SettingsUi・Feedback*・TitleScreen・MapScreen・MapForest | 走破／敗北の作り直し・幕の題字・評価の窓の遅延・設定「演出」・環境音の切り替え・地図の着いた・題の呼吸 |
| D 音 | Audio・Synth・audio.json | ダッキング・クロスフェード・契約の鍵を合成で全部・環境音 4・鼓動・揺らぎ |
| E 舞台 | StageBeat・Stage*.cs・look_act1 | 箱庭でも揺れ・白い点滅・とどめの露出・勝利／敗北／危険／猛り火／手番／登場の光 |
| F 撮影 | Autopilot・juice-states/after.txt・juice-sheet.py | motion=1・take/rest/buy/lose/outcome の連写・シートの道具 |

束が終わったら: compile → 赤を直す → build → `after.txt` を撮る → シートで見る → 直す → commit → push。

## 4. 実装

## 5. 直した後の姿と残った事
