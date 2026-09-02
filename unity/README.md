# unity/ — Unity 移植のベース（2026-09-03 時点の「下地」。エンジン本体の翻訳はまだ）

一次資料は `docs/unity-port.md`（方式A: 手書きC#移植＋ゴールデンマスター検証）。ここにあるのは、ルールが動いている間も
陳腐化しない**契約と骨格**だけ。

| 置き場 | 中身 | 状態 |
|---|---|---|
| `Packages/com.deckrogue.engine/Runtime/Rng.cs` | mulberry32 の厳密移植（`goldens/rng-golden.json` 124,100値と bit-exact） | 済（P0） |
| `Packages/com.deckrogue.engine/Runtime/Generated/Types.g.cs` | `src/engine/types.ts`・`run.ts`・`map.ts` から生成したデータ型（record）。**手で編集しない** | 自動生成（`npm run gen:csharp`） |
| `EngineTests/` | dotnet で回す等価性検証（今は RNG のみ。次はゴールデンのリプレイ照合） | P0 |
| `../goldens/runs/*.json` | ラン全体のジャーナル（origin+commands）と各手の状態ハッシュ。C# 側が同じ手順で再計算して一致させる契約 | `npm run goldens` / `npm run goldens:verify` |

## 契約

1. **データ**: `src/data/*.json` をそのまま読む（キーは camelCase のまま。Newtonsoft Json.NET）。
2. **状態**: TS の不変オブジェクトは C# の `record` + `with` 式。`Types.g.cs` の record を土台に、移植側で振る舞いを足す。
3. **乱数**: `Rng.cs`。GameState/RunState が `RngState{seed,counter}` を持ち、消費のたびに差し替える（TS と同じ）。
4. **等価性**: `goldens/runs/*.json` の `commands` を `origin` から順に適用し、各手の後に `engine/golden.ts` と同じ要約
   （キー順固定の JSON）を作って FNV-1a 32bit（UTF-8 バイト列）を取る。`hashes[i]` と一致すれば OK。
   分岐した手が「どのシードの何手目か」で特定できる。TS 側のルール変更でゴールデンが変わったら再生成する（= 移植側にも同じ変更が要るサイン）。

## まだやらないこと

エンジン本体（combat/effects/run/map）の翻訳。曲線パッケージ（2026-09-03）で毎日ルールが動いている間は、
翻訳した先から追随コストになる。着手条件は `docs/roadmap.md` のゲートB（ルール凍結）。
