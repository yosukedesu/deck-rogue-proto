using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

public sealed class PresentationTimeline
{
	private readonly Queue<PresentationBeat> _pending = new Queue<PresentationBeat>();

	private float _retimed;

	public PresentationBeat? Active { get; private set; }

	public float Time { get; private set; }

	public bool ContactDelivered { get; private set; }

	public int ContactOrdinal { get; private set; }

	public float LastContactTime { get; private set; }

	public float LastAdvanceDelta { get; private set; }

	public Func<GameEvent_DamageDealt, EnemyAttackContactPlan?>? ContactPlanner { get; set; }

	public bool Busy
	{
		get
		{
			if ((object)Active == null)
			{
				return _pending.Count > 0;
			}
			return true;
		}
	}

	public int PendingCount => _pending.Count;

	public long Epoch { get; private set; }

	public int CompletedBeats { get; private set; }

	public event Action<PresentationBeat>? Started;

	public event Action<PresentationBeat>? Contact;

	public event Action<PresentationBeat>? Finished;

	public event Action? Drained;

	public event Action<float>? TimeAdvanced;

	public void Reset()
	{
		_pending.Clear();
		Active = null;
		Time = 0f;
		ContactDelivered = false;
		Epoch++;
	}

	public void Enqueue(IReadOnlyList<GameEvent> events, AttackRouteCatalog routes)
	{
		int finalHit = (events.OfType<GameEvent_CombatEnded>().Any((GameEvent_CombatEnded gameEvent_CombatEnded) => gameEvent_CombatEnded.Result == "won") ? Enumerable.Range(0, events.Count).LastOrDefault((int index) => events[index] is GameEvent_DamageDealt gameEvent_DamageDealt && gameEvent_DamageDealt.HpLoss > 0, -1) : (-1));
		int i = 0;
		while (i < events.Count)
		{
			GameEvent e = events[i];
			AttackRoute route = null;
			EffectScenario scenario = null;
			float duration = DurationFor(e);
			float contact = 0f;
			EnemyAttackContactPlan contacts = null;
			bool flag;
			if (e is GameEvent_DamageDealt hit)
			{
				route = ((hit.HpLoss > 0) ? routes.Resolve(hit) : new AttackRoute("system:outcome", "", "system", CombatOutcome.Read(hit).Kind ?? "nullified", "SystemOutcomeFx", "neutral", FixedSmall: true, "source outcome path"));
				scenario = EffectScenario.For(route, hit.HpLoss, hit.PresentationReceipt?.Before.Hp, hit.PresentationReceipt?.Before.MaxHp);
				duration = scenario.Duration;
				contact = scenario.Windup;
				contacts = ContactPlanner?.Invoke(hit);
				if (contacts != null && contacts.Individual)
				{
					duration += contacts.Span;
				}
			}
			else if (e is GameEvent_ConfusedAttack confused)
			{
				route = new AttackRoute("system:confused", "", "system", (confused.HpLoss > 0) ? "minimal_neutral_contact" : (CombatOutcome.Read(confused).Kind ?? "nullified"), (confused.HpLoss > 0) ? "BoundedContactFx" : "SystemOutcomeFx", "neutral", FixedSmall: true, "explicit confused target");
				scenario = EffectScenario.For(route, confused.HpLoss, confused.PresentationReceipt?.Before.Hp, confused.PresentationReceipt?.Before.MaxHp);
				duration = scenario.Duration;
				contact = scenario.Windup;
			}
			else
			{
				string sys = CombatOutcome.PositiveSystemFamily(e, out var _);
				if (sys == null)
				{
					if (e is GameEvent_HpHealed gameEvent_HpHealed)
					{
						if (gameEvent_HpHealed.Amount > 0)
						{
							goto IL_0343;
						}
					}
					else if (e is GameEvent_EnemyHealed gameEvent_EnemyHealed)
					{
						if (gameEvent_EnemyHealed.Amount > 0)
						{
							goto IL_0343;
						}
					}
					else if (e is GameEvent_RegenTicked { Amount: >0 })
					{
						goto IL_0343;
					}
					flag = false;
					goto IL_034b;
				}
				route = new AttackRoute("system:" + sys, "", "system", sys, "SystemOutcomeFx", (sys == "burn") ? "fire" : "neutral", FixedSmall: true, "validated system loss");
				scenario = EffectScenario.For(route, 1);
				duration = scenario.Duration;
				contact = scenario.Windup;
			}
			goto IL_03eb;
			IL_034b:
			if (flag)
			{
				int recovered;
				if (e is GameEvent_HpHealed h)
				{
					recovered = h.Amount;
				}
				else
				{
					recovered = ((e is GameEvent_EnemyHealed eh) ? eh.Amount : ((GameEvent_RegenTicked)e).Amount);
				}
				route = new AttackRoute("system:heal", "", "system", "heal", "HealContinuousFx", "light", recovered <= 4, "source semantic recovery size");
				scenario = EffectScenario.For(route, 1);
				duration = scenario.Duration;
				contact = scenario.Windup;
			}
			goto IL_03eb;
			IL_03eb:
			if (i == finalHit)
			{
				duration += 0.16f;
			}
			_pending.Enqueue(new PresentationBeat(e, route, scenario, i == finalHit, duration, contact, contacts));
			i++;
			continue;
			IL_0343:
			flag = true;
			goto IL_034b;
		}
	}

