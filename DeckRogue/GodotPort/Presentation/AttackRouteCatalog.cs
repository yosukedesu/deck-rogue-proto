using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;

namespace DeckRogue.GodotPort.Presentation;

public sealed class AttackRouteCatalog
{
	private readonly Dictionary<string, AttackRoute> _routes = new Dictionary<string, AttackRoute>();

	public IReadOnlyCollection<AttackRoute> Routes => _routes.Values;

	public int UnknownRoutes { get; private set; }

	public string LeaderId { get; set; } = "";

	public static AttackRoute Neutral(string key = "unresolved")
	{
		return new AttackRoute(key, "", "system", "minimal_neutral_contact", "BoundedContactFx", "neutral", FixedSmall: true, "unresolved provenance; conservative contact");
	}

	public void Load(string json)
	{
		_routes.Clear();
		foreach (AttackRoute r in JsonConvert.DeserializeObject<List<AttackRoute>>(json) ?? new List<AttackRoute>())
		{
			_routes[Identity(r)] = r;
		}
	}

	public static string Identity(AttackRoute r)
	{
		string sourceKind = r.SourceKind;
		string text;
		if (!(r.SourceKind == "enemy_actual_execution"))
		{
			text = r.DefinitionId;
		}
		else
		{
			string key = r.Key;
			text = key.Substring(6, key.Length - 6);
		}
		return sourceKind + ":" + text;
	}

	public AttackRoute Resolve(GameEvent_DamageDealt hit)
	{
		DamagePresentationSource source = hit.PresentationSource;
		string key = (((object)source == null) ? null : (source.Kind + ":" + source.DefinitionId));
		EnemyActionPresentation enemy = hit.PresentationEnemyAction;
		if ((object)enemy != null && enemy.MoveId != null)
		{
			key = "enemy_actual_execution:" + enemy.EnemyId + "/" + enemy.MoveId;
		}
		if (source?.DefinitionId == "blue_aether_burst" && LeaderId == "leader_izzet")
		{
			return new AttackRoute("card:blue_aether_burst", source.DefinitionId, source.Kind, "lightning_discharge", "LightningDischargeFx", "lightning", FixedSmall: false, "source leader override");
		}
		if (key != null && _routes.TryGetValue(key, out AttackRoute route))
		{
			return route;
		}
		UnknownRoutes++;
		return Neutral(key ?? hit.Source);
	}

	public AttackRoute ResolveKey(string key)
	{
		AttackRoute r;
		return _routes.TryGetValue(key, out r) ? r : Neutral(key);
	}
}
