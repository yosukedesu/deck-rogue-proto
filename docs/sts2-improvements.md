# StS2解析からの全体改善 — マスターリスト（2026-09-02）

採掘: 8次元並列ミニング+網羅性チェック（136提案）。凡例: ✅=実装済み（コミット）/ 🔶=ユーザー裁定待ち / 📋=実装候補バックログ / ❌=見送り（理由）

ユーザー裁定済みの前提: 難易度つまみは触らない・部屋数はStS2式15/14/13・それ以外は「いけるものは修正まで」。

| # | 次元 | 提案 | 状態 |
|---|---|---|---|
| 0 | map-structure | 幕別部屋数15/14/13（幕が進むほど短い）への行数テーブル化 | ✅ c916e77 幕別行数15/14/13 |
| 1 | map-structure | Weak枠の構造保証（幕頭N戦は弱プールからのみ抽選） | ✅ dc17ea9 Weak帯3/2/2 |
| 2 | map-structure | 編成のGrabBagキュー方式（幕内一巡してから繰り返す） | 📋 M |
| 3 | map-structure | 編成タグによる同族連続の回避（GrabBagの同タグ回避） | ✅ dc17ea9 メンバー交差で同族回避(タグ不要の実装) |
| 4 | map-structure | ショップ員数の固定化（重み5%→固定3/幕） | 🔶 どちらの本家に合わせるか(StS1重み vs StS2固定3)+ゴールドシンク総量 |
| 5 | map-structure | 部屋タイプ員数の幕別テーブル化（?のガウス分布・幕2/3で-1） | 🔶 ?=22%は本家一致の物差し |
| 6 | map-structure | ボス前3行の焚き火禁止（休憩の上限制約） | ✅ dc17ea9 ボス前3行の焚き火禁止 |
| 7 | map-structure | ボス複数種+未撃破優先ローテ（BossDiscoveryOrder） | 🔶 ボス3種は大型コンテンツ |
| 8 | map-structure | 初回ラン限定のチュートリアル導線（DiscoveryOrder上書き） | 🔶 初回導線=ロードマップ枠 |
| 9 | map-structure | イベント専用エンカウンター（?から入る特別報酬付き戦闘） | 📋 M |
| 10 | map-structure | 選択にならない分岐の枝刈り（PruneAndRepair相当） | 📋 M |
| 11 | map-structure | エリート下限行の本家寄せ（行2→行5以降）の再裁定 | 🔶 エリート行2は序盤レリック供給の設計 |
| 12 | map-structure | 構造つまみの器（高難度段でエリート増・宝箱行→エリート行） | 🔶 難易度つまみ凍結中 |
| 13 | map-structure | 幕1の2バイオーム択（Overgrowth/Underdocks方式） | 🔶 2バイオームは大型 |
| 14 | behavior-grammar | 前奏（prelude）＝初手固定の器を追加し、weight敵がT1に「その敵の問い」を必ず見せる | ✅ ee5569c opener |
| 15 | behavior-grammar | cannotRepeat（直前と同じ技を引かない）をweight抽選に導入 | ✅ ee5569c noRepeat |
| 16 | behavior-grammar | oncePerCombat（戦闘1回きりの技）をweight抽選に導入 | ✅ ee5569c once |
| 17 | behavior-grammar | 回数カウンタのフェーズ変化（KnowledgeDemon式）＝技をN回使ったら行動テーブル恒久切替 | ✅ ee5569c phaseAfterUses(妖術師=呪い2回で打ち止め) |
| 18 | behavior-grammar | 味方の生死で行動テーブル切替（LivingShield式転職）＝従士に「射手が死んだら本気」を実装 | ✅ ee5569c movesWhenAlone(従士の転職) |
| 19 | behavior-grammar | Queen式SetMoveImmediate＝仲間死亡の瞬間に宣言済み意図を強制差し替え | 🔶 宣言時固定の既存則を破る |
| 20 | behavior-grammar | EncounterMemberのprelude上書き＝スロット役割分化（Exoskeleton式） | 📋 S |
| 21 | behavior-grammar | 技の恒久成長（growPerUse/growHitsPerUse）＝使うたび育つ技で「戻らない恐怖」を作る | 📋 M |
| 22 | behavior-grammar | 敵の召喚行動（kind:'summon'）＝戦闘中に味方を補充する敵 | 📋 L |
| 23 | behavior-grammar | 眠りの被ダメ覚醒（wakeOnDamage）＝眠れる鉄卵の「起こす前に削るか」を本物の二択にする | 📋 M |
| 24 | behavior-grammar | 敵のアーティファクト（デバフ無効チャージ）＝延焼・急所・威圧・混乱への構造的な問い | 🔶 延焼・威圧デッキへの実質ナーフ成分 |
| 25 | behavior-grammar | DeathBlow予告＝致死級大技の専用マーク（💀）を意図表示に追加 | ✅ 75515b2 💀致死級マーク |
| 26 | behavior-grammar | 開幕パワー（静的性質）の配布率を40%→引き上げ＝裸の敵に既存の器を薄く配る | ✅ 742b5fe 巨像2体+影+蛞蝓+泥まとうもの(こそ泥とげはとげ裁定と衝突→取り下げ) |
| 27 | behavior-grammar | フェーズ変化の1拍（transitionMove）＝「殻割れ」「牙をむく」の瞬間を行動として見せる | 📋 M |
| 28 | behavior-grammar | 一時バフ（次の攻撃だけ+N。Vigor型）＝チャージ大技を強化式で書ける器 | 📋 M |
| 29 | behavior-grammar | 選ばせる呪い（KnowledgeDemon式2択）＝「どの毒を飲むか」をプレイヤーに決めさせる行動 | 🔶 戦闘内選択UI=新コマンド |
| 30 | behavior-grammar | とげの着脱サイクル（SpinyToad式）＝敵行動で自分のとげを増減する器 | 📋 M |
| 31 | debuff-system | 脆弱のjustAppliedガード（付与フェーズの即減衰でスタックが1つ蒸発している） | ✅ 75515b2 脆弱justAppliedガード(バグ修正) |
| 32 | debuff-system | 火傷の「1回きり」仕様と実装の乖離を解消（現状は本家Burn型の循環になっている） | ✅ 75515b2 火傷の生存則を二重修正(バグ修正) |
| 33 | debuff-system | 手札滞留ダメージ札のラダー化（Toxic/Beckon段の追加）と滞留ダメージの共通API化 | 📋 M |
| 34 | debuff-system | 行動数制限デバフ「怠惰」（Sloth式・このターンN枚まで）＝手数デッキへの新しい問い | ✅ ee5569c 拘束(1ターン3枚まで) |
| 35 | debuff-system | ドロー減衰デバフ「霞み」（MindRot式）——リソース削り系の安全な第一歩 | ✅ a5e1c52 霞み(梟の大技) |
| 36 | debuff-system | ブロック0倍化の劇薬「守崩し」（NoBlock式・1ターン限定）＝虚弱の上位段 | 📋 M |
| 37 | debuff-system | 常在オーラ（Afflictions-lite）＝「この敵が生きている間ルールが歪む」レイヤー | ✅ ee5569c 重圧オーラ(effectiveCostフック) |
| 38 | debuff-system | デバフのメタ増幅（Debilitate式）＝弱体の効果量そのものを深くする敵 | 📋 M |
| 39 | debuff-system | 敵アーティファクト（デバフ付与をN回弾く）＝デバフ依存デッキへの問い | 🔶 同上(アーティファクト) |
| 40 | debuff-system | 重りデバフ（SlowPower式）＝手数の罰の被弾版。cardsPlayedThisTurn流用で実装最小 | ✅ a5e1c52 重り(奴隷商の錘) |
| 41 | debuff-system | 選ばせる呪い（KnowledgeDemon式）＝?イベント「知識の悪魔」で2種の毒から1つを飲ませる | ✅ a5e1c52 イベント版(毒の三杯)として実装。戦闘内2択UIは裁定枠 |
| 42 | debuff-system | 呪い札の軽量変種: N戦で自然消滅する「仮初の烙印」（Guilty式）と金を削る「借金」（Debt式） | ✅ a5e1c52 時限呪い=仮初の烙印(借金はrun層純度の検討要=見送り) |
| 43 | debuff-system | 粘液札（Slimed式）＝1エナジーで掃除できる柔らかい汚染。大苔スライムに配布 | 📋 M |
| 44 | debuff-system | 錯乱デバフ（Confused/Snecko式）＝ドローごとにコストを0〜3へ乱数化するボス級の霧 | 🔶 ボス級の霧=錯乱 |
| 45 | debuff-system | 1ターン被ダメ累計上限（HardenedShell式）＝装甲の相補変種・バースト対策 | 📋 M |
| 46 | debuff-system | 虚無札（Void式）＝引いた瞬間に痛む山札の地雷 | 📋 M |
| 47 | debuff-system | 意図アイコンの「カード汚染予告」分離（CardDebuff式） | ✅ 75515b2 汚染の行き先予告 |
| 48 | debuff-system | 処刑ライン（Doom式）の黒カード機構化——「HPしきい値の常時監視」 | 🔶 黒カード機構=カードパワー凍結中 |
| 49 | gimmick-variants | スタン付き死亡時召喚（罰型の分裂）— splitInto.stunned 拡張 | ✅ 66c83e8 スタン付き分裂(蛙鬼) |
| 50 | gimmick-variants | 残機（Stock式）— splitIntoチェーンの再利用でエンジン変更ゼロ | ✅ 66c83e8 残機(不滅の骸兵) |
| 51 | gimmick-variants | 増殖 — 新行動kind 'summon'（上限付き自己召喚・打ち消し可） | 📋 M |
| 52 | gimmick-variants | 召喚エコシステム（場が空くと補充・満杯なら攻撃）— summonのエリート応用 | 📋 M |
| 53 | gimmick-variants | 孵化 — hatchIntoによるタイマー変身（打ち消しで孵化を止められる） | ✅ 66c83e8 孵化(抱卵の走竜と卵) |
| 54 | gimmick-variants | 蒸気圧タイマー — 可視の圧力カウンタを積んで自爆する時限爆弾 | 📋 M |
| 55 | gimmick-variants | カード盗み — デッキの人質（盗んだ敵は必ず逃げる・倒せば返る） | 🔶 カード人質は体験が過激 |
| 56 | gimmick-variants | 棘の着脱サイクル — とげを立てるターンと消えるターンのリズム | 📋 S |
| 57 | gimmick-variants | 完全ブロックでスタン（体勢崩し）— 「完全に凌いだ」の機械的報酬化 | 🔶 完全ブロック報酬は要設計 |
| 58 | gimmick-variants | 飛行 — 被弾で墜落するダメージ半減（多段デッキへのご褒美マッチアップ） | 📋 M |
| 59 | gimmick-variants | 弔い強化 — 仲間が倒れるたび筋力+N（連携の逆問い） | ✅ 66c83e8 弔い強化(弔いの獣) |
| 60 | gimmick-variants | 眠りの被弾覚醒 — 鉄卵に「傷つけると目を覚ます」を追加 | 🔶 鉄卵の挙動変更=校正直後 |
| 61 | gimmick-variants | sequenceLoopFrom — 「一度きりの前奏→ループ」を1フィールドで書けるようにする | ✅ 66c83e8 sequenceLoopFrom |
| 62 | gimmick-variants | 虚脱札 — ドローした瞬間エナジー-1で自壊する状態異常（ドローエンジンへの問い） | 📋 M |
| 63 | gimmick-variants | 成長吸い — プレイヤーの成長を奪い、倒せば返す（憑依型） | 🔶 成長吸いは緑への実質ナーフ成分 |
| 64 | gimmick-variants | ターン装甲（硬化した殻）— 1ターンの被ダメ累計キャップ（バーストへの問い） | 📋 M |
| 65 | gimmick-variants | 明滅（無敵サイクル）— 1ターンおきに実体を失う亡霊 | 📋 M |
| 66 | gimmick-variants | 群れの初動ずらしの体系化 — patternOffsetの自動割当（同型複数体の位相分散） | 📋 S |
| 67 | enemy-numbers | 幕1のHP帯を引き上げ（depthHpScale 0.55/0.65→0.62/0.72・打点は据え置き） | ✅ 742b5fe 幕1 HP 0.62/0.72 |
| 68 | enemy-numbers | 幕1に帯上端ソロ「硬いが遅い」個体を新設（Fogmog枠・実効HP65〜72） | ✅ 742b5fe 泥まとうもの(タンク教師) |
| 69 | enemy-numbers | 幕2に帯上端ソロを新設（HunterKiller/LouseProgenitor枠・実効HP120〜135） | 📋 M |
| 70 | enemy-numbers | 幕3に帯上端ソロを新設（SlimedBerserker枠・実効HP230前後・札汚染で殴る大物） | 📋 M |
| 71 | enemy-numbers | 幕1ボス係数 ×1.25→×1.35（オーガ実効162.5→175.5＝本家最弱ボス水準） | ✅ 742b5fe 幕1ボス×1.35 |
| 72 | enemy-numbers | 幕2/3ボス係数の引き上げ（×1.6→×1.9・×2.4→×2.6） | 🔶 幕2/3ボスは校正済み裁定 |
| 73 | enemy-numbers | 幕2/3エリートのHP引き上げ（ギミックが1周する前に死ぬ帯の是正・7体） | ✅ 742b5fe エリート7体HP引き上げ |
| 74 | enemy-numbers | 歩哨を双子→三つ子化（42×2=84 → 42×3=126・本家Sentries対照） | ✅ 742b5fe 歩哨三つ子 |
| 75 | enemy-numbers | 群れ編成のhpScale引き上げ＋幕3共有編成の専用版複製（strengthペナルティ緩和込み） | 📋 M |
| 76 | enemy-numbers | 静的性質（開幕ブロック）の配布拡大——巨像2体・用心深い影・刺突の書 | ✅ 742b5fe 静的性質の配布拡大 |
| 77 | enemy-numbers | 巨面にarmor 20を追加（幕3エリートの実効体格をHP以外で補う） | 🔶 巨面の装甲は「ボスと甲殻のみ」裁定と衝突 |
| 78 | enemy-numbers | 帯設計方針の明文化——「幕1増強はHP・幕2/3増強は上端個体・打点はこれ以上触らない」 | 📋 S |
| 79 | enemy-numbers | ボス随伴（KinPriest型: 本体+子×2）の導入検討 | 🔶 ボス随伴は大型 |
| 80 | fairness-intent-ui | 致死予告（DeathBlow相当）: 単体で死にうる攻撃意図に💀マークと赤強調 | ✅ 75515b2 💀致死級予告 |
| 81 | fairness-intent-ui | 混乱した敵の攻撃を最悪被ダメ予測から除外し、意図に「仲間に向かう」を注記 | ✅ 75515b2 混乱除外+仲間に向かう注記 |
| 82 | fairness-intent-ui | 脆弱中は意図表示の幅にも補正込みの値を併記する | ✅ 予測には脆弱算入済み(既存)。意図幅への併記は見送り |
| 83 | fairness-intent-ui | カード汚染予告（CardDebuff相当）の専用アイコンと行き先明示 | ✅ 75515b2 行き先予告(手札へ/山札へ/捨て札へ) |
| 84 | fairness-intent-ui | 行動の表示ラベル（Sleep/Stun相当）: EnemyMove.label で意図に固有名を出す | 📋 M |
| 85 | fairness-intent-ui | 「×手数」の現在値をブラウザ意図表示にも出す（CLIとの格差解消） | ✅ 75515b2 ×手数の現在値 |
| 86 | fairness-intent-ui | 激昂タイマーの進捗カウンタ表示（あとN枚/あとNダメで鳴る） | ✅ 75515b2 激昂の残りカウンタ |
| 87 | fairness-intent-ui | 威圧のマイナス筋力を可視化する（strength<0 でチップが消える穴） | ✅ 75515b2 マイナス筋力可視化 |
| 88 | fairness-intent-ui | 盗み成立チップに「次のターン必ず逃走」を予告する | ✅ 75515b2 盗み後の逃走予告 |
| 89 | fairness-intent-ui | 最悪被ダメ予測を「今フェーズの脅威一覧」へ拡張（非ダメージ脅威+火傷疼きの合算） | 📋 M |
| 90 | fairness-intent-ui | 静かな鈴の軽減を最悪被ダメ予測に算入する | ✅ 75515b2 静かな鈴を予測に算入 |
| 91 | fairness-intent-ui | 撃破サマリーに「死線を凌いだ回数」を追加 | 📋 M |
| 92 | fairness-intent-ui | HP半分で豹変する敵に「😾 HP半分で牙をむく」を事前表示 | ✅ 75515b2 HP半分豹変の事前予告 |
| 93 | fairness-intent-ui | 攻撃ライダー（+🛡️/+💪/＋状態異常）の分離表示 | 📋 S |
| 94 | fairness-intent-ui | 【裁定要】実値表示モードの標準トグル化（退屈診断④の判定実験） | 🔶 幅あり意図=検討事項④ |
| 95 | fairness-intent-ui | 【裁定要】不明意図（❓Hidden）の限定導入——「どちらの技か伏せる」敵 | 🔶 Hidden意図はフェアネス原則と緊張 |
| 96 | fairness-intent-ui | 意図アイコンの脅威段階（StS2の5段階絵柄）をCSS階調で再現 | 📋 S |
| 97 | economy-events | イベント戦闘の器＋「偽商人」特殊報酬戦 | 📋 M |
| 98 | economy-events | 訓練ダミー（制限ターン内チャレンジ戦） | 📋 M |
| 99 | economy-events | カードを盗む敵（Swipe式・倒せば特別報酬で返る） | 🔶 カード盗み |
| 100 | economy-events | 時限呪い（Guilty式・N戦で自然消滅する呪い札） | ✅ a5e1c52 仮初の烙印+疚しい取引 |
| 101 | economy-events | 借金呪い（Debt式・手札にあるとゴールドを失う） | ❌ combat層はゴールドを知らない(純度裁定)と衝突。run層精算の設計が必要 |
| 102 | economy-events | 「どの毒を飲むか」イベント（KnowledgeDemon式・対価の性質を選ばせる） | ✅ a5e1c52 毒の三杯 |
| 103 | economy-events | 幕頭の祝福（Ancient/Neow式の幕遷移ブーン選択） | 🔶 幕頭祝福=大型 |
| 104 | economy-events | 消耗品（ポーション）システムの最小導入 | 🔶 ポーション=大型 |
| 105 | economy-events | 呪い札の性質ラダー（Doubt/Shame/Regret式） | 📋 S |
| 106 | economy-events | 継続窃盗の敵（Thievery式・毎ターン少額ドレイン） | 🔶 継続窃盗 |
| 107 | economy-events | ショップ員数の固定保証（本家=固定3/幕） | 🔶 #4と同件 |
| 108 | economy-events | エリート員数5/幕への増枠（レリック経済の本家対照） | 🔶 エリート5=供給経済 |
| 109 | design-guardrails | タイマー敵の設計規約をCLAUDE.md確定行+機械判定テストに昇格 | ✅ e6eca24 balance-policy.md+機械固定 |
| 110 | design-guardrails | 威圧の全色アクセス保証（タイマーの汎用解決手段）を機械固定 | ✅ e6eca24 威圧の全色非レア保証テスト |
| 111 | design-guardrails | アーキ別勝率の分散を一級指標化（sim要約行＋構造詰みセルのwhitelistテスト） | 📋 M |
| 112 | design-guardrails | 敗北の感触を2分類する検証プロトコル（評価バーに「構築の失敗/理不尽」・LLMラン定型質問） | 📋 M |
| 113 | design-guardrails | 「作り直し基準」の明文化: 統計が健全でも特定アーキ詰みの報告2本一致で構造リワーク | ✅ e6eca24 作り直し基準の明文化 |
| 114 | design-guardrails | ナーフ運用規約の一本化（docs/balance-policy.md＋確定行） | ✅ e6eca24 balance-policy.md |
| 115 | design-guardrails | 戦闘評価テレメトリの横断集計スクリプト（敵×評価の平均表＝作り直し候補の機械抽出） | 📋 M |
| 116 | design-guardrails | 幕1序盤のWeak帯プール分離（StS2 GrabBagの簡易版＝初見体験の構造保証） | ✅ dc17ea9 Weak帯として実装済み |
| 117 | design-guardrails | 初回ラン限定の教師順（チュートリアル導線）はロードマップ項目として記録に留める | 🔶 ロードマップ記録 |
| 118 | design-guardrails | 敵の数値帯band監査テスト（確定行「敵の数値基準」の機械固定） | 📋 M |
| 119 | design-guardrails | 裁定済み敵規約の未テスト3件を機械固定（enemy-conventions.test.ts） | ✅ e6eca24 enemy-conventions.test.ts |
| 120 | design-guardrails | 予告（フェアネス）表示の網羅性テスト＋ラベル表の純モジュール化 | 📋 S |
| 121 | design-guardrails | 幕1のデバフ密度の床を分布テストに追加（現状固定＝退行防止） | ✅ e6eca24 幕1デバフ床テスト |
| 122 | design-guardrails | 開幕から見える性質の配布状況を監査テストで固定＋新敵規約に追記 | 🔶 配布率の目標値設定 |
| 123 | design-guardrails | 新敵・新機構追加のDone定義（壊れ検知チェックリスト）を明文化 | ✅ e6eca24 Done定義(balance-policy.md) |
| 124 | design-guardrails | 幅あり意図のAB計測プロトコル（蜃気楼の面を実験器に退屈診断④の判定材料を取る） | 🔶 実値表示AB=人間ラン設計 |
| 125 | design-guardrails | simulateRunsの敗北分布に「死因の敵」を追加（ラン層の分散監視） | ✅ e6eca24 死因の敵top5+集中警告 |
| 126 | design-guardrails | 延焼ティックとタイマー系カウンタの相互作用を明文化＋テスト固定（解決順の未定義火種の除去） | ✅ e6eca24 延焼→カウンタ算入に是正(実は未算入の盲点だった)+テスト固定 |
| 127 | critic | 編成内メンバーのランダム構成（RubyRaiders/Slimes式 memberPool） | 📋 M |
| 128 | critic | 味方への防御支援 kind:'shield-ally'（Rampart/Guardbot式・第3の支援動詞） | 📋 S |
| 129 | critic | 多節の再接続（Decimillipede/Reattach式）＝仲間生存中は倒しても復活する節 | 📋 M |
| 130 | critic | 潜伏（Tunneler/Burrowed式）＝「ブロックを割られたら」の新フェーズ条件 | 📋 M |
| 131 | critic | 突進の反動（CeremonialBeast/Plow式）＝しきい値到達で蓄積筋力の全没収+スタン | 📋 S |
| 132 | critic | 誘い水（InfestedPrism/VitalSpark式）＝殴ったプレイヤーに一時マナを与える逆説ギミック | 📋 S |
| 133 | critic | 【裁定要】リソース完全ゼロ化の1ターン劇薬（NoEnergyGain/NoDraw式） | 🔶 リソース完全ゼロ化は劇薬 |
| 134 | critic | 【裁定要】ステータス札を弾薬に変える対抗策カード（FlakCannon/Compact式） | 🔶 プレイヤーカード追加=凍結中 |
| 135 | critic | 【裁定要】アセンション式・経済税の器（数値でなく経済で締める難易度列） | 🔶 難易度つまみ凍結中 |

