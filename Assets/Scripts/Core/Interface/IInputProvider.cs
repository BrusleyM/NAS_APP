using UnityEngine;

namespace NAS.Core.Interfaces
{
    public interface IInputProvider
    {
        bool GetTap(out Vector2 screenPosition);
    }
}