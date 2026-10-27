# dot の Godot 枝（codex/unity-parity-20261013）を Unity へ移す — 棚卸しと移植の記録（2026-10-27）

ユーザー「codex/unity-parity-20261013 が現在 dot が作業しているブランチ。これも Unity に移植する必要がある」。
対象は dot の枝の先頭 **8834bd18（2026-10-27 19:26・"Add validated HD-2D combat presentation checkpoint 192"）**。10/26 の評価（`docs/engine-choice-2026-10-26.md`）は 68480465（10/25）までを見ていたので、その後に増えた分を含めて棚卸しし直した。

## 0. 結論

- **Unity に無かったものは 4 つ**: ①HD-2D の攻撃演出（10/27 の checkpoint 192。最大） ②字を全画面 Noto Sans JP にする「Font A」（10/25） ③敵の技ごとのコマの使い方（10/21〜23） ④①の音 6 本。
- **①と④を今日 Unity に移した**（この文書の §3）。②は見た目の裁定なので ask_user、③は次の段。
- それ以外（ルール・データ・札の絵・敵のコマ・舞台・遺物・ギア・人形・アイコン・地図・色の表・音）は **Unity と同一**だった（§1）。dot の枝の UI コード 100 本は Unity の UI を Godot に写したものなので移す対象ではない。

## 1. 棚卸し（同じもの・違うもの）

数え方は末尾 §6。比較は `origin/codex/unity-parity-20261013`（8834bd18）対 main（6a28cf67）。

| 種類 | dot の枝 | Unity main | 判定 |
|---|---|---|---|
| ルール（C# 25 本） | `engine/runtime` | `unity/Packages/com.deckrogue.engine/Runtime` | **25/25 バイト一致** |
| データ JSON 14 本 | `data/*.json` | `src/data` | **14/14 一致** |
| 札の絵 567 | `assets/Art/cards_modern` | `Art/cards` | **567/567 一致**（10/26 に取り込み済み） |
| 敵の一枚絵・コマ 89 体 | `authored-frames/<slug>/` | `Art/enemies/dot/<id>/` | 取り込み済み（10/26）。技ごとのコマは未使用（§4） |
| リーダー 155・遺物 103・ギア 36・人形 30・アイコン 39・小物 120・タイル 67・地図 8・背景 3・場面 4・舞台 466 | `assets/Art/*` | `Art/*` | **画素一致**（dot の PNG は再圧縮で bytes が違うだけ。抽出 60 枚を PIL で比較） |
| 舞台の配置と見え方 JSON 124 | `assets/Stage` | `Resources/Stage` | **124/124 一致** |
| 演出の色の表 17 | `assets/presentation-data` | `Resources/Data` | 17/17 一致 ＋ **hd2d-*.json 15 本が新しい**（→ 取り込み） |
| 札→演出の表 | `card-vfx-profiles.json` | `Data/card-vfx.json` | **同じ中身**（整形違い） |
| 音の表と素材 | `assets/Audio` | `Resources/Audio` | 同じ ＋ **hd2d_three の wav 6 本と鍵 6 つが新しい**（→ 取り込み） |
| 字 | Noto Sans JP・Klee One・Kaisei Decol | 同じ 3 書体 | 同じファイル。**使い分けが違う**（§4 Font A） |
| 演出の幾何（DeckRogue.Game 25 本） | ILSpy で展開した写し | 原本 | 内容は同じ（dot の文書「byte-identical original」。枝で触っていない） |
| HD-2D の攻撃演出 | `Hd2dAttackPrototype.cs` 1,862 行＋表 9 本＋シェーダ 329 行＋絵 46 枚＋BattleStage/BattleActor の統合 ～700 行 | **無い** | **→ 移植（§3）** |
| 敵の技ごとの動き | `Style04EnemyMotion.cs` 1,397 行・`EnemyAuthoredClipSet.cs` 2,025 行・技ごとの計画 10 本 | `BindUnit` は待機 8・被弾・防御・攻撃 3 だけ | **→ 次の段（§4）** |
| 敵の動きの確認窓 | `EnemyMotionDebugMenu.cs`（DEBUG のみ） | shots の STATE で代用 | 任意 |

dot の枝の 15 コミットのうち、10/13 の 11 本は「Unity の見え方を Godot で再現する」作業（復元）で、Unity へ戻すものは無い。10/21〜27 の 4 本が今日の対象。

## 2. 何を移したか（HD-2D の攻撃演出）

Godot 側の層と Unity 側の置き場。**幾何と時間の数値は一切変えていない**（dot が Godot で続きを出しても `diff` が取れる形）。

