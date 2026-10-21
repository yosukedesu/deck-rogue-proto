using System;
using System.Collections.Generic;

namespace DeckRogue.GodotPort.Presentation;

public static class EnemySpiritSourceContract
{
	public readonly record struct Registration(int Width, int Height, int Top, int Bottom);

	public sealed record Pair(Registration Desktop, Registration? Phone);

	private static readonly Dictionary<string, Pair> Known = new Dictionary<string, Pair>(StringComparer.Ordinal)
	{
		["enemy_wide_power"] = new Pair(new Registration(64, 64, 13, 5), null),
		["enemy_set_wary"] = new Pair(new Registration(64, 64, 17, 7), null),
		["enemy_set_breaker"] = new Pair(new Registration(64, 64, 5, 3), null),
		["enemy_brute"] = new Pair(new Registration(128, 128, 26, 2), new Registration(96, 96, 18, 3)),
		["enemy_turtle"] = new Pair(new Registration(128, 128, 26, 2), new Registration(96, 96, 13, 7)),
		["enemy_hexer"] = new Pair(new Registration(64, 64, 10, 6), null),
		["enemy_wolf"] = new Pair(new Registration(64, 64, 4, 7), null),
		["enemy_moss"] = new Pair(new Registration(64, 64, 4, 2), null),
		["enemy_joker"] = new Pair(new Registration(64, 64, 8, 4), null),
		["enemy_drummer"] = new Pair(new Registration(64, 64, 8, 5), null),
		["enemy_warden"] = new Pair(new Registration(128, 128, 22, 2), new Registration(96, 96, 7, 2)),
		["enemy_thorn_squirrel"] = new Pair(new Registration(64, 64, 12, 4), null),
		["enemy_thief"] = new Pair(new Registration(64, 64, 9, 4), null),
		["enemy_bomber"] = new Pair(new Registration(64, 64, 12, 8), null),
		["enemy_moss_healer"] = new Pair(new Registration(64, 64, 3, 2), null),
		["enemy_axe_ogre"] = new Pair(new Registration(64, 64, 6, 2), null),
		["enemy_shell_guard"] = new Pair(new Registration(64, 64, 5, 4), null),
		["enemy_apprentice_colossus"] = new Pair(new Registration(64, 64, 9, 1), null),
		["enemy_mimic_imp"] = new Pair(new Registration(64, 64, 5, 2), null),
		["enemy_cultist"] = new Pair(new Registration(64, 64, 12, 6), null),
		["enemy_slug"] = new Pair(new Registration(64, 64, 22, 7), null),
		["enemy_whetstone_colossus"] = new Pair(new Registration(64, 64, 5, 1), null),
		["enemy_mimic_jester"] = new Pair(new Registration(64, 64, 8, 5), null),
		["enemy_cinder_imp"] = new Pair(new Registration(64, 64, 6, 5), null),
		["enemy_rock_beetle"] = new Pair(new Registration(64, 64, 14, 6), null),
		["enemy_big_slime"] = new Pair(new Registration(64, 64, 8, 4), null),
		["enemy_moss_slime"] = new Pair(new Registration(64, 64, 6, 4), null),
		["enemy_moss_spawner"] = new Pair(new Registration(64, 64, 8, 2), null),
		["enemy_elite_sergeant"] = new Pair(new Registration(80, 80, 3, 4), null),
		["enemy_elite_sentry"] = new Pair(new Registration(80, 80, 5, 2), null),
		["enemy_elite_iron_egg"] = new Pair(new Registration(80, 80, 2, 4), null),
		["enemy_elite_slaver"] = new Pair(new Registration(80, 80, 9, 4), null),
		["enemy_elite_stab_book"] = new Pair(new Registration(80, 80, 2, 2), null),
		["enemy_elite_giant_face"] = new Pair(new Registration(80, 80, 0, 2), null),
		["enemy_elite_gold_raven"] = new Pair(new Registration(80, 80, 9, 6), null),
		["enemy_elite_mirror_djinn"] = new Pair(new Registration(80, 80, 11, 1), null),
		["enemy_elite_doom_chanter"] = new Pair(new Registration(80, 80, 4, 4), null),
		["enemy_elite_devourer"] = new Pair(new Registration(80, 80, 8, 8), null),
		["enemy_elite_owl"] = new Pair(new Registration(80, 80, 9, 1), null),
		["enemy_elite_deathless"] = new Pair(new Registration(80, 80, 2, 1), null),
		["enemy_shield_squire"] = new Pair(new Registration(64, 64, 15, 11), null),
		["enemy_crossbow_archer"] = new Pair(new Registration(64, 64, 11, 2), null),
		["enemy_bond_wolf"] = new Pair(new Registration(64, 64, 2, 6), null),
		["enemy_mud_lump"] = new Pair(new Registration(64, 64, 5, 4), null),
		["enemy_elite_husk_3"] = new Pair(new Registration(80, 80, 3, 3), null),
		["enemy_elite_husk_2"] = new Pair(new Registration(80, 80, 7, 2), null),
		["enemy_elite_husk_1"] = new Pair(new Registration(80, 80, 3, 3), null),
		["enemy_brood_toad"] = new Pair(new Registration(64, 64, 8, 2), null),
		["enemy_broodling"] = new Pair(new Registration(64, 64, 12, 7), null),
		["enemy_brood_raptor"] = new Pair(new Registration(64, 64, 10, 4), null),
		["enemy_raptor_egg"] = new Pair(new Registration(64, 64, 12, 3), null),
		["enemy_raptor_chick"] = new Pair(new Registration(64, 64, 11, 5), null),
		["enemy_mourn_beast"] = new Pair(new Registration(64, 64, 5, 3), null),
		["enemy_sludge_berserker"] = new Pair(new Registration(64, 64, 7, 0), null),
		["enemy_mudling"] = new Pair(new Registration(64, 64, 10, 8), null),
		["enemy_gaping_maw"] = new Pair(new Registration(64, 64, 4, 3), null),
		["enemy_cog_construct"] = new Pair(new Registration(64, 64, 3, 3), null),
		["enemy_spore_cap"] = new Pair(new Registration(64, 64, 8, 4), null),
		["enemy_strangler_serpent"] = new Pair(new Registration(64, 64, 7, 4), null),
		["enemy_snap_fruit"] = new Pair(new Registration(64, 64, 6, 4), null),
		["enemy_vine_walker"] = new Pair(new Registration(64, 64, 7, 2), null),
		["enemy_sludge_spider"] = new Pair(new Registration(64, 64, 9, 5), null),
		["enemy_axe_automaton"] = new Pair(new Registration(64, 64, 3, 3), null),
		["enemy_axe_automaton_reboot"] = new Pair(new Registration(64, 64, 7, 1), null),
		["enemy_thunder_globe"] = new Pair(new Registration(64, 64, 7, 1), null),
		["enemy_frog_knight"] = new Pair(new Registration(64, 64, 2, 3), null),
		["enemy_biting_scroll"] = new Pair(new Registration(64, 64, 7, 4), null),
		["enemy_lost_soul"] = new Pair(new Registration(64, 64, 7, 4), null),
		["enemy_forgotten_soul"] = new Pair(new Registration(64, 64, 17, 5), null),
		["enemy_devoted_sculptor"] = new Pair(new Registration(64, 64, 5, 3), null),
		["enemy_chomper"] = new Pair(new Registration(64, 64, 5, 3), null),
		["enemy_scald_gnat"] = new Pair(new Registration(64, 64, 8, 4), null),
		["enemy_kin_priest"] = new Pair(new Registration(96, 96, 8, 5), null),
		["enemy_kin_follower"] = new Pair(new Registration(96, 96, 9, 6), null),
		["enemy_crab_crusher"] = new Pair(new Registration(96, 96, 26, 4), null),
		["enemy_crab_cannon"] = new Pair(new Registration(96, 96, 34, 3), null),
		["enemy_chimera_1"] = new Pair(new Registration(128, 128, 23, 2), new Registration(96, 96, 10, 7)),
		["enemy_chimera_2"] = new Pair(new Registration(128, 128, 17, 2), new Registration(96, 96, 13, 2)),
		["enemy_chimera_3"] = new Pair(new Registration(128, 128, 15, 2), new Registration(96, 96, 11, 2)),
		["enemy_burrow_worm"] = new Pair(new Registration(64, 64, 16, 4), null),
		["enemy_bowl_bug"] = new Pair(new Registration(64, 64, 22, 4), null),
		["enemy_nemesis_wraith"] = new Pair(new Registration(64, 64, 5, 3), null),
		["enemy_haze_stag"] = new Pair(new Registration(128, 128, 22, 2), new Registration(96, 96, 4, 3)),
		["enemy_winch_warden"] = new Pair(new Registration(96, 96, 3, 3), null),
		["enemy_seal_orb"] = new Pair(new Registration(48, 48, 2, 2), null),
		["enemy_ember_furnace"] = new Pair(new Registration(128, 128, 22, 2), new Registration(96, 96, 8, 3))
	};

	public static bool Accepts(string id, EnemySpiritProfile.Variant desktop, EnemySpiritProfile.Variant? phone)
	{
		if (!Known.TryGetValue(id, out Pair known))
		{
			return false;
		}
		if (Equal(known.Desktop, desktop))
		{
			Registration? phone2 = known.Phone;
			if (phone2.HasValue)
			{
				Registration p = phone2.GetValueOrDefault();
				if ((object)phone != null)
				{
					return Equal(p, phone);
				}
				return false;
			}
			return (object)phone == null;
		}
		return false;
		static bool Equal(Registration r, EnemySpiritProfile.Variant v)
		{
			if (r.Width == v.Width && r.Height == v.Height && r.Top == v.Top)
			{
				return r.Bottom == v.Bottom;
			}
			return false;
		}
	}
}
