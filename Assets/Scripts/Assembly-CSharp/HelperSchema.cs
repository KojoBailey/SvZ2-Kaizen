using System.ComponentModel;
using UnityEngine;

[DataBundleClass(Category = "Design")]
public class HelperSchema
{
	[DataBundleKey]
	public string id;

	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	[DataBundleRecordTableFilter("LocalizedStrings")]
	public DataBundleRecordKey displayName;

	[DataBundleRecordTableFilter("LocalizedStrings")]
	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	public DataBundleRecordKey desc;

	[DataBundleRecordTableFilter("Character")]
	[DataBundleSchemaFilter(typeof(CharacterSchema), false, DontFollowRecordLink = true)]
	public DataBundleRecordKey resources;

	[DataBundleField(StaticResource = true, Group = (DataBundleResourceGroup.FrontEnd | DataBundleResourceGroup.Preview))]
	public Texture2D HUDIcon;

	[DataBundleField(StaticResource = true, Group = (DataBundleResourceGroup.FrontEnd | DataBundleResourceGroup.Preview))]
	public Texture2D championIcon;

	[DataBundleField(StaticResource = true, Group = (DataBundleResourceGroup.FrontEnd | DataBundleResourceGroup.Preview))]
	public Texture2D lockedIcon;

	[DataBundleField(StaticResource = true, Group = (DataBundleResourceGroup.FrontEnd | DataBundleResourceGroup.Preview))]
	public Texture2D platinumIcon;

	[DataBundleField(StaticResource = true, Group = (DataBundleResourceGroup.InGame | DataBundleResourceGroup.Preview))]
	public GameObject meleeWeaponPrefab;

	[DataBundleField(StaticResource = true, Group = (DataBundleResourceGroup.InGame | DataBundleResourceGroup.Preview))]
	public GameObject rangedWeaponPrefab;

	[DataBundleRecordTableFilter("Lane")]
	[DataBundleSchemaFilter(typeof(DynamicEnum), false)]
	public DataBundleRecordKey lane;

	[DataBundleField]
	public int waveToUnlock;

	[DataBundleField]
	public int availableAtWave;

	[DataBundleField]
	public float resourcesCost;

	[DataBundleField]
	public float cooldownTimer;

	[DataBundleField]
	public float health;

	[DataBundleField]
	public float armor;

	[DataBundleField]
	public float speed;

	[DataBundleField]
	public float attackFrequency;

	[DataBundleField]
	public float meleeRange;

	[DataBundleField]
	public float meleeDamage;

	[DataBundleField]
	public bool isArmorPiercing;

	[DataBundleSchemaFilter(typeof(DynamicEnum), false)]
	[DataBundleRecordTableFilter("Projectile")]
	public DataBundleRecordKey projectile;

	[DataBundleField]
	public float bowRange;

	[DataBundleField]
	public float bowDamage;

	[DataBundleField]
	[DataBundleDefaultValue(true)]
	public bool knockbackable;

	[DataBundleField]
	public int knockbackPower;

	[DataBundleField]
	public int knockbackResistance;

	[DataBundleSchemaFilter(typeof(HelperSchema), false)]
	public DataBundleRecordKey upgradeAlliesFrom;

	[DataBundleSchemaFilter(typeof(HelperSchema), false)]
	public DataBundleRecordKey upgradeAlliesTo;

	[DataBundleRecordTableFilter("LocalizedStrings")]
	[DataBundleSchemaFilter(typeof(TaggedString), false)]
	public DataBundleRecordKey specialUnlockText;

	[DataBundleSchemaFilter(typeof(BuffSchema), false)]
	public DataBundleRecordKey buffRecordKey;

	[DataBundleField]
	public bool usesBladeWeapon;

	[DataBundleField]
	public bool hideInStore;

	[DataBundleField]
	public bool hideInEquip;

	[DataBundleField]
	public bool unique;

	[DataBundleField]
	public bool exploseOnMelee;

	[DataBundleField]
	public bool enemyIgnoresMe;

	[DataBundleField]
	public bool canMeleeFliers;

	[DataBundleField]
	public bool canMeleeProjectiles;

