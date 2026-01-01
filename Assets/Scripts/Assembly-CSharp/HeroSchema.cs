using UnityEngine;

[DataBundleClass(Category = "Design")]
public class HeroSchema
{
	[DataBundleKey]
	public string id;

	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	[DataBundleRecordTableFilter("LocalizedStrings")]
	public DataBundleRecordKey displayName;

	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	[DataBundleRecordTableFilter("LocalizedStrings")]
	public DataBundleRecordKey desc;

	[DataBundleRecordTableFilter("LocalizedStrings")]
	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	public DataBundleRecordKey store_levelup;

	[DataBundleSchemaFilter(typeof(CharacterSchema), false)]
	public DataBundleRecordKey resources;

	[DataBundleField(StaticResource = true, Group = DataBundleResourceGroup.All)]
	public Texture2D icon;

	[DataBundleField(StaticResource = true, Group = DataBundleResourceGroup.FrontEnd)]
	public Texture2D storePortrait;

	[DataBundleSchemaFilter(typeof(WeaponSchema), false)]
	[DataBundleRecordTableFilter("Weapons")]
	public DataBundleRecordKey meleeWeapon;

	[DataBundleSchemaFilter(typeof(WeaponSchema), false)]
	[DataBundleRecordTableFilter("Weapons")]
	public DataBundleRecordKey rangedWeapon;

	[DataBundleSchemaFilter(typeof(AbilitiesListSchema), false)]
	public DataBundleRecordTable PotentialAbilties;

	[DataBundleSchemaFilter(typeof(ArmorLevelSchema), false)]
	public DataBundleRecordTable armorLevels;

	[DataBundleField]
	public int health;

	[DataBundleField]
	public float healthRecovery;

	[DataBundleField]
	public float speed;

	[DataBundleField]
	public int allySlots;

	[DataBundleField]
	public int abilitySlots;

	[DataBundleField]
	public bool canMeleeFliers;

	[DataBundleField]
	public int waveToUnlock;

	[DataBundleField]
	public bool purchaseToUnlock;

	[DataBundleField]
	public bool hideUntilUnlocked;

	[DataBundleField]
	public bool overrideRequirements;

	[DataBundleField]
	public bool disabled;

	[DataBundleSchemaFilter(typeof(AchievementSchema), false)]
	public DataBundleRecordKey unlockAchievement;

	[DataBundleSchemaFilter(typeof(AchievementSchema), false)]
	public DataBundleRecordKey upgradeAchievement;

	[DataBundleSchemaFilter(typeof(AchievementSchema), false)]
	public DataBundleRecordKey meleeAchievement;

	[DataBundleSchemaFilter(typeof(AchievementSchema), false)]
	public DataBundleRecordKey rangedAchievement;

	[DataBundleSchemaFilter(typeof(AchievementSchema), false)]
	public DataBundleRecordKey leadershipAchievement;

	[DataBundleDefaultValue(1.3f)]
	public float backPedalTime;

	[DataBundleField]
	public bool blocksHeroMovement;

	[DataBundleField]
	public int defenseRating;

	public AbilitySchema[] Abilities { get; private set; }

	public ArmorLevelSchema[] ArmorLevels { get; set; }

	public WeaponSchema MeleeWeapon { get; private set; }

	public WeaponSchema RangedWeapon { get; private set; }

	public bool Locked
	{
		get { return !Purchased || Singleton<Profile>.Instance.highestUnlockedWave < waveToUnlock; }
	}

	public bool Purchased
	{
		get { return !purchaseToUnlock || Singleton<Profile>.Instance.GetHeroPurchased(id); }
	}

	public string IconPath { get; private set; }

	public HeroStarsSchema HeroStarsSchema { get; set; }

	public ArmorLevelSchema GetArmorLevel(int level)
	{
		if (ArmorLevels != null && ArmorLevels.Length > 0)
		{
			return ArmorLevels[Mathf.Clamp(level - 1, 0, ArmorLevels.Length - 1)];
		}
		return null;
	}

