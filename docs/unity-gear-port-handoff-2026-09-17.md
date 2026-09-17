# Unity 版ギア移植の引き継ぎ書（2026-09-17）

ギア（消耗品）は TS エンジン・ブラウザ版・CLI で実装済み（`e190c35`〜`d005f84`）。**C# エンジンと Unity の画面は未対応**。
このセッション（クラウド）は dotnet の配布元がプロキシで遮断され Unity も無いので、C# のコンパイルとゴールデン照合を回せない。
ユーザー裁定「ローカルのセッションが引き継げるようにしてほしい。実装は進めないでオーケー」に従い、
**調査の結果と裁定と作業手順を残す**。ローカル（WSL＋Windows の Unity）のセッションはこの文書から着手する。

## 0. このセッションで済ませたこと／済ませていないこと

済:
- `npm run gen:csharp` を実行して `unity/Packages/com.deckrogue.engine/Runtime/Generated/Types.g.cs` を再生成した（GearDef・GearInstance・ShopStateGears・
  RunState の gears/mana/gearPity/seenGearIds/gearOption・GameState の gearUsedThisTurn/retainHandThisTurn/energyCarryThisTurn/gearDeathSave・
  EnemyState の actionNegated/summonBlocked/interruptBlocked・GameEvent_GearUsed・GameEvent_DeathSaved.Source・RunCommand の UseGear/TakeGear/SkipGear/DiscardGear/ShopBuyGear/ShopBuyMana）。
  record が増えただけなので既存の C# は壊れない（未コンパイル。手で編集しない）。
- `npm run unity:sync` で `src/data/gears.json` を `unity/Assets/StreamingAssets/data/` と `unity/Assets/Resources/Data/` に複製した（C# はまだ読まない）。
- TS 側の基線: `npm run goldens:verify` 13本一致・`npm test` 956件緑（2026-09-17 11:34）。

未: 下の §2（C# エンジン）と §3（Unity の画面）の全部。

ユーザー裁定（2026-09-17 ask_user 4件）:
1. 進め方＝**このセッションでは実装を進めず、引き継ぎ書を残す**（この文書）。
2. **ショップのギアの棚＝右の「店主のサービス」欄に「ギアの棚」ボタンを足し、除去/鍛えると同じ下位モードの専用画面**（3枠＋魔素の購入＋持ち物の整理＝捨てる/入れ替え。PC/スマホ共通）。
3. **煙玉は確認ダイアログを挟む**（「この戦闘から逃げる？（報酬は得られない）」。放棄と同じ朱のボタン＝`ConfirmBox{Danger=true}`）。
4. **組んだ時の演出はからくりと同じ**（舞台の匣の蓋が開いて閃く `Stage.SetKarakuriBox`＋トークンが跳ねて対象へ飛ぶ＋判「組んだ」＋光の輪。効果の数字は既存の浮き文字）。

## 1. 一次資料（読む順）

1. `CLAUDE.md` の「ギア（消耗品）」行（骨格・数値の裁定・答え合わせ）。
2. `docs/parts-proposal-2026-09-17.md` §2〜§4.5（台帳33種・供給）と §6-2（置き場＝案A「匣の帯」の裁定文）。デザインカンバスは `docs/design/gear-placement/`（`Main`＝案A スマホ／`AWindow`＝使う時の窓／`APC`＝PC／`Reward`＝報酬画面）。
3. TS の実体: `src/engine/gears.ts`（311行・全部読む）と、差分 `git diff ee7d735..d005f84 -- src/engine/run.ts src/engine/combat.ts src/engine/effects.ts src/engine/content.ts src/engine/types.ts src/ui/report.ts src/ui/log.ts`。
   ブラウザ版の画面は `git diff ee7d735..d005f84 -- src/ui/App.tsx`（GearBar／報酬のギア枠／ショップの棚）、CLI の文言は同 `src/sim/play.ts`。
4. 機械固定: `src/engine/gears.test.ts`（31件）が仕様の写し。C# の挙動はこれとゴールデンで確かめる。
5. 移植の規約: `unity/PORTING.md`（1行ずつ忠実・RNG の消費順・イベントの順）。

