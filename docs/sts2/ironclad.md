# StS2 アイアンクラッド カードプール構造分析（v0.103.3 デコンパイル解析）

出典: `IroncladCardPool.GenerateAllCards()` に列挙された 87 クラス。各 `Models.Cards/<Class>.cs` のコンストラクタ（コスト/タイプ/レアリティ）・`OnPlay`・`OnUpgrade`、参照先の `Models.Powers/*Power.cs` を読んで言語化した。コードは転載していない。
凡例: 「→」は鍛えた（アップグレード）後の値。E=エナジー。「脆弱」=Vulnerable（被ダメ1.5倍）、「弱体」=Weak、「筋力」=Strength。
キャラ基礎: 初期HP80・初期ゴールド99・初期デッキ Strike×5 / Defend×4 / Bash×1（10枚）・初期レリック Burning Blood。

---

## ① 集計

| 項目 | 値 |
|---|---|
| 総数（プール列挙） | **87** |
| レアリティ | Basic 3 / Common 20 / Uncommon 36 / Rare 26 / **Ancient 2**（Break・Corruption。通常のレア度抽選の外＝Ancientイベント経由の特殊枠） |
| うち協力プレイ専用（単人では非提示） | 2（DemonicShield=U・Tank=R） |
| 単人の通常報酬プール（Basic・Ancient・協力専用を除く） | **80枚 = C20 / U35 / R25** |
| タイプ | Attack **37** / Skill **29** / Power **21** / Status 0 / Curse 0 |
| コスト | 0E **12** / 1E **47** / 2E **19** / 3E **7** / X **2**（Whirlwind・Cascade）/ 4E以上 **0** |
| Power数 | **21**（24%。本家1の14枚=19%から+7） |
| 「消滅」キーワード持ち（自身が消滅） | **11**（DemonicShield・Dominate・Feed・FiendFire・ForgottenRitual・Impervious・InfernalBlade・MoltenFist・NotYet・Offering・Tremble） |
| 他のカードを消滅させる札 | **11**（Brand・BurningPact・Cinder・Corruption・DrumOfBattle・FiendFire・Havoc・SecondWind・Stoke・Thrash・TrueGrit） |
| 消滅を参照する刈り取り/接着剤 | **7**（AshenStrike・DarkEmbrace・EvilEye・FeelNoPain・ForgottenRitual・HowlFromBeyond・PactsEnd） |
| 消滅に関与する札の合計（重複除く） | **27**（31%） |
| 全体攻撃（AoE） | **7**（Breakthrough・Conflagration・HowlFromBeyond・PactsEnd・Stomp・Thunderclap・Whirlwind）＋Power由来の全体ダメ1（Inferno） |
| 多段ヒット | **9**（TwinStrike×2・SwordBoomerang×3ランダム・FightMe×2・Thrash×2・Whirlwind×X・FiendFire×手札枚数・TearAsunder×被弾回数+1・Spite 条件×2/3・Dismantle 条件×2） |
| ドロー札 | **9**（BattleTrance・BurningPact・DrumOfBattle・Offering・Pillage・PommelStrike・ShrugItOff・DarkEmbrace・Vicious）＋捨て札回収 Aggression・山札操作 Headbutt |
| エナジー札 | **5**（Bloodletting・ExpectAFight・ForgottenRitual・Offering・Pyre）＋コスト0化/自動プレイ系 8（Unrelenting・Hellraiser・Stampede・InfernalBlade・Cascade・Havoc・Corruption・OneTwoPunch） |
| ドロー/エナジー札（重複除く） | **13** |
| 自傷札（自分のHPを失う） | **9**（BloodWall2・Bloodletting3・Brand1・Breakthrough1・Hemokinesis2・Offering6・DemonicShield1・CrimsonMantle毎T1・Inferno毎T1） |
| 自傷/被弾の刈り取り | **4**（Rupture・Spite・TearAsunder・Inferno）＋返し FlameBarrier |
| 脆弱（Vulnerable）関連 | **13**（付与7: Bash・Tremble・Thunderclap・Taunt・Uppercut・Dominate・Break／参照6: Bully・Dismantle・MoltenFist・Cruelty・Colossus・Vicious）＝StS2新設の軸 |
| 筋力を得る札 | **7**（Inflame・DemonForm・Brand・Dominate・FightMe・SetupStrike[一時]・Rupture）＋敵の筋力を下げる Mangle |
| Strikeタグ | **6**（StrikeIronclad・PommelStrike・TwinStrike・PerfectedStrike・SetupStrike・AshenStrike） |
| Innate / Ethereal / Retain | 素で持つ札は **0**。鍛えるとInnate: Aggression・Juggling。Ethereal・Retain・Unplayable は無し |
| 回復札 | 1（NotYet=HP10回復）。最大HP: Feed |
| 段階解放（Epoch） | 9枚が解放待ち: Epoch2=MoltenFist・Cruelty・Dominate／Epoch5=Cinder・PactsEnd・DrumOfBattle／Epoch7=BloodWall・TearAsunder・Inferno |

