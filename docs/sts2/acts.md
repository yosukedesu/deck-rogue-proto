# Slay the Spire 2 デコンパイル解析: 幕構成・戦闘編成プール

出典: `MegaCrit.Sts2.Core.Models.Acts`（4幕）/ `MegaCrit.Sts2.Core.Models.Encounters`（88ファイル）/
`MegaCrit.Sts2.Core.Models.ActModel` / `MegaCrit.Sts2.Core.Rooms.RoomSet` / `MegaCrit.Sts2.Core.Map.*`
（コード転載なし・数値と構造のみ）

---

## 1. 幕の概要

| 幕 | クラス名 | 部屋数(BaseNumberOfRooms) | Weak戦の数 | 全Encounter数 | 祠(Ancient)候補 | イベント数 |
|---|---|---|---|---|---|---|
| Act1-A | Overgrowth | 15 | 3 | 22 | 1 (Neow) | 13 |
| Act1-B | Underdocks | 15 | 3 | 20 | 1 (Neow, 共有) | 10 |
| Act2 | Hive | 14 | 2 | 20 | 3 (Orobas/Pael/Tezcatara) | 10 |
| Act3 | Glory | 13 | 2 | 18 | 3 (Nonupeipe/Tanx/Vakuu) | 7 |

- Overgrowth と Underdocks は**どちらも「1幕目」の代替**（`ActModel.GetRandomList` が抽選。Underdocksはエポック解禁後にランダムで置き換わる、または初回未発見なら強制）。
- 部屋数はマルチプレイ時 -1（`GetNumberOfRooms`）。マップの行数は `部屋数+1`。

---

## 2. 編成プール生成ロジック（`ActModel.GenerateRooms`, 全幕共通）

各幕は起動時（ラン開始時）に以下の順で戦闘の「出現順リスト」を1本構築する。**マップ上の同タイプ部屋を訪れるたびにこのリストを先頭から順番に消費**し、リストを使い切ったら周回（`% Count`）する。フロアごとの動的抽選ではなく、**幕頭で決定された固定シーケンス**。

1. **Weak枠**: `NumberOfWeakEncounters`回、`AllWeakEncounters`（RoomType=Monster かつ IsWeak=true）から GrabBag 抽選で `normalEncounters` に追加
2. **Normal枠**: `NumberOfWeakEncounters` 〜 `BaseNumberOfRooms-1` の残り回数ぶん、`AllRegularEncounters`（RoomType=Monster かつ IsWeak=false）から同様に追加（Weak枠と**同じリストの続き**）
3. **Elite枠**: 15回、`AllEliteEncounters`（RoomType=Elite）から GrabBag 抽選で `eliteEncounters` に追加
4. **Boss**: `AllBossEncounters` からRNGで1体を `_rooms.Boss` に設定（後述の DiscoveryOrder で初回プレイは上書きされる）

**GrabBag抽選のルール**（`AddWithoutRepeatingTags`）: 直前に選ばれたEncounterと `EncounterTag`（例: Slimes/Workers/Exoskeletons等）が重複しない候補を優先して抽選し、無ければタグ制約なしで抽選。**GrabBagが空になったら同じ幕内プールで補充**（同じ敵が幕内で複数回出現しうる）。

**初回プレイ限定の上書き**（`ApplyActDiscoveryOrderModifications`）: Overgrowthのみ実装あり。初回ラン(`NumberOfRuns==0`)は Normal/Elite/Event の先頭数枠を固定順に差し替え（チュートリアル導線）:
- Normal: `[0]NibbitsWeak → [1]SlimesWeak → [2]ShrinkerBeetleWeak → [3]InkletsNormal → [4]MawlerNormal → [5]RubyRaidersNormal → [6]NibbitsNormal`
- Event: `[0]ByrdonisNest → [1]SapphireSeed`
- Elite: `[0]ByrdonisElite → [1]PhrogParasiteElite`

他3幕は `ApplyActDiscoveryOrderModifications` が空実装（固定導線なし）。

