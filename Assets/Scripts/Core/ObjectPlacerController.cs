using System;
using UnityEngine;
using NAS.Core.Events;
using NAS.Core.Interfaces;
using System.Collections;
using System.Collections.Generic;
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

        [Header("Settings")]
        [SerializeField] private bool _preventMultiplePerPlane = true; // Enable/disable plane tracking

        private IInputProvider _inputProvider;
        private IARPlacementService _placementService;
        private Coroutine _placementCoroutine;

        // Track which planes have already been used (only if _preventMultiplePerPlane is true)
        private HashSet<TrackableId> _usedPlanes = new HashSet<TrackableId>();

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
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnterArRequestedEvent>(OnEnterAr);
            EventBus.Unsubscribe<ExitArRequestedEvent>(OnExitAr);
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
        }

        private void OnExitAr(ExitArRequestedEvent evt)
        {
            DisablePlacement();
            if (_planeManager != null)
                _planeManager.enabled = false;
            if (_arSession != null)
                _arSession.enabled = false;
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
                        GameObject placedInstance;
                        if (_placementService.RaycastPrefabIsLiveInstance)
                        {
                            placedInstance = prefab;
                            placedInstance.transform.SetPositionAndRotation(pose.position, pose.rotation);
                            placedInstance.SetActive(true);
                        }
                        else
                        {
                            placedInstance = Instantiate(prefab, pose.position, pose.rotation);
                        }
                        EventBus.Publish(new CarPlacedEvent(placedInstance));

                        if (_preventMultiplePerPlane)
                            _usedPlanes.Add(planeId);

                        // Optional: hide other planes
                        if (_planeManager != null)
                        {
                            ARPlane hitPlane = _planeManager.GetPlane(planeId);
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
    }
}