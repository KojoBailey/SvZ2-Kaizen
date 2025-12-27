using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Glu.Plugins.ASocial;
using UnityEngine;

public class Profile : Singleton<Profile>
{
	public static bool UseVsMode = true;

	public static bool FastLoseItem = false;

	public const string kCloudSaveDisabledKey = "cloudSaveDisabled";

	public const string kPlayerRatingLeaderboard = "SAMUZOMBIE2_PLAYER_POWER";
	public const string kDailyChallengeLeaderboard = "SAMUZOMBIE2_DAILY_CHALLENGE";
	public const string kMultiplayerLeaderboard = "SAMUZOMBIE2_MP_WINS";

	protected int mPlayerAttackRating;
	protected int[] mAttackRatingCategory = new int[14];

	private static bool mRefreshingDailyChallenge = false;
	private ProceduralWaveSchema mDailyChallengProceduralWaveSchema;
	private HeroSchema mDailyChallengeHeroSchema;
	private List<string> mDailyChallengeHelpers;
	private List<string> mDailyChallengeAbilities;

	private static DateTime? lastUpdateCheck;
	private static TimeSpan? timeBetweenUpdateChecks;

	private static object _lock = new object();

	private SDFTreeSaveProvider mSavedData;
	private SDFTreeSaveProvider mCloudSavedData;

	private MultiplayerData multiplayerData;

	private WaveManager.WaveType mWaveTypeToPlay;

	private DataBundleTableHandle<UpgradesSchema> upgradeData;

	private DateTime mMultiplayerShieldExpireTime = new DateTime(2000, 1, 1);

	private string soulQtyString;

	private Flurry_Session flurrySession = new Flurry_Session();

	private string[] onboardingEvents = new string[20]
	{
		"OnboardingStep1_AppStart", "OnboardingStep2_StartScreen", "OnboardingStep3_StartWave1", "OnboardingStep4_MoveWave1", "OnboardingStep5_AttackWave1", "OnboardingStep6_AbilityWave1", "OnboardingStep7_CompleteWave1", "OnboardingStep8_StoreTutorial", "OnboardingStep9_StoreMain1", "OnboardingStep10_LeaveStore",
		"OnboardingStep11_Wave2Select", "OnboardingStep12_HeroSelect", "OnboardingStep13_AllySelect", "OnboardingStep14_AbilitySelect", "OnboardingStep15_CharmSelect", "OnboardingStep16_LeaveCarousel", "OnboardingStep17_StartWave2", "OnboardingStep18_SummonFarmer", "OnboardingStep19_CompleteWave2", "OnboardingStep20_StoreMain2"
	};

	private float mMinutesPerSoulTick;

	private int mHighestUnlockedWave;

	private bool mJustMadeSC_IAP;
	private bool mJustMadeHC_IAP;

	public int[] AttackRatingCategory
	{
		get { return mAttackRatingCategory; }
	}

	public SeededRandomizer dailyRandomizer { get; private set; }

	private static DateTime CurrentTime
	{
		get { return SntpTime.UniversalTime.AddHours(-8.0); }
	}
	public ProceduralWaveSchema dailyChallengeProceduralWaveSchema
	{
		get { return mDailyChallengProceduralWaveSchema; }
		private set { mDailyChallengProceduralWaveSchema = value; }
	}
	public HeroSchema dailyChallengeHeroSchema
	{
		get { return mDailyChallengeHeroSchema; }
		private set { mDailyChallengeHeroSchema = value; }
	}
	public List<string> dailyChallengeHelpers
	{
		get { return mDailyChallengeHelpers; }
		private set { mDailyChallengeHelpers = value; }
	}
	public List<string> dailyChallengeAbilities
	{
		get { return mDailyChallengeAbilities; }
		private set { mDailyChallengeAbilities = value; }
	}

	public static bool iCloudEnabled
	{
		get { return GeneralConfig.iCloudEnabled && PlayerPrefs.GetInt(kCloudSaveDisabledKey, 1) == 0; }
		set { PlayerPrefs.SetInt(kCloudSaveDisabledKey, (!value) ? 1 : 0); }
	}
	public SDFTreeSaveProvider CloudSave
	{
		get
		{
			SetupCloudSave();
			return mCloudSavedData;
		}
	}
	public SaveProvider.Result CloudSaveResult { get; private set; }

	public string this[string key]
	{
		get { return mSavedData.Data[key]; }
		set { mSavedData.Data[key] = value; }
	}

	public MultiplayerData MultiplayerData
	{
		get { return multiplayerData; }
	}

	public Flurry_Session FlurrySession
	{
		get { return flurrySession; }
	}

	public bool Initialized { get; private set; }

	public bool Loading { get; private set; }

	public bool HasSeenSoulJarFullPopup { get; set; }

	public string GameSpyUserID
	{
		get { return mSavedData.GetValue("GameSpyUserID"); }
		set { mSavedData.SetValue("GameSpyUserID", value); }
	}
	public float GameSpyUserAccountVersion
	{
		get { return mSavedData.GetValueFloat("GameSpyUserAccountVersion"); }
		set { mSavedData.SetValueFloat("GameSpyUserAccountVersion", value); }
	}

	public int lastDailyRewardIndex
	{
		get { return mSavedData.GetValueInt("lastDailyRewardIndex"); }
		set { mSavedData.SetValueInt("lastDailyRewardIndex", value); }
	}
	public DateTime? lastDailyRewardDate
	{
		get
		{
			string value = mSavedData.GetValue("lastDailyReward");
			DateTime result;
			if (DateTime.TryParseExact(value, "O", null, DateTimeStyles.None, out result))
			{
				return result.ToUniversalTime();
			}
			return null;
		}
		set
		{
			if (value.HasValue)
			{
				mSavedData.SetValue("lastDailyReward", value.Value.ToString("O"));
			}
			else
			{
				mSavedData.SetValue("lastDailyReward", string.Empty);
			}
		}
	}

	public int playerLevel
	{
		get { return mSavedData.GetValueInt("playerLevel"); }
		set { mSavedData.SetValueInt("playerLevel", value); }
	}

	public int mpWavesWon
	{
		get { return mSavedData.GetValueInt("mpWavesWon"); }
		set { mSavedData.SetValueInt("mpWavesWon", value); }
	}

	public bool wasBasicGameBeaten
	{
		get { return HasWaveBeenCompleted(maxBaseWave); }
	}

	public DateTime timeStamp
	{
		get
		{
			DateTime result;
			if (DateTime.TryParseExact(mSavedData.GetValue("timeStamp"), "O", null, DateTimeStyles.None, out result))
			{
				return result.ToUniversalTime();
			}
			return ApplicationUtilities.Now;
		}
		set { mSavedData.SetValue("timeStamp", value.ToString("O")); }
	}
	public DateTime sntpTimeStamp
	{
		get
		{
			DateTime result;
			if (DateTime.TryParseExact(mSavedData.GetValue("sntpTimeStamp"), "O", null, DateTimeStyles.None, out result))
			{
				return result.ToUniversalTime();
			}
			return SntpTime.UniversalTime;
		}
		set { mSavedData.SetValue("sntpTimeStamp", value.ToString("O")); }
	}

	public int timeRemainingUntilNotification
	{
		get { return mSavedData.GetValueInt("timeRemainingUntilNotification"); }
		set { mSavedData.SetValueInt("timeRemainingUntilNotification", value); }
	}

	public int lastYearPlayed
	{
		get { return mSavedData.GetValueInt("lastYearPlayed"); }
		set { mSavedData.SetValueInt("lastYearPlayed", value); }
	}
	public int lastMonthPlayed
	{
		get { return mSavedData.GetValueInt("lastMonthPlayed"); }
		set { mSavedData.SetValueInt("lastMonthPlayed", value); }
	}
	public int lastDayPlayed
	{
		get { return mSavedData.GetValueInt("lastDayPlayed"); }
		set { mSavedData.SetValueInt("lastDayPlayed", value); }
	}

	public string profileName
	{
		get { return mSavedData.GetValue("profileName"); }
		set { mSavedData.SetValue("profileName", value); }
	}

	public string latestDetectedOnlineVersion
	{
		get { return mSavedData.GetValue("latestDetectedOnlineVersion"); }
		set { mSavedData.SetValue("latestDetectedOnlineVersion", value); }
	}
	public bool newVersionDetected
	{
		get { return ActiveSavedData.GetValueBool("newVersionDetected"); }
		set { mSavedData.SetValueBool("newVersionDetected", value); }
	}

	public bool tutorialIsComplete
	{
		get { return GetIsWaveUnlocked(3); }
	}

	public bool hasReportedUser
	{
		get { return mSavedData.GetValueBool("hasReportedUser"); }
		set { mSavedData.SetValueBool("hasReportedUser", value); }
	}
	public bool hasReportedGemPurchase
	{
		get { return mSavedData.GetValueBool("hasReportedGemPurchase"); }
		set { mSavedData.SetValueBool("hasReportedGemPurchase", value); }
	}

	public int latestOnlineBundleVersion
	{
		get { return mSavedData.GetValueInt("onlineBundleVersion"); }
		set { mSavedData.SetValueInt("onlineBundleVersion", value); }
	}

	public bool hasWonPachinkoBefore
	{
		get { return mSavedData.GetValueBool("hasWonPachinkoBefore"); }
		set { mSavedData.SetValueBool("hasWonPachinkoBefore", value); }
	}

	public int lifetimeSCSpent
	{
		get { return mSavedData.GetValueInt("lifetimeSCSpent"); }
		set { mSavedData.SetValueInt("lifetimeSCSpent", value); }
	}

	public int globalPlayerRating
	{
		get { return mSavedData.GetValueInt("playerRating"); }
		set { mSavedData.SetValueInt("playerRating", Mathf.Max(0, value)); }
	}
	public int dailyRewardRating
	{
		get { return mSavedData.GetValueInt("dailyRewardRating"); }
		set { mSavedData.SetValueInt("dailyRewardRating", Mathf.Max(0, value)); }
	}
	public int multiplayerWinRating
	{
		get { return mSavedData.GetValueInt("multiplayerWinRating"); }
		set { mSavedData.SetValueInt("multiplayerWinRating", Mathf.Max(0, value)); }
	}
	public int multiplayerLossRating
	{
		get { return mSavedData.GetValueInt("multiplayerLossRating"); }
		set { mSavedData.SetValueInt("multiplayerLossRating", Mathf.Max(0, value)); }
	}
	public int collectionItemLossRating
	{
		get { return mSavedData.GetValueInt("collectionItemLossRating"); }
		set { mSavedData.SetValueInt("collectionItemLossRating", Mathf.Max(0, value)); }
	}
	public int achievementRating
	{
		get { return mSavedData.GetValueInt("achievementRating"); }
		set { mSavedData.SetValueInt("achievementRating", Mathf.Max(0, value)); }
	}
	public int playerAttackRating
	{
		get { return mPlayerAttackRating; }
	}

