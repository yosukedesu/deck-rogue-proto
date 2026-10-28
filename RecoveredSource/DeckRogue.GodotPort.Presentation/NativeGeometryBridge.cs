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
        if(EnemyContactVfxProfiles.SelectedRoute(route))return route.Family=="minimal_neutral_contact"
            ?Hd2dAttackPrototype.CreateEnemyNeutralContact():route.Family=="enemy_blunt"
            ?Hd2dAttackPrototype.CreateEnemyBlunt():Hd2dAttackPrototype.CreateEnemyPoke();
        if(PermanentContactVfxProfiles.ThornFlameRoute(route))return Hd2dAttackPrototype.CreateThornFlameContact();
        if(PermanentContactVfxProfiles.SelectedRoute(route))return route.Family=="fire_localburst"
            ?Hd2dAttackPrototype.CreateInstalledFireContact():route.Family=="dark_drain"
            ?Hd2dAttackPrototype.CreateInstalledDarkContact(route):route.Family=="plant_point_contact"
            ?Hd2dAttackPrototype.CreateInstalledPlantContact():route.Family=="lightning_discharge"
            ?Hd2dAttackPrototype.CreateInstalledLightningContact():Hd2dAttackPrototype.CreatePermanentPulse();
        if(StatusContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateBurnTickContact();
        if(NecroContactVfxProfiles.CorpseKissRoute(route))return Hd2dAttackPrototype.CreateCorpseKissContact();
        if(NecroContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateNecroDark(route);
        if(ReactionContactVfxProfiles.CurseRoute(route))return Hd2dAttackPrototype.CreateCurseReturnContact();
        if(ReactionContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateReactionDark(route);
        if(LightningContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateLightningDischarge();
        if(route.Renderer=="Hd2dSupportFx" && route.Family=="ordinary_guard_gain")return Hd2dAttackPrototype.CreateOrdinaryGuard();
        if(SupportVfxProfiles.IceWallRoute(route))return Hd2dAttackPrototype.CreateIceWall();
        if(route.Renderer=="Hd2dSupportFx" && route.Family=="ice_guard_gain")return Hd2dAttackPrototype.CreateIceGuard();
        if(SupportVfxProfiles.MassHealRoute(route))return Hd2dAttackPrototype.CreateMassHeal();
        if(route.Renderer=="Hd2dSupportFx" && route.Family=="prayer_heal")return Hd2dAttackPrototype.CreatePrayerHeal();
        if (Hd2dAttackPrototype.SupportsRoute(route)) return Hd2dAttackPrototype.Create(route);
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
			result = new NativeEffect
			{
				Batches = Batches(b12.Batches, Wrap),
				Sample = (double age) =>
				{
					PhysicalSlashGeometry.Sample(b12, age, mirror, PhysicalSlashGeometry.Lead(t));
				},
				ConfigureMirror = (bool value) =>
				{
					mirror = value;
				},
				Light = () => (!(sampledAge >= 0.0)) ? 0f : ((float)b12.LightEnvelope),
				Shader = "PhysicalSlashFx",
				Palette = "physical-contact-palette-r1",
				Texture = "vine-slash-surface-r1"
			};
			break;
		}
		case "WaterContinuousFx":
		{
			WaterContinuousGeometry.Buffer b7 = WaterContinuousGeometry.Create();
			result = new NativeEffect
			{
				Batches = Batches(b7.Batches, Wrap),
				Sample = (double age) =>
				{
					WaterContinuousGeometry.Sample(b7, age, mirror, WaterContinuousGeometry.Lead());
				},
				ConfigureSource = (Vector3 v) =>
				{
					mirror = SourceAttackOrientation.IncomingFromRight(v);
				},
				Light = () => (float)b7.LightEnvelope,
				Shader = "WaterContinuousFx",
				Palette = "water-continuous-palette-r1",
				Texture = "water-flow-surface-r1",
				Lookup = "water-flow-droplet-lookup-r1"
			};
			break;
		}
		case "FireContinuousFx":
		{
			FireContinuousGeometry.Buffer b17 = FireContinuousGeometry.Create((family == "fire") ? "projectile" : "localburst");
			result = new NativeEffect
			{
				Batches = Batches(b17.Batches, Wrap),
				Sample = (double age) =>
				{
					FireContinuousGeometry.Sample(b17, age, FireContinuousGeometry.Lead(b17));
				},
				ConfigureSource = (Vector3 v) =>
				{
					FireContinuousGeometry.ConfigureSource(b17, v.X, v.Y, v.Z);
				},
				Light = () => (float)b17.LightEnvelope,
				Shader = "FireContinuousFx",
				Palette = "fire-continuous-palette-r1",
				Texture = "fire-continuous-surface-r1"
			};
			break;
		}
		case "IceCrystalFx":
		{
			IceCrystalGeometry.Buffer b14 = IceCrystalGeometry.Create();
			result = new NativeEffect
			{
				Batches = Batches(b14.Batches, Wrap),
				Sample = (double age) =>
				{
					IceCrystalGeometry.Sample(b14, age, IceCrystalGeometry.Lead());
				},
				Light = () => (float)b14.LightEnvelope,
				Shader = "IceCrystalFx",
				Palette = "ice-crystal-palette-r2",
				Texture = "ice-crystal-surface-r2"
			};
			break;
		}
		case "LightRayFx":
		{
			LightRayGeometry.Buffer b24 = LightRayGeometry.Create();
			result = new NativeEffect
			{
				Batches = Batches(b24.Batches, Wrap),
				Sample = (double age) =>
				{
					LightRayGeometry.Sample(b24, age, LightRayGeometry.Lead());
				},
				Light = () => (float)b24.LightEnvelope,
				Shader = "LightRayFx",
				Palette = "light-ray-palette-r2",
				Texture = "light-ray-surface-r2"
			};
			break;
		}
		case "DarkDrainFx":
		{
			DarkDrainGeometry.Buffer b20 = DarkDrainGeometry.Create();
			result = new NativeEffect
			{
				Batches = Batches(b20.Batches, Wrap),
				Sample = (double age) =>
				{
					DarkDrainGeometry.Sample(b20, age, DarkDrainGeometry.Lead());
				},
				ConfigureSource = (Vector3 v) =>
				{
					DarkDrainGeometry.ConfigureSource(b20, v.X, v.Y, v.Z);
				},
				Light = () => (float)b20.LightEnvelope,
				Shader = "DarkDrainFx",
				Palette = "dark-drain-palette-r1",
				Texture = "dark-drain-surface-r1"
			};
			break;
		}
		case "LightningDischargeFx":
		{
			LightningDischargeGeometry.Buffer b15 = LightningDischargeGeometry.Create();
			result = new NativeEffect
			{
				Batches = Batches(b15.Batches, Wrap),
				Sample = (double age) =>
				{
					LightningDischargeGeometry.SampleReceipt(b15, age, LightningDischargeGeometry.Lead());
				},
				Light = () => (float)b15.LightEnvelope,
				VisualAge = () => (float)b15.VisualAge,
				Shader = "LightningDischargeFx",
				Palette = "lightning-discharge-palette-r2",
				Texture = "lightning-discharge-surface-r2"
			};
			break;
		}
		case "EnemyContactFx":
		{
			string f10 = family.Replace("enemy_", "");
			EnemyContactGeometry.Buffer b25 = EnemyContactGeometry.Create(f10);
			EnemyContactGeometry.Point origin = new EnemyContactGeometry.Point(0.0, 0.0, 0.0);
			result = new NativeEffect
			{
				Batches = Batches(b25.Batches, Wrap),
				Sample = (double age) =>
				{
					EnemyContactGeometry.Sample(b25, age);
					if (f10 == "beam")
					{
						EnemyContactGeometry.FitBeam(b25, origin);
					}
				},
				ConfigureSource = (Vector3 v) =>
				{
					origin = new EnemyContactGeometry.Point(v.X, v.Y, v.Z);
				},
				Light = () => (float)b25.LightEnvelope,
				Shader = "PhysicalContactFx",
				Palette = ((f10 == "beam") ? "neutral-beam-palette-r1" : "physical-contact-palette-r1"),
				Texture = ((f10 == "fang") ? "enemy-fang-row-r1" : ((f10 == "claw") ? "vine-slash-surface-r1" : "enemy-blunt-contact-r1"))
			};
			break;
		}
		case "VineSlashFx":
		{
			VineSlashGeometry.Buffer b21 = VineSlashGeometry.Create();
			NativeEffect nativeEffect2 = new NativeEffect();
			nativeEffect2.Batches = new NativeBatch[4]
			{
				Wrap(b21.Primary),
				Wrap(b21.Secondary),
				Wrap(b21.Core),
				Wrap(b21.Particles)
			};
			nativeEffect2.Sample = (double age) =>
			{
				VineSlashGeometry.Sample(b21, age, mirror, 0.066666667);
			};
			nativeEffect2.ConfigureMirror = (bool value) =>
			{
				mirror = value;
			};
			nativeEffect2.Light = () => (float)b21.LightEnvelope;
			nativeEffect2.Shader = "VineSlashFx";
			nativeEffect2.Palette = "vine-slash-palette-r1";
			nativeEffect2.Texture = "vine-slash-surface-r1";
			result = nativeEffect2;
			break;
		}
		case "PlayerContactFx":
		{
			string f8 = ((family == "physical_meteor") ? "meteor" : "pierce");
			PlayerContactGeometry.Buffer b19 = PlayerContactGeometry.Create(f8);
			result = new NativeEffect
			{
				Batches = Batches(b19.Batches, Wrap),
				Sample = (double age) =>
				{
					PlayerContactGeometry.Sample(b19, age);
				},
				Light = () => (float)PlayerContactGeometry.Light(f8, sampledAge),
				Shader = "PlayerContactFx",
				Palette = "physical-contact-palette-r1",
				Texture = "enemy-blunt-contact-r1"
			};
			break;
		}
		case "WideProfileFx":
		{
			WideProfileGeometry.Profile f7 = ((!(family == "wide_water")) ? WideProfileGeometry.Profile.Fire : WideProfileGeometry.Profile.Water);
			WideProfileGeometry.Buffer b16 = WideProfileGeometry.Create(f7);
			result = new NativeEffect
			{
				Batches = Batches(b16.Batches, Wrap),
				Sample = (double age) =>
				{
					WideProfileGeometry.SampleReceipt(b16, age, WideProfileGeometry.Lead());
				},
				Light = () => (float)b16.LightEnvelope,
				Shader = "WideProfileFx",
				Palette = "wide-profile-palette-r2",
				Family = (int)f7,
				Texture = ((f7 == WideProfileGeometry.Profile.Water) ? "water-flow-surface-r1" : "fire-continuous-surface-r1"),
				Lookup = "water-flow-droplet-lookup-r1"
			};
			break;
		}
		case "ScatterProfileFx":
		{
			ScatterProfileGeometry.Profile f6 = AuthoredEffectPlan.Scatter(family);
			ScatterProfileGeometry.Buffer b11 = ScatterProfileGeometry.Create(f6);
			result = new NativeEffect
			{
				Batches = Batches(b11.Batches, Wrap),
				Sample = (double age) =>
				{
					ScatterProfileGeometry.SampleReceipt(b11, age);
				},
				Light = () => (float)b11.LightEnvelope,
				Shader = "ScatterProfileFx",
				Palette = "scatter-profile-palette-r2",
				Family = (int)f6
			};
			break;
		}
		case "LocalMaterialProfileFx":
		{
			LocalMaterialProfileGeometry.Profile f9 = AuthoredEffectPlan.Local(family);
			LocalMaterialProfileGeometry.Buffer b23 = LocalMaterialProfileGeometry.Create(f9);
			NativeEffect nativeEffect4 = new NativeEffect();
			nativeEffect4.Batches = Batches(b23.Batches, Wrap);
			nativeEffect4.Sample = (double age) =>
			{
				LocalMaterialProfileGeometry.SampleReceipt(b23, age);
			};
			nativeEffect4.Light = () => (float)b23.LightEnvelope;
			nativeEffect4.Shader = "LocalMaterialProfileFx";
			nativeEffect4.Palette = "local-material-palette-r1";
			nativeEffect4.Family = (int)f9;
			NativeEffect nativeEffect5 = nativeEffect4;
			nativeEffect5.Texture = f9 switch
			{
				LocalMaterialProfileGeometry.Profile.Water => "water-flow-surface-r1", 
				LocalMaterialProfileGeometry.Profile.Light => "light-ray-surface-r2", 
				LocalMaterialProfileGeometry.Profile.Dark => "dark-drain-surface-r1", 
				LocalMaterialProfileGeometry.Profile.Botanical => "", 
				_ => "enemy-blunt-contact-r1", 
			};
			nativeEffect4.Lookup = "water-flow-droplet-lookup-r1";
			result = nativeEffect4;
			break;
		}
		case "HealContinuousFx":
		{
			HealContinuousGeometry.Buffer b18 = HealContinuousGeometry.Create();
			result = new NativeEffect
			{
				Batches = Batches(b18.Batches, Wrap),
				Sample = (double age) =>
				{
					HealContinuousGeometry.Sample(b18, age);
				},
				Light = () => (float)b18.LightEnvelope,
				Shader = "HealContinuousFx",
				Palette = "heal-uplift-palette-r1",
				Texture = "heal-uplift-surface-r1"
			};
			break;
		}
		case "SystemOutcomeFx":
		{
			SystemOutcomeGeometry.Buffer b5 = SystemOutcomeGeometry.Create(family);
			Vector3 incoming = new Vector3(float.NaN, 0f, 0f);
			NativeEffect nativeEffect = new NativeEffect
			{
				Batches = Batches(b5.Batches, Wrap),
				Sample = (double age) =>
				{
					SystemOutcomeGeometry.Sample(b5, age);
					SystemOutcomeGeometry.FitIncoming(b5, incoming.X, incoming.Y);
				},
				ConfigureSource = (Vector3 v) =>
				{
					incoming = v;
				},
				Light = () => (float)SystemOutcomeGeometry.Light(family, sampledAge),
				Shader = "SystemOutcomeFx"
			};
			string text = family;
			nativeEffect.Palette = ((text == "reflect" || text == "burn") ? "system-impact-palettes-r1" : "system-outcome-palette-r1");
			nativeEffect.TargetPeak = 0.1f;
			nativeEffect.FloorPeak = 0f;
			result = nativeEffect;
			break;
		}
		case "BoundedContactFx":
		{
			int motes = 3;
			BoundedContactGeometry.Buffer b22 = new BoundedContactGeometry.Buffer();
			BoundedContactGeometry.Profile p = BoundedContactGeometry.Find(family);
			if (p == null)
			{
				return null;
			}
			NativeBatch batch = new NativeBatch
			{
				Count = b22.X.Length,
				Indices = b22.Triangles,
				Position = (int i) => new Vector3((float)b22.X[i], (float)b22.Y[i], 0f),
				Uv = (int i) => new Vector2((float)b22.U[i], (float)b22.V[i]),
				Alpha = (int i) => (float)b22.Alpha[i],
				Gain = (int i) => 1f,
				Effect = (int i) => new Vector2((float)b22.Soft[i], 0f)
			};
			NativeEffect nativeEffect3 = new NativeEffect();
			nativeEffect3.Batches = new NativeBatch[1] { batch };
			nativeEffect3.Sample = (double age) =>
			{
				BoundedContactGeometry.Sample(b22, p, age, motes);
			};
			nativeEffect3.ConfigureParticleGain = (float gain) =>
			{
				motes = BoundedContactGeometry.MoteCount(gain);
			};
			nativeEffect3.Shader = "BoundedContactFx";
			nativeEffect3.Palette = "bounded-contact-palette-r1";
			nativeEffect3.Family = (int)p.Material;
			result = nativeEffect3;
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
				Position = (int i) => new Vector3((i == 0 || i == 3) ? (-1.25f) : 1.25f, 0f, (i < 2) ? (-0.85f) : 0.85f),
				Uv = (int i) => new Vector2(((i != 0 && i != 3) || 1 == 0) ? 1 : 0, (i >= 2) ? 1 : 0),
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
				Sample = (double age) =>
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
		{
			NativeEffect nativeEffect5 = result;
			nativeEffect5.TargetPeak = 0.35f * tier switch
			{
				EffectTier.Large => 1.25f, 
				EffectTier.Small => 7f / 12f, 
				_ => 1f, 
			};
			nativeEffect5 = result;
			nativeEffect5.FloorPeak = 0.12f * tier switch
			{
				EffectTier.Large => 1.2f, 
				EffectTier.Small => 0.4f, 
				_ => 1f, 
			};
			break;
		}
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
			result.TargetPeak *= 558f / 1023f;
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
		result.Sample = (double age) =>
		{
			sampledAge = age;
			sample(age);
		};
		return result;
	}
}
