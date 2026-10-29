using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace DeckRogue.Game {
// Opt-in diagnostic candidate. Bind before the ground snapshot; never swap a material during a lease.
internal static class OwnedFloorReceive {
 sealed class Entry {internal MeshRenderer Renderer;internal Material Original,Private;internal int Slot;}
 static readonly List<Entry> Entries=new List<Entry>();static bool Probe;
 static void Prune(){for(int i=Entries.Count-1;i>=0;i--)if(Entries[i].Renderer==null){if(Entries[i].Private!=null)UnityEngine.Object.Destroy(Entries[i].Private);Entries.RemoveAt(i);}}
 internal static void SetProbe(bool enabled){Probe=enabled;Apply(new List<Vector4>());}
 internal static void RegisterIntegrated(GameObject root){
  Prune();
  if(Autopilot.Arg("-floor-receiver-experiment")!="1")return;
  var mesh=Resources.Load<Mesh>("FullStageIntegrated/StageBatch_02");var shader=Resources.Load<Shader>("Shaders/OwnedFloorModule");
  Debug.Log("[OwnedFloor] register resource="+(mesh!=null)+" shader="+(shader!=null)+" supported="+(shader!=null&&shader.isSupported));
  if(mesh==null||shader==null||!shader.isSupported)return;
  foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
   if(renderer.GetComponent<MeshFilter>()?.sharedMesh!=mesh)continue;
   var mats=renderer.sharedMaterials;
   Debug.Log("[OwnedFloor] actual renderer="+renderer.name+" slots="+mats.Length+" submeshes="+mesh.subMeshCount+" materials="+string.Join(";",Array.ConvertAll(mats,m=>m.name+"|"+m.shader.name)));
   if(mats.Length!=2||mesh.subMeshCount!=2)continue;
   if(mats[0]?.name!="moss_turf_128"||mats[1]?.name!="earth_128")continue;
   for(int slot=0;slot<2;slot++){
    var original=mats[slot];if(original.shader.name!="DeckRogue/StageModule")continue;
    // Preserve names, properties, keywords and all passes. Only owned additional-light color differs.
    var clone=new Material(original){shader=shader,name=original.name};clone.shaderKeywords=original.shaderKeywords;clone.renderQueue=original.renderQueue;
    clone.SetFloat("_OwnedFloorReceiveCount",0);clone.SetVectorArray("_OwnedFloorReceiveLights",new Vector4[4]);
    mats[slot]=clone;Entries.Add(new Entry{Renderer=renderer,Original=original,Private=clone,Slot=slot});
   }
   renderer.sharedMaterials=mats;Debug.Log("[OwnedFloor] registered entries="+Entries.Count);
  }
 }
 internal static void Apply(List<Vector4> values){Prune();foreach(var e in Entries){var mats=e.Renderer.sharedMaterials;if(e.Slot>=mats.Length||mats[e.Slot]!=e.Private)continue;e.Private.CopyPropertiesFromMaterial(e.Original);var a=new Vector4[4];for(int i=0;i<Math.Min(4,values.Count);i++)a[i]=values[i];e.Private.SetVectorArray("_OwnedFloorReceiveLights",a);e.Private.SetFloat("_OwnedFloorReceiveCount",values.Count<=4?values.Count:0);}}
 internal static JArray Capture(){Prune();var rows=new JArray();foreach(var e in Entries){var m=e.Renderer.sharedMaterials[e.Slot];rows.Add(new JObject{["name"]=e.Renderer.name,["slot"]=e.Slot,["material"]=m.name,["renderer"]=e.Renderer.GetEntityId().ToString(),["boundMaterial"]=m.GetEntityId().ToString(),["shader"]=m.shader.name,["count"]=m.GetFloat("_OwnedFloorReceiveCount"),["lights"]=JArray.FromObject(Array.ConvertAll(m.GetVectorArray("_OwnedFloorReceiveLights")??Array.Empty<Vector4>(),v=>new[]{v.x,v.y,v.z,v.w}))});}return rows;}
}
}
