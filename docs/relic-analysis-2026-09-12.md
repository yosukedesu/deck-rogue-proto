# レリックの本家分析と改善提案（2026-09-12）

ユーザー「レリックを本家の様に充実させたい。本家のレリックのゲームデザインを分析して今のレリックセットとの差分を見つけて改善してほしい」への提案書。
一次資料: 本家 StS1 全181個（`docs/sts2/relics-sts1.json`）・StS2 全296個（`docs/sts2/relics-sts2.json`＝公開アーカイブの説明文、`docs/sts2/relics-sts2-hooks.json`＝ローカルにデコンパイル済みの v0.103.3 のコードから抽出したレア度・プール・フックの override）・うちの39個（`src/data/relics.json`）。
付録A〜Cは3本の並列分析（StS1 の文法／StS2 の変化／うちとの差分監査）の全文。本文はその統合。

## 0. 結論（要約）

1. **在庫が供給に対して薄い**。うちは全供給源が3択（本家はボスだけ3択）で露出が本家の3倍、1ランで在庫39をほぼ全部見る。幕3の3択は common／rare／boss で「断った顔」が期待2/3。本家換算の実効在庫は約13個。
2. **型が偏っている**。39個の51%がラン定数（B型）、戦闘内で鳴るのは19個、うち「戦闘開始時に一発」が12（31%。本家は8%）。本家の中核である**カウンター／リズム（N枚ごと・Nターン目）・戦闘で最初のX・ターン終了時・キル・シャッフル・報酬の質（卵）・取得時の一回効果・マップ／部屋・状態異常への応答**が全部0〜1。
3. **色固有が緑とからくりだけ**（青赤白黒は0）。本家は各キャラ8〜11個で、固有資源を「開幕から乗せる」か「出来事の接着剤」にする。うちの `RelicDef` には色ゲートすら無い。
4. **ボス層は「上限+1＋税」が5/8**。本家 StS2 は Ancient 100個のうちエナジー系は15%で、85%が一回効果・ルール改変。人間#7b「全部デメリット付きだと嬉しくない」の受け皿が3個しかない。
5. **経済系が23%**（本家6%）で、しかも全部「勝利時」か「ショップ」。ノード単位・行動単位の経済（階ごと・?ごと・札追加ごと）が0。

## 1. 本家の文法（StS1・StS2 の要点）

- **層と供給源が一体**。StS1: C36/U37/R34/Boss30/Shop20/Special20/Starter4。StS2: C30/U40/R50/Shop30/Event35/Ancient100/Starter10＝**逆ピラミッド（レアの種類が最多）**でレア1個あたりの提示率を下げ「同じレアを二度見ない」。
- **コモン＝無条件で小さく開幕か戦闘外**（StS1 の69%が戦闘中の判断を要さない）。**アンコモン＝条件とリズム**（カウンター14個の半分）。**レア＝出来事フックとルール改変（50%）＋ビルドアラウンド**。**ボス（Ancient）＝一回効果53%・ルール改変・エナジー+1は必ず税**。**ショップ＝ビルド部品・保険・デッキ操作、エナジーは売らない**。**イベント＝呪いや代償と対、ハズレも刷る**（StS2 は Event 層の4割が「物語の器」）。
- **カウンターの数字は 3 と 10**（1ターン内3枚・周期3ターン・累積10枚）。ターン指定の一発は T2→ブロック14・T3→18（StS2 は T2/T3 発火が6個＝「T1で終わらない戦闘」前提）。
- **「初回だけ」でブレーキ**（戦闘の初撃・初回HP損失・ターンの初回捨て）。StS2 は「戦闘で最初のXを倍」型が10個に増えた。
- **StS2 の追加パターン**: エンチャント（札への恒久印＝鍛えの第2チャンネル）15個／取得時一回効果73個（25%）／時限・消耗・変身（5戦で消える蝋燭・3戦ごと溶ける玩具・5体撃破で変身）／ラン進行で育つ（休むたび最大HP+5）／エリート限定強化と経路改変で「踏む理由」を数字に／「少なく打つ」への報酬／免疫は全廃→上限と割合（1ターンHP損失20まで・ブロック10持ち越し）。
- **StS2 のコードのフック分布**（295クラス。上位）:

| フック（override） | 個数 |
|---|---|
| `AfterObtained` | 80 |
| `AfterCombatEnd` | 39 |
| `AfterRoomEntered` | 34 |
| `AfterSideTurnStart` | 30 |
| `AfterCardPlayed` | 28 |
| `BeforeSideTurnStart` | 21 |
| `IsAllowed` | 20 |
| `BeforeCombatStart` | 18 |
| `ModifyMaxEnergy` | 11 |
| `AfterPlayerTurnStart` | 10 |
| `ModifyHandDraw` | 9 |
| `BeforeTurnEnd` | 8 |
| `TryModifyCardRewardOptionsLate` | 8 |
| `TryModifyRewards` | 6 |
| `AfterTurnEnd` | 6 |
| `AfterDamageReceived` | 6 |
| `BeforeHandDraw` | 6 |
| `AfterCombatVictory` | 5 |
| `BeforeCardPlayed` | 5 |
| `AfterCardChangedPiles` | 4 |
| `AfterCardExhausted` | 4 |
| `ModifyExtraRestSiteHealText` | 4 |
| `ModifyDamageAdditive` | 4 |
| `ModifyMerchantCardCreationResults` | 4 |

（AfterObtained＝取得時一回・AfterRoomEntered＝部屋に入った時＝マップ／経済系・IsAllowed＝制約系。うちの器で言えば B型の一回効果と run 層の「ノード単位」フックが最も薄い）

## 2. 差分（うちの39個 → 本家の型）

穴の一覧（詳細は付録C §3）: G1 カウンター0／G2 ターンN固定0／G3 シャッフル0／G4 初回だけ0／G5 ターン終了時0／G6 タイプ参照（物理・呪文・置物）0／G7 被弾→資源 薄／G8 被ダメ経路 薄／G9 キル0／G10 報酬の質（卵）0／G11 取得時変成 薄／G12 手札・エナジー規則改変0／G13 焚き火の第3選択肢0／G14 マップ・部屋0／G15 状態異常応答0／G16 ノード単位の経済0／G17 烙印の資産化0／G18 敵デバフの増幅 薄／G19 非エナジーのボス 薄／G21 青赤白黒0／G27 うち固有（工房・からくり壊し・温存・敵ギミック）0。

## 3. 提案

> **実装状況（2026-09-12 同日）**: ユーザー裁定＝在庫≈94・供給は据え置き（3択）／新機構は第1波を全部／裁定4件（翼の靴・意図隠し・工房への口・焚き火の第3選択肢＝レリック限定）も全部／凍結色の固有レリックは解凍時。
> 実装した在庫は 39→**102**（C22/U24/R22/Boss17/Shop12/Event5。色固有16は未実装ぶん）。§3-3 の案からの差し替え: 赤べこは「開幕勢い+N」が多段に乗りすぎるため見送り、代わりに投げ刃の束（成長）／表裏の帯／干からびた手／ルーンの立方体／自ら固まる粘土を刷った。ボス層は非エナジー8（角錐・星読みの盤・空の鳥籠・呼び鈴・古代の匣・氷菓）＋エナジー税3（円蓋・首輪・融合の鎚）を追加。
> 機構・レリック名の一覧は `src/data/relics.json`、機械固定は `src/engine/relics-2026-09-12.test.ts`、CLAUDE.md「レリック」行に要約。未了＝新レリック63個のドット絵（PixelLab）。

### 3-1. 在庫の目標と層（本家の1/3縮尺・逆ピラミッド）

| 層 | いま | 目標 | 増分の中身 |
|---|---|---|---|
| Common | 12 | **22** | 無条件で小さい開幕・毎T・T2/T3 の時計・ノード経済 |
| Uncommon | 11 | **26** | カウンター（3枚ごと・10枚ごと）・タイプ参照・初回だけ・色固有の生成型 |
| Rare | 5 | **20** | 出来事フック（キル・シャッフル・被弾・消滅）・ルール改変・色固有の接着剤型・ビルドアラウンド |
| Boss | 8 | **12** | 非エナジー4追加（Runic Pyramid 型・Ice Cream 型・Astrolabe 型・Calling Bell 型）。エナジー+税は5のまま |
| Shop | 3 | **8** | 卵（報酬の質）・Chemical X（X札+2）・保険（状態異常の上限）・ビルド部品 |
| Event | 0 | **6** | 呪い（烙印）と対の器・ハズレ枠・「次の1戦だけ」のお茶型 |
| 合計 | 39 | **≈94** | 色固有（青赤白黒 各4＝16）は Common/Uncommon/Rare に内数 |

供給は据え置き（3択）でも、この在庫なら1ランで見る顔が在庫の4割に落ちる（本家並みの「同じレアを二度見ない」帯）。

### 3-2. エンジンの新機構（第1波＝軽くて穴が大きい順）

| # | 機構 | 開く型 | 規模 |
|---|---|---|---|
| N0 | `RelicDef.colors` の色ゲート | 色固有レリック全部 | 1行 |
| N1 | `every: N` ＋ `everyScope: turn/combat`（置物インスタンスのカウンタ） | 3枚ごと・10枚ごと・3ターンごと（Kunai/Shuriken/Fan/Ink Bottle/Happy Flower） | M |
| N2 | `once: combat/turn` | 戦闘で最初のX（Akabeko/Centennial/Vambrace 型） | S |
| N3 | 条件 `turn: N` | T2/T3/T7 の時計 | S |
| N4 | `onTurnEnd` トリガー＋条件（ブロック0・攻撃0枚・プレイ3枚以下） | Orichalcum/Art of War/Pocketwatch | S〜M |
| N5 | 次ターン繰り越し（draw/energy/block） | 被弾→ドロー・少なく打つ→次T | M |
| N6 | `onShuffle` | Sundial/Abacus | S |
| N7 | `onEnemyDied` | Gremlin Horn | S |
| N12 | `addCardToDeck` 一本化＋ `upgradeOnAdd`（卵） | 報酬の質 | M |
| N13 | 取得時変成（transform/remove/upgradeType/healFull/relics+brand） | Astrolabe/Empty Cage/Whetstone/Lee's Waffle/Calling Bell | S〜M |
| N11 | C型キー: retainHand・energyCarry・blockKeep・drawPerTurnBonus・playCap・xBonus・hpLossReduce・maxHpLossPerTurn | Runic Pyramid/Ice Cream/Calipers/Serpent/Choker/Chemical X/Tungsten Rod/Beating Remnant | S each |
| N10 | 状態異常の上限・割合（免疫は作らない＝StS2 準拠） | 弱体/虚弱/脆弱の倍率・負傷上限 | S |
| N16 | マップ／部屋の B型（階ごと金・?ごと金・?→宝箱周期・ショップ回復） | Maw Bank/Ssserpent/Tiny Chest/Meal Ticket | S |

第2波（裁定が要る）: 焚き火の第3選択肢（発掘＝レリック／鍛錬＝成長／除去＝Peace Pipe 型は「除去はショップ専売」裁定と衝突）・意図を隠す Runic Dome 型（蜃気楼撤去と鏡像）・Wing Boots（経路無視＝「エッジも可視」と要整合）・工房への口（合成を1幕2回・素材の鍛え継承強化）。

### 3-3. レリック案（第1波で刷る分・約55）

記法: 層／名前＝効果〔本家の対応・器〕。名前は世界観（古代の遺物＝坑で出る／行商の品＝店）。

**Common（+10）**
- C／**二の刻の鐘**＝2ターン目の開始時にブロック14〔Horn Cleat・N3〕
- C／**三の刻の鐘**＝3ターン目の開始時にブロック18〔Captain's Wheel・N3〕※Rare でもよい
- C／**水銀の砂時計**＝毎ターン開始時、敵全体に3ダメージ〔Mercury Hourglass・既存〕
- C／**赤面の面**＝戦闘開始時、敵全体に威圧1〔Red Mask・既存〕
- C／**鉄の靴底**＝ターン終了時にブロックが0なら6得る〔Orichalcum・N4〕
- C／**幸せの花**＝3ターンごとに一時エナジー+1〔Happy Flower・N1〕
- C／**坑夫の帳簿**＝1行進むごとに12G。ショップで何か買うと止まる〔Maw Bank・N16〕
- C／**古い地図の切れ端**＝?に入るたび+30G〔Ssserpent Head・N16〕
- C／**小さな宝箱**＝?の4回目は必ず宝箱〔Tiny Chest・N16〕
- C／**初撃の赤べこ**＝戦闘の最初の攻撃札に+8ダメージ〔Akabeko・N2〕

**Uncommon（+15）**
- U／**苦無**＝1ターンに攻撃札を3枚プレイするたび成長+1〔Kunai/Shuriken・N1 turn〕
- U／**飾り扇**＝1ターンに攻撃札を3枚プレイするたびブロック4〔Ornamental Fan・N1〕
- U／**ペーパーナイフ**＝1ターンに呪文を3枚プレイするたび敵全体に5〔Letter Opener・N1〕
- U／**墨壺**＝札を10枚プレイするたび1ドロー〔Ink Bottle・N1 combat〕
- U／**ヌンチャク**＝攻撃札を10枚プレイするたび一時エナジー+1〔Nunchaku・N1〕
- U／**日時計**＝山札を3回切り直すたび一時エナジー+2〔Sundial・N6+N1〕
- U／**小鬼の角笛**＝敵を倒すたび一時エナジー+1と1ドロー〔Gremlin Horn・N7〕
- U／**百年の謎かけ**＝戦闘で最初にHPを失った時、次のターンのドロー+3〔Centennial Puzzle・N2+N5〕
- U／**自ら固まる粘土**＝被攻撃後にHPを失っていたらブロック3〔Self-Forming Clay・既存〕
- U／**懐中時計**＝プレイが3枚以下で終えたターンの次のターン、ドロー+3〔Pocketwatch・N4+N5〕
- U／**兵法書**＝攻撃札を1枚も打たずに終えたターンの次のターン、一時エナジー+1〔Art of War・N4+N5〕
- U／**干からびた手**＝置物を場に出すたび次のカード-1〔Mummified Hand・既存〕
- U／**鳥面の壺**（白）＝置物を場に出すたびHP+2〔Bird-Faced Urn・既存・色ゲート〕
- U／**二面の帯**＝攻撃札をプレイするたびブロック1〔Duality・既存〕
- U／**目利きの片眼鏡**＝ショップ2割引〔Discerning Monocle・既存〕

**Rare（+15）**
- R／**石の暦**＝7ターン目の終了時、敵全体に52〔Stone Calendar・N3+N4〕
- R／**算盤**＝山札を切り直すたびブロック6〔The Abacus・N6〕
- R／**カロンの灰**（黒）＝札が消滅するたび敵全体に3〔Charon's Ashes・既存〕
- R／**木の骨組**＝ターン終了時にブロックを最大10持ち越す〔Sturdy Clamp（StS2）・N11 blockKeep〕
- R／**氷菓子**＝余ったエナジーを次のターンへ持ち越す〔Ice Cream・N11〕
- R／**タングステンの棒**＝HPを失うたび1軽減〔Tungsten Rod・N11〕
- R／**鳥居**＝5以下のHP損失は1になる〔Torii・N11〕
- R／**残り火の遺骸**＝1ターンに失うHPは20まで〔Beating Remnant（StS2）・N11〕
- R／**蜥蜴の尻尾**＝致死ダメージを1度だけ50%で耐える〔Lizard Tail・N11 deathSave〕
- R／**祈りの車輪**＝通常戦の勝利で報酬をもう1組〔Prayer Wheel・N12〕
- R／**歌う鉢**＝報酬を見送ると最大HP+2〔Singing Bowl・N12〕
- R／**気まぐれの外套**＝ターン終了時、手札1枚につきブロック1〔Cloak Clasp・N4+N18〕
- R／**筋力の錘（緑）**＝戦闘開始時HPが半分以下なら成長+3〔Red Skull・既存〕
- R／**盗人の金貨**＝取得時+150G〔Old Coin・既存〕
- R／**大きな果実**＝最大HP+14〔Mango・既存〕

**色固有（各4・Common/Uncommon/Rare に内数。色ゲート N0）**
- 青: **氷の護符**（C・開幕氷壁4）／**静電の管**（U・霊気を得るたび敵に2）／**逆流の栓**（U・打ち消すたび1ドロー）／**こだまの環**（R・毎T反復+1）
- 赤: **蓄熱の炉**（C・開幕 敵全体に延焼2）／**火口の指輪**（U・衝動をプレイするたび勢い+2）／**賭博師の護符**（U・乱数札をプレイするたび延焼1）／**ねじれた漏斗**（Shop・開幕 全体延焼4）
- 白: **従者の呼び鈴**（C・開幕 従者の少年1体）／**聖なる砂**（U・回復するたびブロック2）／**軍旗の欠片**（U・置物登場ごと全体1）／**鏡の盾**（R・ブロックを得るたび敵に1）
- 黒: **忍びの巻物**（C・開幕 骨のナイフ3枚）／**闇市の帳**（U・消滅コストを払うたび1ドロー）／**腐葉の種**（U・開幕 山札の上2枚を消滅）／**カロンの灰**（R・上）
- 緑（追加）: **硫黄**（Shop・毎T成長+2、敵全員筋力+1）／**読み勝ちの実**（U・からくりを動かすたび成長+2）

