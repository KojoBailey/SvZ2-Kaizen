using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.XPath;
using UnityEngine;

public class WaveManager : WeakGlobalInstance<WaveManager>
{
	public enum WaveType
	{
		Wave_SinglePlayer = 0,
		Wave_Bonus = 1,
		Wave_Multiplayer = 2,
		Wave_DailyChallenge = 3
	}

	public class WaveRecyclingMultipliers
	{
		public float enemiesHealth = 1f;

		public float enemiesDamages = 1f;

		public float drops = 1f;
	}

	public delegate void OnLegionCallback();

	private string specialBossName = string.Empty;

	private int mWaveIndex;

	private WaveType mWaveType;

	private BoxCollider mEnemiesSpawnArea;

	private float mZTarget;

	private WaveSchema waveRootData;

	private Dictionary<string, CharacterData> mCharacterDataPool = new Dictionary<string, CharacterData>();

	private DataBundleTableHandle<HelperEnemySwapSchema> mCorruptionSwapData;

	private DataBundleTableHandle<EnemySwapSchema> mDeathSwapData;

	private WaveRecyclingMultipliers mLevelMultipliers = new WaveRecyclingMultipliers();

	private const float MinimumWaveDelay = 2.0f;

	private class QueueItem
	{
		public string enemy;
		public float delay;

		public QueueItem(string _enemy, float _delay)
		{
			enemy = _enemy;
			delay = _delay;
		}
	}
	private Queue<QueueItem> mWaveQueue = new Queue<QueueItem>();

	private int mNextCommandToRun;

	private float mSpawnDelayTimer = 0f;

	private bool mSkipNextLegion;

	private string mTutorial = string.Empty;

	private int mTotalNumEnemies;

	private int mSpawnedEnemiesSoFar;

	private int mEnemiesKilledSoFar;

	private int mEnemiesToKillBeforeNextWave;

	private int mEnemiesQueuedSoFar;

	private List<string> mAllDifferentEnemies = new List<string>();

	private int mVillageArchersLevel;

	private int mBellLevel;

	public bool isDone
	{
		get
		{
			return mNextCommandToRun >= waveRootData.Commands.Length && specialBossName == string.Empty;
		}
	}

	public bool isWaveComplete
	{
		get
		{
			return isDone && mEnemiesKilledSoFar >= mTotalNumEnemies;
		}
	}

	public WaveSchema Data
	{
		get
		{
			return waveRootData;
		}
	}

	public DataBundleRecordKey WaveRecordKey { get; private set; }

	public int totalEnemies
	{
		get
		{
			return mTotalNumEnemies;
		}
		set
		{
			mTotalNumEnemies = value;
		}
	}

	public int enemiesKilledSoFar
	{
		get { return mEnemiesKilledSoFar; }
	}

	public List<string> allDifferentEnemies
	{
		get
		{
			return mAllDifferentEnemies;
		}
	}

	public bool skipNextLegion
	{
		get
		{
			return mSkipNextLegion;
		}
		set
		{
			mSkipNextLegion = value;
		}
	}

	public BoxCollider enemiesSpawnArea
	{
		get
		{
			return mEnemiesSpawnArea;
		}
	}

	public int waveLevel
	{
		get
		{
			return mWaveIndex;
		}
	}

	public string tutorial
	{
		get
		{
			return mTutorial;
		}
	}

	public WaveRecyclingMultipliers multipliers
	{
		get
		{
			return mLevelMultipliers;
		}
	}

	public int villageArchersLevel
	{
		get
		{
			return mVillageArchersLevel;
		}
	}

	public int bellLevel
	{
		get
		{
			return mBellLevel;
		}
	}

	public HelperEnemySwapSchema[] helperEnemySwaps
	{
		get
		{
			return mCorruptionSwapData.Data;
		}
		set
		{
		}
	}

	private GameObject corruptionSpawnEffect { get; set; }

	private float mRemainingEnemyHealthPercent
	{
		get
		{
			if (WeakGlobalInstance<CharactersManager>.Instance == null) return 0f;
			return WeakGlobalInstance<CharactersManager>.Instance.remainingEnemyHealthPercent;
		}
	}