**ボス選出順**（`BossDiscoveryOrder` × `ApplyDiscoveryOrderModifications`）: 未撃破のボスを配列先頭から探して優先出現させる（3体制覇でローテ）。

| 幕 | BossDiscoveryOrder（初回〜3周目の優先順） |
|---|---|
| Overgrowth | Vantom → CeremonialBeast → TheKin |
| Underdocks | WaterfallGiant → SoulFysh → LagavulinMatriarch |
| Hive | TheInsatiable → KnowledgeDemon → KaiserCrab |
| Glory | Queen → TestSubject → Doormaker |

---

## 3. 幕ごとの編成プール詳細

### 3-1. Overgrowth（Act1-A）

**Weakプール**（候補4、毎ラン3体使用）
| 編成 | メンバー | 特記 |
|---|---|---|
| NibbitsWeak | Nibbit ×1 | `IsAlone=true` |
| SlimesWeak | 小スライム(Leaf/Twig からランダム2種のうち1) + 中スライム(Leaf/Twig ランダム) + 小スライム残り1種 ×計3体 | tag: Slimes |
| ShrinkerBeetleWeak | ShrinkerBeetle ×1 | tag: Shrinker |
| FuzzyWurmCrawlerWeak | FuzzyWurmCrawler ×1 | tag: Crawler |

**Normalプール**（候補12）
| 編成 | メンバー | 特記 |
|---|---|---|
| CubexConstructNormal | CubexConstruct ×1 | |
| FlyconidNormal | 中スライム(Leaf/Twigランダム1) + Flyconid | tag: Mushroom, Slimes |
| FogmogNormal | Fogmog ×1（スロット"fogmog"、EyeWithTeethは後続スポーン想定） | |
| InkletsNormal | Inklet ×3（1体が `MiddleInklet=true`） | |
| MawlerNormal | Mawler ×1 | |
| NibbitsNormal | Nibbit ×2（front/back、front側 `IsFront=true`） | |
| OvergrowthCrawlers | ShrinkerBeetle + FuzzyWurmCrawler | tag: Shrinker, Crawler |
| RubyRaidersNormal | AxeRubyRaider/AssassinRubyRaider/BruteRubyRaider/CrossbowRubyRaider/TrackerRubyRaider から重複無しで3体ランダム | |
| SlimesNormal | TwigSlimeM + LeafSlimeM + 小スライム2体（順番ランダム化） | tag: Slimes |
| SlitheringStranglerNormal | SlitheringStrangler + {SnappingJaxfruit / 中スライム1体 / 小スライム2体}からランダム1パターン | |
| SnappingJaxfruitNormal | SnappingJaxfruit + Flyconid | tag: Mushroom |
| VineShamblerNormal | VineShambler ×1 | |

**Eliteプール**（候補3）
| 編成 | メンバー |
|---|---|
| BygoneEffigyElite | BygoneEffigy ×1 |
| ByrdonisElite | Byrdonis ×1 |
| PhrogParasiteElite | PhrogParasite ×1（スロット"phrog" + wriggler1-4予約スロット、後続スポーン想定） |

**Bossプール**（候補3）
| 編成 | メンバー |
|---|---|
| CeremonialBeastBoss | CeremonialBeast ×1 |
| TheKinBoss | KinFollower ×2（1体`StartsWithDance=true`）+ KinPriest ×1 |
| VantomBoss | Vantom ×1 |

---

### 3-2. Underdocks（Act1-B）

**Weakプール**（候補4、毎ラン3体使用）
| 編成 | メンバー |
|---|---|
| CorpseSlugsWeak | CorpseSlug ×2（開始行動が重複しないよう調整） tag: Slugs |
| SeapunkWeak | Seapunk ×1 tag: Seapunk |
| SludgeSpinnerWeak | SludgeSpinner ×1 |
| ToadpolesWeak | Toadpole ×2（front/back） |

