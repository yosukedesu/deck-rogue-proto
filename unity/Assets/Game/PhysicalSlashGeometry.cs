using System;

namespace DeckRogue.Game
{
    // Frozen physical slash authored tiers R1 (2026-10-09). Contact-relative seconds;
    // camera right/up/toward-camera, in captured base-visible target-height units.
    // Source manifest SHA256: d7b9371096bf8003fd99497374d89056284069fd5e84dddc26491f14b3f94c75.
    // Pure C#, no Unity, RNG, I/O, palette remap, old tier scalars, or allocation per Sample.
    internal static class PhysicalSlashGeometry
    {
        public const int TextureKind = 0, CoreKind = 1, RibbonRows = 49;
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
            // Kind 0: original straight source texture RGB, decoded once by the material.
            // Kind 1: direct linear core #F2EEE4. Gain is already tier-authored.
            public readonly int Kind;
            public readonly Point[] Vertices;
            public readonly Uv[] UV;
            public readonly double[] Alpha, Gain;
            public readonly int[] Triangles;
            internal MeshData(int count, int quads, int kind, double gain)
            {
                Kind = kind; Vertices = new Point[count]; UV = new Uv[count];
                Alpha = new double[count]; Gain = new double[count]; Triangles = new int[quads * 6];
                for (int i = 0; i < count; i++) Gain[i] = gain;
            }
        }
        public sealed class Buffer
        {
            public readonly string RequestedTier, Tier;
            public bool Fallback { get { return RequestedTier != Tier; } }
            // Fixed order: primary, secondary (empty for small), cores, particles.
            public readonly MeshData[] Batches;
            public MeshData Primary { get { return Batches[0]; } }
            public MeshData Secondary { get { return Batches[1]; } }
            public MeshData Core { get { return Batches[2]; } }
            public MeshData Particles { get { return Batches[3]; } }
            public readonly bool[] ParticleActive;
            public readonly Point[] ParticleCenters;
            public double VisualAge, LightEnvelope, TargetAreaW, FloorAreaW;
            internal readonly TierData Data;
            internal readonly Point[] Centers = new Point[RibbonRows];
            internal readonly Point[] ParticleStarts, ParticleVelocities;
            internal readonly double[] Widths = new double[RibbonRows];
            internal Buffer(string requested, TierData data)
            {
                RequestedTier = requested; Tier = data.Name; Data = data;
                Batches = new[] {
                    new MeshData(147, 96, TextureKind, Tier == "small" ? 1.20 : Tier == "large" ? 1.45 : 1.35),
                    new MeshData(Tier == "small" ? 0 : 147, Tier == "small" ? 0 : 96, TextureKind, Tier == "large" ? 1.12 : 1.00),
                    new MeshData(data.CoreCount * 4, data.CoreCount, CoreKind, data.CoreGain),
                    new MeshData(data.ParticleCount * 4, data.ParticleCount, CoreKind, data.ParticleGain)
                };
                ParticleActive = new bool[data.ParticleCount]; ParticleCenters = new Point[data.ParticleCount];
                ParticleStarts = new Point[data.ParticleCount]; ParticleVelocities = new Point[data.ParticleCount];
            }
        }
        internal sealed class TierData
        {
            internal readonly string Name;
            internal readonly int CoreCount, ParticleCount;
            internal readonly double P, E, Hold, Fade, Length, Width, Life, End;
            internal readonly double CoreLength, CoreGain, ParticleGain, ParticleScale, TargetW, FloorW;
            internal readonly double[,] Knots, Specs;
            internal TierData(string name, int cores, double p, double e, double hold, double fade,
                double length, double width, double life, double end, double coreLength, double coreGain,
                double particleGain, double particleScale, double targetW, double floorW,
                double[,] knots, double[,] specs)
            {
                Name = name; CoreCount = cores; P = p; E = e; Hold = hold; Fade = fade;
                Length = length; Width = width; Life = life; End = end; CoreLength = coreLength;
                CoreGain = coreGain; ParticleGain = particleGain; ParticleScale = particleScale;
                TargetW = targetW; FloorW = floorW; Knots = knots; Specs = specs;
                ParticleCount = specs.GetLength(0);
            }
        }

