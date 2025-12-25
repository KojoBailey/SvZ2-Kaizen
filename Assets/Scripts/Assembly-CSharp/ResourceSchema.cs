using UnityEngine;

[DataBundleClass(Category = "Design")]
public class ResourceSchema
{
    // [DataBundleKey]
    // public string id;

    [DataBundleKey]
    public ECollectableType type;

    [DataBundleField(StaticResource = true)]
	public GameObject prefab;

    [DataBundleField]
    public int minValue;

    [DataBundleField]
    public int maxValue;

    public int RandomValue
    {
        get { return UnityEngine.Random.Range(minValue, maxValue + 1); }
    }

    [DataBundleField]
    public float dropRate;
}