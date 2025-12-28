using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices;
using UnityEngine;

public class InGameImpl : WeakGlobalMonoBehavior<InGameImpl>
{
	public const int MaxPlayers = 2;

	public Transform heroSpawnPointLeft;
	public Transform heroSpawnPointRight;

	public GameObject enemiesTargetLeft;
	public GameObject enemiesTargetRight;

	public GameObject enemyGateGroup;
	public GameObject enemyGatePositions;

	public GameObject vortexLeft;
	public GameObject vortexRight;

	public GameObject gateSparklesLeft;

	public Camera gameCamera;

	public Transform heroWalkLeftEdge;
	public Transform heroWalkRightEdge;

	public float heroLeftConstraint
	{
		get { return heroWalkLeftEdge.position.z; }
	}
	public float heroRightConstraint
	{
		get { return heroWalkRightEdge.position.z; }
	}

	public BoxCollider enemiesSpawnAreaRight;
	public BoxCollider helpersSpawnAreaLeft;

	public GameObject bellRingerLocationObject;
	public GameObject bellLocationObject;
	public GameObject bellObject;

	public Transform[] villageArcher = new Transform[3];

	public Transform bannerLocObject;

	public BoxCollider pitArea;

	private float mTimeToNextTutorial;

	private float mHitEffectTimer;

	private float mHitEffectFrequency;

	private string mFriendshipHelperID;

	private BuffIconManager mBuffIconManager = new BuffIconManager();

	private Hero[] mHero = new Hero[MaxPlayers];

	private Gate[] mGate = new Gate[MaxPlayers];

	private Bell mBell;

	private Pit mPit;

	private CharactersManager mCharactersManager;

	private CollectableManager mCollectableManager;

	private ProjectileManager mProjectileManager;

	private WaveManager mWaveManager;

	private ProceduralShaderManager mShaderManager;

	private Leadership[] mLeadership = new Leadership[MaxPlayers];

	private Souls[] mSouls = new Souls[MaxPlayers];

	private VillageArchers mVillageArchers;

	private float mTimeStarted;

	private List<string> mLegendaryStrikeEnemies = new List<string>();

	private RailManager mRailManager;

	private bool mGameOver;

	private float mAllAlliesInvincibleTimer;

	private float mCameraYOffset = 100f;

	private float mBaseTimeScale = 1f;

	private bool mStartedSlowMoFinisher;

	private float mGameTimer;

	private bool mReviveOfferDismissed;

	private PlayModesManager.GameDirection mGameDirection;

	private string mTagHeroID;

	private string mTagAbilityID;

