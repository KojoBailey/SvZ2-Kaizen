using UnityEngine;

public class AbilityHandler : IAbilityHandler
{
	public readonly float kGravityAccel = 9.8f;

	protected Vector3 mVelocity = Vector3.zero;

	public float gravityAccel
	{
		get { return kGravityAccel; }
	}

	public bool leftToRightGameplay { get; set; }

	public AbilitySchema schema { get; set; }

	public string id
	{
		get { return schema.id; }
	}

	public int activatingPlayer { get; set; }

	public virtual void Activate(Character executor)
	{
		activatingPlayer = (executor != null) ? executor.ownerId : 1;
	}

	public virtual void Execute(Character executor) {}
}
