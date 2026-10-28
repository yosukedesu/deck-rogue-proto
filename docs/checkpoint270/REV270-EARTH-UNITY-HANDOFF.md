# 270 Earth — 地表主断面と実 contact 同期：最終 Unity 意味移植ハンドオフ

## 最優先：Unity 全文置換禁止 / 本書が途中契約より優先
同梱 Godot 全文は比較元です。既存 Unity renderer / math / shader / Stage / Actor を丸ごと置換しない。ユーザー側の Linear 必須、palette c.linear、HDR gain 別UV、三角形510整列、zero-safe 数学、現在の所有権/APIを残し、地表 factory と exact Earth visual clock の意図だけを移植します。Unity 実機・性能は未確認。
採用候補最終は269 geometry＋270 Stage clock。262/263/264/266/267/268 はHOLDの履歴、269単独も反射安全境界未解決のため最終ではありません。歴史contract内pending/数値を本書・最終270ソースより優先しない。

元265は環境切断時の書込/ビルド結果不明。復元264のSHAを確認後、新266から明示再構成しました。265原本を回復した/その実行が成功したとは言いません。旧環境原本が永久消失したとも断定しません。

## 最終結果と限界
270は **4,653 assertions / 5成功 exit0**。診断615、通常trio874、white栗鼠小窓639、Max5＋lifecycle1720、反射連続movie805。全5runで終了resource notice非再現、既知VSync driver warningは全runにあります。過去act1/card_draw Ogg shutdown issueの修正証明ではありません。
静止・連続frame・最小boundsの独立美術gateはPASS。直接等速の主観視聴、Unity、実スマホ、全敵形状を認定していません。white既存hurtで顔が一時白く覆われる残課題は据置。主地表が低い割れ塊として読め、人工長帯を回避し、補助岩だけの印象を緩和した限定採用です。

## 実ルールと範囲（変更なし）
実 green_earth_roar『大地の唸り』cost2、all6、EnergyMax>=5 のとき追加 all6。初期 Energy3 と cost は別。現 EarthFracture family 登録は同カードのみ。geometry変更はその factory 内のみ。他 family/elemental/Actor/renderer/shader は据置で、Stage は後述270のexact receiving clock guardのみ追加。actual hit/receipt/owner/receiver 間時刻、world light を演出都合で増減しない。

## 最終構図の移植意図
足下rootから術者側地表へ延びる一破断。暗い開口、内部土密度を持つ露出面、地面に残る後端へ落ちる斜め上面の3層を一体で構成する。カメラ向き billboard の土板を大きくする方式ではない。root/end/軸はworld local geometryであり固定画面座標ではない。

- main root=(-.04,-.12)、outer=(-.61,-.80)、width基準.24、8区間。
- Center は u=0,.25,.5,.75,1 に法線方向 0,.045,-.065,.030,0 の不等折れ。
- width profile は u=0,.25,.4,.65,1 に 0,.43,.13,1,0。既存seed微変化と最小.012を保持。
- advance=Ease(age/.09)。mainはu=k/8、v=min((k+1)/8,advance)で固定stationを順に見せる。未到達stationは出さない。末端vの進行は残るが全stationを一様に横へ伸縮しない。secondaryの旧progress式は保持。
- mainのsoil k2を実空隙とし、密度の高い k3..7 主spanは連続。短副を同大の二穴にしない。
- 元主soil独立piece0は新断面へ置換済み。旧piece1/2は維持し、同主bankを二重に重ねない。

### 低い割れ塊の面
`Section(q)=Ease((q-.375)/.125)*(1-Ease((q-.875)/.125))`。
既存 height=.065 と中央追加 `.045*Ease((q-.30)/.20)*(1-Ease((q-.75)/.20))` を既存soilShapeに掛ける。数値上限は local .116（.006基底を含む）。主spanの上端はさらに Section で両端を地面へ閉じる。根や短副を一括増高しない。
上面は前上端 Upper(q) から `Ground(Center(q)+normal*((1+.65*Section(q))*Width(q)))+.002Up` の後端へ繋ぐ。後端式には aperture/soilShape を入れず、固定qで地面上に残す。前開口幅の初期aperture進行・先端revealの移動は別であり、全頂点全期間が不動とは言わない。

