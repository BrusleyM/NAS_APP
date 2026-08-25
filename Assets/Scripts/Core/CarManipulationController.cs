using NAS.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace NAS.Core
{
    /// <summary>
    /// Owns every way the placed AR car's transform can be manipulated after
    /// placement: one-finger drag to reposition, two-finger pinch to scale,
    /// and the rotation slider in the AR viewport UI (ArViewportController
    /// owns the slider control itself, this just applies the values it
    /// raises via RotationSliderChangedEvent - purely event-driven, never
    /// talks to ArViewportController or ObjectPlacerController directly).
    /// Reports interaction counts via GestureCountsUpdatedEvent for
    /// ObjectPlacerController to fold into the ArSession telemetry it owns
    /// sending, and the live scale ratio via CarScaleChangedEvent for
    /// ArViewportController's "Pinch to scale [1:x]" hint label.
    ///
    /// Two-finger twist-to-rotate was deliberately replaced by the slider -
    /// real users found twisting hard to do accurately. Deliberately does
    /// NOT read raw touches for rotation at all any more.
    /// </summary>
    public class CarManipulationController : MonoBehaviour
    {
        [Header("Gesture Settings")]
        // A gesture only counts once its cumulative movement crosses this
        // threshold - guards against counting incidental jitter (a finger
        // that barely moved while lifting off, or a slider nudge) as a real
        // reposition/scale/rotation interaction.
        [SerializeField] private float _repositionThresholdMeters = 0.03f;
        [SerializeField] private float _scaleThresholdRatio = 0.05f;
        [SerializeField] private float _rotationThresholdDegrees = 8f;
        // 1 = real-world size, never below it - shrinking the car below its
        // real dimensions defeats the point of an AR showroom placement
        // (seeing it at true scale). Scaling UP past 1 is still allowed, for
        // inspecting details up close.
        [SerializeField] private float _minScaleMultiplier = 1f;
        [SerializeField] private float _maxScaleMultiplier = 2.5f;

        private GameObject _placedInstance;
        private Vector3 _originalLocalScale;
        private float _scaleMultiplier = 1f;
        private bool _isCustomizeSheetOpen;

        private int _repositionCount;
        private int _scaleCount;
        private int _rotationCount;

        // Per-gesture-segment state, reset whenever the active touch count
        // changes (drag) or the segment restarts (pinch/rotation) - "segment"
        // means one continuous interaction, not the whole AR visit.
        private bool _hasLastDragHit;
        private Vector3 _lastDragHit;
        private float _repositionAccumThisGesture;
        private bool _repositionCountedThisGesture;

        private bool _hasLastPinchDistance;
        private float _lastPinchDistance;
        private float _scaleAtGestureStart;
        private bool _scaleCountedThisGesture;

        private float _lastSliderDegrees;
        private float _rotationAccumThisGesture;
        private bool _rotationCountedThisGesture;

        private void OnEnable()
        {
            EventBus.Subscribe<CarPlacedEvent>(OnCarPlaced);
            EventBus.Subscribe<EnterArRequestedEvent>(OnEnterAr);
            EventBus.Subscribe<CustomizeSheetToggledEvent>(OnCustomizeSheetToggled);
            EventBus.Subscribe<RotationSliderChangedEvent>(OnRotationSliderChanged);
            EventBus.Subscribe<RotationSliderReleasedEvent>(OnRotationSliderReleased);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CarPlacedEvent>(OnCarPlaced);
            EventBus.Unsubscribe<EnterArRequestedEvent>(OnEnterAr);
            EventBus.Unsubscribe<CustomizeSheetToggledEvent>(OnCustomizeSheetToggled);
            EventBus.Unsubscribe<RotationSliderChangedEvent>(OnRotationSliderChanged);
            EventBus.Unsubscribe<RotationSliderReleasedEvent>(OnRotationSliderReleased);
        }

        private void OnEnterAr(EnterArRequestedEvent evt) => ResetSession();

        private void OnCarPlaced(CarPlacedEvent evt)
        {
            _placedInstance = evt.Instance;
            _originalLocalScale = _placedInstance.transform.localScale;
            _scaleMultiplier = 1f;
            _lastSliderDegrees = 0f;
            ResetDragState();
            ResetPinchState();
            ResetRotationGestureState();
            EventBus.Publish(new CarScaleChangedEvent(_scaleMultiplier));
        }

        private void ResetSession()
        {
            _placedInstance = null;
            _repositionCount = 0;
            _scaleCount = 0;
            _rotationCount = 0;
            _scaleMultiplier = 1f;
            _lastSliderDegrees = 0f;
            ResetDragState();
            ResetPinchState();
            ResetRotationGestureState();
            PublishGestureCounts();
            EventBus.Publish(new CarScaleChangedEvent(_scaleMultiplier));
        }

        private void ResetDragState()
        {
            _hasLastDragHit = false;
            _repositionAccumThisGesture = 0f;
            _repositionCountedThisGesture = false;
        }

        private void ResetPinchState()
        {
            _hasLastPinchDistance = false;
            _scaleAtGestureStart = _scaleMultiplier;
            _scaleCountedThisGesture = false;
        }

        private void ResetRotationGestureState()
        {
            _rotationAccumThisGesture = 0f;
            _rotationCountedThisGesture = false;
        }

        // Suspend touch manipulation while the Customize sheet is open -
        // otherwise tapping/dragging across a paint swatch would also read
        // as a reposition drag on the car underneath it. The rotation
        // slider doesn't need this: it's a real UI Toolkit control, so a tap
        // that lands on a swatch simply never reaches it in the first place.
        private void OnCustomizeSheetToggled(CustomizeSheetToggledEvent evt)
        {
            _isCustomizeSheetOpen = evt.IsOpen;
            if (_isCustomizeSheetOpen)
            {
                ResetDragState();
                ResetPinchState();
            }
        }

        private void OnRotationSliderChanged(RotationSliderChangedEvent evt)
        {
            if (_placedInstance == null)
                return;

            float delta = Mathf.Abs(Mathf.DeltaAngle(_lastSliderDegrees, evt.Degrees));
            // Absolute, not cumulative - the slider's own position is the
            // single source of truth for the car's current rotation, unlike
            // the old twist gesture which only ever applied deltas.
            _placedInstance.transform.localRotation = Quaternion.Euler(0f, evt.Degrees, 0f);
            _rotationAccumThisGesture += delta;
            _lastSliderDegrees = evt.Degrees;

            if (!_rotationCountedThisGesture && _rotationAccumThisGesture >= _rotationThresholdDegrees)
            {
                _rotationCount++;
                _rotationCountedThisGesture = true;
                PublishGestureCounts();
            }
        }

        private void OnRotationSliderReleased(RotationSliderReleasedEvent evt) => ResetRotationGestureState();

        // Only ever runs once something's placed. One-finger drag
        // repositions, two-finger pinch scales - both read every frame so a
        // gesture segment naturally continues across frames.
        private void Update()
        {
            if (_placedInstance == null || _isCustomizeSheetOpen)
                return;

            if (Touchscreen.current != null)
            {
                TouchControl first = null;
                TouchControl second = null;
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed)
                        continue;
                    if (first == null)
                        first = touch;
                    else if (second == null)
                        second = touch;
                }

                if (first != null && second == null)
                {
                    ResetPinchState();
                    HandleReposition(first.position.ReadValue());
                    return;
                }

                if (first != null && second != null)
                {
                    ResetDragState();
                    HandlePinch(first.position.ReadValue(), second.position.ReadValue());
                    return;
                }
            }

            // No active touches (or running somewhere with no touchscreen at
            // all, e.g. the Editor) - fall back to a mouse drag for
            // reposition only. Pinch has no natural mouse equivalent; real
            // gesture testing happens on-device, same as every other AR
            // feature in this project.
            ResetPinchState();
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
                    PublishGestureCounts();
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

        private void HandlePinch(Vector2 posA, Vector2 posB)
        {
            float distance = Vector2.Distance(posA, posB);

            if (_hasLastPinchDistance && _lastPinchDistance > 0.001f)
            {
                float frameRatio = distance / _lastPinchDistance;
                float newMultiplier = Mathf.Clamp(_scaleMultiplier * frameRatio, _minScaleMultiplier, _maxScaleMultiplier);
                if (!Mathf.Approximately(newMultiplier, _scaleMultiplier))
                {
                    _scaleMultiplier = newMultiplier;
                    _placedInstance.transform.localScale = _originalLocalScale * _scaleMultiplier;
                    EventBus.Publish(new CarScaleChangedEvent(_scaleMultiplier));

                    if (!_scaleCountedThisGesture && Mathf.Abs(_scaleMultiplier - _scaleAtGestureStart) >= _scaleThresholdRatio)
                    {
                        _scaleCount++;
                        _scaleCountedThisGesture = true;
                        PublishGestureCounts();
                    }
                }
            }

            _lastPinchDistance = distance;
            _hasLastPinchDistance = true;
        }

        private void PublishGestureCounts() =>
            EventBus.Publish(new GestureCountsUpdatedEvent(_repositionCount, _scaleCount, _rotationCount));
    }
}