	public int coins
	{
		get { return mSavedData.GetValueInt("coins"); }
		set
		{
			Singleton<Achievements>.Instance.CheckThresholdAchievement("CollectCoins", value);

			if (WeakGlobalMonoBehavior<InGameImpl>.Instance != null)
			{
				int num = value - coins;
				if (num > 0)
				{
					Singleton<PlayStatistics>.Instance.data.inGameGoldGained += num;
				}
			}

			int num2 = value;
			mSavedData.SetValueInt("coins", num2);
			if (SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Exists)
			{
				SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("coins", StringUtils.FormatAmountString(num2));
			}
		}
	}

	public int pachinkoBalls
	{
		get { return mSavedData.GetValueInt("pachinkoBalls"); }
		set
		{
			int num = value;
			mSavedData.SetValueInt("pachinkoBalls", num);
			if (SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Exists)
			{
				SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("pachinkoBalls", num);
			}
		}
	}
	public int pachinkoBallsLaunched
	{
		get { return mSavedData.GetValueInt("pachinkoBallsLaunched"); }
		set { mSavedData.SetValueInt("pachinkoBallsLaunched", value); }
	}

	public int freeBoosterPacks
	{
		get { return mSavedData.GetValueInt("fbp"); }
		set { mSavedData.SetValueInt("fbp", value); }
	}

	public int souls
	{
		get { return mSavedData.GetValueInt("souls"); }
		set
		{
			if (WeakGlobalMonoBehavior<InGameImpl>.Instance != null)
			{
				int num = value - souls;
				if (num > 0)
				{
					Singleton<PlayStatistics>.Instance.data.inGameSoulsAwarded += num;
					Singleton<Achievements>.Instance.IncrementAchievement("CollectSouls", num);
				}
			}
			int maxSouls = GetMaxSouls();
			value = Mathf.Clamp(value, 0, maxSouls);
			if (WeakGlobalMonoBehavior<InGameImpl>.Instance != null)
			{
				int num2 = value - souls;
				if (num2 > 0)
				{
					Singleton<PlayStatistics>.Instance.data.inGameSoulsGained += num2;
				}
			}
			mSavedData.SetValueInt("souls", value);
			if (SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Exists)
			{
				if (soulQtyString == null)
				{
					soulQtyString = StringUtils.GetStringFromStringRef("MenuFixedStrings", "Menu_QtyOfQty");
				}
				SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("souls", string.Format(soulQtyString, StringUtils.FormatAmountString(value), StringUtils.FormatAmountString(maxSouls)));
				SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("CUR_SOULS", value);
				SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("MAX_SOULS", maxSouls);
			}
		}
	}

	public int storeVisitCount
	{
		get { return mSavedData.GetValueInt("storeVisitsCount"); }
		set { mSavedData.SetValueInt("storeVisitsCount", value); }
	}

	public WaveManager.WaveType waveTypeToPlay
	{
		get
		{
			if (IsInMultiplayerWave)
			{
				return WaveManager.WaveType.Wave_Multiplayer;
			}
			if (IsInDailyChallenge)
			{
				return WaveManager.WaveType.Wave_DailyChallenge;
			}
			return WaveManager.WaveType.Wave_SinglePlayer;
		}
	}

	public bool AreAllNormalWavesBeaten
	{
		get
		{
			PlayModeSchema selectedModeData = Singleton<PlayModesManager>.Instance.selectedModeData;
			return Singleton<Profile>.Instance.highestUnlockedWave > selectedModeData.maxBaseWave || HasWaveBeenCompleted(selectedModeData.maxBaseWave);
		}
	}

	public int CurrentStoryWave
	{
		get { return Mathf.Max(1, mSavedData.GetValueInt("waveToBeat", playModeSubSection)); }
		set
		{
			int num = ValidateWaveIndex(value);
			mSavedData.SetValueInt("waveToBeat", num, playModeSubSection);
			if (!GetIsWaveUnlocked(num))
			{
				SetIsWaveUnlocked(num, true);
			}
		}
	}

	public int WaveToPlay
	{
		get
		{
			if (IsInMultiplayerWave)
			{
				return MultiplayerData.MultiplayerGameSessionData.WaveToPlay;
			}
			return CurrentStoryWave;
		}
	}

	public bool DoesWaveAllowBow
	{
		get { return Singleton<Profile>.Instance.WaveToPlay > 1 || Singleton<Profile>.Instance.HasWaveBeenCompleted(1); }
	}

	public bool IsInMultiplayerWave
	{
		get { return MultiplayerData != null && MultiplayerData.IsMultiplayerGameSessionActive(); }
	}
	public bool IsInVSMultiplayerWave
	{
		get { return MultiplayerData != null && MultiplayerData.IsMultiplayerGameSessionActive() && UseVsMode; }
	}
	public bool IsInDailyChallenge
	{
		get { return (string)SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.GetData("GAME_MODE") == "Button_DailyChallenge"; }
	}
	public bool IsInStoryWave
	{
		get { return (string)SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.GetData("GAME_MODE") == "Button_Story"; }
	}


	public SDFTreeSaveProvider ActiveSavedData
	{
		get { return mSavedData; }
	}
	public SDFTreeSaveProvider SaveData
	{
		get { return mSavedData; }
	}

	public int CurrentHeroLevel
	{
		get { return GetHeroLevel(CurrentHeroId); }
		set { SetHeroLevel(CurrentHeroId, value); }
	}

	public string CurrentHeroId
	{
		get
		{
			if (ChangingDefenseLoadout)
			{
				return SelectedDefendHero;
			}
			string text = (!IsInDailyChallenge)
				? mSavedData.GetValue("CurrentHeroId", playModeSubSection)
				: mSavedData.GetValue("dailychallenge_heroID", playModeSubSection);
			if (text == string.Empty && Singleton<PlayModesManager>.Exists && Singleton<PlayModesManager>.Instance.selectedModeData != null)
			{
				return Singleton<PlayModesManager>.Instance.selectedModeData.defaultHeroID;
			}
			return text;
		}
		set
		{
			if (ChangingDefenseLoadout)
			{
				SelectedDefendHero = value;
				multiplayerData.LocalPlayerLoadout.UpdateLocalProfile();
			}
			else if (IsInDailyChallenge)
			{
				mSavedData.SetValue("dailychallenge_heroID", value, playModeSubSection);
			}
			else
			{
				mSavedData.SetValue("CurrentHeroId", value, playModeSubSection);
			}
		}
	}

	public int InitialLeadershipLevel
	{
		get { return GetLeadershipLevel(CurrentHeroId); }
		set { SetLeadershipLevel(CurrentHeroId, value); }
	}
	public int SoulsLevel
	{
		get { return GetSoulsLevel(CurrentHeroId); }
		set { SetSoulsLevel(CurrentHeroId, value); }
	}

	public int SwordLevel
	{
		get { return GetMeleeWeaponLevel(CurrentHeroId); }
		set { SetMeleeWeaponLevel(CurrentHeroId, value); }
	}
	public int BowLevel
	{
		get { return GetRangedWeaponLevel(CurrentHeroId); }
		set { SetRangedWeaponLevel(CurrentHeroId, value); }
	}
	public int armorLevel
	{
		get { return GetArmorLevel(CurrentHeroId); }
		set { SetArmorLevel(CurrentHeroId, value); }
	}
	public int archerLevel
	{
		get { return ActiveSavedData.GetValueInt("archerLevel", playModeSubSection); }
		set
		{
			mSavedData.SetValueInt("archerLevel", value, playModeSubSection);
			CalculateAttackRating();
		}
	}
	public int baseLevel
	{
		get { return Mathf.Max(1, ActiveSavedData.GetValueInt("baseLevel", playModeSubSection)); }
		set
		{
			mSavedData.SetValueInt("baseLevel", Mathf.Max(0, value), playModeSubSection);
			CalculateAttackRating();
		}
	}
	public int bellLevel
	{
		get { return ActiveSavedData.GetValueInt("bellLevel", playModeSubSection); }
		set
		{
			mSavedData.SetValueInt("bellLevel", value, playModeSubSection);
			CalculateAttackRating();
		}
	}
	public int pitLevel
	{
		get { return ActiveSavedData.GetValueInt("pitLevel", playModeSubSection); }
		set
		{
			mSavedData.SetValueInt("pitLevel", value, playModeSubSection);
			CalculateAttackRating();
		}
	}

	public List<string> novelties
	{
		get { return mSavedData.GetSimpleList("novelties", playModeSubSection); }
		set { mSavedData.SetSimpleList("novelties", value, playModeSubSection); }
	}

	public List<string> alreadyEarnedAchievements
	{
		get { return mSavedData.GetSimpleList("alreadyEarnedAchievements"); }
		set { mSavedData.SetSimpleList("alreadyEarnedAchievements", value); }
	}

	public List<string> giveOnceCompleted
	{
		get { return mSavedData.GetSimpleList("giveOnce"); }
		set { mSavedData.SetSimpleList("giveOnce", value); }
	}

	public List<string> alreadySeenEnemies
	{
		get { return mSavedData.GetSimpleList("seenEnemies"); }
	}

	public string SelectedDefendHero
	{
		get { return ActiveSavedData.GetDictionaryValue<string>("defend", "hero"); }
		set { ActiveSavedData.SetDictionaryValue("defend", "hero", value); }
	}

	public bool HasSetupDefenses
	{
		get { return ActiveSavedData.GetDictionaryValue<bool>("defend", "setup"); }
		set { ActiveSavedData.SetDictionaryValue("defend", "setup", value); }
	}

	public int MaxDefensiveRating
	{
		get { return ActiveSavedData.GetDictionaryValue<int>("defend", "maxRating"); }
		set { ActiveSavedData.SetDictionaryValue("defend", "maxRating", value); }
	}

	public int maxSelectedHelpers
	{
		get
		{
			int num = Singleton<HeroesDatabase>.Instance[CurrentHeroId].allySlots;
			if (GetUpgradeLevel("AllySlot") > 0)
			{
				num++;
			}
			return num;
		}
	}
	public int maxSelectedAbilities
	{
		get
		{
			int num = Singleton<HeroesDatabase>.Instance[CurrentHeroId].abilitySlots;
			if (GetUpgradeLevel("AbilitySlot") > 0)
			{
				num++;
			}
			return num;
		}
	}

