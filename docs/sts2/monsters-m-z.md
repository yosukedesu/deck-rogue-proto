# Slay the Spire 2 モンスターデータ抽出 — M〜Z (MagiKnight〜Zapbot)

注記: HP・ダメージ等の数値は `AscensionHelper.GetValueIfAscension(level, ascValue, baseValue)` から「基本値/アセンション時の値」の順で記載（表記ゆれあり: 基本/ASC、base/Asc等は同じ意味）。ゲームデザイン研究目的のローカル解析であり、ソースコードの転載は行わず数値・構造のみを日本語で言い換えて記録する。抽出対象は MegaCrit.Sts2.Core.Models.Monsters のうちファイル名が M〜Z で始まるもの（MagiKnight.cs〜Zapbot.cs、Mocks/除く）。

---

## Part A: MagiKnight 〜 PhrogParasite

### MagiKnight (MagiKnight)
- HP: 82(基本) / 89(ASC:ToughEnemies)。MaxInitialHp=MinInitialHpのため固定値（幅なし）。
- Move一覧:
  - FIRST_POWER_SHIELD_MOVE: 攻撃+防御。ダメージ6/7(ASC)、命中後に自身へブロック5/9(ASC)取得。
  - DAMPEN_MOVE: デバフ。対象へ DampenPower を1付与（未知の減衰系パワー、キャスター追跡あり）。
  - PREP_MOVE: 防御のみ。ブロック5/9(ASC)取得（PowerShieldBlockと同量、ダメージなし）。
  - MAGIC_BOMB: 攻撃。ダメージ35/40(ASC)。遠距離風の演出（プレイヤー位置に応じてエフェクト位置調整、ダメージ自体は固定）。
  - RAM_MOVE (SpearMove): 攻撃。ダメージ10/11(ASC)。
- 行動パターン: 固定ローテーション。初手 FIRST_POWER_SHIELD → DAMPEN → RAM → PREP → MAGIC_BOMB → RAM →（以降 RAM→PREP→MAGIC_BOMB のループ）。
- ギミック: 特になし（AfterAddedToRoom未オーバーライド、HP半分分岐なし）。DampenPowerの具体効果は本ファイルからは不明。
- 幕: 不明。

### Mawler (Mawler)
- HP: 72(基本) / 76(ASC)。固定値。
- Move一覧:
  - RIP_AND_TEAR_MOVE: 攻撃。ダメージ14/16(ASC)。
  - ROAR_MOVE: デバフ。対象全体へ VulnerablePower 3付与。
  - CLAW_MOVE: 多段攻撃。ダメージ4/5(ASC) ×2ヒット。
- 行動パターン: RandomBranchState（確率分岐、3技とも重み1.0で均等）。RIP_AND_TEARとCLAWは「連続不可」、ROARは「戦闘中1回きり」の制約付き。初期呼び出しはCLAW_MOVEから。
- ギミック: なし。
- 幕: 不明。

### MechaKnight (MechaKnight)
- HP: 300(基本) / 320(ASC)。固定値。HP水準が高くエリート級かボスと推測。
- AfterAddedToRoom: 自身に ArtifactPower 3付与（開幕デバフ無効3回）。
- Move一覧:
  - CHARGE_MOVE: 攻撃。ダメージ25/30(ASC)。
  - FLAMETHROWER_MOVE: 特殊。対象の手札にBurnカードを4枚追加。
  - WINDUP_MOVE: 防御+バフ。ブロック15固定取得＋自身にStrengthPower+5。「振りかぶり中」フラグ(IsWoundUp)がtrueになり被弾アニメが変化（ゲームプレイ効果なし）。
  - HEAVY_CLEAVE_MOVE: 攻撃。ダメージ35/40(ASC)。画面演出（ラジアルブラー・ヒットストップ）付きの大技。IsWoundUp解除。
- 行動パターン: 固定ローテ。初手 CHARGE → FLAMETHROWER → WINDUP → HEAVY_CLEAVE →（以降 FLAMETHROWER→WINDUP→HEAVY_CLEAVEのループ）。
- ギミック: 開幕アーティファクト3層でデバフ無効化。振りかぶり(WINDUP)→大技(HEAVY_CLEAVE)という溜め技構造。
- 幕: 不明（HP規模からエリート/ボス級と推測）。

### MultiAttackMoveMonster (MultiAttackMoveMonster)
- 基底/テスト用クラス。表示名"BIG_DUMMY"。HP=999固定。Move: POKE（1ダメ×5ヒット、自己ループ）のみ。実モンスターではなくダメージ計算テスト用ダミー。

### MysteriousKnight (MysteriousKnight)
- FlailKnight（範囲外ファイル）を継承した亜種/エリート版。
- AfterAddedToRoom: 基底のAfterAddedToRoomに加え、自身にStrengthPower+6、PlatingPower+6を追加付与。
- HP・Move・行動パターンはFlailKnight基底のまま（本ファイルには記載なし＝不明）。
- ギミック: 開幕から筋力+6・鎧(装甲)+6を持つ強化型。
- 幕: 不明。

### Myte (Myte)
- HP: 61〜67(基本) / 64〜69(ASC)。幅あり。
- Move一覧:
  - TOXIC_MOVE: 特殊。手札にToxicカードを2枚追加。
  - BITE_MOVE: 攻撃。ダメージ13/15(ASC)。
  - SUCK_MOVE: 攻撃+バフ。ダメージ4/6(ASC)＋自身にStrengthPower+2/3(ASC)。
- 行動パターン: 編成スロット依存の初期分岐。SlotName=="first"ならTOXICから、"second"ならSUCKから開始。以降固定ローテ TOXIC→BITE→SUCK→TOXIC…（ループ）。
- ギミック: 複数体編成前提で、スロット位置により初手が変わる。
- 幕: 不明。

### Nibbit (Nibbit)
- HP: 42(基本) / 44(ASC)。Max: 46(基本) / 48(ASC)。
- Move一覧:
  - BUTT_MOVE: 単発攻撃。ダメージ12(基本)/13(ASC)。
  - SLICE_MOVE: 攻撃+防御。ダメージ6(基本)/7(ASC)、命中後に自身へブロック5(基本)/6(ASC)取得。
  - HISS_MOVE: バフ。自身にStrengthPower+2(基本)/+3(ASC)。
- 行動パターン: 初期分岐は編成フラグ依存（`ConditionalBranchState`）。`_isAlone`=trueならBUTT_MOVEから開始（単独編成専用ルート）。`_isAlone`=falseの場合はさらに`_isFront`で分岐: 後列(`!IsFront`)ならHISS_MOVEから、前列(`IsFront`)ならSLICE_MOVEから開始。以降はFollowUpStateの固定ループ SLICE→HISS→BUTT→SLICE…（3すくみ循環）。
- ギミック: 特になし（AfterAddedToRoom未オーバーライド、HP半分分岐・死亡時効果なし）。前列/後列・単独/複数編成で初手だけが変わる。
- 幕: 不明。

### Noisebot (Noisebot)
- HP: 18〜23(基本) / 19〜24(ASC)。
- AfterAddedToRoom: 見た目上の落下位置調整のみ（ゲームプレイ効果なし）。
- Move一覧:
  - NOISE_MOVE: 特殊。Dazed（使用不可札）を2枚生成し、1枚を捨て札山、もう1枚を山札のランダムな位置に注入。
- 行動パターン: NOISE_MOVEのみの単一無限ループ（一切攻撃しない）。
- ギミック: 妨害特化の雑魚。毎ターン確実にDazedを2枚ばらまく。
- 幕: 不明。

### OneHpMonster (OneHpMonster)
- 基底/テスト用クラス。表示名"BIG_DUMMY"。HP=1固定。Move: NOTHING（隠し行動、何もしない）のみ、自己ループ。実データなし。

