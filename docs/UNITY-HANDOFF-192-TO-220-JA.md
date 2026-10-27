# Unity移植用 checkpoint 192→219 読取監査

## 結論
- remote192（8834bd18）→selected219：**37ファイル（C#8＋契約/統合docs29）**。197凍結ファイルのSHAを全検証。削除なし。素材PNG/WAV、shader、csproj、BattleActor、Godot UX/APKの差分なし。
- 次commit候補は `unity-handoff192-to219-source-proof.json` の `changedFiles` 全37件。**liveや220からコピーしない**。192以前にある8系統の基盤もUnityへの参照対象なので、差分commitだけを「移植一式」としない。
- Unity本線のコード/取り込み状態は未調査。以下はGodot側の正本・意味契約であり「Unity移植済」の主張ではない。

## 8系統の移植境界
以下のProfile/Factoryは `RecoveredSource/DeckRogue.GodotPort.Presentation/`、Stageは `scripts/presentation/BattleStage.cs`。

|系統|正本・イベント/owner|Unityへ渡す演出/数値の要点|
|---|---|---|
|守り|`SupportVfxProfiles` / BlockGained, IceBlockGained。普通6文脈、Ice4。普通は実PlayCard UID・Previous.Hand・first CardPlayed・mode0等＋SourceUid null＋ExactGain。IceはUID無しのためowner-action first gain限定|`CreateOrdinaryGuard`, `CreateIceGuard`。普通scenario Small: Windup0/Contact.016/Sustain.174/Release.31/Branches1/Radius.6/LightEnergy.20。Iceは0/.016/.144/.36/1/.6/.45。普通とIceを同一色替え扱いしない|
|回復|`SupportVfxProfiles` / HpHealed。PrayerVerse/MassHealのみ。最初のeligible owner-action heal、**0回復でも枠を消費**し後続passiveへ付け替えない|`CreatePrayerHeal`。scenario Small0/.016/.244/.40/1/.6/.35。実回復量のみ、直接/盗んだHPという因果を推定しない|
|延焼/燃焼|`StatusContactVfxProfiles` / **BurnTick** system:burn。DamageDealtやBurnAppliedで代用不可|`CreateBurnStatus`：compact local thermal、fire-volume02/04。TargetPeak.035、FloorPeak.015（FloorLight0）。Burn付与・スタック減衰・死・ターン順はengineのまま。用語「延焼」が別カード伝播を指す場合、このBurnTick契約だけでは網羅しない|
|置物|`PermanentContactVfxProfiles` / DamageDealt(PresentationSource permanent)。18定義、Previous/Current installed UID、nonretainer/noLife/noEcho、**SourceUid==PresentationUID**。Sparksのみpermanent:キー、それ以外card:でもsourceKindはpermanent|Neutral9/Fire3/Dark2/Plant3/Lightning1。`CreatePermanentPulse`, InstalledFire/Dark/Plant/LightningContact。局所材質だけでcaster/weaponを借用しない。親カードの正当な動作を保持し、証明済みpassive fallbackだけ抑制。Ring後Leaves/Canopyはbase2+Growth2=4、Fence+BarkArmorはbase8+Growth1=9がclean nominal、実modifier/receiptが正|
|死者|`NecroContactVfxProfiles` / real Censer deck-exhaust。CorpseKiss base3、Skull base4。Previous.Draw→Current.Exhaustの実card UID＋ordered mill/NecroFired＋Censer ambient owner|`CreateNecroDark`→Dark(localOnly=true)、TargetPeak.028/Floor0。**SourceUidはCenser、PresentationUIDは死者カードで異なる**。置物のUID一致条件をコピーしない。直接Skull13 neutralと自動Skull4 Darkを分離。独立heal/blocked/capped0を改変しない|
|返し|`ReactionContactVfxProfiles` / 実ConfirmFire UID、Previous armed usable Set、postattack attacker、先行ReactionTriggered exact CardId、Current Setから除去、実receipt。Curse/UnderworldHand/Grudge/LastStandの4定義|`CreateReactionDark` localOnly。**実ユーザーConfirmReactionのcounter/cast gestureは正当**、死者/置物の無姿勢規則を流用しない。Grudgeはaction value、LastStandは確認時HP条件。heal後に条件を再判定しない|
|敵の突き|`EnemyContactVfxProfiles` / EnemyActionExecuting＋PresentationEnemyActionのactor/move/ExecutionEventIndex/branch一致。aggregate-final-mutation、LoopBound/ExecutedHits/contributions=1、EffectiveActual==Amount、player receipt|`CreateEnemyPoke`。exact7組のみ（Probe poke/lunge, Beetle horn_jab, BondWolf lunge, GoldRaven peck, HazeStag antler_jab/antler_charge）。元の敵動作/接触点を保持。219全敵43は他にBlunt15/Neutral21。multihitのaggregateと投影contactを混同しない|
|雷の門（名称要確認）|既存Izzet契約なら `LightningContactVfxProfiles`：leader_izzet＋blue_aether_burstの実PlayCard UID/Previous.Hand＋resolved lightning tuple。他leader同じkeyは未許可。219追加Staticは上記installed契約で別枠|`CreateLightningDischarge` と `CreateInstalledLightningContact` は別。既存surface-r2 PNGとlightning paletteを再利用。Izzet TargetPeak.35/Floor.12、Static.035/Floor0。AetherDischarged単独からcardUIDを推定しない。「雷の門」という別ID/イベントとの同一性は未確認|

