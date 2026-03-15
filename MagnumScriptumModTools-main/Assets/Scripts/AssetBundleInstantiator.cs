using UnityEngine;

public class AssetBundleInstantiator : MonoBehaviour
{
    public string path = string.Empty;
    public string assetName = string.Empty;
    public bool Test = false;
    public bool Reset = false;

    public void InstantiateBundle()
    {
        var go = Instantiate(AssetBundle.LoadFromFile(path).LoadAsset(assetName, typeof(GameObject))) as GameObject;
    }

    private void OnValidate()
    {
        if (Test)
        {
            Test = false;
            InstantiateBundle();
        }
        if (Reset)
        {
            Reset = false;
            AssetBundle.UnloadAllAssetBundles(true);
        }
    }
}