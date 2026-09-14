// Program.cs — エンジン等価性の照合ハーネス (Unity移植)
//   1) RNGゴールデンマスター照合 (P0スパイク)
//      dotnet run -- [goldens/rng-golden.json]
//   2) ランのゴールデン照合 (P1: origin+commands を再生し各手の状態ハッシュを突き合わせる)
//      dotnet run -- verify ../../goldens/runs/leader_green-9001.json [...] [--data ../../src/data]
// 実行時のカレントは dotnet run のプロジェクトディレクトリ (unity/EngineTests) を想定した相対パス。

using System.Text.Json;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length > 0 && args[0] == "verify")
{
    return VerifyGoldenRuns(args.Skip(1).ToArray());
}
if (args.Length > 0 && args[0] == "dump-save")
{
    return DumpSave(args.Skip(1).ToArray());
}

var path = args.Length > 0 ? args[0] : "../../goldens/rng-golden.json";
if (!File.Exists(path))
{
    Console.Error.WriteLine($"ゴールデンファイルが見つからない: {path}");
    return 2;
}

using var doc = JsonDocument.Parse(File.ReadAllText(path));
var root = doc.RootElement;
int checkedCount = 0;
int failCount = 0;

void Fail(string what)
{
    failCount++;
    if (failCount <= 5) Console.Error.WriteLine($"不一致: {what}");
}

// ---- next: value*2^32 (内部uint32) の系列 ----
foreach (var seedProp in root.GetProperty("next").EnumerateObject())
{
    var seed = long.Parse(seedProp.Name);
    var rng = Rng.Create(seed);
    int i = 0;
    foreach (var expected in seedProp.Value.EnumerateArray())
    {
        var (v, next) = Rng.Next(rng);
        rng = next;
        var actual = (long)(v * 4294967296.0);
        if (actual != expected.GetInt64()) Fail($"next seed={seed} index={i}: 期待{expected.GetInt64()} 実際{actual}");
        i++;
        checkedCount++;
    }
}

// ---- nextInt: 5種のレンジを巡回 ----
{
    var ranges = new (int Min, int Max)[] { (1, 6), (0, 99), (5, 7), (0, 1), (3, 17) };
    var rng = Rng.Create(1001);
    int i = 0;
    foreach (var expected in root.GetProperty("nextInt").EnumerateArray())
    {
        var (min, max) = ranges[i % ranges.Length];
        var (v, next) = Rng.NextInt(rng, min, max);
        rng = next;
        if (v != expected.GetInt32()) Fail($"nextInt index={i}: 期待{expected.GetInt32()} 実際{v}");
        i++;
        checkedCount++;
    }
}

// ---- weightedIndex: 5種の重み表を巡回 ----
{
    var tables = new double[][] { new double[] { 1 }, new double[] { 1, 1 }, new double[] { 3, 1 }, new double[] { 1, 2, 1 }, new double[] { 5, 1, 1, 1 } };
    var rng = Rng.Create(2002);
    int i = 0;
    foreach (var expected in root.GetProperty("weightedIndex").EnumerateArray())
    {
        var (v, next) = Rng.WeightedIndex(rng, tables[i % tables.Length]);
        rng = next;
        if (v != expected.GetInt32()) Fail($"weightedIndex index={i}: 期待{expected.GetInt32()} 実際{v}");
        i++;
        checkedCount++;
    }
}

// ---- shuffle: サイズ1〜40 × 5回の順列 ----
{
    var rng = Rng.Create(3003);
    int idx = 0;
    foreach (var expectedPerm in root.GetProperty("shuffle").EnumerateArray())
    {
        var size = expectedPerm.GetArrayLength();
        var items = Enumerable.Range(0, size).ToArray();
        var (shuffled, next) = Rng.Shuffle(rng, items);
        rng = next;
        int j = 0;
        foreach (var expected in expectedPerm.EnumerateArray())
        {
            if (shuffled[j] != expected.GetInt32()) Fail($"shuffle perm={idx} pos={j}");
            j++;
            checkedCount++;
        }
        idx++;
    }
}

