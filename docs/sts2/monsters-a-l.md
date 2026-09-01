# STS2 モンスターデータ抽出 (A〜L, MegaCrit.Sts2.Core.Models.Monsters)

ゲームデザイン研究目的のローカル解析。コードは転載せず、数値・行動パターンのみを日本語でまとめている。
クラス名・Move名・パワー名は英語表記のまま。

**注記**: HPやダメージ等の数値は「基本値 / アセンション上昇値」の順で記載。
`AscensionHelper.GetValueIfAscension(level, ascValue, baseValue)` は
**第3引数=基本値・第2引数=アセンション上昇時の値**（`level` はどのアセンションレベルで切り替わるか、
主に `ToughEnemies`=HP系・`DeadlyEnemies`=攻撃力系の2種）。

対象: Architect.cs 〜 LouseProgenitor.cs（ファイル名A〜L、Mocks/除外、DeprecatedMonster.csスキップ）。
59ソースファイルを解析し、Decimillipedeの3派生クラス（Back/Front/Middle）は共通ロジックのため1セクションに統合。

---

## Architect (Architect.cs)
- HP: 9999/9999(アセンション差なし)
- Move: `NOTHING` のみ。HiddenIntent(意図非表示)で何もしない
- 行動パターン: 自己ループのみ(NOTHING→NOTHING…)
- ギミック: 特になし。攻撃されない/攻撃しない前提の固定HP異常値
- 幕: 不明。戦闘用エンカウンターに出典なし。おそらく背景演出・開発/デバッグ用のダミーオブジェクト

## Assassin Ruby Raider (AssassinRubyRaider.cs)
- HP: 基本18〜23 / アセンション(ToughEnemies)19〜24
- Move: `KILLSHOT_MOVE`(攻撃・単体) ダメージ基本11/アセンション(DeadlyEnemies)12
- 行動パターン: 単一Moveの自己ループ(常にKillshotのみを繰り返す)
- ギミック: 特になし。ダメージ属性はArmor(金属打撃音)
- 幕: Overgrowth(緑幕)。`RubyRaidersNormal` 編成(通常戦。5種のRuby Raiderが混成)

## Axe Ruby Raider (AxeRubyRaider.cs)
- HP: 基本20〜22 / アセンション21〜23
- Move:
  - `SWING_1`/`SWING_2`(攻撃+防御): 攻撃基本5/アセンション6、同時にブロック基本5/アセンション6を得る
  - `BIG_SWING`(攻撃のみ): 基本12/アセンション13
- 行動パターン: 固定ローテーション `SWING_1→SWING_2→BIG_SWING→SWING_1→…` の3ステップ巡回
- ギミック: 特になし。ダメージ属性Armor
- 幕: Overgrowth(緑幕)。`RubyRaidersNormal` 編成

## Axebot (Axebot.cs)
- HP: 基本40〜44 / アセンション(ToughEnemies)42〜46
- Move:
  - `BOOT_UP_MOVE`(防御+バフ、初回のみ想定): ブロック10 + Strength+1
  - `ONE_TWO_MOVE`(攻撃・2ヒット): 1ヒットあたり基本5/アセンション6 × 2回
  - `SHARPEN_MOVE`(バフ): Strength+4
  - `HAMMER_UPPERCUT_MOVE`(攻撃+デバフ): 基本8/アセンション10、命中後に対象へWeak+1・Frail+1
- 行動パターン: 起動時は `_stockOverrideAmount`(残機の上書き指定)の有無で初期状態が変わる。
  - 通常スポーン(上書きなし): いきなりランダム分岐(`RAND_MOVE`)から開始
  - 上書きあり(他モンスターが個体数を制御して再スポーンさせた場合等): `BOOT_UP_MOVE`(ブロック+Strength)から開始
  - `RAND_MOVE` はランダム分岐: ONE_TWO(重み2) / SHARPEN(1回だけ・CannotRepeatで連続不可) / HAMMER_UPPERCUT(重み2) を確率選択し、どのMoveの後も再度この分岐に戻る
- ギミック: 開始時に `StockPower`(既定量2)を付与。`respawnTrigger`定数・`ShouldPlaySpawnAnimation`(復活時演出フラグ)が存在し、撃破後に残機ぶん再出現するモンスターと推測される(Stock=残機に相当する可能性)
- 幕: Glory(最終幕)。`AxebotsNormal` 編成(front/backの2体構成)

## Battle Friend V1 (BattleFriendV1.cs)
- HP: 75/75(固定)
- Move: `NOTHING_MOVE` のみ(何もしない・攻撃してこない)
- 行動パターン: 自己ループのみ
- ギミック: 開始時に `BattlewornDummyTimeLimitPower` を量3で付与(制限時間付きサンドバッグと推測)。見た目スキンは "v1"
- 幕: 幕不問。`BattlewornDummyEventEncounter`(訓練用ダミーのイベント戦)専用

## Battle Friend V2 (BattleFriendV2.cs)
- HP: 150/150(固定)
- Move/行動パターン/ギミック: V1と同一構造(NOTHING_MOVEのみ・BattlewornDummyTimeLimitPower量3)。スキンのみ"v2"
- 幕: 幕不問。BattlewornDummyイベント専用(V1よりHPが高い上位版)

## Battle Friend V3 (BattleFriendV3.cs)
- HP: 300/300(固定)
- Move/行動パターン/ギミック: V1・V2と同一構造。スキン"v3"
- 幕: 幕不問。BattlewornDummyイベント専用(最上位版。HP75→150→300と倍々)

## Big Dummy (BigDummy.cs)
- HP: 9999/9999
- Move: `NOTHING` のみ(HiddenIntent)
- 行動パターン: 自己ループのみ
- ギミック: なし。`VisualsPath` が Defect(過去作キャラ)の見た目を流用しているテスト用と思われる
- 幕: 不明(実戦での出典なし)。命名・9999HP・no-opから、`Mocks/`配下のテスト用エンカウンター(MockTwoMonsterEncounter等)で使われる開発/テスト専用モンスターと推測

## Bowlbug Egg (BowlbugEgg.cs)
- HP: 基本21〜22 / アセンション(ToughEnemies)23〜24
- Move: `BITE_MOVE`(攻撃+防御): 攻撃基本7/アセンション8 + ブロック基本7/アセンション8を同時獲得
- 行動パターン: 単一Moveの自己ループのみ
- ギミック: 見た目は繭(cocoon)スキン。特殊メカニクスなし
- 幕: Hive(蜂の巣幕)。`BowlbugsNormal`/`BowlbugsWeak` 編成(Bowlbug各種の混成群れ)

## Bowlbug Nectar (BowlbugNectar.cs)
- HP: 基本35〜38 / アセンション36〜39
- Move:
  - `THRASH_MOVE`/`THRASH2_MOVE`(攻撃): 固定3ダメージ(アセンション差なし)
  - `BUFF_MOVE`(バフ): Strength基本15/アセンション16
- 行動パターン: `THRASH→BUFF→THRASH2→THRASH2→…`。バフは初回1回のみ発生し、以降はTHRASH2の自己ループに固定(2度目のバフは発生しない)
- ギミック: 見た目"goop"スキン
- 幕: Hive。`BowlbugsNormal`/`Weak` 編成

## Bowlbug Rock (BowlbugRock.cs)
- HP: 基本45〜48 / アセンション46〜49
- Move:
  - `HEADBUTT_MOVE`(攻撃): 基本15/アセンション16
  - `DIZZY_MOVE`(自己スタン明け): 攻撃なし、`IsOffBalance`をfalseに戻す
- 行動パターン: `HEADBUTT_MOVE`実行後、条件分岐(`POST_HEADBUTT`)で `IsOffBalance` がtrueなら `DIZZY_MOVE` へ、falseなら再度 `HEADBUTT_MOVE` へ。`DIZZY_MOVE` の後は必ず `HEADBUTT_MOVE` に戻る。つまり通常は殴り続けるだけだが、バランスを崩した回のみ「頭突き→(自己スタン)→ダイズィー(様子見)→頭突き…」という中断が入る
- ギミック: 開始時に `ImbalancedPower` を量1で付与。`HeadbuttMove` 内で `IsOffBalance==true` の場合、命中後に自身へ `Stun` を掛けて次のMoveを`DizzyMove`に固定する(＝頭突きの反動で目を回す)。`IsOffBalance` を実際にtrueへ切り替える条件は `ImbalancedPower` 側の実装(本ファイル外)に依存するため未確認
- 幕: Hive。`BowlbugsNormal`/`Weak` 編成

