using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime;
using UnityEngine;

public class Souls : WeakGlobalInstance<Souls>
{
	private SoulSchema mSchema;
	public SoulSchema schema
	{
		get  { return mSchema; }
	}

	public int maxSouls
	{
		get { return mSchema.maxSouls; }
	}

	private int mSouls;
	public int souls
	{
		get { return mSouls; }
		set { mSouls = Mathf.Clamp(value, 0, maxSouls); }
	}

	public Hero hero { get; set; }

	public int level
	{
		get { return Singleton<Profile>.Instance.soulsLevel; }
	}

	public Souls() {}

	public Souls(int playerIndex)
	{
		if (playerIndex == 0)
		{
			SetUniqueInstance(this);

			var dataBundleRecordKey = new DataBundleRecordKey(
				Singleton<Profile>.Instance.heroId, Singleton<Profile>.Instance.soulsLevel.ToString());
			mSchema = DataBundleUtils.InitializeRecord<SoulSchema>(dataBundleRecordKey);
		}
	}
}