	[method: MethodImpl(32)]
	public event OnLegionCallback onLegionStart;

	public WaveManager(WaveType waveType, int waveIndex, BoxCollider enemiesSpawnArea, float zTarget)
	{
		SetUniqueInstance(this);
		mWaveType = waveType;
		mWaveIndex = waveIndex;
		mEnemiesSpawnArea = enemiesSpawnArea;
		mZTarget = zTarget;

		LoadData();

		AnalyseWaveCommandsForStats();

		mNextCommandToRun = 0;
		mEnemiesKilledSoFar = 0;
		mEnemiesToKillBeforeNextWave = 0;
		mEnemiesQueuedSoFar = 0;

		if (Singleton<Profile>.Instance.MultiplayerData.IsMultiplayerGameSessionActive())
		{
			int level = Singleton<Profile>.Instance.MultiplayerData.MultiplayerGameSessionData.defensiveBuffs[0];
			string text = SpecialBossName(level);
			if (text != string.Empty)
			{
				Singleton<EnemiesDatabase>.Instance.LoadInGameData(text);
				allDifferentEnemies.Add(text);
			}
		}

		if (helperEnemySwaps.Length > 0)
		{
			corruptionSpawnEffect = ResourceCache.GetCachedResource("FX/Corrupt", 1).Resource as GameObject;
		}
	}

	public static string SpecialBossName(int level)
	{
		string result = string.Empty;
		switch (level)
		{
		case 1:
			result = "oni";
			break;
		case 2:
			result = "jurogumo";
			break;
		case 3:
			result = "Shogun_SvZ2";
			break;
		}
		return result;
	}

	public void Update()
	{
		// [TODO] Make dependent on remaining health of previous enemies.
		if (!isDone)
		{
			QueueNextWave();
		}

		UpdateDelayTimer();
	}

	public void registerEnemyKilled(string enemyId)
	{
		mEnemiesKilledSoFar++;
		Singleton<Profile>.Instance.IncNumKillsOfEnemyType(enemyId);
	}

	public void AddSpecialRewardsToCollectables()
	{
		if (waveRootData.Rewards != null && Singleton<Profile>.Instance.GetWaveLevel(mWaveIndex) == 1)
		{
			WeakGlobalInstance<CollectableManager>.Instance.GiveResource(ECollectableType.coin, waveRootData.Rewards.coins);
			WeakGlobalInstance<CollectableManager>.Instance.GiveResource(ECollectableType.gem, waveRootData.Rewards.gems);
			WeakGlobalInstance<CollectableManager>.Instance.GiveResource(ECollectableType.pachinkoball, waveRootData.Rewards.pachinkoBalls);
			WeakGlobalInstance<CollectableManager>.Instance.GiveResource(ECollectableType.soul, waveRootData.Rewards.souls);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("revivePotion", waveRootData.Rewards.revive);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("power", waveRootData.Rewards.power);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("luck", waveRootData.Rewards.luck);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("friendship", waveRootData.Rewards.friendship);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("wealth", waveRootData.Rewards.wealth);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("haste", waveRootData.Rewards.haste);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("peace", waveRootData.Rewards.peace);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("healthPotion", waveRootData.Rewards.sushi);
			WeakGlobalInstance<CollectableManager>.Instance.GiveSpecificPresent("leadershipPotion", waveRootData.Rewards.tea);
		}
	}

	public static void LoadSceneForWave()
	{
		Singleton<Profile>.Instance.ClearBonusWaveData();
		WaveSchema waveData = GetWaveData(Singleton<Profile>.Instance.waveToPlay, Singleton<Profile>.Instance.waveTypeToPlay);
		LoadingScreen.LoadLevel(waveData.scene);
	}

	public WaveSchema GetWaveData()
	{
		return GetWaveData(mWaveIndex, mWaveType);
	}

	public static WaveSchema GetWaveData(DataBundleRecordKey recordKey, SeededRandomizer randomizer)
	{
		return WaveSchema.Initialize(recordKey, randomizer);
	}

