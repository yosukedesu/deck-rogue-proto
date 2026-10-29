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
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }
    }
}
