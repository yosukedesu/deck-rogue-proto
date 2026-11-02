using Godot;

namespace DeckRogue.GodotPort.Presentation;

public readonly record struct SourceCameraLayoutResult(Vector3 Position, Vector3 RotationDegrees, float Fov, float ReferenceDistance, float PlaneUnitsPerScreen, float GroundLineRatio);
