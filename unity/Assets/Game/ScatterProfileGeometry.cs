using System;

namespace DeckRogue.Game
{
    // Frozen scatter4 R2 reconstruction. Pure numerical CPU port, not Unity/device validation.
    // Local coordinates use captured camera right/up/toward and captured alpha-visible H.
    internal static class ScatterProfileGeometry
    {
        public enum Profile { Fire = 0, WarmSpark = 1, Ice = 2, Spore = 3 }
        public const int CoreKind = 0, ParticleKind = 1, BatchCount = 2, TextureCount = 0;
        public const double NativeLead = 0, CoreEnd = .050, LightEnd = .070;
        // Explicit diagnostic input domain prevents finite-but-overflowing squared ages.
        // It is much wider than the authored 0..260ms lifetime; reject, never silently remap.
        public const double MaximumAbsoluteSampleAge = 1000000;
        public struct Point
        {
            public double X, Y, Z;
            public Point(double x, double y, double z) { X = x; Y = y; Z = z; }
        }
        public struct Uv
        {
            public double X, Y;
            public Uv(double x, double y) { X = x; Y = y; }
        }
        public struct Bounds
        {
            public Point Min, Max;
            public Bounds(Point min, Point max) { Min = min; Max = max; }
        }
        public sealed class MeshData
        {
            public readonly int Kind;
            public readonly Point[] Vertices;
            public readonly Uv[] UV;
            public readonly double[] Alpha, Gain, Phase;
            public readonly int[] Triangles;
            internal MeshData(int vertices, int triangles, int kind)
            {
                Kind = kind; Vertices = new Point[vertices]; UV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices]; Phase = new double[vertices];
                Triangles = new int[triangles * 3];
            }
        }
        public sealed class Buffer
        {
            public readonly Profile Family;
            public readonly int MirrorRight, MaximumParticles;
            public readonly MeshData Core, Particles;
            public readonly MeshData[] Batches;
            public readonly bool[] ParticleActive;
            public readonly Point[] ParticleCenters;
            public double VisualAge, LightEnvelope, TargetAreaW, FloorAreaW;
            public Bounds SampleBounds;
            internal Buffer(Profile profile, int mirror)
            {
                Family = profile; MirrorRight = mirror; MaximumParticles = Count(profile);
                Core = new MeshData(6, 4, CoreKind); Particles = new MeshData(4 * MaximumParticles, 2 * MaximumParticles, ParticleKind);
                Batches = new[] { Core, Particles }; ParticleActive = new bool[MaximumParticles]; ParticleCenters = new Point[MaximumParticles];
            }
        }
        public static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        public static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        public static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        public static void CheckProfile(Profile family) { if ((int)family < 0 || (int)family > 3) throw new ArgumentOutOfRangeException("family"); }
        public static int Count(Profile family) { CheckProfile(family); return family == Profile.Ice ? 10 : 12; }
        public static string ProfileId(Profile family)
        {
            CheckProfile(family);
            switch (family) { case Profile.Fire: return "scatter_fire"; case Profile.WarmSpark: return "scatter_warm_spark"; case Profile.Ice: return "scatter_ice"; default: return "scatter_spore"; }
        }
        public static double End(Profile family)
        {
            CheckProfile(family);
            switch (family) { case Profile.Fire: return .205; case Profile.WarmSpark: return .190; case Profile.Ice: return .219; default: return .260; }
        }
        public static double End(Buffer b) { if (b == null) throw new ArgumentNullException("b"); return End(b.Family); }
        public static double Lead() { return NativeLead; }
        // Independent, allocation-free light query; never resamples or mutates a mesh buffer.
        public static double LightEnvelope(Profile family, double age)
        {
            CheckProfile(family);
            if (!Finite(age)) throw new ArgumentOutOfRangeException("age");
            double maximum = family == Profile.Fire ? .32 : family == Profile.WarmSpark ? .26 : family == Profile.Ice ? .24 : .15;
            return 0 <= age && age <= .070 ? maximum * (1 - Smooth((age - .012) / .058)) : 0;
        }
        // P0 has no pre-contact remap, regardless of a host's available anticipation.
        public static double? NativeAge(double age, double availableA = 0)
        {
            if (!Finite(age) || !Finite(availableA)) throw new ArgumentException("Finite clock inputs required.");
            return age < 0 ? (double?)null : age;
        }
        public static Bounds ConservativeVisibleBounds
        { get { return new Bounds(new Point(-.4, -.4, .02), new Point(.4, .4, .033)); } }
        public static Buffer Create(Profile family, int mirrorRight = 1)
        {
            CheckProfile(family);
            if (mirrorRight != -1 && mirrorRight != 1) throw new ArgumentOutOfRangeException("mirrorRight");
            Buffer b = new Buffer(family, mirrorRight);
            double carrier = 1 / Math.Cos(Math.PI / 6), radius = family == Profile.Fire ? .022 : family == Profile.Ice ? .018 : .020;
            for (int i = 0; i < 6; i++)
            {
                double theta = i * (2 * Math.PI) / 6, ca = Math.Cos(theta), sa = Math.Sin(theta);
                b.Core.Vertices[i] = new Point(mirrorRight * carrier * radius * ca, carrier * radius * .7 * sa, .02);
                b.Core.UV[i] = new Uv(.5 + .5 * carrier * ca, .5 + .5 * carrier * sa); b.Core.Gain[i] = 1.7;
            }
            for (int i = 0; i < 4; i++) { b.Core.Triangles[i * 3] = 0; b.Core.Triangles[i * 3 + 1] = i + 1; b.Core.Triangles[i * 3 + 2] = i + 2; }
            for (int i = 0; i < b.MaximumParticles; i++)
            {
                int v = i * 4, f = i * 6;
                b.Particles.Triangles[f] = v; b.Particles.Triangles[f + 1] = v + 1; b.Particles.Triangles[f + 2] = v + 2;
                b.Particles.Triangles[f + 3] = v; b.Particles.Triangles[f + 4] = v + 2; b.Particles.Triangles[f + 5] = v + 3;
                bool spore = family == Profile.Spore;
                b.Particles.UV[v] = new Uv(0, spore ? 0 : .5); b.Particles.UV[v + 1] = new Uv(spore ? 1 : .42, 0);
                b.Particles.UV[v + 2] = new Uv(1, spore ? 1 : .5); b.Particles.UV[v + 3] = new Uv(spore ? 0 : .45, 1);
                double gain = family == Profile.Fire ? 1.6 : family == Profile.WarmSpark ? 1.45 : family == Profile.Ice ? 1.3 : .95;
                for (int j = 0; j < 4; j++) { b.Particles.Phase[v + j] = i * 1.913; b.Particles.Gain[v + j] = gain; }
            }
            Sample(b, 0); return b;
        }
        // count selects a fixed prefix. Hidden entries still preserve birth, geometry, phase and gain.
        // No RNG, delta-time accumulation, hitstop input, receipt routing, or damage logic.
        public static void Sample(Buffer b, double t, int count = int.MaxValue)
        {
            if (b == null) throw new ArgumentNullException("b");
            if (!Finite(t) || Math.Abs(t) > MaximumAbsoluteSampleAge) throw new ArgumentOutOfRangeException("t");
            b.VisualAge = t; int n = Math.Max(0, Math.Min(b.MaximumParticles, count)); Profile family = b.Family;
            double core = 0 <= t && t <= .050 ? 1 - Smooth((t - .008) / .042) : 0;
            core *= family == Profile.Fire ? .8 : family == Profile.WarmSpark ? .7 : family == Profile.Ice ? .5 : .35;
            for (int j = 0; j < 6; j++) b.Core.Alpha[j] = core;
            for (int i = 0; i < b.MaximumParticles; i++)
            {
                int q = i % 4;
                double birth = (family == Profile.Ice ? .003 : family == Profile.Spore ? .002 : .004) * q;
                double life = family == Profile.Fire ? .169 + .008 * q : family == Profile.WarmSpark ? .160 + .006 * q : family == Profile.Ice ? .180 + .010 * q : .230 + .008 * q;
                double a = Math.Max(0, t - birth), phase = i * 1.913;
                double alpha = birth <= t && t <= birth + life && i < n ? Smooth(a / .008) * (1 - Smooth((a - life * .28) / (life * .72))) : 0;
                double x, y, angle, length, width;
                if (family == Profile.Fire)
                {
                    double vx = 1.30 * Math.Sin(phase) + .30, vy = .64 + .26 * (i % 5), travel = (1 - Math.Exp(-5 * a)) / 5;
                    x = .045 * Math.Sin(i * 2.3) + vx * travel; y = -.018 + .014 * (i % 4) + vy * travel + .2 * a * a;
                    angle = Math.Atan2(vy + .4 * a, vx); length = (.015 + .0045 * (i % 3)) * (1 - .45 * Smooth(a / life)); width = .0045 + .001 * (i % 2);
                }
                else if (family == Profile.WarmSpark)
                {
                    int sign = i % 2 != 0 ? -1 : 1;
                    double vx = sign * (.85 + .18 * (i % 5)), vy = .32 * Math.Sin(phase) + .10 * ((i % 3) - 1), travel = (1 - Math.Exp(-9 * a)) / 9;
                    x = .020 * sign + .011 * Math.Sin(i * 1.7) + vx * travel; y = .035 * Math.Sin(i * 2.4) + vy * travel - .25 * a * a;
                    angle = Math.Atan2(vy - .5 * a, vx); length = (.014 + .0035 * (i % 3)) * (1 - .5 * Smooth(a / life)); width = .0055;
                }
                else if (family == Profile.Ice)
                {
                    x = (i - 4.5) * .027 + (.25 * Math.Sin(phase)) * a; y = .042 * Math.Sin(i * 2.1) - (.23 + .10 * (i % 3)) * a - 1.5 * a * a;
                    angle = phase + (i % 2 != 0 ? -2.8 : 2.1) * a; length = .022 + .006 * (i % 3); width = .011 + .0025 * (i % 2);
                }
                else
                {
                    x = .14 * Math.Sin(i * 2.399) + (.11 * Math.Cos(phase)) * a + .014 * Math.Sin(18 * a + phase) * Smooth(a / .03);
                    y = .08 * Math.Cos(i * 1.7) - (.09 + .035 * (i % 3)) * a - .30 * a * a;
                    angle = .18 * Math.Sin(phase + a * 5); length = .022 + .006 * (i % 3); width = length * (.72 + .09 * (i % 2)); alpha *= .82;
                }
                double ca = Math.Cos(angle), sa = Math.Sin(angle), z = .03 + .0002 * i;
                for (int j = 0; j < 4; j++)
                {
                    double u, w;
                    if (family == Profile.Spore) { u = j == 0 || j == 3 ? -width : width; w = j < 2 ? -length : length; }
                    else { u = j == 0 ? -length : j == 1 ? -length * .16 : j == 2 ? length : -length * .10; w = j == 1 ? -width : j == 3 ? width : 0; }
                    int v = i * 4 + j; b.Particles.Vertices[v] = new Point(b.MirrorRight * (x + u * ca - w * sa), y + u * sa + w * ca, z); b.Particles.Alpha[v] = alpha;
                }
                b.ParticleCenters[i] = new Point(b.MirrorRight * x, y, z); b.ParticleActive[i] = alpha > 0;
            }
            b.LightEnvelope = LightEnvelope(family, t);
            // Blender R2 reference Area watts only; no native Unity point intensity implied.
            b.TargetAreaW = 32 * b.LightEnvelope; b.FloorAreaW = 8 * b.LightEnvelope;
            Point min = b.Core.Vertices[0], max = min;
            for (int k = 0; k < 2; k++) for (int v = 0; v < b.Batches[k].Vertices.Length; v++)
            {
                Point p = b.Batches[k].Vertices[v]; min.X = Math.Min(min.X, p.X); min.Y = Math.Min(min.Y, p.Y); min.Z = Math.Min(min.Z, p.Z);
                max.X = Math.Max(max.X, p.X); max.Y = Math.Max(max.Y, p.Y); max.Z = Math.Max(max.Z, p.Z);
            }
            b.SampleBounds = new Bounds(min, max);
        }
        public static bool SampleReceipt(Buffer b, double age, double availableA = 0, int count = int.MaxValue)
        {
            // Even suppressed negative age refreshes geometry/clears stale channels deterministically.
            double? native = NativeAge(age, availableA); Sample(b, age, count); return native.HasValue;
        }
    }
}
