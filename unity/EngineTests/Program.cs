// Program.cs — RNGゴールデンマスター照合 (Unity移植 P0スパイク)
// goldens/rng-golden.json (TS側 scripts/dump-rng-golden.ts が生成) を読み、
// C#版 Rng が全系列を bit-exact に再現することを検証する。
// 実行: dotnet run --project unity/EngineTests -- goldens/rng-golden.json

using System.Text.Json;
using DeckRogue.Engine;

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