### Osty (Osty)
- HP=1固定。DeathSfxあり(HasDeathSfx=true)。IsHealthBarVisibleはCreature.IsAlive依存（死亡時HPバー非表示）。
- Move: NOTHING_MOVE（何もしない）のみ、自己ループ。
- ギミック: 通常の敵ではなくプレイヤー側の演出用マスコット/ペット的存在と推測。"revive"アニメ、"dead_loop"アニメ、`CheckMissingWithAnim(Player owner)`のような`IsOstyMissing`判定を持ち、味方の状態表示ギミックの可能性が高い。
- 幕: 不明。

### Ovicopter (Ovicopter)
- HP: 124〜130(基本) / 126〜132(ASC)。
- Move一覧:
  - LAY_EGGS_MOVE: 特殊（召喚）。空きスロットにToughEgg（範囲外ファイル）を最大3体召喚、各卵にMinionPower+1付与。
  - SMASH_MOVE: 攻撃。ダメージ16/17(ASC)。
  - TENDERIZER_MOVE: 攻撃+デバフ。ダメージ7/8(ASC)＋対象へVulnerablePower+2。
  - NUTRITIONAL_PASTE_MOVE: バフ。自身にStrengthPower+3/4(ASC)。
- 行動パターン: 固定ローテ SMASH→TENDERIZER→(分岐)→SMASH…。分岐は「生存する味方が3体以下ならLAY_EGGS、そうでなければNUTRITIONAL_PASTE」。分岐後もSMASHに戻る。初手はSMASH。
- ギミック: 味方の頭数を見て卵召喚か自己バフかを選ぶ編成依存の分岐。召喚された卵はMinionPower（雑魚指定）付き。
- 幕: 不明。

### OwlMagistrate (OwlMagistrate)
- HP: 234(基本) / 243(ASC)。Min=Maxの固定値。
- Move一覧:
  - MAGISTRATE_SCRUTINY: 単発攻撃。ダメージ16(基本)/17(ASC)。
  - PECK_ASSAULT: 多段攻撃×6ヒット。1ヒット4固定（ASC差なし）。
  - JUDICIAL_FLIGHT: バフ（特殊状態移行）。自身にSoarPower+1付与。離陸アニメ再生後、飛行ループSFXを開始し`IsFlying`フラグをtrueに（飛行中は被弾/死亡SFXが専用バリエーションに切替）。
  - VERDICT: 攻撃+デバフ。ダメージ33(基本)/36(ASC)、命中後に対象全体へVulnerablePower+4を付与。自身のSoarPowerを除去し`IsFlying`をfalseへ戻す（着地）。
- 行動パターン: 固定4手ループ。MAGISTRATE_SCRUTINY→PECK_ASSAULT→JUDICIAL_FLIGHT→VERDICT→MAGISTRATE_SCRUTINY…（分岐なし・確率要素なし）。
- ギミック: JUDICIAL_FLIGHTで飛行状態（SoarPower）に入り、次のVERDICTで着地してVulnerable付与＋Soar解除する「浮上→急降下の裁き」の2段構え。飛行中は見た目・SFXが専用バリエーションに切り替わる演出ギミックのみでダメージ計算自体は変化しない（本ファイルからはSoarPowerの回避効果の有無は不明）。HP半分でのフェーズ変化・死亡時効果はなし。
- 幕: 不明。HP234〜243は本抽出範囲内でも高水準で、固定4手の「尋問→処罰」構成から中盤〜終盤のミニボス級と推測。

### PaelsLegion (PaelsLegion)
- HP=9999固定。IsHealthBarVisible=false（実質倒せない/観賞用オブジェクト）。
- Move: NOTHING_MOVEのみ（何もしない）、自己ループ。
- SetupSkins: 同名レリック「PaelsLegion」の所持者が選択したスキンに応じて見た目を切替。
- ギミック: 戦闘参加しない演出専用ユニット。レリック連動の見た目カスタム要素と推測。
- 幕: 不明。

### Parafright (Parafright)
- HP=21固定（アセンションでも変化しない、GetValueIfAscension不使用）。
- HasDeathSfx=false、ShouldDisappearFromDoom=false（通常の死亡演出をしない特殊個体）。
- AfterAddedToRoom: 自身にIllusionPower 1付与。
- Move一覧:
  - SLAM_MOVE: 攻撃。ダメージ16/17(ASC)。自己ループの単一行動。
- ギミック: IllusionPower（幻影）に「復活中(IsReviving)」フラグがあり、被弾アニメが分岐。SFXパスが"obscura_hologram_*"であることから、ボス「Obscura」の幻影分身ユニットと推測。本体(IsPrimaryEnemy)が全滅するまでDeadアニメが再生されない＝倒しても本体が生きていれば消えない可能性。
- 幕: 不明（Obscura関連ならボス戦の随伴ユニット）。

### PhantasmalGardener (PhantasmalGardener)
- HP: 26〜31(基本) / 27〜32(ASC)。
- AfterAddedToRoom: 自身にSkittishPower 6/7(ASC)付与（"臆病"系。アニメ分岐にHasGainedBlockThisTurnがあり、被弾すると自動でブロックを得る系のパワーと推測）。
- Move一覧:
  - BITE_MOVE: 攻撃。ダメージ5（基本・ASC共通）。
  - LASH_MOVE: 攻撃。ダメージ7（基本・ASC共通）。
  - FLAIL_MOVE: 多段攻撃。ダメージ1固定 ×3ヒット（基本・ASC共通）。
  - ENLARGE_MOVE: バフ。自身にStrengthPower+2/3(ASC)。使用のたびEnlargeTriggersが増加し、見た目スケールが対数的に拡大（1+0.1×ln(n+1)）。
- 行動パターン: 4体編成専用。編成スロット(first/second/third/fourth)ごとに初期技が異なる（first→FLAIL、second→BITE、third→LASH、fourth→ENLARGE）。以降は固定ローテ BITE→LASH→FLAIL→ENLARGE→BITE…（ループ）。
- ギミック: 4体同時編成が前提のギミック敵。個体ごとに役割（多段役・単発役・バフ役）が分かれ、ENLARGEを使うほど見た目が巨大化する演出。SkittishPowerで被弾時ブロック自動取得。
- 幕: 不明。

### PhrogParasite (PhrogParasite)
- HP: 61〜64(基本) / 66〜68(ASC)。
- AfterAddedToRoom: 自身にInfestedPower 4付与。
- Move一覧:
  - INFECT_MOVE: 特殊。捨て札山にInfectionカードを3枚追加。
  - LASH_MOVE: 多段攻撃。ダメージ4/5(ASC) ×4ヒット。
- 行動パターン: RandomBranchState。INFECT/LASHをランダム選択（一度選んだ技は連続不可）。初期呼び出しはINFECT側。
- ギミック: InfestedPowerを開幕4層自己付与（効果詳細は別ファイルのため不明。名称的に寄生/死亡時放出系の可能性があるが本ファイルには死亡時処理の記載なし）。
- 幕: 不明。

---

## Part B: PunchConstruct 〜 SoulFysh

### PunchConstruct

- HP: 55(基本) / 60(アセンション) ※Min=Max固定
- Move:
  - READY_MOVE: 防御。ブロック+10（固定値、アセンション非依存）
  - STRONG_PUNCH_MOVE: 単発攻撃。ダメージ 14(基本)/16(アセンション)
  - FAST_PUNCH_MOVE: 多段攻撃×2ヒット。1ヒット5(基本)/6(アセンション)。命中後、対象にWeakPower+1（弱体化デバフ）
- 行動パターン: READY→STRONG_PUNCH→FAST_PUNCH→READY…の固定3手ループ。ただし外部フラグ`StartsWithStrongPunch`がtrueの場合、初期状態がSTRONG_PUNCHから始まる（同型の別個体・変異体用のフック）。
- ギミック: 戦闘開始時（AfterAddedToRoom）に自身へArtifactPower+1（デバフ無効化1回分）を付与。`StartingHpReduction`プロパティで開始時HPを外部から減算可能（弱った状態で登場する編成演出用のフック）。
- 幕: 不明（金属打撃音=Armorサウンド。機械系の敵）

### Queen

