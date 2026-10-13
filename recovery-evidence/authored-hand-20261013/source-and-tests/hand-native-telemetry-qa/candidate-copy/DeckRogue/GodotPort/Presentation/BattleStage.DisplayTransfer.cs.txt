// RECONSTRUCTED accepted baseline toggle; no gameplay or clock mutation.
using System.Linq;
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleStage
{
    public bool ActorSourceDisplayRequested { get; private set; } = !OS.GetCmdlineUserArgs().Contains("--actor-color-baseline");
    public void SetActorSourceDisplay(bool enabled)
    {
        ActorSourceDisplayRequested = enabled && !OS.GetCmdlineUserArgs().Contains("--actor-color-baseline");
        if (_actorRoot == null || !GodotObject.IsInstanceValid(_actorRoot)) return;
        foreach (BattleActor actor in _actorRoot.GetChildren().OfType<BattleActor>())
            actor.SetSourceDisplayTransferEnabled(ActorSourceDisplayRequested);
    }
}
