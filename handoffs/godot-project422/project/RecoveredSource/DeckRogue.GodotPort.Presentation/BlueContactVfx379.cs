using System;
using Godot;
namespace DeckRogue.GodotPort.Presentation;

//379: two owned receiving materials only. Original catalog, beat clock and all other contact families are unchanged.
internal static class BlueContactVfx379
{
    private static Texture2D? iceTexture;
    internal static Texture2D IceDensityTexture()
    {
        if (iceTexture == null)
        {
            using var image = Image.LoadFromFile("res://assets/Art/fx/hd2d-ice-wall-density-v1.png");
            iceTexture = ImageTexture.CreateFromImage(image);
        }
        return iceTexture;
    }

    private static bool Tuple(AttackRoute? route, bool selected)
    {
        if (route == null || route.SourceKind != "card" || route.FixedSmall
            || route.Renderer != (selected ? "BlueOwnedContact379Fx" : "BoundedContactFx"))
        {
            return false;
        }
        return (route.Key == "card:blue_ripple_blade" && route.DefinitionId == "blue_ripple_blade"
                && route.Family == "water_slash_contact" && route.Element == "water")
            || (route.Key == "card:blue_glacial_maul" && route.DefinitionId == "blue_glacial_maul"
                && route.Family == "ice_blunt_contact" && route.Element == "ice");
    }
    internal static bool OriginalRoute(AttackRoute? route) => Tuple(route, selected: false);
    internal static bool SelectedRoute(AttackRoute? route) => Tuple(route, selected: true);
    internal static AttackRoute Select(AttackRoute route, bool owned)
    {
        return owned && OriginalRoute(route)
            ? route with { Renderer = "BlueOwnedContact379Fx", PortStatus = "379 exact owned blue receipt-bound water cut / crystalline blunt contact" }
            : route;
    }

    private sealed class MeshBuffer
    {
        internal readonly Vector3[] Position = new Vector3[256];
        internal readonly Vector2[] Uv = new Vector2[256];
        internal readonly Vector2[] Role = new Vector2[256];
        internal readonly float[] Alpha = new float[256];
        internal readonly int[] Indices = new int[384];
        private int count;
        internal MeshBuffer()
        {
            for (int i = 0; i < 64; i++)
            {
                int v = i * 4, t = i * 6;
                Indices[t] = v; Indices[t + 1] = v + 2; Indices[t + 2] = v + 1;
                Indices[t + 3] = v; Indices[t + 4] = v + 3; Indices[t + 5] = v + 2;
            }
        }
        internal void Reset() { count = 0; Array.Clear(Alpha); }
        internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv0, Vector2 uv1, float alpha, float role)
        {
            int i = count++ * 4;
            Position[i] = a; Position[i + 1] = b; Position[i + 2] = c; Position[i + 3] = d;
            Uv[i] = uv0; Uv[i + 1] = new Vector2(uv1.X, uv0.Y); Uv[i + 2] = uv1; Uv[i + 3] = new Vector2(uv0.X, uv1.Y);
            for (int j = 0; j < 4; j++) { Alpha[i + j] = alpha; Role[i + j] = new Vector2(role, 0); }
        }
        internal void IceCell(Vector2[] polygon, Vector2 displacement, float alpha, float tone)
        {
            // Cell triangulation shares one continuous body-space raster mapping.
            // Internal triangle boundaries have identical UV/tone and are not drawn as spokes.
            for (int v = 1; v < polygon.Length - 1; v++)
            {
                int i = count++ * 4;
                var points = new[] { polygon[0], polygon[v], polygon[v + 1], polygon[0] };
                for (int j = 0; j < 4; j++)
                {
                    Vector2 point = points[j];
                    Position[i + j] = new Vector3(point.X + displacement.X, point.Y + displacement.Y, .045f);
                    Uv[i + j] = new Vector2(.5f + point.X / .60f, .5f + point.Y / .44f);
                    Alpha[i + j] = alpha;
                    Role[i + j] = new Vector2(tone, 0);
                }
            }
        }

