using System.Collections.Generic;

public class StoreAvailability_Helpers
{
	public static void Get(List<StoreData.Item> items)
	{
		HelperSchema[] allHelpers = Singleton<HelpersDatabase>.Instance.AllHelpers;
		foreach (HelperSchema helperSchema in allHelpers)
		{
			Get(helperSchema, false, items);
		}
	}

	public static void Get(string helperID, bool force, List<StoreData.Item> items)
	{
		Get(Singleton<HelpersDatabase>.Instance[helperID], force, items);
	}

	public static void Get(HelperSchema helperSchema, bool force, List<StoreData.Item> items)
	{
		if (helperSchema.hideInStore && !force) return;

		string displayName = StringUtils.GetStringFromStringRef(helperSchema.displayName);
		StoreData.Item item = new StoreData.Item
		{
			id = helperSchema.id,
			isUpgradable = false,
			locked = helperSchema.Locked,
			unlockAtWave = helperSchema.waveToUnlock,
			availableAtWave = helperSchema.availableAtWave,
			isNew = Singleton<Profile>.Instance.highestUnlockedWave == helperSchema.waveToUnlock && (!SingletonMonoBehaviour<StoreMenuImpl>.Exists || !SingletonMonoBehaviour<StoreMenuImpl>.Instance.HasViewedNewItem(helperSchema.id)),
			title = displayName,
			unlockTitle = displayName,
		};
		item.details.Name = displayName;
		
		item.details.AddStat("health_stats", helperSchema.health.ToString());
		string meleeDamage = helperSchema.meleeDamage.ToString();
		item.details.AddStat("strength_stats", !string.Equals(item.id, "Mount_Balanced") ? meleeDamage : (meleeDamage + "%"));
		string bowDamage = helperSchema.bowDamage.ToString();
		item.details.AddStat("strength_stats", !string.Equals(item.id, "Mount_Balanced") ? bowDamage : (bowDamage + "%"));

		string lockedIconPath = helperSchema.LockedIconPath;
		if (item.locked && !string.IsNullOrEmpty(lockedIconPath))
		{
			item.LoadIcon(lockedIconPath);
		}
		else
		{
			item.LoadIcon(helperSchema.IconPath);
		}

		if (item.locked)
		{
			item.unlockCondition = string.Format(
				StringUtils.GetStringFromStringRef("LocalizedStrings", "store_unlockatwave"), item.unlockAtWave);
		}

		item.details.AddSmallDescription(StringUtils.GetStringFromStringRef(helperSchema.desc));

		items.Add(item);
	}

	private static void FillInfinityPath(HelperSchema helperSchema, List<StoreData.Item> items) {}

	public static bool IsAnyChampionForSale()
	{
		HelperSchema[] allHelpers = Singleton<HelpersDatabase>.Instance.AllHelpers;
		foreach (HelperSchema helperSchema in allHelpers)
		{
			if (ChampionAvailableForSale(helperSchema))
			{
				return true;
			}
		}
		return false;
	}

	public static void GetChampions(List<StoreData.Item> items)
	{
		HelperSchema[] allHelpers = Singleton<HelpersDatabase>.Instance.AllHelpers;
		foreach (HelperSchema helperSchema in allHelpers)
		{
			if (ChampionAvailableForSale(helperSchema))
			{
				string helperID = helperSchema.id;
				StoreData.Item item = new StoreData.Item(delegate
				{
					UnlockChampion(helperID);
				});
				item.cost = new Cost(helperSchema.goldenHelperCostToUnlock, SaleItemSchema.FindActiveSaleForItem(helperID + "_Champion"));
				item.icon = helperSchema.TryGetChampionIcon();
				item.title = string.Format(StringUtils.GetStringFromStringRef("LocalizedStrings", "UnlockChampion_Title"), StringUtils.GetStringFromStringRef(helperSchema.displayName));
				item.details.AddDescription(string.Format(StringUtils.GetStringFromStringRef("LocalizedStrings", "UnlockChampion_Desc"), StringUtils.GetStringFromStringRef(helperSchema.displayName)));
				item.locked = helperSchema.Locked;
				item.unlockAtWave = helperSchema.waveToUnlock;
				item.analyticsEvent = "ChampionPurchased";
				item.analyticsParams = new Dictionary<string, object>();
				item.analyticsParams["ItemName"] = "Golden_" + helperID;
				items.Add(item);
			}
		}
	}

	private static bool ChampionAvailableForSale(HelperSchema helperSchema)
	{
		return !helperSchema.hideInStore && !string.IsNullOrEmpty(helperSchema.goldenHelperCostToUnlock) && !Singleton<Profile>.Instance.GetGoldenHelperUnlocked(helperSchema.id);
	}

	private static void LevelUpHelper(Cost cost, string helperID, bool isLastUpgrade)
	{
		int val = Singleton<Profile>.Instance.GetHelperLevel(helperID) + 1;
		Singleton<Profile>.Instance.SetHelperLevel(helperID, val);
		if (string.Equals(helperID, "Mount_Balanced"))
		{
			SingletonSpawningMonoBehaviour<GluiAgent_CentralDispatch>.Instance.SendSimpleOrder("Store", "HeroBalanced", GluiAgentBase.Order.Redraw, null, null);
		}
		else
		{
			SingletonSpawningMonoBehaviour<GluiAgent_CentralDispatch>.Instance.SendSimpleOrder("Store", "Allies", GluiAgentBase.Order.Redraw, null, null);
		}
		HelperSchema helperSchema = Singleton<HelpersDatabase>.Instance[helperID];
		if (helperSchema != null)
		{
			Singleton<Analytics>.Instance.LogEvent("AbilityUpgradePurchased", Analytics.Param("ItemName", helperID), Analytics.Param("UpgradeLevel", Singleton<Profile>.Instance.GetHelperLevel(helperID)), Analytics.Param("Cost", cost.price), Analytics.Param("Currency", cost.currencyAnalyticCode), Analytics.Param("PlayerLevel", Singleton<Profile>.Instance.playerLevel));
			if (isLastUpgrade && helperSchema.upgradeAchievement != null && !string.IsNullOrEmpty(helperSchema.upgradeAchievement.Key))
			{
				Singleton<Achievements>.Instance.SetAchievementCompletionCount(helperSchema.upgradeAchievement.Key, 1);
				Singleton<Achievements>.Instance.CheckMetaAchievement("AllUpgrades");
			}
		}
		Singleton<Profile>.Instance.Save();
	}

	private static void UnlockChampion(string helperID)
	{
		Singleton<Profile>.Instance.SetGoldenHelperUnlocked(helperID, true);
		if (Singleton<Profile>.Instance.GetHelperLevel(helperID) == 0)
		{
			Singleton<Profile>.Instance.SetHelperLevel(helperID, 1);
		}
		SingletonSpawningMonoBehaviour<GluiAgent_CentralDispatch>.Instance.SendSimpleOrder("Store", "Champions", GluiAgentBase.Order.Redraw, null, null);
		SingletonSpawningMonoBehaviour<GluiAgent_CentralDispatch>.Instance.SendSimpleOrder("Store", "Allies", GluiAgentBase.Order.Redraw, null, null);
		Singleton<Profile>.Instance.Save();
	}
}
