// Optional read-only instrumentation. No framebuffer/texture readbacks, rule access,
// rendering changes, synchronous per-frame file writes, or persistent enable flag.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DeckRogue.GodotPort.Presentation;
using Godot;
using Newtonsoft.Json;
namespace DeckRogue.GodotPort.Debugging;
public sealed class PresentationDiagnostics : IDisposable {
 const int Capacity=36000,MaxEvents=16000;
 readonly BattleStage stage; readonly Node root; readonly Func<object> extra;
 readonly List<object> events=new();readonly Dictionary<ulong,Pose> poses=new();readonly List<Sample> samples=new();
 long started,last;string renderer,driver,adapter;bool limited; int sampleIndex;
 readonly record struct Pose(string Name,int Frame,int Starts,int Motion,int Recoil,bool Paused);
 readonly record struct Sample(int N,int RenderFrame,double IntervalMs,float Clock,string Quality,bool Busy,bool Paused,bool Focused);
 public bool Enabled {get;private set;} public string LastFile {get;private set;}="";
 public event Action<string> Status;
 public PresentationDiagnostics(Node root,BattleStage stage,Func<object> extra=null){this.root=root;this.stage=stage;this.extra=extra;}
 public void SetEnabled(bool on){if(on==Enabled)return;if(on)Start();else Stop();}
 void Start(){
  samples.Clear();events.Clear();poses.Clear();limited=false;sampleIndex=0;
  renderer=RenderingServer.GetCurrentRenderingMethod();driver=RenderingServer.GetCurrentRenderingDriverName();adapter=RenderingServer.GetVideoAdapterName();
  started=last=Stopwatch.GetTimestamp();Enabled=true;RenderingServer.FramePostDraw+=SampleFrame;Status?.Invoke("描画診断: 記録中（画像取得なし）");
 }
 void SampleFrame(){if(!Enabled)return;try{
  long now=Stopwatch.GetTimestamp();double dt=(now-last)*1000.0/Stopwatch.Frequency;last=now;
  bool paused=false;SampleActors(stage.GetNodeOrNull<Node3D>("Combatants"),ref paused);SampleActors(stage.GetNodeOrNull<Node3D>("RetiredCombatants"),ref paused);
  samples.Add(new Sample(sampleIndex++,Godot.Engine.GetFramesDrawn(),dt,stage.PresentationTime,stage.EffectiveQualityProfile,stage.IsPresenting,paused,DisplayServer.WindowIsFocused()));
  if(samples.Count>=Capacity)Stop();
 }catch(Exception e){Enabled=false;RenderingServer.FramePostDraw-=SampleFrame;Status?.Invoke("描画診断: 記録を停止しました");GD.PushWarning("Presentation diagnostics stopped: "+e.Message);}}
 void SampleActors(Node3D container,ref bool anyPaused){if(container==null)return;for(int i=0;i<container.GetChildCount();i++)if(container.GetChild(i) is BattleActor a){
  bool paused=a.Motion.IsPaused(stage.PresentationTime);anyPaused|=paused;var current=new Pose(a.PoseName,a.PoseFrame,a.PoseStartCount,a.Motion.MotionVersion,a.Motion.RecoilVersion,paused);ulong id=a.GetInstanceId();
  if(!poses.TryGetValue(id,out var prior)||prior!=current){if(events.Count<MaxEvents)events.Add(new{frame=sampleIndex,clock=stage.PresentationTime,actor=a.DefinitionId,instance=id,pose=current.Name,spriteFrame=current.Frame,poseStarts=current.Starts,restart=prior.Starts!=current.Starts,motion=current.Motion,recoil=current.Recoil,hitpause=paused,pauseRemaining=a.Motion.PauseRemaining(stage.PresentationTime)});else limited=true;poses[id]=current;}
 }}
 void Stop(){if(!Enabled)return;Enabled=false;RenderingServer.FramePostDraw-=SampleFrame;try{
  double[] times=samples.Skip(1).Select(x=>x.IntervalMs).OrderBy(x=>x).ToArray();double P(double q)=>times.Length==0?0:times[Math.Min(times.Length-1,(int)Math.Floor(q*(times.Length-1)))];
  var bounds=new[]{8.34,11.12,16.68,22.23,33.35,50.0,100.0,double.PositiveInfinity};double lower=0;var bins=new List<object>();foreach(double upper in bounds){bins.Add(new{greaterThanMs=lower,upToMs=double.IsFinite(upper)?(double?)upper:null,count=times.Count(x=>x>lower&&x<=upper)});lower=upper;}
  string folder=ProjectSettings.GlobalizePath("user://presentation-diagnostics");Directory.CreateDirectory(folder);LastFile=Path.Combine(folder,"presentation-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+".json");
  System.IO.File.WriteAllText(LastFile,JsonConvert.SerializeObject(new{schema="deckrogue-presentation-diagnostics/1",moduleMvid=typeof(PresentationDiagnostics).Assembly.ManifestModule.ModuleVersionId,recordedUtc=DateTime.UtcNow.ToString("O"),renderer,driver,adapter,selectedQuality=stage.QualityProfile,effectiveQuality=stage.EffectiveQualityProfile,qualityFrames=samples.GroupBy(s=>s.Quality).Select(g=>new{quality=g.Key,frames=g.Count()}),fixedFps=OS.GetCmdlineArgs().Contains("--fixed-fps"),captureActive=OS.GetCmdlineUserArgs().Contains("--unity-parity-sequence"),manualQaFixture=OS.GetCmdlineUserArgs().Contains("--unity-ui-manual"),frameCount=samples.Count,intervals=new{meanMs=times.Length>0?times.Average():0,p50Ms=P(.50),p95Ms=P(.95),p99Ms=P(.99),maxMs=P(1),histogram=bins},spriteEventLimitReached=limited,extra=extra?.Invoke(),frames=samples,spriteTransitions=events,scope="Natural host render-completion intervals. Instrumentation has overhead; no screenshot/texture readback. Intentional stepped sprite frames and hit-pause are logged separately. No rule state or personal data is exported."},Formatting.Indented));
  Status?.Invoke("描画診断: 保存しました（"+samples.Count+"フレーム）");GD.Print("PRESENTATION_DIAGNOSTICS_SAVED "+LastFile);
 }catch(Exception e){Status?.Invoke("描画診断: 保存できませんでした");GD.PushWarning("Presentation diagnostics save failed: "+e.Message);}}
 public void Dispose(){if(Enabled)Stop();}
}