---

## ② 全カード表

タグ凡例: 筋力／消滅／ブロック蓄積／自傷／怒り・被弾／脆弱／手数（攻撃連打・Strike）／ドロー／エナジー／AoE／多段／大技／防御／その他

| クラス名 | コスト | タイプ | レア | 効果要約（→は鍛えた後） | タグ |
|---|---|---|---|---|---|
| Aggression | 1 | Power | Rare | 自ターン開始時、捨て札のランダムな攻撃1枚を手札に戻し、その札を鍛える → Innate付与 | 手数, ドロー |
| Anger | 0 | Attack | Common | 6ダメ(→8)。このカードのコピー1枚を捨て札に加える | 手数, その他(増殖) |
| Armaments | 1 | Skill | Common | ブロック5。手札1枚を鍛える → 手札全てを鍛える | 防御, その他 |
| AshenStrike | 1 | Attack | Uncommon | 6＋消滅置き場の枚数×3ダメ(→×4)。Strikeタグ | 消滅, 大技 |
| Barricade | 3 | Power | Rare | ブロックがターン終了で消えなくなる → 2E | ブロック蓄積 |
| Bash | 2 | Attack | Basic | 8ダメ＋脆弱2(→10＋3) | 脆弱 |
| BattleTrance | 0 | Skill | Uncommon | 3ドロー(→4)。このターン以降ドローできない | ドロー |
| BloodWall | 2 | Skill | Common | HP-2、ブロック16(→20)【Epoch7解放】 | 自傷, 防御 |
| Bloodletting | 0 | Skill | Common | HP-3、エナジー+2(→+3) | 自傷, エナジー |
| Bludgeon | 3 | Attack | Uncommon | 32ダメ(→42) | 大技 |
| BodySlam | 1 | Attack | Common | 現在のブロック×1ダメ → 0E | ブロック蓄積, 大技 |
| Brand | 0 | Skill | Rare | HP-1、手札1枚を選んで消滅、筋力+1(→+2) | 筋力, 消滅 |
| Break | 1 | Attack | **Ancient** | 20ダメ＋脆弱5(→30＋7) | 脆弱, 大技 |
| Breakthrough | 1 | Attack | Common | HP-1、全体9ダメ(→13) | AoE, 自傷 |
| Bully | 0 | Attack | Uncommon | 4＋対象の脆弱×2ダメ(→×3) | 脆弱, 大技 |
| BurningPact | 1 | Skill | Uncommon | 手札1枚を選んで消滅、2ドロー(→3) | 消滅, ドロー |
| Cascade | X | Skill | Rare | 山札の上X枚をコスト0で自動プレイ（消滅はしない）→ X+1枚 | その他(自動プレイ), エナジー |
| Cinder | 2 | Attack | Common | 18ダメ(→24)。手札のランダム1枚を消滅【Epoch5】 | 消滅, 大技 |
| Colossus | 1 | Skill | Uncommon | ブロック5(→8)。次の敵ターンまで、脆弱を持つ敵の攻撃ダメージを半減 | 脆弱, 防御 |
| Conflagration | 1 | Attack | Rare | 全体に 8＋このターンにプレイした攻撃×2ダメ(→9＋×3) | AoE, 手数 |
| Corruption | 3 | Power | **Ancient** | スキルのコストが0になり、プレイしたスキルは消滅する → 2E | 消滅, エナジー |
| CrimsonMantle | 1 | Power | Rare | 自ターン開始時にHP-1（重ね掛けごと+1）、ブロック8(→10) | ブロック蓄積, 自傷 |
| Cruelty | 1 | Power | Rare | 脆弱による被ダメ増加が+25%（1.5→1.75倍。→+50%＝2倍） | 脆弱 |
| DarkEmbrace | 2 | Power | Rare | カードが消滅するたび1ドロー → 1E | 消滅, ドロー |
| DefendIronclad | 1 | Skill | Basic | ブロック5(→8) | 防御 |
| DemonForm | 3 | Power | Rare | 自ターン開始時に筋力+2(→+3) | 筋力 |
| DemonicShield | 0 | Skill | Uncommon | 【協力専用】HP-1、味方1人に自分の現在ブロックと同量のブロック。消滅 → 消滅なし | ブロック蓄積, 自傷 |
| Dismantle | 1 | Attack | Uncommon | 8ダメ(→10)。対象が脆弱なら2回ヒット | 脆弱, 多段 |
| Dominate | 1 | Skill | Uncommon | 脆弱1(→2)を付与し、対象の脆弱の合計ぶん筋力+N。消滅【Epoch2】 | 脆弱, 筋力 |
| DrumOfBattle | 0 | Power | Uncommon | 2ドロー(→3)。以降毎ターン、手札ドロー後に山札の上1枚を消滅【Epoch5】 | 消滅, ドロー |
| EvilEye | 1 | Skill | Uncommon | ブロック8(→11)。このターンにカードを消滅していれば2回得る | 消滅, 防御 |
| ExpectAFight | 2 | Skill | Uncommon | 手札の攻撃1枚につきエナジー+1。このターン以降エナジーを得られない → 1E | エナジー, 手数 |
| Feed | 1 | Attack | Rare | 10ダメ(→12)。とどめなら最大HP+3(→+4)。消滅。戦闘内生成不可 | その他(最大HP), 消滅 |
| FeelNoPain | 1 | Power | Uncommon | カードが消滅するたびブロック3(→4) | 消滅, 防御 |
| FiendFire | 2 | Attack | Rare | 手札を全て消滅し、消滅した枚数×7ダメ(→10)。消滅 | 消滅, 多段 |
| FightMe | 2 | Attack | Uncommon | 5ダメ×2(→6×2)。自分の筋力+3(→+4)、対象の筋力+1 | 筋力, 多段 |
| FlameBarrier | 2 | Skill | Uncommon | ブロック12(→16)。このターン攻撃してきた敵に4(→6)ダメージ返し | 防御, 怒り・被弾 |
| ForgottenRitual | 1 | Skill | Uncommon | このターンにカードを消滅していればエナジー+3(→+4)。消滅 | 消滅, エナジー |
| Havoc | 1 | Skill | Common | 山札の上1枚をコスト0で自動プレイし消滅 → 0E | 消滅, その他 |
| Headbutt | 1 | Attack | Common | 9ダメ(→12)。捨て札から1枚選んで山札の上に置く | その他(山札操作) |
| Hellraiser | 2 | Power | Rare | Strikeタグの札を引くたび、即コスト0で自動プレイ → 1E | 手数, エナジー |
| Hemokinesis | 1 | Attack | Uncommon | HP-2、15ダメ(→20) | 自傷, 大技 |
| HowlFromBeyond | 3 | Attack | Uncommon | 全体16ダメ(→21)。消滅置き場にある間、自ターン開始時（ドロー前）にコスト0で自動プレイされ捨て札へ戻る | AoE, 消滅 |
| Impervious | 2 | Skill | Rare | ブロック30(→40)。消滅 | 防御, ブロック蓄積 |
| InfernalBlade | 1 | Skill | Uncommon | ランダムなアイアンクラッド攻撃1枚をこのターン0コストで手札に。消滅 → 0E | 手数, その他(生成) |
| Inferno | 1 | Power | Uncommon | 自ターン開始時HP-1（重ね掛けごと+1）。自ターン中にHPを失うたび敵全体に6(→9)ダメ【Epoch7】 | 自傷, AoE |
| Inflame | 1 | Power | Uncommon | 筋力+2(→+3) | 筋力 |
| IronWave | 1 | Attack | Common | ブロック5＋5ダメ(→7/7) | 防御, その他 |
| Juggernaut | 2 | Power | Rare | ブロックを得るたびランダムな敵に5(→7)ダメ | ブロック蓄積 |
| Juggling | 1 | Power | Uncommon | 1ターンに3枚目の攻撃をプレイした時、そのコピー1枚を手札に加える → Innate付与 | 手数 |
| Mangle | 3 | Attack | Rare | 15ダメ(→20)。対象の筋力をこのターン-10(→-15) | 大技, その他(デバフ) |
| MoltenFist | 1 | Attack | Common | 10ダメ(→14)。対象の脆弱を2倍にする（現在値ぶん追加付与）。消滅【Epoch2】 | 脆弱, 消滅 |
| NotYet | 2 | Skill | Rare | HP10回復(→13)。消滅。戦闘内生成不可 | その他(回復) |
| Offering | 0 | Skill | Rare | HP-6、エナジー+2、3ドロー(→5)。消滅 | 自傷, ドロー・エナジー |
| OneTwoPunch | 1 | Skill | Rare | このターン、次の攻撃1枚(→2枚)を2回プレイする | 手数, 大技 |
| PactsEnd | 0 | Attack | Rare | 全体17ダメ(→23)。消滅置き場に3枚以上ある時のみプレイ可【Epoch5】 | 消滅, AoE |
| PerfectedStrike | 2 | Attack | Common | 6＋デッキ内のStrike札×2ダメ(→×3)。Strike | 手数, 大技 |
| Pillage | 1 | Attack | Uncommon | 6ダメ(→9)。攻撃以外を引くまでドローし続ける（手札10まで） | ドロー, 手数 |
| PommelStrike | 1 | Attack | Common | 9ダメ＋1ドロー(→10＋2)。Strike | ドロー, 手数 |
| PrimalForce | 0 | Skill | Rare | 手札の攻撃全てをトークン「巨岩」（1E・16ダメ→鍛え済み20）に変身 | 大技, 手数 |
| Pyre | 2 | Power | Rare | 毎ターンのエナジー上限+1(→+2)。代償なし | エナジー |
| Rage | 0 | Skill | Uncommon | このターン、攻撃をプレイするたびブロック3(→5) | 手数, 防御 |
| Rampage | 1 | Attack | Uncommon | 9ダメ。プレイするたびこの戦闘中ダメージ+5(→+9) | 大技(成長), その他 |
| Rupture | 1 | Power | Uncommon | 自ターン中にHPを失うたび筋力+1(→+2) | 自傷, 筋力 |
| SecondWind | 1 | Skill | Uncommon | 手札の攻撃以外を全て消滅し、1枚につきブロック5(→7) | 消滅, 防御 |
| SetupStrike | 1 | Attack | Common | 7ダメ(→9)。このターン筋力+2(→+3)。Strike | 筋力(一時), 手数 |
| ShrugItOff | 1 | Skill | Common | ブロック8(→11)＋1ドロー | 防御, ドロー |
| Spite | 0 | Attack | Uncommon | 5ダメ。このターンにHPを失っていれば2回(→3回)ヒット | 自傷, 多段 |
| Stampede | 2 | Power | Uncommon | 自ターン終了時、手札のランダムな攻撃1枚をコスト0で自動プレイ → 1E | 手数, エナジー |
| Stoke | 1 | Skill | Rare | 手札を全て消滅し、同じ枚数のランダムなアイアンクラッド札を手札に加える → 加える札を鍛え済みに | 消滅, その他(生成) |
| Stomp | 3 | Attack | Uncommon | 全体12ダメ(→15)。このターンにプレイした攻撃1枚につきコスト-1 | AoE, 手数 |
| StoneArmor | 1 | Power | Uncommon | 装甲(Plating)4(→6)を得る＝毎ターン終了時に装甲ぶんブロック、毎ラウンド1ずつ減衰 | 防御, ブロック蓄積 |
| StrikeIronclad | 1 | Attack | Basic | 6ダメ(→9)。Strike | その他 |
| SwordBoomerang | 1 | Attack | Common | ランダムな敵に3ダメ×3(→×4) | 多段, 筋力 |
| Tank | 1 | Power | Rare | 【協力専用】味方の被攻撃ダメージ半減、自分は2倍 → 0E | 防御, その他 |
| Taunt | 1 | Skill | Uncommon | ブロック7(→8)＋脆弱1(→2) | 脆弱, 防御 |
| TearAsunder | 2 | Attack | Rare | 5ダメ(→7)×（1＋この戦闘で自分がHPを失った回数）【Epoch7】 | 自傷・被弾, 多段 |
| Thrash | 1 | Attack | Rare | 4ダメ(→6)×2。手札のランダムな攻撃1枚を消滅し、そのダメージ値をこの戦闘中永続加算 | 消滅, 大技(成長) |
| Thunderclap | 1 | Attack | Common | 全体4ダメ(→7)＋全体に脆弱1 | AoE, 脆弱 |
| Tremble | 1 | Skill | Common | 脆弱3(→4)。消滅 | 脆弱, 消滅 |
| TrueGrit | 1 | Skill | Common | ブロック7(→9)。手札のランダム1枚(→選んだ1枚)を消滅 | 消滅, 防御 |
| TwinStrike | 1 | Attack | Common | 5ダメ×2(→7×2)。Strike | 多段, 筋力 |
| Unmovable | 2 | Power | Rare | 毎ターン、カードで得る最初の1回のブロックが2倍 → 1E | ブロック蓄積 |
| Unrelenting | 2 | Attack | Uncommon | 12ダメ(→18)。このターン次にプレイする攻撃1枚のコストが0 | 手数, エナジー |
| Uppercut | 2 | Attack | Uncommon | 13ダメ＋弱体1＋脆弱1(→2/2) | 脆弱, 大技 |
| Vicious | 1 | Power | Uncommon | 脆弱を付与するたび1ドロー(→2) | 脆弱, ドロー |
| Whirlwind | X | Attack | Uncommon | 全体に5ダメ(→8)×X | AoE, 多段 |

