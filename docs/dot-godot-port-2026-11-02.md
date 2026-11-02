# dot の Godot Project422 (codex/godot-vfx422-unity-handoff-20261102) を Unity へ移す — 棚卸しと移植の記録 (2026-11-02)

ユーザー「ブランチ codex/godot-vfx422-unity-handoff-20261102 を fetch し、handoffs/godot-project422/UNITY-HANDOFF-JA.md に従って、Project422 の全対象エフェクトを既存 Unity へ移植」。
前回 (`docs/dot-godot-port-2026-10-27.md`・checkpoint 192) の上に、422 で増えた分を移した。作業枝 `unity/vfx422-port`。

## 0. 結論

- **Godot 422 の VFX は 3 層**: ①幾何 (`Hd2dAttackPrototype.cs` 1,868 → 3,373 行・新しい工場 21)、②描き手の橋 (`NativeGeometryBridge.cs` 816 → 1,522 行・選ばれた経路の工場 16 ＋ `BlueContactVfx379`)、③門 (`*ContactVfxProfiles` 30 種 ＝ 元の経路 → 選ばれた経路 の書き換えと所有権)。それに表面のシェーダ 19 本・絵 21 枚・色の表 3 本・共有の幾何の変更 2 本。
- **Unity には全部移した** (§2)。幾何と橋と門の組 (経路の tuple・数値・時間) は Godot のコードを一字も変えずに置き (`unity/Assets/Game/Hd2d/`)、Unity 固有の所 (出来事 → 経路の解決・所有権の近似・頂点の流し込み・光・配線) だけ書いた (§3)。
- **Godot で検証されていないもの・Unity で近似にしたもの** は §5・§6 に列挙。Android 実機は未確認 (422 側も未確認)。

## 1. 棚卸し (Godot 422 の全対象と Unity の対応)

数え方は末尾 §7。比較は `handoffs/godot-project422/project` (422) 対 Unity main (`d3a4f55a`) と、cp192 (`origin/codex/unity-parity-20261013`)。

### 1-1. 幾何と表

