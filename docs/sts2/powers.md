# Slay the Spire 2 デコンパイル解析 — 状態異常・パワー体系

対象: `MegaCrit.Sts2.Core.Models.Powers`(262ファイル) / `MegaCrit.Sts2.Core.Models.Afflictions`(8ファイル) /
`MegaCrit.Sts2.Core.Models.Cards` 内のステータス・呪いカード。
`Monsters`フォルダ(121体)から実際に参照されているPower名を逆引きして優先度付けした
(`grep -rhoE '[A-Za-z]+Power' Monsters/` で73種、うち実体は70種)。
コードそのものの転載はせず、仕様のみを言語化している。

---

## 0. 実装上の前提(表を読む前に)

- `PowerModel` は `PowerType`(Buff/Debuff) と `PowerStackType`(None/Single/Counter) を持つ。**プレイヤー用と敵用でクラスが分かれておらず、同じPowerクラスをどちらの`Creature`にも付与できる**汎用設計(StS1は多くがキャラ側/敵側で別実装だったのに対し、StS2は「誰が持つか」で分岐するコードが随所にある: 例 `WeakPower`は`dealer != base.Owner`で失敗、`VulnerablePower`は`target != base.Owner`で失敗、のように向き先を明示的にチェックしている)。
- Weak/Vulnerable/Frailの減衰は **`side == CombatSide.Enemy`のターン終了時に一律1減る**実装(誰が保持していてもラウンド境界で減衰。StS1の「自分のターン開始時に減る」と結果的にほぼ同義になる)。
- `IsInstanced`フラグを持つPowerは対象(`Target`)を個別に持てる(1体に対して複数インスタンスが並立可能)。`SwipePower`(カード窃盗)や`ThieveryPower`(金窃盗)、`SandpitPower`など「誰から奪ったか」を覚える系で使われる。
- `AfterCardEnteredCombat` / `AfterApplied` / `AfterRemoved` の3点セットで「デッキ全体のカードに呪いを配って、剥がれたら全部戻す」パターンが8種類のAfflictions配布パワーで共通して使われている(詳細は§4)。これは**StS1に存在しなかった新しいテンプレート**と見てよい。
- `Osty`という単語が多数のPowerから参照される(`player.Osty`, `dealer.PetOwner`など)。プレイヤーに随伴する僚機/使い魔のような第二の戦闘体で、`IntangiblePower`のダメージ上限緩和(`TheBoot`所持時)や`PersonalHivePower`のDazed付与先判定などに絡む。Power/Affliction本体ではないため詳細調査は対象外だが、設計文脈として記録。

---

## 1. プレイヤーに付くデバフ(Weak/Vulnerable/Frail相当 + 新種)

### 1-a. 基幹デバフ(StS1から継続)

| 名前 | 効果 | 減衰・解除ルール | 備考 |
|---|---|---|---|
| **WeakPower**(弱体) | 保持者が与える攻撃ダメージを×0.75(-25%) | Enemyターン終了時に1減衰(Counter) | StS1と同じ倍率。`DebilitatePower`保持者が与えるWeakは効果が強化される(後述) |
| **VulnerablePower**(脆弱) | 保持者が受ける攻撃ダメージを×1.5(+50%) | 同上 | `CrueltyPower`(付与側)/`DebilitatePower`(受け側)で倍率が動的に強化される |
| **FrailPower**(虚弱) | 保持者がカード/敵行動で得るブロックを×0.75(-25%、固定値・DynamicVars化されていない) | 同上 | StS1と同じ25%減 |
| **PoisonPower**(毒) | 自ターン開始時にスタック数ぶんダメージ(ブロック無視)を与え、そのたびに1減少 | 0で自然消滅 | StS1同様。**`AccelerantPower`**(味方の毒スタック合計ぶん)保持者がいると1ターンに複数回トリガーする拡張あり(★新の増幅器) |
| **ConfusedPower**(混乱) | カードをドローするたび、そのカードのコストを0〜3のランダム値に上書き | Single(効果自体に期限なし、外部で除去) | StS1のSnecko系と同一仕様 |
| **IntangiblePower**(無敵/実体なし) | 保持者が受けるダメージを1点(`TheBoot`所持プレイヤーが与える場合は5点)にキャップ | Enemyターン終了時にCounterで減衰 | StS1と同一効果。上限を動かす専用レリック`TheBoot`が存在 |
| **BarricadePower**(バリケード) | ブロックがターン開始時にクリアされなくなる | Single、明示除去まで継続 | StS1と同一。敵側にも付与可能な汎用実装 |
| **BufferPower**(バッファー) | 「HPを失う」処理そのものをN回無効化(ダメージ0化ではなく損失前提のHPロスをキャンセル) | ヒットごとに1減衰 | StS1と同一の「次のダメージを何回か無効」系 |
| **FlameBarrierPower**(火の障壁) | 攻撃で被弾するたび、攻撃者に固定ダメージ反射 | 自分のターンが終わったら除去(=1ターン限定) | StS1と同一。Thornsとの違いは「持続時間限定の反射」 |
| **CorruptionPower**(堕落) | スキルのコストが0になり、使用後は必ず消滅送りになる | Single | StS1のアイアンクラッドPowerと同一実装 |
| **EnvenomPower**(エンヴェノム) | 保持者の攻撃が命中するたび対象に毒を付与 | Counter(付与毒量として振る舞う) | StS1のサイレント固有Powerと同一 |
| **FocusPower**(集中) | オーブの効果値を±N | AllowNegative | StS1のDefect固有Powerと同一(オーブ系はStS2にも継続) |

