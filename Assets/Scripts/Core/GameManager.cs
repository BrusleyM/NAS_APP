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
        // Set by ParentPageController once the splash card has been shown/dismissed
        // for this app session, so returning to "Main App" from the AR scene (Back/
        // Confirm both reload this scene) doesn't show the splash again every time -
        // only once, on cold start. Lives here rather than as a scene-local bool on
        // ParentPageController because that controller (and its GameObject) gets
        // torn down and recreated on every "Main App" scene load; GameManager is the
        // DontDestroyOnLoad singleton that actually survives across it.
        public bool HasShownSplash { get; set; } = false;
        // Set once AR Scene has been additively loaded for the first time this
        // app session. AR Scene is deliberately never unloaded/reloaded after
        // that - re-entering AR toggles visibility/tracking instead of a full
        // scene reload, which is what was causing a black camera feed on the
        // second+ entry (destroying and recreating ARSession/XROrigin is not
        // the AR Foundation-documented pattern; disabling/re-enabling is).
        public bool IsArSceneLoaded { get; set; } = false;

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

            // Subscribing here rather than in OnEnable() is deliberate: Unity
            // guarantees every object's Awake() finishes before any object's
            // OnEnable() runs, within the same load - but does NOT guarantee
            // Awake()/OnEnable() ordering ACROSS different GameObjects
            // otherwise. This is what let a real bug happen: a UI controller
            // read GameManager.AccessToken before GameManager's own handler
            // for that login had run yet, sending an unauthenticated request
            // ("Please sign in to continue" on device). The actual structural
            // fix for THAT class of bug is the Session*Event pattern below
            // (see AuthSucceededEvent/SessionAuthenticatedEvent's doc comments
            // in GameEvents.cs) - subscribing here in Awake() is kept anyway
            // as defense in depth, so GameManager is guaranteed ready before
            // anything else in the scene, for any event it's ever given.
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

        // Publishing the Session*Event AFTER the field assignment (not
        // before) is the actual guarantee here - it's what makes it
        // structurally impossible for a subscriber to observe this event
        // before the state it describes is set, regardless of subscription
        // order. Don't reorder these.
        private void OnAuthSucceeded(AuthSucceededEvent evt)
        {
            CurrentUser = evt.User;
            AccessToken = evt.AccessToken;
            EventBus.Publish(new SessionAuthenticatedEvent(CurrentUser, AccessToken));
        }

        private void OnCarSelected(CarSelectedEvent evt)
        {
            SelectedCar = evt.Vehicle;
            EventBus.Publish(new SessionCarSelectedEvent(SelectedCar));
        }
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