	public void Initialize(string tableName)
	{
		if (PotentialAbilties != null)
		{
			AbilitiesListSchema[] array = PotentialAbilties.InitializeRecords<AbilitiesListSchema>();
			Abilities = new AbilitySchema[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				Abilities[i] = Singleton<AbilitiesDatabase>.Instance[array[i].ability.Key];
			}
		}

		MeleeWeapon = meleeWeapon.InitializeRecord<WeaponSchema>();
		if (MeleeWeapon != null)
		{
			MeleeWeapon.Initialize();
		}

		RangedWeapon = rangedWeapon.InitializeRecord<WeaponSchema>();
		if (RangedWeapon != null)
		{
			RangedWeapon.Initialize();
		}

		if (!DataBundleRecordTable.IsNullOrEmpty(armorLevels))
		{
			ArmorLevels = armorLevels.InitializeRecords<ArmorLevelSchema>();
			ArmorLevelSchema[] array2 = ArmorLevels;
			foreach (ArmorLevelSchema armorLevelSchema in array2)
			{
				armorLevelSchema.Initialize(armorLevels.RecordTable);
			}
		}

		IconPath = DataBundleRuntime.Instance.GetValue<string>(typeof(HeroSchema), tableName, id, "icon", true);

		HeroStarsSchema = DataBundleRuntime.Instance.InitializeRecord<HeroStarsSchema>("HeroStars", id);
	}

	public void LoadCachedResources(bool frontEnd)
	{
		if (MeleeWeapon != null)
		{
			MeleeWeapon.LoadCachedResources(0);
			if (frontEnd)
			{
				ResourceCache.GetCachedResource(MeleeWeapon.IconPath, 1);
			}
		}

		if (RangedWeapon != null)
		{
			RangedWeapon.LoadCachedResources(0);
			if (frontEnd)
			{
				ResourceCache.GetCachedResource(RangedWeapon.IconPath, 1);
			}
		}

		ArmorLevelSchema armorLevel2 = GetArmorLevel(1);
		if (armorLevel2 != null)
		{
			string tableRecordKey = DataBundleRuntime.TableRecordKey(armorLevels.RecordTable, armorLevel2.level.ToString());
			ResourceCache.LoadCachedResources(armorLevel2, tableRecordKey);
			if (frontEnd)
			{
				string iconPath3 = armorLevel2.IconPath;
				ResourceCache.GetCachedResource(iconPath3, 1);
			}
		}
	}

	public void UnloadCachedResources()
	{
		if (MeleeWeapon != null)
		{
			string tableRecordKey = DataBundleRuntime.TableRecordKey("Weapons", MeleeWeapon.id);
			ResourceCache.UnloadCachedResources(MeleeWeapon, tableRecordKey);
			ResourceCache.UnCache(MeleeWeapon.IconPath);
		}

		if (RangedWeapon != null)
		{
			string tableRecordKey = DataBundleRuntime.TableRecordKey("Weapons", RangedWeapon.id);
			ResourceCache.UnloadCachedResources(MeleeWeapon, tableRecordKey);
			ResourceCache.UnCache(RangedWeapon.IconPath);
		}
		
		if (ArmorLevels != null)
		{
			ArmorLevelSchema[] array = ArmorLevels;
			foreach (ArmorLevelSchema armorLevelSchema in array)
			{
				string tableRecordKey = DataBundleRuntime.TableRecordKey(armorLevels.RecordTable, armorLevelSchema.level.ToString());
				ResourceCache.UnloadCachedResources(armorLevelSchema, tableRecordKey);
			}
			ArmorLevelSchema armorLevel2 = GetArmorLevel(0);
			if (armorLevel2 != null)
			{
				string iconPath3 = armorLevel2.IconPath;
				ResourceCache.UnCache(iconPath3);
			}
		}
	}
}
