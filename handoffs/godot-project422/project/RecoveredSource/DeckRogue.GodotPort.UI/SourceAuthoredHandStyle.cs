namespace DeckRogue.GodotPort.UI;

public readonly record struct SourceAuthoredHandStyle(long Revision, string CardUid, string DefinitionId, string AuthoredAttack)
{
	public bool Matches(long revision, string uid, string definitionId)
	{
		bool flag = Revision == revision && CardUid == uid && DefinitionId == definitionId;
		if (flag)
		{
			string authoredAttack = AuthoredAttack;
			flag = authoredAttack == definitionId switch
			{
				"green_strike" => "slash", 
				"blue_strike" => "water", 
				"green_finisher_stomp" => "stomp", 
				_ => null, 
			};
		}
		if (flag)
		{
			return AuthoredAttack != null;
		}
		return false;
	}
}
