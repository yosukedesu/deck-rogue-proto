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

	public bool Busy => (object)Active != null || _pending.Count > 0;

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
				string reason;
				string sys = CombatOutcome.PositiveSystemFamily(e, out reason);
				if (sys == null)
				{
					if (e is GameEvent_HpHealed { Amount: var amount })
					{
						if (amount > 0)
						{
							goto IL_0379;
						}
					}
					else if (e is GameEvent_EnemyHealed { Amount: var amount2 })
					{
						if (amount2 > 0)
						{
							goto IL_0379;
						}
					}
					else if (e is GameEvent_RegenTicked { Amount: var amount3 } && amount3 > 0)
					{
						goto IL_0379;
					}
					flag = false;
					goto IL_0381;
				}
				route = new AttackRoute("system:" + sys, "", "system", sys, "SystemOutcomeFx", (sys == "burn") ? "fire" : "neutral", FixedSmall: true, "validated system loss");
				scenario = EffectScenario.For(route, 1);
				duration = scenario.Duration;
				contact = scenario.Windup;
			}
			goto IL_042a;
			IL_0381:
			if (flag)
			{
				int recovered = ((e is GameEvent_HpHealed h) ? h.Amount : ((e is GameEvent_EnemyHealed eh) ? eh.Amount : ((GameEvent_RegenTicked)e).Amount));
				route = new AttackRoute("system:heal", "", "system", "heal", "HealContinuousFx", "light", recovered <= 4, "source semantic recovery size");
				scenario = EffectScenario.For(route, 1);
				duration = scenario.Duration;
				contact = scenario.Windup;
			}
			goto IL_042a;
			IL_042a:
			if (i == finalHit)
			{
				duration += 0.16f;
			}
			_pending.Enqueue(new PresentationBeat(e, route, scenario, i == finalHit, duration, contact, contacts));
			i++;
			continue;
			IL_0379:
			flag = true;
			goto IL_0381;
		}
	}

	private static float DurationFor(GameEvent e)
	{
		string type = e.Type;
		if (1 == 0)
		{
		}
		float result;
		switch (type)
		{
		case "Cascaded":
		case "CascadeRetargeted":
			result = 0.22f;
			break;
		case "ImpulseReturned":
		case "ImpulseKept":
		case "ImpulseKindled":
		case "BlazeIgnited":
		case "BlazeExtinguished":
			result = 0.16f;
			break;
		case "EnemyActionExecuting":
			result = 0.2f;
			break;
		case "EnemyActionResolved":
			result = 0.07f;
			break;
		case "EnemyDied":
			result = 1.08f;
			break;
		case "EnemyFled":
			result = 0.56f;
			break;
		case "CombatEnded":
			result = 0.22f;
			break;
		case "TurnStarted":
			result = 0.14f;
			break;
		case "TurnEnded":
			result = 0.12f;
			break;
		case "CombatStarted":
			result = 0.2f;
			break;
		case "BlockGained":
		case "IceBlockGained":
		case "HpHealed":
		case "EnemyHealed":
		case "BurnTick":
		case "ScaldTick":
			result = 0.24f;
			break;
		case "ReactionTriggered":
		case "ActionNegated":
		case "DeathSaved":
			result = 0.26f;
			break;
		case "EnemySummoned":
		case "EnemySplit":
		case "EnemyHatched":
		case "PermanentPlayed":
		case "RetainerRushed":
			result = 0.22f;
			break;
		case "CardPlayed":
		case "CardSet":
		case "GearUsed":
			result = 0.11f;
			break;
		case "StatusInflicted":
		case "ExposedApplied":
		case "EnemyWeakened":
		case "BurnApplied":
		case "StrengthGained":
		case "GrowthAdded":
		case "MomentumAdded":
		case "LightGained":
		case "LightSpent":
		case "LightDischarged":
		case "ArtifactBlocked":
		case "PlayerArtifactBlocked":
			result = 0.16f;
			break;
		default:
			result = 0.035f;
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private void BeginNext()
	{
		if (!_pending.TryDequeue(out PresentationBeat next))
		{
			Active = null;
			this.Drained?.Invoke();
			return;
		}
		Active = next;
		Time = 0f;
		_retimed = 0f;
		LastContactTime = 0f;
		ContactDelivered = false;
		ContactOrdinal = -1;
		this.Started?.Invoke(next);
	}

	private void DeliverContact(PresentationBeat beat)
	{
		LastContactTime = Time;
		ContactOrdinal++;
		EnemyAttackContactPlan? contacts = beat.Contacts;
		int count = ((contacts == null || !contacts.Individual) ? 1 : beat.Contacts.Count);
		ContactDelivered = ContactOrdinal >= count - 1;
		this.Contact?.Invoke(beat);
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
			int count = ((contacts == null || !contacts.Individual) ? 1 : beat.Contacts.Count);
			float nextContact = beat.ContactAt + (float)(ContactOrdinal + 1) * 0.12f + _retimed;
			bool visual = (object)beat.Route != null;
			if (!ContactDelivered && Time >= nextContact - 1E-06f)
			{
				if (visual && visualContact)
				{
					break;
				}
				DeliverContact(beat);
				visualContact = visualContact || visual;
				if (!Busy)
				{
					break;
				}
			}
			float end = beat.Duration + _retimed;
			if (Time >= end - 1E-06f)
			{
				this.Finished?.Invoke(beat);
				CompletedBeats++;
				Active = null;
				if (_pending.Count == 0)
				{
					this.Drained?.Invoke();
					break;
				}
				continue;
			}
			float boundary = end;
			float visibleCap = LastContactTime + Math.Min(0.1f, Math.Max(0.002f, end - LastContactTime) * 0.75f);
			if (visualContact && visual)
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
			this.TimeAdvanced?.Invoke(consumed * rate);
			if (!visualContact || ContactDelivered || !(Time >= boundary - 1E-06f))
			{
				continue;
			}
			break;
		}
	}
}
