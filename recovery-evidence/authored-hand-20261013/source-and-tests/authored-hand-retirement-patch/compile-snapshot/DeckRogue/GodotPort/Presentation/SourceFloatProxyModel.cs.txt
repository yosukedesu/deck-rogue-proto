// Source Presenter.EnemyFloatPos samples the UI proxy, not its displaced billboard.
using System;
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public static class SourceFloatProxyModel
{
    // Camera.UnprojectPosition and CanvasLayer labels use the logical viewport,
    // while actor sizing reads render-texture pixels. Convert explicitly.
    public static float ProjectionUnit(float canvasScale,float logicalHeight,float renderHeight)
        =>canvasScale*logicalHeight/renderHeight;
    public static float UnroundedHeight(int baseTextureHeight,float pixelScale,float canvasScale)
        =>baseTextureHeight*pixelScale*canvasScale;
    public static Vector2 PlayerDamageOrigin(Vector2 proxyFeet,float unscaledProxyHeight,float executionScaleY,float unit)
    {
        // TransformPoint(rect.center) sees bottom-pinned proxy scale. The extra
        // .1*rect.height term is LOCAL unscaled height in Unity, not scaled bounds.
        return proxyFeet-new Vector2(0,unscaledProxyHeight*(.5f*executionScaleY+.1f)+20*unit);
    }
}