**Boss（+4・非エナジー）**
- **ルーンの角錐**＝手札を捨てない（火傷・衝動の失効は従来どおり）〔Runic Pyramid・N11 retainHand〕
- **星読みの盤**＝取得時、デッキから3枚を選んで同レア度の別の札に変え、鍛える〔Astrolabe・N13〕
- **空の鳥籠**＝取得時、デッキから2枚を取り除く〔Empty Cage・N13〕
- **呼び鈴**＝取得時、レリック3個と烙印1枚〔Calling Bell・N13+N14〕

**Shop（+5）**
- **溶けた卵／凍った卵／毒の卵**＝デッキに加える物理／呪文／置物の札が鍛えた状態になる〔Eggs・N12〕
- **薬品X**＝X札のXが+2〔Chemical X・N11〕
- **時計仕掛けの土産**＝戦闘開始時、状態異常を1回だけ弾く〔Clockwork Souvenir・N10（プレイヤー側アーティファクト）〕

**Event（+6）**
- **黒曜の護符**＝烙印を持つ札1枚につき最大HP+6（取得時）〔Darkstone Periapt・N14〕
- **お守り**＝次に受ける烙印2回を無効〔Omamori・N14〕
- **呪いの蝋燭**＝烙印の札をプレイできる（プレイ=HP-1して消滅）〔Blue Candle・N14〕
- **骨の茶**＝次の1戦だけ、初手が鍛えた状態〔Bone Tea（StS2）・時限〕
- **偽の錨**＝戦闘開始時ブロック4（本物の劣化）〔Anchor???（StS2）・既存〕
- **何もしない札入れ**＝何もしない〔Circlet／Wongo Badge・既存〕

### 3-4. 既存39個の手当て
- 死にキー削除（`fusionDiscount`・`revealOnSet`）、`'event'` 層を実際に使う（?イベントのレリック選択肢は event 層から引く）。
- 経済系の割合を下げるため、古い財布・戦利品袋・砥石の欠片は Event 層へ降格（本家 StS2 も Maw Bank/Dream Catcher を Event へ降格）。
- 猛禽の眼は「装甲持ちに乗らない」ので、急所→**威圧1**（赤面の面）に置換するか、対象を「装甲を持たない敵」に。

### 3-5. 実装順（案）
1. N0 色ゲート＋既存フックだけの約25個（色固有16＋Common/Uncommon の既存器分）＝engine 1行・データ中心。
2. N1〜N4（every/once/turn/onTurnEnd）＋N6/N7 → カウンター・時計・キル・シャッフルの約15個。
3. N12/N13（卵・取得時変成）＋N11（規則改変の C型キー）＋N10 → Shop/Boss/Rare の約15個。
4. Event 層＋烙印の資産化（N14）。
各波で `npm test`→`npm run goldens`（レリック追加はゴールデン不変。candidate 列のシャッフルは全レリック順なので**在庫が増えるとゴールデンが変わる**＝再生成して C# 追随）→Opus 1本で「取る理由」の答え合わせ。



---

# 付録A: StS1 レリック181個の分類（本家の文法）

# Slay the Spire 1 レリック181個の分類（本家の文法）

凡例: **C**=Common 36／**U**=Uncommon 37／**R**=Rare 34／**B**=Boss 30／**Sh**=Shop 20／**Sp**=Special 20／**St**=Starter 4（計181）。
データの description で数字が欠落している箇所（Happy Flower「Every turns」＝3ターン、Nunchaku「Every time you play Attacks」＝攻撃10枚、Sundial「Every times you shuffle」＝3回、Pen Nib「Every 10th」）は本家の実値で補った。

---

## 1. 全181個の分類（タイミング × ペイロード）

各レリックを (a) タイミング 1つ・(b) ペイロード 1つ に割り当てた（複合効果は主効果で判定。ボスレリックは「利益」側で分類し、税は §3 で扱う）。

### 1-a. タイミング／トリガー × ティア

| タイミング | C | U | R | B | Sh | Sp | St | 計 |
|---|---|---|---|---|---|---|---|---|
| 戦闘開始時（初回限定・エリート/ボス戦限定含む） | 12 | 7 | 3 | 2 | 5 | 4 | 3 | **36** |
| 毎ターン（開始/終了・条件付き含む） | 3 | 1 | 3 | 14 | 1 | 2 | · | **24** |
| N回ごと（カウンター・ターン数指定） | 3 | 7 | 3 | 1 | · | · | · | **14** |
| 特定タイプの札をプレイ時 | · | 2 | 1 | · | 1 | 1 | · | **5** |
| 出来事（撃破・HP損失・捨て・消滅・シャッフル・デバフ付与・金・ブロック破壊） | 2 | 2 | 10 | 3 | 3 | 1 | · | **21** |
| 戦闘終了時 | · | 1 | · | 1 | · | 1 | 1 | **4** |
| ラン恒常（経済・報酬・焚き火・ショップ・マップ・宝箱・?） | 9 | 11 | 5 | 1 | 2 | 5 | · | **33** |
| 取得時一回（即時） | 4 | 1 | 2 | 5 | 4 | · | · | **16** |
| ルール改変・常在補正（手札保持・コスト・意図・免疫・倍率） | 2 | 5 | 7 | 2 | 4 | 2 | · | **22** |
| ポーション系（使用時/効果倍率） | 1 | · | · | 1 | · | · | · | **2** |
| 無効果（飾り） | · | · | · | · | · | 4 | · | **4** |
| **計** | 36 | 37 | 34 | 30 | 20 | 20 | 4 | **181** |

**メンバー一覧（全181）**

- **戦闘開始時（36）**: Akabeko(C) Anchor(C) Ancient Tea Set(C・焚き火後の次戦) Bag of Marbles(C) Bag of Preparation(C) Blood Vial(C) Bronze Scales(C) Data Disk(C) Lantern(C) Oddly Smooth Stone(C) Preserved Insect(C・エリート戦) Vajra(C) ／ Bottled Flame(U) Bottled Lightning(U) Bottled Tornado(U) Ninja Scroll(U) Pantograph(U・ボス戦) Symbiotic Virus(U) Teardrop Locket(U) ／ Du-Vu Doll(R) Gambling Chip(R) Thread and Needle(R) ／ Holy Water(B) Nuclear Battery(B) ／ Clockwork Souvenir(Sh) Runic Capacitor(Sh) Sling of Courage(Sh・エリート戦) Toolbox(Sh) Twisted Funnel(Sh) ／ Enchiridion(Sp) Gremlin Visage(Sp) Mutagenic Strength(Sp) Red Mask(Sp) ／ Cracked Core(St) Pure Water(St) Ring of the Snake(St)
- **毎ターン（24）**: Art of War(C・条件) Damaru(C) Orichalcum(C・条件) ／ Mercury Hourglass(U) ／ Cloak Clasp(R) Emotion Chip(R・条件) Pocketwatch(R・条件) ／ Busted Crown(B) Coffee Dripper(B) Cursed Key(B) Ectoplasm(B) Frozen Core(B・条件) Fusion Hammer(B) Mark of Pain(B) Philosopher's Stone(B) Ring of the Serpent(B) Runic Dome(B) Slaver's Collar(B・ボス/エリート戦のみ) Snecko Eye(B) Sozu(B) Velvet Choker(B) ／ Brimstone(Sh) ／ Nilry's Codex(Sp) Warped Tongs(Sp)
- **N回ごと（14）**: Happy Flower(C) Nunchaku(C) Pen Nib(C) ／ Horn Cleat(U) Ink Bottle(U) Kunai(U) Letter Opener(U) Ornamental Fan(U) Shuriken(U) Sundial(U) ／ Captain's Wheel(R) Incense Burner(R) Stone Calendar(R) ／ Inserter(B)
- **特定タイプの札をプレイ時（5）**: Duality(U・攻撃) Mummified Hand(U・パワー) ／ Bird-Faced Urn(R・パワー) ／ Orange Pellets(Sh・P+A+S同一ターン) ／ Necronomicon(Sp・コスト2以上の攻撃)
- **出来事（21）**: Centennial Puzzle(C・初回HP損失) Snecko Skull(C・毒付与) ／ Gremlin Horn(U・撃破) Self-Forming Clay(U・HP損失) ／ Champion Belt(R・脆弱付与) Charon's Ashes(R・消滅) Dead Branch(R・消滅) Fossilized Helix(R・初回HP損失) Golden Eye(R・占術) Lizard Tail(R・死亡) The Specimen(R・撃破) Tingsha(R・捨て) Tough Bandages(R・捨て) Unceasing Top(R・手札0) ／ Hovering Kite(B・ターン初回の捨て) Runic Cube(B・HP損失) Violet Lotus(B・平静解除) ／ Hand Drill(Sh・ブロック破壊) Melange(Sh・シャッフル) The Abacus(Sh・シャッフル) ／ Bloody Idol(Sp・金獲得)
- **戦闘終了時（4）**: Meat on the Bone(U・HP50%以下) ／ Black Blood(B) ／ Face Of Cleric(Sp) ／ Burning Blood(St)
- **ラン恒常（33）**: Ceramic Fish(C) Dream Catcher(C) Juzu Bracelet(C) Maw Bank(C) Meal Ticket(C) Omamori(C) Regal Pillow(C) Smiling Mask(C) Tiny Chest(C) ／ Darkstone Periapt(U) Discerning Monocle(U) Eternal Feather(U) Frozen Egg(U) Matryoshka(U) Molten Egg(U) Question Card(U) Singing Bowl(U) The Courier(U) Toxic Egg(U) White Beast Statue(U) ／ Girya(R) Peace Pipe(R) Prayer Wheel(R) Shovel(R) Wing Boots(R) ／ Black Star(B) ／ Membership Card(Sh) Prismatic Shard(Sh) ／ Golden Idol(Sp) N'loth's Gift(Sp) N'loth's Hungry Face(Sp) Neow's Lament(Sp) Ssserpent Head(Sp)
- **取得時一回（16）**: Potion Belt(C) Strawberry(C) War Paint(C) Whetstone(C) ／ Pear(U) ／ Mango(R) Old Coin(R) ／ Astrolabe(B) Calling Bell(B) Empty Cage(B) Pandora's Box(B) Tiny House(B) ／ Cauldron(Sh) Dolly's Mirror(Sh) Lee's Waffle(Sh) Orrery(Sh)
- **ルール改変・常在補正（22）**: Red Skull(C) The Boot(C) ／ Blue Candle(U) Gold-Plated Cables(U) Paper Krane(U) Paper Phrog(U) Strike Dummy(U) ／ Calipers(R) Ginger(R) Ice Cream(R) Magic Flower(R) Torii(R) Tungsten Rod(R) Turnip(R) ／ Runic Pyramid(B) Wrist Blade(B) ／ Chemical X(Sh) Frozen Eye(Sh) Medical Kit(Sh) Strange Spoon(Sh) ／ Mark of the Bloom(Sp) Odd Mushroom(Sp)
- **ポーション系（2）**: Toy Ornithopter(C) ／ Sacred Bark(B)
- **無効果（4）**: Circlet(Sp) Cultist Headpiece(Sp) Red Circlet(Sp) Spirit Poop(Sp)

### 1-b. ペイロード × ティア

| ペイロード | C | U | R | B | Sh | Sp | St | 計 |
|---|---|---|---|---|---|---|---|---|
| 火力（特定札補正・とげ・敵HP減含む） | 5 | 3 | 3 | 1 | 1 | 2 | · | **15** |
| ブロック・防御（無形・被ダメ軽減） | 2 | 3 | 9 | · | 1 | · | · | **15** |
| エナジー | 5 | 3 | 1 | 13 | · | · | · | **22** |
| ドロー・手札操作 | 2 | 1 | 5 | 4 | 1 | · | 1 | **14** |
| 筋力・敏捷・フォーカス・オーブ・マントラ・スタンス | 5 | 6 | 3 | 3 | 3 | 1 | 1 | **22** |
| 状態異常（付与・強化・免疫・除去） | 2 | 2 | 4 | · | 4 | 3 | · | **15** |
| 回復・最大HP・生存 | 5 | 6 | 4 | 1 | 1 | 3 | 1 | **21** |
| 経済（金・ショップ） | 3 | 2 | 1 | · | 1 | 2 | · | **9** |
| カード品質（鍛え・変化・除去・呪い対策） | 3 | 3 | 1 | 3 | · | 1 | · | **11** |
| デッキ/手札構成（ボトル・トークン札・複製・追加・状態札処理） | · | 5 | · | 1 | 5 | 2 | 1 | **14** |
| 報酬・マップ・情報 | 3 | 2 | 3 | 3 | 2 | 2 | · | **15** |
| ポーション | 1 | 1 | · | 1 | 1 | · | · | **4** |
| 無し | · | · | · | · | · | 4 | · | **4** |
| **計** | 36 | 37 | 34 | 30 | 20 | 20 | 4 | **181** |

**メンバー一覧**