## Bowlbug Silk (BowlbugSilk.cs)
- HP: 基本40〜43 / アセンション41〜44
- Move:
  - `TRASH_MOVE`(攻撃・2ヒット): 1ヒットあたり基本4/アセンション5 × 2
  - `TOXIC_SPIT_MOVE`(デバフ): 対象全員にWeak+1
- 行動パターン: 開始状態が `TOXIC_SPIT_MOVE`(!)で、`TOXIC_SPIT→TRASH→TOXIC_SPIT→TRASH→…` の2状態ループ
- ギミック: 特殊メカニクスなし。見た目"web"スキン
- 幕: Hive。`BowlbugsNormal`/`Weak` 編成

## Brute Ruby Raider (BruteRubyRaider.cs)
- HP: 基本30〜33 / アセンション31〜34
- Move:
  - `BEAT_MOVE`(攻撃): 基本7/アセンション8
  - `ROAR_MOVE`(バフ): Strength+3(固定・アセンション差なし)
- 行動パターン: `BEAT→ROAR→BEAT→ROAR→…` の交互ループ
- ギミック: なし。ダメージ属性Armor
- 幕: Overgrowth(緑幕)。`RubyRaidersNormal` 編成

## Bygone Effigy (BygoneEffigy.cs)
- HP: 固定127(アセンション時132。Min=Max)
- Move:
  - `INITIAL_SLEEP_MOVE`(睡眠/せりふのみ): 効果なし、開始台詞を再生して待機
  - `SLEEP_MOVE`(睡眠): 効果なし(このMoveは定義されているが、現行の状態遷移表からは到達経路が見当たらない=未使用/外部トリガー用の可能性)
  - `WAKE_MOVE`(バフ・覚醒): Strength+10、覚醒時BGM演出
  - `SLASHES_MOVE`(攻撃): 基本13/アセンション15
- 行動パターン: `INITIAL_SLEEP→WAKE→SLASHES→SLASHES→…`(覚醒後は永久に斬撃を繰り返す固定ムーブ)
- ギミック: 開始時に `SlowPower` を量1で付与(睡眠中は行動が遅い/1回休むフレーバーと推測)。石像(彫像)がまず眠っており、1ターン睡眠後に目覚めてStrength+10を得てから永久に攻撃し続けるという「起こしてはいけないエリート」構造
- 幕: Overgrowth(緑幕)エリート。`BygoneEffigyElite`

## Byrdonis (Byrdonis.cs)
- HP: 基本81(アセンション時は81〜90の幅が消え固定90になる。MinInitialHp=基本81/アセンション90、MaxInitialHp=基本84/アセンション90)
- Move:
  - `SWOOP_MOVE`(攻撃): 基本17/アセンション19
  - `PECK_MOVE`(攻撃・複数ヒット): 1ヒットあたり基本3/アセンション4 × 3回(ヒット数はアセンションでも3のまま変化なし)
- 行動パターン: `SWOOP→PECK→SWOOP→PECK→…` の交互ループ(初期状態はSWOOP)
- ギミック: 開始時に `TerritorialPower` を量1で付与(「縄張り意識」系。具体的な発動条件・効果は本ファイルからは不明。攻撃を受ける/近づかれると強化される類のパワーと推測される)
- 幕: Overgrowth(緑幕)エリート。`ByrdonisElite`

## Byrdpip (Byrdpip.cs)
- HP: 9999/9999、HPバー非表示
- Move: `NOTHING_MOVE` のみ(自己ループ)
- 行動パターン: 何もしない
- ギミック: **戦闘敵ではない**。レリック「Byrdpip」に紐づくペット/コンパニオン(`base.Creature.PetOwner.GetRelic<Byrdpip>().Skin` でスキンをレリック所持者から取得)。Byrdonis(エリート鳥)とは無関係の別モンスター定義
- 幕: 幕不問(レリック効果としてどの幕でも同伴しうる非戦闘ペット)

## Calcified Cultist (CalcifiedCultist.cs)
- HP: 基本38〜41 / アセンション(ToughEnemies)39〜42
- Move:
  - `INCANTATION_MOVE`(バフ): `RitualPower` を量2で自身に付与
  - `DARK_STRIKE_MOVE`(攻撃): 基本9/アセンション(DeadlyEnemies)11。命中のたびにSFXの強度パラメータが+0.2ずつ蓄積(演出のみ、ゲームプレイに影響なし)
- 行動パターン: `INCANTATION_MOVE`(1回のみ)→`DARK_STRIKE_MOVE`の自己ループ。RitualPower(通常「毎ターンStrength増加」系のStS定番パワー)は開始直後の1回しか付与されず、以降はひたすら攻撃を続ける
- ギミック: 特になし
- 幕: Underdocks(下水幕)。`CultistsNormal` 編成、および `SeapunkNormal` 編成にも混成で出現

## Ceremonial Beast (CeremonialBeast.cs) ― ボス
- HP: 固定252(アセンション時262。Min=Max)
- Move(フェーズ1):
  - `STAMP_MOVE`(バフ): `PlowPower` を量基本150/アセンション160で自身に付与(疑似シールド/次の突進の威力蓄積と推測)
  - `PLOW_MOVE`(攻撃+バフ): 全力突進攻撃 基本18/アセンション20、着地後にStrength+2を獲得
- Move(フェーズ2、スタン明け後):
  - `STUN_MOVE`(スタン・強制1回): 何もしない(突進を止められた反動で目を回す)
  - `BEAST_CRY_MOVE`(デバフ): 対象全員に `RingingPower` を量1付与(咆哮による耳鳴り/デバフ系)
  - `STOMP_MOVE`(攻撃): 基本15/アセンション17
  - `CRUSH_MOVE`(攻撃+バフ): 基本17/アセンション19、命中後にStrength+基本3/アセンション4
- 行動パターン: 初期状態は `STAMP_MOVE→PLOW_MOVE`。`PLOW_MOVE` は自己ループ(`PLOW→PLOW→PLOW…`)に設定されており、外部条件(プレイヤーが`PlowPower`のスタックを削り切る等)で `SetStunned()` が呼ばれない限り永久に突進し続ける想定。`SetStunned()` が呼ばれると `IsStunnedByPlowRemoval`/`IsInSecondPhase` がtrueになり、`STUN_MOVE`(1回休み・MustPerformOnceBeforeTransitioning指定)→`BEAST_CRY_MOVE→STOMP_MOVE→CRUSH_MOVE→BEAST_CRY_MOVE→…` のフェーズ2ループへ移行(BeastCry↔Stomp↔Crushの3状態巡回)
- ギミック: 「Plow(突進の溜め)」を破壊されるとスタンして第2形態に移行する、シールドブレイク型のボス構造(STS1のBronze Automaton/Nemesisに近い設計思想)。`SetStunned()` を呼ぶ具体的トリガー条件(PlowPowerの実装)は本ファイル外のため未確認。画面揺れ・ヒットストップ等の演出も豊富
- 幕: Overgrowth(緑幕)ボス。`CeremonialBeastBoss`

## Chomper (Chomper.cs)
- HP: 基本60〜64 / アセンション(ToughEnemies)時63〜67
- Move:
  - `CLAMP_MOVE`(攻撃): ClampDamage(基本8/アセンション9) × 2ヒット(multi-attack)
  - `SCREECH_MOVE`(特殊/デバフ): 使用不可札「Dazed」を対象の捨て札に3枚追加(StatusIntent(3)表示)。会話台詞演出あり
- 行動パターン: `CLAMP_MOVE ⇔ SCREECH_MOVE` の2状態が互いを`FollowUpState`として指し合う単純な交互ローテーション。ただし開始ノードは `ScreamFirst` フラグで選べる(true なら SCREECH から開始、false なら CLAMP から開始)。実質「叩く→叫ぶ→叩く→叫ぶ…」または逆順の固定ローテ
- ギミック: `AfterAddedToRoom` で自身に ArtifactPower 2 を付与(2回まで無効貫通の効果を持つ想定)。分裂・爆発なし
- 幕: Hive

