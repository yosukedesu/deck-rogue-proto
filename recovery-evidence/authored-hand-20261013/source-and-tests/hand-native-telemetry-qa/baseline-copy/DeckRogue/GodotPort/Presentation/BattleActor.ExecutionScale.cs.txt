// Reconstructed from Presenter.ShowEnemyAct and Tween.Squash/Puff; feet stay anchored.
#nullable enable
using System;
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleActor {
 public Vector2 ExecutionScale {get;private set;}=Vector2.One;
 public bool ExecutionScaleActive=>_executionDuration>0;
 float _executionStart,_executionDuration,_executionX,_executionY;bool _executionPuff;int _executionVersion;
 public int ExecutionVersion=>_executionVersion;
 public void BeginExecutionScale(string kind,float now){
  CancelExecutionScale();_executionStart=now;
  switch(kind){
   case "attack":case "destroy-set":case "destroy-token":_executionDuration=.26f;_executionX=1.12f;_executionY=.86f;break;
   case "steal-gold":case "flee":_executionDuration=.26f;_executionX=1.12f;_executionY=.86f;break;
   case "seal":case "mill":_executionDuration=.26f;_executionX=1.10f;_executionY=.90f;break;
   case "rest":_executionDuration=.50f;_executionX=1.04f;_executionY=.96f;break;
   case "summon":case "hatch":_executionDuration=.40f;_executionX=_executionY=1.12f;_executionPuff=true;break;
   case "defend":_executionDuration=.30f;_executionX=1.06f;_executionY=.94f;break;
   case "buff":_executionDuration=.40f;_executionX=_executionY=1.15f;_executionPuff=true;break;
   case "rally":_executionDuration=.40f;_executionX=_executionY=1.12f;_executionPuff=true;break;
   case "hex":_executionDuration=.30f;_executionX=_executionY=1.08f;_executionPuff=true;break;
   case "heal":_executionDuration=.30f;_executionX=_executionY=1.06f;_executionPuff=true;break;
  }
 }
 public void CancelExecutionScale(){_executionVersion++;_executionDuration=0;_executionPuff=false;ExecutionScale=Vector2.One;}
 void SampleExecutionScale(float now){
  if(_executionDuration<=0)return;float k=Math.Clamp((now-_executionStart)/_executionDuration,0,1);if(k>=1){_executionDuration=0;ExecutionScale=Vector2.One;return;}
  float a;if(_executionPuff)a=MathF.Sin(k*MathF.PI);else if(k<.45f){float t=k/.45f;a=1-(1-t)*(1-t);}else{float t=(k-.45f)/.55f-1;a=-(2.70158f*t*t*t+1.70158f*t*t);}
  ExecutionScale=new(1+(_executionX-1)*a,1+(_executionY-1)*a);
 }
}