- **火力（15）**: Akabeko(C・初撃+8) Bronze Scales(C・とげ3) Pen Nib(C・10枚目2倍) Preserved Insect(C・エリートHP−25%) The Boot(C・4以下→5) ／ Letter Opener(U・全体5) Mercury Hourglass(U・毎T全体3) Strike Dummy(U・Strike+3) ／ Charon's Ashes(R・消滅ごと全体3) Stone Calendar(R・T7に全体52) Tingsha(R・捨てごと3) ／ Wrist Blade(B・0コスト攻撃+4) ／ Chemical X(Sh・X札+2) ／ Necronomicon(Sp・2回プレイ) Neow's Lament(Sp・3戦HP1)
- **ブロック・防御（15）**: Anchor(C・開幕10) Orichalcum(C・ブロック0なら6) ／ Horn Cleat(U・T2に14) Ornamental Fan(U・攻撃3枚で4) Self-Forming Clay(U・HP損失で次T3) ／ Calipers(R・15持越) Captain's Wheel(R・T3に18) Cloak Clasp(R・手札×1) Fossilized Helix(R・初回HP損失を無効) Incense Burner(R・6Tごと無形) Thread and Needle(R・鍍金4) Torii(R・5以下→1) Tough Bandages(R・捨てごと3) Tungsten Rod(R・HP損失−1) ／ The Abacus(Sh・シャッフルごと6)
- **エナジー（22）**: Ancient Tea Set(C・焚き火後+2) Art of War(C・攻撃なしで次T+1) Happy Flower(C・3Tごと+1) Lantern(C・開幕+1) Nunchaku(C・攻撃10枚で+1) ／ Gremlin Horn(U・撃破で+1&1ドロー) Mummified Hand(U・パワーで1枚0コスト) Sundial(U・シャッフル3回で+2) ／ Ice Cream(R・持越) ／ Busted Crown(B) Coffee Dripper(B) Cursed Key(B) Ectoplasm(B) Fusion Hammer(B) Hovering Kite(B・ターン初回捨て) Mark of Pain(B) Philosopher's Stone(B) Runic Dome(B) Slaver's Collar(B) Sozu(B) Velvet Choker(B) Violet Lotus(B・平静解除)
- **ドロー・手札操作（14）**: Bag of Preparation(C・開幕+2) Centennial Puzzle(C・初回HP損失で3) ／ Ink Bottle(U・10枚で1) ／ Dead Branch(R・消滅でランダム札) Gambling Chip(R・開幕捨て引き) Golden Eye(R・占術+2) Pocketwatch(R・3枚以下なら次T+3) Unceasing Top(R・手札0で1) ／ Ring of the Serpent(B・毎T+1) Runic Cube(B・HP損失で1) Runic Pyramid(B・手札保持) Snecko Eye(B・毎T+2) ／ Melange(Sh・シャッフルで占術3) ／ Ring of the Snake(St・開幕+2)
- **筋力・敏捷・フォーカス・オーブ・マントラ・スタンス（22）**: Damaru(C・毎Tマントラ1) Data Disk(C・フォーカス1) Oddly Smooth Stone(C・敏捷1) Red Skull(C・HP50%以下で筋力3) Vajra(C・筋力1) ／ Duality(U・攻撃で一時敏捷1) Gold-Plated Cables(U・右端オーブ2回) Kunai(U・攻撃3枚で敏捷1) Shuriken(U・攻撃3枚で筋力1) Symbiotic Virus(U・開幕Dark) Teardrop Locket(U・開幕平静) ／ Du-Vu Doll(R・呪い×筋力1) Emotion Chip(R・前T被弾でオーブ全発動) Girya(R・焚き火で筋力最大3) ／ Frozen Core(B・空枠でFrost) Inserter(B・2Tごとオーブ枠) Nuclear Battery(B・開幕Plasma) ／ Brimstone(Sh・毎T筋力2/敵+1) Runic Capacitor(Sh・オーブ枠+3) Sling of Courage(Sh・エリート戦筋力2) ／ Mutagenic Strength(Sp・T1だけ筋力3) ／ Cracked Core(St・開幕Lightning)
- **状態異常（15）**: Bag of Marbles(C・全体脆弱1) Snecko Skull(C・毒+1) ／ Paper Krane(U・弱体25→40%) Paper Phrog(U・脆弱50→75%) ／ Champion Belt(R・脆弱に弱体1) Ginger(R・弱体免疫) The Specimen(R・毒転移) Turnip(R・虚弱免疫) ／ Clockwork Souvenir(Sh・アーティファクト1) Hand Drill(Sh・ブロック破壊で脆弱2) Orange Pellets(Sh・デバフ全解除) Twisted Funnel(Sh・全体毒4) ／ Gremlin Visage(Sp・自分に弱体1) Odd Mushroom(Sp・脆弱50→25%) Red Mask(Sp・全体弱体1)
- **回復・最大HP・生存（21）**: Blood Vial(C・開幕2) Meal Ticket(C・ショップで15) Regal Pillow(C・休息+15) Strawberry(C・最大+7) Toy Ornithopter(C・ポーションで5) ／ Darkstone Periapt(U・呪いで最大+6) Eternal Feather(U・5枚ごと3) Meat on the Bone(U・50%以下で12) Pantograph(U・ボス戦25) Pear(U・最大+10) Singing Bowl(U・札の代わりに最大+2) ／ Bird-Faced Urn(R・パワーで2) Lizard Tail(R・蘇生50%×1) Magic Flower(R・回復+50%) Mango(R・最大+14) ／ Black Blood(B・終了12) ／ Lee's Waffle(Sh・最大+7&全快) ／ Bloody Idol(Sp・金で5) Face Of Cleric(Sp・終了で最大+1) Mark of the Bloom(Sp・回復禁止) ／ Burning Blood(St・終了6)
- **経済（9）**: Ceramic Fish(C・札追加で9G) Maw Bank(C・階ごと12G) Smiling Mask(C・除去50G固定) ／ Discerning Monocle(U・−20%) The Courier(U・再入荷&−20%) ／ Old Coin(R・300G) ／ Membership Card(Sh・−50%) ／ Golden Idol(Sp・+25%) Ssserpent Head(Sp・?で50G)
- **カード品質（11）**: Omamori(C・呪い2枚無効) War Paint(C・スキル2枚鍛え) Whetstone(C・攻撃2枚鍛え) ／ Frozen Egg(U) Molten Egg(U) Toxic Egg(U)（追加時に鍛え） ／ Peace Pipe(R・焚き火で除去) ／ Astrolabe(B・3枚変化+鍛え) Empty Cage(B・2枚除去) Pandora's Box(B・基本札全変化) ／ Warped Tongs(Sp・毎T手札1枚を戦闘内鍛え)
- **デッキ/手札構成（14）**: Blue Candle(U・呪いをプレイ可) Bottled Flame(U) Bottled Lightning(U) Bottled Tornado(U)（イネイト化） Ninja Scroll(U・開幕シヴ3) ／ Holy Water(B・開幕奇跡3) ／ Dolly's Mirror(Sh・複製) Medical Kit(Sh・状態札をプレイ可) Orrery(Sh・5枚追加) Strange Spoon(Sh・消滅50%回避) Toolbox(Sh・開幕無色1枚) ／ Enchiridion(Sp・開幕ランダムパワー0コスト) Nilry's Codex(Sp・毎T3択を山札へ) ／ Pure Water(St・開幕奇跡1)
- **報酬・マップ・情報（15）**: Dream Catcher(C・休息で札) Juzu Bracelet(C・?で戦闘なし) Tiny Chest(C・?4回目が宝箱) ／ Matryoshka(U・宝箱2個×2) Question Card(U・提示+1) ／ Prayer Wheel(R・通常戦の札報酬+1) Shovel(R・焚き火で発掘) Wing Boots(R・経路無視×3) ／ Black Star(B・エリートでレリック+1) Calling Bell(B・レリック3+呪い) Tiny House(B・詰め合わせ) ／ Frozen Eye(Sh・山札順を公開) Prismatic Shard(Sh・他色札が報酬に) ／ N'loth's Gift(Sp・レア3倍) N'loth's Hungry Face(Sp・次の宝箱が空)
- **ポーション（4）**: Potion Belt(C・枠+2) ／ White Beast Statue(U・報酬に必ず) ／ Sacred Bark(B・効果2倍) ／ Cauldron(Sh・5個)
- **無し（4）**: Circlet(Sp) Cultist Headpiece(Sp) Red Circlet(Sp) Spirit Poop(Sp)

**表から読める偏り**
- コモン36のうち **戦闘開始時12＋取得時4＋ラン恒常9＝25（69%）** が「戦闘中の判断を要さない」型。毎ターン型はわずか3、しかも全部条件付き（Art of War・Orichalcum）か微量（Damaru）。
- カウンター14のうち **アンコモン7（50%）**。リズム設計はアンコモンの持ち場。
- レア34のうち **出来事10＋ルール改変7＝17（50%）** で、ブロック系15のうち **9がレア（60%）**。「守り」は本家では希少資源。
- ボス30のうち **毎ターン14（47%）**、エナジー22のうち **13がボス（59%）**。「毎ターン無条件」はボスの専売に近い（例外はMercury Hourglass 3ダメ・Brimstone・Cloak Clasp）。
- 経済9はコモン3・アンコモン2・イベント2で、レアはOld Coin1つ。金は「小さく分散」が本家。
- デッキ/手札構成14はアンコモン5＋ショップ5。「札で戦う手段」はドラフト中盤と店で買う。

---

## 2. カウンター型（リズム設計）

### 2-a. 周期型（戦闘内で繰り返し鳴る）

| レリック | ティア | N | 計る対象 | 支払い |
|---|---|---|---|---|
| Happy Flower | C | 3 | ターン | エナジー+1（≈0.33/T） |
| Nunchaku | C | 10 | 攻撃の累積枚数 | エナジー+1 |
| Pen Nib | C | 10 | 攻撃の累積枚数 | 10枚目が2倍ダメ |
| Ink Bottle | U | 10 | カードの累積枚数 | ドロー1 |
| Kunai | U | 3 | 1ターン内の攻撃 | 敏捷+1（永続） |
| Shuriken | U | 3 | 1ターン内の攻撃 | 筋力+1（永続） |
| Ornamental Fan | U | 3 | 1ターン内の攻撃 | ブロック4 |
| Letter Opener | U | 3 | 1ターン内のスキル | 全体5ダメ |
| Sundial | U | 3 | シャッフル | エナジー+2 |
| Incense Burner | R | 6 | ターン | 無形1 |
| Inserter | B | 2 | ターン | オーブ枠+1 |

### 2-b. ターン数指定（1戦闘に1回だけ鳴る時計）

| レリック | ティア | ターン | 支払い |
|---|---|---|---|
| Mutagenic Strength | Sp | T1終了 | 筋力3を**失う**（逆向きの時計） |
| Horn Cleat | U | T2開始 | ブロック14 |
| Captain's Wheel | R | T3開始 | ブロック18 |
| Stone Calendar | R | T7終了 | 全体52ダメ |

### 2-c. チャージ型（ランで N 回だけ）

| レリック | ティア | 残数 | 内容 |
|---|---|---|---|
| Omamori | C | 2 | 呪いを無効 |
| Tiny Chest | C | 4周期 | ?部屋4回目が宝箱 |
| Matryoshka | U | 2 | 宝箱がレリック2個 |
| Wing Boots | R | 3 | マップの経路を無視 |
| Girya | R | 3 | 焚き火で筋力+1 |
| Lizard Tail | R | 1 | 死亡時50%で復活 |
| Neow's Lament | Sp | 3戦 | 敵HP1 |
| N'loth's Hungry Face | Sp | 1 | 次の宝箱が空 |
| Maw Bank | C | フラグ | 店で使うと停止（counter −2） |
| Ancient Tea Set | C | フラグ | 焚き火で装填（counter −1） |

### 2-d. 「初回だけ」型（回数ではなく 1戦/1ターンに1回のブレーキ）
Akabeko(C・戦闘の初撃) ／ Centennial Puzzle(C・戦闘の初回HP損失) ／ Fossilized Helix(R・戦闘の初回HP損失) ／ Hovering Kite(B・ターンの初回捨て) ／ Necronomicon(Sp・ターンの初回2コスト以上攻撃)。

**リズムの数字は 3 と 10 に集約**: 1ターン内しきい値＝3（4個）、周期＝3（ターン・シャッフル）、累積＝10（攻撃・カード）。長周期6はレア（無形）、ボスだけ2（オーブ枠）。ターン指定の一発は 2→14 / 3→18（遅らせ1ターンで+4ブロック）、7→52。

---

## 3. ボスレリック 30

### 3-a. エナジー+1 系（13）

| レリック | 利益 | 税 | 税の型 |
|---|---|---|---|
| Coffee Dripper | 毎T+1 | 焚き火で休めない | 資源の禁止 |
| Fusion Hammer | 毎T+1 | 焚き火で鍛えられない | 資源の禁止 |
| Ectoplasm | 毎T+1 | 金を得られない | 資源の禁止 |
| Sozu | 毎T+1 | ポーションを得られない | 資源の禁止 |
| Busted Crown | 毎T+1 | カード報酬の提示−2 | 報酬の減少 |
| Cursed Key | 毎T+1 | 非ボス宝箱を開けるたび呪い | 呪い・汚染札 |
| Mark of Pain | 毎T+1 | 開幕に負傷2を山札へ | 呪い・汚染札 |
| Philosopher's Stone | 毎T+1 | 全敵が筋力+1で開始 | 敵強化 |
| Runic Dome | 毎T+1 | 敵の意図が見えない | ルール制約 |
| Velvet Choker | 毎T+1 | 1ターン6枚まで | ルール制約 |
| Slaver's Collar | ボス/エリート戦のみ毎T+1 | 通常戦では無効 | ルール制約（条件限定） |
| Hovering Kite | ターン初回の捨てで+1 | 捨て札の型を要求 | 条件付き（税なし） |
| Violet Lotus（W） | 平静解除ごと+1 | スタンス型を要求 | 条件付き（税なし） |

### 3-b. 非エナジー系（17）

| レリック | 利益 | 税 | 型 |
|---|---|---|---|
| Calling Bell | レリック3個 | 固有の呪い1枚 | 呪い |
| Snecko Eye | 毎T+2ドロー | 開幕から混乱（コストランダム） | ルール制約 |
| Runic Pyramid | 手札を捨てない | なし（手札事故は自己責任） | ルール改変・代償なし |
| Runic Cube | HP損失ごと1ドロー | なし（HPを払う型） | 条件付き・代償なし |
| Wrist Blade | 0コスト攻撃+4 | なし | ビルド指定・代償なし |
| Inserter | 2Tごとオーブ枠+1 | なし | ビルド指定・代償なし |
| Nuclear Battery | 開幕Plasma | なし | ビルド指定・代償なし |
| Sacred Bark | ポーション2倍 | なし | 代償なし |
| Astrolabe | 3枚変化+鍛え | なし（ランダム性が代価） | 一発変換・代償なし |
| Pandora's Box | Strike/Defend全変化 | なし（ランダム性が代価） | 一発変換・代償なし |
| Empty Cage | 2枚除去 | なし | 一発変換・代償なし |
| Tiny House | ポーション1・50G・最大HP5・札1・鍛え1 | なし（各項が小さい） | 一発詰め合わせ・代償なし |
| Black Star | エリートでレリック+1 | なし | 報酬増・代償なし |
| Black Blood（IC） | 戦闘終了で12回復（6→12） | なし | スターター強化 |
| Ring of the Serpent（SI） | 毎T+1ドロー（開幕2→毎T1） | なし | スターター強化 |
| Frozen Core（DE） | 空枠でFrost | なし | スターター強化 |
| Holy Water（W） | 開幕奇跡3（1→3） | なし | スターター強化 |

**税の型ごとの数**: 資源の禁止4／報酬の減少1／呪い・汚染札3（Cursed Key・Mark of Pain・Calling Bell）／敵強化1／ルール制約4（Runic Dome・Velvet Choker・Snecko Eye・Slaver's Collar）／条件付き・代償なし17。
**文法**: エナジー+1（11個）は必ず税と対。税は「ラン資源のどれか1本を切る」が基本（休息・鍛え・金・ポーション・報酬幅・意図）。代償なしのものは (a) ランダム性で払う（Astrolabe・Pandora）(b) 小さく分散（Tiny House）(c) ビルドを指定する（Wrist Blade・Inserter・Nuclear Battery・Hovering Kite）(d) スターター強化のいずれか。

---

## 4. ショップレリック 20

| レリック | 効果 | 役割 |
|---|---|---|
| Membership Card | 全品−50% | 経済 |
| Lee's Waffle | 最大HP+7＆全快 | 保険（HP） |
| Cauldron | ポーション5個 | 保険（消耗品） |
| Clockwork Souvenir | 開幕アーティファクト1 | 保険（デバフ1回） |
| Orange Pellets | P+A+S同ターンでデバフ全解除 | 保険（条件付き） |
| Dolly's Mirror | 1枚複製 | カード操作（デッキ構成） |
| Orrery | 5枚選んで追加 | カード操作（デッキ構成） |
| Prismatic Shard | 報酬に他色・無色 | カード操作（報酬プール改変） |
| Toolbox | 開幕に無色3択 | カード操作（手札構成） |
| Frozen Eye | 山札順を公開 | 情報 |
| Melange（W） | シャッフルで占術3 | 情報／薄いデッキ |
| Strange Spoon | 消滅を50%で捨てに | ルール改変（消滅ビルドの逆） |
| Medical Kit | 状態札をプレイして消滅 | ルール改変（状態札対策） |
| Brimstone（IC） | 毎T筋力+2／敵+1 | ビルド（筋力） |
| Chemical X | X札+2 | ビルド（X札） |
| Hand Drill | ブロック破壊で脆弱2 | ビルド（対ブロック敵） |
| Runic Capacitor（DE） | オーブ枠+3 | ビルド（オーブ） |
| Twisted Funnel（SI） | 開幕全体毒4 | ビルド（毒） |
| The Abacus | シャッフルで6ブロック | ビルド（薄いデッキ） |
| Sling of Courage | エリート戦で筋力2 | ビルド（エリート狩り） |

内訳: 経済1／保険4／カード操作4／情報2／ルール改変2／ビルド7。**エナジー+1は売らない**。店は「今のデッキに足りない部品を金で埋める」場所であり、取得時一回が4（Lee's Waffle・Cauldron・Dolly's Mirror・Orrery）、戦闘開始時が5と、即効性の高いものが半分近い。

---

## 5. イベント産（Special）20 と対になる代償

| レリック | 効果 | 出所（イベント） | 対の代償・構造 |
|---|---|---|---|
| Neow's Lament | 最初の3戦の敵HP1 | Neow（ラン開始） | 他の開始ボーナスを捨てる |
| Golden Idol | 敵の金+25% | Golden Idol（幕1） | 取ると罠（呪いDecay／ダメージ／最大HP減の3択） |
| Bloody Idol | 金を得るたび5回復 | Forgotten Altar（幕2） | Golden Idol を捧げる（レリック→レリックの変換） |
| Odd Mushroom | 脆弱50→25% | Hypnotizing Mushrooms（幕1） | 戦闘に勝つ |
| Red Mask | 開幕全体弱体1 | Masked Bandits（幕2） | 戦闘に勝つ（金も戻る） |
| Mutagenic Strength | 開幕筋力3・T1終了で失う | Augmenter（幕2） | 「変異」の実験台になる |
| Necronomicon | 2コスト以上の攻撃を2回 | Cursed Tome（幕2） | 固有呪い Necronomicurse を得る（除去不能） |
| Enchiridion | 開幕ランダムパワー0コスト | Cursed Tome | 本を読む間にHPを失う |
| Nilry's Codex | 毎T3択を山札へ | Cursed Tome | 同上 |
| Warped Tongs | 毎T手札1枚を戦闘内鍛え | Ominous Forge（幕2） | 呪い Pain を得る |
| N'loth's Gift | レア3倍 | N'loth（幕2） | 手持ちレリック1個を渡す |
| Mark of the Bloom | 回復不能 | Mind Bloom（幕3） | 全札鍛えの代価（純粋な負） |
| Spirit Poop | 無効果 | Bonfire Spirits（幕3） | 呪いを捧げた時の「空手形」 |
| Face Of Cleric | 戦闘終了で最大HP+1 | Face Trader（顔の交換） | 5面ガチャの当たり |
| Ssserpent Head | ?部屋で50G | Face Trader | 5面ガチャの当たり |
| Cultist Headpiece | 無効果 | Face Trader | 5面ガチャの空振り |
| Gremlin Visage | 開幕自分に弱体1 | Face Trader | 5面ガチャの外れ（負） |
| N'loth's Hungry Face | 次の宝箱が空 | Face Trader | 5面ガチャの外れ（負） |
| Circlet | 無効果 | プール枯渇時の埋め草 | — |
| Red Circlet | 無効果 | プール枯渇時の埋め草 | — |