## CorpseSlug (CorpseSlug.cs)
- HP: 基本25〜27 / アセンション(ToughEnemies)時27〜29
- Move:
  - `WHIP_SLAP_MOVE`(攻撃): 固定3ダメ×2ヒット(WhipSlapRepeat=2、アセンション影響なし)
  - `GLOMP_MOVE`(攻撃): GlompDamage(基本8/アセンション9)単発
  - `GOOP_MOVE`(デバフ): 対象にFrailPower 2付与(固定)
- 行動パターン: `WHIP_SLAP → GLOMP → GOOP → WHIP_SLAP …` の3状態固定ローテーション(ループ)。ただし開始位置は `StarterMoveIdx % 3` で個体ごとにずらされる。`EnsureCorpseSlugsStartWithDifferentMoves` という専用staticメソッドがあり、同時に出現する複数のCorpseSlugが互いに異なる開始Moveを取るようRNGで割り振る(群れ内でローテーションの位相をずらす仕組み)
- ギミック: `AfterAddedToRoom` で自身に RavenousPower(基本4/アセンション5)を付与。「Ravenous(貪欲)」状態のときはアニメーション分岐が変わる(死亡・被弾モーションが専用の"devouring"系に切り替わる)。HP閾値によるフェーズ変化ではなく、常時パワーによる演出分岐
- 幕: Underdocks

## CrossbowRubyRaider (CrossbowRubyRaider.cs)
- HP: 基本18〜21 / アセンション(ToughEnemies)時19〜22
- Move:
  - `RELOAD_MOVE`(防御): ブロック3固定を得る。同時に内部フラグ `IsCrossbowReloaded=true`
  - `FIRE_MOVE`(攻撃): FireDamage(基本14/アセンション16)単発。撃つと `IsCrossbowReloaded=false` に戻る
- 行動パターン: `RELOAD → FIRE → RELOAD → FIRE …` の2状態交互ローテーション(開始はRELOADから)。装填フラグは見た目(空の弩/装填済みの弩のアニメ切替)のみに使われ、行動選択自体には影響しない
- ギミック: 特になし(ArtifactやHP半分フェーズなし)。RubyRaiders群れの一員としてOvergrowth幕の通常戦で複数体同時出現する設計
- 幕: Overgrowth (RubyRaidersNormal)

## Crusher (Crusher.cs)
- HP: 基本209 / アセンション(ToughEnemies)時219(Min=Maxで固定値)
- Move:
  - `THRASH_MOVE`(攻撃): ThrashDamage(基本12/アセンション14)単発
  - `ENLARGING_STRIKE_MOVE`(攻撃): EnlargingStrikeDamage 固定4(アセンションでも変化なし)単発
  - `BUG_STING_MOVE`(攻撃+デバフ): BugStingDamage(基本6/アセンション7)×2ヒット、さらに対象にWeakPower 2・FrailPower 2を同時付与
  - `ADAPT_MOVE`(バフ): 自身にStrengthPower AdaptStrengthGain(基本2/アセンション3)
  - `GUARDED_STRIKE_MOVE`(攻撃+防御): GuardedStrikeDamage(基本12/アセンション14)単発 + ブロック18固定を得る
- 行動パターン: `THRASH → ENLARGING_STRIKE → BUG_STING → ADAPT → GUARDED_STRIKE → THRASH …` の5状態固定ローテーション(1周ループ)
- ギミック: 「Kaiser Crab Boss」の左腕パーツとして実装されたサブユニット(`NKaiserCrabBossBackground.ArmSide.Left` を参照し、背景演出=専用の腕アニメを操作)。`AfterAddedToRoom` で自身にBackAttackLeftPower・CrabRagePower(各1)を付与。死亡時は腕の死亡演出+(戦闘終了条件が満たされていれば)本体の死亡演出も再生。単体モンスターというより多パーツ構成ボスの一部品
- 幕: Hive (KaiserCrabBoss戦の構成パーツ)

## CubexConstruct (CubexConstruct.cs)
- HP: 基本65 / アセンション(ToughEnemies)時70(Min=Max固定)
- Move:
  - `CHARGE_UP_MOVE`(バフ): 0.75秒待機演出後、自身にStrengthPower 2固定
  - `REPEATER_MOVE`/`REPEATER_MOVE_2`(攻撃+バフ、同一実装 RepeaterBlastMove を共有): BlastDamage(基本7/アセンション8)単発 + 自身にStrengthPower 2固定を追加付与(=攻撃するたびさらに筋力が伸びる)
  - `EXPEL_BLAST`(攻撃): ExpelDamage(基本5/アセンション6)×2ヒット
  - `SUBMERGE_MOVE`(防御): 1.25秒の潜行演出後、ブロック15固定を得る
- 行動パターン: 開始は `CHARGE_UP → REPEATER(1回目) → REPEATER_2(2回目) → EXPEL_BLAST → REPEATER(1回目に戻る)` のループ(SUBMERGE単体は`FollowUpState`が`CHARGE_UP`に戻る別枝だが、通常ローテーションには組み込まれておらず初期状態にも選ばれていないため、実質使われない孤立ノードに見える)。実運用上のメインループは「チャージ→連射×2(筋力が毎回+2ずつ蓄積)→放出2連撃→連射に戻る」で、殴るたびに強くなる自己バフ内蔵の攻撃パターン
- ギミック: `AfterAddedToRoom` でブロック13固定を得た上でArtifactPower 1を付与(開幕から守りと無効貫通を持つ)。潜る/潜行を解く見た目のスキン(目・苔模様)をランダム選択するだけの装飾コードあり。HP減少をトリガーにSEパラメータを切り替える(演出のみ、行動パターンへの影響なし)
- 幕: Overgrowth(通常戦 CubexConstructNormal)。Glory幕の「ConstructMenagerieNormal」(PunchConstructと混成)にも再登場する

## DampCultist (DampCultist.cs)
- HP: 基本51〜53 / アセンション(ToughEnemies)時52〜54
- Move:
  - `INCANTATION_MOVE`(バフ): 自身にRitualPower(基本5/アセンション6)。以後毎ターン自動で筋力が増え続ける典型的な「儀式」バフ
  - `DARK_STRIKE_MOVE`(攻撃): DarkStrikeDamage(基本1/アセンション3)単発。攻撃するたびSFX強度パラメータが+0.2ずつ蓄積(演出のみ)
- 行動パターン: `INCANTATION_MOVE → DARK_STRIKE_MOVE → DARK_STRIKE_MOVE → DARK_STRIKE_MOVE …`(INCANTATIONは開始時に1回だけ発生し、`FollowUpState`はDARK_STRIKE自身をループさせる自己参照。つまり最初に1回だけ詠唱でRitualを積み、以降は永久にDARK_STRIKEを連打し続ける構造)
- ギミック: 基礎ダメージが極端に低い(基本1)代わりにRitual(毎ターン筋力up)を先に積んでおり、時間経過で殴打が加速するタイプ。HP半分フェーズなし
- 幕: Underdocks (Cultists)

## Decimillipede (DecimillipedeSegment.cs 基底 + DecimillipedeSegmentBack/Front/Middle.cs)

3ファイル(Back/Front/Middle)はすべて `DecimillipedeSegment` 抽象基底クラスを継承する12〜15行の薄いサブクラスで、差分は「自分の攻撃時に揺らすビジュアルノードの指定」だけ(Back/Frontは単一の`SegmentDriver`、Middleは`LeftSegmentDriver`と`RightSegmentDriver`の両方を揺らす)。ゲームプレイ上のMove・数値・行動パターンは3体で完全に共通。

- HP(各セグメント共通): 基本40〜46 / アセンション(ToughEnemies)時46〜52。ただし`AfterAddedToRoom`で「他の生存中の仲間セグメントとHPが被らないよう2ずつ増減して被り回避」する特殊な均し処理があり、同一エンカウンター内の複数体が意図的に少しずつ異なるHPになる(倍率上限に達したら最小値へ巻き戻す)
- Move:
  - `WRITHE_MOVE`(攻撃): WritheDamage(基本5/アセンション6)×2ヒット
  - `BULK_MOVE`(攻撃+バフ): BulkDamage(基本6/アセンション7)単発 + 自身にStrengthPower 2固定
  - `CONSTRICT_MOVE`(攻撃+デバフ): ConstrictDamage(基本8/アセンション9)単発 + 対象にWeakPower 1固定
  - `DEAD_MOVE`(特殊・無効): 何もしない空アクション。セグメントが「死亡」状態にある間のプレースホルダー
  - `REATTACH_MOVE`(回復/特殊、`MustPerformOnceBeforeTransitioning=true`): `ReattachPower.DoReattach()` を呼び、死亡状態から再接続(復活)する専用アクション。HealIntentとして表示