**Normalプール**（候補10）
| 編成 | メンバー |
|---|---|
| CorpseSlugsNormal | CorpseSlug ×3（開始行動を相互にずらす） tag: Slugs |
| CultistsNormal | CalcifiedCultist + DampCultist |
| FossilStalkerNormal | FossilStalker ×1 |
| GremlinMercNormal | GremlinMerc ×1（スロット"merc"。FatGremlin/SneakyGremlinはAllPossibleMonstersに含むが初期配置は無し=後続スポーン想定） |
| HauntedShipNormal | HauntedShip ×1 |
| LivingFogNormal | LivingFog ×1（スロット"livingFog" + bomb1-5予約、GasBomb後続スポーン想定） |
| PunchConstructNormal | PunchConstruct ×1 |
| SeapunkNormal | CalcifiedCultist + Seapunk tag: Seapunk |
| SewerClamNormal | SewerClam ×1 |
| TwoTailedRatsNormal | TwoTailedRat ×3（開始行動を相互にずらす、スロット3〜5番目を使用） |

**Eliteプール**（候補3）
| 編成 | メンバー |
|---|---|
| PhantasmalGardenersElite | PhantasmalGardener ×4 |
| SkulkingColonyElite | SkulkingColony ×1 |
| TerrorEelElite | TerrorEel ×1 |

**Bossプール**（候補3）
| 編成 | メンバー |
|---|---|
| LagavulinMatriarchBoss | LagavulinMatriarch ×1 |
| SoulFyshBoss | SoulFysh ×1 |
| WaterfallGiantBoss | WaterfallGiant ×1 |

---

### 3-3. Hive（Act2）

**Weakプール**（候補4、毎ラン2体使用）
| 編成 | メンバー |
|---|---|
| BowlbugsWeak | BowlbugRock + {BowlbugEgg/BowlbugNectar}からランダム1 tag: Workers |
| ExoskeletonsWeak | Exoskeleton ×3 tag: Exoskeletons |
| ThievingHopperWeak | ThievingHopper ×1 tag: Thieves |
| TunnelerWeak | Tunneler ×1 tag: Burrower |

**Normalプール**（候補10）
| 編成 | メンバー |
|---|---|
| BowlbugsNormal | BowlbugRock + {Egg/Silk/Nectar}から重複無しで2体ランダム tag: Workers |
| ChompersNormal | Chomper ×2（1体`ScreamFirst=true`） tag: Chomper |
| ExoskeletonsNormal | Exoskeleton ×4 tag: Exoskeletons |
| HunterKillerNormal | HunterKiller ×1 |
| LouseProgenitorNormal | LouseProgenitor ×1 |
| MytesNormal | Myte ×2 |
| OvicopterNormal | Ovicopter ×1（ToughEggは後続スポーン想定、egg1-5予約スロット） |
| SlumberingBeetleNormal | BowlbugRock + BowlbugSilk + SlumberingBeetle tag: Workers |
| SpinyToadNormal | SpinyToad ×1 |
| TheObscuraNormal | TheObscura ×1（スロット"obscura"、illusion予約スロット） |

**Eliteプール**（候補3）
| 編成 | メンバー |
|---|---|
| DecimillipedeElite | DecimillipedeSegmentFront/Middle/Back 各1体（開始行動をローテーション: `StarterMoveIdx` が0/1/2をずらして被らないようにする） |
| EntomancerElite | Entomancer ×1 |
| InfestedPrismsElite | InfestedPrism ×1 |

**Bossプール**（候補3）
| 編成 | メンバー |
|---|---|
| KaiserCrabBoss | Crusher + Rocket（`FullyCenterPlayers=true`） |
| KnowledgeDemonBoss | KnowledgeDemon ×1 |
| TheInsatiableBoss | TheInsatiable ×1 |

**注**: `TunnelerNormal.cs` というファイルは存在するが、Hive の `GenerateAllEncounters()` には含まれていない（未参照＝どの幕プールからも到達不能。カット/未使用コンテンツの可能性）。

