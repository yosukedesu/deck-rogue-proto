# 本線を Unity と Godot のどちらにするか — 評価（2026-10-26）

ユーザー「Godot 版の UX が悪すぎて、やはり Unity を本筋にしようか悩んでいる。しかし作り直した素材や見た目はかなりいい。どちらを正規で進めるのがいいか評価してほしい」への回答。
比べたのは **main（9c656c8）＋dot の Unity 枝 `codex/hd2d-vfx-motion-20261005`（a7f86b5）** と **dot の Godot 枝 `origin/codex/unity-parity-20261013`（68480465・10/25）**。
数字は全部この日に手元で数えたもの（コマンドは末尾）。

## 0. 結論（推奨）

**Unity を本線に戻す。Godot は製品の線としては閉じ、dot の枝は「素材の倉庫」として残す。**
いいと感じている見た目の正体は **PNG と JSON の素材**で、Godot に縛られていない。札の絵 567 枚は Unity と同じ 80×48 で、そのまま置ける。敵の 8 コマ待機も PNG＋台帳で、読み込み口を一つ書けば Unity で動く。森の舞台の原本（Blender）はもともと Unity の枝にある。
一方、UX の差は「作り込みの量」の差で、Unity には S25 で詰めた UI が約 45,000 行とそれを撮って確かめる道具一式がある。Godot でそこへ追いつくには同じ量を作り直すことになる（dot 自身の受入文書も「ラン全体の同等は未達」と書いている）。

## 1. 事実（何が同じで何が違うか）

### 1-1. ルール（エンジンとデータ）＝同じ

| | Unity（main） | Godot 枝 |
|---|---|---|
| C# エンジン 25 ファイル | `unity/Packages/com.deckrogue.engine/Runtime` | `engine/runtime` — **25/25 バイト一致** |
| JSON 7 本（札5色・敵・リーダー・編成） | `src/data` | `data` — **全部一致**（赤の解凍・段2b まで入っている） |

どちらを選んでもルールの移植は今のまま（TS が正 → C# へ翻訳 → ゴールデン照合）。Godot を選ぶと「Godot へ複製してもう一度照合」が毎回足される。

### 1-2. 素材（dot が作り直したもの）＝ほぼ全部が Unity にそのまま持ち込める

| 素材 | Godot 枝にあるもの | 形式 | Unity へ移す手間 |
|---|---|---|---|
| 札の絵 | `assets/Art/cards_modern` **567 枚**。データの札 466 種を **466/466** 網羅（赤 106/106、合成札も） | PNG **80×48**（Unity の `Art/cards` と同じ寸法） | **コピーだけ**（`Art/cards/<id>.png` → `scripts/sync-web-art.sh` でプロトにも） |
| 敵の待機アニメ | `authored-frames/<敵>/idle_00〜07.png` **89 体×8 コマ＝712 枚**（rest・技のコマ込みで 1,725 枚）＋`clipset.json`（枠・足の位置・コマの長さ） | PNG＋JSON。枠は 136〜256px（今の Unity の規約 64/80/96/128・1ドット＝4px より細かい） | 読み込み口を 1 つ書く（Unity の codex 枝には 64px の `r2idle` 348 枚を回す口が既にある）。**大きさの規約は裁定が要る** |
| 森の舞台 | APK から取り出した圧縮テクスチャ（`.ctex`）＋JSON 124 | 原本の PNG は失われた（dot の文書どおり） | 不要——**原本（Blender・FBX・1,204 ファイル）は Unity の codex 枝にある** |
| 字 | Noto Sans JP Regular／Bold（10/25「Font A」） | — | Unity は最初から Noto Sans JP（TMP） |
| 属性の演出（9 属性） | Godot 向けに描き直した描画系（15 系統） | Godot 専用コード | 不要——**Unity 版が codex 枝にある**（`unity/Assets/Game` 142 ファイル・59,485 行。main は 57・44,903 行） |

比べて見た所見（証跡 PNG を手元で並べた）:
- 札の絵: 新しい 567 枚は「手元・一人称」の構図で全色が同じ調子。今の Unity の 267 枚（風景の構図）とは画風が違うので、**混ぜずにどちらかへ揃える**判断が要る。
- 敵: 新しい斧鬼（144×120）は今の 64×64 より線が細かく、オクトラ寄り。蟹は「岩を割って出る爪」、鹿は角の大きな姿——質は高い。
- どちらも **Godot でなければ出ない絵ではない**。

### 1-3. UI と UX ＝ Unity が大きく先行

