using NAS.Core;
using NAS.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class AssetBundleTest : MonoBehaviour
{
    [Header("AssetBundle Settings")]
    [SerializeField] private string _localBundlePath = "AssetBundles/carmodels/genericcar"; // relative to project folder
    [SerializeField] private string _remoteBundleKey = "models/carmodels.bundle";
    [SerializeField] private string _assetName = "genericcar"; // name used when assigning bundle in Unity

    [Header("Instantiation")]
    [SerializeField] private Vector3 _spawnPosition = Vector3.zero;
    [SerializeField] private Quaternion _spawnRotation = Quaternion.identity;
    private bool _shouldUpload = true;
    private async void Start()
    {
        await Task.Delay(200); // let GameManager initialize

        // Run the complete test flow
        if (!await ListModels("models/", "before upload")) return;
        if (_shouldUpload)
        {
            if (!await UploadBundle()) return;
            if (!await ListModels("models/", "after upload")) return;
        }
        if (!await DownloadAndInstantiate()) return;

        Debug.Log("=== ASSET BUNDLE TEST COMPLETED SUCCESSFULLY ===");
    }

    /// <summary>List all objects with a given prefix.</summary>
    private async Task<bool> ListModels(string prefix, string context)
    {
        Debug.Log($"--- Listing models (prefix '{prefix}') {context} ---");
        Result<List<string>> result = await GameManager.Instance.ListModels(prefix);
        if (!result.IsSuccess)
        {
            Debug.LogError($"List failed {context}: {result.ErrorMessage}");
            return false;
        }

        Debug.Log($"Found {result.Value.Count} objects:");
        if (result.Value.Count == 0) return true;
        foreach (string key in result.Value)
            Debug.Log($"   {key}");
        return true;
    }

    /// <summary>Upload the local AssetBundle file to the bucket.</summary>
    private async Task<bool> UploadBundle()
    {
        Debug.Log("--- Uploading AssetBundle ---");

        // Resolve absolute path
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", _localBundlePath));
        
        Debug.Log("Checking bundle at: " + fullPath);
        Debug.Log("File exists? " + File.Exists(fullPath));
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Bundle file not found at {fullPath}. Build it first.");
            return false;
        }

        Debug.Log($"Uploading {fullPath} → {_remoteBundleKey}");
        Result uploadResult = await GameManager.Instance.UploadModel(fullPath, _remoteBundleKey);
        if (!uploadResult.IsSuccess)
        {
            Debug.LogError($"Upload failed: {uploadResult.ErrorMessage}");
            return false;
        }

        Debug.Log("Upload successful.");
        return true;
    }

    /// <summary>Download the bundle and instantiate the car.</summary>
    private async Task<bool> DownloadAndInstantiate()
    {
        Debug.Log("--- Downloading and instantiating AssetBundle ---");

        Result<byte[]> downloadResult = await GameManager.Instance.DownloadModel(_remoteBundleKey);
        if (!downloadResult.IsSuccess) return false;

        // Use Async to prevent frame stutters
        AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromMemoryAsync(downloadResult.Value);
    
        // Wait for the bundle to decompress
        while (!bundleRequest.isDone) await Task.Yield();

        AssetBundle bundle = bundleRequest.assetBundle;
        if (bundle == null) return false;

        // Recommended: Use the full path or lowercase name found in bundle.GetAllAssetNames()
        // often Unity stores them as: "assets/path/to/your/fbx_name.fbx"
        GameObject carPrefab = bundle.LoadAsset<GameObject>(_assetName);
    
        if (carPrefab != null)
        {
            //Instantiate(carPrefab, _spawnPosition, _spawnRotation);
            GameObject carInstance = Instantiate(carPrefab, _spawnPosition, _spawnRotation);

            foreach (Renderer rend in carInstance.GetComponentsInChildren<Renderer>())
            {
                foreach (Material mat in rend.materials)
                {
                    // Force the material to find the URP shader already in memory
                    mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                }
            }
        }
    
        // Unload(false) keeps the instantiated object but frees the bundle's compressed data
        bundle.Unload(false); 
        return true;
    }

}