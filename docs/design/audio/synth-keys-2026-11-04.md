# 合成で足した鍵の一覧と音色の意図（2026-11-04 束 D・音）

「見た目・UX・演出を一晩で引き上げる」の音の束。朝の **#5 音の点検**（品質スプリント `docs/design/quality-sprint-2026-10-28.md`）の材料。
素材は `Resources/Audio/sfx` に無いので全部 `Synth.cs` の合成。本物に差し替える時は **`Resources/Audio/sfx/<sfx 名>.ogg` を置くだけ**（`Audio.Load` は素材を先に見る。表の鍵は変えない）。

音の肌は `docs/audio-design.md` §11「静かで物悲しい・夜の坑」に合わせた:
- **硝貨・着地・刻み** ＝ 短い金属の粒（基音＋倍音 1 本・減衰 60ms）。
- **当たりの 3 段** ＝ ノイズの短い打撃を低域の量で分ける（小は乾いた紙・中は胴・大は床）。
- **とどめ** ＝ 低い打撃＋高い裂け。
- **勝利の帯** ＝ 旋律を持たない和音 1 つ（ジングル無しの裁定 2026-09-14 を守る）。
- **紙の swish** ＝ 帯域ノイズの掃き（`NoiseSweep`＝ローパスの係数を滑らせ、低域を引く）。
- **環境音** ＝ 8 秒ループ（8.5 秒を作って末尾 0.5 秒を先頭に焼く `Loopify`）。
- **鼓動** ＝ 2 拍の低い太鼓を `Audio.Heartbeat` が rate の間隔で打つ。
- ラウドネスは `Finish` の gain 0.4〜0.95（既存の light_gain 0.55・hit 0.9 の間）。

## 1. Audio.cs に入れた共通の口（中身）

| 口 | 中身 |
|---|---|
| `Duck(depth, hold, release, attack)` | BGM の目標音量に `(1-沈み)` を掛ける。沈みは `Update` の包絡（attack で深さへ・hold 保ち・release で 0 へ）。重ねたら深い方・長い方・短い attack。時計は `Tween.UnscaledDt` の積算 `_clock`（ヒットストップ・とどめのスロー中も実時間）。 |
| `Muffle(seconds)` | BGM の子 GO に `AudioLowPassFilter` 1 本。0.12 秒で 22000→700Hz、`seconds` で 700→22000 へ（対数補間・戻りは InQuad）。0 で即戻し、終わると filter を無効化（CPU）。 |
| `Ambience(key)` | 環境音の AudioSource 1 本（`ambience.xxx` の鍵）。鍵が変われば 0.4 秒で沈め 0.4 秒で出す（1 本なので重ねずに谷で繋ぐ。合計 0.8 秒）。同じ鍵なら何もしない・null で止める。音量は Master×BgmVol×表の volume（設定の BGM のスライダに含める）。 |
| `Heartbeat(on, rate)` | 専用 AudioSource 1 本。`1/rate` 秒ごとに `heartbeat` を打つ。rate 1.3 以上は音量 0.5・pitch 1.08（致死級）、それ以下は 0.35。20 秒続いたら音量を半分（絵は続ける）。 |
| `KeyPitched(key, pitchMul, volMul)` | `Play` に音程の倍率を渡す（`PlayEx`）。チェインの段・多段の積み上がり・数え上げの刻みのラチェット。 |
| 同じ鍵の最短間隔 | `SfxEntry.MinGap`（既定 0.05 秒・表の `minGap` で上書き・0 で間引かない）。`_lastAt[key]` を `_clock` で見る。Verbose なら間引いた音もログに。 |
| 音量の揺らぎ | `SfxEntry.VolJitter`（既定 ±8%・表の `volJitter`）。jitter 0 の鍵は揺らさない。乱数でなく黄金比の小数部で散らす（撮影で再現する）。 |
| BGM 2 本 | 子 GO「Bgm」に AudioSource 2 本。切り替えは 0.8 秒の等パワー（sin/cos）で両方鳴る。ループ版でない曲（表の `_overlap`＝act1・title）は `loop=false` にして残り 1.0 秒で同じ曲をもう 1 本から始めて 1.0 秒重ねる（継ぎ目の谷を消す。PeriTune のループ版に差し替えたら `_overlap` から外す）。 |
| `Hover()` | 表の `ui.hover`（minGap 0.06）を通す（直書きをやめた）。 |

AudioSource の数: 効果音のプール 12 ＋ BGM 2 ＋ 環境音 1 ＋ 鼓動 1 ＝ 16。AudioMixer は入れていない。

## 2. 契約の鍵（戦闘）