## 2. C# エンジンへの追随（`unity/Packages/com.deckrogue.engine/Runtime/`）

ゴールデン13本はギアの抽選で RNG を消費する手（報酬・店・チェックポイント）と `TakeGear` コマンドを含むので、
**§2 を全部写して初めて `dotnet run -- verify` が通る**（今は `未知のランコマンド: TakeGear` で全本落ちるはず）。

### 2.1 Content.cs

- `AllGears`（`IReadOnlyList<GearDef>`）と `GetGearDef(id)`（未定義は `InvalidOperationException("未定義のギア: " + id)`）。
- `LoadFrom` に `AllGears = ParseList<GearDef>(read("gears.json"), "gears.json");` と `Reindex(GearIndex, AllGears, g => g.Id);`。
- `EngineTests/Program.cs` の「データ読込:」の行にギアの数を足す（任意）。

### 2.2 Gears.cs（新規。TS `gears.ts` の写し。静的クラス `Gears`）

| TS | C# | 備考 |
|---|---|---|
| `GEAR_CARRY_MAX=10` `MANA_MAX=50` `GEAR_MANA_COST=10` `GEAR_RARITY_WEIGHTS` | `public const int` | 単位10の裁定（2026-09-17）。 |
| `makeGear(gearId, uid)` | `MakeGear` | `Charges = def.Charges ?? 1` |
| `gearNeeds(def)` | `GearNeeds` | UI が入力を集める。`target`/`card`(hand/discard/draw/null)/`gear`(special=="nameless") |
| `gearBlockedReason(state|null, mana, gear)` | `GearBlockedReason` | 理由の文言をそのまま: 魔素がない／使い切っている／戦闘中でない／自分の番ではない／敵の番には使えない／このターンはもう組んだ。判定順も同じ。 |
| `gearCardChoices(state, def)` | `GearCardChoices` | hand: `upgradeInHand` を持つ札（砥ぎ油）だけ `Upgrade.CanUpgradeInHand` で絞る／discard: そのまま／draw: **名前順に並べ替えて見せる**（表示専用。TS は `localeCompare('ja')`。C# は `string.Compare(a,b,StringComparison.Ordinal)` でよい＝RNG に触れない）。 |
| `gearLiveDamage(state, def, targetIndex?)` | `GearLiveDamage` | `Effects.PlayerDamageAfterModifiers` と `Effects.DamageBreakdownOf(state, i, amount, pierce, applyExpose:true, withMomentum:false)`。文言「実際に与える値: 10→敵0:7（成長+2・急所・装甲・敵ブロック込み。勢いは乗らない）」 |
| `manaLabel(mana)` | `ManaLabel` | `"25/50（あと2個）"` |
| `gearNoEffectReason(state, def, targetIndex?)` | `GearNoEffectReason` | 効果ごとの空振り理由（blockEnemySummon＝`def.SplitInto/HatchInto/moves.kind=="summon"` のどれも無い／blockEnemyInterrupt＝`def.Interrupts` から `FiredInterrupts` の添字を除いて残り0／clearEnemyStrength＝筋力0以下／shatterBlock＝ブロック0かつ殻なし／cleanseStatuses／purgeHandStatus＝`status_wound/scald/junk/brand/guilt`／gainHpRatio・gainHp＝HP満タン／retrieveFromDiscard＝捨て札なし／searchDeck＝山札なし）。対象未定（2体以上で targetIndex 省略）は null。 |
| `resolveGear(state, def, opts)` | `ResolveGear(GameState, GearDef, UseGearOptions)` | ①先頭で `GameEvent_GearUsed{GearId, Name}` を emit ②`flee` は emit だけして返す ③`nameless` は `AsGearId` を `SeenGearIds` で検証して中身の def で再帰（中身が nameless なら throw） ④対象: `NeedsTarget` なら生存1体は自動・倒れた敵は throw／不要なら生存先頭（無ければ0） ⑤効果ごと: `retrieveFromDiscard`/`searchDeck` → `MoveChosenCard`、`upgradeInHand` → `UpgradeChosenCard`、`copyCardInHand` → `CopyChosenCard`、`transformInHand` → `TransformChosenCard`、それ以外は `Effects.ResolveEffectTargeted(s, effect, target)` ⑥最後に `Combat.CheckCombatEnd(s)`（火薬で全滅→勝利・分裂・弔い・仲間の死亡割り込みがその場で起きる）。 |
| `moveChosenCard` | `MoveChosenCard` | 山が空なら無変化。`cardUid` 省略は先頭。見つからなければ throw `"{name}: 選んだ札が見つからない"`。手札の末尾に足す。 |
| `upgradeChosenCard` | `UpgradeChosenCard` | 候補＝`CanUpgradeInHand` の手札。`Upgrade.UpgradeCard` を手札のその1枚に。 |
| `copyChosenCard` | `CopyChosenCard` | コピーの uid は `{uid}_copy{hand.Count}`・`Token=true`。 |
| `transformChosenCard` | `TransformChosenCard` | **RNG を1回消費**: `base = id の末尾 "+" を落とした id`、`src = AllCards.Find(base) ?? card.Def`、`pool = AllCards.Where(color 同じ && (rarity??"common") 同じ && id != src.Id)`、空なら無変化、`(i, rng) = Rng.NextInt(state.Rng, 0, pool.Count-1)`、uid `{uid}_morph`・`Token=true`。 |

