using NAS.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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
    ///
    /// Touch handling is event-driven (EnhancedTouch's onFingerDown/Move/Up),
    /// not a per-frame Update() poll - on the real target platform (a
    /// touchscreen device), Update() below never does more than a single
    /// early-out null check; it only does real work as an Editor-only mouse
    /// fallback, since a mouse can't fire finger events.
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
        private bool _isRotationSliderActive;

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
            EventBus.Subscribe<RotationSliderGrabbedEvent>(OnRotationSliderGrabbed);
            EventBus.Subscribe<RotationSliderChangedEvent>(OnRotationSliderChanged);
            EventBus.Subscribe<RotationSliderReleasedEvent>(OnRotationSliderReleased);

            // Ref-counted internally - safe even if something else in the
            // project also enables it. This is the only place in the
            // project reading touches through EnhancedTouch specifically
            // (UnityInputProvider's tap detection uses the lower-level
            // Touchscreen.current.primaryTouch instead, which needs no
            // enabling).
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CarPlacedEvent>(OnCarPlaced);
            EventBus.Unsubscribe<EnterArRequestedEvent>(OnEnterAr);
            EventBus.Unsubscribe<CustomizeSheetToggledEvent>(OnCustomizeSheetToggled);
            EventBus.Unsubscribe<RotationSliderGrabbedEvent>(OnRotationSliderGrabbed);
            EventBus.Unsubscribe<RotationSliderChangedEvent>(OnRotationSliderChanged);
            EventBus.Unsubscribe<RotationSliderReleasedEvent>(OnRotationSliderReleased);

            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerMove -= OnFingerMove;
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
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
        // as a reposition drag on the car underneath it.
        private void OnCustomizeSheetToggled(CustomizeSheetToggledEvent evt)
        {
            _isCustomizeSheetOpen = evt.IsOpen;
            if (_isCustomizeSheetOpen)
            {
                ResetDragState();
                ResetPinchState();
            }
        }

        // The slider is a real UI Toolkit control, so a tap that lands on
        // it never gets consumed by anything else in the UI - but the SAME
        // physical touch is still visible to Update()'s raw Touchscreen
        // read below, which has no idea a UI element is also handling it.
        // Without this suspension, dragging the slider also drags the car
        // underneath: most noticeable once the slider hits its -180/180
        // limit and stops producing RotationSliderChangedEvents, but the
        // finger is still moving - that continued movement was leaking
        // straight into HandleReposition.
        private void OnRotationSliderGrabbed(RotationSliderGrabbedEvent evt)
        {
            _isRotationSliderActive = true;
            ResetDragState();
            ResetPinchState();
        }

        private void OnRotationSliderChanged(RotationSliderChangedEvent evt)
        {
            if (_placedInstance == null)
                return;

            float delta = Mathf.Abs(Mathf.DeltaAngle(_lastSliderDegrees, evt.Degrees));
            // Absolute, not cumulative - the slider's own position is the
            // single source of truth for the car's current rotation, unlike
            // the old twist gesture which only ever applied deltas. Negated:
            // dragging the slider right (increasing degrees) should turn the
            // car the same way it turns for a swipe-right in AR, which is
            // the opposite sign from a raw Y-axis Euler increase given how
            // the camera views the placed car.
            _placedInstance.transform.localRotation = Quaternion.Euler(0f, -evt.Degrees, 0f);
            _rotationAccumThisGesture += delta;
            _lastSliderDegrees = evt.Degrees;

            if (!_rotationCountedThisGesture && _rotationAccumThisGesture >= _rotationThresholdDegrees)
            {
                _rotationCount++;
                _rotationCountedThisGesture = true;
                PublishGestureCounts();
            }
        }

        private void OnRotationSliderReleased(RotationSliderReleasedEvent evt)
        {
            _isRotationSliderActive = false;
            ResetRotationGestureState();
        }

        // Fires once per finger touching down - no gesture work needed here,
        // HandleReposition/HandlePinch below establish their own baseline on
        // the first onFingerMove after a touch-count change.
        private void OnFingerDown(Finger finger)
        {
        }

        // Only ever does anything once something's placed. Reads
        // Touch.activeTouches (this frame's full active set), not just the
        // finger that triggered this particular callback, so a two-finger
        // pinch/rotate always sees both current positions regardless of
        // which finger's movement fired the event.
        private void OnFingerMove(Finger finger)
        {
            if (_placedInstance == null || _isCustomizeSheetOpen || _isRotationSliderActive)
                return;

            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count == 1)
            {
                ResetPinchState();
                HandleReposition(activeTouches[0].screenPosition);
            }
            else if (activeTouches.Count >= 2)
            {
                ResetDragState();
                HandlePinch(activeTouches[0].screenPosition, activeTouches[1].screenPosition);
            }
        }

        // A finger lifting changes the active count for whichever gesture
        // was in progress (2->1 means the pinch/rotate segment is over, even
        // though a drag could still start fresh with the remaining finger;
        // 1->0 means everything stops) - reset both unconditionally rather
        // than trying to infer which one to keep, since either can restart
        // cleanly from its own next onFingerMove.
        private void OnFingerUp(Finger finger)
        {
            ResetDragState();
            ResetPinchState();
        }

        // Real touchscreen devices (the actual AR target) are handled
        // entirely by OnFingerDown/Move/Up above - EnhancedTouch is
        // event-driven, so this never runs any real work there beyond the
        // Touchscreen.current check failing fast. This only exists for
        // Editor mouse-drag testing (reposition only - pinch has no natural
        // mouse equivalent, and a mouse can't fire finger events at all).
        private void Update()
        {
            if (Touchscreen.current != null)
                return;
            if (_placedInstance == null || _isCustomizeSheetOpen || _isRotationSliderActive)
                return;

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
