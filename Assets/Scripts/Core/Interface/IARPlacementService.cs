using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace NAS.Core.Interfaces
{
    public interface IARPlacementService
    {
        /// <summary>Gets or sets the prefab that will be instantiated on placement.</summary>
        GameObject RaycastPrefab { get; set; }

        /// <summary>
        /// True when RaycastPrefab is already a live scene instance (e.g. the
        /// downloaded car model SelectedCarModelLoader built and parked
        /// inactive) that should just be repositioned and activated on tap,
        /// rather than cloned via Instantiate(). False (the default) for the
        /// placeholder prefab ASSET fallback, which has no scene instance yet
        /// and genuinely needs Instantiate() to create one.
        /// </summary>
        bool RaycastPrefabIsLiveInstance { get; set; }

        /// <summary>Attempts to get a placement pose from a screen position.</summary>
        bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose, out TrackableId planeId);
    }
}