	public Transform heroSpawnPoint
	{
		get
		{
			return (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
				? heroSpawnPointRight
				: heroSpawnPointLeft;
		}
	}

	public Transform enemySpawnPoint
	{
		get
		{
			return (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
				? heroSpawnPointLeft
				: heroSpawnPointRight;
		}
	}

	public GameObject enemiesTarget
	{
		get
		{
			return (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
				? enemiesTargetRight
				: enemiesTargetLeft;
		}
	}

	public GameObject heroTarget
	{
		get
		{
			return (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
				? enemiesTargetLeft
				: enemiesTargetRight;
		}
	}

	public BoxCollider enemiesSpawnArea
	{
		get
		{
			return (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
				? helpersSpawnAreaLeft
				: enemiesSpawnAreaRight;
		}
	}

	public BoxCollider helpersSpawnArea
	{
		get
		{
			return (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
				? enemiesSpawnAreaRight
				: helpersSpawnAreaLeft;
		}
	}

	public bool gameOver
	{
		get { return mGameOver; }
	}

	public bool playerWon
	{
		get { return mGameOver && hero.health > 0f; }
	}
	public bool enemiesWon
	{
		get { return mGameOver && hero.health <= 0f; }
	}

	public bool useSlowMoFinisher
	{
		get
		{
			return
				mStartedSlowMoFinisher ||
				(hero != null &&
				(hero.controller.currentAnimation == "attack" || hero.controller.currentAnimation == "katanaslash"));
		}
	}

	public Hero hero
	{
		get { return mHero[0]; }
	}
	public Hero enemyHero
	{
		get { return mHero[1]; }
	}

	public Gate gate
	{
		get { return (mGate[0] == null) ? mGate[1] : mGate[0]; }
	}
	public Gate enemyGate
	{
		get { return mGate[1]; }
	}

	public string activeCharm { get; private set; }

	public float timeScalar
	{
		get { return (!useSlowMoFinisher) ? 1f : 0.8f; }
	}

	public float TimeSinceStart
	{
		get { return Time.time - mTimeStarted; }
	}

	public float allAlliesInvincibleTimer
	{
		get { return mAllAlliesInvincibleTimer; }
		set { mAllAlliesInvincibleTimer = value; }
	}

	private GameObject fireworksResource { get; set; }

	public bool gamePaused
	{
		get { return Time.timeScale == 0f; }
		set
		{
			if (value == (Time.timeScale == 0f)) return;

			SingletonSpawningMonoBehaviour<USoundThemeManager>.Instance.PauseSoundThemes(value);
			if (value)
			{
				Time.timeScale = 0f;
			}
			else
			{
				Time.timeScale = mBaseTimeScale;
			}

			if (WeakGlobalMonoBehavior<HUD>.Exists)
			{
				WeakGlobalMonoBehavior<HUD>.Instance.HandleGamePause(value);
			}
		}
	}

	public string FriendshipHelperID
	{
		get
		{
			if (mFriendshipHelperID == null)
			{
				CharmSchema charmSchema = Singleton<CharmsDatabase>.Instance[activeCharm];
				if (charmSchema == null || charmSchema.id != "friendship")
				{
					mFriendshipHelperID = string.Empty;
				}
				else
				{
					string table = "FriendshipCharmHelpers";
					DataBundleRecordTable dataBundleRecordTable = new DataBundleRecordTable(table);
					HelperListSchema[] array = dataBundleRecordTable.InitializeRecords<HelperListSchema>();
					int num = UnityEngine.Random.Range(0, array.Length);
					DataBundleRecordKey dataBundleRecordKey = new DataBundleRecordKey(array[num].ability);
					mFriendshipHelperID = DataBundleRuntime.RecordKey(dataBundleRecordKey);
				}
			}
			return mFriendshipHelperID;
		}
	}

	public Pit Pit
	{
		get { return mPit; }
	}

	public CharactersManager CharacterMgr
	{
		get { return mCharactersManager; }
	}

	public List<string> LegendaryStrikeEnemies
	{
		get { return mLegendaryStrikeEnemies; }
	}

	public float GameTimer
	{
		get { return mGameTimer; }
	}

	public bool CanDoRevolutionAchievement { get; set; }

	public string TagHeroID
	{
		get { return mTagHeroID; }
	}

	public string TagAbilityID
	{
		get { return mTagAbilityID; }
	}

	public Profile ProfileData
	{
		get { return Singleton<Profile>.Instance; }
	}

	public int CurrentWave
	{
		get { return ProfileData.CurrentStoryWave; }
	}

	public int DefenderIndex
	{
		get { return (ProfileData.IsInVSMultiplayerWave && Singleton<PlayModesManager>.Instance.Attacking) ? 1 : 0; }
	}

	private List<float> enemyGateZPositions = new List<float>();

	private List<string> adhocHelpers = new List<string>();

	private List<string> allEnemyKeys = new List<string>();

	private void Awake()
	{
		SetUniqueInstance(this);

		if (!Singleton<Profile>.Exists)
		{
			StartCoroutine(Singleton<Profile>.Instance.Init());
		}

		SingletonSpawningMonoBehaviour<USoundThemeManager>.Instance.SetResourceLevel(1);
	}

	private IEnumerator Start()
	{
		while (!ProfileData.Initialized) yield return null;

		ApplicationUtilities._allowAutoSave = true;
		ResourceCache.DefaultCacheLevel = 1;
		Singleton<Achievements>.Instance.SuppressPartialReporting(true);

		InitializeHelpersAndAbilities();

		UpdatePlayStatistics();

		Profile.UpdatePlayMode();

		mGameDirection = Singleton<PlayModesManager>.Instance.gameDirection;

		InitializeWaveManager();
		mCharactersManager = new CharactersManager();
		mRailManager = new RailManager();
		mProjectileManager = new ProjectileManager();
		mShaderManager = new ProceduralShaderManager();
		mCollectableManager = new CollectableManager(
			heroWalkLeftEdge.position.z + 0.16f,
			heroWalkRightEdge.position.z - 0.16f,
			heroSpawnPoint.position.x
		);

		mBell = new Bell(bellRingerLocationObject, bellLocationObject, DefenderIndex);
		mPit = new Pit(pitArea, DefenderIndex);
		mVillageArchers = new VillageArchers();
		InitializeGates();
		InitializeHeroes();
		InitializeEnemies();
		InitializeLeadership();
		InitializeSouls();

		ApplyInitialCharmEffects();

		mCameraYOffset = gameCamera.transform.position.y - heroSpawnPointLeft.transform.position.y;
		RenderSettings.fog = false;

		BeginWaveMusic();

		WeakGlobalMonoBehavior<HUD>.Instance.Init();

		WeakGlobalMonoBehavior<BannerManager>.Instance.Init();

		SingletonMonoBehaviour<TutorialMain>.Instance.Init();
		StartCoroutine(CheckTutorials());

		int waveBannerDelay =
			SingletonMonoBehaviour<TutorialMain>.Instance.IsTutorialNeeded("Tutorial_Game02_Movement") ? 4.5f :
			(SingletonMonoBehaviour<TutorialMain>.Instance.IsTutorialNeeded("Tutorial_Game03_Ally") ? 6.0f :
			1f);
		RunAfterDelay(delegate {
			WeakGlobalMonoBehavior<BannerManager>.Instance.OpenBanner(new BannerStartWave(5f, ProfileData.WaveToPlay));
		}, waveBannerDelay);

		IncrementWaveAttempts();

		LogInitializationData();

		CacheResources();

		InitializeTimers();

		Singleton<PlayerWaveEventData>.Instance.StartWave();

		Shader.WarmupAllShaders();
		MemoryWarningHandler.Instance.unloadOnMemoryWarning = true;
		AdjustEffectQuality();
	}

	private void InitializeHelpersAndAbilities()
	{
		List<string> abilitiesToLoad = new List<string>();
		foreach (string ability in ProfileData.GetSelectedAbilities())
		{
			abilitiesToLoad.Add(ability);
		}

		List<string> helpersToLoad = new List<string>();

		Singleton<CharmsDatabase>.Instance.LoadInGameData(activeCharm, true);
		if (activeCharm == "TagTeam")
		{
			List<string> possibleTag = new List<string>();
			string[] tagHeroes = new string[3] { "HeroBalanced", "HeroAttack", "HeroDefense" };
			foreach (string hero in tagHeroes)
			{
				bool isMultiplayerOpponentDifferentHero = ProfileData.MultiplayerData.CurrentOpponent.loadout.heroId != hero;
				if (ProfileData.CurrentHeroId != hero && (!ProfileData.IsInMultiplayerWave || isMultiplayerOpponentDifferentHero))
				{
					possibleTag.Add(hero);
				}
			}
			mTagHeroID = possibleTag[UnityEngine.Random.Range(0, possibleTag.Count)];
			Singleton<HeroesDatabase>.Instance.LoadInGameData(mTagHeroID, 0);
			switch (mTagHeroID)
			{
			case "HeroBalanced":
				mTagAbilityID = "SummonLightning";
				break;
			case "HeroAttack":
				mTagAbilityID = "DaggerBarrage";
				break;
			case "HeroDefense":
				mTagAbilityID = "DivineWind";
				break;
			}
			if (!abilitiesToLoad.Contains(mTagAbilityID))
			{
				abilitiesToLoad.Add(mTagAbilityID);
			}

			// [???] Why is the Hero created and immediately destroyed?
			Vector3 spawnPos = new Vector3(0f, -100f, 0f);
			Hero tagHero = new Hero(spawnPos, 0, TagHeroID, false);
			UnityEngine.Object.Destroy(tagHero.controlledObject);
			tagHero.Destroy();
		}

		activeCharm = ProfileData.selectedCharm;
		if (!string.IsNullOrEmpty(activeCharm))
		{
			CharmSchema charm = Singleton<CharmsDatabase>.Instance[activeCharm];
			string helperName = FriendshipHelperID;
			string abilityName = DataBundleRuntime.RecordKey(charm.abilityToActivate.Key.ToString());
			if (!string.IsNullOrEmpty(helperName) || !string.IsNullOrEmpty(abilityName))
			{
				List<string> currentAbilities = ProfileData.GetSelectedAbilities();
				if (!string.IsNullOrEmpty(abilityName))
				{
					currentAbilities.Add(abilityName);
				}
				else
				{
					currentAbilities.Add("ActiveCharm");
				}
				ProfileData.SetSelectedAbilities(currentAbilities);
				if (!string.IsNullOrEmpty(helperName) && !helpersToLoad.Contains(helperName))
				{
					helpersToLoad.Add(helperName);
				}
				if (!string.IsNullOrEmpty(abilityName) && !abilitiesToLoad.Contains(abilityName))
				{
					abilitiesToLoad.Add(abilityName);
				}
			}
		}

		bool noNonUniqueAllySelected =
			ProfileData.GetSelectedHelpers().FindIndex((string s) => !Singleton<HelpersDatabase>.Instance[s].unique) < 0;
		if (ProfileData.GetSelectedAbilities().Contains("DivineIntervention") && noNonUniqueAllySelected)
		{
			helpersToLoad.Add("Farmer");
		}

		foreach (string helper in ProfileData.GetSelectedHelpers())
		{
			if (!helpersToLoad.Contains(helper))
			{
				helpersToLoad.Add(helper);
			}
		}

		if (ProfileData.IsInVSMultiplayerWave)
		{
			foreach (string ability2 in ProfileData.MultiplayerData.CurrentOpponent.loadout.abilityIdList)
			{
				if (!string.IsNullOrEmpty(ability2) && abilitiesToLoad.Contains(ability2))
				{
					abilitiesToLoad.Add(ability2);
				}
			}
			string[] aiHelpers = ProfileData.MultiplayerData.CurrentOpponent.loadout.GetSelectedHelperIDs();
			string[] array = aiHelpers;
			foreach (string helper2 in array)
			{
				if (!string.IsNullOrEmpty(helper2) && !helpersToLoad.Contains(helper2))
				{
					helpersToLoad.Add(helper2);
				}
			}
		}

		Singleton<AbilitiesDatabase>.Instance.LoadInGameData(abilitiesToLoad, true);
		Singleton<HelpersDatabase>.Instance.LoadInGameData(helpersToLoad, adhocHelpers);
		Singleton<PotionsDatabase>.Instance.LoadInGameData();
	}

	private void UpdatePlayStatistics()
	{
		Singleton<PlayStatistics>.Instance.Reset();
		Singleton<PlayStatistics>.Instance.data.wavePlayed = ProfileData.WaveToPlay;
		Singleton<PlayStatistics>.Instance.data.wavePlayedLevel = ProfileData.GetWaveCompletionCount(ProfileData.WaveToPlay) + 1;
		Singleton<PlayStatistics>.Instance.data.waveTypePlayed = ProfileData.waveTypeToPlay;
	}

	private void InitializeWaveManager()
	{
		if (!ProfileData.IsInVSMultiplayerWave)
		{
			for (int i = 1; true; i++)
			{
				// [TODO] Implement in every stage
				string childName = string.Format("RGatePosition{0}", i);
				GameObject child = enemyGatePositions.FindChild(childName);
				if (child == null) break;
				enemyGateZPositions.Add(child.transform.position.z);
			}

			mWaveManager = new WaveManager(
				ProfileData.waveTypeToPlay,
				ProfileData.WaveToPlay,
				enemyGateZPositions,
				enemyGateGroup,
				enemiesSpawnArea,
				enemiesTarget.transform.position.z
			);

			allEnemyKeys = new List<string>(mWaveManager.allDifferentEnemies);
			if (mWaveManager.HasCorruptionEnemy())
			{
				DataBundleTableHandle<HelperEnemySwapSchema> swapData = WeakGlobalInstance<WaveManager>.Instance.GetHelperSwapData();
				HelperEnemySwapSchema[] swapSchemas = swapData.Data;
				List<string> selectedHelpers = ProfileData.GetSelectedHelpers();
				HelperEnemySwapSchema[] array3 = swapSchemas;
				foreach (HelperEnemySwapSchema swapSchema2 in array3)
				{
					string swapFromKey2 = swapSchema2.helperSwapFrom.Key.ToString();
					if (selectedHelpers.Contains(swapFromKey2) || adhocHelpers.Contains(swapFromKey2))
					{
						allEnemyKeys.Add(swapSchema2.enemySwapTo.Key.ToString());
					}
				}
			}
		}
	}

	private void InitializeEnemies()
	{
		if (ProfileData.IsInMultiplayerWave)
		{
			mLegendaryStrikeEnemies = DataBundleRuntime.Instance.GetRecordKeys(typeof(EnemyListSchema), "LegendaryStrikeEnemies", false);
			foreach (string enemy in mLegendaryStrikeEnemies)
			{
				if (!allEnemyKeys.Contains(enemy))
				{
					allEnemyKeys.Add(enemy);
				}
			}
		}
		if (!ProfileData.IsInVSMultiplayerWave)
		{
			List<string> enemiesSoFar = new List<string>(allEnemyKeys);
			foreach (string enemyID in enemiesSoFar)
			{
				EnemySchema levelEnemySchema = Singleton<EnemiesDatabase>.Instance[enemyID];
				DataBundleRecordKey onDeathSpawnKey = levelEnemySchema.spawnOnDeath;
				if (!DataBundleRecordKey.IsNullOrEmpty(onDeathSpawnKey))
				{
					string spawnOnDeathKeyString = onDeathSpawnKey.Key.ToString();
					if (!string.IsNullOrEmpty(spawnOnDeathKeyString) && !allEnemyKeys.Contains(spawnOnDeathKeyString))
					{
						allEnemyKeys.Add(spawnOnDeathKeyString);
					}
				}
			}
			DataBundleTableHandle<EnemySwapSchema> enemyDeathSwapData = WeakGlobalInstance<WaveManager>.Instance.GetDeathSwapData();
			EnemySwapSchema[] enemyDeathSwapSchemas = enemyDeathSwapData.Data;
			EnemySwapSchema[] array4 = enemyDeathSwapSchemas;
			foreach (EnemySwapSchema swapSchema in array4)
			{
				string swapFromKey = swapSchema.swapFrom.Key.ToString();
				if (allEnemyKeys.Contains(swapFromKey))
				{
					allEnemyKeys.Add(swapSchema.swapTo.Key.ToString());
				}
			}
		}
		Singleton<EnemiesDatabase>.Instance.LoadInGameData(allEnemyKeys);
	}

	private void InitializeGates()
	{
		mGate[DefenderIndex] = new Gate(enemiesTargetLeft, DefenderIndex);

		if (mGate[0] != null)
		{
			mCharactersManager.AddCharacter(mGate[0]);
		}
		if (mGate[1] != null)
		{
			mCharactersManager.AddCharacter(mGate[1]);
		}
	}

	private void InitializeHeroes()
	{
		CreateHero(0, heroSpawnPoint);

		if (ProfileData.IsInVSMultiplayerWave)
		{
			CreateHero(1, enemySpawnPoint);
		}
	}

	private void InitializeLeadership()
	{
		mLeadership[0] = new Leadership(0);
		mLeadership[0].characterManagerRef = mCharactersManager;
		mLeadership[0].helperSpawnArea = helpersSpawnArea;
		mLeadership[0].helpersZTarget = enemiesSpawnArea.transform.position.z;
		mLeadership[0].hero = mHero[0];

		if (ProfileData.IsInVSMultiplayerWave)
		{
			mLeadership[1] = new EnemyLeadership(1);
			mLeadership[1].characterManagerRef = mCharactersManager;
			mLeadership[1].helperSpawnArea = enemiesSpawnArea;
			mLeadership[1].helpersZTarget = enemiesTarget.transform.position.z;
			mLeadership[1].hero = mHero[1];
			mLeadership[0].helpersZTarget = heroTarget.transform.position.z;
		}
	}

	private void InitializeSouls()
	{
		mSouls[0] = new Souls(0);
		mSouls[0].hero = mHero[0];
	}

	private void BeginWaveMusic()
	{
		var musicManager = SingletonSpawningMonoBehaviour<UMusicManager>.Instance;
		if (mWaveManager != null && !DataBundleRecordKey.IsNullOrEmpty(mWaveManager.Data.music))
		{
			musicManager.PlayByKey(mWaveManager.Data.music);
		}
		else
		{
			var designerVariables = SingletonSpawningMonoBehaviour<DesignerVariables>.Instance;
			var defaultWaveMusic = designerVariables.GetVariable<string>("DefaultWaveMusic");
			musicManager.PlayByKey(new DataBundleRecordKey(defaultWaveMusic));
		}
	}

	private void IncrementWaveAttempts()
	{
		if (!ProfileData.IsInDailyChallenge)
		{
			if (!ProfileData.MultiplayerData.IsMultiplayerGameSessionActive())
			{
				ProfileData.IncrementWaveAttemptCount(CurrentWave);
			}
			else
			{
				ProfileData.IncrementMPWaveAttemptCount(ProfileData.MultiplayerData.MultiplayerGameSessionData.missionName);
			}
		}
	}

	private void LogInitializationData()
	{
		ProfileData.FlurrySession.ReportWaveStarted();

		Singleton<Analytics>.Instance.LogEvent(
			"AudioSettings",
			Analytics.Param("DeviceVolume", NUF.GetHardwareVolume()),
			Analytics.Param("GameMusicVolume", AudioUtils.MasterMusicVolume),
			Analytics.Param("GameSfxVolume", AudioUtils.MasterSoundThemeVolume)
		);
	}

	private void CacheResources()
	{
		fireworksResource = ResourceCache.GetCachedResource("FX/Fireworks", 1).Resource as GameObject;

		int bannerLevel = ProfileData.MultiplayerData.CollectionLevel("Banner");
		if (ProfileData.IsInVSMultiplayerWave && Singleton<PlayModesManager>.Instance.Attacking)
		{
			bannerLevel = ProfileData.MultiplayerData.CurrentOpponent.loadout.bannersCollected;
		}
		SharedResourceLoader.SharedResource res3;
		if (bannerLocObject != null && bannerLevel > 0)
		{
			res3 = ResourceCache.GetCachedResource("Assets/Game/Resources/Props/LevelProps/Banner.prefab", 1);
			if (res3 != null)
			{
				UnityEngine.Object.Instantiate(res3.Resource, bannerLocObject.position, Quaternion.identity);
			}
		}

		int flowerCount = ProfileData.MultiplayerData.CollectionLevel("Flower");
		if (ProfileData.IsInVSMultiplayerWave && Singleton<PlayModesManager>.Instance.Attacking)
		{
			flowerCount = ProfileData.MultiplayerData.CurrentOpponent.loadout.flowersCollected;
		}
		if (flowerCount > 0)
		{
			res3 = ResourceCache.GetCachedResource("Assets/Game/Resources/FX/GateDefense.prefab", 1);
			if (res3 != null)
			{
				mGate[DefenderIndex].controller.SpawnEffectAt(res3.Resource as GameObject, gate.transform.position);
			}
		}

		res3 = ResourceCache.GetCachedResource("Assets/Game/Resources/FX/FullScreenBloodEffect.prefab", 1);
		if (res3 != null)
		{
			hero.HUDBloodEffect = UnityEngine.Object.Instantiate(res3.Resource) as GameObject;
		}
	}

	private void InitializeTimers()
	{
		if (ProfileData.IsInDailyChallenge)
		{
			mGameTimer = ProfileData.dailyChallengeProceduralWaveSchema.maxTime;
		}
		else if (ProfileData.IsInMultiplayerWave && Singleton<PlayModesManager>.Instance.Attacking)
		{
			mGameTimer = SingletonSpawningMonoBehaviour<DesignerVariables>.Instance.GetVariable("AttackTime", 300f);
		}

		mTimeStarted = Time.time;
	}

	private void AdjustEffectQuality()
	{
		if (PortableQualitySettings.GetQuality() == EPortableQualitySetting.Low)
		{
			mHitEffectFrequency = 4f;
		}
		else if (PortableQualitySettings.GetQuality() == EPortableQualitySetting.Medium)
		{
			mHitEffectFrequency = 2.5f;
		}
		else
		{
			mHitEffectFrequency = 0.5f;
		}
	}

	private IEnumerator CheckForKatanaSlashUse()
	{
		while (true)
		{
			yield return new WaitForSeconds(0f);
			if (SingletonMonoBehaviour<TutorialMain>.Instance.GetCurrentTutorial_Key() == "Tutorial_Game04_Ability")
			{
				if (!WeakGlobalMonoBehavior<HUD>.Instance.FindHUD<HUDAbilities>().IsAvailable("KatanaSlash"))
				{
					SingletonMonoBehaviour<TutorialMain>.Instance.TutorialDone();
					ProfileData.ForceOnboardingStage("OnboardingStep6_AbilityWave1");
					yield break;
				}

				if (mGameOver) break;
			}
		}
		SingletonMonoBehaviour<TutorialMain>.Instance.TutorialDone();
	}

	private IEnumerator CheckForFarmerSpawn()
	{
		do
		{
			yield return new WaitForSeconds(0f);
		}
		while (!(SingletonMonoBehaviour<TutorialMain>.Instance.GetCurrentTutorial_Key() == "Tutorial_Game03_Ally") || (WeakGlobalInstance<CharactersManager>.Instance.helpersCount <= 0 && !mGameOver));
		
		SingletonMonoBehaviour<TutorialMain>.Instance.TutorialDone();
		ProfileData.ForceOnboardingStage("OnboardingStep18_SummonFarmer");
	}

	private IEnumerator CheckTutorials()
	{
		if (!ProfileData.IsInStoryWave && !ProfileData.HasWaveBeenCompleted(CurrentWave)) yield break;

		var tutorialManager = Singleton<TutorialManager>.Instance;

		switch (CurrentWave)
		{
		case 1:
			WeakGlobalMonoBehavior<HUD>.Instance.alliesEnabled = false;
			WeakGlobalMonoBehavior<HUD>.Instance.abilitiesEnabled = false;
			break;
		case 2:
			ProfileData.SetSelectedHelpers(new List<string>(new string[1] { "Farmer" }));
			break;
		}

		while (true)
		{
			if (mTimeToNextTutorial <= 0f)
			{
				switch (CurrentWave)
				{
				case 1:
					if (!tutorialManager.HasCompleted(ETutorial.MovingTheHero))
					{
						tutorialManager.StartTutorial(ETutorial.MovingTheHero);
						mTimeToNextTutorial = 0f;
						yield return new WaitForSeconds(0f);
					}
					else if (!tutorialManager.HasCompleted(ETutorial.UsingAbilities))
					{
						List<Character> enemiesInSight = WeakGlobalInstance<CharactersManager>.Instance.GetCharactersInRangeMaxCount(
							hero.controller.position.z, hero.controller.position.z + 4f, 1, 1f);
						if (enemiesInSight.Count >= 2 && WeakGlobalInstance<Souls>.Instance.souls >= 3)
						{
							ProfileData.SetSelectedAbilities(new List<string>(new string[1] { "KatanaSlash" }));
							WeakGlobalMonoBehavior<HUD>.Instance.abilitiesEnabled = true;
							WeakGlobalMonoBehavior<HUD>.Instance.ResetAbilities();
							tutorialManager.StartTutorial(ETutorial.UsingAbilities);
							StartCoroutine(CheckForKatanaSlashUse());
							mTimeToNextTutorial = 0f;
							yield return new WaitForSeconds(5f);
						}
					}
					break;
				case 2:
					if (!tutorialManager.HasCompleted(ETutorial.SpawningAllies))
					{
						tutorialManager.StartTutorial(ETutorial.SpawningAllies);
						WeakGlobalMonoBehavior<HUD>.Instance.alliesEnabled = true;
						StartCoroutine(CheckForFarmerSpawn());
						mTimeToNextTutorial = 0f;
						yield return new WaitForSeconds(5f);
					}
					break;
				case 4:
					if (!tutorialManager.HasCompleted(ETutorial.UsingBowAgainstFlyingEnemies))
					{
						List<Character> enemiesPlayerCanSee = WeakGlobalInstance<CharactersManager>.Instance.GetCharactersInRange(hero.controller.position.z, hero.controller.position.z + 4f, 1);
						int foundIndex = enemiesPlayerCanSee.FindIndex((Character c) => c.isFlying);
						if (foundIndex >= 0)
						{
							tutorialManager.StartTutorial(ETutorial.UsingBowAgainstFlyingEnemies);
							mTimeToNextTutorial = 0f;
							yield return new WaitForSeconds(5f);
						}
					}
					break;
				}
			}

			mTimeToNextTutorial -= Time.deltaTime;
			yield return new WaitForSeconds(0f);
		}
	}

	public void CreateHero(int index, Transform spawnPoint)
	{
		if (mHero[index] != null)
		{
			WeakGlobalInstance<CharactersManager>.Instance.DestroyCharacter(mHero[index]);
			mHero[index] = null;
		}
		mHero[index] = new Hero(spawnPoint, index);
		mHero[index].controller.constraintLeft = heroWalkLeftEdge.position.z;
		mHero[index].controller.constraintRight = heroWalkRightEdge.position.z;
		mCharactersManager.AddCharacter(mHero[index]);
	}

	private void OnDestroy()
	{
		Time.timeScale = 1f;
		if (!ApplicationUtilities.HasShutdown)
		{
			MemoryWarningHandler.Instance.unloadOnMemoryWarning = false;
			Singleton<AbilitiesDatabase>.Instance.UnloadData();
			Singleton<CharmsDatabase>.Instance.UnloadData();
			Singleton<HelpersDatabase>.Instance.UnloadData();
			Singleton<EnemiesDatabase>.Instance.UnloadData();
			Singleton<HeroesDatabase>.Instance.UnloadData();
			Singleton<PotionsDatabase>.Instance.UnloadData();
			mProjectileManager.UnloadData();
			if (mWaveManager != null)
			{
				mWaveManager.UnloadData();
			}
			if (mVillageArchers != null)
			{
				mVillageArchers.UnloadData();
			}
			if (mBell != null)
			{
				mBell.UnloadData();
			}
			if (mLeadership.Length > 0)
			{
				mLeadership[0].Clear();
			}
			mLeadership = null;
			mSouls = null;
			mBuffIconManager = null;
			mHero = null;
			mGate = null;
			mBell = null;
			mPit = null;
			mCharactersManager = null;
			mCollectableManager = null;
			mProjectileManager = null;
			mWaveManager = null;
			mShaderManager = null;
			mVillageArchers = null;
			mRailManager = null;
			ResourceCache.UnloadAllAboveLevel(1);
			ResourceCache.DefaultCacheLevel = 0;
			SingletonSpawningMonoBehaviour<USoundThemeManager>.Instance.UnloadResourceLevel(1);
			SingletonSpawningMonoBehaviour<USoundThemeManager>.Instance.SetResourceLevel(0);
			GluiCore.sMaterialDictionary.Clear();
		}
	}

	private void OnApplicationPause(bool pause)
	{
		if (!gamePaused && pause)
		{
			GluiActionSender.SendGluiAction("POPUP_PAUSEMENU", null, null);
		}
	}

	private void Update()
	{
		if (!ProfileData.Initialized) return;

		if (gamePaused)
		{
			if (!mGameOver)
			{
				StoreMenuImpl.UpdateTapJoyPoints(base.gameObject);
			}
			return;
		}

		mHitEffectTimer -= Time.deltaTime;

		if (mGameOver)
		{
			if (useSlowMoFinisher)
			{
				mBaseTimeScale = Mathf.Min(1f, mBaseTimeScale + 0.8f * Time.deltaTime);
				Time.timeScale = mBaseTimeScale;
				mCameraYOffset += 0.25f * Time.deltaTime;
				gameCamera.transform.position = new Vector3(gameCamera.transform.position.x + 0.4f * Time.deltaTime, gameCamera.transform.position.y, gameCamera.transform.position.z);
			}
			mLeadership[0].Update();
			mCharactersManager.Update();
			mCollectableManager.Update();
			mProjectileManager.Update();
			mShaderManager.update();
			mBell.Update();
			mPit.Update();
			mVillageArchers.Update();
			return;
		}

		if (Debug.isDebugBuild && !WeakGlobalMonoBehavior<HUD>.Instance.gameObject.activeInHierarchy && Input.touchCount >= 2)
		{
			string text = ProfileData.GetSelectedAbilities()[0];
			if (!string.IsNullOrEmpty(text))
			{
				hero.DoAbility(text);
			}
		}

		mLeadership[0].Update();

		if (mLeadership[1] != null)
		{
			mLeadership[1].Update();
		}

		mWaveManager.Update();

		mCharactersManager.Update();

		mCollectableManager.Update();

		mProjectileManager.Update();

		mShaderManager.update();

		mBell.Update();

		mPit.Update();

		mVillageArchers.Update();

		CheckWinLoseConditions();

		if (mAllAlliesInvincibleTimer > 0f)
		{
			mAllAlliesInvincibleTimer = Mathf.Max(0f, mAllAlliesInvincibleTimer - Time.deltaTime);
		}

		Singleton<PlayerWaveEventData>.Instance.Update(Time.deltaTime);

		if (mGameTimer > 0f)
		{
			mGameTimer -= Time.deltaTime;
			if (mGameTimer <= 0f)
			{
				mGameTimer = 0f;
				mHero[0].ForceDeath();
			}
		}

		StoreMenuImpl.UpdateTapJoyPoints(base.gameObject);
	}

	private void LateUpdate()
	{
		if (ProfileData.Initialized)
		{
			UpdateCamera();
		}
	}

	public Hero GetHero(int playerId)
	{
		if (playerId >= 0 && playerId < MaxPlayers)
		{
			return mHero[playerId];
		}
		return null;
	}

	public Gate GetGate(int playerId)
	{
		if (playerId >= 0 && playerId < MaxPlayers)
		{
			return mGate[playerId];
		}
		return null;
	}

	public Leadership GetLeadership(int playerIndex)
	{
		if (playerIndex >= 0 && playerIndex < MaxPlayers)
		{
			return mLeadership[playerIndex];
		}
		return null;
	}

	public Souls GetSouls(int playerIndex)
	{
		if (playerIndex >= 0 && playerIndex < MaxPlayers)
		{
			return mSouls[playerIndex];
		}
		return null;
	}

	public void RunSpecialWaveCommand(string cmd)
	{
		switch (cmd)
		{
		case "@bossalert":
			WeakGlobalMonoBehavior<BannerManager>.Instance.OpenBanner(new BannerBoss(5f * timeScalar));
			break;
		case "@legionalert":
			break;
		default:
			UnityEngine.Debug.Log("WARNING: Unknown wave command: " + cmd);
			break;
		}
	}

	public void ShowInGameStore(string itemID)
	{
	}

	public bool ShouldShowHitFX()
	{
		return mHitEffectTimer <= 0f;
	}

	public void ResetHitEffectTimer()
	{
		mHitEffectTimer = mHitEffectFrequency;
	}

	public void RunAfterDelay(Action fn, float delay)
	{
		if (fn != null)
		{
			StartCoroutine(RunAfterDelayInternal(fn, delay));
		}
	}

	public void RunNextUpdate(Action fn)
	{
		if (fn != null)
		{
			StartCoroutine(RunAfterYieldInternal(fn));
		}
	}

	public void Win()
	{
		ProfileData.FlurrySession.ReportWaveWon();
		Singleton<PlayStatistics>.Instance.data.victory = true;
		mGameOver = true;
		if (mWaveManager != null)
		{
			ProfileData.AddSeenEnemies(mWaveManager.allDifferentEnemies);
		}
		if (ProfileData.MultiplayerData.IsMultiplayerGameSessionActive())
		{
			ProfileData.MultiplayerData.FinishMultiplayerGameSession(true);
		}
		if (fireworksResource != null)
		{
			UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(fireworksResource, hero.transform.position, Quaternion.identity), 10f);
		}
		CreateWinDialog();
		StartCoroutine(PostWinDelay());
		if (!ProfileData.IsInMultiplayerWave && ProfileData.WaveToPlay > 2)
		{
			if (mGate[0] != null && mGate[0].NoDamage)
			{
				Singleton<Achievements>.Instance.IncrementAchievement("PerfectWave", 1);
			}
			if (mLeadership[0] != null)
			{
				if (mLeadership[0].ResourcesSpentOnHelpers == 0f)
				{
					Singleton<Achievements>.Instance.IncrementAchievement("OneManArmy", 1);
				}
				if (CanDoRevolutionAchievement && mLeadership[0].CanDoRevolutionAchievement)
				{
					Singleton<Achievements>.Instance.IncrementAchievement("Revolution", 1);
				}
			}
			if (mHero[0] != null && mHero[0].NoAttack)
			{
				Singleton<Achievements>.Instance.IncrementAchievement("NoAttack", 1);
			}
		}
		if (mWaveManager != null)
		{
			NarrativeSchema narrativeSchema = NarrativeSchema.NarrativeForWave(mWaveManager.WaveRecordKey);
			if (narrativeSchema != null)
			{
				GluiState_NarrativePanel.resourceToLoad = DataBundleRuntime.Instance.GetValue<string>(typeof(NarrativeSchema), NarrativeSchema.UdamanTableName, narrativeSchema.id, "prefab", true);
			}
		}
	}

	public void Lose()
	{
		ProfileData.FlurrySession.ReportWaveLost();
		mGameOver = true;
		if (ProfileData.MultiplayerData.IsMultiplayerGameSessionActive())
		{
			ProfileData.MultiplayerData.FinishMultiplayerGameSession(false);
		}
		CreateLoseDialog();
		ProfileData.Save();
		if (ProfileData.CurrentStoryWave == 1 && ProfileData.GetIsWaveUnlocked(1) && !ProfileData.IsInMultiplayerWave)
		{
			Restart();
		}
		else
		{
			onFinishGameRequest();
		}
	}

	public void Restart()
	{
		WaveManager.LoadSceneForWave();
		Singleton<PlayerWaveEventData>.Instance.StartWave();
	}

	public void OnHudRestart()
	{
		if (!string.IsNullOrEmpty(activeCharm))
		{
			List<string> selectedAbilities = ProfileData.GetSelectedAbilities();
			CharmSchema charmSchema = Singleton<CharmsDatabase>.Instance[activeCharm];
			string text = charmSchema.abilityToActivate;
			if (!string.IsNullOrEmpty(text))
			{
				string item = DataBundleRuntime.RecordKey(text);
				selectedAbilities.Remove(item);
				ProfileData.SetSelectedAbilities(selectedAbilities);
				WeakGlobalMonoBehavior<HUD>.Instance.ResetAbilities();
			}
		}
	}

	public void OnReviveDialog_Accept()
	{
		hero.Revive();
		mReviveOfferDismissed = false;
		ProfileData.globalPlayerRating += 20;
		Singleton<Achievements>.Instance.IncrementAchievement("UseRevive", 1);
		if (ProfileData.IsInVSMultiplayerWave && Singleton<PlayModesManager>.Instance.Attacking)
		{
			mGameTimer = SingletonSpawningMonoBehaviour<DesignerVariables>.Instance.GetVariable("AttackTime", 300f);
		}
		else if (ProfileData.IsInDailyChallenge)
		{
			mGameTimer = ProfileData.dailyChallengeProceduralWaveSchema.maxTime;
		}
	}

	public void OnReviveDialog_Dismissed()
	{
		mReviveOfferDismissed = true;
	}

	private void onFinishGameRequest()
	{
		if (ProfileData.waveTypeToPlay == WaveManager.WaveType.Wave_SinglePlayer && Singleton<PlayStatistics>.Instance.data.victory)
		{
			Singleton<Achievements>.Instance.CheckThresholdAchievement("CompleteWave1", Singleton<PlayStatistics>.Instance.data.wavePlayed);
			Singleton<Achievements>.Instance.CheckThresholdAchievement("CompleteWave2", Singleton<PlayStatistics>.Instance.data.wavePlayed);
		}
		Singleton<PlayerWaveEventData>.Instance.Reset();
		if (Singleton<PlayStatistics>.Instance.data.victory && LootListController.allLoots.Count > 0)
		{
			SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("MainMenuScreen", "Menu_Results");
		}
		else
		{
			SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("MainMenuScreen", "Menu_Results_NoLoot");
		}
		StoreMenuImpl.ReturnedFromGame = true;
		LoadingScreen.LoadLevel("AllMenus");
	}

	private void UpdateCamera()
	{
		if (mHero[0] == null) return;
		gameCamera.transform.position = new Vector3(gameCamera.transform.position.x, mHero[0].position.y + mCameraYOffset, mHero[0].position.z);
	}

	private void CheckWinLoseConditions()
	{
		if (mGameOver) return;

		bool flag = false;
		if (mHero[0].isOver)
		{
			if ((ProfileData.CurrentStoryWave > 1 || ProfileData.HasWaveBeenCompleted(1)) && !mReviveOfferDismissed)
			{
				// WeakGlobalInstance<CollectableManager>.Instance.OpenPresents(true);
				GluiActionSender.SendGluiAction("POPUP_REVIVE", base.gameObject, null);
				return;
			}
			flag = true;
		}
		else
		{
			if (mGate[0] != null && mGate[0].health == 0f && mHero[0] != null)
			{
				mHero[0].ForceDeath();
			}
			if (mGate[1] != null && mHero[1] != null && mGate[1].health == 0f)
			{
				mHero[1].ForceDeath();
			}
		}
		
		if (flag)
		{
			Lose();
		}
		else if (ProfileData.IsInVSMultiplayerWave)
		{
			if (Singleton<PlayModesManager>.Instance.Attacking)
			{
				if (mGate[1] != null && mGate[1].health <= 0f)
				{
					Win();
					if (mHero[1] != null)
					{
						mHero[1].health = 0f;
					}
				}
			}
			else if (mLeadership[1].GetPercentDoneWithWave() == 1f && mCharactersManager.GetPlayerCharacters(1).Count <= 1 && mHero[1].health == 0f)
			{
				Win();
			}
		}
		else if (mWaveManager.isWaveComplete)
		{
			Win();
		}
	}

	private void CreateWinDialog()
	{
		if (useSlowMoFinisher)
		{
			mBaseTimeScale = 0.1f;
			Time.timeScale = mBaseTimeScale;
			gameCamera.transform.position = new Vector3(
				gameCamera.transform.position.x - 2f,
				gameCamera.transform.position.y,
				gameCamera.transform.position.z
			);
			mCameraYOffset /= 2f;
			mStartedSlowMoFinisher = true;
		}
		
		RunAfterDelay(delegate
		{
			WeakGlobalMonoBehavior<BannerManager>.Instance.OpenBanner(new BannerWinGame(5f * timeScalar));
		}, timeScalar * 0.5f);
	}

	private void CreateLoseDialog()
	{
		RunAfterDelay(delegate
		{
			WeakGlobalMonoBehavior<BannerManager>.Instance.OpenBanner(new BannerLoseGame(5f));
		}, 0.5f);
	}

	private IEnumerator PostWinDelay()
	{
		float waitTime = Singleton<Config>.Instance.data.GetFloat(TextDBSchema.ChildKey("Game", "postWinTime"));
		yield return new WaitForSeconds(waitTime);
		RegisterWin();
		onFinishGameRequest();
	}

	private void RegisterWin()
	{
		if (WeakGlobalInstance<WaveManager>.Instance != null)
		{
			WeakGlobalInstance<WaveManager>.Instance.AddSpecialRewardsToCollectables();
		}
		WeakGlobalInstance<CollectableManager>.Instance.BankAllResources();
		ProfileData.GoToNextWave();
	}

	private IEnumerator RunAfterDelayInternal(Action fn, float delay)
	{
		yield return new WaitForSeconds(delay);
		if (fn != null)
		{
			fn();
		}
	}

	private IEnumerator RunAfterYieldInternal(Action fn)
	{
		yield return null;
		if (fn != null)
		{
			fn();
		}
	}

	private void SpendCharm()
	{
		string selectedCharm = ProfileData.selectedCharm;
		if (selectedCharm != string.Empty)
		{
			ProfileData.SetNumCharms(selectedCharm, Mathf.Max(0, ProfileData.GetNumCharms(selectedCharm) - 1));
			ProfileData.selectedCharm = string.Empty;
			ProfileData.globalPlayerRating += 20;
		}
	}

	public void ApplyInitialCharmEffects()
	{
		CharmSchema charmSchema = Singleton<CharmsDatabase>.Instance[activeCharm];
		if (charmSchema != null)
		{
			UseCharm();
		}
	}

	public void UseCharm()
	{
		CharmSchema charmSchema = Singleton<CharmsDatabase>.Instance[activeCharm];
		if (charmSchema == null) return;

		if (!string.IsNullOrEmpty(charmSchema.abilityToActivate.Key))
		{
			Singleton<Achievements>.Instance.IncrementAchievement("UseCharm", 1);
		}
		else
		{
			if (HasCommandCharm())
			{
				GetLeadership(0).ApplyLeadershipCostBuff(charmSchema.leadershipReduction);
			}
			Singleton<Achievements>.Instance.IncrementAchievement("UseCharm", 1);
		}

		SpendCharm();
	}

	public bool HasHasteCharm() { return activeCharm == "haste" || activeCharm.Contains("+haste"); }
	public bool HasPowerCharm() { return activeCharm == "power" || activeCharm.Contains("+power"); }
	public bool HasWealthCharm() { return activeCharm == "wealth" || activeCharm.Contains("+wealth"); }
	public bool HasPeaceCharm() { return activeCharm == "peace" || activeCharm.Contains("+peace"); }
	public bool HasLuckCharm() { return activeCharm == "luck" || activeCharm.Contains("+luck"); }
	public bool HasCommandCharm() { return activeCharm == "command"; }
	public bool HasMagicCharm() { return activeCharm == "magic"; }

	private void ShowReviveDialog() {}

	public bool SpawnFriendshipHelper()
	{
		if (string.IsNullOrEmpty(FriendshipHelperID)) return false;

		Character character = WeakGlobalInstance<Leadership>.Instance.ForceSpawn(FriendshipHelperID);
		if (character == null || mWaveManager == null) return false;

		character.maxHealth *= mWaveManager.multipliers.enemiesHealth;
		character.health = character.maxHealth;
		character.meleeDamage *= mWaveManager.multipliers.enemiesDamages;
		return true;
	}

	public bool SetCheatAbility(string ability)
	{
		if (Singleton<AbilitiesDatabase>.Instance.GetSchema(ability) != null) return false;

		List<string> list = new List<string>(1){ability};
		ProfileData.SetSelectedAbilities(list);
		return true;
	}

	public void SetHeroInvulnByDefault(bool toSet)
	{
		ProfileData.debugHeroInvuln = toSet;
		ResetHeroInvulnToDefault(0);
	}

	public void ResetHeroInvulnToDefault(int ownerID)
	{
		bool invuln = ownerID == 0 && ProfileData.debugHeroInvuln;
		GetHero(ownerID).invuln = invuln;

		var gate = GetGate(ownerID);
		if (gate != null)
		{
			gate.invuln = invuln;
		}
	}
}