`UseGearOptions`: `TargetIndex?`, `CardUid?`, `AsGearId?`, `SeenGearIds`（run 層が渡す）。

### 2.3 Effects.cs

`ResolveEffect` の switch に14ケースを足す（default は `NotImplementedException` なので、足し漏れは C# で落ちる）:

- `gainHpRatio`: `HealPlayer(state, Math.Max(1, (int)Math.Floor(maxHp * amount / 100.0)), enemyIndex)`。
- `negateEnemyAction`: 対象（生存）に `ActionNegated = true`。
- `blockEnemySummon`: `SummonBlocked = true`。
- `blockEnemyInterrupt`: `InterruptBlocked = true`。
- `cleanseStatuses`: 全部0なら無変化、そうでなければ weak/vulnerable/frail/restrain/mist/slow を 0。
- `purgeHandStatus`: 手札の `status_wound/status_scald/status_junk/status_brand/status_guilt` を消滅置き場へ移し、`FireExhaustTriggers(s, purged.Count, enemyIndex)`（亡骸・onCardExhausted は鳴る）。
- `gainArtifact`: `Player.Artifact = (Artifact ?? 0) + amount`。
- `redrawHand`: 手札0なら無変化。手札を全部捨て札の末尾へ・`ImpulseUids` を空に・同じ枚数 `DrawCards`。
- `clearEnemyStrength`: 対象の筋力が >0 なら 0 に。`RefreshIntentValues` を通す（宣言済みの実値を引き直す）。
- `retainHandOnce` → `RetainHandThisTurn = true`／`energyCarryOnce` → `EnergyCarryThisTurn = true`／`gainDeathSaveOne` → `GearDeathSave = true`。
- `copyCardInHand`・`transformInHand`: `return state`（Gears.cs が解決する）。
- `drawCardsNextTurn` は既存のまま（過負荷の歯車は負の量を積む。StartPlayerTurn 側で 0 に下げ止める＝§2.4）。

`ApplyInterrupts` の先頭（`e == null || e.Hp <= 0` の次）に `if (e.InterruptBlocked == true) return state;`。

### 2.4 Combat.cs

- `StartPlayerTurn`: `EnemyPhase` と一緒に `GearUsedThisTurn = null`・`RetainHandThisTurn = null`・`EnergyCarryThisTurn = null` を落とす。落とす前に `bool carryOnce = state.EnergyCarryThisTurn == true` を読み、
  `Energy = EnergyMax + ((EnergyCarry == true || carryOnce) && turn > 1 ? Player.Energy : 0) + (nextTurnEnergy ?? 0)`。
  ドローは `Math.Max(0, (霞み計算) + (nextTurnDraw ?? 0))`（過負荷の歯車の −2）。