- 行動パターン: `CONSTRICT → BULK → WRITHE → CONSTRICT …` の3状態が環状に`FollowUpState`で繋がった通常ローテーション(開始位置は`StarterMoveIdx % 3`で個体ごとにずらす、CorpseSlugと同じ手法)。加えて `DeadState`(死亡時に強制遷移)→ `REATTACH_MOVE`(必ず1回実行)→ 通常ローテーションへの合流用 `RandomBranchState`(3ムーブから CannotRepeat 付きでランダム選択して復帰)という「死亡後に自動で復活し、ランダムな行動から通常ローテへ再合流する」特殊経路を持つ
- ギミック: 開幕でReattachPower 25を自身に付与(=このパワーの残数ぶん「戦闘不能になっても自動で繋ぎ直って復活」できる百足の節、と読める)。死亡すると「wither(しおれる)」アニメーションに入り、REATTACH実行時に「regenerate(再生)」アニメーションへ復帰。フォビアモード(虫恐怖症配慮の代替テクスチャ)専用の生死テクスチャ切替処理あり。複数セグメント(Front/Middle/Back)が連結した1体の百足として描かれ、まとめて攻撃演出が同期する(`AnimSegmentsAttack`で他の生存セグメント全員を同時に揺らす)
- 幕: Hive のエリート戦(DecimillipedeElite)。3体編成(Front/Middle/Back)

## DevotedSculptor (DevotedSculptor.cs)
- HP: 基本162 / アセンション(ToughEnemies)時172(Min=Max固定)
- Move:
  - `FORBIDDEN_INCANTATION_MOVE`(バフ): 自身にRitualPower 9固定(対象は本人ではなくnull=自己参照扱いの詠唱)。台詞演出あり
  - `SAVAGE_MOVE`(攻撃): SavageDamage(基本12/アセンション15)単発
- 行動パターン: `FORBIDDEN_INCANTATION_MOVE`(開始時1回のみ)→ `SAVAGE_MOVE`(以降は自己ループで永久連打)。DampCultistと同型の「最初に大きなRitualを1回積んでから、以後はひたすら攻撃し続ける」パターン。Ritual 9は非常に高い値で、ターンを経るごとの筋力上昇が急激
- ギミック: 特になし(HP半分フェーズ・召喚等なし)。単体HPが162〜172と高く、Ritual 9の急成長と合わせて「長引かせると危険な雑魚(weak)」の設計
- 幕: Glory (弱敵格 DevotedSculptorWeak)

## Doormaker (Doormaker.cs) ― ボス
- HP: 基本489 / アセンション(ToughEnemies)時512(Min=Max固定)。ただし開幕は下記ギミックにより見かけ上「無限HP」になる
- Move:
  - `DRAMATIC_OPEN_MOVE`(特殊/SummonIntent表示): 「扉」状態から「真の姿」への変身演出。実HPを公開し、それまでに付いていた全パワーを剥奪、HungerPowerを自身に付与、外見テクスチャを差し替え、専用台詞とBGMパラメータ変化(`queen_progress`)を発生させる
  - `HUNGER_MOVE`(攻撃): HungerDamage(基本30/アセンション35)単発。実行後ScrutinyPowerへフェーズを切替
  - `SCRUTINY_MOVE`(攻撃): ScrutinyDamage(基本24/アセンション26)単発。実行後GraspPowerへフェーズを切替
  - `GRASP_MOVE`(攻撃+バフ): GraspDamage(基本10/アセンション11)×2ヒット + 自身にStrengthPower(基本3/アセンション4)。実行後HungerPowerへフェーズを切替
- 行動パターン: `DRAMATIC_OPEN`(開幕1回限り)→ `HUNGER → SCRUTINY → GRASP → HUNGER …` の3状態固定ループ。各Moveの実行後に自分の「フェーズパワー」(Hunger/Scrutiny/Grasp)を毎回付け替えており、パワーが次の見た目・示唆演出の役割を持つ(単なる行動選択には使っていない、`SwapPhasePower`は現在の3パワーを全部剥がしてから新しいものを1つ付ける排他的スイッチ)
- ギミック: 「タイトルが未開扉状態(`DOOR.name`)と開扉後(`base.Id.Entry.name`=本来のボス名)で切り替わる」変身演出。`AfterAddedToRoom`でHPを999999999(実質無限表示)にセットし`ShowsInfiniteHp=true`にする=プレイヤーには最初「倒せない扉」に見える。`DRAMATIC_OPEN_MOVE`の実行で初めて本来のHP(489〜512)に戻り、全パワーが吹き飛ばされて素の状態から本番の戦闘が始まる。死亡時にBGMパラメータを進行させる(ストーリー的な演出フック)
- 幕: Glory (ボス, DoormakerBoss)

## Entomancer (Entomancer.cs)
- HP: 基本145 / アセンション(ToughEnemies)時155(Min=Max固定)
- Move:
  - `PHEROMONE_SPIT_MOVE`(バフ/特殊): 自身の`PersonalHivePower`が3未満なら PersonalHivePower+1 と StrengthPower+1 を両方付与、3以上に達していれば代わりにStrengthPower+2のみを付与(スタック上限に応じて効果が変化する条件分岐バフ)
  - `BEES_MOVE`(攻撃): BeesDamage 固定3 × BeesRepeat(基本7/アセンション8)ヒットの多段攻撃
  - `SPEAR_MOVE`(攻撃): SpearMoveDamage(基本18/アセンション20)単発
- 行動パターン: `BEES_MOVE → SPEAR_MOVE → PHEROMONE_SPIT_MOVE → BEES_MOVE …` の3状態固定ローテーション(開始はBEES_MOVEから)
- ギミック: `AfterAddedToRoom`でPersonalHivePower 1を自身に付与(蜂の巣スタック)。PHEROMONE_SPIT_MOVEの効果分岐がこのスタック依存で、蜂の巣が育ちきる(3以上)と純粋な筋力バフに切り替わる=「蜂を増やす初期フェーズ」→「蜂が増えたら殴りを強化するフェーズ」という緩やかな成長ギミック。HP半分フェーズなし
- 幕: Hive (エリート, EntomancerElite)

## Exoskeleton (Exoskeleton.cs)
- HP: 基本24〜28 / アセンション(ToughEnemies)時25〜29
- Move:
  - `SKITTER_MOVE`(攻撃): SkitterDamage固定1 × SkitterRepeats(基本3/アセンション4)ヒットの多段攻撃
  - `MANDIBLE_MOVE`(攻撃): MandiblesDamage(基本8/アセンション9)単発
  - `ENRAGE_MOVE`(バフ): 自身にStrengthPower 2固定
- 行動パターン: 群れの中での「配置スロット」に応じた初手固定分岐が特徴的。`ConditionalBranchState`で `SlotName == "first"` なら初手SKITTER、`"second"`なら初手MANDIBLE、`"third"`なら初手ENRAGE、`"fourth"`なら初手からランダム分岐(SKITTERとMANDIBLEを重み1:1でランダム、CannotRepeat付き)。SKITTERの後続はランダム分岐へ、MANDIBLEの後続はENRAGEへ、ENRAGEの後続はランダム分岐へ、というように収束していく。つまり「同じ種類の敵が並んでいても、立ち位置(1〜4体目)ごとに初動の役割が変わる」設計で、複数体編成時の初手多様性を作っている
- ギミック: `AfterAddedToRoom`でHardToKillPower 9を自身に付与(高スタックの「倒されにくさ」系パワー=雑魚だが群れで削り切りにくい耐久ギミックと推測される)。HP半分フェーズなし、死亡時特殊効果なし
- 幕: Hive (通常/弱 ExoskeletonsNormal・ExoskeletonsWeak)

