DeckRogue 森ステージ 最終素材統合候補

自然斜面版に、以前のcohesion試作の道23枚だけを統合した候補です。
見た目の最終ユーザー承認は未了。Unity回帰確認・Android buildへ渡すための素材で、APK配布は保留です。

統合内容
- 入力: natural_banks_packed.blend。道の形状と位置の出典: cohesion/assets/mossbound_full_stage.blend。
- 18枚の不規則な階段アプローチと5枚の周辺欠片、計23枚を転送。厚み8.2cmの立体polygon。
- 実名はGround_inset_stone_*。既存のPaving_slab_00〜04は別の5オブジェクトで、完全に無変更。
- Unityで呼ばれたPaving_slab_00〜22はこのsource名と一致しないため、名前だけで置換しない。paving_source_map.jsonとbatch_source_map.jsonを使ってsemantic sourceを確認する。
- 元cohesionの道にあった16組の共面重なりを、接触する17枚の輪郭だけ小さくして解消。縮小率は0.817907〜0.980100。中心、並び、向きは維持。新しい位置移動はなし。
- 縮めた石の角を現在の同じ床に再接地。新しいBoolean切り欠きは採用していない。
- 岩や階段下部との自然な部分交差・遮蔽は維持。全ての交差をゼロにする処理はしていない。プレビューで目立つ宙浮きや階段上面を覆う板は認めなかった。

不変確認
- 自然斜面版と比較し、変更メッシュは上記23枚だけ。残り218ステージメッシュは位置・形状・UVのhash完全一致。
- 接地済み岩、木、シダ、門、階段、土手、床、既存Paving_slab5枚を維持。bank_rock_12は旧配置に戻していない。
- オブジェクトのtransform変更0。ライト・カメラ・霧・画像ピクセル・材質ノード・戦闘マーカーも一致。
- 手前のカード戦闘床と自然斜面の形状は無変更。cohesionの岩・シダ・門の変更は取り込んでいない。

検証結果
- 最終27,818 triangles（自然斜面版28,508から690減）。16 meshes/batches、14 materials。
- 道23枚の上面クリアランス: 0.002907〜0.028800m。底面は-0.084839〜-0.053200mで接地。
- 23枚同士の水平/共面重なり0件。
- GLB再読込で全16meshがUV0 1枚、UV面積ゼロ0件。UV正規化後にbatch化。
- 全画像packedの編集用.blend、画像内包FBX、self-contained GLB。
- 同カメラ・同照明・1600×900・96 samples・denoising OFFのBlender previewを確認。Unity実画面とAndroidの検証は未実施。

Unity向け注意
- 自然斜面版と同様、旧bank_rock_12の94頂点−0.36m runtime位置補正はOFF。二重補正しない。
- 未知の旧index依存geometry補正があれば新モデルへ適用前に棚卸し。旧v3 UV0復旧は不要。
- 光・霧・背景render layer・素材設定などの既存実装は維持する。AOなしの比較では両モデルAO strength=0。
- 下記23枚の対応はsource名が正。番号欠番を詰めて既存Paving_slab_00〜04と混同しない。

全23枚のsource対応（sourceと統合先の名前は同一）
00: Ground_inset_stone_00 | stair_approach | 50→20 triangles | footprint 0.960596
01: Ground_inset_stone_01 | stair_approach | 50→20 triangles | footprint 0.980100
02: Ground_inset_stone_02 | stair_approach | 50→20 triangles | footprint 0.960596
03: Ground_inset_stone_03 | stair_approach | 50→20 triangles | footprint 1.000000
04: Ground_inset_stone_04 | stair_approach | 50→20 triangles | footprint 0.932065
05: Ground_inset_stone_06 | stair_approach | 50→20 triangles | footprint 0.817907
06: Ground_inset_stone_07 | stair_approach | 50→20 triangles | footprint 0.868746
07: Ground_inset_stone_08 | stair_approach | 50→20 triangles | footprint 0.817907
08: Ground_inset_stone_09 | stair_approach | 50→20 triangles | footprint 0.868746
09: Ground_inset_stone_10 | stair_approach | 50→20 triangles | footprint 0.895338
10: Ground_inset_stone_11 | stair_approach | 50→20 triangles | footprint 0.877521
11: Ground_inset_stone_12 | stair_approach | 50→20 triangles | footprint 0.877521
12: Ground_inset_stone_13 | stair_approach | 50→20 triangles | footprint 0.904382
13: Ground_inset_stone_14 | stair_approach | 50→20 triangles | footprint 0.904382
14: Ground_inset_stone_15 | stair_approach | 50→20 triangles | footprint 0.941480
15: Ground_inset_stone_17 | stair_approach | 50→20 triangles | footprint 0.922745
16: Ground_inset_stone_20 | stair_approach | 50→20 triangles | footprint 0.922745
17: Ground_inset_stone_22 | stair_approach | 50→20 triangles | footprint 1.000000
18: Ground_inset_stone_23 | edge_fragment | 50→20 triangles | footprint 1.000000
19: Ground_inset_stone_25 | edge_fragment | 50→20 triangles | footprint 1.000000
20: Ground_inset_stone_28 | edge_fragment | 50→20 triangles | footprint 1.000000
21: Ground_inset_stone_39 | edge_fragment | 50→20 triangles | footprint 1.000000
22: Ground_inset_stone_44 | edge_fragment | 50→20 triangles | footprint 0.960596

主要ファイル
integrated_forest_packed.blend: 編集可能なpacked source
mossbound_full_stage.fbx / .glb: 正規化済みexport
integrated_forest_preview.png: 最終同条件preview
road_integration_diff.json / paving_source_map.json: 変更と正確なsource対応
validation.json / road_contact_validation.json / export_validation.json: 検証結果
static_overlap_validation.json: 許容した自然な部分交差の記録
unchanged_natural_bank_contact_validation.json: 無変更と確認した自然斜面版の接地検証