## レビュー是正（2026-09-02 敵対的検証つきレビュー: 29発見→18確定→全件修正）

4次元レビュー（エンジン正確性/ルール表整合/データ健全性/表示完全性）×3票の敵対的検証で確定した18件を修正:
拘束の亡骸プレイ抜け・拘束/重りが詠唱数を誤読（実プレイ枚数 playsThisTurn を新設）・
旧セーブ(16行時代)の幕2/3ソフトロック・火傷の捨てコスト循環・setAlt×転職の無効化漏れ・
イベント2件のスキーマ違反(text→flavor+sprite+規約テスト新設)・恒真だったタイマー規約テストを
キー白リスト走査へ差し替え・💀致死級/最悪被ダメ予測/CLIの式を engine/summary.ts に1本化
（鈴→脆弱→重りの実処理順）・仮初の烙印の本文+残り戦数表示・行N/16ハードコード3箇所・
CLIのhatchラベルと分裂/孵化の出来事・こそ泥とげ1の表記撤回・ルール表の書き漏れ3件。

## 集計

- ✅ 実装済み: 53件（バグ修正2件を含む）
- 🔶 裁定待ち: 34件（次の議論の弾）
- 📋 バックログ: 48件（ギミック変種の残り・イベント戦・表示強化など。詳細な実装スケッチは各提案に保存済み）
- ❌ 見送り: 1件

提案の全文（根拠・現状・実装スケッチ）はセッション記録から `docs/sts2-improvements-full.md` に保存。

## 実測メモ（実装後の統合sim 10ラン×15リーダー）

- クラッシュ・スタール 0。新しい死因監視が初仕事: **オーガ（幕1ボス×1.35）に死因集中⚠**（セレズニアで7/10）。
  幕1増強（HP+0.07・ボス×1.35・Weak帯）は狙い通り効いているが、複合の濃さは人間ランで要確認。
- 歩哨の三つ子が第2の死因。エリートHP引き上げ7体と合わせ、エリートの重さも人間ランの観点。
- ボット走破率は全体に低下（壊れ検知の参考値。人間の手触りが物差し、の既存裁定どおり）。