	public string selectedCharm
	{
		get { return ActiveSavedData.GetValue("selectedCharm", playModeSubSection); }
		set { mSavedData.SetValue("selectedCharm", value, playModeSubSection); }
	}

	public int highestUnlockedWave
	{
		get { return mHighestUnlockedWave; }
	}

	public bool hasSummonedFarmer
	{
		get { return mSavedData.GetValueBool("hasSummonedFarmer"); }
		set { mSavedData.SetValueBool("hasSummonedFarmer", value); }
	}

	public UpgradesSchema[] AllUpgrades
	{
		get
		{
			if (upgradeData != null)
			{
				return upgradeData.Data;
			}
			return null;
		}
	}

	public int maxBaseWave
	{
		get { return Singleton<PlayModesManager>.Instance.selectedModeData.maxBaseWave; }
	}
	public int maxStoryModeWave
	{
		get { return Singleton<PlayModesManager>.Instance.allModes[0].maxBaseWave; }
	}

	public string playModeSubSection
	{
		get
		{
			if (Singleton<PlayModesManager>.Exists && Singleton<PlayModesManager>.Instance.selectedModeData != null)
			{
				string profileSubSection = Singleton<PlayModesManager>.Instance.selectedModeData.profileSubSection;
				if (!string.IsNullOrEmpty(profileSubSection))
				{
					return profileSubSection;
				}
			}
			return null;
		}
	}

	public int lastCompletedDailyChallenge
	{
		get { return mSavedData.GetValueInt("lastCompletedDailyChallenge"); }
		set { mSavedData.SetValueInt("lastCompletedDailyChallenge", value); }
	}

	public int dailyChallengesCompleted
	{
		get { return mSavedData.GetValueInt("dailyChallengesCompleted"); }
		set { mSavedData.SetValueInt("dailyChallengesCompleted", value); }
	}

	public bool CompletedTodaysDailyChallenge
	{
		get { return lastCompletedDailyChallenge == GetDailyChallengeDaysSinceStart(); }
	}

	public DateTime MultiplayerShieldExpireTime
	{
		get { return mMultiplayerShieldExpireTime; }
	}

	private int onboardingStage
	{
		get { return SaveData.GetValueInt("OnboardingStage"); }
		set { SaveData.SetValueInt("OnboardingStage", value); }
	}

	public bool showHealthText
	{
		get { return SaveData.GetValueBool("ShowHealthText"); }
		set { SaveData.SetValueBool("ShowHealthText", value); }
	}

	public bool debugHeroInvuln
	{
		get { return SaveData.GetValueBool("DebugHeroInvuln"); }
		set { SaveData.SetValueBool("DebugHeroInvuln", value); }
	}

	public bool DisplayedDailyChallengeUnlock
	{
		get { return SaveData.GetValueBool("DisplayedDailyChallengeUnlock"); }
		set { SaveData.SetValueBool("DisplayedDailyChallengeUnlock", value); }
	}

	public bool ChangingDefenseLoadout { get; set; }

	public bool JustMadeSC_IAP
	{
		get { return mJustMadeSC_IAP; }
		set { mJustMadeSC_IAP = value; }
	}

	public Profile()
	{
		mSavedData = SaveProvider.Create<SDFTreeSaveProvider>("profile");
		FileSaveTarget fileSaveTarget = mSavedData.AddTarget<FileSaveTarget>("local");
		mSavedData.AutoSaveEnabled = false;
		mSavedData.SaveOnExit = false;
		mSavedData.Header.UseDeviceData = true;
		fileSaveTarget.UseBackup = true;
		iCloudSaveTarget iCloudSaveTarget2 = mSavedData.AddTarget<iCloudSaveTarget>("cloud");
		iCloudSaveTarget2.UseBackup = true;
		mSavedData.RequireCRCMatch = !Debug.isDebugBuild;
		mSavedData.Header.UseEncoding = !Debug.isDebugBuild;
	}

	private bool SetupCloudSave()
	{
		if (mCloudSavedData != null || !GeneralConfig.iCloudEnabled) return false;

		mCloudSavedData = SaveProvider.Create<SDFTreeSaveProvider>("cloudProfile");
		mCloudSavedData.AutoSaveEnabled = false;
		mCloudSavedData.SaveOnExit = false;
		mCloudSavedData.Header.UseDeviceData = true;
		iCloudSaveTarget iCloudSaveTarget2 = mCloudSavedData.AddTarget<iCloudSaveTarget>("cloud");
		iCloudSaveTarget2.UseBackup = true;
		mCloudSavedData.RequireCRCMatch = !Debug.isDebugBuild;
		mCloudSavedData.Header.UseEncoding = !Debug.isDebugBuild;
		return true;
	}

	public IEnumerator Init()
	{
		if (Initialized || Loading) yield break;

		if (!GeneralConfig.IsLive)
		{
			SingletonSpawningMonoBehaviour<OutputLogFile>.Instance.Initialize();
		}
		
		UnityThreadHelper.Activate();
		MemoryWarningHandler.CreateInstance();
		PortableQualitySettings.SetQualityLevelForDevice();
		LoadingScreen.LogStep("Misc");
		DataBundleRuntime.Initialize();
		LoadingScreen.LogStep("DataBundleRuntime.Initialize");
		yield return SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.StartCoroutine(CheckForUpdates());
		LoadingScreen.LogStep("CheckForUpdates");
		SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.Init();
		LoadingScreen.LogStep("ApplicationUtilities.Instance.Init");
		DataBundleRuntime.PreloadItemRecords<DynamicEnum>();
		DataBundleRuntime.PreloadItemRecords<UMusicEventSchema>();
		DataBundleRuntime.PreloadItemRecords<USoundBusSchema>();
		DataBundleRuntime.PreloadItemRecords<USoundThemeSetSchema>();
		DataBundleRuntime.PreloadItemRecords<USoundThemeEventSetSchema>();
		DataBundleRuntime.PreloadItemRecords<USoundThemeClipsetSchema>();
		DataBundleRuntime.PreloadItemRecords<GluiAtlasedTextureSchema>();
		DataBundleRuntime.PreloadItemRecords<GluiState_MetadataSchema>();
		DataBundleRuntime.PreloadItemRecords<LeadershipSchema>();
		DataBundleRuntime.PreloadItemRecords<TextDBSchema>();
		DataBundleRuntime.PreloadItemRecords<AIEnemySchema>();
		DataBundleRuntime.PreloadItemRecords<MultiplayerAIOpponentSchema>();
		DataBundleRuntime.PreloadItemRecords<MultiplayerTweakSchema>();
		DataBundleRuntime.PreloadItemRecords<DesignerVariablesSchema>();
		LoadingScreen.LogStep("DataBundleRuntime.PreloadItemRecords");
		SingletonSpawningMonoBehaviour<GluIap>.Instance.Init();
		LoadingScreen.LogStep("GluIap.Instance.Init");
		upgradeData = new DataBundleTableHandle<UpgradesSchema>("Upgrades");
		LoadingScreen.LogStep("DataBundleTableHandle<UpgradesSchema>");
		if (Debug.isDebugBuild)
		{
			if (SingletonSpawningMonoBehaviour<DebugMain>.Instance == null)
			{
			}
			LoadingScreen.LogStep("DebugMain.Instance");
		}
		CFlurry.SetSessionReportsOnPauseEnabled(true);
		multiplayerData = SaveProvider.Create<MultiplayerData>("MultiplayerData");
		multiplayerData.Init();
		LoadingScreen.LogStep("multiplayerData.Init");
		SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("GAME_VERSION", string.Format(StringUtils.GetStringFromStringRef("MenuFixedStrings", "LegalText_Version"), GeneralConfig.Version, GeneralConfig.DataVersion));
		SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("PUSH_NOTIFICATION_TEXT", StringUtils.GetStringFromStringRef("MenuFixedStrings", (!PushNotification.IsEnabled()) ? "Menu_Off" : "Menu_On"));
		SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("GAME_MODE", "Button_Story");
		Load(delegate
		{
			LoadingScreen.LogStep("Profile.Load");
			ClearBonusWaveData();
			Initialized = true;
		});
		NUF.CancelAllLocalNotification();
		DecaySchema soulTable = DataBundleRuntime.Instance.InitializeRecord<DecaySchema>("DecayTimers", "Souls");
		if (soulTable != null)
		{
			mMinutesPerSoulTick = soulTable.minutesPerTick;
		}
	}

	private void RefreshDailyChallengeHero()
	{
		DataBundleRecordTable possibleHeroes = dailyChallengeProceduralWaveSchema.possibleHeroes;
		List<string> list = new List<string>(5);
		foreach (string item2 in DataBundleRuntime.Instance.EnumerateRecordKeys(typeof(HeroListSchema), possibleHeroes.RecordTable))
		{
			string item = DataBundleRuntime.RecordKey(item2);
			list.Add(item);
		}
		int count = list.Count;
		int index = dailyRandomizer.NextRange(0, count - 1, this);
		string id = list[index];
		dailyChallengeHeroSchema = Singleton<HeroesDatabase>.Instance[id];
		CurrentHeroId = dailyChallengeHeroSchema.id;
	}