## 数値・Stage/Actorの対応で落とせない点
- `EffectScenario`引数順は **Tier, Windup, Contact, Sustain, Release, Branches, Radius, LightEnergy**。Durationは4時間の和（普通Guard.50秒、回復.66秒、Ice.52秒）。
- `CombatPresentationLedger`（Game側既存基盤）、`CombatOutcome`、`DamageVfxIntensity`、`PresentationTimeline`、`SourceHeroBlockFloatModel`も192以前の依存。エフェクトから戦闘値を再計算せず、実receipt Before/AfterのHP/Block/IceBlockを正とする。
- 普通Block floatの既存参考値：Lifetime.7秒/Rise40px/Font30/color6f95b8。Godotの座標/フォント単位をUnity world unitへ直コピーしない。
- Stage `AdvanceOrdinaryEffects` は証明済み限定sourceのcontact前を Windup−.0001へ制限。**Contactedだけでcommit成功と見なさず**、実Committed/Exact/Reasonを検証。対象・イベント順・damage数は不変。
- `AuthoredGeometryFx` は camera-local `(source-target)/(height*size)` でConfigureSourceへ入力。Actor.Contact/Impact/hero cast sourceを別々に保持する。Godot light換算は target=`light*1.6*(TargetPeak/.35)`、floor=`floorLight*.48*(FloorPeak/.12)`（quality条件付き）；Unityでは光源単位を再校正する。TargetPeak自体をUnityの物理光量と断定しない。
- `BattleActor.cs` は192→219で不変。新しい敵アニメ/rigを作った差分ではなく、実動作を保った受け側材質・owner/timing refinement。
- Kind付きroute identityを保持。card:keyだけでcache/profileを引くとSkull direct/necro、Izzet、Sparks等が衝突する。

## 素材引継ぎ
新規素材差分は0。既存192の `assets/Art/fx/`、`assets/presentation-data/hd2d-*.json`、`shaders/authored_Hd2dPrototype.gdshader` / `shaders/recovered_forest_surface.gdshader`、既存Audioを必要素材としてUnity側で照合する。守り/回復/局所Dark/植物はprocedural layer/role/paletteの組合せが重要で、PNGコピーだけでは再現しない。NativeGeometryBridge→各Create factory→Layer/UV/role/alpha/gainの三段を移す。実shaderファイル名はproofの197manifestから確認し、推定パスで追加しない。

## commit本文に入れる値・限定
- selected219 / master Library v89 / 197 frozen hashes verified。
- direct card114（192比+11）＋support12文脈−重複Entangle1＝125 unique IDs。
- bounded noncard67＝enemy43（pierce7/blunt15/neutral21）＋installed18＋confirmed reaction4＋Censer necro2。Izzet contextual overrideとBurnTickは別。
- remaining catalog：card94、noncard168。これは未完成攻撃数や品質達成率ではない。Heart/retainer/他source・220は未採用。
- 219 fresh2932PASS/4clean、2183077PASS/6clean、2172052PASS/5clean、2164808PASS/1clean等は各checkpointの範囲。集計を一つの全件test総数にしない。
- 197/210等のtrial/失敗契約も履歴として含まれる。219/218契約のstatus文字列がtrial/pendingでも、最終採用はversioned terminal checkpointで判断。次commitの統合メモにその対応を付ける。

## 含めないもの・未確認
- 220の未終端8技、live作業中コード、QA実行物/動画/PNGスクショ、cache/.godot、bin/obj、recovery ZIP、APK/export/Android設定、UXの追加は今回不要。
- Godot runtime8ファイルは製品checkpointには必要だが、Unity csprojへそのまま貼るものではない。Godot API部分をUnity適合コードへ写像するのは別工程。
- clean-original-reimportでは既知GLB原本欠落/既存cache依存がある。192の公開時制限を解消したとは言わない。今回新素材0でそれを悪化させる追加cacheも不要。
- Unity側の実イベント型/StageActor実装/現在の移植済み資産/実機結果は未確認。ユーザー未受領分類との意味対応は上表の仮対応、特に「雷の門」「延焼」の語義は要照合。


## selected220 採用追補（公開checkpointの最終判定）
219の全採用範囲を保持し、敵neutral経路を8追加。whip(Sergeant)、chain(Slaver)、tongue(Toad)、wing(Owl)、great_swing(AxeOgre)、bell_strike/gate_guard(Warden)、lash(Serpent)。実7シーケンス8(key,index)、5815 PASS/1 clean exit0、AxeOgre/Owl/Toad目視PASS。敵経路51 = pierce7 + blunt15 + neutral29、bounded noncard75 = enemy51 + installed18 + reaction4 + Censer necro2。card114/support12重複1=125 unique IDsは不変。未受領分類を完了率へ読み替えない。
上記219監査は履歴。公開220は192比38製品パス（C#8＋docs30）と本引継ぎ文書/manifest/cleanQA3点。Godot import設定不要という指示でUID追加のみ5png.importを意図的除外、198凍結中193製品subset一致。raw PNG/WAV原本は変更・縮小・削除なし。BattleActor/shader/UX/APK変更なし。Unity本線の移植済み主張ではなく、Godot VFX/素材の意味契約を渡す。
Support/回復/IceGuard/Burn/Izzetは192以前からの参照基盤として上表を保持。実receiptとkind付きroute、Stage contact前cap、Actor source/impact/cast区別、時間/光換算をUnityへ移す際に落とさない。219/220契約JSONのpending/trialは凍結時履歴であり、最終採用は同梱REV220-CHECKPOINT-QA/PER-KEY/VISUAL-GATESで判定。既知GLB原本欠落/cache依存、実機FPS/主観聴音未確認は未解消。