| | Unity | Godot 枝 |
|---|---|---|
| 戦闘画面 | 2026-09-07〜10-07 の積み上げ（スマホ配置・帳面・頭上の意図・扇の手札・仕込み札の帯・確認の窓・被ダメ予測・レリック列・演出キュー） | 戦闘の静止画は Unity の写し（配置・森・札の絵まで近い）。dot の文書で未達と明記: 手札の扇と持ち上がり・演出中の入力停止・字は Godot の素のラスタ（TMP の SDF ではない）・HUD の重なり・画面を速く切り替えた時の callback 残り・終了時の handle エラー |
| ラン画面（地図・店・工房・焚き火・報酬・イベント） | 絵本の肌（紙・栞・一面の棚・タップ＝選ぶ・落書き・レリック一覧） | **平らな暗い板と汎用ボタン**（工房 `manual-085`・戦利品 `manual-110`）。dot: 「ラン全体・メニュー全体の同等は含意しない」 |
| S25 実機 | APK 10/5・10/7（108MB）で確認済み。IL2CPP の穴も踏み済み | APK は **10/12 の 1 本だけ**（169MB・UI 未達と判定済み）。10/13〜10/25 の作業は APK になっていない（枝に Android の export 設定が無い・`project.godot` は「Attack Preview 20261014 Windows」） |

### 1-4. コードの健康 ＝ Godot 枝は「復元物」

- **2026-10-12 に dot のクラウド作業場が消えた**（`recovery-provenance/WORKSPACE_RECOVERY_INCIDENT.md`）。push していない履歴 `b05a0da` ごと失われ、公開済み APK から **ILSpy で逆コンパイルして作り直した**。
- 今コンパイルされる 487 ファイルの内訳: **431 が `RecoveredSource`（復元）・56 が `scripts/`（手書き）**。`DeckRogue/` の 213 ファイルは写しでコンパイル対象外。
- 原本の PNG・フォント・音が無いので `.godot/imported`（取り込みキャッシュ 2,002 ファイル）をコミットして動かしている（dot: 「エディタで再取り込みできるかは別作業」）。
- 枝の pack は 483MB。証跡 PNG 約 1,900 枚を含む。
- 今は origin に push されている（これは良い）。

### 1-5. 道具 ＝ Unity は手元で回る、Godot は手元に無い

- Unity: `scripts/unity-win.sh {compile|verify|shots|android|install}`、状態へ跳んで撮る `STATE=…`、常駐 Editor、`adb`。Claude が全画面を自分で見て直せる。
- Godot: Windows にも WSL にも Godot と dotnet が無い。dot のクラウド（ソフト描画 llvmpipe）でしか動かない。dot 自身「Android/S25 の実測（描画の滑らかさ・タッチ・音）は未計測」。
- dot は PC の利用上限で止まっており、クラウドでしか動けない。**Godot を選んだ動機は「dot がクラウドで組んで確かめられる」で、製品の都合ではなかった**（10/12 の共有文の原文）。

### 1-6. 配布と費用（決め手ではないが記録）

- Unity 6 Personal は年商 20 万ドル未満なら無料（ランタイム料金は 2024-09 に撤回）。Godot は MIT。今の規模では差にならない。
- Android はどちらも出せる。Unity の IL2CPP は S25 で通し済み。Godot .NET の Android は 4.2 以降の対応で、ここではまだ 1 本しか実機に乗っていない。

## 2. 軸ごとの評価

| 軸 | Unity | Godot | 重み |
|---|---|---|---|
| ルールの一致 | ◎ 同じ | ◎ 同じ（複製が 1 段増える） | — |
| 見た目（素材） | ◎ 全部持ち込める | ◎ 既に入っている | 低（引き分け） |
| UX の完成度 | ◎ S25 で詰めた | △ 戦闘は写し・ラン画面は平板 | **高** |
| 直す速さ | ◎ 撮って確かめる道具がある | × 手元に無い・クラウドだけ | **高** |
| コードの信頼 | ○ 手書き・履歴あり | △ 431/487 が逆コンパイル復元 | 中 |
| 事故への強さ | ○（ただし main は 23 コミット未 push） | △ 一度消えた。今は push 済み | 中 |
| dot の作業しやすさ | △ dot はクラウドで Unity を動かせない | ◎ | 中 |
| 製品化の近さ | ○ あと素材の差し替えと赤の人間ラン | × UX の作り直しが丸ごと残る | **高** |

## 3. 推奨の理由（3 つ）

1. **「いい」の正体は素材で、素材は引っ越せる。** 札 567 枚は寸法まで同じ。敵のコマは PNG＋台帳。舞台と演出の原本は Unity 側にある。Godot を捨てても見た目は失わない。
2. **UX は作った量がすべてで、Unity の量に Godot が追いつく見込みが立っていない。** dot の受入文書（`ACCEPTANCE_PLAN.md`・`CHECKPOINT_SCOPE.md`）が自分で「同等は未達・字は違う・ラン全体は含意しない」と書いており、10/12 以降の作業は実機にも乗っていない。追いつく作業はそのまま「Unity をもう一度作る」こと。
3. **Godot 版は一度消えて逆コンパイルから起こしたもの。** 原本が無い素材を取り込みキャッシュごとコミットして動かしている状態で、ここを製品の土台にするのは危ない。Unity は手書きで履歴があり、手元で撮って確かめられる。