- HP: 400(基本) / 419(アセンション) ※Min=Max固定、非常に高い＝ボス級
- Move:
  - PUPPET_STRINGS_MOVE: デバフ。ChainsOfBindingPower+3（拘束系、行動制限デバフと推測）
  - YOUR_MINE_MOVE: 強力な複合デバフ。対象にFrailPower+99、WeakPower+99、VulnerablePower+99を同時付与（事実上の無力化デバフ）
  - BURN_BRIGHT_FOR_ME_MOVE: バフ+防御。自分以外の味方全員にStrengthPower+1、自身はブロック+20
  - OFF_WITH_YOUR_HEAD_MOVE: 多段攻撃×5ヒット。1ヒット3(基本)/4(アセンション)
  - EXECUTION_MOVE: 単発攻撃。ダメージ15(基本)/18(アセンション)
  - ENRAGE_MOVE: バフ。自身にStrengthPower+2
- 行動パターン: PUPPET_STRINGS→YOUR_MINE→分岐（随伴クリーチャーTorchHeadAmalgamが生存中か）。**生存中はBURN_BRIGHT_FOR_MEを無限に繰り返す**（取り巻きへの支援バフループ）。Amalgamが死亡すると分岐が切り替わり、OFF_WITH_YOUR_HEAD→EXECUTION→ENRAGE→OFF_WITH_YOUR_HEAD…の攻撃ループ（Enrageで毎周Strength蓄積）へ移行。
- ギミック: 戦闘開始時に同室のTorchHeadAmalgam（随伴クリーチャー）を参照保持。Amalgamが死亡した瞬間（AfterDeathイベント）に`HasAmalgamDied`フラグを立て、**もしその時点でQueenの次の予定行動がBURN_BRIGHT_FOR_MEだった場合は即座にENRAGEへ強制切替**（`SetMoveImmediate`）＝「取り巻きを先に倒すと即座に激怒フェーズへ移行する」ボス構造。BGMパラメータ（queen_progress）を戦闘進行に応じて変化させる演出制御あり。
- 幕: 不明。随伴ボス級クリーチャーを伴う規模から終盤ボスと推測

### Rocket

- HP: 199(基本) / 209(アセンション) ※Min=Max固定
- Move:
  - TARGETING_RETICLE_MOVE: 単発攻撃。ダメージ3(基本)/4(アセンション)
  - PRECISION_BEAM_MOVE: 単発攻撃。ダメージ18(基本)/20(アセンション)
  - CHARGE_UP_MOVE: バフ。StrengthPower+2(基本)/+3(アセンション)
  - LASER_MOVE: 単発大技攻撃。ダメージ31(基本)/35(アセンション)
  - RECHARGE_MOVE: 休止（SleepIntent、実質何もしない）
- 行動パターン: TARGETING_RETICLE→PRECISION_BEAM→CHARGE_UP→LASER→RECHARGE→(最初へ)の固定5手ループ。
- ギミック: 戦闘開始時にSurroundedPower（相手全体へ）・BackAttackRightPower・CrabRagePowerを自身へ付与＝**複数パーツ構成の合体ボス「Kaiser Crab」の右腕パーツ**であることを示唆（`NKaiserCrabBossBackground`という専用背景ノードを操作）。`ShouldFadeAfterDeath=false`・`ShouldDisappearFromDoom=false`により、このパーツが死んでも画面から消えず、腕の切断アニメを再生する（戦闘終了時のみ本体死亡アニメも再生）。
- 幕: 不明（合体ボスのパーツ。ボス級）

### ScrollOfBiting

- HP: 31(基本)/32(アセンション) 〜 38(基本)/39(アセンション)
- Move:
  - CHOMP: 単発攻撃。ダメージ14(基本)/16(アセンション)
  - CHEW: 多段攻撃×2ヒット。1ヒット5(基本)/6(アセンション)
  - MORE_TEETH: バフ。StrengthPower+2固定
- 行動パターン: 確率分岐（RandomBranchState）を持つ。CHOMP→MORE_TEETH→CHEW→(乱数分岐: CHOMPは連続不可/CHEWは重み2で出やすい)→再度乱数分岐…。**初期行動は`StarterMoveIdx % 3`で3パターン（CHOMP/CHEW/MORE_TEETHのいずれか）からランダム選択**される。
- ギミック: 戦闘開始時にPaperCutsPower+2を自身へ付与（名称的に継続ダメージ/被弾増加系デバフと推測）。見た目スキンをskin1/skin2からランダム抽選する視覚バリエーションあり。
- 幕: 不明

### Seapunk

- HP: 44(基本)/47(アセンション) 〜 46(基本)/49(アセンション)
- Move:
  - SEA_KICK_MOVE: 単発攻撃。ダメージ11(基本)/13(アセンション)
  - SPINNING_KICK_MOVE: 多段攻撃×4ヒット。1ヒット2固定（アセンション非依存）
  - BUBBLE_BURP_MOVE: バフ+防御。ブロック7(基本)/8(アセンション)、StrengthPower+1(基本)/+2(アセンション)
- 行動パターン: SEA_KICK→SPINNING_KICK→BUBBLE_BURP→(最初へ)の固定3手ループ。
- ギミック: 特になし。
- 幕: 不明

### SewerClam

- HP: 56(基本)/58(アセンション) ※Min=Max固定
- Move:
  - PRESSURIZE_MOVE: バフ。StrengthPower+4固定
  - JET_MOVE: 単発攻撃。ダメージ10(基本)/11(アセンション)
- 行動パターン: JET_MOVEを初期状態としてJET⇔PRESSURIZEの固定交互ループ（JET→PRESSURIZE→JET→…）。
- ギミック: 戦闘開始時にPlatingPower（8基本/9アセンション、盾/装甲系）を自身へ付与。
- 幕: 不明

### ShrinkerBeetle

- HP: 38(基本)/40(アセンション) 〜 40(基本)/42(アセンション)
- Move:
  - SHRINKER_MOVE: 強デバフ（`strong: true`指定のDebuffIntent）。ShrinkPower-1（対象を「縮小」させる特殊マイナス値デバフ）
  - CHOMP_MOVE: 単発攻撃。ダメージ7(基本)/8(アセンション)
  - STOMP_MOVE: 単発攻撃。ダメージ13(基本)/14(アセンション)
- 行動パターン: SHRINKER(初手固定)→CHOMP→STOMP→CHOMP→STOMP…（SHRINKERは最初の1回のみ、以降CHOMPとSTOMPの固定交互ループ）。
- ギミック: 特になし（被弾ブラー演出のみ）。
- 幕: 不明

### SingleAttackMoveMonster (ClassName: SingleAttackMoveMonster)

- 基底/デバッグ用テストクラス、実データなし。HP固定999（Min=Max）。POKE攻撃（ダメージ1固定）を無限ループするだけのダミー敵（表示名"BIG_DUMMY"）。

### SkulkingColony

- HP: 70(基本)/75(アセンション) ※Min=Max固定
- Move:
  - SMASH_MOVE: 単発攻撃。ダメージ12(基本)/13(アセンション)
  - ZOOM_MOVE: 単発攻撃（ダメージ14基本/16アセンション）+防御（ブロック10基本/13アセンション）
  - INERTIA_MOVE: 単発攻撃（ダメージ9基本/11アセンション）+バフ（StrengthPower+2基本/+3アセンション）
  - PIERCING_STABS_MOVE: 多段攻撃×2ヒット。1ヒット7(基本)/8(アセンション)
- 行動パターン: SMASH→ZOOM→INERTIA→PIERCING_STABS→(最初へ)の固定4手ループ。
- ギミック: 戦闘開始時にHardenedShellPower+15を自身へ付与（硬い殻＝装甲系バフ）。
- 幕: 不明

### SlimedBerserker

