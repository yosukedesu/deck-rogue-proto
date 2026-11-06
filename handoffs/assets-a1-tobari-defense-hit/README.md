# A1 とばり — 防御 1・被弾 1

2026-11-06。納品枝 `codex/assets-a1-tobari-defense-hit-20261106`。
公開 `main` の `114ee430310c5a2df02ea048f23d1f8375c22c66` を基点とした、素材だけの追加。

## 中身

- `Art/leaders/anim/leader_black_block_0.png`：両手で鎌の柄を胸の前に構え、膝を曲げて受ける防御。
- `Art/leaders/anim/leader_black_hurt_0.png`：足を残したまま上体を反らし、髪と鎌が遅れて動く被弾。
- 各 **192×128 RGBA、1 コマ、足元 (99,116)**。左上原点。足の最下不透明行は **115**、その直下の境界が y116。指定された攻撃素材と同じ定義。
- `raw/`：生成器が返した 1536×1024 PNG 2 枚。無加工の原本。
- `reference/`：参照した承認済み攻撃 00 の 192×128 と 960×640。元 ZIP とバイト一致。
- `contactsheet.png`：参照・防御・被弾を実寸と 2 倍 Nearest で並べた 1 枚。線と目印はレビューシートだけにあり、納品コマには無い。
- `manifest.json`：ID、ファイル、実寸、足元、コマ数、推奨時間、取り込み先。
- `provenance.json` / `prompts.json` / `export-records.json`：出所、実際の発注文、切り出しと配置の全数値。
- `qa.json` / `checksum_manifest.json`：検査結果とファイルの SHA-256。

## 出所と、確認できた範囲

外見の正本はユーザーが指定した **Tobari_idle_attack_Claude_v1.zip**。その中の
`attack/Tobari_transparent_sprite_v1/frames_192x128/tobari_attack_00.png` と 5 倍原本を使った。
銀灰色の長髪、暗い薔薇の飾り、黒と生成りの服、大きな三日月の鎌を保つ。
旧 `codex/assets-a1-tobari-20261102` の絵を新正本の代わりには使っていない。

**2026-11-06 に確認した GitHub main は 114ee430（11 月 4 日の内容）で、ユーザーが取り込み済みと報告した `scripts/import-leader-tobari.py` と `handoffs/assets-tobari-claude-v1` はまだ公開されていなかった。**
ユーザーの取り込み済み報告を否定するものではなく、今回クラウドから読めた範囲の記録。
そのため未知の取り込み倍率・時間表・コードの分岐は仮定せず、指定 ZIP の攻撃 00 と足元・見かけの身長をそろえた素材を渡す。

元 ZIP の待機は 128×84、攻撃は 192×128。元 README は、同梱した時点では待機と攻撃の縮尺・切替原点を未検証と明記している。
今回の **192×128・(99,116)** は最新のユーザー指定と攻撃側の記録に従う。旧一覧の一般規格 128×84・(60,76) に縮めてはいけない。
最新ゲームでの待機との一致や実際の取り込み倍率は、この枝だけでは確認していない。

新規姿勢は内蔵 imagegen の参照画像編集で制作。後処理は ImageMagick の切り出し、Nearest 縮小、透明キャンバスへの配置だけ。Python による描画・ポーズ生成は行っていない。
原画の権利・出所を引き継ぐ。CC0 や第三者フリー素材とは宣言しない。

## 決めた事

1. 防御は鎌の柄で受ける。被弾は右側からの衝撃に対して上体を左へ逃がす。追加の盾・光・煙・出血・地面・接地影は描かない。VFX と混ぜない。
2. 2 枚とも同じ足元に固定。足先 5 行の左右端は防御 x83〜114、被弾 x87〜110。参照 x86〜111 と同じ中心 x98.5、すなわち画素の境界で x99。最下行も参照と同じ y115。
3. キャラ本体は参照の頭から足まで約 52px を目安にした。防御は約 50px の低い構え、被弾は約 52px。鎌の描画範囲を本体の身長と取り違えない。新しい姿勢に伴う髪・裾・手の幅の差は残す。
4. 生成原本の α を 128 で二値化し、指定矩形を Nearest 縮小して配置した。透明 RGB は 0。raw の RGB/α は変更していない。正本に既に含まれる細かな色を維持し、旧とばりや舞台の色表には写していない。
5. 防御 300ms、被弾 220ms を素材側の推奨保持時間とする。**実ゲームのタイミング変更ではない**。既存の被弾・防御時計がある場合は受け入れ側でそちらを優先する。
6. 1 コマを 4 枚・3 枚に複製する互換セットは作らない。新 importer が未公開なので、必要な重複名や再生方式を推測で固定しない。
7. 取り込み用 2 PNG と付随資料だけを独立フォルダへ追加。既存の待機 8・攻撃 16、立ち絵、BGM、キービジュアル、src/engine、src/data、Unity のコード・設定・法線は触らない。

## 使い方

推奨取り込み先は manifest の `target_file`（`unity/Assets/Resources/Art/leaders/anim/`）。これは納品先の提案であり、今回コピー・インポートしたという意味ではない。
既存の importer で下記の設定とファイル名を確認してから取り込む。

- Point / sRGB / mip 無し / 非圧縮 / NPOT 無変換。
- 左上原点の pivot `(99,116)`。Unity の左下からの正規化 pivot は `(0.515625,0.09375)`。
- 右向きの姿勢。反転する場合は同じ足元を基準に反転する。
- `.meta` や GUID はこの束に含めない。既存 GUID を無造作に置き換えない。
- 既存の待機 8・攻撃 16 は今回の参照画像で上書きしない。`reference/` は比較専用。
- 新しい姿勢専用の法線は含めない。古い静止姿勢の法線を自動で流用しない。

## raw から実寸 PNG を再現する

このフォルダをカレントにして ImageMagick 7 で実行する。PNG ファイルのメタデータ差はあり得るが、再現した RGBA 画素の一致を検査済み。

```sh
magick raw/defense.png -channel A -threshold 50% +channel \
  -crop 788x567+205+342 +repage -filter point -resize 99x71\! \
  -background none -gravity northwest -extent 192x128-27-45 \
  -background black -alpha background PNG32:Art/leaders/anim/leader_black_block_0.png
magick raw/hurt.png -channel A -threshold 50% +channel \
  -crop 839x550+182+379 +repage -filter point -resize 96x63\! \
  -background none -gravity northwest -extent 192x128-30-53 \
  -background black -alpha background PNG32:Art/leaders/anim/leader_black_hurt_0.png
```

## 検査

2 コマの寸法・RGBA・α0/255・透明RGB0・足元の下端と左右中心・全辺の透明余白・ハッシュ差分・rawから再現した画素一致・参照コピーの一致・全PNGのデコード・全JSONの構文を検査した。
実寸と拡大の並べたシートで外見、身長、姿勢の違い、切れや背景の混入を目視確認し、独立したレビューでも素材として問題なし。

Unity/Godot の正式インポート、実ゲームでの切替、Android は **未実施**。生成に伴う髪・服の細部の差は残る。ユーザーの採点や実機の確認に代わるものではない。
採点待ちで後続の束を止めない。
