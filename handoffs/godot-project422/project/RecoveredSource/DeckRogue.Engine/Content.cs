using System;
using System.Collections.Generic;
using System.IO;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Engine;

public static class Content
{
	public static readonly IReadOnlyDictionary<string, string> DEPARTURE_MASTER_LINES = new Dictionary<string, string>
	{
		["green"] = "「大樹の里の工房の出かい。坑に降りる前に、要る物だけ揃えていきな」",
		["blue"] = "「潮の里の工房の出かい。潮目が変わる前に、要る物だけ揃えていきな」",
		["red"] = "「燼の里の工房の出かい。燃やす物が足りなきゃ、ここで揃えていきな」",
		["white"] = "「光耀の里の工房の出かい。灯りの替えも、要る物もここで揃えていきな」",
		["black"] = "「宵闇の里の工房の出かい。暗がりに降りる前に、要る物だけ揃えていきな」"
	};

	public const string DEPARTURE_TITLE = "出立の店";

	public const string DEPARTURE_LEAD = "坑口の行商の店。坑に降りる前に、支度を整えに寄った。";

	private static readonly Dictionary<string, CardDef> CardIndex = new Dictionary<string, CardDef>();

	private static readonly Dictionary<string, EnemyDef> EnemyIndex = new Dictionary<string, EnemyDef>();

	private static readonly Dictionary<string, EncounterDef> EncounterIndex = new Dictionary<string, EncounterDef>();

	private static readonly Dictionary<string, DeckDef> DeckIndex = new Dictionary<string, DeckDef>();

	private static readonly Dictionary<string, LeaderDef> LeaderIndex = new Dictionary<string, LeaderDef>();

	private static readonly Dictionary<string, RelicDef> RelicIndex = new Dictionary<string, RelicDef>();

	private static readonly Dictionary<string, EventDef> EventIndex = new Dictionary<string, EventDef>();

	private static readonly Dictionary<string, GearDef> GearIndex = new Dictionary<string, GearDef>();

	private static bool _loaded;

	private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
	{
		MissingMemberHandling = MissingMemberHandling.Ignore,
		DateParseHandling = DateParseHandling.None,
		Converters = new List<JsonConverter>
		{
			new ReadOnlyDictionaryAsDictionaryConverter()
		}
	};

	public static readonly CardDef WoundDef = new CardDef
	{
		Id = "status_wound",
		Name = "負傷",
		Cost = 0,
		Type = "spell",
		Color = "red",
		Effects = new List<DeclarativeEffect>()
	};

	public static readonly CardDef ScaldDef = new CardDef
	{
		Id = "status_scald",
		Name = "火傷",
		Cost = 0,
		Type = "spell",
		Color = "red",
		Effects = new List<DeclarativeEffect>()
	};

	public static readonly CardDef BrandDef = new CardDef
	{
		Id = "status_brand",
		Name = "呪いの烙印",
		Cost = 0,
		Type = "spell",
		Color = "black",
		Effects = new List<DeclarativeEffect>()
	};

	public static readonly CardDef GuiltDef = new CardDef
	{
		Id = "status_guilt",
		Name = "仮初の烙印",
		Cost = 0,
		Type = "spell",
		Color = "black",
		Effects = new List<DeclarativeEffect>()
	};

	public static readonly CardDef JunkDef = new CardDef
	{
		Id = "status_junk",
		Name = "がらくた",
		Cost = 0,
		Type = "physical",
		Color = "red",
		Effects = new List<DeclarativeEffect>()
	};

	public static IReadOnlyList<CardDef> AllCards { get; private set; } = new List<CardDef>();

	public static IReadOnlyList<EnemyDef> AllEnemies { get; private set; } = new List<EnemyDef>();

	public static IReadOnlyList<EncounterDef> AllEncounters { get; private set; } = new List<EncounterDef>();

	public static IReadOnlyList<DeckDef> AllDecks { get; private set; } = new List<DeckDef>();

	public static IReadOnlyList<LeaderDef> AllLeaders { get; private set; } = new List<LeaderDef>();

	public static IReadOnlyList<RelicDef> AllRelics { get; private set; } = new List<RelicDef>();

	public static IReadOnlyList<EventDef> AllEvents { get; private set; } = new List<EventDef>();

	public static IReadOnlyList<GearDef> AllGears { get; private set; } = new List<GearDef>();

	public static IReadOnlyList<GearDef> PoolGears { get; private set; } = new List<GearDef>();

	public static IReadOnlyList<DepartureTemplate> AllDepartures { get; private set; } = new List<DepartureTemplate>();

	public static JArray AllFusionsRaw { get; private set; } = new JArray();

