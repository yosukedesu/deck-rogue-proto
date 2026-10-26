using System;

namespace DeckRogue.Game
{
    // Frozen heal uplift R1, 2026-10-09. Unity/device appearance remains unverified.
    // Manifest SHA256: 4daa99c59410e7ce6ba61fc5cf48e0bbdce377f2ad80f1c93ff051a47ba75118.
    // Actual positive recovered-amount receipt only. P=0; no prediction, HP timing or hitstop.
    // Captured camera-local right/up/towards-camera, in stable base-alpha-visible recipient H.
    // Adapter places origin at feet + cameraUp*(.4652173913043478*H), with separate depth bias.
    // No Unity, engine RNG, I/O, or managed allocations in warmed Sample.
    internal static class HealContinuousGeometry
    {
        public const int MoteKind = 0, TailKind = 1, AuraKind = 2;
        public const int ShadowRole = 0, PrimaryRole = 1, EdgeRole = 2, CoreRole = 3;
        public const int MoteCount = 16, TailCount = 8, VertexCount = 193, TriangleCount = 168, BatchCount = 3;
        public const double NativeLead = 0, VisibleEnd = .541, AuraEnd = .205, LightEnd = .240;
        public const double FeetAnchorUpH = .4652173913043478;
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
            public readonly double[] Alpha, Gain, Coverage;
            public readonly int[] Role, Triangles;
            internal MeshData(int vertices, int triangles, int kind, double gain, int role)
            {
                Kind = kind; Vertices = new Point[vertices]; UV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices]; Coverage = new double[vertices];
                Role = new int[vertices]; Triangles = new int[triangles * 3];
                for (int i = 0; i < vertices; i++) { Gain[i] = gain; Role[i] = role; Coverage[i] = 1; }
            }
        }
        public sealed class Buffer
        {
            public readonly MeshData[] Batches;
            public MeshData Aura { get { return Batches[0]; } }
            public MeshData Tails { get { return Batches[1]; } }
            public MeshData Motes { get { return Batches[2]; } }
            public readonly Point[] MoteCenters;
            public readonly bool[] MoteActive, TailActive;
            public double VisualAge, LightEnvelope, TargetAreaW, FloorAreaW;
            internal Buffer()
            {
                Batches = new[] { new MeshData(9, 8, AuraKind, 1.0, PrimaryRole),
                    new MeshData(120, 128, TailKind, 1.15, EdgeRole),
                    new MeshData(64, 32, MoteKind, 1.65, PrimaryRole) };
                MoteCenters = new Point[MoteCount]; MoteActive = new bool[MoteCount]; TailActive = new bool[TailCount];
            }
        }
        static readonly double[] Births = new[] { 0.0, .006, .013, .021, .030, .037, .046, .054, .063, .071, .080, .088, .097, .107, .116, .126 };
        public static double Lead() { return NativeLead; }
        public static double End() { return VisibleEnd; }
        static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        static double Fade(double t, double hold, double end) { return 0 <= t && t < end ? 1 - Smooth((t - hold) / (end - hold)) : 0; }
        static double SafeAge(double age)
        {
            if (double.IsNaN(age) || age < -1000000) return -1;
            if (age > 1000000) return VisibleEnd;
            return age;
        }
        public static double Clock(double age) { return SafeAge(age); }
        public static double Light(double age)
        { double t = SafeAge(age); return Smooth(t / .030) * Fade(t, .055, LightEnd); }
        static void Quad(int[] tri, int q, int a, int b, int c, int d)
        {
            int n = q * 6; tri[n] = a; tri[n + 1] = b; tri[n + 2] = c;
            tri[n + 3] = a; tri[n + 4] = c; tri[n + 5] = d;
        }
        public static Buffer Create()
        {
            Buffer b = new Buffer(); b.Aura.UV[0] = new Uv(.5, .5);
            for (int k = 0; k < 8; k++)
            {
                b.Aura.UV[k + 1] = new Uv(.5 + .5 * Math.Cos(k * Math.PI / 4), .5 + .5 * Math.Sin(k * Math.PI / 4));
                b.Aura.Triangles[k * 3] = 0; b.Aura.Triangles[k * 3 + 1] = 1 + k; b.Aura.Triangles[k * 3 + 2] = 1 + (k + 1) % 8;
            }
            for (int i = 0; i < TailCount; i++)
            {
                for (int k = 0; k < 5; k++) for (int j = 0; j < 3; j++) b.Tails.UV[i * 15 + k * 3 + j] = new Uv(j / 2.0, k / 4.0);
                for (int k = 0; k < 4; k++) for (int j = 0; j < 2; j++)
                { int a = i * 15 + k * 3 + j; Quad(b.Tails.Triangles, i * 8 + k * 2 + j, a, a + 1, a + 4, a + 3); }
            }
            for (int i = 0; i < MoteCount; i++)
            {
                Quad(b.Motes.Triangles, i, i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3);
                for (int j = 0; j < 4; j++)
                {
                    int x = j == 0 || j == 3 ? -1 : 1, y = j < 2 ? 1 : -1;
                    b.Motes.UV[i * 4 + j] = new Uv((520 + x * 52) / 1774.0, 1 - (154 + y * 52) / 887.0);
                }
            }
            return b;
        }
        static Point Center(double t, int i, out double q, out double age, out double life)
        {
            age = Math.Max(0, t - Births[i]); life = .310 + .035 * (i % 4); q = Clamp(age / life);
            double rise = (.58 + .045 * (i % 5)) * (.65 * q + .35 * q * q);
            double x = (.24 * Math.Sin(i * 2.399963)) * (1 - .14 * q) + .035 * Math.Sin(i * 1.33 + q * 2.5) * Math.Sin(Math.PI * q);
            return new Point(x, -.24 + .100 * (i % 3) + rise, .040 + .002 * (i % 4));
        }
        static void Mote(Buffer b, double t, int i)
        {
            double q, age, life; Point p = Center(t, i, out q, out age, out life);
            double alpha = Smooth((t - Births[i]) / .026) * Fade(t - Births[i], life * .40, life) * .90;
            double r = (.042 + .006 * (i % 3)) * (.70 + .30 * Math.Sin(Math.PI * q));
            double theta = .08 * Math.Sin(i + q * 4), ca = Math.Cos(theta), sa = Math.Sin(theta);
            b.MoteCenters[i] = p; b.MoteActive[i] = alpha > 0;
            for (int j = 0; j < 4; j++)
            {
                double x = j == 0 || j == 3 ? -r : r, y = j < 2 ? -r * 1.15 : r * 1.15;
                int n = i * 4 + j; b.Motes.Vertices[n] = new Point(p.X + ca * x - sa * y, p.Y + sa * x + ca * y, p.Z); b.Motes.Alpha[n] = alpha;
            }
        }
        static void Tail(Buffer b, double t, int tail)
        {
            int i = tail * 2; double q, age, life; Point p = Center(t, i, out q, out age, out life);
            double alpha = Smooth((t - Births[i]) / .035) * Fade(t - Births[i], life * .30, life) * .28;
            double length = .085 * Smooth(age / .055) * (.65 + .35 * q); b.TailActive[tail] = alpha > 0;
            for (int k = 0; k < 5; k++)
            {
                double u = k / 4.0, x = p.X - .015 * Math.Sin(i + q * 3) * (1 - u), y = p.Y - (1 - u) * length;
                double w = .010 * Math.Pow(Math.Sin(Math.PI * u), .6);
                for (int j = 0; j < 3; j++)
                {
                    int n = tail * 15 + k * 3 + j; b.Tails.Vertices[n] = new Point(x + (j - 1) * w, y, p.Z - .006); b.Tails.Alpha[n] = alpha;
                }
            }
        }
        static void Aura(Buffer b, double t)
        {
            double alpha = .15 * Smooth(t / .035) * Fade(t, .055, AuraEnd), r = .11 + .025 * Smooth(t / .09);
            b.Aura.Vertices[0] = new Point(0, -.14, .025); b.Aura.Alpha[0] = alpha;
            for (int k = 0; k < 8; k++)
            {
                b.Aura.Vertices[k + 1] = new Point(r * Math.Cos(k * Math.PI / 4), -.14 + r * 1.65 * Math.Sin(k * Math.PI / 4), .024); b.Aura.Alpha[k + 1] = alpha;
            }
        }
        public static void Sample(Buffer b, double age)
        {
            if (b == null) throw new ArgumentNullException("b");
            double t = SafeAge(age); b.VisualAge = t; b.LightEnvelope = Light(t);
            b.TargetAreaW = 28 * b.LightEnvelope; b.FloorAreaW = 10 * b.LightEnvelope;
            Aura(b, t); for (int i = 0; i < TailCount; i++) Tail(b, t, i); for (int i = 0; i < MoteCount; i++) Mote(b, t, i);
        }
    }
}
