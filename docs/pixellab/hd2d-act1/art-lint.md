# art-lint (scripts/art-lint.py が書く。手で直さない)

計画 docs/design/hd2d-slice-plan-2026-09-30.md P05 手順1。輝度は 0〜255 (0.299R+0.587G+0.114B)。光の向きは明るい画素の重心 − 形の重心 (上が +)。

- _KeyFlip の対象 (画像ファイルを左右反転した絵) 16 枚: enemy_bowl_bug, enemy_brood_raptor, enemy_devoted_sculptor, enemy_elite_husk_1, enemy_elite_mirror_djinn, enemy_ember_furnace, enemy_haze_stag, enemy_haze_stag_96, enemy_kin_priest, enemy_mimic_jester, enemy_mourn_beast, enemy_scald_gnat, enemy_seal_orb, enemy_set_wary, enemy_vine_walker, enemy_wolf
- 光が右から描き込まれている絵 (候補) 9 枚: enemy_elite_doom_chanter, enemy_elite_iron_egg, enemy_kin_follower, enemy_kin_priest, enemy_mimic_imp, enemy_raptor_egg, enemy_wide_power, enemy_wolf, white_perm_hound

## まとめ (リーダーの一枚絵・敵・人形。アニメのコマは除く)

- 枚数 115・輪郭の最暗色の中央値 5.0 (範囲 0〜46)・20〜30 に入る絵 12 枚
- 白 (235超) を含む絵 110 枚・白の割合の中央値 3.010%
- 光の向き: 右 2・右上 11・上 42・左上 30・左 13・左下 7・下 2・右下 0・なし 8
- 台座の疑い 23 枚: enemy_burrow_worm, enemy_crossbow_archer, enemy_devoted_sculptor, enemy_drummer, enemy_elite_giant_face, enemy_elite_iron_egg, enemy_elite_sentry, enemy_forgotten_soul, enemy_haze_stag, enemy_haze_stag_96, enemy_lost_soul, enemy_moss_healer, enemy_moss_spawner, enemy_nemesis_wraith, enemy_sludge_berserker, enemy_snap_fruit, enemy_thief, white_perm_band, white_perm_bonfire, white_perm_choir, white_perm_hound, white_perm_monk, white_perm_shieldmaiden

## キャラ

