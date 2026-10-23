#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Godot;
using Newtonsoft.Json.Linq;
namespace DeckRogue.GodotPort.Presentation;
// Debug-only, presentation-only sandbox. Never constructs GameSession or writes saves.
internal sealed class EnemyMotionDebugMenu
{
 readonly Node owner; readonly SceneTree tree; readonly bool direct, previousPause;
 readonly CanvasLayer layer = new() {Layer=100, ProcessMode=Node.ProcessModeEnum.Always};
 readonly List<JObject> enemies = new(); readonly List<(string key,JObject? move)> choices=new();
 readonly OptionButton enemy = new(), motion = new(), speed = new();
 readonly CheckButton loop = new(){Text="ループ",ButtonPressed=true};
 readonly Label state=new(), note=new(), title=new(); readonly TextureRect picture=new();
 readonly Control stage=new(); readonly Button play=new(){Text="再生"};
 BattleActor? actor; EnemyAuthoredClipSet? clips; bool playing, closed, ranged, released, captureDown; int captures; double time, accumulator; int tick, nextContact; float duration=2; string id="",key=""; JObject? move;
 Style04EnemySupportMotion.Token support; Basis baseSpriteBasis=Basis.Identity;
 Dictionary<string,string> labels=new(), moveLabels=new();
 const double Step=1.0/60.0;
 public static void InstallEntry(Node owner) { var l=new CanvasLayer{Layer=90}; owner.AddChild(l); var b=new Button{Text="モーション確認［DEBUG］",Position=new Vector2(1250,10),CustomMinimumSize=new Vector2(330,52)};l.AddChild(b);b.Pressed+=()=>Open(owner,false); }
 public static void Open(Node owner,bool direct) { if(owner.HasMeta("motion_debug_open"))return; owner.SetMeta("motion_debug_open",true);new EnemyMotionDebugMenu(owner,direct); }
 EnemyMotionDebugMenu(Node o,bool d) {
  owner=o;direct=d;tree=o.GetTree();previousPause=tree.Paused;tree.Paused=true;try { owner.AddChild(layer);owner.TreeExiting+=OwnerExiting;
  var panel=new PanelContainer{Theme=new Theme{DefaultFontSize=22,DefaultFont=GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf")}};layer.AddChild(panel);panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=new Color("101826")});
  var margin=new MarginContainer();panel.AddChild(margin);foreach(var s in new[]{"left","right","top","bottom"})margin.AddThemeConstantOverride("margin_"+s,24);
  var rows=new VBoxContainer();rows.AddThemeConstantOverride("separation",14);margin.AddChild(rows);
  title.Text="敵モーション確認  /  DEBUG SANDBOX";title.AddThemeFontSizeOverride("font_size",30);rows.AddChild(title);
  var top=new HBoxContainer();rows.AddChild(top);top.AddChild(new Label{Text="敵キャラ"});top.AddChild(enemy);enemy.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;enemy.CustomMinimumSize=new Vector2(520,58);top.AddChild(Button("前の敵",()=>SelectEnemy((enemy.Selected+enemies.Count-1)%enemies.Count)));top.AddChild(Button("次の敵",()=>SelectEnemy((enemy.Selected+1)%enemies.Count)));top.AddChild(Button(direct?"終了":"ゲームへ戻る",Close));
  var mr=new HBoxContainer();rows.AddChild(mr);mr.AddChild(new Label{Text="モーション"});mr.AddChild(motion);motion.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;motion.CustomMinimumSize=new Vector2(700,58);
  var view=new PanelContainer();view.SizeFlagsVertical=Control.SizeFlags.ExpandFill;view.AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=new Color("1b2939"),BorderColor=new Color("40566b"),BorderWidthBottom=2,BorderWidthTop=2,BorderWidthLeft=2,BorderWidthRight=2});rows.AddChild(view);view.AddChild(stage);stage.CustomMinimumSize=new Vector2(100,380);
  var ground=new ColorRect{Color=new Color("415365"),MouseFilter=Control.MouseFilterEnum.Ignore};stage.AddChild(ground);ground.AnchorTop=.84f;ground.AnchorBottom=.84f;ground.AnchorRight=1;ground.OffsetBottom=2;
  stage.AddChild(picture);picture.TextureFilter=CanvasItem.TextureFilterEnum.Nearest;picture.MouseFilter=Control.MouseFilterEnum.Ignore;picture.ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize;picture.StretchMode=TextureRect.StretchModeEnum.Scale;
  note.AutowrapMode=TextServer.AutowrapMode.WordSmart;note.AddThemeFontSizeOverride("font_size",18);rows.AddChild(note);
  var controls=new HBoxContainer();controls.AddThemeConstantOverride("separation",12);rows.AddChild(controls);controls.AddChild(play);play.CustomMinimumSize=new Vector2(130,60);play.Pressed+=()=>Safe(()=>{if(time>=duration)Restart();playing=!playing;Refresh();});controls.AddChild(Button("最初から",()=>Restart()));controls.AddChild(Button("1コマ進む",()=>{playing=false;Advance();Refresh();}));controls.AddChild(new Label{Text="速度"});controls.AddChild(speed);foreach(var s in new[]{"0.25×","0.5×","1×","2×"})speed.AddItem(s);speed.Select(2);speed.CustomMinimumSize=new Vector2(110,60);controls.AddChild(loop);controls.AddChild(state);state.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
  rows.AddChild(new Label{Text="1コマ = 1/60秒。固定カメラ・揺れなし。プレビューはゲーム状態・セーブを変更しません。",Modulate=new Color("a9bed0")});
  var labelPath="res://motion-debug-labels.json";if(Godot.FileAccess.FileExists(labelPath))labels=JObject.Parse(EnemyResourceFile.ReadAllText(labelPath)).Properties().ToDictionary(x=>x.Name,x=>x.Value.ToString());
  var moveLabelPath="res://motion-debug-move-labels.json";if(Godot.FileAccess.FileExists(moveLabelPath))moveLabels=JObject.Parse(EnemyResourceFile.ReadAllText(moveLabelPath)).Properties().ToDictionary(x=>x.Name,x=>x.Value.ToString());
  foreach(var e in JArray.Parse(EnemyResourceFile.ReadAllText("res://data/enemies.json")).OfType<JObject>())if(Style04EnemySpriteRegistry.TryGet(e["id"]!.ToString(),out _))enemies.Add(e);
  enemies.Sort((a,b)=>string.CompareOrdinal(a["name"]!.ToString(),b["name"]!.ToString()));foreach(var e in enemies)enemy.AddItem(e["name"]+"  /  "+e["id"]);
  enemy.ItemSelected+=i=>Safe(()=>SelectEnemy((int)i));motion.ItemSelected+=i=>Safe(()=>SelectMotion((int)i));
  tree.ProcessFrame+=Process;stage.Resized+=Refresh;SelectEnemy(0);
  GD.Print("MOTION_DEBUG_READY profiles="+enemies.Count+" save_session=false");
  if(OS.GetCmdlineUserArgs().Contains("--motion-debug-selftest"))SelfTest();
  } catch { Cleanup(); throw; }
 }
 Button Button(string text,Action action){var b=new Button{Text=text,CustomMinimumSize=new Vector2(140,58)};b.Pressed+=()=>Safe(action);return b;}
 string Label(string s)=>labels.TryGetValue(s,out var x)?x:s;
 void SelectEnemy(int index){playing=false;enemy.Select(index);picture.Texture=null;if(actor!=null){actor.DebugDisposeFrames();actor.Free();}id=enemies[index]["id"]!.ToString();actor=new BattleActor{Visible=false};owner.AddChild(actor);actor.Configure("enemies",id,enemies[index]["name"]!.ToString(),Vector3.Zero);clips=actor.DebugClipSet;actor.SetSourceCanvasMotionScale(1);baseSpriteBasis=actor.Sprite.Basis;choices.Clear();motion.Clear();choices.Add(("idle",null));motion.AddItem("待機  /  実行時モーション");foreach(var m in enemies[index]["moves"]!.OfType<JObject>()){choices.Add(("@move",m));motion.AddItem(Kind(m["kind"]!.ToString())+"："+(moveLabels.TryGetValue(m["id"]!.ToString(),out var ml)?ml:m["id"]!.ToString())+" ["+m["id"]+"]  /  実行時プレビュー");}foreach(var k in clips.Textures.Keys){choices.Add((k,null));motion.AddItem("個別素材："+Label(k)+"  ["+k+"]");}SelectMotion(0);GD.Print("MOTION_DEBUG_SELECT "+id+" clips="+clips.Textures.Count);}
 static string Kind(string k)=>k switch{"attack"=>"攻撃","defend"=>"防御","buff"=>"強化","hex"=>"状態付与","heal"=>"回復","summon"=>"召喚","rest"=>"休息",_=>"特殊行動 ("+k+")"};
 void SelectMotion(int i){motion.Select(i);(key,move)=choices[i];Restart();}
 void Restart(){playing=false;time=0;tick=0;accumulator=0;nextContact=0;released=false;ranged=false;actor!.DebugResetMotion();support=default;
  if(move!=null){var kind=move["kind"]!.ToString();var name=move["id"]!.ToString();int hits=(int?)move["hits"]??1;duration=.5f+.18f*(hits-1)+.9f;if(kind=="attack"){var profiles=JObject.Parse(EnemyResourceFile.ReadAllText("res://assets/presentation-data/card-vfx-profiles.json"));var entry=profiles["_enemyMoves"]?[id+"/"+name];ranged=entry?["requiresExecutedMoveMetadata"]==null&&(entry?["style"]?.ToString() is "beam" or "throw");actor.BeginEnemyAttack(0,.5f,0,name,ranged);if(!ranged&&!MimicAuthoredCountProof.Supports(id)){actor.BeginSourceLunge(new Vector2(-90,12),0,0,.28f,.5f-.28f*.35f,.18f*(hits-1));actor.UiLunge.ArmContactHold();}}else {support=actor.BeginStyle04Support(kind,name,0,0);duration=2;}}
  else duration=key=="idle"?2:clips!.Durations[key].Sum();
  note.Text=move==null?"個別素材・待機：本番と同じ承認済み画像とフレーム時間。足元の登録位置を固定。":"本番のモーションと移動サンプラーで再生（シェーダー・戦闘エフェクトは対象外）。接触はプレビュー用時刻で模擬（接触0.5秒後／連打0.18秒間隔／遠隔発射0.35秒後）。\n戦闘結果・ダメージ・状態付与・召喚・形態遷移は実行しません。適用後の姿は「個別素材」で確認できます。";
  if(move!=null&&MimicAuthoredCountProof.Supports(id))note.Text+="\nこの敵の攻撃は実戦証明が必要なため個別素材で確認してください。";
  Sample();Refresh();}
 void Advance(){if(time+Step>=duration){if(loop.ButtonPressed){bool p=playing;double remaining=accumulator;Restart();accumulator=remaining;playing=p;return;}time=duration;playing=false;}else time+=Step;tick++;Sample();}
 void Sample(){if(actor==null||clips==null)return;if(move!=null){string name=move["id"]!.ToString();string kind=move["kind"]!.ToString();int hits=(int?)move["hits"]??1;if(kind=="attack"){if(ranged&&!released&&time>=.35){actor.PreviewEnemyAttack(.35f,.5f,0,name,true);actor.BeginSourceLunge(new Vector2(18,0),.35f,tick,.2f);released=true;}while(nextContact<hits&&time>=.5+.18*nextContact){float at=.5f+.18f*nextContact;if(!ranged)actor.PreviewEnemyAttack(at,at,0,name);actor.ContactEnemyAttack(at,0,name,nextContact,hits);nextContact++;if(nextContact==hits&&!ranged)actor.UiLunge.ReleaseContactHold(at,tick);}}else if(time>=.9&&support.IsValid){actor.EndStyle04Support(support,.9f);support=default;}actor.UpdateVisual((float)time,presentationFrame:tick,frameDelta:(float)Step);picture.Texture=actor.Sprite.Texture;}else {var ds=clips.Durations[key];double t=time;float total=ds.Sum();if(key=="idle")t%=total;int frame=0;while(frame<ds.Length-1&&t>=ds[frame])t-=ds[frame++];picture.Texture=clips.Textures[key][frame];} }
 void Refresh(){if(closed)return;play.Text=playing?"一時停止":"再生";state.Text=$"  {time:0.000}秒 / {duration:0.000}秒  [{tick}f]";if(picture.Texture==null||clips==null)return;var c=clips.Canvas;float scale=Math.Min(3,Math.Min((stage.Size.X-80)/c.FrameWidth,(stage.Size.Y*.78f)/c.FrameHeight));picture.Size=new Vector2(c.FrameWidth,c.FrameHeight)*scale;picture.PivotOffset=new Vector2(c.AnchorX,c.AnchorY)*scale;picture.Position=new Vector2(stage.Size.X*.5f-c.AnchorX*scale,stage.Size.Y*.84f-c.AnchorY*scale);
  picture.Scale=Vector2.One;picture.Rotation=0;if(move!=null&&actor!=null){var delta=actor.Position-actor.Home;picture.Position+=new Vector2(delta.X,-delta.Y);var basis=actor.Sprite.Basis;picture.Scale=new Vector2(basis.X.Length()/baseSpriteBasis.X.Length(),basis.Y.Length()/baseSpriteBasis.Y.Length());picture.Rotation=-(basis.GetEuler().Z-baseSpriteBasis.GetEuler().Z);}}
 void Elapse(double dt){if(playing){double rate=new[]{.25,.5,1,2}[speed.Selected];accumulator+=Math.Min(.25,dt)*rate;while(accumulator+1e-10>=Step&&playing){accumulator-=Step;Advance();}}}
 void Process(){if(closed)return;if(Input.IsActionJustPressed("ui_cancel")){Close();return;}Safe(()=>{Elapse(owner.GetProcessDeltaTime());Refresh();
  bool capture=Input.IsPhysicalKeyPressed(Key.F12);if(capture&&!captureDown&&captures<3){var arg=OS.GetCmdlineUserArgs().FirstOrDefault(x=>x.StartsWith("--motion-debug-capture-dir="));if(arg!=null){var dir=arg.Substring("--motion-debug-capture-dir=".Length);Directory.CreateDirectory(dir);using var img=owner.GetViewport().GetTexture().GetImage();var path=Path.Combine(dir,$"motion-menu-{++captures:00}.png");img.SavePng(path);GD.Print("MOTION_DEBUG_CAPTURE "+path);}}captureDown=capture;
 });}
 void SelfTest(){
  try {
   void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
   Check(enemies.Count==89,"profile count");int totalClips=0;
   for(int i=0;i<enemies.Count;i++){SelectEnemy(i);Check(clips!=null&&actor!.AuthoredFramesEnabled,"authored load "+id);totalClips+=clips!.Textures.Count;foreach(var c in choices.Where(x=>x.move==null))Check(clips.Textures.ContainsKey(c.key),"clip selection");}
   SelectEnemy(enemies.FindIndex(x=>x["id"]!.ToString()=="enemy_wolf"));SelectMotion(choices.FindIndex(x=>x.move?["id"]?.ToString()=="flurry"));
   Elapse(.2);Check(time==0,"pause drift");Advance();Check(Math.Abs(time-Step)<1e-8&&tick==1,"single frame");Restart();speed.Select(0);playing=true;Elapse(.2);Check(tick==3,"quarter speed");Restart();speed.Select(3);playing=true;Elapse(.2);Check(tick==24,"double speed");
   Restart();speed.Select(2);playing=true;loop.ButtonPressed=false;for(int i=0;i<180;i++)Advance();Check(!playing&&Math.Abs(time-duration)<1e-6,"non-loop end");
   Restart();loop.ButtonPressed=true;for(int i=0;i<180;i++)Advance();Check(time<duration,"loop wrap");
   Restart();loop.ButtonPressed=false;for(int i=0;i<31;i++)Advance();Check(actor!.AuthoredFrameSample.AcceptedContacts==1,"runtime contact preview");Check(actor.UiLunge.Offset.Length()>1,"sampled root travel");for(int i=0;i<100;i++)Advance();Check(actor.UiLunge.Offset.Length()<.001,"sampled root recovery");
   Check(owner.GetChildren().All(n=>n.Name!="Session"),"no GameSession");
   Cleanup();Check(tree.Paused==previousPause&&!owner.HasMeta("motion_debug_open"),"close restores pause");
   GD.Print("MOTION_DEBUG_SELFTEST_PASS "+Newtonsoft.Json.JsonConvert.SerializeObject(new{profiles=enemies.Count,totalClips,pause=true,step=true,speeds=true,loop=true,runtimeContact=true,noGameSession=true}));tree.Quit();
  }catch(Exception e){GD.PushError("MOTION_DEBUG_SELFTEST_FAIL "+e);tree.Quit(1);}
 }
 void Safe(Action action){try{action();}catch(Exception e){GD.PushError("Motion preview closed safely: "+e);Cleanup();if(direct)tree.Quit(1);}}
 void OwnerExiting()=>Cleanup();
 void Cleanup(){if(closed)return;closed=true;tree.ProcessFrame-=Process;owner.TreeExiting-=OwnerExiting;picture.Texture=null;if(actor!=null&&GodotObject.IsInstanceValid(actor)){actor.DebugDisposeFrames();actor.QueueFree();}if(GodotObject.IsInstanceValid(layer))layer.QueueFree();owner.RemoveMeta("motion_debug_open");tree.Paused=previousPause;}
 void Close(){Cleanup();if(direct)tree.Quit();}
}
#endif