	public PresentationBeat Describe(GameEvent e, AttackRouteCatalog routes)
	{
		PresentationTimeline description = new PresentationTimeline
		{
			ContactPlanner = ContactPlanner
		};
		description.Enqueue(new GameEvent[1] { e }, routes);
		return description._pending.Dequeue();
	}

	private static float DurationFor(GameEvent e)
	{
		switch (e.Type)
		{
		case "Cascaded":
		case "CascadeRetargeted":
			return 0.22f;
		case "BlazeExtinguished":
		case "ImpulseReturned":
		case "ImpulseKept":
		case "ImpulseKindled":
		case "BlazeIgnited":
			return 0.16f;
		case "EnemyActionExecuting":
			return 0.2f;
		case "EnemyActionResolved":
			return 0.07f;
		case "EnemyDied":
			return 1.08f;
		case "EnemyFled":
			return 0.56f;
		case "CombatEnded":
			return 0.22f;
		case "TurnStarted":
			return 0.14f;
		case "TurnEnded":
			return 0.12f;
		case "CombatStarted":
			return 0.2f;
		case "HpHealed":
		case "BurnTick":
		case "BlockGained":
		case "EnemyHealed":
		case "IceBlockGained":
		case "ScaldTick":
			return 0.24f;
		case "ReactionTriggered":
		case "ActionNegated":
		case "DeathSaved":
			return 0.26f;
		case "PermanentPlayed":
		case "RetainerRushed":
		case "EnemyHatched":
		case "EnemySummoned":
		case "EnemySplit":
			return 0.22f;
		case "GearUsed":
		case "CardPlayed":
		case "CardSet":
			return 0.11f;
		case "LightDischarged":
		case "StatusInflicted":
		case "ArtifactBlocked":
		case "BurnApplied":
		case "GrowthAdded":
		case "LightGained":
		case "StrengthGained":
		case "ExposedApplied":
		case "EnemyWeakened":
		case "MomentumAdded":
		case "LightSpent":
		case "PlayerArtifactBlocked":
			return 0.16f;
		default:
			return 0.035f;
		}
	}

	private void BeginNext()
	{
		if (!_pending.TryDequeue(out PresentationBeat next))
		{
			Active = null;
			Drained?.Invoke();
			return;
		}
		Active = next;
		Time = 0f;
		_retimed = 0f;
		LastContactTime = 0f;
		ContactDelivered = false;
		ContactOrdinal = -1;
		Started?.Invoke(next);
	}

	private void DeliverContact(PresentationBeat beat)
	{
		LastContactTime = Time;
		ContactOrdinal++;
		EnemyAttackContactPlan? contacts = beat.Contacts;
		int count = ((contacts == null || !contacts.Individual) ? 1 : beat.Contacts.Count);
		ContactDelivered = ContactOrdinal >= count - 1;
		Contact?.Invoke(beat);
	}

	public void Advance(float realDelta)
	{
		LastAdvanceDelta = 0f;
		if (realDelta <= 0f || !Busy)
		{
			return;
		}
		float remaining = realDelta;
		bool visualContact = false;
		int safety = 0;
		while (remaining > 1E-06f && Busy && safety++ < 4096)
		{
			if ((object)Active == null)
			{
				BeginNext();
			}
			if ((object)Active == null)
			{
				break;
			}
			PresentationBeat beat = Active;
			EnemyAttackContactPlan? contacts = beat.Contacts;
			if (contacts != null && contacts.Individual)
			{
				_ = beat.Contacts.Count;
			}
			float nextContact = beat.ContactAt + (float)(ContactOrdinal + 1) * 0.12f + _retimed;
			bool visual = (object)beat.Route != null;
			if (!ContactDelivered && Time >= nextContact - 1E-06f)
			{
				if (visual & visualContact)
				{
					break;
				}
				DeliverContact(beat);
				visualContact |= visual;
				if (!Busy)
				{
					break;
				}
			}
			float end = beat.Duration + _retimed;
			if (Time >= end - 1E-06f)
			{
				Finished?.Invoke(beat);
				CompletedBeats++;
				Active = null;
				if (_pending.Count == 0)
				{
					Drained?.Invoke();
					break;
				}
				continue;
			}
			float boundary = end;
			float visibleCap = LastContactTime + Math.Min(0.1f, Math.Max(0.002f, end - LastContactTime) * 0.75f);
			if (visualContact & visual)
			{
				if (Time >= visibleCap - 1E-06f)
				{
					break;
				}
				boundary = Math.Min(boundary, visibleCap);
			}
			if (!ContactDelivered)
			{
				boundary = Math.Min(boundary, beat.ContactAt + (float)(ContactOrdinal + 1) * 0.12f + _retimed);
			}
			float slowStart = beat.ContactAt;
			float slowEnd = slowStart + 0.14f;
			float rate = ((beat.WinningHit && Time >= slowStart && Time < slowEnd) ? 0.38f : 1f);
			if (beat.WinningHit)
			{
				if (Time < slowStart)
				{
					boundary = Math.Min(boundary, slowStart);
				}
				else if (Time < slowEnd)
				{
					boundary = Math.Min(boundary, slowEnd);
				}
			}
			float consumed = Math.Min(remaining, Math.Max(0f, (boundary - Time) / rate));
			if (consumed <= 1E-06f)
			{
				Time = boundary + 2E-06f;
				continue;
			}
			Time += consumed * rate;
			LastAdvanceDelta += consumed * rate;
			remaining -= consumed;
			TimeAdvanced?.Invoke(consumed * rate);
			if (visualContact && !ContactDelivered && Time >= boundary - 1E-06f)
			{
				break;
			}
		}
	}
}