	private void RefreshDailyChallengeHelpers()
	{
		if (dailyChallengeHelpers == null)
		{
			dailyChallengeHelpers = new List<string>(6);
		}
		else
		{
			dailyChallengeHelpers.Clear();
		}
		DataBundleRecordTable possibleHelpers = dailyChallengeProceduralWaveSchema.possibleHelpers;
		List<string> list = new List<string>(10);
		foreach (string item2 in DataBundleRuntime.Instance.EnumerateRecordKeys(typeof(HelperListSchema), possibleHelpers.RecordTable))
		{
			string text = DataBundleRuntime.RecordKey(item2);
			HelperSchema helperSchema = Singleton<HelpersDatabase>.Instance[text];
			DataBundleRecordKey requiredHero = helperSchema.requiredHero;
			if (DataBundleRecordKey.IsNullOrEmpty(requiredHero) || DataBundleRuntime.RecordKey(requiredHero) == dailyChallengeHeroSchema.id)
			{
				list.Add(text);
			}
		}
		while (dailyChallengeHelpers.Count < maxSelectedHelpers && list.Count > 0)
		{
			int num = maxSelectedHelpers - dailyChallengeHelpers.Count;
			for (int i = 0; i < num; i++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				int count = list.Count;
				int index = dailyRandomizer.NextRange(0, count - 1, this);
				string item = list[index];
				dailyChallengeHelpers.Add(item);
				list.RemoveAt(index);
			}
			int num2 = 0;
			while (num2 < dailyChallengeHelpers.Count)
			{
				string id = dailyChallengeHelpers[num2];
				HelperSchema helperSchema2 = Singleton<HelpersDatabase>.Instance[id];
				HelperLevelSchema curLevel = helperSchema2.CurLevel;
				DataBundleRecordKey upgradeAlliesFrom = curLevel.upgradeAlliesFrom;
				bool flag = !DataBundleRecordKey.IsNullOrEmpty(upgradeAlliesFrom);
				BuffSchema buffSchema = curLevel.buffSchema;
				bool flag2 = false;
				if (buffSchema != null || flag)
				{
					flag2 = true;
					string text2 = ((!flag) ? null : DataBundleRuntime.RecordKey(upgradeAlliesFrom));
					for (int j = 0; j < dailyChallengeHelpers.Count; j++)
					{
						string text3 = dailyChallengeHelpers[j];
						if (!string.IsNullOrEmpty(text2) && text2 == text3)
						{
							flag2 = false;
							break;
						}
						if (buffSchema != null)
						{
							HelperSchema helperSchema3 = Singleton<HelpersDatabase>.Instance[text3];
							if (buffSchema.CanBuff(helperSchema3) && helperSchema3 != helperSchema2)
							{
								flag2 = false;
								break;
							}
						}
					}
				}
				if (flag2)
				{
					dailyChallengeHelpers.RemoveAt(num2);
				}
				else
				{
					num2++;
				}
			}
		}
	}

	private void RefreshDailyChallengeAbilities()
	{
		if (dailyChallengeAbilities == null)
		{
			dailyChallengeAbilities = new List<string>(6);
		}
		else
		{
			dailyChallengeAbilities.Clear();
		}
		List<AbilitySchema> list = new List<AbilitySchema>(Singleton<AbilitiesDatabase>.Instance.AllAbilitiesForActiveHero);
		int num = maxSelectedAbilities;
		for (int i = 0; i < num; i++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			int count = list.Count;
			int index = dailyRandomizer.NextRange(0, count - 1, this);
			dailyChallengeAbilities.Add(list[index].id);
			list.RemoveAt(index);
		}
	}

	private void RefreshDailyChallengeEnemies()
	{
		dailyChallengeProceduralWaveSchema.CachedWaveSchema = WaveManager.GetWaveData(dailyChallengeProceduralWaveSchema.wave, dailyRandomizer);
	}

	public static int GetDailyChallengeDaysSinceStart()
	{
		return CurrentTime.DayOfYear;
	}

	public void RefreshDailyChallenge()
	{
		if (mRefreshingDailyChallenge)
		{
			return;
		}
		mRefreshingDailyChallenge = true;
		if (Singleton<Profile>.Instance.waveTypeToPlay == WaveManager.WaveType.Wave_DailyChallenge)
		{
			int dailyChallengeDaysSinceStart = GetDailyChallengeDaysSinceStart();
			if (dailyRandomizer == null || dailyRandomizer.seed != dailyChallengeDaysSinceStart)
			{
				dailyRandomizer = new SeededRandomizer(dailyChallengeDaysSinceStart);
				dailyRandomizer.PushRandomSession(this);
				DateTime currentTime = CurrentTime;
				int month = currentTime.Month;
				int day = currentTime.Day;
				DataBundleRecordTable dataBundleRecordTable = new DataBundleRecordTable("DailyChallengeSchedule");
				ProceduralWaveSelectionSchema proceduralWaveSelectionSchema = null;
				foreach (ProceduralWaveSelectionSchema item in dataBundleRecordTable.EnumerateRecords<ProceduralWaveSelectionSchema>())
				{
					int num = DynamicEnum.ToIndex(item.startMonth);
					if (num >= 0)
					{
						num++;
					}
					int num2 = DynamicEnum.ToIndex(item.endMonth);
					if (num2 >= 0)
					{
						num2++;
					}
					if (month >= num && month <= num2 && day >= item.startDay && day <= item.endDay)
					{
						proceduralWaveSelectionSchema = item;
					}
				}
				DataBundleRecordTable proceduralWavePool = proceduralWaveSelectionSchema.proceduralWavePool;
				ProceduralWaveListSchema[] array = proceduralWavePool.InitializeRecords<ProceduralWaveListSchema>();
				List<ProceduralWaveSchema> list = new List<ProceduralWaveSchema>();
				ProceduralWaveListSchema[] array2 = array;
				foreach (ProceduralWaveListSchema proceduralWaveListSchema in array2)
				{
					ProceduralWaveSchema proceduralWaveSchema = proceduralWaveListSchema.proceduralWave.InitializeRecord<ProceduralWaveSchema>();
					if (!proceduralWaveSchema.disabled)
					{
						list.Add(proceduralWaveSchema);
					}
				}
				dailyChallengeProceduralWaveSchema = list[dailyChallengeDaysSinceStart % list.Count];
				RefreshDailyChallengeHero();
				RefreshDailyChallengeHelpers();
				RefreshDailyChallengeAbilities();
				RefreshDailyChallengeEnemies();
				dailyRandomizer.PopRandomSession(this);
			}
		}
		else
		{
			dailyChallengeProceduralWaveSchema = null;
			dailyChallengeHeroSchema = null;
			dailyChallengeHelpers.Clear();
			dailyChallengeHelpers = null;
			dailyChallengeAbilities.Clear();
			dailyChallengeAbilities = null;
			dailyRandomizer = null;
		}
		mRefreshingDailyChallenge = false;
	}

	public static IEnumerator CheckForUpdates()
	{
		SingletonSpawningMonoBehaviour<UpdateSystem>.Instance.DataBundlesModified = false;
		DateTime now = DateTime.Now;
		bool forceUpdate = false;
		if (!timeBetweenUpdateChecks.HasValue)
		{
			string minutesString = ConfigSchema.Entry("CheckForUpdatesMinutes");
			if (!string.IsNullOrEmpty(minutesString))
			{
				timeBetweenUpdateChecks = TimeSpan.FromMinutes(double.Parse(minutesString));
			}
			else
			{
				timeBetweenUpdateChecks = TimeSpan.FromMinutes(60.0);
				forceUpdate = true;
			}
		}
		if (lastUpdateCheck.HasValue && !(now - lastUpdateCheck.Value >= timeBetweenUpdateChecks.Value))
		{
			yield break;
		}
		yield return SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.StartCoroutine(SingletonSpawningMonoBehaviour<UpdateSystem>.Instance.BeginUpdate(true));
		if (SingletonSpawningMonoBehaviour<UpdateSystem>.Instance.Error != null)
		{
			switch (SingletonSpawningMonoBehaviour<UpdateSystem>.Instance.Error.type)
			{
			case UpdateSystem.UpdateError.ErrorType.AppBundleAssetMissing:
			case UpdateSystem.UpdateError.ErrorType.CacheMissingFile:
				ShowRestartPopup();
				while (true)
				{
					yield return null;
				}
			}
		}
		if (SingletonSpawningMonoBehaviour<UpdateSystem>.Instance.DataBundlesModified || forceUpdate)
		{
			DataBundleRuntime.Instance.Reinitialize();
			ConfigSchema.Init();
		}
	}

	public static void ShowForceUpdatePopup()
	{
	}

	private static void OnForceUpdateAccept(int num)
	{
		Application.OpenURL(GeneralConfig.ForcedUpdateURL);
		ShowForceUpdatePopup();
	}

	public static void ShowRestartPopup()
	{
	}

	private static void OnRestartAccept(int num)
	{
		Application.Quit();
	}

	public void Save()
	{
		Save(true);
	}

	public void Save(bool saveScore)
	{
		if (Loading) return;

		if (SingletonSpawningMonoBehaviour<SaveManager>.Exists)
		{
			lock (_lock)
			{
				SingletonSpawningMonoBehaviour<SaveManager>.Instance.StartCoroutine(SaveInternal());
				AJavaTools.Backup.DataChanged();
			}
		}

		if (AJavaTools.Properties.IsBuildAmazon())
		{
			Amazon.SubmitScore(kPlayerRatingLeaderboard, mPlayerAttackRating);
		}
	}

	private IEnumerator SaveInternal()
	{
		timeStamp = ApplicationUtilities.Now;
		if (SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.SNTPTime.SNTPSuccessful)
		{
			sntpTimeStamp = SntpTime.UniversalTime;
		}
		CalculateAttackRating();
		mSavedData.GetDeviceData("local").Current["attackRating"] = playerAttackRating;
		mSavedData.Save("local");
		if (iCloudEnabled)
		{
			mSavedData.GetDeviceData("cloud").Current["attackRating"] = playerAttackRating;
			mSavedData.Save("cloud");
		}
		yield break;
	}

	public void Load()
	{
		Load(null);
	}

	private void Load(Action onComplete)
	{
		Loading = true;
		lock (_lock)
		{
			Load("local", onComplete);
		}
	}

	public void SaveToCloud()
	{
		timeStamp = ApplicationUtilities.Now;
		if (SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.SNTPTime.SNTPSuccessful)
		{
			sntpTimeStamp = SntpTime.UniversalTime;
		}
		CalculateAttackRating();
		mSavedData.GetDeviceData("cloud").Current["attackRating"] = playerAttackRating;
		mSavedData.Save("cloud");
		mCloudSavedData.Data = mSavedData.Data.Clone();
	}

	public void LoadCloudSave(Action<SaveProvider.Result> onComplete)
	{
		CloudSaveResult = SaveProvider.Result.None;
		if (GeneralConfig.iCloudEnabled && FileManager.CheckCloudStorageAvailability())
		{
			SetupCloudSave();
			mCloudSavedData.Load("cloud", delegate(SaveProvider.Result result)
			{
				CloudSaveResult = result;
				if (onComplete != null)
				{
					onComplete(result);
				}
			});
		}
		else
		{
			CloudSaveResult = SaveProvider.Result.Invalid;
			if (onComplete != null)
			{
				onComplete(SaveProvider.Result.Invalid);
			}
		}
	}

	public void CopyCloudSaveToLocal()
	{
		SetupCloudSave();
		mSavedData.Data = mCloudSavedData.Data.Clone();
		PostLoadSyncing();
	}