        static readonly TierData Small = new TierData("small", 1,
            0.045, 0.16, 0.02, 0.02, 0.72, 0.027, 0.128, 0.14, 0.08, 3.3, 1.9, 1.1, 28.0, 8.0,
            new double[,] {
                { -0.045, -0.42, 1.0 },
                { -0.022, -0.28, 10.0 },
                { 0.0, 0.0, 21.0 },
                { 0.022, 0.38, 9.0 },
                { 0.075, 0.6, 0.0 },
                { 0.16, 0.64, 0.0 }
            },
            new double[,] {
                { 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
                { 0.006, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
                { 0.012, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 }
            });

        static readonly TierData Medium = new TierData("medium", 3,
            0.066666667, 0.333333333, 0.066666667, 0.035, 1.5, 0.045, 0.23, 0.252, 0.12, 3.8, 2.2, 1.45, 48.0, 20.0,
            new double[,] {
                { -0.066666667, -0.8, 1.0 },
                { -0.035, -0.65, 10.0 },
                { 0.0, 0.0, 22.0 },
                { 0.035, 0.67, 10.0 },
                { 0.11, 1.05, 2.0 },
                { 0.333333333, 1.25, 0.0 }
            },
            new double[,] {
                { 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
                { 0.0, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
                { 0.0, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 },
                { 0.022, 0.5538025156552036, 0.781381770579271, 0.0552109878293629 },
                { 0.022, -0.6328030518934236, 0.9867369945327584, 0.030297687296996265 },
                { 0.022, 0.061664644824827586, 0.8502843849475583, 0.043524457601980884 }
            });

        static readonly TierData Large = new TierData("large", 3,
            0.13, 0.4, 0.085, 0.035, 1.7, 0.073, 0.27, 0.32, 0.145, 4.0, 2.4, 1.7, 60.0, 24.0,
            new double[,] {
                { -0.13, -1.0, 0.5 },
                { -0.07, -0.8, 4.0 },
                { -0.025, -0.45, 14.0 },
                { 0.0, 0.0, 24.0 },
                { 0.035, 0.65, 10.0 },
                { 0.125, 0.9, 0.6 },
                { 0.4, 0.96, 0.0 }
            },
            new double[,] {
                { 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
                { 0.004, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
                { 0.008, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 },
                { 0.018, 0.5538025156552036, 0.781381770579271, 0.0552109878293629 },
                { 0.022, -0.6328030518934236, 0.9867369945327584, 0.030297687296996265 },
                { 0.026, 0.061664644824827586, 0.8502843849475583, 0.043524457601980884 },
                { 0.04, 0.1181540250242763, 0.9482632634123469, 0.05014185475559578 },
                { 0.045, 0.25177522002504815, 1.0755780824187333, 0.057097001477406185 },
                { 0.05, -0.31071945638799014, 1.125430356417509, 0.050080117253616996 }
            });

        static readonly double[,] UVProfile = new double[,] {
                { 0.0, 0.5, 0.05 },
                { 0.020833333333333332, 0.37430939226519333, 0.013 },
                { 0.041666666666666664, 0.3604972375690608, 0.013 },
                { 0.0625, 0.34806629834254144, 0.013 },
                { 0.08333333333333333, 0.3370165745856354, 0.013 },
                { 0.10416666666666666, 0.3287292817679558, 0.013 },
                { 0.125, 0.32182320441988954, 0.013 },
                { 0.14583333333333331, 0.3162983425414365, 0.015469613259668507 },
                { 0.16666666666666666, 0.31353591160220995, 0.01740331491712707 },
                { 0.1875, 0.3107734806629834, 0.020303867403314916 },
                { 0.20833333333333331, 0.3107734806629834, 0.04157458563535912 },
                { 0.22916666666666666, 0.3121546961325967, 0.04640883977900552 },
                { 0.25, 0.3149171270718232, 0.050276243093922646 },
                { 0.2708333333333333, 0.3204419889502762, 0.05414364640883977 },
                { 0.29166666666666663, 0.324585635359116, 0.08218232044198895 },
                { 0.3125, 0.3287292817679558, 0.0841160220994475 },
                { 0.3333333333333333, 0.3328729281767956, 0.08701657458563536 },
                { 0.35416666666666663, 0.33563535911602205, 0.08701657458563536 },
                { 0.375, 0.33977900552486184, 0.08798342541436464 },
                { 0.3958333333333333, 0.34392265193370164, 0.0947513812154696 },
                { 0.41666666666666663, 0.35635359116022103, 0.12859116022099448 },
                { 0.4375, 0.3715469613259669, 0.10732044198895027 },
                { 0.4583333333333333, 0.3825966850828729, 0.11505524861878452 },
                { 0.47916666666666663, 0.39364640883977897, 0.12182320441988949 },
                { 0.5, 0.40607734806629836, 0.12569060773480664 },
                { 0.5208333333333333, 0.4116022099447514, 0.12762430939226518 },
                { 0.5416666666666666, 0.419889502762431, 0.13439226519337016 },
                { 0.5625, 0.425414364640884, 0.13632596685082873 },
                { 0.5833333333333333, 0.4350828729281768, 0.14212707182320442 },
                { 0.6041666666666666, 0.4475138121546961, 0.10055248618784529 },
                { 0.625, 0.46685082872928174, 0.11505524861878452 },
                { 0.6458333333333333, 0.4875690607734806, 0.1102209944751381 },
                { 0.6666666666666666, 0.5013812154696133, 0.10635359116022099 },
                { 0.6875, 0.5151933701657458, 0.15566298342541435 },
                { 0.7083333333333333, 0.5193370165745856, 0.15759668508287292 },
                { 0.7291666666666666, 0.5276243093922652, 0.10828729281767954 },
                { 0.75, 0.5441988950276243, 0.10828729281767954 },
                { 0.7708333333333333, 0.569060773480663, 0.11312154696132597 },
                { 0.7916666666666666, 0.5883977900552486, 0.11215469613259668 },
                { 0.8125, 0.6049723756906078, 0.10732044198895027 },
                { 0.8333333333333333, 0.6174033149171271, 0.08218232044198895 },
                { 0.8541666666666666, 0.638121546961326, 0.02803867403314917 },
                { 0.875, 0.6574585635359116, 0.025138121546961323 },
                { 0.8958333333333333, 0.6781767955801106, 0.025138121546961323 },
                { 0.9166666666666666, 0.7002762430939227, 0.022237569060773482 },
                { 0.9375, 0.7265193370165746, 0.019337016574585635 },
                { 0.9583333333333333, 0.7596685082872928, 0.013535911602209943 },
                { 0.9791666666666666, 0.8024861878453039, 0.013 },
                { 1.0, 0.5, 0.05 }
            };

        static TierData Data(string tier) { return tier == "small" ? Small : tier == "large" ? Large : Medium; }
        public static string ResolveTier(string tier) { return Data(tier).Name; }
        // Lead is native anticipation duration; End excludes the invisible diagnostic tail.
        public static double Lead(string tier) { return Data(tier).P; }
        public static double End(string tier) { return Data(tier).End; }
        public static double Horizon(string tier) { return Data(tier).E; }
        static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        static double SafeAge(TierData p, double age)
        {
            // Invalid clock input fails closed, without NaN/Infinity in GPU-bound buffers.
            return double.IsNaN(age) || double.IsNegativeInfinity(age) ? -p.P - 1e-6 :
                double.IsPositiveInfinity(age) ? p.E : age;
        }
        public static double VisualClock(string tier, double age, double availableAnticipation = double.NaN)
        {
            return Clock(Data(tier), age, availableAnticipation);
        }
        static double Clock(TierData p, double age, double available)
        {
            age = SafeAge(p, age);
            if (age >= 0 || double.IsNaN(available) || available >= p.P) return age;
            if (available <= 0 || age < -available) return -p.P - 1e-6;
            double q = Clamp((age + available) / available);
            return -p.P + p.P * (q + (available / p.P - 1) * (q * q * q - q * q));
        }
        static double Head(TierData p, double t)
        {
            double[,] k = p.Knots; int last = k.GetLength(0) - 1;
            if (t <= k[0, 0]) return k[0, 1];
            if (t >= k[last, 0]) return k[last, 1];
            int i = 0; while (i < last - 1 && t > k[i + 1, 0]) i++;
            double dt = k[i + 1, 0] - k[i, 0], q = (t - k[i, 0]) / dt;
            double q2 = q * q, q3 = q2 * q;
            return (2 * q3 - 3 * q2 + 1) * k[i, 1] + (q3 - 2 * q2 + q) * dt * k[i, 2]
                + (-2 * q3 + 3 * q2) * k[i + 1, 1] + (q3 - q2) * dt * k[i + 1, 2];
        }
        static Point Center(TierData p, double lead, double t, bool secondary, int i)
        {
            double u = i / 48.0, length = secondary ? p.Name == "large" ? .95 : p.Length * .55 : p.Length;
            double angle = secondary && p.Name == "large" ? -.50 : .60;
            double s = lead - length * (1 - u), sx = 2 * Math.Sin(s / 2), sy = 2 * (1 - Math.Cos(s / 2));
            double x = Math.Cos(angle) * sx - Math.Sin(angle) * sy;
            double y = Math.Sin(angle) * sx + Math.Cos(angle) * sy;
            if (secondary && p.Name == "large") { x += .10; y -= .04; }
            else if (secondary) y -= .11 * Math.Sin(Math.PI * u);
            return new Point(x, y, .016 * Math.Sin(u * 5 + t * 6) + (secondary ? .013 : 0));
        }
        static Point Emitter(TierData p, double t) { return Center(p, Head(p, t), t, false, 46); }
        static double BaseAlpha(TierData p, double t)
        {
            return -p.P <= t && t < p.E ? Smooth((t + p.P) / Math.Min(p.P * .25, .025)) * (1 - Smooth((t - .08) / (p.E - .08))) : 0;
        }
        static double CoreAlpha(TierData p, double t) { return 0 <= t && t < p.E ? 1 - Smooth((t - p.Hold) / p.Fade) : 0; }
        public static double CoreAlpha(string tier, double t) { TierData p = Data(tier); return CoreAlpha(p, SafeAge(p, t)); }
        // Light takes visual age; Sample applies VisualClock before calculating all envelopes.
        public static double Light(string tier, double visualAge) { TierData p = Data(tier); return Light(p, SafeAge(p, visualAge)); }
        static double Light(TierData p, double t)
        {
            double light = t >= 0 ? CoreAlpha(p, t) : -p.P <= t && t < p.E ? .025 * Math.Pow(Smooth((t + p.P) / p.P), 3) : 0;
            return light * (1 - Smooth((t - .10) / .15));
        }
        static void Quad(int[] indices, int q, int a, int b, int c, int d)
        {
            int o = q * 6; indices[o] = a; indices[o + 1] = b; indices[o + 2] = c;
            indices[o + 3] = a; indices[o + 4] = c; indices[o + 5] = d;
        }
        public static Buffer Create(string tier)
        {
            TierData p = Data(tier); Buffer b = new Buffer(tier, p);
            for (int k = 0; k < 2; k++)
            {
                MeshData m = b.Batches[k]; if (m.Vertices.Length == 0) continue;
                for (int i = 0; i < RibbonRows; i++) for (int j = 0; j < 3; j++)
                    m.UV[i * 3 + j] = new Uv(UVProfile[i, 0], UVProfile[i, 1] + UVProfile[i, 2] * (j - 1));
                for (int i = 0; i < 48; i++) for (int j = 0; j < 2; j++)
                    Quad(m.Triangles, i * 2 + j, i * 3 + j, i * 3 + j + 1, (i + 1) * 3 + j + 1, (i + 1) * 3 + j);
            }
            for (int k = 2; k < 4; k++) for (int i = 0; i < b.Batches[k].Vertices.Length / 4; i++)
                Quad(b.Batches[k].Triangles, i, i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3);
            for (int i = 0; i < RibbonRows; i++) b.Widths[i] = p.Width * Math.Pow(Math.Max(0, Math.Sin(Math.PI * (i / 48.0))), .85);
            for (int i = 0; i < p.ParticleCount; i++)
            {
                double born = p.Specs[i, 0], eps = 1e-5;
                Point start = Emitter(p, born), ep = Emitter(p, born + eps), em = Emitter(p, born - eps);
                double x = (ep.X - em.X) / 2 / eps * .30 + Math.Cos(p.Specs[i, 1]) * p.Specs[i, 2] * .45;
                double y = (ep.Y - em.Y) / 2 / eps * .30 + Math.Sin(p.Specs[i, 1]) * p.Specs[i, 2] + .45;
                double z = (ep.Z - em.Z) / 2 / eps * .30 + .04 * (i % 3 - 1);
                double radius = Math.Sqrt(x * x + y * y), sign = i % 2 == 1 ? 2.1 : -2.1;
                b.ParticleStarts[i] = start; b.ParticleVelocities[i] = new Point(x + (-y / radius) * sign, y + (x / radius) * sign, z);
            }
            return b;
        }
        static void Ribbon(Buffer b, double t, bool secondary, bool mirrorX)
        {
            TierData p = b.Data; MeshData m = b.Batches[secondary ? 1 : 0]; if (m.Vertices.Length == 0) return;
            double lead = Head(p, t - (secondary ? .028 : 0)), alpha = BaseAlpha(p, t);
            if (p.Name == "small") alpha *= .82 * (1 - Smooth((t - .025) / .050));
            else if (p.Name == "large") alpha *= secondary ? .65 * Smooth((t - .020) / .016) * (1 - Smooth((t - .100) / .130)) : .95 * (1 - Smooth((t - .060) / .100));
            else alpha *= secondary ? .55 * Smooth((t - .018) / .016) * (1 - Smooth((t - .095) / .080)) : .87 * (1 - Smooth((t - .060) / .075));
            double widthScale = secondary ? p.Name == "large" ? .52 : .24 : 1;
            for (int i = 0; i < RibbonRows; i++) b.Centers[i] = Center(p, lead, t, secondary, i);
            for (int i = 0; i < RibbonRows; i++)
            {
                Point center = b.Centers[i], a = b.Centers[Math.Max(0, i - 1)], c = b.Centers[Math.Min(48, i + 1)];
                double dx = c.X - a.X, dy = c.Y - a.Y, r = Math.Sqrt(dx * dx + dy * dy), width = b.Widths[i] * widthScale;
                for (int j = 0; j < 3; j++)
                {
                    int n = i * 3 + j; double side = j - 1, x = center.X + (-dy / r) * width * side;
                    m.Vertices[n] = new Point(mirrorX ? -x : x, center.Y + (dx / r) * width * side, center.Z + .008 * (1 - side * side));
                    m.Alpha[n] = alpha;
                }
            }
        }
        static void Diamond(MeshData m, int offset, Point center, double size, double angle, double halfWidth, double shrink, double alpha, bool mirrorX)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle);
            for (int i = 0; i < 4; i++)
            {
                double x = i == 0 ? -.5 * shrink : i == 2 ? .5 * shrink : 0;
                double y = i == 1 ? -halfWidth : i == 3 ? halfWidth : 0;
                double vx = center.X + size * (c * x - s * y);
                m.Vertices[offset + i] = new Point(mirrorX ? -vx : vx, center.Y + size * (s * x + c * y), center.Z);
                m.Alpha[offset + i] = alpha;
            }
        }
        public static void Sample(Buffer b, double age, bool mirrorX = false, double availableAnticipation = double.NaN)
        {
            if (b == null) throw new ArgumentNullException("b");
            TierData p = b.Data; double t = Clock(p, age, availableAnticipation);
            b.VisualAge = t; b.LightEnvelope = Light(p, t);
            // Reference Blender watts only. Never use these directly as Unity point-light intensity.
            b.TargetAreaW = p.TargetW * b.LightEnvelope; b.FloorAreaW = p.FloorW * b.LightEnvelope;
            Ribbon(b, t, false, mirrorX); Ribbon(b, t, true, mirrorX);
            double coreAlpha = CoreAlpha(p, t);
            for (int i = 0; i < p.CoreCount; i++)
                Diamond(b.Core, i * 4, new Point(.012 * i, .008 * (i - 1), .07 / 2.3), p.CoreLength * (1 - .24 * i), i == 0 ? .44 : i == 1 ? .51 : .36, .045, 1, coreAlpha, mirrorX);
            for (int i = 0; i < p.ParticleCount; i++)
            {
                double ageParticle = t - p.Specs[i, 0]; int offset = i * 4;
                bool active = -p.P <= t && t < p.E && 0 <= ageParticle && ageParticle < p.Life;
                b.ParticleActive[i] = active;
                if (!active)
                {
                    b.ParticleCenters[i] = default(Point);
                    for (int j = 0; j < 4; j++) { b.Particles.Vertices[offset + j] = default(Point); b.Particles.Alpha[offset + j] = 0; }
                    continue;
                }
                Point start = b.ParticleStarts[i], v = b.ParticleVelocities[i]; double k = 2.4, q = (1 - Math.Exp(-k * ageParticle)) / k;
                Point center = new Point(start.X + v.X * q, start.Y + v.Y * q - 1.6 * (ageParticle / k - q / k), start.Z + v.Z * q + .06 / 2.3);
                b.ParticleCenters[i] = new Point(mirrorX ? -center.X : center.X, center.Y, center.Z);
                Diamond(b.Particles, offset, center, p.Specs[i, 3] * p.ParticleScale, Math.Atan2(v.Y - 1.6 * ageParticle, v.X), .12, 1 - .35 * ageParticle / p.Life, 1 - Smooth(ageParticle / p.Life), mirrorX);
            }
        }
    }
}
