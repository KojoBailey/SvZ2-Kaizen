using UnityEngine;

public struct Cost
{
	public enum Currency
	{
		Unknown = 0,
		Coin,
		Soul,
	}

	public Currency currency;

	public int price;

	public int preSalePrice;

	public bool isOnSale
	{
		get
		{
			return preSalePrice != price;
		}
	}

	public bool canAfford
	{
		get
		{
			int num = 0;
			switch (currency)
			{
			case Currency.Coin:
				num = Singleton<Profile>.Instance.coins;
				break;
			case Currency.Soul:
				num = Singleton<Profile>.Instance.souls;
				break;
			}
			return price <= num;
		}
	}

	public int percentOff
	{
		get
		{
			float num = 1f;
			if (price > 0 && preSalePrice > 0)
			{
				num = (float)price / (float)preSalePrice;
			}
			return 100 - (int)(num * 100f);
		}
	}

	public string currencyAnalyticCode
	{
		get
		{
			switch (currency)
			{
			case Currency.Coin:
				return "SC";
			case Currency.Soul:
				return "SO";
			default:
				return "??";
			}
		}
	}

	public Cost(int price, Currency currency, float salePercentage)
	{
		salePercentage /= 100f;
		this.currency = currency;
		salePercentage = Mathf.Clamp(salePercentage, 0f, 1f);
		preSalePrice = price;
		this.price = (int)((float)preSalePrice * (1f - salePercentage));
	}

	public Cost(string str, float salePercentage)
	{
		salePercentage /= 100f;
		currency = Currency.Unknown;
		preSalePrice = 0;
		price = 0;
		string[] array = str.Split(',');
		if (array == null)
		{
			return;
		}
		int num = 0;
		string[] array2 = array;
		foreach (string s in array2)
		{
			if (int.TryParse(s, out preSalePrice) && preSalePrice != 0)
			{
				switch (num)
				{
				case 0:
					currency = Currency.Coin;
					break;
				case 1:
					currency = Currency.Soul;
					break;
				}
				break;
			}
			num++;
		}
		price = (int)((float)preSalePrice * (1f - salePercentage));
	}

	public override string ToString()
	{
		string empty = string.Empty;
		if (price == 0)
		{
			return "Free";
		}
		empty = price.ToString();
		switch (currency)
		{
		case Currency.Coin:
			empty += " Coins";
			break;
		case Currency.Soul:
			empty += " Souls";
			break;
		}
		int num = percentOff;
		if (num != 0)
		{
			empty += string.Format(" at {0}% off.", num);
		}
		return empty;
	}

	public override bool Equals(object o)
	{
		Cost cost = (Cost)o;
		return price == cost.price && currency == cost.currency;
	}

	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}

	public void Spend()
	{
		switch (currency)
		{
		case Currency.Coin:
			Singleton<Profile>.Instance.SpendCoins(price);
			break;
		case Currency.Soul:
			Singleton<Profile>.Instance.souls -= price;
			break;
		}
		if (price <= 0)
		{
		}
	}

	public static bool operator ==(Cost a, Cost b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Cost a, Cost b)
	{
		return !a.Equals(b);
	}
}