	public static IReadOnlyList<FusionRecipe> AllFusions { get; private set; } = new List<FusionRecipe>();

	public static bool IsLoaded => _loaded;

	public static CardDef WOUND_DEF => WoundDef;

	public static CardDef SCALD_DEF => ScaldDef;

	public static CardDef BRAND_DEF => BrandDef;

	public static CardDef GUILT_DEF => GuiltDef;

	public static CardDef JUNK_DEF => JunkDef;

	public static string DepartureMasterLine(IReadOnlyList<string> colors)
	{
		string c = ((colors.Count > 0) ? colors[0] : "green");
		if (!DEPARTURE_MASTER_LINES.TryGetValue(c, out var line))
		{
			return DEPARTURE_MASTER_LINES["green"];
		}
		return line;
	}

	public static void Load(string dataDir)
	{
		LoadFrom((string file) =>
		{
			string text = Path.Combine(dataDir, file);
			if (!File.Exists(text))
			{
				throw new InvalidOperationException("データファイルが見つからない: " + text);
			}
			return File.ReadAllText(text);
		});
	}

	public static void LoadFrom(Func<string, string> read)
	{
		List<CardDef> list = new List<CardDef>();
		list.AddRange(WithColor(ParseList<CardDef>(read("cards.green.json"), "cards.green.json"), "green"));
		list.AddRange(WithColor(ParseList<CardDef>(read("cards.blue.json"), "cards.blue.json"), "blue"));
		list.AddRange(WithColor(ParseList<CardDef>(read("cards.red.json"), "cards.red.json"), "red"));
		list.AddRange(WithColor(ParseList<CardDef>(read("cards.white.json"), "cards.white.json"), "white"));
		list.AddRange(WithColor(ParseList<CardDef>(read("cards.black.json"), "cards.black.json"), "black"));
		AllCards = list;
		AllEnemies = ParseList<EnemyDef>(read("enemies.json"), "enemies.json");
		AllEncounters = ParseList<EncounterDef>(read("encounters.json"), "encounters.json");
		AllDecks = ParseList<DeckDef>(read("decks.json"), "decks.json");
		AllLeaders = ParseList<LeaderDef>(read("leaders.json"), "leaders.json");
		AllRelics = ParseList<RelicDef>(read("relics.json"), "relics.json");
		AllEvents = ParseList<EventDef>(read("events.json"), "events.json");
		AllGears = ParseList<GearDef>(read("gears.json"), "gears.json");
		List<GearDef> pool = new List<GearDef>();
		foreach (GearDef g in AllGears)
		{
			if (g.Retired != true)
			{
				pool.Add(g);
			}
		}
		PoolGears = pool;
		AllDepartures = ParseList<DepartureTemplate>(read("departures.json"), "departures.json");
		AllFusionsRaw = JArray.Parse(read("fusions.json"));
		AllFusions = DeserializeFusions<FusionRecipe>();
		Reindex(CardIndex, AllCards, (CardDef c) => c.Id);
		Reindex(EnemyIndex, AllEnemies, (EnemyDef e) => e.Id);
		Reindex(EncounterIndex, AllEncounters, (EncounterDef e) => e.Id);
		Reindex(DeckIndex, AllDecks, (DeckDef d) => d.Id);
		Reindex(LeaderIndex, AllLeaders, (LeaderDef l) => l.Id);
		Reindex(RelicIndex, AllRelics, (RelicDef r) => r.Id);
		Reindex(EventIndex, AllEvents, (EventDef e) => e.Id);
		Reindex(GearIndex, AllGears, (GearDef gearDef) => gearDef.Id);
		_loaded = true;
	}

	public static IReadOnlyList<T> DeserializeFusions<T>()
	{
		return AllFusionsRaw.ToObject<List<T>>(JsonSerializer.Create(Settings)) ?? new List<T>();
	}

	private static List<T> ParseList<T>(string json, string name)
	{
		if (json == null)
		{
			throw new InvalidOperationException("データファイルが見つからない: " + name);
		}
		return JsonConvert.DeserializeObject<List<T>>(json, Settings) ?? throw new InvalidOperationException("データファイルを読めない: " + name);
	}

	private static IEnumerable<CardDef> WithColor(IReadOnlyList<CardDef> cards, string color)
	{
		for (int i = 0; i < cards.Count; i++)
		{
			yield return cards[i]with
			{
				Color = color
			};
		}
	}

	private static void Reindex<T>(Dictionary<string, T> index, IReadOnlyList<T> items, Func<T, string> idOf)
	{
		index.Clear();
		for (int i = 0; i < items.Count; i++)
		{
			string id = idOf(items[i]);
			if (!index.ContainsKey(id))
			{
				index[id] = items[i];
			}
		}
	}

