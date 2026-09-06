# エンジン C# 移植の規約（P1・2026-09-06 着手）

一次資料は `docs/unity-port.md`（方式A: 手書き移植＋ゴールデンマスター）。ここは**翻訳作業の規約**。
目標は `goldens/runs/leader_green-*.json` の全手のハッシュ一致（`unity/README.md` の契約4）。

## 置き場と名前

| TS (`src/engine/`) | C# (`unity/Packages/com.deckrogue.engine/Runtime/`) | 静的クラス |
|---|---|---|
| `rng.ts` | `Rng.cs`（済） | `Rng` |
| `types.ts` `run.ts` `map.ts` の型 | `Generated/Types.g.cs`（生成。**手で編集しない**。`npm run gen:csharp`） | namespace `DeckRogue.Engine.Generated` |
| `content.ts` | `Content.cs` | `Content` |
| `events.ts` | `Events.cs` | `Events`（`Emit`） |
| `setany.ts` | `SetAny.cs` | `SetAny` |
| `effects.ts` | `Effects.cs` | `Effects` |
| `combat.ts` | `Combat.cs` | `Combat` |
| `state.ts` | `State.cs` | `State`（`ApplyCommand`） |
| `hooks.ts` | `Hooks.cs` | `Hooks`（`DispatchHooks`） |
| `reactions/*.ts` | `Reactions/SetBase.cs` `Reactions/SetConfirm.cs` `Reactions/SetAuto.cs` `Reactions/HoldManual.cs` `Reactions/ReactionSystems.cs` | `SetBase` `SetConfirmSystem` … `ReactionSystems.Get(mode)` |
| `upgrade.ts` | `Upgrade.cs` | `Upgrade` |
| `fusion.ts` | `Fusion.cs` | `Fusion` |
| `map.ts` | `MapGen.cs` | `MapGen` |
| `run.ts` | `Run.cs` | `Run` |
| `golden.ts` | `Golden.cs` | `Golden` |
| `summary.ts` `traits.ts` `analysis.ts` | 移植しない（UI/計測用） | — |

- namespace は `DeckRogue.Engine`。生成型は `using DeckRogue.Engine.Generated;` で使う。
- **関数名は TS の export 名を PascalCase にしたもの**（`playCard` → `Combat.PlayCard`、`resolveEffectTargeted` → `Effects.ResolveEffectTargeted`、`nextChoices` → `Run.NextChoices`）。
  他モジュールを参照する時もこの規則で名前を決めてよい（並行して翻訳するため、実体がまだ無くても規則どおりの名前で呼ぶ）。
- TS のモジュール内 (非export) 関数は `private static`。定数は `public const` / `public static readonly`。

## 型の対応

| TS | C# |
|---|---|
| `GameState` `RunState` `PlayerState` `EnemyState` `CardInstance` `CardDef` `DeclarativeEffect` … | `Generated` の record。**不変**。更新は `with` 式（`state with { Player = state.Player with { Hp = 10 } }`） |
| `readonly T[]` | `IReadOnlyList<T>`。作る時は `new List<T>{...}` や配列。**状態に入っているリストを変更しない**（常に新しいリストを作る） |
| `[value, rng]` のタプル返し | `(T Value, RngState Rng)` の名前付きタプル |
| `number` | `int`（比率・倍率は `double`。生成型の定義に従う） |
| `string` リテラル union | `string`（定数は `Generated` の `CardTypes.Physical` 等） |
| 判別共用体 `Command` / `GameEvent` / `RunCommand` | 抽象 record + 派生 record `GameEvent_DamageDealt` 等。`switch (cmd) { case Command_PlayCard c: … }`。`Type` プロパティは派生の既定コンストラクタが埋める（`new GameEvent_CardPlayed { CardId = id }` でよい） |
| `undefined` / optional | nullable（`int?` `bool?` `string?`）。`?? 0` の既定は TS と同じ値に |
| `throw new Error(msg)` | `throw new InvalidOperationException(msg)`（メッセージは TS と同じ日本語でよい） |
| `Set<string>` | `HashSet<string>`（状態に入れない一時物）／状態のフィールドなら生成型どおり `IReadOnlyList` |
| `Record<string, number>` | `IReadOnlyDictionary<string,int>`（更新は新しい `Dictionary` を作る） |
| `Math.floor / ceil / max / min` | `Math.Floor / Ceiling / Max / Min`（整数の切り捨ては `(int)Math.Floor(x)`。JS の `Math.floor(a*0.75)` は double で計算してから床） |
| `arr.findIndex` `some` `every` `filter` `map` `slice` `reduce` | LINQ でよい（`ToList()` で確定させる）。**評価順序が RNG 消費や副作用に絡む所は for ループで TS と同じ順に** |
| `Array.from({length:n})` | for ループ |
| `structuredClone` / spread | `with` |

