using UnityEngine;

[DataBundleClass(Category = "Design")]
public class AbilitySchema
{
	[DataBundleKey]
	public string id;

	[DataBundleField(StaticResource = true)]
	public GameObject prefab;

	[DataBundleField(StaticResource = true)]
	public GameObject rightToLeftPrefab;

	[DataBundleField(StaticResource = true)]
	public GameObject prop;

	[DataBundleField(StaticResource = true)]
	public GameObject activateFX;

	[DataBundleField(StaticResource = true)]
	public GameObject resultFX;

	[DataBundleRecordTableFilter("LocalizedStrings")]
	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	public DataBundleRecordKey displayName;

	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	[DataBundleRecordTableFilter("LocalizedStrings")]
	public DataBundleRecordKey description;

	[DataBundleField(StaticResource = true, Group = DataBundleResourceGroup.All)]
	public Texture2D icon;

	[DataBundleField(StaticResource = true, Group = DataBundleResourceGroup.All)]
	public Texture2D iconNoText;

	[DataBundleField]
	public int cost;

	[DataBundleField]
	public float cooldown;

	[DataBundleField]
	public float damage;

	[DataBundleField]
	public float damageMultEachTarget;

	[DataBundleField]
	public float DOTDamage;

	[DataBundleField]
	public float DOTFrequency;

	[DataBundleField]
	public float DOTDuration;

	[DataBundleField]
	public float radius;

	[DataBundleField]
	public float speed;

	[DataBundleField]
	public float distance;

	[DataBundleField]
	public float duration;

	[DataBundleField]
	public float effectDuration;

	[DataBundleField]
	public float effectModifier;

	[DataBundleField]
	public float lifeSteal;

	[DataBundleField]
	public float flyerDamageMultiplier;

	[DataBundleField]
	public float AIMinRange;

	[DataBundleField]
	public float AIMaxRange;

	[DataBundleField]
	public int waveToUnlock;

	[DataBundleField]
	public string animName;

	[DataBundleField]
	public float spawnOffsetHorizontal;

	[DataBundleField]
	public float spawnOffsetVertical;

	[DataBundleField]
	public float infiniteUpgradeMagnitude;

	[DataBundleField]
	public float infiniteUpgradeCostCoins;

	[DataBundleField]
	public float infiniteUpgradeCostGems;

	[DataBundleSchemaFilter(typeof(USoundThemeSetSchema), false)]
	public DataBundleRecordKey soundTheme;

	[DataBundleRecordTableFilter("SoundThemeEnum")]
	[DataBundleSchemaFilter(typeof(DynamicEnum), false)]
	public DataBundleRecordKey soundEvent;

	[DataBundleSchemaFilter(typeof(AbilityLevelSchema), false)]
	public DataBundleRecordTable abilityLevelData;

	[DataBundleSchemaFilter(typeof(HeroSchema), false)]
	public DataBundleRecordKey exclusiveHero;

	[DataBundleSchemaFilter(typeof(AchievementSchema), false)]
	public DataBundleRecordKey upgradeAchievement;

	public int defenseRating;

	public AbilityLevelSchema[] levelData { get; private set; }

	public DynamicEnum SoundEvent { get; private set; }

	public string IconPath { get; private set; }

	public bool EquipLocked
	{
		get
		{
			return Singleton<Profile>.Instance.GetAbilityLevel(id) == 0;
		}
	}

	public static AbilitySchema Initialize(DataBundleRecordKey record)
	{
		AbilitySchema abilitySchema = DataBundleUtils.InitializeRecord<AbilitySchema>(record);
		if (abilitySchema != null)
		{
			abilitySchema.Initialize(record.Table);
		}
		return abilitySchema;
	}

	public void Initialize(string tableName)
	{
		SoundEvent = soundEvent.InitializeRecord<DynamicEnum>();
		IconPath = LocalizedTextureSchema.GetLocalizedPath("Icons", DataBundleRuntime.Instance.GetValue<string>(typeof(AbilitySchema), tableName, id, "icon", true));
	}

	public AbilitySchema ShallowCopy()
	{
		return (AbilitySchema)MemberwiseClone();
	}
}
