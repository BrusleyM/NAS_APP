using NAS.Interfaces;
using NAS.Storage;
using NAS.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NAS.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Environment")]
        [Tooltip("If true, uses production Cognito settings; otherwise uses development basic credentials.")]
        [SerializeField] private bool _useProduction = false;

        private IStorageService _storage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeStorage();
        }

        private void InitializeStorage()
        {
            IStorageConfig config = null;

            if (_useProduction)
            {
                config = Resources.Load<ProdStorageConfig>("Config/ProdStorageConfig");
                if (config == null)
                    Debug.LogError("ProdStorageConfig not found in Resources/Config/!");
            }
            else
            {
                config = Resources.Load<DevStorageConfig>("Config/DevStorageConfig");
                if (config == null)
                    Debug.LogError("DevStorageConfig not found in Resources/Config/!");
            }

            if (config == null) return;

            _storage = _useProduction
                ? new ProdStorageService(config)
                : new DevStorageService(config);

            Debug.Log($"GameManager: Using {(_useProduction ? "PRODUCTION" : "DEVELOPMENT")} storage.");
        }

        // Public storage methods
        public async Task<Result> UploadModel(string localFilePath, string modelKey)
        {
            if (_storage == null) return Result.Failure("Storage not initialized.");
            return await _storage.UploadModelAsync(localFilePath, modelKey);
        }

        public async Task<Result<byte[]>> DownloadModel(string modelKey)
        {
            if (_storage == null) return Result<byte[]>.Failure("Storage not initialized.");
            return await _storage.DownloadModelAsync(modelKey);
        }

        public async Task<Result<List<string>>> ListModels(string prefix = "")
        {
            if (_storage == null) return Result<List<string>>.Failure("Storage not initialized.");
            return await _storage.ListModelsAsync(prefix);
        }

        public async Task<Result> UploadModels(List<string> localFilePaths, List<string> modelKeys)
        {
            if (_storage == null) return Result.Failure("Storage not initialized.");
            return await _storage.UploadModelsAsync(localFilePaths, modelKeys);
        }

        public async Task<Result<List<(string Key, byte[] Data)>>> DownloadModels(List<string> modelKeys)
        {
            if (_storage == null) return Result<List<(string Key, byte[] Data)>>.Failure("Storage not initialized.");
            return await _storage.DownloadModelsAsync(modelKeys);
        }
    }
}