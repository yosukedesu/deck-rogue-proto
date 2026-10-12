using System;
using UnityEngine;

namespace DeckRogue.Game
{
    // R9 body topology adapter. The frozen Geometry sampler still owns every source
    // path, strength, phase, width sample and clock. Native Unity/device QA is separate.
    internal static class LightningR9BodyUnion
    {
        internal const int SegmentCapacity = 112, PathCapacity = 16, PathCount = 15;
        internal const int MaximumSegmentsPerPath = 15, UsedSegments = 98;
        internal const int UniformArrayBytes = (5 * SegmentCapacity + PathCapacity) * 16;
        internal const int BodyVertexCount = PathCount * 4 + 9 * 8 + 2 * 4;
        internal const int BodyTriangleCount = PathCount * 2 + 9 * 6 + 2 * 2;
        internal const int AnalyticKindOffset = 16;
        internal sealed class MeshBuffer
        {
            internal readonly Vector3[] Vertices = new Vector3[BodyVertexCount];
            internal readonly Vector4[] UV0 = new Vector4[BodyVertexCount];
            internal readonly Vector4[] UV1 = new Vector4[BodyVertexCount];
            internal readonly Vector4[] UV2 = new Vector4[BodyVertexCount];
            internal readonly int[] Triangles = new int[BodyTriangleCount * 3];
            internal Bounds Bounds;
            internal bool Visible;
        }
        internal sealed class Buffer
        {
            internal readonly MeshBuffer Body = new MeshBuffer();
            // These are SOURCE coordinates; the existing certified floor projector
            // and exact-frame containment check remain the floor upload owner.
            internal readonly LightningR9Geometry.Point[] FloorVertices = new LightningR9Geometry.Point[24 * 8];
            internal readonly Vector4[] FloorUV0 = new Vector4[24 * 8];
            // Dense fixed-length uploads. No per-path 16x16 padding or texture lookup.
            // 0: projected planar A.xy/B.xy; 1: four frozen layer strengths;
            // 2: frozen width samples at t=0,.25,.75,1;
            // 3: source run-start/run-end distances, projected perpendicular px scale A/B;
            // 4: original source-pixel A.xy/B.xy (clipped precursor endpoint included).
            internal readonly Vector4[] Segment0 = new Vector4[SegmentCapacity];
            internal readonly Vector4[] Segment1 = new Vector4[SegmentCapacity];
            internal readonly Vector4[] Segment2 = new Vector4[SegmentCapacity];
            internal readonly Vector4[] Segment3 = new Vector4[SegmentCapacity];
            internal readonly Vector4[] Segment4 = new Vector4[SegmentCapacity];
            // start, count, source kind, fixed source support radius in pixels.
            internal readonly Vector4[] PathHeaders = new Vector4[PathCapacity];
            internal readonly int[] Sources = new int[PathCount];
            internal int LeaderSource;
            internal Vector3 CameraPosition, Right, Up, Forward, Contact, Correction;
            internal float Scale;
            internal bool Orthographic, Captured;
        }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static bool Finite(Vector3 p) { return Finite(p.x) && Finite(p.y) && Finite(p.z); }
        static bool Finite(Vector4 p) { return Finite(p.x) && Finite(p.y) && Finite(p.z) && Finite(p.w); }
        static InvalidOperationException Invalid() { return new InvalidOperationException("r9_body_union_invalid"); }
        static Vector4 V4(LightningR9Geometry.Quad q)
        {
            var value = new Vector4((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
            if (!Finite(value)) throw Invalid(); return value;
        }
        static Vector4 Strength(LightningR9Geometry.Quad q)
        {
            var value = V4(q);
            if (value.x < 0 || value.y < 0 || value.z < 0 || value.w < 0) throw Invalid();
            return value;
        }
        static bool UnionKind(int kind)
        {
            return kind == LightningR9Geometry.MainKind || kind == LightningR9Geometry.BranchKind ||
                kind == LightningR9Geometry.CrownKind || kind == LightningR9Geometry.TertiaryKind || kind == LightningR9Geometry.LateralKind;
        }
        static void Quad(int[] triangles, ref int at, int v)
        {
            triangles[at++] = v; triangles[at++] = v + 1; triangles[at++] = v + 2;
            triangles[at++] = v; triangles[at++] = v + 2; triangles[at++] = v + 3;
        }
        internal static Buffer Create()
        {
            var b = new Buffer(); int paths = 0, segments = 0, leaders = 0, raw = 0, floor = 0;
            for (int i = 0; i < LightningR9Geometry.Paths.Length; i++)
            {
                var p = LightningR9Geometry.Paths[i];
                if (UnionKind(p.Kind))
                {
                    if (paths >= PathCount || p.Floor || p.Count < 1 || p.Count > MaximumSegmentsPerPath) throw Invalid();
                    b.Sources[paths++] = i; segments += p.Count;
                }
                else if (p.Kind == LightningR9Geometry.LeaderKind) { b.LeaderSource = i; leaders++; }
                else if (p.Floor) floor += p.Count;
                else if (p.Kind == LightningR9Geometry.QuietGroundKind || p.Kind == LightningR9Geometry.StreamerKind) raw += p.Count;
                else throw Invalid();
            }
            if (paths != PathCount || segments != UsedSegments || leaders != 1 || raw != 9 || floor != 24 ||
                LightningR9Geometry.Paths[b.Sources[0]].Kind != LightningR9Geometry.MainKind ||
                LightningR9Geometry.Paths[b.LeaderSource].Count != MaximumSegmentsPerPath) throw Invalid();
            int at = 0;
            for (int p = 0; p < PathCount; p++) Quad(b.Body.Triangles, ref at, p * 4);
            for (int segment = 0; segment < 9; segment++)
                for (int row = 0; row < 3; row++)
                {
                    int v = PathCount * 4 + segment * 8 + row * 2;
                    b.Body.Triangles[at++] = v; b.Body.Triangles[at++] = v + 1; b.Body.Triangles[at++] = v + 3;
                    b.Body.Triangles[at++] = v; b.Body.Triangles[at++] = v + 3; b.Body.Triangles[at++] = v + 2;
                }
            for (int field = 0; field < 2; field++) Quad(b.Body.Triangles, ref at, PathCount * 4 + 9 * 8 + field * 4);
            if (at != b.Body.Triangles.Length) throw Invalid();
            return b;
        }
        // AttackScenarioAnchor captures this exact camera rotation. The body planes
        // and source-to-world basis deliberately share that one immutable snapshot.
        internal static void Capture(Buffer b, Vector3 cameraPosition, Quaternion rotation,
            Vector3 contact, Vector3 correction, float scale, bool orthographic)
        {
            if (b == null) throw Invalid(); b.Captured = false;
            if (!Finite(cameraPosition) || !Finite(contact) || !Finite(correction) || !Finite(scale) || scale <= 0 ||
                !Finite(rotation.x) || !Finite(rotation.y) || !Finite(rotation.z) || !Finite(rotation.w)) throw Invalid();
            float norm = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            if (!Finite(norm) || Mathf.Abs(norm - 1f) > .001f) throw Invalid();
            b.CameraPosition = cameraPosition; b.Right = rotation * Vector3.right; b.Up = rotation * Vector3.up;
            b.Forward = rotation * Vector3.forward; b.Contact = contact; b.Correction = correction; b.Scale = scale;
            b.Orthographic = orthographic; b.Captured = true;
        }
        static Vector3 World(Buffer b, LightningR9Geometry.Point p, double groundWeight)
        {
            var world = b.Contact + (b.Right * (float)p.X + b.Up * (float)p.Y - b.Forward * (float)p.Z) * b.Scale + b.Correction * (float)groundWeight;
            if (!Finite(world)) throw Invalid(); return world;
        }
        static LightningR9Geometry.Point Center(LightningR9Geometry.MeshData source, int v)
        {
            var a = source.Vertices[v]; var c = source.Vertices[v + 1];
            return new LightningR9Geometry.Point((a.X + c.X) * .5, (a.Y + c.Y) * .5, (a.Z + c.Z) * .5);
        }
        static Vector3 Project(Buffer b, Vector3 world, Vector3 origin, float planeDepth, out float pixelScale)
        {
            float depth = Vector3.Dot(world - b.CameraPosition, b.Forward);
            if (!Finite(depth) || depth <= .00001f || !Finite(planeDepth) || planeDepth <= .00001f) throw Invalid();
            float ratio = b.Orthographic ? 1f : planeDepth / depth;
            pixelScale = b.Scale / (float)LightningR9Geometry.SourcePixelsPerHeight * ratio;
            var projected = b.Orthographic ? world + b.Forward * (planeDepth - depth) : b.CameraPosition + (world - b.CameraPosition) * ratio;
            var delta = projected - origin;
            var planar = new Vector3(Vector3.Dot(delta, b.Right), Vector3.Dot(delta, b.Up), 0);
            if (!Finite(planar) || !Finite(pixelScale) || pixelScale <= 0) throw Invalid(); return planar;
        }
        static void Include(Vector3 p, ref Vector3 lo, ref Vector3 hi)
        {
            lo = new Vector3(Mathf.Min(lo.x, p.x), Mathf.Min(lo.y, p.y), Mathf.Min(lo.z, p.z));
            hi = new Vector3(Mathf.Max(hi.x, p.x), Mathf.Max(hi.y, p.y), Mathf.Max(hi.z, p.z));
        }
        static bool Energized(Vector4 p) { return p.x > 0 || p.y > 0 || p.z > 0 || p.w > 0; }
        static void WriteVertex(MeshBuffer m, int v, Vector3 world, Vector4 uv0, Vector4 uv1, Vector4 uv2)
        {
            if (v < 0 || v >= m.Vertices.Length || !Finite(world) || !Finite(uv0) || !Finite(uv1) || !Finite(uv2)) throw Invalid();
            m.Vertices[v] = world; m.UV0[v] = uv0; m.UV1[v] = uv1; m.UV2[v] = uv2;
        }
        internal static void Sample(Buffer b, LightningR9Geometry.Buffer geometry)
        {
            if (b == null || !b.Captured || geometry == null || !LightningR9Geometry.Finite(geometry.VisualAge)) throw Invalid();
            // Clear the entire fixed storage, including inactive slots and unused tail,
            // so pooled slots can never retain radiance from a previous cast/frame.
            Array.Clear(b.Segment0, 0, SegmentCapacity); Array.Clear(b.Segment1, 0, SegmentCapacity);
            Array.Clear(b.Segment2, 0, SegmentCapacity); Array.Clear(b.Segment3, 0, SegmentCapacity);
            Array.Clear(b.Segment4, 0, SegmentCapacity); Array.Clear(b.PathHeaders, 0, PathCapacity);
            b.Body.Visible = false; int descriptor = 0;
            var source = geometry.Batches[0];
            for (int path = 0; path < PathCount; path++)
            {
                int index = path == 0 && geometry.VisualAge < 0 ? b.LeaderSource : b.Sources[path];
                var spec = LightningR9Geometry.Paths[index]; var state = geometry.States[index];
                if (state.Batch != 0 || spec.Count > MaximumSegmentsPerPath || descriptor + spec.Count > SegmentCapacity) throw Invalid();
                // Intentional depth-model change: one camera-parallel body plane per
                // path at its FULL authored depth. Source endpoints are ray-projected
                // onto it, preserving their admission-camera screen positions.
                var origin = b.Contact - b.Forward * (float)(spec.Depth * b.Scale /
                    (LightningR9Geometry.SourcePixelsPerHeight * LightningR9Geometry.SourcePixelWorld));
                float planeDepth = Vector3.Dot(origin - b.CameraPosition, b.Forward);
                if (!Finite(origin) || !Finite(planeDepth) || planeDepth <= .00001f) throw Invalid();
                b.PathHeaders[path] = new Vector4(descriptor, spec.Count, spec.Kind, (float)spec.Radius);
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                bool visible = false;
                for (int k = 0; k < spec.Count; k++, descriptor++)
                {
                    int v = state.VertexOffset + k * 8;
                    var strength = Strength(source.UV1[v]);
                    if (!Energized(strength)) continue;
                    var originalA = source.UV2[v]; var originalB = source.UV2[v + 6];
                    double dx = originalB.X - originalA.X, dy = originalB.Y - originalA.Y;
                    double sourceLength = Math.Sqrt(dx * dx + dy * dy);
                    if (!LightningR9Geometry.Finite(sourceLength) || !LightningR9Geometry.Finite(state.Fraction[k])) throw Invalid();
                    // A precursor boundary can leave a positive fraction smaller
                    // than the uploaded float coordinates can resolve. It has no
                    // drawable area; do not retire the entire otherwise-valid cast.
                    // This exception never admits a collapsed full source segment.
                    bool tinyClippedTip = spec.Kind == LightningR9Geometry.LeaderKind &&
                        state.Fraction[k] > 0 && state.Fraction[k] < 1 && sourceLength <= .0001;
                    if (sourceLength <= 1e-8) { if (tinyClippedTip) continue; throw Invalid(); }
                    var s4 = new Vector4((float)originalA.X, (float)originalA.Y, (float)originalB.X, (float)originalB.Y);
                    if (!Finite(s4)) throw Invalid();
                    if (s4.x == s4.z && s4.y == s4.w) { if (tinyClippedTip) continue; throw Invalid(); }
                    float scaleA, scaleB;
                    var a = Project(b, World(b, Center(source, v), source.GroundWeight[v]), origin, planeDepth, out scaleA);
                    var c = Project(b, World(b, Center(source, v + 6), source.GroundWeight[v + 6]), origin, planeDepth, out scaleB);
                    float cx = c.x - a.x, cy = c.y - a.y, chordLength = Mathf.Sqrt(cx * cx + cy * cy);
                    if (!Finite(chordLength)) throw Invalid();
                    if (chordLength <= 1e-8f) { if (tinyClippedTip) continue; throw Invalid(); }
                    // The old warp shears the centerline. Project raw authored width
                    // supports PERPENDICULAR to the new chord, rather than treating a
                    // source pixel as an unchanged world-perpendicular width.
                    float nx = (float)(-dy / sourceLength), ny = (float)(-dx / sourceLength);
                    float transverse = Mathf.Abs((cx * ny - cy * nx) / chordLength);
                    if (!Finite(transverse) || transverse <= 1e-6f) throw Invalid();
                    scaleA *= transverse; scaleB *= transverse;
                    var s0 = new Vector4(a.x, a.y, c.x, c.y);
                    var s2 = new Vector4((float)source.UV0[v].Z, (float)source.UV0[v + 2].Z,
                        (float)source.UV0[v + 4].Z, (float)source.UV0[v + 6].Z);
                    var s3 = new Vector4((float)state.ToStart[k], (float)state.ToEnd[k], scaleA, scaleB);
                    if (!Finite(s0) || !Finite(s2) || !Finite(s3) || !Finite(s4) || scaleA <= 0 || scaleB <= 0 ||
                        s2.x <= 0 || s2.y <= 0 || s2.z <= 0 || s2.w <= 0 || s3.x < 0 || s3.y < 0) throw Invalid();
                    b.Segment0[descriptor] = s0; b.Segment1[descriptor] = strength; b.Segment2[descriptor] = s2;
                    b.Segment3[descriptor] = s3; b.Segment4[descriptor] = s4;
                    float radius = (float)spec.Radius * Mathf.Max(scaleA, scaleB);
                    if (!Finite(radius) || radius <= 0) throw Invalid();
                    minX = Mathf.Min(minX, Mathf.Min(a.x - radius, c.x - radius)); maxX = Mathf.Max(maxX, Mathf.Max(a.x + radius, c.x + radius));
                    minY = Mathf.Min(minY, Mathf.Min(a.y - radius, c.y - radius)); maxY = Mathf.Max(maxY, Mathf.Max(a.y + radius, c.y + radius));
                    visible = true;
                }
                if (!visible) { minX = minY = -.00001f; maxX = maxY = .00001f; }
                else
                {
                    // Expand raster support outward for float rounding; the shader
                    // still rejects at the unchanged source radius. Bounds alone
                    // would only expand culling and would not prevent edge clipping.
                    float magnitude = Mathf.Max(Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX)), Mathf.Max(Mathf.Abs(minY), Mathf.Abs(maxY)));
                    float pad = Mathf.Max(.0000001f, magnitude * .000002f + b.Scale * .000001f);
                    minX -= pad; minY -= pad; maxX += pad; maxY += pad;
                    if (!Finite(minX) || !Finite(minY) || !Finite(maxX) || !Finite(maxY)) throw Invalid();
                }
                // Round analytic caps fit inside endpoint boxes expanded by the
                // fixed source radius times the maximum projected scale.
                for (int corner = 0; corner < 4; corner++)
                {
                    float x = corner == 0 || corner == 3 ? minX : maxX;
                    float y = corner < 2 ? minY : maxY;
                    WriteVertex(b.Body, path * 4 + corner, origin + b.Right * x + b.Up * y,
                        new Vector4(x, y, 0, AnalyticKindOffset + path), new Vector4(visible ? 1 : 0, 0, 0, 0), new Vector4());
                }
                b.Body.Visible |= visible;
            }
            if (descriptor != UsedSegments) throw Invalid();
            int rawVertex = PathCount * 4;
            for (int index = 0; index < LightningR9Geometry.Paths.Length; index++)
            {
                var spec = LightningR9Geometry.Paths[index];
                if (!spec.Floor && spec.Kind != LightningR9Geometry.QuietGroundKind && spec.Kind != LightningR9Geometry.StreamerKind) continue;
                var state = geometry.States[index]; var batch = geometry.Batches[state.Batch];
                for (int k = 0; k < spec.Count; k++)
                {
                    double dx = spec.XY[k * 2 + 2] - spec.XY[k * 2], dy = spec.XY[k * 2 + 3] - spec.XY[k * 2 + 1];
                    double length = Math.Sqrt(dx * dx + dy * dy);
                    if (!LightningR9Geometry.Finite(length) || length <= 0) throw Invalid();
                    double nx = -dy / length, ny = dx / length;
                    for (int row = 0; row < 4; row++)
                    {
                        int v = state.VertexOffset + k * 8 + row * 2; var center = Center(batch, v);
                        double halfWidth = batch.UV0[v].Z * .5;
                        if (!LightningR9Geometry.Finite(halfWidth) || halfWidth <= 0) throw Invalid();
                        for (int side = 0; side < 2; side++)
                        {
                            double d = side == 0 ? -halfWidth : halfWidth;
                            var point = new LightningR9Geometry.Point(center.X + nx * d / LightningR9Geometry.SourcePixelsPerHeight,
                                center.Y - ny * d / LightningR9Geometry.SourcePixelsPerHeight, center.Z);
                            if (!LightningR9Geometry.Finite(point.X) || !LightningR9Geometry.Finite(point.Y) || !LightningR9Geometry.Finite(point.Z)) throw Invalid();
                            var profile = V4(batch.UV0[v + side]); profile.x = (float)d;
                            if (spec.Floor)
                            {
                                if (v + side >= b.FloorVertices.Length) throw Invalid();
                                b.FloorVertices[v + side] = point; b.FloorUV0[v + side] = profile;
                            }
                            else
                            {
                                var strength = Strength(batch.UV1[v + side]);
                                WriteVertex(b.Body, rawVertex++, World(b, point, batch.GroundWeight[v + side]),
                                    profile, strength, V4(batch.UV2[v + side]));
                                b.Body.Visible |= Energized(strength);
                            }
                        }
                    }
                }
            }
            if (rawVertex != PathCount * 4 + 9 * 8) throw Invalid();
            for (int field = 0; field < 8; field++)
            {
                int v = geometry.FieldsOffset + field; var strength = Strength(source.UV1[v]);
                WriteVertex(b.Body, rawVertex++, World(b, source.Vertices[v], source.GroundWeight[v]), V4(source.UV0[v]), strength, V4(source.UV2[v]));
                b.Body.Visible |= Energized(strength);
            }
            if (rawVertex != BodyVertexCount) throw Invalid();
            var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < b.Body.Vertices.Length; i++) Include(b.Body.Vertices[i], ref lo, ref hi);
            var centerBounds = (lo + hi) * .5f; var sizeBounds = hi - lo + Vector3.one * .001f;
            if (!Finite(centerBounds) || !Finite(sizeBounds)) throw Invalid();
            b.Body.Bounds = new Bounds(centerBounds, sizeBounds);
        }
        internal static void Apply(MaterialPropertyBlock properties, Buffer b)
        {
            if (properties == null || b == null || !b.Captured) throw Invalid();
            // Always the full capacity, including the first upload. Unity's vector
            // array length must never be established by a smaller live-path subset.
            properties.SetVectorArray("_R9Segment0", b.Segment0); properties.SetVectorArray("_R9Segment1", b.Segment1);
            properties.SetVectorArray("_R9Segment2", b.Segment2); properties.SetVectorArray("_R9Segment3", b.Segment3);
            properties.SetVectorArray("_R9Segment4", b.Segment4); properties.SetVectorArray("_R9Paths", b.PathHeaders);
        }
    }
}
