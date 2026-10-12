namespace DeckRogue.GodotPort.Core;

public sealed record SessionResult(bool Success, string? Error = null, string? Warning = null)
{
	public static SessionResult Ok(string? warning = null)
	{
		return new SessionResult(Success: true, null, warning);
	}

	public static SessionResult Fail(string error)
	{
		return new SessionResult(Success: false, error);
	}
}
