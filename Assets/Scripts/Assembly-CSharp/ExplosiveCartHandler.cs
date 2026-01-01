using UnityEngine;

[AddComponentMenu("Game/ExplosiveCartHandler")]
public class ExplosiveCartHandler : AbilityHandler
{
	public override void Activate(Character executor)
	{
		base.Activate(executor);
		if (executor != null)
		{
			Character character = WeakGlobalMonoBehavior<InGameImpl>.Instance.GetLeadership(executor.ownerId).ForceSpawn("CartOfDoom");
			character.meleeDamage = schema.damage;
			float radius = schema.radius;
			character.explosionRange = () => radius;
		}
	}
}
