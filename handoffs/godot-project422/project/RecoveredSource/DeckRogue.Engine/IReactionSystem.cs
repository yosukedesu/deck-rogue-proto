using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public interface IReactionSystem
{
	string Mode { get; }

	bool CanHandle(GameState state, Command command);

	GameState HandleCommand(GameState state, Command command);

	GameState OnEvent(GameState state, GameEvent ev);
}
