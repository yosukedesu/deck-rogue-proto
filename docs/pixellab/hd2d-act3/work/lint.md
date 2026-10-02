# 幕3 の絵の点検 (gen_items.py lint が書く)

門: 半透明 0 (α は 0 か 255)・面積 30,000 以下・色表からの距離 p95 = 0 (法線 _n と発光 _e は色でないので測らない)・小札は背丈 12 以下・タイルは 64×64。ざらつき = ラプラシアン分散 (tile-calm の check と同じ・石積みは目地の段差を含む全体の値。石の面だけの値は index.json の faceLapvar)。光 = art-lint の lint_character の描き込まれた光の向き。

| 絵 | 大きさ | 面積 | 不透明 | 半透明 | 色表 p95 | ざらつき | 彩度の中央値 | 明るさ | 光 | 判定 |
|---|---|---|---|---|---|---|---|---|---|---|
| litter/chip_1.png | 7×3 | 21 | 0.571 | 0 | 0.0 | — | — | — | — | OK |
| litter/chip_2.png | 8×3 | 24 | 0.5 | 0 | 0.0 | — | — | — | — | OK |
| litter/chip_3.png | 7×4 | 28 | 0.5 | 0 | 0.0 | — | — | — | — | OK |
| litter/chip_4.png | 8×3 | 24 | 0.583 | 0 | 0.0 | — | — | — | — | OK |
| litter/gravel_1.png | 7×4 | 28 | 0.429 | 0 | 0.0 | — | — | — | — | OK |
| litter/gravel_2.png | 9×4 | 36 | 0.444 | 0 | 0.0 | — | — | — | — | OK |
| litter/gravel_3.png | 6×3 | 18 | 0.5 | 0 | 0.0 | — | — | — | — | OK |
| litter/gravel_4.png | 10×4 | 40 | 0.4 | 0 | 0.0 | — | — | — | — | OK |
| litter/pebble_1.png | 4×3 | 12 | 0.75 | 0 | 0.0 | — | — | — | — | OK |
| litter/pebble_2.png | 6×4 | 24 | 0.75 | 0 | 0.0 | — | — | — | — | OK |
| litter/pebble_3.png | 3×3 | 9 | 0.778 | 0 | 0.0 | — | — | — | — | OK |
| litter/pebble_4.png | 6×4 | 24 | 0.708 | 0 | 0.0 | — | — | — | — | OK |
| litter/rubble_1.png | 12×5 | 60 | 0.567 | 0 | 0.0 | — | — | — | — | OK |
| litter/rubble_2.png | 13×5 | 65 | 0.569 | 0 | 0.0 | — | — | — | — | OK |
| litter/rubble_3.png | 9×5 | 45 | 0.556 | 0 | 0.0 | — | — | — | — | OK |
| litter/rubble_4.png | 16×6 | 96 | 0.531 | 0 | 0.0 | — | — | — | — | OK |
| relief/aqueduct_far.png | 190×50 | 9500 | 0.507 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/aqueduct_far_e.png | 190×50 | 9500 | 0.507 | 0 | — | — | — | — | — | OK |
| relief/aqueduct_far_n.png | 190×50 | 9500 | 0.507 | 0 | — | — | — | — | — | OK |
| relief/block.png | 38×33 | 1254 | 0.569 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/block_n.png | 38×33 | 1254 | 0.569 | 0 | — | — | — | — | — | OK |
| relief/boiler_pipes.png | 78×138 | 10764 | 0.595 | 0 | 0.0 | — | — | — | 左上 | OK |
| relief/boiler_pipes_e.png | 78×138 | 10764 | 0.595 | 0 | — | — | — | — | — | OK |
| relief/boiler_pipes_n.png | 78×138 | 10764 | 0.595 | 0 | — | — | — | — | — | OK |
| relief/brazier.png | 30×52 | 1560 | 0.419 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/brazier_cold.png | 30×52 | 1560 | 0.419 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/brazier_cold_e.png | 30×52 | 1560 | 0.419 | 0 | — | — | — | — | — | OK |
| relief/brazier_cold_n.png | 30×52 | 1560 | 0.419 | 0 | — | — | — | — | — | OK |
| relief/brazier_e.png | 30×52 | 1560 | 0.419 | 0 | — | — | — | — | — | OK |
| relief/brazier_n.png | 30×52 | 1560 | 0.419 | 0 | — | — | — | — | — | OK |
| relief/ceiling_fissure.png | 131×29 | 3799 | 0.747 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/ceiling_fissure_e.png | 131×29 | 3799 | 0.747 | 0 | — | — | — | — | — | OK |
| relief/ceiling_fissure_n.png | 131×29 | 3799 | 0.747 | 0 | — | — | — | — | — | OK |
| relief/ceiling_fringe.png | 200×81 | 16200 | 0.52 | 0 | 0.0 | — | — | — | 下 | OK |
| relief/ceiling_fringe_n.png | 200×81 | 16200 | 0.52 | 0 | — | — | — | — | — | OK |
| relief/chain_hang.png | 25×155 | 3875 | 0.27 | 0 | 0.0 | — | — | — | 下 | OK |
| relief/chain_hang_n.png | 25×155 | 3875 | 0.27 | 0 | — | — | — | — | — | OK |
| relief/crystal_cluster.png | 63×76 | 4788 | 0.531 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/crystal_cluster_e.png | 63×76 | 4788 | 0.531 | 0 | — | — | — | — | — | OK |
| relief/crystal_cluster_n.png | 63×76 | 4788 | 0.531 | 0 | — | — | — | — | — | OK |
| relief/crystal_spire_big.png | 72×135 | 9720 | 0.462 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/crystal_spire_big_e.png | 72×135 | 9720 | 0.462 | 0 | — | — | — | — | — | OK |
| relief/crystal_spire_big_n.png | 72×135 | 9720 | 0.462 | 0 | — | — | — | — | — | OK |
| relief/drill_rig.png | 121×135 | 16335 | 0.336 | 0 | 0.0 | — | — | — | 左下 | OK |
| relief/drill_rig_e.png | 121×135 | 16335 | 0.336 | 0 | — | — | — | — | — | OK |
| relief/drill_rig_n.png | 121×135 | 16335 | 0.336 | 0 | — | — | — | — | — | OK |
| relief/facade_far.png | 200×117 | 23400 | 0.498 | 0 | 0.0 | — | — | — | なし | OK |
| relief/facade_far_n.png | 200×117 | 23400 | 0.498 | 0 | — | — | — | — | — | OK |
| relief/fountain_dry.png | 68×78 | 5304 | 0.393 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/fountain_dry_n.png | 68×78 | 5304 | 0.393 | 0 | — | — | — | — | — | OK |
| relief/frieze.png | 117×19 | 2223 | 0.706 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/frieze_n.png | 117×19 | 2223 | 0.706 | 0 | — | — | — | — | — | OK |
| relief/gear_wall.png | 151×129 | 19479 | 0.66 | 0 | 0.0 | — | — | — | なし | OK |
| relief/gear_wall_n.png | 151×129 | 19479 | 0.66 | 0 | — | — | — | — | — | OK |
| relief/lamp_hang.png | 28×109 | 3052 | 0.436 | 0 | 0.0 | — | — | — | 下 | OK |
| relief/lamp_hang_e.png | 28×109 | 3052 | 0.436 | 0 | — | — | — | — | — | OK |
| relief/lamp_hang_n.png | 28×109 | 3052 | 0.436 | 0 | — | — | — | — | — | OK |
| relief/lamp_head.png | 22×31 | 682 | 0.519 | 0 | 0.0 | — | — | — | 左下 | OK |
| relief/lamp_head_e.png | 22×31 | 682 | 0.519 | 0 | — | — | — | — | — | OK |
| relief/lamp_head_n.png | 22×31 | 682 | 0.519 | 0 | — | — | — | — | — | OK |
| relief/lamp_post.png | 22×101 | 2222 | 0.377 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/lamp_post_e.png | 22×101 | 2222 | 0.377 | 0 | — | — | — | — | — | OK |
| relief/lamp_post_n.png | 22×101 | 2222 | 0.377 | 0 | — | — | — | — | — | OK |
| relief/monument_disc.png | 93×131 | 12183 | 0.692 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/monument_disc_e.png | 93×131 | 12183 | 0.692 | 0 | — | — | — | — | — | OK |
| relief/monument_disc_n.png | 93×131 | 12183 | 0.692 | 0 | — | — | — | — | — | OK |
| relief/ore_cart.png | 88×60 | 5280 | 0.504 | 0 | 0.0 | — | — | — | 左上 | OK |
| relief/ore_cart_e.png | 88×60 | 5280 | 0.504 | 0 | — | — | — | — | — | OK |
| relief/ore_cart_n.png | 88×60 | 5280 | 0.504 | 0 | — | — | — | — | — | OK |
| relief/pillar_broken.png | 50×121 | 6050 | 0.551 | 0 | 0.0 | — | — | — | 左上 | OK |
| relief/pillar_broken_n.png | 50×121 | 6050 | 0.551 | 0 | — | — | — | — | — | OK |
| relief/pillar_fallen.png | 129×41 | 5289 | 0.448 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/pillar_fallen_n.png | 129×41 | 5289 | 0.448 | 0 | — | — | — | — | — | OK |
| relief/rubble_pile.png | 97×50 | 4850 | 0.387 | 0 | 0.0 | — | — | — | 左上 | OK |
| relief/rubble_pile_n.png | 97×50 | 4850 | 0.387 | 0 | — | — | — | — | — | OK |
| relief/skyline_a.png | 200×57 | 11400 | 0.246 | 0 | 0.0 | — | — | — | 右上 | OK |
| relief/skyline_a_n.png | 200×57 | 11400 | 0.246 | 0 | — | — | — | — | — | OK |
| relief/skyline_b.png | 200×39 | 7800 | 0.484 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/skyline_b_n.png | 200×39 | 7800 | 0.484 | 0 | — | — | — | — | — | OK |
| relief/stalactite_cluster.png | 54×137 | 7398 | 0.505 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/stalactite_cluster_n.png | 54×137 | 7398 | 0.505 | 0 | — | — | — | — | — | OK |
| relief/statue.png | 56×91 | 5096 | 0.598 | 0 | 0.0 | — | — | — | なし | OK |
| relief/statue_e.png | 56×91 | 5096 | 0.598 | 0 | — | — | — | — | — | OK |
| relief/statue_headless.png | 52×112 | 5824 | 0.443 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/statue_headless_n.png | 52×112 | 5824 | 0.443 | 0 | — | — | — | — | — | OK |
| relief/statue_kneel.png | 56×91 | 5096 | 0.598 | 0 | 0.0 | — | — | — | なし | OK |
| relief/statue_kneel_e.png | 56×91 | 5096 | 0.598 | 0 | — | — | — | — | — | OK |
| relief/statue_kneel_n.png | 56×91 | 5096 | 0.598 | 0 | — | — | — | — | — | OK |
| relief/statue_n.png | 56×91 | 5096 | 0.598 | 0 | — | — | — | — | — | OK |
| relief/stele.png | 38×106 | 4028 | 0.56 | 0 | 0.0 | — | — | — | 左 | OK |
| relief/stele_e.png | 38×106 | 4028 | 0.56 | 0 | — | — | — | — | — | OK |
| relief/stele_n.png | 38×106 | 4028 | 0.56 | 0 | — | — | — | — | — | OK |
| relief/urn.png | 28×35 | 980 | 0.459 | 0 | 0.0 | — | — | — | 左 | OK |
| relief/urn_n.png | 28×35 | 980 | 0.459 | 0 | — | — | — | — | — | OK |
| relief/vein_wall.png | 38×51 | 1938 | 0.498 | 0 | 0.0 | — | — | — | 上 | OK |
| relief/vein_wall_e.png | 38×51 | 1938 | 0.498 | 0 | — | — | — | — | — | OK |
| relief/vein_wall_n.png | 38×51 | 1938 | 0.498 | 0 | — | — | — | — | — | OK |
| relief/wall_ruin.png | 133×86 | 11438 | 0.569 | 0 | 0.0 | — | — | — | なし | OK |
| relief/wall_ruin_n.png | 133×86 | 11438 | 0.569 | 0 | — | — | — | — | — | OK |
| tiles/side_brick_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 2447.8 | 0.506 | 55.7 | — | OK |
| tiles/side_brick_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 2328.8 | 0.506 | 56.1 | — | OK |
| tiles/side_brick_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 2446.9 | 0.506 | 55.7 | — | OK |
| tiles/side_brick_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 2341.4 | 0.506 | 56.2 | — | OK |
| tiles/side_pillar_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3200.0 | 0.077 | 135.2 | — | OK |
| tiles/side_pillar_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3053.3 | 0.077 | 135.4 | — | OK |
| tiles/side_pillar_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3200.0 | 0.077 | 135.2 | — | OK |
| tiles/side_pillar_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3184.6 | 0.077 | 135.0 | — | OK |
| tiles/side_step_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3527.5 | 0.226 | 87.3 | — | OK |
| tiles/side_step_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3688.2 | 0.226 | 86.4 | — | OK |
| tiles/side_step_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3402.3 | 0.226 | 88.1 | — | OK |
| tiles/side_step_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3828.1 | 0.226 | 86.3 | — | OK |
| tiles/side_wall_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 2646.6 | 0.217 | 65.7 | — | OK |
| tiles/side_wall_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3686.5 | 0.217 | 63.5 | — | OK |
| tiles/side_wall_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 2524.8 | 0.217 | 67.6 | — | OK |
| tiles/side_wall_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 3734.5 | 0.217 | 63.9 | — | OK |
| tiles/top_floor_seat_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 557.5 | 0.082 | 107.7 | — | OK |
| tiles/top_floor_seat_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 547.4 | 0.082 | 107.7 | — | OK |
| tiles/top_floor_seat_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 589.3 | 0.082 | 107.5 | — | OK |
| tiles/top_floor_seat_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 620.2 | 0.082 | 107.3 | — | OK |
| tiles/top_pillar_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 534.0 | 0.076 | 141.9 | — | OK |
| tiles/top_pillar_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 534.4 | 0.076 | 142.0 | — | OK |
| tiles/top_pillar_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 572.4 | 0.076 | 141.6 | — | OK |
| tiles/top_pillar_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 578.5 | 0.076 | 141.8 | — | OK |
| tiles/top_step_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 213.3 | 0.262 | 105.7 | — | OK |
| tiles/top_step_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 346.5 | 0.262 | 105.1 | — | OK |
| tiles/top_step_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 355.3 | 0.262 | 105.1 | — | OK |
| tiles/top_step_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 270.8 | 0.262 | 105.3 | — | OK |
| tiles/top_wall_a.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 437.6 | 0.217 | 92.6 | — | OK |
| tiles/top_wall_b.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 508.0 | 0.217 | 92.4 | — | OK |
| tiles/top_wall_c.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 437.3 | 0.217 | 92.6 | — | OK |
| tiles/top_wall_d.png | 64×64 | 4096 | 1.0 | 0 | 0.0 | 336.2 | 0.217 | 92.9 | — | OK |