- `ProcessSplits`: `splitInto != null` の後、`e.SummonBlocked == true` なら `Split=true, SummonBlocked=false` だけ立てて `continue`（イベントも出ない）。
- `CheckCombatEnd`: `Player.Hp <= 0` の分岐を **`GearDeathSave == true` を先に**: `GearDeathSave=false, Hp=1` にして `GameEvent_DeathSaved{Hp=1, Source="gear"}`。次に既存の蜥蜴の尾（`Source="relic"` を付ける）。
- `ExecuteEnemyAction`: 「倒れた／意図なし」の早期 return の直後に
  `if (enemy.ActionNegated == true) state = state with { NegateNextAction = true, Enemies = (その敵の ActionNegated=false) };` を挟み、以降は既存の `NegateNextAction` 配管に落とす。
- `Hatch`／`Summon` の case: `HatchInto`／`spawn` の null チェックの直後に `enemy.SummonBlocked == true` なら旗を消して `markResolved(state, 0)`（何も出さない）。
- `FinishEnemyPhase` の `Keeps`: `retainAll = s.RetainHand == true || s.RetainHandThisTurn == true`。

### 2.5 Run.cs

定数（TS と同値）: `GEAR_DROP_BASE=60` `GEAR_DROP_PITY=10` `MANA_PER_WIN=5` `MANA_PER_ELITE_BOSS=10` `SHOP_GEAR_SLOTS=3` `SHOP_GEAR_PRICE{common:40,uncommon:60,rare:90}` `SHOP_MANA_PRICE=30`。
補助: `GearsOf(run)`（`?? 空`）・`ManaOf(run)`（`?? 0`）・`GearFull(run)`・`RollGearId(rng, atLeastUncommon)`・`AddGear(run, id, uidHint)`・`SpendGear(run, index)`・`DiscardGearAt(run, index)`。

**RNG の消費順（ゴールデンの生命線）**:
- `RollGearId`: `NextInt(0,99)` → `roll<10 ? rare : (roll<35 || atLeastUncommon) ? uncommon : common` → その帯の `AllGears` の並び順のプール（空なら全体）から `NextInt(0, pool.Count-1)`。**atLeastUncommon でも最初の roll は消費する**。
- `OpenShop`: レリック抽選の後に `for i < 3: (id, rng) = RollGearId(rng, false)`。**重複 id は棚に足さないが RNG は消費済み**。値段 `JsFloor(SHOP_GEAR_PRICE[rarity] * ShopPriceRatio(run))`。`ManaPrice = JsFloor(30 * ShopPriceRatio(run))`。
- `RollRewards`: 札の抽選の**後**。`guaranteed = run.CurrentElite || CurrentNode(run)?.Type == "boss"` なら `RollGearId(rng, true)`。そうでなければ `NextInt(0,99)`、`roll < pity`（pity＝`GearPity ?? 60`）なら `RollGearId(rng,false)` で当たり・pity を 60 へ、外れなら pity+10。`Phase=Reward` と一緒に `GearOption`・`GearPity` を書く。
- `CreateDebugCheckpointRun`: レリックの後・HP の前。`opts` に gearIds が無いので（`ReplayOriginCheckpoint` に欄が無い＝TS の origin 型も同じ）**幕2は3個・幕3は4個を `RollGearId(run.Rng,false)` で `run.Rng` を進めながら `AddGear(..., "cp{i}_{id}")`**。魔素は `Math.Min(50, 幕3=50／幕2=30／幕1=0)`。Autopilot の `gears=`/`mana=` 指定用に省略可能な引数 `IReadOnlyList<string>? gearIds = null, int? mana = null` を足してよい（origin からの再生は null で呼ぶ）。
- `AfterVictory`: `Mana = Math.Min(50, (Mana ?? 0) + (CurrentElite || isBoss ? 10 : 5))`（RNG は触らない）。
- `TransformChosenCard`（§2.2）は戦闘の RNG。

コマンド（`ApplyRunCommand`）:
- `PickReward`: 末尾の `AdvanceActIfBossCleared(picked)` を `FinishReward(picked)` に。`SkipReward`: `leaving = RewardOptions == null && GearOption != null` なら `GearOption=null` も、末尾は `FinishReward`。
  `FinishReward(run)`: `GearOption != null` か `RewardOptions != null` なら run のまま、両方片付いていたら `AdvanceActIfBossCleared`。