**パターン**: (1) 呪いと抱き合わせ（Necronomicon・Warped Tongs・Golden Idol・Calling Bell〔ボス〕） (2) レリック→レリックの変換（Golden Idol→Bloody Idol・N'loth's Gift） (3) 戦闘の勝利報酬（Odd Mushroom・Red Mask） (4) 良2・無1・負2のガチャ（Face Trader） (5) 純粋な負（Mark of the Bloom）や無効果4個＝「取っても嬉しくない枠」が意図的に存在し、代償やハズレの受け皿になっている。

---

## 6. クラスレリック

### 6-a. データの `color != null`（11個）

| クラス | 数 | レリック |
|---|---|---|
| Watcher | 4 | Damaru(C) Captain's Wheel(R) Holy Water(B) Violet Lotus(B) |
| Defect | 3 | Data Disk(C) Cracked Core(St) Frozen Core(B) |
| Ironclad | 2 | Burning Blood(St) Black Blood(B) |
| Silent | 2 | Ring of the Snake(St) Ring of the Serpent(B) |

### 6-b. 補足：本家の実装上のクラス限定レリック（データ外・41個）
データの color は不完全（Pure Water も null）。本家のプールでは以下がクラス限定で、上の11個はその一部。

| クラス | 数 | 内訳（St→B の梯子＋固有資源へのフック） |
|---|---|---|
| Ironclad | 11 | Burning Blood(St)→Black Blood(B)／Red Skull(C・HP50%で筋力3)／Paper Phrog(U・脆弱75%) Self-Forming Clay(U・HP損失→ブロック)／Champion Belt(R・脆弱→弱体) Charon's Ashes(R・消滅→全体3) Magic Flower(R・回復+50%)／Brimstone(Sh・毎T筋力2)／Mark of Pain(B・負傷) Runic Cube(B・HP損失→ドロー) |
| Silent | 11 | Ring of the Snake(St)→Ring of the Serpent(B)／Snecko Skull(C・毒+1)／Ninja Scroll(U・シヴ3) Paper Krane(U・弱体40%)／The Specimen(R・毒転移) Tingsha(R・捨て→3ダメ) Tough Bandages(R・捨て→3ブロック)／Twisted Funnel(Sh・全体毒4)／Wrist Blade(B・0コスト+4) Hovering Kite(B・捨て→エナジー) |
| Defect | 9 | Cracked Core(St)→Frozen Core(B)／Data Disk(C・フォーカス1)／Gold-Plated Cables(U) Symbiotic Virus(U)／Emotion Chip(R)／Runic Capacitor(Sh・枠+3)／Nuclear Battery(B) Inserter(B) |
| Watcher | 10 | Pure Water(St)→Holy Water(B)／Damaru(C・マントラ)／Duality(U) Teardrop Locket(U・平静開始)／Cloak Clasp(R・手札参照) Golden Eye(R・占術) Captain's Wheel(R)／Melange(Sh・占術)／Violet Lotus(B・平静解除→エナジー) |

**パターン**: ①各クラス 9〜11 個で、全ティアに1〜3個ずつ散らす（C1・U2・R2〜3・Sh1・B2〜3）②スターター→ボスは同系統の2倍化（回復6→12／開幕2ドロー→毎T1／奇跡1→3／Lightning→Frost常在）③クラスの固有資源（筋力・HP損失・消滅／毒・シヴ・捨て／オーブ・フォーカス・枠／スタンス・マントラ・占術）を「戦闘開始時から動かす」か「その資源の出来事に接着剤を付ける」の2形。汎用資源（エナジー・ブロック・回復）はクラス固有にしない。

---

## 7. 本家の文法（設計原則）

1. **コモン＝無条件・小さい・戦闘の外か開幕で一発**。36個中25個（69%）が戦闘中の判断を要さない。数字は「ブロック10・エナジー1・筋力1・敏捷1・ドロー2・回復2・最大HP7・初撃+8・とげ3」。毎ターン型は条件付き3個だけ（Art of War・Orichalcum・Damaru）。
2. **アンコモン＝条件とリズム、ビルドの入口**。カウンター14個の半分（7）がここ。「1ターンに3枚で筋力1／敏捷1／ブロック4／全体5」の4兄弟が同ティア同帯。イネイト化（ボトル3）と追加時鍛え（卵3）でドラフト戦略に接続。ラン恒常11個（ショップ割引20%・報酬+1・宝箱2個）。
3. **レア＝出来事フックとルール改変（17/34）＋ビルドアラウンド**。消滅（Dead Branch・Charon's Ashes）・捨て（Tingsha・Tough Bandages）・手札0（Unceasing Top）・少数プレイ（Pocketwatch）の各アーキタイプに「1枚で軸を立てる」札。免疫（Ginger・Turnip）・被ダメ床（Torii・Tungsten Rod）・持越（Calipers・Ice Cream）もレア。数字は大きく一発（Stone Calendar 52・Old Coin 300・Mango 14・Lizard Tail 50%）。焚き火の新選択肢（Girya・Peace Pipe・Shovel）もレア＝ラン構造そのものを触る権利。
4. **ボス＝「エナジー+1×税」が主形（11/30）**。税は資源1本の禁止（休息・鍛え・金・ポーション）／報酬幅／呪い／敵筋力／情報・手数の制約、の6型。残り19個は一発の大変換（Astrolabe・Pandora・Empty Cage・Tiny House・Calling Bell）かルール改変（Runic Pyramid・Snecko Eye）かビルド指定（Wrist Blade・Inserter・Hovering Kite）で、代償はランダム性かビルド縛りで払う。
5. **ショップ＝金で買うのはビルド部品・保険・デッキ操作・情報**（7/4/4/2）。エナジー+1は売らない。店の品は即効性が高い（取得時一回4＋戦闘開始時5）。
6. **イベント産＝呪いや代償と対。ハズレも刷る**。呪い抱き合わせ4・レリック変換2・戦闘勝利2・5面ガチャ（良2/無1/負2）・純粋な負1・無効果4。「取らない自由」が選択になるよう、負のレリックを堂々と置く。
7. **カウンターの数字は 3 と 10**。1ターン内しきい値3・周期3（ターン・シャッフル）・累積10（攻撃・カード）。長周期は6（レア）、ボスだけ2。ターン指定の一発は 2→14／3→18 で「1ターン遅らせるごとにブロック+4」、7→52。
8. **「初回だけ」でブレーキをかける**。1戦1回（Akabeko・Centennial Puzzle・Fossilized Helix）／1ターン1回（Hovering Kite・Necronomicon）／ラン1回（Lizard Tail）。強い効果は回数で、弱い効果は条件で抑える。
9. **値の帯の換算**。開幕一発 ≈ 毎ターン×2〜3（Anchor 10 vs Orichalcum 6条件付き）／エナジーは C で 1回か 1/3T、B で毎T／ドローは C 開幕2 → B 毎T1／最大HPはティアごと +3〜4（7→10→14）／戦闘終了回復はスターター6→ボス12の2倍／回復の常設加算は Magic Flower +50%（R）まで。毎ターン無条件の火力は Mercury Hourglass 3（U）が上限で、それ以上（Stone Calendar 52）は「7ターン目」の時計に縛る。
10. **ランのノードごとにフックを配る**。焚き火（Ancient Tea Set・Dream Catcher・Regal Pillow・Eternal Feather・Girya・Peace Pipe・Shovel＋税のCoffee Dripper/Fusion Hammer）／ショップ（Meal Ticket・Smiling Mask・Monocle・Courier・Membership Card・Maw Bankの停止）／?（Juzu・Tiny Chest・Ssserpent Head）／宝箱（Matryoshka・Cursed Key・N'loth's Hungry Face・Tiny Chest）／エリート（Preserved Insect・Sling of Courage・Black Star・Slaver's Collar）／ボス戦（Pantograph）／マップ（Wing Boots）／報酬（Question Card・Prayer Wheel・Prismatic Shard・Singing Bowl・Ceramic Fish・White Beast Statue・N'loth's Gift）。レリックは「そのノードに立ち寄る理由」を作る装置でもある。
11. **デッキ枚数は両方向にレバーを置く**。薄いデッキに報いる（The Abacus・Sundial・Melange・Unceasing Top・Pocketwatch・Frozen Eye）／厚いデッキに報いる（Eternal Feather 5枚ごと3回復・Ceramic Fish 9G・Dream Catcher）／取らないことに報いる（Singing Bowl 最大HP+2）／固定札を毎戦見せる（ボトル3・Ninja Scroll・Toolbox・Enchiridion・Pure Water/Holy Water）。「札を減らす／増やす／固定する」の三方向が全部レリック側に口を持つ。
12. **クラス固有はスターター→ボスの梯子で、固有資源を直接動かす**。各クラス9〜11個・全ティアに散らし、汎用資源（エナジー・ブロック・回復）はクラス限定にしない。固有資源への「開幕から乗せる」型（Vajra型：Data Disk・Damaru・Symbiotic Virus・Twisted Funnel）と「出来事の接着剤」型（Charon's Ashes・Tingsha・Emotion Chip・Violet Lotus）の2形で、後者ほどティアが上がる。

---

# 付録B: StS2 の変化（181→296）

# StS2 のレリック設計は StS1 から何を変えたか（181 → 296）

データ: StS1 181個（tier: Common36/Uncommon37/Rare34/Shop20/Boss30/Special20/Starter4）、StS2 296個（Common30/Uncommon40/Rare50/Shop30/Event35/Ancient100/Starter10/Circlet1）。色タグは StS1 が 170 無色＋キャラ11、StS2 が shared116／event140／キャラ8×5＝40。

## 0. 層の総覧（数だけ先に）

| 層 | StS1 | StS2 | StS2 の中身 |
|---|---|---|---|
| Common | 36 | 30 | shared25＋キャラ5（各1） |
| Uncommon | 37 | 40 | shared28＋キャラ10（各2）＋event2 |
| Rare | 34 | 50 | shared34＋キャラ15（各3）＋event1 |
| Shop | 20 | 30 | shared25＋キャラ5（各1） |
| Boss | 30 | — | **廃止 → Ancient に吸収** |
| Special（イベント限定） | 20 | — | **廃止 → Event と Ancient に分割** |
| Event | — | 35 | event34＋shared1 |
| Ancient | — | 100 | event98＋shared2 |
| Starter | 4 | 10 | キャラ5＋その Ancient 版5 |

StS1 は C/U/R がフラット（36/37/34）、StS2 は **逆ピラミッド（30/40/50）**＝レアの「種類」が最多。抽選確率は本家どおり C50/U33/R17 なので、レア1個あたりの提示率は下がり「同じレアを2回見ない」在庫になっている。

## 1. 層構造の変化：Ancient とは何か

説明文から読める Ancient 100個の正体は「**通常プール（宝箱・エリート・ショップ）に乗らない特別枠を1層に統合したもの**」。色タグ `event` は「通常抽選に入らない」を示す供給フラグであってキャラ色ではない（Ancient にキャラ固有は 0 個＝100個すべて共通）。内訳は4系統。

1. **StS1 ボスレリックの後継**——生き残ったボスレリック 11 個がそのまま Ancient（Astrolabe・Black Star・Calling Bell・Empty Cage・Ectoplasm・Pandora's Box・Philosopher's Stone・Runic Pyramid・Snecko Eye・Sozu・Velvet Choker）。新作の「毎ターン+E＋税」もここ（Blessed Antler・Blood-Soaked Rose・Spiked Gauntlets・Seal of Gold・Pumpkin Candle・Whispering Earring・Prismatic Gem）。
2. **Neow の祝福のレリック化**——「Upon pickup」の一回効果が **53/100**。Neow's Bones が「gain 2 random Neow Relics」と書く＝Ancient の部分集合として「Neow レリック」がある（Neow's Talisman／Torment／Bones の3つに加え、金・最大HP・カード獲得・変容・除去の一回効果群）。
3. **Pael（新NPC）の供物系 11 個**——Pael's Blood/Claw/Eye/Flesh/Growth/Horn/Legion/Tears/Tooth/Wing（Pael's Wing「You may sacrifice card rewards to Pael. Every 2 sacrifices, obtain a Relic」＝カード報酬を捧げる新しい取引）。
4. **スターターの Ancient 版 5 個**（tier=Starter・color=event: Black Blood／Divine Destiny／Infused Core／Phylactery Unbound／Ring of the Drake）——Touch of Orobas「replace your starter Relic with an Ancient version」で差し替える。StS1 の「Replaces Burning Blood」型ボスレリックがここへ移った。効果は素の 2〜3 倍（Burning Blood 6→12・Cracked Core 稲妻1→3＋1ダメ・Bound Phylactery 召喚1/T→開幕5＋2/T・Divine Right 星3→6・Ring of the Snake 毎戦2ドロー→最初の3ターン2ドロー）。

### Ancient のエナジーと税

| 区分 | 数 | 該当 |
|---|---|---|
| 毎ターン +E（無条件） | 10 | Blessed Antler・Blood-Soaked Rose・Ectoplasm・Philosopher's Stone・Prismatic Gem・Pumpkin Candle・Sozu・Spiked Gauntlets・Velvet Choker・Whispering Earring ——**10/10 が税つき** |
| 毎ターン +E（条件・遅延） | 2 | Seal of Gold（毎T 5G 払う）・Pael's Flesh（3ターン目以降。無税だが遅い） |
| バースト型 | 3 | Very Hot Cocoa（戦闘開始+4E・無税）・Booming Conch（エリート戦のみ+E+2ドロー）・Pael's Tears（余らせた E → 次T +2） |
| **合計** | **15/100** | StS1 ボス層は 12/30（40%）が +E だった → Ancient では 15% に希釈。**残り 85% はエナジー以外**（一回効果・ルール改変） |

明示的なデメリット持ち **25/100**、制約・時限型を含めると **32/100**。種類:

| デメリットの型 | 数 | 該当 |
|---|---|---|
| 呪い・状態札の混入 | 10 | Blessed Antler（毎戦 Dazed×3）・Blood-Soaked Rose（Enthralled）・Calling Bell（固有の呪い）・Cursed Pearl（Greed）・Hefty Tablet（Injury）・Neow's Bones（ランダム呪い）・Preserved Fog（Folly）・Sere Talon（呪い2）・Biiig Hug（シャッフルごと Soot）・Large Capsule（Strike＋Defend 追加） |
| HP／最大HP | 3 | Distinguished Cape（−9 最大HP）・Leafy Poultice（−12 最大HP）・Precarious Shears（−16 HP） |
| 経済の喪失 | 5 | Ectoplasm（金を得られない）・Silken Tress（全財産を失う）・Seal of Gold（5G/T）・Silver Crucible（最初の宝箱が空）・Sozu（ポーション禁止） |
| プレイ制限 | 6 | Velvet Choker（6枚/T）・Fiddle（自ターン中にドロー不可）・Spiked Gauntlets（パワー+1E）・Snecko Eye（混乱）・Whispering Earring（初手を AI「Vakuu」が打つ）・Diamond Diadem（2枚以下でしか発動しない） |
| 敵の強化 | 1 | Philosopher's Stone（全敵 筋力+1） |
| 時限・消耗 | 3 | Pumpkin Candle（5戦で消える・焚き火で再点火）・Toy Box（3戦ごとに左端の Wax レリックが溶ける）・Pael's Legion（発動後2ターン眠る） |
| 構造の縛り | 3 | Golden Compass（幕2のマップを一本道に）・Prismatic Gem（報酬に他色が混ざる）・Toasty Mittens（毎T山札の上を消滅） |

StS1 ボス層で削られた税（Coffee Dripper「休めない」・Fusion Hammer「鍛えられない」・Busted Crown「提示−2」・Runic Dome「意図が見えない」・Cursed Key・Mark of Pain・Slaver's Collar・Hovering Kite・Wrist Blade）は **18/30 が撤去**。「ランの機能を丸ごと封じる税」から「札を混ぜる／数を縛る／時間で切れる税」へ寄せている。

### Event 35

構成: 偽物 9（`FAKE_*`「Anchor???」「Snecko Eye???」等＝本物の劣化コピー。「Poor imitation. Does nothing.」もある）・Wongo 2（Customer Appreciation Badge「Does nothing」／Mystery Ticket「5戦後にレリック3個」）・お茶 3（Bone Tea／Ember Tea／Tea of Discourtesy＝**次の1戦だけ**効く）・変身 1（Sword of Stone＝エリート5体撃破で強力レリックに）・同行者 1（Byrdpip）・StS1 からの降格 5（The Boot・Darkstone Periapt・Dream Catcher・Hand Drill・Maw Bank）・その他新規 14。
- **エナジー: 2**（Fake Happy Flower「5ターンごと」・Fake Venerable Tea Set「休憩後の戦闘で+1」＝どちらも偽物の弱体版）。
- **デメリット: 明示 5**（Big Mushroom 最大HP+20 だが毎戦ドロー−2／Fragrant Mushroom −15HP／**Royal Poison 毎戦 −4HP＝純粋な罰**／**Tea of Discourtesy 次戦 Dazed×2＝純粋な罰**／**Fake Snecko Eye 混乱だけ＝純粋な罰**）＋条件付き 2（Maw Bank・Bing Bong「カード追加が2枚に」）＋無効 2（Merchant's Rug???・Wongo Badge）。
- 型: HP損失・ドロー減・状態札・経済停止・デッキ膨張・「何もしない」。**Event 層の約4割（偽物9＋お茶3＋無効2）はパワーでなく物語の器**＝イベントの結果を表す小道具。