	private void ReconcileSaveConflict(DeviceData localDeviceData, DeviceData cloudDeviceData, Action<bool> onComplete)
	{
		DateTime localSaveTime = localDeviceData.Current.SaveTime.ToLocalTime();
		DateTime cloudSaveTime = cloudDeviceData.Latest.SaveTime.ToLocalTime();
		Action localConfirm = delegate
		{
			NUF.PopUpAlert(null, string.Format("Are you sure you want to keep your Local Save?\n\n{0}\n\nYOUR iCLOUD SAVE WILL BE OVERWRITTEN", localSaveTime.ToString()), "Cancel", new string[1] { "OK" }, delegate(int buttonIndex)
			{
				if (buttonIndex == 0)
				{
					ReconcileSaveConflict(localDeviceData, cloudDeviceData, onComplete);
				}
				else
				{
					onComplete(true);
				}
			});
		};
		Action cloudConfirm = delegate
		{
			NUF.PopUpAlert(null, string.Format("Are you sure you want to keep your iCloud Save?\n\n{0}\n\nYOUR LOCAL SAVE WILL BE OVERWRITTEN", cloudSaveTime.ToString()), "Cancel", new string[1] { "OK" }, delegate(int buttonIndex)
			{
				if (buttonIndex == 0)
				{
					ReconcileSaveConflict(localDeviceData, cloudDeviceData, onComplete);
				}
				else
				{
					onComplete(false);
				}
			});
		};
		NUF.PopUpAlert(null, "Your local save file and your iCloud save file could not be reconciled.\nWhich one would you like to keep?", "Disable iCloud storage", new string[2]
		{
			string.Format("Local @ {0}", localSaveTime.ToString()),
			string.Format("Cloud @ {0}", cloudSaveTime.ToString())
		}, delegate(int buttonIndex)
		{
			switch (buttonIndex)
			{
			case 0:
				iCloudEnabled = false;
				onComplete(true);
				break;
			case 2:
				cloudConfirm();
				break;
			default:
				localConfirm();
				break;
			}
		});
	}

	private void Load(string targetName, Action onComplete)
	{
		mSavedData.Load(targetName, delegate(SaveProvider.Result result)
		{
			Loading = false;
			if (result != SaveProvider.Result.Success)
			{
				ResetData();
			}
			PostLoadSyncing();
			if (onComplete != null)
			{
				onComplete();
			}
		});
	}

	public void LoadBonusWaveData(SDFTreeNode data)
	{
	}

	public void ClearBonusWaveData()
	{
	}

	public void ForceActiveSaveData(bool bonusWaveData)
	{
	}

	public void ResetData()
	{
		int num3 = latestOnlineBundleVersion;
		List<string> achievementsDisplayed = GetAchievementsDisplayed();
		mSavedData.Clear();
		timeStamp = ApplicationUtilities.Now;
		if (SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.SNTPTime.SNTPSuccessful)
		{
			sntpTimeStamp = SntpTime.UniversalTime;
		}
		coins = 0;
		latestOnlineBundleVersion = num3;
		SetAchievementsDisplayed(achievementsDisplayed);
		coins = SingletonSpawningMonoBehaviour<DesignerVariables>.Instance.GetVariable("StartingCoins", 0);
		souls = SingletonSpawningMonoBehaviour<DesignerVariables>.Instance.GetVariable("StartingSouls", 0);
		PostLoadSyncing();
		Save();
	}

	public void LoadFrontEndData()
	{
		upgradeData.Load(DataBundleResourceGroup.FrontEnd, false, null);
		MultiplayerData.LoadFrontEndResourceData();
	}

	public void LoadInGameData()
	{
	}

	public void UnloadData()
	{
		upgradeData.Unload();
		MultiplayerData.UnloadResourceData();
	}

	public int PlayerAttackCategoryRating(EAttackRatingCategory category)
	{
		return mAttackRatingCategory[(int)category];
	}

	private EAttackRatingCategory GetHeroCategory(string CurrentHeroId)
	{
		switch (CurrentHeroId)
		{
		case "HeroBalanced":
			return EAttackRatingCategory.kSamurai;
		case "HeroAttack":
			return EAttackRatingCategory.kKunoichi;
		case "HeroDefense":
			return EAttackRatingCategory.kRonin;
		case "HeroAbility":
			return EAttackRatingCategory.kSorceress;
		case "HeroLeadership":
			return EAttackRatingCategory.kDaimyo;
		default:
			return EAttackRatingCategory.kGlobal;
		}
	}

	private void CalculateAttackRating()
	{
		mPlayerAttackRating = globalPlayerRating + dailyRewardRating + multiplayerWinRating - multiplayerLossRating - collectionItemLossRating + achievementRating;
		int num = 0;
		int num2 = 0;
		int[] array = new int[14];
		array[6] += globalPlayerRating;
		array[10] += dailyRewardRating;
		array[8] += multiplayerWinRating;
		array[12] += multiplayerLossRating;
		array[13] += collectionItemLossRating;
		array[9] += achievementRating;
		string[] allIDs = Singleton<HeroesDatabase>.Instance.AllIDs;
		foreach (string text in allIDs)
		{
			int num3 = 0;
			if (!Singleton<HeroesDatabase>.Instance[text].Locked)
			{
				num3 += GetHeroLevel(text) * 10;
				num3 += GetMeleeWeaponLevel(text) * 10;
				num3 += GetRangedWeaponLevel(text) * 10;
				num3 += GetArmorLevel(text) * 10;
				EAttackRatingCategory heroCategory = GetHeroCategory(text);
				array[(int)heroCategory] += num3;
			}
			num2++;
			num += num3;
		}
		string[] allIDs2 = Singleton<HelpersDatabase>.Instance.allIDs;
		foreach (string helperID in allIDs2)
		{
			int num4 = GetHelperLevel(helperID) * 10;
			num += num4;
			array[5] += num4;
		}
		string[] allIDs3 = Singleton<AbilitiesDatabase>.Instance.allIDs;
		foreach (string text2 in allIDs3)
		{
			AbilitySchema abilitySchema = Singleton<AbilitiesDatabase>.Instance[text2];
			if (abilitySchema == null)
			{
				continue;
			}
			EAttackRatingCategory eAttackRatingCategory = EAttackRatingCategory.kGlobal;
			bool flag = abilitySchema.EquipLocked;
			if (!string.IsNullOrEmpty(abilitySchema.exclusiveHero.Key))
			{
				HeroSchema heroSchema = Singleton<HeroesDatabase>.Instance[abilitySchema.exclusiveHero.Key];
				if (heroSchema != null)
				{
					if (heroSchema.Locked)
					{
						flag = true;
					}
					else
					{
						eAttackRatingCategory = GetHeroCategory(heroSchema.id);
					}
				}
			}
			if (!flag)
			{
				int num5 = GetAbilityLevel(text2) * 10;
				array[(int)eAttackRatingCategory] += num5;
				num += num5;
			}
		}
		foreach (CollectionSchema collectionDatum in MultiplayerData.CollectionData)
		{
			int num6 = MultiplayerData.TotalTimesCompletedSet(collectionDatum.id);
			int num7 = 0;
			CollectionItemSchema[] items = collectionDatum.Items;
			foreach (CollectionItemSchema collectionItemSchema in items)
			{
				num7 += collectionItemSchema.soulsToAttack;
			}
			int num8 = num6 * num7 / 10;
			array[8] += num8;
			num += num8;
		}
		int num9 = baseLevel * 50;
		int num10 = bellLevel * 50;
		int num11 = pitLevel * 50;
		int num12 = InitialLeadershipLevel * 50;
		array[6] += num9 + num10 + num12 + num11;
		num += num9 + num10 + num12 + num11;
		int num13 = maxStoryModeWave;
		for (int m = 1; m <= num13; m++)
		{
			int num14 = Mathf.Max(0, GetWaveCompletionCount(m, null) - 1);
			int num15 = Mathf.Max(0, GetWaveAttemptCount(m, null) - num14);
			int num16 = num14 * m;
			int num17 = num15 * 5;
			array[7] += num16;
			array[11] += num17;
			num += num16;
			num -= num17;
		}
		mPlayerAttackRating += num;
		mPlayerAttackRating = Mathf.Max(0, mPlayerAttackRating);
		for (int n = 0; n < 14; n++)
		{
			if (mAttackRatingCategory[n] != array[n])
			{
				mAttackRatingCategory[n] = array[n];
			}
		}
		if (FrontEnd_HUD.Instance != null)
		{
			FrontEnd_HUD.Instance.UpdatePowerRating();
		}
		SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("ATTACK_RATING", StringUtils.FormatAmountString(mPlayerAttackRating));
		Singleton<Achievements>.Instance.CheckThresholdAchievement("PlayerRating1", mPlayerAttackRating);
		Singleton<Achievements>.Instance.CheckThresholdAchievement("PlayerRating2", mPlayerAttackRating);
		Singleton<Achievements>.Instance.CheckThresholdAchievement("PlayerRating3", mPlayerAttackRating);
		Singleton<Achievements>.Instance.CheckThresholdAchievement("PlayerRating4", mPlayerAttackRating);
	}

	public void AddCoins(int amount, string source)
	{
		int num = coins;
		coins += amount;
		int num2 = coins - num;
		if (num2 > 0)
		{
			if (source == "IAP")
			{
				mJustMadeSC_IAP = true;
			}
			Singleton<Analytics>.Instance.KontagentEvent("Source", "SINK_SOURCE", "SC", Singleton<Profile>.Instance.CurrentStoryWave, amount, Analytics.KParam("Source", source));
		}
	}

	public void SpendCoins(int amount)
	{
		coins -= amount;
		lifetimeSCSpent += amount;
	}

	public int GetMaxSouls()
	{
		UpgradesSchema upgradesSchema = GetUpgradeData("SoulJar");
		if (upgradesSchema == null)
		{
			return 0;
		}
		switch (GetUpgradeLevel("SoulJar"))
		{
		case 0:
			return Mathf.CeilToInt(upgradesSchema.startingAmount);
		case 1:
			return Mathf.CeilToInt(upgradesSchema.amountLevel1);
		case 2:
			return Mathf.CeilToInt(upgradesSchema.amountLevel2);
		case 3:
			return Mathf.CeilToInt(upgradesSchema.amountLevel3);
		default:
			return Mathf.CeilToInt(upgradesSchema.amountLevel4);
		}
	}

	public int GetHeroLevel(string id)
	{
		return Mathf.Max(1, mSavedData.GetValueInt(heroStat(id, "CurrentHeroLevel"), playModeSubSection));
	}

	public int SetHeroLevel(string id, int value)
	{
		int num = Mathf.Max(0, value);
		mSavedData.SetValueInt(heroStat(id, "CurrentHeroLevel"), num, playModeSubSection);
		CalculateAttackRating();
		return num;
	}