- `UseGear{Index, TargetIndex?, CardUid?, AsGearId?}`: `GearBlockedReason(Phase=="combat" ? Combat : null, ManaOf, gear)` が非 null なら throw `"{name} は組めない: {reason}"`。`flee` は幕ボスなら throw、そうでなければ `SpendGear` して `Combat=null, Hp=combat.Player.Hp, Phase=Map, RewardOptions=null, GearOption=null`。
  それ以外は `Gears.ResolveGear(combat, def, opts{SeenGearIds = run.SeenGearIds ?? 空})` → `SpendGear` → `Combat = next with { GearUsedThisTurn = true }` → `Won` なら `AfterVictory`、`Lost` なら `Hp=0, Phase=Lost`。
- `TakeGear{DiscardIndex?}`: `Phase=="reward" && GearOption != null` でなければ throw。満杯なら `DiscardIndex` 必須（無ければ throw「持ち物が満杯 = 入れ替えるギアを選ぶ」）。`AddGear(base, id, "a{act}_r{row}_{id}")` して `GearOption=null` → `FinishReward`。
- `SkipGear`: `Phase=="reward"` でなければ throw。`GearOption=null` → `FinishReward`。
- `DiscardGear{Index}`: `DiscardGearAt`。
- `ShopBuyGear{Index, DiscardIndex?}`: 店でなければ throw・売切/不正は throw・金不足は throw・満杯は `DiscardIndex` 必須。`AddGear(base, id, "shop_a{act}_r{row}_{id}")`・`Gold -= price`・棚の `Sold=true`・`BreakMawBank`。
- `ShopBuyMana`: 店でなければ throw・`price = ManaPrice ?? 30`・金不足 throw・魔素が上限なら throw「魔素は上限」・`Mana = Math.Min(50, Mana + 10)`・`BreakMawBank`。
- `CreateRun` の初期化に `Gears=空, Mana=0, GearPity=60, SeenGearIds=空, GearOption=null`。

`AddGear` は **満杯でも `SeenGearIds` には必ず残す**（無銘の部品の候補）。`SpendGear` は `Charges-1` が 0 なら持ち物から消す。uid は `"gear_" + uidHint`。

### 2.6 Report.cs（選択履歴）

`DescribeRunChoiceCore` に6ケース（文言は TS `describeRunChoiceCore` と同じ）: `TakeGear`＝「ギア取得: 名前（○○ と入れ替え）」／`SkipGear`＝「ギア見送り: 名前」／`UseGear`＝「ギア使用: 名前（△△ として）（魔素 20→10・残1回｜使い切り）」／`DiscardGear`＝「ギアを捨てた: 名前」／`ShopBuyGear`＝「ショップ: ギア 名前 を 40G で購入」／`ShopBuyMana`＝「ショップ: 魔素を購入（20→30・30G）」。
名前は `GearName(id)`＝`AllGears` に無ければ id（古いセーブ用。`SafeRelicName` と同型）。

### 2.7 変えなくてよいもの

- `Golden.cs`: 要約にギアの欄は無い（TS `golden.ts` も不変）。RNG と状態が合えばハッシュは合う。
- `JsonUnions.cs`: 派生 record を反射で拾うので新コマンドの登録は不要。
- `SaveGame`/`Report` の直列化: RunState の新しい欄は optional（`NullValueHandling.Ignore`）＝旧セーブは `?? 0`/空で読める。IL2CPP で問題になる新しいコレクション型は無い（`IReadOnlyList<GearInstance>` は `Relics` と同型）。
- `Analysis.cs`: TS の計測ブロックにギアの欄は無い。

### 2.8 照合

```bash
cd unity/EngineTests && ~/.dotnet/dotnet build
~/.dotnet/dotnet run -- verify ../../goldens/runs/*.json --data ../../src/data      # 13本すべて一致が目標
~/.dotnet/dotnet run -- roundtrip ../../goldens/runs/*.json --every 10 --data ../../src/data   # セーブ往復 (gears/mana の欄込み)
npx vitest run src/sim/port-parity.test.ts                                          # 手書きの表 (触らないはず)
```
分岐したら `npx tsx scripts/dump-golden-digest.ts goldens/runs/<file>.json <index> --events` で TS 側の要約とイベント列を出して比べる。
ギアの手が最初に出るのは報酬（`TakeGear`）・店（`OpenShop` の3回の抽選）・幕2/3のチェックポイント（開始時の3〜4回の抽選）。