補足（機構）: 自動プレイ（Havoc・Cascade・Hellraiser・Stampede・HowlFromBeyond）は `EnergySpent=0` でコストを払わない。Powerは場に出て捨て札に行かない。Ancient レアリティは通常フレームと別のカード枠を持ち、レア度抽選テーブルでは `None` に写像される（＝通常報酬に混ざらない特殊枠）。

---

## ③ アーキタイプ別のカード群（役割: 生成／エンジン／刈り取り／接着剤）

### A. 筋力（Strength）— 8枚（＋刈り取りは多段・全体札が兼務）
| 札 | 役割 |
|---|---|
| Inflame（+2） | 生成 |
| Brand（0E・+1、消滅コスト付き） | 生成 |
| FightMe（+3、敵にも+1） | 生成 |
| SetupStrike（一時+2） | 生成 |
| Dominate（脆弱→筋力へ換金） | 生成（脆弱との橋） |
| DemonForm（毎T+2） | エンジン |
| Rupture（自傷→筋力） | エンジン（自傷との橋） |
| Mangle（敵の筋力-10一時） | 対抗（敵の筋力を剥がす） |
| 刈り取り: TwinStrike・SwordBoomerang・Whirlwind・FiendFire・TearAsunder・Spite・Thrash・Dismantle・FightMe（多段）／Breakthrough・Thunderclap・Stomp・HowlFromBeyond・Conflagration（全体） | 刈り取り |
| Juggling（3枚目の攻撃をコピー）・Aggression（攻撃を毎T回収） | 接着剤 |