| 種類 | Godot 422 | Unity (前) | 判定・移植 |
|---|---|---|---|
| `Hd2dAttackPrototype.cs` | 3,373 行 | 1,868 行 (cp192) | **更新**: 422 版をそのまま (using・名前空間・Enabled・`Array.Clear`・`MathF.Tau` の 3 点だけ)。新しい工場 21: 中立の圧 (`CreateNeutralCastPressure`)・光の奔流 (`CreateLightTorrent`)・火の一撃の当たり (`FireStrikeContact`)・白の大槌の圧 (`HolyMaulContactCompression`・層 92/93)・所有の軸の突き・二重の鞭の小さな光・氷の壁・大いなる癒し・置物 (林冠・茨の炎・火・闇・植物・雷)・火薬 (ギア)・延焼のティック・亡骸 (接吻・唸る頭骨)・仕込み札 (呪い返し・冥府の手・怨嗟・死中の活)・敵の当たり (突き・鈍打・中立)・光の矢の所有 |
| `ElementalVfxProfiles.cs` | 家族 `NeutralPressure` ＋ 11 札 | 無し | **更新** (そのまま) |
| `PhysicalVfxProfiles.cs`・`AttackRoute`・`NativeBatch`・`SourceShowcaseProfile` | 同じ | 同じ | 変更なし |
| `NativeEffect.cs` | `Configure*CommittedContact` 14 本 | 無し | **更新** |
| `SourceShowcaseProfiles.cs` 後半の門 17 ＋ `PermanentContact`・`Mirror`・`Necro`・`Reaction`・`Lightning`・`EnemyContact`・`ImpSpark`・`Status`・`Gear`×4 ・`Support`・`BlueContactVfx379` の経路 | 30 種 | 無し | **新規** `Hd2dVfxProfiles.cs` (経路の tuple と Select はそのまま) |
| `NativeGeometryBridge.cs` | 選ばれた経路の工場 16 ＋ 旧い幾何の包み ＋ `Create` の分岐 | 無し (Unity は旧い描き手が別) | **新規** `Hd2dGeometryBridge.cs` (選ばれた工場 16・`BlueContactVfx379.Create`・土台の旧い幾何 9 種の `Create`・`Times`) |
| `AttackRouteCatalog.cs` ＋ `assets/attack-routes.json` (443 経路) | 元の経路の表 | 無し (Unity は `card-vfx.json`) | **新規** `Hd2dRouteCatalog.cs` ＋ `Resources/Data/hd2d-attack-routes.json` (表はそのまま) |
| BattleStage の所有権 (`IsOwned*` 60 本・`HasCommitted*`) | SessionChange (コマンドの差分) を見る | 無し | **新規** `Hd2dOwnership.cs` (出来事の記録と前後の盤面で近似。§3-2) |
| `SupportVfxProfiles.Describe` (守り・氷壁・回復の支え) | PresentationBeat | 無し | **新規** `Hd2dOwnership.DescribeSupport` ＋ `PresenterHd2dSupport.cs` |
| 共有の幾何 25 本 (`DeckRogue.Game`) | `HealContinuousGeometry` (+52 行: `OwnedMaulWrap`)・`LocalMaterialProfileGeometry` (+57 行: `SampleMaelstromReceipt`)、他 23 本は cp192 と同一 | cp192 と同一 | **更新 2 本** (式はそのまま。Unity の書き方で) |
| `BattleActor.cs` | 光の奔流の放つコマの保持 (`TorrentReleaseHold`)・ひばなの足の錨 | 無し | 保持 → `Hd2dAttackFx.PoseDurations` で white_light_torrent の 4 コマ目を .42 秒に (近似)。ひばなの足の錨は 422 の立ち絵 (未取り込み) の話＝対象外 |
| `EnemyAttackRigProfile`・`EnemySpiritProfile` ＋ `enemy-attack-rigs.json`・`enemy-spirit-profiles.json` | 敵の関節の揺れ (`rpg_spirit_*.gdshader`) | 技ごとのコマで代替済み (10-28) | **対象外** (VFX でなくキャラの動き。Unity は dot の 285 技のコマ) |

### 1-2. シェーダ

| Godot 422 | Unity | 判定 |
|---|---|---|
| `authored_Hd2dPrototype.gdshader` (329 → 397 行) | `Hd2dPrototype.shader` | **更新**: 層 92/93 (大槌の圧の密度・裂け目)・絵の動き 33 (所有の軸の突き)・7 の帯を 93 の負の時刻にも・92/93 は絵の経路を通さない。**絵の v を 1-v で読む** (§3-4) |
| 新しい表面 19: `AetherLanceContactSurface`・`BlueContact379Surface`・`ColdSweepSurfaceFx`・`CrossbowAimedSurface`・`CultistDarkBladeContactSurface`・`GreenGulpContactSurface`・`Hd2dCanopySurface`・`Hd2dKissDensity`・`Hd2dOpticalFlow`・`LightBoltOpticalSurface`・`LightBurstOpticalSurface`・`MomentumWhipContactSurface`・`ObjectContact415Surface`・`SparkBurstContactSurface`・`Spore385ContactSurface`・`VineFormationCounterSurface`・`VineGraspSurface`・`WarmSpark397Surface`・`WraithCurseSurface` | 無し | **新規 19 本** (`Resources/Shaders/<同名>.shader`。式と数値はそのまま。乗算済み Blend One OneMinusSrcAlpha。`Hd2dOpticalFlow` は加算。絵は Godot の画像座標なので v を 1-v で読む) |
| 旧い 18 本 (`BoundedContactFx`…`PhysicalSlashFx`) | 同じ | cp192 から変更なし |
| `StageUnitLit` (+5 行) | — | 俳優の板の照明 (ひばな)。VFX でない＝対象外 |
| `actor_shadow`・`recovered_*`・`rpg_spirit_*`・`style04_StageUnitOverlay` | — | 舞台/俳優 (cp192 の時点で対象外のまま) |