## 3. Unity の画面（`unity/Assets/Game/`）

置き場は提案書 §6-2 の裁定＝**案A「匣の帯」**。文言はブラウザ版（`App.tsx` GearBar）と CLI（`play.ts renderGearBar`）に揃える。
語彙は Unity のからくり語彙のまま（「組む」「魔素」）。

### 3.1 共通: 上部バー（`RunUi.TopBar` と `BattleScreen.BuildTopBar`）

G の札の隣に魔素の札: 歯車のアイコン＋`Gears.ManaLabel(ManaOf(run))`（「25/50（あと2個）」。スマホは幅が無ければ「25/50」）。
ツールチップ「魔素: ギアを組む動力。1個＝10。通常戦の勝利で+5・強個体/幕ボスで+10。上限50」。演出の的として `g.RegisterAnchor("mana", 札)`。

### 3.2 戦闘（`BattleScreen`）: ギアのトークンと使う時の窓

- **PC** `PcSelfStrip`: 自分の札の段を A: HP／B: からくり／**新 C: ギア**／D: 置物 に。ギアは 62×62 のトークンを横に最大10（`secC = 10*(62+8)+24`、実際の個数ぶんだけ描き空きは詰める）。
- **スマホ** `PhoneSelfColumn`: 上の帯のからくりの右（置物の左）に 64×66 のトークンを 6個まで、溢れは「+N」（タップで名前の一覧＝`PermChips` の「+N …」と同じ）。キャンバス幅 1300 未満は 4個。
- トークン（`GearToken`）: 紙（`PaperFx.Tag2`）＋レア度の外線（C＝`PaperFx.Ink`／U＝`Sky`／R＝`Honey`＝札と同じ `PaperFx.RarityEdge`）＋絵（`Theme.Art("gears", id)` があればそれ、無ければコード生成の歯車 `ThemeFx.GearGlyph(id, family)`＝干渉系 `interfere` だけ青緑 `Mana`、他は墨）＋下に名前（`UiKit.Deco` 13）＋回数つきは右上に残り回数の丸（`PhoneSetToken` の角の数字と同じ作り）。
  `Gears.GearBlockedReason` が非 null なら絵を灰色にし、ツールチップに「（いまは組めない: 理由）」。演出の的 `g.RegisterAnchor("gear:" + gear.Uid, slot)`。
  戦闘外の画面（ラン画面）ではトークンは押せず、ツールチップだけ。
- **押す→窓**（`BuildGearWindow`。`BuildReactionWindow`／`BuildModeChooser` と同じ紙の窓。スマホは手札の上・PC はトークンの上に立ち上げる）:
  名前＋レア度（`CardText.RarityLabel`）＋「残りN回」／`def.Text`／`Gears.GearLiveDamage`（あれば）／`Gears.GearNoEffectReason`（あれば「⚠ いま組んでも何も起きない: …」を蜂蜜の墨で）／
  無銘の部品は化ける先のボタン列（`SeenGearIds` から `gear_nameless` を除く。無ければ「化ける先がない」）／
  札を選ぶギア（needsCard）は「札を選ぶ」ボタン→既存の `BuildPicker` の器で `Gears.GearCardChoices` を並べる（見出し「手札／捨て札／山札から」）／
  対象を取るギアで生存2体以上なら「組む」の代わりに `BuildTargetBanner` と同じ帯「『楔』の対象を選ぶ — 敵をタップ」＝`OnEnemyClicked` が `PendingGear` に流す／
  脚: 「⚙ 魔素 30 → 20・このターンはあと1個」＋「魔素1で組む」（`PaperFx.BrassLight`）＋「やめる」。`GearBlockedReason` が非 null なら組むボタンを灰色にして理由を添える。
- `GameRoot` に `PendingGear`（`Index, TargetIndex?, CardUid?, AsGearId?`）と `GearWindowIndex` を持ち、`Do()` の一時状態の掃除で消す。揃ったら `g.Do(new RunCommand_UseGear{...})`（ラン層のコマンド。`DoCombat` ではない）。
  戦闘が決着（火薬で全滅）した時は `Do()` の既存の `combatEnded` 経路がそのまま効く。