- HP: 266(基本)/276(アセンション) ※Min=Max固定、非常に高い＝ボス〜エリート級
- Move:
  - VOMIT_ICHOR_MOVE: 特殊（カード注入技）。使用不可カード「Slimed」を**捨て札に10枚**直接注入（プレイヤーデッキ汚染）
  - LEECHING_HUG_MOVE: デバフ+バフ。対象にWeakPower+3、自身にStrengthPower+3（吸収型の複合技）
  - FURIOUS_PUMMELING_MOVE: 多段攻撃×4ヒット。1ヒット4(基本)/5(アセンション)
  - SMOTHER_MOVE: 単発大技攻撃。ダメージ30(基本)/33(アセンション)
- 行動パターン: VOMIT_ICHOR→FURIOUS_PUMMELING→LEECHING_HUG→SMOTHER→VOMIT_ICHOR…の固定4手ループ。
- ギミック: **1回の技で使用不可カード「Slimed」を10枚も捨て札へ直接注入する**強力なデッキ汚染技（本家Slay the Spireのスライム系デッキ圧迫ギミックに相当）。
- 幕: 不明（HP規模からボス〜大型エリート級）

### SlitheringStrangler

- HP: 53(基本)/54(アセンション) 〜 55(基本)/56(アセンション)
- Move:
  - CONSTRICT: デバフ。ConstrictPower+3（「締め付け」系、継続ダメージまたは行動制限デバフと推測）
  - TWACK (Thwack): 単発攻撃（ダメージ7基本/8アセンション）+防御（ブロック+5固定）
  - LASH: 単発攻撃。ダメージ12(基本)/13(アセンション)
- 行動パターン: CONSTRICT（初手固定）→確率分岐（TWACK/LASHをそれぞれ無限に連続可能=`CanRepeatForever`）→CONSTRICTに戻る、を繰り返す。TWACK・LASHは同じ技が連続することもある。
- ギミック: 特になし。
- 幕: 不明

### SludgeSpinner

- HP: 37(基本)/41(アセンション) 〜 39(基本)/42(アセンション)
- Move:
  - OIL_SPRAY_MOVE: 単発攻撃（ダメージ8基本/9アセンション）+デバフ（WeakPower+1）
  - SLAM_MOVE: 単発攻撃。ダメージ11(基本)/12(アセンション)
  - RAGE_MOVE: 単発攻撃（ダメージ6基本/7アセンション）+バフ（StrengthPower+3）
- 行動パターン: 初手OIL_SPRAYの後、3技すべてが確率分岐（RandomBranchState、各`CannotRepeat`＝直前と同じ技は出せない）でランダムに選ばれ続ける。
- ギミック: 特になし。
- 幕: 不明

### SlumberingBeetle

- HP: 86(基本)/89(アセンション) ※Min=Max固定
- Move:
  - SNORE_MOVE: 睡眠（SleepIntent）。実処理なし（完全な無行動ターン）
  - ROLL_OUT_MOVE: 単発攻撃（ダメージ16基本/18アセンション）+バフ（StrengthPower+2固定、毎回蓄積）
- 行動パターン: 条件分岐。**SlumberPower（開始時3スタック付与）を持っている間はSNOREを繰り返す**。SlumberPowerが切れると自動的にROLL_OUTへ切り替わり、以降ROLL_OUTを無限ループ（毎周Strength+2が蓄積し続ける暴走状態）。
- ギミック: 戦闘開始時にPlatingPower（15基本/18アセンション、シールド）とSlumberPower3を付与。**プレイヤーが3ターン攻撃せず待つと自然に目覚める型**（StS1のLagavulinと同型構造）。`WakeUpMove`で覚醒時にPlatingPowerを剥奪する処理があり＝「起こす前に削ればシールドを無視して攻撃が通る」という読み合いギミック。覚醒後はStrengthを蓄積し続けるため長期戦ほど危険。
- 幕: 不明（Lagavulin型の別個体。LagavulinMatriarchと同系統の役割と推測）

### SnappingJaxfruit

- HP: 31(基本)/34(アセンション) 〜 33(基本)/36(アセンション)
- Move:
  - ENERGY_ORB_MOVE: 単発攻撃（ダメージ3基本/4アセンション）+バフ（StrengthPower+2固定）
- 行動パターン: 単一行動のみを無限ループ。プレイヤーをロックオンする専用ビジュアル演出（`NSnappingJaxfruitVfx`）あり。
- ギミック: 「充電」状態(`IsCharged`)の間は被弾時アニメーションが変化するのみで、機構的な変化はない。据え置き型の固定砲台的モンスター（植物）。
- 幕: 不明

### SneakyGremlin

- HP: 10(基本)/11(アセンション) 〜 14(基本)/15(アセンション) ※非常に低HPの雑魚
- Move:
  - SPAWNED_MOVE: 特殊（StunIntent）。登場時の起床アニメのみで実質スキップターン
  - TACKLE_MOVE: 単発攻撃。ダメージ9(基本)/10(アセンション)
- 行動パターン: SPAWNED（登場時1回のみ）→TACKLE（以降無限ループ）。
- ギミック: 特になし。低HPと"Sneaky"（こっそり）の名から、他モンスターの召喚技で戦闘途中に呼び出される増援タイプと推測。
- 幕: 不明

### SoulFysh

- HP: 211(基本)/221(アセンション) ※Min=Max固定。ボス〜エリート級
- Move:
  - BECKON_MOVE: 特殊（カード注入）。使用不可/呪い系カード「Beckon」を**2枚**生成し、1枚を山札のランダム位置、1枚を捨て札へ注入
  - DE_GAS_MOVE: 単発攻撃。ダメージ16(基本)/17(アセンション)
  - GAZE_MOVE: 単発攻撃（ダメージ7基本/8アセンション）+特殊。命中後さらに「Beckon」カードを1枚捨て札へ追加注入
  - FADE_MOVE: バフ。IntangiblePower+2（2ターンの実質無敵化）を自身へ付与し、透明化状態(`IsInvisible`)になる
  - SCREAM_MOVE: 単発攻撃（ダメージ11基本/12アセンション）+デバフ（VulnerablePower+3）
- 行動パターン: BECKON→DE_GAS→GAZE→FADE→SCREAM→BECKON…の固定5手ループ。
- ギミック: **周回ごとに最大3枚の呪いカード「Beckon」をプレイヤーデッキへ継続的に送り込む**（BECKONで2枚+GAZEで1枚）デッキ汚染ギミック。さらに**FADE_MOVEで2ターンの実質無敵（Intangible）状態になる**ため、無敵化ターンを避けて攻撃を集中する必要がある一種のタイミングパズル構造。
- 幕: 不明（HP規模からエリート〜ミニボス級）

---

## Part C: SoulNexus 〜 ThievingHopper

### SoulNexus (SoulNexus.cs)
- HP: Min 254(Asc)/234(base)、Max=Min(固定)
- Move:
  - SOUL_BURN_MOVE: 攻撃・単発。SoulBurnDamage 31(Asc)/29(base)
  - MAELSTROM_MOVE: 攻撃・多段(ヒット数=MaelstromRepeat、常に4)。1発あたりMaelstromDamage 7(Asc)/6(base)
  - DRAIN_LIFE_MOVE: 攻撃・単発(強デバフ扱い=DebuffIntent strong:true)。DrainLifeDamage 19(Asc)/18(base)。命中後、対象全体にVulnerable2+Weak2を付与
- 行動パターン: 初期状態はSOUL_BURN固定。3技とも共通のRandomBranchStateへ合流し、以降はSOUL_BURN/MAELSTROM/DRAIN_LIFEを均等重み(各weight=1、CannotRepeat=直前技と同じものは選ばれない)でランダム分岐。実質「初手固定→以後3択ランダム(直前と同技は禁止)」
- ギミック: AfterAddedToRoomでCreature.Diedにフック登録し、死亡時にスプライン差分アニメを"tracks/empty"へ切替(死体演出用、ステータス効果ではない)。ShouldFadeAfterDeath=false=死後フェードアウトしない特殊演出。ゲームプレイ上の特殊メカニクスはなし
- 幕: 不明(HP234〜254は高水準。終盤級と推測)

