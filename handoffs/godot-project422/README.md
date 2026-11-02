# Godot Project422 → Unity 引き継ぎ

**整理版**: この後継ブランチは公開ベースライン `05c4d208a0ae7968bbbe5073c5b22ab79d02c3e2` から、未使用の1バイトC#スタブ25本のみを除去しています。現在の `project/` は10,628ファイルです。実装・素材・依存は変更していません。元ブランチと元コミットは保持しています。差分一覧は `SUCCESSOR-DELTA.json`、調査・所有権条件・Unity確認の追記は `CLEANUP-AND-CONTRIBUTION-JA.md` を参照してください。

このブランチは、完成済みのGodot VFX成果物をUnityへ移植するための参照スナップショットです。Unity移植そのものは未実施です。既存の `unity/` とゲームルールは変更していません。

## 入口

- `project/project.godot`: Godot .NET 4.6.3 / .NET SDK 9 の制作プロジェクト。
- `Godot-Project422-README-JA.txt`: 最新README v2を原文のまま収録。復元・検証範囲はこちらが優先です。ZIP展開の説明は過去の配布方法で、このブランチではすでに展開済みです。
- `DELIVERED-UNION-MANIFEST.json`: **元Project422ベースライン**の10,653ファイルの相対パス、サイズ、SHA-256。整理版の現行一覧ではありません。現行版の照合では `SUCCESSOR-DELTA.json` の25削除を適用してください。
- `TECHNICAL-EXPORT-CONTRACT-422.json`: 配布時の検証記録。
- `UNITY-HANDOFF-JA.md`: Claudeへの移植依頼と確認事項。

本線はUnityで、Godotの節目ごとに移植する運用です。既存の経緯はリポジトリルートの `docs/dot-godot-port-2026-10-27.md` と `docs/codex-dot-request-2026-10-12.md` §16–17 を参照してください。checkpoint192の既存Unity移植を土台に、422との差分と未接続経路を確認してください。

## ソースの保持

元公開コミットのスナップショットは公開用Full ZIPの内容と完全一致します。整理版では上記25削除のみを適用しています。`project/RECOVERY_PROVENANCE.md` は過去の復元履歴です。そこにある旧ベースラインや事前ビルド済み `.godot/mono` の説明より、最新版README v2の正式インポートとビルド手順を優先してください。README v2と技術記録の検証値・10,653件という数値は元ベースラインの記録です。

元素材が欠ける箇所の必須コンパイル済み依存71件を保持しています。`.godot/imported` を一括削除しないでください。`project/lib/GodotSharp.dll` と `Newtonsoft.Json.dll` はcsprojが参照する配布済み依存です。新たな生成キャッシュやビルド出力は追加していません。Gitにはマニフェストで指定されたファイルを明示的に登録しています。

元のパスには `assets/Stage/` と `assets/stage/` の両方があります。Gitには大小文字を含め正確に保存しています。Windowsの通常のファイルシステムでは同じディレクトリとして表示されるため、パスの厳密比較はGitツリーまたは大小文字を区別するLinux環境で行ってください。

## 元公開時の検証記録（整理版の再実行結果ではありません）

配布前の検証: 制作・修正と有限の動作確認は完了。追加確認423/424（黒NeutralPressure・イゼットAetherBurst）で製品変更はなく、422が最新です。全カード・全条件・全端末の網羅検証ではありません。Android実機・音声は未確認です。詳細は同梱READMEと技術記録を参照してください。

今回の公開検証: Full ZIP 167,631,893 bytes / SHA-256 `4265f3ce355dae34bda3940542a2ee42a29e9e3b266660fbf5d8f61ab73b9997`、README v2 6,233 bytes / SHA-256 `a99af26a5361faf39d9ec518858b1820d20bad0804974a7210d42a4d552a026c` を確認。展開後の全10,653ファイルをマニフェストと照合。公開用に除去済みの管理メッセージIDが戻っていないことと、秘密鍵・一般的なアクセストークンのパターンを検査しました。これは包括的な秘密情報監査ではありません。

今回Godot/Unityの実行・ビルドは再実施していません。既存のGitHub Actionsはmainへのpushのみを自動対象にしており、この引き継ぎブランチのUnity動作保証にはなりません。