	public string GetCostume(string characterId)
	{
		string result = mSavedData.GetValue(characterId + ".Costume");
		return (result != string.Empty) ? result : "Normal";
	}

	public void SetCostume(string characterId, string costumeId)
	{
		mSavedData.SetValue(characterId + ".Costume", costumeId);
	}

	public bool GetHeroPurchased(string id)
	{
		return mSavedData.GetValueBool(heroStat(id, "purchased"));
	}

	public void SetHeroPurchased(string id, bool purchased)
	{
		mSavedData.SetValueBool(heroStat(id, "purchased"), purchased, playModeSubSection);
		CalculateAttackRating();
	}

	public int GetLeadershipLevel(string CurrentHeroId)
	{
		return Mathf.Max(0, mSavedData.GetValueInt(heroStat(CurrentHeroId, "InitialLeadershipLevel"), playModeSubSection));
	}

	public int SetLeadershipLevel(string CurrentHeroId, int value)
	{
		int num = Mathf.Max(0, value);
		mSavedData.SetValueInt(heroStat(CurrentHeroId, "InitialLeadershipLevel"), num, playModeSubSection);
		CalculateAttackRating();
		return num;
	}

	public int GetSoulsLevel(string CurrentHeroId)
	{
		return Mathf.Max(1, mSavedData.GetValueInt(heroStat(CurrentHeroId, "SoulsLevel"), playModeSubSection));
	}

	public int SetSoulsLevel(string CurrentHeroId, int value)
	{
		int num = Mathf.Max(1, value);
		mSavedData.SetValueInt(heroStat(CurrentHeroId, "SoulsLevel"), num, playModeSubSection);
		return num;
	}

	public int GetMeleeWeaponLevel(string CurrentHeroId)
	{
		return Mathf.Max(1, mSavedData.GetValueInt(heroStat(CurrentHeroId, "SwordLevel"), playModeSubSection));
	}

	public int SetMeleeWeaponLevel(string CurrentHeroId, int value)
	{
		int num = Mathf.Max(0, value);
		mSavedData.SetValueInt(heroStat(CurrentHeroId, "SwordLevel"), num, playModeSubSection);
		CalculateAttackRating();
		return num;
	}

	public int GetRangedWeaponLevel(string CurrentHeroId)
	{
		return Mathf.Max(1, mSavedData.GetValueInt(heroStat(CurrentHeroId, "BowLevel"), playModeSubSection));
	}

	public int SetRangedWeaponLevel(string CurrentHeroId, int value)
	{
		int num = Mathf.Max(0, value);
		mSavedData.SetValueInt(heroStat(CurrentHeroId, "BowLevel"), num, playModeSubSection);
		CalculateAttackRating();
		return num;
	}

	public int GetArmorLevel(string CurrentHeroId)
	{
		return Mathf.Max(1, mSavedData.GetValueInt(heroStat(CurrentHeroId, "armorLevel"), playModeSubSection));
	}

	public int SetArmorLevel(string CurrentHeroId, int value)
	{
		int num = Mathf.Max(0, value);
		HeroSchema heroSchema = Singleton<HeroesDatabase>.Instance[CurrentHeroId];
		if (heroSchema != null && heroSchema.ArmorLevels != null)
		{
			num = Mathf.Min(heroSchema.ArmorLevels.Length, num);
		}
		mSavedData.SetValueInt(heroStat(CurrentHeroId, "armorLevel"), num, playModeSubSection);
		CalculateAttackRating();
		return num;
	}

	public void AddSeenEnemies(List<string> enemies)
	{
		List<string> simpleList = mSavedData.GetSimpleList("seenEnemies");
		bool flag = false;
		foreach (string enemy in enemies)
		{
			if (!simpleList.Contains(enemy))
			{
				simpleList.Add(enemy);
				flag = true;
			}
		}
		if (flag)
		{
			mSavedData.SetSimpleList("seenEnemies", simpleList);
		}
	}

	public bool IsEventRewardsGiven(string eventRewardID)
	{
		return ActiveSavedData.GetDictionaryValue<bool>("eventRewards", eventRewardID);
	}

	public void SetEventRewardsGiven(string eventRewardID)
	{
		ActiveSavedData.SetDictionaryValue("eventRewards", eventRewardID, true);
	}

	public int GetHelperLevel(string helperID)
	{
		return Mathf.Clamp(ActiveSavedData.GetDictionaryValue<int>("helperLevels", helperID, playModeSubSection), 0, Singleton<HelpersDatabase>.Instance.GetMaxLevel(helperID));
	}

	public int GetRawHelperLevel(string helperID)
	{
		return ActiveSavedData.GetDictionaryValue<int>("helperLevels", helperID, playModeSubSection);
	}

	public void SetHelperLevel(string helperID, int val, bool autoFillSelectedHelpers = true)
	{
		val = Mathf.Clamp(val, 0, Singleton<HelpersDatabase>.Instance.GetMaxLevel(helperID));
		if (autoFillSelectedHelpers && GetHelperLevel(helperID) == 0 && val > 0)
		{
			List<string> selectedHelpers = GetSelectedHelpers();
			if (selectedHelpers.Count < maxSelectedHelpers)
			{
				selectedHelpers.Add(helperID);
				SetSelectedHelpers(selectedHelpers);
			}
		}
		ActiveSavedData.SetDictionaryValue("helperLevels", helperID, val, playModeSubSection);
		CalculateAttackRating();
	}

	public List<string> GetSelectedHelpers()
	{
		if (ChangingDefenseLoadout)
		{
			return GetSelectedDefendHelpers();
		}
		if (IsInDailyChallenge)
		{
			return FilterOutDuplicateIDs(ActiveSavedData.GetSubNodeValueList("dailychallenge_selectedHelpers", playModeSubSection));
		}
		return FilterOutDuplicateIDs(ActiveSavedData.GetSubNodeValueList(heroStat(CurrentHeroId, "selectedHelpers"), playModeSubSection));
	}

	public void SetSelectedHelpers(List<string> helpers)
	{
		FilterOutDuplicateIDs(helpers);
		if (ChangingDefenseLoadout)
		{
			SetSelectedDefendHelpers(helpers);
			multiplayerData.LocalPlayerLoadout.UpdateLocalProfile();
		}
		else if (IsInDailyChallenge)
		{
			ActiveSavedData.SetSubNodeValueList("dailychallenge_selectedHelpers", helpers, playModeSubSection);
		}
		else
		{
			ActiveSavedData.SetSubNodeValueList(heroStat(CurrentHeroId, "selectedHelpers"), helpers, playModeSubSection);
		}
	}

	public List<string> GetSelectedDefendHelpers()
	{
		return FilterOutDuplicateIDs(ActiveSavedData.GetSubNodeValueList("defendHelpers"));
	}

	public void SetSelectedDefendHelpers(List<string> helpers)
	{
		FilterOutDuplicateIDs(helpers);
		ActiveSavedData.SetSubNodeValueList("defendHelpers", helpers);
	}

	public bool GetGoldenHelperUnlocked(string helperID)
	{
		return ActiveSavedData.GetDictionaryValue<bool>("goldenHelperUnlocked", helperID, playModeSubSection);
	}

	public void SetGoldenHelperUnlocked(string helperID, bool unlocked)
	{
		ActiveSavedData.SetDictionaryValue("goldenHelperUnlocked", helperID, unlocked, playModeSubSection);
		CalculateAttackRating();
	}

	public int GetAbilityLevel(string abilityID)
	{
		return ActiveSavedData.GetDictionaryValue<int>("abilityLevels", abilityID, playModeSubSection);
	}

	public void SetAbilityLevel(string abilityID, int val)
	{
		ActiveSavedData.SetDictionaryValue("abilityLevels", abilityID, val, playModeSubSection);
		CalculateAttackRating();
	}

	public List<string> GetSelectedAbilities()
	{
		if (ChangingDefenseLoadout)
		{
			return GetSelectedDefendAbilities();
		}
		if (IsInDailyChallenge)
		{
			return FilterOutDuplicateIDs(ActiveSavedData.GetSubNodeValueList("dailychallenge_selectedAbilities", playModeSubSection));
		}
		return FilterOutDuplicateIDs(ActiveSavedData.GetSubNodeValueList(heroStat(CurrentHeroId, "selectedAbilities"), playModeSubSection));
	}

	public void SetSelectedAbilities(List<string> abilities)
	{
		if (ChangingDefenseLoadout)
		{
			SetSelectedDefendAbilities(abilities);
			multiplayerData.LocalPlayerLoadout.UpdateLocalProfile();
		}
		else if (IsInDailyChallenge)
		{
			ActiveSavedData.SetSubNodeValueList("dailychallenge_selectedAbilities", FilterOutDuplicateIDs(abilities), playModeSubSection);
		}
		else
		{
			ActiveSavedData.SetSubNodeValueList(heroStat(CurrentHeroId, "selectedAbilities"), FilterOutDuplicateIDs(abilities), playModeSubSection);
		}
	}

	public List<string> GetSelectedDefendAbilities()
	{
		return FilterOutDuplicateIDs(ActiveSavedData.GetSubNodeValueList("defendAbilities"));
	}

	public void SetSelectedDefendAbilities(List<string> abilities)
	{
		ActiveSavedData.SetSubNodeValueList("defendAbilities", FilterOutDuplicateIDs(abilities));
	}

	public void AutoEquipNewAbilities(int waveIndex, string heroJustUnlocked = null)
	{
		string[] allIDs = Singleton<AbilitiesDatabase>.Instance.allIDs;
		foreach (string text in allIDs)
		{
			AbilitySchema abilitySchema = Singleton<AbilitiesDatabase>.Instance[text];
			if ((abilitySchema.levelToUnlock != (float)waveIndex && abilitySchema.levelToUnlock != 0f) || (abilitySchema.levelToUnlock == 0f && heroJustUnlocked == null))
			{
				continue;
			}
			List<HeroSchema> list = Singleton<AbilitiesDatabase>.Instance.HeroesUsingAbility(text);
			if (list.Count <= 0)
			{
				continue;
			}
			string text2 = CurrentHeroId;
			foreach (HeroSchema item in list)
			{
				CurrentHeroId = item.id;
				List<string> selectedAbilities = GetSelectedAbilities();
				if ((abilitySchema.levelToUnlock != 0f || !(CurrentHeroId != heroJustUnlocked)) && selectedAbilities.Count < maxSelectedAbilities)
				{
					selectedAbilities.Add(text);
					SetSelectedAbilities(selectedAbilities);
				}
			}
			CurrentHeroId = text2;
		}
	}

