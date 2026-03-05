using UnityEditor;
using System.IO;

public class BuildAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string bundleDirectory = "AssetBundles";
        if (!Directory.Exists(bundleDirectory))
            Directory.CreateDirectory(bundleDirectory);

        BuildPipeline.BuildAssetBundles(bundleDirectory,
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget);
        UnityEngine.Debug.Log("AssetBundles built to " + bundleDirectory);
    }
}