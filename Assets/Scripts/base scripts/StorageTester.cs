using NAS.Core;
using NAS.Core.Interfaces;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class StorageTester : MonoBehaviour
{
    private async void Start()
    {
        await System.Threading.Tasks.Task.Delay(100); // Let GameManager init

        // Test 1: List existing models
        await TestListModels();

        // Test 2: Upload FBX file
        await TestUploadFbx();

        // Test 3: Download FBX file
        await TestDownloadFbx();

        await TestListModels();
        // Test 4: (Optional) Upload materials folder – see note below
    }

    private async Task TestListModels()
    {
        Debug.Log("=== ListModels ===");
        var listResult = await GameManager.Instance.ListModels();
        if (listResult.IsSuccess)
        {
            Debug.Log($"Found {listResult.Value.Count} objects:");
            foreach (var key in listResult.Value)
                Debug.Log($" - {key}");
        }
        else
        {
            Debug.LogError($"ListModels failed: {listResult.ErrorMessage}");
        }
    }

    private async Task TestUploadFbx()
    {
        Debug.Log("=== UploadFBX ===");

        // Build path to FBX – adjust filename if needed
        string fbxPath = Path.Combine(Application.dataPath, "Models", "generic car", "generic car.fbx");

        if (!File.Exists(fbxPath))
        {
            Debug.LogError($"FBX not found at {fbxPath}. Please check the path.");
            return;
        }

        // Read file size just to confirm
        byte[] fbxBytes = File.ReadAllBytes(fbxPath);
        Debug.Log($"Read FBX: {fbxPath}, size {fbxBytes.Length} bytes");

        // Upload with a key that identifies it (e.g., "test/generic_car.fbx")
        string remoteKey = "test/generic_car.fbx";
        var uploadResult = await GameManager.Instance.UploadModel(fbxPath, remoteKey);
        if (uploadResult.IsSuccess)
            Debug.Log($"Uploaded {remoteKey} successfully.");
        else
            Debug.LogError($"Upload failed: {uploadResult.ErrorMessage}");
    }

    private async Task TestDownloadFbx()
    {
        Debug.Log("=== DownloadFBX ===");
        string remoteKey = "test/generic_car.fbx";
        var downloadResult = await GameManager.Instance.DownloadModel(remoteKey);
        if (downloadResult.IsSuccess)
        {
            Debug.Log($"Downloaded {remoteKey}, size {downloadResult.Value.Length} bytes");
            // Save to persistentDataPath for inspection
            string tempPath = Path.Combine(Application.persistentDataPath, "Downloaded_generic_car.fbx");
            File.WriteAllBytes(tempPath, downloadResult.Value);
            Debug.Log($"Saved downloaded FBX to {tempPath}");
        }
        else
        {
            Debug.LogError($"Download failed: {downloadResult.ErrorMessage}");
        }
    }
}