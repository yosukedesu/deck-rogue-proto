#if RECOVERY_QA
// Test-only fixture loader and passive actual-pointer receipts. No synthetic UI input.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.Debugging;
using DeckRogue.GodotPort.Presentation;
using DeckRogue.GodotPort.UI;
using Godot;
using Newtonsoft.Json;
namespace DeckRogue.GodotPort.RecoveryQA;
public static class UnityUiManualQa {
 static Node main;static BattleStage stage;static GameUI ui;static GameSession session;static AgentCapture capture;static string output,fixture;static bool f8,f9;static int shot;
 static readonly HashSet<ulong> hooked=new();
 public static void Start(Node root){var args=OS.GetCmdlineUserArgs();if(!args.Contains("--unity-ui-manual"))return;Callable.From((Action)(async()=>await Begin(root,args))).CallDeferred();}
 static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
 static async Task Begin(Node root,string[] args){main=root;try{
  string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new Exception("Missing "+key);return args[i+1];}
  output=Arg("--parity-out");fixture=Arg("--parity-fixture");Directory.CreateDirectory(output);if(System.IO.File.Exists(Path.Combine(output,"pointer-events.jsonl")))throw new Exception("Use fresh output directory");
  var win=root.GetTree().Root;win.ContentScaleSize=new(1600,900);win.ContentScaleMode=Window.ContentScaleModeEnum.CanvasItems;win.ContentScaleAspect=Window.ContentScaleAspectEnum.Expand;win.Size=new(1170,540);
  for(int i=0;i<6;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
  stage=root.GetNode<BattleStage>("BattleStage");ui=root.GetNode<GameUI>("Interface/GameUI");session=new GameSession{Name="UiParityManualSession",AutoSave=false,SaveDirectory=Path.Combine(output,"isolated-save")};root.AddChild(session);stage.Bind(session);ui.Bind(session);capture=root.GetNode<AgentCapture>("AgentCapture");capture.Bind(session,()=>stage.IsPresenting);stage.PlaybackPaused=false;
  session.StateChanged+=c=>{Log("state-change",new{c.Revision,command=c.Command?.GetType().Name,c.IsReset});Callable.From(Attach).CallDeferred();};session.Diagnostic+=s=>Log("diagnostic",new{message=s});
  Load();root.GetTree().NodeAdded+=n=>{if(n is Control)Callable.From(Attach).CallDeferred();};root.GetTree().ProcessFrame+=Poll;Attach();Log("ready",new{module=typeof(UnityUiManualQa).Assembly.ManifestModule.ModuleVersionId,fixture,keys="F8 reloads provided synthetic fixture only; F9 captures image plus controls/state. Actual pointer only."});GD.Print("UNITY_UI_MANUAL_READY "+output);
 }catch(Exception e){GD.PushError("UNITY_UI_MANUAL_FAILED "+e);}}
 static void Load(){var text=System.IO.File.ReadAllText(fixture);var result=session.Load(text);if(!result.Success)throw new Exception(result.Error);ui.PresentationCompleted();if(session.CurrentRun?.Combat?.Enemies.Count>1)ui.SelectEnemy(1);System.IO.File.WriteAllText(Path.Combine(output,"current-fixture.json"),text);Log("fixture-loaded",new{fixtureSha=Hash(text)});}
 static void Poll(){bool a=Input.IsPhysicalKeyPressed(Key.F8),b=Input.IsPhysicalKeyPressed(Key.F9);if(a&&!f8)try{Load();}catch(Exception e){Log("fixture-error",new{error=e.Message});}if(b&&!f9)Dump();f8=a;f9=b;}
 static IEnumerable<Node> Desc(Node n){foreach(Node c in n.GetChildren()){yield return c;foreach(var d in Desc(c))yield return d;}}
 static void Attach(){foreach(var c in Desc(ui).OfType<Control>()){if(!hooked.Add(c.GetInstanceId()))continue;string path=c.GetPath().ToString();string text=c is Button b?b.Text:c is Label l?l.Text:"";if(c is Button button)button.Pressed+=()=>Log("button-pressed",new{path,text});if(c is CardTile tile)tile.MouseEntered+=()=>Log("card-hover",new{uid=tile.CardUid,index=tile.GetIndex()});c.GuiInput+=e=>{if(e is InputEventMouseButton m)Log(m.Pressed?"pointer-down":"pointer-up",new{path,text,button=m.ButtonIndex.ToString(),local=m.Position.ToString(),global=ui.GetGlobalMousePosition().ToString()});};}}
 static void Dump(){string name="manual-"+(++shot).ToString("000");capture.Capture(name);var controls=Desc(ui).OfType<Control>().Where(c=>c.IsVisibleInTree()).Select(c=>new{path=c.GetPath().ToString(),type=c.GetType().Name,rect=c.GetGlobalRect().ToString(),text=c is Button b?b.Text:c is Label l?l.Text:c is RichTextLabel r?r.GetParsedText():"",mouse=c.MouseFilter.ToString()}).ToArray();System.IO.File.WriteAllText(Path.Combine(output,name+"-controls.json"),JsonConvert.SerializeObject(new{controls,stateSha=Hash(session.ExportSave()),session.Revision,ui.EnemyHeadReadbacks,ui.EnemyHeadCacheHits},Formatting.Indented));System.IO.File.WriteAllText(Path.Combine(output,name+"-save.json"),session.ExportSave());Log("capture",new{name});}
 static void Log(string kind,object detail){string save=session?.ExportSave()??"";var run=session?.CurrentRun;var row=new{utc=DateTime.UtcNow.ToString("O"),processFrame=Godot.Engine.GetProcessFrames(),kind,revision=session?.Revision,stateSha=Hash(save),phase=run?.Phase,heroHp=run?.Combat?.Player.Hp,energy=run?.Combat?.Player.Energy,mana=run?.Mana,detail};System.IO.File.AppendAllText(Path.Combine(output,"pointer-events.jsonl"),JsonConvert.SerializeObject(row)+"\n");}
}
#endif
