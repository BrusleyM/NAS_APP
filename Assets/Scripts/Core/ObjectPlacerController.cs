using System;
using UnityEngine;
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
            }
        }

        private void Start()
        {
            EnablePlacement();
        }

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
                        // Place the object
                        Instantiate(prefab, pose.position, pose.rotation);

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