### SpectralKnight (SpectralKnight.cs)
- HP: 97(Asc)/93(base)、固定
- Move:
  - HEX: デバフ。対象全体にHexPower 2付与
  - SOUL_SLASH: 攻撃・単発。SoulSlashDamage 17(Asc)/15(base)
  - SOUL_FLAME: 攻撃・3ヒット固定。SoulFlameDamage 4(Asc)/3(base)×3
- 行動パターン: 初手HEX固定→SOUL_SLASH固定→以降RandomBranchStateでSOUL_SLASH(重み2)とSOUL_FLAME(CannotRepeat)の分岐。「HEX→SOUL_SLASH→(SOUL_SLASH寄りのランダム。SOUL_FLAMEの連続は不可)」
- ギミック: 特になし。HEXで付与するHexPowerの中身は本ファイルからは不明
- 幕: 不明(HP93前後の中堅)

### SpinyToad (SpinyToad.cs)
- HP: Min 121(Asc)/116(base)、Max 124(Asc)/119(base)
- Move:
  - PROTRUDING_SPIKES_MOVE: バフ。自身にThornsPower +5付与、"Spiked"アニメへ切替(IsSpiny=true)
  - SPIKE_EXPLOSION_MOVE: 攻撃・単発。ExplosionDamage 25(Asc)/23(base)。命中後IsSpiny=falseにしてThornsPower -5(自ら付与した棘を打ち消し)
  - TONGUE_LASH_MOVE: 攻撃・単発。LashDamage 19(Asc)/17(base)
- 行動パターン: 固定3拍子ローテ「PROTRUDING_SPIKES→SPIKE_EXPLOSION→TONGUE_LASH→(最初へ)」の無限ループ
- ギミック: 棘を出す(Thorns付与)→自爆技で棘を消費(Thorns除去)→通常攻撃、という「棘の着脱」ギミック。トゲあり/なしで見た目・被弾/死亡SEが変わる(idle_naked系)
- 幕: 不明

### Stabbot (Stabbot.cs)
- HP: Min 19(Asc)/18(base)、Max 24(Asc)/23(base)
- Move: STAB_MOVE: 攻撃・単発。StabDamage 12(Asc)/11(base) + 対象全体にFrailPower 1付与
- 行動パターン: 唯一の技を自己ループ(FollowUpState=自身)する単純固定連打
- ギミック: AfterAddedToRoomで(非テストモード時)FabricatorNormal.SetBotFallPositionにより「落下して着地する」演出位置を設定。ゲームプレイ上のギミックはなし。量産型ロボ(Fabricatorが生成する雑魚)と推測
- 幕: 不明(低HPからAct1級と推測)

### TenHpMonster (TenHpMonster.cs)
- 実データなし。基底/デバッグ用クラス。HP固定10。TitleはBigDummy("BIG_DUMMY.name")のロカライズキーを流用=訓練用ダミー人形。唯一の技NOTHINGは何もせず自己ループ(HiddenIntent=意図非表示の技)

### TerrorEel (TerrorEel.cs)
- HP: 150(Asc)/140(base)、固定
- Move:
  - CRASH_MOVE: 攻撃・単発。CrashDamage 18(Asc)/16(base)
  - ThrashMove: 攻撃・3ヒット固定(ThrashRepeat=3)。ThrashDamage 4(Asc)/3(base)/hit + 自身にVigorPower 6付与(次攻撃力上昇バフ)
  - STUN_MOVE: 特殊(StunIntent)。処理は空(実ダメージなし、スタン演出のみ)
  - TERROR_MOVE: デバフ。対象全体にVulnerable 99付与(実質デスブロー級の激烈デバフ)
- 行動パターン: 初手CRASH→ThrashMove→CRASH→ThrashMove…の2拍子往復が基本ルート。別途STUN_MOVE→TERROR_MOVE→CRASHへ合流する経路もあるが、遷移条件そのものはこのファイル内になく、開幕付与のShriekPowerが外部で発動制御していると推測される
- ギミック: AfterAddedToRoomで自身にShriekPower 75(Asc)/70(base)を戦闘開始時から付与(スタン/恐怖の閾値パワーと推測)。TERROR_MOVEのVulnerable99は事実上の即死コンボ用デバフ
- 幕: 不明(HP140〜150、特殊ギミックからエリート級と推測)

### TestSubject (TestSubject.cs)
- HP: 第1形態111(Asc)/100(base)、第2形態212(Asc)/200(base)、第3形態313(Asc)/300(base)。MinInitialHp=MaxInitialHp=第1形態HP
- Move:
  - RESPAWN_MOVE(DeadState): 回復+バフ。倒された直後に発動が強制される特殊技(MustPerformOnceBeforeTransitioning)。Respawns回数で分岐: 1回目→HPを人数倍したSecondFormHpに再設定して全回復+PainfulStabsPower付与。2回目→ThirdFormHpに再設定して全回復+NemesisPower付与、AdaptablePower/PainfulStabsPowerを除去(=もう蘇らない)
  - BITE_MOVE: 攻撃・単発。BiteDamage 22(Asc)/20(base)
  - SKULL_BASH_MOVE: 攻撃・単発。SkullBashDamage 16(Asc)/14(base) + 対象にVulnerable1付与
  - MULTI_CLAW_MOVE: 攻撃・多段。ヒット数=BaseMultiClawCount(3)+ExtraMultiClawCount(このMoveを使うたび+1され戦闘中ずっと増加し続ける)。MultiClawDamage 11(Asc)/10(base)/hit。自己ループ技
  - PHASE3_LACERATE_MOVE: 攻撃・3ヒット固定。Phase3LacerateDamage 11(Asc)/10(base)
  - BIG_POUNCE_MOVE: 攻撃・単発。固定45ダメージ(アセンション非依存)
  - BURNING_GROWL_MOVE: 状態異常付与+バフ。対象全員の捨て札に使用不可カード「Burn」をBurningGrowlBurnCount枚(5(Asc)/3(base))注入 + 自身にStrength 3(Asc)/2(base)付与
- 行動パターン: 通常時はBITE⇔SKULL_BASHの2拍子往復。HP0になるたびRESPAWN_MOVEへ強制遷移(死亡即復活)。復活後の分岐: Respawns<2ならMULTI_CLAW_MOVEへ(自己ループで毎ターン肥大化する多段連打)、Respawns>=2ならPHASE3_LACERATE→BIG_POUNCE→BURNING_GROWL→PHASE3_LACERATE…の3拍子ループへ移行。「2回殺されるたびに第2形態→第3形態へ変身し行動パターンごと変わる」多段変身モンスター
- ギミック: AfterAddedToRoomでAdaptablePower1とEnragePower 2(base)/3(Asc)を開幕付与。AdaptablePowerが復活(DoRevive)の鍵。3体目撃破でAdaptablePower除去=真の撃破、ShouldDisappearFromDoom=Respawns>=2で死体消滅処理。BurningGrowlは使用不可札Burnを山札ではなく捨て札に直接注入する珍しい状態異常付与。SaveManager経由でランをまたいだ累計撃破数をTitleに表示(繰り返し遭遇する実験体という設定)
- 幕: 不明(名前・複雑な変身構造からボス/隠しボス級)

### TheAdversaryMkOne (TheAdversaryMkOne.cs)
- HP: 100固定(アセンション影響なし)
- Move:
  - SMASH_MOVE: 攻撃・単発、固定12ダメージ
  - BEAM_MOVE: 攻撃・単発、固定15ダメージ
  - BARRAGE_MOVE: 攻撃・2ヒット固定(BarrageDamage 8×2) + 自身にStrength2付与
- 行動パターン: 固定3拍子ローテ「SMASH→BEAM→BARRAGE→(最初へ)」
- ギミック: AfterAddedToRoomで自身にArtifactPower 0付与(実質無効な付与。MkTwo/MkThreeとの比較用ベースラインと推測)
- 幕: 不明。MkOne/MkTwo/MkThreeは同系統ボスの3段階強化版シリーズ(数値がラウンドナンバーでアセンション無効=デバッグ/イベント専用ボスの可能性が高い)