所見: **Heavy Blade（筋力×3）と Limit Break（筋力2倍）が消え**、筋力の刈り取り口は「多段×筋力」「全体×筋力」の掛け算だけになった。単発大技に筋力を載せる専用札が無いのは本家1からの構造変更。

### B. 消滅（Exhaust）— 27枚（最大パッケージ）
| 札 | 役割 |
|---|---|
| Havoc（山札の上を撃って消滅）・TrueGrit（ブロック+ランダム消滅）・BurningPact（選んで消滅+2ドロー）・Cinder（18ダメ+ランダム消滅）・Brand（選んで消滅+筋力）・Thrash（攻撃を消滅して吸収）・SecondWind（非攻撃を全消滅→ブロック） | 生成（他札を消滅させる） |
| Tremble・MoltenFist・Dominate・InfernalBlade・ForgottenRitual・Impervious・Offering・Feed・NotYet・FiendFire（自身が消滅） | 生成（燃料になる本体） |
| DrumOfBattle（0E・毎T山札の上1枚を消滅） | エンジン |
| Corruption（Ancient・スキル0コスト+全消滅） | エンジン |
| Stoke（手札全消滅→同数のランダム札） | エンジン（手札の丸ごと入替） |
| AshenStrike（消滅枚数×3ダメ） | 刈り取り |
| PactsEnd（消滅3枚以上で0E全体17） | 刈り取り |
| FiendFire（手札枚数×7） | 刈り取り |
| EvilEye（消滅済みならブロック2倍） | 刈り取り |
| ForgottenRitual（消滅済みならエナジー+3） | 刈り取り |
| HowlFromBeyond（消滅置き場から毎T自動で全体16） | 刈り取り（消滅置き場に置きたい札） |
| FeelNoPain（消滅→ブロック3） | 接着剤 |
| DarkEmbrace（消滅→1ドロー） | 接着剤 |