### Shop 30

構成: shared 25＋キャラ各1（Brimstone／Ninja Scroll／Runic Capacitor／Undying Sigil／Vitruvian Minion）。
- **エナジー: 1**（Bread「初手 −2E、以降 +1E」＝税つき）。
- **デメリット: 明示 2**（Bread・Brimstone「毎T 自分+2筋／全敵+1筋」）＋条件 2（Belt Buckle「ポーション0の間 敏捷+2」・Ghost Seed「Strike/Defend が Ethereal」）。
- 特徴: **エンチャント 6**（Gnarled Hammer・Kifuda・Punch Dagger・Royal Stamp・Wing Charm＋換金の Mystic Lighter「Enchanted Attacks +9」）、**一回効果 8**（Cauldron・Dolly's Mirror・Lee's Waffle・Orrery 等）。ショップは「買った瞬間にデッキを直接いじる」枠。StS1 の情報系・ルール曲げ系（Frozen Eye・Melange・Strange Spoon・Medical Kit・Orange Pellets・Clockwork Souvenir）は全部撤去。

## 2. キャラ固有レリック

**8個/キャラ × 5 ＝ 40（総在庫の 13.5%、通常層＋Shop＋Starter 160 個の 25%）**。全員が同じ雛形 **Starter1＋Common1＋Uncommon2＋Rare3＋Shop1**（レア寄り＝ビルドを決めるレリックは希少）。Ancient にキャラ固有は 0。

| キャラ | 機構キーワード（説明文に現れる語） | 個々のレリック |
|---|---|---|
| Ironclad | **Strength**×3・**Exhaust**・**HP loss（自傷）**×2・**Vulnerable**・戦闘後回復 | Burning Blood(S)・Red Skull(C: HP50%以下で筋力+3)・Paper Phrog(U: 脆弱75%)・Self-Forming Clay(U: HP損失→次T ブロック3)・Charon's Ashes(R: 消滅ごと全体3)・Demon Tongue(R: 自ターン初のHP損失を回復)・Ruined Helmet(R: 戦闘初の筋力獲得を2倍)・Brimstone(Shop) |
| Silent | **Shiv**×2・**Poison**×2・**Discard**×2・**Weak**・**Dexterity**・ドロー | Ring of the Snake(S)・Snecko Skull(C: 毒+1)・Tingsha(U: 捨て枚数×3ダメ)・Twisted Funnel(U: 開幕全体毒4)・Helical Dart(R: Shivごと敏捷+1/T)・Paper Krane(R: 弱体40%)・Tough Bandages(R: 捨てごとブロック3)・Ninja Scroll(Shop: 開幕Shiv3) |
| Defect | **Orb／Channel**×6（Lightning・Dark・Orb Slots・passive）・**Focus**・0コスト札 | Cracked Core(S)・Data Disk(C: 集中1)・Gold-Plated Cables(U)・Symbiotic Virus(U: 開幕Dark)・Emotion Chip(R)・Metronome(R: 戦闘初の7オーブ目で全体30)・Power Cell(R: 開幕0コスト札2枚を手札へ)・Runic Capacitor(Shop: 枠+3) |
| Necrobinder | **Summon**・**Osty**（相棒）・**Doom**×2・**Souls**・**Ethereal**・**Retain**・**3E以上の札**・**Minion** | Bound Phylactery(S: 毎T 召喚1)・Bone Flute(C: Osty が攻撃するたびブロック2)・Book Repair Knife(U: 非Minion が Doom で死ぬと回復3)・Funerary Mask(U: 開幕山札に Souls×3)・Big Hat(R: 開幕 Ethereal 札2枚)・Bookmark(R: 毎T末 保持札1枚のコスト−1)・Ivory Tile(R: 3E以上をプレイで+E)・Undying Sigil(Shop: Doom≥HP の敵は与ダメ半減) |
| Regent | **[S] Stars**×4（獲得・消費）・**Forge**・**Minion**・**カード生成**・**Colorless** | Divine Right(S: 開幕星3)・Fencing Manual(C: 開幕 Forge 10)・Galactic Dust(U: 星10消費ごとブロック10)・Regalite(U: 札を生成するたびブロック2)・Lunar Pastry(R: 毎T末 星+1)・Mini Regent(R: 毎T初の星消費で筋力+1)・Orange Dough(R: 開幕 Colorless 2枚)・Vitruvian Minion(Shop: Minion 札の与ダメ・ブロック2倍) |

パターン: 各レリックは **そのキャラの固有軸3本のうち1本だけ**を押し、生成（開幕に資源を配る: Twisted Funnel・Fencing Manual・Funerary Mask）／換金（資源を使うたび: Galactic Dust・Tingsha・Bone Flute）／倍率変更（Paper Phrog・Paper Krane・Vitruvian Minion・Undying Sigil）の3役に分かれる。StS1 由来のキャラ付け 19 個（Brimstone・Charon's Ashes・Data Disk・Twisted Funnel 等、StS1 では無色扱い）を **後付けでキャラ色に振り直した**のも変更点。

共通（shared）側も新キーワードに接続している: **Vigor**（Akabeko）・**Plating**（Gorget）・**Enchant**（Nimble／Sharp／Adroit／Momentum／Swift／Royally Approved／Imbued／Glam／Instinct／Clone／Goopy／Tezcatara's Ember）・**Cook**（Meat Cleaver）・**procure**（ポーション調達: Petrified Toad・Tiny Mailbox・Delicate Frond）・**Ancient Card**（Dusty Tome・Archaic Tooth）・**Multiplayer Cards**（Massive Scroll）・**Wax Relics**（Toy Box）・**Vakuu**（Whispering Earring）・状態札 Soot／Dazed／Enthralled／Greed／Folly／Wishes／Apparitions／Relax／Maul／Luminesce。

## 3. StS1 に無かった設計パターン

1. **エンチャント（札への恒久修飾）**＝15個（Shop6・Ancient8・Event1）。「鍛える」の第2チャンネル。
   - Gnarled Hammer: Upon pickup, Enchant up to 3 Attacks with Sharp 3.
   - Fresnel Lens: Whenever you add a card that gains Block to your Deck, Enchant it with Nimble 2.
   - Mystic Lighter: Enchanted Attacks deal 9 additional damage.（エンチャント数を換金する受け皿）
2. **N ターン目を直接参照するリズム型**（StS1 は Stone Calendar のみ）。
   - Candelabra: At the start of your 2nd turn, gain [E][E].／Horn Cleat: 2nd turn, gain 14 Block.
   - Chandelier: 3rd turn, gain [E][E][E].／Captain's Wheel: 3rd turn, gain 18 Block.／Sparkling Rouge: 3rd turn, +1 Str +1 Dex.
   - Mr. Struggles: At the start of your turn, deal damage equal to the turn number to ALL enemies.
3. **「戦闘/ターンで最初の X」型の倍化・特典**（StS1 は Centennial Puzzle・Akabeko 程度）。
   - Vambrace: The first time you gain Block from a card each combat, double the amount gained.
   - Throwing Axe: The first card you play each combat is played an extra time.
   - Unsettling Lamp: Each combat, the first time you play a card that Debuffs an enemy, double its effect.（他: Ruined Helmet・Permafrost・Metronome・Burning Sticks・Music Box・Mini Regent・Pael's Eye）
4. **マップ・経路・部屋を書き換える**。
   - Golden Compass: Upon pickup, replace the Act 2 Map with a single special path.
   - Fur Coat: Upon pickup, mark 7 random combats. Enemies in those rooms have 1 HP.
   - Lava Rock: The Act 1 Boss drops 2 Relics.（他: Winged Boots・Juzu Bracelet・Planisphere）
5. **エリート戦／ボス戦だけ効く**（踏む理由を数字で作る）。
   - Sling of Courage: Start each Elite combat with 2 Strength.
   - Booming Conch: At the start of Elite combats, draw 2 additional cards and gain [E].
   - War Hammer: Whenever you kill an Elite, Upgrade 4 random cards.／Sword of Stone: Transforms into a powerful Relic after defeating 5 Elites.
6. **時限・消耗・変身（ラン中に価値が動く）**。
   - Pumpkin Candle: Gain [E] at the start of each turn. Extinguishes after 5 combats. Can be Kindled at Rest Sites.
   - Toy Box: Upon pickup, obtain 4 Wax Relics. Every 3 combats, your left-most Wax Relic will melt away.
   - Bone Tea: At the start of the next combat, Upgrade your starting hand.／Wongo's Mystery Ticket: Receive 3 random Relics after 5 combats.
7. **ランの進行で育つ（幕・戦闘数・休憩数スケール）**。
   - Stone Humidifier: Whenever you Rest at a Rest Site, raise your Max HP by 5.
   - The Chosen Cheese: At the end of combat, gain 1 Max HP.／Dragon Fruit: Whenever you gain Gold, raise your Max HP by 1.
   - Pael's Tooth: Upon pickup, remove 5 cards from your Deck. After each combat, randomly add 1 back Upgraded.／Fishing Rod: Every 3 normal combats, Upgrade a random card.
8. **「少なく打つ」ことへの報酬（手数の逆問い）**。
   - Diamond Diadem: Whenever you play 2 or fewer cards in a turn, take half damage from enemies.
   - Pael's Eye: The first time each combat you end your turn without playing cards, Exhaust your Hand, and take an extra turn.
   - Pael's Tears: If you end your turn with unspent [E], gain an additional [E][E] next turn.（他: Ripple Basin・Screaming Flagon・Cloak Clasp）
9. **他キャラ・無色・特殊プールからの札注入**。
   - Sea Glass: See 15 cards from another character. Choose any number of them to add to your Deck.
   - Kaleidoscope: Upon pickup, obtain 2 card rewards from other characters.
   - Toolbox: At the start of each combat, choose 1 of 3 random Colorless cards and add the chosen card into your Hand.（他: Prismatic Gem・Dingy Rug・Lead Paperweight・Orange Dough・Massive Scroll・Dusty Tome）
10. **除去に対価を付けた一回効果**（StS1 の Peace Pipe「休憩で除去」は撤去）。
    - Biiig Hug: remove 4 cards from your Deck. Whenever you shuffle your Draw Pile, add a Soot into your Draw Pile.
    - Precarious Shears: remove 2 cards from your Deck and lose 16 HP.／Precise Scissors: remove 1 card.
    - Preserved Fog: remove 3 cards from your Deck. Add Folly to your Deck.
11. **自動プレイ・選択の委任**。
    - Whispering Earring: Gain [E] at the start of each turn. Vakuu plays your first turn for you.
    - History Course: At the start of your turn, play a copy of your last played Attack or Skill.
    - Choices Paradox: At the start of each combat, add 1 of 5 random cards into your Hand. Add Retain to the chosen card.
12. **偽物・空振りレリック（イベントの結果の器）**。
    - Anchor???: Start each combat with 4 Block.（本物は10）／Snecko Eye???: Start each combat Confused.（+Eが無い）
    - The Merchant's Rug???: Poor imitation. Does nothing.／Wongo Customer Appreciation Badge: Does nothing.
13. **スターターの進化**（Touch of Orobas → Ancient 版 5 個。上記 §1）。
14. **免疫の廃止 → 上限・割合へ**（Ginger／Turnip／Fossilized Helix／Torii／Incense Burner／Orange Pellets／Clockwork Souvenir は全撤去）。
    - Beating Remnant: You cannot lose more than 20 HP in a single turn.
    - Sturdy Clamp: Up to 10 Block persists across turns.（Calipers「15失う」の反転）
    - Paper Krane / Paper Phrog: 弱体・脆弱の倍率を動かす。

## 4. StS1 レリックの存続・変更・撤去

名前一致で **存続 101／撤去 80**（偽物「???」は除外して照合）。名前が変わった実質同型を足すと存続 ≈114（63%）、真の撤去 ≈67（37%）。

**層の移動（存続101の内訳）**: 同層 63（C→C 20・U→U 12・R→R 17・Shop→Shop 11・Starter 3）／**Boss→Ancient 11**／Boss→Starter 1（Black Blood）／昇格 15（U→R 10: Frozen Egg・Molten Egg・Toxic Egg・Mummified Hand・Kunai・Shuriken・Meat on the Bone・White Beast Statue・The Courier・Paper Krane。C→U 4: Akabeko・Nunchaku・Orichalcum・Pen Nib。C→R 1: Art of War）／降格 5（Strike Dummy U→C・Tingsha R→U・Red Mask Special→C・Twisted Funnel Shop→U・Ninja Scroll U→Shop）／**Event へ降格 5**（The Boot・Darkstone Periapt・Dream Catcher・Hand Drill・Maw Bank）。

**効果が実質変わったもの**（文言差を除くと少ない）:
- Akabeko: 「初撃+8」→「開幕 **Vigor 8**」（新キーワードへの移植）
- Tingsha: 捨て1回3ダメ →「捨てた枚数ごと3ダメ」（スケール化）
- Orrery: 「5枚選んで追加」→「カード報酬5回」（選択権の増加）
- Nunchaku: 数値欠落→10攻撃で+E（同値）
- Black Blood: ボスレリック → Ancient 版スターター

**改名・作り直しの実質同型**（撤去80から差し引く分）: Wing Boots→Winged Boots（Rare→Ancient）／Ancient Tea Set→Venerable Tea Set（同値・Common）／Ceramic Fish→Lucky Fysh（9G→15G・C→U）／Prismatic Shard→Prismatic Gem（Shop→Ancient・**+E が付いた**）／Enchiridion→Jeweled Mask（山札から・戦闘中ずっと無料）／Neow's Lament→Fur Coat（最初の3戦→ランダム7戦）／Bloody Idol→Dragon Fruit（回復5→最大HP+1・Shop）／Face of Cleric→The Chosen Cheese／Golden Idol→Bowler Hat（U）／Thread and Needle→Gorget（Plated Armor→Plating・**R→C**）／Calipers→Sturdy Clamp（「15失う」→「10残る」）／Ink Bottle→Iron Club（10枚→**4枚**でドロー・Ancient）／Sundial→The Abacus（シャッフル→+2E ⇒ →ブロック6・Shop）／Question Card→Driftwood（提示+1 → 1回リロール）／Necronomicon→Throwing Axe（呪い無し）／Nilry's Codex→Choices Paradox／Duality→Daughter of the Wind（一時敏捷1→ブロック1）／N'loth's Hungry Face→Silver Crucible の代償側／Frozen Core・Ring of the Serpent→Infused Core・Ring of the Drake（Ancient 版スターター）。

**真に撤去された系統**:
- Watcher 系 6（Damaru・Holy Water・Pure Water・Teardrop Locket・Violet Lotus・Golden Eye）＝キャラ不在
- Bottled 3（Flame／Lightning／Tornado）
- 免疫・無効化 8（Ginger・Turnip・Orange Pellets・Clockwork Souvenir・Omamori・Fossilized Helix・Torii・Incense Burner）
- ボス層の「機能封じ」税 18/30（Coffee Dripper・Fusion Hammer・Cursed Key・Busted Crown・Runic Dome・Mark of Pain・Slaver's Collar・Hovering Kite・Wrist Blade・Sacred Bark・Runic Cube・Inserter・Nuclear Battery・Tiny House ほか）
- 情報・ルール曲げのショップ品 6（Frozen Eye・Melange・Strange Spoon・Medical Kit・Blue Candle・Discerning Monocle）
- 報酬操作 6（Matryoshka・Tiny Chest・Preserved Insect・N'loth's Gift・Singing Bowl・Smiling Mask）
- 休憩で除去（Peace Pipe）→ 一回効果の除去群へ
- その他（Dead Branch・Bird-Faced Urn・Champion Belt・Du-Vu Doll・Magic Flower・The Specimen・Toy Ornithopter・Mutagenic Strength・Gremlin Visage・Warped Tongs・Cultist Headpiece・Spirit Poop・Red Circlet・Ssserpent Head）

撤去率は元の層で偏る: Special 18/20・Boss 18/30・Rare 16/34 が高く、Common 8/36・Shop 7/20 が低い。**残したのは「毎戦確実に仕事をする軽い共通レリック」、削ったのは「ラン構造を曲げる特殊品」で、後者は Ancient として作り直された。**

## 5. 小さなデッキビルダーへの持ち帰り（本家並みに充実させるための数字）

