using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace NAS.Core.Interfaces
{
    public interface IARPlacementService
    {
        /// <summary>Gets or sets the prefab that will be instantiated on placement.</summary>
        GameObject RaycastPrefab { get; set; }

        /// <summary>Attempts to get a placement pose from a screen position.</summary>
        bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose, out TrackableId planeId);
    }
}