### 1-3. 絵・色の表・音

| 種類 | 422 | 判定 |
|---|---|---|
| `assets/Art/fx` の新しい絵 29 | コードが参照する 21 (`aether-piercing-pressure-v1`・`canopy-organic-density-v1`・`cold-frost-density-v1`・`crossbow-pressure-density-v1`・`cultist-dark-pressure-v1`・`hd2d-earth-soil-bank-v1`・`hd2d-fire-volume-powder-density-v1`・`hd2d-fire-volume-thorn-density-v1`・`hd2d-ice-wall-density-v1`・`hd2d-light-torrent-density-v1`・`hd2d-mass-heal-density-v1`・`hd2d-necro-kiss-density-v1`・`hd2d-waterwide-phase-crest-v2`・`holymaul-compressed-fracture-v1`・`light-bolt-pressure-density-v1`・`light-burst-pressure-density-v1`・`maelstrom-water-density-v1`・`momentum-whip-pressure-v1`・`wraith-pressure-density-v1`) ＋ `hd2d-waterwide-phase-peel-v2` (未参照だが 422 の組) を `Resources/Art/fx/` に。**`attack-slash/stomp/water-{body,glow,particles}-8f.png` 9 枚は旧い 8 コマの帯で 422 のコードから参照されない＝入れない** | 取り込み 20 (importer: hd2d-* は 512・他は 1024・ミップ・Bilinear。§3-5) |
| `presentation-data` の新しい表 | `hd2d-mass-heal.json`・`hd2d-holy-maul-pressure-363.json`・`holy-maul-recovery-wrap-r1.json` → `Resources/Data/`。`enemy-attack-rigs.json`・`enemy-spirit-profiles.json` は対象外 (敵の関節)。`card-vfx-profiles.json`・`legacy-attack-routes-stage41.json`・`renderer-port-status.json` は Unity の `card-vfx.json` と同じ中身／記録 | 取り込み 3 |
| 音 | 422 の `assets/Audio` は Unity の部分集合 (hd2d_three 6 本は 10-27 に入れた)。新しい音なし | 変更なし |
| 必須依存 71 (`.godot/imported`) と `lib/*.dll` | Godot の実行にだけ要る (欠けた原素材の代わり)。Unity は PNG/JSON の原本を直接読む | 枝にそのまま (消さない)。Unity へは写さない |

## 2. 何を移したか (Unity 側の置き場)

| Unity | 中身 |
|---|---|
| `unity/Assets/Game/Hd2d/Hd2dAttackPrototype.cs` | 422 版そのまま (3,380 行) |
| `unity/Assets/Game/Hd2d/ElementalVfxProfiles.cs`・`NativeEffect.cs`・`Hd2dMath.cs` | 422 の差分 (NeutralPressure・Committed の口・Vector2 の向きの定数) |
| `unity/Assets/Game/Hd2d/Hd2dVfxProfiles.cs` | 門 30 種 (経路の tuple・Select) |
| `unity/Assets/Game/Hd2d/Hd2dRouteCatalog.cs` | 元の経路の表 (443) と `Resolve` |
| `unity/Assets/Game/Hd2d/Hd2dOwnership.cs` | 所有権 (IsOwned*) ・選ぶ鎖・支えの Describe・延焼・光の方針 |
| `unity/Assets/Game/Hd2d/Hd2dGeometryBridge.cs` | 選ばれた経路の工場 16 ＋ 旧い幾何の包み ＋ `Create`・`Times` |
| `unity/Assets/Game/Hd2d/Hd2dAttackFx.cs` | 描き手 (書き直し): 経路 → 幾何 → 層ごとのシェーダ → 頂点 (float の色) → 光。支え (`BeginSupport`)・延焼 (`BeginSystem`)・大槌の回復 (`BeginHeal`) |
| `unity/Assets/Game/PresenterHd2dSupport.cs` | Presenter から支えの演出へ (BlockGained / IceBlockGained / HpHealed) |
| `unity/Assets/Game/ResolvedCombatFx.cs` | Ticket に文脈 (出来事・記録・前後の盤面・リーダー・持ち物)。延焼・回復の分岐 |
| `unity/Assets/Game/Presenter.cs` | 前の盤面の受け渡し・敵の当たりの `authoredReady`・支えの呼び出し |
| `unity/Assets/Game/HealContinuousGeometry.cs`・`LocalMaterialProfileGeometry.cs` | 422 の 2 本の差分 |
| `unity/Assets/Resources/Shaders/` | `Hd2dPrototype.shader` 更新 ＋ 新規 19 |
| `unity/Assets/Resources/Art/fx/` 20 枚・`Resources/Data/` 4 本 ＋ `unity/Assets/Editor/ArtImporter.cs` の規則 | 絵と表 |

