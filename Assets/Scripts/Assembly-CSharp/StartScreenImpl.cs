using System;
using System.Collections;
using Glu.Plugins.ASocial;
using UnityEngine;

public class StartScreenImpl : MonoBehaviour, IGluiActionHandler
{
	public GluiSprite spriteGameTitle;

	private string titleLogoFileName = "Assets/Game/Resources/UI/textures/StartScreenTemp/Title_English.png";

	private float timeScaleBackup;

	private void Awake()
	{
		DebugMain instance = SingletonSpawningMonoBehaviour<DebugMain>.Instance;
		
		timeScaleBackup = Time.timeScale;
		if (!Singleton<Profile>.Exists)
		{
			StartCoroutine(Singleton<Profile>.Instance.Init());
		}
	}

	private IEnumerator Start()
	{
		string language = BundleUtils.GetSystemLanguage();
		if (language != "English")
		{
			string titleOverrideName = null;
			switch (language)
			{
			case "Chinese (Simplified)":
				titleOverrideName = "Title_ChineseSimplified.png";
				break;
			case "Chinese (Traditional)":
				titleOverrideName = "Title_ChineseTraditional.png";
				break;
			case "Japanese":
				titleOverrideName = "Title_Japanese.png";
				break;
			case "Korean":
				titleOverrideName = "Title_Korean.png";
				break;
			}
			if (titleOverrideName != null)
			{
				ResourceCache.UnCache(titleLogoFileName);
				titleLogoFileName = "Assets/Game/Resources/UI/textures/StartScreenTemp/" + titleOverrideName;
				SharedResourceLoader.SharedResource res = ResourceCache.GetCachedResource(titleLogoFileName, 1);
				spriteGameTitle.Start();
				spriteGameTitle.Texture = res.Resource as Texture2D;
			}
		}
		while (!Singleton<Profile>.Instance.Initialized) yield return null;
		Singleton<Profile>.Instance.ForceOnboardingStage("OnboardingStep2_StartScreen");
		SingletonSpawningMonoBehaviour<ApplicationUtilities>.Instance.canAwardGems = true;
		PlayerPrefs.SetInt("gameLoadedCorrectly", 1);
		PlayerPrefs.SetString("gameTag", AJavaTools.Properties.GetBuildTag());
	}

	private void Update()
	{
		if (ApplicationUtilities.PlayHavenGameLaunchQueued)
		{
			ApplicationUtilities.MakePlayHavenContentRequest("game_launch");
		}
	}

	private void OnDestroy()
	{
		ResourceCache.UnCache(titleLogoFileName);
	}

	public bool HandleAction(string action, GameObject sender, object data)
	{
		if (!Singleton<Profile>.Instance.Initialized)
		{
			return true;
		}
		switch (action)
		{
		case "START_GAME":
			if (!Singleton<Profile>.Instance.HasWaveBeenCompleted(1))
			{
				Singleton<Profile>.Instance.CurrentStoryWave = 1;
				string value = DataBundleRuntime.Instance.GetValue<string>(typeof(WaveSchema), Singleton<PlayModesManager>.Instance.selectedModeData.waves.RecordTable, Singleton<Profile>.Instance.CurrentStoryWave.ToString(), "scene", false);
				LoadingScreen.LoadLevel(value);
			}
			else
			{
				GluiActionSender.SendGluiAction("MENU_MAIN_STORE", sender, data);
			}
			return true;
		case "POPUP_ACHIEVEMENTS":
			return true;
		case "POPUP_LEADERBOARDS":
			return true;
		default:
			return false;
		}
	}

	private void pgsSignInCallback(string info)
	{
		int num = Convert.ToInt32(info);
		if (num == -1)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				AJavaTools.UI.ShowToast(StringUtils.GetStringFromStringRef("LocalizedStrings", "IDS_ICLOUD_REQUIRE_INTERNET_ANDROID"));
			}
		}
		AJavaTools.UI.ShowNotificationPrompt(base.gameObject.name, "setNotification");
	}

	private void setNotification(string info)
	{
		switch (Convert.ToInt32(info))
		{
		case -1:
			ANotificationManager.SetEnabled(true);
			PlayerPrefs.SetInt("PUSH_NOTIFICATIONS_DISABLED", 0);
			AStats.Flurry.LogEvent("NOTIFICATION_SETTING_CHANGED_TO", "true");
			break;
		case -2:
			ANotificationManager.SetEnabled(false);
			PlayerPrefs.SetInt("PUSH_NOTIFICATIONS_DISABLED", 1);
			AStats.Flurry.LogEvent("NOTIFICATION_SETTING_CHANGED_TO", "false");
			break;
		}
		PlayerPrefs.Save();
		Time.timeScale = timeScaleBackup;
	}

	private void OnApplicationPause(bool state)
	{
		if (!state && !AJavaTools.Properties.IsBuildAmazon())
		{
			StartCoroutine(checkButtons());
		}
	}

	private IEnumerator checkButtons()
	{
		yield return new WaitForSeconds(0.25f);
	}
}