### TheAdversaryMkTwo (TheAdversaryMkTwo.cs)
- HP: 200固定
- Move:
  - BASH_MOVE: 攻撃・単発、固定13ダメージ
  - FLAME_BEAM_MOVE: 攻撃・単発、固定16ダメージ(FlameBeamStatusCount=1というプロパティがあるが実処理内で未使用=デッドコードの可能性)
  - BARRAGE_MOVE: 攻撃・2ヒット固定(9×2) + 自身にStrength3付与
- 行動パターン: 固定3拍子ローテ「BASH→FLAME_BEAM→BARRAGE→(最初へ)」
- ギミック: AfterAddedToRoomで自身にArtifactPower1付与(MkOneの0から+1)
- 幕: 不明。MkOne→Two→Threeでダメージ・Strengthバフ量・Artifact量すべてが段階的に増加する設計

### TheAdversaryMkThree (TheAdversaryMkThree.cs)
- HP: 300固定(base/Asc値とも300で実質固定)
- Move:
  - CRASH_MOVE: 攻撃・単発、固定15ダメージ
  - FLAME_BEAM_MOVE: 攻撃・単発、固定18ダメージ
  - BARRAGE_MOVE: 攻撃・2ヒット固定(10×2) + 自身にStrength4付与
- 行動パターン: 固定3拍子ローテ「CRASH→FLAME_BEAM→BARRAGE→(最初へ)」
- ギミック: AfterAddedToRoomで自身にArtifactPower2付与(シリーズ最大)
- 幕: 不明

### TheForgotten (TheForgotten.cs)
- HP: 111(Asc)/106(base)、固定
- Move:
  - MIASMA: デバフ+防御+バフの複合技。対象全体のDexterity-2(DebilitatingSmogDexStealAmount)、自身はブロック8獲得、さらに自身のDexterity+2(相手から吸収する構造)
  - DREAD: 攻撃・単発。ダメージ=基礎15(Asc)/13(base)+自身の現在Dexterityパワー量を加算(器用さが高いほど攻撃力が伸びるスケーリング技)
- 行動パターン: 固定2拍子往復「MIASMA→DREAD→MIASMA→DREAD…」。毎周期Dexterityを相手から奪って自分に足し、その強化されたDexterityでDREADの威力が雪だるま式に上がる
- ギミック: AfterAddedToRoomで基底のAfterAddedToRoomを呼ばずに自身へPossessSpeedPower1のみ付与(名前から「憑依/速度」系の特殊パワーと推測、詳細不明)。DREADのDexterityスケーリングが最大の特徴
- 幕: 不明(特殊メカニクスからエリート/準ボス級)

### TheInsatiable (TheInsatiable.cs)
- HP: 341(Asc)/321(base)、固定
- Move:
  - LIQUIFY_GROUND_MOVE: バフ+状態異常付与。対象全員にSandpitPower4付与(継続デバフ系パワーと推測)、加えて対象の山札上3枚+捨て札3枚(計6枚)に使用不可カード「FranticEscape」を注入
  - THRASH_MOVE_1/2: 攻撃・2ヒット固定(ThrashDamage 9(Asc)/8(base)×2)。同一処理を異なる状態名で2つ用意しローテ内の別ポジションに配置
  - LUNGING_BITE_MOVE: 攻撃・単発。BiteDamage 31(Asc)/28(base)
  - SALIVATE_MOVE: バフ。自身にStrength 3(Asc)/2(base)付与
- 行動パターン: 初手LIQUIFY_GROUND固定→以降「THRASH_1→LUNGING_BITE→SALIVATE→THRASH_2→THRASH_1→…」の4手固定ループ
- ギミック: 初手で対象を沼化(SandpitPower)しつつ、特殊カード「FranticEscape」を山札・捨て札両方に混入させる珍しい状態異常付与。HasLiquifiedフラグで被弾演出が液状化前後で変化。AfterDeathで戦闘BGMパラメータ変更のみ(演出)
- 幕: 不明(HP321〜341と非常に高く、名前・液状化ギミックからAct終盤の大型ボスと推測)

### TheLost (TheLost.cs)
- HP: 99(Asc)/93(base)、固定
- Move:
  - DEBILITATING_SMOG: デバフ+バフ。対象全体のStrength-2(DebilitatingSmogStrengthStealAmount)、自身のStrength+2(強さ吸収)
  - EYE_LASERS: 攻撃・2ヒット固定。EyeLasersDamage 5(Asc)/4(base)×2
- 行動パターン: 固定2拍子往復「DEBILITATING_SMOG→EYE_LASERS→…」。TheForgottenのStrength版に相当する対称構造(EYE_LASERSはスケーリングしない素の多段技)
- ギミック: AfterAddedToRoomで基底を呼ばず自身にPossessStrengthPower1のみ付与。TheForgotten(Possess+Speed)とTheLost(Possess+Strength)は同系統「憑依」シリーズの対と推測される
- 幕: 不明

### TheObscura (TheObscura.cs)
- HP: 129(Asc)/123(base)、固定
- Move:
  - ILLUSION_MOVE: 召喚(SummonIntent)。Parafright("illusion")を1体召喚
  - PIERCING_GAZE_MOVE: 攻撃・単発。PiercingGazeDamage 11(Asc)/10(base)
  - SAIL_MOVE(内部メソッド名WailMove): バフ。自陣営全体(自身含む味方全員)にStrength3付与する応援技
  - HARDENING_STRIKE_MOVE: 攻撃・単発+防御。HardeningStrikeDamage 7(Asc)/6(base)の攻撃後、自身がHardeningStrikeBlock 7(Asc)/6(base)のブロックを獲得する攻防一体技
- 行動パターン: 初手ILLUSION_MOVE(召喚)固定→以降RandomBranchStateでPIERCING_GAZE/SAIL/HARDENING_STRIKEを均等ランダム分岐(いずれもCannotRepeat=直前技と同じものは選ばれない)
- ギミック: 開幕で分身(Parafright)を1体召喚するのが最大の特徴。SAIL_MOVEは編成戦で味方全体を強化する応援技を兼ねる。HasSummonedフラグで被弾/死亡演出が召喚前後で分岐
- 幕: 不明(召喚持ちの中〜上位エリート級と推測)

### ThievingHopper (ThievingHopper.cs)
- HP: 84(Asc)/79(base)、固定
- Move:
  - THIEVERY_MOVE: 攻撃+カードデバフ(CardDebuffIntent)。対象の山札/捨て札からカードを1枚「盗む」——優先度は①エンチャント無しのUncommon②同Common/Rare/Event③Basic/Quest④Ancientまたはエンチャント持ち、の順で候補を絞り込みランダム選出。盗んだカードはSwipePowerとして自身に1スタック蓄積。最後にTheftDamage 19(Asc)/17(base)の攻撃(演出なし)
  - NAB_MOVE: 攻撃・単発。NabDamage 16(Asc)/14(base)
  - HAT_TRICK_MOVE: 攻撃・単発。HatTrickDamage 23(Asc)/21(base)
  - FLUTTER_MOVE: バフ。自身にFlutterPower5付与(ホバー状態へ移行、見た目・SEが変化=回避系パワーと推測)
  - ESCAPE_MOVE: 逃走(EscapeIntent)。戦闘から離脱し、以降は自身へ自己ループ(事実上の退場)
- 行動パターン: 一直線の固定シーケンス「THIEVERY(盗む)→FLUTTER(浮上)→HAT_TRICK→NAB→ESCAPE(離脱)」。ループせず、ESCAPE後は自己ループのみ=一度きりの強盗イベント的パターン
- ギミック: AfterAddedToRoomで自身にEscapeArtistPower5を開幕付与(確実に逃げ切れる系のパワーと推測)。カード窃盗にレアリティ優先度の厳密な選定ロジックを持つ(単純ランダムではない)点が特徴的
- 幕: 不明(盗賊系の中堅モンスター)

---

## Part D: Toadpole 〜 Zapbot


