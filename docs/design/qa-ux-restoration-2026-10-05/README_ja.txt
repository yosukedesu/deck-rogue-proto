みぞれ待機モーション位置ずれ修正

修正内容
・leader_blue_idle_0.pngのみ、画像内容を左へ8px移動。
・対応する通常画像leader_blue.pngとatlas左上セルも同期。
・128×84 RGBA、足元y76、pivot (60/128, 8/84) を維持。
・他7枚のidle、attack4枚、block4枚は元PNGとバイト単位で一致。
・顔、衣装、頭身、向き、色、髪や布の動きは変更なし。再生成・伸縮・補間なし。

原因
旧export_sprites.pyは最下端から上24pxを含む25行の範囲で足位置を算出していた。
idle_0のみ杖の左下端が範囲に入り、source anchor xが192.5から160.5へ変化。
描画オフセットdxが他フレームの10pxに対して18pxになり、全身が右へ8px移動していた。
idleは足裏だけが入る下端8行からanchorを計算するよう修正した。
attackとblockの算出方法は変更していない。

置き換え用ファイル
・leader_blue_idle_0.png
・leader_blue.png
・leader_blue_atlas.png（atlasを使う場合のみ）
既存PNGを差し替える際はUnityの.metaファイルと既存import/pivot設定を維持すること。
PNG個別読込とatlas読込を重複追加する必要はない。使用中の経路に合わせて差し替える。

検証
・idle_0はRGBA全ピクセルが厳密な(-8, 0)整数移動のみ。
・非透明ピクセル数は2139のまま。杖・髪を含め切れたピクセルは0。
・旧靴底x56–79から、新靴底x48–71へ移動。y76は不変。
・全8idleの接地マスクはy75–76で一致。0→1と7→0の両ループ境界を確認。
・他15枚は元PNGとバイト単位で一致。atlasの他15セルはピクセル一致。
・Unity再import、ゲーム内transform、Android実機での最終確認は別工程。

同梱
・qa/idle_registration_before_after.png: 同一canvas、足元基準線付きの全8枚比較
・qa/idle_registration_before_after.gif: 左が修正前、右が修正後、各125ms
・qa/verification.json: 検証結果とSHA-256
・idle_preview.gif: 修正後のプレビュー
・sprite_manifest.json: 修正履歴を加えた仕様
・export_sprites_idle_anchor.patch: 元exporterへの最小差分
・export_sprites.py: anchor修正済みexporter（元の承認済みsource画像のパスを使用）
・apply_idle_anchor_fix.py: 元の出力PNGから同じ3枚を再現するPillow用スクリプト
  使用例: python apply_idle_anchor_fix.py ORIGINAL_EXPORT_DIR NEW_OUTPUT_DIR
  入力SHA-256を確認し、入力とは別のフォルダへ出力。二重適用を拒否する。
