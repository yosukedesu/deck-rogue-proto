using System;

namespace DeckRogue.Game
{
    // Frozen Earth/Stomp R2 numeric reconstruction. NOT a ground eligibility/capture implementation.
    // Manifest: 1ddadae7dc2df23754bab312abb8768da5b16b46156ba8e76ff7dce415a95c1f.
    // Local units are captured Hfx in actual ground R/N/F. The renderer composes world coordinates.
    internal static class EarthStompGeometry
    {
        public const int Earth = 0, Stomp = 1;
        public const int GroundSpace = 0, GroundBillboardSpace = 1;
        public const int RockKind = 0, DustKind = 1;
        public const int PrimaryRole = 1, EdgeRole = 2;
        public const int EarthRockVertices = 96, EarthRockTriangles = 32;
        public const int StompRockVertices = 48, StompRockTriangles = 16;
        public const int StompDustVertices = 32, StompDustTriangles = 16;
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
            public readonly int Kind, Space;
            // Rock vertices: ground R/N/F. Dust vertices: camera right/up/toward LOCAL OFFSETS.
            // For dust use GroundAnchors (R/N/F) separately; never add the two local spaces.
            public readonly Point[] Vertices, Normals, GroundAnchors;
            public readonly Uv[] UV;
            public readonly double[] Alpha, Gain, Phase;
            public readonly int[] Role, Triangles;
            internal MeshData(int vertices, int triangles, int kind, int space)
            {
                Kind = kind; Space = space;
                Vertices = new Point[vertices]; Normals = new Point[vertices];
                GroundAnchors = new Point[vertices]; UV = new Uv[vertices];
                Alpha = new double[vertices]; Gain = new double[vertices]; Phase = new double[vertices];
                Role = new int[vertices]; Triangles = new int[triangles * 3];
                for (int i = 0; i < vertices; i++) { Gain[i] = kind == RockKind ? .22 : .65; Role[i] = kind == RockKind ? PrimaryRole : EdgeRole; }
            }
        }
        public sealed class Buffer
        {
            public readonly int Family;
            public readonly MeshData Rocks, Dust;
            public readonly MeshData[] Batches;
            public readonly Point[] BirthOffsets, ParticleCenters;
            public readonly bool[] ParticleActive;
            public Point GravityUp;
            public double CapturedHfx { get; private set; }
            public double VisualAge, GroundAlpha, Front, LightEnvelope;
            // Blender reference powers only; neither is a Unity intensity.
            public double TargetAreaW, FloorAreaW;
            internal Buffer(int family)
            {
                Family = family;
                Rocks = new MeshData(family == Earth ? 96 : 48, family == Earth ? 32 : 16, RockKind, GroundSpace);
                Dust = family == Stomp ? new MeshData(32, 16, DustKind, GroundBillboardSpace) : null;
                Batches = family == Earth ? new[] { Rocks } : new[] { Rocks, Dust };
                BirthOffsets = new Point[family == Earth ? 8 : 12]; ParticleCenters = new Point[BirthOffsets.Length];
                ParticleActive = new bool[BirthOffsets.Length]; GravityUp = new Point(0, 1, 0); CapturedHfx = 1;
                for (int i = 0; i < Rocks.Triangles.Length; i++) Rocks.Triangles[i] = i;
                if (Dust != null) for (int i = 0; i < 8; i++)
                {
                    int v = 4 * i, f = 6 * i;
                    Dust.Triangles[f] = v; Dust.Triangles[f + 1] = v + 1; Dust.Triangles[f + 2] = v + 2;
                    Dust.Triangles[f + 3] = v; Dust.Triangles[f + 4] = v + 2; Dust.Triangles[f + 5] = v + 3;
                    Dust.UV[v] = new Uv(0, 0); Dust.UV[v + 1] = new Uv(1, 0); Dust.UV[v + 2] = new Uv(1, 1); Dust.UV[v + 3] = new Uv(0, 1);
                }
            }
            // Copies metadata only. Caller MUST prove host domain/birth support first. This is no certificate.
            // Defaults in Create are reference-fixture data, never permission to play without a host capture.
            // Gravity is not normalized here: preserve the caller's captured world-up components exactly.
            public void SetCapture(Point[] offsets, Point gravityUp, double hfx)
            {
                if (offsets == null || offsets.Length != BirthOffsets.Length) throw new ArgumentException("One full XYZ offset per declared birth is required.", "offsets");
                if (!Finite(gravityUp) || !Finite(hfx) || hfx <= 0) throw new ArgumentException("Finite gravity and positive captured Hfx required.");
                for (int i = 0; i < offsets.Length; i++) if (!Finite(offsets[i])) throw new ArgumentException("Birth offsets must be finite.", "offsets");
                Array.Copy(offsets, BirthOffsets, offsets.Length); GravityUp = gravityUp; CapturedHfx = hfx;
            }
        }
        static readonly Point[] Births = { new Point(.27,0,.06), new Point(.57,0,.01), new Point(.46,0,.25), new Point(.72,0,-.18), new Point(-.28,0,.04), new Point(-.55,0,-.06), new Point(-.42,0,.26), new Point(-.09,0,-.44) };
        static readonly Point[] TetraBase = { new Point(-.9,0,-.42), new Point(.85,0,-.34), new Point(.05,0,.78), new Point(.12,1.12,.06) };
        static readonly int[] TetraIndices = { 0,1,2,0,3,1,1,3,2,2,3,0 };
        public static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        static bool Finite(Point p) { return Finite(p.X) && Finite(p.Y) && Finite(p.Z); }
        static void CheckFamily(int family) { if (family != Earth && family != Stomp) throw new ArgumentOutOfRangeException("family"); }
        public static double Clamp(double x) { return Math.Max(0, Math.Min(1, x)); }
        public static double Smooth(double x) { double q = Clamp(x); return q * q * (3 - 2 * q); }
        public static double Envelope(double t, double birth, double life, double rise = .010)
        { return birth <= t && t <= birth + life ? Smooth((t - birth) / rise) * (1 - Smooth((t - birth - life * .55) / (life * .45))) : 0; }
        public static double Lead(int family) { CheckFamily(family); return family == Earth ? .140 : 0; }
        public static double End(int family) { CheckFamily(family); return family == Earth ? .420 : .267; }
        public static double End(Buffer b) { if (b == null) throw new ArgumentNullException("b"); return End(b.Family); }
        // Exact native_age return contract: null when negative time has no available anticipation.
        public static double? NativeAge(double age, double availableA, double nativeP = .140)
        {
            if (!Finite(age) || !Finite(availableA) || !Finite(nativeP) || nativeP < 0) throw new ArgumentException("Finite clock inputs and nonnegative native P required.");
            if (age >= 0) return age;
            double a = Math.Min(nativeP, Math.Max(0, availableA));
            if (a <= 0) return null;
            if (age < -a) return -nativeP;
            return age - (nativeP - a) * (age / a) * (age / a);
        }
        public static Buffer Create(int family) { CheckFamily(family); return new Buffer(family); }
        public static Point BirthTangent(int family, int index)
        {
            CheckFamily(family);
            if (index < 0 || index >= (family == Earth ? 8 : 12)) throw new ArgumentOutOfRangeException("index");
            if (family == Earth) return Births[index];
            double angle = index < 4 ? .35 + index * .80 : .20 + (index - 4) * .38;
            double r = index < 4 ? .16 : .14;
            return new Point(r * Math.Cos(angle), 0, r * Math.Sin(angle));
        }
        static Point Grounded(Buffer b, Point p, int index, double a, double g)
        {
            Point o = b.BirthOffsets[index], u = b.GravityUp;
            return new Point(p.X + o.X - g * a * a * u.X, p.Y + o.Y - g * a * a * u.Y, p.Z + o.Z - g * a * a * u.Z);
        }
        static Point Cross(Point a, Point b) { return new Point(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X); }
        static Point Sub(Point a, Point b) { return new Point(a.X-b.X,a.Y-b.Y,a.Z-b.Z); }
        static void Tetra(Buffer b, int particle, Point c, double size, double rotation, double alpha)
        {
            MeshData m = b.Rocks; int v = particle * 12; double ca = Math.Cos(rotation), sa = Math.Sin(rotation);
            for (int j = 0; j < 12; j++)
            {
                Point p = TetraBase[TetraIndices[j]];
                m.Vertices[v+j] = new Point(c.X + size*(p.X*ca-p.Z*sa), c.Y+size*p.Y, c.Z+size*(p.X*sa+p.Z*ca));
                m.Alpha[v+j] = alpha;
            }
            for (int j = 0; j < 12; j += 3)
            {
                Point n = Cross(Sub(m.Vertices[v+j+1],m.Vertices[v+j]),Sub(m.Vertices[v+j+2],m.Vertices[v+j]));
                double length = Math.Sqrt(n.X*n.X+n.Y*n.Y+n.Z*n.Z);
                n = new Point(n.X/length,n.Y/length,n.Z/length);
                m.Normals[v+j] = n; m.Normals[v+j+1] = n; m.Normals[v+j+2] = n;
            }
            b.ParticleCenters[particle] = c; b.ParticleActive[particle] = alpha > 0;
        }
        static void DustQuad(Buffer b, int index, Point anchor, double width, double height, double angle, double alpha)
        {
            MeshData m = b.Dust; int v = 4*index; double ca = Math.Cos(angle), sa = Math.Sin(angle);
            for (int j = 0; j < 4; j++)
            {
                double x = j == 0 || j == 3 ? -width : width, y = j < 2 ? -height : height;
                m.Vertices[v+j] = new Point(ca*x-sa*y,sa*x+ca*y,0);
                m.Normals[v+j] = new Point(0,0,1); m.GroundAnchors[v+j] = anchor;
                m.Alpha[v+j] = alpha; m.Phase[v+j] = index*.37;
            }
            b.ParticleCenters[index+4] = anchor; b.ParticleActive[index+4] = alpha > 0;
        }
        // Direct frozen motion.sample semantics. No terrain mesh is generated or scaled.
        public static void Sample(Buffer b, double t)
        {
            if (b == null) throw new ArgumentNullException("b");
            if (!Finite(t)) throw new ArgumentOutOfRangeException("t");
            b.VisualAge = t;
            if (b.Family == Earth)
            {
                b.GroundAlpha = Smooth((t+.140)/.040)*(1-Smooth((t-.230)/.190));
                b.Front = t < 0 ? .115+.12*Smooth((t+.140)/.140) : .235+.75*Smooth(t/.060);
                for (int i = 0; i < 8; i++)
                {
                    double x = Births[i].X, z = Births[i].Z, birth = .012+.015*i, a = Math.Max(0,t-birth);
                    double angle = Math.Atan2(z,x), drift = .30+.04*(i%3);
                    Point c = Grounded(b,new Point(x+Math.Cos(angle)*drift*a,1.12*a,z+Math.Sin(angle)*drift*a),i,a,4);
                    Tetra(b,i,c,.027+.010*(i%3),i*.73+a*(i%2 != 0 ? 2.3 : -2.8),Envelope(t,birth,.230,.012));
                }
                b.LightEnvelope = -.140 <= t && t <= .210 ? (t < 0 ? .025*Smooth((t+.140)/.140) : 1-Smooth((t-.026)/.184)) : 0;
            }
            else
            {
                b.GroundAlpha = Envelope(t,0,.180,.008); b.Front = 0;
                for (int i = 0; i < 4; i++)
                {
                    double theta = .35+i*.80,birth = .016+.004*i,a = Math.Max(0,t-birth),r = .16+(.65+.05*i)*a;
                    Point c = Grounded(b,new Point(r*Math.Cos(theta),.96*a,r*Math.Sin(theta)),i,a,4);
                    Tetra(b,i,c,.020+.004*(i%2),i+a*4,Envelope(t,birth,.204,.010));
                }
                for (int i = 0; i < 8; i++)
                {
                    double theta = .20+i*.38,birth = .008+.003*(i%4),a = Math.Max(0,t-birth),r = .14+(.85+.10*(i%3))*a;
                    Point c = Grounded(b,new Point(r*Math.Cos(theta),.018+.68*a,r*Math.Sin(theta)),i+4,a,2.4);
                    DustQuad(b,i,c,.090+.16*a,.043+.13*a,.23*Math.Sin(i),.60*Envelope(t,birth,.250,.018));
                }
                b.LightEnvelope = 0 <= t && t <= .145 ? 1-Smooth((t-.018)/.127) : 0;
            }
            b.TargetAreaW = b.LightEnvelope*(b.Family == Earth ? 35 : 30);
            b.FloorAreaW = b.LightEnvelope*(b.Family == Earth ? 23 : 20);
        }
        // Host receipt clock wrapper. False means absent anticipation; clear visible channels.
        public static bool SampleReceipt(Buffer b, double age, double availableA)
        {
            if (b == null) throw new ArgumentNullException("b");
            double? native = NativeAge(age,availableA,Lead(b.Family));
            if (native.HasValue) { Sample(b,native.Value); return true; }
            b.VisualAge = age; b.GroundAlpha = b.Front = b.LightEnvelope = b.TargetAreaW = b.FloorAreaW = 0;
            for (int j = 0; j < b.Batches.Length; j++) Array.Clear(b.Batches[j].Alpha,0,b.Batches[j].Alpha.Length);
            Array.Clear(b.ParticleActive,0,b.ParticleActive.Length); return false;
        }
    }
}
