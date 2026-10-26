using System;

namespace DeckRogue.Game
{
    // Frozen Light Ray R2 reconstruction, 2026-10-09. Unity/device visuals unverified.
    // Manifest SHA256: 98c787c55cc332a9fb5ac3b318cde6e67c2bdb8c456a1c0b5da913c676e02952.
    // Contact-relative seconds; local captured camera right/up/towards-camera in stable visible H.
    // No source/target endpoints, gameplay events, I/O, Unity dependency, RNG or warm allocation.
    internal static class LightRayGeometry
    {
        public const int RayKind = 0, AiryKind = 1, CoreKind = 2, SolidKind = 3;
        public const int ShadowRole = 0, PrimaryRole = 1, EdgeRole = 2, CoreRole = 3;
        public const int VertexCount = 229, TriangleCount = 252, BatchCount = 4;
        public const int RayRows = 33, RayColumns = 3, AiryCount = 3, MoteCount = 10;
        public const double NativeLead = .100, VisibleEnd = .200, MainEnd = .085;
        public const double Angle = -.16;
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
            // UV1.x is constant one in the renderer. Analytic/texture coverage is fragment-owned.
            public readonly double[] Alpha, Gain;
            public readonly int[] Role, Triangles;
            internal MeshData(int vertices, int triangles, int kind, double gain)
            {
                Kind = kind; Vertices = new Point[vertices]; UV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices];
                Role = new int[vertices]; Triangles = new int[triangles * 3];
                for (int i = 0; i < vertices; i++) { Gain[i] = gain; Role[i] = EdgeRole; }
            }
        }
        public sealed class Buffer
        {
            public readonly MeshData[] Batches;
            public MeshData Main { get { return Batches[0]; } }
            public MeshData Airy { get { return Batches[1]; } }
            public MeshData Core { get { return Batches[2]; } }
            public MeshData Motes { get { return Batches[3]; } }
            public readonly bool[] AiryActive, MoteActive;
            public readonly Point[] MoteCenters;
            public double VisualAge, LightEnvelope;
            // These are explicit R2 Blender reference watts, not Unity point-light intensity.
            public double TargetAreaW, FloorAreaW;
            internal Buffer()
            {
                Batches = new[] { new MeshData(99, 128, RayKind, 2.5),
                    new MeshData(81, 96, AiryKind, 1.8), new MeshData(9, 8, CoreKind, 3.6),
                    new MeshData(40, 20, SolidKind, 1.8) };
                AiryActive = new bool[AiryCount]; MoteActive = new bool[MoteCount];
                MoteCenters = new Point[MoteCount];
            }
        }
        static readonly double Cos = Math.Cos(Angle), Sin = Math.Sin(Angle);
        static readonly double[] AiryBirths = { 0, .011, .024 }, AiryLives = { .155, .172, .148 };
        static readonly double[] AiryY = { -.068, .095, .028 }, AiryLength = { .30, .24, .18 };
        static readonly double[] AiryWidth = { .010, .007, .008 }, AiryAngle = { -.035, .09, -.09 };
        public static double Lead() { return NativeLead; }
        public static double End() { return VisibleEnd; }
        public static double End(Buffer b) { if (b == null) throw new ArgumentNullException("b"); return VisibleEnd; }
        static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        static double Fade(double t, double hold, double end)
        { return 0 <= t && t < end ? 1 - Smooth((t - hold) / (end - hold)) : 0; }
        static double SafeAge(double age)
        {
            if (double.IsNaN(age) || age < -1000000) return -NativeLead;
            return age > 1000000 ? VisibleEnd : age;
        }
        // Normative R2 clock_reference.py: quadratic correction, NOT Dark's cubic correction.
        // NaN means the reference's None: no anticipation was available. Sample fails closed.
        public static double Clock(double age, double availableAnticipation = double.NaN)
        {
            age = SafeAge(age);
            if (age >= 0) return age;
            double a = double.IsNaN(availableAnticipation) ? NativeLead : Math.Min(NativeLead, Math.Max(0, availableAnticipation));
            if (a <= 0) return double.NaN;
            if (age < -a) return -NativeLead;
            double q = age / a;
            return age - (NativeLead - a) * (q * q);
        }
        public static double VisualClock(double age, double availableAnticipation = double.NaN)
        { return Clock(age, availableAnticipation); }
        public static double Light(double visualAge)
        {
            double t = SafeAge(visualAge);
            if (t >= 0) return Fade(t, .022, .110);
            double q = Smooth((t + NativeLead) / NativeLead); return .018 * q * q * q;
        }
        public static double Head(double visualAge)
        {
            double t = SafeAge(visualAge);
            if (t <= 0) { double q = Clamp((t + NativeLead) / NativeLead); return -.90 * (1 - (1.2 * q * q - .2 * q * q * q)); }
            return 16.2 * (1 - Math.Exp(-24 * t)) / 24;
        }
        static Point Rotate(double x, double y, double z)
        { return new Point(Cos * x - Sin * y, Sin * x + Cos * y, z); }
        static Point Rotate(double x, double y, double z, double angle)
        { double c = Math.Cos(angle), s = Math.Sin(angle); return new Point(c * x - s * y, s * x + c * y, z); }
        static void Quad(int[] triangles, int q, int a, int b, int c, int d)
        {
            int n = q * 6; triangles[n] = a; triangles[n + 1] = b; triangles[n + 2] = c;
            triangles[n + 3] = a; triangles[n + 4] = c; triangles[n + 5] = d;
        }
        public static Buffer Create()
        {
            Buffer b = new Buffer();
            for (int row = 0; row < RayRows; row++) for (int col = 0; col < RayColumns; col++)
            {
                double u = row / 32.0, side = col - 1;
                b.Main.UV[row * 3 + col] = new Uv((528 + side * 32) / 1774, 1 - (835 - 740 * u) / 887);
                if (row < 32 && col < 2) { int n = row * 3 + col; Quad(b.Main.Triangles, row * 2 + col, n, n + 1, n + 4, n + 3); }
            }
            for (int k = 0; k < AiryCount; k++) for (int row = 0; row < 9; row++) for (int col = 0; col < 3; col++)
            {
                int n = k * 27 + row * 3 + col;
                b.Airy.UV[n] = new Uv(row / 8.0, col / 2.0);
                if (row < 8 && col < 2) Quad(b.Airy.Triangles, k * 16 + row * 2 + col, n, n + 1, n + 4, n + 3);
            }
            b.Core.Vertices[0] = new Point(0, 0, .064); b.Core.UV[0] = new Uv(.5, .5);
            for (int k = 0; k < 8; k++)
            {
                double c = Math.Cos(k * Math.PI / 4), s = Math.Sin(k * Math.PI / 4);
                b.Core.Vertices[k + 1] = Rotate(.14 * c, .026 * s, .060);
                b.Core.UV[k + 1] = new Uv(.5 + .5 * c, .5 + .5 * s);
                b.Core.Triangles[k * 3] = 0; b.Core.Triangles[k * 3 + 1] = k + 1; b.Core.Triangles[k * 3 + 2] = 1 + (k + 1) % 8;
            }
            for (int i = 0; i < MoteCount; i++)
            {
                Quad(b.Motes.Triangles, i, i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3);
                for (int j = 0; j < 4; j++) b.Motes.Role[i * 4 + j] = i % 3 == 0 ? CoreRole : EdgeRole;
            }
            return b;
        }
        static void Ray(Buffer b, double t)
        {
            double h = Head(t), alpha = -NativeLead <= t && t < MainEnd ? Smooth((t + NativeLead) / .024) * (1 - Smooth(Math.Max(0, t - .020) / .065)) : 0;
            for (int row = 0; row < RayRows; row++)
            {
                double u = row / 32.0, x = h - .78 * (1 - u), w = .027 * Math.Pow(Math.Sin(Math.PI * u), .55);
                for (int col = 0; col < 3; col++)
                {
                    int n = row * 3 + col; double side = col - 1;
                    b.Main.Vertices[n] = Rotate(x, side * w, .026 + .003 * (1 - side * side)); b.Main.Alpha[n] = alpha;
                }
            }
        }
        static void AfterRay(Buffer b, double t, int k)
        {
            double age = Math.Max(0, t - AiryBirths[k]), life = AiryLives[k];
            double x = .025 + (2.1 + .3 * k) * (1 - Math.Exp(-8 * age)) / 8;
            double y = AiryY[k] * (1 + .35 * age), length = AiryLength[k] * (1 - .65 * Clamp(age / life));
            double alpha = .58 * Smooth((t - AiryBirths[k]) / .012) * Fade(t - AiryBirths[k], .022, life);
            b.AiryActive[k] = alpha > 0;
            for (int row = 0; row < 9; row++)
            {
                double u = row / 8.0;
                for (int col = 0; col < 3; col++)
                {
                    int n = k * 27 + row * 3 + col; double side = col - 1;
                    b.Airy.Vertices[n] = Rotate(x + (u - .5) * length, y + side * AiryWidth[k] * Math.Sin(Math.PI * u), .035 + .003 * k, Angle + AiryAngle[k]);
                    b.Airy.Alpha[n] = alpha;
                }
            }
        }
        static void Mote(Buffer b, double t, int i)
        {
            double born = .004 + .004 * i, age = Math.Max(0, t - born), life = .132 + .012 * (i % 4);
            double alpha = Smooth((t - born) / .010) * Fade(t - born, .025, life);
            double phi = -.46 + .16 * (i % 6), v = 1.2 + .19 * (i % 4), q = (1 - Math.Exp(-5 * age)) / 5;
            double x = .025 + Math.Cos(phi) * v * q, y = (i % 3 - 1) * .018 + Math.Sin(phi) * v * q;
            double length = (.024 + .005 * (i % 3)) * (1 - .5 * Clamp(age / life));
            b.MoteCenters[i] = Rotate(x, y, .051); b.MoteActive[i] = alpha > 0;
            for (int j = 0; j < 4; j++)
            {
                double a = j == 0 ? -length / 2 : j == 2 ? length / 2 : 0;
                double w = j == 1 ? -.007 / 2 : j == 3 ? .007 / 2 : 0;
                int n = i * 4 + j; b.Motes.Vertices[n] = Rotate(x + a, y + w, .051); b.Motes.Alpha[n] = alpha;
            }
        }
        public static void Sample(Buffer b, double age, double availableAnticipation = double.NaN)
        {
            if (b == null) throw new ArgumentNullException("b");
            double mapped = Clock(age, availableAnticipation), t = double.IsNaN(mapped) ? -NativeLead : mapped;
            b.VisualAge = t; b.LightEnvelope = Light(t);
            b.TargetAreaW = 60 * b.LightEnvelope; b.FloorAreaW = 20 * b.LightEnvelope;
            Ray(b, t); for (int k = 0; k < AiryCount; k++) AfterRay(b, t, k);
            double coreAlpha = Fade(t, .026, .072);
            for (int i = 0; i < 9; i++) b.Core.Alpha[i] = coreAlpha;
            for (int i = 0; i < MoteCount; i++) Mote(b, t, i);
        }
    }
}
