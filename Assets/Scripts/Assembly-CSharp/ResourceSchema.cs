using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;

[DataBundleClass(Category = "Design")]
public class ResourceSchema
{
    [DataBundleKey]
    public ECollectableType type;

    [DataBundleField(StaticResource = true)]
	public GameObject prefab;

    [DataBundleField]
    public int minValue;

    [DataBundleField]
    public int maxValue;

    [DataBundleField]
    [DataBundleDefaultValue(-1)]
    public int constValue;

    public int Value
    {
        get
        {
            if (constValue == -1)
            {
                return UnityEngine.Random.Range(minValue, maxValue + 1);
            }
            return constValue;
        }
    }

    [DataBundleField]
    public float dropRate;

    [DataBundleField]
    [DataBundleDefaultValue(1)]
    public int notBeforeWave;
}