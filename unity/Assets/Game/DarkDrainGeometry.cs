using System;

namespace DeckRogue.Game
{
    // Frozen dark-drain R1 candidate, 2026-10-09. Unity/device visuals remain unverified.
    // Manifest SHA256: 80e3eecf51bb4b59d01ca26d0721f338d319df46136f042f2a3e92e208b033d6.
    // Contact-relative seconds. Coordinates: captured camera right/up/toward-camera in visible target H.
    // Decorative positive-DamageDealt absorption only; never emits, delays, or settles HpHealed.
    // No Unity, I/O, runtime RNG, or managed allocation in ConfigureSource/Sample.
    internal static class DarkDrainGeometry
    {
        public const int TextureKind = 0, CoreKind = 1, SolidKind = 2;
        public const int ShadowRole = 0, PrimaryRole = 1, EdgeRole = 2, CoreRole = 3;
        public const int RibbonRows = 33, RibbonColumns = 5, IntakeCount = 8, ReturnCount = 12;
        public const int VertexCount = 419, TriangleCount = 560, BatchCount = 4;
        public const double NativeLead = .130, BodyEnd = .092, CoreEnd = .062;
        public const double ValidSourceEnd = .301, InvalidSourceEnd = .104;
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
            // UV already includes the frozen material flow. Do not add a second UV offset.
            public readonly Uv[] UV;
            // Alpha and Coverage remain separate. Texture alpha * Alpha * Coverage exactly once.
            public readonly double[] Alpha, Gain, Coverage;
            public readonly int[] Role, Triangles;
            internal MeshData(int vertices, int triangles, int kind, double gain, int role)
            {
                Kind = kind; Vertices = new Point[vertices]; UV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices]; Coverage = new double[vertices];
                Role = new int[vertices]; Triangles = new int[triangles * 3];
                for (int i = 0; i < vertices; i++) { Gain[i] = gain; Coverage[i] = 1; Role[i] = role; }
            }
        }
        public sealed class Buffer
        {
            // Two inward paths share one material batch, without losing per-vertex coverage.
            public readonly MeshData[] Batches;
            public MeshData Main { get { return Batches[0]; } }
            public MeshData Core { get { return Batches[1]; } }
            public MeshData Intake { get { return Batches[2]; } }
            public MeshData Return { get { return Batches[3]; } }
            public readonly bool[] IntakeActive, ReturnActive;
            public readonly Point[] IntakeCenters, ReturnCenters;
            public bool SourceValid { get; internal set; }
            public bool ReturnEnabled { get { return SourceValid; } }
            public Point SourceH { get; internal set; }
            public double VisualAge, LightEnvelope, TargetAreaW, FloorAreaW;
            internal Buffer()
            {
                Batches = new[] { new MeshData(330, 512, TextureKind, 2.1, PrimaryRole),
                    new MeshData(9, 8, CoreKind, 3.4, CoreRole),
                    new MeshData(32, 16, SolidKind, 1.8, EdgeRole),
                    new MeshData(48, 24, SolidKind, 2.2, EdgeRole) };
                IntakeActive = new bool[IntakeCount]; ReturnActive = new bool[ReturnCount];
                IntakeCenters = new Point[IntakeCount]; ReturnCenters = new Point[ReturnCount];
            }
        }
        static readonly double[,] Curves = new double[,] {
            { -.46, .30, -.62, -.37, .24, -.12 },
            { .18, .55, .58, .48, .22, .055 }
        };
        static readonly double[] ReturnBirths = new[] { 0.0, .006, .011, .019, .028, .035, .042, .052, .059, .066, .074, .082 };
        public static double Lead() { return NativeLead; }
        // No-argument End is the maximum pooled lifetime; use End(buffer) after source capture.
        public static double End() { return ValidSourceEnd; }
        public static double End(bool sourceValid) { return sourceValid ? ValidSourceEnd : InvalidSourceEnd; }
        public static double End(Buffer b) { if (b == null) throw new ArgumentNullException("b"); return End(b.SourceValid); }
        static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        static double Fade(double t, double hold, double end) { return 0 <= t && t < end ? 1 - Smooth((t - hold) / (end - hold)) : 0; }
        static double Hypot(double x, double y)
        {
            x = Math.Abs(x); y = Math.Abs(y); double big = Math.Max(x, y), small = Math.Min(x, y);
            if (big == 0) return 0; double q = small / big; return big * Math.Sqrt(1 + q * q);
        }
        static double SafeAge(double age)
        {
            if (double.IsNaN(age) || age < -1000000) return -NativeLead - 1e-6;
            if (age > 1000000) return ValidSourceEnd;
            return age;
        }
        public static double Clock(double age, double availableAnticipation = double.NaN)
        {
            age = SafeAge(age);
            if (age >= 0 || double.IsNaN(availableAnticipation) || availableAnticipation >= NativeLead) return age;
            if (availableAnticipation <= 0 || age < -availableAnticipation) return -NativeLead - 1e-6;
            // Algebraically identical C1 polynomial in r=1-q; avoids cancellation at contact.
            double r = Clamp(-age / availableAnticipation);
            return age - (NativeLead - availableAnticipation) * r * r * (2 - r);
        }
        public static double VisualClock(double age, double availableAnticipation = double.NaN) { return Clock(age, availableAnticipation); }
        // Light is independently evaluated. Reference Blender watts are not Unity point intensities.
        public static double Light(double visualAge)
        {
            double t = SafeAge(visualAge), s = Smooth((t + NativeLead) / NativeLead);
            return t >= 0 ? Fade(t, .022, InvalidSourceEnd) : .045 * s * s;
        }
        // Capture once from (sourceWorld - targetContactWorld), projected on the captured camera basis / H.
        // Caller must pass explicit validity from its real board-center capture. Never infer a hand/socket.
        public static void ConfigureSource(Buffer b, Point sourceH, bool sourceValid = true)
        {
            if (b == null) throw new ArgumentNullException("b");
            bool valid = sourceValid && Finite(sourceH.X) && Finite(sourceH.Y) && Finite(sourceH.Z)
                && Hypot(sourceH.X, sourceH.Y) > .05;
            b.SourceValid = valid; b.SourceH = valid ? sourceH : new Point(-1.10, .035, 0);
        }
        public static void ConfigureSource(Buffer b, double x, double y, double z, bool sourceValid = true)
        { ConfigureSource(b, new Point(x, y, z), sourceValid); }
        static void Quad(int[] tri, int q, int a, int b, int c, int d)
        {
            int n = q * 6; tri[n] = a; tri[n + 1] = b; tri[n + 2] = c;
            tri[n + 3] = a; tri[n + 4] = c; tri[n + 5] = d;
        }
        public static Buffer Create()
        {
            Buffer b = new Buffer(); ConfigureSource(b, default(Point), false);
            for (int k = 0; k < 2; k++) for (int i = 0; i < 32; i++) for (int j = 0; j < 4; j++)
            {
                int offset = k * 165, a = offset + i * 5 + j;
                Quad(b.Main.Triangles, k * 128 + i * 4 + j, a, a + 1, a + 6, a + 5);
            }
            b.Core.UV[0] = new Uv(.5, .5);
            for (int k = 0; k < 8; k++)
            {
                b.Core.UV[k + 1] = new Uv(.5 + .5 * Math.Cos(k * Math.PI / 4), .5 + .5 * Math.Sin(k * Math.PI / 4));
                b.Core.Triangles[k * 3] = 0; b.Core.Triangles[k * 3 + 1] = k + 1; b.Core.Triangles[k * 3 + 2] = 1 + (k + 1) % 8;
            }
            for (int i = 0; i < IntakeCount; i++) Quad(b.Intake.Triangles, i, i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3);
            for (int i = 0; i < ReturnCount; i++)
            {
                Quad(b.Return.Triangles, i, i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3);
                for (int j = 0; j < 4; j++) b.Return.Role[i * 4 + j] = i % 4 == 0 ? CoreRole : EdgeRole;
            }
            return b;
        }
        static Point Path(double q, int k)
        {
            if (q < 0) return new Point(Curves[k, 0] + 3 * q * (Curves[k, 2] - Curves[k, 0]), Curves[k, 1] + 3 * q * (Curves[k, 3] - Curves[k, 1]), 0);
            if (q > 1) return new Point(3 * (q - 1) * -Curves[k, 4], 3 * (q - 1) * -Curves[k, 5], 0);
            double a = Math.Pow(1 - q, 3), c = 3 * Math.Pow(1 - q, 2) * q, d = 3 * (1 - q) * q * q;
            return new Point(a * Curves[k, 0] + c * Curves[k, 2] + d * Curves[k, 4], a * Curves[k, 1] + c * Curves[k, 3] + d * Curves[k, 5], 0);
        }
        static void Ribbon(Buffer b, double t, int k)
        {
            MeshData m = b.Main; double q = (t + NativeLead) / NativeLead;
            double alpha = -NativeLead <= t && t < BodyEnd ? .94 * Smooth((t + NativeLead) / .02) : 0;
            for (int i = 0; i < RibbonRows; i++)
            {
                double u = i / 32.0, s = q - (1 - u) * .55;
                Point c = Path(s, k), a = Path(s - 1e-4, k), z = Path(s + 1e-4, k);
                double dx = z.X - a.X, dy = z.Y - a.Y, len = Math.Max(1e-7, Hypot(dx, dy));
                double nx = -dy / len, ny = dx / len;
                double w = (k == 0 ? .042 : .033) * Math.Pow(Math.Max(0, Math.Sin(Math.PI * u)), .45) * (.75 + .25 * u);
                double v = Clamp(u + .018 * Math.Sin(t * 21 + k)), px = 560 + 300 * v - 150 * v * v, py = 470 - 420 * v + 60 * v * v;
                double coverage = Smooth(s / .07) * (1 - Smooth((s - .98) / .17));
                for (int j = 0; j < RibbonColumns; j++)
                {
                    int n = k * 165 + i * 5 + j; double side = (j - 2) / 2.0;
                    m.Vertices[n] = new Point(c.X + nx * side * w, c.Y + ny * side * w, .023 + .009 * k + .005 * (1 - side * side));
                    m.UV[n] = new Uv((px + side * 62) / 1774, 1 - (py + side * 32) / 887);
                    m.Alpha[n] = alpha; m.Coverage[n] = coverage;
                }
            }
        }
        static void Core(Buffer b, double t)
        {
            double alpha = Fade(t, .016, CoreEnd), r = .027 * (1 + .3 * Smooth(t / .025));
            b.Core.Vertices[0] = new Point(0, 0, .059); b.Core.Alpha[0] = alpha;
            for (int k = 0; k < 8; k++)
            {
                b.Core.Vertices[k + 1] = new Point(r * Math.Cos(k * Math.PI / 4), r * .78 * Math.Sin(k * Math.PI / 4), .056);
                b.Core.Alpha[k + 1] = alpha;
            }
        }
        static void Diamond(MeshData m, int particle, Point center, double theta, double length, double width, double alpha)
        {
            double ca = Math.Cos(theta), sa = Math.Sin(theta);
            for (int j = 0; j < 4; j++)
            {
                double a = j == 0 ? -length / 2 : j == 2 ? length / 2 : 0;
                double c = j == 1 ? -width / 2 : j == 3 ? width / 2 : 0;
                int n = particle * 4 + j;
                m.Vertices[n] = new Point(center.X + ca * a - sa * c, center.Y + sa * a + ca * c, center.Z);
                m.Alpha[n] = alpha;
            }
        }
        static void IntakeParticle(Buffer b, double t, int i)
        {
            double born = -.115 + .008 * i, life = .135 + .003 * (i % 3), age = t - born;
            double q = Clamp(age / life), angle = 2.399963 * i + .30 * q, r = (.28 + .025 * (i % 4)) * (1 - q);
            Point center = new Point(Math.Cos(angle) * r, Math.Sin(angle) * r, .05);
            double alpha = Smooth(age / .012) * Fade(age, .06, life);
            b.IntakeCenters[i] = center; b.IntakeActive[i] = alpha > 0;
            Diamond(b.Intake, i, center, angle + Math.PI + .25, .021 * (1 - .4 * q), .0085, alpha);
        }
        static void ReturnParticle(Buffer b, double t, int i)
        {
            double life = .195 + .012 * (i % 3), age = t - ReturnBirths[i], q = Clamp(age / life);
            double p = 1 - Math.Pow(1 - q, 1.65), len = Math.Max(.001, Hypot(b.SourceH.X, b.SourceH.Y));
            double nx = -b.SourceH.Y / len, ny = b.SourceH.X / len;
            double amplitude = (.035 + .009 * (i % 4)) * (i % 2 == 1 ? -1 : 1), offset = amplitude * Math.Sin(Math.PI * p);
            Point center = new Point(b.SourceH.X * p + nx * offset, b.SourceH.Y * p + ny * offset, b.SourceH.Z * p + .05);
            double dx = b.SourceH.X + nx * amplitude * Math.PI * Math.Cos(Math.PI * p);
            double dy = b.SourceH.Y + ny * amplitude * Math.PI * Math.Cos(Math.PI * p);
            double length = (.025 + .010 * (i % 3)) * (.35 + .65 * Math.Sin(Math.PI * q)), width = (.010 + .002 * (i % 2)) * (1 - .4 * q);
            double alpha = b.SourceValid ? Smooth(age / .012) * Fade(age, life * .55, life) : 0;
            b.ReturnCenters[i] = center; b.ReturnActive[i] = alpha > 0;
            Diamond(b.Return, i, center, Math.Atan2(dy, dx), length, width, alpha);
        }
        public static void Sample(Buffer b, double age, double availableAnticipation = double.NaN)
        {
            if (b == null) throw new ArgumentNullException("b");
            double t = Clock(age, availableAnticipation); b.VisualAge = t; b.LightEnvelope = Light(t);
            b.TargetAreaW = 38 * b.LightEnvelope; b.FloorAreaW = 13 * b.LightEnvelope;
            Ribbon(b, t, 0); Ribbon(b, t, 1); Core(b, t);
            for (int i = 0; i < IntakeCount; i++) IntakeParticle(b, t, i);
            for (int i = 0; i < ReturnCount; i++) ReturnParticle(b, t, i);
        }
    }
}