---

### 3-4. Glory（Act3）

**Weakプール**（候補3、毎ラン2体使用）
| 編成 | メンバー |
|---|---|
| DevotedSculptorWeak | DevotedSculptor ×1 |
| ScrollsOfBitingWeak | ScrollOfBiting ×3（開始行動を相互にずらす） tag: Scrolls |
| TurretOperatorWeak | LivingShield + TurretOperator |

**Normalプール**（候補9）
| 編成 | メンバー |
|---|---|
| AxebotsNormal | Axebot ×2（front/back） |
| ConstructMenagerieNormal | PunchConstruct ×1 + CubexConstruct ×2 |
| FabricatorNormal | Fabricator ×1（スロット"fabricator"、bot1-4予約=後続スポーン想定） |
| FrogKnightNormal | FrogKnight ×1 |
| GlobeHeadNormal | GlobeHead ×1 |
| OwlMagistrateNormal | OwlMagistrate ×1 |
| ScrollsOfBitingNormal | ScrollOfBiting ×4（3体は開始行動をローテ、4体目は`StarterMoveIdx=2`固定） tag: Scrolls |
| SlimedBerserkerNormal | SlimedBerserker ×1 |
| TheLostAndForgottenNormal | TheLost + TheForgotten |

**Eliteプール**（候補3）
| 編成 | メンバー |
|---|---|
| KnightsElite | FlailKnight + SpectralKnight + MagiKnight（3スロット固定） tag: Knights |
| MechaKnightElite | MechaKnight ×1 |
| SoulNexusElite | SoulNexus ×1 |

**Bossプール**（候補3）
| 編成 | メンバー |
|---|---|
| DoormakerBoss | Doormaker ×1 |
| QueenBoss | TorchHeadAmalgam + Queen |
| TestSubjectBoss | TestSubject ×1 |

---

## 4. イベント専用Encounter（幕プール外・7種）

戦闘としてはどの幕の `GenerateAllEncounters()` にも含まれず、特定イベント文脈から直接呼ばれる想定:
`DeprecatedEncounter`（廃止マーカー）, `BattlewornDummyEventEncounter`（3種の設定違い敵から1体）,
`DenseVegetationEventEncounter`（Wriggler×4）, `FakeMerchantEventEncounter`（偽商人・報酬300G固定）,
`MysteriousKnightEventEncounter`（MysteriousKnight×1）, `PunchOffEventEncounter`（PunchConstruct×2、
うち1体`StartsWithStrongPunch=true`＋両体に`StartingHpReduction`をRNG(2〜10)で個別付与）,
`TheArchitectEventEncounter`（Architect×1）。

---

## 5. マップ生成（`StandardActMap` / `MapPointTypeCounts`）

### グリッドとパス生成
- グリッド: **7列 × (部屋数+1)行**。開始行(row0)は仮想スタート地点、最終行がボス部屋。
- **7本のパス**を左右±1移動の乱歩で生成（`_iterations=7` 本の独立ウォーク。2本目は1本目と異なる開始列を強制）。
- 交差防止: 隣接列のパスと交差するステップは選び直す（`HasInvalidCrossover`）。
- 全パスの最終行はボス部屋に接続。row1（最初の部屋行）は全てスタート地点から接続＝**最初の部屋は必ず複数の選択肢**。

### 部屋タイプの割り当て順序
1. **最終行（row = 行数-1）= 全てRestSite固定**（ボス前の休憩、`CanBeModified=false`）
2. **ボスから7行手前 = 全てTreasure固定**（`ShouldReplaceTreasureWithElites=true`のバリアント時は全てElite固定。宝箱行をエリート行に置き換える特殊モード）
3. **row1（最初の部屋行）= 全てMonster固定**（初戦は必ず戦闘、StSでお馴染みの「1階は必ず戦闘」相当）
4. 残りの未割当マスに、目標数ぶんの `RestSite` / `Shop` / `Elite` / `Unknown` をキューへ積み、行単位でシャッフルしてランダム配置（`AssignPointTypesToRandomRows` → 足りなければ `AssignRemainingTypesToRandomPoints` で個別マス単位に3回リトライ）
5. 割り当てられず残ったマスは全て `Monster` にフォールバック