| 絵 | 種 | 寸法 | 背丈 | 輪郭の最暗 | 輪郭の中央 | 白235超 | 光の向き | 大きさ | 下端に届く列 | 台座 | 反転 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| leader_green | leader | 88×64 | 62 | 4 | 8 | 0.00% | 左 | 0.08 | 34% |  |  |
| leader_green_48 | leader | 68×50 | 48 | 4 | 8 | 0.00% | 左 | 0.11 | 36% |  |  |
| leader_white | leader | 112×64 | 56 | 24 | 103 | 24.81% | 左下 | 0.39 | 25% |  |  |
| leader_green_48_attack_0 | leader-anim | 71×60 | 52 | 4 | 17 | 0.00% | 下 | 0.14 | 54% |  |  |
| leader_green_48_attack_1 | leader-anim | 71×60 | 46 | 4 | 12 | 0.00% | なし | 0.03 | 37% |  |  |
| leader_green_48_attack_2 | leader-anim | 71×60 | 50 | 4 | 14 | 0.00% | 上 | 0.22 | 58% | 疑い |  |
| leader_green_48_attack_3 | leader-anim | 71×60 | 46 | 4 | 8 | 0.00% | 上 | 0.12 | 47% |  |  |
| leader_green_48_block_0 | leader-anim | 71×60 | 47 | 4 | 17 | 0.00% | 左下 | 0.08 | 32% |  |  |
| leader_green_48_block_1 | leader-anim | 71×60 | 45 | 4 | 17 | 0.00% | 左 | 0.11 | 31% |  |  |
| leader_green_48_block_2 | leader-anim | 71×60 | 45 | 4 | 12 | 0.00% | 左下 | 0.10 | 33% |  |  |
| leader_green_48_block_3 | leader-anim | 71×60 | 45 | 4 | 12 | 0.00% | 左下 | 0.12 | 33% |  |  |
| leader_green_48_idle_0 | leader-anim | 71×60 | 48 | 4 | 8 | 0.00% | 左下 | 0.07 | 32% |  |  |
| leader_green_48_idle_1 | leader-anim | 71×60 | 48 | 4 | 8 | 0.00% | 左下 | 0.07 | 32% |  |  |
| leader_green_48_idle_2 | leader-anim | 71×60 | 49 | 4 | 8 | 0.00% | 左 | 0.11 | 33% |  |  |
| leader_green_48_idle_3 | leader-anim | 71×60 | 50 | 4 | 8 | 0.00% | 左下 | 0.14 | 33% |  |  |
| leader_green_48_idle_4 | leader-anim | 71×60 | 50 | 4 | 8 | 0.00% | 左下 | 0.14 | 33% |  |  |
| leader_green_48_idle_5 | leader-anim | 71×60 | 50 | 4 | 8 | 0.00% | 左下 | 0.15 | 33% |  |  |
| leader_green_48_idle_6 | leader-anim | 71×60 | 50 | 4 | 8 | 0.00% | 左 | 0.11 | 33% |  |  |
| leader_green_48_idle_7 | leader-anim | 71×60 | 49 | 4 | 12 | 0.00% | 左下 | 0.10 | 32% |  |  |
| leader_green_attack_0 | leader-anim | 92×78 | 67 | 4 | 12 | 0.00% | 下 | 0.15 | 51% |  |  |
| leader_green_attack_1 | leader-anim | 92×78 | 59 | 4 | 12 | 0.00% | なし | 0.00 | 34% |  |  |
| leader_green_attack_2 | leader-anim | 92×78 | 65 | 4 | 12 | 0.00% | 上 | 0.19 | 56% | 疑い |  |
| leader_green_attack_3 | leader-anim | 92×78 | 59 | 4 | 8 | 0.00% | 上 | 0.14 | 41% |  |  |
| leader_green_block_0 | leader-anim | 92×78 | 61 | 4 | 12 | 0.00% | 左 | 0.10 | 33% |  |  |
| leader_green_block_1 | leader-anim | 92×78 | 58 | 4 | 17 | 0.00% | 左 | 0.08 | 31% |  |  |
| leader_green_block_2 | leader-anim | 92×78 | 58 | 4 | 12 | 0.00% | 左下 | 0.13 | 31% |  |  |
| leader_green_block_3 | leader-anim | 92×78 | 58 | 4 | 12 | 0.00% | 左下 | 0.12 | 30% |  |  |
| leader_green_idle_0 | leader-anim | 92×78 | 62 | 4 | 8 | 0.00% | 左 | 0.10 | 33% |  |  |
| leader_green_idle_1 | leader-anim | 92×78 | 62 | 4 | 8 | 0.00% | 左 | 0.10 | 33% |  |  |
| leader_green_idle_2 | leader-anim | 92×78 | 63 | 4 | 8 | 0.00% | 左 | 0.11 | 34% |  |  |
| leader_green_idle_3 | leader-anim | 92×78 | 64 | 4 | 8 | 0.00% | 左下 | 0.12 | 34% |  |  |
| leader_green_idle_4 | leader-anim | 92×78 | 65 | 4 | 8 | 0.00% | 左下 | 0.13 | 34% |  |  |
| leader_green_idle_5 | leader-anim | 92×78 | 64 | 4 | 8 | 0.00% | 左 | 0.13 | 34% |  |  |
| leader_green_idle_6 | leader-anim | 92×78 | 64 | 4 | 8 | 0.00% | 左 | 0.11 | 34% |  |  |
| leader_green_idle_7 | leader-anim | 92×78 | 63 | 4 | 8 | 0.00% | 左 | 0.11 | 33% |  |  |
| leader_white_attack_0 | leader-anim | 112×84 | 65 | 24 | 82 | 28.05% | 下 | 0.32 | 41% |  |  |
| leader_white_attack_1 | leader-anim | 112×84 | 54 | 24 | 78 | 27.48% | 下 | 0.29 | 7% |  |  |
| leader_white_attack_2 | leader-anim | 112×84 | 54 | 33 | 159 | 39.79% | なし | 0.04 | 15% |  |  |
| leader_white_attack_3 | leader-anim | 112×84 | 54 | 33 | 142 | 36.54% | 下 | 0.18 | 16% |  |  |
| leader_white_block_0 | leader-anim | 112×64 | 56 | 24 | 85 | 25.84% | 左下 | 0.31 | 22% |  |  |
| leader_white_block_1 | leader-anim | 112×64 | 54 | 24 | 85 | 28.48% | 下 | 0.26 | 20% |  |  |
| leader_white_block_2 | leader-anim | 112×64 | 54 | 33 | 138 | 51.94% | 右下 | 0.17 | 21% |  |  |
| leader_white_block_3 | leader-anim | 112×64 | 56 | 33 | 125 | 36.90% | 下 | 0.19 | 21% |  |  |
| leader_white_idle_0 | leader-anim | 112×84 | 56 | 24 | 103 | 24.81% | 左下 | 0.39 | 25% |  |  |
| leader_white_idle_1 | leader-anim | 112×84 | 57 | 24 | 103 | 25.47% | 左下 | 0.36 | 25% |  |  |
| leader_white_idle_2 | leader-anim | 112×84 | 58 | 24 | 103 | 25.46% | 左下 | 0.35 | 24% |  |  |
| leader_white_idle_3 | leader-anim | 112×84 | 59 | 11 | 103 | 25.43% | 左下 | 0.37 | 24% |  |  |
| leader_white_idle_4 | leader-anim | 112×84 | 60 | 24 | 103 | 25.26% | 左下 | 0.33 | 25% |  |  |
| leader_white_idle_5 | leader-anim | 112×84 | 61 | 24 | 103 | 26.26% | 左下 | 0.34 | 25% |  |  |
| leader_white_idle_6 | leader-anim | 112×84 | 59 | 24 | 103 | 25.39% | 左下 | 0.35 | 25% |  |  |
| leader_white_idle_7 | leader-anim | 112×84 | 58 | 24 | 103 | 25.47% | 左下 | 0.36 | 25% |  |  |
| enemy_apprentice_colossus | enemy | 64×64 | 54 | 4 | 12 | 1.64% | 左上 | 0.16 | 47% |  |  |
| enemy_axe_automaton | enemy | 64×64 | 58 | 0 | 1 | 5.73% | 上 | 0.19 | 15% |  |  |
| enemy_axe_automaton_reboot | enemy | 64×64 | 56 | 0 | 1 | 7.06% | 左上 | 0.18 | 14% |  |  |
| enemy_axe_ogre | enemy | 64×64 | 56 | 2 | 9 | 0.81% | なし | 0.04 | 33% |  |  |
| enemy_big_slime | enemy | 64×64 | 52 | 7 | 19 | 3.20% | 上 | 0.25 | 52% |  |  |
| enemy_biting_scroll | enemy | 64×64 | 53 | 0 | 6 | 5.08% | 上 | 0.24 | 24% |  |  |
| enemy_bomber | enemy | 64×64 | 44 | 9 | 16 | 3.64% | 左上 | 0.33 | 34% |  |  |
| enemy_bond_wolf | enemy | 64×64 | 56 | 25 | 42 | 5.87% | 左上 | 0.29 | 49% |  |  |
| enemy_bowl_bug | enemy | 64×64 | 38 | 5 | 13 | 7.27% | 左 | 0.18 | 35% |  | 反転 |
| enemy_brood_raptor | enemy | 64×64 | 50 | 6 | 12 | 4.88% | 左下 | 0.22 | 32% |  | 反転 |
| enemy_brood_toad | enemy | 64×64 | 54 | 9 | 13 | 0.17% | 左 | 0.22 | 40% |  |  |
| enemy_broodling | enemy | 64×64 | 45 | 2 | 10 | 5.57% | 左 | 0.18 | 26% |  |  |
| enemy_brute | enemy | 128×128 | 100 | 1 | 3 | 0.52% | 左上 | 0.07 | 10% |  |  |
| enemy_brute_96 | enemy | 96×96 | 75 | 5 | 8 | 0.46% | 上 | 0.08 | 14% |  |  |
| enemy_burrow_worm | enemy | 64×64 | 44 | 4 | 12 | 0.00% | 左下 | 0.28 | 74% | 疑い |  |
| enemy_chimera_1 | enemy | 128×128 | 103 | 1 | 5 | 1.19% | 左上 | 0.07 | 11% |  |  |
| enemy_chimera_1_96 | enemy | 96×96 | 79 | 4 | 16 | 3.25% | 左上 | 0.15 | 13% |  |  |
| enemy_chimera_2 | enemy | 128×128 | 109 | 2 | 6 | 1.46% | 左上 | 0.35 | 10% |  |  |
| enemy_chimera_2_96 | enemy | 96×96 | 81 | 1 | 11 | 2.13% | 左上 | 0.34 | 16% |  |  |
| enemy_chimera_3 | enemy | 128×128 | 111 | 2 | 4 | 0.16% | 左上 | 0.24 | 14% |  |  |
| enemy_chimera_3_96 | enemy | 96×96 | 83 | 0 | 3 | 0.44% | 左上 | 0.23 | 25% |  |  |
| enemy_chomper | enemy | 64×64 | 56 | 1 | 6 | 3.34% | 上 | 0.26 | 29% |  |  |
| enemy_cinder_imp | enemy | 64×64 | 53 | 7 | 16 | 11.57% | 上 | 0.07 | 38% |  |  |
| enemy_cog_construct | enemy | 64×64 | 58 | 0 | 24 | 5.93% | 左上 | 0.27 | 21% |  |  |
| enemy_crab_cannon | enemy | 96×96 | 59 | 8 | 22 | 12.53% | 左上 | 0.13 | 12% |  |  |
| enemy_crab_crusher | enemy | 96×96 | 66 | 32 | 37 | 8.02% | なし | 0.04 | 19% |  |  |
| enemy_crossbow_archer | enemy | 64×64 | 51 | 7 | 43 | 1.25% | 右上 | 0.10 | 59% | 疑い |  |
| enemy_cultist | enemy | 64×64 | 46 | 4 | 48 | 1.42% | 左上 | 0.33 | 32% |  |  |
| enemy_devoted_sculptor | enemy | 64×64 | 56 | 0 | 5 | 5.93% | 左上 | 0.19 | 61% | 疑い | 反転 |
| enemy_drummer | enemy | 64×64 | 51 | 8 | 24 | 0.18% | 右上 | 0.09 | 62% | 疑い |  |
| enemy_elite_deathless | enemy | 80×80 | 77 | 1 | 10 | 1.37% | 左上 | 0.20 | 20% |  |  |
| enemy_elite_devourer | enemy | 80×80 | 64 | 3 | 18 | 15.01% | 左上 | 0.27 | 12% |  |  |
| enemy_elite_doom_chanter | enemy | 80×80 | 72 | 5 | 48 | 0.27% | 右上 | 0.21 | 31% |  |  |
| enemy_elite_giant_face | enemy | 80×80 | 78 | 3 | 44 | 0.72% | 上 | 0.24 | 60% | 疑い |  |
| enemy_elite_gold_raven | enemy | 80×80 | 65 | 1 | 6 | 1.87% | 上 | 0.36 | 19% |  |  |
| enemy_elite_husk_1 | enemy | 80×80 | 74 | 0 | 2 | 0.28% | 上 | 0.14 | 4% |  | 反転 |
| enemy_elite_husk_2 | enemy | 80×80 | 71 | 0 | 3 | 4.55% | 上 | 0.16 | 5% |  |  |
| enemy_elite_husk_3 | enemy | 80×80 | 74 | 0 | 4 | 0.94% | 上 | 0.24 | 5% |  |  |
| enemy_elite_iron_egg | enemy | 80×80 | 74 | 1 | 6 | 6.26% | 右上 | 0.22 | 89% | 疑い |  |
| enemy_elite_mirror_djinn | enemy | 80×80 | 68 | 5 | 14 | 10.29% | 左上 | 0.24 | 22% |  | 反転 |
| enemy_elite_owl | enemy | 80×80 | 70 | 4 | 10 | 1.89% | 左 | 0.17 | 16% |  |  |
| enemy_elite_sentry | enemy | 80×80 | 73 | 18 | 40 | 0.84% | 上 | 0.24 | 61% | 疑い |  |
| enemy_elite_sergeant | enemy | 80×80 | 73 | 2 | 4 | 3.37% | 左上 | 0.09 | 18% |  |  |
| enemy_elite_slaver | enemy | 80×80 | 67 | 3 | 8 | 5.37% | 上 | 0.34 | 55% |  |  |
| enemy_elite_stab_book | enemy | 80×80 | 76 | 1 | 17 | 3.40% | 上 | 0.16 | 33% |  |  |
| enemy_ember_furnace | enemy | 128×128 | 104 | 5 | 9 | 0.78% | 左上 | 0.19 | 22% |  | 反転 |
| enemy_ember_furnace_96 | enemy | 96×96 | 85 | 7 | 18 | 1.52% | 左 | 0.21 | 22% |  |  |
| enemy_forgotten_soul | enemy | 64×64 | 42 | 31 | 161 | 1.76% | 左上 | 0.19 | 59% | 疑い |  |
| enemy_frog_knight | enemy | 64×64 | 59 | 5 | 9 | 2.49% | 左上 | 0.22 | 35% |  |  |
| enemy_gaping_maw | enemy | 64×64 | 57 | 20 | 60 | 4.89% | 右上 | 0.18 | 31% |  |  |
| enemy_haze_stag | enemy | 128×128 | 104 | 25 | 79 | 7.59% | なし | 0.01 | 89% | 疑い | 反転 |
| enemy_haze_stag_96 | enemy | 96×96 | 89 | 46 | 94 | 11.65% | なし | 0.02 | 90% | 疑い | 反転 |
| enemy_hexer | enemy | 64×64 | 48 | 21 | 25 | 2.89% | 左下 | 0.18 | 21% |  |  |
| enemy_iron_clam | enemy | 64×64 | 49 | 8 | 47 | 24.07% | なし | 0.06 | 48% |  |  |
| enemy_joker | enemy | 64×64 | 52 | 13 | 22 | 27.28% | なし | 0.03 | 10% |  |  |
| enemy_kin_follower | enemy | 96×96 | 81 | 22 | 27 | 13.99% | 右 | 0.36 | 10% |  |  |
| enemy_kin_priest | enemy | 96×96 | 83 | 8 | 42 | 10.98% | 右上 | 0.23 | 31% |  | 反転 |
| enemy_lost_soul | enemy | 64×64 | 53 | 13 | 75 | 31.78% | 上 | 0.19 | 64% | 疑い |  |
| enemy_maw_hunter | enemy | 64×64 | 52 | 5 | 16 | 2.30% | 上 | 0.25 | 5% |  |  |
| enemy_mimic_imp | enemy | 64×64 | 57 | 7 | 12 | 0.34% | 右 | 0.20 | 19% |  |  |
| enemy_mimic_jester | enemy | 64×64 | 51 | 1 | 4 | 18.80% | 左上 | 0.07 | 23% |  | 反転 |
| enemy_moss | enemy | 64×64 | 58 | 8 | 24 | 4.50% | 左 | 0.40 | 33% |  |  |
| enemy_moss_healer | enemy | 64×64 | 59 | 3 | 83 | 6.44% | 左上 | 0.25 | 68% | 疑い |  |
| enemy_moss_slime | enemy | 64×64 | 54 | 12 | 20 | 2.61% | 上 | 0.25 | 33% |  |  |
| enemy_moss_spawner | enemy | 64×64 | 54 | 12 | 30 | 2.95% | 上 | 0.46 | 69% | 疑い |  |
| enemy_mourn_beast | enemy | 64×64 | 56 | 5 | 58 | 0.26% | 左 | 0.18 | 31% |  | 反転 |
| enemy_mud_lump | enemy | 64×64 | 55 | 4 | 61 | 1.34% | 上 | 0.43 | 46% |  |  |
| enemy_mudling | enemy | 64×64 | 46 | 16 | 44 | 3.90% | 上 | 0.36 | 20% |  |  |
| enemy_nemesis_wraith | enemy | 64×64 | 56 | 13 | 108 | 0.00% | 上 | 0.43 | 64% | 疑い |  |
| enemy_probe | enemy | 64×64 | 54 | 6 | 40 | 8.37% | 下 | 0.17 | 26% |  |  |
| enemy_raptor_chick | enemy | 64×64 | 48 | 6 | 63 | 0.20% | 左下 | 0.16 | 50% |  |  |
| enemy_raptor_egg | enemy | 64×64 | 49 | 10 | 88 | 0.87% | 上 | 0.61 | 53% |  |  |
| enemy_rock_beetle | enemy | 64×64 | 44 | 15 | 44 | 2.72% | 上 | 0.32 | 38% |  |  |
| enemy_scald_gnat | enemy | 64×64 | 52 | 0 | 34 | 0.10% | 右上 | 0.20 | 4% |  | 反転 |
| enemy_seal_orb | enemy | 48×48 | 44 | 3 | 36 | 2.20% | 左上 | 0.22 | 6% |  | 反転 |
| enemy_set_breaker | enemy | 64×64 | 56 | 0 | 6 | 2.21% | 上 | 0.24 | 27% |  |  |
| enemy_set_wary | enemy | 64×64 | 40 | 7 | 14 | 0.15% | 左 | 0.31 | 29% |  | 反転 |
| enemy_shell_guard | enemy | 64×64 | 55 | 0 | 6 | 0.91% | 左上 | 0.21 | 24% |  |  |
| enemy_shield_squire | enemy | 64×64 | 38 | 2 | 7 | 4.11% | 左上 | 0.27 | 35% |  |  |
| enemy_sludge_berserker | enemy | 64×64 | 57 | 4 | 39 | 2.77% | 上 | 0.24 | 68% | 疑い |  |
| enemy_sludge_spider | enemy | 64×64 | 50 | 6 | 43 | 0.09% | 上 | 0.29 | 11% |  |  |
| enemy_slug | enemy | 64×64 | 35 | 15 | 26 | 8.31% | なし | 0.05 | 10% |  |  |
| enemy_snap_fruit | enemy | 64×64 | 54 | 29 | 35 | 3.01% | 左 | 0.28 | 65% | 疑い |  |
| enemy_spore_cap | enemy | 64×64 | 52 | 1 | 16 | 0.07% | 下 | 0.33 | 37% |  |  |
| enemy_strangler_serpent | enemy | 64×64 | 53 | 10 | 22 | 1.09% | 右上 | 0.08 | 23% |  |  |
| enemy_thief | enemy | 64×64 | 51 | 3 | 22 | 2.12% | 上 | 0.45 | 87% | 疑い |  |
| enemy_thorn_squirrel | enemy | 64×64 | 48 | 11 | 58 | 0.25% | 左 | 0.26 | 41% |  |  |
| enemy_thunder_globe | enemy | 64×64 | 56 | 0 | 12 | 7.83% | 左上 | 0.32 | 43% |  |  |
| enemy_turtle | enemy | 128×128 | 100 | 1 | 46 | 2.27% | 左下 | 0.28 | 11% |  |  |
| enemy_turtle_96 | enemy | 96×96 | 76 | 1 | 63 | 7.82% | 左下 | 0.26 | 14% |  |  |
| enemy_vine_walker | enemy | 64×64 | 55 | 0 | 3 | 0.00% | 上 | 0.51 | 42% |  | 反転 |
| enemy_warden | enemy | 128×128 | 104 | 1 | 8 | 0.36% | 上 | 0.21 | 39% |  |  |
| enemy_warden_96 | enemy | 96×96 | 87 | 2 | 26 | 0.27% | 上 | 0.22 | 40% |  |  |
| enemy_whetstone_colossus | enemy | 64×64 | 58 | 0 | 6 | 0.63% | 左 | 0.20 | 47% |  |  |
| enemy_wide_power | enemy | 64×64 | 46 | 3 | 24 | 0.56% | 右上 | 0.47 | 49% |  |  |
| enemy_winch_warden | enemy | 96×96 | 90 | 3 | 9 | 1.93% | 左上 | 0.12 | 18% |  |  |
| enemy_wolf | enemy | 64×64 | 53 | 6 | 9 | 11.27% | 右上 | 0.30 | 27% |  | 反転 |
| white_perm_archer | doll | 32×32 | 26 | 6 | 9 | 11.90% | 上 | 0.11 | 46% |  |  |
| white_perm_band | doll | 32×32 | 30 | 28 | 45 | 11.58% | 上 | 0.23 | 55% | 疑い |  |
| white_perm_bandleader | doll | 32×32 | 26 | 18 | 24 | 14.56% | 上 | 0.16 | 50% |  |  |
| white_perm_banneret | doll | 32×32 | 28 | 44 | 83 | 20.79% | 上 | 0.17 | 47% |  |  |
| white_perm_bonfire | doll | 32×32 | 28 | 24 | 97 | 9.09% | 上 | 0.36 | 77% | 疑い |  |
| white_perm_candle | doll | 32×32 | 27 | 34 | 63 | 14.00% | 上 | 0.28 | 46% |  |  |
| white_perm_choir | doll | 32×32 | 30 | 29 | 39 | 11.69% | 上 | 0.27 | 91% | 疑い |  |
| white_perm_dragon | doll | 48×48 | 43 | 34 | 41 | 7.84% | なし | 0.05 | 43% |  |  |
| white_perm_hound | doll | 32×32 | 26 | 4 | 9 | 12.64% | 右上 | 0.29 | 57% | 疑い |  |
| white_perm_lantern | doll | 32×32 | 27 | 35 | 39 | 14.81% | 上 | 0.17 | 48% |  |  |
| white_perm_lion | doll | 48×48 | 46 | 34 | 38 | 14.74% | 上 | 0.26 | 51% |  |  |
| white_perm_monk | doll | 32×32 | 29 | 29 | 32 | 16.50% | 上 | 0.32 | 68% | 疑い |  |
| white_perm_page | doll | 32×32 | 26 | 33 | 49 | 11.11% | 上 | 0.25 | 50% |  |  |
| white_perm_shieldmaiden | doll | 32×32 | 26 | 32 | 35 | 17.41% | 左上 | 0.39 | 75% | 疑い |  |
| white_perm_squire | doll | 32×32 | 30 | 21 | 63 | 10.44% | 上 | 0.31 | 39% |  |  |

