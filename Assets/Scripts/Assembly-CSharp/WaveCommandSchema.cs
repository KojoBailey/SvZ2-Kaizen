[DataBundleClass(Category = "Design")]
public class WaveCommandSchema
{
	[DataBundleKey]
	public int index;

	public enum Type
	{
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
		Loose,
		Moderate,
		Tight,
	};
	[DataBundleField(ColumnWidth = 200)]
	public Spacing spacing;

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