合計 131 枚・NG 0

## 別の種どうしの継ぎ目 (材質ごと・gen_items.py cross_seams)

門: 左右 = 隣の種の端の列の差 ÷ 中の隣の列の差 ≤ 1.6（4 種 × 反転の全部の組の最大）・上下 = 上の種の行 63 と下の種の行 0 の差の最大 ÷ 同じ種どうしの差 ≤ 1.25・天面は 上下の境の差 ÷ 中の「横に続く目地 → 下の石の面」の段差 ≤ 1.25 と、行 63 以外で目地 (中央値より 10 暗い画素) が横幅の 6 割を超える行が無いこと（石積みは段が横に通るのが正しいので数えず、境÷中の目地は記録だけ）

| 材質 | 左右の比 (最悪の組) | 左右の差 / 中 | 上下の比 (最悪の組) | 上下の差 / 同じ種 | 境 ÷ 中の目地 | 横幅いっぱいの行 (a b c d) | 判定 |
|---|---|---|---|---|---|---|---|
| side_wall | 0.48 (aa) | 4.7 / 9.8 | 1.16 (ad) | 77.8 / 66.9 | 1.06 | — | OK |
| side_step | 0.69 (aa) | 6.1 / 8.8 | 1.04 (db') | 80.0 / 77.0 | 1.06 | — | OK |
| side_pillar | 0.6 (aa) | 5.0 / 8.2 | 1.03 (ad) | 76.8 / 74.6 | 1.34 | — | OK |
| side_brick | 0.65 (aa) | 5.0 / 7.7 | 1.04 (ab) | 41.9 / 40.4 | 0.85 | — | OK |
| top_floor_seat | 1.28 (a'b') | 6.5 / 5.1 | 1.09 (ca') | 17.0 / 15.5 | 0.6 | [] [] [] [] | OK |
| top_wall | 0.45 (ac) | 1.4 / 3.0 | 1.15 (ab') | 26.7 / 23.2 | 0.5 | [] [] [] [] | OK |
| top_step | 0.34 (ad) | 0.7 / 2.2 | 1.11 (da') | 24.7 / 22.2 | 0.51 | [] [] [] [] | OK |
| top_pillar | 1.17 (ac) | 4.3 / 3.7 | 1.28 (ca) | 21.8 / 17.0 | 0.4 | [] [] [] [] | OK |

組の書き方: 左の種・右の種 (上の種・下の種) の順。' は左右反転

材質 8・NG 0
