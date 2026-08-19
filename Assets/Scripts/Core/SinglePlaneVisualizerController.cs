using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using NAS.Core.Events;

namespace NAS.Core
{
    // ARPlaneManager instantiates one growing debug-mesh GameObject per
    // detected ARPlane trackable. On real devices this produces multiple
    // overlapping meshes as ARKit reports separate surfaces (or hasn't yet
    // merged nearby ones into a single plane). Detection/tracking itself is
    // unaffected either way - this only controls which plane's mesh is
    // visible, so raycasts against non-primary planes still work exactly as
    // before.
    public class SinglePlaneVisualizerController : MonoBehaviour
    {
        [SerializeField] private ARPlaneManager _planeManager;
        private TrackableId _primaryPlaneId = TrackableId.invalidId;

        private void OnEnable()
        {
            if (_planeManager != null)
                _planeManager.planesChanged += OnPlanesChanged;
            EventBus.Subscribe<EnterArRequestedEvent>(OnEnterAr);
        }

        private void OnDisable()
        {
            if (_planeManager != null)
                _planeManager.planesChanged -= OnPlanesChanged;
            EventBus.Unsubscribe<EnterArRequestedEvent>(OnEnterAr);
        }

        // Re-entering AR reuses the same ARPlaneManager rather than reloading
        // the scene (see ObjectPlacerController.OnEnterAr) - reset which
        // plane counts as primary so the first plane detected in the new
        // session wins again, instead of comparing against a stale id from
        // the previous session.
        private void OnEnterAr(EnterArRequestedEvent evt) => _primaryPlaneId = TrackableId.invalidId;

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            foreach (var plane in args.added)
            {
                if (_primaryPlaneId == TrackableId.invalidId)
                    _primaryPlaneId = plane.trackableId;
                else if (plane.trackableId != _primaryPlaneId)
                    plane.gameObject.SetActive(false);
            }
            foreach (var plane in args.removed)
            {
                if (plane.trackableId == _primaryPlaneId)
                {
                    _primaryPlaneId = TrackableId.invalidId;
                    break;
                }
            }
        }
    }
}
