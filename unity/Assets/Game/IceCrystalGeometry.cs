using System;

namespace DeckRogue.Game
{
    // Frozen radial nine-cell Ice R2 reconstruction, 2026-10-09. Unity/device visuals unverified.
    // Manifest SHA256: b8207f936f0beb8c02dc9c8448e535fcc125de54b1d9eb3c2dbeb8ec7bf03fa4.
    // Contact-relative seconds; captured camera right/up/towards-camera, in stable visible H.
    // No gameplay events, I/O, Unity dependency, RNG, clipping or warm allocation.
    internal static class IceCrystalGeometry
    {
        public const int CrystalKind = 0, CoreKind = 1, SolidKind = 2;
        public const int ShadowRole = 0, PrimaryRole = 1, EdgeRole = 2, CoreRole = 3;
        public const int VertexCount = 109, TriangleCount = 62, BatchCount = 3;
        public const int CrystalCount = 9, ChipCount = 14;
        public const double NativeLead = .130, VisibleEnd = .305, RigidHoldEnd = .055;
        public const double PivotX = 480.0 / 887, PivotY = 1 - 420.0 / 887;
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
            // Upload Front to UV1.x. Reveal is applied per fragment, never baked into Alpha.
            public readonly double[] Alpha, Gain, Front;
            public readonly int[] Role, Triangles;
            internal MeshData(int vertices, int triangles, int kind, double gain)
            {
                Kind = kind; Vertices = new Point[vertices]; UV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices]; Front = new double[vertices];
                Role = new int[vertices]; Triangles = new int[triangles * 3];
                for (int i = 0; i < vertices; i++) { Gain[i] = gain; Role[i] = EdgeRole; Front[i] = 2; }
            }
        }
        public sealed class Buffer
        {
            public readonly MeshData[] Batches;
            public MeshData Crystals { get { return Batches[0]; } }
            public MeshData Main { get { return Batches[0]; } }
            public MeshData Core { get { return Batches[1]; } }
            public MeshData Chips { get { return Batches[2]; } }
            public readonly bool[] ChipActive;
            public readonly Point[] ChipCenters;
            public double VisualAge, LightEnvelope, RevealFront;
            // Newly calibrated R2 Blender Area watts; not Unity point-light intensity.
            public double TargetAreaW, FloorAreaW;
            internal Buffer()
            {
                Batches = new[] { new MeshData(44, 26, CrystalKind, 1.75),
                    new MeshData(9, 8, CoreKind, 3.0), new MeshData(56, 28, SolidKind, 1.55) };
                ChipActive = new bool[ChipCount]; ChipCenters = new Point[ChipCount];
            }
        }
        static readonly int[] CellStarts = { 0, 8, 13, 18, 22, 26, 31, 35, 40, 44 };
        static readonly double[] Rotation = { .10, .21, -.17, .27, -.25, .19, -.21, .24, -.16 };
        // Exact binary64 polygon coordinates from hash-verified runtime/cells.json.
        // These preserve the reference's independently clipped shared-edge roundoff.
        static readonly Uv[] Cells = {
            // Cell 0
            new Uv(0.4405822836871397, 0.4963086080275954),
            new Uv(0.5163468214160081, 0.43997903105578556),
            new Uv(0.6335475840541117, 0.42131454260392154),
            new Uv(0.6484986630565808, 0.5024885407846669),
            new Uv(0.6492035959125354, 0.5786896634169102),
            new Uv(0.5983706141377628, 0.6262474702668746),
            new Uv(0.5207017551078363, 0.6141400861026001),
            new Uv(0.44858381306299416, 0.5760570061590002),
            // Cell 1
            new Uv(0.4405822836871397, 0.4963086080275954),
            new Uv(0.0, 0.36406867600334225),
            new Uv(0.0, 0.0),
            new Uv(0.39020819910371524, 0.0),
            new Uv(0.5163468214160081, 0.43997903105578556),
            // Cell 2
            new Uv(0.5163468214160081, 0.43997903105578556),
            new Uv(0.39020819910371524, 0.0),
            new Uv(1.0, 0.0),
            new Uv(1.0, 0.00416976969112226),
            new Uv(0.6335475840541117, 0.42131454260392154),
            // Cell 3
            new Uv(0.6335475840541117, 0.42131454260392154),
            new Uv(1.0, 0.004169769691121872),
            new Uv(1.0, 0.42388601515433544),
            new Uv(0.6484986630565808, 0.5024885407846669),
            // Cell 4
            new Uv(0.6484986630565808, 0.5024885407846669),
            new Uv(1.0, 0.42388601515433544),
            new Uv(1.0, 0.7481436434114404),
            new Uv(0.6492035959125354, 0.5786896634169102),
            // Cell 5
            new Uv(0.6492035959125354, 0.5786896634169102),
            new Uv(1.0, 0.7481436434114401),
            new Uv(1.0, 1.0),
            new Uv(0.8127624270202721, 1.0),
            new Uv(0.5983706141377628, 0.6262474702668746),
            // Cell 6
            new Uv(0.5983706141377628, 0.6262474702668746),
            new Uv(0.812762427020272, 1.0),
            new Uv(0.43067927323710853, 1.0),
            new Uv(0.5207017551078363, 0.6141400861026001),
            // Cell 7
            new Uv(0.5207017551078363, 0.6141400861026001),
            new Uv(0.4306792732371086, 1.0),
            new Uv(0.0, 1.0),
            new Uv(0.0, 0.8162447743376113),
            new Uv(0.44858381306299416, 0.5760570061590002),
            // Cell 8
            new Uv(0.44858381306299416, 0.5760570061590002),
            new Uv(0.0, 0.8162447743376113),
            new Uv(0.0, 0.36406867600334214),
            new Uv(0.4405822836871397, 0.4963086080275954),
        };
        static readonly Uv[] Centers = MakeCenters();
        static Uv[] MakeCenters()
        {
            Uv[] result = new Uv[CrystalCount];
            for (int k = 0; k < CrystalCount; k++)
            {
                double x = 0, y = 0; int count = CellStarts[k + 1] - CellStarts[k];
                for (int i = CellStarts[k]; i < CellStarts[k + 1]; i++) { x += Cells[i].X; y += Cells[i].Y; }
                result[k] = new Uv(x / count, y / count);
            }
            return result;
        }
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
        // Normative R2 negative-only quadratic compression; actual contact is never delayed.
        // NaN result represents Python None when no anticipation was available.
        public static double Clock(double age, double availableAnticipation = double.NaN)
        {
            age = SafeAge(age);
            if (age >= 0) return age;
            double a = double.IsNaN(availableAnticipation) ? NativeLead : Math.Min(NativeLead, Math.Max(0, availableAnticipation));
            if (a <= 0) return double.NaN;
            if (age < -a) return -NativeLead;
            double q = age / a; return age - (NativeLead - a) * (q * q);
        }
        public static double VisualClock(double age, double availableAnticipation = double.NaN)
        { return Clock(age, availableAnticipation); }
        public static double Light(double visualAge)
        {
            double t = SafeAge(visualAge);
            if (t >= 0) return Fade(t, .025, .130);
            double q = Smooth((t + NativeLead) / NativeLead); return .025 * q * q;
        }
        public static double Front(double visualAge)
        {
            double t = SafeAge(visualAge);
            if (t <= 0) return .55 * Math.Pow(Clamp((t + NativeLead) / NativeLead), 10 * NativeLead / .55);
            return .55 + 10 * t;
        }
        static void Quad(int[] triangles, int q, int a, int b, int c, int d)
        {
            int n = q * 6; triangles[n] = a; triangles[n + 1] = b; triangles[n + 2] = c;
            triangles[n + 3] = a; triangles[n + 4] = c; triangles[n + 5] = d;
        }
        public static Buffer Create()
        {
            Buffer b = new Buffer(); int triangle = 0;
            for (int k = 0; k < CrystalCount; k++)
            {
                int start = CellStarts[k], count = CellStarts[k + 1] - start;
                for (int i = 0; i < count; i++) b.Crystals.UV[start + i] = new Uv(Cells[start + i].X * .5, Cells[start + i].Y);
                for (int j = 1; j < count - 1; j++)
                { b.Crystals.Triangles[triangle++] = start; b.Crystals.Triangles[triangle++] = start + j; b.Crystals.Triangles[triangle++] = start + j + 1; }
            }
            b.Core.Vertices[0] = new Point(0, 0, .070); b.Core.UV[0] = new Uv(.5, .5);
            for (int k = 0; k < 8; k++)
            {
                double c = Math.Cos(k * Math.PI / 4), s = Math.Sin(k * Math.PI / 4);
                b.Core.Vertices[k + 1] = new Point(.034 * c, .034 * .8 * s, .067);
                b.Core.UV[k + 1] = new Uv(.5 + .5 * c, .5 + .5 * s);
                b.Core.Triangles[k * 3] = 0; b.Core.Triangles[k * 3 + 1] = k + 1; b.Core.Triangles[k * 3 + 2] = 1 + (k + 1) % 8;
            }
            for (int i = 0; i < ChipCount; i++)
            {
                Quad(b.Chips.Triangles, i, i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3);
                for (int j = 0; j < 4; j++) b.Chips.Role[i * 4 + j] = i % 3 == 0 ? CoreRole : EdgeRole;
            }
            return b;
        }
        static void Crystal(Buffer b, double t, int k, double front, double alpha)
        {
            double a = Math.Max(0, t - RigidHoldEnd), separation = a * a / (a + .045);
            double cx = Centers[k].X, cy = Centers[k].Y, dx = cx - PivotX, dy = cy - PivotY;
            double n = Math.Max(.07, Math.Sqrt(dx * dx + dy * dy)); dx /= n; dy /= n;
            double speed = k == 0 ? 0 : .65 + .11 * (k % 3);
            double tx = dx * speed * separation, ty = dy * speed * separation - (k == 0 ? .2 : .8) * a * a;
            double angle = Rotation[k] * separation, ca = Math.Cos(angle), sa = Math.Sin(angle);
            for (int i = CellStarts[k]; i < CellStarts[k + 1]; i++)
            {
                double x = Cells[i].X - cx, y = Cells[i].Y - cy;
                b.Crystals.Vertices[i] = new Point(cx - PivotX + ca * x - sa * y + tx,
                    cy - PivotY + sa * x + ca * y + ty, .026 + .002 * k);
                b.Crystals.Alpha[i] = alpha; b.Crystals.Front[i] = front;
            }
        }
        static void Chip(Buffer b, double t, int i)
        {
            double born = .055 + .006 * (i % 7), age = Math.Max(0, t - born), life = .170;
            double phi = i * 2.399963, speed = .8 + .17 * (i % 4), q = (1 - Math.Exp(-4 * age)) / 4;
            double x = Math.Cos(phi) * speed * q, y = Math.Sin(phi) * speed * q * .75 - 1.5 * age * age;
            double angle = phi + age * (i % 2 != 0 ? 8 : -6), ca = Math.Cos(angle), sa = Math.Sin(angle);
            double length = .028 + .006 * (i % 3), width = .014 + .003 * (i % 2);
            double alpha = Smooth((t - born) / .008) * Fade(t - born, .025, life);
            b.ChipCenters[i] = new Point(x, y, .055); b.ChipActive[i] = alpha > 0;
            for (int j = 0; j < 4; j++)
            {
                double a = j == 0 ? -length * .5 : j == 2 ? length * .5 : 0;
                double w = j == 1 ? -width * .5 : j == 3 ? width * .5 : 0;
                int n = i * 4 + j; b.Chips.Vertices[n] = new Point(x + ca * a - sa * w, y + sa * a + ca * w, .055);
                b.Chips.Alpha[n] = alpha;
            }
        }
        public static void Sample(Buffer b, double age, double availableAnticipation = double.NaN)
        {
            if (b == null) throw new ArgumentNullException("b");
            double mapped = Clock(age, availableAnticipation), t = double.IsNaN(mapped) ? -NativeLead : mapped;
            b.VisualAge = t; b.LightEnvelope = Light(t); b.RevealFront = Front(t);
            b.TargetAreaW = 42 * b.LightEnvelope; b.FloorAreaW = 16 * b.LightEnvelope;
            double alpha = -NativeLead <= t && t < VisibleEnd ? Smooth((t + NativeLead) / .014) * (1 - Smooth((t - .155) / .150)) : 0;
            for (int k = 0; k < CrystalCount; k++) Crystal(b, t, k, b.RevealFront, alpha);
            double coreAlpha = Fade(t, .018, .079);
            for (int i = 0; i < 9; i++) b.Core.Alpha[i] = coreAlpha;
            for (int i = 0; i < ChipCount; i++) Chip(b, t, i);
        }
    }
}
