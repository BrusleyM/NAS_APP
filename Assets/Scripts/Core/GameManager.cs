using NAS.Core.Interfaces;
using NAS.Core.Events;
using NAS.Storage;
using NAS.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAS.Core.Models;
using UnityEngine;

namespace NAS.Core
{
    /// <summary>
    /// Holds session state and owns the storage service. It no longer has its
    /// CurrentUser/SelectedCar properties set directly by whichever controller
    /// happens to be handling a click — it derives them by listening to the
    /// same domain events everything else reacts to.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Environment")]
        [Tooltip("If true, uses production Cognito settings; otherwise uses development basic credentials.")]
        [SerializeField] private bool _useProduction = false;

        [Tooltip("Project-wide switch for which backend API endpoint to use. Local = http://localhost:5080 (safe default, always works). ApiDomain = https://api.nas.test:8443 via the optional local nginx proxy (NAS_Backend/nginx/README.md) - only works on a machine that has that setup running (nginx + mkcert + /etc/hosts). Keep this at Local in anything committed/pushed, or teammates without that setup will have auth silently fail. AuthController reads this in Start() rather than owning its own toggle.")]
        [SerializeField] private AppEnvironment _environment = AppEnvironment.Local;
        public AppEnvironment CurrentEnvironment => _environment;

        [Header("Session")]
        public User CurrentUser { get; private set; }
        public string AccessToken { get; private set; }
        public VehicleInfo SelectedCar { get; private set; }
        public bool ReturnToEstimator { get; set; } = false;

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

        private void OnEnable()
        {
            EventBus.Subscribe<AuthSucceededEvent>(OnAuthSucceeded);
            EventBus.Subscribe<CarSelectedEvent>(OnCarSelected);
            EventBus.Subscribe<ReturnToEstimatorRequestedEvent>(OnReturnToEstimatorRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AuthSucceededEvent>(OnAuthSucceeded);
            EventBus.Unsubscribe<CarSelectedEvent>(OnCarSelected);
            EventBus.Unsubscribe<ReturnToEstimatorRequestedEvent>(OnReturnToEstimatorRequested);
        }

        private void OnAuthSucceeded(AuthSucceededEvent evt)
        {
            CurrentUser = evt.User;
            AccessToken = evt.AccessToken;
        }
        private void OnCarSelected(CarSelectedEvent evt) => SelectedCar = evt.Vehicle;
        private void OnReturnToEstimatorRequested(ReturnToEstimatorRequestedEvent evt) => ReturnToEstimator = true;

        private void InitializeStorage()
        {
            IStorageConfig config = Resources.Load<DevStorageConfig>("Config/DevStorageConfig");
            if (config == null)
            {
                Debug.LogError("DevStorageConfig not found in Resources/Config/!");
                return;
            }

            // _useProduction is currently dead - only affects the log line below, not
            // which service gets constructed. Deliberately not fixed yet (no plan for
            // multiple Tigris environments at this stage) - see .claude/CLAUDE.md's
            // Tigris storage TODO for what to do here once AR integration starts:
            // follow AuthController's single-enum pattern, not a second drifting bool.
            _storage = new DevStorageService(config);

            Debug.Log($"GameManager: Using {(_useProduction ? "PRODUCTION" : "DEVELOPMENT")} storage.");
        }

        // Public storage methods — unchanged.
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
