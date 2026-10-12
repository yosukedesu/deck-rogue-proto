using System;

namespace DeckRogue.Game
{
    // Frozen waterA R1 motion candidate, 2026-10-09. Unity/device visuals remain unverified.
    // Source manifest SHA256: eb703eb4f0661727a6f2fd899943bdb399d3a9baa43150e76786e81f207aa66f.
    // Contact-relative seconds; captured camera right/up/toward-camera; captured base-visible H.
    // No Unity, engine RNG, I/O, world-space caster offset, or allocation in Sample.
    internal static class WaterContinuousGeometry
    {
        public const int TextureKind = 0, CoreKind = 1, DropletKind = 2;
        public const int RibbonRows = 49, ParticleCount = 12;
        public const double NativeLead = .160, BodyEnd = .150, CoreEnd = .090;
        public const double VisibleEnd = .354, DiagnosticHorizon = .620, ParticleLife = .240;
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
            // UV is GPU-ready: ribbon flow is already added exactly once. SourceUV stays fixed.
            public readonly Uv[] UV, SourceUV;
            public readonly double[] Alpha, Gain;
            public readonly int[] Triangles;
            public Uv UvOffset;
            internal MeshData(int vertices, int triangles, int kind, double gain)
            {
                Kind = kind; Vertices = new Point[vertices];
                UV = new Uv[vertices]; SourceUV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices];
                Triangles = new int[triangles * 3];
                for (int i = 0; i < vertices; i++) Gain[i] = gain;
            }
        }
        public sealed class Buffer
        {
            // Fixed pool-ready topology: 147 + 147 + 4 + 84 vertices, 458 triangles, four batches.
            public readonly MeshData[] Batches;
            public MeshData Primary { get { return Batches[0]; } }
            public MeshData Secondary { get { return Batches[1]; } }
            public MeshData Core { get { return Batches[2]; } }
            public MeshData Particles { get { return Batches[3]; } }
            // ParticleActive means alpha > 0, including false on the exact zero-alpha birth frame.
            public readonly bool[] ParticleActive;
            public readonly Point[] ParticleCenters;
            public double VisualAge, LightEnvelope, TargetAreaW, FloorAreaW;
            internal readonly Point[] Centers, ParticleStarts, ParticleVelocities;
            internal readonly double[] WidthBases;
            internal Buffer()
            {
                Batches = new[] { new MeshData(147, 192, TextureKind, 1.35),
                    new MeshData(147, 192, TextureKind, 1.10),
                    new MeshData(4, 2, CoreKind, 3.0),
                    new MeshData(84, 72, DropletKind, 1.10) };
                ParticleActive = new bool[ParticleCount]; ParticleCenters = new Point[ParticleCount];
                ParticleStarts = new Point[ParticleCount]; ParticleVelocities = new Point[ParticleCount];
                Centers = new Point[RibbonRows]; WidthBases = new double[RibbonRows];
            }
        }
        static readonly double[,] Knots = new double[,] {
            { -.160, -.95, 1.2 }, { -.100, -.82, 3 }, { 0, 0, 11 },
            { .140, .88, 4.5 }, { .340, 1.45, 1.5 }, { .620, 1.68, .1 }
        };
        // Frozen launch specifications; no runtime RNG. birth, extra angle, speed, unscaled size.
        static readonly double[,] Specs = new double[,] {
            { 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
            { 0.004, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
            { 0.009, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 },
            { 0.035, 0.5538025156552036, 0.781381770579271, 0.0552109878293629 },
            { 0.03900000000000001, -0.6328030518934236, 0.9867369945327584, 0.030297687296996265 },
            { 0.044000000000000004, 0.061664644824827586, 0.8502843849475583, 0.043524457601980884 },
            { 0.07, 0.1181540250242763, 0.9482632634123469, 0.05014185475559578 },
            { 0.07400000000000001, 0.25177522002504815, 1.0755780824187333, 0.057097001477406185 },
            { 0.079, -0.31071945638799014, 1.125430356417509, 0.050080117253616996 },
            { 0.10500000000000001, 0.6239549361559444, 1.0962418684217539, 0.042639429252739454 },
            { 0.10900000000000001, -0.6757082081078837, 1.1861443107606338, 0.0477089878612777 },
            { 0.114, 0.27593909790993376, 0.6508527792538551, 0.03167280396479855 }
        };
        static readonly Point[] DropLocal = new[] {
            new Point(-.55, 0, 0), new Point(-.18, -.22, 0), new Point(.20, -.14, 0),
            new Point(.50, 0, 0), new Point(.20, .14, 0), new Point(-.18, .22, 0), new Point(0, 0, .12)
        };
        static readonly Uv[] DropUV = new[] {
            new Uv(0, .5), new Uv(.3, 0), new Uv(.7, .2), new Uv(1, .5),
            new Uv(.7, .8), new Uv(.3, 1), new Uv(.5, .5)
        };
        public static double Lead() { return NativeLead; }
        public static double End() { return VisibleEnd; }
        public static double Horizon() { return DiagnosticHorizon; }
        static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        static double SafeAge(double age)
        {
            // Invalid/extreme clock inputs fail closed and leave finite GPU-bound arrays.
            if (double.IsNaN(age) || age < -1000000) return -NativeLead - 1e-6;
            if (age > 1000000) return DiagnosticHorizon;
            return age;
        }
        public static double Clock(double age, double availableAnticipation = double.NaN)
        {
            age = SafeAge(age);
            if (age >= 0 || double.IsNaN(availableAnticipation) || availableAnticipation >= NativeLead) return age;
            if (availableAnticipation <= 0 || age < -availableAnticipation) return -NativeLead - 1e-6;
            double q = Clamp((age + availableAnticipation) / availableAnticipation);
            return -NativeLead + NativeLead * (q + (availableAnticipation / NativeLead - 1) * (q * q * q - q * q));
        }
        public static double VisualClock(double age, double availableAnticipation = double.NaN)
        { return Clock(age, availableAnticipation); }
        static double Head(double t)
        {
            int last = Knots.GetLength(0) - 1;
            if (t <= Knots[0, 0]) return Knots[0, 1];
            if (t >= Knots[last, 0]) return Knots[last, 1];
            int i = 0; while (i < last - 1 && t > Knots[i + 1, 0]) i++;
            double dt = Knots[i + 1, 0] - Knots[i, 0], q = (t - Knots[i, 0]) / dt;
            double q2 = q * q, q3 = q2 * q;
            return (2 * q3 - 3 * q2 + 1) * Knots[i, 1] + (q3 - 2 * q2 + q) * dt * Knots[i, 2]
                + (-2 * q3 + 3 * q2) * Knots[i + 1, 1] + (q3 - q2) * dt * Knots[i + 1, 2];
        }
        static Point Center(double lead, double t, bool secondary, int i)
        {
            double u = i / 48.0, length = 1.90 * (secondary ? .87 : 1);
            double s = lead - length * (1 - u) - (secondary ? .025 : 0);
            return new Point(s, .09 * Math.Sin(Math.PI * s / .95)
                + .012 * Math.Sin(8 * t + u * 7) * Math.Sin(Math.PI * u)
                + (secondary ? .075 * Math.Sin(Math.PI * u) : 0),
                .016 * Math.Sin(u * 5 + t * 6) + (secondary ? .013 : 0));
        }
        static Point Emitter(double t) { return Center(Head(t) * 1.25, t, false, 46); }
        static double BaseAlpha(double t)
        {
            return -NativeLead <= t && t < DiagnosticHorizon
                ? Smooth((t + NativeLead) / .025) * (1 - Smooth((t - .32) / (DiagnosticHorizon - .32))) : 0;
        }
        public static double CoreAlpha(double visualAge)
        {
            double t = SafeAge(visualAge);
            return 0 <= t && t < DiagnosticHorizon ? 1 - Smooth((t - .055) / .035) : 0;
        }
        // Light takes visual age; independent reference watts are NOT Unity point intensities.
        public static double Light(double visualAge)
        {
            double t = SafeAge(visualAge);
            double light = t >= 0 ? CoreAlpha(t) : -NativeLead <= t && t < DiagnosticHorizon
                ? .025 * Math.Pow(Smooth((t + NativeLead) / NativeLead), 3) : 0;
            return light * (1 - Smooth((t - .10) / .15));
        }
        static void Quad(int[] triangles, int q, int a, int b, int c, int d)
        {
            int n = q * 6; triangles[n] = a; triangles[n + 1] = b; triangles[n + 2] = c;
            triangles[n + 3] = a; triangles[n + 4] = c; triangles[n + 5] = d;
        }
        public static Buffer Create()
        {
            Buffer b = new Buffer();
            for (int k = 0; k < 2; k++)
            {
                MeshData m = b.Batches[k];
                for (int i = 0; i < RibbonRows; i++) for (int j = 0; j < 3; j++)
                    m.UV[i * 3 + j] = m.SourceUV[i * 3 + j] = new Uv(i / 48.0, .50 + .18 * (j - 1));
                for (int i = 0; i < 48; i++) for (int j = 0; j < 2; j++)
                    Quad(m.Triangles, i * 2 + j, i * 3 + j, i * 3 + j + 1, (i + 1) * 3 + j + 1, (i + 1) * 3 + j);
            }
            Quad(b.Core.Triangles, 0, 0, 1, 2, 3);
            for (int i = 0; i < RibbonRows; i++)
                b.WidthBases[i] = .112 * Math.Pow(Math.Max(0, Math.Sin(Math.PI * (i / 48.0))), .85);
            for (int i = 0; i < ParticleCount; i++)
            {
                int offset = i * 7;
                for (int j = 0; j < 7; j++) b.Particles.UV[offset + j] = b.Particles.SourceUV[offset + j] = DropUV[j];
                for (int j = 0; j < 6; j++)
                {
                    int n = (i * 6 + j) * 3;
                    b.Particles.Triangles[n] = offset + j;
                    b.Particles.Triangles[n + 1] = offset + (j + 1) % 6;
                    b.Particles.Triangles[n + 2] = offset + 6;
                }
                double born = Specs[i, 0], eps = 1e-5;
                Point start = Emitter(born), ep = Emitter(born + eps), em = Emitter(born - eps);
                b.ParticleStarts[i] = start;
                b.ParticleVelocities[i] = new Point(
                    (ep.X - em.X) / 2 / eps * .28 + Math.Cos(Specs[i, 1]) * Specs[i, 2] * .45,
                    (ep.Y - em.Y) / 2 / eps * .28 + Math.Sin(Specs[i, 1]) * Specs[i, 2] + .45,
                    (ep.Z - em.Z) / 2 / eps * .28 + .04 * (i % 3 - 1));
            }
            return b;
        }
        static void Ribbon(Buffer b, double t, bool secondary, bool mirrorX)
        {
            MeshData m = b.Batches[secondary ? 1 : 0]; double lead = Head(t) * 1.25;
            double alpha = BaseAlpha(t) * (1 - Smooth((t - .080) / .070)) * (secondary ? .52 : .90);
            double narrowing = 1 - .62 * Smooth((t - .060) / .120);
            m.UvOffset = new Uv(.012 * Math.Sin(t * 10 + (secondary ? 1.7 : 0)), .008 * Math.Sin(t * 13));
            for (int i = 0; i < RibbonRows; i++) b.Centers[i] = Center(lead, t, secondary, i);
            for (int i = 0; i < RibbonRows; i++)
            {
                double u = i / 48.0, pulse = Math.Sin(u * 8 - t * 10);
                double width = b.WidthBases[i] * (.72 + .28 * pulse * pulse) * narrowing * (secondary ? .53 : 1);
                Point center = b.Centers[i], a = b.Centers[Math.Max(0, i - 1)], c = b.Centers[Math.Min(48, i + 1)];
                double dx = c.X - a.X, dy = c.Y - a.Y, r = Math.Sqrt(dx * dx + dy * dy);
                for (int j = 0; j < 3; j++)
                {
                    int n = i * 3 + j; double side = j - 1, x = center.X + (-dy / r) * width * side;
                    // Executable motion.py and frozen goldens are authoritative here; the prose
                    // contract's constant .008 center bulge is stale for this retained water pack.
                    m.Vertices[n] = new Point(mirrorX ? -x : x, center.Y + (dx / r) * width * side,
                        center.Z + .014 * Math.Sin(u * 13 - t * 6) * (1 - side * side));
                    m.UV[n] = new Uv(m.SourceUV[n].X + m.UvOffset.X, m.SourceUV[n].Y + m.UvOffset.Y);
                    m.Alpha[n] = alpha;
                }
            }
        }
        static void Core(Buffer b, double t, bool mirrorX)
        {
            double alpha = CoreAlpha(t);
            for (int i = 0; i < 4; i++)
            {
                double x = i == 0 ? -.5 : i == 2 ? .5 : 0;
                double y = i == 1 ? -.045 : i == 3 ? .045 : 0;
                b.Core.Vertices[i] = new Point((mirrorX ? -x : x) * .075, -.008 + y * .075, .07 / 2.3);
                b.Core.Alpha[i] = alpha;
            }
        }
        public static void Sample(Buffer b, double age, bool mirrorX = false, double availableAnticipation = double.NaN)
        {
            if (b == null) throw new ArgumentNullException("b");
            double t = Clock(age, availableAnticipation); b.VisualAge = t; b.LightEnvelope = Light(t);
            b.TargetAreaW = 40 * b.LightEnvelope; b.FloorAreaW = 18 * b.LightEnvelope;
            Ribbon(b, t, false, mirrorX); Ribbon(b, t, true, mirrorX); Core(b, t, mirrorX);
            for (int i = 0; i < ParticleCount; i++)
            {
                double ageParticle = t - Specs[i, 0]; int offset = i * 7;
                if (!(-NativeLead <= t && t < DiagnosticHorizon && 0 <= ageParticle && ageParticle < ParticleLife))
                {
                    b.ParticleActive[i] = false; b.ParticleCenters[i] = default(Point);
                    for (int j = 0; j < 7; j++) { b.Particles.Vertices[offset + j] = default(Point); b.Particles.Alpha[offset + j] = 0; }
                    continue;
                }
                Point start = b.ParticleStarts[i], v = b.ParticleVelocities[i];
                double k = 4, q = (1 - Math.Exp(-k * ageParticle)) / k;
                Point center = new Point(start.X + v.X * q,
                    start.Y + v.Y * q - 1.6 * (ageParticle / k - q / k), start.Z + v.Z * q + .06 / 2.3);
                b.ParticleCenters[i] = new Point(mirrorX ? -center.X : center.X, center.Y, center.Z);
                double size = Specs[i, 3], sx = size * (1 - .35 * ageParticle / ParticleLife), sy = size * .58;
                double angle = Math.Atan2(v.Y - 1.6 * ageParticle, v.X), c = Math.Cos(angle), s = Math.Sin(angle);
                double alpha = (1 - Smooth(ageParticle / ParticleLife)) * Smooth(ageParticle / .012);
                b.ParticleActive[i] = alpha > 0;
                for (int j = 0; j < 7; j++)
                {
                    Point p = DropLocal[j]; double x = center.X + c * p.X * sx - s * p.Y * sy;
                    b.Particles.Vertices[offset + j] = new Point(mirrorX ? -x : x,
                        center.Y + s * p.X * sx + c * p.Y * sy, center.Z + p.Z * size);
                    b.Particles.Alpha[offset + j] = alpha;
                }
            }
        }
    }
}