	public static WaveSchema GetWaveData(int waveId, WaveType waveType)
	{
		switch (waveType)
		{
		case WaveType.Wave_Multiplayer:
		{
			string recordTable = Singleton<PlayModesManager>.Instance.selectedModeData.waves.RecordTable;
			WaveSchema waveSchema2 = WaveSchema.Initialize(DataBundleRuntime.TableRecordKey(recordTable, waveId.ToString()));
			if (waveSchema2 == null)
			{
			}
			return waveSchema2;
		}
		case WaveType.Wave_DailyChallenge:
		{
			ProceduralWaveSchema dailyChallengeProceduralWaveSchema = Singleton<Profile>.Instance.dailyChallengeProceduralWaveSchema;
			WaveSchema cachedWaveSchema = dailyChallengeProceduralWaveSchema.CachedWaveSchema;
			if (cachedWaveSchema == null)
			{
			}
			return cachedWaveSchema;
		}
		default:
		{
			int num = waveId;
			while (num > 0)
			{
				WaveSchema waveSchema = null;
				switch (waveType)
				{
				case WaveType.Wave_SinglePlayer:
				{
					PlayModeSchema selectedModeData = Singleton<PlayModesManager>.Instance.selectedModeData;
					if (selectedModeData.maxBaseWave >= num)
					{
						waveSchema = WaveSchema.Initialize(DataBundleRuntime.TableRecordKey(selectedModeData.waves.RecordTable, num.ToString()));
						break;
					}
					SeededRandomizer randomizer = new SeededRandomizer(num);
					if (num % selectedModeData.bonusWaveInterval == 0)
					{
						int num2 = num / selectedModeData.bonusWaveInterval % DataBundleRuntime.Instance.GetRecordTableLength(typeof(WaveSchema), selectedModeData.endlessBonusWaves) + 1;
						waveSchema = WaveSchema.Initialize(DataBundleRuntime.TableRecordKey(selectedModeData.endlessBonusWaves.RecordTable, num2.ToString()), randomizer);
					}
					else
					{
						int num3 = (num - 1) % DataBundleRuntime.Instance.GetRecordTableLength(typeof(WaveSchema), selectedModeData.endlessWaves) + 1;
						waveSchema = WaveSchema.Initialize(DataBundleRuntime.TableRecordKey(selectedModeData.endlessWaves.RecordTable, num3.ToString()), randomizer);
					}
					break;
				}
				}
				if (waveSchema == null)
				{
					num--;
					continue;
				}
				return waveSchema;
			}
			return null;
		}
		}
	}

	public DataBundleTableHandle<HelperEnemySwapSchema> GetHelperSwapData()
	{
		if (mCorruptionSwapData == null)
		{
			mCorruptionSwapData = new DataBundleTableHandle<HelperEnemySwapSchema>("CorruptionSwap");
		}
		return mCorruptionSwapData;
	}

	public DataBundleTableHandle<EnemySwapSchema> GetDeathSwapData()
	{
		if (mDeathSwapData == null)
		{
			mDeathSwapData = new DataBundleTableHandle<EnemySwapSchema>("OnDeathSwaps");
		}
		return mDeathSwapData;
	}