所見: 本家1の Exhume・Sever Soul・Immolate が消えた代わりに、**「消滅置き場の枚数」参照（AshenStrike・PactsEnd）**と**「このターン消滅したか」の条件札（EvilEye・ForgottenRitual）**が増え、刈り取りが4系統に分化。HowlFromBeyond は「消滅させると復活する」逆張りの刈り取り。

### C. ブロック蓄積（Barricade / Body Slam）— 16枚
| 札 | 役割 |
|---|---|
| Impervious（30）・BloodWall（16）・FlameBarrier（12）・ShrugItOff（8）・Taunt（7）・TrueGrit（7）・IronWave（5）・Armaments（5）・Defend（5）・EvilEye（8×2）・SecondWind・Rage・DemonicShield（協力） | 生成 |
| Barricade（持ち越し） | エンジン |
| StoneArmor（Plating減衰） | エンジン（時限） |
| CrimsonMantle（毎T8、HP-1） | エンジン |
| Unmovable（初回ブロック2倍） | エンジン |
| FeelNoPain（消滅→ブロック） | エンジン（消滅との橋） |
| BodySlam（ブロック×1、鍛えて0E） | 刈り取り |
| Juggernaut（ブロック→5ダメ） | 接着剤 |
| Colossus（脆弱敵から半減） | 接着剤（脆弱との橋） |

