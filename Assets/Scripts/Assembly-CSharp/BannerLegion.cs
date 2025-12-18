public class BannerLegion : Banner
{
	protected override string uiPrefabPath
	{
		get
		{
			return "UI/Prefabs/HUD/Banner_LegionOnTheLoose";
		}
	}

	public BannerLegion(float fTimeBeforeFade)
		: base(fTimeBeforeFade)
	{
	}

	protected override void InitText()
	{
	}
}