### 1-b. 新種の重量デバフ(★新)

| 名前 | 効果 | 減衰・解除 | ★新の理由 |
|---|---|---|---|
| **NoDrawPower** | このターン、手札ドローを一切禁止(ドロー枚数を強制0) | 次のターン終了時に自動除去(1ターン限定の劇薬デバフ) | StS1に同種の「今ターンのドロー全禁止」単体Powerは無い(Entangleはコスト増、Confusedはコスト変化のみ) |
| **NoEnergyGainPower** | 次に得るはずのエナジー獲得量を強制0に | 同上、1ターン限定 | 同上 |
| **NoBlockPower** | カード由来のブロック獲得を完全に0倍化(Unpowered/効果由来ブロックは対象外) | Counterで数ターン継続 | Frailの「-25%」と違い**ブロックを完全ゼロ化**する上位互換的デバフ。StS1未確認 |
| **SlowPower**(累積被ダメ増) | 自分のターンにカードをプレイするたび内部カウンタ+1し、被弾ダメージを+10%×カウンタで増加。次の自ターン開始でカウンタリセット | ターンごとに自動リセット(恒久デバフではなく毎ターン仕切り直し) | 「手数を出すほど脆くなる」というプレイヤー行動連動の新型デバフ。StS1に同名の一般Powerは確認できず |
| **MindRotPower** | ドロー枚数を-Nする(WeakPowerのドロー版) | Counter | ステータスカード`MindRot`からのみ付与される特殊効果。§3のKnowledgeDemon参照 |
| **WasteAwayPower** | 最大エナジーを-Nする | Counter、恒久 | ステータスカード`WasteAway`専用。エナジー版Frail |
| **SlothPower** | 1ターンにプレイ可能なカード枚数をN枚に制限(超過分は自動でプレイ不可扱い) | ターンごとに使用数リセット、Power自体はCounterで持続 | ステータスカード`Sloth`専用 |
| **DebilitatePower**(弱体化の型) | **他のデバフを増幅する「メタ効果」**。保持者にWeakが掛かる時さらに弱体化/保持者が与えるVulnerable倍率をさらに強化(2倍のVulnerableなら3倍相当に、0.75のWeakなら0.5相当に、という加算式) | 自ターン終了時にCounter減衰 | StS1に同型のPower増幅器は無い。**Weak/Vulnerableの効果式に外部フックが刺さる設計**自体が新しい |
| **CrueltyPower**(付与側) | 保持者が与えるVulnerable効果の倍率にさらに+N%を加算(Vulnerableを付与する側のバフ) | Counter | Debilitateと対になる「付与側の増幅バフ」。★新 |
| **AccelerantPower**(触媒) | 前述の通り、味方の毒トリガー回数を1ターンに複数回に増やす | Counter | 毒のダメージ量ではなく**発動回数**を増やす設計。StS1未確認 |
| **DoomPower**(処刑閾値) | 自ターン終了時、HPが「Doomの値」以下なら即死扱いで処理(通常の与ダメではなく専用の即死シーケンス`DoomKill`) | Counter(閾値そのものが数値) | StS1に同型の「閾値以下で自動処刑」汎用Powerは無い。処刑札(`とどめの一撃`等)とは別の"継続する処刑ライン"型デバフ |

