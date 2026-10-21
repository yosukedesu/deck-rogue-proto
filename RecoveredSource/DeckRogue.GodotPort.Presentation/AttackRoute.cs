namespace DeckRogue.GodotPort.Presentation;

public sealed record AttackRoute(string Key, string DefinitionId, string SourceKind, string Family, string Renderer, string Element, bool FixedSmall, string PortStatus)
{
	public string? PresentationPolicy { get; init; }

	public bool SemanticSmall => PresentationPolicy == "small";

	public bool FixedContact
	{
		get
		{
			if (FixedSmall)
			{
				return !SemanticSmall;
			}
			return false;
		}
	}
}
