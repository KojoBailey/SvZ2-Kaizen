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

		float damage = Extrapolate((AbilityLevelSchema als) => als.damage);
		float damageMultEachTarget = Extrapolate((AbilityLevelSchema als) => als.damageMultEachTarget);
		float radius = Extrapolate((AbilityLevelSchema als) => als.radius);
		var knockback = (int)Extrapolate((AbilityLevelSchema als) => als.effectModifier);

		List<Character> enemiesAhead = hero.GetEnemiesAhead(radius);
		foreach (Character item in enemiesAhead)
		{
			item.RecievedAttack(EAttackType.Slice, damage, executor);
			item.TryKnockback(knockback);
			damage *= damageMultEachTarget;
		}
	}
}