### Toadpole (Toadpole.cs)
- HP: Min 21(基本)/22(Asc) / Max 25(基本)/26(Asc)
- IsFront（前列/後列）フラグで初期分岐する個体
- Move一覧:
  - SPIKE_SPIT_MOVE: 多段攻撃。1ヒット3(基本)/4(Asc)、3ヒット固定。発動前に自分のとげ(ThornsPower)を-2する（消費）
  - WHIRL_MOVE: 単発攻撃。7(基本)/8(Asc)
  - SPIKEN_MOVE: バフ。自分にThornsPower+2付与（Asc差なし）
- 行動パターン: 初期分岐（IsFront=false→WHIRL開始／IsFront=true→SPIKEN開始）。以降 WHIRL→SPIKEN→SPIKE_SPIT→WHIRL… の3すくみ固定ループ
- ギミック: とげを「SPIKEN(+2)で貯めてSPIKE_SPIT(-2)で消費して多段攻撃」というとげ貯蓄サイクル。ThornsPower有無でアニメ・被弾セリフが分岐
- 幕: 不明

### TorchHeadAmalgam (TorchHeadAmalgam.cs)
- HP: 固定（Min=Max）。基本199/Asc211
- AfterAddedToRoom: 自分にMinionPower+1を付与（雑魚判定・処刑技等の対象外扱いの可能性）
- Move一覧:
  - TACKLE_1/2_MOVE: 単発攻撃。18(基本)/19(Asc)
  - BEAM_MOVE: 多段攻撃3ヒット固定。1ヒット8(基本/Asc共通、差なし)
  - TACKLE_3/4_MOVE (弱タックル): 単発攻撃。14(基本)/15(Asc)
- 行動パターン: 固定ローテ TACKLE_1→TACKLE_2→BEAM→TACKLE_3(弱)→TACKLE_4(弱)→BEAM→TACKLE_3→TACKLE_4→BEAM…（初手2連だけ通常タックルで、以降はBEAMと弱タックル2連の3段ループ）
- ギミック: 死亡時に体表の炬火の光エフェクトを非表示にする専用演出(OnDieToDoom)のみ。特殊な死亡・分裂等のロジックはなし
- 幕: 不明

### ToughEgg (ToughEgg.cs)
- HP（卵状態）: Min 14(基本)/15(Asc) / Max 18(基本)/19(Asc)
- HP（孵化後Hatchling、孵化時にランロールで再設定）: Min 19(基本)/20(Asc) / Max 22(基本)/23(Asc)（幕インデックス・マルチプレイ人数でさらにスケール）
- 表示名: 孵化前は本来の名前、孵化後は「HATCHLING」名義に変わる
- AfterAddedToRoom: 未孵化ならHatchPower（開始値=味方側スロットなら2、敵側スロットなら1）を付与しカウントダウン開始。既に孵化済み扱いの個体は即Hatch()して行動ステートを強制上書き
- Move一覧:
  - HATCH_MOVE: 特殊/召喚扱い(SummonIntent)。孵化処理本体——MinionPower以外の全パワーとHatchPowerを除去し、HatchlingMin〜MaxHpの範囲でHPを再ロール・再設定
  - NIBBLE_MOVE: 単発攻撃。4(基本)/5(Asc)。孵化後にループ使用
- 行動パターン: 初手HATCH_MOVE（実際にはHatchPowerのカウントダウンが0になったタイミングで孵化）。孵化後はNIBBLE_MOVEを自己ループ
- ギミック: 「卵→孵化」で別モンスター名義に変身する成長ギミック（本家Slime系の逆で、育つと本体が出てくる）。孵化時に蓄積パワーをリセットしHPを幕・人数でスケールし直す
- 幕: 不明

### TrackerRubyRaider (TrackerRubyRaider.cs)
- HP: Min 21(基本)/22(Asc) / Max 25(基本)/26(Asc)
- Move一覧:
  - TRACK_MOVE: デバフ。対象にFrailPower+2付与（アニメのみ、ダメージなし）
  - HOUNDS_MOVE: 多段攻撃。1ヒット1(基本/Asc共通)、ヒット数8(基本)/9(Asc)
- 行動パターン: 初手TRACK_MOVE固定（1回のみ）→以降HOUNDS_MOVEを無限ループ
- ギミック: 特になし。「RubyRaider」シリーズ（Axe/Assassin/Brute/Crossbow等）の一員と見られる低火力多段デバフ役
- 幕: 不明（RubyRaiderシリーズは編成専用の盗賊系と推測）

### Tunneler (Tunneler.cs)
- HP: 固定（Min=Max）。基本87/Asc92
- Move一覧:
  - BITE_MOVE: 単発攻撃。13(基本)/15(Asc)
  - BURROW_MOVE: バフ+防御。自分にBurrowedPower+1付与、ブロック32(基本)/37(Asc)獲得
  - BELOW_MOVE_1: 単発攻撃。23(基本)/26(Asc)（地中からの不意打ち演出付き）
  - DIZZY_MOVE (StillDizzyMove): 特殊（StunIntent、実処理は何もしない待機技）
- 行動パターン: 固定ローテ BITE→BURROW→BELOW→BELOW(自己ループ、以降BELOWを連打し続ける)。DIZZY_MOVEはBITEへ繋がる（スタンから復帰する専用の合流ルート）
- ギミック: 「潜る(Burrow)→地中から強打(Below)」という隠れ強化型。Burrowed中は専用アニメ・被弾音・死亡音に切り替わる
- 幕: 不明

### TurretOperator (TurretOperator.cs)
- HP: 41(基本) / 51(ASC)。Min=Maxの固定値。
- Move一覧:
  - UNLOAD_MOVE（1・2の2インスタンス）: 多段攻撃×5ヒット固定。1ヒット3(基本)/4(ASC)。
  - RELOAD_MOVE: バフ。自身にStrengthPower+1（クランクを回すアニメ演出付き）。
- 行動パターン: 固定3手ループ。UNLOAD_MOVE_1→UNLOAD_MOVE_2→RELOAD_MOVE→UNLOAD_MOVE_1…（同じ多段攻撃を2回連続で撃ってから1回強化する、を繰り返す）。
- ギミック: 特になし（AfterAddedToRoom未オーバーライド、HP半分分岐・死亡時効果なし）。強化を挟みつつ多段攻撃を連発するのでStrengthが乗るほど後半の多段火力が線形に伸びる構造。
- 幕: 不明。

### TwigSlimeM (TwigSlimeM.cs)
- HP: Min 26(基本)/27(Asc) / Max 28(基本)/29(Asc)
- Move一覧:
  - CLUMP_SHOT_MOVE: 単発攻撃。11(基本)/12(Asc)
  - STICKY_SHOT_MOVE: 特殊(StatusIntent量1)。使用不可カード「Slimed」1枚を手札でなく**捨て札山**へ直接注入
- 行動パターン: 確率分岐(RandomBranchState)。初期状態はSTICKY_SHOT。CLUMP_SHOTは重み2、STICKY_SHOTは重み1相当で直前と同じ技の連続は不可(CannotRepeat)。両技とも解決後は同じ抽選に戻る（約2:1でCLUMP_SHOTが出やすいが連続はしない）
- ギミック: 使用不可札「Slimed」を捨て札に直接注入し山札を汚染する軽量妨害。本家Medium Slime相当の中量級
- 幕: 不明

### TwigSlimeS (TwigSlimeS.cs)
- HP: Min 7(基本)/8(Asc) / Max 11(基本)/12(Asc)
- Move一覧:
  - BUTT_MOVE: 単発攻撃。4(基本)/5(Asc)
- 行動パターン: BUTT_MOVEのみを永久自己ループ
- ギミック: なし。最小構成の最弱スライム個体（本家Small Slime相当）
- 幕: 不明

### TwoTailedRat (TwoTailedRat.cs)
- HP: Min 17(基本)/18(Asc) / Max 21(基本)/22(Asc)
- Move一覧:
  - SCRATCH_MOVE: 単発攻撃。8(基本)/9(Asc)
  - DISEASE_BITE_MOVE: 単発攻撃。6(基本)/7(Asc)
  - SCREECH_MOVE: デバフ。対象にFrailPower+1付与
  - CALL_FOR_BACKUP_MOVE: 召喚。空きスロットに新しいTwoTailedRatを1体追加召喚し、全仲間のCallForBackupCountを最大値+1へ同期
