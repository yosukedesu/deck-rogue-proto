# デザインカンバスを `/design` 無しで束ねる

`/design`（Claude Code のデザインカンバス）は Remote Control から呼べないので、同じ器をここに置いた（2026-09-19）。
`payload.template.html`＝カンバスの編集器（Claude Code 2.1.258 の bundled skill から複製）、`seed-canvas.mjs`＝artboard と画像と canvas.json を編集器に埋め込む。

手順（例: `docs/design/dolls-on-stage`）:

```bash
cd docs/design/<name>
python3 build.py                       # *.dc.html と canvas.json を書く
node ../../../scripts/design-canvas/seed-canvas.mjs \
  --template ../../../scripts/design-canvas/payload.template.html \
  --out <name>.html --title "<題>" \
  --artboard Main.dc.html --artboard Compare.dc.html ... \
  --image base-phone.jpg ... --canvas canvas.json
node ../../../scripts/design-canvas/seed-canvas.mjs --check <name>.html
```

できた `<name>.html` を Artifact で公開する（capabilities は `{"self": {}, "downloads": {}}`）。
artboard の確認は playwright-core（`node_modules` に有り）で `.dc.html` を撮る（各セッションの scratchpad の render.mjs 参照）。