所見: Entrench（ブロック2倍）が消え、Unmovable（毎T初回2倍）に置換。Metallicize は減衰する Plating（StoneArmor）へ。刈り取りは BodySlam 1枚のみで本家1と同じ細さ。

### D. 自傷・被弾（Self-Damage / Rupture）— 13枚
| 札 | 役割 |
|---|---|
| Bloodletting（HP-3→+2E）・Offering（HP-6→+2E+3ドロー）・Hemokinesis（HP-2→15）・BloodWall（HP-2→16ブロック）・Breakthrough（HP-1→全体9）・Brand（HP-1→筋力）・DemonicShield（協力） | 生成（自傷の本体） |
| CrimsonMantle（毎T HP-1）・Inferno（毎T HP-1） | 生成（恒常自傷） |
| Rupture（自傷→筋力） | エンジン |
| Inferno（HP損失→全体6） | エンジン兼刈り取り |
| Spite（0E・HP損失済みなら5×2） | 刈り取り |
| TearAsunder（5×被弾回数+1。敵からの被弾も数える） | 刈り取り |
| NotYet（HP10回復）・Feed（最大HP+3） | 接着剤（HP予算の補填） |

所見: 本家1では自傷の刈り取りが Rupture 1枚だったのに対し、**Spite・TearAsunder・Inferno の3枚が追加**され独立したアーキになった。TearAsunder は敵の攻撃も数えるので「殴られること」が資源になる（怒り系の後継）。

### E. 怒り・反応（Rage / 被攻撃返し）— 3枚
| 札 | 役割 |
|---|---|
| Rage（攻撃ごとブロック3） | 接着剤（手数→防御） |
| FlameBarrier（ブロック12＋返し4） | 刈り取り（被攻撃の返し） |
| Juggernaut（ブロック→ダメ） | 接着剤 |

所見: 本家1の Combust・Fire Breathing・Evolve（被弾・ステータス参照）は消え、被弾系は D（TearAsunder・Inferno）に吸収された。

