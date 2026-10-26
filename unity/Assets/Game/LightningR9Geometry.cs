using System;

namespace DeckRogue.Game
{
    // M-only frozen R9 candidate. Unity shader compilation/device appearance remain unverified.
    // Source: actual_motion/{geometry_cache,shape_profiles,render_contact,optics,render_plan}.
    // No textures, gameplay RNG, material changes, scene state, or allocations in warm Sample.
    internal static class LightningR9Geometry
    {
        public const double NativeLead = .14, VisibleEnd = .26, SourcePixelsPerHeight = 192;
        public const double FootX = 22.0 / SourcePixelsPerHeight, FootY = -1;
        public const double SourcePixelWorld = 5.4499998 / 720.0;
        public const int BatchCount = 2, RowsPerSegment = 4, VerticesPerSegment = 8;
        public const int MainKind = 0, BranchKind = 1, CrownKind = 2, TertiaryKind = 3;
        public const int LateralKind = 4, QuietGroundKind = 5, FloorForkKind = 6, LeaderKind = 7;
        public const int ContactAirKind = 8, HotKind = 9, StreamerKind = 10;
        public static readonly Point ReferenceFoot = new Point(FootX, FootY, 0);
        public struct Point
        {
            public double X, Y, Z;
            public Point(double x, double y, double z) { X = x; Y = y; Z = z; }
        }
        public struct Quad
        {
            public double X, Y, Z, W;
            public Quad(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }
        }
        public sealed class MeshData
        {
            public readonly Point[] Vertices;
            // UV0: signed transverse distance(px), segment t / field y, width(px), kind.
            // UV1: independent core, plasma, air, scatter radiance strengths, never alpha.
            // UV2: source x, source y, common transverse support radius(px), distance to run end(px).
            public readonly Quad[] UV0, UV1, UV2;
            public readonly double[] GroundWeight;
            public readonly int[] Triangles;
            public readonly bool Floor;
            public readonly int VertexCount, TriangleCount;
            public bool Visible;
            internal MeshData(int segments, int fields, bool floor)
            {
                Floor = floor; VertexCount = segments * VerticesPerSegment + fields * 4;
                TriangleCount = segments * 6 + fields * 2;
                Vertices = new Point[VertexCount]; UV0 = new Quad[VertexCount];
                UV1 = new Quad[VertexCount]; UV2 = new Quad[VertexCount];
                GroundWeight = new double[VertexCount]; Triangles = new int[TriangleCount * 3];
                int at = 0;
                for (int s = 0; s < segments; s++)
                    for (int row = 0; row < RowsPerSegment - 1; row++)
                    {
                        int v = s * VerticesPerSegment + row * 2;
                        Triangles[at++] = v; Triangles[at++] = v + 1; Triangles[at++] = v + 3;
                        Triangles[at++] = v; Triangles[at++] = v + 3; Triangles[at++] = v + 2;
                    }
                for (int f = 0; f < fields; f++)
                {
                    int v = segments * VerticesPerSegment + f * 4;
                    Triangles[at++] = v; Triangles[at++] = v + 1; Triangles[at++] = v + 2;
                    Triangles[at++] = v; Triangles[at++] = v + 2; Triangles[at++] = v + 3;
                }
            }
        }
        internal sealed class PathSpec
        {
            internal readonly string Name;
            internal readonly int Kind, Seed;
            internal readonly bool Floor;
            internal readonly double Birth, Tau, FragmentStart, FragmentSpan, FragmentMax, Depth;
            internal readonly double[] XY, Widths, Energy, Random, Lengths, Cumulative, ClipLengths;
            internal readonly double TotalLength, ClipTotal, Radius, MaxEnergy;
            internal readonly int Count;
            internal PathSpec(string name, int kind, bool floor, int seed, double birth, double tau,
                double fragmentStart, double fragmentSpan, double fragmentMax, double depth,
                double[] xy, double[] widths, double[] energy, double[] random, double[] clipLengths)
            {
                Name = name; Kind = kind; Floor = floor; Seed = seed; Birth = birth; Tau = tau;
                FragmentStart = fragmentStart; FragmentSpan = fragmentSpan; FragmentMax = fragmentMax;
                Depth = depth; XY = xy; Widths = widths; Energy = energy; Random = random; Count = widths.Length;
                Lengths = new double[Count]; Cumulative = new double[Count + 1];
                double maxWidth = 0;
                for (int i = 0; i < Count; i++)
                {
                    double dx = XY[i * 2 + 2] - XY[i * 2], dy = XY[i * 2 + 3] - XY[i * 2 + 1];
                    Lengths[i] = Math.Sqrt(dx * dx + dy * dy); Cumulative[i + 1] = Cumulative[i] + Lengths[i];
                    maxWidth = Math.Max(maxWidth, widths[i]);
                    MaxEnergy = Math.Max(MaxEnergy, energy[i]);
                }
                TotalLength = Cumulative[Count]; ClipLengths = clipLengths ?? Lengths;
                for (int i = 0; i < Count; i++) ClipTotal += ClipLengths[i];
                double sigma = kind == MainKind ? 14 : kind == BranchKind ? 2.4 : kind == CrownKind ? 1.7 : kind == LateralKind ? 3.2 : kind == LeaderKind ? 2 : 0;
                if (kind == MainKind) maxWidth *= 2.55;
                Radius = Math.Max(2 * maxWidth, 4 * sigma + maxWidth);
            }
        }
        internal sealed class PathState
        {
            internal readonly int Batch, VertexOffset;
            internal readonly bool[] Active;
            internal readonly double[] Fraction, ToStart, ToEnd;
            internal PathState(PathSpec p, int offset)
            {
                Batch = p.Floor ? 1 : 0; VertexOffset = offset;
                Active = new bool[p.Count]; Fraction = new double[p.Count];
                ToStart = new double[p.Count]; ToEnd = new double[p.Count];
            }
        }
        public sealed class Buffer
        {
            public readonly MeshData[] Batches;
            public readonly int VertexCount, TriangleCount;
            internal readonly PathState[] States;
            internal readonly int FieldsOffset;
            public double VisualAge, MainCoreEnvelope, PlasmaEnvelope, AirEnvelope, ScatterEnvelope;
            public double TargetLightEnvelope, FloorLightEnvelope;
            internal Buffer()
            {
                int body = 0, floor = 0;
                for (int i = 0; i < Paths.Length; i++) { if (Paths[i].Floor) floor += Paths[i].Count; else body += Paths[i].Count; }
                Batches = new[] { new MeshData(body, 2, false), new MeshData(floor, 0, true) };
                FieldsOffset = body * VerticesPerSegment; States = new PathState[Paths.Length];
                int bodyOffset = 0, floorOffset = 0;
                for (int i = 0; i < Paths.Length; i++)
                {
                    PathSpec p = Paths[i]; States[i] = new PathState(p, p.Floor ? floorOffset : bodyOffset);
                    if (p.Floor) floorOffset += p.Count * VerticesPerSegment; else bodyOffset += p.Count * VerticesPerSegment;
                }
                VertexCount = Batches[0].VertexCount + Batches[1].VertexCount;
                TriangleCount = Batches[0].TriangleCount + Batches[1].TriangleCount;
            }
        }
        public static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        public static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        public static double Smooth(double x) { x = Clamp(x); return x * x * (3 - 2 * x); }
        public static double Lead() { return NativeLead; }
        public static double End() { return VisibleEnd; }
        public static Buffer Create()
        {
            Buffer b = new Buffer(); Sample(b, 0); return b;
        }
        public static void Sample(Buffer b, double age)
        {
            if (b == null) throw new ArgumentNullException("b");
            if (!Finite(age)) throw new ArgumentException("Finite age required.", "age");
            b.VisualAge = age;
            bool post = age >= 0 && age < VisibleEnd, pre = age > -NativeLead && age < 0;
            b.MainCoreEnvelope = post ? Math.Exp(-age / .019) : 0;
            b.PlasmaEnvelope = post ? Math.Exp(-age / .036) : 0;
            b.AirEnvelope = post ? Math.Exp(-age / .043) : 0;
            b.ScatterEnvelope = post ? Math.Exp(-age / .062) : 0;
            b.TargetLightEnvelope = b.AirEnvelope;
            b.FloorLightEnvelope = post ? Math.Exp(-age / .058) : 0;
            for (int i = 0; i < b.Batches.Length; i++)
            {
                MeshData m = b.Batches[i]; m.Visible = false;
                Array.Clear(m.UV1, 0, m.UV1.Length);
            }
            for (int i = 0; i < Paths.Length; i++) SamplePath(b, Paths[i], b.States[i], age, pre, post);
            SampleField(b, 0, ContactAirKind, 124, 124, .13, post ? 4.8 * Math.Exp(-age / .045) : 0);
            SampleField(b, 1, HotKind, 32, 20, .15, post ? 3.8 * Math.Exp(-age / .022) : 0);
        }
        static void SamplePath(Buffer b, PathSpec p, PathState s, double age, bool pre, bool post)
        {
            bool precursor = p.Kind == LeaderKind || p.Kind == StreamerKind;
            double localAge = age - p.Birth;
            bool enabled = precursor ? pre : post && localAge >= 0 && (p.Kind != TertiaryKind || age < .083);
            double clipFraction = 1, leaderU = 0;
            if (p.Kind == LeaderKind) { leaderU = Smooth((age + NativeLead) / NativeLead); clipFraction = .04 + .91 * leaderU; }
            if (p.Kind == StreamerKind) { enabled &= age > -.034; clipFraction = Clamp((age + .034) / .034); }
            double remaining = p.ClipTotal * clipFraction;
            double fragment = precursor ? 0 : Smooth((localAge - p.FragmentStart) / p.FragmentSpan) * p.FragmentMax;
            for (int k = 0; k < p.Count; k++)
            {
                double fraction = precursor ? Clamp(remaining / p.ClipLengths[k]) : 1;
                remaining -= p.ClipLengths[k]; s.Fraction[k] = fraction;
                bool tipSkip = p.Kind == FloorForkKind && p.Cumulative[k] / p.TotalLength > .78 && (k + p.Seed) % 3 != 0;
                s.Active[k] = enabled && fraction > 0 && !(fragment > 0 && p.Random[k] < fragment) && !tipSkip;
            }
            double run = 0;
            for (int k = 0; k < p.Count; k++) { s.ToStart[k] = run; run = s.Active[k] ? run + p.Lengths[k] * s.Fraction[k] : 0; }
            run = 0;
            for (int k = p.Count - 1; k >= 0; k--) { s.ToEnd[k] = run; run = s.Active[k] ? run + p.Lengths[k] * s.Fraction[k] : 0; }
            MeshData m = b.Batches[s.Batch];
            for (int k = 0; k < p.Count; k++)
            {
                bool active = s.Active[k]; if (active) m.Visible = true;
                double env = enabled && !precursor ? Math.Exp(-localAge / p.Tau) : 0;
                Quad strength = new Quad();
                if (active)
                {
                    double e = p.Energy[k];
                    switch (p.Kind)
                    {
                        case MainKind: strength = new Quad(e * b.MainCoreEnvelope, 2.5 * b.PlasmaEnvelope, 5.3 * b.AirEnvelope, 5.2 * b.ScatterEnvelope); break;
                        case BranchKind: strength = new Quad(e * env, 1.9 * e * env, 0, 0); break;
                        case CrownKind: strength = new Quad(e * env, 1.15 * e * env, 0, 0); break;
                        case LateralKind: strength = new Quad(e * env, 0, 1.8 * 4.3 * Math.Exp(-age / .056), 0); break;
                        case LeaderKind: strength = new Quad(.2 + .23 * leaderU, 0, .7, 0); break;
                        case StreamerKind: strength = new Quad(.23, 0, 0, 0); break;
                        default: strength = new Quad(e * env, 0, 0, 0); break;
                    }
                    // Match source curve()/ribbon() construction's radiance cut-off.
                    // Weighted branch ribbons use their path's maximum for admission.
                    if (strength.X < .001) strength.X = 0;
                    if (p.Kind == BranchKind && 1.9 * p.MaxEnergy * env < .001) strength.Y = 0;
                    else if (p.Kind == CrownKind && 1.15 * p.MaxEnergy * env < .001) strength.Y = 0;
                    else if (p.Kind != BranchKind && p.Kind != CrownKind && strength.Y < .001) strength.Y = 0;
                    if (strength.Z < .001) strength.Z = 0;
                    if (strength.W < .001) strength.W = 0;
                }
                double ax = p.XY[k * 2], ay = p.XY[k * 2 + 1];
                double dx = p.XY[k * 2 + 2] - ax, dy = p.XY[k * 2 + 3] - ay;
                double fraction = s.Fraction[k], length = p.Lengths[k] * fraction;
                for (int row = 0; row < RowsPerSegment; row++)
                {
                    double t = row == 0 ? 0 : row == 1 ? .25 : row == 2 ? .75 : 1;
                    double st = t * fraction, x = ax + dx * st, y = ay + dy * st;
                    double q = (p.Cumulative[k] + p.Lengths[k] * st) / p.TotalLength;
                    double nx = -dy / p.Lengths[k], ny = dx / p.Lengths[k];
                    int neighbor = row == 0 ? k - 1 : row == RowsPerSegment - 1 && fraction == 1 ? k + 1 : -1;
                    if (neighbor >= 0 && neighbor < p.Count && active && s.Active[neighbor])
                    {
                        double ndx = p.XY[neighbor * 2 + 2] - p.XY[neighbor * 2];
                        double ndy = p.XY[neighbor * 2 + 3] - p.XY[neighbor * 2 + 1];
                        nx += -ndy / p.Lengths[neighbor]; ny += ndx / p.Lengths[neighbor];
                        double n = Math.Sqrt(nx * nx + ny * ny);
                        if (n > 1e-8) { nx /= n; ny /= n; } else { nx = -dy / p.Lengths[k]; ny = dx / p.Lengths[k]; }
                    }
                    double width = p.Widths[k];
                    if (p.Kind == LateralKind || p.Kind == QuietGroundKind || p.Kind == FloorForkKind)
                    {
                        double wave = Math.Sin((k + st) * 1.73 + .4);
                        width *= (.66 + .37 * wave * wave) * (1 - .32 * q);
                        if (p.Kind == FloorForkKind) width *= 1 - .68 * p.Cumulative[k] / p.TotalLength;
                    }
                    double endDistance = Math.Min(s.ToStart[k] + length * t, s.ToEnd[k] + length * (1 - t));
                    double depth = p.Depth / (SourcePixelsPerHeight * SourcePixelWorld) * Smooth(q / .25);
                    for (int side = 0; side < 2; side++)
                    {
                        int v = s.VertexOffset + k * VerticesPerSegment + row * 2 + side;
                        double distance = side == 0 ? -p.Radius : p.Radius;
                        double originX = p.Floor ? 309 : 287, originY = p.Floor ? 372 : 180;
                        m.Vertices[v] = new Point((x + nx * distance - originX) / SourcePixelsPerHeight,
                            (originY - y - ny * distance) / SourcePixelsPerHeight, depth);
                        // The source warp is evaluated at the centerline, not at each lateral edge.
                        m.GroundWeight[v] = p.Floor ? 0 : Clamp((y - 180) / SourcePixelsPerHeight);
                        m.UV0[v] = new Quad(distance, t, width, p.Kind);
                        m.UV1[v] = strength;
                        m.UV2[v] = new Quad(x, y, p.Radius, Math.Max(0, endDistance));
                    }
                }
            }
        }
        static void SampleField(Buffer b, int field, int kind, double width, double height, double depth, double strength)
        {
            MeshData m = b.Batches[0]; if (strength > 0) m.Visible = true;
            for (int corner = 0; corner < 4; corner++)
            {
                double x = corner == 0 || corner == 3 ? -1 : 1, y = corner < 2 ? -1 : 1;
                int v = b.FieldsOffset + field * 4 + corner;
                m.Vertices[v] = new Point(x * width * .5 / SourcePixelsPerHeight, y * height * .5 / SourcePixelsPerHeight,
                    depth / (SourcePixelsPerHeight * SourcePixelWorld));
                m.GroundWeight[v] = 0;
                m.UV0[v] = new Quad(x, y, 0, kind); m.UV1[v] = new Quad(strength, 0, 0, 0); m.UV2[v] = new Quad();
            }
        }
        // CPU numerical contract for LightningR9Candidate.shader, not proof of native compilation.
        // Abramowitz-Stegun erf approximation: absolute error < 1.5e-7 (double reference tests).
        public static double Erf(double value)
        {
            double a = Math.Abs(value), t = 1 / (1 + .3275911 * a);
            double result = 1 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - .284496736) * t + .254829592) * t * Math.Exp(-a * a);
            return value < 0 ? -result : result;
        }
        public static double Profile(double distance, double width, double sigma)
        {
            return Math.Max(0, .5 * (Erf((distance + width * .5) / (1.4142135623730951 * sigma)) - Erf((distance - width * .5) / (1.4142135623730951 * sigma))));
        }
        static double Gaussian(double x, double y, double cx, double cy, double sx, double sy)
        {
            x = (x - cx) / sx; y = (y - cy) / sy; return Math.Exp(-.5 * (x * x + y * y));
        }
        public static double Srgb(double s) { return s <= .04045 ? s / 12.92 : Math.Pow((s + .055) / 1.055, 2.4); }
        static readonly Point Shadow = Color(0x333557), Primary = Color(0x928cc6), Edge = Color(0xbab4e3), Core = Color(0xeeeafa);
        static readonly Point Violet = Color(0x7765b8);
        static Point Color(int hex) { return new Point(Srgb(((hex >> 16) & 255) / 255.0), Srgb(((hex >> 8) & 255) / 255.0), Srgb((hex & 255) / 255.0)); }
        static Point Sum(Point a, double sa, Point b, double sb, Point c, double sc, Point d, double sd)
        {
            return new Point(a.X * sa + b.X * sb + c.X * sc + d.X * sd, a.Y * sa + b.Y * sb + c.Y * sc + d.Y * sd, a.Z * sa + b.Z * sb + c.Z * sc + d.Z * sd);
        }
        public static Point EvaluateRadiance(Quad uv0, Quad energy, Quad uv2)
        {
            int kind = (int)Math.Round(uv0.W);
            if (kind == ContactAirKind || kind == HotKind)
            {
                double u = (uv0.X + 1) * .5, v = (uv0.Y + 1) * .5;
                double window = Smooth(u / .1) * Smooth((1 - u) / .1) * Smooth(v / .1) * Smooth((1 - v) / .1);
                double x = uv0.X * (kind == HotKind ? 16 : 62), y = -uv0.Y * (kind == HotKind ? 10 : 62);
                double density = kind == HotKind
                    ? Gaussian(x, y, 0, 0, 3.3, 2) + .38 * Gaussian(x, y, -6, -2, 4.5, 1.8)
                    : (.82 * Gaussian(x, y, -10, -4, 17, 13) + .42 * Gaussian(x, y, 18, 5, 20, 8) + .24 * Gaussian(x, y, 4, -23, 11, 21)) * (1 - .67 * Gaussian(x, y, 6, 7, 8, 13));
                return Sum(kind == HotKind ? Core : Violet, energy.X * density * window, Core, 0, Core, 0, Core, 0);
            }
            double d = uv0.X, width = uv0.Z, radius = uv2.Z;
            if (Math.Abs(d) >= radius || uv2.W <= 0 || width <= 0) return new Point();
            double coreCoverage = Smooth((width * .5 - Math.Abs(d)) / Math.Max(.06, width * .15)) * Smooth(uv2.W / Math.Max(.1, width));
            double plasma = 0, air = 0, scatter = 0;
            double endWindow = Smooth(uv2.W / 7);
            Point coreColor = Core, plasmaColor = Primary, airColor = Primary;
            if (kind == MainKind)
            {
                double w = width * 2.55;
                plasma = Profile(d, w, .7) * Smooth((radius - Math.Abs(d)) / 1);
                double wave = Math.Sin(uv2.X * .064 + uv2.Y * .041);
                air = Profile(d, w, 5.4) * (.54 + .46 * wave * wave) * Smooth((radius - Math.Abs(d)) / 3.24);
                scatter = Profile(d, w, 14) * Smooth((radius - Math.Abs(d)) / 8.4);
            }
            else if (kind == BranchKind) { coreColor = Edge; plasmaColor = Violet; plasma = Profile(d, width, 2.4) * Smooth((radius - Math.Abs(d)) / 1.44); }
            else if (kind == CrownKind) { plasma = Profile(d, width, 1.7) * Smooth((radius - Math.Abs(d)) / 1.02); }
            else if (kind == LateralKind) { coreColor = Edge; airColor = Violet; air = Profile(d, width, 3.2) * Smooth((radius - Math.Abs(d)) / 1.92); }
            else if (kind == LeaderKind) { coreColor = Primary; airColor = Shadow; air = Profile(d, width, 2) * Smooth((radius - Math.Abs(d)) / 1.2); }
            else if (kind == QuietGroundKind || kind == StreamerKind) coreColor = Primary;
            else coreColor = Edge;
            return Sum(coreColor, energy.X * coreCoverage, plasmaColor, energy.Y * plasma * endWindow,
                airColor, energy.Z * air * endWindow, Shadow, energy.W * scatter * endWindow);
        }
        // Baked from source JSON and NumPy PCG64 seeds by qa/lightning_r9_geometry/export_source.py.
        internal static readonly PathSpec[] Paths = {
            new PathSpec("main", 0, false, 77, 0, 0.019, 0.044999999999999998, 0.11, 0.89000000000000001, 0,
                new double[] { 135, -14, 154, 22, 180, 38, 176, 50, 218, 93, 220, 111, 250, 137, 265, 162, 287, 180, 300, 195, 320, 188, 333, 207, 351, 195, 368, 210, 390, 201, 406, 213 },
                new double[] { 0.74373402647257947, 0.68608860762583634, 0.82948876437983066, 0.65651195682854535, 0.90402862194081457, 1.0961495479110037, 1.7990633851600528, 2.867019839591288, 2.2725447656057485, 2.456415222367752, 1.6358909708511118, 1.2289475079801289, 1.0954583068245021, 0.72035660032278637, 0.84695988927681676 },
                new double[] { 5.1217493739146382, 5.8084626008346838, 4.8763907091269534, 6.3509670144159625, 5.1940103600786474, 7.1952034836461785, 9.3582108352299418, 11.365843538882103, 13.721764343210021, 9.9168488414999061, 10.388633453614093, 6.7261810286667938, 6.4767885062952546, 5.6575679963787566, 5.0141316634052737 },
                new double[] { 0.78593230854249307, 0.55117195580031519, 0.24388819695343822, 0.33445320647792809, 0.31869019776778229, 0.39015401116516413, 0.80130067964236051, 0.090801639991679894, 0.37362439276446435, 0.79132290026416818, 0.75867146456836199, 0.60397503938027419, 0.13074783281680769, 0.35984468315075979, 0.87080485249742168 },
                null),
            new PathSpec("branch_0", 1, false, 3, 0, 0.041000000000000002, 0.028000000000000001, 0.051999999999999998, 0.93999999999999995, 0.055,
                new double[] { 265, 162, 258.53505650444021, 168.15253660703183, 255.90405131897612, 167.10405000303436, 254.04959984930912, 163.84972185989918, 252.73427337302348, 164.57125217688139, 249.33363703169437, 167.59424190601766, 244.97790474871491, 173.4070911176799, 237, 174 },
                new double[] { 0.64000000000000001, 0.5742804119760665, 0.50661889680863381, 0.43660964524822332, 0.36365732727203237, 0.28681186818363019, 0.2043379195593194 },
                new double[] { 1.8, 1.5954061656259599, 1.3840076869359001, 1.1641659694101023, 0.93332875509555457, 0.68702020373127182, 0.41549757290127753 },
                new double[] { 0.085649167143624361, 0.2368105065960997, 0.80127446520639689, 0.58216203606436778, 0.094128642240399185, 0.4331269402364738, 0.47905129814083403 },
                null),
            new PathSpec("branch_1", 1, false, 4, 0.002, 0.067000000000000004, 0.049000000000000002, 0.051999999999999998, 0.93999999999999995, 0.055,
                new double[] { 300, 195, 300.77854554873932, 197.07608763748291, 301.01349845868879, 197.45773821220874, 306.34533221790559, 199.1300690666898, 307.14405353080076, 209.69804266401528, 311.4294639812353, 206.66467081664192, 311.35411923103925, 211.10685619570017, 317, 217 },
                new double[] { 0.64000000000000001, 0.5742804119760665, 0.50661889680863381, 0.43660964524822332, 0.36365732727203237, 0.28681186818363019, 0.2043379195593194 },
                new double[] { 1.8, 1.5954061656259599, 1.3840076869359001, 1.1641659694101023, 0.93332875509555457, 0.68702020373127182, 0.41549757290127753 },
                new double[] { 0.94305610557236763, 0.5113275528143616, 0.97624370570770413, 0.080836023895602183, 0.60735583199502963, 0.37648658437727256, 0.80190120698580725 },
                null),
            new PathSpec("branch_2", 1, false, 5, 0.0040000000000000001, 0.092999999999999999, 0.070000000000000007, 0.051999999999999998, 0.93999999999999995, 0.055,
                new double[] { 333, 207, 329.74063606333971, 208.74993466805978, 323.95019890086616, 212.06664623614614, 321.42691010149355, 211.20336665938848, 321.52773605351325, 212.15566802986504, 320.4680936209279, 211.88745541179773, 311.40169244605084, 214.53613090180767, 299, 222 },
                new double[] { 0.64000000000000001, 0.5742804119760665, 0.50661889680863381, 0.43660964524822332, 0.36365732727203237, 0.28681186818363019, 0.2043379195593194 },
                new double[] { 1.8, 1.5954061656259599, 1.3840076869359001, 1.1641659694101023, 0.93332875509555457, 0.68702020373127182, 0.41549757290127753 },
                new double[] { 0.80500292374538018, 0.80794078973649375, 0.515325561042142, 0.2858013800881416, 0.053930702381656426, 0.38336888078551823, 0.40847320541999865 },
                null),
            new PathSpec("branch_3", 1, false, 6, 0, 0.041000000000000002, 0.028000000000000001, 0.051999999999999998, 0.93999999999999995, 0.055,
                new double[] { 368, 210, 374.61961232552761, 213.80258631617531, 377.49524883393076, 218.99660319623487, 378.01006851097299, 218.69337652015977, 384.30514417516684, 222.76943909746967, 383.41563526077039, 223.5927586622023, 386.2413877983962, 228.22602183278815, 391, 235 },
                new double[] { 0.64000000000000001, 0.5742804119760665, 0.50661889680863381, 0.43660964524822332, 0.36365732727203237, 0.28681186818363019, 0.2043379195593194 },
                new double[] { 1.8, 1.5954061656259599, 1.3840076869359001, 1.1641659694101023, 0.93332875509555457, 0.68702020373127182, 0.41549757290127753 },
                new double[] { 0.53816435147194319, 0.34327086981333843, 0.36906723979537825, 0.37449676558788236, 0.98744499018646659, 0.63275627260714606, 0.67432393050449357 },
                null),
            new PathSpec("tertiary_0", 3, false, 53, 0, 0.043999999999999997, 0.025000000000000001, 0.059999999999999998, 0.90000000000000002, 0.055,
                new double[] { 252.73427337302343, 164.57125217688139, 257.31516883117615, 167.12577414317346, 266.78203539880388, 168.07337197104465, 266.92739282095692, 168.45914634356669, 272.73427337302343, 172.57125217688139 },
                new double[] { 0.5, 0.40000000000000002, 0.30000000000000004, 0.20000000000000001 },
                new double[] { 1.6499999999999999, 1.2, 0.75, 0.29999999999999999 },
                new double[] { 0.011126994564266246, 0.71891065376770091, 0.3311271911509952, 0.9330886636200264 },
                null),
            new PathSpec("tertiary_3", 3, false, 56, 0, 0.043999999999999997, 0.025000000000000001, 0.059999999999999998, 0.90000000000000002, 0.055,
                new double[] { 384.30514417516684, 222.76943909746967, 378.60356754626469, 223.59798557834111, 370.48777760287493, 226.78114979992347, 369.5708724320084, 228.81950390305246, 367.05142369691117, 229.33559928685688 },
                new double[] { 0.5, 0.40000000000000002, 0.30000000000000004, 0.20000000000000001 },
                new double[] { 1.6499999999999999, 1.2, 0.75, 0.29999999999999999 },
                new double[] { 0.72612604996354002, 0.016902814478643169, 0.98080106776927578, 0.72460421212380033 },
                null),
            new PathSpec("crown_0", 2, false, 321, 0, 0.032000000000000001, 0.025000000000000001, 0.051999999999999998, 0.93999999999999995, 0.074999999999999997,
                new double[] { 287, 180, 278.40181986827446, 173.09680750746065, 277.84071054619272, 173.64628761808703, 268.35476567712522, 170.95141877994277, 245.19083970060439, 158.48532392842623, 242.82845509870455, 158.31535671115651, 239, 157 },
                new double[] { 2.1000000000000001, 1.73, 1.3600000000000001, 0.99000000000000021, 0.62000000000000011, 0.25 },
                new double[] { 6.5, 5.3100000000000005, 4.1200000000000001, 2.9300000000000002, 1.7400000000000002, 0.55000000000000004 },
                new double[] { 0.65876669538662325, 0.90830835796157439, 0.48343832852577617, 0.7254101662830692, 0.55585650714749868, 0.90057524236120445 },
                null),
            new PathSpec("crown_1", 2, false, 322, 0, 0.045999999999999999, 0.037000000000000005, 0.051999999999999998, 0.93999999999999995, 0.074999999999999997,
                new double[] { 287, 180, 282.98271513762961, 175.5428383474617, 271.88240532922237, 155.43517730497823, 264.53831140632838, 157.41201502257996, 265.97881497447645, 154.26997466795498, 261.5241363500142, 144.74894038823379, 255, 138 },
                new double[] { 1.5, 1.25, 1, 0.75, 0.5, 0.25 },
                new double[] { 4.7999999999999998, 3.9499999999999997, 3.0999999999999996, 2.25, 1.3999999999999999, 0.55000000000000004 },
                new double[] { 0.30865841803565464, 0.47613583535314485, 0.27560469768305318, 0.26060448501886802, 0.57817221242946426, 0.18865979174253211 },
                null),
            new PathSpec("crown_2", 2, false, 323, 0, 0.059999999999999998, 0.049000000000000002, 0.051999999999999998, 0.93999999999999995, 0.074999999999999997,
                new double[] { 287, 180, 287.48304081125104, 171.25579422958131, 282.14233085041911, 160.05802014736338, 280.96482407066952, 156.65830256301123, 280.93575046302698, 152.40197720651679, 281.0849774493509, 152.06049669980845, 280, 148 },
                new double[] { 1.5, 1.25, 1, 0.75, 0.5, 0.25 },
                new double[] { 4.7999999999999998, 3.9499999999999997, 3.0999999999999996, 2.25, 1.3999999999999999, 0.55000000000000004 },
                new double[] { 0.278812721563513, 0.83519165862601619, 0.97387932663911492, 0.74298972077419734, 0.12169238144260341, 0.70878803072900876 },
                null),
            new PathSpec("crown_3", 2, false, 324, 0, 0.032000000000000001, 0.025000000000000001, 0.051999999999999998, 0.93999999999999995, 0.074999999999999997,
                new double[] { 287, 180, 298.05144736763361, 167.5498022014047, 294.60496812372827, 163.68568458073494, 298.22704647301197, 163.28567301740824, 302.02896254392164, 156.64244861662928, 304.40538224819039, 154.80698780143098, 308, 152 },
                new double[] { 2.1000000000000001, 1.73, 1.3600000000000001, 0.99000000000000021, 0.62000000000000011, 0.25 },
                new double[] { 6.5, 5.3100000000000005, 4.1200000000000001, 2.9300000000000002, 1.7400000000000002, 0.55000000000000004 },
                new double[] { 0.24408523539999183, 0.60285411106675724, 0.55929139906237912, 0.3115539947888526, 0.5692071095471275, 0.83698174046624274 },
                null),
            new PathSpec("crown_4", 2, false, 325, 0.016666666666666666, 0.045999999999999999, 0.037000000000000005, 0.051999999999999998, 0.93999999999999995, 0.074999999999999997,
                new double[] { 287, 180, 311.70724223991436, 177.4357903598671, 319.53317757050348, 174.94902542240257, 320.0010635367488, 175.30397134591226, 326.30146525662883, 170.51264384233869, 327.42917024354676, 172.46553722321258, 332, 171 },
                new double[] { 1.5, 1.25, 1, 0.75, 0.5, 0.25 },
                new double[] { 4.7999999999999998, 3.9499999999999997, 3.0999999999999996, 2.25, 1.3999999999999999, 0.55000000000000004 },
                new double[] { 0.46855820995129116, 0.37993473037976866, 0.65074034890730448, 0.63621183294461636, 0.19686899848952721, 0.099303287098819637 },
                null),
            new PathSpec("crown_5", 2, false, 326, 0.016666666666666666, 0.059999999999999998, 0.049000000000000002, 0.051999999999999998, 0.93999999999999995, 0.074999999999999997,
                new double[] { 287, 180, 290.46810721495967, 181.55040753105283, 300.23152946437904, 181.99984209398121, 299.71350098187452, 189.26748529387092, 308.14304589124578, 186.71763530028682, 309.84406996347258, 190.60127572631234, 312.77427410327061, 192.31155035510102 },
                new double[] { 1.5, 1.25, 1, 0.75, 0.5, 0.25 },
                new double[] { 4.7999999999999998, 3.9499999999999997, 3.0999999999999996, 2.25, 1.3999999999999999, 0.55000000000000004 },
                new double[] { 0.047688918705065864, 0.52759666926850002, 0.87016188853296161, 0.92975607893087742, 0.43897395582231802, 0.67575736481271353 },
                null),
            new PathSpec("crown_6", 2, false, 327, 0.016666666666666666, 0.032000000000000001, 0.025000000000000001, 0.051999999999999998, 0.93999999999999995, 0.074999999999999997,
                new double[] { 287, 180, 273.5239173219245, 188.60225633390161, 262.75664551889162, 188.52268230350239, 263.57456730940805, 191.07919605927353, 261.99150223921288, 190.73186261951989, 257.37391753803723, 192.27629393668863, 250.36569880436082, 196.415400473468 },
                new double[] { 2.1000000000000001, 1.73, 1.3600000000000001, 0.99000000000000021, 0.62000000000000011, 0.25 },
                new double[] { 6.5, 5.3100000000000005, 4.1200000000000001, 2.9300000000000002, 1.7400000000000002, 0.55000000000000004 },
                new double[] { 0.63716003587189729, 0.22477695288411981, 0.58480053257386255, 0.1427971051501683, 0.77349030718939538, 0.094341972247867112 },
                null),
            new PathSpec("lateral", 4, false, 919, 0, 0.051999999999999998, 0.044999999999999998, 0.070000000000000007, 0.92000000000000004, 0.074999999999999997,
                new double[] { 289, 181, 296, 201, 312, 198, 317, 217, 341, 210, 353, 224 },
                new double[] { 1.1000000000000001, 1.1000000000000001, 1.1000000000000001, 1.1000000000000001, 1.1000000000000001 },
                new double[] { 2.6000000000000001, 2.6000000000000001, 2.6000000000000001, 2.6000000000000001, 2.6000000000000001 },
                new double[] { 0.95121780372887299, 0.79655528817276366, 0.54682256223189041, 0.23743503737116844, 0.46592645466176907 },
                null),
            new PathSpec("quiet_ground", 5, false, 661, 0, 0.083000000000000004, 0.029000000000000001, 0.089999999999999997, 0.94999999999999996, 0,
                new double[] { 289, 182, 291, 217, 303, 241, 297, 271, 310, 296, 300, 329, 309, 372 },
                new double[] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 },
                new double[] { 0.22, 0.22, 0.22, 0.22, 0.22, 0.22 },
                new double[] { 0.2167967234311472, 0.5476183966846363, 0.70671828359250177, 0.85119688923583292, 0.036054522642977371, 0.77253341925036356 },
                null),
            new PathSpec("ground_fork_0", 6, true, 37, 0, 0.058999999999999997, 0.047, 0.12, 0.90000000000000002, 0,
                new double[] { 309, 372, 346.71388455520724, 376.88665273949465, 350.98802973966622, 372.58355997277823, 361.01107353814911, 375.18324935888455, 362.0427585458587, 377.55544810343696, 363.66271212786603, 376.50058478364235, 368.9628898190403, 377.14582104959118, 372.17507701938592, 376.8585873847415, 378.07573861894161, 377.24595848773703 },
                new double[] { 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998 },
                new double[] { 0.371, 0.371, 0.371, 0.371, 0.371, 0.371, 0.371, 0.371 },
                new double[] { 0.70415303161356957, 0.66166872080247408, 0.06899323623202247, 0.70280164710971882, 0.31896221411422587, 0.45009567421035723, 0.98061453340247351, 0.06443209671845207 },
                null),
            new PathSpec("ground_fork_1", 6, true, 38, 0, 0.058999999999999997, 0.047, 0.12, 0.90000000000000002, 0,
                new double[] { 309, 372, 304.86374840258594, 372.09854324774341, 296.95851535194271, 367.31817148368327, 296.57409021694247, 368.70229247506632, 295.67637800098532, 368.43924814778745, 293.28272176315511, 370.63753810665355, 292.5286772398162, 367.20694853027555, 288.26640234561751, 365.32589156619753, 271.92126532936715, 364.463137807213 },
                new double[] { 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998 },
                new double[] { 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996 },
                new double[] { 0.48576104483786753, 0.24973585906507756, 0.72038116641110073, 0.63461400626186049, 0.93257174200865856, 0.039525154550753561, 0.83509032301390751, 0.49659704805823346 },
                null),
            new PathSpec("ground_fork_2", 6, true, 39, 0, 0.058999999999999997, 0.047, 0.12, 0.90000000000000002, 0,
                new double[] { 309, 372, 296.54747894996154, 370.47069997939502, 294.00475190234891, 368.51328642867469, 293.84157240791319, 367.50477799079977, 293.55957943383112, 367.90867216939967, 291.94432022561267, 366.32945527547582, 288.35985926422063, 365.95932109976434, 269.93291881977592, 359.77823268188234, 264.93626301618775, 359.34439323494564 },
                new double[] { 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998, 0.84999999999999998 },
                new double[] { 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996, 0.69999999999999996 },
                new double[] { 0.50171195715140815, 0.53933540620648857, 0.13587420195528843, 0.30638656842165901, 0.5791846788017333, 0.79128663949656153, 0.15107398269944206, 0.22916386669926614 },
                null),
            new PathSpec("leader", 7, false, 0, 0, 0, 0, 1, 0, 0,
                new double[] { 135, -14, 154, 22, 180, 38, 176, 50, 218, 93, 220, 111, 250, 137, 265, 162, 287, 180, 300, 195, 320, 188, 333, 207, 351, 195, 368, 210, 390, 201, 406, 213 },
                new double[] { 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997, 0.71999999999999997 },
                new double[] { 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001, 0.20000000000000001 },
                new double[] { 0.63696168732145431, 0.26978671376387031, 0.040973523936194689, 0.016527635528529094, 0.81327023920027242, 0.91275557727772172, 0.60663577576717986, 0.7294965609839984, 0.54362499146542287, 0.93507242378776823, 0.81585355412153215, 0.0027385001701480949, 0.85740427658756935, 0.033585575305464355, 0.72965544642994407 },
                new double[] { 40.70626487409524, 30.528675044947494, 12.649110640673518, 60.108235708594876, 18.110770276274835, 39.698866482558458, 29.154759474226502, 28.42534080710379, 25.755821668303398, 19.555155421182629, 30.281376228362156, 20.160469695530338, 28.714983306222532, 21.860019655138753, 24.870109978070108 }),
            new PathSpec("streamer", 10, false, 0, 0, 0, 0, 1, 0, 0,
                new double[] { 309.00000000000011, 371.99999999999994, 305.73316685324079, 346.55612926612457, 323.33947346735368, 324.3953386269427, 322.5092102010305, 300.59300794041411 },
                new double[] { 0.59999999999999998, 0.59999999999999998, 0.59999999999999998 },
                new double[] { 0.23000000000000001, 0.23000000000000001, 0.23000000000000001 },
                new double[] { 0.63696168732145431, 0.26978671376387031, 0.040973523936194689 },
                new double[] { 33.241540277189323, 28.792360097775937, 30.364452901377952 }),
        };
    }
}