## 乱数・決定性（最重要）

- 乱数は `Rng.Next / NextInt / WeightedIndex / Shuffle` だけ。**TS と同じ順・同じ回数で消費する**。
  `Math.random` `Guid` `DateTime` `Dictionary` の列挙順に依存するロジックは禁止（TS が配列順で回している所は配列順で）。
- `weightedIndex(rng, weights)` の weights は `IReadOnlyList<double>`（TS の number 配列は double で渡す）。
- ID 生成（`uid`）は TS と同じ文字列規則（`t${i}_${id}`、`summon_p${n}_${id}`、`pick_a${act}_r${row}_${id}` 等）。ハッシュ照合は id 列を見る。

## イベントとハッシュ

- `Events.Emit(state, ev)` = `state with { EventLog = 新しいリスト（末尾に ev） }`。
- ゴールデンの要約（`Golden.RunDigest` / `Golden.CombatDigest`）は `src/engine/golden.ts` と**同じキー順・同じ値**の JSON 文字列を作り FNV-1a 32bit（UTF-8）。
  `eventTail` はイベント型名（`GameEvent.Type`）の直近50件。数値は整数のみ。`null` は `null`。
- したがって**イベントの種類と順番**が TS と一致していないとハッシュが合わない。`emit` の位置を勝手に動かさない。

## 翻訳の作法

1. **1行ずつ忠実に。ロジックの「改善」はしない**。TS の分岐・順序・既定値をそのまま。コメントは要旨を残す（日本語のまま可）。
2. TS が `state.player.hand.find(...)` で見つからない時に throw する所は同じ条件で throw。
3. 他色（青・赤・白・黒）専用の効果も**可能なら移植する**が、時間が無ければ `throw new NotImplementedException("effect: xxx")` にして構わない（緑のゴールデンには出ない）。ただし switch の default は TS と同じ挙動（TS が黙って `return state` なら同じ）。
4. `content.ts` の JSON 読込は `Content.Load(dataDir)` を最初に1回呼ぶ（テストは `../../src/data`）。Newtonsoft Json.NET で生成型へ直接読む（キーは camelCase のまま）。
5. データ側の `color` はファイル単位で付与（`content.ts` と同じ）。
6. コンパイルは `cd unity/EngineTests && ~/.dotnet/dotnet build`。照合は `~/.dotnet/dotnet run -- verify ../../goldens/runs/leader_green-9001.json`（Program.cs に実装）。
7. 不一致の調査は `npx tsx scripts/dump-golden-digest.ts goldens/runs/leader_green-9001.json <index> [--events]` で TS 側の要約とイベント列を出して比べる。

## 進め方（並行翻訳）

モジュールごとに別担当で翻訳し、最後に結合してビルド→ゴールデン照合→分岐した手から直す。
担当は互いのファイルを編集しない。他モジュールの関数は上の命名規則で呼ぶ（結合時に名前ずれを直す）。