| 鍵 | sfx | 音色 | 呼ぶ場面（想定） |
|---|---|---|---|
| hit_tier1 | 合成 | ノイズ 80ms（lp .5）＋ sine 220→120。乾いた紙の打撃 | 当たり <8 |
| hit_tier2 | 合成 | ノイズ 120ms ＋ sine 170→70 ＋ 薄い square。胴が鳴る | 中 |
| hit_tier3 | 合成 | ノイズ 220ms（lp .45）＋ sine 130→38 ＋ square ＋ 後乗りのノイズ。床が震える | 15 以上か最大 HP の 1/4 |
| finish_blow | 合成 | sine 110→30 の打撃 ＋ 帯域ノイズの裂け（hp .6）＋ saw 2400→900 | とどめ（Duck 0.5/0.35/0.3/0.05 と） |
| enemy_down | 合成 | 砂がこぼれるノイズの下降 ＋ 低い胴 ＋ 砕けた粒 3 | 敵が崩れる |
| enemy_down_boss | 合成 | 同じ構成を 1.3 秒・80→25Hz・tri 55Hz の残り | 幕ボスが崩れる |
| coin_fly | 合成 | sine 2400→3000 60ms ＋ tri 3600 | 硝貨が飛び出す |
| coin_land | 合成 | 2600Hz の粒（倍音 2 本・60ms） | 上部バーに着く＝数え上げの刻み（KeyPitched 1+0.03i） |
| mana_land | 合成 | sine 1320→1480 ＋ tri 2640。硝子 | 魔素が着く |
| victory_band | 合成 | C-E-G-C-D の和音（sine）＋ tri 131Hz・1.7 秒 | 勝利の帯（Duck 0.35/1.4/0.8/0.15 と） |
| tally_tick | 合成 | tri 1800 60ms ＋ 紙の粒 | 戦いの記録の 1 行（KeyPitched 1+0.04i） |
| lethal_mark | 合成 | square の点 ＋ sine 1760/2637 の細い鈴 | 倒せる印が点く |
| preview_tick | 合成 | ノイズ 40ms ＋ sine 1200→900 | 予測の帯が出る |
| cascade_flip | 合成 | tri の粒 3 つ 1200/1400/1600 を 60ms 刻み。歯車のラチェット | チェインがめくる |
| cascade_hit | 合成 | tri 1600 の粒 ＋ square 600→1200 ＋ sine 300→150 の胴 | チェインの当たり（KeyPitched 1+0.08n） |
| blaze_ignite | 合成 | ノイズ lp .2→.7 を上げながら saw 200→900。炎が立つ | 猛り火が点く |
| blaze_out | 合成 | sine 600→200 ＋ 短い息 | 消える |
| aether_gain | 合成 | sine 880→1760 ＋ 1320→2640 ＋ tri 3520。上へ抜ける硝子 | 霊気 |
| combo_up | 合成 | tri 1320 ＋ sine 2640 80ms | 多段の積み上がり（KeyPitched 1+0.06n） |
| danger_in | 合成 | sine 90→45 の一打 ＋ sine 440→220 の沈み | HP 3 割を割った |
| defeat_fall | 合成 | sine 220→50 0.7 秒 ＋ ノイズ ＋ 0.55 秒で床の鈍い音 | 倒れる（PlayerDied もこれ） |
| card_lift | 合成 | 帯域ノイズ lp .3→.6（hp .3）＋ sine 700→1000 | 札を持ち上げる |
| card_cancel | 合成 | 帯域ノイズ lp .6→.25 ＋ sine 900→550 | 戻す |
| energy_spend | 合成 | sine 1400→700 ＋ tri 2100。玉が沈む | エナジーを払う |
| enemy_telegraph | 合成 | pulse 300→600 ＋ sine 150。旧 buff の静かな版 | 敵の意図が実行の直前に脈打つ（EnemyIntentDeclared も 0.35 で） |
| trap_fire_big | 合成 | square 1200→400 の発条 ＋ sine 2400 ＋ ノイズ ＋ sine 120→40 の床 | からくりの発動の山 |
| status_apply | debuff（既存） | — | 状態異常が付く |
| impulse_burn | 合成 | 帯域ノイズ lp .5→.15 ＋ saw 300→700 ＋ sine 500→250。紙が燃え落ちる | 衝動の失効・くべる |
| heartbeat | 合成 | sine 70→45 120ms を 0.16 秒置きに 2 発 | Audio.Heartbeat |

## 3. 契約の鍵（画面）