## EyeWithTeeth (EyeWithTeeth.cs)
- HP: 6 / 6（アセンション差なし。MaxInitialHp = MinInitialHp）
- Move: `DISTRACT_MOVE`（特殊）: ダメージなし。プレイヤーの捨て札に使用不可札 `Dazed` を3枚注入する(StatusIntent(3))
- 行動パターン: `DISTRACT_MOVE` のみの単一ループ（A→A→A…）
- ギミック: `AfterAddedToRoom` で自身に `IllusionPower` を1付与。`ShouldDisappearFromDoom = false`。Fogmogの `ILLUSION_MOVE` から追加召喚されることがある(下記Fogmog参照。召喚時のid="illusion")
- 幕: Overgrowth（FogmogNormal encounterの一員として直接も出現し、Fogmogの召喚でも登場）

## Fabricator (Fabricator.cs)
- HP: 基本150 / アセンション(ToughEnemies)155（Min=Max固定）
- Move:
  - `FABRICATE_MOVE`（特殊/召喚）: ダメージなし。防御用ボット1体＋攻撃用ボット1体を召喚(SummonIntent)
  - `FABRICATING_STRIKE_MOVE`（攻撃+召喚）: 単体攻撃 基本18/アセンション(DeadlyEnemies)21 + 攻撃用ボットを1体追加召喚
  - `DISINTEGRATE_MOVE`（攻撃）: 単体攻撃のみ 基本11/アセンション13
- 行動パターン: `ConditionalBranchState "fabricateBranch"` で分岐。「生存する味方(自分含む)が4体未満」なら確率分岐(FABRICATE 50%とFABRICATING_STRIKE 50%、共にCanRepeatForeverで連続可)を繰り返す。「4体で満杯」ならDISINTEGRATE_MOVE固定に切り替わる。要するに味方が少ない間は召喚を優先し、場が埋まったら素の攻撃連打に転じる
- ギミック: 召喚先は防御枠(Guardbot/Noisebot からランダム、直前に出したのと同じ個体は選ばない)と攻撃枠(Zapbot/Stabbot)。召喚された子分には `MinionPower` が付与される。`ShouldFadeAfterDeath = false`
- 幕: Glory（Guardbotを召喚元として使用）

## FakeMerchantMonster (FakeMerchantMonster.cs)
- HP: 基本165 / アセンション(ToughEnemies)175（Min=Max）
- Move:
  - `SWIPE_MOVE`（攻撃）: 単体攻撃 基本13/アセンション(DeadlyEnemies)15
  - `SPEW_COINS_MOVE`（攻撃・多段）: 2ダメ×8ヒット固定（アセンション差なし）
  - `THROW_RELIC_MOVE`（攻撃+デバフ）: 単体攻撃 基本9/アセンション10 + Frail 1付与
  - `ENRAGE_MOVE`（バフ）: ダメージなし、Strength+2
- 行動パターン: 初手は`SWIPE_MOVE`固定。以降は`RAND_MOVE`分岐（Swipe/SpewCoins/ThrowRelic/Enrage[重み3・他は重み1、いずれも直前技は再選択不可]）で確率選択するが、`THROW_RELIC_MOVE`の直後だけは例外的に`RAND_ATTACK_MOVE`（Swipe/SpewCoins/ThrowRelicのみ、Enrageを含まない）に分岐し、次の1手はEnrageが出ない仕様
- ギミック: 各Moveに専用のセリフ(banter)が紐づく（`ShowDialogueForMove`）。クラス名・SFX(`reverse_merchant`)から「偽の商人／強盗イベント」専用モンスターと推測。`FakeMerchantEventEncounter`というイベント専用encounterからのみ生成され、4幕(Overgrowth/Underdocks/Hive/Glory)いずれのActファイルにも直接の紐づけが見当たらない
- 幕: 不明（幕非依存のイベント専用モンスターと推測。ソース上「幕」の直接記載なし）

## FatGremlin (FatGremlin.cs)
- HP: 基本13 / アセンション(ToughEnemies)14（Min）、基本17 / アセンション18（Max）
- Move:
  - `SPAWNED_MOVE`（特殊）: ダメージなし。StunIntent（起き上がる演出のみ、実質何もしない）
  - `FLEE_MOVE`（特殊/逃走）: ダメージなし。EscapeIntent。セリフ表示後、戦闘から離脱する(`CreatureCmd.Escape`)
- 行動パターン: `SPAWNED_MOVE`→`FLEE_MOVE`→（以降FLEE_MOVEを自己ループ、ただし通常は最初のFLEEで戦闘離脱済み）。攻撃行動は一切なく、目覚めてすぐ逃げるだけの敵
- ギミック: 攻撃力ゼロ。存在自体が「戦わずに逃げる」タイプ。起床/逃走で見た目アニメ(WakeUpTrigger/FleeTrigger)が変わるのみ
- 幕: Underdocks（GremlinMercNormal 編成の一員）

## FlailKnight (FlailKnight.cs)
- HP: 基本101 / アセンション(ToughEnemies)108（Min=Max固定）
- Move:
  - `WAR_CHANT`（バフ）: ダメージなし、Strength+3
  - `FLAIL_MOVE`（攻撃・多段）: 基本9/アセンション(DeadlyEnemies)10 ×2ヒット固定
  - `RAM_MOVE`（攻撃）: 単体攻撃 基本15/アセンション17
- 行動パターン: 初手は`RAM_MOVE`固定。以降は3種すべてが同じ`RandomBranchState "RAND"`に合流し、重み(WarChant:1・Flail:2・Ram:2、いずれも直前技は再選択不可)で確率選択を繰り返す
- ギミック: 特になし（KnightsElite編成のエリート3体の一角）
- 幕: Glory（`KnightsElite` = FlailKnight・MagiKnight・SpectralKnightの3体編成エリート）

## Flyconid (Flyconid.cs)
- HP: 基本47 / アセンション(ToughEnemies)51（Min）、基本49 / アセンション53（Max）
- Move:
  - `VULNERABLE_SPORES_MOVE`（デバフ）: ダメージなし。Vulnerable 2付与
  - `FRAIL_SPORES_MOVE`（攻撃+デバフ）: 単体攻撃 基本8/アセンション(DeadlyEnemies)9 + Frail 2付与
  - `SMASH_MOVE`（攻撃）: 単体攻撃 基本11/アセンション12
- 行動パターン: 開始時は専用の`INITIAL`分岐（FrailSpores重み2 / Smash重み1、直前技禁止）で1手選び、以降は`RAND`分岐（VulnerableSpores重み3 / FrailSpores重み2 / Smash重み1、いずれも直前技禁止）を繰り返す確率型
- ギミック: 特殊パワー付与なし。胞子の見た目色(脆弱/フレイル)が攻撃種別で変わる演出のみ
- 幕: Overgrowth

## Fogmog (Fogmog.cs)
- HP: 基本74 / アセンション(ToughEnemies)78（Min=Max固定）
- Move:
  - `ILLUSION_MOVE`（特殊/召喚）: ダメージなし。`EyeWithTeeth`を1体召喚(id="illusion")
  - `SWIPE_MOVE` / `SWIPE_RANDOM_MOVE`（同一メソッド、攻撃+バフ）: 単体攻撃 基本8/アセンション(DeadlyEnemies)9 + Strength+1
  - `HEADBUTT_MOVE`（攻撃）: 単体攻撃 基本14/アセンション16
- 行動パターン: 初手`ILLUSION_MOVE`（幻影のEyeWithTeethを召喚）→`SWIPE_MOVE`固定→以降`BRANCH`分岐で「40%: SWIPE_RANDOM_MOVE→次は必ずHEADBUTT_MOVE」「60%: HEADBUTT_MOVE→次は必ずSWIPE_MOVE」を繰り返す(実質「薙ぎ払いと頭突きが交互だが、たまに薙ぎ払いを2連打してから頭突きに戻る」ような揺らぎ)
- ギミック: 開幕でEyeWithTeethを追加召喚し戦闘序盤の的を増やす
- 幕: Overgrowth

## FossilStalker (FossilStalker.cs)
- HP: 基本51 / アセンション(ToughEnemies)54（Min）、基本53 / アセンション56（Max）
- Move:
  - `TACKLE_MOVE`（攻撃+デバフ）: 単体攻撃 基本9/アセンション(DeadlyEnemies)11 + Frail 1付与
  - `LATCH_MOVE`（攻撃）: 単体攻撃 基本12/アセンション14
  - `LASH_MOVE`（攻撃・多段）: 基本3/アセンション4 ×2ヒット固定
