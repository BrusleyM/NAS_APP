using UnityEngine;
using UnityEngine.InputSystem;
using NAS.Interfaces;

namespace NAS.InputImplementation
{
    public class UnityInputProvider : MonoBehaviour, IInputProvider
    {
        public bool GetTap(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }
    }
}