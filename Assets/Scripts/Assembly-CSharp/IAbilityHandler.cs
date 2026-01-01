public interface IAbilityHandler
{
	float gravityAccel { get; }

	bool leftToRightGameplay { get; }

	AbilitySchema schema { get; }

	string id { get; }
}