### F. 脆弱（Vulnerable）— 13枚 ★StS2新設
| 札 | 役割 |
|---|---|
| Bash（2）・Tremble（3・消滅）・Thunderclap（全体1）・Taunt（1）・Uppercut（1）・Dominate（1）・Break（5・Ancient） | 生成 |
| MoltenFist（脆弱を2倍） | エンジン |
| Cruelty（脆弱の倍率+25%） | エンジン |
| Bully（0E・4＋脆弱×2） | 刈り取り |
| Dismantle（脆弱なら2ヒット） | 刈り取り |
| Colossus（脆弱敵からの被ダメ半減） | 刈り取り（防御側） |
| Vicious（脆弱付与→1ドロー） | 接着剤 |
| Dominate（脆弱→筋力） | 接着剤（筋力との橋） |

所見: 本家1は Bash・Thunderclap・Uppercut・Shockwave の「付与」だけで参照札ゼロ。StS2では**付与・倍加・参照・換金の4役が揃った完全なアーキ**になり、Epoch2 の解放枚（MoltenFist・Cruelty・Dominate）が中核。

### G. 手数・攻撃連打（Attack Count / Strike）— 18枚 ★StS2で大幅増
| 札 | 役割 |
|---|---|
| Anger（0E・コピー増殖）・InfernalBlade（0E攻撃を生成）・PrimalForce（手札の攻撃→巨岩16）・Aggression（捨て札の攻撃を毎T回収）・Pillage（攻撃を引き続ける） | 生成 |
| Hellraiser（Strikeを引いた瞬間自動プレイ）・Stampede（ターン終了時に攻撃を自動プレイ）・Juggling（3枚目の攻撃をコピー） | エンジン |
| Unrelenting（次の攻撃0E）・OneTwoPunch（次の攻撃2回）・ExpectAFight（手札の攻撃数→エナジー） | エンジン（テンポ） |
| Conflagration（8＋攻撃数×2全体）・Stomp（攻撃数でコスト減の全体12）・PerfectedStrike（Strike数×2）・Rampage（撃つほど+5）・Thrash（吸収成長） | 刈り取り |
| Rage（攻撃→ブロック3） | 接着剤 |
| Strikeタグ6枚（Strike・PommelStrike・TwinStrike・PerfectedStrike・SetupStrike・AshenStrike） | 燃料 |

所見: 本家1では Perfected Strike 1枚だった Strike 参照が、Hellraiser（Strike自動プレイ）で「Strikeを抜かない」デッキを成立させる軸に昇格。

### H. ドロー・エナジー — 13枚（+自動プレイ系）
生成: BattleTrance・BurningPact・Offering・Pillage・PommelStrike・ShrugItOff（ドロー）／Bloodletting・ExpectAFight・ForgottenRitual・Offering（エナジー）
エンジン: Pyre（上限+1）・DrumOfBattle（毎Tドロー相当は無いが0E2ドロー）・DarkEmbrace・Vicious・Aggression（条件ドロー）
テンポ: Cascade（X枚自動）・Havoc（1枚自動）・Corruption（スキル0）・Unrelenting・Hellraiser・Stampede

### I. 全体攻撃（AoE）— 7枚＋Inferno
Breakthrough（1E 9・HP-1）／Thunderclap（1E 4＋脆弱1）／Whirlwind（X 5×X）／Conflagration（1E 8＋攻撃数×2）／PactsEnd（0E 17・消滅3枚条件）／Stomp（3E 12・攻撃数で軽減）／HowlFromBeyond（3E 16・消滅置き場から自動）／Inferno（Power・HP損失ごと全体6）
所見: 本家1の Cleave・Immolate・Reaper・Sever Soul（単体）が消え、**7枚中5枚が別アーキ（手数・消滅・自傷）の刈り取りを兼ねる**設計に。素の全体札は Breakthrough と Whirlwind のみ。

### J. 大技（Big Hit）— 単発高打点
Bludgeon（3E 32→42）／Break（1E 20・Ancient）／Cinder（2E 18）／Hemokinesis（1E 15・HP-2）／Mangle（3E 15＋筋力-10）／Uppercut（2E 13＋弱体脆弱）／Unrelenting（2E 12＋次0E）／Impervious（2E ブロック30）／PrimalForce（巨岩16）
成長型: Rampage（+5/回）・Thrash（吸収）・PerfectedStrike・AshenStrike・Bully・BodySlam

---

## ④ StS1 アイアンクラッド75枚との差分（3基本＋C20/U36/R16）

