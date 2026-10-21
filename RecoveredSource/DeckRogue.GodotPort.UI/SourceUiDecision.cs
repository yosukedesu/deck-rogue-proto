namespace DeckRogue.GodotPort.UI;

public sealed class SourceUiDecision
{
	private readonly long _revision;

	public bool Accepted { get; private set; }

	public SourceUiDecision(long revision)
	{
		_revision = revision;
	}

	public bool TryAccept(long revision, bool blocked)
	{
		if ((Accepted | blocked) || revision != _revision)
		{
			return false;
		}
		Accepted = true;
		return true;
	}
}
