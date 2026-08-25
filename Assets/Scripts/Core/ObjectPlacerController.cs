using System;
using UnityEngine;
using NAS.Core.Events;
using NAS.Core.Interfaces;
using NAS.Core.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace NAS.Core
{
    public class ObjectPlacerController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour _inputProviderBehaviour; // Must implement IInputProvider
        [SerializeField] private MonoBehaviour _placementServiceBehaviour; // Must implement IARPlacementService
        [SerializeField] private ARPlaneManager _planeManager; // Optional: for plane visibility control
        [SerializeField] private ARSession _arSession; // Disabled/re-enabled (not destroyed) across AR entry/exit - see OnEnterAr/OnExitAr
        [SerializeField] private ARAnchorManager _anchorManager; // Anchors the placed car to the hit plane so it doesn't drift when tracking is corrected - see TryPlaceObject

        [Header("Settings")]
        [SerializeField] private bool _preventMultiplePerPlane = true; // Enable/disable plane tracking
        [SerializeField] private float _minDistanceFromUser = 1f; // Placement point is pushed out to at least this far from the camera, so the car never spawns on top of the user

        [Header("Gesture Settings")]
        // A gesture only counts once its cumulative movement crosses this
        // threshold - guards against counting incidental jitter (a finger
        // that barely moved while lifting off) as a real reposition/pinch/twist.
        [SerializeField] private float _repositionThresholdMeters = 0.03f;
        [SerializeField] private float _scaleThresholdRatio = 0.05f;
        [SerializeField] private float _rotationThresholdDegrees = 8f;
        // Clamped against the scale the model was placed at, not an absolute
        // size - keeps the range sensible regardless of a given car's actual
        // real-world dimensions.
        [SerializeField] private float _minScaleMultiplier = 0.4f;
        [SerializeField] private float _maxScaleMultiplier = 2.5f;

        private IInputProvider _inputProvider;
        private IARPlacementService _placementService;
        private Coroutine _placementCoroutine;

        // Track which planes have already been used (only if _preventMultiplePerPlane is true)
        private HashSet<TrackableId> _usedPlanes = new HashSet<TrackableId>();

        // AR-session telemetry. Reset both on the very first AR entry
        // (OnEnable, no EnterArRequestedEvent fires then - see GameEvents.cs)
        // and on every re-entry (OnEnterAr).
        private int _placementCount;
        private int _repositionCount;
        private int _scaleCount;
        private int _rotationCount;
        private string _clientArSessionId;
        private DateTime _arSessionStartedAt;

        // Gesture manipulation - only becomes active once something's been
        // placed (see Update()). Anchor-relative (transform.position/localScale
        // on the placed instance itself), not world-space bookkeeping, so
        // AR Foundation's anchor drift-correction and the user's own
        // manipulation compose naturally instead of fighting each other.
        private GameObject _placedInstance;
        private Vector3 _originalLocalScale;
        private float _scaleMultiplier = 1f;
        private bool _isCustomizeSheetOpen;

        // Per-gesture-segment state, reset whenever the active touch count
        // changes (see ResetDragState/ResetPinchTwistState) - "segment" means
        // one continuous drag/pinch/twist from fingers-down to fingers-up or
        // a touch-count change, not the whole AR visit.
        private bool _hasLastDragHit;
        private Vector3 _lastDragHit;
        private float _repositionAccumThisGesture;
        private bool _repositionCountedThisGesture;

        private bool _hasLastPinchDistance;
        private float _lastPinchDistance;
        private float _scaleAtGestureStart;
        private bool _scaleCountedThisGesture;

        private bool _hasLastTwistAngle;
        private float _lastTwistAngle;
        private float _rotationAccumThisGesture;
        private bool _rotationCountedThisGesture;

        private void OnEnable()
        {
            _inputProvider = _inputProviderBehaviour as IInputProvider;
            _placementService = _placementServiceBehaviour as IARPlacementService;

            if (_inputProvider == null || _placementService == null)
            {
                Debug.LogError("Missing required dependencies. Disabling component.");
                enabled = false;
                return;
            }

            EventBus.Subscribe<EnterArRequestedEvent>(OnEnterAr);
            EventBus.Subscribe<ExitArRequestedEvent>(OnExitAr);
            EventBus.Subscribe<CustomizeSheetToggledEvent>(OnCustomizeSheetToggled);

            ResetArSessionTracking();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnterArRequestedEvent>(OnEnterAr);
            EventBus.Unsubscribe<ExitArRequestedEvent>(OnExitAr);
            EventBus.Unsubscribe<CustomizeSheetToggledEvent>(OnCustomizeSheetToggled);
        }

        // Suspend gesture manipulation while the Customize sheet is open -
        // otherwise tapping/dragging across a paint swatch would also read as
        // a reposition drag on the car underneath it. Clears in-flight
        // gesture-segment state so a drag that was happening when the sheet
        // opened doesn't resume mid-gesture once it closes.
        private void OnCustomizeSheetToggled(CustomizeSheetToggledEvent evt)
        {
            _isCustomizeSheetOpen = evt.IsOpen;
            if (_isCustomizeSheetOpen)
            {
                ResetDragState();
                ResetPinchTwistState();
            }
        }

        // AR Scene is loaded once and never reloaded (see ParentPageController.OnCarSelected) -
        // these handle re-entry/exit by disabling/re-enabling ARSession and
        // plane detection instead of relying on a fresh scene load to reset
        // them. Disabling (not destroying) ARSession is the AR
        // Foundation-documented way to pause tracking; re-enabling lets it
        // attempt to recover, which is what fixed the black camera feed on
        // the second+ AR entry - destroying and recreating it was the bug.
        private void OnEnterAr(EnterArRequestedEvent evt)
        {
            if (_arSession != null)
                _arSession.enabled = true;
            if (_planeManager != null)
                _planeManager.enabled = true;
            ResetUsedPlanes();
            ResetArSessionTracking();
        }

        private void OnExitAr(ExitArRequestedEvent evt)
        {
            SendArSessionTelemetry();
            DisablePlacement();
            if (_planeManager != null)
                _planeManager.enabled = false;
            if (_arSession != null)
                _arSession.enabled = false;
        }

        private void ResetArSessionTracking()
        {
            _placementCount = 0;
            _repositionCount = 0;
            _scaleCount = 0;
            _rotationCount = 0;
            _clientArSessionId = Guid.NewGuid().ToString();
            _arSessionStartedAt = DateTime.UtcNow;

            _placedInstance = null;
            _scaleMultiplier = 1f;
            ResetDragState();
            ResetPinchTwistState();
        }

        private void ResetDragState()
        {
            _hasLastDragHit = false;
            _repositionAccumThisGesture = 0f;
            _repositionCountedThisGesture = false;
        }

        private void ResetPinchTwistState()
        {
            _hasLastPinchDistance = false;
            _hasLastTwistAngle = false;
            _scaleAtGestureStart = _scaleMultiplier;
            _scaleCountedThisGesture = false;
            _rotationAccumThisGesture = 0f;
            _rotationCountedThisGesture = false;
        }

        // Best-effort, same philosophy as every other telemetry send in this
        // project - a failed/slow call must never block exiting AR. Sent
        // even when _placementCount is 0 ("entered AR, placed nothing" is
        // real signal too), as long as a telemetry session and a selected
        // car both exist. Uses `this` as the coroutine runner (not
        // GameManager.Instance) because this component stays enabled through
        // OnExitAr - it isn't being destroyed the way EstimatorCardController
        // is when it sends its own telemetry from OnDisable.
        private void SendArSessionTelemetry()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.TelemetrySessionId <= 0) return;

            // A visit where the real car model failed to load (network error,
            // bad glTF, etc.) isn't genuine placement behaviour - the
            // customer either stared at the wrong placeholder or immediately
            // backed out to retry, same as what actually happened in testing
            // this. A resulting 0-placement record would misread as "looked
            // and lost interest" when it's really a technical failure -
            // don't record it at all rather than ship a false signal.
            if (!gameManager.CurrentArModelLoadSucceeded) return;

            var selectedCar = gameManager.SelectedCar;
            var accessToken = gameManager.AccessToken;
            if (selectedCar == null || selectedCar.id <= 0 || string.IsNullOrEmpty(accessToken)) return;

            var resolved = EnvironmentResolver.Resolve("[NAS AR Telemetry]");
            if (resolved.Settings == null) return;

            var telemetryApi = new TelemetryApi(this, resolved.Settings, resolved.TrustAnyCertificate);
            var request = new ArSessionTelemetryRequest
            {
                customerSessionId = gameManager.TelemetrySessionId,
                clientArSessionId = _clientArSessionId,
                vehicleModelId = selectedCar.id,
                startedAt = _arSessionStartedAt.ToString("o"),
                endedAt = DateTime.UtcNow.ToString("o"),
                placementCount = _placementCount,
                repositionCount = _repositionCount,
                scaleCount = _scaleCount,
                rotationCount = _rotationCount
            };
            telemetryApi.LogArSession(request, accessToken, result =>
            {
                if (!result.Success)
                    Debug.LogWarning($"[NAS AR Telemetry] AR session telemetry failed: {result.Error.Detail}");
            });
        }

        // No auto-start here on purpose - EnablePlacement() is called by
        // SelectedCarModelLoader once it knows what to place (the downloaded
        // car model, or the default placeholder on failure). Starting
        // placement immediately in Start() risked a tap landing before that
        // async swap finished, placing the stale placeholder instead.

        /// <summary>Call this to enable placement mode (e.g., from a UI button).</summary>
        public void EnablePlacement()
        {
            if (_placementCoroutine != null)
            {
                Debug.LogWarning("Placement already active. Wait for it to complete or call DisablePlacement().");
                return;
            }

            _placementCoroutine = StartCoroutine(PlacementRoutine());
            Debug.Log("Placement mode enabled. Tap to place an object.");
        }

        /// <summary>Call this to cancel placement mode manually (e.g., if user changes mind).</summary>
        public void DisablePlacement()
        {
            if (_placementCoroutine != null)
            {
                StopCoroutine(_placementCoroutine);
                _placementCoroutine = null;
                Debug.Log("Placement mode cancelled.");
            }
        }

        private IEnumerator PlacementRoutine()
        {
            bool objectPlaced = false;

            while (!objectPlaced)
            {
                // Wait for a tap
                yield return new WaitUntil(() => _inputProvider.GetTap(out _));

                // Get the tap position
                _inputProvider.GetTap(out Vector2 screenPos);

                // Attempt to place the object (this coroutine handles the attempt)
                yield return StartCoroutine(TryPlaceObject(screenPos, (success) => objectPlaced = success));

                // Small delay to prevent immediate retry if the tap was held
                yield return new WaitForSeconds(0.1f);
            }

            // Placement finished successfully – clean up
            _placementCoroutine = null;
            Debug.Log("Placement mode ended. Call EnablePlacement() to place another.");
        }

        private IEnumerator TryPlaceObject(Vector2 screenPos, System.Action<bool> callback)
        {
            GameObject prefab = _placementService.RaycastPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("No prefab assigned to ARRaycastManager. Cannot place object.");
                callback?.Invoke(false);
                yield break;
            }

            bool success = false;

            try
            {
                if (_placementService.TryGetPlacementPose(screenPos, out Pose pose, out TrackableId planeId))
                {
                    // Check if plane already used (if enabled)
                    if (_preventMultiplePerPlane && _usedPlanes.Contains(planeId))
                    {
                        Debug.Log("This plane already has an object. Try a different plane.");
                        // Optionally provide user feedback (UI text, sound)
                    }
                    else
                    {
                        // The downloaded car model is already a live scene
                        // instance (SelectedCarModelLoader built it once and
                        // parked it inactive) - reposition and reveal that
                        // same object instead of cloning it, since only one
                        // placement ever happens per session anyway (this
                        // coroutine loop exits after the first success). The
                        // placeholder prefab fallback has no scene instance
                        // yet, so it still needs a real Instantiate().
                        // Use the raycast hit's Y as-is - it's the real
                        // detected floor height. A fixed 0f assumed local
                        // Y=0 always meant "the floor", which only held while
                        // XR Origin's CameraYOffset was compensating for it;
                        // with that offset at 0, Y=0 is wherever the AR
                        // session started tracking from, not the floor.
                        Vector3 placementPosition = pose.position;

                        // Tapping close to your own feet would spawn the car
                        // (and its origin) right on top of you - push the
                        // placement point out to at least _minDistanceFromUser
                        // along the same direction from the camera, so it
                        // never starts overlapping the user.
                        if (Camera.main != null)
                        {
                            Vector3 camPos = Camera.main.transform.position;
                            Vector3 fromCamera = new Vector3(placementPosition.x - camPos.x, 0f, placementPosition.z - camPos.z);
                            float distance = fromCamera.magnitude;
                            if (distance < _minDistanceFromUser)
                            {
                                Vector3 direction = distance > 0.001f
                                    ? fromCamera / distance
                                    : new Vector3(Camera.main.transform.forward.x, 0f, Camera.main.transform.forward.z).normalized; // camera exactly on the point - fall back to where it's facing
                                placementPosition = new Vector3(camPos.x + direction.x * _minDistanceFromUser, placementPosition.y, camPos.z + direction.z * _minDistanceFromUser);
                            }
                        }

                        ARPlane hitPlane = _planeManager != null ? _planeManager.GetPlane(planeId) : null;

                        // Attach the car to an ARAnchor pinned to the hit
                        // plane instead of just setting a raw world-space
                        // Transform - without this, a brief tracking-quality
                        // drop (a freeze) followed by AR Foundation
                        // correcting the session's coordinate frame visibly
                        // drags an unanchored object along with the
                        // correction ("car follows the camera, shifted from
                        // where it was placed"). An anchored object gets that
                        // same correction applied to its transform, so it
                        // stays pinned to its real-world point instead.
                        Pose anchoredPose = new Pose(placementPosition, pose.rotation);
                        ARAnchor anchor = (_anchorManager != null && hitPlane != null)
                            ? _anchorManager.AttachAnchor(hitPlane, anchoredPose)
                            : null;

                        GameObject placedInstance;
                        if (_placementService.RaycastPrefabIsLiveInstance)
                        {
                            placedInstance = prefab;
                            placedInstance.SetActive(true);
                        }
                        else
                        {
                            placedInstance = Instantiate(prefab);
                        }

                        if (anchor != null)
                        {
                            placedInstance.transform.SetParent(anchor.transform, worldPositionStays: false);
                            placedInstance.transform.localPosition = Vector3.zero;
                            placedInstance.transform.localRotation = Quaternion.identity;
                        }
                        else
                        {
                            Debug.LogWarning("Could not create an ARAnchor for this placement - car will not be corrected for tracking drift.");
                            placedInstance.transform.SetPositionAndRotation(anchoredPose.position, anchoredPose.rotation);
                        }

                        EventBus.Publish(new CarPlacedEvent(placedInstance));
                        _placementCount++;

                        _placedInstance = placedInstance;
                        _originalLocalScale = placedInstance.transform.localScale;
                        _scaleMultiplier = 1f;
                        ResetDragState();
                        ResetPinchTwistState();

                        if (_preventMultiplePerPlane)
                            _usedPlanes.Add(planeId);

                        // Optional: hide other planes
                        if (_planeManager != null)
                        {
                            if (hitPlane != null)
                            {
                                foreach (var plane in _planeManager.trackables)
                                    //plane.gameObject.SetActive(plane == hitPlane);
                                    if(plane.gameObject.activeSelf)
                                        plane.gameObject.SetActive(false);
                            }
                            _planeManager.enabled = false; // Stops new plane detection
                        }

                        Debug.Log("Object placed successfully.");
                        success = true;
                    }
                }
                else
                {
                    Debug.Log("No surface detected. Try again.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex, this);
            }

            // Brief cooldown to prevent accidental double-taps (optional)
            yield return new WaitForSeconds(0.25f);

            callback?.Invoke(success);
        }

        /// <summary>Optional: clear the list of used planes (e.g., for a new session).</summary>
        public void ResetUsedPlanes()
        {
            _usedPlanes.Clear();
        }

        // ---- Gesture manipulation --------------------------------------------
        // Only ever runs once something's placed (placement mode's own tap
        // loop, above, has already ended by then - see EnablePlacement's doc
        // comment). One-finger drag repositions, two-finger pinch scales,
        // two-finger twist rotates; pinch and twist read off the same two
        // touches each frame so both can happen in the same gesture, same as
        // any mobile map/photo app.

        private void Update()
        {
            if (_placedInstance == null || _isCustomizeSheetOpen)
                return;

            if (Touchscreen.current != null)
            {
                var activeTouches = new List<TouchControl>();
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed)
                        activeTouches.Add(touch);
                }

                if (activeTouches.Count == 1)
                {
                    ResetPinchTwistState();
                    HandleReposition(activeTouches[0].position.ReadValue());
                    return;
                }

                if (activeTouches.Count >= 2)
                {
                    ResetDragState();
                    HandlePinchAndTwist(activeTouches[0].position.ReadValue(), activeTouches[1].position.ReadValue());
                    return;
                }
            }

            // No active touches (or running somewhere with no touchscreen at
            // all, e.g. the Editor) - fall back to a mouse drag for
            // reposition only. Pinch/twist have no natural mouse equivalent;
            // real gesture testing happens on-device, same as every other AR
            // feature in this project.
            ResetPinchTwistState();
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                HandleReposition(Mouse.current.position.ReadValue());
            else
                ResetDragState();
        }

        private void HandleReposition(Vector2 screenPos)
        {
            if (!TryGetDragPlaneHit(screenPos, out var hitPoint))
            {
                _hasLastDragHit = false;
                return;
            }

            if (_hasLastDragHit)
            {
                Vector3 delta = hitPoint - _lastDragHit;
                _placedInstance.transform.position += delta;
                _repositionAccumThisGesture += delta.magnitude;

                if (!_repositionCountedThisGesture && _repositionAccumThisGesture >= _repositionThresholdMeters)
                {
                    _repositionCount++;
                    _repositionCountedThisGesture = true;
                }
            }

            _lastDragHit = hitPoint;
            _hasLastDragHit = true;
        }

        // Re-derived every frame from the anchor's (or the placed instance's
        // own, if it has no anchor parent) CURRENT world transform, not a
        // pose cached at placement time - AR Foundation's drift correction
        // can move the anchor mid-gesture, and dragging against a stale plane
        // would make the car visibly detach from the real floor.
        private bool TryGetDragPlaneHit(Vector2 screenPos, out Vector3 hitPoint)
        {
            hitPoint = default;
            if (Camera.main == null || _placedInstance == null)
                return false;

            Transform planeReference = _placedInstance.transform.parent != null
                ? _placedInstance.transform.parent
                : _placedInstance.transform;
            var dragPlane = new Plane(planeReference.up, planeReference.position);

            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (!dragPlane.Raycast(ray, out float enter))
                return false;

            hitPoint = ray.GetPoint(enter);
            return true;
        }

        private void HandlePinchAndTwist(Vector2 posA, Vector2 posB)
        {
            float distance = Vector2.Distance(posA, posB);
            float angle = Mathf.Atan2(posB.y - posA.y, posB.x - posA.x) * Mathf.Rad2Deg;

            if (_hasLastPinchDistance && _lastPinchDistance > 0.001f)
            {
                float frameRatio = distance / _lastPinchDistance;
                _scaleMultiplier = Mathf.Clamp(_scaleMultiplier * frameRatio, _minScaleMultiplier, _maxScaleMultiplier);
                _placedInstance.transform.localScale = _originalLocalScale * _scaleMultiplier;

                if (!_scaleCountedThisGesture && Mathf.Abs(_scaleMultiplier - _scaleAtGestureStart) >= _scaleThresholdRatio)
                {
                    _scaleCount++;
                    _scaleCountedThisGesture = true;
                }
            }

            if (_hasLastTwistAngle)
            {
                // Screen-space angle sign matches a top-down view of the car
                // (twisting clockwise turns it clockwise) since AR placement
                // keeps the car roughly upright facing the camera.
                float deltaAngle = Mathf.DeltaAngle(_lastTwistAngle, angle);
                _placedInstance.transform.Rotate(Vector3.up, -deltaAngle, Space.World);
                _rotationAccumThisGesture += Mathf.Abs(deltaAngle);

                if (!_rotationCountedThisGesture && _rotationAccumThisGesture >= _rotationThresholdDegrees)
                {
                    _rotationCount++;
                    _rotationCountedThisGesture = true;
                }
            }

            _lastPinchDistance = distance;
            _hasLastPinchDistance = true;
            _lastTwistAngle = angle;
            _hasLastTwistAngle = true;
        }
    }
}