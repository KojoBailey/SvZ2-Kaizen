using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using UnityEngine;

public class CollectableManager : WeakGlobalInstance<CollectableManager>
{
	public const float kHeroCollectRange = 0.32f;

	public static WaveSpoils currentWaveSpoils = new WaveSpoils();

	private List<Collectable> mCollectables;

	private ResourceTemplate[] mResourceTemplates;

	private ResourceSchema[] mResourceSchemas;
	private Dictionary<ECollectableType, ResourceSchema> mResourceSchemaById;

	private float mTotalDropWeight;

	private float mCenterX;

	public float magnetMaxDist { get; private set; }

	public float magnetMinSpeed { get; private set; }

	public float magnetMaxSpeed { get; private set; }

	public float LeftEdge
	{
		get
		{
			if (!WeakGlobalMonoBehavior<InGameImpl>.Exists) return 0f;
			return WeakGlobalMonoBehavior<InGameImpl>.Instance.heroLeftConstraint;
		}
	}

	public float RightEdge
	{
		get
		{
			if (!WeakGlobalMonoBehavior<InGameImpl>.Exists) return 0f;
			return WeakGlobalMonoBehavior<InGameImpl>.Instance.heroRightConstraint;
		}
	}

	public CollectableManager(float leftEdge, float rightEdge, float centerX)
	{
		SetUniqueInstance(this);
		mCollectables = new List<Collectable>();
		mCenterX = centerX;
		currentWaveSpoils = new WaveSpoils();

		// Enable wealth charm effects by default.
		CharmSchema charmSchema = Singleton<CharmsDatabase>.Instance["wealth"];
		magnetMaxDist = charmSchema.magnetRange;
		magnetMinSpeed = charmSchema.magnetMinPullSpeed;
		magnetMaxSpeed = charmSchema.magnetMaxPullSpeed;

		mResourceSchemas = DataBundleUtils.InitializeRecords<ResourceSchema>("Resources");
		if (mResourceSchemas == null)
		{
			Debug.LogError("Failed to load Resources from ResourceSchema.");
		}

		mResourceSchemaById = mResourceSchemas.ToDictionary(resourceSchema => resourceSchema.type);

		mResourceTemplates = new ResourceTemplate[11];
		mTotalDropWeight = 0f;

		int i = 0;
		foreach (var resourceSchema in mResourceSchemas)
		{
			// [TODO] Need to figure out why this doesn't work in the first place... Band-aid solution for now.
			if (resourceSchema.prefab == null)
			{
				UnityEngine.Debug.LogWarning(string.Format("Unable to load prefab for {0}.", resourceSchema.type));
				
				string buffer;

				switch (resourceSchema.type)
				{
				case ECollectableType.copperCoin:
					buffer = "CoinCopperPickup";
					break;
				case ECollectableType.silverCoin:
					buffer = "CoinSilverPickup";
					break;
				case ECollectableType.goldCoin:
					buffer = "CoinGoldPickup";
					break;
				case ECollectableType.leadership:
					buffer = "LeadershipPickup";
					break;
				case ECollectableType.soul:
					buffer = "SoulsIcon";
					break;
				default:
					UnityEngine.Debug.LogError("Unsupported collectable type.");
					continue;
				}

				resourceSchema.prefab = Resources.Load<GameObject>("FX/" + buffer);

				if (resourceSchema.prefab == null)
				{
					UnityEngine.Debug.LogError("Couldn't load resource manually either.");
				}
			}

			mResourceTemplates[i] = new ResourceTemplate
			{
				amount = resourceSchema.Value,
				prefab = resourceSchema.prefab,
				dropRate = resourceSchema.dropRate,
			};

			// if (resourceSchema.type >= ECollectableType.presentA && resourceSchema.type <= ECollectableType.presentD)
			// {
			// 	string text3 = TextDBSchema.ChildKey(field, "normal", text);
			// 	string text4 = TextDBSchema.ChildKey(field, "death", text);
			// 	TextDBSchema[] array2 = array;
			// 	foreach (TextDBSchema textDBSchema in array2)
			// 	{
			// 		if (textDBSchema.key.StartsWith(text3))
			// 		{
			// 			string text5 = textDBSchema.key.Substring(text3.Length + 1);
			// 			if (IsValidPresentContent(text5) && !AlreadyHasPermenantItem(text5) && !ItemRestrictedByLevel(text5))
			// 			{
			// 				if (resourceTemplate.contents == null)
			// 				{
			// 					resourceTemplate.contents = new Dictionary<string, float>();
			// 				}
			// 				float num = float.Parse(textDBSchema.value);
			// 				resourceTemplate.contents.Add(text5, num);
			// 				resourceTemplate.contentsTotalWeight += num;
			// 			}
			// 		}
			// 		else
			// 		{
			// 			if (!textDBSchema.key.StartsWith(text4)) continue;

			// 			string text6 = textDBSchema.key.Substring(text4.Length + 1);
			// 			if (IsValidPresentContent(text6) && !AlreadyHasPermenantItem(text6) && !ItemRestrictedByLevel(text6))
			// 			{
			// 				if (resourceTemplate.postDeathContents == null)
			// 				{
			// 					resourceTemplate.postDeathContents = new Dictionary<string, float>();
			// 				}
			// 				float num2 = float.Parse(textDBSchema.value);
			// 				resourceTemplate.postDeathContents.Add(text6, num2);
			// 				resourceTemplate.postDeathContentsTotalWeight += num2;
			// 			}
			// 		}
			// 	}
			// }

			// mTotalDropWeight += resourceTemplate.weight;
		}
	}

