using System;

namespace DeckRogue.Game
{
    // Frozen Lightning R2 reconstruction, 2026-10-09. Unity/device visuals unverified.
    // Manifest: 9051416bf18ead557adcb9f8383d41801ce95bae4c91b51488b95e6a80ff3212.
    // Coordinates: captured camera right/up/toward in stable alpha-visible H at actual target contact.
    // A decorative local leader is NOT a recovered caster socket or world source-target beam.
    internal static class LightningDischargeGeometry
    {
        public const int BranchKind = 0, CoreKind = 1, SparkKind = 2;
        public const int ShadowRole = 0, PrimaryRole = 1, EdgeRole = 2, CoreRole = 3;
        public const int BatchCount = 3, VertexCount = 65, TriangleCount = 38, ParticleCount = 12;
        public const double NativeLead = .100, VisibleEnd = .200, BranchEnd = .110, CoreEnd = .060, LightEnd = .075;
        public const double Size = 1.50, PivotX = 477.0 / 887.0, PivotY = 1.0 - 442.0 / 887.0;
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
        public sealed class MeshData
        {
            public readonly int Kind;
            public readonly Point[] Vertices;
            public readonly Uv[] UV;
            public readonly double[] Alpha, Gain;
            public readonly int[] Role, Triangles;
            internal MeshData(int vertices, int triangles, int kind, double gain)
            {
                Kind = kind; Vertices = new Point[vertices]; UV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices];
                Role = new int[vertices]; Triangles = new int[triangles * 3];
                for (int i = 0; i < vertices; i++) { Gain[i] = gain; Role[i] = PrimaryRole; }
            }
        }
        public sealed class Buffer
        {
            public readonly MeshData Branches, Core, Sparks;
            public readonly MeshData[] Batches;
            public readonly bool[] ParticleActive;
            public readonly Point[] ParticleCenters;
            // Optional captured-side mirror, applied to every mesh; UV/winding unchanged (CullOff).
            public readonly int MirrorRight;
            public double VisualAge, LightEnvelope;
            // R2 Blender reference powers only. Neither value is Unity intensity.
            public double TargetAreaW, FloorAreaW;
            internal Buffer(int mirrorRight)
            {
                MirrorRight = mirrorRight;
                Branches = new MeshData(4, 2, BranchKind, 2.35);
                Core = new MeshData(13, 12, CoreKind, 3.0);
                Sparks = new MeshData(48, 24, SparkKind, 2.1);
                Batches = new[] { Branches, Core, Sparks };
                ParticleActive = new bool[ParticleCount]; ParticleCenters = new Point[ParticleCount];
            }
        }
        static readonly double[] Angles = { 2.35, 2.57, 3.00, 3.48, 3.92, 4.55, 4.85, 5.55, 5.92, .42, .88, 1.18 };
        public static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        public static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        public static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        public static double Lead() { return NativeLead; }
        public static double End() { return VisibleEnd; }
        public static double End(Buffer b) { if (b == null) throw new ArgumentNullException("b"); return VisibleEnd; }
        // Exact frozen quadratic C1 negative-only remap; None becomes nullable null at A=0.
        public static double? NativeAge(double age, double availableA, double nativeP = NativeLead)
        {
            if (!Finite(age) || !Finite(availableA) || !Finite(nativeP) || nativeP < 0) throw new ArgumentException("Finite clock inputs and nonnegative native P required.");
            if (age >= 0) return age;
            double a = Math.Min(nativeP, Math.Max(0, availableA));
            if (a <= 0) return null;
            if (age < -a) return -nativeP;
            return age - (nativeP - a) * Math.Pow(age / a, 2);
        }
        static void Quad(MeshData m, int index)
        {
            int v = index * 4, f = index * 6;
            m.Triangles[f] = v; m.Triangles[f + 1] = v + 1; m.Triangles[f + 2] = v + 2;
            m.Triangles[f + 3] = v; m.Triangles[f + 4] = v + 2; m.Triangles[f + 5] = v + 3;
            m.UV[v] = new Uv(0, 0); m.UV[v + 1] = new Uv(1, 0); m.UV[v + 2] = new Uv(1, 1); m.UV[v + 3] = new Uv(0, 1);
        }
        public static Buffer Create(int mirrorRight = 1)
        {
            if (mirrorRight != 1 && mirrorRight != -1) throw new ArgumentOutOfRangeException("mirrorRight");
            Buffer b = new Buffer(mirrorRight); Quad(b.Branches, 0);
            for (int j = 0; j < 4; j++)
            {
                Uv uv = b.Branches.UV[j];
                b.Branches.Vertices[j] = new Point(mirrorRight * (uv.X - PivotX) * Size, (uv.Y - PivotY) * Size, 0);
                b.Branches.UV[j] = new Uv(uv.X * .5, uv.Y);
            }
            b.Core.Vertices[0] = new Point(0, 0, .025); b.Core.UV[0] = new Uv(.5, .5);
            for (int j = 0; j < 12; j++)
            {
                double theta = j * (2 * Math.PI) / 12, ca = Math.Cos(theta), sa = Math.Sin(theta);
                b.Core.Vertices[j + 1] = new Point(mirrorRight * .065 * ca, .065 * sa, .025);
                b.Core.UV[j + 1] = new Uv(.5 + .5 * ca, .5 + .5 * sa);
                b.Core.Triangles[j * 3] = 0; b.Core.Triangles[j * 3 + 1] = j + 1; b.Core.Triangles[j * 3 + 2] = (j + 1) % 12 + 1;
                Quad(b.Sparks, j);
            }
            return b;
        }
        // Normative motion.sample(t). Host controls receipt ownership and actual Land/contact0.
        // One aggregate hits3 receipt is ONE playback, never three inferred hits or particle events.
        public static void Sample(Buffer b, double t)
        {
            if (b == null) throw new ArgumentNullException("b");
            if (!Finite(t)) throw new ArgumentOutOfRangeException("t");
            b.VisualAge = t;
            double branches = -NativeLead < t && t < BranchEnd ? 1 : 0;
            for (int i = 0; i < 4; i++) b.Branches.Alpha[i] = branches;
            double core = t < 0 ? 0 : 1 - Smooth((t - .018) / .042);
            for (int i = 0; i < 13; i++) b.Core.Alpha[i] = core;
            for (int i = 0; i < ParticleCount; i++)
            {
                double ang = Angles[i], birth = .006 * (i % 4), life = .160 + .011 * (i % 3), age = Math.Max(0, t - birth);
                double alpha = birth <= t && t <= birth + life ? Smooth(age / .008) * (1 - Smooth((age - life * .46) / (life * .54))) : 0;
                double originR = .13 + .035 * (i % 5), speed = .70 + .12 * (i % 4), dist = originR + speed * (1 - Math.Exp(-7 * age)) / 7;
                double ca = Math.Cos(ang), sa = Math.Sin(ang), x = ca * dist, y = sa * dist - .4 * age * age;
                double length = (.026 + .009 * (i % 3)) * (1 - .6 * Smooth(age / life)), w = .005 + .0015 * (i % 2);
                for (int j = 0; j < 4; j++)
                {
                    double u = j == 0 || j == 3 ? -length : length, v = j < 2 ? -w : w;
                    b.Sparks.Vertices[4 * i + j] = new Point(b.MirrorRight * (x + ca * u - sa * v), y + sa * u + ca * v, .04);
                    b.Sparks.Alpha[4 * i + j] = alpha;
                }
                b.ParticleCenters[i] = new Point(b.MirrorRight * x, y, .04); b.ParticleActive[i] = alpha > 0;
            }
            b.LightEnvelope = -NativeLead <= t && t < 0 ? .018 * Smooth((t + NativeLead) / NativeLead) : (0 <= t && t <= LightEnd ? 1 - Smooth((t - .015) / .060) : 0);
            b.TargetAreaW = 42 * b.LightEnvelope; b.FloorAreaW = 14 * b.LightEnvelope;
        }
        // False means no available anticipation; clear all visible channels. Positive time unchanged.
        public static bool SampleReceipt(Buffer b, double age, double availableA)
        {
            if (b == null) throw new ArgumentNullException("b");
            double? native = NativeAge(age, availableA);
            if (native.HasValue) { Sample(b, native.Value); return true; }
            b.VisualAge = age; b.LightEnvelope = b.TargetAreaW = b.FloorAreaW = 0;
            for (int i = 0; i < b.Batches.Length; i++) Array.Clear(b.Batches[i].Alpha, 0, b.Batches[i].Alpha.Length);
            Array.Clear(b.ParticleActive, 0, b.ParticleActive.Length); return false;
        }
    }
}