- **煙玉**（裁定3）: 「組む」で `ConfirmBox{Title="この戦闘から逃げる？", Message="報酬は得られない。HPはそのまま", OkLabel="逃げる", Danger=true, OnOk=UseGear}`。幕ボスの節では組むボタンを灰色に（エンジンも throw する）。

### 3.3 演出（`Presenter`。裁定4＝からくりと同じ）

- `GameEvent_GearUsed`: `Play` の見せる出来事の一覧（`IsTrapEvent` に足すのが早い）に入れ、`TableSound` の除外にも入れる。
  絵: トークン（`Anchor("gear:"+uid)` は使い切りで消えているので **`GearId` で直前の盤面の持ち物から uid を引く**か、`ShowTrap` と同じ `GhostToken` を `def.Name` で作る）が跳ねて、対象へ飛ぶ（needsTarget・全体ダメージ・全体の隙＝敵の意図の札／それ以外＝自分）。着弾で `Tween.RingBurst(fx, pos, PaperFx.Mana, 140, 0.4)`、トークンの側に `Tween.Stamp(fx, pos, "組んだ", PaperFx.BrassLight, PaperFx.Ink, PaperFx.Brass)`。
  舞台: `Stage.SetKarakuriBox(setCount, fired:true)` を呼んで蓋が開いて閃く（からくりの発動と同じ呼び方。`BattleView`/`ShowTrap` の呼び元を真似る）。音は `Audio.Ui("card_play")` か、`audio.json` に `GearUsed` の鍵を足す。
- `GameEvent_DeathSaved` は `Source=="gear"` なら「蘇りの発条がはじけた」の判（朱の縁）＋HP1 の浮き文字（既存の蜥蜴の尾の見せ方があればそれに倣う）。
- `CardText.LogLine`: `GearUsed`＝「⚙ 名前 を組んだ」、`DeathSaved`＝Source で「蘇りの発条がはじけ、HP1で踏みとどまった」／「蜥蜴の尾が砕け、HPNで踏みとどまった」（今は型名がそのまま出ている）。
- 順送りの間（敵フェーズ）はギアを組めないので、`PlaySequenced` との競合は無い。

### 3.4 報酬（`RewardScreen.Reward`。デザイン `Reward.dc.html`）

- 札3枚の右にギアの札（200×272。スマホは札と同じ倍率で縮める）: レア度の外線・歯車の絵・名前・回数・`Text`・「自ターンに魔素1で組む（1ターン1個）」。見出しの下に「魔素 +5（20→25）」（前の値は無いので `ManaLabel(ManaOf(run))` だけでもよい）。
- 「取る」→ `TakeGear`。満杯（`GearFull`）なら持ち物の一覧（トークン列）が開き、1つ押すと `TakeGear{DiscardIndex}`（やめれば見送りではなく閉じるだけ）。「見送る」→ `SkipGear`。
- **札とギアは独立**（`FinishReward`）: `RewardOptions == null && GearOption != null` の局面（先に札を取った）では札の列を出さずギアの札だけ＋下の「見送る」は `SkipGear` を送る。逆（先にギアを片付けた）は今の画面のまま。
- **自動操作の穴**: `Autopilot.cs` 695行（`case RunPhases.Reward: PickReward{Index=0}`）と 812行・`Assets/Editor/BatchTools.cs` 216行（`SkipReward`）は、`GearOption` が残ると `PickReward` が「報酬フェーズではない」で throw して止まる。**`GearOption != null` なら先に `TakeGear`（満杯なら `SkipGear`）を送る**（TS の `golden-driver.ts botRunCandidates` と同じ）。

### 3.5 ショップ（裁定2＝サービス欄のボタン→専用画面）