	public void Update()
	{
		for (int num = 0; num < mCollectables.Count; num++)
		{
			if (mCollectables[num].isReadyToBeCollected && IsInRange(WeakGlobalMonoBehavior<InGameImpl>.Instance.hero, mCollectables[num]))
			{
				mCollectables[num].OnCollected();
			}
			else if (mCollectables[num].isReadyToDie)
			{
				mCollectables[num].Destroy();
			}
			else
			{
				mCollectables[num].Update();
			}

			if (mCollectables[num].isDestroyed)
			{
				mCollectables.RemoveAt(num);
				num--;
			}
		}
	}

	public void SpawnResources(ResourceDrops drops, Vector3 position)
	{
		foreach (var resourceSchema in mResourceSchemas)
		{
			if (!Singleton<Profile>.Instance.GetIsWaveUnlocked(resourceSchema.notBeforeWave) ||
				resourceSchema.dropRate != 1 &&
				UnityEngine.Random.value > resourceSchema.dropRate)
				continue;
			
			mCollectables.Add(new Collectable(
				resourceSchema,
				resourceSchema.Value,
				position,
				FindNewCollectableFinalPosition(position)
			));
		}
	}

	public void ForceSpawnResourceType(string dropTypeStr, Vector3 position)
	{
		var type = (ECollectableType)Enum.Parse(typeof(ECollectableType), dropTypeStr);
		var resourceSchema = mResourceSchemaById[type];

		mCollectables.Add(new Collectable(
			resourceSchema,
			resourceSchema.Value,
			position,
			FindNewCollectableFinalPosition(position)
		));
	}

