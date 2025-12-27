using System.Collections.Generic;
using UnityEngine;

public class HUDSouls : UIHandlerComponent
{
	private GameObject mRootObject;

	private GluiText mSoulsLevel;

	private GluiText mSoulsCount;

	private int mPreviousSoulsCount = -1;

	private List<GluiMeter> mMeters = new List<GluiMeter>(4);

	private GluiMeter mActiveMeter;

	private bool mEnabled = true;

	public bool enabled
	{
		get { return mEnabled; }
		set
		{
			if (value != mEnabled)
			{
				mEnabled = value;
				mRootObject.SetActive(mEnabled);
			}
		}
	}

	public HUDSouls(GameObject uiParent)
	{
		mRootObject = uiParent.FindChild("HUD_Souls");
		mSoulsLevel = mRootObject.FindChildComponent<GluiText>("Swap_Text_Level");
		mSoulsCount = mRootObject.FindChildComponent<GluiText>("Swap_Text_Counter");
		int num = 0;
		while (true)
		{
			string name = string.Format("Meter_Souls_{0:D2}", num);
			Transform transform = ObjectUtils.FindTransformInChildren(mRootObject.transform, name);
			if (transform == null) break;
			mMeters.Add(transform.gameObject.GetComponent<GluiMeter>());
			num++;
		}

		int level = WeakGlobalInstance<Souls>.Instance.level;
		ActivateMeter(level);
		mSoulsLevel.Text = string.Format(StringUtils.GetStringFromStringRef("MenuFixedStrings", "stat_level"), level);
	}

	private void ActivateMeter(int meterLevel)
	{
		mActiveMeter = null;
		for (int i = 0; i < mMeters.Count; i++)
		{
            bool isMeterActive = i == meterLevel - 1;
            mMeters[i].gameObject.SetActive(isMeterActive);
			if (isMeterActive)
			{
				mActiveMeter = mMeters[i];
			}
		}
	}

	public void Update(bool updateExpensiveVisuals)
	{
		Update();
	}

	public void Update()
	{
		if (!mEnabled) return;

		int souls = WeakGlobalInstance<Souls>.Instance.souls;
		if (souls != mPreviousSoulsCount)
		{
			mPreviousSoulsCount = souls;
			mSoulsCount.Text = souls.ToString();
		}
		mActiveMeter.Value = (float)souls / WeakGlobalInstance<Souls>.Instance.maxSouls;
	}

	public bool OnUIEvent(string eventID) { return true; }

	public void OnPause(bool pause) {}
}