	public static IReadOnlyList<EncounterMember> ResolveEncounter(string id)
	{
		if (EncounterIndex.TryGetValue(id, out var enc))
		{
			return enc.Members;
		}
		if (EnemyIndex.ContainsKey(id))
		{
			return new List<EncounterMember>
			{
				new EncounterMember
				{
					EnemyId = id
				}
			};
		}
		throw new InvalidOperationException("未定義の敵/編成: " + id);
	}

	public static string EncounterName(string id)
	{
		if (EncounterIndex.TryGetValue(id, out var enc))
		{
			return enc.Name;
		}
		return GetEnemyDef(id).Name;
	}

	public static EventDef GetEventDef(string id)
	{
		if (!EventIndex.TryGetValue(id, out var def))
		{
			throw new InvalidOperationException("未定義イベント: " + id);
		}
		return def;
	}

	public static GearDef GetGearDef(string id)
	{
		if (!GearIndex.TryGetValue(id, out var def))
		{
			throw new InvalidOperationException("未定義のギア: " + id);
		}
		return def;
	}

	public static RelicDef GetRelicDef(string id)
	{
		if (!RelicIndex.TryGetValue(id, out var def))
		{
			throw new InvalidOperationException("未定義レリック: " + id);
		}
		return def;
	}

	public static CardInstance BuildRelicPermanent(RelicDef relic)
	{
		return new CardInstance
		{
			Uid = "relic_" + relic.Id,
			Innate = true,
			Def = new CardDef
			{
				Id = relic.Id + "_passive",
				Name = relic.Name,
				Cost = 0,
				Type = "permanent",
				Color = "green",
				Effects = (relic.Effects ?? new List<DeclarativeEffect>())
			}
		};
	}

	public static LeaderDef GetLeaderDef(string id)
	{
		if (!LeaderIndex.TryGetValue(id, out var def))
		{
			throw new InvalidOperationException("未定義リーダー: " + id);
		}
		return def;
	}

	public static bool DeckAllowedForLeader(LeaderDef leader, DeckDef deck)
	{
		for (int i = 0; i < leader.Colors.Count; i++)
		{
			if (leader.Colors[i] == deck.Color)
			{
				return true;
			}
		}
		return false;
	}

	public static CardInstance BuildLeaderPassive(LeaderDef leader)
	{
		return new CardInstance
		{
			Uid = "leader_" + leader.Id,
			Innate = true,
			Def = new CardDef
			{
				Id = leader.Id + "_passive",
				Name = leader.Name + "の能力",
				Cost = 0,
				Type = "permanent",
				Color = leader.Colors[0],
				Effects = leader.Passive
			}
		};
	}

	public static CardDef GetCardDef(string id)
	{
		if (id == WoundDef.Id)
		{
			return WoundDef;
		}
		if (id == ScaldDef.Id)
		{
			return ScaldDef;
		}
		if (id == BrandDef.Id)
		{
			return BrandDef;
		}
		if (id == GuiltDef.Id)
		{
			return GuiltDef;
		}
		if (id == JunkDef.Id)
		{
			return JunkDef;
		}
		if (!CardIndex.TryGetValue(id, out var def))
		{
			throw new InvalidOperationException("未定義カード: " + id);
		}
		return def;
	}

	public static EnemyDef GetEnemyDef(string id)
	{
		if (!EnemyIndex.TryGetValue(id, out var def))
		{
			throw new InvalidOperationException("未定義の敵: " + id);
		}
		return def;
	}

	public static DeckDef GetDeckDef(string id)
	{
		if (!DeckIndex.TryGetValue(id, out var def))
		{
			throw new InvalidOperationException("未定義デッキ: " + id);
		}
		return def;
	}

	public static IReadOnlyList<CardInstance> BuildDeck(string deckId)
	{
		DeckDef deck = GetDeckDef(deckId);
		List<CardInstance> cards = new List<CardInstance>();
		for (int e = 0; e < deck.Cards.Count; e++)
		{
			DeckCardEntry entry = deck.Cards[e];
			CardDef def = GetCardDef(entry.CardId);
			for (int i = 0; i < entry.Count; i++)
			{
				cards.Add(new CardInstance
				{
					Uid = $"{entry.CardId}#{i}",
					Def = def
				});
			}
		}
		return cards;
	}

	public static int DeckSize(DeckDef deck)
	{
		int sum = 0;
		for (int i = 0; i < deck.Cards.Count; i++)
		{
			sum += deck.Cards[i].Count;
		}
		return sum;
	}

	public static bool DebugOverridesActive()
	{
		return false;
	}
}