## 3. Unity 固有の所 (Godot と違うかもしれない所)

### 3-1. 経路の解決
- Godot は `AttackRouteCatalog.Resolve(hit)` → 元の経路 → 門で選ぶ。Unity も同じ表 (`hd2d-attack-routes.json`) を引く。**登録のある札 (108) は今まで Unity が `Renderer="hd2d-prototype-r192"` の経路を自前で作っていた**が、422 の分岐 (`FireStrikeContactRoute`・光の奔流・白の大槌) は元の経路の tuple (`FireContinuousFx`・`LightRayFx`…) を見るので、**表の経路を使うように変えた**。
- 表に無い出来事 (表の 443 に無い札) は Godot と同じ中立の当たり (`Neutral`) になるが、Unity では選ばれた経路にならないので旧い描き手 (BoundedContactFx ほか) のまま。

### 3-2. 所有権の近似 (Godot の SessionChange が Unity に無い)
- Godot: 1 コマンドの差分 (前後の盤面・コマンド・出来事の束) で「本物のその札のプレイか」を見る。
- Unity: 出来事の記録 (`GameState.EventLog`) の中で **直前の区切り (CardPlayed・ReactionTriggered・GearUsed・TurnEnded・TurnStarted・EnemyPhaseEnded・EnemyActionExecuting) から次の区切りまで** を「同じコマンドの出来事」、見せていた盤面を「前」、今の盤面を「後」とする。
- 置き換えた判定: `Command_PlayCard.CardUid==source.Uid` → 区切りの CardPlayed が同じ札で、前の手札にその uid がある／`Command_ConfirmReaction{Fire}` → 前の仕込み場にあり後に無く、同じコマンドに ReactionTriggered／`RunCommand_UseGear` → 同じコマンドの GearUsed (前の持ち物は Presenter に無い)／`ModeIndex` (絡み蔦の守りの面) → 同じコマンドに自分の打撃が無い／蛇の呑み込みの「払って捨てた 1 枚」→ 前の手札が 2 枚以上。
- 受領の検証 (`CombatPresentationLedger.Valid`・敵の番号・HP の差) は Godot と同じ関数。

### 3-3. 「約束済みの当たり」
- Godot: 受領 (receipt) が Committed になった瞬間に `Configure*CommittedContact(true)`。Unity: 数字が出る瞬間 (`AttackPlayback.Land`) に立てる。支えは始まりが接触なので最初から true。

### 3-4. 絵の縦の向き
- Godot の絵は左上が原点、Unity は左下。`Hd2dAttackPrototype` の `MaterialSprite` は Godot 向きに Y を反転した uv を作るので、Unity のシェーダは **読む時に v を 1-v にする** (`Hd2dPrototype.shader` の `Tex`・新規 19 本の `Tex*`)。cp192 の移植ではこの反転が無かった (絵が上下逆だった)。