| 鍵 | sfx | 音色 |
|---|---|---|
| card_flip | 合成 | 帯域ノイズ lp .4→.8（hp .4）＋ tri 900→1400。紙が返る（minGap 0.03） |
| rare_reveal | 合成 | 1047/1319/1568/2093 の和音を 50ms ずらし ＋ tri 3136 ＋ 高い帯域ノイズの煌めき。旋律は無い |
| relic_get | relic（既存） | 宝箱 |
| upgrade_shine | 合成 | 金床の一打（square ＋ ノイズ）の後に sine 2093/3136 の細い鈴 |
| rest_heal | 合成 | sine 880 ＋ 1320 100ms。温かい刻み（KeyPitched で上げる） |
| buy_coin | gold（既存） | 硝貨 |
| sold_stamp | 合成 | ノイズ lp .35 ＋ sine 180→90 ＋ square。判を押す |
| fuse_melt | 合成 | ノイズ lp .7→.15 ＋ sine 900→220 ＋ tri 440→110 → 0.5 秒で sine 1320 の鈴 |
| act_title | act_start（既存） | 幕の題字 |
| paper_swish | 合成 | 帯域ノイズ lp .5→.9（hp .3）0.22 秒。音量 0.3・minGap 0.1。閉じは pitch 0.85・開きは 1.1 |
| land_card | 合成 | tri 1320 50ms ＋ 紙 |
| land_relic | 合成 | sine 1500 ＋ 3000 ＋ 金属の粒 |
| event_gain | 合成 | tri 660 → 990（上がる二音） |
| event_loss | 合成 | tri 660 → 440 ＋ 低い 220（下がる二音） |
| map_arrive | 合成 | tri 220 ＋ 330 ＋ 柔らかい床。低い木の音 |
| button_appear | 合成 | tri 990 60ms ＋ 紙 |
| boss_start | 合成 | sine 90→35 の太鼓 0.9 秒 ＋ 息 ＋ square 110 の低い持続 1.2 秒 ＋ sine 55 |
| block | block（既存） | Presenter の `Audio.Play("block")` 直書きを表に移す口 |

## 4. 環境音（Audio.Ambience）

| 鍵 | sfx | 中身（8 秒ループ） |
|---|---|---|
| ambience.forest_night | amb_forest_night | 風（2 段ローパス lp .08・0.2Hz の揺らぎ）＋ 虫（4200/5600Hz・30ms の粒を 3〜5 連・11 群・遠近で音量を散らす） |
| ambience.campfire | amb_campfire | ノイズ lp .35 の低い揺らぎ（0.4Hz）＋ 短い爆ぜ 26 ＋ 低い弾け 5 |
| ambience.cave | amb_cave | ごく薄い風 ＋ tri 55Hz の唸り ＋ 水滴 6（下降の sine と 2 回の残響） |
| ambience.ruins | amb_ruins | 風（0.15Hz）＋ saw 41.2/61.7Hz のわずかに外した唸り ＋ 遠い金属の残り 2 |

呼ぶ側（GameRoot.UpdateBgm の隣が想定）: Map/Reward/Event/Shop/Workshop/Combat → 幕1 `ambience.forest_night`・幕2 `ambience.cave`・幕3 `ambience.ruins`、Campfire → `ambience.campfire`、Title → `Ambience(null)`。
合成の虫は「安っぽい」と言われる可能性がある＝聴いて不評なら USFX ELEMENTS/Fire の Campfire loop 等（`docs/audio-design.md` §9 の在庫）に差し替える。

## 5. 表で直した既存の鍵

- `EnemyIntentDeclared`: null → `enemy_telegraph` 0.35（A17）。
- `PlayerDied`: hit_big → `defeat_fall`（A11）。敗北は呼ぶ側が `Duck(0.15, 2.0, 0, 1.2)` ＋ `Muffle(3)` で BGM を遠くする想定。
- `ui.hover`: minGap 0.06（Audio.Hover が通す）。`TurnStarted`/`TurnEnded`: minGap 0（音楽的な鍵は間引かない）。
- `DeathSaved` が 2 行あったのを 1 行に。`CombatEnded`/`ui.lose` は null のまま（理由を `_note` に）。
- **型名の鍵は足していない**（Cascaded・BlazeIgnited・Impulse* 等）。`Presenter.Show` は `TableSound` で鍵があれば音だけ鳴らして return するので、型名を足すと絵の case が動かなくなる。絵つきで扱う出来事は Presenter が契約の鍵を `Audio.Key`/`KeyPitched` で呼ぶ。

## 6. 残り（朝の点検で）

- 一周の動画を音つきで撮って、swish・環境音・硝貨の刻みの音量を詰める（`loop-video.sh`）。
- 当たりの 3 段は Presenter が `hit_tier*` を呼ぶようになってから既存の `DamageDealt.player*`（USFX の BLUNT）と聴き比べ、どちらを残すか決める。
- `hd2d_*.wav` の .meta 無しは触っていない（Resources の .meta は Unity が作る）。
