using System.Collections.Generic;

public class StoreItemListController : GluiSimpleCollectionController
{
	public string contentType;

	public override int initialIndexToShow
	{
		get
		{
			int num = 0;
			object[] array = mData;
			foreach (object obj in array)
			{
				var item = obj as StoreData.Item;
				if (item.isNew)
				{
					return num;
				}
				num++;
			}
			return 0;
		}
	}

	public override void ReloadData(object arg)
	{
		string text = contentType;

		if (arg != null && arg is string)
		{
			text = arg as string;
		}

		if (text != null)
		{
			List<StoreData.Item> list = StoreAvailability.GetList(text);
			if (list != null)
			{
				contentType = text;
				mData = list.ToArray();
			}
		}
		else
		{
			mData = new object[0];
		}
	}
}