### 3-5. 絵の大きさ
- 422 の新しい絵は Godot で原寸・ミップなし (size_limit 0)。Unity は端末のメモリを考えて hd2d-* は 512 (cp192 と同じ)、表面の素材 11 枚は 1024 に縮め、ミップつき。シェーダは正規化した uv で読むので画素の式は保たれる。

### 3-6. 光
- Godot の光の方針 (SupportLightVfx391・NeutralPressure・EarthRoar・GreenPhysical・WhiteLight396・RootingVine404・LegacyContact419・WaterWide) は光の **範囲と量** を変える。Unity の契約の光 (StageFx) は範囲を持たないので **量の倍率 (×高さ²/9) だけ** 写した。光の矢の所有 (LightBolt) は目標 .50・床 .14。BoundedContactFx 系は 0 (Godot と同じ)。
- 受ける側の俳優の光 (TripleHorn .12・DoubleLash .16・HolyMaul .10・大槌の回復 .08 の「hero/forest receive」= `AppendSourceLights`) は Godot の俳優の素材の照明で、Unity の板には同じ口が無い＝**未移植** (幾何の方だけ: 二重の鞭の小さな光・大槌の圧は移した)。

### 3-7. 見える終わり
- Godot の beat は元の経路の Duration (`AuthoredEffectPlan.Times`) を持つ。Unity も元の経路の `Times(original).end` を使い、選ばれた幾何が自分で持つ上限 (例: 唸る頭骨 .44) の方が長ければそこまで見せる。

### 3-8. 旧い描き手との住み分け
- 選ばれた経路になった当たりだけ新しい描き手 (`Hd2dAttackFx`) が受け、それ以外は今までの Unity の描き手 (BoundedContactFx・FireContinuousFx…) のまま。TripleHorn の受け (`TripleHornReceiverFx`) は光だけの印なので旧い PlayerContactFx のまま描く。

## 4. 検証 (Unity・PC の撮影)

条件: `scripts/unity-win.sh compile` 0 エラー → `build` (Win64・シェーダのエラー 0) → `states` で下の状態を 1920×1080・PC・`hd2dfx=1` (既定) で撮る。1 フレーム＝1/60 秒 (`playevery=2`＝1/30 秒刻み)。絵の読み込みの警告 0。
撮影の台本は scratchpad の `states-422.txt` (27 状態)・`states-422b.txt` (20 状態)、絵は `scratchpad/shots422/` (枚ごと) と `shots422/sheets/`・`zoom2/` (コマを並べた一覧)。
「鳴った」＝`player.log` に `[Hd2dAttackFx] begin|support|system|heal route=…` が出て renderer が選ばれた経路の描き手になっていること。「見えた」＝並べたコマに絵が写っていること。

### 4-1. 結果