- 行動パターン: 初手`LATCH_MOVE`固定。以降は3種均等重み(各2)のRAND分岐（直前技は再選択不可）を繰り返す
- ギミック: `AfterAddedToRoom`で自身に`SuckPower`を3付与（開幕から吸収系の常在パワーを保持。効果詳細はパワーカタログ側で要確認）
- 幕: Underdocks

## FrogKnight (FrogKnight.cs)
- HP: 基本191 / アセンション(ToughEnemies)199（Min=Max固定）
- Move:
  - `FOR_THE_QUEEN`（バフ）: ダメージなし、Strength+5
  - `STRIKE_DOWN_EVIL`（攻撃）: 単体攻撃 基本21/アセンション(DeadlyEnemies)23
  - `TONGUE_LASH`（攻撃+デバフ）: 単体攻撃 基本13/アセンション14 + Frail 2付与
  - `BEETLE_CHARGE`（攻撃・大技）: 単体攻撃 基本35/アセンション40。使用後1秒待機演出あり、一度使うとフラグが立ち以後使用不可
- 行動パターン: 初手`TONGUE_LASH`固定→`STRIKE_DOWN_EVIL`→`FOR_THE_QUEEN`→条件分岐`HALF_HEALTH`（「BEETLE_CHARGE使用済み」または「HP50%以上」なら`TONGUE_LASH`へ戻る／「未使用かつHP50%未満」なら一度きりの`BEETLE_CHARGE`へ）→`BEETLE_CHARGE`使用後は`TONGUE_LASH`に戻る。通常は3手固定ローテ(舌打ち→斬撃→鼓舞)を回しつつ、HPが半分を切った瞬間に一度だけ大技を挟む
- ギミック: `AfterAddedToRoom`で自身に`PlatingPower`(基本15/アセンション19)を付与（開幕から纏う装甲/シールド系常在パワーと推測）。HP半分閾値で一度きりの大技分岐（フェーズ変化に近いが単発）
- 幕: Glory

## FuzzyWurmCrawler (FuzzyWurmCrawler.cs)
- HP: 基本55 / アセンション(ToughEnemies)58（Min）、基本57 / アセンション59（Max）
- Move:
  - `FIRST_ACID_GOOP` / `ACID_GOOP`（同一メソッド、攻撃）: 単体攻撃 基本4/アセンション(DeadlyEnemies)6
  - `INHALE`（バフ）: ダメージなし、Strength+7
- 行動パターン: `FIRST_ACID_GOOP`→`INHALE`→`ACID_GOOP`→`FIRST_ACID_GOOP`…の完全固定ローテ（A→B→A→B型、分岐なし）。INHALEのたびにStrengthが+7ずつ積み上がるため、酸攻撃が雪だるま式に伸びていく
- ギミック: `IsPuffed`フラグで「膨らんだ」見た目・アニメに切り替わるが、戦闘への数値効果はStrength蓄積のみ
- 幕: Overgrowth（弱編成 `FuzzyWurmCrawlerWeak`、および`OvergrowthCrawlers`でShrinkerBeetleと組んで登場）

## GasBomb (GasBomb.cs)
- HP: 基本7 / アセンション(ToughEnemies)8（Min=Max固定）
- Move: `EXPLODE_MOVE`（自爆攻撃/特殊）: 単体攻撃 基本8/アセンション(DeadlyEnemies)9。特殊Intent型`DeathBlowIntent`を使用し、攻撃実行直後に自分自身を強制的にKillする
- 行動パターン: 選択肢なし。唯一のMoveのみで、行動が回ってくると必ず自爆して攻撃→自滅する使い切り型
- ギミック: `AfterAddedToRoom`で自身に`MinionPower`を1付与（雑魚/子分判定）。`ShouldFadeAfterDeath = false`（爆発演出のため通常の死亡フェードを無効化）。LivingFog編成の子分と推測
- 幕: Underdocks（LivingFogNormal編成の一員）

## GlobeHead (GlobeHead.cs)
- HP: 基本148 / アセンション(ToughEnemies)158（Min=Max固定）
- Move:
  - `THUNDER_STRIKE`（攻撃・多段）: 基本6/アセンション(DeadlyEnemies)7 ×3ヒット固定
  - `SHOCKING_SLAP`（攻撃+デバフ）: 単体攻撃 基本13/アセンション14 + Frail 2付与
  - `GALVANIC_BURST`（攻撃+バフ）: 単体攻撃 基本16/アセンション17 + Strength+2
- 行動パターン: `SHOCKING_SLAP`→`THUNDER_STRIKE`→`GALVANIC_BURST`→`SHOCKING_SLAP`…の完全固定3手ローテーション（分岐なし）
- ギミック: `AfterAddedToRoom`で自身に`GalvanicPower`を6付与（開幕から蓄積する常在パワー。ダメージ増幅系と推測、要カタログ確認）
- 幕: Glory

## GremlinMerc (GremlinMerc.cs)
- HP: 基本47 / アセンション(ToughEnemies)51（Min）、基本49 / アセンション53（Max）
- Move:
  - `GIMME_MOVE`（攻撃・多段+盗み）: 基本7/アセンション(ToughEnemies基準、DeadlyEnemiesではない点に注意)8 ×2ヒット固定 + `ThieveryPower`のSteal()発動
  - `DOUBLE_SMASH_MOVE`（攻撃・多段+デバフ+盗み）: 基本6/アセンション7 ×2ヒット固定 + Weak 2付与 + Steal()
  - `HEHE_MOVE`（攻撃+バフ+盗み）: 単体攻撃 基本8/アセンション9 + Strength+2 + Steal()
- 行動パターン: `GIMME_MOVE`→`DOUBLE_SMASH_MOVE`→`HEHE_MOVE`→`GIMME_MOVE`…の完全固定3手ローテーション（分岐なし）
- ギミック: `AfterAddedToRoom`で自身に`SurprisePower`を1付与、さらに全プレイヤーそれぞれに`ThieveryPower`(20)を個別付与。すべてのMoveで`ThieveryPower`インスタンスの`Steal()`を呼ぶため、毎ターン確実にゴールドを盗む（盗みタイプの敵）
- 幕: Underdocks

## Guardbot (Guardbot.cs)
- HP: 基本16 / アセンション(ToughEnemies)17（Min）、基本20 / アセンション21（Max）
- Move: `GUARD_MOVE`（防御/支援）: 自分にはダメージ・ブロックを与えず、`Fabricator`本体（このCreatureが所属する編成内のFabricatorモンスター）に15ブロックを付与する支援専用行動
- 行動パターン: `GUARD_MOVE`のみの単一ループ（毎ターン同じ支援行動）
- ギミック: `Fabricator`が召喚する防御用の子分(defenseSpawns)。単独のencounterは存在せず、Fabricator戦にのみ出現する。着地位置調整以外の開幕パワー付与なし
- 幕: Glory（Fabricatorの召喚体）

## HauntedShip (HauntedShip.cs)
- HP: 基本63 / アセンション(ToughEnemies)67（Min=Max固定）
- Move:
  - `RAMMING_SPEED_MOVE`（攻撃+デバフ）: 単体攻撃 基本10/アセンション(DeadlyEnemies)11 + Weak 1付与
  - `SWIPE_MOVE`（攻撃）: 単体攻撃 基本13/アセンション14
  - `STOMP_MOVE`（攻撃・多段）: 基本4/アセンション5 ×3ヒット固定
  - `HAUNT_MOVE`（特殊）: ダメージなし。捨て札に使用不可札`Dazed`を5枚注入(StatusIntent(5))
- 行動パターン: 初手`HAUNT_MOVE`固定（Dazed5枚を先制注入）。以降は`RAND`分岐で体当たり/薙ぎ払い/踏みつけの3択（各重み「ラウンド数が奇数なら1、偶数なら0」という条件付きウェイト）を繰り返す——実装上、偶数ラウンドでは3つとも重み0になるため実質「奇数ラウンドでのみ通常攻撃を選ぶ」形になっている点に注意（要検証、実装の癖の可能性）
- ギミック: 開幕Dazedばら撒きに加え、ラウンドの奇偶で行動可否が変わる珍しい条件分岐
- 幕: Underdocks

## HunterKiller (HunterKiller.cs)
- HP: MinInitialHp=MaxInitialHp=121（アセンションToughEnemies時126）
- Move:
  - `TENDERIZING_GOOP_MOVE`(デバフ): TenderPower 1を全対象に付与。ダメージなし
  - `BITE_MOVE`(攻撃): 単体17ダメ（アセンションDeadlyEnemies時19）
  - `PUNCTURE_MOVE`(攻撃): 3ヒット、1ヒットあたり7ダメ（アセンション時8）
