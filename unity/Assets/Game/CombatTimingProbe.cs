using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Unity.Profiling;
namespace DeckRogue.Game {
 public static class CombatTimingProbe {
  public static bool Enabled;
  public struct Mark {public string kind,detail;public int frame;public double ms;public float time;}
  public struct Frame {public int frame;public double ms;public float delta,timeScale,x,y,z;public long mainNs,renderNs,gcBytes,draws;}
  static readonly List<Mark> marks=new List<Mark>(1024);
  static readonly List<Frame> frames=new List<Frame>(4096);
  static ProfilerRecorder main,render,gc,draw;
  static double start;
  public static void Begin(){marks.Clear();frames.Clear();start=Time.realtimeSinceStartupAsDouble;Enabled=true;main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");render=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread");gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");}
  public static void Event(string kind,string detail=""){if(!Enabled)return;marks.Add(new Mark{kind=kind,detail=detail,frame=Time.frameCount,ms=(Time.realtimeSinceStartupAsDouble-start)*1000,time=Time.time});}
  public static void Sample(){if(!Enabled)return;Stage.TryGetUnitBox("player",out var p,out var h);frames.Add(new Frame{frame=Time.frameCount,ms=(Time.realtimeSinceStartupAsDouble-start)*1000,delta=Time.unscaledDeltaTime*1000,timeScale=Time.timeScale,x=p.x,y=p.y,z=p.z,mainNs=main.Valid?main.LastValue:-1,renderNs=render.Valid?render.LastValue:-1,gcBytes=gc.Valid?gc.LastValue:-1,draws=draw.Valid?draw.LastValue:-1});}
  public static void Save(string path){Enabled=false;File.WriteAllText(path,JsonConvert.SerializeObject(new{captureFramerate=Time.captureFramerate,platform=Application.platform.ToString(),unity=Application.unityVersion,recorders=new{main=main.Valid,render=render.Valid,gc=gc.Valid,draw=draw.Valid},marks,frames},Formatting.Indented));main.Dispose();render.Dispose();gc.Dispose();draw.Dispose();}
 }
}