1. **在庫と供給の比を 6:1 以上にする**。本家の通常プール（C30/U40/R50＋キャラ各6）120〜126 に対し1ランで手に入るのは宝箱・エリート・ショップ経由で 10〜14 個＝比 9〜12:1。3幕・供給14回のゲームなら **通常層 60〜75（C25%/U33%/R42%）＋ボス層 10〜12＋イベント／一回効果 15〜20 ＝ 90前後**が本家の 1/4〜1/3 の縮尺（現在の 39 は比 2.8:1 で、走破ランは在庫の 4〜5 割を見る計算）。
2. **レアを最多にする（逆ピラミッド）**。抽選確率 C50/U33/R17 のまま種類数を C<U<R にすると、レア1個あたりの提示率が下がり「同じレアが二度出ない」在庫になる。本家は Rare 50 に対し Common 30。
3. **税はボス層に閉じ込め、通常層は無税にする**。本家の C/U/R 120 個に恒常デメリットは 0（条件付きのみ）。Ancient は明示 25%・制約込み 32%、うち毎ターン+E 12 個は 11 個が税つき。逆に **ボス層の 85% はエナジー以外**（一回効果 53％・ルール改変）なので、ボス3択が「3つとも上限+1」にならないよう非エナジー系を 6 割以上にする。
4. **一回効果（Upon pickup）を全体の 25% 持つ**（本家 73/296。StS1 は 11%）。持ち物が増えず・表示も査定も要らない最安の在庫。系統は カード獲得／変容・鍛え／除去（対価つき）／金・最大HP／エンチャント付与／レリック・ポーション調達 の6つで、除去には必ず対価（HP・呪い札・Soot）を付ける。
5. **カウンター／リズム型を通常層の 25% に**（本家 31/120: 「N枚ごと」15・「Nターン目」6・「戦闘で最初の X」10）。特に **2ターン目・3ターン目に発火する型（Candelabra／Horn Cleat／Chandelier／Captain's Wheel／Sparkling Rouge／Pael's Flesh）**は通常戦 4〜6T の物差しと直結する＝「T1 で終わらない戦闘」を前提にした設計。T2/T3 発火を 4〜6 個。
6. **キャラ固有は 8個/キャラ、雛形は Starter1＋C1＋U2＋R3＋Shop1**（総在庫の 13.5%・通常層の 25%）。各レリックは固有軸3本のうち1本だけ（生成／換金／倍率変更の3役）。1色に 3軸×2〜3 ＝ 8 が本家の密度で、レア寄り（3/8）にする。
7. **スターターの進化版を 1 個ずつ用意し、差し替えレリック1個で取らせる**（Ancient 版スターター5＋Touch of Orobas。効果は素の 2〜3 倍）。「ボスレリックがスターターを置換」の StS1 形より、1個の差し替え口で全キャラに配れる。
8. **エンチャント（札への恒久印）をレリックの第2チャンネルにする**（本家 15 個＝Shop 6・Ancient 8・Event 1、換金の Mystic Lighter 込み）。鍛えると別軸で「攻撃3枚に印」「ブロック札を取るたび印」のように配布条件を変えられ、CardInstance のフラグ1つで実装できる。
9. **ラン中に価値が動くレリックを 5〜8%**（時限: Pumpkin Candle・Toy Box／次戦限定: 3種のお茶／変身: Sword of Stone／遅延: Wongo's Ticket／成長: Stone Humidifier・Chosen Cheese・Pael's Tooth）。「取った瞬間が最大」を崩し、焚き火・エリート数・戦闘数がレリックの価値に接続する。
10. **免疫は作らず、上限と割合で受ける**。「〜にならない」8 個は全廃、代わりに Beating Remnant（1ターン 20HP 上限）・Sturdy Clamp（ブロック10持ち越し）・Paper Krane（弱体 40%）。敵側のアーティファクトと対になる「プレイヤー側の付与無効」は本家も持たない。
11. **エリート戦限定の強化と経路レリックで「踏む理由」を数字にする**（Sling of Courage +2筋・Booming Conch +E+2ドロー・War Hammer 撃破で4枚鍛え・Black Star／Lava Rock でレリック追加・Golden Compass／Fur Coat／Winged Boots で経路そのものを変える）。エリート回避ルートを許容するなら、その対は「エリートを取ると乗算で伸びるレリック」で作る。
12. **イベント層の 4 割は「物語の器」でよい**（偽物 9・お茶 3・無効 2）。パワーを持たないレリックを在庫に数えることで、?イベントの結果を「金か HP か」以外の物で語れる。

---

# 付録C: うちの39個との差分監査と実装可否

# レリック監査 — 本家 StS1 との対照とギャップ（2026-09-12）

対象: `src/data/relics.json`（39個）／`src/engine/types.ts` の `RelicDef`・トリガー・条件・効果／`src/engine/run.ts` の `drawRelicOptions`・`afterVictory`・`applyRelicBonus`・`openShop`・`resolveUnknown`／`src/engine/map.ts`（`ELITE_COUNT=4`・`SHOP_COUNT=3`・`treasureRowFor`）／`src/data/events.json`（36件）／`docs/relic-redesign-proposal.md`・`docs/relics-design.md`／CLAUDE.md「レリック」行／本家データ `sts1_relics.json`（181個: C36・U37・R34・Boss30・Shop20・Special20・Starter4）。

エンジンの現行の器（レリックが使えるもの）:
- **A型**（`effects`＝戦闘開始時に不可視の置物として注入）: トリガー30種（onCombatStart / onTurnStart / onAttackIncoming / onAttacked / onEnemyAction / onEnemyBuffed / onEnemyDefended / onAttackPlayed / onSpellPlayed / onCardPlayed / onSetDestroyed / onHealed / onBlockGained / onActionNegated / onHpLost / onCardExhausted / onCostExhausted / onPermanentEntered / onImpulsePlayed / onRandomPlayed / onAetherGained / onCardSet / onReactionFired / onGrowthGained / onMomentumGained / onSelfExhausted ほか）、条件16種（hpAtOrBelowRatio / minDamageTaken / maxActionValue / minActionValue / minGrowth / minMomentum / minEnergyMax / blaze / enemyIntent / enemyIntentNot / enemyExposed / perfectBlockLastPhase / targetDead / actionKinds / lastActionNoHpLoss / healedThisTurn）、効果≈90種、`target:'all'`、`eliteBossOnly`。
- **B型**（`bonus`＝ラン定数）: maxHp / victoryHeal / victoryHealFlat / rewardChoices / campfireRatio / campfireForge / noRest / restMaxHp / goldPerVictory / goldMultiplier / eliteGoldBonus / goldOnPickup / upgradeRandomOnPickup / eliteRelicPicks / brandOnRelic / shopPriceRatio / shopUpgradeDiscount / removalStepDelta / fusionDiscount（大工の道具撤去後は**参照するレリック無し＝死にキー**）。
- **C型**（`combatRule`）: setDamageReduction / retrieveFree / energyMaxRefBonus / harvestKeep / revealIntents（デバッグ専用）/ revealOnSet（蜃気楼撤去後は**死にキー**）。
- 供給層 `RelicRarity`: common / uncommon / rare / boss / shop / **event（型にはあるが該当レリック0・`drawRelicOptions` の `'event'` 供給源も C/U/R をロールするだけ＝死に層）**。`actMin` / `actMax` で幕を絞れる。

---

## 1. 現行39個の一覧

型: A=フック効果／B=ラン定数／C=戦闘ルール改変。層は `rarity`。

| # | id | 名前 | 層 | 型 | タイミング | ペイロード | 本家StS1の対応 | 備考（経緯） |
|---|---|---|---|---|---|---|---|---|
| 1 | relic_thorn_crown | 茨の冠 | C | A | 戦闘内誘発（被攻撃後） | 返しダメージ3 | Bronze Scales（C・棘3） | |
| 2 | relic_sage_scroll | 賢者の巻物 | C | A | 戦闘開始 | ドロー1 | Bag of Preparation（C・本家は2） | 2→1 弱体化（2026-09-01） |
| 3 | relic_swift_boots | 早駆けの靴 | C | A | 戦闘開始 | 一時エナジー+1 | Lantern（C） | T1系・据え置きで監視 |
| 4 | relic_shield_shard | 大盾の欠片 | C | A | 毎ターン開始 | ブロック2 | 独自（Orichalcum の無条件版） | 1→2 半戻し |
| 5 | relic_vanguard_shield | 先手の盾 | C | A | 戦闘開始 | ブロック5 | Anchor（C・本家は10） | 8→5 弱体化 |
| 6 | relic_iron_heart | 鉄の心臓 | C | B | 取得時 | 最大HP+8 | Strawberry（C・+7） | |
| 7 | relic_hunters_boon | 狩人の恵み | U | B | 勝利時（HP≤30%） | 回復8 | Meat on the Bone（U・≤50%で12） | 50%→30% |
| 8 | relic_collectors_bag | 収集家の鞄 | C | B | 報酬時 | 提示+1 | Question Card（U） | |
| 9 | relic_deep_breath | 深呼吸の香 | C | B | 焚き火 | 休む回復35% | Regal Pillow（C） | 50→35% |
| 10 | relic_growth_seed | 成長の種 | C | A | 戦闘開始 | 成長+1 | Vajra（C・筋力+1） | 2→1。T1系監視 |
| 11 | relic_swift_sash | 韋駄天の帯 | U | A | 毎ターン開始 | 勢い+1 | 独自（緑。Damaru/Duality 型の毎T小バフ） | |
| 12 | relic_oldroot_cup | 古根の杯 | Boss | A+B | 戦闘開始／焚き火 | 上限+1／休めない | Coffee Dripper（Boss） | 代償付与済 |
| 13 | relic_raptor_eye | 猛禽の眼 | U | A | 戦闘開始 | 敵全体に急所1 | Bag of Marbles（C・脆弱1） | 装甲持ちに乗らない |
| 14 | relic_merchant_scale | 商人の秤 | R | B | 勝利時 | ゴールド+8 | Golden Idol（Special・+25%） | |
| 15 | relic_smith_whetstone | 鍛冶の砥石 | R | B | 焚き火 | 鍛える2枚（1幕1回） | 独自（Girya に近い「焚き火の追加価値」） | 1幕1回に絞り |
| 16 | relic_talisman_pouch | 符師の懐 | U | A | 戦闘内誘発（仕込む） | ドロー1 | 独自（からくり） | |
| 17 | relic_quiet_bell | 静かな鈴 | U | C | 常在（仕込み中） | 敵攻撃-2（最低1） | 独自（からくり。Torii/Tungsten Rod の条件版） | -1→-2 |
| 18 | relic_double_talisman | 二重の符 | U | A | 戦闘開始 | 仕込み枠+1 | 独自（からくり。枠+型＝Potion Belt/Runic Capacitor） | 2026-09-09 追加・ロールバック条件付き |
| 19 | relic_crown_shard | 王冠の欠片 | Boss | A+B | 戦闘開始／報酬 | 上限+1／提示-1 | Busted Crown（Boss・本家は-2） | |
| 20 | relic_cursed_key | 呪いの鍵 | Boss | A+B | 戦闘開始／レリック取得時 | 上限+1／烙印1 | Cursed Key（Boss） | 供給源を問わずに拡張 |
| 21 | relic_philosopher_stone | 賢者の石 | Boss | A | 戦闘開始 | 上限+1／敵全員筋力+1 | Philosopher's Stone（Boss） | |
| 22 | relic_slaver_collar | 鎖の首輪 | Boss | A | 戦闘開始（エリート・ボスのみ） | 上限+1 | Slaver's Collar（Boss） | `eliteBossOnly` の唯一の使用者 |
| 23 | relic_whetstone_chip | 砥石の欠片 | C | B | ショップ | 鍛える-25G | 独自（Discerning Monocle の限定版） | actMax 2 |
| 24 | relic_herb_pouch | 薬草袋 | C | B | 勝利時 | 回復3 | Burning Blood（Starter・6）の縮小 | |
| 25 | relic_old_purse | 古い財布 | C | B | 勝利時 | ゴールド+4 | Golden Idol の小型 | actMax 2 |
| 26 | relic_momentum_whip | 勢いの鞭 | U | A | 戦闘内誘発（勢い獲得） | ダメージ1 | 独自（緑） | |
| 27 | relic_reading_glasses | 読みの眼鏡 | U | A | 戦闘内誘発（からくり発動） | 次のカード-1 | 独自（からくり） | |
| 28 | relic_retrieve_cord | 回収の紐 | U | C | 常在 | 取り出し0E | 独自（からくり） | |
| 29 | relic_mortar | 薬研 | U | B | 焚き火（休む） | 最大HP+2 | 独自（Eternal Feather/Girya 系） | |
| 30 | relic_loot_bag | 戦利品袋 | U | B | エリート撃破 | ゴールド+20 | 独自（Golden Idol のエリート限定） | actMax 2 |
| 31 | relic_great_tree_heart | 大樹の心 | R | C | 常在 | 上限参照+1 | 独自（緑。Chemical X 型＝参照値+） | 上限参照札は3枚のみ |
| 32 | relic_steadfast_root | 不動の根 | R | A | 戦闘開始（HP≤50%） | ブロック10 | Red Skull × Anchor（条件付き開幕） | |
| 33 | relic_harvest_sickle | 収穫の鎌 | R | C | 常在 | 放出後に成長2残る | 独自（緑） | 成長放出は人間0/4ピック |
| 34 | relic_membership_card | 会員証 | Shop | B | ショップ | 全品半額 | Membership Card（Shop） | |
| 35 | relic_golden_boots | 金の靴 | Shop | B | 勝利時 | ゴールド×1.5 | Golden Idol（+25%） | |
| 36 | relic_removal_chisel | 除去の鑿 | Shop | B | ショップ | 除去の逓増なし | Smiling Mask（C） | |
| 37 | relic_witch_scale | 魔女の秤 | Boss | B | 報酬時／取得時 | 提示+2／最大HP-10 | 独自（Busted Crown の逆） | |
| 38 | relic_black_star | 黒星の欠片 | Boss | B | エリート撃破 | 3択から2個 | Black Star（Boss） | actMin 2 |
| 39 | relic_tiny_house | 小さな家 | Boss | B | 取得時 | 最大HP+8／60G／ランダム1枚鍛え | Tiny House（Boss） | 代償なし枠 |

集計:
- 型: A単独15・A+B 3・B単独17・C 4（B型が全体の51%＝本家の「戦闘内で鳴る」中心と逆）。
- 層: C12（うち actMax2 が2）・U11（actMax2 が1）・R5・Boss8（actMin2 が1）・Shop3・Event0。
- タイミング: 戦闘開始12（31%）／毎T開始2／戦闘内誘発4（茨の冠・符師の懐・勢いの鞭・読みの眼鏡）／常在4／勝利時6／取得・報酬時5／焚き火3／ショップ3。
- 本家に直接の対応がある: 21／独自: 18（うち緑固有5・からくり固有5・経済/焚き火系8）。
- ゴールド・ショップ系が9個（23%）。本家は約10/181（6%）。

---

## 2. 供給の算数（3幕フルラン）

### 2-1. 供給源ごとの提示

| 供給源 | 1ランの回数 | 提示 | 抽選層 | 出典 |
|---|---|---|---|---|
| エリート撃破 | 各幕4配置・「3個以上踏める経路」保証・回避可。人間ランの実測は2〜3/幕 → **6〜9回**（0〜12） | 3択・1個（黒星で2個） | C/U/R 50/33/17 | `ELITE_COUNT=4`・`afterVictory` |
| 幕ボス撃破 | **3回・確定** | 3択・1個 | boss層のみ（尽きたら R→U→C） | `drawRelicOptions('boss')` |
| 宝箱行 | 幕2・幕3の全ノード行 → **2回・確定**（幕1は撤去） | 3択・1個・スキップ可 | C/U/R | `treasureRowFor` |
| ?→宝箱 | ?は総ノードの22%・経路上≈3/幕≈9〜10/ラン × 宝箱解決5.2%（実測） → **≈0.5回** | 3択 | C/U/R | `UNKNOWN_PITY_BASE.treasure=2` |
| ?→イベントのレリック選択肢 | 幕1: 9件中1／幕2: 8件中4／幕3: 6件中0／祠: 6件中1／ワンタイム: 6件中3。?→イベント65%（実測） → **≈1〜1.5回**（幕2に偏る） | 1個固定・対価付き（負傷/烙印/HP/金） | C/U/R を1つロール | `events.json`・`'event'` 供給源 |
| ショップのレリック枠 | 固定3/幕・到達保証。経路上≈1〜2/幕 → **枠を4〜5回見る**。150G（会員証75G） | 1枠 | shop層優先→C/U/R | `openShop`・`SHOP_RELIC_PRICE` |
| 合計 | 提示機会 **≈17〜21回**、取得 **≈12〜18個** | | | 実測: 友人ラン18個・人間#6 10個・段3検証12個 |

本家StS1: エリートは**3択なし1個**、宝箱も1個（大中小の層ロール）、3択は**ボスのみ**。1ランの取得≈13〜17個、目にする「顔」≈20個／181＝約11%。
うちは**全供給源が3択**なので露出が3倍。1ランで目にする顔 ≈ C/U/R スロット30 ＋ boss 9 ＋ shop 3〜4 ＝ **39超＝在庫を1ランで全部見る**。

