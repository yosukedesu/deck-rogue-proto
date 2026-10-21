// NEW implementation of the preserved, documented fallback order.
// Existing authored icons are returned unchanged. No enemy art is involved.
using Godot;
using DeckRogue.GodotPort.Presentation;
namespace DeckRogue.GodotPort.UI;
public static class SourceLeaderArtwork
{
    public static Texture2D Resolve(string id)
    {
        string requested = $"res://assets/Art/leaders/{id}.png";
        if (ResourceLoader.Exists(requested)) return GD.Load<Texture2D>(requested);
        string baseId = id.EndsWith("_icon", System.StringComparison.Ordinal) ? id[..^5] : id;
        string original = $"res://assets/Art/leaders/{baseId}.png";
        if (ResourceLoader.Exists(original)) return GD.Load<Texture2D>(original);
        return ProceduralActorTexture.Generate(baseId, friendly: true, 64);
    }
}
