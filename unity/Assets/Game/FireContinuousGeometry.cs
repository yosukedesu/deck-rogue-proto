using System;

namespace DeckRogue.Game
{
    // Frozen fire projectile/localburst R1, 2026-10-09. Unity/device visuals unverified.
    // Manifest SHA256: 4342a7b800ddf1b8edc7b7f36b2d0a1486bc737481b01117c6dd55d80bd47d39.
    // Contact-relative seconds, captured camera right/up/toward-camera, normalized visible H.
    // No Unity, RNG, gameplay events, I/O, or allocation in warm ConfigureSource/Sample/Surface.
    internal static class FireContinuousGeometry
    {
        public const int ProjectileKind = 0, TongueKind = 1, CoreKind = 2, SolidKind = 3;
        public const int ShadowRole = 0, PrimaryRole = 1, EdgeRole = 2, CoreRole = 3;
        public const int RibbonRows = 33, EmberCount = 16;
        public const double NativeLead = .130, ProjectileEnd = .276, LocalBurstEnd = .284;
        public const double FlightEnd = .023, CoreEnd = .057, LightEnd = .115;
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
        public struct Rgba
        {
            public double R, G, B, A;
            public Rgba(double r, double g, double b, double a) { R = r; G = g; B = b; A = a; }
        }
        public sealed class MeshData
        {
            public readonly string Name;
            public readonly int Kind;
            public readonly Point[] Vertices;
            // GPU-ready current UV; no extra renderer UV flow should be applied.
            public readonly Uv[] UV;
            public readonly double[] Alpha, Gain, Erosion;
            public readonly int[] Role, Triangles;
            internal MeshData(string name, int count, int triangleCount, int kind, double gain, int role)
            {
                Name = name; Kind = kind;
                Vertices = new Point[count]; UV = new Uv[count];
                Alpha = new double[count]; Gain = new double[count]; Erosion = new double[count];
                Role = new int[count]; Triangles = new int[triangleCount * 3];
                for (int i = 0; i < count; i++) { Gain[i] = gain; Role[i] = role; }
            }
        }
        public sealed class Buffer
        {
            public readonly string Mode;
            public readonly bool IsProjectile;
            public readonly int TongueCount, TongueOffset;
            // Fixed topology. Projectile: flight, 3 tongues, core, 16 merged embers (6 batches).
            // Localburst: 5 tongues, core, 16 merged embers (7 batches), no flight mesh.
            public readonly MeshData[] Batches;
            public MeshData Flight { get { return IsProjectile ? Batches[0] : null; } }
            public MeshData Core { get { return Batches[TongueOffset + TongueCount]; } }
            public MeshData Embers { get { return Batches[Batches.Length - 1]; } }
            public readonly bool[] EmberActive;
            public readonly Point[] EmberCenters;
            public Point Source { get; internal set; }
            public bool SourceValid { get; internal set; }
            public bool FlightEnabled { get { return IsProjectile && SourceValid; } }
            public string SourceFallbackReason { get { return SourceValid ? null : "invalid_or_coincident_capture"; } }
            public double VisualAge, LightEnvelope, TargetAreaW, FloorAreaW;
            internal double DirectionX, DirectionY, RotationCos, RotationSin;
            internal Buffer(string mode, bool projectile)
            {
                Mode = mode; IsProjectile = projectile; TongueCount = projectile ? 3 : 5;
                TongueOffset = projectile ? 1 : 0;
                Batches = new MeshData[TongueOffset + TongueCount + 2];
                if (projectile) Batches[0] = new MeshData("flight", 165, 256, ProjectileKind, 1.75, EdgeRole);
                for (int k = 0; k < TongueCount; k++)
                    Batches[TongueOffset + k] = new MeshData("flame_" + k, 99, 128, TongueKind, 1.95, EdgeRole);
                Batches[TongueOffset + TongueCount] = new MeshData("hot_contact", 9, 8, CoreKind, 3.8, CoreRole);
                Batches[Batches.Length - 1] = new MeshData("embers", EmberCount * 4, EmberCount * 2, SolidKind, 1.85, EdgeRole);
                EmberActive = new bool[EmberCount]; EmberCenters = new Point[EmberCount];
            }
        }
        // angle, birth, life, length, width; author-authored unequal tongues.
        static readonly double[,] ProjectileFlames = new double[,] {
            { -.12, 0, .170, .44, .080 }, { .55, .018, .205, .76, .090 }, { 1.30, .055, .160, .56, .055 }
        };
        static readonly double[,] LocalFlames = new double[,] {
            { -.18, 0, .180, .29, .083 }, { .48, .014, .210, .55, .066 },
            { 1.14, .040, .200, .66, .096 }, { 1.86, .062, .180, .41, .047 }, { 2.60, .024, .180, .25, .052 }
        };
        static readonly double[] ProjectileBirths = { -.085, -.065, -.045, -.025, 0, .004, .010, .018, .024, .034, .045, .055, .064, .074, .085, .095 };
        static readonly double[] LocalBirths = { 0, .003, .008, .013, .019, .027, .034, .042, .050, .059, .067, .075, .082, .089, .096, .103 };
        static readonly Point[] Palette = {
            LinearColor(0x61, 0x22, 0x21), LinearColor(0xD6, 0x5A, 0x28),
            LinearColor(0xF2, 0xA0, 0x52), LinearColor(0xFF, 0xE8, 0xAE)
        };
        public static readonly Point LightColorLinear = LinearColor(0xDF, 0xAA, 0x78);
        static bool ParseMode(string mode)
        {
            if (mode == "projectile") return true;
            if (mode == "localburst") return false;
            throw new ArgumentException("Unknown fire mode; expected projectile or localburst.", "mode");
        }
        public static double Lead(string mode) { return ParseMode(mode) ? NativeLead : 0; }
        public static double Lead(Buffer b) { return b.IsProjectile ? NativeLead : 0; }
        public static double End(string mode) { return ParseMode(mode) ? ProjectileEnd : LocalBurstEnd; }
        public static double End(Buffer b) { return b.IsProjectile ? ProjectileEnd : LocalBurstEnd; }
        static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        static double Fade(double t, double hold, double end)
        { return 0 <= t && t < end ? 1 - Smooth((t - hold) / (end - hold)) : 0; }
        static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        static double Hypot(double x, double y)
        {
            x = Math.Abs(x); y = Math.Abs(y); double m = Math.Max(x, y);
            return m == 0 ? 0 : m * Math.Sqrt((x / m) * (x / m) + (y / m) * (y / m));
        }
        static double SafeAge(double age)
        {
            if (double.IsNaN(age) || age < -1000000) return -NativeLead - 1e-6;
            if (age > 1000000) return LocalBurstEnd;
            return age;
        }
        // Negative visual time alone is remapped. Actual receipt.Land/contact=0 never moves.
        // At contact the left derivative is 1, matching the unmodified positive clock.
        static double RemapClock(bool projectile, double age, double available)
        {
            age = SafeAge(age);
            if (!projectile || age >= 0 || double.IsNaN(available) || available >= NativeLead) return age;
            if (available <= 0 || age < -available) return -NativeLead - 1e-6;
            double q = Clamp((age + available) / available);
            return -NativeLead + NativeLead * (q + (available / NativeLead - 1) * (q * q * q - q * q));
        }
        public static double Clock(string mode, double age, double available = double.NaN)
        { return RemapClock(ParseMode(mode), age, available); }
        public static double Clock(Buffer b, double age, double available = double.NaN)
        { return RemapClock(b.IsProjectile, age, available); }
        // Light takes visual age, not wall time. Invalid capture suppresses projectile prelight.
        public static double Light(Buffer b, double visualAge)
        {
            double t = SafeAge(visualAge);
            if (t >= 0) return Fade(t, .025, LightEnd);
            return b.FlightEnabled && t >= -NativeLead ? .035 * Math.Pow(Smooth((t + NativeLead) / NativeLead), 3) : 0;
        }
        public static void ConfigureSource(Buffer b, double x, double y, double z, bool valid = true)
        {
            valid = valid && Finite(x) && Finite(y) && Finite(z) && Hypot(x, y) > .05;
            b.SourceValid = valid;
            // Fallback only orients the legitimate target-local impact; it cannot enable flight.
            b.Source = valid ? new Point(x, y, z) : new Point(-1.12, .04, 0);
            double length = Hypot(b.Source.X, b.Source.Y);
            b.DirectionX = -b.Source.X / length; b.DirectionY = -b.Source.Y / length;
            double delta = Math.Atan2(-b.Source.Y, -b.Source.X) - Math.Atan2(-.04, 1.12);
            b.RotationCos = b.IsProjectile ? Math.Cos(delta) : 1;
            b.RotationSin = b.IsProjectile ? Math.Sin(delta) : 0;
        }
        static void Quad(int[] triangles, int q, int a, int b, int c, int d)
        {
            int n = q * 6; triangles[n] = a; triangles[n + 1] = b; triangles[n + 2] = c;
            triangles[n + 3] = a; triangles[n + 4] = c; triangles[n + 5] = d;
        }
        public static Buffer Create(string mode)
        {
            Buffer b = new Buffer(mode, ParseMode(mode));
            ConfigureSource(b, 0, 0, 0, false); // capture must be supplied; never invent a flight source.
            for (int batch = 0; batch < b.TongueOffset + b.TongueCount; batch++)
            {
                MeshData m = b.Batches[batch]; int columns = m.Kind == ProjectileKind ? 5 : 3;
                for (int i = 0; i < 32; i++) for (int j = 0; j < columns - 1; j++)
                    Quad(m.Triangles, i * (columns - 1) + j, i * columns + j, i * columns + j + 1,
                        (i + 1) * columns + j + 1, (i + 1) * columns + j);
            }
            b.Core.UV[0] = new Uv(.5, .5);
            for (int k = 0; k < 8; k++)
            {
                int n = k * 3; b.Core.Triangles[n] = 0; b.Core.Triangles[n + 1] = 1 + k; b.Core.Triangles[n + 2] = 1 + (k + 1) % 8;
                double angle = 2 * Math.PI * k / 8;
                b.Core.UV[k + 1] = new Uv(.5 + .5 * Math.Cos(angle), .5 + .5 * Math.Sin(angle));
            }
            for (int i = 0; i < EmberCount; i++)
            {
                int offset = i * 4; Quad(b.Embers.Triangles, i, offset, offset + 1, offset + 2, offset + 3);
                for (int j = 0; j < 4; j++) b.Embers.Role[offset + j] = i % 4 == 0 ? CoreRole : EdgeRole;
            }
            return b;
        }
        public static Point Tip(Buffer b, double visualAge)
        {
            double t = SafeAge(visualAge);
            if (t <= 0)
            {
                double q = Clamp((t + NativeLead) / NativeLead), v = .24 * q + .76 * q * q;
                return new Point(b.Source.X * (1 - v), b.Source.Y * (1 - v), b.Source.Z * (1 - v));
            }
            double factor = 1.76 / NativeLead * (1 - Math.Exp(-35 * t)) / 35;
            return new Point(-b.Source.X * factor, -b.Source.Y * factor, -b.Source.Z * factor);
        }
        static Point Rotate(Buffer b, double x, double y, double z)
        { return new Point(b.RotationCos * x - b.RotationSin * y, b.RotationSin * x + b.RotationCos * y, z); }
        static void Projectile(Buffer b, double t)
        {
            MeshData m = b.Flight; Point center = Tip(b, t);
            double dx = b.DirectionX, dy = b.DirectionY, nx = -dy, ny = dx;
            double alpha = -NativeLead <= t && t < FlightEnd && b.FlightEnabled
                ? Smooth((t + NativeLead) / .018) * (1 - Smooth(Math.Max(0, t) / FlightEnd)) * .92 : 0;
            for (int i = 0; i < RibbonRows; i++)
            {
                double u = i / 32.0, x = -(1 - u) * .66;
                double y = .025 * Math.Sin(u * 7 - t * 42) * Math.Pow(1 - u, 1.4);
                double w = .115 * Math.Pow(Math.Max(0, Math.Sin(Math.PI * u)), .34) * (.6 + .4 * u);
                for (int k = 0; k < 5; k++)
                {
                    int n = i * 5 + k; double side = (k - 2) / 2.0;
                    m.Vertices[n] = new Point(center.X + dx * x + nx * (y + side * w),
                        center.Y + dy * x + ny * (y + side * w), center.Z + .022 + .006 * (1 - side * side));
                    m.UV[n] = new Uv((380 + 1030 * u) / 1774, (1 - (475 + 210 * side) / 887)
                        + .005 * Math.Sin(10 * u - 25 * t) * (1 - u));
                    m.Alpha[n] = alpha;
                }
            }
        }
        static void Flame(Buffer b, double t, int k)
        {
            double[,] specs = b.IsProjectile ? ProjectileFlames : LocalFlames;
            double born = specs[k, 1], age = Math.Max(0, t - born), life = specs[k, 2];
            double alpha = Smooth((t - born) / .012) * Fade(t - born, .042, life) * .95;
            double growth = .18 + .82 * (1 - Math.Exp(-age * 35));
            double length = specs[k, 3] * growth, width = specs[k, 4] * (.75 + .25 * growth);
            double angle = specs[k, 0] + .12 * Math.Sin(k * 1.4 + age * 7) * Smooth(age / .05);
            double ca = Math.Cos(angle), sa = Math.Sin(angle), phase = k * 1.731;
            double erosion = 1.15 * Smooth((age - .045) / (life - .045));
            MeshData m = b.Batches[b.TongueOffset + k];
            for (int i = 0; i < RibbonRows; i++)
            {
                double u = i / 32.0, bend = .075 * Math.Sin(3.2 * u - age * (13 + k) + phase) * u * u;
                double advance = .025 + age * (.10 + .025 * k), x = length * u + advance;
                double w = width * Math.Pow(Math.Max(0, Math.Sin(Math.PI * u)), .65) * (1 - .25 * u)
                    * (1 + .15 * Smooth((u - .45) / .55) * Math.Sin(17 * u - age * (27 + k) + phase));
                for (int j = 0; j < 3; j++)
                {
                    int n = i * 3 + j; double side = j - 1, y = bend + side * w;
                    m.Vertices[n] = Rotate(b, ca * x - sa * y, sa * x + ca * y, .025 + k * .004 + .008 * (1 - side * side));
                    double px = 1385 - 970 * u - 42 * age * Math.Sin(Math.PI * u);
                    double py = 475 - 45 * u + side * 175 + .012 * 887 * Math.Sin(7 * u - age * 24 + phase) * Math.Sin(Math.PI * u);
                    m.UV[n] = new Uv(px / 1774, 1 - py / 887);
                    m.Alpha[n] = alpha; m.Erosion[n] = erosion;
                }
            }
        }
        static void HotCore(Buffer b, double t)
        {
            MeshData m = b.Core; double alpha = Fade(t, .018, CoreEnd), growth = 1 + .35 * Smooth(Math.Max(0, t) / .04);
            m.Vertices[0] = new Point(0, 0, .061); m.Alpha[0] = alpha;
            for (int k = 0; k < 8; k++)
            {
                double angle = 2 * Math.PI * k / 8;
                m.Vertices[k + 1] = Rotate(b, Math.Cos(angle) * .056 * growth, Math.Sin(angle) * .034 * growth, .057);
                m.Alpha[k + 1] = alpha;
            }
        }
        static void Ember(Buffer b, double t, int i)
        {
            double born = (b.IsProjectile ? ProjectileBirths : LocalBirths)[i], age = Math.Max(0, t - born);
            double life = .160 + .007 * (i % 4), phi = i * 2.399963, vx, vy;
            Point p0;
            if (born < 0)
            {
                Point tip = Tip(b, born); p0 = new Point(tip.X - b.DirectionX * .28, tip.Y - b.DirectionY * .28, tip.Z + .025);
                vx = -b.DirectionX * (.8 + .12 * i); vy = .35 + Math.Sin(phi) * .5;
            }
            else
            {
                p0 = new Point(.025 * Math.Cos(phi), .022 * Math.Sin(phi), .05);
                vx = Math.Cos(phi) * (1.15 + .14 * (i % 4)) + (b.IsProjectile ? .70 : 0);
                vy = Math.Sin(phi) * (1.0 + .13 * (i % 3)) + .62;
                if (b.IsProjectile)
                {
                    double oldX = vx; vx = b.RotationCos * oldX - b.RotationSin * (vy - .62);
                    vy = b.RotationSin * oldX + b.RotationCos * (vy - .62) + .62;
                }
            }
            double q = (1 - Math.Exp(-4 * age)) / 4, x = p0.X + vx * q, y = p0.Y + vy * q + .20 * age * age;
            double theta = Math.Atan2(vy + .4 * age, vx), ca = Math.Cos(theta), sa = Math.Sin(theta);
            double length = (.027 + .008 * (i % 3)) * (1 - .65 * Clamp(age / life)), width = .0055 + .001 * (i % 2);
            double alpha = Smooth((t - born) / .008) * Fade(t - born, .035, life);
            if (born < 0 && !b.FlightEnabled) alpha = 0;
            b.EmberCenters[i] = new Point(x, y, p0.Z); b.EmberActive[i] = alpha > 0;
            for (int j = 0; j < 4; j++)
            {
                double a = j == 0 ? -length / 2 : j == 2 ? length / 2 : 0;
                double side = j == 1 ? -width / 2 : j == 3 ? width / 2 : 0;
                int n = i * 4 + j;
                b.Embers.Vertices[n] = new Point(x + ca * a - sa * side, y + sa * a + ca * side, p0.Z);
                b.Embers.Alpha[n] = alpha;
            }
        }
        public static void Sample(Buffer b, double age, double available = double.NaN)
        {
            double t = Clock(b, age, available); b.VisualAge = t;
            if (b.IsProjectile) Projectile(b, t);
            for (int k = 0; k < b.TongueCount; k++) Flame(b, t, k);
            HotCore(b, t);
            for (int i = 0; i < EmberCount; i++) Ember(b, t, i);
            b.LightEnvelope = Light(b, t);
            // Offline reference watts only, never Unity intensity values.
            b.TargetAreaW = (b.IsProjectile ? 55 : 62) * b.LightEnvelope;
            b.FloorAreaW = (b.IsProjectile ? 20 : 22) * b.LightEnvelope;
        }
        public static double SrgbToLinear(double x)
        { return x <= .04045 ? x / 12.92 : Math.Pow((x + .055) / 1.055, 2.4); }
        static Point LinearColor(int r, int g, int b)
        { return new Point(SrgbToLinear(r / 255.0), SrgbToLinear(g / 255.0), SrgbToLinear(b / 255.0)); }
        public static Point RoleColorLinear(int role)
        {
            if (role < ShadowRole || role > CoreRole) throw new ArgumentOutOfRangeException("role");
            return Palette[role];
        }
        static Point Lerp(Point a, Point b, double q)
        { return new Point(a.X + (b.X - a.X) * q, a.Y + (b.Y - a.Y) * q, a.Z + (b.Z - a.Z) * q); }
        // Executable linear-HDR oracle for runtime/surface_reference.py. Texture RGB must already
        // be decoded exactly once; alpha stays straight. Result premultiplies exactly once.
        public static Rgba Surface(int kind, Uv uv, Rgba textureLinear, double alpha, double gain,
            int role = EdgeRole, double erosion = 0)
        {
            if (kind < ProjectileKind || kind > SolidKind) throw new ArgumentOutOfRangeException("kind");
            double a = Clamp(alpha); Point rgb = RoleColorLinear(role);
            if (kind == ProjectileKind || kind == TongueKind)
            {
                double lum = textureLinear.R * .2126 + textureLinear.G * .7152 + textureLinear.B * .0722;
                if (lum <= 0) rgb = Palette[ShadowRole];
                else if (lum >= 1) rgb = Palette[CoreRole];
                else if (lum <= .20) rgb = Lerp(Palette[ShadowRole], Palette[PrimaryRole], lum / .20);
                else if (lum <= .65) rgb = Lerp(Palette[PrimaryRole], Palette[EdgeRole], (lum - .20) / .45);
                else rgb = Lerp(Palette[EdgeRole], Palette[CoreRole], (lum - .65) / .35);
                a *= Clamp(textureLinear.A);
                if (!(0 <= uv.X && uv.X <= 1 && 0 <= uv.Y && uv.Y <= 1)) a = 0;
                if (kind == TongueKind)
                {
                    double u = (1385 - 1774 * uv.X) / 970;
                    double field = .28 + .68 * u - .38 * u * u * u + .12 * Math.Sin(19 * u + 11 * uv.Y);
                    a *= 1 - Smooth((erosion - field) / .16);
                }
            }
            else if (kind == CoreKind)
            {
                double coverage = Math.Pow(Math.Max(0, 1 - Hypot(2 * uv.X - 1, 2 * uv.Y - 1)), .70);
                a *= coverage; rgb = Lerp(Palette[PrimaryRole], Palette[CoreRole], coverage);
            }
            return new Rgba(rgb.X * gain * a, rgb.Y * gain * a, rgb.Z * gain * a, a);
        }
    }
}
