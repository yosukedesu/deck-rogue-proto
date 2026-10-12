// Reconstructed source phase timing/paint. No input interception or rules access.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleStage {
 CanvasLayer? _phaseLayer;Control? _phaseBand;Label? _phaseText;ColorRect? _phaseAccent,_injuryFlash;
 float _bannerAt=-100,_flashAt=-100,_flashAlpha;bool _bannerEnemy;
 Func<IReadOnlyList<Rect2>>? _enemyLedgerBounds;
 public void SetEnemyLedgerBounds(Func<IReadOnlyList<Rect2>>? bounds)=>_enemyLedgerBounds=bounds;
 public string ActivePhaseBanner=>_phaseBand?.Visible==true?_phaseText?.Text??"":"";
 public float ActivePhaseBannerAlpha=>_phaseBand?.Visible==true?_phaseBand.Modulate.A:0;
 public int EnemyExecutionCount {get;private set;}
 public int InjuryFlashCount {get;private set;}
 public Rect2 PhaseBannerBounds=>_phaseBand?.GetGlobalRect()??default;
 void EnsurePhaseLayer(){
  if(_phaseLayer is not null)return;_phaseLayer=new CanvasLayer{Name="SourcePhaseEffects",Layer=8};AddChild(_phaseLayer);
  _injuryFlash=new ColorRect{Name="SourceInjuryFlash",MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false};_phaseLayer.AddChild(_injuryFlash);
  _phaseBand=new ColorRect{Name="SourceTurnBanner",MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false};_phaseLayer.AddChild(_phaseBand);
  _phaseText=new Label{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore};
  _phaseText.AddThemeFontOverride("font",GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf"));_phaseText.AddThemeFontSizeOverride("font_size",34);_phaseBand.AddChild(_phaseText);
  _phaseAccent=new ColorRect{MouseFilter=Control.MouseFilterEnum.Ignore};_phaseBand.AddChild(_phaseAccent);
 }
 void PresentSourcePhaseCue(GameEvent e){
  if(e is GameEvent_EnemyActionExecuting act){Enemy(act.EnemyIndex)?.BeginExecutionScale(act.Kind,_clock);EnemyExecutionCount++;}
  if(e is not GameEvent_TurnEnded and not GameEvent_TurnStarted)return;
  EnsurePhaseLayer();_bannerEnemy=e is GameEvent_TurnEnded;_bannerAt=_clock;_phaseText!.Text=_bannerEnemy?"敵の番":"あなたの番";
  ((ColorRect)_phaseBand!).Color=new Color(_bannerEnemy?"252433":"f4ecd6");_phaseText.AddThemeColorOverride("font_color",new Color(_bannerEnemy?"f4ecd6":"2f2e35"));_phaseAccent!.Color=new Color(_bannerEnemy?"c9635a":"7fa86c");SamplePhaseLayer();
 }
 void PresentInjuryFlash(int loss){if(loss<=0)return;EnsurePhaseLayer();_flashAt=_clock;_flashAlpha=Math.Min(.35f,.1f+loss*.015f);InjuryFlashCount++;}
 Rect2 ActorScreenBody(BattleActor a){
  var r=a.Sprite.GetItemRect();var p=r.Position;var z=r.End;Vector2 min=new(float.PositiveInfinity,float.PositiveInfinity),max=new(float.NegativeInfinity,float.NegativeInfinity);
  foreach(var q in new[]{new Vector2(p.X,p.Y),new Vector2(p.X,z.Y),new Vector2(z.X,p.Y),new Vector2(z.X,z.Y)}){
   var v=_world.Camera.UnprojectPosition(a.Sprite.GlobalTransform*new Vector3(q.X*a.Sprite.PixelSize,-q.Y*a.Sprite.PixelSize,0));min=min.Min(v);max=max.Max(v);
  }return new(min,max-min);
 }
 void SamplePhaseLayer(){
  if(_phaseLayer is null)return;var size=GetViewport().GetVisibleRect().Size;
  _injuryFlash!.Size=size;float flashAge=_clock-_flashAt;_injuryFlash.Visible=_interfaceRoute=="combat"&&flashAge<.25f;_injuryFlash.Color=new Color(.9f,.1f,.1f,_flashAlpha*Math.Clamp(1-flashAge/.25f,0,1));
  float age=_clock-_bannerAt,k=age/.9f;_phaseBand!.Visible=_interfaceRoute=="combat"&&k<1;
  if(!_phaseBand.Visible)return;_phaseBand.Modulate=new Color(1,1,1,k<.15f?k/.15f:k>.7f?1-(k-.7f)/.3f:1);
  float need=_phaseText!.GetThemeFont("font").GetStringSize(_phaseText.Text,HorizontalAlignment.Left,-1,34).X+72;
  float cx=size.X/2,cy=size.Y/2,w=720;
  var actors=_enemies.Values.Where(a=>a.Visible&&a.DissolveAmount<1).ToArray();
  if(_hero is not null&&actors.Length>0){float left=ActorScreenBody(_hero).End.X,right=actors.Min(a=>ActorScreenBody(a).Position.X);if(right>left){w=Math.Max(need,Math.Min(560,right-left-32));cx=Math.Clamp((left+right)/2,w/2+8,Math.Max(w/2+8,size.X-w/2-8));}}
  foreach(var ledger in _enemyLedgerBounds?.Invoke()??Array.Empty<Rect2>())if(ledger.End.X>cx-w/2&&ledger.Position.X<cx+w/2)cy=Math.Min(cy,ledger.Position.Y-8-40);
  cy=Math.Max(100+8+40,cy);_phaseBand.Position=new(cx-w/2,cy-40);_phaseBand.Size=new(w,80);_phaseText.Position=new(0,4);_phaseText.Size=new(w,76);_phaseAccent!.Position=new(w/2-60,64);_phaseAccent.Size=new(120,4);
 }
 void CancelPhaseLayer(){_bannerAt=_flashAt=-100;if(_phaseBand is not null)_phaseBand.Visible=false;if(_injuryFlash is not null)_injuryFlash.Visible=false;}
}
