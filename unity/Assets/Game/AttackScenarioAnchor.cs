using UnityEngine;
namespace DeckRogue.Game {
 // Immutable, receipt-owned basis. Target H is alpha-visible height, never sprite canvas height.
 // Camera-facing local +Z in the source points toward camera; Unity mesh upload flips Z once.
 internal readonly struct AttackScenarioAnchor {
  internal readonly Vector3 Position,Feet;internal readonly Quaternion Rotation;internal readonly float Height;
  AttackScenarioAnchor(Vector3 position,Vector3 feet,Quaternion rotation,float height){Position=position;Feet=feet;Rotation=rotation;Height=height;}
  internal static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
  internal static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
  internal static bool TryCapture(ResolvedCombatFx.Ticket ticket,out AttackScenarioAnchor anchor){
   anchor=default;if(ticket==null||!ticket.HasWorld||!Finite(ticket.Position)||!Finite(ticket.Feet)||!Finite(ticket.ContactVisibleHeight)||ticket.ContactVisibleHeight<=0||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled)return false;
   var q=Stage.Camera.transform.rotation;if(!Finite(q.x)||!Finite(q.y)||!Finite(q.z)||!Finite(q.w))return false;
   anchor=new AttackScenarioAnchor(ticket.Position,ticket.Feet,q,ticket.ContactVisibleHeight);return true;
  }
 }
}
