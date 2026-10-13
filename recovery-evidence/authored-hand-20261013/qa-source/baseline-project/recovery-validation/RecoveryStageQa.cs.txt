#if RECOVERY_QA
// Test-only recovery QA. Call Start(main) at END of normal Main._Ready under RECOVERY_QA.
// Uses the real Main BattleStage/UI and an isolated GameSession; never writes normal saves.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.Presentation;
using DeckRogue.GodotPort.UI;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using File = System.IO.File;
using Directory = System.IO.Directory;

namespace DeckRogue.GodotPort.RecoveryQA;

public static class RecoveryStageQa
{
    private static Node _main = null!;
    private static BattleStage _stage = null!;
    private static GameUI _ui = null!;
    private static GameSession _session = null!;
    private static string _out = "";
    private static int _sequence;
    private static readonly List<object> Results = new();
    private static readonly List<string> Diagnostics = new();
    private static int _checks;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string[] ProtectedUniforms = { "_BaseColor", "_Cutoff", "_Flash", "_Dissolve", "_Fog", "_Rim", "_Receive", "_AmbientScale", "_HeroLift", "_KeyFlip", "_LocalLights", "_OutlineFloor", "_CharTint", "_BlackLift", "_WhiteCap", "_CharSat", "_CharKeyColor", "_CharKeyDir", "_RimColor", "_FogEnabled", "_FogColor", "_FogRange", "_ActorLightCount", "_ActorLightPositionRange", "_ActorLightColorKind", "_HasNormal", "_HasEmission" };
    public static void Start(Node main)
    {
        if (!OS.GetCmdlineUserArgs().Contains("--recovery-stage-preview")) return;
        Callable.From((Action)(async () => await RunAsync(main))).CallDeferred();
    }
    private static async Task RunAsync(Node main)
    {
        _main = main;
        try {
            string[] args = OS.GetCmdlineUserArgs();
            int oi = Array.IndexOf(args,"--recovery-qa-out");
            _out = oi >= 0 && oi+1 < args.Length ? args[oi+1] : ProjectSettings.GlobalizePath("user://actor-color-recovery-qa");
            Directory.CreateDirectory(_out);
            if (File.Exists(Path.Combine(_out,"results.json"))) throw new InvalidOperationException("QA output already has results; use a new directory.");
            // Let the native window manager finish launch normalization first.
            await main.ToSignal(main.GetTree().CreateTimer(3.0),SceneTreeTimer.SignalName.Timeout);
            Window root=main.GetTree().Root;
            root.ContentScaleSize=new Vector2I(1600,900);
            root.ContentScaleMode=Window.ContentScaleModeEnum.CanvasItems;
            root.ContentScaleAspect=Window.ContentScaleAspectEnum.Expand;
            root.Size=new Vector2I(1170,540);
            await Frames(5);
            _stage = main.GetNode<BattleStage>("BattleStage");
            _ui = main.GetNode<GameUI>("Interface/GameUI");
            _session = new GameSession { Name="ColorQaSession", SaveDirectory=Path.Combine(_out,"isolated-session"), AutoSave=false };
            main.AddChild(_session);
            Check("isolated-session-ready",_session.IsInitialized);
            _session.Diagnostic += text => Diagnostics.Add(text);
            _stage.Bind(_session); _ui.Bind(_session);
            _stage.PlaybackPaused=true;
            Check("actual-compatibility-renderer",RenderingServer.GetCurrentRenderingMethod()=="gl_compatibility");
            for(int act=1;act<=3;act++) {
                await LoadFixture(act,"leader_green");
                await Capture($"act{act}-idle",false);
                RunState before=_session.CurrentRun!;
                var command=new RunCommand_Combat { Command=new Command_PlayCard { CardUid="qa-strike", TargetIndex=0 } };
                RunState expected=Run.ApplyRunCommand(before,command);
                Check($"act{act}-positive-rule-hit",expected.Combat!.Enemies[0].Hp<before.Combat!.Enemies[0].Hp);
                int contacts=_stage.DamageContactCount;
                Check($"act{act}-actual-command-accepted",_session.Dispatch(command).Success);
                Check($"act{act}-exact-original-rule-outcome",HashRun(_session.CurrentRun!)==HashRun(expected));
                for(int i=0;i<10000 && _stage.DamageContactCount==contacts;i++) { _stage.AdvancePresentation(.001f); if(i%250==249) { Progress($"act{act}-contact-step-{i+1}"); await Frames(1); } }
                Check($"act{act}-actual-contact-reached",_stage.DamageContactCount>contacts);
                await Capture($"act{act}-contact",true);
                while(_stage.IsPresenting) { for(int n=0;n<1000 && _stage.IsPresenting;n++)_stage.AdvancePresentation(1f/120); if(_stage.IsPresenting)throw new Exception("Presentation did not drain in bounded fixture."); }
                _stage.SetActorSourceDisplay(true);
                await Capture($"act{act}-settled",false);
                Check($"act{act}-post-drain-rules-unchanged",HashRun(_session.CurrentRun!)==HashRun(expected));
            }
            Check("no-session-listener-diagnostics",Diagnostics.Count==0);
            WriteResults(true,null);
            GD.Print("RECOVERY_STAGE_QA_PASS checks="+_checks+" output="+_out);
            RenderingServer.RenderLoopEnabled=true;
            main.GetTree().Paused=false;
            main.GetTree().Quit(0);
        } catch(Exception e) {
            try { if(_out.Length>0)WriteResults(false,e.ToString()); }
            catch(Exception reportError) {
                GD.PrintErr("QA_REPORT_FAILED "+reportError);
                try { File.WriteAllText(Path.Combine(_out,"failure.txt"),e+"\nREPORT_FAILURE\n"+reportError); } catch { }
            }
            GD.PrintErr("RECOVERY_STAGE_QA_FAILED "+e);
            RenderingServer.RenderLoopEnabled=true;
            if(GodotObject.IsInstanceValid(main)) {main.GetTree().Paused=false;main.GetTree().Quit(1);}
        }
    }
    private static void WriteResults(bool passed,string? error)
    {
        File.WriteAllText(Path.Combine(_out,"results.json"),JsonConvert.SerializeObject(new { passed,checks=_checks,error,diagnostics=Diagnostics,results=Results,
            provenance="Fresh reconstructed color QA against recovered early runtime; no claim of exact lost b05a0da or Unity Act2/3 optical parity." },Formatting.Indented));
    }
    private static async Task Frames(int n) { for(int i=0;i<n;i++)await _main.ToSignal(_main.GetTree(),SceneTree.SignalName.ProcessFrame); }
    private static void Progress(string operation) {
        if(_out.Length>0)File.WriteAllText(Path.Combine(_out,"progress.json"),JsonConvert.SerializeObject(new {operation,checks=_checks,captures=_sequence,atUtc=DateTime.UtcNow.ToString("O")}));
    }
    private static void Check(string name,bool pass) { _checks++;Results.Add(new {kind="assertion",name,pass});Progress(name+":"+pass);if(!pass)throw new Exception(name); }
    private static IEnumerable<Node> Desc(Node root) { foreach(Node child in root.GetChildren()) {yield return child;foreach(Node next in Desc(child))yield return next;} }
    private static BattleActor[] Actors()=>Desc(_stage).OfType<BattleActor>().Where(a=>a.IsInsideTree()).OrderBy(a=>a.Name.ToString(),StringComparer.Ordinal).ToArray();
    private static StageWorld World()=>Desc(_stage).OfType<StageWorld>().Single();
    private static ShaderMaterial Material(BattleActor actor)=>(ShaderMaterial)actor.Sprite.MaterialOverride;
    private static string Sha(byte[] data)=>Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    private static string HashRun(RunState run)=>Sha(System.Text.Encoding.UTF8.GetBytes(JsonUnions.Serialize(run)));
    private static string SaveHash()=>File.Exists(Path.Combine(_out,"isolated-session/run.json"))?Sha(File.ReadAllBytes(Path.Combine(_out,"isolated-session/run.json"))):"absent";
    private static float Clock()=>(float)(typeof(BattleStage).GetField("_clock",Private)!.GetValue(_stage)!);
    private static RunState Fixture(int act,string leader)
    {
        RunState seed=Run.CreateDebugCheckpointRun(52000+act,"set-confirm",leader,new ReplayOriginCheckpoint {Act=act,DeckId="starter",Difficulty=3});
        string encounter=seed.Map.SelectMany(r=>r).First(n=>n.Type==MapNodeTypes.Battle&&n.EncounterId!=null).EncounterId!;
        RunState run=Run.DebugLaunchCombat(seed,encounter);
        GameState combat=run.Combat!;
        CardDef strike=_session.Catalog.Cards.Single(c=>c.Id=="green_strike");
        var hand=new[]{new CardInstance {Uid="qa-strike",Def=strike}}.Concat(combat.Player.Hand.Take(4)).ToArray();
        // Explicit fixture-only inputs guarantee a positive nonlethal contact. Rules are unchanged.
        combat=combat with {Player=combat.Player with {Hand=hand,Energy=10,EnergyMax=10,EnergyMaxAtTurnStart=10},
            Enemies=combat.Enemies.Select(e=>e with {Hp=Math.Max(100,e.Hp),MaxHp=Math.Max(100,e.MaxHp),Block=0}).ToArray()};
        return run with {Combat=combat};
    }
    private static async Task LoadFixture(int act,string leader)
    {
        RenderingServer.RenderLoopEnabled=true;
        _main.GetTree().Paused=false;
        RunState run=Fixture(act,leader);
        string json=Report.SerializeRunSaveFile(new RunSaveFile {Run=run,Fingerprint=_session.Catalog.Fingerprint});
        File.WriteAllText(Path.Combine(_out,$"fixture-act{act}-{leader}.json"),json);
        Check($"load-act{act}-{leader}",_session.Load(json).Success);
        _stage.PlaybackPaused=true;
        // State listeners retain the normal UI/stage seam; force final reset reconciliation only.
        _ui.PresentationCompleted();_stage.SetInterfaceRoute("combat");
        await Frames(5);
        World().SetProcess(false);
        _stage.AdvancePresentation(0);
        _stage.RefreshActorLighting();
        _main.GetTree().Paused=true;
        // A paused SceneTree does not freeze spatial shader TIME. Manual zero-step
        // draws preserve the current render time without changing stage shader code.
        RenderingServer.RenderLoopEnabled=false;
        Check("actor-population-present",Actors().Length>=2);
        Check("fixture-rules-roundtrip",HashRun(_session.CurrentRun!)==HashRun(run));
    }
    private static string FrozenSignature()
    {
        var world=World();
        return JsonConvert.SerializeObject(new {run=HashRun(_session.CurrentRun!),save=SaveHash(),clock=Clock(),
            camera=new {position=world.Camera.GlobalPosition.ToString(),basis=world.Camera.GlobalBasis.ToString(),world.Camera.Fov,world.Camera.Near,world.Camera.Far},
            lights=Desc(world).OfType<Light3D>().Select(l=>new {name=l.Name.ToString(),position=l.GlobalPosition.ToString(),color=l.LightColor.ToString(),energy=l.LightEnergy}),
            actors=Actors().Select(a=>new {id=a.DefinitionId,art=a.ArtId,texture=a.Sprite.Texture.ResourcePath,transform=a.Sprite.GlobalTransform.ToString(),offset=a.Sprite.Offset.ToString(),pixel=a.Sprite.PixelSize,a.DisplayedHp,a.DisplayedBlock,modulate=a.Sprite.Modulate.ToString(),
                uniforms=ProtectedUniforms.ToDictionary(n=>n,n=>Material(a).GetShaderParameter(n).ToString())})});
    }
    private sealed record Shot(byte[] Rgb,int Width,int Height,string File);
    private static async Task<Shot> Capture(string label,bool contact)
    {
        await Frames(3);
        RenderingServer.ForceDraw(true,0.0);
        RenderingServer.ForceSync();
        using Image image=_main.GetViewport().GetTexture().GetImage();
        Check("native-1170x540",image.GetWidth()==1170&&image.GetHeight()==540);
        image.Convert(Image.Format.Rgb8);
        string filename=$"{++_sequence:000}_{label}.png";
        Check("png-save-"+label,image.SavePng(Path.Combine(_out,filename))==Error.Ok);
        var record=new {loadedMvid=typeof(RecoveryStageQa).Assembly.ManifestModule.ModuleVersionId,label,contact,png=filename,pngSha256=Sha(File.ReadAllBytes(Path.Combine(_out,filename))),width=image.GetWidth(),height=image.GetHeight(),
            controls=Desc(_ui).OfType<Control>().Where(c=>c.IsVisibleInTree()).Select(c=>new {path=c.GetPath().ToString(),type=c.GetType().Name,rect=c.GetGlobalRect().ToString(),size=c.Size.ToString(),minimum=c.GetCombinedMinimumSize().ToString(),parent=c.GetParent() is Control pc?pc.GetGlobalRect().ToString():null,text=c is Label l?l.Text:c is RichTextLabel r?r.GetParsedText():c is Button b?b.Text:null,contentHeight=c is RichTextLabel rt?rt.GetContentHeight():-1,contentWidth=c is RichTextLabel rw?rw.GetContentWidth():-1}).ToArray(),
            logicalViewport=_main.GetViewport().GetVisibleRect().Size.ToString(),renderer=RenderingServer.GetCurrentRenderingMethod(),clock=Clock(),runHash=HashRun(_session.CurrentRun!),saveHash=SaveHash(),
            worldAtmosphere=World().HasMeta("stage_atmosphere_version")?World().GetMeta("stage_atmosphere_version").ToString():"baseline",
            actors=Actors().Select(a=>new {id=a.DefinitionId,art=a.ArtId,active=a.SourceDisplayTransferActive,status=a.SourceDisplayTransferStatus,hp=a.DisplayedHp,
                ambientTop=Material(a).GetShaderParameter("_CharAmbTop").ToString(),ambientBottom=Material(a).GetShaderParameter("_CharAmbBottom").ToString(),
                exposure=Material(a).GetShaderParameter("_SourceDisplayExposureEv").ToString(),flash=Material(a).GetShaderParameter("_Flash").ToString(),
                lightCount=Material(a).GetShaderParameter("_ActorLightCount").ToString(),lut=LutHash(a)}).ToArray()};
        File.WriteAllText(Path.Combine(_out,filename.Replace(".png",".json")),JsonConvert.SerializeObject(record,Formatting.Indented));
        Results.Add(record);
        return new Shot(image.GetData(),image.GetWidth(),image.GetHeight(),filename);
    }
    private static string LutHash(BattleActor actor)
    {
        Texture2D? lut=Material(actor).GetShaderParameter("_SourceDisplayLut").AsGodotObject() as Texture2D;
        if(lut==null)return "none";
        using Image image=lut.GetImage();image.Convert(Image.Format.Rgb8);return Sha(image.GetData());
    }
    private static async Task Pair(string name,bool contact)
    {
        string before=FrozenSignature();
        _stage.SetActorSourceDisplay(false);var baseline=await Capture(name+"-baseline",contact);
        Check(name+"-baseline-disabled",Actors().All(a=>!a.SourceDisplayTransferActive));
        _stage.SetActorSourceDisplay(true);var candidate=await Capture(name+"-candidate",contact);
        Check(name+"-candidate-enabled",Actors().All(a=>a.SourceDisplayTransferActive));
        bool[] mask=ActorMask(candidate.Width,candidate.Height);
        _stage.SetActorSourceDisplay(false);var restored=await Capture(name+"-restored",contact);
        Check(name+"-restored-full-rgb-exact",baseline.Rgb.SequenceEqual(restored.Rgb));
        Check(name+"-frozen-rule-camera-light-pose-inputs",before==FrozenSignature());
        int changed=0,outside=0;
        for(int p=0;p<mask.Length;p++)if(baseline.Rgb[p*3]!=candidate.Rgb[p*3]||baseline.Rgb[p*3+1]!=candidate.Rgb[p*3+1]||baseline.Rgb[p*3+2]!=candidate.Rgb[p*3+2]){changed++;if(!mask[p])outside++;}
        Results.Add(new {kind="pixel-delta",name,changed,outsideActorMask=outside,strictActorIsolation=outside==0,contact,glowEnabled=World().Lighting.GlowEnabled});
        Check(name+"-visible-pixel-change",changed>0);
        if(!contact)Check(name+"-idle-actor-isolation",outside==0);
        // Contact glow can propagate corrected radiance. Keep the strict result, never relabel it.
        if(contact&&outside>0&&World().Lighting.GlowEnabled) {
            bool glow=World().Lighting.GlowEnabled;World().Lighting.GlowEnabled=false;
            _stage.SetActorSourceDisplay(false);var cb=await Capture(name+"-glow-off-baseline",true);
            _stage.SetActorSourceDisplay(true);var cc=await Capture(name+"-glow-off-candidate",true);
            _stage.SetActorSourceDisplay(false);var cr=await Capture(name+"-glow-off-restored",true);
            Check(name+"-glow-off-restored-exact",cb.Rgb.SequenceEqual(cr.Rgb));
            int co=0;for(int p=0;p<mask.Length;p++)if(!mask[p]&&(cb.Rgb[p*3]!=cc.Rgb[p*3]||cb.Rgb[p*3+1]!=cc.Rgb[p*3+1]||cb.Rgb[p*3+2]!=cc.Rgb[p*3+2]))co++;
            Check(name+"-glow-off-isolation",co==0);
            Results.Add(new {kind="causal-glow-control",name,glowOnOutside=outside,glowOffOutside=co});World().Lighting.GlowEnabled=glow;
        }
        _stage.SetActorSourceDisplay(true);
        Check(name+"-no-rule-save-drift",HashRun(_session.CurrentRun!)==JObject.Parse(before)["run"]!.Value<string>()&&SaveHash()==JObject.Parse(before)["save"]!.Value<string>());
    }
    private static bool[] ActorMask(int width,int height)
    {
        bool[] mask=new bool[width*height];Camera3D camera=World().Camera;Vector2 logical=_main.GetViewport().GetVisibleRect().Size;
        Vector2 scale=new(width/logical.X,height/logical.Y);
        foreach(BattleActor actor in Actors()) {
            Sprite3D sprite=actor.Sprite;using Image image=sprite.Texture.GetImage();if(image.IsCompressed())image.Decompress();
            int w=image.GetWidth(),h=image.GetHeight();
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(image.GetPixel(x,y).A>=.4f) {
                Vector2 lo=new(float.PositiveInfinity,float.PositiveInfinity),hi=new(float.NegativeInfinity,float.NegativeInfinity);
                foreach(var c in new[]{new Vector2(x,y),new Vector2(x+1,y),new Vector2(x,y+1),new Vector2(x+1,y+1)}) {
                    Vector3 local=new((c.X-w*.5f+sprite.Offset.X)*sprite.PixelSize,(h*.5f-c.Y+sprite.Offset.Y)*sprite.PixelSize,0);
                    Vector2 point=camera.UnprojectPosition(sprite.GlobalTransform*local)*scale;lo=lo.Min(point);hi=hi.Max(point);
                }
                for(int py=Math.Max(0,(int)MathF.Floor(lo.Y)-2);py<=Math.Min(height-1,(int)MathF.Ceiling(hi.Y)+2);py++)
                    for(int px=Math.Max(0,(int)MathF.Floor(lo.X)-2);px<=Math.Min(width-1,(int)MathF.Ceiling(hi.X)+2);px++)mask[py*width+px]=true;
            }
        }
        return mask;
    }
    private static async Task LiveActorGuards()
    {
        BattleActor actor=Actors().First(a=>a.DefinitionId.StartsWith("leader_",StringComparison.Ordinal));
        string run=HashRun(_session.CurrentRun!),save=SaveHash();ulong id=actor.GetInstanceId();Camera3D camera=World().Camera;
        foreach(int act in new[]{2,3,1}) {
            actor.ConfigureSourceAppearance(act,camera,2);actor.RefreshSourceDisplayTransfer();
            Check($"same-live-actor-act{act}",actor.GetInstanceId()==id&&actor.SourceDisplayTransferStatus==SourceActorDisplayTransfer.GetProfileId(act));
            Check($"actual-lut-act{act}",LutHash(actor)==SourceActorDisplayTransfer.GetGeneratedLutSha256(act));
            Check($"actual-ev-act{act}",Math.Abs(Material(actor).GetShaderParameter("_SourceDisplayExposureEv").AsSingle()-SourceActorDisplayTransfer.CreateSourcePost(act).ExposureEv)<.000001f);
        }
        JObject look=(JObject)typeof(BattleActor).GetField("_sourceLook",Private)!.GetValue(actor)!;
        JToken original=look["post"]!["exposure"]!.DeepClone();look["post"]!["exposure"]=9;
        actor.RefreshSourceDisplayTransfer();Check("unsupported-profile-disables",!actor.SourceDisplayTransferActive&&LutHash(actor)=="none");
        look["post"]!["exposure"]=original;actor.RefreshSourceDisplayTransfer();Check("profile-restores",actor.SourceDisplayTransferActive);
        float exposure=World().Lighting.TonemapExposure;World().Lighting.TonemapExposure=2;actor.RefreshSourceDisplayTransfer();
        Check("unsupported-live-environment-disables",!actor.SourceDisplayTransferActive);
        World().Lighting.TonemapExposure=exposure;actor.RefreshSourceDisplayTransfer();Check("environment-restores",actor.SourceDisplayTransferActive);
        actor.ConfigureSourceAppearance(2,camera,2);actor.SetOtherActSourceDisplayEnabled(false);Check("other-act-opt-out",!actor.SourceDisplayTransferActive);
        actor.SetOtherActSourceDisplayEnabled(true);Check("other-act-opt-in",actor.SourceDisplayTransferActive);
        _stage.SetActorSourceDisplay(false);Check("global-opt-out-precedence",Actors().All(a=>!a.SourceDisplayTransferActive));
        actor.ConfigureSourceAppearance(1,camera,2);_stage.SetActorSourceDisplay(true);
        Check("guard-checks-rule-save-invariant",run==HashRun(_session.CurrentRun!)&&save==SaveHash());
        // Real actor exit closes its owned texture, with a retained reference for inspection.
        ImageTexture? owned=Material(actor).GetShaderParameter("_SourceDisplayLut").AsGodotObject() as ImageTexture;
        Check("owned-lut-before-exit",owned!=null);
        actor.GetParent().RemoveChild(actor);
        Check("exit-clears-active-transfer",!actor.SourceDisplayTransferActive&&actor.SourceDisplayTransferStatus=="legacy:actor-exiting");
        Check("exit-disposes-owned-lut",owned!=null&&!GodotObject.IsInstanceValid(owned));
        actor.Free();
        await LoadFixture(1,"leader_green");Check("reentry-recreates-transfer",Actors().All(a=>a.SourceDisplayTransferActive));
    }
}

#endif
