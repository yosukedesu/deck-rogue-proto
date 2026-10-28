# Unity 移植キット：240 → 243 雷の主幹・光学肩

**Godot全文は比較元。Unityのrenderer/math/URP shader/Stageを丸ごと置換しないでください。ユーザー側のLinear色処理、HDR gain UV、三角形510切り詰め、owner/clock等の修正を保持し、CreateLightningDischarge内部のgeometry/UV意図だけ移植します。**

## 復元provenance（重要）
このキットのHd2dAttackPrototype.cs全文は、正式Libraryの採用240原本＋実装時tool contextの241/242/243 scriptsから隔離環境で再構成し、当時実際にbuild/native検証した各版の全文SHAと3段階すべて完全一致を確認したものです。243最終SHAは32c3ae0b2821abe53dc0d6f7959ba651c82d5d3d712e38220ae6d28ee29b4869。主要ソースbyte一致回復であり、元227全manifestや元native QAログ一式の復元完了ではありません。

契約3本は元tool生成script＋最終status文字列から再構成した説明資料で、元契約hashは未取得です。最終C#を優先し、241/242失敗試作の説明で243を上書きしないでください。Unity handoffも保持contextから転記し、元測定4920bytesと一致しますが原SHAによる同一証明ではありません。歴史的テストを新環境で再実行したとは主張しません。

## 対象
実leader_izzetが所有してplayしたblue_aether_burst専用既存routeだけ。カード名は霊気放出。非Izzet neutral、installedStatic、他Lightning、MassHeal240は変更なし。実1hit/owner/receipt/time/ゲームbalance保持。Stage/Actor/renderer/sharedshader/palette/raw変更0。新raw0なので再配布画像はありません。

細白芯＋透過青肩へ主幹密度を分配し、肩UVを全幹で連続化、枝は従属、同幹2短区間が減衰します。数値と秒窓はUNITY-HANDOFF.md参照。

## 検証資格
信頼済み履歴：1608assert/6runs＝5clean＋原疎capture1件ObjectDB/2resources notice。後続診断/movie/boundsでは非再現ですが原因未解決、leak-freeとは言いません。静止＋MP4連続decodeで形成/唯一contact/消灯/断片clearを確認。直接等速の主観、実端末性能、Unity撮影は未確認です。

MANIFEST.jsonはこのキットmemberの実SHA、RECONSTRUCTION-VERIFIED.jsonは主要ソース復元検証。Godot import、巨大QA動画、UX/APK、Unity側コードは含みません。