### 2-2. 層ごとの在庫 vs スロット需要（典型ラン: エリート7・宝箱2・?宝箱0.5・イベント1.3）

C/U/R のスロット需要 ≈ 7×3 + 2×3 + 0.5×3 + 1.3 ≈ **30スロット**（期待: C15／U10／R5）。取らなかった候補は候補列に残る（`relicQueue` から外れるのは取得時のみ）。

| 層 | 在庫 | 1ランのスロット需要 | 何が起きるか |
|---|---|---|---|
| common | 12（幕3は actMax2 で10） | ≈15 | 幕2終了までに4〜5個取ると幕3の候補は5個前後。幕3の common スロット（≈5）は**全部「既に見た顔」**。隣層フォールバックで空にはならないが選択が退屈 |
| uncommon | 11（幕3は10） | ≈10 | 最も健全。幕3でも6〜7個の候補 |
| rare | **5** | ≈5 | 幕2までに2個取ると残り3個が rare ロールのたびに再登場。K「★レアは3幕通算5回中4回が死に札」の構造要因（軸専用レア3/5＝大樹の心・収穫の鎌・砥石） |
| boss | 8（幕1は7） | 3幕×3択＝9 | 幕1で断った2つが幕2に再登場する確率 71%（1−C(5,3)/C(7,3)）。幕3は候補6のうち既出が最大4 → **幕3のボス3択はほぼ確実に「断ったもの」を含む**（期待2/3）。本家はクラス別≈20個 |
| shop | 3 | ショップ枠4〜5回 | 1軒目で3種のどれか、買わなければ同じ顔が全ショップに並ぶ。**3個買い切ると4軒目からショップ専売が無い**（C/U/R に落ちる） |
| event | 0 | — | 死に層 |

### 2-3. 判定
- 在庫39は提案書の目標「本家の1/4＝40」を満たしたが、**供給側が本家並み以上（エリート4/幕＝本家2〜3/幕・全供給源3択・黒星2個取り）**なので、実効在庫は本家換算で約13個相当。幕3の3択は common／rare／boss の3層で再登場が構造的に起きる。
- 是正の方向は2つ: ①在庫を60前後へ（C20／U20／R12／Boss8＝幕3でも rare が尽きない） ②露出を絞る（エリートは3択→1個固定 or 「レリック／金+40G」の二択、宝箱行だけ3択）。本家の文法は②（3択はボスだけ）。

---

## 3. カテゴリ別ギャップ（タイミング × ペイロード × 層）

本家181個を型で分類し、うちの該当数と並べた。「0〜1」が穴。

| # | カテゴリ（型） | 本家の例（数） | うち（数） | 判定 |
|---|---|---|---|---|
| G1 | **カウンター／リズム**（N枚・N回・Nターンごと） | Nunchaku・Pen Nib・Ink Bottle・Kunai・Shuriken・Ornamental Fan・Letter Opener・Happy Flower・Incense Burner・Sundial・Velvet Choker・Pocketwatch（12） | **0** | 穴。`every` が無い |
| G2 | **ターンN固定** | Horn Cleat（T2）・Captain's Wheel（T3）・Stone Calendar（T7）（3） | **0** | 穴。条件 `turn` が無い（`state.turn` はある） |
| G3 | **シャッフル誘発** | Sundial・The Abacus・Melange（3） | **0** | 穴。リシャッフルのイベントが無い |
| G4 | **初回／一度きり**（first X each combat/turn） | Akabeko・Centennial Puzzle・Hovering Kite・Necronomicon・Fossilized Helix（5） | **0** | 穴。`once` が無い |
| G5 | **ターン終了時** | Orichalcum・Cloak Clasp・Art of War・Pocketwatch・Nilry's Codex（5） | **0** | 穴。`onTurnEnd` が無い |
| G6 | **タイプ参照のプレイ誘発**（攻撃3枚／スキル3枚／パワー／0コスト） | Kunai・Shuriken・Fan・Letter Opener・Bird-Faced Urn・Mummified Hand・Wrist Blade・Strike Dummy・Duality・Necronomicon（10） | **0**（物理／呪文／リアクション／置物の4分割をレリックが一度も参照していない） | 穴。単発は onAttackPlayed/onSpellPlayed/onPermanentEntered で可、「3枚ごと」は G1 |
| G7 | **被弾／HP損失誘発（戦闘内）** | Centennial Puzzle・Self-Forming Clay・Runic Cube・Red Skull・Emotion Chip（5） | 茨の冠（onAttacked）・不動の根（開幕HP条件）（2、いずれも本家型でない） | 薄い。onHpLost は**カード自傷限定**（ルプチャー式）で被弾に反応しない。被弾でドローしても敵ターン終了の全捨てで消える |
| G8 | **ダメージ受け方の改変** | Tungsten Rod・Torii・Fossilized Helix・Lizard Tail・The Boot（5） | 静かな鈴（条件付き-2）（1） | 薄い。C型のダメージ経路キーが1つ |
| G9 | **キル誘発** | Gremlin Horn・The Specimen（2） | **0** | 穴。`onEnemyDied` が無い（`mournStrength` の死亡走査は既にある） |
| G10 | **報酬の質**（卵・魚・鉢・レア率・追加報酬） | Molten/Toxic/Frozen Egg・Ceramic Fish・Singing Bowl・N'loth's Gift・Prayer Wheel・Prismatic Shard（8） | **0**（提示数の増減3個＝量のみ） | 穴 |
| G11 | **取得時の一回きり変成** | Astrolabe・Pandora's Box・Empty Cage・Calling Bell・Whetstone・War Paint・Orrery・Dolly's Mirror・Old Coin・Lee's Waffle・Mango・Pear（12） | 鉄の心臓・小さな家（2） | 薄い。transformCard／duplicateCard／removeCard は**イベント効果として実装済み**なのに B型に口が無い |
| G12 | **手札／ドロー／エナジー規則の改変** | Runic Pyramid・Snecko Eye・Ring of the Serpent・Ice Cream・Calipers・Unceasing Top・Gambling Chip・Frozen Eye（8） | **0**（ドロー枚数はリーダー個性のみ） | 穴。C型に drawPerTurn／retainHand／energyCarry／blockKeep が無い |
| G13 | **焚き火の選択肢追加** | Girya（鍛錬）・Peace Pipe（除去）・Shovel（発掘）・Dream Catcher（休むと札）・Ancient Tea Set・Eternal Feather（6） | 深呼吸の香・薬研・砥石（量の修飾3）＋杯の代償 | 穴。「第3の選択肢」が0。**除去の焚き火廃止（2026-09-03）とは裁定が要る** |
| G14 | **マップ／経路／部屋** | Wing Boots・Juzu Bracelet・Tiny Chest・Maw Bank・Ssserpent Head・Matryoshka・Meal Ticket・N'loth's Hungry Face（8） | **0**（黒星は供給量） | 穴。B型にマップ系キーが無い |
| G15 | **状態異常への免疫／改変** | Ginger・Turnip・Clockwork Souvenir・Odd Mushroom・Orange Pellets・Paper Krane・Paper Phrog・Champion Belt（8） | **0**（プレイヤー側の状態異常9種に触るレリックが無い。敵側アーティファクトはある） | 穴 |
| G16 | **経済（ノード単位・行動単位）** | Maw Bank（階ごと）・Ssserpent Head（?ごと）・Ceramic Fish（札追加ごと）・Meal Ticket・Bloody Idol・Old Coin（6） | 勝利ごと4個＋ショップ割引3個＋エリート1個（8） | 量は過剰（23%）だが**全部「勝利時」か「ショップ」**＝ノード単位0 |
| G17 | **イベント／呪い連動** | Darkstone Periapt・Omamori・Blue Candle・Du-Vu Doll・Cursed Key・Calling Bell（6） | 呪いの鍵（代償側のみ）（1） | 穴。烙印を**資産**にする側が0（烙印・仮初の烙印・負傷の供給源はイベント15択・鍵・敵ギミック） |
| G18 | **敵デバフの増幅**（付与量・倍率・連鎖） | Snecko Skull・Paper Phrog・Paper Krane・Champion Belt・Hand Drill（5） | 猛禽の眼（開幕付与）（1） | 薄い。急所×1.5／威圧-25%／延焼の量を触る C型が無い |
| G19 | **ボス（非エナジー）** | Astrolabe・Black Star・Calling Bell・Empty Cage・Pandora's Box・Snecko Eye・Runic Pyramid・Runic Cube・Ice Cream・Hovering Kite・Wrist Blade・Tiny House 他（20） | 魔女の秤・黒星・小さな家（3） | 薄い。人間#7b「全部デメリット付きだと嬉しくない」の受け皿が3 |
| G20 | **ボス（エナジー+代償）** | Busted Crown・Coffee Dripper・Cursed Key・Ectoplasm・Fusion Hammer・Mark of Pain・Philosopher's Stone・Runic Dome・Slaver's Collar・Sozu・Velvet Choker（11） | 5 | ほぼ充足。Ectoplasm（`goldMultiplier:0` で今すぐ）・Fusion Hammer（鍛えられない）・Mark of Pain（負傷2）・Runic Dome（意図非表示）・Velvet Choker（プレイ上限6＝拘束の器を再利用）が未 |
| G21 | **クラス固有** | データの色タグ11＋実質クラス向け≈23（Snecko Skull・Paper Krane・Ninja Scroll・Twisted Funnel・Tingsha・Charon's Ashes・Magic Flower・Brimstone・Red Skull・Self-Forming Clay・Emotion Chip・Damaru・Duality…）≈34 | 緑5・からくり5、**青0・赤0・白0・黒0** | 穴。しかも `RelicDef` に色ゲートが無い（青レリックを作るとこのはにも出る） |
| G22 | **戦闘開始時の小バフ** | Anchor・Bag of Prep・Lantern・Vajra・Bag of Marbles・Bronze Scales・Blood Vial・Oddly Smooth Stone・Thread and Needle・Red Mask・Ninja Scroll・Toolbox・Enchiridion・Gambling Chip（14） | 12（賢者の巻物・靴・盾・種・猛禽・二重の符・不動の根・杯・王冠・鍵・石・首輪） | **偏り**。うちの31%がここ。本家は14/181＝8% |
| G23 | **毎ターン開始の固定出力** | Mercury Hourglass・Brimstone・Damaru・Happy Flower（周期）（4） | 大盾の欠片・韋駄天の帯（2） | 妥当 |
| G24 | **エリート／ボス専用** | Preserved Insect・Sling of Courage・Pantograph・Slaver's Collar（4） | 鎖の首輪・戦利品袋・黒星（3） | 妥当（`eliteBossOnly` は首輪だけが使用） |
| G25 | **勝利時の回復** | Burning Blood・Black Blood・Meat on the Bone・Face of Cleric（4） | 狩人の恵み・薬草袋（2） | 妥当（幕ボス全回復・ポーション無しの補正として） |
| G26 | **ポーション** | Potion Belt・Sacred Bark・Toy Ornithopter・White Beast Statue・Cauldron・Sozu（6） | — | 対象外（設計上ポーションが無い） |
| G27 | **うち固有の機構で本家に無い枠** | — | 工房0（大工の道具撤去後）／敵ギミック（装甲・ターン装甲・とげ・再生・激昂・潜伏・因縁・バランス崩し）に触る0／状態異常9種0／からくり壊し（onSetDestroyed）への保険0／「温存」（ReactionHeld イベント）の換金0／リーダー個性の drawPerTurn に触る0 | 本家にない独自の売り場が空いている |

タイミング×層の俯瞰（うち39）:

| タイミング＼層 | C | U | R | Boss | Shop |
|---|---|---|---|---|---|
| 戦闘開始 | 巻物・靴・盾・種 | 猛禽・二重の符 | 不動の根 | 杯・王冠・鍵・石・首輪 | — |
| 毎T開始 | 大盾の欠片 | 韋駄天の帯 | — | — | — |
| 戦闘内誘発 | 茨の冠 | 符師の懐・勢いの鞭・読みの眼鏡 | — | — | — |
| 常在ルール | — | 静かな鈴・回収の紐 | 大樹の心・収穫の鎌 | — | — |
| 勝利時 | 薬草袋・古い財布 | 狩人の恵み・戦利品袋 | 商人の秤 | — | 金の靴 |
| 取得・報酬時 | 鉄の心臓・収集家の鞄 | — | — | 魔女の秤・黒星・小さな家 | — |
| 焚き火 | 深呼吸の香 | 薬研 | 砥石 | — | — |
| ショップ | 砥石の欠片 | — | — | — | 会員証・除去の鑿 |
| **ターン終了／カウンター／シャッフル／キル／マップ／免疫／報酬の質** | **0** | **0** | **0** | **0** | **0** |

rare 5個のうち戦闘内で鳴るのは不動の根だけ（残りはルール改変2・経済1・焚き火1）。本家の rare は Charon's Ashes・Dead Branch・Calipers・Ice Cream・Torii・Tungsten Rod など「戦闘の形が変わる」枠が中心。

---

## 4. 実装可否 — 既存フックで作れるもの／新機構が要るもの

### 4-1. 既存の器だけで作れる（engine 変更ゼロ。データ追加のみ）

色固有は **`RelicDef.colors` の色ゲート（§4-2 N0）を先に入れないと全リーダーに出る**。回復系は「回復は白の専売」裁定に従い白限定で。

| ギャップ | レリック案（例） | 使う器 | 本家対応 |
|---|---|---|---|
| G23 毎T全体ダメ | 水銀の砂時計: 毎T開始時 敵全体に3 | onTurnStart→dealDamage target:all | Mercury Hourglass |
| G22 開幕デバフ | 赤面の面: 戦闘開始時 敵全体に威圧1 | onCombatStart→weakenEnemy all | Red Mask |
| G22 開幕棘 | （茨の冠を onAttacked→counter で既に実装） | — | Bronze Scales |
| G24 エリート限定バフ | 勇気の投石: エリート・ボス戦は開幕成長+2 | eliteBossOnly + onCombatStart→addGrowth | Sling of Courage |
| G24 ボス前回復（白） | 縮図の板: エリート・ボス戦の開幕HP+15 | eliteBossOnly + gainHp | Pantograph |
| G20 ボス（エナジー+代償） | 幽体の袋: 上限+1／戦闘の金が入らない | gainEnergyMax + `goldMultiplier: 0` | Ectoplasm |
| G19 ボス（非エナジー） | 古い金貨: 取得時+150G | `goldOnPickup` | Old Coin |
| G11 取得時 | 大きな果実: 最大HP+14 | `maxHp` | Mango |
| G6 置物プレイ誘発（白） | 鳥面の壺: 置物が場に出るたびHP+2 | onPermanentEntered→gainHp | Bird-Faced Urn |
| G6 置物プレイ誘発 | 干からびた手: 置物が場に出るたび次のカード-1 | onPermanentEntered→discountNext | Mummified Hand（近似） |
| G6 攻撃プレイ誘発 | 二面の帯: 攻撃をプレイするたびブロック1 | onAttackPlayed→gainBlock | Duality（近似） |
| G7 被弾→守り | 自ら固まる粘土: 被攻撃後（HP損失1以上）ブロック3 | onAttacked + condition minDamageTaken:1 →gainBlock（本家の「次ターン」でなく即時。敵フェーズ中のブロックは次の敵行動に効く） | Self-Forming Clay（近似） |
| G7 被弾→資源 | 被攻撃後に成長+1／霊気+1／延焼（憤怒型） | onAttacked→addGrowth/addAether/applyBurn | Runic Cube 型（ドローは不可＝§4-2 N5） |
| G21 青 | 氷の護符: 戦闘開始時 氷壁4／静電の管: 霊気を得るたび敵に2／逆流の栓: 打ち消すたび1ドロー／こだまの環: 毎T反復+1（R） | onCombatStart→gainIceBlock／onAetherGained→dealDamage／onActionNegated→drawCards／onTurnStart→addSpellEcho | Data Disk・Cables 相当の自軸バフ |
| G21 赤 | 蓄熱の炉: 戦闘開始時 敵全体に延焼2／ねじれた漏斗: 開幕全体延焼4（Shop）／火口の指輪: 衝動をプレイするたび勢い+2／賭博師の護符: 乱数札をプレイするたび延焼1 | onCombatStart→applyBurn all／onImpulsePlayed→addMomentum／onRandomPlayed→applyBurn | Twisted Funnel・Snecko Skull 系 |
| G21 白 | 従者の呼び鈴: 戦闘開始時 従者の少年1体（summonId）／聖なる砂: 回復するたびブロック2／軍旗の欠片: 置物登場ごと全体1／鏡の盾: ブロックを得るたび敵に1 | onCombatStart→summonPermanent／onHealed→gainBlock／onPermanentEntered→dealDamage all／onBlockGained→dealDamage | Ninja Scroll 型・Juggernaut 型 |
| G21 黒 | カロンの灰: カードが消滅するたび敵全体に3（R）／忍びの巻物: 戦闘開始時 骨のナイフ3枚／闇市の帳: 消滅コストを払うたび1ドロー／腐葉の種: 戦闘開始時 山札の上2枚を消滅 | onCardExhausted→dealDamage all／onCombatStart→addCardToHand(summonId: 骨のナイフ)／onCostExhausted→drawCards／onCombatStart→exhaustFromDeck | Charon's Ashes・Ninja Scroll |
| G21 緑（追加） | 硫黄: 毎T成長+2・敵全員筋力+1（Shop） | onTurnStart→addGrowth + strengthenEnemy all | Brimstone |
| G27 からくり | 仕込みの甲: 仕込むたびブロック3／読み勝ちの実: からくりを動かすたび成長+2（緑）・霊気+2（青） | onCardSet→gainBlock／onReactionFired→addGrowth/addAether | 独自 |
| G4 開幕HP条件 | 赤い髑髏: 戦闘開始時HPが半分以下なら成長+3 | onCombatStart + hpAtOrBelowRatio（不動の根と同型。本家の「常在」ではなく開幕判定） | Red Skull（近似） |
| G16 経済 | 目利きの片眼鏡: ショップ2割引 | `shopPriceRatio: 0.8` | Discerning Monocle |
| G25 勝利回復 | 黒い血: 勝利ごとHP+12（Boss・白） | `victoryHealFlat` | Black Blood |