| Godot（dot） | Unity（今日） | どう移したか |
|---|---|---|
| `Hd2dAttackPrototype.cs`（1,862 行。斬り・殴り・突き・踏み・火・水・氷・光・闇・散り・地割れ・木・蔦・雷・守り・回復・置物・延焼・敵の突き） | `unity/Assets/Game/Hd2d/Hd2dAttackPrototype.cs` | **ほぼそのまま**。変えたのは `using Godot`→`Hd2dMath`・名前空間・`Enabled` の旗（OS の引数→`HD2DFlags.Hd2dFx`）・.NET Standard 2.1 に無い `Array.Clear(1引数)`/`MathF.Tau` の 3 点 |
| Godot の `Vector3`/`Vector2` | `Hd2dMath.cs` | Godot 風（X/Y/Z・`Normalized()`・`Lerp`・`DistanceTo`・`Back=+Z`）の構造体。UnityEngine の型と暗黙変換 |
| `PhysicalVfxProfiles.cs`（札 73 種）・`ElementalVfxProfiles.cs`（札 35 種）・`SourceShowcaseProfile`・`AttackRoute`・`NativeBatch`・`NativeEffect` | 同名で `Hd2d/` に | そのまま（名前空間だけ） |
| `authored_Hd2dPrototype.gdshader`（329 行） | `Resources/Shaders/Hd2dPrototype.shader` | URP の HLSL に写した。`_Kind`・`_TextureMotion` の番号と式は同じ。頂点の色は COLOR（役割の色）× TEXCOORD1.x（明るさ）で HDR を保つ（Unity の頂点色は 8bit） |
| `AuthoredGeometryFx.cs`（Begin・ConfigureMaterial・VertexColor・WorldPoint・Sample） | `Hd2d/Hd2dAttackFx.cs` | Unity の流れ（`ResolvedCombatFx.Ticket` → `WorldBattleFx.BeginAuthoredAttack`/`AuthoredAttackContact` → `AttackPlayback.Land`）に合わせて書き直し。Mesh を層ごとに 1 枚・MaterialPropertyBlock で層の値・接触の光は `StageFx.BeginContractAttackLight` |
| `BattleActor.PrototypeAxeContactPoint`・`TryPrototypeOpaqueSurface`（斧の先・絵の不透明な縁） | `Hd2d/StageHd2dAnchors.cs`（`Stage.TryGetUnitAnchor`・`TryUnitSurface`） | 板の足元中央から、コマの uv（左上が原点）を世界の点へ。リーダー 3 種の斧の先・振りかぶり・杖の石の画素の位置は dot の表そのまま。縁は不透明 (α≥.4) が横に 3 画素続く行の四分位 |
| `BattleStage.PrototypeStrikeApproach`（斧が相手の縁に届くまで寄る・体が先に当たるなら遠隔打ち） | `Hd2dAttackFx.Approach` ＋ `Stage.ShowcaseApproach` の引数追加（`reachWorld`・`poseLead`・`attackFrameDur`） | 寄る距離＝斧の先→相手の縁。体の前が相手の上半身に当たる手前で止める。他の敵が間にいる判定は未（§5） |
| `BattleActor.ApplyHd2dPose`（攻撃のコマの長さ .10/.075/.100/.18） | `Hd2dAttackFx.PoseDurations` → `ShowcaseApproach` が一時的に差し替え | 当たりの `PoseLead`（.22・火 .28）前に攻撃のコマを始める（旧 .12） |
| `BattleAudio`（`Hd2dOnset.<札>`・`Hd2dContact.<札>`） | `Hd2dAttackFx.Begin`（予備動作）・`Presenter`（当たり） | 鍵と音量は `audio.json` に dot の値 |
| `assets/Art/fx/hd2d-*.png` 37 枚 | `Resources/Art/fx/`（`ArtImporter` に hd2d-* の規則＝sRGB・512 に縮める・ミップ・Bilinear） | Godot の import（size_limit 512・mipmaps）と同じ |
| `assets/presentation-data/hd2d-*.json` 15 本 | `Resources/Data/` | そのまま |
| `assets/Audio/hd2d_three/*.wav` 6 本 | `Resources/Audio/sfx/`・`CREDITS-hd2d-three.md` | CC0（Kenney・qubodup・AntumDeluge・Joseph SARDIN）。dot の注記「試聴はしていない」 |

**旗**: `HD2DFlags.Hd2dFx`（STATE `hd2dfx=0|1`、既定 1）。**合わせて `CombatPresentationDefault` を true に**（dot の Godot 版は `SourceShowcaseEnabled=true`＝ユーザーが良いと感じた見た目は「見せる」側。旧 Unity は定義 `DECKROGUE_IMPACT_SHOWCASE` のビルドだけ。比べる時は `combatpresentation=baseline`）。

