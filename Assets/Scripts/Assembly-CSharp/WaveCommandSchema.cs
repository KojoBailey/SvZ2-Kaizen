[DataBundleClass(Category = "Design")]
public class WaveCommandSchema
{
	public enum Type
	{
		Default, // Default
		Spawn,
		Delay, // not yet implemented
		Banner,
		MoveGate,
		Tutorial, // not yet implemented
	}

	public enum Spacing
	{
		Loose, // Default
		Moderate,
		Tight,
	}

	public enum SpawnAt
	{
		MediumHealth, // Default
		NoDelay,
		HighHealth,
		LowHealth,
		VeryLowHealth,
		Death,
	}

	public enum MaxDelay
	{
		Normal, // Default
		Low,
		High,
		Infinite,
	}

	public enum GatePos
	{
		Close, // Default
		Middle,
		Far,
	}

	[DataBundleKey]
	public int index;

	[DataBundleField]
	public Type type;

	[DataBundleSchemaFilter(typeof(EnemySchema), false)]
	[DataBundleField(ColumnWidth = 200)]
	[DataBundleRecordTableFilter("Enemies")]
	public DataBundleRecordKey enemy;

	[DataBundleField]
	public int count;

	[DataBundleField]
	public Spacing spacing;

	public float spacingSeconds
	{
		get { return SpacingToSeconds(spacing); }
	}

	[DataBundleField]
	public float duration;

	[DataBundleField]
	public SpawnAt spawnAt;

	public float spawnAtPercent
	{
		get { return SpawnAtToPercent(spawnAt); }
	}

	[DataBundleField]
	public MaxDelay maxDelay;

	public float maxDelaySeconds
	{
		get { return MaxDelayToSeconds(maxDelay); }
	}

	[DataBundleField]
	[DataBundleDefaultValue(false)]
	public bool simultaneous;

	[DataBundleField(ColumnWidth = 200)]
	public string banner;

	[DataBundleField]
	public GatePos gatePos;

	public int gatePosIndex
	{
		get { return GatePosToIndex(gatePos); }
	}

	[DataBundleField(ColumnWidth = 200)]
	public string tutorial;
	
	[DataBundleField(ColumnWidth = 200)]
	public string command;

	[DataBundleSchemaFilter(typeof(EnemyGroupSchema), false)]
	[DataBundleField(ColumnWidth = 200)]
	public DataBundleRecordTable enemyGroup;

	public static float SpacingToSeconds(Spacing _spacing)
	{
		switch (_spacing)
		{
		case Spacing.Loose		: return 3.0f;
		case Spacing.Moderate	: return 1.5f;
		case Spacing.Tight		: return 0.5f;
		default: return 0;
		}
	}

	public static float SpawnAtToPercent(SpawnAt _spawnAt)
	{
		switch (_spawnAt)
		{
		case SpawnAt.NoDelay		: return 1f;	// 100%
		case SpawnAt.HighHealth		: return 0.7f;	// 70%
		case SpawnAt.MediumHealth	: return 0.5f;	// 50%
		case SpawnAt.LowHealth		: return 0.3f;	// 30%
		case SpawnAt.VeryLowHealth 	: return 0.2f;	// 20%
		case SpawnAt.Death			: return 0f;	// 0%
		default: return 0;
		}
	}

	public static float MaxDelayToSeconds(MaxDelay _maxDelay)
	{
		switch (_maxDelay)
		{
		case MaxDelay.Low		: return 15.0f;
		case MaxDelay.Normal	: return 25.0f;
		case MaxDelay.High		: return 35.0f;
		case MaxDelay.Infinite	: return float.MaxValue;
		default: return 0;
		}
	}

	public static int GatePosToIndex(GatePos _gatePos)
	{
		switch (_gatePos)
		{
		case GatePos.Close	: return 0;
		case GatePos.Middle	: return 1;
		case GatePos.Far	: return 2;
		default: return 0;
		}
	}
}