	public void ReplaceHelperWithEnemy(Helper h)
	{
		if (h == null || h.health <= 0f)
		{
			return;
		}
		string text = null;
		HelperEnemySwapSchema[] data = mCorruptionSwapData.Data;
		foreach (HelperEnemySwapSchema helperEnemySwapSchema in data)
		{
			if (helperEnemySwapSchema.helperSwapFrom.Key == h.id)
			{
				text = helperEnemySwapSchema.enemySwapTo.Key;
				break;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			Enemy c = ConstructEnemyWithEffect(text, 0f, h.transform.position, true, corruptionSpawnEffect);
			WeakGlobalInstance<CharactersManager>.Instance.AddCharacter(c);
			WeakGlobalInstance<CharactersManager>.Instance.DestroyCharacter(h);
		}
	}

	private void LoadData()
	{
		waveRootData = GetWaveData();
		string recordTable = Singleton<PlayModesManager>.Instance.selectedModeData.waves.RecordTable;
		WaveRecordKey = DataBundleRuntime.TableRecordKey(recordTable, waveRootData.index.ToString());
		Singleton<PlayStatistics>.Instance.data.shouldShowRateMeDialog = waveRootData.showRateMe;
		Singleton<PlayStatistics>.Instance.data.shouldAwardMysteryBox = waveRootData.awardMysteryBox;
		GetHelperSwapData();
		mTutorial = waveRootData.tutorial;
		mVillageArchersLevel = waveRootData.villageArchers;
		mBellLevel = waveRootData.bell;
		LoadLevelMultipliers();
	}

	private void LoadLevelMultipliers()
	{
		if (Singleton<Profile>.Instance.inDailyChallenge || Singleton<Profile>.Instance.inMultiplayerWave)
		{
			return;
		}
		PlayModeSchema selectedModeData = Singleton<PlayModesManager>.Instance.selectedModeData;
		if (mWaveIndex <= selectedModeData.maxBaseWave)
		{
			int num = Singleton<Profile>.Instance.GetWaveLevel(mWaveIndex);
			if (num >= 2)
			{
				float @float = Singleton<Config>.Instance.data.GetFloat(TextDBSchema.ChildKey("waveLevelMultipliers", "enemiesHealth"));
				float float2 = Singleton<Config>.Instance.data.GetFloat(TextDBSchema.ChildKey("waveLevelMultipliers", "enemiesDamages"));
				float float3 = Singleton<Config>.Instance.data.GetFloat(TextDBSchema.ChildKey("waveLevelMultipliers", "drops"));
				mLevelMultipliers.enemiesHealth = CalcMultiplier(@float, num - 1);
				mLevelMultipliers.enemiesDamages = CalcMultiplier(float2, num - 1);
				mLevelMultipliers.drops = CalcMultiplier(float3, num - 1);
			}
		}
		else
		{
			int times = mWaveIndex - selectedModeData.maxBaseWave;
			float float4 = Singleton<Config>.Instance.data.GetFloat(TextDBSchema.ChildKey("waveLevelMultipliersEndless", "enemiesHealth"));
			float float5 = Singleton<Config>.Instance.data.GetFloat(TextDBSchema.ChildKey("waveLevelMultipliersEndless", "enemiesDamages"));
			float float6 = Singleton<Config>.Instance.data.GetFloat(TextDBSchema.ChildKey("waveLevelMultipliersEndless", "drops"));
			mLevelMultipliers.enemiesHealth = CalcMultiplier(float4, times);
			mLevelMultipliers.enemiesDamages = CalcMultiplier(float5, times);
			mLevelMultipliers.drops = CalcMultiplier(float6, times);
		}
	}

	private void FlashBossBanner()
	{
		WeakGlobalMonoBehavior<BannerManager>.Instance.OpenBanner(
			new BannerBoss(5f * WeakGlobalMonoBehavior<InGameImpl>.Instance.timeScalar)
		);
	}

	public void SpawnRandomEnemy(Vector3 position, float spawnRange)
	{
		string cmd = mAllDifferentEnemies[UnityEngine.Random.Range(0, mAllDifferentEnemies.Count - 1)];
		SpawnExtraEnemy(cmd, position, spawnRange);
	}

	public void SpawnLegendaryStrikeEnemy(Vector3 position, float spawnRange)
	{
		if (WeakGlobalMonoBehavior<InGameImpl>.Instance.LegendaryStrikeEnemies != null)
		{
			string cmd = WeakGlobalMonoBehavior<InGameImpl>.Instance.LegendaryStrikeEnemies[UnityEngine.Random.Range(0, WeakGlobalMonoBehavior<InGameImpl>.Instance.LegendaryStrikeEnemies.Count)];
			SpawnExtraEnemy(cmd, position, spawnRange);
		}
		else
		{
			SpawnRandomEnemy(position, spawnRange);
		}
	}

	public void SpawnExtraEnemy(string cmd, Vector3 position, float spawnRange)
	{
		bool flag = false;
		foreach (string mAllDifferentEnemy in mAllDifferentEnemies)
		{
			if (string.Compare(mAllDifferentEnemy, cmd, true) == 0)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			WeakGlobalInstance<CharactersManager>.Instance.AddCharacter(ConstructEnemy(cmd, spawnRange, position, true));
		}
	}

	private void UpdateDelayTimer()
	{
		if (isWaveComplete) return;

		mSpawnDelayTimer -= Time.deltaTime;
		if (mSpawnDelayTimer > 0f) return;

		mSpawnDelayTimer = 0f;
		if (WeakGlobalInstance<CharactersManager>.Instance.enemiesCount < 10) // [TODO] Remove hard limit?
		{
			RunNextQueueItem();
		}
	}

	private void RunNextQueueItem()
	{
		if (mWaveQueue.Count == 0) return;

		var queueItem = mWaveQueue.Dequeue();

		if (queueItem.enemy != string.Empty)
		{
			WeakGlobalInstance<CharactersManager>.Instance.AddCharacter(ConstructEnemy(queueItem.enemy));
		}

		mSpawnDelayTimer += queueItem.delay;
	}

	private void QueueNextWave()
	{
		var waveCommandData = waveRootData.Commands[mNextCommandToRun];

		float advanceAt;
		switch (waveCommandData.startMode)
		{
		case WaveCommandSchema.StartMode.Overlap:
			advanceAt = 1f;
			break;
		default:
			advanceAt = waveCommandData.advanceAt;
			break;
		}

		if (mRemainingEnemyHealthPercent > advanceAt) return;

		switch (waveCommandData.type)
		{
		case WaveCommandSchema.Type.Spawn:
			string enemy = waveCommandData.enemy.Key;
			if (enemy != string.Empty)
			{
				int count = (waveCommandData.count > 1) ? waveCommandData.count : 1;
				float delay = waveCommandData.spacingDuration;
				for (int i = 0; i < count - 1; i++)
				{
					mWaveQueue.Enqueue(new QueueItem(enemy, delay));
					mEnemiesQueuedSoFar++;
				}
				mWaveQueue.Enqueue(new QueueItem(enemy, MinimumWaveDelay));
				mEnemiesQueuedSoFar++;

				mEnemiesToKillBeforeNextWave = mEnemiesQueuedSoFar - 1;
			}
			break;
		case WaveCommandSchema.Type.Banner:
			string banner = waveCommandData.banner;

			if (banner == string.Empty) break;

			switch (banner)
			{
			case "Legion":
				WeakGlobalMonoBehavior<BannerManager>.Instance.OpenBanner(
					new BannerLegion(5f * WeakGlobalMonoBehavior<InGameImpl>.Instance.timeScalar)
				);
				break;
			case "Boss":
				WeakGlobalMonoBehavior<BannerManager>.Instance.OpenBanner(
					new BannerBoss(5f * WeakGlobalMonoBehavior<InGameImpl>.Instance.timeScalar)
				);
				break;
			}
			break;
		default: break;
		}

		mNextCommandToRun++;
	}

	public Enemy ConstructEnemy(string enemyId)
	{
		if (!Singleton<EnemiesDatabase>.Instance.Contains(enemyId))
		{
			return null;
		}
		return ConstructEnemyWithEffect(enemyId, mEnemiesSpawnArea.size.x, mEnemiesSpawnArea.transform.position, false, null);
	}

	public Enemy ConstructEnemy(string enemyId, float sizeOfSpawnArea, Vector3 spawnPos, bool dynamicSpawn)
	{
		if (!Singleton<EnemiesDatabase>.Instance.Contains(enemyId))
		{
			return null;
		}
		return ConstructEnemyWithEffect(enemyId, sizeOfSpawnArea, spawnPos, dynamicSpawn, null);
	}

	private Enemy ConstructEnemyWithEffect(string enemyId, float sizeOfSpawnArea, Vector3 spawnPos, bool dynamicSpawn, GameObject effectToSpawn)
	{
		return SpawnEnemy(enemyId, GetCharacterData(enemyId), sizeOfSpawnArea, spawnPos, dynamicSpawn, effectToSpawn);
	}

	private Enemy SpawnEnemy(string uniqueId, CharacterData data, float sizeOfSpawnArea, Vector3 spawnPos, bool dynamicSpawn, GameObject effectToUse)
	{
		spawnPos.x = WeakGlobalInstance<CharactersManager>.Instance.GetBestSpawnXPos(spawnPos, sizeOfSpawnArea, data.lanePref, true, data.isFlying, data.bowAttackRange > 0f);
		Enemy enemy = new Enemy(data, mZTarget, spawnPos, 1);
		data.Setup(enemy);
		enemy.maxHealth *= mLevelMultipliers.enemiesHealth;
		enemy.health = enemy.maxHealth;
		enemy.meleeDamage *= mLevelMultipliers.enemiesDamages;
		enemy.bowDamage *= mLevelMultipliers.enemiesDamages;
		if (WeakGlobalMonoBehavior<InGameImpl>.Instance.HasPeaceCharm())
		{
			CharmSchema charmSchema = Singleton<CharmsDatabase>.Instance[WeakGlobalMonoBehavior<InGameImpl>.Instance.activeCharm];
			enemy.meleeDamage *= charmSchema.multiplier;
			enemy.bowDamage *= charmSchema.multiplier;
		}
		enemy.SetupFromCharacterData(data);
		if (data.resourceDropAlways != string.Empty)
		{
			Character selfPtr = enemy;
			string resourceId = data.resourceDropAlways;
			enemy.onDeathEvent = (Action)Delegate.Combine(enemy.onDeathEvent, (Action)delegate
			{
				SpawnExtraCollectable(resourceId, selfPtr);
			});
		}
		if (effectToUse != null)
		{
			enemy.controller.SpawnEffectAtJoint(effectToUse, "body_effect", false);
		}
		else
		{
			SharedResourceLoader.SharedResource cachedResource = ResourceCache.GetCachedResource("Assets/Game/Resources/FX/DivineEnemy.prefab", 1);
			if (cachedResource != null)
			{
				enemy.controller.SpawnEffectAtJoint(cachedResource.Resource as GameObject, "body_effect", false);
			}
		}
		if (dynamicSpawn)
		{
			enemy.dynamicSpawn = true;
		}
		else
		{
			mSpawnedEnemiesSoFar++;
		}
		return enemy;
	}

	private void SpawnExtraCollectable(string collectableType, Character theEnemy)
	{
		if (theEnemy != null)
		{
			WeakGlobalInstance<CollectableManager>.Instance.ForceSpawnResourceType(collectableType, theEnemy.position);
		}
	}

	private void AnalyseWaveCommandsForStats()
	{
		var multiplayerData = Singleton<Profile>.Instance.MultiplayerData;
		mTotalNumEnemies = (multiplayerData.IsMultiplayerGameSessionActive() && multiplayerData.MultiplayerGameSessionData.defensiveBuffs[0] > 0)
			? 1
			: 0;
		
		mAllDifferentEnemies.Clear();

		for (int i = 0; i < waveRootData.Commands.Length; i++)
		{
			var waveCommandData = waveRootData.Commands[i];

			switch (waveCommandData.type)
			{
			case WaveCommandSchema.Type.Spawn:
				mTotalNumEnemies += (waveCommandData.count > 1) ? waveCommandData.count : 1;

				string enemy = waveCommandData.enemy.Key;
				if (Singleton<EnemiesDatabase>.Instance.Contains(enemy) && !mAllDifferentEnemies.Exists((string element) => element == enemy))
				{
					mAllDifferentEnemies.Add(enemy);
				}

				break;
			default: break;
			}
		}
	}

	private CharacterData GetCharacterData(string id)
	{
		if (!mCharacterDataPool.ContainsKey(id))
		{
			mCharacterDataPool.Add(id, new CharacterData(id, 1));
		}
		return mCharacterDataPool[id];
	}

	public bool HasCorruptionEnemy()
	{
		return allDifferentEnemies.Find((string s) => s.Contains("Amanojaku")) != null;
	}

	public void UnloadData()
	{
		mCharacterDataPool.Clear();
		mCorruptionSwapData.Unload();
		waveRootData = null;
	}

	private static float CalcMultiplier(float mult, int times)
	{
		float num = mult;
		num -= 1f;
		num *= (float)times;
		return num + 1f;
	}
}