	public int GetNumCharms(string charmID)
	{
		return mSavedData.GetDictionaryValue<int>("numCharms", charmID);
	}

	public void SetNumCharms(string charmID, int val)
	{
		mSavedData.SetDictionaryValue("numCharms", charmID, val);
	}

	public int GetNumPotions(string potionID)
	{
		if (potionID.Equals("souls"))
		{
			return souls;
		}
		return mSavedData.GetDictionaryValue<int>("numPotions", potionID);
	}

	public void SetNumPotions(string potionID, int val)
	{
		if (potionID.Equals("souls"))
		{
			souls = val;
			return;
		}
		mSavedData.SetDictionaryValue("numPotions", potionID, val);
		if (SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Exists && potionID.Equals("revivePotion"))
		{
			SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("revivePotion", val);
		}
	}

	public bool IsChapterAvailable(string chapterID)
	{
		return mSavedData.GetDictionaryValue<bool>("chapterUnlock", chapterID, playModeSubSection);
	}

	public void SetChapterAvailable(string chapterID, bool val)
	{
		mSavedData.SetDictionaryValue("chapterUnlock", chapterID, val, playModeSubSection);
	}

	public bool GetIsWaveUnlocked(int waveIndex)
	{
		return GetIsWaveUnlocked(waveIndex, playModeSubSection);
	}
	public bool GetIsWaveUnlocked(int waveIndex, string playMode)
	{
		return mSavedData.GetDictionaryValue<bool>("wavesUnlocked", "w" + waveIndex, playMode);
	}
	public void SetIsWaveUnlocked(int waveIndex, bool isWaveUnlocked)
	{
		mSavedData.SetDictionaryValue("wavesUnlocked", "w" + waveIndex, isWaveUnlocked, playModeSubSection);

		if (waveIndex > mHighestUnlockedWave)
		{
			mHighestUnlockedWave = waveIndex;
		}
	}

	public int GetWaveCompletionCount(int waveIndex)
	{
		return GetWaveCompletionCount(waveIndex, playModeSubSection);
	}
	public int GetWaveCompletionCount(int waveIndex, string playMode)
	{
		return mSavedData.GetDictionaryValue<int>("waveCompletionCounts", "w" + waveIndex, playMode);
	}
	public void SetWaveCompletionCount(int waveIndex, int completionCount)
	{
		if (GetWaveCompletionCount(waveIndex) >= completionCount) return;

		mSavedData.SetDictionaryValue("waveCompletionCounts", "w" + waveIndex, completionCount, playModeSubSection);

		if (waveIndex == maxBaseWave && completionCount == 1)
		{
			ApplicationUtilities.MakePlayHavenContentRequest("wave_won_50");
		}
	}
	public void IncrementWaveCompletionCount(int waveIndex)
	{
		SetWaveCompletionCount(waveIndex, GetWaveCompletionCount(waveIndex) + 1);
	}

	public bool HasWaveBeenCompleted(int waveIndex)
	{
		return GetWaveCompletionCount(waveIndex) >= 2;
	}

	public void GoToNextWave()
	{
		if (IsInMultiplayerWave || IsInDailyChallenge) return;

		IncrementWaveCompletionCount(CurrentStoryWave);
		CurrentStoryWave++;
		SetIsWaveUnlocked(CurrentStoryWave, true);
		CurrentHeroId = WaveManager.GetWaveData(CurrentStoryWave, waveTypeToPlay).recommendedHero.Key;

		var wavePlayed = Singleton<PlayStatistics>.Instance.data.wavePlayed;
		if (HasWaveBeenCompleted(wavePlayed))
		{
			ResultsMenuImpl.UnlockedFeature unlockedHero = ResultsMenuImpl.GetUnlockedHero();
			AutoEquipNewAbilities(wavePlayed + 1, (unlockedHero != null) ? unlockedHero.id : null);
		}

		Save();
	}

	private void CalcHighestWave()
	{
		mHighestUnlockedWave = 1;
		for (int i = 1; i <= 999; i++)
		{
			if (!GetIsWaveUnlocked(i))
			{
				mHighestUnlockedWave = Mathf.Max(1, i - 1);
				return;
			}
		}
		mHighestUnlockedWave = 999;
	}

	public int GetWaveAttemptCount(int waveIndex)
	{
		return GetWaveAttemptCount(waveIndex, playModeSubSection);
	}

	public int GetWaveAttemptCount(int waveIndex, string playMode)
	{
		return mSavedData.GetDictionaryValue<int>("waveAttemptCounts", "w" + waveIndex, playMode);
	}

	public void IncrementWaveAttemptCount(int waveIndex)
	{
		mSavedData.SetDictionaryValue("waveAttemptCounts", "w" + waveIndex, GetWaveAttemptCount(waveIndex) + 1, playModeSubSection);
	}

	public int GetMPWaveAttemptCount(string mpMission)
	{
		return mSavedData.GetDictionaryValue<int>("multiplayer_waves", "count_" + mpMission, playModeSubSection);
	}

	public void IncrementMPWaveAttemptCount(string mpMission)
	{
		mSavedData.SetDictionaryValue("multiplayer_waves", "count_" + mpMission, GetMPWaveAttemptCount(mpMission) + 1, playModeSubSection);
	}

	public bool IsTutorialDone(string module, string id)
	{
		return mSavedData.GetDictionaryValue<bool>("tutorials", module + "_" + id);
	}

	public void SetTutorialDone(string module, string id)
	{
		mSavedData.SetDictionaryValue("tutorials", module + "_" + id, true);
		if (id.Equals("Store", StringComparison.OrdinalIgnoreCase))
		{
			Singleton<Profile>.Instance.ForceOnboardingStage("OnboardingStep9_StoreMain1");
		}
	}

	public void ClearTutorialDone(string module, string id)
	{
		mSavedData.SetDictionaryValue("tutorials", module + "_" + id, false);
	}

	public List<string> GetAchievementsDisplayed()
	{
		return mSavedData.GetSubNodeValueList("achievementsDisplayed");
	}

	public void SetAchievementsDisplayed(List<string> achievementsDisplayed)
	{
		mSavedData.SetSubNodeValueList("achievementsDisplayed", achievementsDisplayed);
	}

	public void IncNumKillsOfEnemyType(string enemyID)
	{
		switch (enemyID)
		{
		case "Chochinobake":
		case "Chochinobake_alt":
			Singleton<Achievements>.Instance.IncrementAchievement("KillLanterns", 1);
			break;
		case "Harionago":
			Singleton<Achievements>.Instance.IncrementAchievement("KillHarionago", 1);
			break;
		}
	}

	public UpgradesSchema GetUpgradeData(string id)
	{
		if (upgradeData != null && upgradeData.Data != null)
		{
			return Array.Find(upgradeData.Data, (UpgradesSchema s) => string.Equals(id, s.id));
		}
		return null;
	}

	public int GetUpgradeLevel(string id)
	{
		return mSavedData.GetDictionaryValue<int>("upgrades", id);
	}

	public void SetUpgradeLevel(string id, int level)
	{
		level = Mathf.Clamp(level, 0, GetUpgradeData(id).numUpgradeLevels);
		mSavedData.SetDictionaryValue("upgrades", id, level);
		CalculateAttackRating();
		if (id == "SoulCollectionRate")
		{
			float currentUpgradeAmount = Singleton<Profile>.Instance.GetCurrentUpgradeAmount("SoulCollectionRate");
			DecayTimer decayTimer = SingletonSpawningMonoBehaviour<DecaySystem>.Instance.Timers.Find((DecayTimer t) => t.Data.name == "Souls");
			decayTimer.Data.minutesPerTick = currentUpgradeAmount;
		}
	}

	public float GetCurrentUpgradeAmount(string id)
	{
		int upgradeLevel = GetUpgradeLevel(id);
		UpgradesSchema upgradesSchema = GetUpgradeData(id);
		switch (upgradeLevel)
		{
		case 0:
			return upgradesSchema.startingAmount;
		case 1:
			return upgradesSchema.amountLevel1;
		case 2:
			return upgradesSchema.amountLevel2;
		case 3:
			return upgradesSchema.amountLevel3;
		default:
			return upgradesSchema.amountLevel4;
		}
	}

	public bool GetMysteryBoxPurchased()
	{
		return mSavedData.GetValueBool("mysteryBoxPurchased");
	}

	public void SetMysteryBoxPurchased(bool purchased)
	{
		mSavedData.SetValueBool("mysteryBoxPurchased", purchased);
	}

	public void NotifyUniqueIAPPurchase(IAPSchema s)
	{
		if (s.unique)
		{
			List<string> subNodeValueList = ActiveSavedData.GetSubNodeValueList("specialIAPPurchased");
			subNodeValueList.Add(s.productId);
			ActiveSavedData.SetSubNodeValueList("specialIAPPurchased", FilterOutDuplicateIDs(subNodeValueList));
			Save();
		}
	}

	public bool IsUniqueIAPItemAlreadyPurchased(IAPSchema s)
	{
		if (s.unique)
		{
			if (ActiveSavedData.GetSubNodeValueList("specialIAPPurchased").Contains(s.productId))
			{
				return true;
			}
			if (!string.IsNullOrEmpty(s.items))
			{
				string[] array = s.items.Split(',');
				string[] array2 = array;
				foreach (string id in array2)
				{
					if (Singleton<HeroesDatabase>.Instance.Contains(id) && Singleton<Profile>.Instance.GetHeroLevel(id) == 0)
					{
						return false;
					}
				}
			}
		}
		return false;
	}