1. **総数 75 → 87（+12）**。増分はほぼ Rare（16→26）で、Common 20・Uncommon 36 は枚数据え置きのまま中身を入れ替えた（Common 7枚・Uncommon 21枚が交代）。加えて本家1に無い区分＝**Ancient 2枚（Break・Corruption）**と**協力専用 2枚（Tank・DemonicShield）**が出来た。単人の通常プールは 80枚。
2. **継続40枚／削除35枚／新規47枚**。継続組は基本3を含めほぼ本家1と同数値（Strike/Defend/Bash/Anger/TwinStrike/IronWave/Shrug/Pommel/Headbutt/Armaments/Thunderclap/TrueGrit/Havoc/BodySlam/PerfectedStrike/SwordBoomerang/BattleTrance/BurningPact/FeelNoPain/FlameBarrier/Hemokinesis/Inflame/Rage/Rupture/SecondWind/Uppercut/Whirlwind/Barricade/DemonForm/Feed/FiendFire/Impervious/Juggernaut/Offering/DarkEmbrace/InfernalBlade/Bloodletting は同値）。
3. **数値・レア度の変更**: Rampage 8→9（増分+5は同じ、鍛えて+8→+9）／**Bludgeon Rare→Uncommon**（32→42のまま）／Bloodletting Uncommon→Common／**Corruption Rare→Ancient**（通常報酬から外れた）／Bash はそのまま基本。
4. **消えた札（35）**: Clash・Cleave・Clothesline・Flex・Heavy Blade・Warcry・Wild Strike／Blood for Blood・Carnage・Combust・Disarm・Dropkick・Dual Wield・Entrench・Evolve・Fire Breathing・Ghostly Armor・Intimidate・Metallicize・Power Through・Pummel・Reckless Charge・Searing Blow・Seeing Red・Sentinel・Sever Soul・Shockwave・Spot Weakness／Berserk・Brutality・Double Tap・Exhume・Immolate・Limit Break・Reaper。**ステータス札（Wound/Dazed）を撒く/参照する札（Wild Strike・Reckless Charge・Power Through・Evolve・Fire Breathing）が全滅**し、アイアンクラッドからステータス軸が消えた。
5. **後継関係が読める新規札**: Cleave→Breakthrough（8全体→9全体＋HP-1）／Flex→SetupStrike（一時筋力2に7ダメが付いた）／Metallicize→StoneArmor（減衰する装甲）／Carnage→Cinder（Ethereal 20→18＋手札ランダム消滅）／Disarm→Mangle（永続-2→一時-10＋15ダメ・3E）／Double Tap→OneTwoPunch＆Unrelenting／Seeing Red→ForgottenRitual（消滅条件付き+3）／Combust→Inferno（HP損失トリガーの全体6）／Exhume→HowlFromBeyond（消滅置き場から自力で戻る）／Berserk→Pyre（脆弱の対価が消え2E Rare に）／Entrench→Unmovable。
6. **Heavy Blade・Limit Break・Spot Weakness の消失**で、筋力パッケージは「積む」札は残ったが「倍加」と「一撃換金」が無くなり、刈り取りが多段×全体に一本化された（本家2の筋力は"広く浅く"の設計）。
7. **新設アーキ「脆弱」13枚**（付与7・倍加2・参照3・換金1）。本家1のBashは単なる序盤補助だったが、StS2では Bully（0E）・Dismantle・MoltenFist・Cruelty・Vicious・Dominate が「脆弱を積んで殴る」を独立の勝ち筋にした。
8. **自傷の刈り取りが1→4枚**（Rupture のみ → Rupture・Spite・TearAsunder・Inferno）。回復札 NotYet（10回復・消滅）が新設され、自傷デッキのHP予算が自前で賄えるようになった。
9. **Power が14→21**、うち 0E Power（DrumOfBattle）と 1E Power 9枚（Aggression・CrimsonMantle・Cruelty・FeelNoPain・Inferno・Inflame・Juggling・Rupture・StoneArmor・Vicious）が多く、「置く1ターンのテンポ損」を軽くした設計。3E Power は Barricade・DemonForm・Corruption の3枚だけ。
10. **自動プレイ（コスト0で撃つ）の器が増えた**: Havoc に加え Cascade（X枚）・Hellraiser（Strike）・Stampede（ターン終了時）・HowlFromBeyond（消滅置き場から）。エナジー経済を「払わずに撃つ」で迂回する札が5枚。4E以上の札は本家1同様ゼロ。
