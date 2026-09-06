# unity/ — Unity 移植（2026-09-06 P1: エンジン本体の翻訳済み・ゴールデン8本1,105手が一致）

一次資料は `docs/unity-port.md`（方式A: 手書きC#移植＋ゴールデンマスター検証）。ここにあるのは、ルールが動いている間も
陳腐化しない**契約と骨格**だけ。

| 置き場 | 中身 | 状態 |
|---|---|---|
| `Packages/com.deckrogue.engine/Runtime/Rng.cs` | mulberry32 の厳密移植（`goldens/rng-golden.json` 124,100値と bit-exact） | 済（P0） |
| `Packages/com.deckrogue.engine/Runtime/Generated/Types.g.cs` | `src/engine/types.ts`・`run.ts`・`map.ts` から生成したデータ型（record）。**手で編集しない** | 自動生成（`npm run gen:csharp`） |
| `Packages/com.deckrogue.engine/Runtime/*.cs` | `src/engine/` の C# 翻訳（Content/Events/SetAny/Effects/Combat/State/Hooks/Reactions/Upgrade/Fusion/MapGen/Run/Golden/JsonUnions。約8,800行）。規約は `PORTING.md` | 済（P1・2026-09-06） |
| `EngineTests/` | dotnet で回す等価性検証: RNG ゴールデン照合と **ラン全体のゴールデン照合**（`dotnet run -- verify ../../goldens/runs/*.json --data ../../src/data`） | 済（8本1,105手一致） |
| `../goldens/runs/*.json` | ラン全体のジャーナル（origin+commands）と各手の状態ハッシュ。C# 側が同じ手順で再計算して一致させる契約 | `npm run goldens` / `npm run goldens:verify` |

## 契約

1. **データ**: `src/data/*.json` をそのまま読む（キーは camelCase のまま。Newtonsoft Json.NET）。
2. **状態**: TS の不変オブジェクトは C# の `record` + `with` 式。`Types.g.cs` の record を土台に、移植側で振る舞いを足す。
3. **乱数**: `Rng.cs`。GameState/RunState が `RngState{seed,counter}` を持ち、消費のたびに差し替える（TS と同じ）。
4. **等価性**: `goldens/runs/*.json` の `commands` を `origin` から順に適用し、各手の後に `engine/golden.ts` と同じ要約
   （キー順固定の JSON）を作って FNV-1a 32bit（UTF-8 バイト列）を取る。`hashes[i]` と一致すれば OK。
   分岐した手が「どのシードの何手目か」で特定できる。TS 側のルール変更でゴールデンが変わったら再生成する（= 移植側にも同じ変更が要るサイン）。

## 運用（P1 以降）

- ルール変更のフロー: TS で変更 → `npm run goldens`（8本再生成）→ C# へ追随 → `dotnet run -- verify` で全一致（`docs/unity-port.md` §3）。
- `summary.ts` / `traits.ts` / `analysis.ts`（UI・計測用）と sim ボットは翻訳しない。NUnit へのテスト翻訳は未着手（ゴールデンが当面の安全網）。
- まだやらないこと: Unity プロジェクト本体（P2 骨格・P3 戦闘UI）。エンジンは `noEngineReferences` の純 C# として使える状態。

## Unity で開く（P2 骨格・2026-09-06）

`unity/` がそのまま Unity プロジェクトのルート（`Assets/` `Packages/` `ProjectSettings/`）。エンジンは `Packages/com.deckrogue.engine`（埋め込みパッケージ＝manifest への記載不要）、
JSON は `Assets/StreamingAssets/data/`（`npm run unity:sync` で `src/data` から複製。`npm run unity:sync check` で同期確認）。

1. Windows 側にリポジトリを clone する（`\\wsl$\...` の UNC パスは Unity が扱えないので、WSL の作業ツリーを直接開かない）。
2. Unity Hub → Add → `unity/` を選ぶ。`ProjectSettings/ProjectVersion.txt` は Unity 6（6000.0 系）を指している。手元のバージョンで開いてよい（アップグレード確認は許可）。
3. 初回はパッケージ解決（Newtonsoft Json 3.2.1・uGUI・Input System）を待つ。Input System の有効化を聞かれたら「Yes」（`Assets/Game` は `ENABLE_INPUT_SYSTEM` の有無で入力モジュールを切り替える）。
4. 空のシーンのまま **Play**。`Assets/Game/GameRoot.cs` が `RuntimeInitializeOnLoadMethod` で画面を組み立てる（シーン・プレハブ不要）。セットアップ→ラン開始→マップ→戦闘…がブラウザ版と同じエンジンで動く。
5. うまく行かない時: Console のエラーをそのまま貼って報告（Assets/Game はこのマシンでコンパイルできないので、最初のコンパイルは Unity 側）。

## WSL からのバッチ実行（2026-09-07・`scripts/unity-win.sh`）

Windows 側の Unity Editor（Hub が入れた `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe`）を WSL から直接叩く。
正本はこのリポジトリの `unity/`。`\\wsl$` の UNC パスを Unity が扱えないので、`C:\Users\yosuke\deck-rogue-unity` へ rsync した
使い捨ての作業コピー（`Library/` はそこに溜まる）で動かす。

```bash
scripts/unity-win.sh compile   # 同期 → -batchmode -nographics -quit → error CS... を要約
scripts/unity-win.sh verify    # 同期 → Assets/Editor/BatchTools.VerifyGoldens (Unity 内でゴールデン8本を照合)
scripts/unity-win.sh play      # 同期 → PlaySmoke (プレイモードでセットアップ→ラン開始→進路→戦闘3ターンを GameRoot の API で回し、例外・エラーログ・空画面を検出)
scripts/unity-win.sh sync      # 同期だけ (Hub で作業コピーを開いて手で触る時)
```

ログは `C:\Users\yosuke\deck-rogue-unity\unity-batch.log`。初回はパッケージ解決で数分かかる。作業コピー側で Unity が書き換えた
`ProjectSettings/ProjectVersion.txt` はスクリプトが正本へ戻す。Hub の GUI で開く時も同じ作業コピーを Add する（手で直した
`Assets/` の変更はリポジトリへ手動で戻すこと＝作業コピーは常に上書きされる）。

Android はまだ（StreamingAssets を `File` で読んでいる＝Editor/デスクトップ向け。実機は UnityWebRequest 読込に切り替える）。