        internal NativeBatch Batch(string texture) => new NativeBatch
        {
            Kind = 91, Texture = texture, Count = Position.Length, Indices = Indices,
            Position = i => Position[i], Uv = i => Uv[i], Alpha = i => Alpha[i],
            Gain = _ => 1f, Effect = i => Role[i]
        };
    }
    private static float Ease(float t) { t = Math.Clamp(t, 0f, 1f); return t * t * (3f - 2f * t); }

    internal static NativeEffect Create(AttackRoute route)
    {
        bool ripple = route.DefinitionId == "blue_ripple_blade";
        var mesh = new MeshBuffer();
        bool committed = false;
        float age = -1f;
        var effect = new NativeEffect
        {
            Shader = "BoundedContactFx", Palette = "bounded-contact-palette-r1", Family = ripple ? 1 : 2,
            Batches = new[] { mesh.Batch(ripple ? "water-flow-surface-r1" : "hd2d-ice-wall-density-v1") },
            TargetPeak = 0f, FloorPeak = 0f, Light = () => 0f, FloorLight = () => 0f,
            VisualAge = () => age, ConfigureBlueCommittedContact = value => committed = value
        };
        effect.Sample = time =>
        {
            age = (float)time;
            mesh.Reset();
            if (!committed || !float.IsFinite(age) || age < 0f || age >= .18f)
            {
                return;
            }
            if (ripple) { SampleRipple(mesh, age); }
            else { SampleMaul(mesh, age); }
        };
        return effect;
    }

    private static void SampleRipple(MeshBuffer mesh, float age)
    {
        float release = Ease((age - .035f) / .135f);
        float opacity = 1f - Ease((age - .075f) / .10f);
        Vector3 Curve(float u)
        {
            float x = -.40f + .80f * u;
            return new Vector3(x, .22f * MathF.Sin(u * MathF.PI) + .20f * x - .06f, .035f);
        }
        // The flowing transparent raster is bent along a narrow cutting sheet, not stamped as a static sprite.
        for (int i = 0; i < 28; i++)
        {
            float u = i / 28f, v = (i + 1) / 28f;
            float tip = MathF.Sin(MathF.PI * (u + v) * .5f);
            float thickness = (.045f + .075f * tip) * (1f - release * .50f);
            float local = 1f - Ease((release - u * .55f) / .45f);
            Vector3 a = Curve(u), b = Curve(v);
            Vector3 normal = new Vector3(-(b.Y - a.Y), b.X - a.X, 0).Normalized();
            mesh.Quad(a - normal * thickness, b - normal * thickness, b + normal * thickness, a + normal * thickness,
                uv0: new Vector2(u, 0), uv1: new Vector2(v, 1), alpha: opacity * local, role: 0);
        }
        // A few separated liquid fragments follow the same cut and fall; never a second impact.
        for (int i = 0; i < 7; i++)
        {
            float u = .22f + .105f * i;
            Vector3 p = Curve(u) + new Vector3(age * (.26f + .06f * i), .02f + age * (.45f - .07f * i) - age * age * 3f, .005f);
            float size = (.008f + .003f * (i % 3)) * (1f - release * .65f);
            mesh.Quad(p + new Vector3(-size, -size, 0), p + new Vector3(size, -size, 0), p + new Vector3(size, size, 0), p + new Vector3(-size, size, 0),
                uv0: Vector2.Zero, uv1: Vector2.One, alpha: opacity * .70f, role: 1);
        }
    }

    private static void SampleMaul(MeshBuffer mesh, float age)
    {
        float fracture = Ease((age - .018f) / .10f);
        float opacity = 1f - Ease((age - .070f) / .105f);
        // Asymmetric connected ice mass: two offset crack junctions, clipped perimeter,
        // unequal cells. No central hub or equal angular wedges.
        Vector2 a = new(-.225f, -.035f), b = new(-.188f, .095f), c = new(-.100f, .138f);
        Vector2 d = new(.012f, .112f), e = new(.102f, .133f), f = new(.205f, .065f);
        Vector2 g = new(.236f, -.010f), h = new(.167f, -.094f), k = new(.042f, -.126f);
        Vector2 l = new(-.080f, -.112f), m = new(-.191f, -.082f);
        Vector2 p = new(-.075f, .030f), q = new(.055f, -.020f), n = new(-.046f, -.054f);
        float crack = .0015f + .0065f * fracture;
        float fall = age * age * 1.1f;
        mesh.IceCell(new[] { a, b, c, p, n, m }, new Vector2(-crack * 1.1f, -fall), opacity * .88f, 3.08f);
        mesh.IceCell(new[] { c, d, e, q, p }, new Vector2(-crack * .25f, crack * .50f - fall), opacity * .86f, 3.14f);
        mesh.IceCell(new[] { p, q, k, l, n }, new Vector2(crack * .25f, -crack * .9f - fall), opacity * .88f, 3.04f);
        mesh.IceCell(new[] { e, f, g, q }, new Vector2(crack * 1.4f, -crack * .30f - fall), opacity * .84f, 3.18f);
        mesh.IceCell(new[] { q, g, h, k }, new Vector2(crack * 1.65f, -crack * 1.4f - fall), opacity * .86f, 3.10f);
        mesh.IceCell(new[] { m, n, l }, new Vector2(-crack * 1.5f, -crack * 1.8f - fall), opacity * .80f, 3.03f);
        // Uneven, short edge chips peel down and toward the compressed right edge.
        mesh.IceCell(new[] { new Vector2(.211f,.061f), new Vector2(.238f,.046f), new Vector2(.252f,.012f), new Vector2(.226f,.025f) },
            new Vector2(.006f + fracture * .020f, -fall * 1.2f), opacity * .75f, 3.16f);
        mesh.IceCell(new[] { new Vector2(.131f,-.108f), new Vector2(.164f,-.101f), new Vector2(.157f,-.128f), new Vector2(.139f,-.135f) },
            new Vector2(fracture * .012f, -.006f - fall * 1.5f), opacity * .78f, 3.12f);
        mesh.IceCell(new[] { new Vector2(-.183f,-.092f), new Vector2(-.156f,-.102f), new Vector2(-.170f,-.121f) },
            new Vector2(-fracture * .009f, -.004f - fall * 1.7f), opacity * .72f, 3.06f);
    }
}
