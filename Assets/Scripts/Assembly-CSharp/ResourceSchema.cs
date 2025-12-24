using UnityEngine;

[DataBundleClass(Category = "Design")]
public class ResourceSchema
{
    [DataBundleKey]
    public string id;

    [DataBundleField]
    public ECollectableType type;

    [DataBundleField]
    public int minValue;

    [DataBundleField]
    public int maxValue;

    [DataBundleField]
    public float dropRate;

    [DataBundleField(StaticResource = true)]
	public GameObject prefab;
}