---

## 2. 敵が自分に付けるバフ・ギミックパワー(Strength/Artifact/Thorns/Regen相当 + 新種)

### 2-a. 基幹バフ(StS1から継続、プレイヤー/敵どちらにも付与可能な汎用実装)

| 名前 | 効果 |
|---|---|
| **StrengthPower**(筋力) | 与える攻撃ダメージに加算(AllowNegative=True、マイナス化=脱力扱いも可) |
| **DexterityPower**(敏捷) | カード/敵行動由来のブロック獲得に加算(こちらもAllowNegative) |
| **ArtifactPower**(アーティファクト) | 次に受けるデバフ付与をN回無効化(可視デバフのみ対象。付与ごとにCounterが1減る) |
| **ThornsPower**(棘) | 攻撃を受けるたび攻撃者に固定反射ダメージ |
| **RegenPower**(再生) | 自ターン終了時にAmountぶん回復し、Counterが1減る(StS1のRegenerateと同一) |
| **RitualPower**(儀式) | 自ターン終了時にStrengthを+Amount(**ただし直前の敵ターンに"付与された"直後は1回だけスキップ**——`_wasJustAppliedByEnemy`フラグで、付与直後即座に発動する「無償の初回トリガー」を防ぐガードが入っている) |

### 2-b. 新種の敵専用ギミック(★新、個体名から見て新モンスター専用の演出フック多数)

