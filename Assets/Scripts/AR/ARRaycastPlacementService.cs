using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using NAS.Interfaces;

namespace NAS.ARImplementation
{
    public class ARRaycastPlacementService : MonoBehaviour, IARPlacementService
    {
        [SerializeField] private ARRaycastManager _raycastManager;

        public GameObject RaycastPrefab
        {
            get => _raycastManager != null ? _raycastManager.raycastPrefab : null;
            set
            {
                if (_raycastManager != null)
                    _raycastManager.raycastPrefab = value;
                else
                    Debug.LogError("Cannot set RaycastPrefab: ARRaycastManager is missing.", this);
            }
        }

        private void Awake()
        {
            if (_raycastManager == null)
                throw new MissingComponentException($"{nameof(ARRaycastPlacementService)} requires an ARRaycastManager reference.");
        }

        public bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose, out TrackableId planeId)
        {
            pose = Pose.identity;
            planeId = TrackableId.invalidId;
            var hits = new List<ARRaycastHit>();
            if (_raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                pose = hits[0].pose;
                planeId = hits[0].trackableId;
                return true;
            }
            return false;
        }
    }
}