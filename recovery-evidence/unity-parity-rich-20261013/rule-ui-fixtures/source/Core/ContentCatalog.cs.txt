using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.Core;

public sealed class ContentCatalog
{
	public IReadOnlyList<CardDef> Cards => Content.AllCards;

	public IReadOnlyList<EnemyDef> Enemies => Content.AllEnemies;

	public IReadOnlyList<EncounterDef> Encounters => Content.AllEncounters;

	public IReadOnlyList<DeckDef> Decks => Content.AllDecks;

	public IReadOnlyList<LeaderDef> Leaders => Content.AllLeaders;

	public IReadOnlyList<RelicDef> Relics => Content.AllRelics;

	public IReadOnlyList<EventDef> Events => Content.AllEvents;

	public IReadOnlyList<GearDef> Gears => Content.AllGears;

	public IReadOnlyList<DepartureTemplate> Departures => Content.AllDepartures;

	public IReadOnlyList<FusionRecipe> Fusions => Content.AllFusions;

	public string Fingerprint => Report.DataFingerprint();
}