	[DataBundleField]
	public bool isMount;

	[DataBundleField]
	public bool isMounted;

	[DataBundleField]
	public bool blocksHeroMovement;

	[DataBundleField]
	public bool canBeEaten;

	[DataBundleField]
	public bool isCharger;

	[DataBundleField]
	public int award;

	[DataBundleField]
	public int resourceDropMin;

	[DataBundleField]
	public int resourceDropMax;

	[DataBundleField]
	public int valueInCoins;

	[DataBundleSchemaFilter(typeof(HelperSchema), false)]
	public DataBundleRecordKey levelMatchOtherHelper;

	[DataBundleSchemaFilter(typeof(HeroSchema), false)]
	public DataBundleRecordKey requiredHero;

	public int goldenHelperProbability;

	public float goldenHelperHealthMultiplier;

	public float goldenHelperDamageMultiplier;

	public float goldenHelperSizeScale;

	public string goldenHelperCostToUnlock;

	[DataBundleSchemaFilter(typeof(AchievementSchema), false)]
	public DataBundleRecordKey upgradeAchievement;

	public string unlockPlayhavenRequest;

	public int summonIndex;

	public int defenseRating;

	public bool Locked
	{
		get { return Singleton<Profile>.Instance.highestUnlockedWave < waveToUnlock; }
	}

	public string IconPath { get; private set; }

	public string LockedIconPath { get; private set; }

	public string PlatinumIconPath { get; private set; }

	public string ChampionIconPath { get; private set; }

	public BuffSchema buffSchema { get; private set; }

	public Texture2D TryGetChampionIcon()
	{
		if (championIcon != null)
		{
			return championIcon;
		}

		if (!string.IsNullOrEmpty(ChampionIconPath))
		{
			return LoadIcon(ChampionIconPath);
		}

		return null;
	}

	public Texture2D TryGetLockedIcon()
	{
		if (lockedIcon != null)
		{
			return lockedIcon;
		}

		if (!string.IsNullOrEmpty(LockedIconPath))
		{
			return LoadIcon(LockedIconPath);
		}

		if (HUDIcon != null)
		{
			return HUDIcon;
		}

		if (!string.IsNullOrEmpty(IconPath))
		{
			return LoadIcon(IconPath);
		}

		return null;
	}

	public Texture2D TryGetPlatinumIcon()
	{
		if (platinumIcon != null)
		{
			return platinumIcon;
		}

		if (!string.IsNullOrEmpty(PlatinumIconPath))
		{
			return LoadIcon(PlatinumIconPath);
		}

		if (HUDIcon != null)
		{
			return HUDIcon;
		}

		if (!string.IsNullOrEmpty(IconPath))
		{
			return LoadIcon(IconPath);
		}

		return null;
	}

	public Texture2D TryGetHUDIcon()
	{
		if (HUDIcon != null)
		{
			return HUDIcon;
		}

		if (!string.IsNullOrEmpty(IconPath))
		{
			return LoadIcon(IconPath);
		}

		return null;
	}

	public Texture2D LoadIcon(string path)
	{
		SharedResourceLoader.SharedResource cachedResource = ResourceCache.GetCachedResource(path, 1);
		if (cachedResource != null)
		{
			return cachedResource.Resource as Texture2D;
		}
		return null;
	}

	public void Initialize(string tableName)
	{
		IconPath = DataBundleRuntime.Instance.GetValue<string>(typeof(HelperSchema), tableName, id, "HUDIcon", true);
		LockedIconPath = DataBundleRuntime.Instance.GetValue<string>(typeof(HelperSchema), tableName, id, "lockedIcon", true);
		PlatinumIconPath = DataBundleRuntime.Instance.GetValue<string>(typeof(HelperSchema), tableName, id, "platinumIcon", true);
		ChampionIconPath = DataBundleRuntime.Instance.GetValue<string>(typeof(HelperSchema), tableName, id, "championIcon", true);

		if (!string.IsNullOrEmpty(buffRecordKey.Key))
		{
			buffSchema = DataBundleRuntime.Instance.InitializeRecord<BuffSchema>(buffRecordKey);
		}
	}
}
