using UnityEngine;

[DataBundleClass(Category = "Design")]
public class WeaponSchema
{
	[DataBundleKey]
	public string id;

	[DataBundleField]
	public bool isRanged;

	[DataBundleField]
	public bool isBladeWeapon;

	[DataBundleField]
	public bool isDualWield;

	[DataBundleField]
	public float attackRange;

	[DataBundleField]
	public float attackFrequency;

	[DataBundleField]
	public float damage;

	[DataBundleField]
	public int knockbackPower;

	[DataBundleField]
	public float DOTDamageRatio;

	[DataBundleField]
	public float DOTDuration;

	[DataBundleField]
	public float DOTInterval;

	[DataBundleField(StaticResource = true, Group = (DataBundleResourceGroup.InGame | DataBundleResourceGroup.Preview))]
	public GameObject prefab;

	[DataBundleRecordTableFilter("Projectile")]
	[DataBundleSchemaFilter(typeof(DynamicEnum), false)]
	public DataBundleRecordKey projectile;

	[DataBundleField(StaticResource = true, Group = DataBundleResourceGroup.None)]
	public Texture2D icon;

	[DataBundleField(StaticResource = true, Group = DataBundleResourceGroup.None)]
	public Texture2D iconNoText;

	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	[DataBundleRecordTableFilter("LocalizedStrings")]
	public DataBundleRecordKey desc;

	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	[DataBundleRecordTableFilter("LocalizedStrings")]
	public DataBundleRecordKey title;

	public int defenseRating;

	public string IconPath { get; private set; }

	public void Initialize()
	{
		IconPath = DataBundleRuntime.Instance.GetValue<string>(typeof(WeaponSchema), "Weapons", id, "icon", true);
	}

	public void LoadCachedResources(int level)
	{
		string tableRecordKey = DataBundleRuntime.TableRecordKey("Weapons", id);
		ResourceCache.LoadCachedResources(this, tableRecordKey);
	}

	public void UnloadCachedResources()
	{
		string tableRecordKey = DataBundleRuntime.TableRecordKey("Weapons", id);
		ResourceCache.UnloadCachedResources(this, tableRecordKey);
	}
}