| 名前 | 効果概要 | 想定モンスター/文脈 |
|---|---|---|
| **AsleepPower**(睡眠) | 被弾しても目覚めないが、実ダメージを受けた瞬間に覚醒し行動を差し込む。ターン経過でCounterが0になると自動覚醒 | `LagavulinMatriarch`(StS1のLagavulinの後継種と思われる) |
| **CurlUpPower**(丸まる) | 特定の1枚から初めてダメージを受けたターンにブロックを得て自壊 | ダンゴムシ系(Louse系統) — StS1のCurl Upと概ね同一だが「同一カード限定」判定が追加 |
| **BurrowedPower**(潜伏) | ブロックが尽きるまで攻撃を受け付けない実質シールド。ブロックが割れた瞬間に潜行攻撃(Bite)へ移行 | 地中生物系 |
| **ReattachPower**(再接続) | 同系統の他セグメントが全滅していなければ、瀕死になっても離脱→回復して復帰する | `Decimillipede`(ムカデ型、セグメント制の多節モンスター) |
| **AdaptablePower**(適応/蘇生) | 死亡時に一度だけ蘇生シーケンスへ移行し、全回復して復帰 | `TestSubject` |
| **InfestedPower**(寄生) | 死亡時、スタン済みの子(`Wriggler`)を4体その場に召喚。戦闘終了を一時ブロック | `PhrogParasiteElite` — 分裂に近い「死亡トリガー召喚」型 |
| **StockPower**(在庫) | 死亡時、在庫数(Amount)が残っていれば同種の下位モンスターを1体その場に再生成(在庫を1消費) | `Axebot` — 疑似的な「量産・補充」ギミック |
| **SteamEruptionPower** | 死亡時に専用の「今にも爆発する」状態へ移行し戦闘終了を一時ブロック | `WaterfallGiant` |
| **SandpitPower**(蟻地獄) | 対象を毎ターン自分側へ引き寄せ、Amountが尽きると対象を強制ステージアウト(実質即死級の拘束+処刑演出) | `TheInsatiable` — 「距離」を持つ珍しいPower |
| **IllusionPower**(幻影/擬態蘇生) | 死亡時に1回だけ蘇生ステートへ移行し、フォローアップ行動を挟んで全回復 | `Parafright`等のホログラム系 |
| **RavenousPower**(貪食) | 味方が死ぬと自分をスタンさせて「捕食」演出を行い、その後Strengthを獲得 | `CorpseSlug` |
| **CrabRagePower**(蟹の怒り) | 味方が死ぬとStrength+ブロックを得て自壊(1回限りの弔い強化) | 蟹系モンスター |
| **StrengthPower関連の増幅トリガー群** | **EnragePower**(スキル被使用でStrength上昇=StS1 Gremlin Wizard型)/**HighVoltagePower**(自ターン終了時に無条件でStrength上昇)/**TerritorialPower**(自ターン終了時に無条件でStrength上昇、Enrageと似た別実装)/**SuckPower**(攻撃が複数体に命中するたび命中数ぶんStrength上昇)/**VigorPower**(1回の攻撃コマンド中だけダメージ加算、攻撃終了で自動剥離=StS1 Vigorの一撃限定ダメージ加算と同型) | 各種 |
| **PossessStrengthPower / PossessSpeedPower**(強奪) | プレイヤーのStrength/Dexterityが減少した量を記憶しておき、自分の死亡時にその分をプレイヤーへ丸ごと返す(=一時的に"奪って"いた筋力/敏捷を解放) | ゴースト/憑依系と思われる |
| **ImbalancedPower**(バランス崩し) | 自分の攻撃が完全ブロックされるとスタン(反動で隙が生まれる) | `BowlbugRock`等 |
| **PlowPower**(突進の反動) | 被弾しHPが閾値以下になると、蓄積したStrengthを全て失いスタンする | `CeremonialBeast` |
| **ShriekPower**(悲鳴) | HPが閾値以下でダメージを受けると恐慌ステートに移行し自壊(Powerとしては消滅) | `TerrorEel` |
| **ShrinkPower**(縮小) | 保持者の**与ダメージ**を-30%(固定・DynamicVars化はされているが既定30固定)。付与者が死ぬと解除。無限スタック(負値)にも対応 | 敵に対して使う"矮小化"系の呪い。効果対象は保持者自身の攻撃力なので、実質「敵に掛ける弱体化バフ風デバフ」 |
| **SkittishPower**(臆病) | 攻撃で被弾した最初の1回だけブロックを得て、ターン終了で解除(触角を引っ込める演出) | `PhantasmalGardeners` |
| **SlumberPower**(半覚醒) | 被弾するたびCounterが減り、0になると覚醒して強攻撃に移行(ターン経過でも自然減衰) | `SlumberingBeetle` |
| **FlutterPower / SoarPower**(飛行回避) | 被弾ダメージを50%カット。Flutterは被弾のたびCounterが減り0でスタン(飛行→墜落の演出)、Soarは固定Single | `ThievingHopper`等の飛行系 |
| **SurprisePower**(奇襲・不意打ち) | 死亡時に増援(`SneakyGremlin`/`FatGremlin`)を召喚し、盗んでいたゴールドを引き継がせる | ゴブリン系 |
| **SurroundedPower / BackAttackLeft・RightPower**(挟撃) | 「背面攻撃」フラグを持つ味方から攻撃されると被ダメ+50%。プレイヤーの対象選択に応じて自動的に向きを切り替える | 複数体編成の位置取りギミック |
| **HardToKillPower**(打たれ強さ) | 1ヒットあたりの被ダメージ上限をAmountに固定キャップ(**このゲームの「ヒットごとの被ダメ上限」= 現行プロトの`armor`（装甲）とほぼ同じ発想**) | — |
| **HardenedShellPower**(硬化した殻) | 1ターンあたりの被ダメ累計に上限を設け、超過分をカット。ターン開始でリセット | — |
| **RampartPower**(城壁) | 自ターン開始時、味方の`TurretOperator`にブロックを配る支援型 | 砲台編成 |
| **VitalSparkPower**(活火花/カウンター給餌) | 攻撃してきたプレイヤーにこちらから毎ターン1回だけエナジーを付与(=「殴らせて得させる」逆説的ギミック) | — |
| **PersonalHivePower**(私設の巣) | 被弾するたび、攻撃者(プレイヤー)の**山札に直接**`Dazed`カードを挿入する。Thornsのダメージ版ではなくカード呪いを送りつける反撃 | 巣/蜂系。**「呪い付与などの器」の代表例** |
| **ThieveryPower / HeistPower**(窃盗) | Thievery=毎ターン対象のゴールドをAmountまで奪って自分の中に貯蔵/Heist=死亡時に貯めたゴールドをそのまま報酬化(=倒せば取り返せる) | ゴブリン系 |
| **SwipePower**(カード強奪) | プレイヤーの手札からカードを1枚奪って保持し、自分が死亡すると奪ったカードがプレイヤーへ特別報酬として返る | `ThievingHopper` |
| **MinionPower**(雑魚指定) | 「二次的な敵」フラグ。死亡してもFatal判定(戦闘終了条件)に数えない=護衛・取り巻き専用の存在指定 | 召喚された子分共通 |
| **NemesisPower**(因縁) | 自ターン終了ごとにIntangibleの付与/剥奪を交互に繰り返す(1ターンおきに無敵化する変則パターン) | — |
| **HatchPower**(孵化までのタイマー) | ターン経過でCounterが減るだけの単純タイマー(孵化演出のトリガー元と思われる) | 卵系 |
| **BattlewornDummyTimeLimitPower** | 制限ターン内に倒されないと`RanOutOfTime`フラグを立てて戦闘から離脱(イベント戦専用の時間制限) | イベント「使い古しの案山子」系 |

---

## 3. ステータスカード/呪いカード(使用不可札)

いずれも `CardType.Status` または `CardType.Curse`。共通して`CardKeyword.Unplayable`(使用不可)が付き、
**手札に残ったままターンを終えると発火する**専用フック `HasTurnEndInHandEffect` / `OnTurnEndInHand` を持つものが多い
(StS1にはこの「手札に居座るだけで毎ターン発火する」共通APIは無く、カードごとに個別実装されていた——**設計の一般化という意味で★新**)。

### 3-a. ステータスカード(`CardRarity.Status`、戦闘限定生成)

| 名前 | プレイ可否 | 効果 | 消滅/捨て札挙動 |
|---|---|---|---|
| **Dazed**(困惑) | 不可(Unplayable+Ethereal) | 何もしない完全なデッドカード | Ethereal=ターン終了時に自動消滅 |
| **Wound**(傷) | 不可(Unplayable) | 何もしない完全なデッドカード | Etherealなし=捨て札行き。StS1のWoundと同一 |
| **Burn**(火傷) | 不可 | 手札に残ったままターンを終えると保持者に2ダメージ | 消滅なし(捨て札を回り続ける)。StS1と同一 |
| **Soot**(煤) | 不可(Unplayable) | 何もしない(現状はDazed同等の空撃ち札) | Etherealなし |
| **Slimed**(粘液) | 可(コスト1) | プレイすると1ドロー、消滅 | Exhaustキーワード付き。StS1と同一 |
| **Void**(虚無) | 不可(Unplayable+Ethereal) | **ドローされた瞬間**にエナジーを1失う(手札に来るだけで損をする) | Ethereal自動消滅。StS1と同一挙動 |
| **Toxic**(有毒) | 不可 | 手札に残ったままターン終了で5ダメージ、その後Exhaust | Burnより重いダメージ版・使い切り |
| **Debris**(瓦礫) | 不可(挙動上プレイ不能に近いが実装はOnPlayが空処理) | Exhaustキーワードのみ | ほぼ空撃ち専用 |
| **Infection**(感染) | 不可 | 手札に残ったままターン終了で3ダメージ(専用ビジュアルオーバーレイ付き) | 消滅なし(捨て札を回り続ける) |
| **Beckon**(誘い) | 不可 | 手札に残ったままターン終了で6ダメージ(=Burnより重い) | 消滅なし |
| **RocketPunch**(ロケットパンチ) | 可(コスト2) | 13ダメージ+1ドロー。**ステータスカードとして生成された場合のみコスト0で配布される**特殊カード(生成元の状況で挙動を変える) | 通常カード寄りの"当たり"枠 |
| **FlakCannon**(対空砲) | 可(コスト2、レア) | 手札/山札上のステータスカードを全消滅させ、その消滅数ぶんランダム多段攻撃(8ダメ×N) | ステータスカードを"弾薬"に変える対抗策カード |
| **Compact**(圧縮) | 可(コスト1、アンコモン) | 6ブロック+手札上の変換可能なステータスカードを`Fuel`カードへ全変換 | ステータス浄化系 |
| **MindRot / WasteAway / Sloth / Disintegration** | 不可(選択専用) | プレイでなく**選択(`OnChosen`)された時点**で対応するデバフPower(§1-b)を自分に付与する特殊ステータス。`KnowledgeDemon.IChoosable`インターフェースを実装しており、**手札に配られる通常のステータスカードではなく「複数の呪いカードから1つを選ばされる」専用イベント/モンスター(`KnowledgeDemon`)専用の選択肢**と判明 | 戦闘生成不可(`CanBeGeneratedInCombat=false`) |
| **FranticEscape**(逃走) | 可(コスト1、自己対象) | プレイすると`SandpitPower`(§2-b)の蓄積を+1進め、以後このカード自体のコストが+1される(蟻地獄から逃げるほど疲弊する専用カード) | `TheInsatiable`専用 |

### 3-b. 呪いカード(`CardRarity.Curse`、ラン永続)

| 名前 | 効果 | 特徴 |
|---|---|---|
| **AscendersBane** | 何もしない(Eternal+Unplayable+Ethereal) | StS1と同一の「昇天の罰」 |
| **CurseOfTheBell** | 何もしない(Eternal+Unplayable) | StS1と同一 |
| **Greed** | 何もしない(Eternal+Unplayable) | StS1同名と同一 |
| **Injury**(負傷) | 何もしない(Unplayable) | StS1のInjuryと同一だが**ラン永続の呪いカードとして再配置**(StS1ではSlay枠のステータス扱いだった記憶と異なりCurseレアリティ) |
| **Decay**(腐敗) | 手札に残ったまま終了で2ダメージ | StS1と同一の毎ターンダメージ型 |
| **Doubt**(疑念) | 手札に残ったまま終了でWeak+1を付与(既にWeak持ちなら次の減衰を1回スキップ) | ★新: Weak付与型の呪い。StS1未確認 |
| **Shame**(恥) | 同様の仕組みでFrail+1を付与 | ★新: Frail付与型の呪い |
| **BadLuck**(不運) | 手札に残ったまま終了で13ダメージ(Eternal=毎回配り直され続ける) | 高火力の恒久呪い |
| **Regret**(後悔) | 自ターン終了時、**その時点の手札枚数ぶん**のダメージ | StS1のRegretと同一(手札枚数依存ダメージ) |
| **Debt**(借金) | 手札に残ったまま終了でゴールドをN失う | ★新: ゴールドを削る呪い。StS1未確認 |
| **Normality**(正常性) | このターン3枚目以降のカードプレイを禁止する常在ロック | StS1のNormalityと概ね同一(1ターンのプレイ数制限) |
| **Guilty**(罪悪感) | デッキに残ったまま5戦闘を経過すると自動的にデッキから消える(タイマー付き呪い) | ★新: 「一定戦闘数で自然消滅する」呪いはStS1未確認 |
| **PoorSleep**(寝不足) | 何もしない(Unplayable+Retain=手札保持されたまま溜まり続ける) | Retain付き呪いはStS1未確認、★新寄り |
| **Clumsy**(不器用) | 何もしない(Unplayable+Ethereal) | Etherealで自然消滅する呪いという逆説的な軽量呪い |
| **Writhe / Folly** | 何もしない(Innate等の特殊キーワード付き、Follyは4キーワード全部盛り) | 初手固定(Innate)で必ず来る重量級デッドカードと思われる |
| **Enthralled**(魅了) | 手札にある間、**このカード自身以外は自動プレイ強制が掛からない程度の弱いロック**(`ShouldPlay`で自分自身以外のプレイを妨げない実装。ほぼInjuryの上位互換に近い空撃ち札) | Eternal |
| **SporeMind**(胞子の心) | Exhaustのみ(効果はプレイ即消滅) | 詳細な追加効果は本ファイル未確認(データ側の可能性) |

---

## 4. Afflictions(新システム) ★新(システムまるごと新規)

**「カード1枚1枚に個別の呪い状態を付与する」新レイヤー。** 敵のPowerが戦闘中ずっと働き続け、
デッキ全体のカードへ**リアルタイムに**Afflictionを配って回る、という設計になっている
(StS1にはカード単位の持続的呪い状態というレイヤー自体が存在しなかった)。

共通の配布パターン(8種すべてで反復):
1. 該当の敵Powerが `AfterApplied`(初回付与時)で、対象プレイヤーの手持ち全カードに一括でAfflictionを配る
2. `AfterCardEnteredCombat`(新しく生成/ドローで戦闘に加わったカード)にも同じAfflictionを自動追加
3. 敵Powerが除去される/敵が死亡すると `AfterRemoved` で該当Afflictionを**デッキ全体から一括撤去**する

| Affliction名 | 何が起きるか | 配布元Power | スタック可否 |
|---|---|---|---|
| **Bound**(束縛) | このターン中に「束縛」カードを1枚プレイすると、以後同種の束縛カードはプレイ禁止になる(1ターン1枚制限を強制) | `ChainsOfBindingPower` | — |
| **Devoured**(捕食) | 対象カードに`Exhaust`キーワードを追加(=プレイしたら消滅する使い捨てに変わる) | `HungerPower` | — |
| **Entangled**(絡み付き) | 対象(攻撃カードのみ)のコストを+Amountする | `TangledPower` | — |
| **Galvanized**(帯電) | 対象(パワーカードのみ)をプレイすると保持者自身にAmountダメージが入る(強いカードほど自傷の代償を払う) | `GalvanicPower` | 可(スタック蓄積) |
| **Hexed**(呪縛) | 対象カードに`Ethereal`キーワードを追加(=使わないと手札から消える圧を全カードにかける) | `HexPower` | — |
| **Ringing**(耳鳴り) | このターン中1枚もカードをプレイしていない場合のみプレイ可能(=最初の1枚しか切らせない足止め)。**このデバフPower自体は1ターンで自動消滅**する使い切り拘束 | `RingingPower` | — |
| **Smog**(煙幕) | 自分がスキルをプレイした直後、他の未使用スキル全てに即座に付与され、そのターンはスキルを二度と使えなくする(=「1ターン1スキルまで」を後追いで強制) | `SmoggyPower` | — |
| **Weighted**(加重) | プレイ時に追加でエナジーを消費させる(カードのコストとは別枠で二重に払わせる) | `GraspPower` | — |

補足:
- `Devoured`/`Hexed`は「付与したキーワード(Exhaust/Ethereal)を、Affliction解除時に自分で剥がす」後始末ロジックまで実装されている(`AppliedExhaust`/`AppliedEthereal`フラグで、元からそのキーワードを持っていたカードには触れない差分管理)。
- `CanAfflictCardType`のようなオーバーライドで「Attack/Skillのみ対象」「Powerカードのみ対象」など**対象カード種別をAffliction側が自己申告する**設計になっており、配布元のPowerは対象種別を気にせず`AfterCardEnteredCombat`を呼ぶだけでよい(責務分離)。

---

## 5. ★新まとめ

新規と判定した項目は**32件**(デバフ本体・増幅器11、敵ギミック多数のうち明確に新規パターンと呼べるもの約10、呪いカード6、Afflictionsシステム丸ごと1 + 個別8種)。
代表5つ:

1. **Afflictions システムそのもの**(§4) — カード1枚単位に持続する呪い状態を敵Powerが自動配布/自動回収する新レイヤー。StS1に無かった最大の新機構。
2. **DebilitatePower / CrueltyPower**(§1-b) — Weak/Vulnerableの効果量そのものを動的に増幅する「デバフのメタ強化」専用Power。StS1では固定倍率だった基幹デバフに外部フックが刺さるようになった。
3. **DoomPower**(§1-b) — 「HPが閾値以下ならターン終了時に強制処刑」という継続型の処刑ライン。StS1の一撃処刑カードとは別に、常時監視型の即死デバフが存在する。
4. **PersonalHivePower**(§2-b) — 被弾すると反射ダメージではなく**攻撃者の山札にDazedを直接挿入する**という、ダメージ以外の「カードで仕返す」反撃ギミック。
5. **NoDrawPower / NoEnergyGainPower / NoBlockPower**(§1-b) — ターン単位でリソース(ドロー/エナジー/ブロック)を丸ごと0化する劇薬デバフ群。StS1のFrail(-25%)より踏み込んだ「完全ゼロ化」を採用している。

---

出力ファイル: 本ファイル (`extract-powers.md`)
