#nullable enable
// dot の Godot 版の AttackRoute.cs をそのまま。演出の経路 (鍵 card:<id>・出所の種類・家族・描き手)。Unity では Hd2dAttackFx が Ticket から作る。
namespace DeckRogue.Game.Hd2d
{
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
}
