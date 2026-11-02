using System;
using System.Collections.Generic;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceIncomingRegionModel
{
	public enum PlacementStatus
	{
		Placed,
		SoftAvoidance,
		Saturated,
		RegionTooSmall,
		InvalidRegion,
		InvalidGeometry
	}

	public readonly record struct Placement(Vector2 Origin, IReadOnlyList<Rect2> SweptRects, Rect2 Envelope, bool CollisionFree, bool AvoidanceClear, bool RegionContained, PlacementStatus Status, int CandidatesEvaluated);

	private readonly record struct Score(int Tier, double LiveArea, double AvoidArea, double Distance, float X, float Y);

	public const int MaxAxisCandidates = 33;

	public const int MaxPlacementCandidates = 1089;

	private const double Back = 1.70158;

	private const double A = 1.080632;

	private const double B = 0.6806320000000001;

	private const double Epsilon = 1.1920928955078125E-07;

	private static double Pop => 0.18000000715255737;

	public static Rect2 SweptInk(Rect2 pivotRelativeInk, float localY, float rise, float duration)
	{
		if (!ValidRect(pivotRelativeInk) || !float.IsFinite(localY) || !float.IsFinite(rise) || rise < 0f || !float.IsFinite(duration) || duration <= 0f)
		{
			return default;
		}
		double left = double.PositiveInfinity;
		double right = double.NegativeInfinity;
		double top = double.PositiveInfinity;
		double bottom = double.NegativeInfinity;
		double popEnd = Math.Min(Pop, duration);
		double lastU = popEnd / Pop - 1.0;
		double peak = Pop * 0.5801025079151212;
		double[] array = new double[4]
		{
			0.0,
			popEnd,
			duration,
			Math.Min(peak, duration)
		};
		for (int i = 0; i < array.Length; i++)
		{
			double s = ScaleAt(array[i]);
			left = Math.Min(left, (double)pivotRelativeInk.Position.X * s);
			right = Math.Max(right, (double)pivotRelativeInk.End.X * s);
		}
		array = new double[2]
		{
			pivotRelativeInk.Position.Y,
			pivotRelativeInk.End.Y
		};
		foreach (double edge in array)
		{
			Include(0.0);
			Include(popEnd);
			Include(duration);
			double a = edge * 3.0 * 1.080632 / Pop;
			double b = 2.0 * (double)rise * Pop / ((double)duration * (double)duration) + edge * 2.0 * 0.6806320000000001 / Pop;
			double c = -2.0 * (double)rise / (double)duration + 2.0 * (double)rise * Pop / ((double)duration * (double)duration);
			if (a == 0.0)
			{
				if (b != 0.0)
				{
					IncludeRoot((0.0 - c) / b);
				}
				continue;
			}
			double discriminant = b * b - 4.0 * a * c;
			if (!(discriminant < 0.0))
			{
				double q = -0.5 * (b + Math.CopySign(Math.Sqrt(discriminant), b));
				if (q == 0.0)
				{
					IncludeRoot((0.0 - b) / (2.0 * a));
					continue;
				}
				IncludeRoot(q / a);
				IncludeRoot(c / q);
			}
			void Include(double time)
			{
				double k = Math.Clamp(time / (double)duration, 0.0, 1.0);
				double value = (double)localY - (double)rise * (2.0 * k - k * k) + edge * ScaleAt(time);
				top = Math.Min(top, value);
				bottom = Math.Max(bottom, value);
			}
			void IncludeRoot(double u)
			{
				if (double.IsFinite(u) && u >= -1.0 && u <= lastU)
				{
					Include((u + 1.0) * Pop);
				}
			}
		}
		double guard = Guard(Math.Max(Math.Max(Math.Abs((double)localY), rise), Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), Math.Max(Math.Abs(top), Math.Abs(bottom)))));
		if (!OutwardRect(left - guard, top - guard, right + guard, bottom + guard, out var sweep))
		{
			return default;
		}
		return sweep;
	}

	public static bool TryPlace(Vector2 preferredOrigin, Rect2 region, IReadOnlyList<Rect2>? localSweptRects, IReadOnlyList<Rect2>? occupiedWorldSweeps, IReadOnlyList<Rect2>? avoidRects, float gap, out Placement placement)
	{
		placement = Empty(PlacementStatus.InvalidGeometry);
		if (!Finite(preferredOrigin) || !float.IsFinite(gap) || gap < 0f || localSweptRects == null || localSweptRects.Count == 0)
		{
			return false;
		}
		Rect2[] local = new Rect2[localSweptRects.Count];
		for (int i = 0; i < local.Length; i++)
		{
			if (!ValidRect(localSweptRects[i]))
			{
				return false;
			}
			local[i] = localSweptRects[i];
		}
		if (!Union(local, out var localEnvelope))
		{
			return false;
		}
		if (!ValidRect(region))
		{
			placement = Empty(PlacementStatus.InvalidRegion);
			return false;
		}
		List<Rect2> live = CopyValid(occupiedWorldSweeps, out var liveValid);
		List<Rect2> avoid = CopyValid(avoidRects, out var avoidValid);
		double minX = (double)region.Position.X - (double)localEnvelope.Position.X;
		double maxX = (double)region.End.X - (double)localEnvelope.End.X;
		double minY = (double)region.Position.Y - (double)localEnvelope.Position.Y;
		double maxY = (double)region.End.Y - (double)localEnvelope.End.Y;
		if (minX > maxX || minY > maxY)
		{
			float x = (float)((minX > maxX) ? ((minX + maxX) * 0.5) : Math.Clamp(preferredOrigin.X, minX, maxX));
			float y = (float)((minY > maxY) ? ((minY + maxY) * 0.5) : Math.Clamp(preferredOrigin.Y, minY, maxY));
			if (!Translated(local, new Vector2(x, y), out Rect2[] sweeps, out Rect2 envelope))
			{
				return false;
			}
			placement = Make(new Vector2(x, y), sweeps, envelope, live, avoid, liveValid, avoidValid, gap, contained: false, PlacementStatus.RegionTooSmall, 1);
			return true;
		}
		List<float> xs = AxisCandidates(preferredOrigin.X, minX, maxX, local, live, avoid, gap, horizontal: true);
		List<float> list = AxisCandidates(preferredOrigin.Y, minY, maxY, local, live, avoid, gap, horizontal: false);
		bool haveBest = false;
		Score best = default;
		Rect2[] bestSweeps = null;
		Rect2 bestEnvelope = default;
		int evaluated = 0;
		foreach (float y2 in list)
		{
			foreach (float x2 in xs)
			{
				evaluated++;
				Vector2 origin = new Vector2(x2, y2);
				if (Translated(local, origin, out Rect2[] sweeps2, out Rect2 envelope2) && AllContained(region, sweeps2))
				{
					if (!Contains(region, envelope2))
					{
						envelope2 = region;
					}
					double liveArea = OverlapArea(sweeps2, live, gap);
					double avoidArea = OverlapArea(sweeps2, avoid, gap);
					bool collisionFree = liveValid && liveArea == 0.0;
					bool avoidanceClear = avoidValid && avoidArea == 0.0;
					double dx = (double)x2 - (double)preferredOrigin.X;
					double dy = (double)y2 - (double)preferredOrigin.Y;
					Score score = new Score(collisionFree ? ((!avoidanceClear) ? 1 : 0) : 2, liveArea, avoidArea, dx * dx + dy * dy, x2, y2);
					if (!haveBest || Better(score, best))
					{
						haveBest = true;
						best = score;
						bestSweeps = sweeps2;
						bestEnvelope = envelope2;
					}
				}
			}
		}
		if (!haveBest || bestSweeps == null)
		{
			if (!Translated(local, preferredOrigin, out Rect2[] sweeps3, out Rect2 envelope3))
			{
				return false;
			}
			placement = Make(preferredOrigin, sweeps3, envelope3, live, avoid, liveValid, avoidValid, gap, contained: false, PlacementStatus.RegionTooSmall, evaluated);
			return true;
		}
		placement = new Placement(new Vector2(best.X, best.Y), Array.AsReadOnly(bestSweeps), bestEnvelope, best.Tier < 2, avoidValid && best.AvoidArea == 0.0, RegionContained: true, (best.Tier != 0) ? ((best.Tier == 1) ? PlacementStatus.SoftAvoidance : PlacementStatus.Saturated) : PlacementStatus.Placed, evaluated);
		return true;
	}

	private static Placement Empty(PlacementStatus status)
	{
		return new Placement(default, Array.Empty<Rect2>(), default, CollisionFree: false, AvoidanceClear: false, RegionContained: false, status, 0);
	}

	private static Placement Make(Vector2 origin, Rect2[] sweeps, Rect2 envelope, List<Rect2> live, List<Rect2> avoid, bool liveValid, bool avoidValid, float gap, bool contained, PlacementStatus status, int count)
	{
		return new Placement(origin, Array.AsReadOnly(sweeps), envelope, liveValid && OverlapArea(sweeps, live, gap) == 0.0, avoidValid && OverlapArea(sweeps, avoid, gap) == 0.0, contained, status, count);
	}

	private static bool Better(Score a, Score b)
	{
		if (a.Tier != b.Tier)
		{
			return a.Tier < b.Tier;
		}
		if (a.Tier == 2)
		{
			if (a.LiveArea != b.LiveArea)
			{
				return a.LiveArea < b.LiveArea;
			}
			if (a.AvoidArea != b.AvoidArea)
			{
				return a.AvoidArea < b.AvoidArea;
			}
		}
		if (a.Distance != b.Distance)
		{
			return a.Distance < b.Distance;
		}
		if (a.Y == b.Y)
		{
			return a.X < b.X;
		}
		return a.Y < b.Y;
	}

	private static List<float> AxisCandidates(float preferred, double min, double max, Rect2[] local, List<Rect2> live, List<Rect2> avoid, float gap, bool horizontal)
	{
		min = Math.Max(min, -3.4028234663852886E+38);
		max = Math.Min(max, 3.4028234663852886E+38);
		SortedSet<float> mandatory = new SortedSet<float>();
		SortedSet<float> optional = new SortedSet<float>();
		if (min > max)
		{
			return new List<float>();
		}
		double guard = Guard(Math.Max(Math.Abs(min), Math.Abs(max)));
		Add(preferred, mandatory);
		Add(min, mandatory);
		Add(max, mandatory);
		Add(min + guard, mandatory);
		Add(max - guard, mandatory);
		Add((min + max) * 0.5, mandatory);
		AddObstacles(live);
		AddObstacles(avoid);
		List<float> list = new List<float>(optional);
		list.Sort((float a, float b) =>
		{
			int num = Math.Abs((double)a - (double)preferred).CompareTo(Math.Abs((double)b - (double)preferred));
			return (num == 0) ? a.CompareTo(b) : num;
		});
		foreach (float value in list)
		{
			if (mandatory.Count >= 33)
			{
				break;
			}
			mandatory.Add(value);
		}
		return new List<float>(mandatory);
		void Add(double num, SortedSet<float> set)
		{
			if (double.IsFinite(num))
			{
				float f = (float)Math.Clamp(num, min, max);
				if ((double)f < min)
				{
					f = MathF.BitIncrement(f);
				}
				if ((double)f > max)
				{
					f = MathF.BitDecrement(f);
				}
				if (float.IsFinite(f) && (double)f >= min && (double)f <= max)
				{
					set.Add(f);
				}
			}
		}
		void AddObstacles(List<Rect2> obstacles)
		{
			foreach (Rect2 obstacle in obstacles)
			{
				Rect2[] array = local;
				for (int i = 0; i < array.Length; i++)
				{
					Rect2 ink = array[i];
					double o0 = (horizontal ? obstacle.Position.X : obstacle.Position.Y);
					double o1 = (horizontal ? obstacle.End.X : obstacle.End.Y);
					double i2 = (horizontal ? ink.Position.X : ink.Position.Y);
					double i3 = (horizontal ? ink.End.X : ink.End.Y);
					Add(o0 - (double)gap - i3 - guard, optional);
					Add(o1 + (double)gap - i2 + guard, optional);
					Add(o0 - (double)gap - i2, optional);
					Add(o1 + (double)gap - i3, optional);
				}
			}
		}
	}

	private static List<Rect2> CopyValid(IReadOnlyList<Rect2>? input, out bool valid)
	{
		valid = true;
		List<Rect2> result = new List<Rect2>();
		if (input == null)
		{
			return result;
		}
		for (int i = 0; i < input.Count; i++)
		{
			if (ValidRect(input[i]))
			{
				result.Add(input[i]);
			}
			else
			{
				valid = false;
			}
		}
		result.Sort((Rect2 a, Rect2 b) =>
		{
			int num = a.Position.X.CompareTo(b.Position.X);
			if (num != 0)
			{
				return num;
			}
			num = a.Position.Y.CompareTo(b.Position.Y);
			if (num != 0)
			{
				return num;
			}
			num = a.Size.X.CompareTo(b.Size.X);
			return (num == 0) ? a.Size.Y.CompareTo(b.Size.Y) : num;
		});
		return result;
	}

	private static double OverlapArea(Rect2[] moving, List<Rect2> obstacles, float gap)
	{
		double area = 0.0;
		for (int i = 0; i < moving.Length; i++)
		{
			Rect2 ink = moving[i];
			foreach (Rect2 obstacle in obstacles)
			{
				double w = Math.Max(0.0, Math.Min(ink.End.X, (double)obstacle.End.X + (double)gap) - Math.Max(ink.Position.X, (double)obstacle.Position.X - (double)gap));
				double h = Math.Max(0.0, Math.Min(ink.End.Y, (double)obstacle.End.Y + (double)gap) - Math.Max(ink.Position.Y, (double)obstacle.Position.Y - (double)gap));
				area += w * h;
			}
		}
		return area;
	}

	private static bool Translated(Rect2[] local, Vector2 origin, out Rect2[] world, out Rect2 envelope)
	{
		world = new Rect2[local.Length];
		envelope = default;
		if (!Finite(origin))
		{
			return false;
		}
		for (int i = 0; i < local.Length; i++)
		{
			Rect2 r = local[i];
			if (!OutwardRect((double)origin.X + (double)r.Position.X, (double)origin.Y + (double)r.Position.Y, (double)origin.X + (double)r.End.X, (double)origin.Y + (double)r.End.Y, out world[i]))
			{
				return false;
			}
		}
		return Union(world, out envelope);
	}

	private static bool Union(Rect2[] rects, out Rect2 envelope)
	{
		double left = double.PositiveInfinity;
		double top = double.PositiveInfinity;
		double right = double.NegativeInfinity;
		double bottom = double.NegativeInfinity;
		for (int i = 0; i < rects.Length; i++)
		{
			Rect2 r = rects[i];
			left = Math.Min(left, r.Position.X);
			top = Math.Min(top, r.Position.Y);
			right = Math.Max(right, r.End.X);
			bottom = Math.Max(bottom, r.End.Y);
		}
		return OutwardRect(left, top, right, bottom, out envelope);
	}

	private static bool OutwardRect(double left, double top, double right, double bottom, out Rect2 rect)
	{
		rect = default;
		if (!double.IsFinite(left) || !double.IsFinite(top) || !double.IsFinite(right) || !double.IsFinite(bottom))
		{
			return false;
		}
		float x = (float)left;
		float y = (float)top;
		if ((double)x > left)
		{
			x = MathF.BitDecrement(x);
		}
		if ((double)y > top)
		{
			y = MathF.BitDecrement(y);
		}
		float w = (float)(right - (double)x);
		float h = (float)(bottom - (double)y);
		if ((double)w < right - (double)x)
		{
			w = MathF.BitIncrement(w);
		}
		if ((double)h < bottom - (double)y)
		{
			h = MathF.BitIncrement(h);
		}
		rect = new Rect2(x, y, w, h);
		if ((double)rect.End.X < right)
		{
			w = MathF.BitIncrement(w);
			rect = new Rect2(x, y, w, h);
		}
		if ((double)rect.End.Y < bottom)
		{
			h = MathF.BitIncrement(h);
			rect = new Rect2(x, y, w, h);
		}
		return ValidRect(rect);
	}

	private static double ScaleAt(double age)
	{
		double u = Math.Clamp(age / Pop, 0.0, 1.0) - 1.0;
		return 1.0 + 1.080632 * u * u * u + 0.6806320000000001 * u * u;
	}

	private static double Guard(double magnitude)
	{
		return Math.Max(0.0001, 1.0 / 1048576.0 * Math.Max(1.0, magnitude));
	}

	private static bool Finite(Vector2 value)
	{
		if (float.IsFinite(value.X))
		{
			return float.IsFinite(value.Y);
		}
		return false;
	}

	private static bool ValidRect(Rect2 r)
	{
		if (Finite(r.Position) && Finite(r.Size) && Finite(r.End) && r.Size.X > 0f && r.Size.Y > 0f && r.End.X > r.Position.X)
		{
			return r.End.Y > r.Position.Y;
		}
		return false;
	}

	private static bool Contains(Rect2 outer, Rect2 inner)
	{
		if (inner.Position.X >= outer.Position.X && inner.Position.Y >= outer.Position.Y && inner.End.X <= outer.End.X)
		{
			return inner.End.Y <= outer.End.Y;
		}
		return false;
	}

	private static bool AllContained(Rect2 outer, Rect2[] inner)
	{
		foreach (Rect2 r in inner)
		{
			if (!Contains(outer, r))
			{
				return false;
			}
		}
		return true;
	}
}