- 行動パターン: 初手固定 TENDERIZING_GOOP → 以降はRandomBranch「RAND」に入り、BITE(重み・CannotRepeat=連続不可)とPUNCTURE(重み2、連続可)を確率抽選でループ
- ギミック: 開幕演出なし。TenderPowerは弱体化デバフ（詳細は別カタログ参照）
- 幕: Hive（HunterKillerNormal）

## InfestedPrism (InfestedPrism.cs)
- HP: Min=Max=200（アセンションToughEnemies時215）
- Move:
  - `JAB_MOVE`(攻撃): 単体22ダメ（アセンション24）
  - `RADIATE_MOVE`(攻撃+防御): 単体16ダメ（アセンション18）+ ブロック16（アセンション18。ダメージ・ブロックとも基本値16/アセンション値18で偶然同一の定義）
  - `WHIRLWIND_MOVE`(攻撃): 3ヒット、1ヒットあたり9ダメ（アセンション10）
  - `PULSATE_MOVE`(バフ+防御): ブロック20（アセンション22）+Strength+4（アセンション5）
- 行動パターン: 固定ローテーション JAB→RADIATE→WHIRLWIND→PULSATE→(JABに戻る)のループ（分岐なし）
- ギミック: AfterAddedToRoomでVitalSparkPower 1を自身に付与（開幕バフ、詳細はパワーカタログ参照）
- 幕: Hive（エリート、InfestedPrismsElite）

## Inklet (Inklet.cs)
- HP: Min=11/Max=17（アセンションToughEnemies時 Min12/Max18）
- Move:
  - `JAB_MOVE`(攻撃): 単体3ダメ（アセンション4）
  - `WHIRLWIND_MOVE`(攻撃): 3ヒット、1ヒットあたり2ダメ（アセンション3）
  - `PIERCING_GAZE_MOVE`(攻撃): 単体10ダメ（アセンション11）
- 行動パターン: 開幕はRandomBranch「INIT_RAND」で JAB(重み2)かWHIRLWIND(重み1,CannotRepeat)を抽選。以降はJAB→2つ目のRandomBranch「RAND」に入りPIERCING_GAZE(CannotRepeat)かWHIRLWIND(CannotRepeat)を抽選→(WHIRLWINDならJABへ、PIERCING_GAZEならJABへ)戻る、という「JABを挟んで別行動を確率選択」の反復構造。`MiddleInklet`フラグがtrueの個体は初期状態がWHIRLWINDから始まる（同編成内でのポジション差別化用と思われる）
- ギミック: AfterAddedToRoomでSlipperyPower 1を自身に付与（開幕バフ）
- 幕: Overgrowth（InkletsNormal）

## KinFollower (KinFollower.cs)
- HP: Min=58/Max=59（アセンションToughEnemies時 Min62/Max63）
- Move:
  - `QUICK_SLASH_MOVE`(攻撃): 単体5ダメ（アセンション差なし=5固定）
  - `BOOMERANG_MOVE`(攻撃): 2ヒット、1ヒットあたり2ダメ（アセンション差なし=2固定）
  - `POWER_DANCE_MOVE`(バフ): Strength+2（アセンション時3）
- 行動パターン: 固定ローテーション QUICK_SLASH→BOOMERANG→POWER_DANCE→(QUICK_SLASHに戻る)のループ。`StartsWithDance`フラグがtrueの個体はPOWER_DANCEから開始
- ギミック: AfterAddedToRoomでMinionPower 1を自身に付与（「雑魚」扱いの印。詳細はパワーカタログ）。ボス編成TheKinBoss（KinPriestとセット）の随伴モンスター。見た目は3種のヘアバリエーションからランダム選択（外見のみ、数値に影響なし）
- 幕: Overgrowth（ボス編成 TheKinBoss）

## KinPriest (KinPriest.cs) ― ボス(随伴)
- HP: Min=Max=190（アセンションToughEnemies時199）
- Move:
  - `ORB_OF_FRAILTY_MOVE`(攻撃+デバフ): 単体8ダメ（アセンション9）+ FrailPower(虚弱相当)1付与。ヒット前に1秒のウェイト演出あり
  - `ORB_OF_WEAKNESS_MOVE`(攻撃+デバフ): 単体8ダメ（アセンション9）+ WeakPower(弱体)1付与
  - `BEAM_MOVE`(攻撃): 3ヒット、1ヒットあたり3ダメ（アセンション差なし=3固定）
  - `RITUAL_MOVE`(バフ): Strength+2（アセンション時3）。初回発動時のみ専用セリフ(_ritualApplyLine)を表示
- 行動パターン: 固定ローテーション ORB_OF_FRAILTY→ORB_OF_WEAKNESS→BEAM→RITUAL→(ORB_OF_FRAILTYに戻る)のループ（分岐なし）
- ギミック: **随伴モンスター(KinFollower)が全滅すると専用セリフ(_followersDeathLine)を発する**（AfterDeathフックでKinFollower死亡を監視し、生存するKinFollowerが0体になった瞬間にKinPriest.AllFollowerDeathResponse()を呼ぶ）。音楽パラメータ"the_kin_progress"を戦闘進行に応じて変化させる演出フックあり（ゲームプレイに直接影響しない）
- 幕: Overgrowth（ボス、TheKinBoss。KinFollowerと組み合わせで登場）

## KnowledgeDemon (KnowledgeDemon.cs) ― ボス
- HP: Min=Max=379（アセンションToughEnemies時399）
- Move:
  - `CURSE_OF_KNOWLEDGE_MOVE`(デバフ・特殊): ダメージなし。全対象に「カード選択」を強制する特殊行動——3段階のカードセット（各2択、いずれもDisintegrationを含む: 1段目=Disintegration/MindRot、2段目=Disintegration/Sloth、3段目=Disintegration/WasteAway）から1枚を各プレイヤーに選ばせてデッキに追加する。DisintegrationのDynamicVarはカウンター段階に応じて6/7/8ダメージに設定される
  - `SLAP_MOVE`(攻撃): 単体17ダメ（アセンション18）
  - `KNOWLEDGE_OVERWHELMING_MOVE`(攻撃): 3ヒット、1ヒットあたり8ダメ（アセンション9）。発動すると`IsBurnt`フラグが立ち、以後の被弾・死亡アニメが「炎上」バリエーションに切替（演出のみ）
  - `PONDER_MOVE`(攻撃+回復+バフ): 単体11ダメ（アセンション13）+ 自己回復30×プレイヤー人数（マルチプレイ対応の全体回復量）+ Strength+2（アセンション3）。発動で`IsBurnt`は解除される
- 行動パターン: 固定ローテーション CURSE_OF_KNOWLEDGE→SLAP→KNOWLEDGE_OVERWHELMING→PONDER→ConditionalBranch「CurseOfKnowledgeBranch」で、**CurseOfKnowledgeCounter(発動回数)が3未満ならCURSE_OF_KNOWLEDGEへ戻り、3以上に達したらSLAPへ直行**（呪い付与ムーブは3回だけで打ち止めになり、以降はSLAP起点のループに固定される）
- ギミック: HP閾値による分岐は無いが、**カウンター(発動回数)による行動テーブル切替**を持つ唯一のボス級敵。PONDER_MOVEが大量自己回復+バフの「息継ぎ」役。BeforeRemovedFromRoomで音楽パラメータを進行させる演出フックあり
- 幕: Hive（ボス、KnowledgeDemonBoss）

## LagavulinMatriarch (LagavulinMatriarch.cs) ― ボス
- HP: Min=Max=222（アセンションToughEnemies時233）
- Move:
  - `SLEEP_MOVE`(特殊): 何もしない（睡眠中の空ターン）
  - `SLASH_MOVE`(攻撃): 単体19ダメ（アセンション21）
  - `SLASH2_MOVE`(攻撃+防御): 単体12ダメ（アセンション14）+ ブロック12（アセンション14）
  - `DISEMBOWEL_MOVE`(攻撃): 2ヒット、1ヒットあたり9ダメ（アセンション10）
  - `SOUL_SIPHON_MOVE`(デバフ+バフ): 全対象にStrength-2・Dexterity-2を付与し、自身のStrength+2