受ける札: `PhysicalVfxProfiles.ForCard`（73）＋`ElementalVfxProfiles.ForCard`（35）＝**108 種**の直接の札の当たり（`SourceKind=="card"`）。それ以外の札は今までの描き手のまま。

## 3. 検証

- Unity コンパイル 0 エラー（`scripts/unity-win.sh compile`）。
- 撮影（`STATE="phase=combat;enemy=enemy_probe;hand=<札>;play=0;playshots=16;playevery=3"`）: §3-1 に結果を書く。

### 3-1. 撮影の結果（2026-10-28）

`STATE="phase=combat;enemy=enemy_probe;hand=<札>;play=0;playshots=10;playevery=1"` で 9 札（斬り green_strike・殴り green_basic_bash・突き green_weak_point・踏み green_finisher_stomp・火 red_strike・水 blue_strike・光 white_strike・闇 black_drain・蔦 green_double_lash）。

- 最初のビルドは**層が一枚も描かれなかった**。原因＝Godot の `ArrayMesh` は 512 個の添字をそのまま受けるが、Unity の `Mesh.triangles` は 3 の倍数でないと "Failed setting triangles" で三角形が空になる（player.log に 4 行）。見えていたのは旧い細い筋（`WorldBattleFx.Trace`）と接触の光だけだった。`Hd2dAttackFx.Acquire` で 510 個に切って解決。
- 直した後: 斬り＝斧の軌跡（当たりの前）→ 大きな温色の三日月の刃が相手の縁から走って裂け、破片と火花／殴り＝圧の塊と破片／踏み＝宙に浮く足の影（hd2d-stomp-aura）が降り、圧の扇と岩と砂／火＝杖の石から火の玉が飛んで着弾の火の塊 4 相／水＝帯が飛んで濡れた面と滴／闇＝紫の収縮／蔦＝緑の繊維。全部 dot の幾何のまま（数値は触っていない）。主人公は斧の先が相手の不透明な縁に届く所まで踏み込む（距離 7.79 のうち 7.79＝縁まで）。音は Hd2dOnset/Hd2dContact が鳴った（player.log）。
- 気になる点（dot の Godot 版と見比べる必要）: 斬りの当たりの前の「斧の軌跡」が点線に見える（軌跡の 12 区分の UV のつなぎ）／踏みの足の影が相手の左上に出る（dot は相手の真上。板の基底の違い）／火の塊が相手より大きい（dot の数値どおりだが、こちらの舞台では相手が小さい）。
- 敵の技のコマ（`STATE="phase=combat;enemy=enemy_brute;endplay=1;endshots=30;endevery=3"`）: 小泥は棍棒を振り上げて止まり（予備動作の最後）、自分への当たりの瞬間に振り下ろしのコマ、戻って待機。狼は噛みつきの構え→当たり→戻り。最初は当たりで進まず 1.2 秒の時間切れで動いていた（当たりの演出の本道 `Presenter` の `RunContact` に `ReleaseEnemyMove` が無かった）→ 直した。
- 旧い当たりの演出（細い筋・UI の斬撃）は HD-2D の札では出さない（Presenter の `ctx.Hit.Style=="slash"` の筋を `Hd2dAttackFx.Eligible` で抑止）。

## 4. まだ移していないもの