	public DateTime GetIAPExpireTime(IAPSchema s)
	{
		if (s.hoursToExpire > 0)
		{
			string attrib = "specialIAPExpireTime~" + s.productId;
			string value = ActiveSavedData.GetValue(attrib);
			DateTime result;
			if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out result))
			{
				return result;
			}
		}
		return DateTime.MaxValue;
	}

	public void SetIAPExpireTimeIfNeeded(IAPSchema s)
	{
		if (s.hoursToExpire > 0)
		{
			string attrib = "specialIAPExpireTime~" + s.productId;
			string value = ActiveSavedData.GetValue(attrib);
			if (string.IsNullOrEmpty(value))
			{
				value = SntpTime.UniversalTime.AddHours(s.hoursToExpire).ToString();
				ActiveSavedData.SetValue(attrib, value);
				Save();
			}
		}
	}

	public bool HasIAPExpired(IAPSchema s)
	{
		return s.hoursToExpire > 0 && GetIAPExpireTime(s) < SntpTime.UniversalTime;
	}

	public void UpdateIAPTimers()
	{
		if (SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.SNTPTime == null || !SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.SNTPTime.SNTPSuccessful)
		{
			return;
		}
		foreach (IAPSchema product in SingletonSpawningMonoBehaviour<GluIap>.Instance.Products)
		{
			Singleton<Profile>.Instance.SetIAPExpireTimeIfNeeded(product);
		}
	}

	public void PostLoadSyncing()
	{
		if (waveTypeToPlay == WaveManager.WaveType.Wave_SinglePlayer)
		{
			CurrentStoryWave = ValidateWaveIndex(CurrentStoryWave);
			if (!GetIsWaveUnlocked(1))
			{
				SetIsWaveUnlocked(1, true);
			}
		}
		CalcHighestWave();
		if (highestUnlockedWave == 50 && HasWaveBeenCompleted(50))
		{
			CurrentStoryWave = 51;
		}
		SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("coins", StringUtils.FormatAmountString(Singleton<Profile>.Instance.coins));
		SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("revivePotion", StringUtils.FormatAmountString(Singleton<Profile>.Instance.GetNumPotions("revivePotion")));
		Singleton<Profile>.Instance.souls = Singleton<Profile>.Instance.souls;
		SingletonSpawningMonoBehaviour<GluiPersistentDataCache>.Instance.Save("pachinkoBalls", StringUtils.FormatAmountString(Singleton<Profile>.Instance.pachinkoBalls));
		CalculateAttackRating();
		FlurrySession.StartSession();
		UpdateIAPTimers();
		Singleton<Achievements>.Instance.Initialize();
		string value = mSavedData.GetValue("mpShieldTime");
		DateTime result;
		if (DateTime.TryParseExact(value, "O", null, DateTimeStyles.None, out result))
		{
			mMultiplayerShieldExpireTime = result.ToUniversalTime();
		}
	}

	public void StartDecaySystem()
	{
		if (!SingletonSpawningMonoBehaviour<DecaySystem>.Instance.Initialized && SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.SNTPTime.SNTPSuccessful)
		{
			DateTime value = sntpTimeStamp;
			TimeSpan timeSpan = SntpTime.UniversalTime.Subtract(value);
			SingletonSpawningMonoBehaviour<DecaySystem>.Instance.Initialize();
			float currentUpgradeAmount = Singleton<Profile>.Instance.GetCurrentUpgradeAmount("SoulCollectionRate");
			DecayTimer decayTimer = SingletonSpawningMonoBehaviour<DecaySystem>.Instance.Timers.Find((DecayTimer t) => t.Data.name == "Souls");
			decayTimer.Data.minutesPerTick = currentUpgradeAmount;
			SingletonSpawningMonoBehaviour<DecaySystem>.Instance.FastForward((uint)timeSpan.TotalSeconds, true);
			UnityEngine.Object.DontDestroyOnLoad(SingletonSpawningMonoBehaviour<DecaySystem>.Instance.gameObject);
		}
	}

	private int ValidateWaveIndex(int index)
	{
		if (index < 1)
		{
			return 1;
		}
		if (index > 999)
		{
			index = 1;
		}
		else if (string.IsNullOrEmpty(Singleton<PlayModesManager>.Instance.selectedModeData.endlessWaves) && string.IsNullOrEmpty(Singleton<PlayModesManager>.Instance.selectedModeData.endlessBonusWaves))
		{
			string item = index.ToString();
			List<string> recordKeys = DataBundleRuntime.Instance.GetRecordKeys(typeof(WaveSchema), Singleton<PlayModesManager>.Instance.selectedModeData.waves.RecordTable, false);
			if (!recordKeys.Contains(item))
			{
				index = 1;
			}
		}
		return index;
	}

	private int ValidateBonusWaveIndex(int index)
	{
		if (index < 1)
		{
			return 1;
		}
		return index;
	}

	public static void UpdatePlayMode()
	{
		switch (Singleton<Profile>.Instance.waveTypeToPlay)
		{
		case WaveManager.WaveType.Wave_Multiplayer:
			Singleton<PlayModesManager>.Instance.selectedMode = Singleton<Profile>.Instance.MultiplayerData.MultiplayerGameSessionData.playMode;
			break;
		case WaveManager.WaveType.Wave_DailyChallenge:
			Singleton<PlayModesManager>.Instance.selectedMode = "challenge";
			break;
		default:
			Singleton<PlayModesManager>.Instance.selectedMode = "classic";
			break;
		}
	}

	private void UpgradeWaveRecycling()
	{
		int num = CurrentStoryWave;
		int valueInt = mSavedData.GetValueInt("waveRecycle");
		if (GetIsWaveUnlocked(num))
		{
			return;
		}
		for (int i = 1; i <= 20; i++)
		{
			if (i < num)
			{
				SetWaveCompletionCount(i, valueInt + 2);
			}
			else if (i == num)
			{
				SetWaveCompletionCount(i, valueInt + 1);
			}
			else if (valueInt > 0)
			{
				SetWaveCompletionCount(i, valueInt + 1);
			}
		}
		if (valueInt > 0)
		{
			mSavedData.SetValueInt("waveRecycle", 0);
			CurrentStoryWave = 21;
		}
	}

	public void UpgradeToExtraWaves()
	{
		int num = highestUnlockedWave;
		if (HasWaveBeenCompleted(num))
		{
			int recordTableLength = DataBundleRuntime.Instance.GetRecordTableLength(typeof(WaveSchema), "Waves");
			if (num + 1 < recordTableLength)
			{
				CurrentStoryWave = num + 1;
				SetIsWaveUnlocked(CurrentStoryWave, true);
			}
		}
	}

	public string PrintToString()
	{
		using (StringWriter stringWriter = new StringWriter())
		{
			SDFTree.Save(mSavedData.Data, stringWriter);
			return stringWriter.ToString();
		}
	}

	public void ScheduleNotifications()
	{
		if (!Initialized || !PushNotification.IsEnabled())
		{
			return;
		}
		NUF.CancelAllLocalNotification();
		int num = GetMaxSouls() - souls;
		string stringFromStringRef = StringUtils.GetStringFromStringRef("LocalizedStrings", "tapjoy_awarded_gems_button");
		string stringFromStringRef2;
		if (num > 0 && mMinutesPerSoulTick > 0f)
		{
			float currentUpgradeAmount = GetCurrentUpgradeAmount("SoulCollectionRate");
			int b = (int)((float)num * currentUpgradeAmount);
			b = Mathf.Max(1, b);
			stringFromStringRef2 = StringUtils.GetStringFromStringRef("LocalizedStrings", "Notification_SoulJarFull");
			if (!string.IsNullOrEmpty(stringFromStringRef2))
			{
				NUF.ScheduleNotification(b * 60, stringFromStringRef2, stringFromStringRef, "SvZ2_Custom_Notification.aif");
			}
		}
		stringFromStringRef2 = StringUtils.GetStringFromStringRef("LocalizedStrings", "Notification_ComeBack01");
		if (!string.IsNullOrEmpty(stringFromStringRef2))
		{
			NUF.ScheduleNotification(172800, stringFromStringRef2, stringFromStringRef, "SvZ2_Custom_Notification.aif");
		}
		stringFromStringRef2 = StringUtils.GetStringFromStringRef("LocalizedStrings", "Notification_ComeBack02");
		if (!string.IsNullOrEmpty(stringFromStringRef2))
		{
			NUF.ScheduleNotification(432000, stringFromStringRef2, stringFromStringRef, "SvZ2_Custom_Notification.aif");
		}
		if (MultiplayerData == null || MultiplayerData.CollectionStatus == null)
		{
			return;
		}
		int shortestDefenseDuration = MultiplayerData.CollectionStatus.GetShortestDefenseDuration();
		if (shortestDefenseDuration > 0)
		{
			stringFromStringRef2 = StringUtils.GetStringFromStringRef("LocalizedStrings", "Notification_DefenseRunOut");
			if (!string.IsNullOrEmpty(stringFromStringRef2))
			{
				shortestDefenseDuration = Mathf.Max(15, shortestDefenseDuration - 360);
				NUF.ScheduleNotification(shortestDefenseDuration, stringFromStringRef2, stringFromStringRef, "SvZ2_Custom_Notification.aif");
			}
		}
	}

	private static string heroStat(string CurrentHeroId, string statID)
	{
		return string.Format("{0}.{1}", CurrentHeroId, statID);
	}

	private static List<string> FilterOutDuplicateIDs(List<string> lst)
	{
		for (int num = lst.Count - 1; num >= 0; num--)
		{
			for (int i = 0; i < num; i++)
			{
				if (lst[num] == lst[i])
				{
					lst.RemoveAt(num);
					break;
				}
			}
		}
		return lst;
	}

	public void SetMultiplayerShieldTime(DateTime time)
	{
		DateTime dateTime = time.ToUniversalTime();
		if (dateTime != mMultiplayerShieldExpireTime)
		{
			mMultiplayerShieldExpireTime = dateTime;
			multiplayerData.UpdateShieldTime();
			mSavedData.SetValue("mpShieldTime", time.ToString("O"));
		}
	}

	public void CheckOnboardingStage(string stageName)
	{
		if (onboardingStage < onboardingEvents.Length && !SaveData.GetValueBool(stageName))
		{
			string text = onboardingEvents[onboardingStage];
			if (text == stageName)
			{
				Singleton<Analytics>.Instance.LogEvent(stageName);
				onboardingStage++;
				SaveData.SetValueBool(stageName, true);
				Singleton<Profile>.Instance.Save();
			}
		}
	}

	public void ForceOnboardingStage(string stageName)
	{
		if (onboardingStage >= onboardingEvents.Length || SaveData.GetValueBool(stageName)) return;

		for (int i = 0; i < onboardingEvents.Length; i++)
		{
			if (onboardingEvents[i] == stageName)
			{
				Singleton<Analytics>.Instance.LogEvent(stageName);
				Singleton<Analytics>.Instance.KontagentEvent(stageName, "Tutorial_step_completed", onboardingStage, 0);
				if (onboardingStage <= i)
				{
					onboardingStage = i + 1;
				}
				SaveData.SetValueBool(stageName, true);
				Singleton<Profile>.Instance.Save();
				break;
			}
		}
	}

	public bool IsOnboardingStageComplete(string stageName)
	{
		return SaveData.GetValueBool(stageName);
	}

	public bool UnlockedDailyChallenge()
	{
		return highestUnlockedWave >= 12;
	}

	public bool JustUnlockedDailyChallenge()
	{
		return highestUnlockedWave == 12;
	}

	public void syncCloudDataToLocal(SDFTreeSaveProvider _cloudSaveData)
	{
	}
}
