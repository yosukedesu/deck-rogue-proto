using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Rng
{
	public static RngState Create(long seed)
	{
		return new RngState
		{
			Seed = (uint)seed,
			Counter = 0L
		};
	}

	public static (double Value, RngState Next) Next(RngState rng)
	{
		if (rng.Counter >= 4000000)
		{
			throw new InvalidOperationException("RNG counter がJS等価範囲を超過");
		}
		uint t = (uint)((uint)rng.Seed + 1831565813 * (rng.Counter + 1));
		t = (t ^ (t >> 15)) * (t | 1);
		t ^= t + (t ^ (t >> 7)) * (t | 0x3D);
		return (Value: (double)(t ^ (t >> 14)) / 4294967296.0, Next: rng with
		{
			Counter = rng.Counter + 1
		});
	}

	public static (int Value, RngState Next) NextInt(RngState rng, int min, int max)
	{
		(double Value, RngState Next) tuple = Next(rng);
		double v = tuple.Value;
		RngState next = tuple.Next;
		long span = (long)max - (long)min + 1;
		return (Value: (int)(min + (long)Math.Floor(v * (double)span)), Next: next);
	}

	public static (int Index, RngState Next) WeightedIndex(RngState rng, IReadOnlyList<double> weights)
	{
		double total = 0.0;
		for (int i = 0; i < weights.Count; i++)
		{
			total += weights[i];
		}
		(double Value, RngState Next) tuple = Next(rng);
		double v = tuple.Value;
		RngState next = tuple.Next;
		double roll = v * total;
		for (int j = 0; j < weights.Count; j++)
		{
			roll -= weights[j];
			if (roll < 0.0)
			{
				return (Index: j, Next: next);
			}
		}
		return (Index: weights.Count - 1, Next: next);
	}

	public static (T[] Items, RngState Next) Shuffle<T>(RngState rng, IReadOnlyList<T> items)
	{
		T[] result = new T[items.Count];
		for (int i = 0; i < items.Count; i++)
		{
			result[i] = items[i];
		}
		RngState state = rng;
		for (int i2 = result.Length - 1; i2 > 0; i2--)
		{
			(int Value, RngState Next) tuple = NextInt(state, 0, i2);
			int j = tuple.Value;
			state = tuple.Next;
			T[] array = result;
			int num = i2;
			T[] array2 = result;
			int num2 = j;
			T val = result[j];
			T val2 = result[i2];
			array[num] = val;
			array2[num2] = val2;
		}
		return (Items: result, Next: state);
	}
}
