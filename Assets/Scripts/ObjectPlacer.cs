using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private ARPlaneManager _planeManager;
    
    private bool _isPlacing = false;
    private ARPlane _targetPlane;

    private void Update()
    {
        if (!_raycastManager) return;

        bool pressed = false;
        Vector2 pressPosition = Vector2.zero;

        // Check for mouse click (left button)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
            pressPosition = Mouse.current.position.ReadValue();
        }
        // Check for touch begin
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pressed = true;
            pressPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (pressed && !_isPlacing)
        {
            _isPlacing = true;
            PlaceObject(pressPosition);
        }
    }

    private void PlaceObject(Vector2 position)
    {
        var rayHits = new List<ARRaycastHit>();
        if (_raycastManager.Raycast(position, rayHits, TrackableType.PlaneWithinPolygon))
        {
            // Use the hit's pose – that's the exact intersection point
            Pose hitPose = rayHits[0].pose;
            Instantiate(_raycastManager.raycastPrefab, hitPose.position, hitPose.rotation);

            // If you still need the plane reference (e.g., to disable others later), keep this
            _targetPlane = _planeManager.GetPlane(rayHits[0].trackableId);
        }
        StartCoroutine(ResetPlacementFlagAfterDelay());
    }

    private IEnumerator ResetPlacementFlagAfterDelay(float delay = 0.25f)
    {
        yield return new WaitForSeconds(delay);
        _isPlacing = false;
    }

    public void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> changes)
    {
        if (_targetPlane)
        {
            foreach (var plane in changes.added)
            {
                if (plane != _targetPlane)
                    plane.gameObject.SetActive(false);
            }
        }

        foreach (var plane in changes.updated)
        {
            // handle updated planes if needed
        }

        foreach (var plane in changes.removed)
        {
            // handle removed planes if needed
        }
    }
}