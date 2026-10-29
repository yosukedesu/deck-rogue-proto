using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DeckRogue.EditorTools
{
    // Geometry checks only: this does not certify route ownership or rendered parity.
    public static class Hd2dCheckpointPortChecks
    {
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static float Coordinate(object value, string name) => (float)Field(value, name);
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        public static void Run()
        {
            try
            {
                var type = Type.GetType("DeckRogue.Game.Hd2d.Hd2dAttackPrototype, Assembly-CSharp", true);
                var create = type.GetMethod("CreateLightningDischarge", BindingFlags.Static | BindingFlags.NonPublic);
                int samples = 0, vertices = 0;
                foreach (float sourceX in new[] { -3f, -1f, 2f })
                {
                    var effect = create.Invoke(null, null);
                    var configure = (Delegate)Field(effect, "ConfigureSource");
                    var vector = configure.GetType().GetGenericArguments()[0];
                    configure.DynamicInvoke(Activator.CreateInstance(vector, sourceX, .2f, 0f));
                    var batches = (Array)Field(effect, "Batches");
                    Require(batches.Length == 6, "Lightning layer contract changed");
                    foreach (double age in new[] { -.10, -.07, -.03, 0, .003, .02, .037, .06, .07, .085, .10, .137, .145, .20, .30 })
                    {
                        ((Action<double>)Field(effect, "Sample"))(age); samples++;
                        float visible = 0;
                        for (int layer = 0; layer < batches.Length; layer++)
                        {
                            var batch = batches.GetValue(layer);
                            int count = (int)Field(batch, "Count");
                            Require(count <= 512, "Per-layer capacity exceeded");
                            var position = (Delegate)Field(batch, "Position");
                            var alpha = (Func<int, float>)Field(batch, "Alpha");
                            var gain = (Func<int, float>)Field(batch, "Gain");
                            int active = 0;
                            for (int i = 0; i < count; i++)
                            {
                                var p = position.DynamicInvoke(i);
                                foreach (string axis in new[] { "X", "Y", "Z" })
                                    Require(float.IsFinite(Coordinate(p, axis)), "Nonfinite vertex");
                                Require(float.IsFinite(alpha(i)) && alpha(i) >= 0, "Invalid alpha");
                                Require(float.IsFinite(gain(i)) && gain(i) >= 0, "Invalid gain");
                                if (alpha(i) > 0) active++;
                                visible += alpha(i); vertices++;
                            }
                            foreach (int index in (int[])Field(batch, "Indices"))
                                Require(index >= 0 && index < count, "Index outside batch");
                            if (layer == 1) Require(active <= 468, "Shoulder budget exceeded");
                            if (layer == 5) Require(active <= 342, "Channel budget exceeded");
                            if (layer == 4) Require(active <= 108, "Fork budget exceeded");
                        }
                        if (age >= .20) Require(visible == 0, "Lightning failed to clear by tail");
                    }
                }
                Debug.Log($"[Hd2dCheckpointPort] PASS Lightning243 geometry: {samples} samples, {vertices} finite vertex checks, source variation, index/budget and tail checks. Visual parity NOT evaluated.");
                CheckLightBolt(type);
                CheckLightBoltOwnership();
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }
        static void CheckLightBolt(Type type) {
            var routeType=Type.GetType("DeckRogue.Game.Hd2d.AttackRoute, Assembly-CSharp",true);
            var route=Activator.CreateInstance(routeType,"card:white_light_bolt","white_light_bolt","card","light","test","light",false,"geometry test");
            var effect=type.GetMethod("CreateOwnedLightBoltContact",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new[]{route});
            var batches=(Array)Field(effect,"Batches");
            Require(batches.Length==8,"LightBolt305 layer count");
            var sample=(Action<double>)Field(effect,"Sample");
            var commit=(Action<bool>)Field(effect,"ConfigureLightBoltCommittedContact");
            int samples=0;
            foreach(bool committed in new[]{false,true,false,true,false}) {
                commit(committed);
                foreach(double age in new[]{-.3,-.1,-.001,0,.018,.033,.066,.100,.18,.22,.3,double.NaN,double.PositiveInfinity}) {
                    sample(age);samples++;
                    float contact=0;
                    for(int layer=0;layer<batches.Length;layer++) {
                        var batch=batches.GetValue(layer);int count=(int)Field(batch,"Count");
                        var indices=(int[])Field(batch,"Indices");
                        // The shared fixed-capacity buffer has padding; renderer uploads full triangles only.
                        int submitted=indices.Length-indices.Length%3;
                        Require(submitted>0,"No light bolt triangles");
                        for(int j=0;j<submitted;j++)Require(indices[j]>=0 && indices[j]<count,"Index outside light bolt layer");
                        var alpha=(Func<int,float>)Field(batch,"Alpha");
                        for(int i=0;i<count;i++) {
                            Require(float.IsFinite(alpha(i)),"Nonfinite light bolt opacity");
                            if(layer>=5)contact+=alpha(i);
                            else if(!(age<0))Require(alpha(i)==0,"Legacy contact layer leaked");
                        }
                    }
                    if(!committed || !(age>=0 && age<.22))Require(contact==0,"Uncommitted, negative, stale or expired contact leaked");
                    if(committed && age==0)Require(contact>0,"Committed contact absent");
                }
            }
            Debug.Log($"[Hd2dCheckpointPort] PASS LightBolt305 geometry: {samples} samples, commit reset, negative/nonfinite age gates, legacy suppression and triangle bounds. Runtime ownership and visual parity NOT evaluated.");
        }
        static void CheckLightBoltOwnership() {
            DeckRogue.Engine.Content.Load(System.IO.Path.Combine(Application.streamingAssetsPath,"data"));
            var run=DeckRogue.Engine.Run.DebugLaunchCombat(DeckRogue.Engine.Run.CreateRun(4242,"set_confirm","leader_white"),"enemy_probe");
            var card=new DeckRogue.Engine.Generated.CardInstance{Uid="white_light_bolt#owned-test",Def=DeckRogue.Engine.Content.GetCardDef("white_light_bolt")};
            run=run with {Combat=run.Combat with {Player=run.Combat.Player with {Hand=new[]{card},Energy=3,Light=1}}};
            var command=new DeckRogue.Engine.Generated.RunCommand_Combat{Command=new DeckRogue.Engine.Generated.Command_PlayCard{CardUid=card.Uid,TargetIndex=0}};
            var after=DeckRogue.Engine.Run.ApplyRunCommand(run,command);
            var ledger=new DeckRogue.Game.CombatPresentationLedger();
            var batch=ledger.Register(run.Combat,after.Combat,command);
            var bindingType=Type.GetType("DeckRogue.Game.Hd2dLightBoltBinding, Assembly-CSharp",true);
            var capture=bindingType.GetMethod("Capture");int hits=0;var bindings=new System.Collections.Generic.List<object>();
            for(int i=batch.From;i<batch.To;i++) {
                var ev=after.Combat.EventLog[i];
                Debug.Log("[LightBolt305 ownership] actual event="+ev.Type);
                if(!(ev is DeckRogue.Engine.Generated.GameEvent_DamageDealt))continue;
                var receipt=ledger.Find(i,ev);
                var binding=capture.Invoke(null,new object[]{null,batch,receipt,new Func<int>(()=>ledger.Generation)});
                Require(binding!=null,"Actual explicit LightBolt command was rejected");
                Require(!(bool)bindingType.GetProperty("Committed").GetValue(binding),"Contact committed early");
                batch.OwnerCommand=command with {Command=((DeckRogue.Engine.Generated.Command_PlayCard)command.Command) with {TargetIndex=null}};
                Require(capture.Invoke(null,new object[]{null,batch,receipt,new Func<int>(()=>ledger.Generation)})==null,"Implicit target incorrectly admitted");
                batch.OwnerCommand=command;
                Require(ledger.Commit(receipt),"Actual numeric commit failed");
                Require((bool)bindingType.GetProperty("Committed").GetValue(binding),"Exact commit not observed");
                bindings.Add(binding);
                hits++;
            }
            Require(hits==3,"Actual LightBolt hit count changed");
            ledger.Clear();foreach(var binding in bindings)Require(!(bool)bindingType.GetProperty("Committed").GetValue(binding),"Old generation survived reset");
            Debug.Log("[Hd2dCheckpointPort] PASS LightBolt305 actual explicit-command ownership: 3 real engine hits, commit observation, implicit-target rejection and generation reset. No renderer claim.");
        }
    }
}