	// public void OpenPresents(bool fromPlayerDeath)
	// {
	// 	foreach (CollectedPresent present in currentWaveSpoils.presents)
	// 	{
	// 		if (present.hasBeenOpened)
	// 		{
	// 			continue;
	// 		}
	// 		present.hasBeenOpened = true;
	// 		float num = 0f;
	// 		Dictionary<string, float> dictionary = null;
	// 		if (fromPlayerDeath)
	// 		{
	// 			num = UnityEngine.Random.Range(0f, mResourceTemplates[(int)present.type].postDeathContentsTotalWeight - float.Epsilon);
	// 			dictionary = mResourceTemplates[(int)present.type].postDeathContents;
	// 		}
	// 		else
	// 		{
	// 			num = UnityEngine.Random.Range(0f, mResourceTemplates[(int)present.type].contentsTotalWeight - float.Epsilon);
	// 			dictionary = mResourceTemplates[(int)present.type].contents;
	// 		}
	// 		if (dictionary != null)
	// 		{
	// 			try
	// 			{
	// 				foreach (KeyValuePair<string, float> item in dictionary)
	// 				{
	// 					num -= item.Value;
	// 					if (num < 0f)
	// 					{
	// 						present.contents = item.Key;
	// 						RemovePermenantItemFromFuturePresents(item.Key);
	// 						break;
	// 					}
	// 				}
	// 			}
	// 			catch (Exception)
	// 			{
	// 			}
	// 		}
	// 		Singleton<PlayStatistics>.Instance.data.AddLoot(present.contents, 1, present.type);
	// 		int valueInCoins = CashIn.GetValueInCoins(present.contents, 1);
	// 		Singleton<PlayStatistics>.Instance.data.totalLootDroppedValue += valueInCoins;
	// 		switch (CashIn.GetType(present.contents))
	// 		{
	// 		case CashIn.ItemType.Coins:
	// 			Singleton<PlayStatistics>.Instance.data.droppedCoins++;
	// 			break;
	// 		case CashIn.ItemType.Gems:
	// 			Singleton<PlayStatistics>.Instance.data.droppedGems++;
	// 			break;
	// 		case CashIn.ItemType.Souls:
	// 			Singleton<PlayStatistics>.Instance.data.droppedSouls++;
	// 			break;
	// 		case CashIn.ItemType.Helper:
	// 		case CashIn.ItemType.Ability:
	// 			Singleton<PlayStatistics>.Instance.data.droppedUnlockables++;
	// 			break;
	// 		default:
	// 			Singleton<PlayStatistics>.Instance.data.droppedOtherValue += valueInCoins;
	// 			break;
	// 		}
	// 	}
	// }

	public void GiveResource(ECollectableType type, int amount)
	{
		switch (type)
		{
		case ECollectableType.copperCoin:
		case ECollectableType.silverCoin:
		case ECollectableType.goldCoin:
			if (Singleton<Profile>.Instance.GetUpgradeLevel("CoinDouble") > 0)
			{
				currentWaveSpoils.coins += amount * 2;
			}
			else
			{
				currentWaveSpoils.coins += amount;
			}
			return;
		case ECollectableType.soul:
			WeakGlobalInstance<Souls>.Instance.souls += amount;
			return;
		case ECollectableType.leadership:
			WeakGlobalInstance<Leadership>.Instance.resources += amount;
			return;
		case ECollectableType.pachinkoBall:
			currentWaveSpoils.pachinkoBalls += amount;
			return;
		// case ECollectableType.presentA:
		// case ECollectableType.presentB:
		// case ECollectableType.presentC:
		// {
		// 	CollectedPresent collectedPresent = new CollectedPresent();
		// 	collectedPresent.type = type;
		// 	currentWaveSpoils.presents.Add(collectedPresent);
		// 	break;
		// }
		// case ECollectableType.presentD:
		// 	break;
		}
	}

	public void RecordLootDropped(ECollectableType type, int amount)
	{
		switch (type)
		{
		case ECollectableType.copperCoin:
		case ECollectableType.silverCoin:
		case ECollectableType.goldCoin:
			if (Singleton<Profile>.Instance.GetUpgradeLevel("CoinDouble") > 0)
			{
				amount *= 2;
			}
			Singleton<PlayStatistics>.Instance.data.totalLootDroppedValue += amount;
			Singleton<PlayStatistics>.Instance.data.droppedCoins += amount;
			break;
		case ECollectableType.soul:
			if (Singleton<Profile>.Instance.GetUpgradeLevel("SoulsDouble") > 0)
			{
				amount *= 2;
			}
			Singleton<PlayStatistics>.Instance.data.totalLootDroppedValue += CashIn.GetValueInCoins("souls", amount);
			Singleton<PlayStatistics>.Instance.data.droppedSouls += amount;
			break;
		case ECollectableType.leadership:
			amount = CashIn.GetValueInCoins("leadership", amount);
			Singleton<PlayStatistics>.Instance.data.totalLootDroppedValue += amount;
			Singleton<PlayStatistics>.Instance.data.droppedOtherValue += amount;
			break;
		}
	}

	// public void GiveSpecificPresent(string id, int amount)
	// {
	// 	if (amount > 0)
	// 	{
	// 		CollectedPresent collectedPresent = new CollectedPresent();
	// 		collectedPresent.type = ECollectableType.presentD;
	// 		collectedPresent.contents = id;
	// 		collectedPresent.hasBeenOpened = true;
	// 		collectedPresent.amount = amount;
	// 		currentWaveSpoils.presents.Add(collectedPresent);
	// 		Singleton<PlayStatistics>.Instance.data.AddLoot(id, amount);
	// 	}
	// }

