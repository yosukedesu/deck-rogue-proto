using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceIncomingFloatModel
{
	public sealed record Spec(string Text, string Color, int FontSize, float Delay, float LocalY, float Rise, float Duration);

	public readonly record struct EnvelopeLabel(Vector2 Size, Vector2 Offset, float Delay, float Rise, float Duration);

	public const float PopDuration = 0.18f;

	public const float PrimaryDuration = 0.9f;

	public const float PrimaryRise = 60f;

	public const string Rose = "c9635a";

	public const string SkyLight = "d6e6fa";

	public const string PaperDim = "c4beb2";

	private const double Back = 1.70158;

	private const double A = 1.080632;

	private const double B = 0.6806320000000001;

	public static Spec? AggregatePrimary(int amount, int hpLoss, string? outcome)
	{
		if (hpLoss <= 0)
		{
			if (amount <= 0 || !(outcome != "nullified"))
			{
				if (amount > 0)
				{
					return null;
				}
				return Primary("0", "c4beb2", 30);
			}
			return Primary("防いだ", "d6e6fa", 30);
		}
		return Loss(hpLoss);
	}

	public static Spec? IndividualPrimary(int amount, int hpLoss, string? outcome)
	{
		if (hpLoss <= 0)
		{
			if (!(outcome == "guard"))
			{
				if (amount != 0)
				{
					return null;
				}
				return Primary("0", "c4beb2", 30);
			}
			return Primary("防いだ", "d6e6fa", 30);
		}
		return Loss(hpLoss);
	}

	public static Spec? Absorption(int absorbed)
	{
		if (absorbed <= 0)
		{
			return null;
		}
		return new Spec("ブロックで −" + absorbed.ToString(CultureInfo.InvariantCulture), "d6e6fa", 22, 0.08f, 36f, 34f, 1f);
	}

	private static Spec Primary(string text, string color, int size)
	{
		return new Spec(text, color, size, 0f, 0f, 60f, 0.9f);
	}

	private static Spec Loss(int loss)
	{
		return Primary("-" + loss.ToString(CultureInfo.InvariantCulture), "c9635a", (loss >= 15) ? 46 : 36);
	}

	public static Vector2 Origin(float seatLogicalX, float logicalHeight, float unit, bool phoneLayout)
	{
		return new Vector2(seatLogicalX + 215f * unit, logicalHeight - ((phoneLayout ? 293f : 285f) + 360f + 10f) * unit);
	}

	public static float Scale(float age)
	{
		if (!float.IsFinite(age))
		{
			return 0.6f;
		}
		return (float)ScaleAt(Math.Max(0.0, age));
	}

	public static float Offset(float age, float rise, float duration)
	{
		if (!float.IsFinite(age) || !float.IsFinite(rise) || !(rise >= 0f) || !float.IsFinite(duration) || !(duration > 0f))
		{
			return 0f;
		}
		return (float)RiseAt(Math.Clamp(age, 0.0, duration), rise, duration);
	}

	public static float Alpha(float age, float duration)
	{
		if (!float.IsFinite(age) || age < 0f || !float.IsFinite(duration) || duration <= 0f)
		{
			return 0f;
		}
		double k = Math.Clamp((double)age / (double)duration, 0.0, 1.0);
		return (float)((k < 0.6) ? 1.0 : (1.0 - (k - 0.6) / 0.4));
	}

	public static bool Expired(float age, float duration, int bornGeneration, int currentGeneration, string? route)
	{
		if (float.IsFinite(age) && float.IsFinite(duration) && !(duration <= 0f) && !(age >= duration) && bornGeneration == currentGeneration)
		{
			return route != "combat";
		}
		return true;
	}

	public static bool TryPlace(Vector2 sourceOrigin, Rect2 safeRect, IReadOnlyList<EnvelopeLabel>? labels, out Vector2 placedOrigin, out Rect2 placedEnvelope)
	{
		placedOrigin = default;
		placedEnvelope = default;
		if (!Finite(sourceOrigin) || !Finite(safeRect.Position) || !Finite(safeRect.Size) || safeRect.Size.X <= 0f || safeRect.Size.Y <= 0f || labels == null || labels.Count == 0)
		{
			return false;
		}
		Vector2 safeEnd = safeRect.End;
		if (!Finite(safeEnd) || safeEnd.X <= safeRect.Position.X || safeEnd.Y <= safeRect.Position.Y)
		{
			return false;
		}
		double left = double.PositiveInfinity;
		double top = double.PositiveInfinity;
		double right = double.NegativeInfinity;
		double bottom = double.NegativeInfinity;
		for (int i = 0; i < labels.Count; i++)
		{
			EnvelopeLabel label = labels[i];
			if (!Finite(label.Size) || !Finite(label.Offset) || label.Size.X <= 0f || label.Size.Y <= 0f || !float.IsFinite(label.Delay) || label.Delay < 0f || !float.IsFinite(label.Rise) || label.Rise < 0f || !float.IsFinite(label.Duration) || label.Duration <= 0f)
			{
				return false;
			}
			double end = label.Duration;
			double peak = 0.10441845557393828;
			double maxScale = Math.Max(ScaleAt(0.0), ScaleAt(Math.Min(peak, end)));
			double halfWidth = (double)label.Size.X * 0.5 * maxScale;
			left = Math.Min(left, (double)label.Offset.X - halfWidth);
			right = Math.Max(right, (double)label.Offset.X + halfWidth);
			VerticalBounds(label, out var low, out var high);
			top = Math.Min(top, low);
			bottom = Math.Max(bottom, high);
		}
		if (!Finite(left, top, right, bottom))
		{
			return false;
		}
		double magnitude = Math.Max(1.0, Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), Math.Max(Math.Abs(top), Math.Abs(bottom))));
		magnitude = Math.Max(magnitude, Math.Max(Math.Max(Math.Abs((double)safeRect.Position.X), Math.Abs((double)safeRect.Position.Y)), Math.Max(Math.Abs((double)safeEnd.X), Math.Abs((double)safeEnd.Y))));
		double guard = Math.Max(0.0001, 1.0 / 1048576.0 * magnitude);
		if (!NearestRepresentable(sourceOrigin.X, (double)safeRect.Position.X - left + 2.0 * guard, (double)safeEnd.X - right - 2.0 * guard, out var x) || !NearestRepresentable(sourceOrigin.Y, (double)safeRect.Position.Y - top + 2.0 * guard, (double)safeEnd.Y - bottom - 2.0 * guard, out var y))
		{
			return false;
		}
		if (!OutwardRect((double)x + left - guard, (double)y + top - guard, (double)x + right + guard, (double)y + bottom + guard, out var envelope))
		{
			return false;
		}
		if (envelope.Position.X < safeRect.Position.X || envelope.Position.Y < safeRect.Position.Y || envelope.End.X > safeEnd.X || envelope.End.Y > safeEnd.Y)
		{
			return false;
		}
		placedOrigin = new Vector2(x, y);
		placedEnvelope = envelope;
		return true;
	}

	private static void VerticalBounds(EnvelopeLabel label, out double low, out double high)
	{
		double min = double.PositiveInfinity;
		double max = double.NegativeInfinity;
		double half = (double)label.Size.Y * 0.5;
		double rise = label.Rise;
		double duration = label.Duration;
		double popEnd = Math.Min(0.18000000715255737, duration);
		double lastU = popEnd / 0.18000000715255737 - 1.0;
		int sign;
		for (sign = -1; sign <= 1; sign += 2)
		{
			Include(0.0);
			Include(popEnd);
			Include(duration);
			double a = (double)sign * half * 3.0 * 1.080632 / 0.18000000715255737;
			double b = 2.0 * rise * 0.18000000715255737 / (duration * duration) + (double)sign * half * 2.0 * 0.6806320000000001 / 0.18000000715255737;
			double c = -2.0 * rise / duration + 2.0 * rise * 0.18000000715255737 / (duration * duration);
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
		}
		low = min;
		high = max;
		void Include(double time)
		{
			double value = (double)label.Offset.Y - RiseAt(time, rise, duration) + (double)sign * half * ScaleAt(time);
			min = Math.Min(min, value);
			max = Math.Max(max, value);
		}
		void IncludeRoot(double u)
		{
			if (double.IsFinite(u) && u >= -1.0 && u <= lastU)
			{
				Include((u + 1.0) * 0.18000000715255737);
			}
		}
	}

	private static double ScaleAt(double age)
	{
		double u = Math.Clamp(age / 0.18000000715255737, 0.0, 1.0) - 1.0;
		return 1.0 + 1.080632 * u * u * u + 0.6806320000000001 * u * u;
	}

	private static double RiseAt(double age, double rise, double duration)
	{
		double k = Math.Clamp(age / duration, 0.0, 1.0);
		return rise * (2.0 * k - k * k);
	}

	private static bool NearestRepresentable(float source, double min, double max, out float result)
	{
		result = 0f;
		if (!double.IsFinite(min) || !double.IsFinite(max) || min > max)
		{
			return false;
		}
		float candidate = (float)Math.Clamp(source, min, max);
		if (!float.IsFinite(candidate))
		{
			return false;
		}
		if ((double)candidate < min)
		{
			candidate = MathF.BitIncrement(candidate);
		}
		if ((double)candidate > max)
		{
			candidate = MathF.BitDecrement(candidate);
		}
		if (!float.IsFinite(candidate) || (double)candidate < min || (double)candidate > max)
		{
			return false;
		}
		result = candidate;
		return true;
	}

	private static bool OutwardRect(double left, double top, double right, double bottom, out Rect2 rect)
	{
		rect = default;
		if (!Finite(left, top, right, bottom))
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
		float width = (float)(right - (double)x);
		float height = (float)(bottom - (double)y);
		if ((double)width < right - (double)x)
		{
			width = MathF.BitIncrement(width);
		}
		if ((double)height < bottom - (double)y)
		{
			height = MathF.BitIncrement(height);
		}
		if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(width) || !float.IsFinite(height) || width <= 0f || height <= 0f)
		{
			return false;
		}
		rect = new Rect2(x, y, width, height);
		if ((double)rect.End.X < right)
		{
			width = MathF.BitIncrement(width);
			rect = new Rect2(x, y, width, height);
		}
		if ((double)rect.End.Y < bottom)
		{
			height = MathF.BitIncrement(height);
			rect = new Rect2(x, y, width, height);
		}
		return Finite(rect.End);
	}

	private static bool Finite(Vector2 value)
	{
		if (float.IsFinite(value.X))
		{
			return float.IsFinite(value.Y);
		}
		return false;
	}

	private static bool Finite(double a, double b, double c, double d)
	{
		if (double.IsFinite(a) && double.IsFinite(b) && double.IsFinite(c))
		{
			return double.IsFinite(d);
		}
		return false;
	}
}