### 原本密度と補助石
raw `hd2d-earth-soil-bank-v1.png` は無加工。完成bank輪郭を薄板へ貼るのでなく、主spanの面材質を内部x=250+250q、face y600..700 / top y600..660（原寸1254）から参照する。検査patch x250..500/y600..700 alpha min251・平均252.466。これは原画像加工やリサイズではない。元source-defined解像度を維持する。
主face+topともgain .82、同soilOpacity。上面だけ新しい発光を足さない。
7補助石のうち元最大i0/i3だけlinear size*.72（面積比.5184）。opacity/gain/7個数/初速/重力/出生/下降/消退時刻は不変。石を主役とする再膨張や飾り輪を追加しない。

## Renderer / 容量
既存6batch kinds3/1/3/2/4/14、各 Count512 は固定容量で使用数ではない。lips 最大168、bank 最大144（main faces42 + tops30 + old short banks72）。既存world-ground変換、depth/materialを移植し、全頂点をfiniteに保つ。新shader/styleflag/raw/import追加なし。

## 270 Stage 修正：何を、どの時刻で、どの owner に
反射を挟む実 queue で、次 Earth の予定 ContactAt をclockが越えても、その数値receiptはまだ未commitという境界を発見しました。旧選定261と269双方で再現した既存時刻問題です。ledgerReady=trueはcommit済みではありません。

追加 `IsExactEarthReceivingRoute(AttackRoute?)` は fulltuple：
`card:green_earth_roar / green_earth_roar / card / earth_fissure / EarthStompFx / earth / FixedSmall=false`。
さらに `IsOwnedEarthReceivingContact(OrdinaryBeat)` は既存 direct、非EnemyPhase、非Rejected、player正HpLoss、実card presentationUID、eventOwner==Action.Owner、実PlayCardUID、Previous.Handの定義一致だけを認めます。現在Handで再判定せず、新owner/仮hitを作りません。**Direct=false cascade Earth は対象外**で、全Earth文脈の一般安全解消とは言いません。

`AdvanceOrdinaryEffects` 内だけ：
1. 初期 retirementTime=元elapsed。
2. 実b.Contacted、同ledger generation、receipt.Committed && Exact がまだ揃わなければ、effectTime=min(元時刻, scenario.Windup−0.0001秒)。VisualAgeは負に留まり、既存予兆だけが許可されます。
3. 揃った後は `contactAge=max(0,clock-b.LastContact)`、effectTime=Windup+contactAge。実commit直後はage0、予定遅延分を一気に追い越しません。
4. 同じ正常postcommitの場合のみretirementTime=Windup+contactAgeとし、遅延分だけ見せる末尾を切り詰めません。数値queue順/receipt/予定ContactAtは変更せず、視覚終了の待ちだけが実contact基準です。
5. 接触済みだがreceipt不正な経路はretirementTimeを負に固定せず元elapsedを維持するため永久activeになりません。既存Rejectedは既存退場処理のままです。

`UsesOrderedSharedLaunchMaterial` / `CanPreviewIndependentScatterOwner` を一切変更していません。そこへEarthを追加すると予約preview許可まで広がるため、別guardにしました。他routeは元effectTimeとretirementTime=elapsedで同値。Actor、geometry、shader、raw、hero音/接触再発火は変更しません。

実private predicateに対するnull＋7tuple near-miss、Direct=false/rejected/enemyphase/differentowner referenceは、独立MemberwiseCloneによるunit否例です。実natural gameplay境界と混同せず、live field/dictionaryは触っていません。