| 状態 (STATE) | 経路 → 描き手 | 鳴った | 見えた |
|---|---|---|---|
| `hand=red_strike;play=0` | card:red_strike → FireStrikeContact (Hd2dPrototype) | ○ | ○ 火の当たり・絵の向きも正 (cp192 は上下逆だった) |
| `hand=green_finisher_stomp` | card → 踏みつけ (Hd2dPrototype) | ○ | ○ |
| `hand=white_holy_maul` | card → HolyMaulContactCompression ＋ 自分への回復の包み (BeginHeal) | ○ | ○ |
| `hand=white_light_burst;plight=1;enemy=enc_probe_trio` | card → LightBurst (LightBurstOpticalSurface) ×3 体 | ○ ×3 | ○ |
| `hand=white_light_torrent;plight=3` | card → LightTorrent (LightRayFx) ×3 | ○ ×3 | ○ 光の筋が敵へ |
| `hand=green_double_lash` | card → DoubleLashReceive | ○ | ○ |
| `hand=green_catalyst_spore` | card → Spore385 (Spore385ContactSurface) | ○ | ○ |
| `hand=green_weak_point` | card → OwnedAxial (PlayerContactFx) | ○ | ○ (小さい) |
| `hand=blue_glacial_maul` | card → BlueOwnedContact379 (BlueContact379Surface) | ○ | ○ 青い塊 |
| `hand=blue_ripple_blade` | card → BlueOwnedContact379 | ○ | △ 薄い筋 (防がれて 0 ダメの状態で撮れた) |
| `hand=blue_cold_sweep` | card → ColdSweep (ColdSweepSurfaceFx) | ○ | **× 写っていない** (§5) |
| `hand=white_spark_burst` | card → SparkBurst (ScatterProfileFx) | ○ | **× 写っていない** (§5) |
| `hand=white_spark_storm` | card → WarmSpark397 (ScatterProfileFx) | ○ | △ 数字の横に粒が少し |
| `hand=green_guard` | support:green_guard/block → Hd2dSupportFx (.50 秒) | ○ | ○ 自分の前に白い盾の面 |
| `hand=blue_ice_wall;leader=leader_blue` | support:blue_ice_wall/ice-gain (.52 秒) | ○ | ○ 氷の霧 |
| `hand=white_mass_heal;hp=40;leader=leader_white` | support:white_mass_heal/heal (.66 秒) | ○ | ○ 光の弧と輝き |
| `enemy=enc_probe_pair;endplay=1` | enemy:enemy_probe/poke → Hd2dEnemyContactFx (PlayerContactFx) | ○ ×3 | ○ 刺しの閃き |
| `enemy=enc_breaker_hexer;endplay=1` | enemy_hexer/mud・enemy_set_breaker/smash → Hd2dEnemyContactFx | ○ ×2 | ○ 泥の当たり |
| `enemy=enc_cultist_imp;endplay=1` | enemy_cultist/dark_blade → CultistDarkBlade (BoundedContactFx) | ○ | ○ 紫の刃 |
| `enemy=enc_imp_jester;endplay=1` | enemy_cinder_imp/spark_toss → ImpSpark (FireContinuousFx) | ○ | ○ 火の粒 |
| `enemy=enc_squire_archer;endplay=1` | enemy_shield_squire/shield_bash → Hd2dEnemyContactFx (EnemyContactFx) | ○ | ○ |
| `gears=gear_powder;mana=10;usegear=0` | gear:gear_powder → Powder (NamelessPowder) | ○ ×2 (2 体) | ○ 粉の破裂 |
| `hand=red_spreading_fire;endplay=1` | system:burn → BurnTick (BeginSystem) | ○ | ○ 延焼の刻み |
| `sets=black_reaction_curse*;fire=1` | reaction:black_reaction_curse → CurseReturn (ReactionContact) | ○ | ○ |
| `perms=green_perm_thorn_canopy;endplay=1` | permanent → Canopy | × 鳴らず (棘の置物が敵の攻撃で誘発しなかった＝撮影の条件の問題。§5) | − |
| `perms=black_blood_candle;endplay=1` | permanent → InstalledDark | × 同上 | − |
| `hand=green_serpent_gulp` | card → GreenGulp | × 撮れず (捨てるコストの選択の窓が開き自動操縦が止まる) | − |

ダメージの重複・使い回しの残り: 同じ状態を 2 度撮っても描き手の begin は 1 回ずつ。戦闘の終わり (`Cancel`) で `_routeMemo` と約束済みの当たりを捨てる。

## 5. 未検証・差異

### 5-1. 鳴ったが見えなかった (次の作業)
- **冷手の一払い (ColdSweepSurfaceFx)** と **火花の破裂 (SparkBurstContactSurface ← ScatterProfileFx)**: begin は出るが並べたコマに絵が無い。他の Bounded 系 (379・暗い刃・探り屋の刺し) は見えるので、この 2 本のシェーダの出力 (α か `_Coverage`/`_Gain` の既定) を疑う。カメラへ .28 寄せる処方 (§3-8) は効いている (379 が見えるようになった)。
- さざなみの刃: 防がれて 0 の状態でしか撮れていない。素通しの当たりで撮り直す。

