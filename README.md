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

## スマホ実機から触る

アプリは完全にクライアントサイドなので、スマホのブラウザでそのまま動く。

```bash
npm run dev:lan   # 0.0.0.0 で待ち受け
```

WSL2 は既定で NAT 内にいるため、スマホ→Windows→WSL の中継がもう一段必要。どちらか：

1. **mirrored ネットワーク（Windows 11 22H2+ 推奨）**: `%UserProfile%\.wslconfig` に
   `[wsl2]` `networkingMode=mirrored` を書いて `wsl --shutdown` → 以後はスマホから
   `http://<WindowsのIP>:5173` で直接届く。
2. **ポートプロキシ**: 管理者 PowerShell で
   `netsh interface portproxy add v4tov4 listenport=5173 listenaddress=0.0.0.0 connectport=5173 connectaddress=$(wsl hostname -I)`
   ＋ ファイアウォールで 5173 を許可。

または `npm run build` の `dist/` を Netlify / GitHub Pages 等に置けば URL を開くだけ（静的サイトとして完結）。