- 行動パターン: ConditionalBranch「SLEEP_BRANCH」で**AsleepPowerを保持している間はSLEEP_MOVE(何もしない)を継続**、AsleepPowerが無くなるとSLASH開始。覚醒後は SLASH→DISEMBOWEL→SLASH2→SOUL_SIPHON→(SLASHに戻る)の固定ローテーション
- ギミック: 開幕でPlatingPower(装甲相当)12・AsleepPower(睡眠)3を自身に付与——**STS1のLagavulin(ラガヴーリン)と同型の「3ターン眠って高い装甲で凌ぐ、起こされると攻撃開始」ギミック**。HP半分以下になると`IsShellAwake`フラグが立ち「目が開く」演出に切替（ゲームプレイ効果はなく見た目のみ、実測: `AfterDamageReceived`内で`CurrentHp <= MaxHp/2`を判定）。`WakeUpMove`という公開メソッドがあり外部（睡眠妨害システム等）から強制的に起こせる構造
- 幕: Underdocks（ボス、LagavulinMatriarchBoss）

## LeafSlimeM (LeafSlimeM.cs)
- HP: Min=32/Max=35（アセンションToughEnemies時 Min33/Max36）
- Move:
  - `CLUMP_SHOT`(攻撃): 単体8ダメ（アセンション9）
  - `STICKY_SHOT`(特殊デバフ): ダメージなし。**Slimedカード（使用不可札）2枚を全対象の「捨て札」に直接注入**（山札ではなく捨て札行き=StS1と異なりターン中すぐには引かれない）
- 行動パターン: 固定の2手交互ローテーション STICKY_SHOT→CLUMP_SHOT→STICKY_SHOT→…（初期状態はSTICKY_SHOT）
- ギミック: なし（開幕パワーなし）
- 幕: Overgrowth（Slimesエンカウンター群。SlitheringStranglerNormal等の複合編成にも登場）

## LeafSlimeS (LeafSlimeS.cs)
- HP: Min=11/Max=15（アセンションToughEnemies時 Min12/Max16）
- Move:
  - `BUTT_MOVE`(攻撃/Tackle): 単体3ダメ（アセンション4）
  - `GOOP_MOVE`(特殊デバフ): ダメージなし。Slimedカード1枚を対象の捨て札に注入
- 行動パターン: RandomBranch「RAND」でBUTT_MOVEとGOOP_MOVEを確率抽選（両方CannotRepeat=同じ手を連続で出さない）
- ギミック: なし
- 幕: Overgrowth（Slimesエンカウンター群）

## LivingFog (LivingFog.cs)
- HP: Min=Max=80（アセンションToughEnemies時82）
- Move:
  - `ADVANCED_GAS_MOVE`(攻撃+カードデバフ): 単体8ダメ（アセンション9）+ SmoggyPower(カード関連デバフ)1付与
  - `BLOAT_MOVE`(攻撃+召喚): 単体5ダメ（アセンション6）+ **味方スロットにGasBombモンスターをBloatAmount体（既定1体）召喚**（召喚失敗=空きスロットなしの場合はスキップ）
  - `SUPER_GAS_BLAST_MOVE`(攻撃): 単体8ダメ（アセンション9）
- 行動パターン: 固定ローテーション ADVANCED_GAS→BLOAT→SUPER_GAS_BLAST→(BLOATに戻る)のループ（初手ADVANCED_GASのみ一度きり、以降はBLOATとSUPER_GAS_BLASTの往復）
- ギミック: `ShouldFadeAfterDeath=false`（死亡時に通常のフェードアウト演出をしない特殊設定）。BLOAT_MOVEでGasBomb（別ファイル、爆弾型モンスター）を戦闘中に追加召喚する数少ない「召喚」ギミック持ち
- 幕: Underdocks（LivingFogNormal）

## LivingShield (LivingShield.cs)
- HP: Min=Max=55（アセンションToughEnemies時65）
- Move:
  - `SHIELD_SLAM_MOVE`(攻撃): 単体6ダメ（アセンション差なし=固定6）
  - `SMASH_MOVE`(攻撃+バフ): 単体16ダメ（アセンション18）+ Strength+3（アセンション差なし=固定3、毎回発動時に加算）
- 行動パターン: ConditionalBranch「SHIELD_SLAM_BRANCH」で**生存する味方(自分以外)がいる間はSHIELD_SLAMを継続**、**味方が全滅した瞬間からSMASHに切り替わり、以後SMASH→SMASHの自己ループ（毎回Strength+3が乗るため無限に強化され続ける）**
- ギミック: 開幕でRampartPower(装甲/盾役パワー)25を自身に付与——「味方を庇う盾役」で、味方が全員死ぬと役目を終えて自身が怒りの連続強化モードに入る設計。`HasDeathSfx=false`（死亡音を鳴らさない）
- 幕: Glory（弱編成、TurretOperatorWeak。TurretOperatorとペア）

## LouseProgenitor (LouseProgenitor.cs)
- HP: 基本134〜136 / アセンション(ToughEnemies)138〜141
- Move:
  - `WEB_CANNON_MOVE`(攻撃+デバフ): WebDamage(基本9/アセンション(DeadlyEnemies)10)単発 + 対象にFrailPower 2付与。丸まっていた場合(`Curled==true`)は攻撃前にほどける演出が入る
  - `CURL_AND_GROW_MOVE`(防御+バフ): ブロックCurlBlock(基本14/アセンション(ToughEnemies)18)を得る + 自身にStrengthPower+5(固定)。実行後`Curled=true`になる
  - `POUNCE_MOVE`(攻撃): PounceDamage(基本14/アセンション(DeadlyEnemies)16)単発。丸まっていた場合は攻撃前にほどける演出が入る
- 行動パターン: 固定3手ローテーション `WEB_CANNON_MOVE → CURL_AND_GROW_MOVE → POUNCE_MOVE → WEB_CANNON_MOVE …`(初期状態はWEB_CANNON_MOVE、分岐なし)
- ギミック: `AfterAddedToRoom`で`CurlUpPower`をCurlBlockと同じ量(基本14/アセンション18)で自身に付与——StS1のGiant Louseにあった「HP半分でCurl Up」という**HP閾値トリガーはこの実装には見当たらず**、代わりに丸まる(ブロック+Strength)がローテーションに組み込まれた通常Moveとして定期的に発生する設計に変更されている。`Curled`フラグは見た目(丸まり/伸びるアニメーション)の管理のみに使われ、行動選択自体には影響しない
- 幕: Hive(`LouseProgenitorNormal`)。名称からLouse系(StS1 Giant Louseに相当)の始祖個体

---

## 横断的に見えたギミック・構造（統合時の補足）

1. **多パーツ/召喚ボスが複数存在**: Doormaker(扉→真の姿の変身)、Fabricator(Guardbot/Noisebot/Zapbot/Stabbotを召喚)、Fogmog(EyeWithTeethを召喚)、LivingFog(GasBombを召喚)、Crusher(Kaiser Crab Bossの腕パーツ)、KinPriest+KinFollower(相互参照ペア)など、単体で完結しないモンスター設計が多い。
2. **フェーズ変化の実装方式が多様**: HP50%閾値(FrogKnight・LagavulinMatriarch[見た目のみ]・CeremonialBeastは外部スタン依存)、発動回数カウンタ(KnowledgeDemon)、味方の生死(LivingShield)、睡眠パワーの残量(LagavulinMatriarch)、と単純なHP割合以外の分岐条件が豊富。
3. **開幕パワー付与がほぼ全モンスターに存在**: `AfterAddedToRoom`でArtifact・Ritual・Strength・装甲系・特殊スタックパワーを持たせる個体が非常に多く、素のHP/攻撃力だけでなく常在パワーが難易度設計の主軸になっている。
4. **群れ内での個体差別化**: CorpseSlug・Decimillipedeの「開始Moveの位相をずらす」、Exoskeletonの「スロット位置で初手を変える」、Inkletの「Middle個体は初期状態が違う」など、同一モンスターが複数体出現する編成では意図的に初動をずらして単調な同時攻撃を避けている。
5. **Decimillipedeの死亡→自動復活(REATTACH)**、GasBombの自爆(DeathBlowIntent)、FatGremlinの即逃走(EscapeIntent)など、通常の「攻撃/防御/バフ/デバフ」に収まらない特殊Intent型のMoveが複数確認された。