if (failCount == 0)
{
    Console.WriteLine($"RNGゴールデンマスター照合: 全{checkedCount:N0}値が一致 ✅ (mulberry32のC#移植はbit-exact)");
    return 0;
}
Console.Error.WriteLine($"照合失敗: {failCount}件の不一致 / {checkedCount:N0}値");
return 1;

// ================= ランのゴールデン照合 =================

// goldens/runs/*.json の origin からランを起こし、commands を順に適用して各手の RunHash を突き合わせる。
// 不一致・例外が出たファイルはそこで打ち切り、次のファイルへ進む (どのシードの何手目で分岐したかを特定する道具)。
int VerifyGoldenRuns(string[] argv)
{
    var dataDir = "../../src/data";
    var files = new List<string>();
    for (int i = 0; i < argv.Length; i++)
    {
        if (argv[i] == "--data" && i + 1 < argv.Length) { dataDir = argv[++i]; continue; }
        files.Add(argv[i]);
    }
    if (files.Count == 0)
    {
        Console.Error.WriteLine("usage: dotnet run -- verify <golden.json...> [--data <src/data>]");
        return 2;
    }
    if (!Directory.Exists(dataDir))
    {
        Console.Error.WriteLine($"データディレクトリが見つからない: {dataDir}");
        return 2;
    }

    Content.Load(dataDir);
    Console.WriteLine(
        $"データ読込: カード{Content.AllCards.Count} 敵{Content.AllEnemies.Count} 編成{Content.AllEncounters.Count} " +
        $"デッキ{Content.AllDecks.Count} リーダー{Content.AllLeaders.Count} レリック{Content.AllRelics.Count} " +
        $"イベント{Content.AllEvents.Count} 合成レシピ{Content.AllFusions.Count}");

    int failedFiles = 0;
    foreach (var file in files)
    {
        var name = Path.GetFileName(file);
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"{name}: ファイルが見つからない ({file})");
            failedFiles++;
            continue;
        }

        var golden = JObject.Parse(File.ReadAllText(file));
        var origin = (JObject)golden["origin"];
        var commands = (JArray)golden["commands"];
        var hashes = (JArray)golden["hashes"];
        if (origin == null || commands == null || hashes == null)
        {
            Console.Error.WriteLine($"{name}: origin/commands/hashes が無い");
            failedFiles++;
            continue;
        }

        // origin は run (幕1から) と checkpoint (幕2/3のデバッグ開始。2026-09-14 幕2/3の照合) の両方 = TS の replayInitialRun と同じ入口
        RunState run;
        try
        {
            run = Run.ReplayInitialRun(JsonUnions.FromToken<ReplayOrigin>(origin));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{name}: ラン生成で例外: {e.Message}");
            failedFiles++;
            continue;
        }

        var count = Math.Min(commands.Count, hashes.Count);
        var ok = true;
        for (int i = 0; i < count; i++)
        {
            var raw = commands[i].ToString(Formatting.None);
            RunCommand cmd;
            try
            {
                cmd = JsonUnions.FromToken<RunCommand>(commands[i]);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{name}: index={i} ({i + 1}手目) のコマンドを読めない: {e.Message} / {raw}");
                ok = false;
                break;
            }

            try
            {
                run = Run.ApplyRunCommand(run, cmd);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{name}: index={i} ({i + 1}手目) で例外: {e.Message} / 手={raw}");
                ok = false;
                break;
            }

            var actual = Golden.RunHash(run);
            var expected = (string)hashes[i];
            if (actual != expected)
            {
                Console.Error.WriteLine($"{name}: index={i} ({i + 1}手目) で不一致 期待{expected} 実際{actual} / 手={raw}");
                Console.Error.WriteLine($"  → TS側の要約: npx tsx scripts/dump-golden-digest.ts {file} {i}");
                Console.Error.WriteLine($"  → C#側の要約: {Golden.RunDigest(run)}");
                ok = false;
                break;
            }
        }

        if (ok) Console.WriteLine($"{name}: 全{count}手が一致 ✅");
        else failedFiles++;
    }

    if (failedFiles == 0)
    {
        Console.WriteLine($"ランのゴールデン照合: {files.Count}ファイルすべて一致 ✅");
        return 0;
    }
    Console.Error.WriteLine($"照合失敗: {failedFiles}/{files.Count} ファイル");
    return 1;
}