### 5-2. 撮れていない経路 (Godot と同じ条件が作れていない)
- 棘の天蓋 (Canopy)・血の蝋燭 (InstalledDark)・置物の当たり全般: `perms=` で置いても敵の攻撃に対して誘発する絵が出なかった (置物の効果の誘発の条件を満たす状態キーが要る)。
- 大蛇の丸呑み (GreenGulp): 追加コストの選択の窓を自動操縦が押せない。
- 亡骸 (唸る頭骨・亡者の口づけ＝香炉の忘却)・無銘のギア (NamelessKeg)・勢いの鞭 (遺物)・蔦の陣形の返し (仕込み札)・霊気の槍 (霊気 2 が要る)・光の矢 (LightBolt)・写し身 (マナ漏出の後の MirrorReturn)・亡霊の呪い (WraithCurse)・絡め捕る根 (VineGrasp)・敵の鈍器の変種・三つ角 (TripleHorn)・大地の咆哮／根付く蔦／白光 396 の光の方針: どれも **撮っていない**。移植は済んでいる (棚卸し §1) が、絵は机上の照合のみ。
- Android 実機: 撮っていない (IL2CPP で Float32 の頂点色の流れが通るかは未確認)。

### 5-3. Godot と違う所 (承知の上の近似)
- 所有権: SessionChange が無いので出来事の記録の区切り (§3-2) で近似。ギアの「使う前の持ち物」は無い → GearUsed の出来事で代える。
- 俳優の「受け」の光 (TripleHorn .12／DoubleLash .16／HolyMaul .10／回復 .08) は移していない。契約の光に範囲が無いので光の方針は量の倍率だけ (§3-6)。
- TripleHorn の受けは旧い PlayerContactFx のまま。
- 絵は 1024／512 に縮めて取り込む (Godot は原寸)。Torrent の「放つまで止める」は姿勢の長さで近似 (白光の流れは 4 コマ目を .42 秒)。
- 色の既定 (役割の 4 色・舞台の光・Bounded の 3 色) は `Data/hd2d-palette-defaults.json` に出した (C# に色の生の値を書かない規約)。

## 6. dot への伝言 (次の節目のお願い)

- 422 で `scripts/presentation/authored_geometry/*.cs` が 1 byte の空ファイルになっている (本体は `RecoveredSource/DeckRogue.Game/`)。混乱の元なので、どちらかに揃えてほしい。
- `attack-*-8f.png` 9 枚はコードから参照が無い。残すなら用途を README に。

## 7. 数え方 (再現用)

```bash
git fetch origin codex/godot-vfx422-unity-handoff-20261102
B=origin/codex/godot-vfx422-unity-handoff-20261102; P=handoffs/godot-project422/project
git archive $B $P | tar -x -C /tmp/p422
# 幾何の差 (cp192 → 422)
diff <(git show origin/codex/unity-parity-20261013:RecoveredSource/DeckRogue.GodotPort.Presentation/Hd2dAttackPrototype.cs) /tmp/p422/$P/RecoveredSource/DeckRogue.GodotPort.Presentation/Hd2dAttackPrototype.cs | grep -c '^[<>]'   # 1631
# 共有の幾何 25 本: 2 本だけ違う (HealContinuousGeometry 52・LocalMaterialProfileGeometry 57)
# シェーダ: 旧い 18 本は 0 差・Hd2dPrototype +68・新規 19
# 絵: comm -23 <(ls $P/assets/Art/fx | grep -v import) <(ls unity/Assets/Resources/Art/fx | grep -v meta)  # 29 → 参照 21
```
