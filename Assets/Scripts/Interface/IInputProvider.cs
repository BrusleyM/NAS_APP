using UnityEngine;

namespace NAS.Interfaces
{
    public interface IInputProvider
    {
        bool GetTap(out Vector2 screenPosition);
    }
}