- 行動パターン: 確率分岐(RandomBranchState、いずれも直前技の連続不可)。CanSummon()がfalseの間はSCRATCH/DISEASE_BITEが各1/12、SCREECHが3/12（重み3）。CanSummon()がtrueになるとSCRATCH/DISEASE_BITEは各1/12のまま、CALL_FOR_BACKUPが75%の圧倒的重みで選ばれる（CALL_FOR_BACKUPは個体ごとに一度きり=UseOnlyOnce）。編成側からStarterMoveIndexが指定されていれば初手をSCRATCH/DISEASE_BITE/SCREECHの3パターンに固定し複数体の初動をずらせる
- ギミック: CanSummon()は「召喚後2ターン経過」「自身の召喚済み回数が3回未満」「空きスロットあり」「同ターンに他個体が召喚予約していない」の4条件を満たすとtrueになる**増殖ギミック**。満たすと75%の高確率で自分と同じ個体を追加召喚する。見た目のバリエーション（バーナクル・頭部）をランダムスキンで割当
- 幕: 不明

### Vantom (Vantom.cs)
- HP: 固定（Min=Max）。基本173/Asc183
- AfterAddedToRoom: 自分にSlipperyPower+9を付与
- Move一覧:
  - INK_BLOT_MOVE: 単発攻撃。7(基本)/8(Asc)
  - INKY_LANCE_MOVE: 多段攻撃2ヒット固定。1ヒット6(基本)/7(Asc)
  - DISMEMBER_MOVE: 単発攻撃27(基本)/30(Asc)＋使用不可札Wound(負傷)を捨て札山へ3枚注入
  - PREPARE_MOVE: バフ。自分にStrengthPower+2付与
- 行動パターン: 固定ローテ INK_BLOT→INKY_LANCE→DISMEMBER→PREPARE→INK_BLOT…の4段ループ
- ギミック: 専用BGMパラメータ「vantom_progress」を各技解決後に1→2→3→5(死亡時)と段階的に上げていく演出付きの強敵。ShouldDisappearFromDoom=falseで処刑技(Doom系)無効の特殊耐性を持つ。DISMEMBER技でWoundを3枚も注入する強力な妨害。HP173〜183・専用進行演出からエリート/ミニボス級と推測
- 幕: 不明

### VineShambler (VineShambler.cs)
- HP: 固定（Min=Max）。基本61/Asc64
- Move一覧:
  - GRASPING_VINES_MOVE: 単発攻撃8(基本)/9(Asc)＋対象にTangledPower+1付与（カード関連デバフ、CardDebuffIntent）
  - SWIPE_MOVE: 多段攻撃2ヒット固定。1ヒット6(基本)/7(Asc)
  - CHOMP_MOVE: 単発攻撃16(基本)/18(Asc)
- 行動パターン: 初期SWIPE_MOVE開始→GRASPING_VINES_MOVE→CHOMP_MOVE→SWIPE_MOVE…の3段固定ループ
- ギミック: TangledPowerという専有のカード関連デバフを付与する唯一の技を持つ（詳細な効果は本ファイルからは不明）
- 幕: 不明

### WaterfallGiant (WaterfallGiant.cs)
- HP: 固定（Min=Max）。基本240/Asc250（爆発準備状態では一時的に999999999へ差し替え＝実質無敵化）
- AfterAddedToRoom: 現在の水圧銃ダメージを初期値(基本20/Asc23)にリセット、環境音ループ再生開始
- Move一覧:
  - PRESSURIZE_MOVE: バフ。自分にSteamEruptionPower+15(基本)/+20(Asc)付与
  - STOMP_MOVE: 単発攻撃15(基本)/16(Asc)＋対象にWeakPower+1付与＋自分にSteamEruptionPower+3付与
  - RAM_MOVE: 単発攻撃10(基本)/11(Asc)＋自分にSteamEruptionPower+3付与
  - SIPHON_MOVE: 回復。自HPを15×プレイヤー人数ぶん回復＋自分にSteamEruptionPower+3付与
  - PRESSURE_GUN_MOVE: 単発攻撃。使用中の蓄積ダメージ値を参照（初期20(基本)/23(Asc)、使うたび+5ずつ増加）＋自分にSteamEruptionPower+3付与
  - PRESSURE_UP_MOVE: 単発攻撃13(基本)/14(Asc)＋自分にSteamEruptionPower+3付与
  - ABOUT_TO_BLOW_MOVE: 特殊（StunIntent。MustPerformOnceBeforeTransitioning=true＝必ず1度は実行される）。蓄積したSteamEruptionPower量を記録して剥がし、爆発準備インデックスを最大値(6)に固定
  - EXPLODE_MOVE: 自爆技（DeathBlowIntent）。記録した蒸気量ぶんのダメージを与えたのち自分自身を強制的に撃破する
- 行動パターン: 固定ローテ PRESSURIZE(初手のみ)→STOMP→RAM→SIPHON→PRESSURE_GUN→PRESSURE_UP→STOMP…（STOMPに戻ってループ）。外部トリガーTriggerAboutToBlowState()が呼ばれると強制的にABOUT_TO_BLOW_MOVEへ割り込み遷移し、HPを999999999にセットして無敵化した上でEXPLODE_MOVE（自己ループ）へ移行し自滅する
- ギミック: **蒸気圧(SteamEruptionPower)を毎ターン積み増し、外部条件成立で「about to blow」状態へ切り替わり蓄積量ぶん大爆発して自滅する時限爆弾ボス**。PRESSURE_GUNは使うたびダメージが恒久increaseする武器強化技。爆発準備中はHPを実質無限化して倒せなくする(ShowsInfiniteHp)。処刑技無効(ShouldDisappearFromDoomはSteamEruptionPower保持中false)。死亡後フェード演出もPressureBuildupIdx==0の時のみという細かい条件分岐あり
- 幕: 不明（HP240/250のボス級数値・専用時限ギミックからエリア固有ボスと推測）

### Wriggler (Wriggler.cs)
- HP: Min 17(基本)/18(Asc) / Max 21(基本)/22(Asc)
- Move一覧:
  - NASTY_BITE_MOVE: 単発攻撃。6(基本)/7(Asc)
  - WRIGGLE_MOVE: バフ+特殊。自分にStrengthPower+2付与＋使用不可札Infectionを1枚捨て札山へ注入
  - SPAWNED_MOVE: 特殊（StunIntent、実処理なし）。後から湧いた個体の待機技
- 行動パターン: スロット名で初期分岐（"wriggler1"/"wriggler3"→NASTY_BITE開始、"wriggler2"/"wriggler4"→WRIGGLE開始）。以降NASTY_BITE⇄WRIGGLEの2択を交互ループ。StartStunnedフラグが立つ個体はSPAWNED_MOVE(何もしない)から入り、その後スロット分岐へ合流
- ギミック: 4体同時湧き編成(wriggler1〜4)を前提としたスロット名分岐で初動をずらす設計。後から出現した個体は1ターン様子見してから合流。Infectionという使用不可札を注入する妨害技を持つ
- 幕: 不明

### Zapbot (Zapbot.cs)
- HP: Min 18(基本)/19(Asc) / Max 23(基本)/24(Asc)
- AfterAddedToRoom: 落下着地位置の演出調整＋自分にHighVoltagePower+2を付与
- Move一覧:
  - ZAP: 単発攻撃。14(基本)/15(Asc)
- 行動パターン: ZAPのみを永久自己ループする最小構成
- ギミック: 開幕からHighVoltagePower（詳細不明の固有パワー、感電関連と推測）を持つ。落下演出（FabricatorNormal.SetBotFallPosition）から「Fabricator系ボスが投下する量産ロボット」の位置づけと見られる
- 幕: 不明
