using GLTFast;
using NAS.Core.Interfaces;
using UnityEngine;

namespace NAS.Core
{
    /// <summary>
    /// Downloads the selected car's 3D model from Tigris and hands it to the
    /// AR placement pipeline as what gets spawned on tap, replacing the
    /// scene's default placeholder prefab. Lives on the same GameObject as
    /// ObjectPlacerController and its IARPlacementService implementation -
    /// self-discovers both via GetComponent rather than Inspector wiring.
    ///
    /// Any failure (no key on the selected car, download error, glTF parse
    /// error) falls back to whatever placeholder prefab is already assigned
    /// on the placement service - this never blocks placement outright.
    /// </summary>
    public class SelectedCarModelLoader : MonoBehaviour
    {
        private async void Start()
        {
            var objectPlacer = GetComponent<ObjectPlacerController>();
            var placementService = GetComponent<IARPlacementService>();

            if (objectPlacer == null || placementService == null)
            {
                Debug.LogError("SelectedCarModelLoader: ObjectPlacerController or IARPlacementService missing on this GameObject.");
                return;
            }

            var selectedCar = GameManager.Instance != null ? GameManager.Instance.SelectedCar : null;
            string modelKey = selectedCar != null ? selectedCar.tigrisModelKey : null;

            if (string.IsNullOrEmpty(modelKey))
            {
                Debug.LogWarning("SelectedCarModelLoader: no tigrisModelKey for the selected car - using the default placeholder.");
                objectPlacer.EnablePlacement();
                return;
            }

            var downloadResult = await GameManager.Instance.DownloadModel(modelKey);
            if (this == null) return; // AR Scene unloaded mid-download

            if (!downloadResult.IsSuccess)
            {
                Debug.LogWarning($"SelectedCarModelLoader: download failed for '{modelKey}': {downloadResult.ErrorMessage} - using the default placeholder.");
                objectPlacer.EnablePlacement();
                return;
            }

            var gltf = new GltfImport();
            bool loadOk = await gltf.LoadGltfBinary(downloadResult.Value);
            if (this == null) return;

            if (!loadOk)
            {
                Debug.LogWarning($"SelectedCarModelLoader: glTF parse failed for '{modelKey}' - using the default placeholder.");
                objectPlacer.EnablePlacement();
                return;
            }

            // Kept active but parked far below the scene so it's never visible
            // itself - Instantiate(prefab, position, rotation) always places the
            // CLONE at the given pose regardless of where this template sits,
            // but an inactive template would produce inactive (invisible) clones.
            var modelRoot = new GameObject("SelectedCarModel_" + modelKey);
            modelRoot.transform.position = new Vector3(0f, -1000f, 0f);

            bool instantiateOk = await gltf.InstantiateMainSceneAsync(modelRoot.transform);
            if (this == null) return;

            if (!instantiateOk)
            {
                Debug.LogWarning($"SelectedCarModelLoader: instantiate failed for '{modelKey}' - using the default placeholder.");
                Destroy(modelRoot);
                objectPlacer.EnablePlacement();
                return;
            }

            placementService.RaycastPrefab = modelRoot;
            Debug.Log($"SelectedCarModelLoader: '{modelKey}' ready to place.");
            objectPlacer.EnablePlacement();
        }
    }
}