### 部屋タイプ数の目標値（`MapPointTypeCounts`）
| 項目 | 値 | 備考 |
|---|---|---|
| NumOfElites | 5（`Math.Round(5 × 1.6)`=8 with SwarmingElites上級難度） | `StandardActMap.maxElites`定数は15だが未使用の上限枠 |
| NumOfShops | 3 | 全幕共通・固定 |
| NumOfUnknowns | `NextGaussianInt(12, σ1, min10, max14)` が基本形。Hive/Gloryは基本形から**-1** | 幕ごとに `GetMapPointTypes()` をオーバーライド |
| NumOfRests | 幕ごとに異なる乱数 | 下表参照 |

| 幕 | NumOfRests の分布 |
|---|---|
| Overgrowth | `NextGaussianInt(mean7, σ1, min6, max7)` |
| Underdocks | `NextGaussianInt(mean7, σ1, min6, max7)` |
| Hive | `NextGaussianInt(mean6, σ1, min6, max7)` |
| Glory | `NextInt(5, 7)`（一様分布） |

### 配置制約（`IsValidPointType`。5種のルールをAND判定）
- **下限制約**: row<6（マップ下部＝ボスに近い側）には `RestSite` / `Elite` を置けない
- **上限制約**: 最終3行以内（マップ上部＝スタートに近い側）には `RestSite` を置けない
- **親制約**: `Elite/RestSite/Treasure/Shop` は、親ノード（1つ前の部屋）が同タイプなら不可
- **子制約**: 同上のタイプは、子ノード（次の部屋）が同タイプなら不可
- **兄弟制約**: `RestSite/Monster/Unknown/Elite/Shop` は、同じ親を共有する同じ行の他ノードと同タイプなら不可

### 修復・後処理
- `MapPathPruning.PruneAndRepair`: 完全に同一の経路セグメント（分岐しても合流して同じになる区間）を検出して枝刈りし、それにより部屋タイプ数が目標を割ったら `RepairPointType` で `Monster`固定でないマスを対象タイプへ差し替えて補充（Shop→Elite→RestSite→Unknownの順）。最大3イテレーション。
- `MapPostProcessing`: グリッドの中央寄せ・隣接ノードの間隔調整・パスの直線化（表示用の整形、部屋タイプには影響しない）。

---

## 要点5行

1. 4幕構成（Overgrowth/Underdocks/Hive/Glory）で部屋数は15/15/14/13、Weak戦は3/3/2/2回。Overgrowth と Underdocks は「1幕目」の二択（ランダムまたは初回強制）。
2. 各幕の敵編成は幕頭で「Weak→Normal」の固定順シーケンス（GrabBag、直前と同タグを避ける抽選）として一括生成され、部屋を訪れるたび先頭から消費・使い切ったら周回する（フロア毎の動的抽選ではない）。
3. 88の編成ファイルのうち80が実際に4幕いずれかのプールに含まれ、7つはイベント専用（幕プール外）、`TunnelerNormal`のみ定義済みだが全幕から未参照（デッドコード）。
4. マップは7列グリッド×7本パスの乱歩生成で、最終行=全休憩・ボス7行手前=全宝箱(またはエリート)・最初の行=全戦闘が固定、残りはElite目標5/Shop固定3/Unknown≈12/Restは幕別乱数を行内シャッフルで配置し、親子兄弟の隣接禁止ルールで検証・不足分は再修復する。
5. 個体差の演出はコード側のフラグで細かく作り込まれている（開始行動のずらし・スクリーム先制・前後配置・ランダム部位選択・HPランダム減少など）が、幕・編成のプール構造自体はシンプルなタグ抽選+固定行制約でできている。
