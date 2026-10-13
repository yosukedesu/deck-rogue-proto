#if RECOVERY_QA
// New QA only. Native sequential frame recording through the normal process/render loop.
// --fixed-fps 60 controls simulation cadence; this is not a real-time performance result.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.Presentation;
using DeckRogue.GodotPort.UI;
using Godot;
using Newtonsoft.Json;
namespace DeckRogue.GodotPort.RecoveryQA;
public static class UnityParitySequenceQa {
 static Node main; static BattleStage stage; static GameUI ui; static GameSession session;
 static string output; static int frame=-1, count=390; static bool active,stopped; static ulong lastProcess;static long wallStart;
 static readonly List<object> contacts=new(); static readonly List<string> diagnostics=new();
 static StreamWriter stream; static string card="green_strike"; static bool realTime;
 const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static object F(object obj,string name)=>obj?.GetType().GetField(name,Fields)?.GetValue(obj);
 static object Vec(Vector3 v)=>new[]{v.X,v.Y,v.Z};static object Vec2(Vector2 v)=>new[]{v.X,v.Y};
 static IEnumerable<Node> Desc(Node n){foreach(Node c in n.GetChildren()){yield return c;foreach(var d in Desc(c))yield return d;}}
 static string Sha(string path)=>Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(path))).ToLowerInvariant();
 static void Save(string name,object data)=>System.IO.File.WriteAllText(Path.Combine(output,name),JsonConvert.SerializeObject(data,Formatting.Indented));
 public static void Start(Node root){var args=OS.GetCmdlineUserArgs();if(!args.Contains("--unity-parity-sequence"))return;Callable.From((Action)(async()=>await Begin(root,args))).CallDeferred();}
 static async Task Begin(Node root,string[] args){main=root;try{
  string Arg(string key,string fallback){int i=Array.IndexOf(args,key);return i>=0?args[i+1]:fallback;}
  output=Arg("--parity-out",ProjectSettings.GlobalizePath("user://unity-parity"));card=Arg("--parity-card","green_strike");realTime=args.Contains("--parity-real-time");count=int.Parse(Arg("--parity-frames","390"));
  Directory.CreateDirectory(output);if(System.IO.File.Exists(Path.Combine(output,"frames.jsonl")))throw new Exception("Use fresh output directory");
  if(DisplayServer.GetName()=="headless")throw new Exception("Native display required for sequence recording");
  var win=main.GetTree().Root;win.ContentScaleSize=new(1600,900);win.ContentScaleMode=Window.ContentScaleModeEnum.CanvasItems;win.ContentScaleAspect=Window.ContentScaleAspectEnum.Expand;win.Size=new(1170,540);
  for(int i=0;i<6;i++)await main.ToSignal(main.GetTree(),SceneTree.SignalName.ProcessFrame);
  stage=main.GetNode<BattleStage>("BattleStage");ui=main.GetNode<GameUI>("Interface/GameUI");
  session=new GameSession{Name="UnityParitySession",SaveDirectory=Path.Combine(output,"isolated-save"),AutoSave=false};main.AddChild(session);session.Diagnostic+=diagnostics.Add;stage.Bind(session);ui.Bind(session);
  var run=DeckRogue.Engine.Run.CreateDebugCheckpointRun(4242,"set-confirm","leader_green",new ReplayOriginCheckpoint{Act=1,DeckId="starter",Difficulty=3,HpRatio=.6,Gold=150});
  if(run.Phase==RunPhases.Departure)run=DeckRogue.Engine.Run.ApplyRunCommand(run,new RunCommand_LeaveDeparture());
  run=DeckRogue.Engine.Run.DebugLaunchCombat(run,"enc_mudling_trio");
  string[] ids={card,"green_guard","green_growth_ring","green_reaction_thorns","green_double_lash"};
  run=run with{Combat=run.Combat with{Player=run.Combat.Player with{Energy=3,Block=3,Hand=ids.Select((id,i)=>new CardInstance{Uid=id+"#dbgh"+i,Def=Content.GetCardDef(id)}).ToArray()}}};
  var save=Report.SerializeRunSaveFile(new RunSaveFile{Run=run,Fingerprint=session.Catalog.Fingerprint});System.IO.File.WriteAllText(Path.Combine(output,"fixture-save.json"),save);
  var result=session.Load(save);if(!result.Success)throw new Exception(result.Error);ui.SelectEnemy(1);stage.PlaybackPaused=false;
  for(int i=0;i<90;i++)await main.ToSignal(main.GetTree(),SceneTree.SignalName.ProcessFrame);
  Save("run-identity.json",new{schema="unity-parity-native-sequence/1",baseCommit="bd66ae1655023fa3bc57f89ed217ce404fcdf54b",assemblyMvid=typeof(UnityParitySequenceQa).Assembly.ManifestModule.ModuleVersionId,assemblySha256=Sha(ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/DeckRogue.dll")),reference="a7f86b5 ux-target3-video",referenceTimeOffset=4.6,renderCadence=realTime?"measured live host delta":"--fixed-fps 60 (not realtime)",everyNativeFrame=true,width=1170,height=540,card,fixtureSha256=Sha(Path.Combine(output,"fixture-save.json")),actions=new[]{new{frame=30,referenceFrame=306,command="PlayCard target1"},new{frame=123,referenceFrame=399,command="EndTurn"}},input="Original command dispatch; this motion test does not claim pointer input or uxdrag parity"});
  stage.NumericContactPresented+=e=>contacts.Add(new{frame,stage.PresentationTime,kind="numeric",type=e.Type,eventData=JsonConvert.SerializeObject(e)});
  stage.EventPresented+=e=>contacts.Add(new{frame,stage.PresentationTime,kind="event",type=e.Type,eventData=JsonConvert.SerializeObject(e)});
  stream=new StreamWriter(Path.Combine(output,"frames.jsonl")){AutoFlush=true};wallStart=System.Diagnostics.Stopwatch.GetTimestamp();active=true;
  main.GetTree().ProcessFrame+=OnProcess;RenderingServer.FramePostDraw+=OnDraw;
 }catch(Exception e){Fail(e);}}
 static void OnProcess(){if(!active)return;try{frame++;if(frame==30)Dispatch(new RunCommand_Combat{Command=new Command_PlayCard{CardUid=card+"#dbgh0",TargetIndex=1}});if(frame==123)Dispatch(new RunCommand_Combat{Command=new Command_EndTurn()});}catch(Exception e){Fail(e);}}
 static void Dispatch(RunCommand command){var r=session.Dispatch(command);if(!r.Success)throw new Exception("Command rejected: "+r.Error);}
 static object Surface(BattleActor a){var m=F(a,"_surface") as ShaderMaterial;if(m==null)return null;return new{shader=m.Shader?.ResourcePath,flash=m.GetShaderParameter("_Flash").AsSingle(),lightCount=m.GetShaderParameter("_ActorLightCount").AsInt32(),positions=(F(a,"_lightPositions") as Vector4[])?.Select(v=>new[]{v.X,v.Y,v.Z,v.W}).ToArray(),colors=(F(a,"_lightColors") as Vector4[])?.Select(v=>new[]{v.X,v.Y,v.Z,v.W}).ToArray()};}
 static void OnDraw(){if(!active||frame<0)return;try{
  ulong process=Godot.Engine.GetProcessFrames();if(lastProcess!=0&&process!=lastProcess+1)throw new Exception($"Nonconsecutive native frame {lastProcess}->{process}");lastProcess=process;
  using var image=main.GetViewport().GetTexture().GetImage();if(image.GetWidth()!=1170||image.GetHeight()!=540)throw new Exception("Native viewport size mismatch");
  string png=$"frame-{frame:0000}.png";if(image.SavePng(Path.Combine(output,png))!=Error.Ok)throw new Exception("PNG save failed");
  var actors=Desc(stage).OfType<BattleActor>().Select(a=>new{a.DefinitionId,a.EnemyIndex,a.PoseName,a.PoseFrame,a.PoseFrameTime,poseClock=F(a,"_poseClock"),texture=a.Sprite?.Texture?.ResourcePath,a.Motion.MotionVersion,a.Motion.RecoilVersion,a.Motion.ApproachElapsed,a.Motion.RecoilElapsed,approach=Vec(a.Motion.ApproachOffset),recoil=Vec(a.Motion.RecoilOffset),pause=a.Motion.PauseRemaining(stage.PresentationTime),feet=Vec(a.SourceMotionFeet),position=Vec(a.Position),executionScale=Vec2(a.ExecutionScale),a.DissolveAmount,surface=Surface(a)}).ToArray();
  var cam=Desc(stage).OfType<Camera3D>().FirstOrDefault();
  stream.WriteLine(JsonConvert.SerializeObject(new{frame,png,sha256=Sha(Path.Combine(output,png)),processFrame=process,drawnFrame=Godot.Engine.GetFramesDrawn(),presentationFrame=F(stage,"_presentationFrame"),hostDelta=F(stage,"_lastHostDelta"),clock=stage.PresentationTime,wallSeconds=(System.Diagnostics.Stopwatch.GetTimestamp()-wallStart)/(double)System.Diagnostics.Stopwatch.Frequency,stage.IsPresenting,stage.InputLocked,stage.ActiveEffectCount,stage.ActiveMotionCount,stage.ActivePhaseBanner,stage.ActivePhaseBannerAlpha,session.Revision,phase=session.CurrentRun?.Phase,energy=session.CurrentRun?.Combat?.Player.Energy,heroHp=session.CurrentRun?.Combat?.Player.Hp,enemyHp=session.CurrentRun?.Combat?.Enemies.Select(e=>e.Hp).ToArray(),actors,camera=cam==null?null:new{position=Vec(cam.Position),rotation=Vec(cam.Rotation),cam.Size},contacts=contacts.ToArray()}));contacts.Clear();
  if(frame%30==0)Save("progress.json",new{frame,count,clock=stage.PresentationTime});
  if(frame+1>=count){active=false;stream.Dispose();Save("results.json",new{passed=true,frames=count,consecutiveFrames=true,diagnostics,phase=session.CurrentRun?.Phase,turn=session.CurrentRun?.Combat?.Turn,remainingPresentation=stage.IsPresenting,scope="Consecutive native frames, normal process/render loop. Fixed-fps capture is not live performance or complete Unity parity."});main.GetTree().Quit(0);}
 }catch(Exception e){Fail(e);}}
 static void Fail(Exception e){if(stopped)return;stopped=true;active=false;stream?.Dispose();if(output!=null)Save("results.json",new{passed=false,frame,error=e.ToString(),diagnostics});GD.PrintErr(e);main.GetTree().Quit(1);}
}
#endif
