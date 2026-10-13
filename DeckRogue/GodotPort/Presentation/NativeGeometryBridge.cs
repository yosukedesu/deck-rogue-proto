using System;
using System.Collections.Generic;
using DeckRogue.Game;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

internal static class NativeGeometryBridge
{
	private static NativeBatch Wrap(PhysicalSlashGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => -1,
			Effect = (int i) => Vector2.One,
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(WaterContinuousGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => -1,
			Effect = (int i) => Vector2.One,
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(FireContinuousGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => new Vector2((float)m.Erosion[i], 0f),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(IceCrystalGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => new Vector2((float)m.Front[i], 0f),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(LightRayGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => Vector2.One,
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(DarkDrainGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => new Vector2((float)m.Coverage[i], 0f),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(LightningDischargeGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => Vector2.One,
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(EnemyContactGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = 0,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => Vector2.One,
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(VineSlashGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = 0,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => 1f,
			Role = (int i) => -1,
			Effect = (int i) => Vector2.One,
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(PlayerContactGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Particle = (int i) => m.Particle[i],
			Effect = (int i) => Vector2.One,
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(EarthStompGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = m.Space + 1,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => new Vector2((float)m.Phase[i], m.Role[i]),
			Normal = (int i) => new Vector3((float)m.Normals[i].X, (float)m.Normals[i].Y, (float)m.Normals[i].Z),
			Anchor = (int i) => new Vector3((float)m.GroundAnchors[i].X, (float)m.GroundAnchors[i].Y, (float)m.GroundAnchors[i].Z)
		};
	}

	private static NativeBatch Wrap(WideProfileGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => -1,
			Effect = (int i) => new Vector2((float)m.Phase[i], 0f),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(ScatterProfileGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => -1,
			Effect = (int i) => new Vector2((float)m.Phase[i], 0f),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(LocalMaterialProfileGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => new Vector2((float)m.Phase[i], m.Role[i]),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(HealContinuousGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => new Vector2((float)m.Coverage[i], 0f),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch Wrap(SystemOutcomeGeometry.MeshData m)
	{
		return new NativeBatch
		{
			Count = m.Vertices.Length,
			Kind = m.Kind,
			Space = 0,
			Indices = m.Triangles,
			Position = (int i) => new Vector3((float)m.Vertices[i].X, (float)m.Vertices[i].Y, (float)m.Vertices[i].Z),
			Uv = (int i) => new Vector2((float)m.UV[i].X, (float)m.UV[i].Y),
			Alpha = (int i) => (float)m.Alpha[i],
			Gain = (int i) => (float)m.Gain[i],
			Role = (int i) => m.Role[i],
			Effect = (int i) => new Vector2((m.Phase == null) ? 0f : ((float)m.Phase[i]), 0f),
			Normal = (int i) => Vector3.Back,
			Anchor = (int i) => Vector3.Zero
		};
	}

	private static NativeBatch[] Batches<T>(T[] data, Func<T, NativeBatch> wrap)
	{
		NativeBatch[] result = new NativeBatch[data.Length];
		for (int i = 0; i < data.Length; i++)
		{
			result[i] = wrap(data[i]);
		}
		return result;
	}

	public static NativeEffect? Create(AttackRoute route, EffectTier tier)
	{
		string family = route.Family;
		string t = tier.ToString().ToLowerInvariant();
		double sampledAge = 0.0;
		bool mirror = false;
		NativeEffect result;
		switch (route.Renderer)
		{
		case "PhysicalSlashFx":
		{
			PhysicalSlashGeometry.Buffer b12 = PhysicalSlashGeometry.Create(t);
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b12.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				PhysicalSlashGeometry.Sample(b12, age, mirrorX: mirror, PhysicalSlashGeometry.Lead(t));
			};
			nativeEffect.ConfigureMirror = value => mirror = value;
			// Source PhysicalSlashFx admits this lease at Land, after preview.
			nativeEffect.Light = () => sampledAge >= 0 ? (float)b12.LightEnvelope : 0f;
			nativeEffect.Shader = "PhysicalSlashFx";
			nativeEffect.Palette = "physical-contact-palette-r1";
			nativeEffect.Texture = "vine-slash-surface-r1";
			result = nativeEffect;
			break;
		}
		case "WaterContinuousFx":
		{
			WaterContinuousGeometry.Buffer b7 = WaterContinuousGeometry.Create();
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b7.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				WaterContinuousGeometry.Sample(b7, age, mirrorX: mirror, WaterContinuousGeometry.Lead());
			};
			nativeEffect.ConfigureSource = v => mirror = SourceAttackOrientation.IncomingFromRight(v);
			nativeEffect.Light = () => (float)b7.LightEnvelope;
			nativeEffect.Shader = "WaterContinuousFx";
			nativeEffect.Palette = "water-continuous-palette-r1";
			nativeEffect.Texture = "water-flow-surface-r1";
			nativeEffect.Lookup = "water-flow-droplet-lookup-r1";
			result = nativeEffect;
			break;
		}
		case "FireContinuousFx":
		{
			FireContinuousGeometry.Buffer b13 = FireContinuousGeometry.Create((family == "fire") ? "projectile" : "localburst");
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b13.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				FireContinuousGeometry.Sample(b13, age, FireContinuousGeometry.Lead(b13));
			};
			nativeEffect.ConfigureSource = delegate(Vector3 v)
			{
				FireContinuousGeometry.ConfigureSource(b13, v.X, v.Y, v.Z);
			};
			nativeEffect.Light = () => (float)b13.LightEnvelope;
			nativeEffect.Shader = "FireContinuousFx";
			nativeEffect.Palette = "fire-continuous-palette-r1";
			nativeEffect.Texture = "fire-continuous-surface-r1";
			result = nativeEffect;
			break;
		}
		case "IceCrystalFx":
		{
			IceCrystalGeometry.Buffer b14 = IceCrystalGeometry.Create();
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b14.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				IceCrystalGeometry.Sample(b14, age, IceCrystalGeometry.Lead());
			};
			nativeEffect.Light = () => (float)b14.LightEnvelope;
			nativeEffect.Shader = "IceCrystalFx";
			nativeEffect.Palette = "ice-crystal-palette-r2";
			nativeEffect.Texture = "ice-crystal-surface-r2";
			result = nativeEffect;
			break;
		}
		case "LightRayFx":
		{
			LightRayGeometry.Buffer b3 = LightRayGeometry.Create();
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b3.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				LightRayGeometry.Sample(b3, age, LightRayGeometry.Lead());
			};
			nativeEffect.Light = () => (float)b3.LightEnvelope;
			nativeEffect.Shader = "LightRayFx";
			nativeEffect.Palette = "light-ray-palette-r2";
			nativeEffect.Texture = "light-ray-surface-r2";
			result = nativeEffect;
			break;
		}
		case "DarkDrainFx":
		{
			DarkDrainGeometry.Buffer b4 = DarkDrainGeometry.Create();
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b4.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				DarkDrainGeometry.Sample(b4, age, DarkDrainGeometry.Lead());
			};
			nativeEffect.ConfigureSource = delegate(Vector3 v)
			{
				DarkDrainGeometry.ConfigureSource(b4, v.X, v.Y, v.Z);
			};
			nativeEffect.Light = () => (float)b4.LightEnvelope;
			nativeEffect.Shader = "DarkDrainFx";
			nativeEffect.Palette = "dark-drain-palette-r1";
			nativeEffect.Texture = "dark-drain-surface-r1";
			result = nativeEffect;
			break;
		}
		case "LightningDischargeFx":
		{
			LightningDischargeGeometry.Buffer b15 = LightningDischargeGeometry.Create();
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b15.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				LightningDischargeGeometry.SampleReceipt(b15, age, LightningDischargeGeometry.Lead());
			};
			nativeEffect.Light = () => (float)b15.LightEnvelope;
			nativeEffect.VisualAge = () => (float)b15.VisualAge;
			nativeEffect.Shader = "LightningDischargeFx";
			nativeEffect.Palette = "lightning-discharge-palette-r2";
			nativeEffect.Texture = "lightning-discharge-surface-r2";
			result = nativeEffect;
			break;
		}
		case "EnemyContactFx":
		{
			string f3 = family.Replace("enemy_", "");
			EnemyContactGeometry.Buffer b8 = EnemyContactGeometry.Create(f3);
			EnemyContactGeometry.Point origin = new EnemyContactGeometry.Point(0.0, 0.0, 0.0);
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b8.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				EnemyContactGeometry.Sample(b8, age);
				if (f3 == "beam")
				{
					EnemyContactGeometry.FitBeam(b8, origin);
				}
			};
			nativeEffect.ConfigureSource = delegate(Vector3 v)
			{
				origin = new EnemyContactGeometry.Point(v.X, v.Y, v.Z);
			};
			nativeEffect.Light = () => (float)b8.LightEnvelope;
			nativeEffect.Shader = "PhysicalContactFx";
			nativeEffect.Palette = ((f3 == "beam") ? "neutral-beam-palette-r1" : "physical-contact-palette-r1");
			nativeEffect.Texture = ((f3 == "fang") ? "enemy-fang-row-r1" : ((f3 == "claw") ? "vine-slash-surface-r1" : "enemy-blunt-contact-r1"));
			result = nativeEffect;
			break;
		}
		case "VineSlashFx":
		{
			VineSlashGeometry.Buffer b2 = VineSlashGeometry.Create();
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = new NativeBatch[4]
			{
				Wrap(b2.Primary),
				Wrap(b2.Secondary),
				Wrap(b2.Core),
				Wrap(b2.Particles)
			};
			nativeEffect.Sample = delegate(double age)
			{
				VineSlashGeometry.Sample(b2, age, mirrorX: mirror, 0.066666667);
			};
			nativeEffect.ConfigureMirror = value => mirror = value;
			nativeEffect.Light = () => (float)b2.LightEnvelope;
			nativeEffect.Shader = "VineSlashFx";
			nativeEffect.Palette = "vine-slash-palette-r1";
			nativeEffect.Texture = "vine-slash-surface-r1";
			result = nativeEffect;
			break;
		}
		case "PlayerContactFx":
		{
			string f5 = ((family == "physical_meteor") ? "meteor" : "pierce");
			PlayerContactGeometry.Buffer b10 = PlayerContactGeometry.Create(f5);
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b10.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				PlayerContactGeometry.Sample(b10, age);
			};
			nativeEffect.Light = () => (float)PlayerContactGeometry.Light(f5, sampledAge);
			nativeEffect.Shader = "PlayerContactFx";
			nativeEffect.Palette = "physical-contact-palette-r1";
			nativeEffect.Texture = "enemy-blunt-contact-r1";
			result = nativeEffect;
			break;
		}
		case "WideProfileFx":
		{
			WideProfileGeometry.Profile f4 = ((!(family == "wide_water")) ? WideProfileGeometry.Profile.Fire : WideProfileGeometry.Profile.Water);
			WideProfileGeometry.Buffer b9 = WideProfileGeometry.Create(f4);
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b9.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				WideProfileGeometry.SampleReceipt(b9, age, WideProfileGeometry.Lead());
			};
			nativeEffect.Light = () => (float)b9.LightEnvelope;
			nativeEffect.Shader = "WideProfileFx";
			nativeEffect.Palette = "wide-profile-palette-r2";
			nativeEffect.Family = (int)f4;
			nativeEffect.Texture = ((f4 == WideProfileGeometry.Profile.Water) ? "water-flow-surface-r1" : "fire-continuous-surface-r1");
			nativeEffect.Lookup = "water-flow-droplet-lookup-r1";
			result = nativeEffect;
			break;
		}
		case "ScatterProfileFx":
		{
			ScatterProfileGeometry.Profile f6 = AuthoredEffectPlan.Scatter(family);
			ScatterProfileGeometry.Buffer b11 = ScatterProfileGeometry.Create(f6);
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b11.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				ScatterProfileGeometry.SampleReceipt(b11, age);
			};
			nativeEffect.Light = () => (float)b11.LightEnvelope;
			nativeEffect.Shader = "ScatterProfileFx";
			nativeEffect.Palette = "scatter-profile-palette-r2";
			nativeEffect.Family = (int)f6;
			result = nativeEffect;
			break;
		}
		case "LocalMaterialProfileFx":
		{
			LocalMaterialProfileGeometry.Profile f2 = AuthoredEffectPlan.Local(family);
			LocalMaterialProfileGeometry.Buffer b6 = LocalMaterialProfileGeometry.Create(f2);
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b6.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				LocalMaterialProfileGeometry.SampleReceipt(b6, age);
			};
			nativeEffect.Light = () => (float)b6.LightEnvelope;
			nativeEffect.Shader = "LocalMaterialProfileFx";
			nativeEffect.Palette = "local-material-palette-r1";
			nativeEffect.Family = (int)f2;
			nativeEffect.Texture = f2 switch
			{
				LocalMaterialProfileGeometry.Profile.Water => "water-flow-surface-r1", 
				LocalMaterialProfileGeometry.Profile.Light => "light-ray-surface-r2", 
				LocalMaterialProfileGeometry.Profile.Dark => "dark-drain-surface-r1", 
				LocalMaterialProfileGeometry.Profile.Botanical => "", 
				_ => "enemy-blunt-contact-r1", 
			};
			nativeEffect.Lookup = "water-flow-droplet-lookup-r1";
			result = nativeEffect;
			break;
		}
		case "HealContinuousFx":
		{
			HealContinuousGeometry.Buffer b16 = HealContinuousGeometry.Create();
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = Batches(b16.Batches, Wrap);
			nativeEffect.Sample = delegate(double age)
			{
				HealContinuousGeometry.Sample(b16, age);
			};
			nativeEffect.Light = () => (float)b16.LightEnvelope;
			nativeEffect.Shader = "HealContinuousFx";
			nativeEffect.Palette = "heal-uplift-palette-r1";
			nativeEffect.Texture = "heal-uplift-surface-r1";
			result = nativeEffect;
			break;
		}
		case "SystemOutcomeFx":
		{
			SystemOutcomeGeometry.Buffer b5 = SystemOutcomeGeometry.Create(family);
			Vector3 incoming = new Vector3(float.NaN, 0f, 0f);
			NativeEffect nativeEffect2 = new NativeEffect();
			nativeEffect2.Batches = Batches(b5.Batches, Wrap);
			nativeEffect2.Sample = delegate(double age)
			{
				SystemOutcomeGeometry.Sample(b5, age);
				SystemOutcomeGeometry.FitIncoming(b5, incoming.X, incoming.Y);
			};
			nativeEffect2.ConfigureSource = delegate(Vector3 v)
			{
				incoming = v;
			};
			nativeEffect2.Light = () => (float)SystemOutcomeGeometry.Light(family, sampledAge);
			nativeEffect2.Shader = "SystemOutcomeFx";
			NativeEffect nativeEffect3 = nativeEffect2;
			string text = family;
			bool flag = ((text == "reflect" || text == "burn") ? true : false);
			nativeEffect3.Palette = (flag ? "system-impact-palettes-r1" : "system-outcome-palette-r1");
			nativeEffect2.TargetPeak = 0.1f;
			nativeEffect2.FloorPeak = 0f;
			result = nativeEffect2;
			break;
		}
		case "BoundedContactFx":
		{
			int motes = 3;
			BoundedContactGeometry.Buffer b17 = new BoundedContactGeometry.Buffer();
			BoundedContactGeometry.Profile p = BoundedContactGeometry.Find(family);
			if (p == null)
			{
				return null;
			}
			NativeBatch batch = new NativeBatch
			{
				Count = b17.X.Length,
				Indices = b17.Triangles,
				Position = (int i) => new Vector3((float)b17.X[i], (float)b17.Y[i], 0f),
				Uv = (int i) => new Vector2((float)b17.U[i], (float)b17.V[i]),
				Alpha = (int i) => (float)b17.Alpha[i],
				Gain = (int i) => 1f,
				Effect = (int i) => new Vector2((float)b17.Soft[i], 0f)
			};
			NativeEffect nativeEffect = new NativeEffect();
			nativeEffect.Batches = new NativeBatch[1] { batch };
			nativeEffect.Sample = delegate(double age)
			{
				BoundedContactGeometry.Sample(b17, p, age, motes);
			};
			nativeEffect.ConfigureParticleGain = delegate(float gain)
			{
				motes = BoundedContactGeometry.MoteCount(gain);
			};
			nativeEffect.Shader = "BoundedContactFx";
			nativeEffect.Palette = "bounded-contact-palette-r1";
			nativeEffect.Family = (int)p.Material;
			result = nativeEffect;
			break;
		}
		case "EarthStompFx":
		{
			int f = ((!(family == "earth_fissure")) ? 1 : 0);
			EarthStompGeometry.Buffer b = EarthStompGeometry.Create(f);
			List<NativeBatch> batches = new List<NativeBatch>();
			batches.Add(new NativeBatch
			{
				Kind = -1,
				Space = 1,
				Count = 4,
				Indices = new int[6] { 0, 1, 2, 0, 2, 3 },
				Position = delegate(int i)
				{
					bool flag2 = ((i == 0 || i == 3) ? true : false);
					return new Vector3(flag2 ? (-1.25f) : 1.25f, 0f, (i < 2) ? (-0.85f) : 0.85f);
				},
				Uv = delegate(int i)
				{
					bool flag2 = ((i == 0 || i == 3) ? true : false);
					return new Vector2((!flag2) ? 1 : 0, (i >= 2) ? 1 : 0);
				},
				Alpha = (int i) => 1f,
				Gain = (int i) => 1f,
				Normal = (int i) => Vector3.Up
			});
			EarthStompGeometry.MeshData[] batches2 = b.Batches;
			foreach (EarthStompGeometry.MeshData mesh in batches2)
			{
				if (mesh != null)
				{
					batches.Add(Wrap(mesh));
				}
			}
			result = new NativeEffect
			{
				Batches = batches.ToArray(),
				Sample = delegate(double age)
				{
					EarthStompGeometry.SampleReceipt(b, age, EarthStompGeometry.Lead(f));
				},
				Light = () => (float)b.LightEnvelope,
				VisualAge = () => (float)b.VisualAge,
				Front = () => (float)b.Front,
				Coverage = () => (float)b.GroundAlpha,
				Shader = "EarthStompSurfaceR2",
				Palette = "earth-stomp-palette-r2",
				Texture = "earth-ground-grain-r2",
				Family = f
			};
			break;
		}
		default:
			return null;
		}
		switch (result.Shader)
		{
		case "PhysicalSlashFx":
			result.TargetPeak = 0.35f * tier switch
			{
				EffectTier.Large => 1.25f, 
				EffectTier.Small => 7f / 12f, 
				_ => 1f, 
			};
			result.FloorPeak = 0.12f * tier switch
			{
				EffectTier.Large => 1.2f, 
				EffectTier.Small => 0.4f, 
				_ => 1f, 
			};
			break;
		case "FireContinuousFx":
			if (family != "fire")
			{
				result.TargetPeak *= 1.1272727f;
				result.FloorPeak *= 1.1f;
			}
			break;
		case "IceCrystalFx":
			result.TargetPeak *= 42f / 55f;
			result.FloorPeak *= 0.8f;
			break;
		case "LightRayFx":
			result.TargetPeak *= 1.0909091f;
			break;
		case "DarkDrainFx":
			result.TargetPeak *= 38f / 55f;
			result.FloorPeak *= 0.65f;
			break;
		case "LightningDischargeFx":
			result.TargetPeak *= 42f / 55f;
			result.FloorPeak *= 0.7f;
			break;
		case "WideProfileFx":
			result.TargetPeak *= ((result.Family == 0) ? 38f : 45f) / 55f;
			result.FloorPeak *= ((result.Family == 0) ? 14f : 16f) / 20f;
			break;
		case "ScatterProfileFx":
			result.TargetPeak *= 32f / 55f;
			result.FloorPeak *= 0.4f;
			break;
		case "LocalMaterialProfileFx":
			result.TargetPeak *= 0.54545456f;
			result.FloorPeak *= 0.5f;
			break;
		case "HealContinuousFx":
			result.TargetPeak *= 28f / 55f;
			result.FloorPeak *= 0.5f;
			break;
		case "EarthStompSurfaceR2":
			result.TargetPeak *= ((result.Family == 0) ? 35f : 30f) / 55f;
			result.FloorPeak *= ((result.Family == 0) ? 23f : 20f) / 20f;
			break;
		case "BoundedContactFx":
			result.TargetPeak = (result.FloorPeak = 0f);
			break;
		}
		result.RouteFamily = family;
		Action<double> sample = result.Sample;
		result.Sample = delegate(double age)
		{
			sampledAge = age;
			sample(age);
		};
		return result;
	}
}