- `ShopScreen.Build` の「店主のサービス」に `ServiceBtn("ギアの棚  3枠 ／ 魔素 30G", true, ShopMode="gears")`。
- `ShopMode == "gears"` の画面（除去/鍛えると同じ形・`RunUi.Heading("ギアの棚", "自ターンに1個・魔素を払って組む。持ち物 N/10・魔素 25/50（あと2個）")`）:
  棚＝`Shop.Gears` の3枠（名前・レア度・回数・`Text`・値札 `PriceTag`・売切は覆い。金不足は灰色）→ `ShopBuyGear{Index}`（満杯なら持ち物の入れ替えを選ぶ＝報酬と同じ列）／
  魔素の札（「ギアを組む動力（1個ぶん。上限50）」・値段・上限なら灰色）→ `ShopBuyMana`／
  持ち物の列（トークン＋「捨てる」→ `DiscardGear{Index}`）。`RunUi.BackButton("戻る")`。
- 会員証の値引きは `OpenShop` の値付けに掛かる（買った瞬間の値下げ `ShopBuyRelic` の処理はカードだけ＝TS も同じなので追随不要）。

### 3.6 Autopilot（撮影の状態キー）

`gears=<id,...>`（持ち物を直接置く。`Gears.MakeGear`）／`mana=<n>`／`gearwin=<idx>`（トークンを押した状態＝窓）／`gearopt=<id>`（報酬のギア枠）／`shopmode=gears`。
チェックポイント（`act=2|3`）は既定で3〜4個を抽選するので `gears=` 無しでも持ち物がある。撮影例:
```bash
STATE="phase=combat;enemy=enemy_probe;gears=gear_powder,gear_spring,gear_wedge;mana=30;gearwin=0" scripts/unity-win.sh shots state 4242
STATE="phase=reward;gearopt=gear_spring" scripts/unity-win.sh shots state 4242
STATE="phase=shop;shopmode=gears;gold=300;act=2" scripts/unity-win.sh shots state 4242
SHOT_W=1920 SHOT_H=886 UISCALE=1.6 STATE="phase=combat;enemy=enemy_probe;gears=gear_powder,gear_cog,gear_wedge,gear_copy" scripts/unity-win.sh shots state 4242   # スマホ相当
```

### 3.7 絵

PixelLab の挿絵は未発注。`Theme.Art("gears", <id>)`（`Assets/Resources/Art/gears/<id>.png`・32×32）を差し替え口にし、無い間はコード生成の歯車 `ThemeFx.GearGlyph`。
発注書 `docs/pixellab-assets.md` と `docs/art-todo.md` に33個の行を足す（名前と `family` が手掛かり。干渉系は青緑の歯車）。

## 4. 完了の定義

1. `dotnet run -- verify` ゴールデン13本が全手一致・`roundtrip` 一致・`npm test` 緑・`npm run unity:sync check` 差分0。
2. `scripts/unity-win.sh compile` 0エラー・`verify`（Unity 内ゴールデン）一致・`play`（スモーク）通過。
3. `shots state` で 戦闘（PC/スマホ・窓・対象の帯・札の選択）・報酬（ギア枠・満杯の入れ替え・札を先に取った後）・店（ギアの棚）の PNG を撮って確認。
4. Android: APK を出す前に `adb logcat` の `E Unity` を確認（`ReadOnlyDictionary` の前例）。
5. `CLAUDE.md` の「ギア（消耗品）」行を「C# 移植・Unity の画面も対応済み」に更新し、この文書の §0 の裁定を残す。

## 5. 落とし穴（まとめ）

- RNG の消費順（§2.5）。特に「店の棚の重複は捨てるが RNG は消費」「エリート/幕ボスの確定でも最初の roll は消費」「チェックポイントは origin に gearIds が無いので必ず抽選」。
- `ResolveGear` は `Combat.CheckCombatEnd` を通す＝ギアで勝てる。`UseGear` は run 層のコマンドで、勝敗の後始末（`AfterVictory`／`Lost`）も run 層で行う。
- `DeathSaved` の順序: ギア（HP1）が先、蜥蜴の尾（最大HPの半分）が後。
- `PickReward` 後にフェーズが `reward` のまま残る局面（`GearOption` が残っている）を、画面と自動操作の両方が扱うこと。
- `Report.DescribeRunChoiceCore` の default は `null` を返すので、選択履歴の6ケースを足すまで落ちはしないが記録も残らない。
- `Types.g.cs` は再生成済み。TS の `types.ts`/`run.ts` を触ったら `npm run gen:csharp` を回し直す（手で編集しない）。