## 実証の詳細
- 通常trio：実enc_probe_trio、HP>=200観測fixture、shipped intent/block/innate保持。初期3、実cost2、一波6×3。270は静止＋全oracleで269と一致、270通常trioの新movieは撮っていません。269通常movieはgeometryの歴史証拠として別扱い。
- 2波：実trioに明示初期Energy/EnergyMax/EnergyMaxAtTurnStart=5を与えた境界fixture。自然なcard列でMax5を獲得した証明ではありません。実1cardUIDの6hit、各実正eventがnewgeometrypositiveへ到達することを参照identityで確認。
- 小体：leader_white / Act2 / 960×540 native window / 実enc_squirrel_probe。旧223のenemyID差替ではなく自然member/start保持。HP>=200は生存観測fixture。実phoneではありません。
- lifecycle：既存before-contact/自然完走/実接触後cancel/actorcomponent-dead＋cancel。Engineによる致死証明とは別。
- 未commitframe16（疎diagnostic）はProbeHP200、age−.0001、fissure予兆6のみ。bank/lip/rocks/dust0ですが全mesh0とは言いません。frame17実commit age0→18+.0333。
- 反射movie連続ではidx15未commit/HP200/age−.0001で主crest無し→idx16commit/HP194/age0→17+.0333自然成立。先に接触した栗鼠演出は継続、receipt14/16各一度、37+全effectclear。geometry/worldlightを弱めて不整合を隠していません。
- 269通常movieのage>=.10後端39receiver/vertex組はWorldPoint移動最大0。初期tip・alpha0全頂点不動の認定ではありません。

## 失敗/HOLDの保全
- 262〜268の静止HOLDはすべて保全し、技術PASSを美術PASSに置換しません。
- 269最初のmovieはQA生成器がffmpeg `-n` まで変数置換して `-movieFrame` とし、17checks後encoder終了/Broken pipeでexit1、完成MP4無し。Bはその引数を訂正、geometry無変更で996PASS。BにはOgg2終了noticeがあり別issueとして保持。
- 269white栗鼠原失敗152checks/exit1、追加診断150checks/exit1は正agegeometryが未commitで出た製品境界の確定失敗。旧261 A/Bも149PASS+1FAIL/exit1（計150）で再現。assertは緩めていません。
- bounds観測追加のengine receipt型/runtime receipt型混同compile失敗、source guardのQA hook分類誤assert等も別保全。新270成功総計へ加算しません。

## 成果物/ソースの資格
現logical source257、実復元full8781/不可替import62のSHA保護PASS。269main geometrySHA b15ef92bb6ef68fe924087bba5754f04044773cf6de3930e87c186117576728f、270StageSHA 11adbe1a2a88ddd136fd15f37d1996a2bffa7011c2d972311c336e8c6b7cdfee。
実270反射MP4は365,594B、SHA81f1568375821a9995b0a00663aa78d6cc958059cac0881676381f3c4d7d28db。無音70連続native frame、30fps simulation=2.333秒、wall17.60秒は実性能動画ではありません。
移植kitは261→270製品差分全文（主factory/Stage/履歴8contract）＋この最終資格＋patch/hash/原本raw参照のみ。新raw0、Godot import/巨大QA群/UX/APKを入れません。現在の公開GitHEAD・ユーザーUnity未push作業とこのkitのsource基準を区別してください。

独立静的最終監査：追加2methodとcap/rebase blockを除去しretirementTime比較をelapsedへ戻すと269 Stage全文SHA一致。他route/旧preview/queue/commitのbyte不変を確認（proofs/audit270-final-source-byte-review.json）。

## 二波画像の最終追加資格（新native追加なし）
既存Max5画像18/31を同age比較し主土塊/岩主従を維持、第1残留と重なって高い二重土板にならないことを独立静止PASS。39沈降、47では土/岩が目立たないがroute age .633/.600/.567のactive残留があるため全FXclearとは言わない。二波の等速テンポや追加pulse不在を連続映像で認定したものではなく、実1UID6hit/最終cleanupは技術証拠。既存6/intent数値の重なりは別UI課題として残る。