1. **Font A（字を全画面 Noto Sans JP に）**: dot は 10/25 に本文・見出し・ボタン・札の名前・HUD を Noto Sans JP（見出しとボタンは Bold）に揃えた。Unity は本文＝Klee One・見出し＝Kaisei Decol・札の面だけ Noto（W09）。**見た目の裁定なので ask_user**。
2. **敵の技ごとのコマ → 2026-10-28 に移植済み**（下の §4-2 の計画どおり。`scripts/import-enemy-moves.py`＝89 体・285 技・891 枚＋法線。`<id>_m_<技>_<n>.png`＋`<id>_moves.json`。Unity 側＝`StageUnits.BindUnit` が `Anims["m:<技>"]` に読み、`Stage.PlayEnemyMove`（行動の実行 `EnemyActionExecuting` で技のコマを始める。無ければ attack/block）→ 予備動作の最後のコマで止まり → `Stage.ReleaseEnemyMove`（自分への当たりの演出 `PresentEnemyContribution`）で当たりのコマへ。1.2 秒で時間切れ。合わせて敵の被弾のコマ `hurt` を自分の札の当たりで再生）。dot の Godot 側の所有権の証明（ハッシュ・epoch）は写していない。**旧記録（計画）**: clipset の `clips`（windup/impact/recovery・`<語幹>prepare`→`<語幹>contact|impact|release|roar|call|hold|gesture|pulse|apply`→（`<語幹>rebound`→`<語幹>contact2`）→`recovery|<語幹>return`）を、技 id の語幹（`club_wild`→wild・`war_roar`→warroar・`acid_spit`→spit…）で引く規約を試したら、**敵の技 285 のうち 267 が機械的に引けた（89 体中 76 体は全部）**。残り 18 は別名の表で足りる（wolf の rend＝2 つめの素の攻撃、chimera の claws/lacerate→claw、stab_book の stab2〜4→stab、thief の sneak/snatch/run_off→sneak/reach・stow/run・flee、warden の bell_hushed→hush、winch_warden の release_orbs→summon、colossus の swing→attack、drummer の war_drum→prepare/beat、iron_clam の pressurize→pressure、crab_cannon の recharge、crossbow の aimed_shot＝素の攻撃）。やること＝①`scripts/import-enemy-dotset.py` に技ごとのコマの書き出し（`<id>_m_<技>_<n>.png`＋秒の表 `<id>_moves.json`）②`BindUnit` が読む ③**敵の攻撃のコマを鳴らす口**（今の Unity は敵の `attack` のコマを一度も再生していない＝`Stage.PlayAnim` の呼び手は主人公だけ。敵の行動の実行（`EnemyActionExecuting`）で技のコマを始め、当たり（`DamageDealt`）に `contact` のコマを合わせる）。dot の Godot 側（`Style04EnemyMotion`・`EnemyAuthoredClipSet` 計 3,400 行）はハッシュ検証と所有権の証明の塊なので、写すのは表と規約だけ。
3. **攻撃以外の HD-2D 演出の配線**（未）: 守り（blue_guard・green_guard…の最初の正のブロック/氷壁）・回復（white_prayer_verse）・延焼ティック・置物の当たり（black_blood_candle ほか 9）・死者の当たり（black_corpse_kiss）・確認済みの返し（black_reaction_curse）・敵の突き（enemy_probe/poke）・イゼットの雷（blue_aether_burst）。幾何は移植済み（`CreateIceGuard`・`CreatePrayerHeal`・`CreateBurnStatus`・`CreatePermanentPulse`・`CreateNecroDark`・`CreateReactionDark`・`CreateEnemyPoke`・`CreateLightningDischarge`）。Godot 側は出来事の受領（`CombatPresentationLedger`）で厳しく門を立てている（`SupportVfxProfiles` ほか）。Unity の `Presenter` の同じ場所に門を足す。
4. **間に別の敵がいる時の遠隔打ち**（`PrototypeApproachProfile`）と、**舞台の暗転・暖色の山**（`StageWorld.SetAttackStaging`＝Godot 側の森の明かりの操作）。Unity は `StageMotion.BeginDim` が別にあるので、要るか見てから。
5. 敵の動きの確認窓（DEBUG）。

## 5. 近似にした所（Godot と違うかもしれない所）

- 相手の原点＝足元＋高さの半分（Godot は俳優の `Contact` 点）。接触の瞬間に斧の届く縁へ寄せて固定する式は同じ。
- 大きさ: 物理の家族は 1、属性は当たりの強さの段（small .88／medium 1／large 1.12）。Godot は経路の元の描き手の名前で分けていた。
- 光: 接触後だけ（Godot は火の接近中にも少し点く）。地面の光の減衰は目標の光と同じ包絡線。
- 相手の縁の探索は板の高さ方向を「上」として計算（板はレイアウト用カメラの回転）。
- 板の枡の底の中央 ＝ `FeetWorld − 上×(FeetPad×高さ) − 右×((FootAnchorX−.5)×幅)`。幅は元絵の縦横比から。

## 6. 数え方（再現用）

```bash
git fetch origin codex/unity-parity-20261013
B=origin/codex/unity-parity-20261013
git log --oneline $B | head                                  # 15 コミット（10/13 ×11・10/21・10/23・10/25・10/27）
# ルール・データ
for f in $(git ls-tree -r --name-only $B engine/runtime); do bn=$(basename $f); m=$(git ls-tree -r --name-only HEAD unity/Packages/com.deckrogue.engine | grep "/$bn$"); [ "$(git rev-parse $B:$f)" = "$(git rev-parse HEAD:$m)" ] && echo SAME || echo DIFF $bn; done | sort | uniq -c
# 絵（画素比較は PIL。bytes は再圧縮で違う）
python3 - <<'EOF'
# ... docs/dot-godot-port-2026-10-27.md §1 の表は、カテゴリごとに 6 枚ずつ ImageChops.difference で比べた
EOF
# fx の差
comm -23 <(git ls-tree -r --name-only $B assets/Art/fx | grep -v '\.import$' | xargs -n1 basename | sort) <(ls unity/Assets/Resources/Art/fx | grep -v '\.meta$' | sort)
```
