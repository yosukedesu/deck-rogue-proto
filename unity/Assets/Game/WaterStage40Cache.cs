// Repository-external Stage40 candidate. Pure managed C#, .NET Standard 2.0 API surface.
// This decoder does not establish Unity, IL2CPP, device performance, or visual acceptance.
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;

namespace DeckRogue.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WaterStage40Vector3
    {
        public readonly float X, Y, Z;
        public WaterStage40Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WaterStage40Particle
    {
        public readonly ushort Id, Flags;
        public readonly WaterStage40Vector3 Position, Velocity;
        public readonly float Radius, Stretch;
        internal WaterStage40Particle(ushort id, ushort flags, WaterStage40Vector3 position,
            WaterStage40Vector3 velocity, float radius, float stretch)
        { Id = id; Flags = flags; Position = position; Velocity = velocity; Radius = radius; Stretch = stretch; }
    }

    public sealed class WaterStage40Frame
    {
        public int Tick { get; private set; }
        public int SprayCount { get; private set; }
        public int FoamCount { get; private set; }
        public WaterStage40Vector3[] Positions { get; private set; }
        public WaterStage40Vector3[] Normals { get; private set; }
        public ushort[] Triangles { get; private set; }
        public WaterStage40Particle[] Particles { get; private set; }
        internal WaterStage40Frame(int tick, int spray, int foam, WaterStage40Vector3[] positions,
            WaterStage40Vector3[] normals, ushort[] triangles, WaterStage40Particle[] particles)
        { Tick = tick; SprayCount = spray; FoamCount = foam; Positions = positions; Normals = normals;
            Triangles = triangles; Particles = particles; }
    }

    // Fixed-capacity caller-owned live window. Only the first explicit count elements are valid.
    public sealed class WaterStage40FrameBuffer
    {
        public int Tick { get; internal set; }
        public int VertexCount { get; internal set; }
        public int TriangleCount { get; internal set; }
        public int ParticleCount { get; internal set; }
        public int SprayCount { get; internal set; }
        public int FoamCount { get; internal set; }
        public WaterStage40Vector3[] Positions { get; private set; }
        public WaterStage40Vector3[] Normals { get; private set; }
        public ushort[] Triangles { get; private set; }
        public WaterStage40Particle[] Particles { get; private set; }
        public long ArrayPayloadBytes { get { return 24L * Positions.Length + 2L * Triangles.Length + 36L * Particles.Length; } }
        public long EstimatedManagedBytes { get { return ArrayPayloadBytes + 5L * 64; } }
        internal WaterStage40FrameBuffer(int vertices, int triangles, int particles)
        {
            Positions = new WaterStage40Vector3[vertices]; Normals = new WaterStage40Vector3[vertices];
            Triangles = new ushort[triangles * 3]; Particles = new WaterStage40Particle[particles]; Tick = -1;
        }
    }

    // One workspace may serve multiple slots serially. It never retains a decoded frame.
    public sealed class WaterStage40DecodeWorkspace
    {
        internal readonly byte[] Raw;
        internal readonly DeflateFraming.Workspace Framing = new DeflateFraming.Workspace();
        internal int Busy;
        public int RawCapacity { get { return Raw.Length; } }
        public long EstimatedRetainedManagedBytes { get { return Raw.Length + 64L * 1024; } }
        internal WaterStage40DecodeWorkspace(int rawCapacity) { Raw = new byte[rawCapacity]; }
    }

    public sealed class WaterStage40Tail
    {
        public WaterStage40Vector3[] Velocities { get; private set; }
        // Key k is at .65 + k / 60 seconds. Base positions/topology come from frame 156.
        public WaterStage40Vector3[][] NormalKeys { get; private set; }
        public ushort[] FirstContactIndices { get; private set; }
        internal WaterStage40Tail(WaterStage40Vector3[] velocities, WaterStage40Vector3[][] normals,
            ushort[] firstContactIndices)
        { Velocities = velocities; NormalKeys = normals; FirstContactIndices = firstContactIndices; }
    }

    public sealed class WaterStage40RecordInfo
    {
        public int Index { get; private set; }
        public int Offset { get; private set; }
        public int CompressedBytes { get; private set; }
        public int RawBytes { get; private set; }
        public int VertexCount { get; private set; }
        public int TriangleCount { get; private set; }
        public int SprayCount { get; private set; }
        public int FoamCount { get; private set; }
        public int Kind { get; private set; }
        public int Tick { get; private set; }
        public int Extra0 { get; private set; }
        public int Extra1 { get; private set; }
        public uint RawCrc32 { get; private set; }
        public int PositionOverrideCount { get { return Kind == 0 ? Extra0 : 0; } }
        public int TailVelocityOverrideCount { get { return Kind == 1 ? (RawBytes - 58 * VertexCount - 2 * SprayCount - 4) / 14 : 0; } }
        public WaterStage40Vector3 BoundsMin { get; private set; }
        public WaterStage40Vector3 BoundsMax { get; private set; }
        // Exact array-element bytes. Runtime-specific object/array headers are not included.
        public long DecodedPayloadBytes { get; private set; }
        // Conservative accounting allowance, not a measured CLR or Unity allocator contract.
        public long EstimatedDecodedManagedBytes { get; private set; }
        // New decoded record + inflated raw + framing scratch + DeflateStream allowance.
        // Add shared cache and all caller-retained old/current/next/tail records separately.
        public long EstimatedDecodeWorkingBytes { get; private set; }
        internal WaterStage40RecordInfo(int index, int offset, int compressed, int raw, int vertices,
            int triangles, int spray, int foam, uint crc, int kind, int tick,
            WaterStage40Vector3 min, WaterStage40Vector3 max, int extra0, int extra1)
        {
            Index = index; Offset = offset; CompressedBytes = compressed; RawBytes = raw;
            VertexCount = vertices; TriangleCount = triangles; SprayCount = spray; FoamCount = foam;
            RawCrc32 = crc; Kind = kind; Tick = tick; BoundsMin = min; BoundsMax = max;
            Extra0 = extra0; Extra1 = extra1;
            DecodedPayloadBytes = kind == 0 ? 24L * vertices + 6L * triangles + 36L * (spray + foam)
                : 168L * vertices + 2L * spray;
            // 64 bytes per managed allocation plus an 8-byte pointer for each nested key.
            EstimatedDecodedManagedBytes = DecodedPayloadBytes + (kind == 0 ? 5L * 64 : 17L * 64 + 13L * 8);
            EstimatedDecodeWorkingBytes = EstimatedDecodedManagedBytes + raw + 64L
                + WaterStage40Cache.DeflateWorkingAllowanceBytes;
        }
    }

    public sealed class WaterStage40Cache
    {
        public const int MainFrameCount = 157, RecordCount = 158, MainHz = 240, TailNormalKeyCount = 13;
        public const int MaxFileBytes = 12 * 1024 * 1024;
        public const int AbsoluteMaxFileBytes = 16 * 1024 * 1024;
        public const int MaxVertices = 8192, MaxTriangles = 20000, MaxParticles = 4096;
        public const int MaxRawRecordBytes = 1024 * 1024, MaxOverrides = 1024, MaxParticleId = 1775;
        public const float FirstPhysicalContact = .0875f, MainEnd = .65f, VisibleEnd = .85f;
        public const float CharacterHeight = 2.3f, VelocityMax = 16f, RadiusMax = .04f;
        public const float StretchMin = 1f, StretchMax = 2.8f;
        // Framing uses fixed tables (<64 KiB); reserve a further 192 KiB for platform inflater state.
        // Re-measure native/managed inflater allocations on each target platform before adoption.
        public const int DeflateWorkingAllowanceBytes = 256 * 1024;
        public WaterStage40Vector3 PositionMin { get; private set; }
        public WaterStage40Vector3 PositionMax { get; private set; }
        public int FileBytes { get { return data.Length; } }
        public int MaximumRawRecordBytes { get; private set; }
        public int MaximumFrameVertices { get; private set; }
        public int MaximumFrameTriangles { get; private set; }
        public int MaximumFrameParticles { get; private set; }
        public long MaximumDecodedFramePayloadBytes { get; private set; }
        public long MaximumDecodeWorkingBytes { get; private set; }
        public long EstimatedSharedManagedBytes { get { return data.Length + 64L + RecordCount * (8L + 256L) + 256L; } }

        private readonly byte[] data;
        private readonly WaterStage40RecordInfo[] entries;
        private WaterStage40Cache(byte[] bytes, WaterStage40RecordInfo[] records,
            WaterStage40Vector3 min, WaterStage40Vector3 max)
        {
            data = bytes; entries = records; PositionMin = min; PositionMax = max;
            for (int i = 0; i < records.Length; i++)
            {
                MaximumRawRecordBytes = Math.Max(MaximumRawRecordBytes, records[i].RawBytes);
                MaximumDecodeWorkingBytes = Math.Max(MaximumDecodeWorkingBytes, records[i].EstimatedDecodeWorkingBytes);
                if (i < MainFrameCount)
                {
                    MaximumDecodedFramePayloadBytes = Math.Max(MaximumDecodedFramePayloadBytes, records[i].DecodedPayloadBytes);
                    MaximumFrameVertices = Math.Max(MaximumFrameVertices, records[i].VertexCount);
                    MaximumFrameTriangles = Math.Max(MaximumFrameTriangles, records[i].TriangleCount);
                    MaximumFrameParticles = Math.Max(MaximumFrameParticles, records[i].SprayCount + records[i].FoamCount);
                }
            }
        }

        // Retains this single compressed byte array; caller must treat it as immutable and release
        // its other references when appropriate. No decoded records or raw inflated data are cached.
        // Array mutation cannot expand allocation bounds; every Decode checks framing/length/CRC.
        public static WaterStage40Cache Open(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            Require(bytes.Length <= AbsoluteMaxFileBytes, "Absolute file cap exceeded.");
            Require(bytes.Length >= 12768 && bytes.Length <= MaxFileBytes, "Deployment file length/cap.");
            byte[] magic = { 87, 84, 82, 83, 52, 48, 0, 0 };
            for (int i = 0; i < magic.Length; i++) Require(bytes[i] == magic[i], "Magic.");
            Require(U16(bytes, 8) == 1 && U16(bytes, 10) == 15, "Unsupported version or flags.");
            Require(U32(bytes, 12) == 128 && U32(bytes, 16) == 80, "Header/directory size.");
            Require(U32(bytes, 20) == MainFrameCount && U32(bytes, 24) == RecordCount && U32(bytes, 28) == MainHz, "State counts or rate.");
            Require(U64(bytes, 32) == (ulong)bytes.Length && U64(bytes, 40) == 128 && U64(bytes, 48) == 12768, "Header offsets/length.");
            Require(F32(bytes, 56) == FirstPhysicalContact && F32(bytes, 60) == MainEnd
                && F32(bytes, 64) == VisibleEnd && F32(bytes, 68) == CharacterHeight, "Timing/character height.");
            WaterStage40Vector3 min = V3(bytes, 72), max = V3(bytes, 84);
            Require(Finite(min) && Finite(max) && min.X < max.X && min.Y < max.Y && min.Z < max.Z, "Position domain.");
            Require(Finite(max.X - min.X) && Finite(max.Y - min.Y) && Finite(max.Z - min.Z)
                && Finite(min.X + (max.X - min.X)) && Finite(min.Y + (max.Y - min.Y))
                && Finite(min.Z + (max.Z - min.Z)), "Position extent overflow.");
            Require(F32(bytes, 96) == VelocityMax && F32(bytes, 100) == RadiusMax
                && F32(bytes, 104) == StretchMin && F32(bytes, 108) == StretchMax, "Codec scales.");
            Require(U32(bytes, 112) == MaxVertices && U32(bytes, 116) == MaxTriangles
                && U32(bytes, 120) == MaxParticles && U32(bytes, 124) == MaxRawRecordBytes, "Unsupported advertised caps.");
            // Only fixed-size metadata is allocated after header caps are checked.
            WaterStage40RecordInfo[] records = new WaterStage40RecordInfo[RecordCount];
            ulong expectedOffset = 12768;
            for (int i = 0; i < RecordCount; i++)
            {
                int p = 128 + i * 80;
                ulong offset = U64(bytes, p);
                uint compressed = U32(bytes, p + 8), raw = U32(bytes, p + 12);
                uint nv = U32(bytes, p + 16), nt = U32(bytes, p + 20), ns = U32(bytes, p + 24), nf = U32(bytes, p + 28);
                uint crc = U32(bytes, p + 32), kind = U32(bytes, p + 36), tick = U32(bytes, p + 40);
                uint extra0 = U32(bytes, p + 72), extra1 = U32(bytes, p + 76);
                Require(offset == expectedOffset && compressed > 0 && compressed <= MaxRawRecordBytes,
                    "Record offsets/compressed cap.");
                // Subtraction avoids unsigned addition overflow on malicious offsets.
                Require(offset <= (ulong)bytes.Length && compressed <= (ulong)bytes.Length - offset, "Record outside file.");
                Require(raw > 0 && raw <= MaxRawRecordBytes && nv > 0 && nv <= MaxVertices && nt <= MaxTriangles
                    && ns <= MaxParticleId + 1 && nf <= MaxParticleId + 1 && (ulong)ns + nf <= MaxParticles, "Record count/raw caps.");
                Require(U32(bytes, p + 44) == 0, "Reserved field.");
                WaterStage40Vector3 boundsMin = V3(bytes, p + 48), boundsMax = V3(bytes, p + 60);
                Require(Finite(boundsMin) && Finite(boundsMax) && boundsMin.X <= boundsMax.X
                    && boundsMin.Y <= boundsMax.Y && boundsMin.Z <= boundsMax.Z, "Record bounds.");
                long expectedRaw;
                if (i < MainFrameCount)
                {
                    Require(kind == 0 && tick == i && nt > 0 && extra0 <= MaxOverrides && extra0 <= nv && extra1 == 0, "Main metadata.");
                    expectedRaw = 10L * nv + 6L * nt + 20L * (ns + nf) + 14L * extra0;
                }
                else
                {
                    Require(kind == 1 && tick == 156 && nt == 0 && nf == 0 && extra0 == TailNormalKeyCount
                        && ns == 1771 && extra1 == ns, "Tail metadata.");
                    Require(nv == records[156].VertexCount && ns == records[156].SprayCount, "Tail/base counts.");
                    Require(Same(min, boundsMin) && Same(max, boundsMax), "Tail bounds/domain.");
                    long baseRaw = 6L * nv + 4L * nv * TailNormalKeyCount + 2L * ns + 4;
                    long overrideBytes = raw - baseRaw;
                    Require(overrideBytes >= 0 && overrideBytes % 14 == 0
                        && overrideBytes / 14 <= Math.Min((long)nv, MaxOverrides), "Tail override raw-length cap.");
                    expectedRaw = baseRaw + overrideBytes;
                }
                Require(raw == expectedRaw, "Record raw length formula.");
                records[i] = new WaterStage40RecordInfo(i, (int)offset, (int)compressed, (int)raw, (int)nv,
                    (int)nt, (int)ns, (int)nf, crc, (int)kind, (int)tick, boundsMin, boundsMax, (int)extra0, (int)extra1);
                expectedOffset = offset + compressed; // Proven within bounded file above.
            }
            Require(expectedOffset == (ulong)bytes.Length, "Trailing file bytes.");
            return new WaterStage40Cache(bytes, records, min, max);
        }

        public WaterStage40RecordInfo GetRecordInfo(int index)
        {
            if (index < 0 || index >= RecordCount) throw new ArgumentOutOfRangeException("index");
            return entries[index];
        }

        public WaterStage40Frame DecodeFrame(int tick)
        {
            if (tick < 0 || tick >= MainFrameCount) throw new ArgumentOutOfRangeException("tick");
            WaterStage40RecordInfo e = entries[tick];
            byte[] raw = Inflate(e);
            ValidateMain(e, raw);
            WaterStage40Vector3[] positions = new WaterStage40Vector3[e.VertexCount], normals = new WaterStage40Vector3[e.VertexCount];
            ushort[] triangles = new ushort[e.TriangleCount * 3];
            WaterStage40Particle[] particles = new WaterStage40Particle[e.SprayCount + e.FoamCount];
            PopulateMain(e, raw, positions, normals, triangles, particles);
            return new WaterStage40Frame(tick, e.SprayCount, e.FoamCount, positions, normals, triangles, particles);
        }

        public WaterStage40FrameBuffer CreateFrameBuffer()
        { return new WaterStage40FrameBuffer(MaximumFrameVertices, MaximumFrameTriangles, MaximumFrameParticles); }
        public WaterStage40DecodeWorkspace CreateDecodeWorkspace()
        { return new WaterStage40DecodeWorkspace(MaximumRawRecordBytes); }

        public void DecodeFrameInto(int tick, WaterStage40FrameBuffer target, WaterStage40DecodeWorkspace workspace)
        {
            if (tick < 0 || tick >= MainFrameCount) throw new ArgumentOutOfRangeException("tick");
            if (target == null) throw new ArgumentNullException("target");
            if (workspace == null) throw new ArgumentNullException("workspace");
            WaterStage40RecordInfo e = entries[tick];
            Require(target.Positions.Length >= e.VertexCount && target.Normals.Length >= e.VertexCount
                && target.Triangles.Length >= e.TriangleCount * 3 && target.Particles.Length >= e.SprayCount + e.FoamCount
                && workspace.Raw.Length >= e.RawBytes, "Reusable capacity.");
            if (Interlocked.CompareExchange(ref workspace.Busy, 1, 0) != 0)
                throw new InvalidOperationException("The Water Stage40 decode workspace is already in use.");
            try
            {
                InflateInto(e, workspace.Raw, workspace.Framing);
                ValidateMain(e, workspace.Raw);
                // Counts are committed only after CRC and full semantic validation succeeds.
                PopulateMain(e, workspace.Raw, target.Positions, target.Normals, target.Triangles, target.Particles);
                target.Tick = tick; target.VertexCount = e.VertexCount; target.TriangleCount = e.TriangleCount;
                target.ParticleCount = e.SprayCount + e.FoamCount; target.SprayCount = e.SprayCount; target.FoamCount = e.FoamCount;
            }
            finally { Volatile.Write(ref workspace.Busy, 0); }
        }

        private void ValidateMain(WaterStage40RecordInfo e, byte[] raw)
        {
            int nv = e.VertexCount, nt = e.TriangleCount, np = e.SprayCount + e.FoamCount;
            int indicesOffset = nv * 10, particlesOffset = indicesOffset + nt * 6, overridesOffset = particlesOffset + np * 20;
            // Complete payload validation before allocating any decoded output arrays.
            for (int i = 0; i < nt * 3; i++) Require(Plane(raw, indicesOffset, nt * 3, i) < nv, "Triangle index.");
            int previous = -1;
            for (int i = 0; i < np; i++)
            {
                if (i == e.SprayCount) previous = -1;
                int id = Plane(raw, particlesOffset, np * 10, i);
                Require(id <= MaxParticleId && id > previous, "Particle ID range/order/group uniqueness.");
                Require(Plane(raw, particlesOffset, np * 10, 9 * np + i) == (i < e.SprayCount ? 1 : 2), "Particle flags/group.");
                for (int a = 4; a <= 6; a++) Require(Plane(raw, particlesOffset, np * 10, a * np + i) != 32768, "Particle signed velocity.");
                previous = id;
            }
            previous = -1;
            for (int i = 0; i < e.Extra0; i++)
            {
                int p = overridesOffset + i * 14, id = U16(raw, p);
                WaterStage40Vector3 position = V3(raw, p + 2);
                Require(id > previous && id < nv, "Override index/order/uniqueness.");
                Require(Finite(position) && InDomain(position), "Override position.");
                previous = id;
            }
        }

        private void PopulateMain(WaterStage40RecordInfo e, byte[] raw, WaterStage40Vector3[] positions,
            WaterStage40Vector3[] normals, ushort[] triangles, WaterStage40Particle[] particles)
        {
            int nv = e.VertexCount, nt = e.TriangleCount, np = e.SprayCount + e.FoamCount;
            int indicesOffset = nv * 10, particlesOffset = indicesOffset + nt * 6, overridesOffset = particlesOffset + np * 20;
            for (int i = 0; i < nv; i++)
            {
                positions[i] = Position(Plane(raw, 0, nv * 5, i), Plane(raw, 0, nv * 5, nv + i), Plane(raw, 0, nv * 5, 2 * nv + i));
                normals[i] = Oct(Plane(raw, 0, nv * 5, 3 * nv + i), Plane(raw, 0, nv * 5, 4 * nv + i));
            }
            for (int i = 0; i < nt * 3; i++) triangles[i] = Plane(raw, indicesOffset, nt * 3, i);
            for (int i = 0; i < np; i++)
            {
                int words = np * 10;
                particles[i] = new WaterStage40Particle(Plane(raw, particlesOffset, words, i),
                    Plane(raw, particlesOffset, words, 9 * np + i),
                    Position(Plane(raw, particlesOffset, words, np + i), Plane(raw, particlesOffset, words, 2 * np + i), Plane(raw, particlesOffset, words, 3 * np + i)),
                    new WaterStage40Vector3(Velocity(Plane(raw, particlesOffset, words, 4 * np + i)),
                        Velocity(Plane(raw, particlesOffset, words, 5 * np + i)), Velocity(Plane(raw, particlesOffset, words, 6 * np + i))),
                    Plane(raw, particlesOffset, words, 7 * np + i) / 65535f * RadiusMax,
                    1f + Plane(raw, particlesOffset, words, 8 * np + i) / 65535f * 1.8f);
            }
            for (int i = 0; i < e.Extra0; i++) positions[U16(raw, overridesOffset + i * 14)] = V3(raw, overridesOffset + i * 14 + 2);
        }

        public WaterStage40Tail DecodeTail()
        {
            WaterStage40RecordInfo e = entries[157];
            byte[] raw = Inflate(e);
            int nv = e.VertexCount, normalOffset = nv * 6, contactOffset = normalOffset + nv * TailNormalKeyCount * 4;
            for (int i = 0; i < nv * 3; i++) Require(U16(raw, i * 2) != 32768, "Tail signed velocity.");
            for (int i = 0; i < e.SprayCount; i++)
            {
                int contact = U16(raw, contactOffset + i * 2);
                Require(contact <= 39 || contact == 65535, "Tail contact index.");
            }
            int overrideOffset = contactOffset + e.SprayCount * 2;
            int overrideCount = (e.RawBytes - overrideOffset - 4) / 14;
            Require(U32(raw, overrideOffset) == overrideCount, "Tail override count.");
            int previous = -1;
            for (int i = 0; i < overrideCount; i++)
            {
                int p = overrideOffset + 4 + i * 14, id = U16(raw, p);
                WaterStage40Vector3 velocity = V3(raw, p + 2);
                Require(id > previous && id < nv, "Tail velocity override index/order/uniqueness.");
                Require(Finite(velocity) && Math.Abs(velocity.X) <= VelocityMax
                    && Math.Abs(velocity.Y) <= VelocityMax && Math.Abs(velocity.Z) <= VelocityMax, "Tail velocity override.");
                previous = id;
            }
            WaterStage40Vector3[] velocities = new WaterStage40Vector3[nv];
            WaterStage40Vector3[][] keys = new WaterStage40Vector3[TailNormalKeyCount][];
            ushort[] contacts = new ushort[e.SprayCount];
            for (int i = 0; i < nv; i++) velocities[i] = new WaterStage40Vector3(Velocity(U16(raw, i * 6)), Velocity(U16(raw, i * 6 + 2)), Velocity(U16(raw, i * 6 + 4)));
            for (int k = 0; k < TailNormalKeyCount; k++)
            {
                keys[k] = new WaterStage40Vector3[nv];
                for (int i = 0; i < nv; i++)
                {
                    int p = normalOffset + (k * nv + i) * 4;
                    keys[k][i] = Oct(U16(raw, p), U16(raw, p + 2));
                }
            }
            for (int i = 0; i < contacts.Length; i++) contacts[i] = U16(raw, contactOffset + i * 2);
            for (int i = 0; i < overrideCount; i++)
            {
                int p = overrideOffset + 4 + i * 14;
                velocities[U16(raw, p)] = V3(raw, p + 2);
            }
            return new WaterStage40Tail(velocities, keys, contacts);
        }

        private byte[] Inflate(WaterStage40RecordInfo e)
        {
            // Validate framing before the only count-sized raw allocation.
            DeflateFraming.Validate(data, e.Offset, e.CompressedBytes, e.RawBytes);
            byte[] raw = new byte[e.RawBytes];
            InflateCheckedStream(e, raw);
            return raw;
        }
        private void InflateInto(WaterStage40RecordInfo e, byte[] raw, DeflateFraming.Workspace workspace)
        {
            DeflateFraming.Validate(data, e.Offset, e.CompressedBytes, e.RawBytes, workspace);
            InflateCheckedStream(e, raw);
        }
        private void InflateCheckedStream(WaterStage40RecordInfo e, byte[] raw)
        {
            // DeflateStream has no public reset API. Large buffers are reusable, but these small
            // stream objects and implementation-specific inflater state are per-decode allocations.
            using (MemoryStream input = new MemoryStream(data, e.Offset, e.CompressedBytes, false))
            using (DeflateStream inflater = new DeflateStream(input, CompressionMode.Decompress))
            {
                int total = 0;
                while (total < e.RawBytes)
                {
                    int read = inflater.Read(raw, total, e.RawBytes - total);
                    Require(read > 0, "Short inflated record."); total += read;
                }
                Require(inflater.ReadByte() == -1, "Inflated record exceeds advertised length.");
            }
            Require(Crc32(raw, e.RawBytes) == e.RawCrc32, "Record CRC32.");
        }

        private WaterStage40Vector3 Position(ushort x, ushort y, ushort z)
        {
            return new WaterStage40Vector3(PositionMin.X + x / 65535f * (PositionMax.X - PositionMin.X),
                PositionMin.Y + y / 65535f * (PositionMax.Y - PositionMin.Y),
                PositionMin.Z + z / 65535f * (PositionMax.Z - PositionMin.Z));
        }
        private bool InDomain(WaterStage40Vector3 p)
        { return p.X >= PositionMin.X && p.X <= PositionMax.X && p.Y >= PositionMin.Y && p.Y <= PositionMax.Y && p.Z >= PositionMin.Z && p.Z <= PositionMax.Z; }
        private static float Velocity(ushort word) { return unchecked((short)word) / 32767f * VelocityMax; }
        private static WaterStage40Vector3 Oct(ushort u, ushort v)
        {
            float x = u / 65535f * 2f - 1f, y = v / 65535f * 2f - 1f;
            float z = 1f - (Math.Abs(x) + Math.Abs(y));
            float t = Math.Max(0f, Math.Min(1f, -z));
            x += x >= 0f ? -t : t; y += y >= 0f ? -t : t;
            float length = (float)Math.Sqrt(x * x + y * y + z * z);
            return new WaterStage40Vector3(x / length, y / length, z / length);
        }
        private static bool Same(WaterStage40Vector3 a, WaterStage40Vector3 b) { return a.X == b.X && a.Y == b.Y && a.Z == b.Z; }
        private static bool Finite(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
        private static bool Finite(WaterStage40Vector3 v) { return Finite(v.X) && Finite(v.Y) && Finite(v.Z); }
        private static ushort Plane(byte[] b, int offset, int words, int i) { return (ushort)(b[offset + i] | b[offset + words + i] << 8); }
        private static ushort U16(byte[] b, int p) { return (ushort)(b[p] | b[p + 1] << 8); }
        private static uint U32(byte[] b, int p) { return (uint)b[p] | (uint)b[p + 1] << 8 | (uint)b[p + 2] << 16 | (uint)b[p + 3] << 24; }
        private static ulong U64(byte[] b, int p) { return U32(b, p) | (ulong)U32(b, p + 4) << 32; }
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatBits { [FieldOffset(0)] public uint Bits; [FieldOffset(0)] public float Value; }
        private static float F32(byte[] b, int p) { return new FloatBits { Bits = U32(b, p) }.Value; }
        private static WaterStage40Vector3 V3(byte[] b, int p) { return new WaterStage40Vector3(F32(b, p), F32(b, p + 4), F32(b, p + 8)); }
        internal static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException("Invalid Water Stage40 cache: " + message); }
        private static uint Crc32(byte[] bytes, int count)
        {
            uint crc = 0xffffffff;
            for (int i = 0; i < count; i++)
            {
                crc ^= bytes[i];
                for (int k = 0; k < 8; k++) crc = (crc >> 1) ^ (0xedb88320u & unchecked((uint)-(int)(crc & 1)));
            }
            return ~crc;
        }
    }

    // RFC1951 framing validator. It expands no output and allocates only fixed-size tables once
    // per call. Iterations are bounded by the <=1 MiB compressed input and <=1 MiB raw byte cap.
    internal static class DeflateFraming
    {
        private static readonly int[] LengthBase = { 3,4,5,6,7,8,9,10,11,13,15,17,19,23,27,31,35,43,51,59,67,83,99,115,131,163,195,227,258 };
        private static readonly int[] LengthExtra = { 0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,4,5,5,5,5,0 };
        private static readonly int[] DistanceBase = { 1,2,3,4,5,7,9,13,17,25,33,49,65,97,129,193,257,385,513,769,1025,1537,2049,3073,4097,6145,8193,12289,16385,24577 };
        private static readonly int[] DistanceExtra = { 0,0,0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,8,9,9,10,10,11,11,12,12,13,13 };

        internal sealed class Workspace
        {
            internal readonly Huffman Lit = new Huffman(288), Dist = new Huffman(32), Code = new Huffman(19);
            internal readonly int[] Lengths = new int[320];
            internal readonly Bits Bits = new Bits();
        }
        private static readonly int[] CodeOrder = {16,17,18,0,8,7,9,6,10,5,11,4,12,3,13,2,14,1,15};
        internal static void Validate(byte[] bytes, int offset, int count, int expectedRaw)
        { Validate(bytes, offset, count, expectedRaw, new Workspace()); }
        internal static void Validate(byte[] bytes, int offset, int count, int expectedRaw, Workspace workspace)
        {
            Bits bits = workspace.Bits; bits.Reset(bytes, offset, count);
            Huffman lit = workspace.Lit, dist = workspace.Dist, code = workspace.Code;
            int[] lengths = workspace.Lengths, order = CodeOrder;
            int output = 0;
            bool final;
            do
            {
                final = bits.Take(1) != 0;
                int type = bits.Take(2);
                if (type == 0)
                {
                    bits.Align();
                    int length = bits.Take(16), complement = bits.Take(16);
                    WaterStage40Cache.Require((length ^ 65535) == complement, "DEFLATE stored length.");
                    WaterStage40Cache.Require(length <= expectedRaw - output, "DEFLATE output cap.");
                    bits.SkipBytes(length); output += length;
                }
                else if (type == 1 || type == 2)
                {
                    if (type == 1)
                    {
                        for (int i = 0; i < 288; i++) lengths[i] = i <= 143 ? 8 : i <= 255 ? 9 : i <= 279 ? 7 : 8;
                        lit.Build(lengths, 0, 288, false);
                        for (int i = 0; i < 32; i++) lengths[i] = 5;
                        dist.Build(lengths, 0, 32, false);
                    }
                    else
                    {
                        int nl = bits.Take(5) + 257, nd = bits.Take(5) + 1, nc = bits.Take(4) + 4;
                        WaterStage40Cache.Require(nl <= 286 && nd <= 32, "DEFLATE dynamic counts.");
                        Array.Clear(lengths, 0, 19);
                        for (int i = 0; i < nc; i++) lengths[order[i]] = bits.Take(3);
                        code.Build(lengths, 0, 19, false);
                        int at = 0;
                        while (at < nl + nd)
                        {
                            int symbol = code.Decode(bits), value, repeat;
                            if (symbol < 16) { lengths[at++] = symbol; continue; }
                            if (symbol == 16)
                            {
                                WaterStage40Cache.Require(at > 0, "DEFLATE repeat without predecessor.");
                                value = lengths[at - 1]; repeat = bits.Take(2) + 3;
                            }
                            else if (symbol == 17) { value = 0; repeat = bits.Take(3) + 3; }
                            else { WaterStage40Cache.Require(symbol == 18, "DEFLATE code-length symbol."); value = 0; repeat = bits.Take(7) + 11; }
                            WaterStage40Cache.Require(repeat <= nl + nd - at, "DEFLATE code-length overrun.");
                            while (repeat-- > 0) lengths[at++] = value;
                        }
                        WaterStage40Cache.Require(lengths[256] > 0, "DEFLATE missing end code.");
                        lit.Build(lengths, 0, nl, true); dist.Build(lengths, nl, nd, true);
                    }
                    while (true)
                    {
                        int symbol = lit.Decode(bits);
                        if (symbol < 256) { WaterStage40Cache.Require(output < expectedRaw, "DEFLATE output cap."); output++; }
                        else if (symbol == 256) break;
                        else
                        {
                            WaterStage40Cache.Require(symbol <= 285, "DEFLATE reserved length symbol.");
                            int i = symbol - 257, length = LengthBase[i] + bits.Take(LengthExtra[i]);
                            int ds = dist.Decode(bits);
                            WaterStage40Cache.Require(ds < 30, "DEFLATE reserved distance symbol.");
                            int distance = DistanceBase[ds] + bits.Take(DistanceExtra[ds]);
                            WaterStage40Cache.Require(distance <= output && length <= expectedRaw - output, "DEFLATE distance/output cap.");
                            output += length;
                        }
                    }
                }
                else WaterStage40Cache.Require(false, "DEFLATE reserved block type.");
            } while (!final);
            WaterStage40Cache.Require(output == expectedRaw && bits.ConsumedBytes == count, "DEFLATE output length/trailing compressed data.");
        }

        internal sealed class Bits
        {
            private byte[] data;
            private int start, count;
            private int bit;
            internal void Reset(byte[] bytes, int offset, int length) { data = bytes; start = offset; count = length; bit = 0; }
            internal int ConsumedBytes { get { return (bit + 7) / 8; } }
            internal int Take(int n)
            {
                WaterStage40Cache.Require(n >= 0 && n <= 16 && n <= count * 8 - bit, "Truncated DEFLATE.");
                int result = 0;
                for (int i = 0; i < n; i++, bit++) result |= ((data[start + bit / 8] >> (bit % 8)) & 1) << i;
                return result;
            }
            internal void Align() { bit = (bit + 7) & ~7; }
            internal void SkipBytes(int n)
            {
                WaterStage40Cache.Require((bit & 7) == 0 && n <= (count * 8 - bit) / 8, "Truncated stored DEFLATE.");
                bit += n * 8;
            }
        }

        // Canonical Huffman decoder following the RFC code order, without large lookup tables.
        internal sealed class Huffman
        {
            private readonly int[] counts = new int[16], symbols, offsets = new int[16];
            private int nonzero;
            internal Huffman(int capacity) { symbols = new int[capacity]; }
            internal void Build(int[] lengths, int start, int count, bool allowSingle)
            {
                Array.Clear(counts, 0, counts.Length);
                for (int i = 0; i < count; i++)
                {
                    int n = lengths[start + i];
                    WaterStage40Cache.Require(n >= 0 && n <= 15, "DEFLATE code length.");
                    counts[n]++;
                }
                nonzero = count - counts[0];
                // An empty distance tree is legal only if no back-reference uses it.
                if (nonzero == 0) { WaterStage40Cache.Require(allowSingle, "Empty DEFLATE code tree."); return; }
                int remaining = 1;
                for (int n = 1; n <= 15; n++) { remaining = (remaining << 1) - counts[n]; WaterStage40Cache.Require(remaining >= 0, "Oversubscribed DEFLATE tree."); }
                WaterStage40Cache.Require(remaining == 0 || (allowSingle && nonzero == 1 && counts[1] == 1), "Incomplete DEFLATE tree.");
                offsets[1] = 0;
                for (int n = 1; n < 15; n++) offsets[n + 1] = offsets[n] + counts[n];
                for (int i = 0; i < count; i++) if (lengths[start + i] != 0) symbols[offsets[lengths[start + i]]++] = i;
            }
            internal int Decode(Bits bits)
            {
                WaterStage40Cache.Require(nonzero > 0, "Empty DEFLATE distance tree used.");
                int code = 0, first = 0, index = 0;
                for (int n = 1; n <= 15; n++)
                {
                    code |= bits.Take(1);
                    int count = counts[n];
                    if (code >= first && code - first < count) return symbols[index + code - first];
                    index += count; first = (first + count) << 1; code <<= 1;
                }
                throw new InvalidDataException("Invalid Water Stage40 cache: Invalid DEFLATE Huffman code.");
            }
        }
    }
}