## 舞台の絵 (色表からの距離。ΔE_ok×100)

色表: docs/pixellab/hd2d-act1/palette-act1.json (48 色)。置いた絵は写してあるので距離 0 が正しい。「写す前 p95」が元の絵のはみ出し (大きいほど写した時に色が動いた)

| 絵 | 色数 | 距離 p50 | 距離 p95 | 写す前 p95 | 元 |
|---|---|---|---|---|---|
| stage/act1/relief/act1_bush2.png | 25 | 0.0 | 0.0 | 5.0 | unity/Assets/Resources/Art/props/act1_bush2.png |
| stage/act1/relief/act1_bush3.png | 21 | 0.0 | 0.0 | 3.92 | unity/Assets/Resources/Art/props/act1_bush3.png |
| stage/act1/relief/act1_fern.png | 21 | 0.0 | 0.0 | 4.64 | unity/Assets/Resources/Art/props/act1_fern.png |
| stage/act1/relief/act1_fern2.png | 20 | 0.0 | 0.0 | 6.01 | unity/Assets/Resources/Art/props/act1_fern2.png |
| stage/act1/relief/act1_leafclump1.png | 19 | 0.0 | 0.0 | 5.91 | unity/Assets/Resources/Art/props/act1_leafclump1.png |
| stage/act1/relief/act1_leafclump2.png | 24 | 0.0 | 0.0 | 5.85 | unity/Assets/Resources/Art/props/act1_leafclump2.png |
| stage/act1/relief/act1_leafclump3.png | 22 | 0.0 | 0.0 | 16.22 | unity/Assets/Resources/Art/props/act1_leafclump3.png |
| stage/act1/relief/act1_leafclump4.png | 22 | 0.0 | 0.0 | 4.77 | unity/Assets/Resources/Art/props/act1_leafclump4.png |
| stage/act1/relief/act1_leafclump5.png | 20 | 0.0 | 0.0 | 6.14 | unity/Assets/Resources/Art/props/act1_leafclump5.png |
| stage/act1/relief/act1_reed.png | 22 | 0.0 | 0.0 | 6.92 | unity/Assets/Resources/Art/props/act1_reed.png |
| stage/act1/relief/act1_rock_big1.png | 31 | 0.0 | 0.0 | 5.68 | unity/Assets/Resources/Art/props/act1_rock_big1.png |
| stage/act1/relief/act1_rock_big2.png | 31 | 0.0 | 0.0 | 13.73 | unity/Assets/Resources/Art/props/act1_rock_big2.png |
| stage/act1/relief/act1_root.png | 25 | 0.0 | 0.0 | 7.53 | unity/Assets/Resources/Art/props/act1_root.png |
| stage/act1/relief/act1_tree_giant.png | 37 | 0.0 | 0.0 | 6.03 | unity/Assets/Resources/Art/props/act1_tree_giant.png |
| stage/act1/relief/act1_tree_oak.png | 32 | 0.0 | 0.0 | 6.42 | unity/Assets/Resources/Art/props/act1_tree_oak.png |
| stage/act1/relief/act1_tree_oak2.png | 28 | 0.0 | 0.0 | 4.21 | unity/Assets/Resources/Art/props/act1_tree_oak2.png |
| stage/act1/relief/bush_1.png | 28 | 0.0 | 0.0 | 3.68 | docs/pixellab/hd2d-act1/raw/relief-clean/bush_1.png |
| stage/act1/relief/canopy_1.png | 26 | 0.0 | 0.0 | 4.03 | docs/pixellab/hd2d-act1/raw/relief-clean/canopy_1.png |
| stage/act1/relief/canopy_2.png | 21 | 0.0 | 0.0 | 3.86 | docs/pixellab/hd2d-act1/raw/relief-clean/canopy_2.png |
| stage/act1/relief/canopy_3.png | 26 | 0.0 | 0.0 | 3.56 | docs/pixellab/hd2d-act1/raw/relief-clean/canopy_3.png |
| stage/act1/relief/fern_1.png | 23 | 0.0 | 0.0 | 4.58 | docs/pixellab/hd2d-act1/raw/relief-clean/fern_1.png |
| stage/act1/relief/fern_2.png | 19 | 0.0 | 0.0 | 3.38 | docs/pixellab/hd2d-act1/raw/relief-clean/fern_2.png |
| stage/act1/relief/frame_1.png | 29 | 0.0 | 0.0 | 5.14 | docs/pixellab/hd2d-act1/raw/relief-clean/frame_1.png |
| stage/act1/relief/frame_2.png | 20 | 0.0 | 0.0 | 3.44 | docs/pixellab/hd2d-act1/raw/relief-clean/frame_2.png |
| stage/act1/tiles/side_bark_a.png | 11 | 0.0 | 0.0 | 2.32 | docs/pixellab/hd2d-act1/calm/side_bark_a.png |
| stage/act1/tiles/side_bark_b.png | 14 | 0.0 | 0.0 | 2.21 | docs/pixellab/hd2d-act1/calm/side_bark_b.png |
| stage/act1/tiles/side_bark_c.png | 13 | 0.0 | 0.0 | 2.53 | docs/pixellab/hd2d-act1/calm/side_bark_c.png |
| stage/act1/tiles/side_bark_d.png | 14 | 0.0 | 0.0 | 2.22 | docs/pixellab/hd2d-act1/calm/side_bark_d.png |
| stage/act1/tiles/side_rock_a.png | 14 | 0.0 | 0.0 | 1.63 | docs/pixellab/hd2d-act1/calm/side_rock_a.png |
| stage/act1/tiles/side_rock_b.png | 14 | 0.0 | 0.0 | 1.72 | docs/pixellab/hd2d-act1/calm/side_rock_b.png |
| stage/act1/tiles/side_rock_c.png | 12 | 0.0 | 0.0 | 1.56 | docs/pixellab/hd2d-act1/calm/side_rock_c.png |
| stage/act1/tiles/side_rock_d.png | 14 | 0.0 | 0.0 | 1.57 | docs/pixellab/hd2d-act1/calm/side_rock_d.png |
| stage/act1/tiles/side_wood_a.png | 8 | 0.0 | 0.0 | 1.57 | docs/pixellab/hd2d-act1/calm/side_wood_a.png |
| stage/act1/tiles/side_wood_b.png | 8 | 0.0 | 0.0 | 1.43 | docs/pixellab/hd2d-act1/calm/side_wood_b.png |
| stage/act1/tiles/side_wood_c.png | 9 | 0.0 | 0.0 | 1.41 | docs/pixellab/hd2d-act1/calm/side_wood_c.png |
| stage/act1/tiles/side_wood_d.png | 8 | 0.0 | 0.0 | 1.39 | docs/pixellab/hd2d-act1/calm/side_wood_d.png |
| stage/act1/tiles/top_grass_a.png | 22 | 0.0 | 0.0 | 2.71 | docs/pixellab/hd2d-act1/calm/top_grass_a.png |
| stage/act1/tiles/top_grass_alt_a.png | 19 | 0.0 | 0.0 | 2.29 | docs/pixellab/hd2d-act1/calm/top_grass_alt_a.png |
| stage/act1/tiles/top_grass_alt_b.png | 19 | 0.0 | 0.0 | 2.13 | docs/pixellab/hd2d-act1/calm/top_grass_alt_b.png |
| stage/act1/tiles/top_grass_alt_c.png | 23 | 0.0 | 0.0 | 2.44 | docs/pixellab/hd2d-act1/calm/top_grass_alt_c.png |
| stage/act1/tiles/top_grass_alt_d.png | 19 | 0.0 | 0.0 | 2.34 | docs/pixellab/hd2d-act1/calm/top_grass_alt_d.png |
| stage/act1/tiles/top_grass_b.png | 21 | 0.0 | 0.0 | 2.59 | docs/pixellab/hd2d-act1/calm/top_grass_b.png |
| stage/act1/tiles/top_grass_c.png | 19 | 0.0 | 0.0 | 2.32 | docs/pixellab/hd2d-act1/calm/top_grass_c.png |
| stage/act1/tiles/top_grass_d.png | 20 | 0.0 | 0.0 | 2.31 | docs/pixellab/hd2d-act1/calm/top_grass_d.png |
| stage/act1/tiles/top_path_a.png | 3 | 0.0 | 0.0 | 1.19 | docs/pixellab/hd2d-act1/calm/top_path_a.png |
| stage/act1/tiles/top_path_b.png | 3 | 0.0 | 0.0 | 1.21 | docs/pixellab/hd2d-act1/calm/top_path_b.png |
| stage/act1/tiles/top_path_c.png | 3 | 0.0 | 0.0 | 1.27 | docs/pixellab/hd2d-act1/calm/top_path_c.png |
| stage/act1/tiles/top_path_d.png | 3 | 0.0 | 0.0 | 1.25 | docs/pixellab/hd2d-act1/calm/top_path_d.png |
| stage/act1/tiles/top_rock_a.png | 9 | 0.0 | 0.0 | 1.87 | docs/pixellab/hd2d-act1/calm/top_rock_a.png |
| stage/act1/tiles/top_rock_b.png | 11 | 0.0 | 0.0 | 1.97 | docs/pixellab/hd2d-act1/calm/top_rock_b.png |
| stage/act1/tiles/top_rock_c.png | 11 | 0.0 | 0.0 | 1.67 | docs/pixellab/hd2d-act1/calm/top_rock_c.png |
| stage/act1/tiles/top_rock_d.png | 12 | 0.0 | 0.0 | 1.65 | docs/pixellab/hd2d-act1/calm/top_rock_d.png |
