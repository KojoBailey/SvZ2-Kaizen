[DataBundleClass(Category = "Design")]
public class WaveCommandSchema
{
	[DataBundleKey]
	public int index;

	public enum Type
	{
		Default = 0,
		Spawn,
		Delay,
	};
	[DataBundleField(ColumnWidth = 200)]
	public Type type;

	[DataBundleSchemaFilter(typeof(EnemySchema), false)]
	[DataBundleField(ColumnWidth = 200)]
	[DataBundleRecordTableFilter("Enemies")]
	public DataBundleRecordKey enemy;

	[DataBundleField(ColumnWidth = 200)]
	public int count;

	public enum Spacing
	{
		Loose = 0,
		Moderate,
		Tight,
	};

	public static float SpacingToDuration(Spacing spacing)
	{
		switch (spacing)
		{
		case Spacing.Loose		: return 3.0f;
		case Spacing.Moderate	: return 1.5f;
		case Spacing.Tight		: return 0.3f;
		default: return 0;
		}
	}

	[DataBundleField(ColumnWidth = 200)]
	public Spacing spacing;

	public float SpacingDuration()
	{
		return SpacingToDuration(spacing);
	}

	[DataBundleField(ColumnWidth = 200)]
	public float duration;

	public enum StartMode
	{
		After,
		Overlap,
	};
	[DataBundleField(ColumnWidth = 200)]
	public StartMode startMode;

	[DataBundleField(ColumnWidth = 200)]
	public float advanceAt;

	[DataBundleField(ColumnWidth = 200)]
	public string command;

	[DataBundleSchemaFilter(typeof(EnemyGroupSchema), false)]
	[DataBundleField(ColumnWidth = 200)]
	public DataBundleRecordTable enemyGroup;
}