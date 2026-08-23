# deck-rogue-proto

デッキ構築ローグライクの**ルール検証用プロトタイプ**。
リアクション方式3案 (set-auto / hold-manual / set-confirm) を同一条件で比較し、採用方式を決めるためのもの。

詳細な仕様・アーキテクチャ原則・運用ルールは [CLAUDE.md](./CLAUDE.md) を参照。

```bash
npm install
npm run dev    # ブラウザで手動プレイ
npm test       # ルールエンジンのテスト
npm run sim    # ヘッドレス自動対戦
```
