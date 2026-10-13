#if RECOVERY_QA
#if RECOVERY_QA
// NEW reconstruction QA, not a recovered original harness. Normal Main, actual
// native pointer input only. Synthetic H10 fixture and logs are isolated here.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.UI;
using DeckRogue.GodotPort.Presentation;
using DeckRogue.GodotPort.Debugging;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace DeckRogue.GodotPort.RecoveryQA;
public static class RecoveryHandQa
{
    private static readonly HashSet<ulong> Attached=new();
    private static GameUI _ui;private static GameSession _session;private static string _out;
    public static void Start(Node main)
    {
        var args=OS.GetCmdlineUserArgs();if(!args.Contains("--recovery-hand10-manual"))return;
        Callable.From(()=>Begin(main,args)).CallDeferred();
    }
    private static void Begin(Node main,string[] args)
    {
        try{
            string Arg(string key){int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length)throw new ArgumentException("Missing "+key);return args[i+1];}
            _out=Arg("--recovery-qa-out");Directory.CreateDirectory(_out);
            if(System.IO.File.Exists(Path.Combine(_out,"pointer-events.jsonl")))throw new InvalidOperationException("Use a new output directory.");
            var root=JObject.Parse(System.IO.File.ReadAllText(Arg("--recovery-hand-fixture")));var run=(JObject)root["run"];var combat=(JObject)run["combat"];var player=(JObject)combat["player"];
            string[] ids={"green_strike","green_guard","green_growth_ring","green_reaction_thorns","green_double_lash","green_strike","green_guard","green_growth_ring","green_reaction_thorns","green_double_lash"};
            player["hand"]=new JArray(ids.Select((id,i)=>JObject.FromObject(new CardInstance{Uid="recovery_h10_"+i.ToString("00")+"_"+id,Def=Content.GetCardDef(id)})));
            player["energy"]=10;player["energyMax"]=10;player["block"]=0;player["iceBlock"]=0;player["impulseUids"]=new JArray();combat["phase"]="player-turn";combat["pendingScry"]=null;combat["enemyPhase"]=false;
            _session=new GameSession{Name="HandQaSession",SaveDirectory=Path.Combine(_out,"isolated-session"),AutoSave=false};main.AddChild(_session);
            var stage=main.GetNode<BattleStage>("BattleStage");_ui=main.GetNode<GameUI>("Interface/GameUI");stage.Bind(_session);_ui.Bind(_session);stage.PlaybackPaused=false;
            main.GetNodeOrNull<AgentCapture>("AgentCapture")?.Bind(_session,()=>stage.IsPresenting);
            _session.StateChanged+=change=>{Log("state-change",null,new{change.Revision,command=change.Command?.GetType().Name});Callable.From(Attach).CallDeferred();};
            _session.Diagnostic+=message=>Log("diagnostic",null,new{message});
            var result=_session.Load(root.ToString(Formatting.None));if(!result.Success)throw new Exception(result.Error);
            System.IO.File.WriteAllText(Path.Combine(_out,"fixture-save.json"),_session.ExportSave());
            Callable.From(()=>{Attach();Log("ready",null,new{nativeWindow=main.GetTree().Root.Size.ToString(),logicalViewport=_ui.Size.ToString(),instructions="Actual pointer only: hover leftmost, press it, cancel or use, move off; logs retain exact tile UID. No automated input and no autoquit."});GD.Print("RECOVERY_HAND_QA_READY "+_out);}).CallDeferred();
        }catch(Exception ex){GD.PushError("RECOVERY_HAND_QA_FAILED "+ex);}
    }
    private static void Attach()
    {
        foreach(var item in _ui.LiveHandTiles){var tile=item.Value;string uid=item.Key;if(!Attached.Add(tile.GetInstanceId()))continue;
            tile.MouseEntered+=()=>Log("hover-enter",uid,new{index=tile.GetIndex(),rect=tile.GetGlobalRect().ToString()});
            tile.MouseExited+=()=>Log("hover-exit",uid,new{index=tile.GetIndex(),order=_ui.LiveHandTiles.Values.OrderBy(t=>t.GetIndex()).Select(t=>t.CardUid).ToArray()});
            tile.GuiInput+=input=>{if(input is InputEventMouseButton mouse)Log(mouse.Pressed?"pointer-down":"pointer-up",uid,new{button=mouse.ButtonIndex.ToString(),local=mouse.Position.ToString(),global=_ui.GetGlobalMousePosition().ToString(),index=tile.GetIndex()});else if(input is InputEventScreenTouch touch)Log(touch.Pressed?"touch-down":"touch-up",uid,new{touch.Index,position=touch.Position.ToString()});};
            tile.Pressed+=()=>Log("pressed",uid,new{index=tile.GetIndex(),aim=_ui.AimActive});
        }
    }
    private static void Log(string kind,string uid,object detail)
    {
        string save=_session?.ExportSave()??"";var state=_session?.CurrentRun?.Combat;
        var line=new{schema="recovery-native-hand-input/1",timeUtc=DateTime.UtcNow.ToString("O"),frame=Godot.Engine.GetProcessFrames(),kind,uid,revision=_session?.Revision,energy=state?.Player.Energy,hand=state?.Player.Hand.Select(c=>c.Uid).ToArray(),saveSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(save))).ToLowerInvariant(),detail};
        System.IO.File.AppendAllText(Path.Combine(_out,"pointer-events.jsonl"),JsonConvert.SerializeObject(line)+"\n");
    }
}
#endif

#endif
