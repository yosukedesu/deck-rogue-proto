// Rng.cs — シード付き決定論RNG (mulberry32)。TS版 src/engine/rng.ts の厳密移植。
// 同じシード + 同じ消費列 = 同じ結果 を言語間でbit-exactに保証する (goldens/rng-golden.json で照合)。
//
// JS→C# 等価性メモ:
// - JSの `>>> 0` (ToUint32) / `Math.imul` (int32wrap乗算) は uint のラップ演算で同一ビットになる
// - 先頭の `seed + 0x6d2b79f5 * (counter+1)` はJSではdouble演算。counter < 約490万まで
//   doubleが正確なので、ulong演算と同値 (実測の消費量は1戦闘で数百〜数千。到達しない)
// - `/ 4294967296` と `Math.Floor(v * n)` は IEEE754 double で JS と同一

using System;
using System.Collections.Generic;

namespace DeckRogue.Engine
{
    /// <summary>RNGの不変状態。GameStateに保持し、消費のたびに次の状態へ差し替える。</summary>
    public readonly struct RngState : IEquatable<RngState>
    {
        public readonly uint Seed;
        public readonly long Counter;

        public RngState(uint seed, long counter)
        {
            Seed = seed;
            Counter = counter;
        }

        public bool Equals(RngState other) => Seed == other.Seed && Counter == other.Counter;
        public override bool Equals(object obj) => obj is RngState s && Equals(s);
        public override int GetHashCode() => HashCode.Combine(Seed, Counter);
    }

    public static class Rng
    {
        /// <summary>JSの `seed >>> 0` と同じくToUint32でシードを正規化する。</summary>
        public static RngState Create(long seed) => new RngState((uint)seed, 0);

        /// <summary>[0, 1) の乱数を1つ消費。(値, 次の状態) を返す純関数。</summary>
        public static (double Value, RngState Next) Next(in RngState rng)
        {
            // JS doubleの正確性が保てる範囲を超えたら等価性が壊れるため防衛 (実用上は到達しない)
            if (rng.Counter >= 4_000_000)
                throw new InvalidOperationException("RNG counter がJS等価範囲を超過");

            uint t = (uint)(rng.Seed + 0x6d2b79f5UL * (ulong)(rng.Counter + 1));
            t = (uint)((t ^ (t >> 15)) * (t | 1u));
            t ^= t + (uint)((t ^ (t >> 7)) * (t | 61u));
            double value = (t ^ (t >> 14)) / 4294967296.0;
            return (value, new RngState(rng.Seed, rng.Counter + 1));
        }

        /// <summary>[min, max] の整数を1つ消費。</summary>
        public static (int Value, RngState Next) NextInt(in RngState rng, int min, int max)
        {
            var (v, next) = Next(rng);
            return (min + (int)Math.Floor(v * (max - min + 1)), next);
        }

        /// <summary>重み配列からインデックスを1つ抽選。重み合計は正であること。</summary>
        public static (int Index, RngState Next) WeightedIndex(in RngState rng, IReadOnlyList<double> weights)
        {
            double total = 0;
            for (int i = 0; i < weights.Count; i++) total += weights[i];
            var (v, next) = Next(rng);
            double roll = v * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0) return (i, next);
            }
            return (weights.Count - 1, next);
        }

        /// <summary>配列をシャッフル (Fisher–Yates)。元の列は変更しない。</summary>
        public static (T[] Items, RngState Next) Shuffle<T>(in RngState rng, IReadOnlyList<T> items)
        {
            var result = new T[items.Count];
            for (int i = 0; i < items.Count; i++) result[i] = items[i];
            var state = rng;
            for (int i = result.Length - 1; i > 0; i--)
            {
                var (j, s) = NextInt(state, 0, i);
                state = s;
                (result[i], result[j]) = (result[j], result[i]);
            }
            return (result, state);
        }
    }
}