	public void BankAllResources()
	{
		Singleton<Profile>.Instance.ForceActiveSaveData(false);
		Singleton<Profile>.Instance.AddCoins(currentWaveSpoils.coins, "Collectables");
		// Singleton<Profile>.Instance.souls += currentWaveSpoils.souls;
		Singleton<Profile>.Instance.pachinkoBalls += currentWaveSpoils.pachinkoBalls;
		// OpenPresents(false);
		// foreach (CollectedPresent present in currentWaveSpoils.presents)
		// {
		// 	int amount = present.amount;
		// 	if (CashIn.From(present.contents, amount, "Present"))
		// 	{
		// 	}
		// }
		Singleton<Profile>.Instance.ForceActiveSaveData(true);
	}

	private bool IsInRange(Hero hero, Collectable obj)
	{
		return (hero.position.z > obj.position.z && !obj.wasAtLeftOfHero) || (hero.position.z < obj.position.z && obj.wasAtLeftOfHero) || Mathf.Abs(hero.position.z - obj.position.z) <= 0.32f;
	}

	public Vector3 FindNewCollectableFinalPosition(Vector3 spawnPosition)
	{
		Vector3 zero = Vector3.zero;
		zero.x = mCenterX + 1.15f + UnityEngine.Random.Range(-0.02f, 0.02f);
		zero.z = spawnPosition.z + UnityEngine.Random.Range(-0.55f, 0.55f);
		zero.z = Mathf.Clamp(zero.z, LeftEdge, RightEdge);
		zero.y = WeakGlobalInstance<RailManager>.Instance.GetY(zero.z) + 0.26f;
		return zero;
	}

	private bool AlreadyHasPermenantItem(string theItemName)
	{
		if (Singleton<AbilitiesDatabase>.Instance.Contains(theItemName))
		{
			if (Singleton<Profile>.Instance.GetAbilityLevel(theItemName) > 0)
			{
				return true;
			}
		}
		else if (Singleton<HelpersDatabase>.Instance.Contains(theItemName))
		{
			return true;
		}
		return false;
	}

	private bool ItemRestrictedByLevel(string theItemName)
	{
		bool flag = false;
		string[] allIDs = Singleton<HelpersDatabase>.Instance.allIDs;
		foreach (string strA in allIDs)
		{
			if (string.Compare(strA, theItemName, true) == 0)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		HelperSchema helperSchema = Singleton<HelpersDatabase>.Instance[theItemName];
		if (helperSchema != null)
		{
			int availableAtWave = helperSchema.availableAtWave;
			if (Singleton<Profile>.Instance.highestUnlockedWave >= availableAtWave)
			{
				return false;
			}
		}
		return true;
	}

	private void RemovePermenantItemFromFuturePresents(string theItemName)
	{
		if (!ArrayContainsString(Singleton<HelpersDatabase>.Instance.allIDs, theItemName) && !ArrayContainsString(Singleton<AbilitiesDatabase>.Instance.allIDs, theItemName))
		{
			return;
		}
		ResourceTemplate[] array = mResourceTemplates;
		foreach (ResourceTemplate resourceTemplate in array)
		{
			if (resourceTemplate.contents != null && resourceTemplate.contents.ContainsKey(theItemName))
			{
				resourceTemplate.contentsTotalWeight -= resourceTemplate.contents[theItemName];
				resourceTemplate.contents.Remove(theItemName);
			}
			if (resourceTemplate.postDeathContents != null && resourceTemplate.postDeathContents.ContainsKey(theItemName))
			{
				resourceTemplate.postDeathContentsTotalWeight -= resourceTemplate.postDeathContents[theItemName];
				resourceTemplate.postDeathContents.Remove(theItemName);
			}
		}
	}

	private bool ArrayContainsString(string[] theStringArray, string theStringToFind)
	{
		foreach (string text in theStringArray)
		{
			if (text.Equals(theStringToFind, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsValidPresentContent(string itemID)
	{
		return true;
	}
}