### 4-2. 新機構が要るもの（TS engine のスケッチ。C# は `Types.g.cs` 再生成＋`Run.cs`/`Combat.cs` を同形で追随、`npm run goldens` → `dotnet run -- verify`）

規模: S＝1関数内の追加、M＝状態フィールド＋2〜3箇所、L＝新フェーズ／選択UI／ゴールデン更新が要る。

| # | 機構 | 埋まるギャップ | 規模 | エンジンのスケッチ |
|---|---|---|---|---|
| N0 | **色ゲート** `RelicDef.colors?: CardColor[]` | G21（青/赤/白/黒レリックの前提） | S | `drawRelicOptions` の `pool` フィルタに `def.colors === undefined || def.colors.some(c => leader.colors.includes(c))` を足す（`rewardPool` と同じ判定）。fusionDiscount／revealOnSet の死にキー削除も同時に |
| N1 | **周期カウンター** `DeclarativeEffect.every?: number` ＋ `everyScope?: 'turn'\|'combat'` | G1・G6（攻撃3枚ごと／10枚ごと／Nターンごと） | M | 注入された置物インスタンスに `counters?: readonly number[]`（効果index別）。トリガー配信で `c=(counters[i]??0)+1` を書き戻し、`c % every !== 0` なら解決しない。`everyScope:'turn'` は `startPlayerTurn` でゼロ化。onTurnStart＋every:3＝Happy Flower、onAttackPlayed＋every:3＋scope turn＝Kunai/Shuriken/Fan、onCardPlayed＋every:10＝Ink Bottle |
| N2 | **一度きり** `DeclarativeEffect.once?: 'combat'\|'turn'` | G4（Akabeko の「初撃」以外／Centennial Puzzle／Hovering Kite） | S | N1 と同じ counters を使い `c===1` の時だけ解決 |
| N3 | 条件 `turn?: number` / `minTurn?` | G2（Horn Cleat＝T2ブロック14・Captain's Wheel・Stone Calendar） | S | 条件評価に `state.turn === cond.turn` を1行。onTurnStart と組む |
| N4 | **ターン終了トリガー** `'onTurnEnd'` | G5（Orichalcum: ブロック0なら+6／Cloak Clasp／Art of War／Pocketwatch） | S〜M | `endTurn()` の敵フェーズ開始前で置物を走査（勢い・反復の失効より前）。条件に `blockIsZero` / `attacksPlayedThisTurnMax:0` / `playsThisTurnMax:3` を追加（参照値は既存 `attacksPlayedThisTurn`・`playsThisTurn`） |
| N5 | **次ターン繰り越しバッファ** `player.nextTurn?: {draw?,energy?,block?}` ＋効果 `drawNextTurn`/`gainEnergyNextTurn`/`gainBlockNextTurn` | G7・G5・G12（Runic Cube／Self-Forming Clay／Pocketwatch／Art of War／Ancient Tea Set） | M | 敵フェーズ中にドローした札は「敵ターン終了後の全捨て」で消えるので、被弾→ドローは繰り越しが必須。`startPlayerTurn` の通常ドロー後に消費してゼロ化 |
| N6 | **シャッフル誘発** `'onShuffle'` ＋イベント `DeckShuffled` | G3（Sundial: 3回ごと一時マナ+2／Abacus: ブロック6／Melange） | S | `effects.ts drawCards`（883行付近）の「山札0→捨て札を混ぜる」分岐で emit＋置物走査。開幕シャッフルは数えない。N1 と組む |
| N7 | **キル誘発** `'onEnemyDied'` | G9（Gremlin Horn: エナジー+1と1ドロー／延焼の転移） | S | `checkCombatEnd` の死亡走査（`mournStrength` と同じ場所）で新規死亡ごとに1回発火（`EnemyState.deathCounted` フラグ）。勝利判定より前に解決＝最後の1体では空振り（本家同様）。延焼転移は効果 `transferBurn` を追加 |
| N8 | **被弾のHP損失トリガー** `'onEnemyHpLoss'`（onHpLost はカード自傷限定のため別名） | G7（Centennial Puzzle＝N2 と組んで初回3ドロー→N5 経由／Runic Cube） | S | 敵攻撃の解決（combat.ts 1770行付近 `hpLoss>0`）で発火。既存の `onAttacked + minDamageTaken` でも代用できるが、延焼ティック等の「攻撃でない損失」は拾えない点が違い |
| N9 | **ダメージ経路の C型キー** `hpLossReduce`（Tungsten Rod）・`chipTo1Threshold`（Torii）・`preventFirstHpLoss`（Fossilized Helix、`GameState.helixUsed`）・`deathSaveRatio`（Lizard Tail、`RunState.deathSaveUsed` を CombatOptions へ）・`minUnblockedAttack: 5`（The Boot）・`firstAttackBonus: 8`（Akabeko、`firstAttackUsed`） | G8・G4 | M | プレイヤー被弾は combat.ts 1739〜1776、与ダメは `dealDamageToEnemy` と **`damageBreakdown` の両方**（表示と実処理の一致テストが固定している） |
| N10 | **状態異常の C型キー** `immuneStatuses: PlayerStatus[]`（Ginger/Turnip）・`playerArtifact: N`（Clockwork Souvenir。敵側 artifact と同じ「N回弾いて1消費」）・`vulnerableRatio`（Odd Mushroom 0.5→0.25）・`exposeRatio`（Paper Phrog 1.5→1.75）・`weakRatio`（Paper Krane -25%→-40%）・`burnBonus`（Snecko Skull: 延焼付与+1）・`woundCap`（負傷上限5→3） | G15・G18 | S〜M | `applyStatusToPlayer`（1571行）の先頭で immune/artifact を判定し `ArtifactBlocked` 相当のイベントを出す。倍率は `applyEnemyWeak`・急所計算・脆弱計算の定数を options から読む |
| N11 | **手札／エナジー規則の C型キー** `retainHand`（Runic Pyramid: `finishEnemyPhase` の全捨てをスキップ。火傷の生存則・衝動失効は別途維持）・`energyCarry`（Ice Cream: `startPlayerTurn` で `energy = max + 残り`）・`blockKeep: 15`（Calipers）・`drawPerTurnBonus`（Ring of the Serpent。CombatOptions.drawPerTurn に加算）・`playCap: 6`（Velvet Choker。`RESTRAIN_PLAY_CAP` を `min(拘束?3:∞, playCap)` に一般化）・`zeroCostAttackBonus`（Wrist Blade）・`xBonus: 2`（Chemical X。`xHits` の回数+2）・`healRatio`（Magic Flower）・`blockBonusPerCardPlay`（Oddly Smooth Stone。虚弱の逆＝`resolvingCardPlay` 中のブロック+1）・`hideIntents`（Runic Dome。幅表示を隠しからくりの確認ウィンドウでは実値が見える＝set-confirm 固有の強い型）・`strikeBonus`（Strike Dummy。スターター攻撃札のidに+3） | G12・G20・G6 | S each | すべて `launchCombat` の combatRule 集計→ `CombatOptions` → GameState の既存配管に乗る。`hideIntents` は撤去した蜃気楼の鏡像なので裁定要 |
| N12 | **報酬の質 B型キー** `upgradeOnAdd: CardType[]`（卵3種）・`goldOnCardAdd`（Ceramic Fish）・`rareWeight`（N'loth's Gift。`rollRewards` の層ロール）・`extraCardReward`（Prayer Wheel。`RunState.pendingRewardSets`）・`rewardAllColors`（Prismatic Shard。カラーパイ裁定要）・`maxHpInsteadOfCard: 2`（Singing Bowl。新 RunCommand `SkipRewardForMaxHp`） | G10 | M | 前提として **`addCardToDeck(run, card, source)` を1本化**（現在 PickReward／ShopBuyCard／イベント獲得／変成が各自 `deck` に push している）。卵・魚はそこに掛ける |
| N13 | **取得時変成 B型キー** `transformOnPickup: N`（Astrolabe＝変成+鍛え）・`transformStartersOnPickup`（Pandora's Box。`REWARD_EXCLUDED` のスターター札を対象）・`removeOnPickup: N`（Empty Cage。選択UI＝イベントの removeCard と同じ配管）・`upgradeTypeOnPickup`（Whetstone/War Paint。`upgradeRandomOnPickup` にタイプフィルタ）・`healFullOnPickup`（Lee's Waffle）・`relicsOnPickup: 3` ＋ `brandOnPickup`（Calling Bell） | G11・G19 | S〜M | `transformCard`／`duplicateCard`／`upgradeRandomCards`／`removeAllWounds` は run.ts のイベント効果として実装済み＝`applyRelicBonus` から呼ぶだけ。選択が要るものだけ L |
| N14 | **烙印の資産化** `negateBrands: N`（Omamori。`RunState.brandNegates`）・`maxHpOnBrand: 6`（Darkstone Periapt）・効果 `addGrowthPerBrand`（Du-Vu Doll。デッキの烙印数を CombatOptions で渡す）・`brandPlayable`（Blue Candle。status_brand を「プレイ＝HP-1して消滅」に） | G17 | M | 前提として **`addBrand(run, n, source)` を1本化**（現在 `withRelicGainBrands`・イベント・毒の三杯が各自 deck に push）。Blue Candle は playCard の使用不可判定に分岐が要る |
| N15 | **焚き火の第3の選択肢** `campfireOptions: ('dig'\|'train'\|'remove'\|'dream')[]` ＋ RunCommand `CampfireDig`（`drawRelicOptions(run,'chest',1)`→relic-reward）・`CampfireTrain`（`RunState.trainedGrowth` ≤3 → launchCombat が onCombatStart addGrowth を注入）・`CampfireRemove`（型は残置・常に拒否中→レリック所持時のみ許可）・`CampfireDream`（休む→`rollRewards`）／`noForge`（Fusion Hammer）／`restHealPerDeckCards`（Eternal Feather）／`teaSet`（Ancient Tea Set: `RunState.teaSetArmed`→次戦の gainEnergy 2） | G13・G20 | M〜L | 焚き火は「休む／鍛える」の排他二択。Peace Pipe 型は「除去は焚き火から撤去・ショップ専売」（2026-09-03）を**レリック限定で戻す**ので裁定要。UI/CLI の焚き火画面に選択肢追加 |
| N16 | **マップ／部屋 B型キー** `wingBoots: N`（`nextChoices` が次行の全ノードを返し、`ChooseNode` で非隣接なら消費。map.test の到達性不変条件と `verify-map-ui` に影響）・`noUnknownMonster`（Juzu。`resolveUnknown` の monster を0）・`unknownChestEvery: 4`（Tiny Chest。`RunState.unknownVisits`）・`goldPerRow: 12` ＋ `mawBankBroken`（Maw Bank。ShopBuy* で折る）・`goldOnUnknown: 50`（Ssserpent Head）・`chestPicks: 2 × N`（Matryoshka。既存 `relicPicksLeft` を宝箱にも）・`shopHeal: 15`（Meal Ticket。`openShop`）・`shopRestock`（Courier。`ShopBuyCard` で sold 枠を再ロール）・`eliteHpRatio: 0.75`（Preserved Insect。`launchCombat` の hpScale）・`workshopPathMaxBonus`（工房1幕2回。幕2/3のマップ生成時のみ参照可） | G14・G16・G27 | S〜M（Wing Boots のみ L） | すべて run 層の純関数。Wing Boots は「エッジも可視」の原則との整合（UI で「無視して進める」表示）が要る |
| N17 | **小さな新トリガー** `'onCardDiscarded'`（discardCost 支払い・焚べ addCasts の捨てで発火。Tingsha／Tough Bandages／Hovering Kite＝N2 と組む）・`'onPhysicalPlayed'`（onSpellPlayed の鏡。4タイプ参照の穴）・`'onBlockShattered'`（粉砕が1以上壊した時。Hand Drill＝緑「粉砕するたび急所2」）・`'onEnemyDebuffed'`（急所/威圧/混乱を付与した時。Champion Belt＝急所付与ごとに威圧1）・`'onReactionHeld'`（既存イベント `ReactionHeld` を置物走査に接続。からくり固有「温存の換金」）・`'onHandEmpty'`（Unceasing Top。playCard 解決後に手札0なら）・`'onGoldGained'` は run 層なので B型 `healOnGold: 5`（Bloody Idol）で代替 | G6・G18・G27 | S each | 各発火点は既存関数1箇所。C# は `Combat.cs` の同名分岐 |
| N18 | **小さな新効果** `gainBlockPerHandCard`（Cloak Clasp。gainIceBlockPerHandCard の兄弟）・`selfStatus`（Mark of Pain: 開幕負傷2／Gremlin Visage: 開幕弱体1。`applyStatusToPlayer` を効果から呼ぶ）・`upgradeRandomInHand`（Warped Tongs。upgradeInHand の選択なし版）・`cleanseDebuffs`（Orange Pellets。条件 `typesPlayedThisTurn` も要る）・`returnDestroyedSet`（からくり壊しされた札を手札へ。onSetDestroyed をレリック側で受ける）・`addRandomCardToHand`（Dead Branch。色プールを CombatOptions で渡す＝M）・`replayCard`（Necronomicon＝L） | G5・G20・G27 | S〜L | |
| N19 | **敵ギミックへの C型キー** `thornsReduce`（とげ反射-N）・`enrageDelay`（激昂の枚数しきい値+N）・`regenBreakBonus`（再生停止しきい値-N）・`burrowPierce`（貫通が殻を素通り＝2026-09-04裁定の逆をレリックで買う） | G27 | S each | 各ギミックの判定箇所で options を読む。「装甲に急所が乗らない」への答え（`exposeIgnoresArmor`）は装甲の目的と衝突するので裁定要 |

### 4-3. 優先順の私見（穴の大きさ × 実装の軽さ）

1. **N0 色ゲート ＋ §4-1 の色固有レリック**（青/赤/白/黒 各3〜4個）: engine 1行で在庫が+15前後。層の再登場（§2-2）を直接薄める。
2. **N1 every ＋ N2 once ＋ N3 turn ＋ N4 onTurnEnd**: 4つで本家の G1・G2・G4・G5・G6（合計≈35個の型）が開く。カウンターは置物インスタンスの配列1本。
3. **N12 の `addCardToDeck` 一本化 ＋ 卵**: 報酬の質は「レリックでビルドが化ける」本家体験の中核で、うちは0。
4. **N7 onEnemyDied ＋ N6 onShuffle**: 発火点が既にある（死亡走査・リシャッフル）。
5. **N10 免疫／倍率**: 状態異常9種の圧を上げた（2026-09-01〜02）のに解答がカードにも無い色がある。レリックは全色に配れる唯一の口。
6. **N15 焚き火の第3選択肢 と N11 hideIntents／N16 Wing Boots** は裁定が先（除去撤去・蜃気楼撤去・エッジ可視の各決定と衝突）。

### 4-4. 監査で見えた小さな不整合（データ／死にキー）
- `RelicRarity` の `'event'` は該当0・抽選も参照せず。`bonus.fusionDiscount`（大工の道具）・`combatRule.revealOnSet`（蜃気楼）は参照元レリックが無い。
- `eliteBossOnly` の使用者は鎖の首輪のみ。Sling of Courage／Pantograph 型に再利用できる。
- ショップ専売3個は買い切ると4軒目から専売枠が C/U/R に落ちる（本家 Shop 層20）。
- boss 層8は幕3で「幕1・2で断った顔」が期待2/3。代償なし枠（魔女の秤・黒星・小さな家）を Boss 層で増やすか、本家型の非エナジーボス（Runic Pyramid／Ice Cream／Calipers＝N11）を足す。
- 経済系9/39（23%）は本家の約4倍。J/K「終盤の外れ枠」は actMax2 で3個だけ手当て済み。