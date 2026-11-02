namespace DeckRogue.GodotPort.UI;

public readonly record struct SourceTokenRect(float X, float Y, float W, float H)
{
	public float Right => X + W;

	public float Bottom => Y + H;

	public bool Intersects(SourceTokenRect r)
	{
		if (X < r.Right && Right > r.X && Y < r.Bottom)
		{
			return Bottom > r.Y;
		}
		return false;
	}
}
