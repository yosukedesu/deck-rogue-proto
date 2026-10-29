using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace DeckRogue.Game {
 internal static class TripleHornReceiveState {
  // Selected only inside the exact TripleHorn ownership admission; QA may override.
  internal static bool Probe=true;internal static float Factor=.12f;
  static readonly HashSet<Material> Receivers=new HashSet<Material>();static Vector4[] lights=new Vector4[2];static int count;
  internal static void Clear(){foreach(var m in Receivers)if(m!=null)m.SetFloat("_TripleHornReceiver",0);Receivers.Clear();count=0;Shader.SetGlobalFloat("_TripleHornLightCount",0);}
  internal static void Apply(List<Vector4> positions,object hero,object target){
   Clear();if(!Probe||positions.Count==0||positions.Count>2||hero==null||target==null)return;
   foreach(var identity in new[]{hero,target}){var c=identity as Component;var m=c!=null?c.GetComponent<MeshRenderer>()?.sharedMaterial:null;if(m==null||!m.HasProperty("_TripleHornReceiver")){Clear();return;}Receivers.Add(m);}
   // Never select an unrelated actor through a shared material instance.
   foreach(var key in new[]{"player","enemy0","enemy1","enemy2"}){var identity=Stage.CaptureImpact(key)?.Target;if(identity==null||ReferenceEquals(identity,hero)||ReferenceEquals(identity,target))continue;var c=identity as Component;var m=c!=null?c.GetComponent<MeshRenderer>()?.sharedMaterial:null;if(m!=null&&Receivers.Contains(m)){Clear();return;}}
   foreach(var m in Receivers)m.SetFloat("_TripleHornReceiver",1);
   lights=new Vector4[2];for(int i=0;i<positions.Count;i++){lights[i]=positions[i];lights[i].w=Mathf.Clamp(Factor,.01f,1);}
   count=positions.Count;Shader.SetGlobalVectorArray("_TripleHornLights",lights);Shader.SetGlobalFloat("_TripleHornLightCount",count);
  }
  internal static JObject Capture(){var actors=new JArray();foreach(var key in new[]{"player","enemy0","enemy1","enemy2"}){var c=Stage.CaptureImpact(key)?.Target as Component;var m=c!=null?c.GetComponent<MeshRenderer>()?.sharedMaterial:null;if(m!=null)actors.Add(new JObject{["key"]=key,["material"]=m.GetEntityId().ToString(),["receiver"]=m.HasProperty("_TripleHornReceiver")?m.GetFloat("_TripleHornReceiver"):0});}return new JObject{["probe"]=Probe,["factor"]=Factor,["count"]=count,["lights"]=JArray.FromObject(Array.ConvertAll(lights,v=>new[]{v.x,v.y,v.z,v.w})),["actors"]=actors};}
 }
}
