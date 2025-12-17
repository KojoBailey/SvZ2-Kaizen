using UnityEngine;

[DataBundleClass(Category = "Design")]
public class CostumeSchema
{
    [DataBundleKey]
    public string id;

    [DataBundleSchemaFilter(typeof(TaggedString), false)]
	[DataBundleRecordTableFilter("LocalizedStrings")]
	public DataBundleRecordKey displayName;

    [DataBundleField(StaticResource = true, Group = DataBundleResourceGroup.All)]
	public Texture2D icon;

    [DataBundleField(StaticResource = true)]
	public Material material;
}