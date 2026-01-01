using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Game/KatanaSlashHandler")]
public class KatanaSlashHandler : AbilityHandler
{
	public override void Activate(Character executor)
	{
		base.Activate(executor);
		if (schema.activateFX != null)
		{
			CharacterModelController component = executor.controlledObject.GetComponent<CharacterModelController>();
			component.SpawnEffectAtJoint(schema.activateFX, "katana_slash_fx", false);
		}
	}

	public override void Execute(Character executor)
	{
		var hero = executor as Hero;
		if (hero == null) return;

		float damage = schema.damage;
		float damageMultEachTarget = schema.damageMultEachTarget;
		float radius = schema.radius;
		var knockback = (int)schema.effectModifier;

		List<Character> enemiesAhead = hero.GetEnemiesAhead(radius);
		foreach (Character item in enemiesAhead)
		{
			item.RecievedAttack(EAttackType.Slice, damage, executor);
			item.TryKnockback(knockback);
			damage *= damageMultEachTarget;
		}
	}
}