// ================= レポート/セーブの往復検証 (2026-09-14 Unity のフィードバック記録) =================

// ゴールデンの origin+commands を n 手だけ再生しながら、Unity 版と同じ手順で BattleArchive・選択履歴・ジャーナルを積み、
// Report.BuildRunSaveFile / BuildReport の出力をファイルへ書く。TS 側 (scratchpad の round-trip スクリプト) がそれを読んで
// 「残りの手を続けるとゴールデンのハッシュに一致する」「計測 JSON が TS の battleMetrics と一致する」を確かめる。
//   dotnet run -- dump-save <golden.json> <n> <outDir> [--data <src/data>]
int DumpSave(string[] argv)
{
    var dataDir = "../../src/data";
    var rest = new List<string>();
    for (int i = 0; i < argv.Length; i++)
    {
        if (argv[i] == "--data" && i + 1 < argv.Length) { dataDir = argv[++i]; continue; }
        rest.Add(argv[i]);
    }
    if (rest.Count < 3) { Console.Error.WriteLine("usage: dotnet run -- dump-save <golden.json> <n> <outDir> [--data <src/data>]"); return 2; }
    Content.Load(dataDir);
    var golden = JObject.Parse(File.ReadAllText(rest[0]));
    var origin = JsonUnions.FromToken<ReplayOrigin>(golden["origin"]!);
    var commands = (JArray)golden["commands"]!;
    int n = Math.Min(int.Parse(rest[1]), commands.Count);
    Directory.CreateDirectory(rest[2]);

    var run = Run.ReplayInitialRun(origin);
    var history = new List<BattleArchive>();
    var choices = new List<RunChoice>();
    var cmds = new List<RunCommand>();
    var times = new List<long>();
    long t0 = 1_800_000_000_000L;   // 決定的な擬似時刻 (epoch ms)。TS 側の「判断時間」が読めることだけ確かめる (commands と同じ長さ)
    var text = new ReportText();
    for (int i = 0; i < n; i++)
    {
        var cmd = JsonUnions.FromToken<RunCommand>(commands[i]);
        var prev = run;
        run = Run.ApplyRunCommand(run, cmd);
        cmds.Add(cmd);
        times.Add(t0 + (i + 1) * 1500L);
        var line = Report.DescribeRunChoice(prev, cmd, run);
        if (line != null) choices.Add(line);
        var c = run.Combat;
        bool ended = c != null && (c.Phase == CombatPhases.Won || c.Phase == CombatPhases.Lost);
        if (ended && prev.Combat != null && prev.Combat.Phase != c!.Phase)
        {
            string enemyId = "unknown";
            for (int k = 0; k < c.EventLog.Count; k++) { var st = c.EventLog[k] as GameEvent_CombatStarted; if (st != null) { enemyId = st.EnemyId; break; } }
            var node = Run.CurrentNode(prev);
            var a = Report.ArchiveBattle(c, prev.BattlesWon + 1, enemyId, prev.CurrentElite, prev.Hp, prev.Deck.Count, prev.Act, node != null && node.Type == MapNodeTypes.Boss, text);
            if (history.Count == 0) a.Rating = new BattleRating { Strength = 3, Fun = 4, Note = "往復検証のダミー評価" };
            history.Add(a);
        }
    }
    var journal = new RunJournal { Origin = origin, Commands = cmds, Times = times };
    var notes = new List<PlayNote> { new PlayNote { At = "2026-09-14T12:00:00.000Z", Context = "幕1 行1 map 0勝 HP80", Text = "往復検証のメモ" } };
    var save = Report.BuildRunSaveFile(run, history, notes, journal, choices);
    File.WriteAllText(Path.Combine(rest[2], "unity-save.json"), save, new System.Text.UTF8Encoding(false));
    var md = Report.BuildReport(run, null, history, "", notes, choices, journal, text, new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc));
    File.WriteAllText(Path.Combine(rest[2], "unity-report.md"), md, new System.Text.UTF8Encoding(false));
    Console.WriteLine($"dump-save: {n}手 / 戦闘{history.Count} / 選択{choices.Count} / hash {Golden.RunHash(run)} / fingerprint {Report.DataFingerprint()} → {rest[2]}");
    return 0;
}
