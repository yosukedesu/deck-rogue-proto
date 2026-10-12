using System;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game;

public sealed class EnemyAttackContactPlan
{
	public sealed class SourceProof
	{
		public readonly int EnemyIndex;

		public readonly string EnemyId;

		public readonly bool ContinuityProven;

		public SourceProof(int enemyIndex, string enemyId, bool continuityProven)
		{
			EnemyIndex = enemyIndex;
			EnemyId = enemyId;
			ContinuityProven = continuityProven;
		}
	}

	public const int MaxIndividualContacts = 12;

	public const int MaxCapturedContributions = 256;

	public const float ContactSpacing = 0.12f;

	private readonly EnemyAttackContribution[] rows;

	internal readonly CombatPresentationVitals Before;

	internal readonly CombatPresentationVitals After;

	public readonly int SourceIndex;

	public readonly int ExecutedHits;

	public readonly string SourceId;

	public readonly string Reason;

	public readonly long Amount;

	public readonly long HpLoss;

	public readonly long Block;

	public readonly long Ice;

	public readonly long Nullified;

	public readonly long Mitigated;

	public bool Individual => Reason == null && Count >= 2 && Count <= 12;

	public int Count => rows.Length;

	public float Span => Individual ? (0.12f * (float)(Count - 1)) : 0f;

	public EnemyAttackContribution this[int ordinal] => rows[ordinal];

	private EnemyAttackContactPlan(GameEvent_DamageDealt e, EnemyAttackContribution[] captured, string reason, long block = 0L, long ice = 0L, long nullified = 0L, long mitigated = 0L)
	{
		rows = captured ?? Array.Empty<EnemyAttackContribution>();
		Reason = reason;
		SourceIndex = e?.EnemyIndex ?? (-1);
		SourceId = e?.PresentationEnemyAttack?.EnemyId;
		ExecutedHits = (e?.PresentationEnemyAttack?.ExecutedHits).GetValueOrDefault();
		Amount = e?.Amount ?? 0;
		HpLoss = e?.HpLoss ?? 0;
		Block = block;
		Ice = ice;
		Nullified = nullified;
		Mitigated = mitigated;
		CombatPresentationReceipt v = e?.PresentationReceipt;
		Before = ((v?.Before == null) ? null : v.Before with { });
		After = ((v?.After == null) ? null : v.After with { });
	}

	public bool Matches(SourceProof proof)
	{
		return proof != null && proof.ContinuityProven && proof.EnemyIndex == SourceIndex && !string.IsNullOrEmpty(proof.EnemyId) && proof.EnemyId == SourceId;
	}

	public static EnemyAttackContactPlan Capture(GameEvent e, CombatPresentationReceipt receipt, SourceProof proof)
	{
		GameEvent_DamageDealt d = e as GameEvent_DamageDealt;
		if (d == null || d.Source != "enemy" || !d.EnemyIndex.HasValue || d.EnemyIndex.Value < 0)
		{
			return Reject("enemy_contact_source_unsupported");
		}
		if ((object)receipt != d.PresentationReceipt || !CombatPresentationLedger.Valid(d, receipt, out string _) || receipt.Target != "player")
		{
			return Reject("enemy_contact_numeric_receipt_invalid");
		}
		EnemyAttackPresentation p = d.PresentationEnemyAttack;
		if (p == null)
		{
			return Reject("missing_enemy_contact_metadata");
		}
		if (p.Version != 1)
		{
			return Reject("unsupported_enemy_contact_metadata_version");
		}
		if (p.Resolution != "aggregate-final-mutation")
		{
			return Reject("unsupported_enemy_contact_resolution");
		}
		if (string.IsNullOrEmpty(p.EnemyId))
		{
			return Reject("enemy_contact_source_id_missing");
		}
		if (proof == null || !proof.ContinuityProven || proof.EnemyIndex != d.EnemyIndex.Value || proof.EnemyId != p.EnemyId)
		{
			return Reject("enemy_contact_source_unproved");
		}
		if (p.EffectiveActual < 0 || p.LoopBound < 0 || p.ExecutedHits < 0 || d.Amount < 0 || d.HpLoss < 0 || d.Blocked.GetValueOrDefault() < 0)
		{
			return Reject("invalid_enemy_contact_scalar");
		}
		if (p.ExecutedHits != Math.Max(0, p.LoopBound))
		{
			return Reject("enemy_contact_count_mismatch");
		}
		if (p.ContributionsOmittedReason != null)
		{
			if (p.Contributions != null || p.ContributionsOmittedReason != "presentation_capture_limit" || p.ExecutedHits <= 256)
			{
				return Reject("contradictory_enemy_contact_omission");
			}
			return Reject("presentation_capture_limit");
		}
		if (p.ExecutedHits > 256)
		{
			return Reject("enemy_contact_capture_limit_violation");
		}
		if (p.Contributions == null || p.Contributions.Count != p.ExecutedHits)
		{
			return Reject("enemy_contact_count_mismatch");
		}
		EnemyAttackContribution[] captured = new EnemyAttackContribution[p.Contributions.Count];
		long amount = 0L;
		long hp = 0L;
		long block = 0L;
		long ice = 0L;
		long nullified = 0L;
		long mitigated = 0L;
		for (int i = 0; i < captured.Length; i++)
		{
			EnemyAttackContribution row = p.Contributions[i];
			if (row == null || row.AmountContribution < 0 || row.HpLossContribution < 0 || row.BlockContribution < 0 || row.IceContribution < 0 || row.NullifiedContribution < 0 || row.MitigatedContribution < 0)
			{
				return Reject("invalid_enemy_contact_contribution");
			}
			if (row.AmountContribution != (long)row.HpLossContribution + (long)row.BlockContribution + row.IceContribution + row.NullifiedContribution + row.MitigatedContribution)
			{
				return Reject("enemy_contact_partition_mismatch");
			}
			captured[i] = row with { };
			amount += row.AmountContribution;
			hp += row.HpLossContribution;
			block += row.BlockContribution;
			ice += row.IceContribution;
			nullified += row.NullifiedContribution;
			mitigated += row.MitigatedContribution;
		}
		if (amount != d.Amount || hp != d.HpLoss || block + ice + nullified != d.Blocked.GetValueOrDefault())
		{
			return Reject("enemy_contact_aggregate_mismatch");
		}
		CombatPresentationVitals before = receipt.Before;
		CombatPresentationVitals after = receipt.After;
		if (before.MaxHp != after.MaxHp || (long)before.Hp - (long)after.Hp != hp || (long)before.Block - (long)after.Block != block || (long)before.IceBlock.Value - (long)after.IceBlock.Value != ice)
		{
			return Reject("enemy_contact_numeric_delta_mismatch");
		}
		string reason2 = ((captured.Length == 0) ? "enemy_contact_zero_executed" : ((captured.Length == 1) ? "enemy_contact_single" : ((captured.Length > 12) ? "enemy_contact_presentation_limit" : null)));
		return new EnemyAttackContactPlan(d, captured, reason2, block, ice, nullified, mitigated);
		EnemyAttackContactPlan Reject(string why)
		{
			return new EnemyAttackContactPlan(d, null, why, 0L, 0L, 0L, 0L);
		}
	}
}
