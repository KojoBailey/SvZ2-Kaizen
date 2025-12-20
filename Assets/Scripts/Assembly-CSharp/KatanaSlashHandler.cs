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

		float num = Extrapolate((AbilityLevelSchema als) => als.damage);
		float num2 = Extrapolate((AbilityLevelSchema als) => als.damageMultEachTarget);
		float num3 = num * Extrapolate((AbilityLevelSchema als) => als.lifeSteal);

		List<Character> enemiesAhead = hero.GetEnemiesAhead(Extrapolate((AbilityLevelSchema als) => als.radius));
		foreach (Character item in enemiesAhead)
		{
			item.RecievedAttack(EAttackType.Slice, num, executor);
			item.TryKnockback((int)Extrapolate((AbilityLevelSchema als) => als.effectModifier));
			num *= num2;
		}

		bool lifesteal = false;
		if (lifesteal)
		{
			hero.health += num3;
		}
	}
}
