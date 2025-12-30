using UnityEngine;

[DataBundleClass(Category = "Design")]
public class HeroSchema
{
	public delegate float InfiniteUpgradeAccessor(HeroSchema s);

	public delegate float LevelValueAccessor(HeroLevelSchema ls);

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

	[DataBundleSchemaFilter(typeof(CharacterSchema), false)]
	public DataBundleRecordKey resourcesGhost;

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

	public void LoadCachedResources(int meleeLevel, int rangedLevel, int armorLevel, bool frontEnd)
	{
		if (MeleeWeapon != null)
		{
			MeleeWeapon.LoadCachedResources(meleeLevel);
			if (frontEnd)
			{
				WeaponLevelSchema weaponLevelSchema = MeleeWeapon.Levels[Mathf.Clamp(meleeLevel, 0, MeleeWeapon.Levels.Length - 1)];
				string iconPath = weaponLevelSchema.IconPath;
				ResourceCache.GetCachedResource(iconPath, 1);
			}
		}
		if (RangedWeapon != null)
		{
			RangedWeapon.LoadCachedResources(rangedLevel);
			if (frontEnd)
			{
				WeaponLevelSchema weaponLevelSchema2 = RangedWeapon.Levels[Mathf.Clamp(rangedLevel, 0, RangedWeapon.Levels.Length - 1)];
				string iconPath2 = weaponLevelSchema2.IconPath;
				ResourceCache.GetCachedResource(iconPath2, 1);
			}
		}
		ArmorLevelSchema armorLevel2 = GetArmorLevel(armorLevel);
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

	public void UnloadCachedResources(int meleeLevel, int rangedLevel, int armorLevel)
	{
		if (MeleeWeapon != null)
		{
			MeleeWeapon.UnloadCachedResources();
			WeaponLevelSchema weaponLevelSchema = MeleeWeapon.Levels[Mathf.Clamp(meleeLevel, 0, MeleeWeapon.Levels.Length - 1)];
			string iconPath = weaponLevelSchema.IconPath;
			ResourceCache.UnCache(iconPath);
		}
		if (RangedWeapon != null)
		{
			RangedWeapon.UnloadCachedResources();
			WeaponLevelSchema weaponLevelSchema2 = RangedWeapon.Levels[Mathf.Clamp(rangedLevel, 0, RangedWeapon.Levels.Length - 1)];
			string iconPath2 = weaponLevelSchema2.IconPath;
			ResourceCache.UnCache(iconPath2);
		}
		if (ArmorLevels != null)
		{
			ArmorLevelSchema[] array = ArmorLevels;
			foreach (ArmorLevelSchema armorLevelSchema in array)
			{
				string tableRecordKey = DataBundleRuntime.TableRecordKey(armorLevels.RecordTable, armorLevelSchema.level.ToString());
				ResourceCache.UnloadCachedResources(armorLevelSchema, tableRecordKey);
			}
			ArmorLevelSchema armorLevel2 = GetArmorLevel(armorLevel);
			if (armorLevel2 != null)
			{
				string iconPath3 = armorLevel2.IconPath;
				ResourceCache.UnCache(iconPath3);
			}
		}
	}
}