## 4. Godot を選ぶ理由があるとすれば

- dot がクラウドで自走できる（Unity はクラウドで動かない）。→ ただし dot の仕事の価値は**素材**にあり、素材は PNG で納品できる。エンジン内の確認は Claude が Windows で撮って返せる（10/12 の `ux-ref` と同じ運用）。
- MIT で将来の料金リスクが無い。→ 今の規模では効かない。
- 「写し」とはいえ戦闘画面はかなり Unity に近づいた。→ 写しは常に元より遅れる。元を本線にするのが自然。

## 5. Unity を本線にする場合の手順

1. **push**（main 23 コミット・codex 枝 a7f86b5）。10/12 の事故の教訓。
2. **codex の Unity 枝を main へ合流**（38 コミット。衝突 5 本＝`combat.ts`・`effects.ts`・`types.ts`・`Combat.cs`・`Effects.cs` は main に dot の解消済みが入っているので main 側を採る → `npm test`・C# verify 15/15・コンパイル → shots）。`unity-colors.test` の 2 件は dot が別件と明記。
3. **札の絵 567 枚を Unity へ**（`Art/cards/<id>.png`・Point・非圧縮 → `sync-web-art.sh`）。**画風を新しい方へ揃えるか**は裁定（§7）。
4. **敵の 8 コマ待機を Unity へ**（`clipset.json` を読む口を `Stage`/敵スプライトの経路に足す）。**大きさの規約（1ドット＝4px・64/80/96/128）をどう変えるか**は裁定。
5. **文書を戻す**: CLAUDE.md 冒頭・ledger「移植先（Unity → Godot）」・`docs/unity-port.md` 冒頭の「Godot へ一本化」を「Unity 本線・Godot は閉じる（素材の倉庫として枝を残す）」へ。経緯はこの文書。
6. **dot へ**: 新素材（札・敵のコマ・字）は受け入れて Unity に入れる／以後の納品は PNG＋JSON（エンジンに依らない形）／Unity の演出は codex 枝で続ける／Godot の移植は止める・クラウドの APK は不要／エンジン内の確認は Claude が撮って返す。
7. その後は元の予定へ: **赤の人間ラン**（赤は 0 本）→ ひばなの核（dot）→ 挿絵。

## 6. Godot を本線にする場合に要ること（比べるため）

- Windows か WSL に Godot 4.6 .NET と .NET 9 SDK を入れ、`unity-win.sh` 相当（状態へ跳んで撮る・verify・APK・adb）を Godot で作り直す。
- ラン画面 6 種と戦闘の未達（扇・持ち上がり・入力停止・字・HUD の重なり）を Godot で作り直す＝Unity の約 45,000 行ぶん。
- 復元コード 431 ファイルを読める手書きに戻すか、復元のまま抱えるかを決める。原本の無い素材の扱いも。
- Unity の道具と codex 枝の演出（59,485 行）は捨てる。

## 7. 裁定待ち

1. 本線を Unity に戻すか（推奨: 戻す）。
2. 札の絵を新しい 567 枚へ全色揃えるか（推奨: 揃える。今の 267 枚と画風が違うので混ぜない）。
3. 敵の大きなコマ（136〜256px）を採るか・採るなら表示の規約をどうするか（後で別に裁定）。

## 8. 数え方（再現用）

```bash
git fetch --all --prune
B=origin/codex/unity-parity-20261013
# ルールの一致
for f in $(git ls-tree -r --name-only $B | grep -E "^engine/runtime/.*\.cs$"); do rel=${f#engine/runtime/}; [ "$(git rev-parse main:unity/Packages/com.deckrogue.engine/Runtime/$rel)" = "$(git rev-parse $B:$f)" ] || echo DIFF $rel; done
# 札の絵の網羅（node で src/data/cards.*.json の id と assets/Art/cards_modern の png を突き合わせ）
# コンパイル対象の内訳
git show $B:DeckRogue.csproj | grep -c 'Compile Include="RecoveredSource/'   # 431
git show $B:DeckRogue.csproj | grep -c 'Compile Include="scripts/'           # 56
# dot の Unity 枝と main の衝突
git merge-tree --write-tree main codex/hd2d-vfx-motion-20261005 | grep ^CONFLICT
```

証跡として見た画像: Godot 枝 `recovery-evidence/unity-parity-rich-20261013/float-size/float-phone130/frame-0040.png`・`native-ownership/green_double_lash/frame-0030.png`・`run-controls/pixels/manual-085.png`（工房）・`manual-110.png`（戦利品）／Unity `~/projects/deck-rogue-godot-sync/ux-ref-a7f86b5/ux-target3/01-ux-target3.png`。
